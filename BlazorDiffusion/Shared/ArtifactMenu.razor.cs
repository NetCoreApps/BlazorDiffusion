using BlazorDiffusion.ServiceModel;
using BlazorDiffusion.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ServiceStack.Blazor;

namespace BlazorDiffusion.Shared;

public partial class ArtifactMenu : AppAuthComponentBase
{
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public UserState UserState { get; set; }
    [Inject] IJSRuntime JS { get; set; }
    [Parameter, EditorRequired] public Artifact Artifact { get; set; } = default!;
    [Parameter, EditorRequired] public MouseEventArgs Position { get; set; } = default!;
    [Parameter] public RenderFragment<Artifact>? TopMenu { get; set; }
    [Parameter] public RenderFragment<Artifact>? BottomMenu { get; set; }
    [Parameter] public int OffsetX { get; set; } = 60;
    [Parameter] public int OffsetY { get; set; } = 60;
    [Parameter] public bool Show { get; set; }
    [Parameter] public EventCallback Done { get; set; }
    [Parameter] public EventCallback Change { get; set; }

    PageView? artifactView;

    async Task<bool> assertAuth()
    {
        if (!IsAuthenticated)
        {
            await OnDone();
            NavigationManager.NavigateTo(NavigationManager.GetLoginUrl(), true);
            return false;
        }
        return true;
    }

    async Task toggleNsfw()
    {
        if (!IsModerator)
            return;

        var api = await ApiAsync(new UpdateArtifact
        {
            Id = Artifact.Id,
            Nsfw = !Artifact.Nsfw.GetValueOrDefault(),
        });
        if (api.Succeeded)
        {
            Artifact.Nsfw = !Artifact.Nsfw;
            await OnDone();
            await OnChange();
        }
    }

    async Task setQuality(int quality)
    {
        if (!IsModerator)
            return;

        var api = await ApiAsync(new UpdateArtifact
        {
            Id = Artifact.Id,
            Quality = quality,
        });
        if (api.Succeeded)
        {
            Artifact.Quality = quality;
            await OnDone();
            await OnChange();
        }
    }

    async Task findSimilarByPerceptual()
    {
        NavigationManager.NavigateTo($"/?similar={Artifact.RefId}&by=perceptual");
        await OnDone();
    }

    async Task findSimilarByAvg()
    {
        NavigationManager.NavigateTo($"/?similar={Artifact.RefId}&by=avg");
        await OnDone();
    }

    async Task findSimilarByDiff()
    {
        NavigationManager.NavigateTo($"/?similar={Artifact.RefId}&by=diff");
        await OnDone();
    }

    async Task openReport()
    {
        if (!await assertAuth())
            return;

        artifactView = PageView.Report;
        await Task.Delay(1);
        await JS.InvokeVoidAsync("JS.elInvoke", "#Type", "focus");
    }

    async Task openNewAlbum()
    {
        if (!await assertAuth())
            return;

        artifactView = PageView.NewAlbum;
        await Task.Delay(1);
        await JS.InvokeVoidAsync("JS.elInvoke", "#Name", "focus");
    }

    async Task saveToAlbum(AlbumResult album)
    {
        if (!album.ArtifactIds.Contains(Artifact.Id))
        {
            var request = new UpdateAlbum
            {
                Id = album.Id,
                AddArtifactIds = new() { Artifact.Id },
            };
            var api = await ApiAsync(request);
            if (api.Succeeded)
            {
                UserState.AddArtifactToAlbum(album, Artifact);
                await OnChange();
                await OnDone();
            }
        }
        else
        {
            var request = new UpdateAlbum
            {
                Id = album.Id,
                RemoveArtifactIds = new() { Artifact.Id },
            };
            var api = await ApiAsync(request);
            if (api.Succeeded)
            {
                UserState.RemoveArtifactFromAlbum(album, Artifact);
                await OnChange();
                await OnDone();
            }
        }
    }

    async Task OnDone()
    {
        artifactView = null;
        await Done.InvokeAsync();
    }

    async Task OnChange()
    {
        await Change.InvokeAsync();
    }

}
