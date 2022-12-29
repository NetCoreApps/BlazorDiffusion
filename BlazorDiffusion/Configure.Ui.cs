using System.Diagnostics;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Script;
using Amazon.S3;
using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.UI;
using BlazorDiffusion.Shared;
using System.Text;
using System.IO;
using System.Data;

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

            var indexFile = appHost.GetVirtualFileSource<FileSystemVirtualFiles>().GetFile("_index.html");
            if (indexFile == null)
                throw new FileNotFoundException("Could not resolve _index.html");

            var htmlTemplate = HtmlTemplate.Create(indexFile.ReadAllText());
            htmlTemplate.RegisterComponent<BlazorDiffusion.Pages.ssg.Image>();

            container.AddSingleton<IComponentRenderer>(c => new ComponentRenderer());
            var prerenderer = new Prerenderer
            {
                BaseUrl = appConfig.BaseUrl,
                AppConfig = appConfig,
                HtmlTemplate = htmlTemplate,
                VirtualFiles = virtualFiles,
                Renderer = container.Resolve<IComponentRenderer>(),
                Pages = {
                    new(typeof(Pages.Index),  "/prerender/index.html",  new() {
                        [nameof(Pages.Index.LazyLoad)] = "false",
                        [nameof(Pages.Index.Ssg)] = "true",
                    }),
                    new(typeof(Pages.Albums), "/prerender/albums.html", new() { 
                        [nameof(Pages.Albums.LazyLoad)] = "false" 
                    }),
                }
            };

            using var db = container.Resolve<IDbConnectionFactory>().OpenDbConnection();
            var albums = db.LoadSelect<Album>(x => x.DeletedDate == null)
                .Select(x => x.ToAlbumResult())
                .ToList();

            container.Register(htmlTemplate);

            prerenderer.Pages.Add(new(typeof(Pages.ssg.Albums), "/albums.html",
                transformer: html => htmlTemplate.Render(title: "Albums", body:html)));

            foreach (var album in albums)
            {
                prerenderer.AddAlbum(db, album);
            }

            container.Register<IPrerenderer>(c => prerenderer);
        });
}

public class Prerenderer : IPrerenderer
{
    public string BaseUrl { get; set; }
    public AppConfig AppConfig { get; set; }
    public HtmlTemplate HtmlTemplate { get; set; }
    public IVirtualFiles VirtualFiles { get; init; }
    public IComponentRenderer Renderer { get; init; }
    public List<PrerenderPage> Pages { get; } = new();

    public void AddAlbum(IDbConnection db, AlbumResult album)
    {
        if (album.Slug == null)
        {
            album.Slug = album.Name.GenerateSlug();
            db.UpdateOnly(() => new Album { Slug = album.Slug }, where: x => x.Id == album.Id);
        }

        var path = $"/albums/{album.Slug}.html";
        var artifactId = album.ArtifactIds?.FirstOrDefault();
        var artifact = artifactId != null ? db.SingleById<Artifact>(artifactId) : null;
        var albumMeta = HtmlTemplate.CreateMeta(url: path, title: album.Name,
            image: AppConfig.AssetsBasePath.CombineWith(artifact?.FilePath));

        Pages.Add(new(typeof(Pages.ssg.Album), path, new()
        {
            [nameof(BlazorDiffusion.Pages.ssg.Album.RefId)] = album.AlbumRef,
        },
            transformer: html => HtmlTemplate.Render(title: album.Name, head: albumMeta, body: html)));
    }

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
