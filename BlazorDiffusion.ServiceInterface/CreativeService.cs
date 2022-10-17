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
    public string DefaultEngine { get; set; } = "stable-diffusion-v1-5";
    public int DefaultHeight { get; set; } = 512;
    public int DefaultWidth { get; set; } = 512;
    public int DefaultImages { get; set; } = 4;
    
    public async Task<object> Post(CreateCreative request)
    {
        var imageGenerationResponse = await GenerateImage(request);
        
        var creative = await PersistCreative(request,imageGenerationResponse).ConfigureAwait(false);
        
        await StableDiffusionClient.SaveMetadata(imageGenerationResponse, creative);

        return creative;
    }

    private async Task<Creative> PersistCreative(CreateCreative request,
        ImageGenerationResponse imageGenerationResponse)
    {
        var creative = (Creative)(await AutoQuery.CreateAsync(request, Request));
        var dimensions = GetDimensions(request.Orientation);
        creative.Width = dimensions.Width;
        creative.Height = dimensions.Height;
        
        var artists = await Db.SelectAsync<Artist>(x => Sql.In(x.Id, request.ArtistIds));
        var modifiers = await Db.SelectAsync<Modifier>(x => Sql.In(x.Id, request.ModifierIds));
        creative.ArtistNames = artists.Select(x => $"{x.FirstName} {x.LastName}").ToList();
        creative.ModifiersText = modifiers.Select(x => x.Name).ToList();
        
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

        var dimensions = GetDimensions(request.Orientation);
        
        var imageGenOptions = new ImageGeneration
        {
            Prompt = apiPrompt,
            Engine = DefaultEngine,
            Height = dimensions.Width,
            Width = dimensions.Height,
            Images = request.Images ?? DefaultImages,
            Seed = request.Seed
        };

        var imageGenerationResponse = await StableDiffusionClient.GenerateImageAsync(imageGenOptions);
        return imageGenerationResponse;
    }

    private ImageDimensions GetDimensions(CreativeOrientation orientation)
    {
        switch (orientation)
        {
            case CreativeOrientation.Landscape:
                return new ImageDimensions(896, 512);
            case CreativeOrientation.Portrait:
                return new ImageDimensions(512, 896);
            case CreativeOrientation.Square:
            default:
                return new ImageDimensions(512, 512);
        }
    }

    private string ConstructPrompt(string userPrompt, List<Modifier> modifiers, List<Artist> artists)
    {
        var finalPrompt = userPrompt;
        finalPrompt += $", {modifiers.Select(x => x.Name).Join(",")}";
        var artistsSuffix = artists.Select(x => $"inspired by {x.FirstName} {x.LastName}").Join(", and ");
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
}

public struct ImageDimensions
{
    public ImageDimensions(int width, int height)
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