using Microsoft.AspNetCore.Components;
using ServiceStack.Blazor;
using System.Security.Claims;

namespace BlazorDiffusion.UI;

/// <summary>
/// For Pages and Components that make use of ServiceStack functionality, e.g. Client
/// </summary>
public abstract class AppComponentBase : BlazorComponentBase
{
}

/// <summary>
/// For Pages and Components requiring Authentication
/// </summary>
public abstract class AppAuthComponentBase : AuthBlazorComponentBase
{
    public bool IsModerator => IsAuthenticated && User.HasRole(AppRoles.Moderator);
    [Inject] public UserState UserState { get; set; } = default!;
    [Inject] public KeyboardNavigation KeyboardNavigation { get; set; }

    protected async Task loadUserState(bool force = false)
    {
        if (IsAuthenticated)
        {
            log("loadUserState...");
            await UserState.LoadAsync(force);
        }
    }

    public void RegisterKeyboardNavigation(Func<string, Task> target)
    {
        log("KEYNAV {0} registered", GetType().Name);
        KeyboardNavigation.Register(target);
    }
    public void DeregisterKeyboardNavigation(Func<string, Task> target)
    {
        log("KEYNAV {0} de-registered", GetType().Name);
        KeyboardNavigation.Deregister(target);
    }
}

public enum AppPage
{
    Search,
    Create,
    Favorites,
}

public enum PageView
{
    Report,
    NewAlbum,
    EditProfile,
}
