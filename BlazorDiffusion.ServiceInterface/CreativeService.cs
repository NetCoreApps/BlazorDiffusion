using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace BlazorDiffusion.ServiceInterface;

public class CreativeService : Service
{
    public IStableDiffusionClient StableDiffusionClient { get; set; }
    public IDbConnectionFactory DbConnectionFactory { get; set; }
    public const string DefaultEngine = "stable-diffusion-v1-5";
    public const int DefaultHeight = 512;
    public const int DefaultWidth = 512;
    public const int DefaultImages = 4;
    public const int DefaultSteps = 50;

    public const int DefaultModeratorImages = 9;
    public const int DefaultModeratorSteps = 50;

    public const int DefaultMaxWidth = 896;
    public const int DefaultMaxHeight = 896;


    public async Task<object> Post(CreateCreative request)
    {
        var modifiers = await Db.SelectAsync<Modifier>(x => Sql.In(x.Id, request.ModifierIds));
        var artists = request.ArtistIds.Count == 0 ? new List<Artist>() :
            await Db.SelectAsync<Artist>(x => Sql.In(x.Id, request.ArtistIds));
        
        var imageGenerationResponse = await GenerateImage(request,modifiers,artists);

        var creativeId = await PersistCreative(request, imageGenerationResponse,modifiers,artists);

        return await Db.SaveCreativeAsync(creativeId, StableDiffusionClient);
    }
    
    public async Task<object> Patch(UpdateCreative request)
    {
        var creative = await Db.LoadSingleByIdAsync<Creative>(request.Id);
        if (creative == null)
            throw HttpError.NotFound("Creative not found");

        var session = await GetSessionAsync();
        if (!await session.IsOwnerOrModerator(AuthRepositoryAsync, creative.OwnerId))
            throw HttpError.Forbidden("You don't own this Creative");

        var artifactId = request.UnpinPrimaryArtifact == true
            ? null
            : request.PrimaryArtifactId;

        if (artifactId != null)
        {
            var artifact = creative.Artifacts.SingleOrDefault(x => x.Id == request.PrimaryArtifactId);
            if (artifact == null)
                throw HttpError.NotFound($"Artifact not found");
        }

        await Db.UpdateOnlyAsync(() => 
            new Creative { 
                PrimaryArtifactId = artifactId,
                ModifiedBy = session.UserAuthId,
                ModifiedDate = DateTime.UtcNow,
            }, where:x => x.Id == request.Id);

        return await Db.SaveCreativeAsync(request.Id, StableDiffusionClient);
    }

    public async Task<object> Patch(UpdateArtifact request)
    {
        var artifact = await Db.SingleByIdAsync<Artifact>(request.Id);
        if (artifact == null)
            throw HttpError.NotFound("Artifact not found");

        var creative = await Db.SingleByIdAsync<Creative>(artifact.CreativeId);
        
        var session = await GetSessionAsync();
        if (!await session.IsOwnerOrModerator(AuthRepositoryAsync, creative?.OwnerId))
            throw HttpError.Forbidden("You don't own this Creative");

        if (request.Nsfw != null)
        {
            await Db.UpdateOnlyAsync(() => new Artifact {
                Nsfw = request.Nsfw,
                ModifiedBy = session.UserAuthId,
                ModifiedDate = DateTime.UtcNow,
            }, where: x => x.Id == request.Id);
        }
        if (request.Quality != null)
        {
            await Db.UpdateOnlyAsync(() => new Artifact {
                Quality = request.Quality.Value,
                ModifiedBy = session.UserAuthId,
                ModifiedDate = DateTime.UtcNow,
            }, where: x => x.Id == request.Id);
        }

        await Db.SaveCreativeAsync(artifact.CreativeId, StableDiffusionClient);

        return artifact;
    }
    
