using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.UI;
using Ljbc1994.Blazor.IntersectionObserver;
using Microsoft.AspNetCore.Components;
using ServiceStack.Blazor;

namespace BlazorDiffusion.Pages;

public partial class Favorites : AppAuthComponentBase
{
    [Inject] IIntersectionObserverService ObserverService { get; set; } = default!;
    public ElementReference BottomElement { get; set; }
    IntersectionObserver? bottomObserver;

    public List<Artifact> results { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UserState.OnChange += StateHasChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await loadUserState();

        results = await UserState.GetLikedArtifactsAsync(UserState.InitialTake);
        StateHasChanged();
        
        await Task.Delay(1);
        results = await UserState.GetLikedArtifactsAsync(results.Count + UserState.InitialTake);
        StateHasChanged();
    }

    async Task loadMore()
    {
        log("Favorites loadMore(): {0} < {1}", results.Count, UserState.LikedArtifactIds.Count);
        if (results.Count < UserState.LikedArtifactIds.Count)
        {
            results = await UserState.GetLikedArtifactsAsync(results.Count + UserState.InitialTake);
            StateHasChanged();
        }
    }

    static string GetBorderColor(Artifact artifact, int? activeId, UserState userState)
    {
        var borderColor = artifact.GetBorderColor(activeId, userState);
        return borderColor != "border-red-700"
            ? borderColor
            : "border-black";
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

    public void Dispose()
    {
        UserState.OnChange -= StateHasChanged;
        bottomObserver?.Dispose();
    }
}
