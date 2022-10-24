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

    SearchArtifacts request = new();

    List<Artifact> results = new();
    HashSet<int> resultIds = new();

    string? lastSearch;
    string? lastUser;
    int? lastSkip;

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

        if (IsAuthenticated)
        {
            await UserState.LoadUserDataAsync();
        }

        await updateAsync();
    }

    async Task updateAsync()
    {
        var existingQuery = lastSearch == request.Query && lastUser == request.User;
        if (existingQuery && lastSkip == request.Skip)
            return;

        api = await ApiAsync(request);
        if (api.Succeeded)
        {
            if (!existingQuery)
                clearResults();

            addResults(api.Response?.Results ?? new());

            lastSearch = request.Query;
            lastUser = request.User;
            lastSkip = request.Skip;
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
