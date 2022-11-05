using BlazorDiffusion.UI;
using Microsoft.AspNetCore.Components;
using ServiceStack.Blazor;
using System.Security.Claims;

namespace BlazorDiffusion;

/// <summary>
/// For Pages and Components that make use of ServiceStack functionality, e.g. Client
/// </summary>
public abstract class AppComponentBase : ServiceStack.Blazor.BlazorComponentBase
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

    protected async Task loadUserState(bool force=false)
    {
        if (IsAuthenticated)
        {
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

public static class AppData
{
    static List<NavItem> DefaultLinks { get; set; } = new() { };
    static List<NavItem> AdminLinks { get; set; } = new() {
        new NavItem { Label = "Admin", Href = "/admin" },
    };
    public static List<NavItem> GetNavItems(bool isAdmin) => isAdmin ? AdminLinks : DefaultLinks;
}
