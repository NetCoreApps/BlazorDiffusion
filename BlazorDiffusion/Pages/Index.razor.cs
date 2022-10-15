using BlazorDiffusion.ServiceModel;
using Microsoft.AspNetCore.Components;

namespace BlazorDiffusion.Pages;

public partial class Index
{
    [Inject] public JsonApiClient? Client { get; set; }

    string artist = "";

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

    [Parameter, SupplyParameterFromQuery] public string? Browse { get; set; }
    bool search => Browse == null;
    bool browse => Browse != null;

    CreateCreative request = new();
    ApiResult<QueryResponse<CreateCreative>> api = new();

    void navToSearch() => NavigationManager.NavigateTo(NavigationManager.Uri.SetQueryParam("browse", null));
    void navToBrowse() => NavigationManager.NavigateTo(NavigationManager.Uri.SetQueryParam("browse", ""));

    KeyValuePair<string, string>[]? artistOptions;
    KeyValuePair<string, string>[]? ArtistOptions => artistOptions ??= DataCache == null ? null :
        DataCache!.Artists.Select(x => new KeyValuePair<string, string>($"{x.Id}", x.FirstName != null ? $"{x.FirstName} {x.LastName}" : x.LastName)).ToArray();

    string[] categoryNames;
    string[] CategoryNames => categoryNames ??= DataCache == null ? Array.Empty<string>() 
        : DataCache.Modifiers.Select(x => x.Category).Distinct().OrderBy(x => x).ToArray();

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

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (DataCache == null)
        {
            DataCache = await Client!.SendAsync(new SearchData());
        }
    }

}
