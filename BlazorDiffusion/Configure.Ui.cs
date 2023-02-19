using System.Diagnostics;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using Amazon.S3;
using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.UI;
using System.Text;
using System.Data;
using Funq;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceStack.Text;
using BlazorDiffusion.Pages.ssg;
using ServiceStack;

[assembly: HostingStartup(typeof(BlazorDiffusion.ConfigureUi))]

namespace BlazorDiffusion;

public class ConfigureUi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddSingleton<RazorPagesEngine>();
        })
        .ConfigureAppHost(afterConfigure:appHost =>
        {
            if (appHost.IsRunAsAppTask())
                return;

            var container = appHost.GetContainer();
            var s3Client = container.Resolve<AmazonS3Client>();
            var appConfig = container.Resolve<AppConfig>();

            IVirtualFiles virtualFiles = appHost.GetHostingEnvironment().IsDevelopment()
                ? new FileSystemVirtualFiles(Path.GetFullPath(Path.Combine(appHost.GetWebRootPath(), appHost.AppSettings.GetString("BlazorWebRoot"))))
                : new R2VirtualFiles(s3Client, appConfig.CdnBucket);

            using var db = container.Resolve<IDbConnectionFactory>().OpenDbConnection();
            var albums = db.LoadSelect<Album>(x => x.DeletedDate == null)
                .Select(x => x.ToAlbumResult())
                .ToList();

            var prerenderer = InitPrerendererWithRazorPages(container, appConfig, virtualFiles, db, albums);

            //var indexFile = appHost.GetVirtualFileSource<FileSystemVirtualFiles>().GetFile("_index.html");
            //if (indexFile == null)
            //    throw new FileNotFoundException("Could not resolve _index.html");
            //var prerenderer = InitPrerendererWithBlazor(container, appConfig, virtualFiles, indexFile, db, albums);

            container.Register<IPrerenderer>(c => prerenderer);
            appHost.Plugins.Add(CreateSiteMap(db, baseUrl: appConfig.BaseUrl));
        });


    SitemapFeature CreateSiteMap(IDbConnection db, string baseUrl)
    {
        var albums = db.LoadSelect<Album>();

        var to = new SitemapFeature() {
            SitemapIndex =
            {
                new Sitemap
                {
                    Location = baseUrl.CombineWith("/sitemaps/sitemap-albums.xml"),
                    AtPath = "/sitemaps/sitemap-albums.xml",
                    LastModified = albums.Max(x => x.ModifiedDate),
                    UrlSet = albums.Map(x => new SitemapUrl {
                        Location = baseUrl.CombineWith(Ssg.GetAlbum(x.ToAlbumResult(), 1)),
                        LastModified = x.Artifacts.Max(x => x.ModifiedDate),
                        ChangeFrequency = SitemapFrequency.Daily,
                    })
                }
            }
        };

        foreach (var album in albums)
        {
            var result = album.ToAlbumResult();
            var total = result.ArtifactIds?.Count ?? 0;
            var pages = (int)Math.Ceiling(total / (double)UserState.StaticPagedTake);
            var albumArtifacts = db.Select(db.From<Artifact>()
                .Join<AlbumArtifact>((a, l) => a.Id == l.ArtifactId && l.AlbumId == album.Id)
                .OrderByDescending<AlbumArtifact>(x => x.Id));
            for (var i = 0; i < pages; i++)
            {
                var pageNo = i + 1;
                var suffix = pages == 1 ? "" : "_" + pageNo;
                var path = $"/sitemaps/albums/sitemap-{album.Slug}{suffix}.xml";
                var pageArtifacts = albumArtifacts.Skip(i * UserState.StaticPagedTake).Take(UserState.StaticPagedTake);
                to.SitemapIndex.Add(new Sitemap
                {
                    Location = baseUrl.CombineWith(path),
                    AtPath = path,
                    LastModified = pageArtifacts.Max(x => x.ModifiedDate),
                    UrlSet = pageArtifacts.Map(x => new SitemapUrl()
                    {
                        Location = baseUrl.CombineWith(Ssg.GetArtifact(x)),
                        LastModified = x.ModifiedDate,
                        ChangeFrequency = SitemapFrequency.Monthly,
                    })
                });
            }
        }

        var pageSize = 10000;
        var index = 0;
        while (true)
        {
            var pageNo = index + 1;
            var suffix = "_" + pageNo;
            var artifacts = db.Select(db.From<Artifact>().OrderBy(x => x.Id).Skip(index++ * pageSize).Take(pageSize));
            var path = $"/sitemaps/artifacts/sitemap{suffix}.xml";
            to.SitemapIndex.Add(new Sitemap
            {
                Location = baseUrl.CombineWith(path),
                AtPath = path,
                LastModified = artifacts.Max(x => x.ModifiedDate),
                UrlSet = artifacts.Map(x => new SitemapUrl()
                {
                    Location = baseUrl.CombineWith(Ssg.GetArtifact(x)),
                    LastModified = x.ModifiedDate,
                    ChangeFrequency = SitemapFrequency.Monthly,
                })
            });

            if (artifacts.Count < pageSize)
                break;
        }
        return to;
    }

    private static PrerendererWithRazorPages InitPrerendererWithRazorPages(Container container, AppConfig appConfig, IVirtualFiles virtualFiles, IDbConnection db, List<AlbumResult> albums)
    {

        var prerenderer = new PrerendererWithRazorPages
        {
            BaseUrl = appConfig.BaseUrl,
            AppConfig = appConfig,
            RazorPages = container.Resolve<RazorPagesEngine>(),
            VirtualFiles = virtualFiles,
        };

        prerenderer.AddPage(Ssg.Pages.Home, Ssg.Empty.Home);

        prerenderer.AddPage(Ssg.Pages.Top, Ssg.GetTop());

        prerenderer.AddPage(Ssg.Pages.Albums, Ssg.GetAlbums());
        foreach (var album in albums)
        {
            prerenderer.AddAlbum(db, album);
        }

        var artifactsCount = db.Scalar<int>(db.From<Artifact>().Join<Creative>((a, c) => c.Id == a.CreativeId)
            .Where<Artifact, Creative>((a, c) => c.PrimaryArtifactId == a.Id)
            .Select(x => Sql.Count("*")));
        int pages = (int)Math.Ceiling(artifactsCount / (double)UserState.StaticPagedTake);

        for (var page = 0; page < pages; page++)
        {
            var scopePage = page + 1;
            prerenderer.AddPage(Ssg.Pages.Latest, Ssg.GetLatest(scopePage), () => new LatestModel { Page = scopePage });
        }

        var log = LogManager.GetLogger(typeof(ConfigureUi));
        log.InfoFormat("InitPrerendererWithRazorPages: {0} albums, {1} artifacts, {2} latest pages",
            albums.Count, artifactsCount, pages);

        return prerenderer;
    }

    private static PrerendererWithBlazor InitPrerendererWithBlazor(Container container, AppConfig appConfig, IVirtualFiles virtualFiles, IVirtualFile indexFile, IDbConnection db, List<AlbumResult> albums)
    {
        container.AddSingleton<IComponentRenderer>(c => new ComponentRenderer());

        var htmlTemplate = HtmlTemplate.Create(indexFile.ReadAllText());
        htmlTemplate.RegisterComponent<BlazorDiffusion.Pages.ssb.Image>();

        var prerenderer = new PrerendererWithBlazor
        {
            BaseUrl = appConfig.BaseUrl,
            AppConfig = appConfig,
            HtmlTemplate = htmlTemplate,
            VirtualFiles = virtualFiles,
            Renderer = container.Resolve<IComponentRenderer>(),
            Pages = {
                new(typeof(Pages.Index),  "/prerender/index.html",  new() {
                    [nameof(Pages.Index.Ssg)] = "true",
                }),
                new(typeof(Pages.Albums), "/prerender/albums.html", new() { }),
            }
        };

        container.Register(htmlTemplate);

        prerenderer.Pages.Add(new(typeof(Pages.ssb.Top), "/top.html",
            transformer: html => htmlTemplate.Render(title: "Top Images", body: html)));

        prerenderer.Pages.Add(new(typeof(Pages.ssb.Latest), "/latest.html",
            transformer: html => htmlTemplate.Render(title: "Latest Images", body: html)));

        prerenderer.Pages.Add(new(typeof(Pages.ssb.Albums), "/albums.html",
            transformer: html => htmlTemplate.Render(title: "Albums", body: html)));

        foreach (var album in albums)
        {
            prerenderer.AddAlbum(db, album);
        }

        return prerenderer;
    }
}


