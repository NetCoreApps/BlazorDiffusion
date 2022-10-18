using BlazorDiffusion.ServiceModel;
using Microsoft.AspNetCore.Components;
using ServiceStack;
using ServiceStack.Blazor;

namespace BlazorDiffusion.Pages;

public partial class Create : AppAuthComponentBase
{
    [Inject] public NavigationManager NavigationManager { get; set; }

    static SearchDataResponse? DataCache;

    string[] VisibleFields => new[] {
        nameof(CreateCreative.UserPrompt),
        nameof(CreateCreative.Images),
        nameof(CreateCreative.Width),
        nameof(CreateCreative.Height),
    };

    [Parameter, SupplyParameterFromQuery] public int? Id { get; set; }
    [Parameter, SupplyParameterFromQuery] public int? View { get; set; }


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
    CreateMenu? createMenu;
    void toggleMenu(CreateMenu menu) => createMenu = menu == createMenu ? null : menu;
    void closeMenu() => createMenu = null;


    void selectImageSize(ImageSize size) => imageSize = size;

    CreateCreative request = new();
    ApiResult<Creative> api = new();
    ApiResult<QueryResponse<Creative>> apiHistory = new();

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

        if (User != null)
        {
            apiHistory = await ApiAsync(new QueryCreatives {
                CreatedBy = User.GetEmail(),
                Take = 30,
                OrderByDesc = nameof(Creative.Id),
            });
        }

        if (Id != null)
        {
            if (apiHistory.Response != null)
            {
                creative = apiHistory.Response.Results.FirstOrDefault(x => x.Id == Id);
            }
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
    }
}
