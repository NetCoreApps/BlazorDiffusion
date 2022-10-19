using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.ServiceInterface;

public class CreativeService : Service
{
    public IStableDiffusionClient StableDiffusionClient { get; set; }
    public IDbConnectionFactory DbConnectionFactory { get; set; }
    public const string DefaultEngine = "stable-diffusion-v1-5";
    public const int DefaultHeight = 512;
    public const int DefaultWidth = 512;
    public const int DefaultImages = 4;
    public const int DefaultSteps = 25;
    
    public async Task<object> Post(CreateCreative request)
    {
        var modifiers = await Db.SelectAsync<Modifier>(x => Sql.In(x.Id, request.ModifierIds));
        var artists = request.ArtistIds.Count == 0 ? new List<Artist>() :
            await Db.SelectAsync<Artist>(x => Sql.In(x.Id, request.ArtistIds));
        
        var imageGenerationResponse = await GenerateImage(request,modifiers,artists);
        
        var creative = await PersistCreative(request, imageGenerationResponse,modifiers,artists);
        
        await StableDiffusionClient.SaveMetadata(imageGenerationResponse, creative);

        return creative;
    }
    
    public async Task<object> Post(UpdateCreative request)
    {
        var creative = await Db.LoadSingleByIdAsync<Creative>(request.Id);
        if (creative == null)
            throw HttpError.NotFound($"Creative {request.Id} not found");

        var artifact = creative.Artifacts.SingleOrDefault(x => x.Id == request.PrimaryArtifactId);
        if(artifact == null)
            throw HttpError.BadRequest($"No such Artifact ID {request.PrimaryArtifactId}");

        creative.PrimaryArtifactId = request.PrimaryArtifactId;
        await Db.SaveAsync(creative);
        artifact.IsPrimaryArtifact = true;
        await Db.SaveAsync(artifact);
        
        return creative;
    }

    public async Task<object> Post(UpdateCreativeArtifact request)
    {
        var artifact = await Db.LoadSingleByIdAsync<CreativeArtifact>(request.Id);
        
        if(artifact == null)
            throw HttpError.NotFound("Artifact not found.");

        var creative = await Db.LoadSingleByIdAsync<Creative>(artifact.CreativeId);
        
        var session = await GetSessionAsync();
        if (!session.HasRole(RoleNames.Admin, AuthRepository))
        {
            if(creative?.AppUserId.ToString() != session.UserAuthId)
                throw HttpError.BadRequest("You don't own this Artifact");
        }

        artifact.Nsfw = request.Nsfw;
        artifact.WithAudit(Request);
        await Db.SaveAsync(artifact);
        return artifact;
    }
    
    private async Task<Creative> PersistCreative(CreateCreative request, 
        ImageGenerationResponse imageGenerationResponse,
        List<Modifier> modifiers, 
        List<Artist> artists)
    {
        string userAuthId = (await GetSessionAsync()).UserAuthId;
        var userId = userAuthId?.ToInt();
        var now = DateTime.UtcNow;
        var creative = new Creative().PopulateWith(request)
            .WithAudit(userAuthId, now);
        creative.Width = request.Width ?? DefaultWidth;
        creative.Height = request.Height ?? DefaultHeight;
        creative.Steps = request.Steps ?? DefaultSteps;
        creative.AppUserId = userId;
        creative.Key = imageGenerationResponse.Key;
        creative.ArtistNames = artists.Select(x => $"{x.FirstName} {x.LastName}").ToList();
        creative.ModifiersText = modifiers.Select(x => x.Name).ToList();
        creative.Prompt = ConstructPrompt(request.UserPrompt, modifiers, artists);
        creative.RefId = Guid.NewGuid().ToString();

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

        var artifacts = imageGenerationResponse.Results.Select(x => new CreativeArtifact {
            CreativeId = creative.Id,
            Width = x.Width,
            Height = x.Height,
            Prompt = x.Prompt,
            Seed = x.Seed,
            FileName = x.FileName,
            FilePath = x.FilePath,
            ContentType = MimeTypes.ImagePng,
            ContentLength = x.ContentLength
        }.WithAudit(userAuthId, now));
        await db.InsertAllAsync(artifacts);
        transaction.Commit();
        
        var result = await Db.LoadSingleByIdAsync<Creative>(creative.Id);
        return result;
    }
    
    private async Task<ImageGenerationResponse> GenerateImage(CreateCreative request,
        List<Modifier> modifiers, List<Artist> artists)
    {
        var apiPrompt = ConstructPrompt(request.UserPrompt, modifiers, artists);
        var imageGenOptions = new ImageGeneration
        {
            Prompt = apiPrompt,
            Engine = DefaultEngine,
            Height = request.Height ?? DefaultHeight,
            Width = request.Width ?? DefaultWidth,
            Images = request.Images ?? DefaultImages,
            Steps = request.Steps ?? DefaultSteps,
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
        var artistsSuffix = artists.Select(x => $"inspired by {x.FirstName} {x.LastName}").Join(",").TrimEnd(',');
        if(artists.Count > 0)
            finalPrompt += $", {artistsSuffix}";
        return finalPrompt;
    }
}

public interface IStableDiffusionClient
{
    Task<ImageGenerationResponse> GenerateImageAsync(ImageGeneration request);
    Task SaveMetadata(ImageGenerationResponse response, Creative entry);
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
    public string? Error { get; set; }
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