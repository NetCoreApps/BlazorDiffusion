using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;
using Gooseai;
using Grpc.Core;
using Grpc.Net.Client;
using ServiceStack.IO;
using static Grpc.Core.Metadata;

namespace BlazorDiffusion;

public class DreamStudioClient : IStableDiffusionClient
{
    GrpcChannel channel;
    GenerationService.GenerationServiceClient client;

    
    public string ApiKey { get; set; }
    public string OutputPathPrefix { get; set; }
    public string EngineId { get; set; }
    
    public DreamStudioClient()
    {
        var credentials = CallCredentials.FromInterceptor((context, metadata) =>
        {
            if (!string.IsNullOrEmpty(ApiKey))
            {
                metadata.Add("Authorization", $"Bearer {ApiKey}");
            }
            return Task.CompletedTask;
        });
        channel = GrpcChannel.ForAddress("https://grpc.stability.ai", new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Create(ChannelCredentials.SecureSsl, credentials)
        });
        client = new GenerationService.GenerationServiceClient(channel);
    }
    public async Task<ImageGenerationResponse> GenerateImageAsync(ImageGeneration request)
    {

        var response = client.Generate(new Request
        {
            EngineId = string.IsNullOrEmpty(EngineId) ? "stable-diffusion-v1-5" : EngineId,
            RequestId = Guid.NewGuid().ToString(),
            Image = new ImageParameters
            {
                Height = Convert.ToUInt32(request.Height),
                Width = Convert.ToUInt32(request.Width),
                Seed = { Convert.ToUInt32(request.Seed) },
                Steps = Convert.ToUInt32(request.Steps),
                Samples = Convert.ToUInt32(request.Images),
                Transform = new TransformType
                {
                    Diffusion = DiffusionSampler.SamplerKLms
                }
            },
            Prompt =
            {
                new Prompt()
                {
                    Text = request.Prompt,
                    Parameters = new PromptParameters
                    {
                        Init = false,
                        Weight = 0.0f
                    }
                },
            },
        });

        var now = DateTime.UtcNow;
        var key = $"{now:yyyy/MM/dd}/{(long)now.TimeOfDay.TotalMilliseconds}";
        var outputDir = new DirectoryInfo(Path.Join(OutputPathPrefix, key).AssertDir());

        var results = new List<ImageGenerationResult>();
        await foreach (var item in response.ResponseStream.ReadAllAsync())
        {
            var hasArtifact = item.Artifacts.Count > 0;
            if (hasArtifact)
            {
                var artifact = item.Artifacts.First();
                var output = Path.Join(outputDir.FullName, $"output_{artifact.Seed}.png");
                var bytes = artifact.Binary.ToByteArray();
                await File.WriteAllBytesAsync(output,bytes);
                results.Add(new()
                {
                    Prompt = request.Prompt,
                    Seed = artifact.Seed,
                    AnswerId = item.AnswerId,
                    FilePath = $"/uploads/artifacts/{key}/output_{artifact.Seed}.png",
                    FileName = $"output_{artifact.Seed}.png",
                    ContentLength = bytes.Length,
                    Width = request.Width,
                    Height = request.Height
                });
            }
        }
        return new ImageGenerationResponse
        {
            Key = key,
            Results = results,
        };
    }

    public async Task SaveMetadataAsync(Creative creative)
    {
        var vfsPathSuffix = creative.Key;
        var outputDir = new DirectoryInfo(Path.Join(OutputPathPrefix, vfsPathSuffix));
        await File.WriteAllTextAsync(Path.Join(outputDir.FullName,"metadata.json"),creative.ToSafeJson());
    }

    public Task DeleteFolderAsync(Creative creative)
    {
        var vfsPathSuffix = creative.Key;
        var outputDir = new DirectoryInfo(Path.Join(OutputPathPrefix, vfsPathSuffix));
        FileSystemVirtualFiles.DeleteDirectoryRecursive(Path.Join(outputDir.FullName));
        return Task.CompletedTask;
    }
}