using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.Shared;
using BlazorDiffusion.UI;
using Ljbc1994.Blazor.IntersectionObserver;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ServiceStack.Blazor;
using ServiceStack.Text;

namespace BlazorDiffusion.Pages;

public partial class Index : AppAuthComponentBase, IDisposable
{
    ApiResult<QueryResponse<ArtifactResult>> api = new();
    [Inject] ILogger<Index> Log { get; set; } = default!;
    [Inject] IIntersectionObserverService ObserverService { get; set; } = default!;
    [Inject] IJSRuntime JS { get; set; } = default!;

    string[] VisibleFields => new[] { 
        nameof(SearchArtifacts.Query), 
    };

    [Parameter, SupplyParameterFromQuery] public string? q { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? user { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? similar { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? by { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? show { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? modifier { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? artist { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? album { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? source { get; set; }
    [Parameter, SupplyParameterFromQuery] public int? Id { get; set; }
    [Parameter, SupplyParameterFromQuery] public int? View { get; set; }

    GalleryResults GalleryResults = new(); // composite params to hopefully reduce params

    int? lastId = null;
    int? lastView = null;

    SearchArtifacts request = new();

    List<Artifact> results = new();
    List<ArtifactResult>? lastResults = null;
    HashSet<int> resultIds = new();

    const int InitialTake = 30;
    const int NextPageTake = 100;

    SearchArtifacts? lastRequest;

    UserResult? SelectedUser;
    public AlbumResult? SelectedAlbum;

    public ElementReference BottomElement { get; set; }
    IntersectionObserver? bottomObserver;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        request.Query = q;
        request.User = user;
        request.Show = show;
        request.Similar = similar;
        request.By = by;
        request.Modifier = modifier;
        request.Artist = artist;
        request.Album = album;
        request.Source = source;
        request.Skip = 0;
        request.Take = InitialTake;
        await loadUserState();

        await updateAsync();
    }

    int i = 0;

    async Task updateAsync()
    {
        var existingQuery = lastRequest != null && lastRequest.Matches(request);
        var existingSelection = lastId == Id && lastView == View;
        if (existingQuery && existingSelection)
            return;
        lastId = Id;
        lastView = View;

        if (lastRequest != null)
        {
            var dirtyFields = request.GetDirtyFields(lastRequest);
            log("\n\n\n\nDirty Fields:", string.Join(", ", dirtyFields));
        }
        else
        {
            log("Loading new request...");
        }

        Log.LogDebug($"\n\n{0}", request.Dump());
        if (!existingQuery)
        {
            request.Skip = 0;
            api = await ApiAsync(request);
            if (api.Succeeded)
            {
                addResults(api.Response?.Results ?? new(), reset:true);
                lastRequest = request.Clone();
            }
        }

        var galleryResults = new GalleryResults(results);
        GalleryResults = await galleryResults.LoadAsync(UserState, Id, View);
        StateHasChanged();

        SelectedUser = user != null
            ? await UserState.GetUserByRefIdAsync(user)
            : null;

        SelectedAlbum = SelectedUser != null && album != null
            ? SelectedUser.Albums.FirstOrDefault(x => x.AlbumRef == album)
            : album != null
                ? await UserState.GetAlbumByRefAsync(album)
                : null;

        if (!existingQuery && request.Take == InitialTake)
        {
            request.Skip += InitialTake;
            request.Take = NextPageTake;

            api = await ApiAsync(request);
            addResults(api.Response?.Results ?? new());
            lastRequest = request.Clone();
        }
    }

    async Task loadMore()
    {
        Log.LogDebug($"Index loadMore() {0} >= {1} / {2}...", lastResults?.Count, request.Take, results.Count);
        if (lastResults == null || lastResults?.Count >= request.Take)
        {
            request.Skip += NextPageTake;
            Log.LogDebug(request.Dump());
            api = await ApiAsync(request);
            if (api.Succeeded)
            {
                addResults(api.Response?.Results ?? new());
                lastRequest = request.Clone();
            }
        }
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
            });
        }
        catch (Exception e)
        {
            // throws on initial load
            Log.LogError("Index ObserverService.Observe(BottomElement): {0}", e.ToString());
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetupObserver();
        }
    }

    void clearResults()
    {
        lastRequest = null;
        results.Clear();
        resultIds.Clear();
        lastResults = null;
        log("\n{0} CLEAR RESULTS: {1}", i++, results.Count);
    }

    void addResults(List<ArtifactResult> artifacts, bool reset = false)
    {
        if (reset)
            clearResults();

        lastResults = artifacts;
        foreach (var artifact in artifacts)
        {
            if (resultIds.Contains(artifact.Id))
                continue;
            
            resultIds.Add(artifact.Id);
            results.Add(artifact);
        }
        log("\n{0} RESULTS: {1}", i++, results.Count);
        StateHasChanged();
    }

    async Task OnKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "enter")
        {
            await submit();
        }
    }

    async Task submit()
    {
        NavigationManager.NavigateTo("/".AddQueryParam("q", request.Query));
    }

    // When navigate + ArtifactMenu Adds/Removes to Albums
    async Task OnGalleryChange(GalleryChangeEventArgs args)
    {
        log("Index OnGalleryChange{0}", args);
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

    public void Dispose()
    {
        bottomObserver?.Dispose();
        bottomObserver = null;
    }
}
