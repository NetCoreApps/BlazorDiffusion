using System.IO;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace BlazorDiffusion.ServiceModel;

[Tag(Tag.Tasks)]
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
public class RecordPrimaryArtifact
{
    public int CreativeId { get; set; }
    public int? FromArtifactId { get; set; }
    public int? ToArtifactId { get; set; }
}

[Tag(Tag.Tasks)]
[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class AnalyticsTasks
{
    public SearchStat? RecordSearchStat { get; set; }
    public ArtifactStat? RecordArtifactStat { get; set; }
}

[Tag(Tag.Tasks)]
[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class SyncTasks : IReturn<SyncTasksResponse>
{
    public bool? Periodic { get; set; }
    public bool? Daily { get; set; }
}
public class SyncTasksResponse
{
    public List<string> Results { get; set; }
}

[Tag(Tag.Tasks)]
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
