using AiServer.ServiceModel.Types;
using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;
using ServiceStack.IO;
using ServiceStack.Text;
using JsonApiClient = ServiceStack.JsonApiClient;

namespace BlazorDiffusion;

public class AiServerClient: IStableDiffusionClient
{
    public JsonApiClient Client { get; set; }
    public IVirtualFiles VirtualFiles { get; set; }
    
    public string? OutputPathPrefix { get; set; }
    
    public async Task<ImageGenerationResponse> GenerateImageAsync(ImageGeneration request)
    {
        var req = request.ToComfy();
        var res = await Client.PostAsync(req);
        var now = DateTime.UtcNow;
        var key = $"{now:yyyy/MM/dd}/{(long)now.TimeOfDay.TotalMilliseconds}";
        
        var results = new List<ImageGenerationResult>();
        var seed = (res.Request.Seed ?? 0).ConvertTo<uint>();
        foreach (var item in res.Images)
        {
            var artifactUrl = $"{Client.BaseUri.TrimEnd('/')}/uploads{item.Url}";
            var output = Path.Join(OutputPathPrefix, key, $"output_{seed}.png");
            var bytes = await artifactUrl.GetBytesFromUrlAsync();
            await VirtualFiles.WriteFileAsync(output, bytes);
            var imageDetails = ImageDetails.Calculate(bytes);

            results.Add(new()
            {
                Prompt = request.Prompt,
                Seed = seed,
                AnswerId = res.PromptId,
                FilePath = $"/artifacts/{key}/output_{seed}.png",
                FileName = $"output_{seed}.png",
                ContentLength = bytes.Length,
                Width = request.Width,
                Height = request.Height,
                ImageDetails = imageDetails,
            });
            // Assume incremental seeds for multiple images as comfyui does not provide the specific image seed back
            seed++;
        }

        return new ImageGenerationResponse
        {
            RequestId = res.PromptId,
            EngineId = "comfy",
            Key = key,
            Results = results,
        };
    }

    public string GetMetadataPath(Creative creative) => OutputPathPrefix.CombineWith(creative.Key, "metadata.json");
    public IVirtualFile GetMetadataFile(Creative creative) => VirtualFiles.GetFile(GetMetadataPath(creative));

    public async Task SaveMetadataAsync(Creative creative)
    {
        var vfsPathSuffix = creative.Key;
        var outputDir = Path.Join(OutputPathPrefix, vfsPathSuffix);
        await VirtualFiles.WriteFileAsync(Path.Join(outputDir, "metadata.json"), creative.ToJson().IndentJson());
    }

    public Task DeleteFolderAsync(Creative creative)
    {
        var vfsPathSuffix = creative.Key;
        var directory = VirtualFiles.GetDirectory(Path.Join(OutputPathPrefix, vfsPathSuffix));
        var allFiles = directory.GetAllFiles();
        VirtualFiles.DeleteFiles(allFiles);
        return Task.CompletedTask;
    }
}

public static class StableDiffusionClientExtensions
{
    public static ComfyTextToImage ToComfy(this ImageGeneration request)
    {
        return new ComfyTextToImage
        {
            Height = request.Height,
            Width = request.Width,
            Seed = request.Seed ?? Random.Shared.Next(),
            BatchSize = request.Images,
            PositivePrompt = request.Prompt,
        };
    }
}