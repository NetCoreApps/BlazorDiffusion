using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.Blazor;

namespace BlazorDiffusion.UI;

public class UserState
{
    public CachedLocalStorage LocalStorage { get; }
    public JsonApiClient Client { get; }
    public AppPrefs AppPrefs { get; internal set; } = new();

    public string? RefId { get; set; }
    public HashSet<int> LikedArtifactIds { get; private set; } = new();
    public HashSet<int> LikedAlbumIds { get; private set; } = new();

    public List<Creative> CreativeHistory { get; private set; } = new();

    public Dictionary<int, Album> AlbumsMap { get; } = new();
    public Dictionary<int, Artifact> ArtifactsMap { get; } = new();

    public Dictionary<int, Creative> CreativesMap { get; } = new();
    public List<Album> UserAlbums { get; private set; } = new();

    public List<Artifact> LikedArtifacts => LikedArtifactIds.Select(x => ArtifactsMap.TryGetValue(x, out var a) ? a : null)
        .Where(x => x != null).Cast<Artifact>().ToList();

    public UserState(CachedLocalStorage localStorage, JsonApiClient client)
    {
        LocalStorage = localStorage;
        Client = client;
    }

    public async Task SaveAppPrefs()
    {
        await LocalStorage.SetItemAsync(nameof(AppPrefs), AppPrefs);
    }

    public async Task LoadAppPrefs()
    {
        AppPrefs = await LocalStorage.GetItemAsync<AppPrefs>(nameof(AppPrefs)) ?? new AppPrefs();
        NotifyStateChanged();
    }

    public async Task LoadAsync(int userId)
    {
        var apiHistory = await Client.ApiAsync(new QueryCreatives
        {
            OwnerId = userId,
            Take = 28,
            OrderByDesc = nameof(Creative.Id),
        });
        if (apiHistory.Succeeded)
        {
            CreativeHistory = apiHistory.Response?.Results ?? new();
        }
        await LoadUserDataAsync();
    }

    public async Task LoadUserDataAsync()
    {
        var api = await Client.ApiAsync(new UserData());
        if (api.Succeeded)
        {
            var r = api.Response!;
            RefId = r.RefId;
            LikedArtifactIds = r.Likes.ArtifactIds.ToSet();
            LikedAlbumIds = r.Likes.AlbumIds.ToSet();
            UserAlbums = r.Albums ?? new();
            LoadAlbums(UserAlbums);
        }

        var missingIds = new List<int>();
        foreach (var artifactId in LikedArtifactIds)
        {
            if (GetCachedArtifact(artifactId) == null)
                missingIds.Add(artifactId);
        }
        if (missingIds.Count > 0)
        {
            var apiArtifacts = await Client.ApiAsync(new QueryArtifacts { Ids = missingIds });
            if (apiArtifacts.Response?.Results != null) LoadArtifacts(apiArtifacts.Response.Results);
        }

        missingIds = new();
        foreach (var albumId in LikedAlbumIds)
        {
            if (GetCachedAlbum(albumId) == null)
                missingIds.Add(albumId);
        }
        if (missingIds.Count > 0)
        {
            var apiAlbums = await Client.ApiAsync(new QueryAlbums { Ids = missingIds });
            if (apiAlbums.Response?.Results != null) LoadAlbums(apiAlbums.Response.Results);
        }

        NotifyStateChanged();
    }

    public void LoadCreatives(IEnumerable<Creative> creatives) => creatives.Each(LoadCreative);
    public void LoadCreative(Creative creative)
    {
        CreativesMap[creative.Id] = creative;
        foreach (var artifact in creative.Artifacts.OrEmpty())
        {
            ArtifactsMap[artifact.Id] = artifact;
        }
    }

