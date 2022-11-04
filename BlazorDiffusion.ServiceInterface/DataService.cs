using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.OrmLite;
using BlazorDiffusion.ServiceModel;
using CoenM.ImageHash.HashAlgorithms;

namespace BlazorDiffusion.ServiceInterface;

public class DataService : Service
{
    public IAutoQueryDb AutoQuery { get; set; }

    // TODO Home page search
    public async Task<object> Any(SearchArtifacts query)
    {
        var search = query.Query ?? "";

        using var db = AutoQuery.GetDb(query, base.Request);
        var q = AutoQuery.CreateQuery(query, base.Request, db);

        var similar = query.Similar?.Trim();
        var similarToArtifact = !string.IsNullOrEmpty(similar)
            ? await Db.SingleAsync<Artifact>(x => x.RefId == similar)
            : null;
        // ?similar={RefId}&by=[^background|avg|diff|...perceptual]
        if (similarToArtifact != null)
        {
            const string DefaultSimilarSearch = "background";

            if (similarToArtifact.MissingImageDetails())
            {
                var hashAlgorithm = new PerceptualHash();
                var artifactFile = VirtualFiles.GetFile(similarToArtifact.FilePath);
                using var filStream = artifactFile.OpenRead();
                similarToArtifact.LoadImageDetails(filStream);
                await Db.UpdateOnlyAsync(() => new Artifact
                {
                    PerceptualHash = similarToArtifact.PerceptualHash,
                    AverageHash = similarToArtifact.AverageHash,
                    DifferenceHash = similarToArtifact.DifferenceHash,
                    Background = similarToArtifact.Background,
                },
                    where: x => x.Id == similarToArtifact.Id);
            }

            var by = query.By ?? DefaultSimilarSearch;
            var background = by == "background";
            var perceptual = !background;

            q.Join<Creative>();
            q.OrderByDescending("Quality");

            if (background)
            {
                db.RegisterBgCompare();
                q.SelectDistinct<Artifact, Creative>((a, c) => new {
                    a,
                    c.UserPrompt,
                    c.ArtistNames,
                    c.ModifierNames,
                    c.PrimaryArtifactId,
                    Similarity = Sql.Custom($"bgcompare('{similarToArtifact.Background}',Background)"),
                });
                var isLandscape = similarToArtifact.Width > similarToArtifact.Height;
                if (isLandscape)
                    q.Where(x => x.Width >= x.Height);
                var isPortrait = similarToArtifact.Height > similarToArtifact.Width;
                if (isPortrait)
                    q.Where(x => x.Height >= x.Width);

                q.Where("Similarity <= 50");
                q.ThenBy("Similarity");
            }
            else
            {
                var fnArgs = by == "avg"
                    ? $"{similarToArtifact.AverageHash},AverageHash"
                    : by == "diff"
                        ? $"{similarToArtifact.DifferenceHash},DifferenceHash"
                        : $"{similarToArtifact.PerceptualHash},PerceptualHash";

                db.RegisterImgCompare();

                q.Where("Similarity >= 60");
                q.SelectDistinct<Artifact, Creative>((a, c) => new {
                    a,
                    c.UserPrompt,
                    c.ArtistNames,
                    c.ModifierNames,
                    c.PrimaryArtifactId,
                    Similarity = Sql.Custom($"imgcompare({fnArgs})"),
                });
                q.ThenByDescending("Similarity");
            }

        }
        else
        {
            // Only return pinned artifacts
            var showUserLikes = query.User != null && query.Show == "likes";
            if (!showUserLikes)
            {
                q.Join<Creative>((a, c) => c.Id == a.CreativeId && a.Id == c.PrimaryArtifactId);
            }
            q.OrderByDescending(x => x.Quality); // always show bad images last

            if (!string.IsNullOrEmpty(search))
            {
                //q.Where<Creative>(x => x.Prompt.Contains(search)); // basic search
                search = search.Replace("\"", ""); // escape
                if (search.EndsWith('s')) // allow wildcard search to match on both
                    search = Words.Singularize(search);
                var ftsSearch = search.Quoted() + "*"; // wildcard search
                q.Join<ArtifactFts>((a, f) => a.Id == f.rowid);
                q.Where(q.Column<ArtifactFts>(x => x.Prompt, prefixTable: true) + " match {0}", ftsSearch);
                q.ThenBy(q.Column<ArtifactFts>("Rank", prefixTable: true));
            }
            else if (query.Modifier != null)
            {
                q.Join<Creative, CreativeModifier>((creative, modifierRef) => creative.Id == modifierRef.CreativeId)
                 .Join<CreativeModifier, Modifier>((modifierRef, modifier) => modifierRef.ModifierId == modifier.Id && modifier.Name == query.Modifier);
            }
            else if (query.Artist != null)
            {
                var lastName = query.Artist.RightPart(',');
                var firstName = lastName == query.Artist
                    ? null
                    : query.Artist.LeftPart(',');

                q.Join<Creative, CreativeArtist>((creative, artistRef) => creative.Id == artistRef.CreativeId)
                 .Join<CreativeArtist, Artist>((artistRef, artist) => artistRef.ArtistId == artist.Id && artist.FirstName == firstName && artist.LastName == lastName);
            }
            else if (query.Album != null)
            {
                q.Join<Artifact, AlbumArtifact>((artifact, albumRef) => artifact.Id == albumRef.ArtifactId)
                 .Join<AlbumArtifact, Album>((albumRef, album) => albumRef.AlbumId == album.Id && album.RefId == query.Album);
            }
            else if (query.User != null)
            {
                if (showUserLikes)
                {
                    q.Join<Creative>()
                     .Join<AppUser>((a,u) => u.RefIdStr == query.User)
                     .Join<Artifact,ArtifactLike,AppUser>((a, l, u) => a.Id == l.ArtifactId && u.Id == l.AppUserId);
                }
                else
                {
                    q.Where<Creative>(c => c.OwnerRef == query.User);
                }
            }

            q.ThenByDescending(x => x.Score + x.TemporalScore).ThenByDescending(x => x.Id);
            // Need distinct else Blazor @key throws when returning dupes
            q.SelectDistinct<Artifact, Creative>((a, c) => new { a, c.UserPrompt, c.ArtistNames, c.ModifierNames, c.PrimaryArtifactId, c.OwnerRef });
        }

        PublishMessage(new AnalyticsTasks {
            RecordSearchStat = new SearchStat {
                Query = query.Query,
                Similar = query.Similar,
                User = query.User,
                Modifier = query.Modifier,
                Artist = query.Artist,
                Album = query.Album,
                ArtifactId = similarToArtifact?.Id,
            }.WithRequest(Request, await GetSessionAsync()),
        });

        return AutoQuery.ExecuteAsync(query, q, base.Request, db);
    }

