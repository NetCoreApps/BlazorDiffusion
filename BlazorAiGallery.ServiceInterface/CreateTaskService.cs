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
    
    public async Task<object> Post(CreateCreativeTask request)
    {
        
        var creativeTask = (CreativeTask)(await AutoQuery.CreateAsync(request, Request));
        
        var imageGenerationResponse = await StableDiffusionClient.GenerateImageAsync(new ImageGeneration
        {
            Prompt = request.Prompt,
            CreativeId = creativeTask.CreativeId,
            CreativeTaskId = creativeTask.Id
        });

        foreach (var imageResult in imageGenerationResponse.Results)
        {
            await Db.InsertAsync(new AiGeneratedFile
            {
                Width = creativeTask.Width,
                Height = creativeTask.Height,
                Prompt = imageResult.Prompt,
                Seed = imageResult.Seed,
                FileName = imageResult.FileName,
                FilePath = imageResult.FilePath,
                ContentType = MimeTypes.ImagePng,
                CreativeTaskId = creativeTask.Id,
                ContentLength = imageResult.ContentLength
            });
        }

        var updatedTask = await Db.LoadSingleByIdAsync<CreativeTask>(creativeTask.Id);

        return updatedTask;
    }
}

public interface IStableDiffusionClient
{
    Task<ImageGenerationResponse> GenerateImageAsync(ImageGeneration request);
}

public class ImageGeneration
{
    public string Engine { get; set; } = "stable-diffusion-v1-5";
    public int Width { get; set; } = 512;
    public int Height { get; set; } = 512;
    public int Images { get; set; } = 4;
    public long? Seed { get; set; } = Random.Shared.Next();
    public string Prompt { get; set; }
    
    public int CreativeId { get; set; }
    public int CreativeTaskId { get; set; }
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