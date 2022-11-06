using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.Text;
using NUnit.Framework;
using BlazorDiffusion.ServiceModel;
using ServiceStack.OrmLite;
using BlazorDiffusion.ServiceInterface;
using System.Data;
using ServiceStack.Logging;

namespace BlazorDiffusion.Tests;

[Explicit]
public class ImportTasks
{
    IDbConnectionFactory ResolveDbFactory() => new ConfigureDb().ConfigureAndResolve<IDbConnectionFactory>();
    public string GetHostDir()
    {
        LogManager.LogFactory = new ConsoleLogFactory();

        JsConfig.Init(new Config {
            TextCase = TextCase.CamelCase,
        });

        var appSettings = JSON.parse(File.ReadAllText(Path.GetFullPath("appsettings.json")));
        return appSettings.ToObjectDictionary()["HostDir"].ToString()!;
    }

    [Test]
    public void Sync_missing_modifiers()
    {
        var hostDir = GetHostDir();
        var seedDir = Path.GetFullPath(Path.Combine(hostDir, "App_Data/seed").AssertDir());
        var modifiersTxt = Path.Combine(seedDir, "modifiers.txt");
        
        using var db = ResolveDbFactory().OpenDbConnection();
        var existingModifiers = db.Select<Modifier>();
        var existingModifiersMap = existingModifiers.ToDictionary(x => x.Name);

        foreach (var line in File.ReadAllLines(modifiersTxt))
        {
            var category = line.LeftPart(':').Trim();
            var modifiers = line.RightPart(':').Split(',').Select(x => x.Trim()).ToList();
            foreach (var modifier in modifiers)
            {
                if (existingModifiersMap.ContainsKey(modifier))
                    continue;

                Console.WriteLine($"Adding {modifier} in {category}");
                db.Insert(new Modifier { Name = modifier, Category = category }.BySystemUser());
            }
        }
    
        var lines = ExportModifiers(db);
        File.WriteAllLines(modifiersTxt, lines);
    }

    [Test]
    public void Can_load_scores()
    {
        var dbFactory = ResolveDbFactory();
        using var db = dbFactory.OpenDbConnection();
        Scores.Load(db);
        using var dbAnalytics = dbFactory.OpenDbConnection(Databases.Analytics);
        Scores.Load(dbAnalytics);
    }

    [Test]
    public void Export_Creatives()
    {
        var hostDir = GetHostDir();

        var slnDir = Path.GetFullPath($"../{hostDir}");

        var appFilesDir = Path.GetFullPath(Path.Combine(hostDir, "App_Files"));
        var artifactPaths = Path.GetFullPath(Path.Combine(hostDir, "App_Files/artifacts"));
        var metadataFiles = Directory.GetFiles(artifactPaths, "metadata.json", SearchOption.AllDirectories);

        var dbFactory = ResolveDbFactory();
        using var db = dbFactory.OpenDbConnection();
        Scores.Load(db);
        using var dbAnalytics = dbFactory.OpenDbConnection(Databases.Analytics);
        Scores.LoadAnalytics(dbAnalytics);

        ImageUtils.Log = true;
        var usersMap = db.Dictionary<int, string>(db.From<AppUser>().Select(x => new { x.Id, x.RefIdStr }));

        foreach (var metadataFile in metadataFiles)
        {
            //var key = metadataFile.Replace('\\','/').Substring(artifactPaths.Length).LastLeftPart('/').TrimStart('/');

            // Resave to remove removed columns
            var creative = File.ReadAllText(metadataFile).FromJson<Creative>();

            // Update from DB
            var creativeId = creative.Id;
            creative = db.LoadSelect<Creative>(x => x.RefId == creative.RefId).FirstOrDefault();
            if (creative == null)
            {
                Console.WriteLine($"Creative {creativeId} not found");
            }
            else
            {
                creative.OwnerRef = creative.OwnerId != null ? usersMap[creative.OwnerId.Value] : null;
                creative.EngineId ??= DreamStudioClient.DefaultEngineId;
                foreach (var artifact in creative.Artifacts)
                {
                    Scores.PopulateArtifactScores(artifact);
                    Scores.PopulateTemporalScore(artifact);
                    //artifact.Background = null;
                    //artifact.PerceptualHash = artifact.AverageHash = artifact.DifferenceHash;
                    if (artifact.MissingImageDetails())
                    {
                        var imagePath = Path.Combine(appFilesDir, artifact.FilePath.TrimStart('/'));
                        using var imageStream = File.OpenRead(imagePath);
                        artifact.LoadImageDetails(imageStream);
                        Console.WriteLine();
                    }
                }
                Console.WriteLine($"Updating {metadataFile}...");
                File.WriteAllText(metadataFile, creative.ToJson().IndentJson());
            }
        }

        //Directory.SetCurrentDirectory(slnDir);
        //ProcessUtils.Run("export.bat").Print();
    }

