using BlazorDiffusion.ServiceInterface;
using Microsoft.AspNetCore.Hosting;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.FluentValidation;
using ServiceStack.Text;

[assembly: HostingStartup(typeof(BlazorDiffusion.ConfigureAuth))]

namespace BlazorDiffusion;

// Custom Validator to add custom validators to built-in /register Service requiring DisplayName and ConfirmPassword
public class CustomRegistrationValidator : RegistrationValidator
{
    public CustomRegistrationValidator()
    {
        RuleSet(ApplyTo.Post, () =>
        {
            RuleFor(x => x.DisplayName).NotEmpty();
            RuleFor(x => x.ConfirmPassword).NotEmpty();
        });
    }
}

public class ConfigureAuth : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        //.ConfigureServices(services => services.AddSingleton<ICacheClient>(new MemoryCacheClient()))
        .ConfigureAppHost(appHost =>
        {
            var appSettings = appHost.AppSettings;
            var msAppId = appSettings.Get<string>("oauth.microsoftgraph.AppId") ??
                Environment.GetEnvironmentVariable("AZURE_APP_ID");
            var msAppSecret = appSettings.Get<string>("oauth.microsoftgraph.AppSecret") ??
                Environment.GetEnvironmentVariable("AZURE_APP_SECRET");
            var googleConsumerKey = appSettings.Get<string>("oauth.google.ConsumerKey") ??
                Environment.GetEnvironmentVariable("GOOGLE_CONSUMER_KEY");
            var googleConsumerSecret = appSettings.Get<string>("oauth.google.ConsumerSecret") ??
                Environment.GetEnvironmentVariable("GOOGLE_CONSUMER_SECRET");
            var facebookAppId = appSettings.Get<string>("oauth.facebook.AppId") ??
                Environment.GetEnvironmentVariable("FACEBOOK_APP_ID");
            var facebookAppSecret = appSettings.Get<string>("oauth.facebook.AppSecret") ??
                Environment.GetEnvironmentVariable("FACEBOOK_APP_SECRET");
            appHost.Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[] {
                    new JwtAuthProvider(appSettings) {
                        AuthKeyBase64 = appSettings.GetString("AUTH_KEY") ?? "cARl12kvS/Ra4moVBIaVsrWwTpXYuZ0mZf/gNLUhDW5=",
                        CreatePayloadFilter = (payload,session) => {
                            payload["ref"] = ((CustomUserSession)session).RefIdStr;
                        },
                        PopulateSessionFilter = (session,payload,req) => {
                            ((CustomUserSession)session).RefIdStr = payload["ref"];
                        },
                        ExpireRefreshTokensIn = TimeSpan.FromDays(90),
                        //RequireSecureConnection = false,
                    },
                    new CredentialsAuthProvider(appSettings),     /* Sign In with Username / Password credentials */
                    new FacebookAuthProvider(appSettings)         /* Create App https://developers.facebook.com/apps */
                    {
                        AppId = facebookAppId,
                        AppSecret = facebookAppSecret,
                        ConsumerKey = facebookAppId,
                        ConsumerSecret = facebookAppSecret
                    },
                    new GoogleAuthProvider(appSettings)           /* Create App https://console.developers.google.com/apis/credentials */
                    {
                        ConsumerKey = googleConsumerKey,
                        ConsumerSecret = googleConsumerSecret
                    },          
                    new MicrosoftGraphAuthProvider(appSettings)   /* Create App https://apps.dev.microsoft.com */
                    {
                        AppId = msAppId,
                        AppSecret = msAppSecret
                    }
                })
            {
                IncludeDefaultLogin = false,
                ValidateRedirectLinks = (ServiceStack.Web.IRequest req, string redirect) => {
                    // Allow external redirect from WASM
                    var allowedRedirects = new[] {
                        "https://blazordiffusion.com",
                        "https://diffusion.works",
                        "https://localhost:5002",
                        "http://localhost:5000",
                        "http://localhost:8080",
                    };
                    if (!allowedRedirects.Any(x => redirect.StartsWith(x)))
                        AuthFeature.NoExternalRedirects(req, redirect);
                }
            });

            appHost.Plugins.Add(new RegistrationFeature()); //Enable /register Service

            //override the default registration validation with your own custom implementation
            appHost.RegisterAs<CustomRegistrationValidator, IValidator<Register>>();
        });
}