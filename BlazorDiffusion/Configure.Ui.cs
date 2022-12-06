using BlazorDiffusion.ServiceInterface;
using Microsoft.AspNetCore.Components;
using ServiceStack.IO;
using BlazorDiffusion.ServiceModel;
using Amazon.S3;
using ServiceStack.Logging;
using System.Diagnostics;

[assembly: HostingStartup(typeof(BlazorDiffusion.ConfigureUi))]

namespace BlazorDiffusion;

public class ConfigureUi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddSingleton<IComponentRenderer>(c => new ComponentRenderer());
        }).ConfigureAppHost(afterConfigure:appHost => {

            //TODO replace with appHost.IsRunAsAppTask()
            var isAppTask = Environment.GetCommandLineArgs().Any(x => x.IndexOf(nameof(AppTasks)) >= 0);
            if (isAppTask)
                return;

            var container = appHost.GetContainer();
            var s3Client = container.Resolve<AmazonS3Client>();
            var appConfig = container.Resolve<AppConfig>();

            IVirtualFiles virtualFiles = appHost.GetHostingEnvironment().IsDevelopment()
                ? new FileSystemVirtualFiles(Path.GetFullPath(Path.Combine(appHost.GetWebRootPath(), appHost.AppSettings.GetString("BlazorWebRoot"))))
                : new R2VirtualFiles(s3Client, appConfig.CdnBucket);

            container.Register<IPrerenderer>(c => new Prerenderer
            {
                BaseUrl = appConfig.BaseUrl,
                VirtualFiles = virtualFiles,
                PrerenderDir = "/prerender",
                Renderer = c.Resolve<IComponentRenderer>(),
                Pages = {
                    new(typeof(Pages.Index),  "/index.html",  new() { [nameof(Pages.Index.LazyLoad)] = "false" }),
                    new(typeof(Pages.Albums), "/albums.html", new() { [nameof(Pages.Index.LazyLoad)] = "false" }),
                }
            });
        });
}

public class PrerenderPage
{
    public Type Component { get; set; }
    public Dictionary<string, object> ComponentArgs { get; set; }
    public string WritePath { get; set; }

    public PrerenderPage(Type component, string writePath, Dictionary<string, object>? componentArgs = null)
    {
        Component = component;
        ComponentArgs = componentArgs ?? new();
        WritePath = writePath;
    }
}

public class Prerenderer : IPrerenderer
{
    public string BaseUrl { get; set; }
    public IVirtualFiles VirtualFiles { get; init; }
    public string PrerenderDir { get; set; }
    public IComponentRenderer Renderer { get; init; }
    public List<PrerenderPage> Pages { get; } = new();

    public async Task RenderPages(HttpContext? httpContext = null)
    {
        var log = LogManager.GetLogger(GetType());
        httpContext ??= HttpContextFactory.CreateHttpContext(BaseUrl);

        var sw = Stopwatch.StartNew();
        foreach (var page in Pages)
        {
            try
            {
                sw.Restart();
                var path = PrerenderDir.CombineWith(page.WritePath);
                log.DebugFormat("Rendering {0} to {1} {2}...", page.Component.FullName, VirtualFiles.GetType().Name, path);
                var html = await Renderer.RenderComponentAsync(page.Component, httpContext, page.ComponentArgs);
                log.DebugFormat("Rendered {0} in {1} bytes, took {2}ms", page.Component.FullName, html?.Length ?? -1, sw.ElapsedMilliseconds);

                if (!string.IsNullOrEmpty(html))
                {
                    await VirtualFiles.WriteFileAsync(path, html);
                }
                else
                {
                    VirtualFiles.DeleteFile(path);
                }
            }
            catch (Exception e)
            {
                LogManager.GetLogger(GetType()).Error(e, "Error trying to render {0}: {1}", page.Component.FullName, e.Message);
            }
        }
    }
}
