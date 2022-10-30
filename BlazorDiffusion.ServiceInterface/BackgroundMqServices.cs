using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;
using System.Threading.Tasks;

namespace BlazorDiffusion.ServiceInterface;

public class BackgroundMqServices : Service
{
    public IStableDiffusionClient StableDiffusionClient { get; set; }

    public async Task Any(DiskTasks request)
    {
        var creative = request.SaveCreative ?? (request.SaveCreativeId != null
            ? await Db.LoadSingleByIdAsync<Creative>(request.SaveCreativeId)
            : null);

        if (creative != null)
        {
            await StableDiffusionClient.SaveCreativeAsync(creative);
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

    public async Task Any(SyncTasks request)
    {
        // Update temporal scores + save all creatives with Artifacts that have changed
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
        }

        if (request.RecordArtifactLikeId != null)
            await Scores.IncrementArtifactLikeAsync(Db, request.RecordArtifactLikeId.Value);
        if (request.RecordArtifactUnlikeId != null)
            await Scores.DecrementArtifactLikeAsync(Db, request.RecordArtifactUnlikeId.Value);

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
    }

    public async Task Any(AnalyticsTasks request)
    {
        using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
        
        if (request.RecordArtifactStat != null)
        {
            await analyticsDb.InsertAsync(request.RecordArtifactStat);

            if (request.RecordArtifactStat.Type == StatType.Download)
                await Scores.IncrementArtifactDownloadAsync(Db, request.RecordArtifactStat.ArtifactId);
        }

        if (request.RecordSearchStat != null)
        {
            await analyticsDb.InsertAsync(request.RecordSearchStat);

            if (request.RecordSearchStat.ArtifactId != null)
                await Scores.IncrementArtifactSearchAsync(Db, request.RecordSearchStat.ArtifactId.Value);

            var albumId = request.RecordSearchStat.AlbumId
                ?? (request.RecordSearchStat.Album != null
                    ? await Db.ScalarAsync<int>(Db.From<Album>().Where(x => x.RefId == request.RecordSearchStat.Album).Select(x => x.Id))
                    : null);
            if (albumId != null)
                await Scores.IncrementAlbumSearchAsync(Db, albumId.Value);
        }
    }
}
