using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceStack.Blazor;
using System.Net;

namespace BlazorDiffusion;

/// <summary>
/// For Pages and Components that make use of ServiceStack functionality, e.g. Client
/// </summary>
public abstract class AppComponentBase : ServiceStack.Blazor.BlazorComponentBase, IHasJsonApiClient
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
    Likes,
}

public static class AppData
{
    static List<NavItem> DefaultLinks { get; set; } = new() { };
    static List<NavItem> AdminLinks { get; set; } = new() {
        new NavItem { Label = "Admin", Href = "/admin" },
    };
    public static List<NavItem> GetNavItems(bool isAdmin) => isAdmin ? AdminLinks : DefaultLinks;
}


public static class ServiceCollectionUtils
{
    public static IHttpClientBuilder AddBlazorServerApiClient(this IServiceCollection services, string baseUrl, Action<HttpClient>? configure = null)
    {
        return
            services
            .AddHttpContextAccessor() // most reliable way to sync AuthenticationState + HttpClient is to access HttpContext on server
            .AddTransient<CookieHandler>()
            .AddBlazorApiClient(baseUrl, configure)
            .ConfigureHttpMessageHandlerBuilder(h => new HttpClientHandler {
                UseCookies = false, // needed to allow manually adding cookies
                DefaultProxyCredentials = CredentialCache.DefaultCredentials,
            })
            .AddHttpMessageHandler<CookieHandler>();
    }
}

public class CookieHandler : DelegatingHandler, IDisposable
{
    IHttpContextAccessor HttpContextAccessor;

    public CookieHandler(IHttpContextAccessor httpContextAccessor)
    {
        this.HttpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var req = AppHostBase.GetOrCreateRequest(HttpContextAccessor);

        if (req.Dto is Authenticate auth && auth.provider == "logout" || req.PathInfo == "/auth/logout")
        {
            request.AddHeader(HttpHeaders.SetCookie, "ss-tok=deleted; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT");
            Console.WriteLine("\n\nZZZZZZZZZZZZZZ CookieHandler " + HttpHeaders.SetCookie);
        }
        else
        {
            var cookies = HttpContextAccessor.HttpContext?.Request.Cookies;
            if (cookies?.Count > 0)
            {
                var cookieHeader = string.Join("; ", cookies.Select(x => $"{x.Key}={x.Value.UrlEncode()}"));
                Console.WriteLine($"\n\nZZZZZZZZZZZZZZ CookieHandler {req.PathInfo}: {cookieHeader}");
                request.AddHeader(HttpHeaders.Cookie, cookieHeader);
            }
        }


        return await base.SendAsync(request, cancellationToken);
    }
}
