using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.UI;
using Microsoft.AspNetCore.Components;
using ServiceStack.Blazor;

namespace BlazorDiffusion.Pages;


public partial class Likes : AppAuthComponentBase
{
    [Inject] UserState UserState { get; set; } = default!;
    ApiResult<QueryResponse<Artifact>> api = new();

    async Task loadUserState()
    {
        if (IsAuthenticated)
        {
            await UserState.LoadUserDataAsync();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await loadUserState();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UserState.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        UserState.OnChange -= StateHasChanged;
    }
}
