using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using BlazorDiffusion.UI;
using BlazorDiffusion.ServiceModel;
using Ljbc1994.Blazor.IntersectionObserver;

namespace BlazorDiffusion.Pages;

public partial class Favorites : AppAuthComponentBase, IDisposable
{
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IIntersectionObserverService ObserverService { get; set; } = default!;
    [Inject] IJSRuntime JS { get; set; }

    [Parameter] public int? Album { get; set; }
    [Parameter] public int? Id { get; set; }
    [Parameter] public int? View { get; set; }
    
    Artifact? SelectedArtifact { get; set; }

    public AlbumResult? SelectedAlbum => Album == null 
        ? null 
        : UserState.UserAlbums.FirstOrDefault(x => x.Id == Album.Value)
            ?? UserState.LikedAlbums.FirstOrDefault(x => x.Id == Album.Value);

    const string TextGray200 = "e7ebe5";
    const string TextGray700 = "374151";
    string SelectedColor => SelectedArtifact != null ? TextGray200 : TextGray700;

    public ElementReference BottomElement { get; set; }
    IntersectionObserver? bottomObserver;
    PageView? pageView;

    public List<Artifact> results { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        log("\n\n\nOnInitializedAsync() += UserState.OnChange, NavigationManager.LocationChanged");
        UserState.OnChange += StateHasChanged;
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        log("\n\n\n");
        await loadUserState();
        StateHasChanged();

        await handleParametersChanged();

        log("LoadAlbumCoverArtifacts()...");
        await UserState.LoadAlbumCoverArtifacts();
        StateHasChanged();

        log("LoadLikedAlbumsAsync()...");
        await UserState.LoadLikedAlbumsAsync();
    }

    async Task handleParametersChanged()
    {
        var query = ServiceStack.Pcl.HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query)!;
        int? asInt(string name) => X.Map(query[name], x => int.TryParse(x, out var num) ? num : (int?)null);

        Album = asInt(nameof(Album));
        Id = asInt(nameof(Id));
        View = asInt(nameof(View));
        pageView = null;

        if (Album != null)
        {
            if (SelectedAlbum == null)
            {
                log("resetting results...");
                results = new();
                navTo("/favorites"); // album no longer exists, e.g. after last image was deleted
                return;
            }

            log("loading album '{0}' results...", SelectedAlbum.Id);
            results = await UserState.GetAlbumArtifactsAsync(SelectedAlbum, UserState.InitialTake);
        }
        else
        {
            log("loading {0} artifact likes", UserState.InitialTake);
            results = await UserState.GetLikedArtifactsAsync(UserState.InitialTake);
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
        base.InvokeAsync(async () =>
        {
            await handleParametersChanged();
        });
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

    async Task openNewAlbum()
    {
        pageView = PageView.NewAlbum;
        await Task.Delay(1);
        await JS.InvokeVoidAsync("JS.elInvoke", "#Name", "focus");
    }

    async Task openEditProfile()
    {
        pageView = PageView.EditProfile;
        await Task.Delay(1);
        await JS.InvokeVoidAsync("JS.elInvoke", "#DisplayName", "focus");
    }

    async Task moveToTop(Artifact artifact)
    {
        if (SelectedAlbum!.PrimaryArtifactId == artifact.Id)
            return;

        var api = await ApiAsync(new UpdateAlbum { 
            Id = SelectedAlbum.Id, 
            RemoveArtifactIds = new() { artifact.Id },
            AddArtifactIds = new() { artifact.Id },
        });
        if (api.Succeeded)
        {
            await KeyboardNavigation.SendKeyAsync("Escape");
            var artifactResult = results.First(x => x.Id == artifact.Id);
            var primaryArtifact = SelectedAlbum.PrimaryArtifactId != null
                ? results.FirstOrDefault(x => x.Id == SelectedAlbum.PrimaryArtifactId.Value)
                : null;
            var newResults = results.Where(x => x.Id != artifact.Id && (primaryArtifact == null || x.Id != primaryArtifact.Id)).ToList();
            newResults.Insert(0, artifactResult);
            if (primaryArtifact != null)
                newResults.Insert(0, primaryArtifact);
            results = newResults;
            StateHasChanged();
        }
    }

    async Task OnDone()
    {
        pageView = null;
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
        log("Dispose() -= UserState.OnChange, NavigationManager.LocationChanged\n\n\n");
        UserState.OnChange -= StateHasChanged;
        NavigationManager.LocationChanged -= HandleLocationChanged;
        bottomObserver?.Dispose();
        bottomObserver = null;
    }
}
