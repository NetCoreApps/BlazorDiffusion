using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorDiffusion.ServiceInterface;

public static class Updated
{
    public static ConcurrentBag<int> CreativeIds = new();
    public static ConcurrentBag<int> ArtifactIds = new();
    public static ConcurrentBag<int> AlbumIds = new();
}

public static class Scores
{
    public static class Weights
    {
        public const int PrimaryArtifact = 10;
        public const int Like = 4;
        public const int InAlbum = 5;
        public const int Download = 2;
        public const int Search = 1;
    }

    public static ConcurrentDictionary<int, int> CreativePrimaryArtifactMap = new();
    public static ConcurrentDictionary<int, int> PrimaryArtifactCreativeMap = new();

    public static ConcurrentDictionary<int, int> ArtifactLikesCountMap = new();
    public static ConcurrentDictionary<int, int> ArtifactInAlbumsCountMap = new();
    public static ConcurrentDictionary<int, int> ArtifactDownloadsCountMap = new();
    public static ConcurrentDictionary<int, int> ArtifactSearchCountMap = new();
    
    public static ConcurrentDictionary<int, int> AlbumSearchCountMap = new();

    static bool LogDuplicates = false;

    public static void Load(IDbConnection db)
    {
        var sw = Stopwatch.StartNew();

        CreativePrimaryArtifactMap = new(db.Dictionary<int, int>(db.From<Creative>().Where(x => x.PrimaryArtifactId != null).Select(x => new {
            x.Id,
            x.PrimaryArtifactId,
        })));

        if (LogDuplicates)
        {
            var artifactRefsMap = db.Dictionary<int, string>(db.From<Artifact>().Select(x => new { x.Id, x.RefId }));
            var lastValue = -1;
            foreach (var entry in CreativePrimaryArtifactMap.ToList().OrderBy(x => x.Value))
            {
                var dupe = lastValue == entry.Value ? "DUPLICATE" : "";
                var refId = artifactRefsMap[entry.Value];
                $"{entry.Value.ToString().PadRight(4, ' ')}: {entry.Key} {refId} {dupe}".Print();
                lastValue = entry.Value;
            }
        }

        var valueMap = CreativePrimaryArtifactMap.Select(x => KeyValuePair.Create(x.Value, x.Key));
        PrimaryArtifactCreativeMap = new(valueMap);

        ArtifactLikesCountMap = new(db.Dictionary<int, int>(db.From<ArtifactLike>().GroupBy(x => x.ArtifactId).Select(x => new {
            x.ArtifactId,
            Count = Sql.Count("*"),
        })));

        ArtifactInAlbumsCountMap = new(db.Dictionary<int, int>(db.From<AlbumArtifact>().GroupBy(x => x.ArtifactId).Select(x => new {
            x.ArtifactId,
            Count = Sql.Count("*"),
        })));
        
        ArtifactDownloadsCountMap = new(db.Dictionary<int, int>(db.From<ArtifactStat>()
            .Where(x => x.Type == StatType.Download)
            .GroupBy(x => x.ArtifactId).Select(x => new {
                x.ArtifactId,
                Count = Sql.Count("*"),
            })));

        ArtifactSearchCountMap = new(db.Dictionary<int, int>(db.From<SearchStat>().Join<Artifact>((s, a) => s.Similar == a.RefId)
            .GroupBy(x => x.Similar).Select<SearchStat, Artifact>((s, a) => new {
                a.Id,
                Count = Sql.Count("*"),
            })));

        AlbumSearchCountMap = new(db.Dictionary<int, int>(db.From<SearchStat>().Join<Album>((s, a) => s.Album == a.RefId)
            .GroupBy(x => x.Album).Select<SearchStat, Album>((s, a) => new {
                a.Id,
                Count = Sql.Count("*"),
            })));

        Console.WriteLine($"Scores.Load() took {sw.ElapsedMilliseconds}ms");
    }

    public static void PopulateArtifactScores(Artifact artifact)
    {
        var isPrimary = PrimaryArtifactCreativeMap.ContainsKey(artifact.Id);
        artifact.LikesCount = ArtifactLikesCountMap.TryGetValue(artifact.Id, out var likes) ? likes : 0;
        artifact.AlbumsCount = ArtifactInAlbumsCountMap.TryGetValue(artifact.Id, out var albums) ? albums : 0;
        artifact.DownloadsCount = ArtifactDownloadsCountMap.TryGetValue(artifact.Id, out var downloads) ? downloads : 0;
        artifact.SearchCount = ArtifactSearchCountMap.TryGetValue(artifact.Id, out var searches) ? searches : 0;
        artifact.Score = Calculate(isPrimary:isPrimary, artifact: artifact);
    }

    public static int Calculate(bool isPrimary, Artifact artifact) =>
        Calculate(isPrimary, artifact.LikesCount, artifact.AlbumsCount, artifact.DownloadsCount, artifact.SearchCount);

