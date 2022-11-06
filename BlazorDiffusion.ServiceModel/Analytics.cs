using ServiceStack;
using ServiceStack.DataAnnotations;
using System;

namespace BlazorDiffusion.ServiceModel;

[NamedConnection(Databases.Analytics)]
public class StatBase
{
    public string RefId { get; set; }
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
    public string? Album { get; set; }
    public string? Show { get; set; }

    public int? ArtifactId { get; set; }
    public int? AlbumId { get; set; }
    public int? AppUserId { get; set; }
    public int? ModifierId { get; set; }
    public int? ArtistId { get; set; }
}


[ValidateHasRole(AppRoles.Moderator)]
public class QueryArtifactStats : QueryDb<ArtifactStat> { }

[ValidateHasRole(AppRoles.Moderator)]
public class QuerySearchStats : QueryDb<SearchStat> { }
