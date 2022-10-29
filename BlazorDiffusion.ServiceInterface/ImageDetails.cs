using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System.Diagnostics;
using System.IO;
using ServiceStack.Logging;

namespace BlazorDiffusion.ServiceModel;

public class ImageDetails
{
    public static bool Log = false;

    public ulong? PerceptualHash { get; set; }
    public ulong? AverageHash { get; set; }
    public ulong? DifferenceHash { get; set; }
    public Rgba32? DominantColor { get; set; }

    public static ImageDetails? Calculate(byte[] imageBytes)
    {

        try
        {
            var sw = Stopwatch.StartNew();
            using var image = Image.Load<Rgba32>(imageBytes);
            if (Log) Console.WriteLine($"Image.Load<Rgba32> took {sw.ElapsedMilliseconds}ms");

            var ret = Calculate(image);

            if (Log) Console.WriteLine($"Calculate() total {sw.ElapsedMilliseconds}ms");
            return ret;
        }
        catch (Exception) { /*ignore*/ }

        return null;
    }

    public static ImageDetails? Calculate(Image<Rgba32> image)
    {
        try
        {
            var ret = new ImageDetails();
            var sw = Stopwatch.StartNew();

            // Scale the image down preserving the aspect ratio to speed up calculations
            image.ResizeImage(new Size(100, 0));
            ret.DominantColor = image.CalculateAverageColor();

            if (Log) Console.WriteLine($"DominantColor took {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            var perceptualProvider = new PerceptualHash();
            var averageProvider = new AverageHash();
            var differenceProvider = new DifferenceHash();

            // Each hash resizes + manipulates image
            var hashImage = image.Clone();
            ret.PerceptualHash = perceptualProvider.Hash(hashImage);
            if (Log) Console.WriteLine($"PerceptualHash took {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            hashImage = image.Clone();
            ret.AverageHash = averageProvider.Hash(hashImage);
            if (Log) Console.WriteLine($"AverageHash took {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            hashImage = image.Clone();
            ret.DifferenceHash = differenceProvider.Hash(hashImage);
            if (Log) Console.WriteLine($"DifferenceHash took {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            return ret;
        }
        catch (Exception) { /*ignore*/ }
        return null;
    }
}

public static class ImageUtils
{
    public static bool Log = false;
    public static Artifact WithImageDetails(this Artifact artifact, ImageDetails? imageDetails)
    {
        if (imageDetails != null)
        {
            artifact.PerceptualHash = imageDetails.PerceptualHash == null ? null : (Int64)imageDetails.PerceptualHash;
            artifact.AverageHash = imageDetails.AverageHash == null ? null : (Int64)imageDetails.AverageHash;
            artifact.DifferenceHash = imageDetails.DifferenceHash == null ? null : (Int64)imageDetails.DifferenceHash;
            artifact.Background = imageDetails.DominantColor != null ? ("#" + imageDetails.DominantColor.Value.ToHex()) : null;
        }
        return artifact;
    }

    public static bool MissingImageDetails(this Artifact artifact) =>
        artifact.PerceptualHash == null || artifact.AverageHash == null || artifact.DifferenceHash == null || artifact.Background == null;

    public static ImageDetails? LoadImageDetails(this Artifact artifact, Stream imageStream)
    {
        if (artifact.MissingImageDetails())
        {
            var sw = Stopwatch.StartNew();
            using var image = Image.Load<Rgba32>(imageStream);
            if (Log) Console.WriteLine($"Image.Load<Rgba32> took {sw.ElapsedMilliseconds}ms");

            var imageDetails = ImageDetails.Calculate(image);
            artifact.WithImageDetails(imageDetails);

            if (Log) Console.WriteLine($"LoadImageDetails() total {sw.ElapsedMilliseconds}ms");
            return imageDetails;
        }
        return null;
    }

    public static void ResizeImage(this Image<Rgba32> image, Size size)
    {
        image.Mutate(
            x => x
            .Resize(new ResizeOptions() { Sampler = KnownResamplers.NearestNeighbor, Size = size }));
    }

    // 512x896 = 4-10ms, 100x100 resized = <0ms, 
    public static Rgba32 CalculateAverageColor(this Image<Rgba32> image)
    {
        long r = 0;
        long g = 0;
        long b = 0;
        long a = 0;
        long pixels = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                for (int x = 0; x < pixelRow.Length; x++)
                {
                    pixels++;
                    ref Rgba32 pixel = ref pixelRow[x];
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                    a += pixel.A;
                }
            }
        });

        byte R = (byte)Math.Floor(r / (double)pixels);
        byte G = (byte)Math.Floor(g / (double)pixels);
        byte B = (byte)Math.Floor(b / (double)pixels);
        byte A = (byte)Math.Floor(a / (double)pixels);

        return new Rgba32(R, G, B, A);
    }

    // 512x896 = 10-36ms, 100x100 resized = 2-3ms, 
    public static Rgba32 CalculateDominantColor(this Image<Rgba32> image)
    {
        image.Mutate(
            x => x
            .Resize(new ResizeOptions() { Sampler = KnownResamplers.NearestNeighbor, Size = new Size(100, 0) })
            // Reduce the color palette to 1 color without dithering.
            .Quantize(new OctreeQuantizer(new QuantizerOptions { MaxColors = 1, Dither = null })));
        return image[0, 0];
    }

}