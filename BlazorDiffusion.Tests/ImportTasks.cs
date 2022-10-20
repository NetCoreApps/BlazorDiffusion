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
    public void Rewrite_Creatives()
    {
        var hostDir = GetHostDir();

        var artifactPaths = Path.Combine(hostDir, "App_Files/artifacts");
        var metadataFiles = Directory.GetFiles(artifactPaths, "metadata.json", SearchOption.AllDirectories);

        foreach (var metadataFile in metadataFiles)
        {
            //var key = metadataFile.Replace('\\','/').Substring(artifactPaths.Length).LastLeftPart('/').TrimStart('/');

            // Resave to remove removed columns
            var creative = File.ReadAllText(metadataFile).FromJson<Creative>();
            creative.Artifacts.Each(x => x.Nsfw = null);

            File.WriteAllText(metadataFile, creative.ToJson().IndentJson());
        }
    }


    [Test]
    public void ExportData()
    {
        var hostDir = GetHostDir();
        
        var seedDir = Path.GetFullPath(Path.Combine(hostDir, "App_Data/seed").AssertDir());

        using var db = ResolveDbFactory().OpenDbConnection();

        // Export Modifiers
        var lines = new List<string>();
        var modifiers = db.Select<Modifier>().OrderBy(x => x.Category).ToList();

        var categories = new List<string>();
        DataService.CategoryGroups.Each(x => categories.AddRange(x.Items));

        foreach (var category in categories)
        {
            var categoryModifiers = string.Join(", ", modifiers.Where(x => x.Category == category).OrderBy(x => x.Name).Select(x => x.Name));
            lines.Add($"{(category + ':').PadRight(14, ' ')} {categoryModifiers}");
        }
        File.WriteAllLines(seedDir.CombineWith("modifiers.txt"), lines);

        // Export Artists
        var artists = db.Select<Artist>();
        File.WriteAllText(seedDir.CombineWith("artists.csv"), artists.ToCsv());
    }

}
