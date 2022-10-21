using BlazorDiffusion.ServiceModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ServiceStack;
using ServiceStack.Blazor;
using ServiceStack.Blazor.Components.Tailwind;
using ServiceStack.Blazor.Components;
using ServiceStack.Text;
using ServiceStack.Web;
using BlazorDiffusion.UI;

namespace BlazorDiffusion.Pages;

public partial class Create : AppAuthComponentBase
{
    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] public KeyboardNavigation KeyboardNavigation { get; set; }
    [Inject] public IJSRuntime JS { get; set; }
    [Inject] public UserState UserState { get; set; }

    static SearchDataResponse? DataCache;

    string[] VisibleFields => new[] {
        nameof(CreateCreative.UserPrompt),
        nameof(CreateCreative.Images),
        nameof(CreateCreative.Width),
        nameof(CreateCreative.Height),
    };

    [Parameter, SupplyParameterFromQuery] public int? Id { get; set; }
    [Parameter, SupplyParameterFromQuery] public int? View { get; set; }

    public SlideOver? SlideOver { get; set; }


    ImageSize imageSize;
    enum ImageSize
    {
        Square,
        Portrait,
        Landscape,
    }

    enum CreateMenu
    {
        History,
    }
    CreateMenu? createMenu = CreateMenu.History;
    void toggleMenu(CreateMenu menu) => createMenu = menu == createMenu ? null : menu;
    void closeMenu() => createMenu = null;


    void selectImageSize(ImageSize size) => imageSize = size;

    CreateCreative request = new();
    ApiResult<Creative> api = new();

    List<ArtistInfo>? ArtistOptions => DataCache?.Artists;
    List<ArtistInfo> artists = new();

    void removeArtist(ArtistInfo artist) => artists.Remove(artist);

    string[]? categoryNames;
    string[] CategoryNames => categoryNames ??= DataCache == null ? Array.Empty<string>()
        : DataCache.Modifiers.Select(x => x.Category).Distinct().OrderBy(x => x).ToArray();

    List<ModifierInfo>? ModifierOptions => DataCache?.Modifiers;


    List<ModifierInfo> modifiers = new();

    string? selectedGroup;
    string? selectedCategory;

    string[] groupCategories => DataCache?.CategoryGroups.FirstOrDefault(x => x.Name == selectedGroup)?.Items ?? Array.Empty<string>();

    List<ModifierInfo> categoryModifiers => (selectedCategory != null
        ? DataCache?.Modifiers.Where(x => x.Category == selectedCategory && !modifiers.Contains(x)).ToList()
        : null) ?? new();

    void addModifier(ModifierInfo modifier) => modifiers.Add(modifier);
    void removeModifier(ModifierInfo modifier) => modifiers.Remove(modifier);

    void selectGroup(string group)
    {
        selectedGroup = group;
        selectedCategory = DataCache?.CategoryGroups.FirstOrDefault(x => x.Name == selectedGroup)?.Items.FirstNonDefault();
    }

    void selectCategory(string category)
    {
        selectedCategory = category;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UserState.OnChange += StateHasChanged;

        if (DataCache == null)
        {
            DataCache = await Client!.SendAsync(new SearchData());
        }
        if (selectedGroup == null)
            selectGroup(DataCache.CategoryGroups[0].Name);
    }

    Creative? creative;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await loadUserState();
        KeyboardNavigation.Register(this.OnNavKeyAsync);

        if (Id != null)
        {
            creative = UserState.CreativeHistory.FirstOrDefault(x => x.Id == Id);
            
            if (creative == null)
            {
                var api = await ApiAsync(new QueryCreatives { Id = Id });
                creative = api.Response?.Results.FirstOrDefault();
            }
            if (creative != null)
            {
                request.UserPrompt = creative.UserPrompt;
                imageSize = creative.Height == 768
                    ? ImageSize.Portrait
                    : creative.Width == 768
                        ? ImageSize.Landscape
                        : ImageSize.Square;

                var artistIds = creative.Artists?.Select(x => x.ArtistId).ToSet() ?? new();
                artists = artistIds.Count > 0
                    ? ArtistOptions!.Where(x => artistIds.Contains(x.Id)).ToList()
                    : new();

                var modifierIds = creative.Modifiers?.Select(x => x.ModifierId).ToSet() ?? new();
                modifiers = modifierIds.Count > 0
                    ? ModifierOptions!.Where(x => modifierIds.Contains(x.Id)).ToList()
                    : new();
            }
        }
    }

    bool isDirty => !string.IsNullOrEmpty(request.UserPrompt)
        || imageSize != ImageSize.Square
        || artists.Count > 0
        || modifiers.Count > 0;

    void reset()
    {
        request.UserPrompt = "";
        imageSize = ImageSize.Square;
        artists.Clear();
        modifiers.Clear();
        StateHasChanged();
    }


    async Task loadUserState()
    {
        if (User != null)
        {
            await UserState.LoadAsync(User.GetUserId().ToInt());
        }
    }

    void noop() {}

    async Task submit()
    {
        request.Width = imageSize switch
        {
            ImageSize.Portrait => 512,
            ImageSize.Landscape => 768,
            _ => 512,
        };
        request.Height = imageSize switch
        {
            ImageSize.Portrait => 768,
            ImageSize.Landscape => 512,
            _ => 512,
        };
        request.ArtistIds = artists.Select(x => x.Id).ToList();
        request.ModifierIds = modifiers.Select(x => x.Id).ToList();

        api.ClearErrors();
        api.IsLoading = true;
        api = await ApiAsync(request);
        creative = api.Response;

        await loadUserState();
    }

    async Task pinArtifact(Artifact artifact)
    {
        var hold = creative!.PrimaryArtifactId;
        creative.PrimaryArtifactId = artifact.Id;

        var api = await ApiAsync(new UpdateCreative { Id = artifact.CreativeId, PrimaryArtifactId = artifact.Id });
        if (!api.Succeeded)
        {
            creative.PrimaryArtifactId = hold;
        }
        StateHasChanged();
    }

    async Task unpinArtifact(Artifact artifact)
    {
        var hold = creative!.PrimaryArtifactId;
        creative.PrimaryArtifactId = null;

        var api = await ApiAsync(new UpdateCreative { 
            Id = artifact.CreativeId, 
            PrimaryArtifactId = artifact.Id,
            UnpinPrimaryArtifact = true,
        });
        if (!api.Succeeded)
        {
            creative.PrimaryArtifactId = hold;
        }
        StateHasChanged();
    }

    async Task softDelete()
    {
        if (creative == null) return;
        var api = await ApiAsync(new DeleteCreative
        {
            Id = creative.Id,
        });
        if (api.Succeeded)
        {
            this.creative = null;
            navTo();
        }
    }

    async Task hardDelete()
    {
        if (creative == null) return;
        var api = await ApiAsync(new HardDeleteCreative
        {
            Id = creative.Id,
        });
        if (api.Succeeded)
        {
            this.creative = null;
            navTo();
        }
    }

    void navTo(int? creativeId = null, int? artifactId = null)
    {
        if (creativeId == null)
        {
            NavigationManager.NavigateTo("/create");
            return;
        }

        var url = artifactId != null
            ? $"/create?id={creativeId}&view={artifactId}"
            : $"/create?id={creativeId}";
        NavigationManager.NavigateTo(url);
    }

    async Task CloseDialogsAsync()
    {
        if (View != null)
            navTo(Id);
    }

    public async Task OnNavKeyAsync(string key)
    {
        if (key == KeyCodes.Escape)
        {
            await CloseDialogsAsync();
            return;
        }

        var results = UserState.CreativeHistory;
        if (Id != null && results?.Count > 0)
        {
            switch (key)
            {
                case KeyCodes.ArrowUp:
                case KeyCodes.ArrowDown:
                case KeyCodes.Home:
                case KeyCodes.End:
                    var activeIndex = results.FindIndex(x => x.Id == Id);
                    if (activeIndex >= 0)
                    {
                        var nextIndex = key switch
                        {
                            KeyCodes.ArrowUp => activeIndex - 1,
                            KeyCodes.ArrowDown => activeIndex + 1,
                            KeyCodes.Home => 0,
                            KeyCodes.End => results.Count - 1,
                            _ => 0
                        };
                        if (nextIndex < 0)
                        {
                            nextIndex = results.Count - 1;
                        }
                        var next = results[nextIndex % results.Count];
                        navTo(next.Id);
                        return;
                    }
                    break;

                case KeyCodes.ArrowLeft:
                case KeyCodes.ArrowRight:
                    if (creative != null)
                    {
                        var artifacts = creative.GetArtifacts();
                        if (View == null)
                        {
                            if (key == KeyCodes.ArrowRight)
                            {
                                var artifact = artifacts.FirstOrDefault();
                                if (artifact != null)
                                {
                                    navTo(Id, artifact.Id);
                                }
                            }
                        }
                        else
                        {
                            activeIndex = artifacts.FindIndex(x => x.Id == View);
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
                                navTo(Id, next.Id);
                            }
                        }
                        return;
                    }
                    break;
            }
        }
    }

    public void Dispose()
    {
        UserState.OnChange -= StateHasChanged;
        KeyboardNavigation.Deregister(this.OnNavKeyAsync);
    }
}
