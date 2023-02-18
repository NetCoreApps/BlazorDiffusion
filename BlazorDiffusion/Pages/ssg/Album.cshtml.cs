using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BlazorDiffusion.UI;
using BlazorDiffusion.ServiceModel;

namespace BlazorDiffusion.Pages.ssg;

public class AlbumModel : PageModel, IGetPageModel
{
    [FromQuery] public string? RefId { get; set; }
    [FromQuery] public new int? Page { get; set; }

    public int UsePage;
    public int Total;
    public int Pages;
    public const int GridColumns = 4;
    public AlbumResult? SelectedAlbum = null;
    public GalleryResults GalleryResults = new GalleryResults { GridColumns = GridColumns };
    public List<AlbumRef> OtherAlbums = new();
    public Artifact? CoverImage = null;
    public string? Title;

    public IServiceGateway? Gateway = null;
    SearchArtifacts request = new();
    List<Artifact> results = new();
    HashSet<int> resultIds = new();

    public async Task OnGetAsync()
    {
        UsePage = Math.Max(Page ?? 1, 1);
        Gateway ??= HostContext.AppHost.GetServiceGateway();

        ApiResult<QueryResponse<ArtifactResult>> api = new();
        GalleryResults = new GalleryResults { GridColumns = GridColumns };

        if (RefId != null)
        {
            request.Album = RefId;
            request.Skip = (UsePage - 1) * UserState.StaticPagedTake;
            request.Take = UserState.StaticPagedTake;
            request.Include = "Total";
            api = await Gateway.ApiAsync(request);
            clearResults();

            if (api.Succeeded)
            {
                if (api.Response?.Results != null)
                {
                    addResults(api.Response.Results);
                }
            }

            var apiAlbum = await Gateway.ApiAsync(new GetAlbumResults { RefIds = new() { RefId } });
            if (apiAlbum.Succeeded)
            {
                SelectedAlbum = apiAlbum.Response?.Results.FirstOrDefault();
                if (SelectedAlbum != null)
                {
                    Title = SelectedAlbum.Name + (UsePage > 1 ? $" Page {UsePage}" : "");
                }
            }

            var apiAlbums = await Gateway.ApiAsync(new GetAlbumRefs());
            if (apiAlbums.Succeeded)
            {
                OtherAlbums = apiAlbums.Response!.Results;
            }
        }

        Total = api.Response?.Total ?? 0;
        Pages = (int)Math.Ceiling(Total / (double)UserState.StaticPagedTake);
        CoverImage = SelectedAlbum != null ? results.FirstOrDefault(x => x.Id == SelectedAlbum.PrimaryArtifactId) : null;
    }

    void setResults(IEnumerable<Artifact> newResults)
    {
        results = newResults.ToList();
        GalleryResults = X.Apply(GalleryResults.Clone(), x => x.Artifacts = results.ShuffleGridArtifacts(GridColumns).ToList());
    }

    void clearResults()
    {
        results.Clear();
        resultIds.Clear();
    }

    void addResults(List<ArtifactResult> artifacts, bool reset = false)
    {
        if (reset)
            clearResults();

        //var hasMore = artifacts.Count >= request.Take;
        foreach (var artifact in artifacts)
        {
            if (resultIds.Contains(artifact.Id))
                continue;

            resultIds.Add(artifact.Id);
            results.Add(artifact);
        }
        setResults(results);
    }
}
