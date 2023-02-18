using BlazorDiffusion.ServiceModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ServiceStack.Blazor;
using System.Collections.Concurrent;
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
    [Inject] ILogger<AppAuthComponentBase> Log { get; set; }

    protected override async Task OnInitializedAsync()
    {
        SetTitle(AppData.Title);
        await base.OnInitializedAsync();
    }

    protected async Task loadUserState(bool force = false)
    {
        var task = UserState.LoadAnonAsync(force);
        if (IsAuthenticated)
        {
            await UserState.LoadAsync(force);
        }
        await task;
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

public enum ImageSize
{
    Square,
    Portrait,
    Landscape,
}

public enum CreateMenu
{
    History,
}
