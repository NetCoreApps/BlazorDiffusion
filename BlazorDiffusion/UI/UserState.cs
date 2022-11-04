using ServiceStack;
using ServiceStack.Blazor;
using BlazorDiffusion.ServiceModel;
using Microsoft.AspNetCore.Components;
using System.Linq;

namespace BlazorDiffusion.UI;

public class UserState
{
    public const int InitialTake = 30;
    public const int NextPage = 100;

    public CachedLocalStorage LocalStorage { get; }
    public IServiceGateway Client { get; }
    public AppPrefs AppPrefs { get; internal set; } = new();
    
    // Capture images that should have loaded in Browsers cache
    public HashSet<int> HasIntersected { get; } = new();

    public string? RefId { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<int> LikedArtifactIds { get; private set; } = new();
    public List<int> LikedAlbumIds { get; private set; } = new();

    public Dictionary<int, AlbumResult> AlbumsMap { get; } = new();
    public Dictionary<int, Artifact> ArtifactsMap { get; } = new();

    public Dictionary<int, Creative> CreativesMap { get; } = new();
    public Dictionary<int, AlbumResult[]> CreativesInAlbumsMap { get; } = new();
    public Dictionary<string, UserResult> UsersMap { get; } = new();

    public List<AlbumResult> UserAlbums { get; private set; } = new();
    public bool IsLoading { get; set; }

    NavigationManager NavigationManager { get; }
    public UserState(CachedLocalStorage localStorage, IServiceGateway client, NavigationManager navigationManager)
    {
        LocalStorage = localStorage;
        Client = client;
        NavigationManager = navigationManager;
    }

    async Task<ApiResult<TResponse>> ApiAsync<TResponse>(IReturn<TResponse> request)
    {
        IsLoading = true;
        NotifyStateChanged();

        var api = await Client.ManagedApiAsync(request);

        IsLoading = false;
        NotifyStateChanged();

        return api;
    }

    async Task<ApiResult<EmptyResponse>> ApiAsync(IReturnVoid request)
    {
        IsLoading = true;
        NotifyStateChanged();

        var api = await Client.ManagedApiAsync(request);

        IsLoading = false;
        NotifyStateChanged();

        return api;
    }

    protected virtual async Task OnApiErrorAsync(object requestDto, IHasErrorStatus apiError)
    {
        if (BlazorConfig.Instance.OnApiErrorAsync != null)
            await BlazorConfig.Instance.OnApiErrorAsync(requestDto, apiError);
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

    public async Task LoadAsync(bool force = false)
    {
        if (force || RefId == null)
        {
            var request = new UserData();
            var api = await ApiAsync(request);
            if (api.Succeeded)
            {
                var r = api.Response!;
                RefId = r.RefId;
                Roles = r.Roles ?? new();
                LikedArtifactIds = r.Likes.ArtifactIds;
                LikedAlbumIds = r.Likes.AlbumIds;
                UserAlbums = r.Albums ?? new();
                LoadAlbums(UserAlbums);
                await LoadAlbumCoverArtifacts();
            }
        }

        await GetLikedArtifactsAsync(InitialTake);
        await GetLikedAlbumsAsync(InitialTake);

        NotifyStateChanged();
    }

    public bool IsModerator() => Roles.Contains(AppRoles.Moderator);

    public async Task LoadAlbumCoverArtifacts()
    {
        var albumArtifactIds = UserAlbums.Select(GetAlbumCoverArtifactId).ToList();
        await LoadArtifactsAsync(albumArtifactIds);
    }

    public int GetAlbumCoverArtifactId(AlbumResult album)
    {
        return album.PrimaryArtifactId != null && album.ArtifactIds.Contains(album.PrimaryArtifactId.Value)
            ? album.PrimaryArtifactId.Value
            : album.ArtifactIds.First();
    }
    public Artifact? GetAlbumCoverArtifact(AlbumResult album)
    {
        var id = GetAlbumCoverArtifactId(album);
        return GetCachedArtifact(id);
    }

    public async Task<List<Artifact>> GetAlbumArtifactsAsync(AlbumResult album, int? take)
    {
        var artifactIds = take != null
            ? album.ArtifactIds.Take(take.Value).ToList()
            : album.ArtifactIds;

        await LoadArtifactsAsync(artifactIds);

        var to = artifactIds.Select(id => GetCachedArtifact(id)).Where(x => x != null).Cast<Artifact>().ToList();
        return to;
    }

    public async Task<List<Artifact>> GetLikedArtifactsAsync(int? take)
    {
        var requestedLikeIds = take != null
            ? LikedArtifactIds.Take(take.Value).ToList()
            : LikedArtifactIds;

        await LoadArtifactsAsync(requestedLikeIds);

        var to = LikedArtifactIds.Select(id => GetCachedArtifact(id)).Where(x => x != null).Cast<Artifact>().ToList();
        return to;
    }

    public async Task LoadArtifactsAsync(IEnumerable<int> artifactIds)
    {
        var missingIds = new List<int>();
        foreach (var id in artifactIds)
        {
            if (GetCachedArtifact(id) == null)
                missingIds.Add(id);
        }
        if (missingIds.Count > 0)
        {
            IsLoading = true;
            var api = await ApiAsync(new QueryArtifacts { Ids = missingIds });
            if (api.Response?.Results != null) LoadArtifacts(api.Response.Results);
        }
    }

    public async Task<List<AlbumResult>> GetLikedAlbumsAsync(int? take)
    {
        var missingIds = new List<int>();
        var requestedLikeIds = take != null
            ? LikedAlbumIds.Take(take.Value).ToList()
            : LikedAlbumIds;

        foreach (var id in requestedLikeIds)
        {
            if (GetCachedAlbum(id) == null)
                missingIds.Add(id);
        }
        if (missingIds.Count > 0)
        {
            var api = await ApiAsync(new GetAlbumResults { Ids = missingIds });
            if (api.Response?.Results != null) LoadAlbums(api.Response.Results);
        }

        var to = LikedAlbumIds.Select(id => GetCachedAlbum(id)).Where(x => x != null).Cast<AlbumResult>().ToList();
        return to;
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

    public void LoadAlbums(IEnumerable<AlbumResult> albums) => albums.Each(LoadAlbum);
    public void LoadAlbum(AlbumResult album) => AlbumsMap[album.Id] = album;
    public void LoadArtifacts(IEnumerable<Artifact> artifacts) => artifacts.Each(LoadArtifact);
    public void LoadArtifact(Artifact artifact) => ArtifactsMap[artifact.Id] = artifact;

    public AlbumResult? GetCachedAlbum(int? id) => id != null
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

        var request = new QueryCreatives { Id = creativeId };
        var api = await ApiAsync(request);
        if (api.Succeeded && api.Response?.Results != null)
        {
            LoadCreatives(api.Response.Results);
        }
        if (!api.Succeeded)
        {
            await OnApiErrorAsync(request, api);
        }
        return GetCachedCreative(creativeId);
    }

    public async Task<Artifact?> GetArtifactAsync(int? artifactId)
    {
        var artifact = GetCachedArtifact(artifactId);
        if (artifactId == null || artifact != null)
            return artifact;

        var request = new QueryArtifacts { Id = artifactId };
        var api = await ApiAsync(request);
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
        LikedArtifactIds.Insert(0, artifact.Id);
        var request = new CreateArtifactLike
        {
            ArtifactId = artifact.Id,
        };
        var api = await ApiAsync(request);
        if (!api.Succeeded)
        {
            LikedArtifactIds.Remove(artifact.Id);
            NavigationManager.NavigateTo(NavigationManager.GetLoginUrl());
        }
        NotifyStateChanged();
    }

    public async Task UnlikeArtifactAsync(Artifact artifact)
    {
        ArtifactsMap[artifact.Id] = artifact;
        var pos = LikedArtifactIds.FindIndex(x => x == artifact.Id);
        LikedArtifactIds.Remove(artifact.Id);
        var request = new DeleteArtifactLike
        {
            ArtifactId = artifact.Id,
        };
        var api = await ApiAsync(request);
        if (!api.Succeeded)
        {
            LikedArtifactIds.Insert(Math.Max(pos,0), artifact.Id);
            NavigationManager.NavigateTo(NavigationManager.GetLoginUrl());
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
        var request = new HardDeleteCreative
        {
            Id = creativeId,
        };
        var api = await ApiAsync(request);
        if (api.Succeeded)
        {
            RemoveCreative(creative);
            NotifyStateChanged();
        }
        else
        {
            await OnApiErrorAsync(request, api);
        }
        return api;
    }

    public async Task<AlbumResult[]> GetCreativeInAlbumsAsync(int creativeId)
    {
        if (CreativesInAlbumsMap.TryGetValue(creativeId, out var albums))
            return albums;

        var api = await ApiAsync(new GetCreativesInAlbums { CreativeId = creativeId });
        if (api.Succeeded)
        {
            return CreativesInAlbumsMap[creativeId] = (api.Response!.Results ?? new()).ToArray();
        }
        return Array.Empty<AlbumResult>();
    }

    public event Action? OnChange;
    private void NotifyStateChanged() => OnChange?.Invoke();

    public bool HasArtifactInAlbum(Artifact artifact)
    {
        return UserAlbums.Any(a => a.ArtifactIds.Contains(artifact.Id));
    }

    public void AddArtifactToAlbum(AlbumResult album, Artifact artifact)
    {
        var userAlbum = UserAlbums.FirstOrDefault(x => x.Id == album.Id);
        if (userAlbum != null)
        {
            if (!album.ArtifactIds.Contains(artifact.Id))
            {
                album.ArtifactIds.Add(artifact.Id);
                NotifyStateChanged();
            }
        }
    }

    public void RemoveArtifactFromAlbum(AlbumResult album, Artifact artifact)
    {
        var userAlbum = UserAlbums.FirstOrDefault(x => x.Id == album.Id);
        if (userAlbum != null)
        {
            if (album.ArtifactIds.Contains(artifact.Id))
            {
                album.ArtifactIds.RemoveAll(x => x == artifact.Id);
                if (!album.ArtifactIds.Any())
                {
                    UserAlbums.RemoveAll(x => x.Id == album.Id);
                }
                NotifyStateChanged();
            }
        }
    }

    public async Task<UserResult?> GetUserByRefIdAsync(string userRefId)
    {
        if (UsersMap.TryGetValue(userRefId, out var result))
            return result;

        var api = await ApiAsync(new GetUserInfo { RefId = userRefId });
        if (api.Succeeded)
        {
            result = UsersMap[userRefId] = api.Response!.Result;

            var artifactIds = result.Albums.Select(x => x.PrimaryArtifactId ?? x.ArtifactIds.First()).ToSet();
            await LoadArtifactsAsync(artifactIds);

            return result;
        }
        
        return null;
    }
}

public class AppPrefs
{
    public string ArtifactGalleryColumns { get; set; } = "5";
}
