using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ServiceStack.Auth;
using ServiceStack.Blazor;
using System.Security.Claims;

namespace BlazorDiffusion;

/// <summary>
/// Manages App Authentication State
/// </summary>
//public class ServiceStackStateProvider : ServiceStackAuthenticationStateProvider
//{
//    public ServiceStackStateProvider(JsonApiClient client, ILogger<ServiceStackAuthenticationStateProvider> log)
//        : base(client, log) { }
//}
public class ServiceStackStateProvider : BlazorServerAuthenticationStateProvider
{
    public ServiceStackStateProvider(
        JsonApiClient client, 
        ILogger<BlazorServerAuthenticationStateProvider> log, 
        IHttpContextAccessor accessor, NavigationManager navigation,
        IJSRuntime js)
        : base(client, log, accessor, navigation, js) { }
}

public class BlazorServerAuthenticationStateProvider : AuthenticationStateProvider
{
    protected ApiResult<AuthenticateResponse> authApi = new();
    protected readonly JsonApiClient Client;

    protected ILogger<BlazorServerAuthenticationStateProvider> Log { get; }
    protected IHttpContextAccessor HttpContextAccessor { get; }
    protected NavigationManager NavigationManager { get; }
    protected IJSRuntime JS { get; }

    public BlazorServerAuthenticationStateProvider(
        JsonApiClient client, ILogger<BlazorServerAuthenticationStateProvider> log, IHttpContextAccessor httpContextAccessor, NavigationManager navigationManager, IJSRuntime js)
    {
        Client = client;
        Log = log;
        HttpContextAccessor = httpContextAccessor;
        NavigationManager = navigationManager;
        JS = js;
    }

    protected AuthenticationState UnAuthenticationState => new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

    public const string DefaultProfileUrl = "data:image/svg+xml,%3Csvg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'%3E%3Cstyle%3E .path%7B%7D %3C/style%3E%3Cg id='male-svg'%3E%3Cpath fill='%23556080' d='M1 92.84V84.14C1 84.14 2.38 78.81 8.81 77.16C8.81 77.16 19.16 73.37 27.26 69.85C31.46 68.02 32.36 66.93 36.59 65.06C36.59 65.06 37.03 62.9 36.87 61.6H40.18C40.18 61.6 40.93 62.05 40.18 56.94C40.18 56.94 35.63 55.78 35.45 47.66C35.45 47.66 32.41 48.68 32.22 43.76C32.1 40.42 29.52 37.52 33.23 35.12L31.35 30.02C31.35 30.02 28.08 9.51 38.95 12.54C34.36 7.06 64.93 1.59 66.91 18.96C66.91 18.96 68.33 28.35 66.91 34.77C66.91 34.77 71.38 34.25 68.39 42.84C68.39 42.84 66.75 49.01 64.23 47.62C64.23 47.62 64.65 55.43 60.68 56.76C60.68 56.76 60.96 60.92 60.96 61.2L64.74 61.76C64.74 61.76 64.17 65.16 64.84 65.54C64.84 65.54 69.32 68.61 74.66 69.98C84.96 72.62 97.96 77.16 97.96 81.13C97.96 81.13 99 86.42 99 92.85L1 92.84Z'/%3E%3C/g%3E%3C/svg%3E";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var req = AppHostBase.GetOrCreateRequest(HttpContextAccessor);
            var session = await req.GetSessionAsync();
            if (!session.IsAuthenticated)
                return UnAuthenticationState;

            List<Claim> claims = new() {
                new Claim(ClaimTypes.NameIdentifier, session.UserAuthId),
                new Claim(ClaimTypes.Name, session.DisplayName),
                new Claim(ClaimTypes.Email, session.UserName),
                new Claim(ClaimUtils.Picture, session.ProfileUrl ?? DefaultProfileUrl),
            };

            var roles = session.FromToken
                ? session.Roles
                : await session.GetRolesAsync(HostContext.AppHost.GetAuthRepositoryAsync());
            foreach (var role in roles.OrEmpty())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var perms = session.FromToken
                ? session.Permissions
                : await session.GetRolesAsync(HostContext.AppHost.GetAuthRepositoryAsync());
            foreach (var permission in perms.OrEmpty())
            {
                claims.Add(new Claim(ClaimUtils.PermissionType, permission));
            }

