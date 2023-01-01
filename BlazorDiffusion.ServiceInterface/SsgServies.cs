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

namespace BlazorDiffusion.ServiceInterface;

public class SsgServies : Service
{
    public static ILog Log = LogManager.GetLogger(typeof(SsgServies));
    public IPrerenderer Prerenderer { get; set; } = default!;
    public IComponentRenderer Renderer { get; set; } = default!;
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

    public Task<List<string>> WriteArtifactHtmlPagesAsync(List<Artifact> artifacts) =>
        WriteArtifactHtmlPagesAsync(Prerenderer.VirtualFiles, artifacts);

    public async Task<List<string>> WriteArtifactHtmlPagesAsync(IVirtualFiles vfs, List<Artifact> artifacts)
    {
        Log.DebugFormat("Writing {0} artifact image html", artifacts.Count);

        var results = new List<string>();

        foreach (var artifact in artifacts)
        {
            var html = await RenderArtifactHtmlPageAsync(artifact);
            var file = artifact.GetHtmlFileName();
            var path = artifact.GetHtmlFilePath();
            Log.DebugFormat("Writing {0} bytes to {1}...", html.Length, path);
            await vfs.WriteFileAsync(path, html);
            results.Add(file);
        }

        return results;
    }

    public async Task<string> RenderArtifactHtmlPageAsync(Artifact artifact)
    {
        string title = artifact.Prompt.LeftPart(',');
        var meta = HtmlTemplate.CreateMeta(
            url: AppConfig.BaseUrl.CombineWith(artifact.GetHtmlFilePath()),
            title: title,
            image: AppConfig.AssetsBasePath.CombineWith(artifact.FilePath));

        var componentType = HtmlTemplate.GetComponentType("BlazorDiffusion.Pages.ssg.Image")
            ?? throw HttpError.NotFound("Component not found");
        var httpCtx = (Request as NetCoreRequest)?.HttpContext
            ?? HttpContextFactory.CreateHttpContext(AppConfig.BaseUrl);

        var args = new Dictionary<string, object>
        {
            [nameof(RenderArtifactHtml.Id)] = artifact.Id,
            [nameof(RenderArtifactHtml.Slug)] = artifact.GetSlug(),
        };
        var body = await Renderer.RenderComponentAsync(componentType, httpCtx, args);

        var html = HtmlTemplate.Render(title: title, head: meta, body: body);
        return html;
    }

    public async Task<object> Any(Prerender request)
    {
        if (!AppConfig.DisableWrites)
            await Prerenderer.RenderPages();
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

        var html = await RenderArtifactHtmlPageAsync(artifact);

        return new HttpResult(html)
        {
            ContentType = MimeTypes.Html,
            LastModified = artifact.ModifiedDate,
            MaxAge = TimeSpan.FromDays(1),
        };
    }
}
