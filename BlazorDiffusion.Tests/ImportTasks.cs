using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.Text;
using NUnit.Framework;
using BlazorDiffusion.ServiceModel;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using BlazorDiffusion.ServiceInterface;
using AngleSharp.Diffing.Extensions;
using System.Data;

namespace BlazorDiffusion.Tests;

[Explicit]
public class ImportTasks
{
    IDbConnectionFactory ResolveDbFactory() => new ConfigureDb().ConfigureAndResolve<IDbConnectionFactory>();
    public string GetHostDir()
    {
        var appSettings = JSON.parse(File.ReadAllText(Path.GetFullPath("appsettings.json")));
        return appSettings.ToObjectDictionary()["HostDir"].ToString()!;
    }

    [Test]
    public void Rename_Artifacts()
    {
        var hostDir = GetHostDir();

        var artifactPaths = Path.Combine(hostDir, "App_Files/artifacts");
        foreach (var dir in Directory.GetDirectories(artifactPaths))
        {
            var dirInfo = new DirectoryInfo(dir);
            dirInfo.FullName.Print();
            string metadataPath = Path.Combine(dirInfo.FullName, "metadata.json");

            var creative = File.ReadAllText(metadataPath).FromJson<Creative>();
            var key = $"{creative.CreatedDate:yyyy/MM/dd}/{(long)creative.CreatedDate.TimeOfDay.TotalMilliseconds}";
            creative.Key = key;
            var vfsDirPath = $"/uploads/fs/{key}";
            foreach (var artifact in creative.Artifacts)
            {
                artifact.FilePath = vfsDirPath.CombineWith(artifact.FileName);
            }
            //File.WriteAllText(metadataPath, creative.ToJson());
            creative.PrintDump();

            var newDirPath = dirInfo.Parent!.FullName.CombineWith(key);
            $"{dirInfo.Name} -> {newDirPath}".Print();
            FileSystemVirtualFiles.AssertDirectory(newDirPath);
            Directory.Delete(newDirPath);
            Directory.Move(dirInfo.FullName, newDirPath);
        }
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
    public void Export_Creatives()
    {
        var hostDir = GetHostDir();

        var slnDir = Path.GetFullPath($"../{hostDir}");

        var artifactPaths = Path.GetFullPath(Path.Combine(hostDir, "App_Files/artifacts"));
        var metadataFiles = Directory.GetFiles(artifactPaths, "metadata.json", SearchOption.AllDirectories);

        using var db = ResolveDbFactory().OpenDbConnection();

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
                //foreach (var artifact in creative.Artifacts)
                //{
                //    artifact.RefId = Guid.NewGuid().ToString("D");
                //}
                Console.WriteLine($"Updating {metadataFile}...");
                File.WriteAllText(metadataFile, creative.ToJson().IndentJson());
            }
        }

        Directory.SetCurrentDirectory(slnDir);
        ProcessUtils.Run("export.bat").Print();
    }

    List<string> ExportModifiers(IDbConnection db)
    {
        var lines = new List<string>();
        var modifiers = db.Select(db.From<Modifier>().SelectDistinct(x => new { x.Name, x.Category })).OrderBy(x => x.Category).ToList();

        var categories = new List<string>();
        DataService.CategoryGroups.Each(x => categories.AddRange(x.Items));

        foreach (var category in categories)
        {
            var categoryModifiers = string.Join(", ", modifiers.Where(x => x.Category == category).OrderBy(x => x.Name).Select(x => x.Name));
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
        var artists = db.Select<Artist>();
        artists.Each(x => x.CreatedDate = x.ModifiedDate = DateTime.MinValue);
        string artistsCsv = artists.ToCsv();
        File.WriteAllText(seedDir.CombineWith("artists.csv"), artistsCsv);
        File.WriteAllText(testSeedDir.CombineWith("artists.csv"), artistsCsv);

        var artifactLikes = db.Select<ArtifactLikeRef>(db.From<ArtifactLike>()
            .Join<Artifact>()
            .Select<ArtifactLike, Artifact>((l,a) => new { a.RefId, l.ArtifactId, l.AppUserId, l.CreatedDate }));
        artifactLikes.Each(x => x.CreatedDate = DateTime.MinValue);
        File.WriteAllText(seedDir.CombineWith("artifact-likes.csv"), artifactLikes.ToCsv());
    }

}
