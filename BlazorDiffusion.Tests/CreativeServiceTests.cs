using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            migrator.Timeout = TimeSpan.Zero;
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
                OutputPathPrefix = Path.Join(ContentRootDirectory.RealPath.CombineWith("App_Files"),"artifacts")
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
        new ImageGenerationTestCase
        {
            UserPrompt = "A broken down building in a stunning landscape, overgrowth of vegetation",
            ModifierNames = new() {"3D","Bloom light effect","CryEngine"},
            ArtistsType = "3d",
        },
        // new ImageGenerationTestCase
        // {
        //     UserPrompt = "A portrait of a character in a scenic environment",
        //     ModifierNames = new() {"Dystopian","Bleak"},
        //     ImageType = ImageType.Square,
        //     Steps = 10
        // },
        // new ImageGenerationTestCase
        // {
        //     UserPrompt = "A portrait of Lara Croft in a scenic environment",
        //     ModifierNames = new() {"Beautiful", "HQ", "Hyper Detailed", "Overgrown","Cityscape", "4k","CryEngine"},
        //     ImageType = ImageType.Square,
        //     Steps = 10
        // },
        //
        // new ImageGenerationTestCase
        // {
        //     UserPrompt = "A portrait of Aloy from the Horizon video game in a scenic environment",
        //     ModifierNames = new() {"Beautiful", "HQ", "Hyper Detailed", "Overgrown","Cityscape", "4k","CryEngine"},
        //     ImageType = ImageType.Landscape,
        //     Steps = 10
        // },
        // new ImageGenerationTestCase
        // {
        //     UserPrompt = "outside of a futuristic gothic cathedral with leds",
        //     ModifierNames = new() {"Beautiful", "HQ", "Hyper Detailed", "Overgrown","Cityscape", "4k","CryEngine"},
        //     ImageType = ImageType.Landscape,
        //     Steps = 10
        // },
        // new ImageGenerationTestCase
        // {
        //     UserPrompt = "Portrait of a ruggedly handsome paladin, soft hair, muscular, half body, masculine, mature",
        //     ModifierNames = new() {"Beautiful", "HQ", "Hyper Detailed", "Digital Illustration","Concept Art", "4k"},
        //     ImageType = ImageType.Portrait,
        //     Steps = 10
        // },
        // new ImageGenerationTestCase
        // {
        //     UserPrompt = "Floating spooky house in the sky",
        //     ModifierNames = new() {"Low Poly", "3D Rendering", "Hyper Detailed", "Isometric","Sharp Focus", "Ray Tracing"},
        //     ImageType = ImageType.Square,
        //     Steps = 10
        // }
    };
    
    [Test]
    [TestCaseSource("AllGenerationCases")]
    [Explicit]
    public void Can_generate_images(ImageGenerationTestCase testCase)
    {
        var client = CreateClient();

        var authResponse = client.Send(new Authenticate("credentials")
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

        if (artists == null && testCase.ArtistNames.Count > 0)
        {
            artists = new List<Artist>();
            foreach (var name in testCase.ArtistNames)
            {
                var nameSplit = name.Split(" ");
                var firstName = nameSplit.Length > 1 ? nameSplit[0] : null;
                var lastName = nameSplit.Length > 1 ? nameSplit[1] : name;
                var artist = db.Select<Artist>(x => x.FirstName == firstName && x.LastName == lastName)
                    .FirstOrDefault();
                if(artist != null)
                    artists.Add(artist);
            }
        }
        
        artists ??= new List<Artist>();
        
        var artistsIds = artists.Select(x => x.Id).ToList();
        var modifierIds = modifiers.Select(x => x.Id).ToList();

        var dimensions = GetDimensions(testCase.ImageType);
        var numberOfImages = testCase.Images ?? 4;
        var response = client.Send(new CreateCreative
        {
            UserPrompt = testCase.UserPrompt,
            ArtistIds = artistsIds,
            ModifierIds = modifierIds,
            Images = numberOfImages,
            Width = dimensions.Width,
            Height = dimensions.Height,
            Steps = testCase.Steps ?? 25
        });
        
        var creative = response.Result;
        Assert.That(creative, Is.Not.Null);
        Assert.That(creative.Artifacts, Is.Not.Null);
        Assert.That(creative.Artifacts.Count, Is.EqualTo(numberOfImages));

        var primaryArtifactResponse = client.Send(new UpdateCreative
        {
            Id = creative.Id,
            PrimaryArtifactId = creative.Artifacts[0].Id
        });

        var nsfwArtifactResponse = client.Send(new UpdateArtifact
        {
            Id = creative.Artifacts[1].Id,
            Nsfw = true
        });
        
        Assert.That(primaryArtifactResponse, Is.Not.Null);
        Assert.That(nsfwArtifactResponse, Is.Not.Null);

        var queryResponse = client.Get(new QueryCreatives
        {
            Id = creative.Id
        });
        
        Assert.That(queryResponse, Is.Not.Null);
        Assert.That(queryResponse.Results, Is.Not.Null);
        Assert.That(queryResponse.Results.Count, Is.EqualTo(1));
        Assert.That(queryResponse.Results[0].OwnerId, Is.Not.Null);
        Assert.That(queryResponse.Results[0].OwnerId, Is.GreaterThan(0));
        Assert.That(queryResponse.Results[0].CreatedBy, Is.EqualTo("1"));
    }
    
    public static ImageSize GetDimensions(ImageType orientation)
    {
        switch (orientation)
        {
            case ImageType.Landscape:
                return new ImageSize(896, 512);
            case ImageType.Portrait:
                return new ImageSize(512, 896);
            case ImageType.Square:
            default:
                return new ImageSize(512, 512);
        }
    }

}

public enum ImageType
{
    Square,
    Portrait,
    Landscape
}



public class ImageGenerationTestCase
{
    public ImageGenerationTestCase()
    {
        ArtistNames = new();
        ModifierNames = new();
    }
    public string UserPrompt { get; set; }
    public string? ArtistsType { get; set; }
    public List<string> ArtistNames { get; set; }
    public List<string> ModifierNames { get; set; }
    public ImageType ImageType { get; set; }
    public int? Images { get; set; }
    public int? Steps { get; set; }
}