            var identity = new ClaimsIdentity(claims, ClaimUtils.AuthenticationType);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "SignIn failed");
            return UnAuthenticationState;
        }
    }

    public virtual async Task LogoutIfAuthenticatedAsync()
    {
        var authState = await GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
            await LogoutAsync();
    }

    public virtual async Task<ApiResult<AuthenticateResponse>> LogoutAsync()
    {
        var logoutResult = ApiResult.Create(new AuthenticateResponse());
        // await JS.InvokeVoidAsync("JS.invoke", "location", "href", "https://localhost:5001/auth/logout");
        //await JS.InvokeVoidAsync("JS.redirect");

        //HttpContextAccessor.HttpContext!.Items["CookieHandler.SetCookie"] = "ss-tok=deleted; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT";
        //var logoutResult = await Client.ApiAsync(new Authenticate { provider = "logout" });

        logoutResult = await Client.ApiAsync(new Authenticate { provider = "logout" });
        //logoutResult = ApiResult.Create((await AppHostBase.Instance.GetServiceGateway().SendAsync(new Authenticate { provider = "logout" }))
        //    .GetResponseDto<AuthenticateResponse>());


        //HttpContextAccessor.HttpContext!.Response.Headers.Add(HttpHeaders.SetCookie,"ss-tok=deleted; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT");
        //NavigationManager.NavigateTo("/auth/logout", forceLoad: true);

        NotifyAuthenticationStateChanged(Task.FromResult(UnAuthenticationState));
        authApi.ClearErrors();
        authApi = new();
        return logoutResult;
    }

    public virtual Task<ApiResult<AuthenticateResponse>> SignInAsync(ApiResult<AuthenticateResponse> api)
    {
        authApi = api;
        if (authApi.Succeeded)
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        return Task.FromResult(authApi);
    }

    public virtual Task<ApiResult<AuthenticateResponse>> SignInAsync(AuthenticateResponse authResponse) =>
        SignInAsync(ApiResult.Create(authResponse));

    // Can SignInAsync with RegisterResponse when Register.AutoLogin = true
    public virtual Task<ApiResult<AuthenticateResponse>> SignInAsync(RegisterResponse registerResponse) =>
        SignInAsync(ApiResult.Create(registerResponse.ToAuthenticateResponse()));

    public virtual async Task<ApiResult<AuthenticateResponse>> LoginAsync(string email, string password)
    {
        return await SignInAsync(await Client.ApiAsync(new Authenticate
        {
            provider = "credentials",
            Password = password,
            UserName = email,
        }));
    }
}


/*
public class ServiceStackStateProvider : ServiceStackAuthenticationStateProvider
{
    UserState userState;
    public ServiceStackStateProvider(JsonApiClient client, ILogger<ServiceStackAuthenticationStateProvider> log, UserState userState)
        : base(client, log) 
    { 
        this.userState = userState;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        Log.LogDebug("\n\nZZZZZZZZZZZ GetAuthenticationStateAsync()");
        var authState = await base.GetAuthenticationStateAsync();
        System.Security.Claims.ClaimsPrincipal? user = authState.AuthenticatedUser();
        
        Log.LogDebug("AuthenticationState {0}, {1}", user.GetUserId(), user.GetDisplayName());
        Log.LogDebug("GetCookieValues() {0}", client.GetCookieValues().Dump());

        return authState;
    }

    public override Task<ApiResult<AuthenticateResponse>> LoginAsync(string email, string password)
    {
        Log.LogDebug("\n\nZZZZZZZZZZZ LoginAsync({0},password)", email);
        return base.LoginAsync(email, password);
    }

    public override Task<ApiResult<AuthenticateResponse>> LogoutAsync()
    {
        Log.LogDebug("\n\nZZZZZZZZZZZ LogoutAsync()");
        return base.LogoutAsync();
    }

    public override Task LogoutIfAuthenticatedAsync()
    {
        Log.LogDebug("\n\nZZZZZZZZZZZ LogoutIfAuthenticatedAsync()");
        return base.LogoutIfAuthenticatedAsync();
    }
    
    public override Task<ApiResult<AuthenticateResponse>> SignInAsync(ApiResult<AuthenticateResponse> api)
    {
        Log.LogDebug("\n\nZZZZZZZZZZZ SignInAsync(ApiResult<AuthenticateResponse>)");
        return base.SignInAsync(api);
    }

    public override Task<ApiResult<AuthenticateResponse>> SignInAsync(AuthenticateResponse authResponse)
    {
        Log.LogDebug("\n\nZZZZZZZZZZZ SignInAsync(AuthenticateResponse)");
        return base.SignInAsync(authResponse);
    }
    public override Task<ApiResult<AuthenticateResponse>> SignInAsync(RegisterResponse registerResponse)
    {
        Log.LogDebug("\n\nZZZZZZZZZZZ SignInAsync(RegisterResponse)");
        return base.SignInAsync(registerResponse);
    }
}
*/