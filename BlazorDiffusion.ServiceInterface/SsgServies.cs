using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.OrmLite;
using ServiceStack.Host.NetCore;
using BlazorDiffusion.ServiceModel;
using System.IO;
using ServiceStack.Host;
using Microsoft.AspNetCore.Http;

namespace BlazorDiffusion.ServiceInterface;

public class SsgServies : Service
{
    public static ILog Log = LogManager.GetLogger(typeof(SsgServies));
    public IPrerenderer Prerenderer { get; set; } = default!;
    public HtmlTemplate HtmlTemplate { get; set; } = default!;
    public IStableDiffusionClient StableDiffusionClient { get; set; } = default!;
    public AppConfig AppConfig { get; set; } = default!;

    public async Task<object> Get(ViewCreativeMetadata request)
    {
        var creative = await Db.SingleByIdAsync<Creative>(request.CreativeId);
        var metadataFile = creative != null ? StableDiffusionClient.GetMetadataFile(creative) : null;
        if (metadataFile == null)
            return HttpError.NotFound("Creative not found");

        var json = metadataFile.ReadAllText();
        var metadataCreative = json.FromJson<Creative>();
        return metadataCreative;
    }

    public async Task<object> Any(TestImageHtml request)
    {
        var maxId = await Db.ScalarAsync<int>(Db.From<Artifact>().Select(x => Sql.Max(x.Id)));
        var idBatches = Math.Floor(maxId / 1000d);
        return $"{maxId}: {idBatches}";
    }

    public async Task<object> Any(PrerenderImages request)
    {
        var ret = new PrerenderResponse { Results = new() };

        foreach (var batch in request.Batches.Safe())
        {
            var maxId = await Db.ScalarAsync<int>(Db.From<Artifact>().Select(x => Sql.Max(x.Id)));
            var maxBatches = Math.Floor(maxId / 1000d);
            if (batch < 0 || batch > maxBatches)
                throw new ArgumentOutOfRangeException(nameof(request.Batches), $"Valid range: 0 to {maxBatches}");

            var vfs = Prerenderer.VirtualFiles;
            var existingIds = new List<int> { -1 };
            if (!request.Force)
            {
                var files = vfs.GetDirectory($"/artifacts/{batch}")?.GetAllMatchingFiles("*.html") ?? Array.Empty<IVirtualFile>();
                existingIds = files.Select(x => int.TryParse(x.Name.LeftPart('_'), out var id) ? id : (int?)null)
                    .Where(x => x != null)
                    .Select(x => x!.Value)
                    .ToList();
            }

            if (existingIds.Count == 0)
                existingIds.Add(-1);

            var from = batch * 1000;
            var to = from + 1000;
            var artifacts = Db.Select(Db.From<Artifact>()
                .Where(x => x.Id >= from && x.Id < to)
                .And(x => !existingIds.Contains(x.Id))
                .OrderBy(x => x.Id)
                .Take(1000))
                .ToList();

            var results = await WriteArtifactHtmlPagesAsync(vfs, artifacts);
            ret.Results.AddRange(results);
        }

        return ret;
    }

    public async Task<object> Any(PrerenderImage request)
    {
        var ret = new PrerenderResponse { Results = new() };

        var artifact = await Db.SingleByIdAsync<Artifact>(request.ArtifactId);
        if (artifact == null)
            return HttpError.NotFound("Artifact does not exist");

        ret.Results.AddRange(await WriteArtifactHtmlPagesAsync(Prerenderer.VirtualFiles, new() { artifact }));

        return ret;
    }

    public async Task<object> Any(PrerenderSitemap request)
    {
        var vfs = Prerenderer.VirtualFiles;
        var req = new BasicRequest();
        var feature = GetPlugin<SitemapFeature>();
        var indexHandler = new SitemapFeature.SitemapIndexHandler(feature);
        await indexHandler.ProcessRequestAsync(req, req.Response, nameof(PrerenderSitemap));
        var contents = await req.Response.OutputStream.ReadToEndAsync();
        await vfs.WriteFileAsync("/sitemap.xml", contents);

        foreach (var sitemap in feature.SitemapIndex)
        {
            var handler = new SitemapFeature.SitemapUrlSetHandler(feature, sitemap.UrlSet);
            req = new BasicRequest();
            await handler.ProcessRequestAsync(req, req.Response, nameof(PrerenderSitemap));
            contents = await req.Response.OutputStream.ReadToEndAsync();
            await vfs.WriteFileAsync(sitemap.AtPath, contents);
        }

        return new PrerenderResponse();
    }

    public Task<List<string>> WriteArtifactHtmlPagesAsync(List<Artifact> artifacts) =>
        WriteArtifactHtmlPagesAsync(Prerenderer.VirtualFiles, artifacts);

    public async Task<List<string>> WriteArtifactHtmlPagesAsync(IVirtualFiles vfs, List<Artifact> artifacts)
    {
        Log.DebugFormat("Writing {0} artifact image html", artifacts.Count);

        var httpCtx = (base.Request as NetCoreRequest)?.HttpContext;
        var results = new List<string>();

        foreach (var artifact in artifacts)
        {
            var html = await Prerenderer.RenderArtifactHtmlPageAsync(artifact, httpCtx);
            var file = Ssg.GetArtifactFileName(artifact);
            var path = Ssg.GetArtifact(artifact);
            Log.DebugFormat("Writing {0} bytes to {1}...", html.Length, path);
            await vfs.WriteFileAsync(path, html);
            results.Add(file);
        }

        return results;
    }

    public async Task<object> Any(Prerender request)
    {
        if (!AppConfig.DisableWrites)
            await Prerenderer.RenderPages((Request as NetCoreRequest)?.HttpContext);
        return new PrerenderResponse();
    }

    public async Task<object> Any(RenderArtifactHtml request)
    {
        var id = request.Id ?? request.Slug.LeftPart('_').ToInt();

        var artifact = await Db.SingleByIdAsync<Artifact>(id);
        if (artifact == null)
            throw HttpError.NotFound("Image does not exist");

        if (Request.HasValidCache(artifact.ModifiedDate))
            return HttpResult.NotModified();

        var html = await Prerenderer.RenderArtifactHtmlPageAsync(artifact, (Request as NetCoreRequest)?.HttpContext);

        if (request.Save == true)
        {
            await Prerenderer.VirtualFiles.WriteFileAsync(Ssg.GetArtifact(artifact), html);
        }

        return new HttpResult(html)
        {
            ContentType = MimeTypes.Html,
            LastModified = artifact.ModifiedDate,
            MaxAge = TimeSpan.FromDays(1),
        };
    }
}
