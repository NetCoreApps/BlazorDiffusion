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
    public string? Source { get; set; }

    public int? ArtifactId { get; set; }
    public int? AlbumId { get; set; }
    public int? AppUserId { get; set; }
    public int? ModifierId { get; set; }
    public int? ArtistId { get; set; }
}

public enum SignupType
{
    Updates,
    Beta,
}

[Icon(Svg = Icons.Signup)]
public class Signup : StatBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public SignupType Type { get; set; }
    public string Email { get; set; }
    public string? Name { get; set; }
    public DateTime? CancelledDate { get; set; }
}


public class CreateSignup : ICreateDb<Signup>, IReturn<EmptyResponse> // IReturnVoid -> support cast EmptyResponse -> byte[]
{
    public SignupType Type { get; set; }
    [ValidateNotEmpty, ValidateEmail]
    public string Email { get; set; }
    public string? Name { get; set; }
}


[ValidateHasRole(AppRoles.Moderator)]
public class QueryArtifactStats : QueryDb<ArtifactStat> { }

[ValidateHasRole(AppRoles.Moderator)]
public class QuerySearchStats : QueryDb<SearchStat> { }


[ValidateHasRole(AppRoles.Moderator)]
public class QuerySignups : QueryDb<Signup> { }

[ValidateHasRole(AppRoles.Moderator)]
public class UpdateSignup : IPatchDb<Signup>, IReturn<Signup>
{
    public int Id { get; set; }
    public SignupType? Type { get; set; }
    [ValidateEmail]
    public string? Email { get; set; }
    public string? Name { get; set; }
    public DateTime? CancelledDate { get; set; }
}

[ValidateHasRole(AppRoles.Moderator)]
public class DeleteSignup : IDeleteDb<Signup>, IReturnVoid
{
    public int Id { get; set; }
}
