using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;
using BlazorDiffusion.ServiceInterface;
using Microsoft.AspNetCore.Components;
using ServiceStack.IO;
using BlazorDiffusion.ServiceModel;
using Amazon.S3;
using ServiceStack.Logging;

[assembly: HostingStartup(typeof(BlazorDiffusion.ConfigureUi))]

namespace BlazorDiffusion;

public class ConfigureUi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddSingleton<IComponentRenderer>(c => new ComponentRenderer(
                typeof(Pages.Index).Assembly.GetTypes().Where(x => typeof(ComponentBase).IsAssignableFrom(x))));
        }).ConfigureAppHost(afterConfigure:appHost => {

            var container = appHost.GetContainer();
            var s3Client = container.Resolve<AmazonS3Client>();
            var appConfig = container.Resolve<AppConfig>();

            container.Register<IPrerenderer>(c => new Prerenderer
            {
                BaseUrl = appConfig.BaseUrl,
                VirtualFiles = appHost.Config.DebugMode
                    ? new FileSystemVirtualFiles(appHost.GetWebRootPath())
                    : new R2VirtualFilesProvider(s3Client, appConfig.CdnBucket),
                PrerenderDir = "/prerender",
                Renderer = c.Resolve<IComponentRenderer>(),
                Pages = {
                    [typeof(Pages.Index)] = "/index.html",
                    [typeof(Pages.Create)] = "/create.html",
                }
            });
        });
}

public class Prerenderer : IPrerenderer
{
    public string BaseUrl { get; set; }
    public IVirtualFiles VirtualFiles { get; init; }
    public string PrerenderDir { get; set; }
    public IComponentRenderer Renderer { get; init; }
    public Dictionary<Type, string> Pages { get; } = new();

    public async Task RenderPages(HttpContext? httpContext = null)
    {
        var log = LogManager.GetLogger(GetType());
        httpContext ??= HttpContextFactory.CreateHttpContext(BaseUrl);

        foreach (var entry in Pages)
        {
            var path = PrerenderDir.CombineWith(entry.Value);
            log.DebugFormat("Rendering {0} to {1} {2}...", entry.Key.FullName, VirtualFiles.RootDirectory.RealPath, path);
            var html = await Renderer.RenderComponentAsync(entry.Value, httpContext);
            await VirtualFiles.WriteFileAsync(path, html);
        }
    }
}

public class ComponentRenderer : IComponentRenderer
{
    public List<Type> Types { get; }

    public ComponentRenderer(IEnumerable<Type> types)
    {
        Types = types.ToList();
    }

    public Task<string> RenderComponentAsync(string typeName, HttpContext httpContext, Dictionary<object, object>? args = null)
    {
        var type = typeName.IndexOf('.') < 0 
            ? Types.FirstOrDefault(x => x.Name == typeName)
            : Types.FirstOrDefault(x => x.FullName == typeName);
        if (type == null)
            throw HttpError.NotFound("Component Not Found");
        
        return RenderComponentAsync(type, httpContext, args);
    }

    public Task<string> RenderComponentAsync<T>(HttpContext httpContext, Dictionary<object, object>? args = null) =>
        RenderComponentAsync(typeof(T), httpContext, args);

    public async Task<string> RenderComponentAsync(Type type, HttpContext httpContext, Dictionary<object, object>? args = null)
    {
        var componentTagHelper = new ComponentTagHelper
        {
            ComponentType = type,
            RenderMode = RenderMode.Static,
            Parameters = new Dictionary<string, object>(), //TODO: Overload and pass in parameters
            ViewContext = new ViewContext { HttpContext = httpContext },
        };

        var tagHelperContext = new TagHelperContext(
            new TagHelperAttributeList(),
            args ?? new Dictionary<object, object>(),
            "uniqueid");

        var tagHelperOutput = new TagHelperOutput(
            "tagName",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        await componentTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

        using var stringWriter = new StringWriter();

        tagHelperOutput.Content.WriteTo(stringWriter, HtmlEncoder.Default);

        return stringWriter.ToString();
    }
}
