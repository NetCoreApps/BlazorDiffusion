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


public static class ServiceCollectionUtils
{
    // WARNING: This prevents broken auth but keeps the user signed in on different browsers, use only for development

    public static IServiceCollection AddBlazorServerApiClient(this IServiceCollection services, string baseUrl) =>
        services.AddBlazorServerApiClient(baseUrl, null);
    public static IServiceCollection AddBlazorServerApiClient(this IServiceCollection services, string baseUrl, Action<HttpClient>? configure)
    {
        if (BlazorConfig.Instance.UseLocalStorage)
        {
            services.TryAddScoped<ILocalStorage, LocalStorage>();
            services.TryAddScoped<LocalStorage>();
            services.TryAddScoped<CachedLocalStorage>();
        }

        services.AddHttpClient(nameof(JsonApiClient), client =>  {
                client.BaseAddress = new Uri(baseUrl);
            })
            .ConfigureHttpMessageHandlerBuilder(builder =>
            {
                builder.PrimaryHandler = new HttpClientHandler
                {
                    UseDefaultCredentials = true,
                    AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.Deflate | DecompressionMethods.GZip,
                };
                builder.Build();
            });

        return services.AddScoped(services => new JsonApiClient(
            X.Apply(services.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(JsonApiClient)), configure)));
    }
}
