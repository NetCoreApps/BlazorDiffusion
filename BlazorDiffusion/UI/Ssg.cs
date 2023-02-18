using BlazorDiffusion.ServiceModel;

namespace BlazorDiffusion.UI;

public interface IGetPageModel
{
    Task OnGetAsync();
}

public class ArtifactImageParams
{
    public Artifact Artifact { get; set; }
    public string? Class { get; set; }
    public string? ImageClass { get; set; }
    public int? MinSize { get; set; }
}

public static class Ssg
{
    public static class Pages
    {
        public const string Home = "/Pages/ssg/Empty/Home.cshtml";
        public const string Top = "/Pages/ssg/Top.cshtml";
        public const string Latest = "/Pages/ssg/Latest.cshtml";
        public const string Albums = "/Pages/ssg/Albums.cshtml";
        public const string Album = "/Pages/ssg/Album.cshtml";
        public const string Image = "/Pages/ssg/Image.cshtml";
    }

    public static class Empty
    {
        public const string Home = "/prerender/index.html";
    }

    public static string GetTop() => $"/top.html";

    public static string GetLatest(int? page = 1) => page.GetValueOrDefault() <= 1
        ? "/latest.html"
        : $"/latest/{page}.html";

    public static string GetAlbums() => $"/albums.html";

    public static string GetAlbum(AlbumResult album, int? pageNo = 1)
    {
        var suffix = pageNo == 1 ? "" : "_" + pageNo;
        return $"/albums/{album.Slug}{suffix}.html";
    }

    public static string GetArtifact(this Artifact artifact) =>
        $"/artifacts/{Math.Floor(artifact.Id / 1000d)}/{artifact.GetHtmlFileName()}";

    public static string GetSlug(Artifact artifact) => artifact.Prompt.LeftPart(',').GenerateSlug();
}