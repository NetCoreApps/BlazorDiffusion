using ServiceStack;
using ServiceStack.DataAnnotations;
using System;

namespace BlazorDiffusion.ServiceModel;

public class ArtifactComment : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public int ArtifactId { get; set; }
    public int? ReplyId { get; set; }
    public string Content { get; set; }
    public int VoteUpCount { get; set; }
    public int VoteDownCount { get; set; }
    public string? FlagReason { get; set; }
    public string? Notes { get; set; }
    public string RefId { get; set; }
    public int AppUserId { get; set; }
}

public class CommentResult
{
    public int Id { get; set; }
    public int ArtifactId { get; set; }
    public int? ReplyId { get; set; }
    public string Content { get; set; }
    public int VoteUpCount { get; set; }
    public int VoteDownCount { get; set; }
    public string? FlagReason { get; set; }
    public string? Notes { get; set; }
    public int AppUserId { get; set; }
    public string DisplayName { get; set; }
    public string? Handle { get; set; }
    public string? ProfileUrl { get; set; }
    public string? Avatar { get; set; } //overrides ProfileUrl
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

[AutoApply(Behavior.AuditQuery)]
public class QueryArtifactComments : QueryDb<ArtifactComment, CommentResult>,
    IJoin<ArtifactComment,AppUser>
{
    public int ArtifactId { get; set; }
} 

[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditCreate)]
public class CreateArtifactComment : ICreateDb<ArtifactComment>, IReturn<ArtifactComment>
{
    public int ArtifactId { get; set; }
    public int? ReplyId { get; set; }
    public string Content { get; set; }
}

[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditModify)]
public class UpdateArtifactComment : IPatchDb<ArtifactComment>, IReturn<ArtifactComment>
{
    public int Id { get; set; }
    public string? Content { get; set; }
    public string? FlagReason { get; set; }
}

[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditSoftDelete)]
[AutoFilter(QueryTerm.Ensure, nameof(ArtifactComment.AppUserId), Eval = "userAuthId.toInt()")]
public class DeleteArtifactComment : IDeleteDb<ArtifactComment>, IReturnVoid
{
    public int Id { get; set; }
}