public class PrerendererWithRazorPages : IPrerenderer
{
    public string BaseUrl { get; set; }
    public AppConfig AppConfig { get; set; }
    public IVirtualFiles VirtualFiles { get; set; }
    public RazorPagesEngine RazorPages { get; set; }
    public List<PrerenderView> Pages { get; } = new();

    public IView AssertPage(string viewPath)
    {
        var viewResult = RazorPages.GetView(viewPath);
        if (!viewResult.Success || viewResult.View == null)
            throw new FileNotFoundException($"Could not find RazorPage at: ${viewPath}");
        return viewResult.View!;
    }

    public void AddPage(string viewPath, string writePath, Func<PageModel>? pageModelFactory = null)
    {
        AssertPage(viewPath);
        Pages.Add(new PrerenderView
        {
            ViewPath = viewPath,
            WritePath = writePath,
            PageModelFactory = pageModelFactory,
        });
    }

    public void AddAlbum(IDbConnection db, AlbumResult album)
    {
        AssertPage(Ssg.Pages.Album);
        if (album.Slug == null)
        {
            album.Slug = album.Name.GenerateSlug();
            db.UpdateOnly(() => new Album { Slug = album.Slug }, where: x => x.Id == album.Id);
        }

        var total = album.ArtifactIds?.Count ?? 0;
        var pages = (int)Math.Ceiling(total / (double)UserState.StaticPagedTake);
        for (var i = 0; i < pages; i++)
        {
            var pageNo = i + 1;
            var path = Ssg.GetAlbum(album, pageNo);
            var artifactId = album.ArtifactIds?.Skip(i * UserState.StaticPagedTake).FirstOrDefault();
            AddPage(Ssg.Pages.Album, path, () => new AlbumModel {
                Gateway = HostContext.AppHost.GetServiceGateway(),
                RefId = album.AlbumRef,
                Page = pageNo,
            });
        }
    }

