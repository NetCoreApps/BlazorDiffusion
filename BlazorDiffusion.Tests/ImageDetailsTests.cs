using BlazorDiffusion.Pages.admin;
using BlazorDiffusion.ServiceModel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorDiffusion.Tests;

[Explicit]
public class ImageDetailsTests
{
    public string GetHostDir()
    {
        JsConfig.Init(new Config
        {
            TextCase = TextCase.CamelCase,
        });

        var appSettings = JSON.parse(File.ReadAllText(Path.GetFullPath("appsettings.json")));
        return appSettings.ToObjectDictionary()["HostDir"].ToString()!;
    }

    [Test]
    public void Can_calculate_ImageDetails()
    {
        var appFilesDir = Path.GetFullPath("../../../App_Files");
        //appFiles.Print();
        ImageDetails.Log = true;

        var testImages = new DirectoryInfo(appFilesDir).GetFiles("*.png", SearchOption.AllDirectories);
        foreach (var testImage in testImages)
        {
            $"{testImage.FullName}".Print();
            var imgBytes = testImage.ReadFully();
            var imageDetails = ImageDetails.Calculate(imgBytes);
            imageDetails.PrintDump();
        }
    }

    [Test]
    public void Compare_calculating_dominant_color()
    {
        var hostDir = GetHostDir();

        var appFilesDir = Path.GetFullPath(Path.Combine(hostDir, "App_Files"));
        var testDir = Path.Combine(appFilesDir, "artifacts/2022/10/22/60784937");
        var imageFiles = new DirectoryInfo(testDir).GetFiles("*.png", SearchOption.AllDirectories);

        foreach (var testImage in imageFiles)
        {
            $"{testImage.FullName}".Print();
            var imageBytes = testImage.ReadFully();
            using var image = Image.Load<Rgba32>(imageBytes);

            var sw = Stopwatch.StartNew();
            //var avgColor = image.CalculateDominantColor();
            image.ResizeImage(new Size(100, 0));
            var avgColor = image.CalculateAverageColor();
            $"{avgColor} #{avgColor.ToHex()} took {sw.ElapsedMilliseconds}ms".Print();
        }
        appFilesDir.Print();
    }
}
