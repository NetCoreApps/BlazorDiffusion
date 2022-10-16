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

    KeyValuePair<string, string>[]? ArtistOptions => DataCache?.Artists;

    InputInfo? artistInput;
    InputInfo? ArtistInput => artistInput ??= DataCache == null ? null : new InputInfo { AllowableEntries = ArtistOptions };

    List<KeyValuePair<string, string>> artists = new();

    void addArtist(KeyValuePair<string, string> artist)
    {
        if (!artists.Any(x => x.Key == artist.Key))
            artists.Add(artist);
    }

    void removeArtist(KeyValuePair<string, string> artist)
    {
        artists.RemoveAll(x => x.Key == artist.Key);
    }


    string[]? categoryNames;
    string[] CategoryNames => categoryNames ??= DataCache == null ? Array.Empty<string>()
        : DataCache.Modifiers.Select(x => x.Category).Distinct().OrderBy(x => x).ToArray();

    KeyValuePair<string, string>[]? modifierOptions;
    KeyValuePair<string, string>[]? ModifierOptions => modifierOptions ??= DataCache == null ? null :
        DataCache!.Modifiers.Select(x => new KeyValuePair<string, string>($"{x.Id}", x.Name)).ToArray();


    List<KeyValuePair<string, string>> modifiers = new();

    string? selectedGroup;
    string? selectedCategory;

    string[] groupCategories => DataCache?.CategoryGroups.FirstOrDefault(x => x.Name == selectedGroup)?.Items ?? Array.Empty<string>();

    KeyValuePair<string, string>[] categoryModifiers => (selectedCategory != null
        ? DataCache?.Modifiers.Where(x => x.Category == selectedCategory && !modifiers.Any(m => m.Key == x.Id.ToString())).Select(x => new KeyValuePair<string, string>($"{x.Id}", x.Name)).ToArray()
        : null) ?? Array.Empty<KeyValuePair<string, string>>();

    void addModifier(KeyValuePair<string, string> modifier)
    {
        if (!modifiers.Any(x => x.Key == modifier.Key))
            modifiers.Add(modifier);
    }

    void removeModifier(KeyValuePair<string, string> modifier)
    {
        modifiers.RemoveAll(x => x.Key == modifier.Key);
    }

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
}