    public async Task RenderPages(HttpContext? httpContext = null)
    {
        var log = LogManager.GetLogger(GetType());

        var sw = Stopwatch.StartNew();

        async Task renderPage(PrerenderView page)
        {
            sw.Restart();
            var view = AssertPage(page.ViewPath);
            var model = page.PageModelFactory?.Invoke();
            var path = page.WritePath;
            log.DebugFormat("Rendering {0} to {1} {2}...", page.ViewPath, VirtualFiles.GetType().Name, path);
            var html = await RenderToHtmlAsync(view, model, httpContext);
            log.DebugFormat("Rendered {0} in {1} bytes, took {2}ms", page.ViewPath, html?.Length ?? -1, sw.ElapsedMilliseconds);

            if (!string.IsNullOrEmpty(html))
            {
                await VirtualFiles.WriteFileAsync(path, html);
            }
            else
            {
                VirtualFiles.DeleteFile(path);
            }
        }

        foreach (var page in Pages)
        {
            try
            {
                await renderPage(page);
            }
            catch (Exception e)
            {
                log.Error(e, "Error trying to render {0}: {1}, retrying...", page.ViewPath, e.Message);
                try
                {
                    await renderPage(page);
                    log.Debug($"#2 Retry of {page.ViewPath} succeeded");
                }
                catch (Exception e2)
                {
                    log.Error(e2, "Error trying to render#2 {0}: {1}", page.ViewPath, e2.Message);
                }
            }
        }
    }

