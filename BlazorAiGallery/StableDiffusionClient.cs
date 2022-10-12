using BlazorAiGallery.ServiceInterface;
using Gooseai;
using Grpc.Core;
using Grpc.Net.Client;

namespace BlazorAiGallery;

public class DreamAiImageGenerationClient : IStableDiffusionClient
{
    GrpcChannel channel;
    GenerationService.GenerationServiceClient client;
    
    public string ApiKey { get; set; }
    public string OutputPathPrefix { get; set; }
    public string EngineId { get; set; }
    
    public DreamAiImageGenerationClient()
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
                Steps = 50,
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
        var results = new List<ImageGenerationResult>();
        await foreach (var item in response.ResponseStream.ReadAllAsync())
        {
            var hasArtifact = item.Artifacts.Count > 0;
            if (hasArtifact)
            {
                var artifact = item.Artifacts.First();
                var vfsPathSuffix = $"{request.CreativeId}/task/{request.CreativeTaskId}";
                var outputDir = new DirectoryInfo(Path.Join(OutputPathPrefix, vfsPathSuffix));
                if (!outputDir.Exists)
                {
                    outputDir.Create();
                }
                var output = Path.Join(outputDir.FullName, $"output_{artifact.Seed}.png");
                var bytes = artifact.Binary.ToByteArray();
                await File.WriteAllBytesAsync(output,bytes);
                results.Add(new()
                {
                    Prompt = request.Prompt,
                    Seed = artifact.Seed,
                    AnswerId = item.AnswerId,
                    FilePath = $"/uploads/fs/{vfsPathSuffix}/output_{artifact.Seed}.png",
                    FileName = $"output_{artifact.Seed}.png",
                    ContentLength = bytes.Length
                });
            }
        }
        return new ImageGenerationResponse
        {
            Results = results
        };
    }
}