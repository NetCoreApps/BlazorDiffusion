using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.UI;
using Ljbc1994.Blazor.IntersectionObserver;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ServiceStack.Text;

namespace BlazorDiffusion.Pages;

public partial class Index : AppAuthComponentBase, IDisposable
{
    ApiResult<QueryResponse<ArtifactResult>> api = new();
    [Inject] ILogger<Index> Log { get; set; } = default!;
    [Inject] IIntersectionObserverService ObserverService { get; set; } = default!;

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
        request.Skip = 0;
        request.Take = InitialTake;
        await loadUserState();

        await updateAsync();
    }

    async Task updateAsync()
    {
        var existingQuery = lastRequest != null && lastRequest.Equals(request);
        if (existingQuery)
            return;

        Log.LogDebug($"\n\n{0}", request.Dump());
        api = await ApiAsync(request);
        if (api.Succeeded)
        {
            clearResults();

            addResults(api.Response?.Results ?? new());
            lastRequest = request.Clone();
        }
        StateHasChanged();
        SelectedUser = user != null
            ? await UserState.GetUserByRefIdAsync(user)
            : null;

        SelectedAlbum = SelectedUser != null && album != null
            ? SelectedUser.Albums.FirstOrDefault(x => x.AlbumRef == album)
            : await UserState.GetAlbumByRefAsync(album);

        await Task.Delay(1);
        if (request.Take == InitialTake)
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
    }

    void addResults(List<ArtifactResult> artifacts)
    {
        lastResults = artifacts;
        foreach (var artifact in artifacts)
        {
            if (resultIds.Contains(artifact.Id))
                continue;
            
            resultIds.Add(artifact.Id);
            results.Add(artifact);
        }
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

    public void Dispose()
    {
        bottomObserver?.Dispose();
    }
}