    public static int Calculate(bool isPrimary, int noOfLikes, int noOfAlbums, int noOfDownloads, int noOfSearches)
    {
        return (isPrimary ? Weights.PrimaryArtifact : 0)
            + (noOfLikes * Weights.Like)
            + (noOfAlbums + Weights.InAlbum)
            + (noOfDownloads + Weights.Download)
            + (noOfSearches + Weights.Search);
    }

    public static async Task ChangePrimaryArtifactAsync(IDbConnection db, int creativeId, int? fromArtifactId, int? toArtifactId)
    {
        if (fromArtifactId != null)
        {
            await db.UpdateAddAsync(() => new Artifact { Score = -Weights.PrimaryArtifact }, where: x => x.Id == fromArtifactId.Value);
            CreativePrimaryArtifactMap.TryRemove(creativeId, out _);
            PrimaryArtifactCreativeMap.TryRemove(fromArtifactId.Value, out _);
            Updated.ArtifactIds.Add(fromArtifactId.Value);
        }
        if (toArtifactId != null)
        {
            await db.UpdateAddAsync(() => new Artifact { Score = Weights.PrimaryArtifact }, where: x => x.Id == toArtifactId.Value);
            CreativePrimaryArtifactMap[creativeId] = toArtifactId.Value;
            PrimaryArtifactCreativeMap[toArtifactId.Value] = creativeId;
            Updated.ArtifactIds.Add(toArtifactId.Value);
        }
    }

    public static async Task SetPrimayArtifactAsync(IDbConnection db, int creativeId, int artifactId)
    {
        CreativePrimaryArtifactMap[creativeId] = artifactId;
        PrimaryArtifactCreativeMap[artifactId] = creativeId;

        await db.UpdateAddAsync(() => new Artifact { Score = Weights.PrimaryArtifact }, where: x => x.Id == artifactId);
        Updated.ArtifactIds.Add(artifactId);
    }

    public static async Task IncrementArtifactLikeAsync(IDbConnection db, int artifactId)
    {
        ArtifactLikesCountMap[artifactId] = ArtifactLikesCountMap.TryGetValue(artifactId, out var count)
            ? count + 1
            : 1;
        await db.UpdateAddAsync(() => new Artifact { LikesCount = 1, Score = Weights.Like }, where: x => x.Id == artifactId);
        Updated.ArtifactIds.Add(artifactId);
    }

    public static async Task DecrementArtifactLikeAsync(IDbConnection db, int artifactId)
    {
        ArtifactLikesCountMap[artifactId] = ArtifactLikesCountMap.TryGetValue(artifactId, out var count)
            ? Math.Min(count - 1, 0)
            : 0;
        await db.UpdateAddAsync(() => new Artifact { LikesCount = -1, Score = -Weights.Like }, where: x => x.Id == artifactId);
        Updated.ArtifactIds.Add(artifactId);
    }

    public static async Task IncrementArtifactInAlbumAsync(IDbConnection db, int artifactId)
    {
        ArtifactInAlbumsCountMap[artifactId] = ArtifactInAlbumsCountMap.TryGetValue(artifactId, out var count)
            ? count + 1
            : 1;
        await db.UpdateAddAsync(() => new Artifact { AlbumsCount = 1, Score = Weights.InAlbum }, where: x => x.Id == artifactId);
        Updated.ArtifactIds.Add(artifactId);
    }

    public static async Task DencrementArtifactInAlbumAsync(IDbConnection db, int artifactId)
    {
        ArtifactInAlbumsCountMap[artifactId] = ArtifactInAlbumsCountMap.TryGetValue(artifactId, out var count)
            ? Math.Min(count - 1, 0)
            : 0;
        await db.UpdateAddAsync(() => new Artifact { AlbumsCount = -1, Score = -Weights.InAlbum }, where: x => x.Id == artifactId);
        Updated.ArtifactIds.Add(artifactId);
    }

    public static async Task IncrementArtifactDownloadAsync(IDbConnection db, int artifactId)
    {
        ArtifactLikesCountMap[artifactId] = ArtifactDownloadsCountMap.TryGetValue(artifactId, out var count)
            ? count + 1
            : 1;
        await db.UpdateAddAsync(() => new Artifact { DownloadsCount = 1, Score = Weights.Download }, where: x => x.Id == artifactId);
        Updated.ArtifactIds.Add(artifactId);
    }

    public static async Task IncrementArtifactSearchAsync(IDbConnection db, int artifactId)
    {
        ArtifactSearchCountMap[artifactId] = ArtifactSearchCountMap.TryGetValue(artifactId, out var count)
            ? count + 1
            : 1;
        await db.UpdateAddAsync(() => new Artifact { SearchCount = 1, Score = Weights.Search }, where: x => x.Id == artifactId);
        Updated.ArtifactIds.Add(artifactId);
    }

    public static async Task IncrementAlbumSearchAsync(IDbConnection db, int albumId)
    {
        AlbumSearchCountMap[albumId] = AlbumSearchCountMap.TryGetValue(albumId, out var count)
            ? count + 1
            : 1;
        await db.UpdateAddAsync(() => new Album { SearchCount = 1, Score = Weights.Search }, where: x => x.Id == albumId);
        Updated.AlbumIds.Add(albumId);
    }
}
