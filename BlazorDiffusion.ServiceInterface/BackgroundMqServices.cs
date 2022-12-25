using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.OrmLite;
using ServiceStack.Host.NetCore;
using BlazorDiffusion.ServiceModel;

namespace BlazorDiffusion.ServiceInterface;

public class BackgroundMqServices : Service
{
    public static ILog Log = LogManager.GetLogger(typeof(BackgroundMqServices));
    public IPrerenderer Prerenderer { get; set; } = default!;
    public IComponentRenderer Renderer { get; set; } = default!;
    public HtmlTemplate HtmlTemplate { get; set; } = default!;
    public IStableDiffusionClient StableDiffusionClient { get; set; } = default!;
    public AppConfig AppConfig { get; set; } = default!;

    public async Task Any(DiskTasks request)
    {
        if (AppConfig.DisableWrites)
            return;

        var creative = request.SaveCreative ?? (request.SaveCreativeId != null
            ? await Db.LoadSingleByIdAsync<Creative>(request.SaveCreativeId)
            : null);

        if (creative != null)
        {
            await StableDiffusionClient.SaveCreativeAsync(creative);
        }

        if (request.SaveFile != null)
        {
            await VirtualFiles.WriteFileAsync(request.SaveFile.FilePath, request.SaveFile.Stream);
        }

        if (request.CdnDeleteFiles != null)
        {
            Prerenderer.VirtualFiles.DeleteFiles(request.CdnDeleteFiles);
        }
    }

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

    static DateTime lastSyncTasksPeriodicRun = DateTime.MinValue;
    async Task PulseSyncTasks()
    {
        if (Interlocked.Read(ref InSyncTasks) > 0)
            return;

        var lastRun = DateTime.UtcNow - lastSyncTasksPeriodicRun;
        if (lastRun > AppConfig.SyncTasksInterval)
        {
            lastSyncTasksPeriodicRun = DateTime.UtcNow;
            await Any(new SyncTasks { Periodic = true });
        }
    }