    private async Task<int> PersistCreative(CreateCreative request, 
        ImageGenerationResponse imageGenerationResponse,
        List<Modifier> modifiers, 
        List<Artist> artists)
    {
        request.UserPrompt = request.UserPrompt.Trim();
        string userAuthId = (await GetSessionAsync()).UserAuthId;
        var userId = userAuthId?.ToInt();
        var now = DateTime.UtcNow;
        var creative = new Creative().PopulateWith(request)
            .WithAudit(userAuthId, now);
        creative.Width = request.Width ?? DefaultWidth;
        creative.Height = request.Height ?? DefaultHeight;
        creative.Steps = request.Steps ?? DefaultSteps;
        creative.OwnerId = userId;
        creative.Key = imageGenerationResponse.Key;
        creative.ArtistNames = artists.Select(x => $"{x.FirstName} {x.LastName}").ToList();
        creative.ModifierNames = modifiers.Select(x => x.Name).ToList();
        creative.Prompt = ConstructPrompt(request.UserPrompt, modifiers, artists);
        creative.RefId = Guid.NewGuid().ToString("D");

        using var db = DbConnectionFactory.OpenDbConnection();
        using var transaction = db.OpenTransaction();
        await db.SaveAsync(creative);
        
        var creativeArtists = request.ArtistIds.Select(x => new CreativeArtist {
            ArtistId = x,
            CreativeId = creative.Id
        });
        var creativeModifiers = request.ModifierIds.Select(x => new CreativeModifier {
            CreativeId = creative.Id,
            ModifierId = x
        });
        
        await db.InsertAllAsync(creativeArtists);
        await db.InsertAllAsync(creativeModifiers);

        var artifacts = imageGenerationResponse.Results.Select(x => new Artifact {
            CreativeId = creative.Id,
            Width = x.Width,
            Height = x.Height,
            Prompt = x.Prompt,
            Seed = x.Seed,
            FileName = x.FileName,
            FilePath = x.FilePath,
            ContentType = MimeTypes.ImagePng,
            ContentLength = x.ContentLength,
            RefId = Guid.NewGuid().ToString("D"),
        }.WithAudit(userAuthId, now));
        await db.InsertAllAsync(artifacts);
        transaction.Commit();

        return creative.Id;
    }
    
    private async Task<ImageGenerationResponse> GenerateImage(CreateCreative request,
        List<Modifier> modifiers, List<Artist> artists)
    {
        var authSession = await GetSessionAsync().ConfigAwait();
        var userRoles = await authSession.GetRolesAsync(AuthRepositoryAsync);
        var adminOrMod = userRoles.Contains(AppRoles.Admin) || userRoles.Contains(AppRoles.Moderator);
        var apiPrompt = ConstructPrompt(request.UserPrompt, modifiers, artists);

        var maxHeight = adminOrMod
            ? request.Height ?? DefaultHeight
            : request.Height > request.Width
                ? DefaultMaxHeight
                : DefaultHeight;
        var maxWidth = adminOrMod
            ? request.Width ?? DefaultWidth
            : request.Width > request.Height
               ? DefaultMaxWidth
               : DefaultWidth;

        var imageGenOptions = new ImageGeneration
        {
            Prompt = apiPrompt,
            Engine = DefaultEngine,
            Height = Math.Min(request.Height ?? DefaultHeight, maxHeight),
            Width = Math.Min(request.Width ?? DefaultWidth, maxWidth),
            Images = request.Images ?? (adminOrMod ? DefaultModeratorImages : DefaultImages),
            Steps = request.Steps ?? (adminOrMod ? DefaultModeratorSteps : DefaultSteps),
            Seed = request.Seed
        };

        ImageGenerationResponse imageGenerationResponse;
        try
        {
            imageGenerationResponse = await StableDiffusionClient.GenerateImageAsync(imageGenOptions);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw HttpError.ServiceUnavailable($"Failed to generate image: {e.Message}");
        }
        return imageGenerationResponse;
    }

    private string ConstructPrompt(string userPrompt, List<Modifier> modifiers, List<Artist> artists)
    {
        var finalPrompt = userPrompt;
        finalPrompt += $", {modifiers.Select(x => x.Name).Join(",").TrimEnd(',')}";
        var artistsSuffix = artists.Select(x => $"by {x.FirstName} {x.LastName}").Join(",").TrimEnd(',');
        if(artists.Count > 0)
            finalPrompt += $", {artistsSuffix}";
        return finalPrompt;
    }

