using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace BlazorDiffusion.ServiceModel;

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

    public static string? LeftPart(string strVal, char needle)
    {
        if (strVal == null) return null;
        var pos = strVal.IndexOf(needle);
        return pos == -1
            ? strVal
            : strVal.Substring(0, pos);
    }

    private static readonly Regex InvalidCharsRegex = new(@"[^a-z0-9\s-]", RegexOptions.Compiled);
    private static readonly Regex SpacesRegex = new(@"\s", RegexOptions.Compiled);
    private static readonly Regex CollapseHyphensRegex = new("-+", RegexOptions.Compiled);
    private static readonly Regex RemoveNonAsciiRegex = new(@"[^\u0000-\u007F]+", RegexOptions.Compiled);
    public static string GenerateSlug(string phrase, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(phrase))
            return string.Empty;

        var str = phrase.ToLower()
            .Replace("#", "sharp")  // c#, f# => csharp, fsharp
            .Replace("++", "pp");   // c++ => cpp

        str = RemoveNonAsciiRegex.Replace(str, "");
        str = InvalidCharsRegex.Replace(str, "-");
        str = str.Substring(0, Math.Min(str.Length, maxLength)).Trim();
        str = SpacesRegex.Replace(str, "-");
        str = CollapseHyphensRegex.Replace(str, "-");

        if (string.IsNullOrEmpty(str))
            return str;

        if (str[0] == '-')
            str = str.Substring(1);
        if (str.Length > 0 && str[str.Length - 1] == '-')
            str = str.Substring(0, str.Length - 1);

        return str;
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

    public static string GetArtifactPrompt(Artifact artifact) =>
        (artifact is ArtifactResult result ? result.UserPrompt : null)
        ?? artifact.Prompt ?? "";

    public static string GetArtifact(Artifact artifact, string slug) =>
        $"/artifacts/{Math.Floor(artifact.Id / 1000d)}/{GetArtifactFileName(artifact, slug)}";

    public static string GetArtifactFileName(Artifact artifact, string slug) =>
        $"{artifact.Id.ToString().PadLeft(4, '0')}_{slug}.html";

    public static string GetSlug(Creative creative) => GenerateSlug(creative.UserPrompt);
    public static string GetSlug(Artifact artifact) => GenerateSlug(GetArtifactPrompt(artifact));
}


[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class DeleteArtifactHtml
{
    public int[] Ids { get; set; }
}

[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class DeleteCdnFilesMq
{
    public List<string> Files { get; set; }
}

[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class GetCdnFile
{
    public string File { get; set; }
}

[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class DeleteCdnFile : IReturnVoid
{
    public string File { get; set; }
}

[ExcludeMetadata]
[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
[Route("/render/{Type}")]
public class RenderComponent : IGet, IReturn<string>
{
    public string Type { get; set; }
    public bool TestContext { get; set; }
}

[ExcludeMetadata]
[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class Prerender : IGet, IReturn<PrerenderResponse> { }
public class PrerenderResponse
{
    public List<string> Results { get; set; }
    public List<string> Failed { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[Tag(Tag.Ssg)]
[Route("/image/{Id}")]
[Route("/artifacts/{Group}/{Slug}")]
public class RenderArtifactHtml : IReturn<string>
{
    public int? Group { get; set; }
    public int? Id { get; set; }
    public string? Slug { get; set; }
    public bool? Save { get; set; }
}

[Tag(Tag.Ssg)]
public class TestImageHtml : IReturnVoid {}

[ExcludeMetadata]
[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class PrerenderImages : IReturn<PrerenderResponse>
{
    public bool Force { get; set; }
    public int[] Batches { get; set; }
}

[ExcludeMetadata]
[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class PrerenderImage : IReturn<PrerenderResponse>
{
    public int ArtifactId { get; set; }
}

[ExcludeMetadata]
[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class PrerenderSitemap : IReturn<PrerenderResponse>
{
}

[ExcludeMetadata]
[Tag(Tag.Ssg)]
[ValidateHasRole(AppRoles.Moderator)]
public class DevTasks : IGet, IReturn<StringResponse>
{
    public bool? DisableWrites { get; set; }
}
