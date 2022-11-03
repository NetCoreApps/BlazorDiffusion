using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.UI;
using Ljbc1994.Blazor.IntersectionObserver;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using ServiceStack.Blazor;

namespace BlazorDiffusion.Pages;

public partial class Favorites : AppAuthComponentBase
{
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IIntersectionObserverService ObserverService { get; set; } = default!;

    [Parameter] public int? Album { get; set; }
    [Parameter] public int? Id { get; set; }
    [Parameter] public int? View { get; set; }
    
    Artifact? SelectedArtifact { get; set; }

    public AlbumResult? SelectedAlbum => Album == null ? null : UserState.UserAlbums.FirstOrDefault(x => x.Id == Album.Value);

    const string TextGray200 = "e7ebe5";
    const string TextGray700 = "374151";
    string SelectedColor => SelectedArtifact != null ? TextGray200 : TextGray700;

    public ElementReference BottomElement { get; set; }
    IntersectionObserver? bottomObserver;
    ArtifactView? artifactView;

    public List<Artifact> results { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UserState.OnChange += StateHasChanged;
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await loadUserState();
        await UserState.LoadAlbumCoverArtifacts();
        await handleParametersChanged();
    }

    async Task loadLikes()
    {
        results = await UserState.GetLikedArtifactsAsync(UserState.InitialTake);
        StateHasChanged();

        await Task.Delay(1);
        results = await UserState.GetLikedArtifactsAsync(results.Count + UserState.InitialTake);
        StateHasChanged();
    }

    async Task handleParametersChanged()
    {
        var query = ServiceStack.Pcl.HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query);
        Album = query[nameof(Album)]?.ConvertTo<int>();
        Id = query[nameof(Id)]?.ConvertTo<int>();
        View = query[nameof(View)]?.ConvertTo<int>();
        artifactView = null;

        if (Album != null)
        {
            if (SelectedAlbum == null)
            {
                results = new();
                navTo("/favorites"); // album no longer exists, e.g. after last image was deleted
                return;
            }
            results = await UserState.GetAlbumArtifactsAsync(SelectedAlbum, UserState.InitialTake);
        }
        else
        {
            await loadLikes();
        }

        SelectedArtifact = View != null
            ? UserState.GetCachedArtifact(View)
            : Id != null
                ? UserState.GetCachedArtifact(Id)
                : null;

        StateHasChanged();
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        base.InvokeAsync(async () => await handleParametersChanged());
    }

    async Task loadMore()
    {
        if (SelectedAlbum == null)
        {
            log("Favorites likes.loadMore(): {0} < {1}", results.Count, UserState.LikedArtifactIds.Count);
            if (results.Count < UserState.LikedArtifactIds.Count)
            {
                results = await UserState.GetLikedArtifactsAsync(results.Count + UserState.InitialTake);
            }
        }
        else
        {
            log("Favorites album.loadMore(): {0} < {1}", results.Count, SelectedAlbum.ArtifactIds.Count);
            if (results.Count < SelectedAlbum.ArtifactIds.Count)
            {
                results = await UserState.GetAlbumArtifactsAsync(SelectedAlbum, results.Count + UserState.InitialTake);
            }
        }
        StateHasChanged();
    }

    string GetBorderColor(Artifact artifact, int? activeId, UserState userState)
    {
        if (SelectedAlbum == null)
        {
            return artifact.Id == activeId
                ? "border-cyan-500"
                    : userState.IsModerator() && artifact.IsModerated()
                        ? "border-gray-500"
                        : userState.HasArtifactInAlbum(artifact)
                          ? "border-green-700"
                          : artifact.Background != null ? "border-black" : "border-transparent";
        }
        else
        {
            return artifact.Id == SelectedAlbum.PrimaryArtifactId
                ? "border-yellow-500"
                : artifact.Id == activeId
                ? "border-cyan-500"
                : userState.IsModerator() && artifact.IsModerated()
                    ? "border-gray-500"
                    : userState.HasArtifactInAlbum(artifact)
                        ? "border-black"
                        : artifact.Background != null ? "border-black" : "border-transparent";
        }
    }

    void navTo(string href)
    {
        NavigationManager.NavigateTo(href);
    }

    public async Task SetupObserver()
    {
        bottomObserver = await ObserverService.Observe(BottomElement, async (entries) =>
        {
            var entry = entries.FirstOrDefault();
            if (entry?.IsIntersecting == true)
            {
                await loadMore();
            }
            StateHasChanged();
        });
    }

    public Artifact? GetAlbumCover(AlbumResult album) => UserState.GetAlbumCoverArtifact(album);

    List<Artifact> ResultsWithPrimaryArtifact(int primaryArtifactId)
    {
        var to = new List<Artifact> { results.First(x => x.Id == primaryArtifactId) };
        to.AddRange(results.Where(x => x.Id != primaryArtifactId));
        return to;
    }

    async Task pinArtifact(Artifact artifact)
    {
        var hold = SelectedAlbum!.PrimaryArtifactId;
        SelectedAlbum.PrimaryArtifactId = artifact.Id;
        
        var holdResults = results;
        results = ResultsWithPrimaryArtifact(artifact.Id);
        StateHasChanged();

        var api = await ApiAsync(new UpdateAlbum { Id = SelectedAlbum.Id, PrimaryArtifactId = artifact.Id });
        if (!api.Succeeded)
        {
            SelectedAlbum.PrimaryArtifactId = hold;
            results = holdResults;
        }
        StateHasChanged();
    }

    async Task unpinArtifact(Artifact artifact)
    {
        var hold = SelectedAlbum!.PrimaryArtifactId;
        SelectedAlbum.PrimaryArtifactId = null;
        StateHasChanged();

        var api = await ApiAsync(new UpdateAlbum
        {
            Id = SelectedAlbum.Id,
            PrimaryArtifactId = artifact.Id,
            UnpinPrimaryArtifact = true,
        });
        if (!api.Succeeded)
        {
            SelectedAlbum.PrimaryArtifactId = hold;
        }
        StateHasChanged();
    }


    async Task OnDone()
    {
        artifactView = null;
    }

    // When ArtifactMenu Adds/Removes to Albums
    async Task OnChange()
    {
        await handleParametersChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetupObserver();
        }
    }

    public void Dispose()
    {
        UserState.OnChange -= StateHasChanged;
        NavigationManager.LocationChanged -= HandleLocationChanged;
        bottomObserver?.Dispose();
    }
}
