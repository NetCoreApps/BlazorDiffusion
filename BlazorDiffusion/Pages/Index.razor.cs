﻿using BlazorDiffusion.ServiceModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorDiffusion.Pages;

public partial class Index : AppComponentBase
{
    ApiResult<QueryResponse<ArtifactResult>> api = new();

    string[] VisibleFields => new[] { 
        nameof(SearchArtifacts.Query), 
    };

    [Parameter, SupplyParameterFromQuery] public string q { get; set; }
    [Parameter, SupplyParameterFromQuery] public int? user { get; set; }

    SearchArtifacts request = new();

    List<Artifact> results = new();
    HashSet<int> resultIds = new();

    string lastSearch = "";
    int? lastUser;
    int? lastSkip;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        request.Query = q;
        request.User = user;

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
