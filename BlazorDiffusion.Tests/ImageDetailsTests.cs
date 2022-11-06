using BlazorDiffusion.ServiceModel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.IO;
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

    [Test]
    public async Task ResizeImages()
    {
        var appFilesDir = Path.GetFullPath("../../../App_Files");
        var avatarsDir = Path.Combine(appFilesDir, "avatars");
        var outDir = Path.Combine(avatarsDir, "out").AssertDir();

        foreach (var file in new DirectoryInfo(avatarsDir).GetFiles())
        {
            using var fs = file.OpenRead();
            var ms = await ImageUtils.CropAndResizeAsync(fs, 128, 128, PngFormat.Instance);
            var outFile = Path.Combine(outDir, file.Name.WithoutExtension() + "_128" + file.Extension);
            var outFs = new FileStream(outFile, FileMode.OpenOrCreate);
            ms.Position = 0;
            await ms.CopyToAsync(outFs);
        }
    }


    [Test]
    [TestCase("#888888FF", "#898888FF", 1)]
    [TestCase("#888888FF", "#888988FF", 1)]
    [TestCase("#888888FF", "#888889FF", 1)]
    [TestCase("#888888FF", "#008888FF", 0x88)]
    [TestCase("#888888FF", "#880088FF", 0x88)]
    [TestCase("#888888FF", "#888800FF", 0x88)]
    public void Can_bgcompare(string rgba1, string rgba2, int expected)
    {
        Assert.That(ImageUtils.BackgroundCompare(rgba1, rgba2), Is.EqualTo(expected));
    }

}
