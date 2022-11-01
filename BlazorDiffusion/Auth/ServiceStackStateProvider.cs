using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ServiceStack;
using ServiceStack.Blazor;

namespace BlazorDiffusion;

/// <summary>
/// Manages App Authentication State
/// </summary>
public class ServiceStackStateProvider : BlazorServerAuthenticationStateProvider
{
    public ServiceStackStateProvider(BlazorServerAuthContext context, ILogger<BlazorServerAuthenticationStateProvider> log) : base(context, log) { }
}
