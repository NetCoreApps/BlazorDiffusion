using Amazon.Runtime;
using Amazon.S3;
using Funq;
using ServiceStack;
using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;
using Gooseai;
using Grpc.Core;
using Grpc.Net.Client;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Web;
using ServiceStack.Data;

[assembly: HostingStartup(typeof(BlazorDiffusion.AppHost))]

namespace BlazorDiffusion;

public class AppHost : AppHostBase, IHostingStartup
{
    public AppHost() : base("Blazor Diffusion", typeof(MyServices).Assembly) { }

    public override void Configure(Container container)
    {
        SetConfig(new HostConfig {
            AddRedirectParamsToQueryString = true,
            UseSameSiteCookies = true,
        });
        
        //Plugins.Add(new ProfilingFeature());

        var cdnUrl = Environment.GetEnvironmentVariable("DEPLOY_CDN");

        var baseUrl = string.IsNullOrEmpty(cdnUrl)
            ? "https://localhost:5001"
            : cdnUrl;

        Plugins.Add(new CorsFeature(allowedHeaders: "Content-Type,Authorization",
            allowOriginWhitelist: new[]{
            "http://localhost:5000",
            baseUrl,
            "https://blazordiffusion.com",
            "https://pub-e17dff5b2d09437a97efdbb7f6ee3701.r2.dev", // CDN diffusion-client public bucket
        }, allowCredentials: true));

        var r2AccessId = Environment.GetEnvironmentVariable("R2_ACCESS_KEY_ID")!;
        var r2AccessKey = Environment.GetEnvironmentVariable("R2_SECRET_ACCESS_KEY")!;
        
        var appConfig = AppConfig.Set(new AppConfig {
            BaseUrl = baseUrl,
            R2Account = "b95f38ca3a6ac31ea582cd624e6eb385",
            R2AccessId = r2AccessId,
            R2AccessKey = r2AccessKey,
            ArtifactBucket = "diffusion",
            CdnBucket = "diffusion-client",
            AssetsBasePath = "https://cdn.diffusion.works",
            FallbackAssetsBasePath = "https://pub-97bba6b94a944260b10a6e7d4bf98053.r2.dev",
        });
        Log.Warn($"R2: Account:{appConfig.R2Account}, Id:{appConfig.R2AccessId}, Key:{appConfig.R2AccessKey}, Bucket:{appConfig.ArtifactBucket}");
        container.Register(appConfig);

        var s3Client = new AmazonS3Client(appConfig.R2AccessId, appConfig.R2AccessKey, new AmazonS3Config {
            ServiceURL = $"https://{appConfig.R2Account}.r2.cloudflarestorage.com"
        });
        container.Register(s3Client);
        var appFs = VirtualFiles = new R2VirtualFilesProvider(s3Client, appConfig.ArtifactBucket);
        Plugins.Add(new FilesUploadFeature(
            new UploadLocation("artifacts", appFs,
                readAccessRole: RoleNames.AllowAnon,
                maxFileBytes: AppData.MaxArtiactSize),
            new UploadLocation("avatars", appFs, allowExtensions: FileExt.WebImages, 
                // need to use file name to create unique URL that invalidates CDN cache
                resolvePath: ctx => X.Map((CustomUserSession)ctx.Session, x => $"/avatars/{x.RefIdStr[..2]}/{x.RefIdStr}/{ctx.FileName}")!,
                maxFileBytes: AppData.MaxAvatarSize,
                transformFile:ImageUtils.TransformAvatarAsync)
            ));

        // Don't use public prefix if working locally
        Register<IStableDiffusionClient>(new DreamStudioClient
        {
            ApiKey = Environment.GetEnvironmentVariable("DREAMAI_APIKEY") ?? "<your_api_key>",
            OutputPathPrefix = "artifacts",
            PublicPrefix = appConfig.AssetsBasePath,
            VirtualFiles = appFs
        });
    }

    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            services.ConfigureNonBreakingSameSiteCookies(context.HostingEnvironment);
            services.AddSingleton<AppUserQuotas>();
        });

    public static void RegisterKey() =>
        Licensing.RegisterLicense("OSS BSD-2-Clause 2022 https://github.com/NetCoreApps/BlazorDiffusion Ml25hVebV/jhTNlJa3WXFowrEn0QhqLjgNqmMhq7v+CylawWO+OEqlekfm2d4s93HbPCZz95Q+w763hDE7WjEPVfX7VzooDTb++JNUKzNfdH84kWe2Yv+p36xh8xAkJPFo8f7mvvP3p9dF62GuRWzBoo3Zh3P/52WpGZuChfJ0w=");
}
