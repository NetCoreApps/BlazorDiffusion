﻿using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;
using Gooseai;
using Grpc.Core;
using Grpc.Net.Client;
using ServiceStack.IO;
using ServiceStack.Text;
using SixLabors.ImageSharp;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;

namespace BlazorDiffusion;

public class DreamStudioClient : IStableDiffusionClient
{
    GrpcChannel channel;
    GenerationService.GenerationServiceClient client;
    public const string DefaultEngineId = "stable-diffusion-v1-5";

    public string ApiKey { get; set; }
    public string OutputPathPrefix { get; set; }
    public string EngineId { get; set; }
    public string? PublicPrefix { get; set; }
    public IVirtualFiles VirtualFiles { get; set; }
    
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

        var generateRequest = new Request
        {
            EngineId = string.IsNullOrEmpty(EngineId) ? DefaultEngineId : EngineId,
            RequestId = Guid.NewGuid().ToString("D"),
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
        };
        var response = client.Generate(generateRequest);

        var now = DateTime.UtcNow;
        var key = $"{now:yyyy/MM/dd}/{(long)now.TimeOfDay.TotalMilliseconds}";

        var hashAlgorithm = new PerceptualHash();

        var results = new List<ImageGenerationResult>();
        await foreach (var item in response.ResponseStream.ReadAllAsync())
        {
            var hasArtifact = item.Artifacts.Count > 0;
            if (hasArtifact)
            {
                var artifact = item.Artifacts.First();
                var output = Path.Join(OutputPathPrefix, key, $"output_{artifact.Seed}.png");
                var bytes = artifact.Binary.ToByteArray();
                await VirtualFiles.WriteFileAsync(output, bytes);

                Int64? perceptualHash = null;
                Int64? averageHash = null;
                Int64? differenceHash = null;

                try
                {
                    using var ms = MemoryStreamFactory.GetStream(bytes);
                    perceptualHash = (Int64)hashAlgorithm.Hash(ms);
                }
                catch (Exception) { /*Not Required*/ }

                results.Add(new()
                {
                    Prompt = request.Prompt,
                    Seed = artifact.Seed,
                    AnswerId = item.AnswerId,
                    FilePath = $"/artifacts/{key}/output_{artifact.Seed}.png",
                    FileName = $"output_{artifact.Seed}.png",
                    ContentLength = bytes.Length,
                    Width = request.Width,
                    Height = request.Height,
                    PerceptualHash = perceptualHash,
                    AverageHash = averageHash,
                    DifferenceHash = differenceHash,
                });
            }
        }
        return new ImageGenerationResponse
        {
            RequestId = generateRequest.RequestId,
            EngineId = generateRequest.EngineId,
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
        await VirtualFiles.WriteFileAsync(Path.Join(outputDir,"metadata.json"), creative.ToJson().IndentJson());
    }

    public Task DeleteFolderAsync(Creative creative)
    {
        var vfsPathSuffix = creative.Key;
        var directory = VirtualFiles.GetDirectory(Path.Join(OutputPathPrefix,vfsPathSuffix));
        var allFiles = directory.GetAllFiles();
        VirtualFiles.DeleteFiles(allFiles);
        return Task.CompletedTask;
    }
}