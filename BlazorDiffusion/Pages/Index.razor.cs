using BlazorDiffusion.ServiceModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorDiffusion.Pages;

public partial class Index : AppComponentBase
{
    ApiResult<QueryResponse<ArtifactResult>> api = new();

    [Parameter, SupplyParameterFromQuery] public string q { get; set; }

    SearchArtifacts request = new();

    List<Artifact> results = new();
    HashSet<int> resultIds = new();

    string lastSearch = "";
    int lastSkip = 0;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        request.Query = q;
        await updateAsync();
    }

    async Task updateAsync()
    {
        var existingQuery = lastSearch == request.Query;
        if (existingQuery && lastSkip == request.Skip)
            return;

        api = await ApiAsync(request);
        if (api.Succeeded)
        {
            if (!existingQuery)
                clearResults();

            addResults(api.Response?.Results ?? new());
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
