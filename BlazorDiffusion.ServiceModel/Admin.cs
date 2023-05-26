using ServiceStack;
using System.Collections.Generic;

namespace BlazorDiffusion.ServiceModel;
/*Admin APIs*/

[Tag(Tag.Admin)]
[ValidateIsAdmin]
public class AdminQueryArtifactComments : QueryDb<ArtifactComment> { }

[Tag(Tag.Admin)]
[ValidateIsAdmin]
[AutoApply(Behavior.AuditModify)]
public class AdminUpdateArtifactComment : IPatchDb<ArtifactComment>, IReturn<ArtifactComment>
{
    public int Id { get; set; }
    public int? ReplyId { get; set; }
    [ValidateLength(1, 280)]
    public string? Content { get; set; }
    public string? Notes { get; set; }
    [Input(Type = "select", EvalAllowableValues = "AppData.FlagReasons")]
    public string? FlagReason { get; set; }
}

[Tag(Tag.Admin)]
[ValidateIsAdmin]
public class AdminDeleteArtifactComment : IDeleteDb<ArtifactComment>, IReturnVoid
{
    public int Id { get; set; }
}

[Tag(Tag.Admin)]
[ValidateIsAdmin]
public class AdminQueryArtifactCommentReports : QueryDb<ArtifactCommentReport> { }

[Tag(Tag.Admin)]
[ValidateIsAdmin]
public class AdminUpdateArtifactCommentReport : IPatchDb<ArtifactCommentReport>, IReturn<ArtifactCommentReport>
{
    public int Id { get; set; }
    public PostReport? PostReport { get; set; }
    public string? Description { get; set; }
}

[Tag(Tag.Admin)]
[ValidateIsAdmin]
public class AdminDeleteArtifactCommentReport : IDeleteDb<ArtifactCommentReport>, IReturnVoid
{
    public int Id { get; set; }
}


[Tag(Tag.Admin)]
[ValidateIsAdmin]
public class AdminData : IGet, IReturn<AdminDataResponse> { }

public class PageStats
{
    public string Label { get; set; }
    public int Total { get; set; }
}

public class AdminDataResponse
{
    public List<PageStats> PageStats { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}
