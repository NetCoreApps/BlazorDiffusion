using AiServer.ServiceModel.Types;
using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;
using ServiceStack.IO;
using JsonApiClient = ServiceStack.JsonApiClient;

namespace BlazorDiffusion;

public class AiServerClient: IStableDiffusionClient
{
    public JsonApiClient Client { get; }
    public IVirtualFiles VirtualFiles { get; }
    
    public string? OutputPathPrefix { get; set; }

    public AiServerClient(string aiServerBaseUrl,IVirtualFiles virtualFiles)
    {
        Client = new JsonApiClient(aiServerBaseUrl);
        VirtualFiles = virtualFiles;
    }
    public async Task<ImageGenerationResponse> GenerateImageAsync(ImageGeneration request)
    {
        var req = request.ToComfy();
        var res = await Client.PostAsync(req);
        var now = DateTime.UtcNow;
        var key = $"{now:yyyy/MM/dd}/{(long)now.TimeOfDay.TotalMilliseconds}";

        var promptId = Guid.NewGuid().ToString();
        var results = new List<ImageGenerationResult>();
        foreach (var item in res.Images)
        {
            var artifactUrl = $"{Client.BaseUri.TrimEnd('/')}/uploads{item.Url}";
            var seed = (uint)Random.Shared.Next();
            var output = Path.Join(OutputPathPrefix, key, $"output_{Guid.NewGuid()}.png");
            var bytes = await artifactUrl.GetBytesFromUrlAsync();
            await VirtualFiles.WriteFileAsync(output, bytes);
            var imageDetails = ImageDetails.Calculate(bytes);

            results.Add(new()
            {
                Prompt = request.Prompt,
                Seed = seed,
                AnswerId = promptId,
                FilePath = $"/artifacts/{key}/output_{seed}.png",
                FileName = $"output_{seed}.png",
                ContentLength = bytes.Length,
                Width = request.Width,
                Height = request.Height,
                ImageDetails = imageDetails,
            });
        }

        return new ImageGenerationResponse
        {
            RequestId = Guid.NewGuid().ToString(),
            EngineId = "comfy",
            Key = key,
            Results = results,
        };
    }

    public IVirtualFile? GetMetadataFile(Creative creative)
    {
        throw new NotImplementedException();
    }

    public Task SaveMetadataAsync(Creative entry)
    {
        throw new NotImplementedException();
    }

    public Task DeleteFolderAsync(Creative entry)
    {
        throw new NotImplementedException();
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
            Steps = request.Steps,
            BatchSize = request.Images,
            PositivePrompt = request.Prompt,
        };
    }
}