    public async Task<string> RenderToHtmlAsync(IView? page, PageModel? model = null, HttpContext? httpContext = null, CancellationToken token = default)
    {
        if (model is IGetPageModel getPage)
        {
            await getPage.OnGetAsync();
        }

        var ms = MemoryStreamFactory.GetStream();
        if (!ms.CanWrite)
        {
            LogManager.GetLogger(GetType()).Warn("MemoryStreamFactory Stream is disposed, using new MemoryStream...");
            ms = new();
        }

        try
        {
            await RazorPages.WriteHtmlAsync(ms, page, model);
            ms.Position = 0;
            var html = Encoding.UTF8.GetString((await ms.ReadFullyAsMemoryAsync(token)).Span);
            return html;
        }
        finally
        {
            try { ms.Dispose(); } catch { }
        }
    }

    public async Task<string> RenderArtifactHtmlPageAsync(Artifact artifact, HttpContext? httpContext = null, CancellationToken token = default)
    {
        var imageView = AssertPage(Ssg.Pages.Image);
        var pageModel = new ImageModel
        {
            Id = artifact.Id,
            Slug = Ssg.GetSlug(artifact),
        };
        var html = await RenderToHtmlAsync(imageView, pageModel, httpContext, token);
        return html;
    }
}

public class PrerendererWithBlazor : IPrerenderer
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

        var total = album.ArtifactIds?.Count ?? 0;
        var pages = (int)Math.Ceiling(total / (double)UserState.StaticPagedTake);
        for (var i=0; i<pages; i++)
        {
            var pageNo = i + 1;
            var path = Ssg.GetAlbum(album, pageNo);
            var artifactId = album.ArtifactIds?.Skip(i * UserState.StaticPagedTake).FirstOrDefault();
            var artifact = artifactId != null ? db.SingleById<Artifact>(artifactId) : null;
            var albumMeta = HtmlTemplate.CreateMeta(url: path, title: album.Name + (pageNo > 1 ? $" Page {pageNo}" : ""),
                image: AppConfig.AssetsBasePath.CombineWith(artifact?.FilePath));

            Pages.Add(new(typeof(Pages.ssb.Album), path, new()
            {
                [nameof(BlazorDiffusion.Pages.ssb.Album.RefId)] = album.AlbumRef,
                [nameof(BlazorDiffusion.Pages.ssb.Album.Page)] = pageNo,
            },
                transformer: html => HtmlTemplate.Render(title: album.Name, head: albumMeta, body: html)));
        }
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

    public async Task<string> RenderArtifactHtmlPageAsync(Artifact artifact, HttpContext? httpContext = null, CancellationToken token = default)
    {
        string title = artifact.Prompt.LeftPart(',');
        var meta = HtmlTemplate.CreateMeta(
            url: AppConfig.BaseUrl.CombineWith(Ssg.GetArtifact(artifact)),
            title: title,
            image: AppConfig.AssetsBasePath.CombineWith(artifact.FilePath));

        var componentType = HtmlTemplate.GetComponentType("BlazorDiffusion.Pages.ssg.Image")
            ?? throw HttpError.NotFound("Component not found");
        var httpCtx = httpContext ?? HttpContextFactory.CreateHttpContext(AppConfig.BaseUrl);

        var args = new Dictionary<string, object>
        {
            [nameof(RenderArtifactHtml.Id)] = artifact.Id,
            [nameof(RenderArtifactHtml.Slug)] = Ssg.GetSlug(artifact),
        };
        var body = await Renderer.RenderComponentAsync(componentType, httpCtx, args);

        var html = HtmlTemplate.Render(title: title, head: meta, body: body);
        return html;
    }
}
