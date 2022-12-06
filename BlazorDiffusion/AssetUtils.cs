using System.IO.Compression;
using ServiceStack.OrmLite;

namespace BlazorDiffusion;

public static class AssetUtils
{
    public static void FetchDemoAssets(this Migrator migrator)
    {
        using var zipStream = "https://github.com/NetCoreApps/BlazorDiffusionAssets/archive/refs/heads/master.zip"
            .GetStreamFromUrl();
        var zipArchive = new ZipArchive(zipStream);
        var appFiles = new DirectoryInfo("./App_Files");
        foreach (var entry in zipArchive.Entries)
        {
            using var stream = entry.Open();
            var relPath = entry.FullName.SplitOnFirst("/")[1];
            var path = Path.Combine(appFiles.FullName, relPath);
            if (entry.Length == 0)
            {
                if(!Directory.Exists(path))
                    Directory.CreateDirectory(path); 
                continue;
            }
            File.WriteAllBytes(Path.Combine(appFiles.FullName,relPath), entry.Open().ReadFully());
        }
    }
}