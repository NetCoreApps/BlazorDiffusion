using BlazorDiffusion.ServiceInterface;
using Microsoft.AspNetCore.Components;
using ServiceStack.IO;
using BlazorDiffusion.ServiceModel;
using Amazon.S3;
using ServiceStack.Logging;
using System.Diagnostics;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Script;
using static ServiceStack.Diagnostics.Events;

[assembly: HostingStartup(typeof(BlazorDiffusion.ConfigureUi))]

namespace BlazorDiffusion;

public class ConfigureUi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureAppHost(afterConfigure:appHost => {

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

            container.AddSingleton<IComponentRenderer>(c => new ComponentRenderer());
            var prerenderer = new Prerenderer
            {
                BaseUrl = appConfig.BaseUrl,
                VirtualFiles = virtualFiles,
                Renderer = container.Resolve<IComponentRenderer>(),
                Pages = {
                    new(typeof(Pages.Index),  "/prerender/index.html",  new() { [nameof(Pages.Index.LazyLoad)] = "false" }),
                    new(typeof(Pages.Albums), "/prerender/albums.html", new() { [nameof(Pages.Albums.LazyLoad)] = "false" }),
                }
            };

            using var db = container.Resolve<IDbConnectionFactory>().OpenDbConnection();
            var albums = db.LoadSelect<Album>(x => x.DeletedDate == null)
                .Select(x => x.ToAlbumResult())
                .ToList();

            var indexFile = appHost.GetVirtualFileSource<FileSystemVirtualFiles>().GetFile("_index.html");
            if (indexFile == null)
                throw new FileNotFoundException("Could not resolve _index.html");
            var template = indexFile.ReadAllText();

            foreach (var album in albums)
            {
                if (album.Slug == null)
                {
                    album.Slug = DefaultScripts.Instance.generateSlug(album.Name);
                    db.UpdateOnly(() => new Album { Slug = album.Slug }, where:x => x.Id == album.Id);
                }

                var path = $"/albums/{album.Slug}.html";
                var artifactId = album.ArtifactIds?.FirstOrDefault();
                var artifact = artifactId != null ? db.SingleById<Artifact>(artifactId) : null;
                var albumMeta = $@"
                    <meta name=""twitter:card"" content=""summary"" />
                    <meta name=""twitter:site"" content=""blazordiffusion.com"" />
                    <meta name=""twitter:creator"" content=""@blazordiffusion"" />
                    <meta property=""og:url"" content=""https://blazordiffusion.com{path}"" />
                    <meta property=""og:title"" content=""{album.Name}"" />
                    <meta property=""og:description"" content="""" />
                    <meta property=""og:image"" content=""{appConfig.AssetsBasePath.CombineWith(artifact?.FilePath)}"" />
                ";

                prerenderer.Pages.Add(new(typeof(Pages.albums.Index), path, new() { 
                        [nameof(Pages.albums.Index.RefId)] = album.AlbumRef,
                    }, 
                    transformer:html => template
                        .Replace("<!--title-->", album.Name)
                        .Replace("<!--head-->", albumMeta)
                        .Replace("<!--body-->", html)));
            }

            container.Register<IPrerenderer>(c => prerenderer);
        });
}

public class PrerenderPage
{
    public Type Component { get; set; }
    public Dictionary<string, object> ComponentArgs { get; set; }
    public string WritePath { get; set; }
    public Func<string, string>? Transformer { get; set; }

    public PrerenderPage(Type component, string writePath, Dictionary<string, object>? componentArgs = null, Func<string, string>? transformer = null)
    {
        Component = component;
        ComponentArgs = componentArgs ?? new();
        WritePath = writePath;
        Transformer = transformer;
    }
}

public class Prerenderer : IPrerenderer
{
    public string BaseUrl { get; set; }
    public IVirtualFiles VirtualFiles { get; init; }
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
                var path = page.WritePath;
                log.DebugFormat("Rendering {0} to {1} {2}...", page.Component.FullName, VirtualFiles.GetType().Name, path);
                var html = await Renderer.RenderComponentAsync(page.Component, httpContext, page.ComponentArgs);
                log.DebugFormat("Rendered {0} in {1} bytes, took {2}ms", page.Component.FullName, html?.Length ?? -1, sw.ElapsedMilliseconds);

                if (!string.IsNullOrEmpty(html))
                {
                    if (page.Transformer != null)
                        html = page.Transformer(html);

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
