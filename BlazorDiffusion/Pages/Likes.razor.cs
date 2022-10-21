using BlazorDiffusion.ServiceModel;
using Microsoft.AspNetCore.Components;
using ServiceStack.Blazor;

namespace BlazorDiffusion.Pages;


public partial class Likes : AppAuthComponentBase
{
    [Inject] UserState UserState { get; set; } = default!;
    ApiResult<QueryResponse<Artifact>> api = new();

    async Task loadUserState()
    {
        if (User != null)
        {
            await UserState.LoadLikesAsync(User.GetUserId().ToInt());
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await loadUserState();
        api = await ApiAsync(new QueryLikedArtifacts());
    }

    void OnStateChange()
    {
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UserState.OnChange += OnStateChange;
    }

    public void Dispose()
    {
        UserState.OnChange -= OnStateChange;
    }
}
