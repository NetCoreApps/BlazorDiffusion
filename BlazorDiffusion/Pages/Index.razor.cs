using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.Shared;
using BlazorDiffusion.UI;
using Ljbc1994.Blazor.IntersectionObserver;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ServiceStack.Blazor;
using ServiceStack.Text;
using System;

namespace BlazorDiffusion.Pages;

public partial class Index : AppAuthComponentBase, IDisposable
{
    ApiResult<QueryResponse<ArtifactResult>> api = new();
    [Inject] ILogger<Index> Log { get; set; } = default!;
    [Inject] IIntersectionObserverService ObserverService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; }
    [Inject] IJSRuntime JS { get; set; }

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
    [Parameter, SupplyParameterFromQuery] public bool? LazyLoad { get; set; }

    GalleryResults GalleryResults = new(); // composite params to hopefully reduce params

    int? lastId = null;
    int? lastView = null;


    SearchArtifacts request = new();

    List<Artifact> results = new();
    HashSet<int> resultIds = new();
    bool hasMore;

    const int InitialTake = 30;
    const int NextPageTake = 100;

    SearchArtifacts? lastRequest;
    bool existingQuery => lastRequest != null && lastRequest.Matches(request);

    UserResult? SelectedUser;
    public AlbumResult? SelectedAlbum;

    public ElementReference BottomElement { get; set; }
    IntersectionObserver? bottomObserver;

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
        var existingSelection = lastId == Id && lastView == View;
        if (existingQuery && existingSelection)
        {
            log("Ignoring existingQuery && existingSelection");
            return;
        }
        lastId = Id;
        lastView = View;

        if (lastRequest != null)
            log("Dirty Fields:", string.Join(", ", request.GetDirtyFields(lastRequest)));
        else
            log("Loading new request...");

        await loadResults();
        await GalleryResults.LoadAsync(UserState, Id, View);
        StateHasChanged();

        SelectedUser = user != null
            ? await UserState.GetUserByRefIdAsync(user)
            : null;

        SelectedAlbum = SelectedUser != null && album != null
            ? SelectedUser.Albums.FirstOrDefault(x => x.AlbumRef == album)
            : album != null
                ? await UserState.GetAlbumByRefAsync(album)
                : null;
        UserState.RemovePrerenderedHtml();
    }

    void setResults(IEnumerable<Artifact> results)
    {
        this.results = results.ToList();
        GalleryResults = X.Apply(GalleryResults.Clone(), x => x.Artifacts = this.results);
        StateHasChanged();
    }

    async Task loadResults()
    {
        if (existingQuery)
            return;

        request.Skip = 0;
        request.Take = InitialTake;
        api = await ApiAsync(request);
        clearResults();
        if (api.Succeeded)
        {
            if (api.Response?.Results != null)
            {
                addResults(api.Response.Results);
            }
            lastRequest = request.Clone();
        }
    }

    async Task loadMore()
    {
        Log.LogDebug("{0} Index loadMore({1}) [{2}..{3}] {4} -> [{5}..{6}]", i++, 
            hasMore, request.Skip, request.Take, results.Count, request.Skip + request.Take, NextPageTake);
        if (hasMore)
        {
            request.Skip += request.Take;
            request.Take = NextPageTake;
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
        log("\n{0} CLEAR RESULTS: {1}", i++, results.Count);
    }

    void addResults(List<ArtifactResult> artifacts, bool reset = false)
    {
        if (reset)
            clearResults();

        hasMore = artifacts.Count >= request.Take;
        foreach (var artifact in artifacts)
        {
            if (resultIds.Contains(artifact.Id))
                continue;
            
            resultIds.Add(artifact.Id);
            results.Add(artifact);
        }
        log("\n{0} RESULTS: {1}, more: {2}", i++, results.Count, hasMore);
        setResults(results);
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
        await loadResults();

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
