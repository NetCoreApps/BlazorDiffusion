using ServiceStack.Blazor;

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
}

public enum AppPage
{
    Search,
    Create,
    Favorites,
}

public static class AppData
{
    static List<NavItem> DefaultLinks { get; set; } = new() { };
    static List<NavItem> AdminLinks { get; set; } = new() {
        new NavItem { Label = "Admin", Href = "/admin" },
    };
    public static List<NavItem> GetNavItems(bool isAdmin) => isAdmin ? AdminLinks : DefaultLinks;
}
