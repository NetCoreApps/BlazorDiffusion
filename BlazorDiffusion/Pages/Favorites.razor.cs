using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using BlazorDiffusion.UI;
using BlazorDiffusion.ServiceModel;
using Ljbc1994.Blazor.IntersectionObserver;
using BlazorDiffusion.Shared;

namespace BlazorDiffusion.Pages;

public partial class Favorites : AppAuthComponentBase, IDisposable
{
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IIntersectionObserverService ObserverService { get; set; } = default!;
    [Inject] IJSRuntime JS { get; set; }
    [Inject] ILogger<Favorites> Log { get; set; } = default!;


    [Parameter, SupplyParameterFromQuery] public int? Album { get; set; }
    [Parameter, SupplyParameterFromQuery] public int? Id { get; set; }
    [Parameter, SupplyParameterFromQuery] public int? View { get; set; }

    Artifact? ActiveArtifact => GalleryResults.Viewing ?? GalleryResults.Selected;
    AlbumResult? SelectedAlbum { get; set; }
    GalleryResults GalleryResults = new();

    const string TextGray200 = "e7ebe5";
    const string TextGray700 = "374151";
    string SelectedColor => ActiveArtifact != null ? TextGray200 : TextGray700;

    public ElementReference BottomElement { get; set; }
    IntersectionObserver? bottomObserver;
    PageView? pageView;

    public List<Artifact> results { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        log("\n\n\n Favorites OnInitializedAsync() += UserState.OnChange");
        UserState.OnChange += UserStateChanged;
    }

    void UserStateChanged()
    {
        if (settingParams)
        {
            log("Favorites ignore UserStateChanged whilst setting params");
            return;
        }

        log("Favorites UserStateChanged()");
        StateHasChanged();
    }

    // When navigate + ArtifactMenu Adds/Removes to Albums
    async Task OnGalleryChange(GalleryChangeEventArgs args)
    {
        if (settingParams)
        {
            log("Favorites ignore onChange whilst setting params");
            return;
        }

        log("Favorites OnGalleryChange{0}", args);
        //await handleParametersChanged();

        //preemptive to hopefully reduce re-renders with invalid args
        await GalleryResults.LoadAsync(UserState, args.SelectedId, args.ViewingId);

        if (args.SelectedId == null && args.ViewingId == null)
        {
            NavigationManager.NavigateTo(NavigationManager.Uri.SetQueryParam("id", args.SelectedId?.ToString()));
        }
        else
        {
            NavigationManager.NavigateTo(NavigationManager.Uri
                .SetQueryParam("id", args.SelectedId?.ToString())
                .SetQueryParam("view", args.ViewingId?.ToString()));
        }
    }

    bool settingParams;
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        settingParams = true;
        log("\n\n\nFavorites OnParametersSetAsync()");
        await loadUserState();
        await handleParametersChanged();
        settingParams = false;
    }

    int counter = 0;
    async Task handleParametersChanged()
    {
        pageView = null;
        log("\n\n Favorites handleParametersChanged({0},{1},{2}) {3}", Album, Id, View, counter++);

        SelectedAlbum = Album == null ? null
            : UserState.UserAlbums.FirstOrDefault(x => x.Id == Album.Value) ??
              UserState.LikedAlbums.FirstOrDefault(x => x.Id == Album.Value);

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

        var galleryResults = new GalleryResults(results);
        GalleryResults = await galleryResults.LoadAsync(UserState, Id, View);
        StateHasChanged();

        log("LoadAlbumCoverArtifacts()...");
        await UserState.LoadAlbumCoverArtifacts();
        StateHasChanged();

        log("LoadLikedAlbumsAsync()...");
        await UserState.LoadLikedAlbumsAsync();
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
        log("Favorites navTo({0})", href);
        NavigationManager.NavigateTo(href);
    }

    public async Task SetupObserver()
    {
        try
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
        catch (Exception e)
        {
            // throws on initial load
            Log.LogError("Favorites ObserverService.Observe(BottomElement): {0}", e.ToString());
        }
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetupObserver();
        }
    }

    public void Dispose()
    {
        log("Dispose() -= UserState.OnChange\n\n\n");
        UserState.OnChange -= UserStateChanged;
        
        bottomObserver?.Dispose();
        bottomObserver = null;
    }
}
