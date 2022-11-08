using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.OrmLite;
using BlazorDiffusion.ServiceModel;
using System;

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
        var userId = session.GetUserId();
        var result = await Db.GetUserResultAsync(userId);

        using var dbAnalytics = OpenDbConnection(Databases.Analytics);
        var signupTypes = await dbAnalytics.ColumnAsync<SignupType>(Db.From<Signup>()
            .Where(x => x.AppUserId == userId && x.Type == SignupType.Beta && x.CancelledDate == null).Select(x => x.Type));

        var topAlbumResults = (await Db.LoadSelectAsync(Db.From<Album>().Where(x => x.DeletedDate == null)
                .OrderByDescending(x => new { x.Score, x.Id }).Take(10)))
            .Map(x => x.ToAlbumResult());

        return new UserDataResponse
        {
            User = result,
            Signups = signupTypes,
            TopAlbums = topAlbumResults,
            Roles = (await session.GetRolesAsync(AuthRepositoryAsync)).ToList(),
        };
    }

    public async Task<object> Any(GetAlbumResults request)
    {
        var ids = request.Ids?.ToArray() ?? Array.Empty<int>();
        var refIds = request.RefIds?.ToArray() ?? Array.Empty<string>();

        var albums = (await Db.LoadSelectAsync<Album>(x => x.DeletedDate == null && (ids.Contains(x.Id) || refIds.Contains(x.RefId))))
            .OrderByDescending(x => x.Artifacts.Max(x => x.Id)).ToList();

        var albumResults = albums.Map(x => x.ToAlbumResult());

        return new GetAlbumResultsResponse
        {
            Results = albumResults,
        };
    }

    public async Task<object> Any(CreateSignup request)
    {
        var session = await SessionAsAsync<CustomUserSession>();
        using var dbAnalytics = OpenDbConnection(Databases.Analytics);

        // If already exists uncancel existing Signup and prevent duplicate registrations
        int existingSignups = session.IsAuthenticated
            ? await dbAnalytics.UpdateOnlyAsync(() => new Signup { Email = request.Email, CancelledDate = null },
                where: x => x.AppUserId == session.GetUserId() && x.Type == request.Type)
            : await dbAnalytics.UpdateOnlyAsync(() => new Signup { CancelledDate = null },
                where: x => x.Email == request.Email && x.Type == request.Type);

        if (existingSignups == 0)
        {
            await dbAnalytics.InsertAsync(new Signup {
                Type = request.Type,
                Name = request.Name, 
                Email = request.Email,
            }
            .WithRequest(Request, session));
        }
     
        return new EmptyResponse();
    }
}
