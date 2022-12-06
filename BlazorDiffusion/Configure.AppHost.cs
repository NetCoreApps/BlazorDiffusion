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

    public const string LocalBaseUrl = "https://localhost:5001";

    public override void Configure(Container container)
    {
        SetConfig(new HostConfig {
            AddRedirectParamsToQueryString = true,
            UseSameSiteCookies = true,
        });

        //Plugins.Add(new ProfilingFeature());
        var cdnUrl = Environment.GetEnvironmentVariable("DEPLOY_CDN");
        var baseUrl = !string.IsNullOrEmpty(cdnUrl)
            ? cdnUrl
            : LocalBaseUrl;

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
            SyncTasksInterval = TimeSpan.FromMinutes(10),
            // DisableWrites = HostingEnvironment.IsDevelopment(),
        });
        
        container.Register(appConfig);

        var hasR2 = !string.IsNullOrEmpty(r2AccessId);
        if(!hasR2)
            Log.Warn($"Starting without R2 access.");

        var s3Client = new AmazonS3Client(appConfig.R2AccessId, appConfig.R2AccessKey, new AmazonS3Config {
            ServiceURL = $"https://{appConfig.R2Account}.r2.cloudflarestorage.com"
        });
        container.Register(s3Client);
        var localFs = new FileSystemVirtualFiles(ContentRootDirectory.RealPath.CombineWith("App_Files").AssertDir());
        var appFs = VirtualFiles = hasR2 ? new R2VirtualFiles(s3Client, appConfig.ArtifactBucket) : localFs;
        Plugins.Add(new FilesUploadFeature(
            new UploadLocation("artifacts", appFs,
                readAccessRole: RoleNames.AllowAnon,
                maxFileBytes: AppData.MaxArtifactSize),
            new UploadLocation("avatars", appFs, allowExtensions: FileExt.WebImages, 
                // Use unique URL to invalidate CDN caches
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

        ScriptContext.Args[nameof(AppData)] = AppData.Instance;
    }

    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            services.ConfigureNonBreakingSameSiteCookies(context.HostingEnvironment);
            services.AddSingleton<AppUserQuotas>();
        });

    public static void RegisterKey() =>
        Licensing.RegisterLicense("OSS BSD-2-Clause 2022 https://github.com/NetCoreApps/BlazorDiffusion Ml25hVebV/jhTNlJa3WXFowrEn0QhqLjgNqmMhq7v+CylawWO+OEqlekfm2d4s93HbPCZz95Q+w763hDE7WjEPVfX7VzooDTb++JNUKzNfdH84kWe2Yv+p36xh8xAkJPFo8f7mvvP3p9dF62GuRWzBoo3Zh3P/52WpGZuChfJ0w=");
}
