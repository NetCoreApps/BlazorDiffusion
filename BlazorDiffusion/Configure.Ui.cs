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
using System.Diagnostics;

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

            var blazorWebRoot = appHost.AppSettings.GetString("BlazorWebRoot");
            var blazorWebRootPath = Path.GetFullPath(Path.Combine(appHost.GetWebRootPath(), blazorWebRoot));

            IVirtualFiles virtualFiles = appHost.Config.DebugMode
                ? new FileSystemVirtualFiles(blazorWebRootPath)
                : new R2VirtualFilesProvider(s3Client, appConfig.CdnBucket);

            container.Register<IPrerenderer>(c => new Prerenderer
            {
                BaseUrl = appConfig.BaseUrl,
                VirtualFiles = virtualFiles,
                PrerenderDir = "/prerender",
                Renderer = c.Resolve<IComponentRenderer>(),
                Pages = {
                    new(typeof(Pages.Index),  "/index.html", new() { [nameof(Pages.Index.LazyLoad)] = "false" }),
                    //new(typeof(Pages.Create), "/create.html"), // needs to be signed in
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
                    VirtualFiles.WriteFile(path, html);
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

public class ComponentRenderer : IComponentRenderer
{
    public List<Type> Types { get; }

    public ComponentRenderer(IEnumerable<Type> types)
    {
        Types = types.ToList();
    }

    public Task<string> RenderComponentAsync(string typeName, HttpContext httpContext, Dictionary<string, object>? args = null)
    {
        var type = typeName.IndexOf('.') < 0 
            ? Types.FirstOrDefault(x => x.Name == typeName)
            : Types.FirstOrDefault(x => x.FullName == typeName);
        if (type == null)
            throw HttpError.NotFound("Component Not Found");
        
        return RenderComponentAsync(type, httpContext, args);
    }

    public Task<string> RenderComponentAsync<T>(HttpContext httpContext, Dictionary<string, object>? args = null) =>
        RenderComponentAsync(typeof(T), httpContext, args);

    public async Task<string> RenderComponentAsync(Type componentType, HttpContext httpContext, Dictionary<string, object>? args = null)
    {
        var componentArgs = new Dictionary<string, object>();
        if (args != null)
        {
            var accessors = TypeProperties.Get(componentType);
            foreach (var entry in args)
            {
                var prop = accessors.GetPublicProperty(entry.Key);
                if (prop == null)
                    continue;

                var value = entry.Value.ConvertTo(prop.PropertyType);
                componentArgs[prop.Name] = value;
            }
        }

        var componentTagHelper = new ComponentTagHelper
        {
            ComponentType = componentType,
            RenderMode = RenderMode.Static,
            Parameters = componentArgs,
            ViewContext = new ViewContext { HttpContext = httpContext },
        };

        var objArgs = new Dictionary<object, object>();
        var tagHelperContext = new TagHelperContext(
            new TagHelperAttributeList(),
            objArgs,
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
