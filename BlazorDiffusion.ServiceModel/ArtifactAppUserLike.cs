using ServiceStack;
using ServiceStack.DataAnnotations;

namespace BlazorDiffusion.ServiceModel;

public class ArtifactAppUserLike : AuditBase
{
    [AutoIncrement]
    public long Id { get; set; }
        
    [References(typeof(CreativeArtifact))]
    public int CreativeArtifactId { get; set; }
    [References(typeof(AppUser))]
    public int AppUserId { get; set; }
}

[AutoPopulate(nameof(AppUserId), Eval = "userAuthId")]
[ValidateIsAuthenticated]
public class QueryArtifactAppUserLike : QueryDb<ArtifactAppUserLike>
{
    public int AppUserId { get; set; }
}

[AutoPopulate(nameof(AppUserId), Eval = "userAuthId")]
[ValidateIsAuthenticated]
public class CreateArtifactAppUserLike : ICreateDb<ArtifactAppUserLike>, IReturn<ArtifactAppUserLike>
{
    public int CreativeArtifactId { get; set; }
    public int AppUserId { get; set; }
}

public class DeleteArtifactAppUserLike : IDeleteDb<ArtifactAppUserLike>, IReturnVoid
{
    public long Id { get; set; }
}