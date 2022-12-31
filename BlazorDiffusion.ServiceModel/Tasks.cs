using System.IO;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace BlazorDiffusion.ServiceModel;


[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class BackgroundTasks
{
    public Creative? NewCreative { get; set; }
    public int? RecordArtifactLikeId { get; set; }
    public int? RecordArtifactUnlikeId { get; set; }

    public int? RecordAlbumLikeId { get; set; }
    public int? RecordAlbumUnlikeId { get; set; }

    public RecordPrimaryArtifact? RecordPrimaryArtifact { get; set; }

    public List<int>? ArtifactIdsAddedToAlbums { get; set; }
    public List<int>? ArtifactIdsRemovedFromAlbums { get; set; }
}

public class AnalyticsTasks
{
    public SearchStat? RecordSearchStat { get; set; }
    public ArtifactStat? RecordArtifactStat { get; set; }
}

public class RecordPrimaryArtifact
{
    public int CreativeId { get; set; }
    public int? FromArtifactId { get; set; }
    public int? ToArtifactId { get; set; }
}

[ExcludeMetadata]
public class SyncTasks : IReturn<SyncTasksResponse>
{
    public bool? Periodic { get; set; }
    public bool? Daily { get; set; }
}

public class SyncTasksResponse
{
    public List<string> Results { get; set; }
}

[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class DiskTasks : IReturnVoid
{
    public int? SaveCreativeId { get; set; }
    public Creative? SaveCreative { get; set; }
    public SaveFile? SaveFile { get; set; }
    public List<string>? CdnDeleteFiles { get; set; }
}

public class SaveFile
{
    public string FilePath { get; set; }
    public Stream Stream { get; set; }
}

[Route("/creative/metadata/{CreativeId}")]
[ValidateHasRole(AppRoles.Moderator)]
public class ViewCreativeMetadata : IGet, IReturn<Creative>
{
    [ValidateGreaterThan(0)]
    public int CreativeId { get; set; }
}

[ExcludeMetadata]
[ValidateHasRole(AppRoles.Moderator)]
[Route("/render/{Type}")]
public class RenderComponent : IGet, IReturn<string>
{
    public string Type { get; set; }
    public bool TestContext { get; set; }
}

[ExcludeMetadata]
[ValidateHasRole(AppRoles.Moderator)]
public class Prerender : IGet, IReturn<PrerenderResponse> { }

public class PrerenderResponse
{
    public List<string> Results { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[Route("/image/{Id}")]
[Route("/artifacts/{Group}/{Slug}")]
public class RenderArtifactHtml : IReturn<string>
{
    public int? Group { get; set; }
    public int? Id { get; set; }
    public string? Slug { get; set; }
}

public class TestImageHtml : IReturnVoid {}

public class PrerenderImages : IReturn<PrerenderResponse>
{
    public bool Force { get; set; }
    public int[] Batches { get; set; }
}

[ExcludeMetadata]
[ValidateHasRole(AppRoles.Moderator)]
public class DevTasks : IGet, IReturn<StringResponse>
{
    public bool? DisableWrites { get; set; }
}
