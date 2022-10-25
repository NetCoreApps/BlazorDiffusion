using BlazorDiffusion.UI;
using Microsoft.AspNetCore.Components.Authorization;
using ServiceStack;
using ServiceStack.Blazor;
using ServiceStack.Text;

namespace BlazorDiffusion;

/// <summary>
/// Manages App Authentication State
/// </summary>
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
        Log.LogDebug("GetAuthenticationStateAsync()");
        var authState = await base.GetAuthenticationStateAsync();
        System.Security.Claims.ClaimsPrincipal? user = authState.AuthenticatedUser();
        
        Log.LogDebug("AuthenticationState {0}, {1}", user.GetUserId(), user.GetDisplayName());
        Log.LogDebug("GetCookieValues() {0}", client.GetCookieValues().Dump());

        return authState;
    }

    public override Task<ApiResult<AuthenticateResponse>> LoginAsync(string email, string password)
    {
        Log.LogDebug("LoginAsync({0},password)", email);
        return base.LoginAsync(email, password);
    }

    public override Task<ApiResult<AuthenticateResponse>> LogoutAsync()
    {
        Log.LogDebug("LogoutAsync()");
        return base.LogoutAsync();
    }

    public override Task LogoutIfAuthenticatedAsync()
    {
        Log.LogDebug("LogoutIfAuthenticatedAsync()");
        return base.LogoutIfAuthenticatedAsync();
    }
    
    public override Task<ApiResult<AuthenticateResponse>> SignInAsync(ApiResult<AuthenticateResponse> api)
    {
        Log.LogDebug("SignInAsync(ApiResult<AuthenticateResponse>)");
        return base.SignInAsync(api);
    }

    public override Task<ApiResult<AuthenticateResponse>> SignInAsync(AuthenticateResponse authResponse)
    {
        Log.LogDebug("SignInAsync(AuthenticateResponse)");
        return base.SignInAsync(authResponse);
    }
    public override Task<ApiResult<AuthenticateResponse>> SignInAsync(RegisterResponse registerResponse)
    {
        Log.LogDebug("SignInAsync(RegisterResponse)");
        return base.SignInAsync(registerResponse);
    }

}
