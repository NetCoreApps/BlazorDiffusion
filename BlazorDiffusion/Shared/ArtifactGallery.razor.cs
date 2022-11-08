using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using ServiceStack;
using ServiceStack.Blazor;
using ServiceStack.Blazor.Components.Tailwind;
using System;
using System.Linq;

namespace BlazorDiffusion.Shared;

public partial class ArtifactGallery : AppAuthComponentBase, IDisposable
{
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] KeyboardNavigation KeyboardNavigation { get; set; } = default!;
    [Inject] public UserState UserState { get; set; } = default!;


    [Parameter] public List<Artifact> Artifacts { get; set; } = new();
    [Parameter] public RenderFragment? LeftHeader { get; set; }
    [Parameter] public RenderFragment? RightHeader { get; set; }
    [Parameter] public RenderFragment<Artifact>? TopRightIcon { get; set; }
    [Parameter] public RenderFragment<Artifact>? TopMenu { get; set; }
    [Parameter] public RenderFragment<Artifact>? BottomMenu { get; set; }
    [Parameter] public int? Id { get; set; }
    [Parameter] public int? View { get; set; }
    [Parameter] public Func<Artifact, int?, UserState, string> ResolveBorderColor { get; set; } = ArtifactExtensions.GetBorderColor;
    [Parameter] public string ColumnsSliderClass { get; set; }
    [Parameter] public EventCallback Change { get; set; }

    public SlideOver? SlideOver { get; set; }

    Creative? creative;
    Artifact? artifact;
    Artifact? viewingArtifact => View == null || creative == null ? null : creative.Artifacts.FirstOrDefault(x => x.Id == View);

    AlbumResult[] creativeAlbums = Array.Empty<AlbumResult>();

    IEnumerable<AlbumResult> GetArtifactAlbums()
    {
        var artifactId = viewingArtifact?.Id ?? artifact?.Id;
        return artifactId != null ? creativeAlbums.Where(x => x.ArtifactIds.Contains(artifactId.Value)) : Array.Empty<AlbumResult>();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UserState.OnChange += StateHasChanged;
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        RegisterKeyboardNavigation(OnNavKeyAsync);

        await handleParametersChanged();
    }

    public async Task handleParametersChanged()
    {
        var query = ServiceStack.Pcl.HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query);
        Id = query[nameof(Id)]?.ConvertTo<int>();
        View = query[nameof(View)]?.ConvertTo<int>();

        artifact = await UserState.GetArtifactAsync(Id);
        creative = await UserState.GetCreativeAsync(artifact?.CreativeId);
        creativeAlbums = creative != null
            ? await UserState.GetCreativeInAlbumsAsync(creative.Id)
            : Array.Empty<AlbumResult>();
        if (creativeAlbums.Length > 0)
            await UserState.LoadArtifactsAsync(creativeAlbums.Where(x => x.PrimaryArtifactId != null).Select(x => x.PrimaryArtifactId!.Value));

        StateHasChanged();
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        base.InvokeAsync(async () => await handleParametersChanged());
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

    async Task LikeArtifactAsync(Artifact artifact)
    {
        await UserState.LikeArtifactAsync(artifact);
        await OnChange();
    }

    async Task UnlikeArtifactAsync(Artifact artifact)
    {
        await UserState.UnlikeArtifactAsync(artifact);
        await OnChange();
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
        await hideArtifactMenu();
        if (Id != null)
            navTo();
        StateHasChanged();
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

    public Task exploreSimilar(Artifact artifact)
    {
        NavigationManager.NavigateTo($"/?similar={artifact.RefId}");
        return Task.CompletedTask;
    }

    const int DefaultArtifactOffsetX = 60;
    Artifact? artifactMenu;
    MouseEventArgs? artifactMenuArgs;   
    int artifactOffsetX = DefaultArtifactOffsetX;

    public async Task hideArtifactMenu()
    {
        artifactMenu = null;
        artifactMenuArgs = null;
        artifactOffsetX = DefaultArtifactOffsetX;
    }

    public async Task showArtifactMenu(MouseEventArgs e, Artifact artifact, int offsetX = DefaultArtifactOffsetX)
    {
        artifactMenuArgs = e;
        artifactMenu = artifact;
        artifactOffsetX = offsetX;
    }

    async Task OnChange()
    {
        StateHasChanged();
        await Change.InvokeAsync();
    }

    public void Dispose()
    {
        UserState.OnChange -= StateHasChanged;
        NavigationManager.LocationChanged -= HandleLocationChanged;
        DeregisterKeyboardNavigation(OnNavKeyAsync);
    }
}
