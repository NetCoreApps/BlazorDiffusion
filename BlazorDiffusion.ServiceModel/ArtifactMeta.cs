using ServiceStack;
using ServiceStack.DataAnnotations;
using System;

namespace BlazorDiffusion.ServiceModel;

public class ArtifactLike
{
    [AutoIncrement]
    public long Id { get; set; }
        
    [References(typeof(Artifact))]
    public int ArtifactId { get; set; }
    [References(typeof(AppUser))]
    public int AppUserId { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ArtifactLikeRef
{
    public string RefId { get; set; }
    public int ArtifactId { get; set; }
    public int AppUserId { get; set; }
    public DateTime CreatedDate { get; set; }
}


[ValidateIsAuthenticated]
public class QueryLikedArtifacts : QueryDb<Artifact> {}


[AutoFilter(QueryTerm.Ensure, nameof(ArtifactLike.AppUserId), Eval = "userAuthId")]
[ValidateIsAuthenticated]
public class QueryArtifactLikes : QueryDb<ArtifactLike>
{
}

[ValidateIsAuthenticated]
public class CreateArtifactLike : ICreateDb<ArtifactLike>, IReturn<ArtifactLike>
{
    [ValidateGreaterThan(0)]
    public int ArtifactId { get; set; }
}

[AutoPopulate(nameof(ArtifactLike.AppUserId), Eval = "userAuthId")]
[ValidateIsAuthenticated]
public class DeleteArtifactLike : IDeleteDb<ArtifactLike>, IReturnVoid
{
    [ValidateGreaterThan(0)]
    public int ArtifactId { get; set; }
}

[Icon(Svg = Icons.Report)]
public class ArtifactReport
{
    [AutoIncrement]
    public long Id { get; set; }

    [References(typeof(Artifact))]
    public int ArtifactId { get; set; }
    [References(typeof(AppUser))]
    public int AppUserId { get; set; }

    public ReportType Type { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? Notes { get; set; }
    public DateTime? ActionedDate { get; set; }
    public string? ActionedBy { get; set; }
}
public enum ReportType
{
    Nsfw,
    Other,
}

[ValidateIsAuthenticated]
[ValidateIsAdmin]
public class QueryArtifactReports : QueryDb<ArtifactReport>
{
    public int? ArtifactId { get; set; }
}

[AutoPopulate(nameof(ArtifactReport.AppUserId), Eval = "userAuthId")]
[AutoPopulate(nameof(ArtifactReport.CreatedDate), Eval = "utcNow")]
[ValidateIsAuthenticated]
public class CreateArtifactReport : ICreateDb<ArtifactReport>, IReturn<ArtifactReport>
{
    [ValidateGreaterThan(0)]
    public int ArtifactId { get; set; }
    public ReportType Type { get; set; }
    public string? Description { get; set; }
}

[AutoPopulate(nameof(ArtifactReport.AppUserId), Eval = "userAuthId")]
[ValidateIsAdmin]
public class UpdateArtifactReport : IPatchDb<ArtifactReport>, IReturn<ArtifactReport>
{
    [ValidateGreaterThan(0)]
    public int ArtifactId { get; set; }
    public ReportType? Type { get; set; }
    public string? Description { get; set; }
}

[ValidateIsAuthenticated]
[ValidateIsAdmin]
public class DeleteArtifactReport : IDeleteDb<ArtifactReport>, IReturnVoid
{
    [ValidateGreaterThan(0)]
    public int ArtifactId { get; set; }
}
