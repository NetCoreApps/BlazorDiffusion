using ServiceStack;
using BlazorDiffusion.ServiceModel;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using System;

namespace BlazorDiffusion.ServiceInterface;

public class MyServices : Service
{
    public async Task<object> Any(GetUserProfile request)
    {
        var session = await SessionAsAsync<CustomUserSession>();
        var userProfile = await Db.GetUserProfileAsync(session.UserAuthId.ToInt());
        return new GetUserProfileResponse {
            Result = userProfile
        };
    }

    public async Task<object> Any(UpdateUserProfile request)
    {
        var session = await SessionAsAsync<CustomUserSession>();
        var userId = session.GetUserId();

        var userInfo = await Db.SingleAsync<UserProfile>(Db.From<AppUser>()
            .Where(x => x.Id == userId));

        if (string.IsNullOrWhiteSpace(request.Handle))
            request.Handle = null;
        if (string.IsNullOrWhiteSpace(request.Avatar))
            request.Avatar = null;

        if (request.Handle != null && !request.Handle.IsValidVarName())
            throw new ArgumentException("Invalid chars in Handle", nameof(request.Handle));

        if (request.Handle != null && await Db.ExistsAsync<AppUser>(x => x.Handle == request.Handle && x.Id != userId))
            throw new ArgumentException("Handle already taken", nameof(request.Handle));

        await Db.UpdateOnlyAsync(() => new AppUser {
            DisplayName = request.DisplayName ?? userInfo.DisplayName,
            Handle = request.Handle,
            Avatar = request.Avatar ?? userInfo.Avatar,
        }, where: x => x.Id == userId);

        return new UserProfile {
            DisplayName = request.DisplayName ?? userInfo.DisplayName,
            Handle = request.Handle,
            Avatar = request.Avatar ?? userInfo.Avatar,
        };
    }
}
