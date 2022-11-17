using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ServiceStack;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.Blazor;
using ServiceStack.Blazor.Components;
using ServiceStack.Blazor.Components.Tailwind;
using BlazorDiffusion.UI;
using BlazorDiffusion.ServiceModel;

namespace BlazorDiffusion.Pages;

public partial class Create : AppAuthComponentBase, IDisposable
{
    [Inject] public NavigationManager NavigationManager { get; set; }
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
    public List<Creative> CreativeHistory { get; set; } = new();


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
    ApiResult<CreateCreativeResponse> api = new();

    string[] SignupVisibleFields => new[] {
        nameof(CreateSignup.Email),
    };

    CreateSignup signup = new();
    ApiResult<EmptyResponse> apiSignup = new();

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

    string[] groupCategories => AppData.CategoryGroups.FirstOrDefault(x => x.Name == selectedGroup)?.Items ?? Array.Empty<string>();

    List<ModifierInfo> categoryModifiers => (selectedCategory != null
        ? DataCache?.Modifiers.Where(x => x.Category == selectedCategory && !modifiers.Contains(x)).ToList()
        : null) ?? new();

    void addModifier(ModifierInfo modifier) => modifiers.Add(modifier);
    void removeModifier(ModifierInfo modifier) => modifiers.Remove(modifier);

    void selectGroup(string group)
    {
        selectedGroup = group;
        selectedCategory = AppData.CategoryGroups.FirstOrDefault(x => x.Name == selectedGroup)
            ?.Items.First(x => !string.IsNullOrEmpty(x));
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
            selectGroup(AppData.CategoryGroups[0].Name);

        await loadHistory();
    }

    Creative? creative;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        RegisterKeyboardNavigation(this.OnNavKeyAsync);

        api.ClearErrors();

        if (Id != null)
        {
            creative = await UserState.GetCreativeAsync(Id);
            log("\nCREATIVE {0}: {1}", creative?.Id, creative?.UserPrompt);
            if (creative != null)
            {
                request.UserPrompt = creative.UserPrompt;
                imageSize = creative.Height > creative.Width
                    ? ImageSize.Portrait
                    : creative.Width > creative.Height
                        ? ImageSize.Landscape
                        : ImageSize.Square;

                var artistIds = creative.Artists?.OrderBy(x => x.Id).Select(x => x.ArtistId).ToList() ?? new();
                artists = ArtistOptions?.Count > 0 && artistIds.Count > 0
                    ? artistIds.Select(x => ArtistOptions.FirstOrDefault(m => m.Id == x)).Where(x => x != null).Cast<ArtistInfo>().ToList()
                    : new();

                var modifierIds = creative.Modifiers?.OrderBy(x => x.Id).Select(x => x.ModifierId).ToList() ?? new();
                modifiers = ModifierOptions?.Count > 0 && modifierIds.Count > 0
                    ? modifierIds.Select(x => ModifierOptions.FirstOrDefault(m => m.Id == x)).Where(x => x != null).Cast<ModifierInfo>().ToList()
                    : new();
            }
        }
    }

    bool isDirty => !string.IsNullOrEmpty(request.UserPrompt)
        || imageSize != ImageSize.Square
        || artists.Count > 0
        || modifiers.Count > 0;

    async Task reset()
    {
        await CloseDialogsAsync();
        request.UserPrompt = "";
        imageSize = ImageSize.Square;
        artists.Clear();
        modifiers.Clear();
        StateHasChanged();
    }

    async Task loadHistory()
    {
        if (User != null)
        {
            var userId = User.GetUserId().ToInt();
            var apiHistory = await ApiAsync(new QueryCreatives
            {
                OwnerId = userId,
                Take = 28,
                OrderByDesc = nameof(Creative.Id),
            });
            if (apiHistory.Succeeded)
            {
                CreativeHistory = apiHistory.Response?.Results ?? new();
                UserState.LoadCreatives(CreativeHistory);
            }
        }
        await loadUserState();
    }

    void noop() {}

    async Task submit()
    {
        request.Width = imageSize switch
        {
            ImageSize.Portrait => 512,
            ImageSize.Landscape => 896,
            _ => 512,
        };
        request.Height = imageSize switch
        {
            ImageSize.Portrait => 896,
            ImageSize.Landscape => 512,
            _ => 512,
        };
        request.ArtistIds = artists.Select(x => x.Id).ToList();
        request.ModifierIds = modifiers.Select(x => x.Id).ToList();

        api.ClearErrors();
        api.IsLoading = true;
        api = await ApiAsync(request);
        creative = api.Response?.Result;

        await loadHistory();
    }

    async Task pinArtifact(Artifact artifact)
    {
        var hold = creative!.PrimaryArtifactId;
        creative.PrimaryArtifactId = artifact.Id;
        StateHasChanged();

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
        StateHasChanged();

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
        var api = await UserState.HardDeleteCreativeAsync(creative);
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

    Task CloseDialogsAsync()
    {
        if (View != null)
            navTo(Id);
        return Task.CompletedTask;
    }

    public async Task OnNavKeyAsync(string key)
    {
        if (key == KeyCodes.Escape)
        {
            await CloseDialogsAsync();
            return;
        }

        var results = CreativeHistory;
        if (Id == null)
        {
            if (key == KeyCodes.ArrowDown)
            {
                var artifact = results.FirstOrDefault();
                if (artifact != null)
                {
                    navTo(artifact.Id);
                }
            }
            return;
        }


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

    async Task signupBeta()
    {
        signup.Type = SignupType.Beta;
        apiSignup = await ApiAsync(signup);
        if (apiSignup.Succeeded)
        {
            UserState.Signups.Add(SignupType.Beta);
        }
    }

    public void Dispose()
    {
        UserState.OnChange -= StateHasChanged;
        DeregisterKeyboardNavigation(this.OnNavKeyAsync);
    }
}
