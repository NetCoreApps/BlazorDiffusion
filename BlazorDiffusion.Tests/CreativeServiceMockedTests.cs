﻿using System;
using System.Collections.Generic;
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

public class CreativeServiceMockedTests
{
    class AppHost : AppSelfHostBase
    {
        public AppHost() : base(nameof(CreativeServiceTests), typeof(MyServices).Assembly) { }

        Migrator CreateMigrator() => new(ResolveDbFactory(), typeof(Migration1000).Assembly); 
        IDbConnectionFactory ResolveDbFactory() => this.Resolve<IDbConnectionFactory>();

        public class MockStableDiffusionClient : IStableDiffusionClient
        {
            public async Task<ImageGenerationResponse> GenerateImageAsync(ImageGeneration request)
            {
                return new ImageGenerationResponse
                {
                    Results = new List<ImageGenerationResult>
                    {
                        new()
                        {
                            Height = request.Height,
                            Width = request.Width,
                            Prompt = request.Prompt,
                            Seed = 12345,
                            AnswerId = "54321",
                            ContentLength = 1234,
                            FileName = "output_12345.png",
                            FilePath = "/blah/output_12345.png"
                        }
                    }
                };
            }

            public async Task SaveMetadata(ImageGenerationResponse response, Creative entry)
            {
                
            }
        }
        
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
            CreativeServiceTests.CreateUser(authRepo, "admin@email.com", "Admin User", "p@55wOrd", roles: new[] { RoleNames.Admin });

            this.Plugins.Add(new AutoQueryFeature {
                MaxLimit = 1000,
                //IncludeTotal = true,
            });
            
            container.Register<IStableDiffusionClient>(new MockStableDiffusionClient());
            container.AddSingleton<ICrudEvents>(c =>
                new OrmLiteCrudEvents(c.Resolve<IDbConnectionFactory>()));
            container.Resolve<ICrudEvents>().InitSchema();
        }
    }

    public CreativeServiceMockedTests()
    {
        BlazorDiffusion.AppHost.RegisterKey();
        appHost = new AppHost()
            .Init()
            .Start(BaseUri);
    }
    
    const string BaseUri = "http://localhost:2001/";
    private readonly ServiceStackHost appHost;
    public IServiceClient CreateClient() => new JsonServiceClient(BaseUri);
    
    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();
    
    public static List<ImageGenerationTestCase> AllGenerationCases = new()
    {
        new ImageGenerationTestCase
        {
            UserPrompt = "Floating spooky house in the sky",
            ModifierNames = new() {"Low Poly", "3D Rendering", "Hyper Detailed", "Isometric","Sharp Focus", "Ray Tracing"},
            ImageType = ImageType.Square
        }
    };
    
    [Test]
    [TestCaseSource("AllGenerationCases")]
    public void Can_generate_images_mocked(ImageGenerationTestCase testCase)
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

        var dimensions = CreativeServiceTests.GetDimensions(testCase.ImageType);
        var numberOfImages = 1;
        var response = client.Post(new CreateCreative()
        {
            UserPrompt = testCase.UserPrompt,
            ArtistIds = artistsIds,
            ModifierIds = modifierIds,
            Images = numberOfImages,
            Width = dimensions.Width,
            Height = dimensions.Height
        });

        Assert.That(response, Is.Not.Null);
        Assert.That(response.Artifacts, Is.Not.Null);
        Assert.That(response.Artifacts.Count, Is.EqualTo(numberOfImages));

        var primaryArtifactResponse = client.Post(new UpdateCreative
        {
            Id = response.Id,
            PrimaryArtifactId = response.Artifacts[0].Id
        });

        var nsfwArtifactResponse = client.Post(new UpdateCreativeArtifact
        {
            Id = response.Artifacts[0].Id,
            Nsfw = true
        });
        
        Assert.That(primaryArtifactResponse, Is.Not.Null);
        Assert.That(nsfwArtifactResponse, Is.Not.Null);

        var queryResponse = client.Get(new QueryCreatives
        {
            Id = response.Id
        });
        
        Assert.That(queryResponse, Is.Not.Null);
        Assert.That(queryResponse.Results, Is.Not.Null);
        Assert.That(queryResponse.Results.Count, Is.EqualTo(1));
        Assert.That(queryResponse.Results[0].AppUserId, Is.Not.Null);
        Assert.That(queryResponse.Results[0].AppUserId, Is.GreaterThan(0));
        Assert.That(queryResponse.Results[0].CreatedBy, Is.EqualTo("1"));
    }
}