using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlazorDiffusion.Migrations;
using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.Tests;

public class CreativeServiceTests
{
    const string BaseUri = "http://localhost:2000/";
    private readonly ServiceStackHost appHost;

    class AppHost : AppSelfHostBase
    {
        public AppHost() : base(nameof(CreativeServiceTests), typeof(MyServices).Assembly) { }

        Migrator CreateMigrator() => new(ResolveDbFactory(), typeof(Migration1000).Assembly); 
        IDbConnectionFactory ResolveDbFactory() => this.Resolve<IDbConnectionFactory>();

        
        public override void Configure(Container container)
        {
            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory("db.sqlite",
                    SqliteDialect.Provider));
            
            var migrator = CreateMigrator();
            var revert = migrator.Revert(Migrator.All);
            Assert.That(revert.Succeeded);
            var migrate = migrator.Run();
            Assert.That(migrate.Succeeded);

            var appSettings = this.AppSettings;
            this.Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[] {
                    new CredentialsAuthProvider(appSettings)
                })
            {
                IncludeDefaultLogin = false
            });
            
            container.AddSingleton<IAuthRepository>(c =>
                new OrmLiteAuthRepository<AppUser, UserAuthDetails>(c.Resolve<IDbConnectionFactory>())
                {
                    UseDistinctRoleTables = true
                });
            
            var authRepo = container.Resolve<IAuthRepository>();
            authRepo.InitSchema();
            CreateUser(authRepo, "admin@email.com", "Admin User", "p@55wOrd", roles: new[] { RoleNames.Admin });

            this.Plugins.Add(new AutoQueryFeature {
                MaxLimit = 1000,
                //IncludeTotal = true,
            });
            
            container.Register<IStableDiffusionClient>(new DreamStudioClient
            {
                ApiKey = Environment.GetEnvironmentVariable("DREAMAI_APIKEY") ?? "<your_api_key>",
                OutputPathPrefix = Path.Join(ContentRootDirectory.RealPath.CombineWith("App_Files"),"fs")
            });
            container.AddSingleton<ICrudEvents>(c =>
                new OrmLiteCrudEvents(c.Resolve<IDbConnectionFactory>()));
            container.Resolve<ICrudEvents>().InitSchema();
        }
    }
    
    // Add initial Users to the configured Auth Repository
    public static void CreateUser(IAuthRepository authRepo, string email, string name, string password, string[] roles)
    {
        if (authRepo.GetUserAuthByUserName(email) == null)
        {
            var newAdmin = new AppUser { Email = email, DisplayName = name };
            var user = authRepo.CreateUserAuth(newAdmin, password);
            authRepo.AssignRoles(user, roles);
        }
    }

    public CreativeServiceTests()
    {
        BlazorDiffusion.AppHost.RegisterKey();
        appHost = new AppHost()
            .Init()
            .Start(BaseUri);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    public IServiceClient CreateClient() => new JsonServiceClient(BaseUri);

    public static List<ImageGenerationTestCase> AllGenerationCases = new()
    {
        // new ImageGenerationTestCase
        // {
        //     UserPrompt = "A broken down building in a stunning landscape, overgrowth of vegetation",
        //     ModifierNames = new() {"3D","Bloom light effect","CryEngine"},
        //     ArtistsType = "3d"
        // },
        // new ImageGenerationTestCase
        // {
        //     UserPrompt = "A portrait of a character in a scenic environment",
        //     ModifierNames = new() {"dystopian","Bleak"}
        // },
        new ImageGenerationTestCase
        {
            UserPrompt = "A portrait of a Lara Croft in a scenic environment",
            ModifierNames = new() {"beautiful", "HQ", "hyper detailed", "overgrown","cityscape", "4k","CryEngine"}
        }
    };
    
    [Test]
    [TestCaseSource("AllGenerationCases")]
    [Explicit]
    public void Can_generate_images(ImageGenerationTestCase testCase)
    {
        var client = CreateClient();

        var authResponse = client.Post(new Authenticate("credentials")
        {
            UserName = "admin@email.com",
            Password = "p@55wOrd"
        });
        
        using var db = appHost.Resolve<IDbConnectionFactory>().OpenDbConnection();
        var modifiers = 
            db.Select<Modifier>(x => Sql.In(x.Name,
                testCase.ModifierNames));
        var artists = testCase.ArtistsType == null ? new List<Artist>() :
            db.Select<Artist>($"select * from Artist where Type like '%{testCase.ArtistsType}%'");

        var artistsIds = artists.Select(x => x.Id).ToList();
        var modifierIds = modifiers.Select(x => x.Id).ToList();
        
        var response = client.Post(new CreateCreative()
        {
            UserPrompt = testCase.UserPrompt,
            ArtistIds = artistsIds,
            ModifierIds = modifierIds,
            Images = 6
        });

        Assert.That(response, Is.Not.Null);
    }
}

public class ImageGenerationTestCase
{
    public string UserPrompt { get; set; }
    public string? ArtistsType { get; set; }
    public List<string> ModifierNames { get; set; }
}