using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using ServiceStack;
using ServiceStack.Blazor;
using ServiceStack.Blazor.Components.Tailwind;
using System;

namespace BlazorDiffusion.Shared;

public partial class ArtifactGallery : AppAuthComponentBase
{
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] KeyboardNavigation KeyboardNavigation { get; set; } = default!;
    [Inject] public UserState UserState { get; set; } = default!;


    [Parameter] public List<Artifact> Artifacts { get; set; } = new();
    [Parameter] public int? Id { get; set; }
    [Parameter] public int? View { get; set; }

    string columns = "6";

    public SlideOver? SlideOver { get; set; }

    Creative? creative;
    Artifact? artifact;
    Artifact? viewingArtifact => View == null || creative == null ? null : creative.Artifacts.FirstOrDefault(x => x.Id == View);

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UserState.OnChange += StateHasChanged;
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        KeyboardNavigation.Register(OnNavKeyAsync);

        var query = ServiceStack.Pcl.HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query);
        Id = query[nameof(Id)]?.ConvertTo<int>();
        View = query[nameof(View)]?.ConvertTo<int>();

        artifact = await UserState.GetArtifactAsync(Id);
        creative = await UserState.GetCreativeAsync(artifact?.CreativeId);

        StateHasChanged();
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        base.InvokeAsync(async () => await OnParametersSetAsync());
    }

    void navTo(int? artifactId = null, int? viewArtifactId = null)
    {
        if (artifactId == null && viewArtifactId == null)
        {
            NavigationManager.NavigateTo(NavigationManager.Uri.SetQueryParam("id", artifactId?.ToString()));
        }
        else
        {
            NavigationManager.NavigateTo(NavigationManager.Uri.SetQueryParam("id", artifactId?.ToString()).SetQueryParam("view", viewArtifactId?.ToString()));
        }
    }

    async Task hardDelete(int creativeId)
    {
        var api = await UserState.HardDeleteCreativeByIdAsync(creativeId);
        if (api.Succeeded)
        {
            Artifacts.RemoveAll(x => x.CreativeId == creativeId);
            navTo();
            StateHasChanged();
        }
    }

    async Task CloseDialogsAsync()
    {
        if (Id != null)
            navTo();
    }


    public async Task OnNavKeyAsync(string key)
    {
        if (key == KeyCodes.Escape)
        {
            await CloseDialogsAsync();
            return;
        }

        if (Id == null)
        {
            if (key == KeyCodes.ArrowRight || key == KeyCodes.ArrowDown)
            {
                var artifact = Artifacts.FirstOrDefault();
                if (artifact != null)
                {
                    navTo(artifact.Id);
                }
            }
            return;
        }

        if (key == KeyCodes.ArrowLeft || key == KeyCodes.ArrowRight)
        {
            var artifacts = Artifacts;
            var activeIndex = artifacts.FindIndex(x => x.Id == Id);
            if (activeIndex >= 0)
            {
                var nextIndex = key switch
                {
                    KeyCodes.ArrowLeft => activeIndex - 1,
                    KeyCodes.ArrowRight => activeIndex + 1,
                    _ => 0
                };
                if (nextIndex < 0)
                {
                    nextIndex = artifacts.Count - 1;
                }
                var next = artifacts[nextIndex % artifacts.Count];
                navTo(next.Id);
            }
        }
        else if (key == KeyCodes.ArrowUp || key == KeyCodes.ArrowDown)
        {
            if (View == null && creative != null)
            {
                if (key == KeyCodes.ArrowDown)
                {
                    var artifact = creative.GetArtifacts().FirstOrDefault();
                    if (artifact != null)
                    {
                        navTo(Id, artifact.Id);
                    }
                }
                return;
            }

            if (creative != null && View != null)
            {
                var artifacts = creative.GetArtifacts();
                var activeIndex = artifacts.FindIndex(x => x.Id == View);
                if (activeIndex >= 0)
                {
                    var nextIndex = key switch
                    {
                        KeyCodes.ArrowUp => activeIndex - 1,
                        KeyCodes.ArrowDown => activeIndex + 1,
                        _ => 0
                    };
                    if (nextIndex < 0)
                    {
                        nextIndex = artifacts.Count - 1;
                    }
                    var next = artifacts[nextIndex % artifacts.Count];
                    navTo(Id, next.Id);
                }
            }
        }

    }

    public void Dispose()
    {
        UserState.OnChange -= StateHasChanged;
        NavigationManager.LocationChanged -= HandleLocationChanged;
        KeyboardNavigation.Deregister(OnNavKeyAsync);
    }
}
