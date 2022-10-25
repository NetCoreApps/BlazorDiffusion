using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Web;
using System;

namespace BlazorDiffusion.ServiceModel;

public static class StatUtils
{
    public static T WithRequest<T>(this T stat, IRequest req, IAuthSession session) where T : StatBase
    {
        if (session.IsAuthenticated)
        {
            stat.AppUserId = session.UserAuthId?.ToInt();
        }

        stat.RawUrl = req.RawUrl;
        stat.RemoteIp = req.RemoteIp;
        stat.CreatedDate = DateTime.UtcNow;

        return stat;
    }
}
