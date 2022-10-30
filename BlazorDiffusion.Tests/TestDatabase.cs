using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.OrmLite;
using BlazorDiffusion.ServiceModel;
using System.Diagnostics;
using ServiceStack.Configuration;
using ServiceStack.Auth;
using ServiceStack.Data;

namespace BlazorDiffusion.Tests;

public static class TestDatabase
{
    private static string ConstructPrompt(string userPrompt, List<string> modifiers, List<string> artists)
    {
        var finalPrompt = userPrompt;
        finalPrompt += $", {modifiers.Select(x => x).Join(",").TrimEnd(',')}";
        var artistsSuffix = artists.Select(x => $"inspired by {x}").Join(",").TrimEnd(',');
        if (artists.Count > 0)
            finalPrompt += $", {artistsSuffix}";
        return finalPrompt;
    }

    public static void CreateDatabase(IDbConnectionFactory dbFactory, IDbConnection Db, string hostDir)
    {
        var swTotal = Stopwatch.StartNew();
        var sw = Stopwatch.StartNew();

        var authRepo = new OrmLiteAuthRepository<AppUser, UserAuthDetails>(dbFactory) { UseDistinctRoleTables = true };
        authRepo.InitSchema(Db);

        void CreateUser(string email, string name, string password, string refId, string[]? roles = null)
        {
            var newAdmin = new AppUser { Email = email, DisplayName = name, RefIdStr = refId };
            var user = authRepo.CreateUserAuth(Db, newAdmin, password);
            if (roles?.Length > 0)
            {
                authRepo.AssignRoles(Db, user.Id.ToString(), roles);
            }
        }

        CreateUser("admin@email.com", "Admin User", "p@55wOrd", "b496e043-3e5b-4410-b0e5-1c9cca04c07f", roles: new[] { RoleNames.Admin });
        CreateUser("system@email.com", "System", "p@55wOrd", "cd1bbe7e-2038-4b43-9086-32c790485588", roles: new[] { AppRoles.Moderator });
        CreateUser("demis@servicestack.com", "Demis", "p@55wOrd", "865d5f4a-4c58-461d-b1b8-2aac005cd2bc", roles: new[] { AppRoles.Moderator });
        CreateUser("darren@servicestack.com", "Darren", "p@55wOrd", "16846ea4-2bb6-4c58-a999-985dac3c31a2", roles: new[] { AppRoles.Moderator });
        CreateUser("test@user.com", "Test", "p@55wOrd", "3823c5af-d0b6-4738-8601-bd91bf6f9771");

        Console.WriteLine($"Creating Users took {sw.ElapsedMilliseconds}ms");
        sw.Restart();


        Db.CreateTable<Artist>();
        Db.CreateTable<Modifier>();
        Db.CreateTable<Creative>();
        Db.CreateTable<CreativeArtist>();
        Db.CreateTable<CreativeModifier>();
        Db.CreateTable<Artifact>();
        Db.CreateTable<ArtifactLike>();
        Db.CreateTable<ArtifactReport>();
        Db.CreateTable<Album>();
        Db.CreateTable<AlbumArtifact>();
        Db.CreateTable<AlbumLike>();
        Db.CreateTable<ArtifactStat>();
        Db.CreateTable<SearchStat>();

        Console.WriteLine($"Creating tables took {sw.ElapsedMilliseconds}ms");
        sw.Restart();

        var appDataDir = Path.Combine(hostDir, "App_Data");
        var seedDir = Path.Combine(appDataDir, "seed");
        var appFilesDir = Path.Combine(hostDir, "App_Files");


        // Import Modifiers
        foreach (var line in File.ReadAllLines(seedDir.CombineWith("modifiers.txt")))
        {
            var category = line.LeftPart(':').Trim();
            var modifiers = line.RightPart(':').Split(',').Select(x => x.Trim()).ToList();
            foreach (var modifier in modifiers)
            {
                Db.Insert(new Modifier { Name = modifier, Category = category }.BySystemUser());
            }
        }
        /// Check for duplicates
        var savedModifiers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var allMods = Db.Select<Modifier>();
        foreach (var modifier in allMods)
        {
            var isUnique = savedModifiers.TryAdd(modifier.Name.ToLowerInvariant(), modifier.Id);
            if (!isUnique)
                Console.WriteLine($"Duplicate - {modifier.Category}/{modifier.Name.ToLowerInvariant()}");
        }

        // Import Artists
        var Artists = File.ReadAllText(seedDir.CombineWith("artists.csv")).FromCsv<List<Artist>>().Select(x => x.BySystemUser()).ToList();
        Db.InsertAll(Artists);
        /// Check for duplicates
        var savedArtistIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var allArtists = Db.Select<Artist>();
        foreach (var a in allArtists)
        {
            var isUnique = savedArtistIds.TryAdd($"{a.FirstName} {a.LastName}".ToLowerInvariant(), a.Id);
            if (!isUnique)
                Console.WriteLine($"Duplicate - {a.FirstName} {a.LastName}");
        }

        Console.WriteLine($"Populating modifiers.txt + artists.csv took {sw.ElapsedMilliseconds}ms");
        sw.Restart();


        // When needing to match on Artifact Ids
        var artifactRefIdsMap = new Dictionary<string, int>();
        var artifactPaths = Path.GetFullPath(Path.Combine(appFilesDir, "artifacts"));
        var artifactPathsDir = new DirectoryInfo(artifactPaths);
        if (!artifactPathsDir.Exists)
            throw new Exception($"{artifactPathsDir.FullName} does not exist");

        // Import Creatives + Artifacts
        var allArtifactLikeRefs = File.ReadAllText(seedDir.CombineWith("artifact-likes.csv")).FromCsv<List<ArtifactLikeRef>>();
        var appFiles = new DirectoryInfo(appFilesDir);
        if (!appFiles.Exists)
            appFiles.Create();
        var filesToLoad = artifactPathsDir.GetMatchingFiles("*metadata.json");
        var creatives = new List<Creative>();
        foreach (var file in filesToLoad)
        {
            var creative = File.ReadAllText(file).FromJson<Creative>();
            creative.Prompt = ConstructPrompt(creative.UserPrompt, creative.ModifierNames, creative.ArtistNames);
            creatives.Add(creative);
        }

        var metadataFiles = Directory.GetFiles(artifactPaths, "metadata.json", SearchOption.AllDirectories);
        foreach (var metadataFile in metadataFiles)
        {
            var creative = File.ReadAllText(metadataFile).FromJson<Creative>();
            creative.Modifiers = new List<CreativeModifier>();
            creative.ModifierNames ??= new List<string>();
            creative.ArtistNames ??= new List<string>();
            creative.OwnerId = creative.CreatedBy != null ? creative.CreatedBy.ToInt() : 2;
            var id = creative.Id = (int)Db.Insert(creative, selectIdentity: true);
            foreach (var text in creative.ModifierNames)
            {
                var mod = savedModifiers[text.ToLowerInvariant()];
                Db.Insert(new CreativeModifier {
                    ModifierId = mod,
                    CreativeId = id
                });
            }

            foreach (var artistName in creative.ArtistNames)
            {
                if (savedArtistIds.TryGetValue(artistName, out var artist))
                {
                    Db.Insert(new CreativeArtist
                    {
                        ArtistId = artist,
                        CreativeId = id
                    });
                }
                else
                {
                    Console.WriteLine($"NotFound: Artist {artistName}");
                }
            }

            var primaryArtifact = creative.PrimaryArtifactId != null
                ? creative.Artifacts.FirstOrDefault(x => x.Id == creative.PrimaryArtifactId)
                : null;

            foreach (var artifact in creative.Artifacts)
            {
                artifact.Id = 0;
                artifact.CreativeId = id;
                var filePath = $"./App_Files/{artifact.FilePath}";
                artifact.Id = (int)Db.Insert(artifact, selectIdentity: true);
                artifactRefIdsMap[artifact.RefId] = artifact.Id;

                if (artifact == primaryArtifact)
                {
                    creative.PrimaryArtifactId = artifact.Id;
                    Db.UpdateOnly(() => new Creative { PrimaryArtifactId = artifact.Id },
                        where: x => x.Id == creative.Id);
                }

                // Add ArtifactLikes
                var artifactLikeRefs = allArtifactLikeRefs.Where(x => x.RefId == artifact.RefId).ToList();
                if (artifactLikeRefs.Count > 0)
                {
                    foreach (var artifactLikeRef in artifactLikeRefs)
                    {
                        var artifactLike = X.Apply(artifactLikeRef.ConvertTo<ArtifactLike>(), x => x.ArtifactId = artifact.Id);
                        Db.Insert(artifactLike);
                    }
                }
            }
        }

        Console.WriteLine($"Populating Creatives took {sw.ElapsedMilliseconds}ms");
        sw.Restart();

        Console.WriteLine($"Creating database took {swTotal.ElapsedMilliseconds}ms");
    }

}