    public static List<Group> CategoryGroups = new Group[] {
        new() { Name = "Scene",     Items = new[] { "Quality", "Style", "Aesthetic", "Features", "Medium", "Setting", "Theme" } },
        new() { Name = "Effects",   Items = new[] { "Effects", "CGI", "Filters", "Lenses", "Photography", "Lighting", "Color" } },
        new() { Name = "Art Style", Items = new[] { "Art Movement", "Art Style", "18 Century", "19 Century", "20 Century", "21 Century" } },
        new() { Name = "Mood",      Items = new[] { "Positive Mood", "Negative Mood" } },
    }.ToList();

    public async Task<object> Any(SearchData request)
    {
        var to = new SearchDataResponse
        {
            CategoryGroups = CategoryGroups,
            Artists = (await Db.SelectAsync<Artist>()).OrderBy(x => x.Rank)
                .Select(x => new ArtistInfo { 
                    Id = x.Id, 
                    Name = x.FirstName != null ? $"{x.FirstName} {x.LastName}" : x.LastName,
                    Type = x.Type == null ? null : string.Join(", ", x.Type.Take(3)),
                }).ToList(),
            
            Modifiers = (await Db.SelectAsync<Modifier>()).OrderBy(x => x.Rank)
                .Select(x => new ModifierInfo { Id = x.Id, Name = x.Name, Category = x.Category }).ToList(),
        };
        return to;
    }

    async Task<UserResult> GetUserResultAsync(int userId)
    {
        var likes = new Likes
        {
            ArtifactIds = await Db.ColumnAsync<int>(Db.From<ArtifactLike>().Where(x => x.AppUserId == userId).Select(x => x.ArtifactId).OrderByDescending(x => x.Id)),
            AlbumIds = await Db.ColumnAsync<int>(Db.From<AlbumLike>().Where(x => x.AppUserId == userId).Select(x => x.AlbumId).OrderByDescending(x => x.Id)),
        };

        var albums = (await Db.LoadSelectAsync<Album>(x => x.OwnerId == userId && x.DeletedDate == null))
            .OrderByDescending(x => x.Artifacts.Max(x => x.Id)).ToList();
        var albumResults = albums.Map(x => x.ToAlbumResult());

        return new UserResult
        {
            Likes = likes,
            Albums = albumResults,
        };
    }

    public async Task<object> Any(UserData request)
    {
        var session = await SessionAsAsync<CustomUserSession>();
        var result = await GetUserResultAsync(session.UserAuthId.ToInt());

        return new UserDataResponse
        {
            RefId = session.RefIdStr,
            Roles = (await session.GetRolesAsync(AuthRepositoryAsync)).ToList(),
            Likes = result.Likes,
            Albums = result.Albums,
        };
    }

    public async Task<object> Any(GetUserInfo request)
    {
        var user = await Db.SingleAsync<AppUser>(x => x.RefIdStr == request.RefId);
        if (user == null)
            return HttpError.NotFound("User not found");

        var result = X.Apply(await GetUserResultAsync(user.Id), x => x.RefId = user.RefIdStr);
        return new GetUserInfoResponse
        {
            Result = result,
        };
    }

    public async Task<object> Any(QueryLikedArtifacts query)
    {
        var session = await GetSessionAsync();
        var userId = session.UserAuthId.ToInt();

        using var db = AutoQuery.GetDb(query, base.Request);
        var q = AutoQuery.CreateQuery(query, base.Request, db);
        q.Join<ArtifactLike>((a, l) => a.Id == l.ArtifactId && l.AppUserId == userId);
        if (query.OrderBy == null)
            q.OrderByDescending<ArtifactLike>(x => x.Id);

        return await AutoQuery.ExecuteAsync(query, q, base.Request, db);
    }

    public async Task<object> Any(GetAlbumResults request)
    {
        var albums = (await Db.LoadSelectAsync<Album>(x => x.DeletedDate == null && request.Ids.Contains(x.Id)))
            .OrderByDescending(x => x.Artifacts.Max(x => x.Id)).ToList();
        var albumResults = albums.Map(x => x.ToAlbumResult());

        return new GetAlbumResultsResponse
        {
            Results = albumResults,
        };
    }
}
