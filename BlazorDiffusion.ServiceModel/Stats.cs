using ServiceStack;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorDiffusion.ServiceModel;

public class StatBase
{
    public string RefId { get; set; }
    [References(typeof(AppUser))]
    public int? AppUserId { get; set; }
    public string RawUrl { get; set; }
    public string RemoteIp { get; set; }
    public DateTime CreatedDate { get; set; }
}

public enum StatType
{
    Download,
}

[Icon(Svg = Icons.Stats)]
public class ArtifactStat : StatBase
{
    [AutoIncrement]
    public int Id { get; set; }

    public StatType Type { get; set; }
    public int ArtifactId { get; set; }
    public string Source { get; set; }
    public string Version { get; set; }
}

[Icon(Svg = Icons.Stats)]
public class SearchStat : StatBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public string? Query { get; set; }
    public string? Similar { get; set; }
    public string? User { get; set; }
    public string? Modifier { get; set; }
    public string? Artist { get; set; }
}


[ValidateHasRole(AppRoles.Moderator)]
public class QueryArtifactStats : QueryDb<ArtifactStat> { }

[ValidateHasRole(AppRoles.Moderator)]
public class QuerySearchStats : QueryDb<SearchStat> { }