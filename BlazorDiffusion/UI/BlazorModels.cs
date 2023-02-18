using BlazorDiffusion.ServiceModel;

namespace BlazorDiffusion.UI;

public class GalleryResults
{
    public List<Artifact> Artifacts { get; set; } = new();
    public Artifact? Selected { get; set; }
    public Artifact? Viewing { get; set; }
    public Creative? Creative { get; set; }
    public AlbumResult[] CreativeAlbums { get; set; } = Array.Empty<AlbumResult>();
    public int? GridColumns { get; set; }

    public GalleryResults(List<Artifact>? artifacts = null)
    {
        Artifacts = artifacts ?? new();
    }

    public async Task<GalleryResults> LoadAsync(UserState userState, int? selectedId, int? viewingId)
    {
        if (selectedId != Selected?.Id || viewingId != Viewing?.Id)
        {
            Selected = await userState.GetArtifactAsync(selectedId);
            Viewing = await userState.GetArtifactAsync(viewingId);
            Creative = await userState.GetCreativeAsync(Selected?.CreativeId);
            CreativeAlbums = await userState.GetCreativeInAlbumsAsync(Selected?.CreativeId);
            if (CreativeAlbums.Length > 0)
                await userState.LoadArtifactsAsync(CreativeAlbums.Where(x => x.PrimaryArtifactId != null).Select(x => x.PrimaryArtifactId!.Value));
        }

        return this;
    }

    public GalleryResults Clone() => new GalleryResults
    {
        Artifacts = Artifacts,
        Selected = Selected,
        Viewing = Viewing,
        Creative = Creative,
        CreativeAlbums = CreativeAlbums,
        GridColumns = GridColumns,
    };
}
