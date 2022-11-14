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

public class GalleryResults
{
    public List<Artifact> Artifacts { get; set; } = new();
    public Artifact? Selected { get; set; }
    public Artifact? Viewing { get; set; }
    public Creative? Creative { get; set; }
    public AlbumResult[] CreativeAlbums { get; set; } = Array.Empty<AlbumResult>();

    public GalleryResults(List<Artifact>? artifacts = null)
    {
        Artifacts = artifacts ?? new();
    }

    public async Task<GalleryResults> LoadAsync(UserState userState, int? selectedId, int? viewingId)
    {
        if (selectedId != Selected?.Id || viewingId != Viewing?.Id)
        {
            Selected = await userState.GetArtifactAsync(selectedId);
            Viewing = await userState.GetArtifactAsync(viewingId);
            Creative = await userState.GetCreativeAsync(Selected?.CreativeId);
            CreativeAlbums = await userState.GetCreativeInAlbumsAsync(Selected?.CreativeId);
            if (CreativeAlbums.Length > 0)
                await userState.LoadArtifactsAsync(CreativeAlbums.Where(x => x.PrimaryArtifactId != null).Select(x => x.PrimaryArtifactId!.Value));
        }

        return this;
    }

    public GalleryResults Clone() => new GalleryResults
    {
        Artifacts = Artifacts,
        Selected = Selected,
        Viewing = Viewing,
        Creative = Creative,
        CreativeAlbums = CreativeAlbums,        
    };
}

public record struct GalleryChangeEventArgs(int? SelectedId, int? ViewingId)
{
    public override string ToString() => $"({SelectedId},{ViewingId})";
}

public partial class ArtifactGallery : AppAuthComponentBase, IDisposable
{
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;


    [Parameter] public RenderFragment? LeftHeader { get; set; }
    [Parameter] public RenderFragment? RightHeader { get; set; }
    [Parameter] public RenderFragment<Artifact>? TopRightIcon { get; set; }
    [Parameter] public RenderFragment<Artifact>? TopMenu { get; set; }
    [Parameter] public RenderFragment<Artifact>? BottomMenu { get; set; }
    [Parameter] public Func<Artifact, int?, UserState, string> ResolveBorderColor { get; set; } = ArtifactExtensions.GetBorderColor;
    [Parameter] public bool? LazyLoad { get; set; }
    [Parameter] public string ColumnsSliderClass { get; set; }
    [Parameter] public EventCallback<GalleryChangeEventArgs> Change { get; set; }

    //public SimpleSlideOver? SlideOver { get; set; }
    [Parameter] public GalleryResults Results { get; set; } = new();

    public List<Artifact> Artifacts => Results.Artifacts;
    public Artifact? Selected => Results.Selected;
    public Artifact? Viewing => Results.Viewing;
    Artifact? Active => Viewing ?? Selected;
    Creative? Creative => Results.Creative;
    AlbumResult[] CreativeAlbums => Results.CreativeAlbums;


    IEnumerable<AlbumResult> GetArtifactAlbums()
    {
        var artifactId = Viewing?.Id ?? Selected?.Id;
        return artifactId != null ? CreativeAlbums.Where(x => x.ArtifactIds.Contains(artifactId.Value)) : Array.Empty<AlbumResult>();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UserState.OnChange += UserStateChanged;
        log("Index OnInitializedAsync() += StateHasChanged");
    }

    class ParamsState
    {
        int? selected;
        int? viewing;
        public ParamsState(int? selected, int? viewing)
        {
            this.selected = selected;
            this.viewing = viewing;
        }
        public bool Matches(ParamsState a) => selected == a.selected && viewing == a.viewing;
        public override string ToString() => $"({selected},{viewing})";
    }
    ParamsState currentState() => new ParamsState(Selected?.Id, Viewing?.Id);

    void UserStateChanged()
    {
        log("ArtifactGallery UserStateChanged()");
        StateHasChanged();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        RegisterKeyboardNavigation(OnNavKeyAsync);

        log("ArtifactGallery OnParametersSetAsync{0}", currentState());
    }

    async Task navTo(int? artifactId = null, int? viewArtifactId = null)
    {
        //waitForState = new ParamsState(artifactId, viewArtifactId);
        log("ArtifactGallery navTo{0}", new ParamsState(artifactId, viewArtifactId));
        DeregisterKeyboardNavigation(OnNavKeyAsync);
        await Change.InvokeAsync(new(artifactId, viewArtifactId));
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
            await navTo();
            StateHasChanged();
        }
    }

    async Task CloseDialogsAsync()
    {
        await hideArtifactMenu();
        if (Selected?.Id != null)
            await navTo();
        StateHasChanged();
    }

    public async Task OnNavKeyAsync(string key)
    {
        if (key == KeyCodes.Escape)
        {
            await CloseDialogsAsync();
            return;
        }

        if (Selected == null)
        {
            if (key == KeyCodes.ArrowRight || key == KeyCodes.ArrowDown)
            {
                var artifact = Artifacts.FirstOrDefault();
                if (artifact != null)
                {
                    await navTo(artifact.Id);
                }
            }
            return;
        }

        if (key == KeyCodes.ArrowLeft || key == KeyCodes.ArrowRight)
        {
            var artifacts = Artifacts;
            var activeIndex = artifacts.FindIndex(x => x.Id == Selected.Id);
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
                await navTo(next.Id);
            }
        }
        else if (key == KeyCodes.ArrowUp || key == KeyCodes.ArrowDown)
        {
            if (Creative != null && Viewing != null)
            {
                var artifacts = Creative.GetArtifacts();
                var activeIndex = artifacts.FindIndex(x => x.Id == Viewing.Id);
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
                    await navTo(Selected.Id, next.Id);
                    return;
                }
            }
            if (Creative != null)
            {
                if (key == KeyCodes.ArrowDown)
                {
                    var artifact = Creative.GetArtifacts().FirstOrDefault();
                    if (artifact != null)
                    {
                        await navTo(Selected.Id, artifact.Id);
                        return;
                    }
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
        var state = new GalleryChangeEventArgs(Selected?.Id, Viewing?.Id);
        log("ArtifactGallery OnChange{0}", state);
        UserStateChanged();
        await Change.InvokeAsync(state);
    }

    public void Dispose()
    {
        UserState.OnChange -= UserStateChanged;
        DeregisterKeyboardNavigation(OnNavKeyAsync);
        log("Index Dispose() -= StateHasChanged");
    }
}
