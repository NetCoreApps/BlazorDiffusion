using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.ServiceInterface;

public class CreativeService : Service
{
    public IStableDiffusionClient StableDiffusionClient { get; set; }
    public IAutoQueryDb AutoQuery { get; set; }
    public const string DefaultEngine = "stable-diffusion-v1-5";
    public const int DefaultHeight = 512;
    public const int DefaultWidth = 512;
    public const int DefaultImages = 4;
    public const int DefaultSteps = 50;
    
    public async Task<object> Post(CreateCreative request)
    {
        var imageGenerationResponse = await GenerateImage(request);
        
        var creative = await PersistCreative(request,imageGenerationResponse).ConfigureAwait(false);
        
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

    private async Task<Creative> PersistCreative(CreateCreative request,
        ImageGenerationResponse imageGenerationResponse)
    {
        var creative = (Creative)(await AutoQuery.CreateAsync(request, Request));
        creative.Width = request.Width ?? DefaultWidth;
        creative.Height = request.Height ?? DefaultHeight;
        creative.AppUserId = (await GetSessionAsync()).UserAuthId?.ToInt();
        
        var artists = await Db.SelectAsync<Artist>(x => Sql.In(x.Id, request.ArtistIds));
        var modifiers = await Db.SelectAsync<Modifier>(x => Sql.In(x.Id, request.ModifierIds));
        creative.ArtistNames = artists.Select(x => $"{x.FirstName} {x.LastName}").ToList();
        creative.ModifiersText = modifiers.Select(x => x.Name).ToList();
        creative.Prompt = ConstructPrompt(request.UserPrompt, modifiers, artists);
        
        await Db.UpdateAsync(creative);
        
        var creativeArtists = request.ArtistIds.Select(x => new CreativeArtist
        {
            ArtistId = x,
            CreativeId = creative.Id
        });
        var creativeModifiers = request.ModifierIds.Select(x => new CreativeModifier
        {
            CreativeId = creative.Id,
            ModifierId = x
        });
        
        await Db.InsertAllAsync(creativeArtists);
        await Db.InsertAllAsync(creativeModifiers);
        
        foreach (var imageResult in imageGenerationResponse.Results)
        {
            await Db.InsertAsync(new CreativeArtifact
            {
                CreativeId = creative.Id,
                Width = imageResult.Width,
                Height = imageResult.Height,
                Prompt = imageResult.Prompt,
                Seed = imageResult.Seed,
                FileName = imageResult.FileName,
                FilePath = imageResult.FilePath,
                ContentType = MimeTypes.ImagePng,
                ContentLength = imageResult.ContentLength
            }.WithAudit(req: Request, DateTime.Now));
        }
        var result = await Db.LoadSingleByIdAsync<Creative>(creative.Id);
        return result;
    }
    
    private async Task<ImageGenerationResponse> GenerateImage(CreateCreative request)
    {
        var modifiers = await Db.SelectAsync<Modifier>(x => Sql.In(x.Id, request.ModifierIds));
        
        var artists = request.ArtistIds.Count == 0 ? new List<Artist>() :
            await Db.SelectAsync<Artist>(x => Sql.In(x.Id, request.ArtistIds));;
        
        var apiPrompt = ConstructPrompt(request.UserPrompt, 
            modifiers, artists);
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
    public string Id { get; set; }
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