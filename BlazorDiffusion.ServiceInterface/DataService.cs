using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.OrmLite;
using BlazorDiffusion.ServiceModel;

namespace BlazorDiffusion.ServiceInterface;

public class DataService : Service
{
    public IAutoQueryDb AutoQuery { get; set; }

    public async Task<object> Any(SearchData request)
    {
        var to = new SearchDataResponse
        {
            Artists = (await Db.SelectAsync<Artist>()).OrderBy(x => x.Rank)
                .Select(x => new ArtistInfo
                {
                    Id = x.Id,
                    Name = x.FirstName != null ? $"{x.FirstName} {x.LastName}" : x.LastName,
                    Type = x.Type == null ? null : string.Join(", ", x.Type.Take(3)),
                }).ToList(),

            Modifiers = (await Db.SelectAsync<Modifier>()).OrderBy(x => x.Rank)
                .Select(x => new ModifierInfo { Id = x.Id, Name = x.Name, Category = x.Category }).ToList(),
        };
        return to;
    }

    public async Task<object> Any(GetUserInfo request)
    {
        var userId = await Db.ScalarAsync<int>(Db.From<AppUser>()
            .Where(x => x.RefIdStr == request.RefId).Select(x => x.Id));
        if (userId == default)
            return HttpError.NotFound("User not found");

        var result = X.Apply(await Db.GetUserResultAsync(userId), x => x.RefId = request.RefId);
        return new GetUserInfoResponse
        {
            Result = result,
        };
    }

    public async Task<object> Any(UserData request)
    {
        var session = await SessionAsAsync<CustomUserSession>();
        var result = await Db.GetUserResultAsync(session.UserAuthId.ToInt());

        return new UserDataResponse
        {
            User = result,
            Roles = (await session.GetRolesAsync(AuthRepositoryAsync)).ToList(),
        };
    }

    public async Task<object> Any(GetAlbumResults request)
    {
        var albums = (await Db.LoadSelectAsync<Album>(x => x.DeletedDate == null && request.Ids.Contains(x.Id)))
            .OrderByDescending(x => x.Artifacts.Max(x => x.Id)).ToList();
        var albumResults = albums.Map(x => x.ToAlbumResult());

        return new GetAlbumResultsResponse
        {
            Results = albumResults,
        };
    }
}