    public void LoadAlbums(IEnumerable<Album> albums) => albums.Each(LoadAlbum);
    public void LoadAlbum(Album album) => AlbumsMap[album.Id] = album;
    public void LoadArtifacts(IEnumerable<Artifact> artifacts) => artifacts.Each(LoadArtifact);
    public void LoadArtifact(Artifact artifact) => ArtifactsMap[artifact.Id] = artifact;

    public Album? GetCachedAlbum(int? id) => id != null
        ? AlbumsMap.TryGetValue(id.Value, out var a) ? a : null
        : null;

    public Artifact? GetCachedArtifact(int? id) => id != null
        ? ArtifactsMap.TryGetValue(id.Value, out var a) ? a : null
        : null;

    public Creative? GetCachedCreative(int? id) => id != null
        ? CreativesMap.TryGetValue(id.Value, out var a) ? a : null
        : null;

    public async Task<Creative?> GetCreativeAsync(int? creativeId)
    {
        var creative = GetCachedCreative(creativeId);
        if (creativeId == null || creative != null)
            return creative;

        var api = await Client.ApiAsync(new QueryCreatives { Id = creativeId });
        if (api.Succeeded && api.Response?.Results != null)
        {
            LoadCreatives(api.Response.Results);
        }
        return GetCachedCreative(creativeId);
    }

    public async Task<Artifact?> GetArtifactAsync(int? artifactId)
    {
        var artifact = GetCachedArtifact(artifactId);
        if (artifactId == null || artifact != null)
            return artifact;

        var api = await Client.ApiAsync(new QueryArtifacts { Id = artifactId });
        if (api.Succeeded && api.Response?.Results != null)
        {
            LoadArtifacts(api.Response.Results);
        }
        return GetCachedArtifact(artifactId);
    }


    public bool HasLiked(Artifact artifact) => LikedArtifactIds.Contains(artifact.Id);

    public async Task LikeArtifactAsync(Artifact artifact)
    {
        ArtifactsMap[artifact.Id] = artifact;
        LikedArtifactIds.Add(artifact.Id);
        var api = await Client.ApiAsync(new CreateArtifactLike
        {
            ArtifactId = artifact.Id,
        });
        if (!api.Succeeded)
        {
            LikedArtifactIds.Remove(artifact.Id);
        }
        NotifyStateChanged();
    }

    public async Task UnlikeArtifactAsync(Artifact artifact)
    {
        ArtifactsMap[artifact.Id] = artifact;
        LikedArtifactIds.Remove(artifact.Id);
        var api = await Client.ApiAsync(new DeleteArtifactLike
        {
            ArtifactId = artifact.Id,
        });
        if (!api.Succeeded)
        {
            LikedArtifactIds.Add(artifact.Id);
        }
        NotifyStateChanged();
    }

    public void RemoveArtifact(Artifact artifact)
    {
        if (artifact == null) return;
        LikedArtifactIds.Remove(artifact.Id);
        ArtifactsMap.Remove(artifact.Id);
    }

    public void RemoveCreative(Creative creative)
    {
        if (creative == null) return;
        creative.Artifacts.Each(RemoveArtifact);
        CreativeHistory.RemoveAll(x => x.Id == creative.Id);
        CreativesMap.Remove(creative.Id);
    }

    public async Task<ApiResult<EmptyResponse>> HardDeleteCreativeByIdAsync(int creativeId)
    {
        var creative = GetCachedCreative(creativeId);
        return creative != null
            ? await HardDeleteCreativeAsync(creative)
            : new();
    }

    public async Task<ApiResult<EmptyResponse>> HardDeleteCreativeAsync(Creative creative)
    {
        var creativeId = creative.Id;
        var api = await Client.ApiAsync(new HardDeleteCreative {
            Id = creativeId,
        });
        if (api.Succeeded)
        {
            RemoveCreative(creative);
            NotifyStateChanged();
        }
        return api;
    }


    public event Action? OnChange;
    private void NotifyStateChanged() => OnChange?.Invoke();
}

public class AppPrefs
{
    public string ArtifactGalleryColumns { get; set; } = "5";
}
