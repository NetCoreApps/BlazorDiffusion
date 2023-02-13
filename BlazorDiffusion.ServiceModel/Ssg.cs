using System.Collections.Generic;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace BlazorDiffusion.ServiceModel;

[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class DeleteArtifactHtml
{
    public int[] Ids { get; set; }
}

[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class DeleteCdnFilesMq
{
    public List<string> Files { get; set; }
}

[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class GetCdnFile
{
    public string File { get; set; }
}

[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class DeleteCdnFile : IReturnVoid
{
    public string File { get; set; }
}

[ExcludeMetadata]
[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
[Route("/render/{Type}")]
public class RenderComponent : IGet, IReturn<string>
{
    public string Type { get; set; }
    public bool TestContext { get; set; }
}

[ExcludeMetadata]
[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class Prerender : IGet, IReturn<PrerenderResponse> { }
public class PrerenderResponse
{
    public List<string> Results { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[Tag(Tag.Ssg)]
[Route("/image/{Id}")]
[Route("/artifacts/{Group}/{Slug}")]
public class RenderArtifactHtml : IReturn<string>
{
    public int? Group { get; set; }
    public int? Id { get; set; }
    public string? Slug { get; set; }
    public bool? Save { get; set; }
}

[Tag(Tag.Ssg)]
public class TestImageHtml : IReturnVoid {}

[ExcludeMetadata]
[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class PrerenderImages : IReturn<PrerenderResponse>
{
    public bool Force { get; set; }
    public int[] Batches { get; set; }
}

[ExcludeMetadata]
[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class PrerenderSitemap : IReturn<PrerenderResponse>
{
}

[ExcludeMetadata]
[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class DevTasks : IGet, IReturn<StringResponse>
{
    public bool? DisableWrites { get; set; }
}