    static long InSyncTasks = 0;
    public async Task<object> Any(SyncTasks request)
    {
        if (Interlocked.CompareExchange(ref InSyncTasks, 1, 0) > 0)
            return new SyncTasksResponse();

        try
        {
            // Update temporal scores + save all creatives with Artifacts that have changed
            var sw = Stopwatch.StartNew();
            var now = DateTime.UtcNow;
            var msgs = new List<string>();
            void log(string message, params object[] args)
            {
                msgs.Add(string.Format(message, args));
                Log.DebugFormat(message, args);
            }

            var type = request.Periodic == true 
                ? nameof(request.Periodic) 
                    : request.Daily == true 
                        ? nameof(request.Daily) 
                        : "";

            log("SyncTasks {0} started at {1}", type, DateTime.UtcNow.ToString("s"));

            if (request.Periodic == true)
            {
                var swTask = Stopwatch.StartNew();
                var thresholdDate = DateTime.UtcNow.Add(-Scores.TemporalScoreThreshold);
                var artifacts = Db.Select(Db.From<Artifact>()
                    .Join<Creative>((a,c) => a.Id == c.PrimaryArtifactId)
                    .Where(x => x.TemporalScore > 0 || x.CreatedDate >= thresholdDate));

                log("Found {0} artifacts created before {1}", artifacts.Count, thresholdDate.ToString("s"));

                var count = 0;
                foreach (var artifact in artifacts)
                {
                    if (Scores.PopulateTemporalScore(artifact))
                    {
                        count++;
                        await Db.UpdateOnlyAsync(() => new Artifact { TemporalScore = artifact.TemporalScore }, x => x.Id == artifact.Id);
                        Updated.ArtifactScore(artifact.Id);
                    }
                }
                log("SyncTasks Periodic updated {0} artifacts, took {1}ms", count, swTask.ElapsedMilliseconds);

                count = 0;
                swTask.Restart();
                var updatedAlbumsIds = new HashSet<int>();
                while (Updated.AlbumIds.TryTake(out var albumId)) updatedAlbumsIds.Add(albumId);

                var updatedAlbums = await Db.SelectAsync<Album>(x => updatedAlbumsIds.Contains(x.Id));
                foreach (var album in updatedAlbums)
                {
                    var needsUpdating = Scores.PopulateAlbumScores(album);
                    if (needsUpdating)
                    {
                        count++;
                        await Db.UpdateOnlyAsync(() => new Album
                        {
                            LikesCount = album.LikesCount,
                            SearchCount = album.SearchCount,
                            Score = album.Score,
                            ModifiedBy = Users.System.Id.ToString(),
                            ModifiedDate = now,
                        }, x => x.Id == album.Id);
                        Updated.AlbumScore(album.Id);
                    }
                }
                log("SyncTasks Periodic updated {0} albums, took {1}ms", count, swTask.ElapsedMilliseconds);

                if (!AppConfig.DisableWrites)
                {
                    if (Updated.ResetScores() > 0)
                    {
                        swTask.Restart();
                        await Prerenderer.RenderPages();
                        // if external request is needed in future
                        //var jwtProvider = AssertPlugin<AuthFeature>().GetRequiredJwtAuthProvider();
                        //var jwtAdmin = jwtProvider.CreateJwtBearerToken(Users.System.ToUserSession());
                        //var client = new JsonApiClient(AppConfig.BaseUrl) {
                        //    BearerToken = jwtAdmin
                        //};
                        //var api = await client.ApiAsync(new Prerender());
                        //if (!api.Succeeded) log("SyncTasks Prerenderer.RenderPages Failed: {0}", api.Error.GetDetailedError());
                        log("SyncTasks Prerenderer.RenderPages took {0}ms", swTask.ElapsedMilliseconds);
                    }
                }
            }

            if (request.Daily == true)
            {
                var swTask = Stopwatch.StartNew();
                Scores.Clear();
                Scores.Load(Db);
                using var dbAnalytics = OpenDbConnection(Databases.Analytics);
                Scores.LoadAnalytics(dbAnalytics);

                var count = 0;
                var allCreatives = await Db.SelectAsync(Db.From<Creative>());
                foreach (var creative in allCreatives)
                {
                    var creativeArtifacts = await Db.SelectAsync(Db.From<Artifact>().Where(x => x.CreativeId == creative.Id));
                    foreach (var artifact in creativeArtifacts)
                    {
                        var needsUpdating = Scores.PopulateArtifactScores(artifact) || Scores.PopulateTemporalScore(artifact);
                        if (needsUpdating)
                        {
                            count++;
                            await Db.UpdateOnlyAsync(() => new Artifact { 
                                TemporalScore = artifact.TemporalScore,
                                LikesCount = artifact.LikesCount,
                                AlbumsCount = artifact.AlbumsCount,
                                DownloadsCount = artifact.DownloadsCount,
                                SearchCount = artifact.SearchCount,
                                Score = artifact.Score,
                                ModifiedBy = Users.System.Id.ToString(),
                                ModifiedDate = now,
                        }, x => x.Id == artifact.Id);
                            Updated.ArtifactIds.Add(artifact.Id);
                        }
                    }
                }
                log("SyncTasks Daily updated {0} artifacts, took {1}ms", count, swTask.ElapsedMilliseconds);

                count = 0;
                swTask.Restart();
                var allAlbums = await Db.SelectAsync<Album>();
                foreach (var album in allAlbums)
                {
                    var needsUpdating = Scores.PopulateAlbumScores(album);
                    if (needsUpdating)
                    {
                        count++;
                        await Db.UpdateOnlyAsync(() => new Album
                        {
                            LikesCount = album.LikesCount,
                            SearchCount = album.SearchCount,
                            Score = album.Score,
                            ModifiedBy = Users.System.Id.ToString(),
                            ModifiedDate = now,
                        }, x => x.Id == album.Id);
                    }
                }
                log("SyncTasks Daily updated {0} albums, took {1}ms", count, swTask.ElapsedMilliseconds);
            }

            var swWrites = Stopwatch.StartNew();
            int id = 0;
            var creativeIds = new HashSet<int>();
            while (Updated.CreativeIds.TryTake(out id)) creativeIds.Add(id);

            var artifactIds = new HashSet<int>();
            while (Updated.ArtifactIds.TryTake(out id)) artifactIds.Add(id);

            var albumIds = new HashSet<int>();
            while (Updated.AlbumIds.TryTake(out id)) albumIds.Add(id);

            var artifactCreativeIds = await Db.ColumnDistinctAsync<int>(Db.From<Artifact>()
                .Where(x => artifactIds.Contains(x.Id))
                .Select(x => x.CreativeId));
            artifactCreativeIds.Each(x => creativeIds.Add(x));

            if (!AppConfig.DisableWrites)
            {
                log("SyncTasks SaveCreatives {0} / {1}: {2}", creativeIds.Count, artifactCreativeIds.Count, string.Join(",", creativeIds));
                var creatives = await Db.LoadSelectAsync<Creative>(x => creativeIds.Contains(x.Id));
                foreach (var creative in creatives)
                {
                    await StableDiffusionClient.SaveCreativeAsync(creative);
                }
                log("SyncTasks SaveCreatives took {0}ms", swWrites.ElapsedMilliseconds);

                var artifacts = await Db.SelectByIdsAsync<Artifact>(artifactIds);
                await WriteArtifactHtmlPagesAsync(Prerenderer.VirtualFiles, artifacts);
            }

            log("SyncTasks {0} Total took {1}ms", type, sw.ElapsedMilliseconds);

            return new SyncTasksResponse { Results = msgs };
        }
        finally
        {
            Interlocked.CompareExchange(ref InSyncTasks, 0, 1);
        }
    }

