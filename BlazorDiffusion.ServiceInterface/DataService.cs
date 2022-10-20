using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using ServiceStack;
using ServiceStack.OrmLite;
using BlazorDiffusion.ServiceModel;
using CoenM.ImageHash;
using Microsoft.Data.Sqlite;

namespace BlazorDiffusion.ServiceInterface;

public class DataService : Service
{
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

    public async Task<object> Any(FindSimilarArtifacts request)
    {
        var artifact = await Db.SingleAsync<CreativeArtifact>(request.CreativeArtifactId);
        if (artifact == null)
            throw HttpError.NotFound($"Artifact ID {request.CreativeArtifactId} not found.");
        var perceptualHash = artifact.PerceptualHash;
        if (perceptualHash == null)
            // TODO just in time hash of request based image?
            throw HttpError.BadRequest($"Artifact ID {artifact.Id} not hashed.");
        
        var connection = (SqliteConnection)Db.ToDbConnection();
        connection.CreateFunction(
            "imgcompare",
            (Int64 hash1, Int64 hash2)
                => CompareHash.Similarity((ulong)hash1,(ulong)hash2));

        var skip = request.Skip ?? 0;
        var take = request.Take ?? 50;

        var matches = await Db.SelectAsync<ImageCompareResult>($@"
select rowid, PerceptualHash, imgcompare({perceptualHash},PerceptualHash) as Similarity from CreativeArtifact
where Similarity > 70 and PerceptualHash != {perceptualHash}
order by Similarity desc limit {take} offset {skip};
");

        var results = await Db.SelectAsync<CreativeArtifact>(x => Sql.In(x.Id, matches.Select(y => y.Id)));
        return new FindSimilarArtifactsResponse
        {
            Results = results
        };
    }
}
