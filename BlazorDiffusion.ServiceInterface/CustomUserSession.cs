using ServiceStack;
using BlazorDiffusion.ServiceModel;

namespace BlazorDiffusion.ServiceInterface;

// Add any additional metadata properties you want to store in the Users Typed Session
public class CustomUserSession : AuthUserSession
{
    public string Handle { get; set; }
    public string Avatar { get; set; }

    public int GetUserId() => UserAuthId.ToInt();
}

public static class UsersExtensions
{
    public static CustomUserSession ToUserSession(this AppUser appUser)
    {
        var session = appUser.ConvertTo<CustomUserSession>();
        session.Id = SessionExtensions.CreateRandomSessionId();
        session.IsAuthenticated = true;
        session.FromToken = true; // use embedded roles
        return session;
    }
}
