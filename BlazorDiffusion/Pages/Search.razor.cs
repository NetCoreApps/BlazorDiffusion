using BlazorDiffusion.ServiceModel;
using Microsoft.AspNetCore.Components;

namespace BlazorDiffusion.Pages;

public partial class Search
{
    [Inject] public JsonApiClient? Client { get; set; }

    static SearchDataResponse? DataCache;

    string[] VisibleFields => new[] {
        nameof(CreateCreative.UserPrompt),
        nameof(CreateCreative.Images),
        nameof(CreateCreative.Width),
        nameof(CreateCreative.Height),
    };

    ImageSize imageSize;
    enum ImageSize
    {
        Square,
        Portrait,
        Landscape,
    }

    void selectImageSize(ImageSize size) => imageSize = size;

    CreateCreative request = new();
    ApiResult<QueryResponse<CreateCreative>> api = new();

    List<ArtistInfo>? ArtistOptions => DataCache?.Artists;
    List<ArtistInfo> artists = new();
    ArtistInfo? artist;

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
    async Task submit()
    {

    }
}
