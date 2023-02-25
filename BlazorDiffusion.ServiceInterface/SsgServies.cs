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
        var ret = new PrerenderResponse { 
            Results = new(),
        };

        foreach (var batch in request.Batches.Safe())
        {
            try
            {
                var maxId = await Db.ScalarAsync<int>(Db.From<Artifact>().Select(x => Sql.Max(x.Id)));
                var maxBatches = Math.Floor(maxId / 1000d);
                if (batch < 0 || batch > maxBatches)
                {
                    ret.Failed.Add($"{batch} not in valid range: 0 to {maxBatches}");
                    continue;
                }

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

                var results = await WriteArtifactHtmlPagesAsync(vfs, artifacts, ret);
                ret.Results.AddRange(results);
            }
            catch (Exception ex)
            {
                var msg = $"Failed executing batch {batch}: {ex.Message} at \n{ex.StackTrace}";
                Log.Error(msg, ex);
                ret.Failed ??= new();
                ret.Failed.Add(msg);
            }
        }

        return ret;
    }

    public async Task<object> Any(PrerenderImage request)
    {
        var ret = new PrerenderResponse { Results = new() };

        var artifact = await Db.SingleByIdAsync<Artifact>(request.ArtifactId);
        if (artifact == null)
            return HttpError.NotFound("Artifact does not exist");

        ret.Results.AddRange(await WriteArtifactHtmlPagesAsync(Prerenderer.VirtualFiles, new() { artifact }, ret));

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

    public async Task<List<string>> WriteArtifactHtmlPagesAsync(IVirtualFiles vfs, List<Artifact> artifacts, PrerenderResponse? ret = null)
    {
        Log.DebugFormat("Writing {0} artifact image html", artifacts.Count);

        var creativeIds = artifacts.Select(a => a.CreativeId).ToSet();
        var creativePrompts = await Db.DictionaryAsync<int, string>(Db.From<Creative>().Where(x => creativeIds.Contains(x.Id))
            .Select(x => new { x.Id, x.UserPrompt }));

        var httpCtx = (base.Request as NetCoreRequest)?.HttpContext;
        var results = new List<string>();

        foreach (var artifact in artifacts)
        {
            try
            {
                var userPrompt = creativePrompts.TryGetValue(artifact.CreativeId, out var prompt)
                    ? prompt
                    : artifact.Prompt.LeftPart(',');
                var slug = Ssg.GenerateSlug(userPrompt);

                var html = await Prerenderer.RenderArtifactHtmlPageAsync(slug, artifact, httpCtx);
                var file = Ssg.GetArtifactFileName(artifact, slug);
                var path = Ssg.GetArtifact(artifact, slug);
                Log.DebugFormat("Writing {0} bytes to {1}...", html.Length, path);
                await vfs.WriteFileAsync(path, html);
                results.Add(file);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to render Artifact {artifact.Id}: {ex.Message} at \n{ex.StackTrace}";
                Log.Error(msg, ex);
                if (ret != null)
                {
                    ret.Failed ??= new();
                    ret.Failed.Add(msg);
                }
            }
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

        var userPrompt = await Db.ScalarAsync<string>(Db.From<Creative>().Where(x => x.Id == artifact.CreativeId).Select(x => x.UserPrompt));
        var slug = Ssg.GenerateSlug(userPrompt);

        if (Request.HasValidCache(artifact.ModifiedDate))
            return HttpResult.NotModified();

        var html = await Prerenderer.RenderArtifactHtmlPageAsync(slug, artifact, (Request as NetCoreRequest)?.HttpContext);

        if (request.Save == true)
        {
            await Prerenderer.VirtualFiles.WriteFileAsync(Ssg.GetArtifact(artifact, slug), html);
        }

        return new HttpResult(html)
        {
            ContentType = MimeTypes.Html,
            LastModified = artifact.ModifiedDate,
            MaxAge = TimeSpan.FromDays(1),
        };
    }
}
