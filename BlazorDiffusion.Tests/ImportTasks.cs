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

namespace BlazorDiffusion.Tests;

[Explicit]
public class ImportTasks
{
    [Test]
    public void Rename_Artifacts()
    {
        var appSettings = JSON.parse(File.ReadAllText(Path.GetFullPath("appsettings.json")));
        var hostDir = appSettings.ToObjectDictionary()["HostDir"].ToString();
        hostDir.Print();

        var artifactPaths = Path.Combine(hostDir, "App_Files/fs");
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
    public void Rewrite_Artifacts()
    {
        var appSettings = JSON.parse(File.ReadAllText(Path.GetFullPath("appsettings.json")));
        var hostDir = appSettings.ToObjectDictionary()["HostDir"].ToString();
        hostDir.Print();

        var artifactPaths = Path.Combine(hostDir, "App_Files/fs");
        var metadataFiles = Directory.GetFiles(artifactPaths, "metadata.json", SearchOption.AllDirectories);
        foreach (var metadataFile in metadataFiles)
        {
            var key = metadataFile.Replace('\\','/').Substring(artifactPaths.Length).LastLeftPart('/').TrimStart('/');

            var creative = File.ReadAllText(metadataFile).FromJson<Creative>();
            creative.Key = key;
            foreach (var artifact in creative.Artifacts)
            {
                artifact.FilePath = $"/uploads/fs/{key}/{artifact.FileName}";
            }
            //creative.ToJson().Print();
            File.WriteAllText(metadataFile, creative.ToJson());
        }
    }

}