    public async Task Delete(DeleteCreative request)
    {
        var creative = await Db.SingleByIdAsync<Creative>(request.Id);

        var session = await GetSessionAsync();
        if (!await session.IsOwnerOrModerator(AuthRepositoryAsync, creative?.OwnerId))
            throw HttpError.Forbidden($"You don't own this Creative {session.UserAuthId} vs {creative.OwnerId}");

        var now = DateTime.UtcNow;
        await Db.UpdateOnlyAsync(() =>
            new Creative {
                OwnerId = 2, // transfer to system user
                ModifiedBy = session.UserAuthId,
                ModifiedDate = now,
                DeletedBy = session.UserAuthId,
                DeletedDate = now,
            }, where: x => x.Id == request.Id);

        await Db.SaveCreativeAsync(request.Id, StableDiffusionClient);
    }

    public async Task Delete(HardDeleteCreative request)
    {
        var creative = await Db.SingleByIdAsync<Creative>(request.Id);
        if (creative == null)
            throw HttpError.NotFound($"Creative {request.Id} does not exist");

        var artifacts = await Db.SelectAsync<Artifact>(x => x.CreativeId == request.Id);
        var artifactIds = artifacts.Select(x => x.Id).ToList();

        using var transaction = Db.OpenTransaction();

        await Db.DeleteAsync<AlbumArtifact>(x => artifactIds.Contains(x.ArtifactId));
        await Db.DeleteAsync<ArtifactReport>(x => artifactIds.Contains(x.ArtifactId));
        await Db.DeleteAsync<ArtifactLike>(x => artifactIds.Contains(x.ArtifactId));
        await Db.DeleteAsync<Artifact>(x => x.CreativeId == request.Id);
        await Db.DeleteAsync<CreativeArtist>(x => x.CreativeId == request.Id);
        await Db.DeleteAsync<CreativeModifier>(x => x.CreativeId == request.Id);
        await Db.DeleteAsync<Creative>(x => x.Id == request.Id);

        transaction.Commit();

        await StableDiffusionClient.DeleteFolderAsync(creative);
    }
}

public static class CreateServiceUtils
{
    public static async Task<bool> IsOwnerOrModerator(this Service service, int? userId) => 
        await (await service.GetSessionAsync()).IsOwnerOrModerator(service.AuthRepositoryAsync, userId);

    public static async Task<bool> IsOwnerOrModerator(this IAuthSession session, IAuthRepositoryAsync AuthRepositoryAsync, int? ownerId)
    {
        var roles = await session.GetRolesAsync(AuthRepositoryAsync);
        if (!roles.Contains(AppRoles.Admin) && !roles.Contains(AppRoles.Moderator))
        {
            return ownerId != null && ownerId.ToString() == session.UserAuthId;
        }
        return true;
    }

    public static async Task<Creative> SaveCreativeAsync(this IDbConnection db, int creativeId, IStableDiffusionClient client)
    {
        var creative = await db.LoadSingleByIdAsync<Creative>(creativeId);
        await client.SaveMetadataAsync(creative);
        return creative;
    }
}

public interface IStableDiffusionClient
{
    Task<ImageGenerationResponse> GenerateImageAsync(ImageGeneration request);
    Task SaveMetadataAsync(Creative entry);
    Task DeleteFolderAsync(Creative entry);
}

public class ImageGeneration
{
    public string Engine { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Images { get; set; }
    public long? Seed { get; set; }
    public string Prompt { get; set; }
    public int Steps { get; set; }
}

public struct ImageSize
{
    public ImageSize(int width, int height)
    {
        Width = width;
        Height = height;
    }
    
    public int Width { get; set; }
    public int Height { get; set; }
}

public class ImageGenerationResponse
{
    public List<ImageGenerationResult> Results { get; set; }
    public string Key { get; set; }
    public string Error { get; set; }
}

public class ImageGenerationResult
{
    public string FilePath { get; set; }
    public string AnswerId { get; set; }
    public uint Seed { get; set; }
    public string Prompt { get; set; }
    public string FileName { get; set; }
    public long ContentLength { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}