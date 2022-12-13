using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.Web;
using ServiceStack.Data;
using ServiceStack.Html;
using ServiceStack.Auth;
using ServiceStack.Configuration;

[assembly: HostingStartup(typeof(BlazorDiffusion.ConfigureAuthRepository))]

namespace BlazorDiffusion;

public enum Department
{
    None,
    Marketing,
    Accounts,
    Legal,
    HumanResources,
}


public class AppUserAuthEvents : AuthEvents
{

    public override async Task OnAuthenticatedAsync(IRequest httpReq, IAuthSession session, IServiceBase authService,
        IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token = default)
    {
        var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(httpReq);
        using (authRepo as IDisposable)
        {
            var userAuth = (AppUser)await authRepo.GetUserAuthAsync(session.UserAuthId, token);
            userAuth.RefIdStr ??= Guid.NewGuid().ToString();
            userAuth.ProfileUrl = session.ProfileUrl = session.GetProfileUrl(Icons.AnonUserUri);
            userAuth.LastLoginIp = httpReq.UserHostAddress;
            userAuth.LastLoginDate = DateTime.UtcNow;
            await authRepo.SaveUserAuthAsync(userAuth, token);
        }
    }
}

public class ConfigureAuthRepository : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => services.AddSingleton<IAuthRepository>(c =>
            new OrmLiteAuthRepository<AppUser, UserAuthDetails>(c.Resolve<IDbConnectionFactory>()) {
                UseDistinctRoleTables = true
            }))
        .ConfigureAppHost(appHost => {
            var authRepo = appHost.Resolve<IAuthRepository>();
            authRepo.InitSchema();

            // Removing unused UserName in Admin Users UI 
            appHost.Plugins.Add(new ServiceStack.Admin.AdminUsersFeature {
                
                // Show custom fields in Search Results
                QueryUserAuthProperties = new() {
                    nameof(AppUser.Id),
                    nameof(AppUser.RefIdStr),
                    nameof(AppUser.Email),
                    nameof(AppUser.DisplayName),
                    nameof(AppUser.CreatedDate),
                    nameof(AppUser.LastLoginDate),
                },

                QueryMediaRules = new()
                {
                    MediaRules.ExtraSmall.Show<AppUser>(x => new { x.Id, x.Email, x.DisplayName }),
                },

                // Add Custom Fields to Create/Edit User Forms
                FormLayout = new() {
                    Input.For<AppUser>(x => x.Email),
                    Input.For<AppUser>(x => x.DisplayName),
                    Input.For<AppUser>(x => x.RefIdStr),
                    Input.For<AppUser>(x => x.Company),
                    Input.For<AppUser>(x => x.PhoneNumber, c => {
                        c.Type = Input.Types.Tel;
                        c.FieldsPerRow(2);
                    }),
                    Input.For<AppUser>(x => x.Nickname, c => {
                        c.Help = "Public alias (3-12 lower alpha numeric chars)";
                        c.Pattern = "^[a-z][a-z0-9_.-]{3,12}$";
                        //c.Required = true;
                    }),
                    Input.For<AppUser>(x => x.ProfileUrl, c => c.Type = Input.Types.Url),
                    Input.For<AppUser>(x => x.Avatar, c => c.Type = Input.Types.Url),
                    Input.For<AppUser>(x => x.IsArchived), Input.For<AppUser>(x => x.ArchivedDate),
                }
            });

        },
        afterPluginsLoaded: appHost => {
            //var anonUser = Svg.ToDataUri(Svg.Fill(Icons.AnonUser, "#0891B2"));
            ((AuthMetadataProvider)appHost.Resolve<IAuthMetadataProvider>()).NoProfileImgUrl = Icons.AnonUserUri;
            appHost.AssertPlugin<AuthFeature>().AuthEvents.Add(new AppUserAuthEvents());
        });
}