    List<string> ExportModifiers(IDbConnection db)
    {
        var lines = new List<string>();
        var modifiers = db.Select(db.From<Modifier>()
            .SelectDistinct(x => new { x.Name, x.Category }))
            .OrderBy(x => x.Category).ToList();

        var categories = new List<string>();
        AppData.CategoryGroups.Each(x => categories.AddRange(x.Items));

        foreach (var category in categories)
        {
            var categoryModifiers = string.Join(", ", modifiers.Where(x => x.Category == category)
                .OrderBy(x => x.Name).Select(x => x.Name));
            lines.Add($"{(category + ':').PadRight(14, ' ')} {categoryModifiers}");
        }
        return lines;
    }


    [Test]
    public void ExportData()
    {
        var hostDir = GetHostDir();
        
        var testSeedDir = Path.GetFullPath("../../../App_Data/seed");
        var seedDir = Path.GetFullPath(Path.Combine(hostDir, "App_Data/seed").AssertDir());

        using var db = ResolveDbFactory().OpenDbConnection();

        // Export Modifiers
        var lines = ExportModifiers(db);
        File.WriteAllLines(seedDir.CombineWith("modifiers.txt"), lines);
        File.WriteAllLines(testSeedDir.CombineWith("modifiers.txt"), lines);

        // Export Artists
        var artists = db.Select<Artist>()
            .OrderByDescending(x => x.Score);
        artists.Each(x => x.CreatedDate = x.ModifiedDate = DateTime.MinValue);
        string artistsCsv = artists.ToCsv();
        File.WriteAllText(seedDir.CombineWith("artists.csv"), artistsCsv);
        File.WriteAllText(testSeedDir.CombineWith("artists.csv"), artistsCsv);

        var artifactLikes = db.Select<ArtifactLikeRef>(db.From<ArtifactLike>()
            .Join<Artifact>()
            .OrderBy(x => x.Id)
            .Select<ArtifactLike, Artifact>((l,a) => new { a.RefId, l.ArtifactId, l.AppUserId, l.CreatedDate }));
        artifactLikes.Each(x => x.CreatedDate = DateTime.MinValue);
        File.WriteAllText(seedDir.CombineWith("artifact-likes.csv"), artifactLikes.ToCsv());

        // Export Albums
        var artifactRefs = db.Dictionary<int, string>(db.From<Artifact>().Select(x => new { x.Id, x.RefId }));
        var albumIdRefs = db.Dictionary<int, string>(db.From<Album>().Select(x => new { x.Id, x.RefId }));

        var albums = db.LoadSelect<Album>()
            .OrderBy(x => x.Id).ToList();
        albums.Each(a => a.RefId ??= Guid.NewGuid().ToString("D"));
        var albumRefs = albums.Map(x => new AlbumRef
        {
            RefId = x.RefId,
            Name = x.Name,
            Description = x.Description,
            OwnerId = x.OwnerId,
            PrimaryArtifactRef = x.PrimaryArtifactId != null && artifactRefs.TryGetValue(x.PrimaryArtifactId.Value, out var refId) 
                ? refId 
                : null,
            Tags = x.Tags,
        });
        File.WriteAllText(seedDir.CombineWith("albums.csv"), albumRefs.ToCsv());

        albums.Each(x => x.Artifacts ??= new());
        var albumArtifactRefs = albums.SelectMany(x => x.Artifacts.Select(a => new AlbumArtifactRef {
            AlbumRefId = x.RefId,
            ArtifactRefId = artifactRefs[a.ArtifactId],
            Description = a.Description,
        }));
        File.WriteAllText(seedDir.CombineWith("album-artifacts.csv"), albumArtifactRefs.ToCsv());

        var albumLikes = db.Select<AlbumLikeRef>(db.From<AlbumLike>()
            .Join<Album>()
            .OrderBy(x => x.Id)
            .Select<AlbumLike, Album>((l, a) => new { a.RefId, l.AppUserId, l.CreatedDate }));
        albumLikes.Each(x => x.CreatedDate = DateTime.MinValue);
        File.WriteAllText(seedDir.CombineWith("album-likes.csv"), albumLikes.ToCsv());
    }

}
