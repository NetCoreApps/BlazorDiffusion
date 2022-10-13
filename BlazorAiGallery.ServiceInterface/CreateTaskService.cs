using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorAiGallery.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace BlazorAiGallery.ServiceInterface;

public class CreateTaskService : Service
{
    public IStableDiffusionClient StableDiffusionClient { get; set; }
    public IAutoQueryDb AutoQuery { get; set; }
    public string DefaultEngine { get; set; } = "stable-diffusion-v1-5";
    public int DefaultHeight { get; set; } = 512;
    public int DefaultWidth { get; set; } = 512;
    public int DefaultImages { get; set; } = 4;
    
    public async Task<object> Post(CreateCreative request)
    {
        var creative = (Creative)(await AutoQuery.CreateAsync(request, Request));
        var imageGenOptions = new ImageGeneration
        {
            Prompt = request.Prompt,
            CreativeId = creative.Id,
            Engine = DefaultEngine,
            Height = request.Height ?? DefaultHeight,
            Width = request.Width ?? DefaultWidth,
            Images = request.Images ?? DefaultImages,
            Seed = request.Seed
        };
        
        var imageGenerationResponse = await StableDiffusionClient.GenerateImageAsync(imageGenOptions);

        foreach (var imageResult in imageGenerationResponse.Results)
        {
            await Db.InsertAsync(new CreativeArtifact
            {
                CreativeId = creative.Id,
                Width = creative.Width,
                Height = creative.Height,
                Prompt = imageResult.Prompt,
                Seed = imageResult.Seed,
                FileName = imageResult.FileName,
                FilePath = imageResult.FilePath,
                ContentType = MimeTypes.ImagePng,
                ContentLength = imageResult.ContentLength
            });
        }

        var updatedTask = await Db.LoadSingleByIdAsync<Creative>(creative.Id);

        return updatedTask;
    }
}

public interface IStableDiffusionClient
{
    Task<ImageGenerationResponse> GenerateImageAsync(ImageGeneration request);
}

public class ImageGeneration
{
    public int CreativeId { get; set; }
    public string Engine { get; set; } = "stable-diffusion-v1-5";
    public int Width { get; set; } = 512;
    public int Height { get; set; } = 512;
    public int Images { get; set; } = 4;
    public long? Seed { get; set; } = Random.Shared.Next();
    public string Prompt { get; set; }
}

public class ImageGenerationResponse
{
    public List<ImageGenerationResult> Results { get; set; }
}

public class ImageGenerationResult
{
    public string FilePath { get; set; }
    public string AnswerId { get; set; }
    public uint Seed { get; set; }
    public string Prompt { get; set; }
    public string FileName { get; set; }
    public long ContentLength { get; set; }
}