    public async Task Any(BackgroundTasks request)
    {
        if (request.NewCreative != null)
        {
            var creative = request.NewCreative;
            var ftsArtifacts = request.NewCreative.Artifacts.Map(x => new ArtifactFts
            {
                rowid = x.Id,
                Prompt = creative.Prompt,
                CreativeId = creative.Id,
                RefId = x.RefId,
            });
            await Db.InsertAllAsync(ftsArtifacts);
            await Any(new DiskTasks { SaveCreative = request.NewCreative });
            await WriteArtifactHtmlPagesAsync(Prerenderer.VirtualFiles, request.NewCreative.Artifacts);
        }

        if (request.RecordArtifactLikeId != null)
            await Scores.IncrementArtifactLikeAsync(Db, request.RecordArtifactLikeId.Value);
        if (request.RecordArtifactUnlikeId != null)
            await Scores.DecrementArtifactLikeAsync(Db, request.RecordArtifactUnlikeId.Value);

        if (request.RecordAlbumLikeId != null)
            await Scores.IncrementAlbumLikeAsync(Db, request.RecordAlbumLikeId.Value);
        if (request.RecordAlbumUnlikeId != null)
            await Scores.DecrementAlbumLikeAsync(Db, request.RecordAlbumUnlikeId.Value);

        if (request.ArtifactIdsAddedToAlbums?.Count > 0)
        {
            foreach (var artifactId in request.ArtifactIdsAddedToAlbums)
            {
                await Scores.IncrementArtifactInAlbumAsync(Db, artifactId);
            }
        }
        if (request.ArtifactIdsRemovedFromAlbums?.Count > 0)
        {
            foreach (var artifactId in request.ArtifactIdsRemovedFromAlbums)
            {
                await Scores.DencrementArtifactInAlbumAsync(Db, artifactId);
            }
        }

        if (request.RecordPrimaryArtifact != null)
        {
            await Scores.ChangePrimaryArtifactAsync(Db,
                request.RecordPrimaryArtifact.CreativeId,
                request.RecordPrimaryArtifact.FromArtifactId,
                request.RecordPrimaryArtifact.ToArtifactId);
        }
        
        await PulseSyncTasks();
    }

    public async Task Any(AnalyticsTasks request)
    {
        
        if (request.RecordArtifactStat != null && !Users.IsAdminOrSystem(request.RecordArtifactStat.AppUserId))
        {
            using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
            await analyticsDb.InsertAsync(request.RecordArtifactStat);

            if (request.RecordArtifactStat.Type == StatType.Download)
                await Scores.IncrementArtifactDownloadAsync(Db, request.RecordArtifactStat.ArtifactId);
        }

        if (request.RecordSearchStat != null && !Users.IsAdminOrSystem(request.RecordSearchStat.AppUserId))
        {
            using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
            await analyticsDb.InsertAsync(request.RecordSearchStat);

            if (request.RecordSearchStat.ArtifactId != null)
                await Scores.IncrementArtifactSearchAsync(Db, request.RecordSearchStat.ArtifactId.Value);

            if (request.RecordSearchStat.Source != AppSource.Top)
            {
                var albumId = request.RecordSearchStat.AlbumId
                    ?? (request.RecordSearchStat.Album != null
                        ? await Db.ScalarAsync<int>(Db.From<Album>().Where(x => x.RefId == request.RecordSearchStat.Album).Select(x => x.Id))
                        : null);
                if (albumId != null)
                    await Scores.IncrementAlbumSearchAsync(Db, albumId.Value);
            }
        }

        await PulseSyncTasks();
    }

    public async Task<object> Any(Prerender request)
    {
        if (!AppConfig.DisableWrites)
            await Prerenderer.RenderPages();
        return new PrerenderResponse();
    }

    public async Task<object> Any(RenderImageHtml request)
    {
        var artifact = await Db.SingleByIdAsync<Artifact>(request.Id);
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
            var files = vfs.GetDirectory($"/artifacts/{batch}")?.GetAllMatchingFiles("*.html") ?? Array.Empty<IVirtualFile>();
            var existingIds = files.Select(x => int.TryParse(x.Name.LeftPart('_'), out var id) ? id : (int?)null)
                .Where(x => x != null)
                .Select(x => x!.Value)
                .ToList();

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
            url: Request.AbsoluteUri,
            title: title,
            image: AppConfig.Instance.AssetsBasePath.CombineWith(artifact.FilePath));

        var componentType = HtmlTemplate.GetComponentType("BlazorDiffusion.Pages.ssg.Image")
            ?? throw HttpError.NotFound("Component not found");
        var httpCtx = (Request as NetCoreRequest)?.HttpContext 
            ?? HttpContextFactory.CreateHttpContext(AppConfig.BaseUrl);

        var args = new Dictionary<string, object>
        {
            [nameof(RenderImageHtml.Id)] = artifact.Id,
            [nameof(RenderImageHtml.Slug)] = artifact.GetSlug(),
        };
        var body = await Renderer.RenderComponentAsync(componentType, httpCtx, args);

        var html = HtmlTemplate.Render(title: title, head: meta, body: body);
        return html;
    }

    public object Any(DevTasks request)
    {
        if (request.DisableWrites != null)
            AppConfig.DisableWrites = request.DisableWrites.Value;
        return new StringResponse { Result = $"DisableWrites = {AppConfig.DisableWrites}" };
    }
}
