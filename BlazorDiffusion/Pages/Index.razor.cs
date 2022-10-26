using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorDiffusion.Pages;

public partial class Index : AppAuthComponentBase
{
    ApiResult<QueryResponse<ArtifactResult>> api = new();
    [Inject] UserState UserState { get; set; } = default!;

    string[] VisibleFields => new[] { 
        nameof(SearchArtifacts.Query), 
    };

    [Parameter, SupplyParameterFromQuery] public string? q { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? user { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? similar { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? modifier { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? artist { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? album { get; set; }

    SearchArtifacts request = new();

    List<Artifact> results = new();
    HashSet<int> resultIds = new();


    SearchArtifacts? lastRequest;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        request.Query = q;
        request.User = user;
        request.Similar = similar;
        request.Modifier = modifier;
        request.Artist = artist;
        request.Album = album;

        if (IsAuthenticated)
        {
            await UserState.LoadUserDataAsync();
        }

        await updateAsync();
    }

    async Task updateAsync()
    {
        var existingQuery = lastRequest != null && lastRequest.Equals(request);
        if (existingQuery)
            return;

        api = await ApiAsync(request);
        if (api.Succeeded)
        {
            if (!existingQuery)
                clearResults();

            addResults(api.Response?.Results ?? new());

            lastRequest = request.Clone();
        }
    }

    void clearResults()
    {
        results.Clear();
        resultIds.Clear();
    }

    void addResults(List<ArtifactResult> artifacts)
    {
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

}
