using Amazon.Runtime.Internal;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorDiffusion.ServiceInterface;

public class AdminServices : Service
{
    public async Task<object> Any(AdminData request)
    {
        var tables = new (string Label, Type Type)[]
        {
            ("Albums",               typeof(Album)),
            ("AlbumArtifacts",       typeof(AlbumArtifact)),
            ("AlbumLikes",           typeof(AlbumLike)),
            ("Artifacts",            typeof(Artifact)),
            ("ArtifactLikes",        typeof(ArtifactLike)),
            ("ArtifactComments",     typeof(ArtifactComment)),
            ("ArtifactCommentVotes", typeof(ArtifactCommentVote)),
            ("Artists",              typeof(Artist)),
            ("Modifiers",            typeof(Modifier)),
            ("Creatives",            typeof(Creative)),
            ("CreativeArtists",      typeof(CreativeArtist)),
            ("CreativeModifiers",    typeof(CreativeModifier)),
        };
        var analyticsTables = new (string Label, Type Type)[] 
        {
            ("ArtifactStats", typeof(ArtifactStat)),
            ("SearchStats",   typeof(SearchStat)),
            ("Signups",       typeof(Signup)),
        };

        var dialect = Db.GetDialectProvider();

        var totalSql = tables.Map(x => $"SELECT '{x.Label}', COUNT(*) FROM {dialect.GetQuotedTableName(x.Type.GetModelMetadata())}")
            .Join(" UNION ");
        var results = await Db.DictionaryAsync<string, int>(totalSql);

        var analyticsTotalSql = analyticsTables.Map(x => $"SELECT '{x.Label}', COUNT(*) FROM {dialect.GetQuotedTableName(x.Type.GetModelMetadata())}")
            .Join(" UNION ");
        var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
        foreach (var entry in await analyticsDb.DictionaryAsync<string, int>(analyticsTotalSql))
        {
            results[entry.Key] = entry.Value;
        }

        return new AdminDataResponse
        {
            PageStats = tables.Union(analyticsTables).Map(x => new PageStats
            {
                Label = x.Label,
                Total = results[x.Label],
            })
        };
    }

}
