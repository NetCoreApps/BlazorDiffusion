using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack;
using ServiceStack.Web;
using BlazorDiffusion.ServiceModel;
using System.Runtime.CompilerServices;
using ServiceStack.Pcl;

namespace BlazorDiffusion;

public static class CreativeExtensions
{
    public const string SystemUserId = "2";

    public static T WithAudit<T>(this T row, IRequest req, DateTime? date = null) where T : AuditBase =>
        row.WithAudit(req.GetSession().UserAuthId, date);

    public static T WithAudit<T>(this T row, string? by, DateTime? date = null) where T : AuditBase
    {
        by ??= SystemUserId;
        var useDate = date ?? DateTime.UtcNow;
        if (string.IsNullOrEmpty(row.CreatedBy))
        {
            row.CreatedBy = by;
            row.CreatedDate = useDate;
        }
        row.ModifiedBy = by;
        row.ModifiedDate = useDate;
        return row;
    }

    public static string SolidImageDataUri(string? fill) => 
        $"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'%3E%3Cpath fill='%23{(fill ?? "#000").Substring(1)}' d='M2 2h60v60H2z'/%3E%3C/svg%3E";

    public static string GetBackgroundImage(this Artifact artifact) => SolidImageDataUri(artifact.Background);
    public static string GetBackgroundStyle(this Artifact artifact) => artifact.Background != null ? "background-color:" + artifact.Background : "";
    public static string GetDownloadUrl(this Artifact artifact) => $"/download/artifact/{artifact.RefId}";
    public static string GetPublicUrl(this Artifact artifact) => AppConfig.Instance.AssetsBasePath + artifact.FilePath;
    public static string GetFallbackUrl(this Artifact artifact) => AppConfig.Instance.FallbackAssetsBasePath + artifact.FilePath;

    public static string GetImageErrorUrl(this Artifact artifact, string? lastImageSrc)
    {
        if (lastImageSrc == null)
            return artifact.GetFallbackUrl();
        if (lastImageSrc == artifact.GetFallbackUrl())
            return artifact.GetPublicUrl().SetQueryParam("r", "1");

        var qs = HttpUtility.ParseQueryString(lastImageSrc);
        var r = (qs != null ? qs["r"] : null) ?? "1";
        var rint = int.TryParse(r, out var rIndex)
            ? rIndex
            : 1;

        if (rint > 5)
            return SolidImageDataUri("#000"); // fail to bg black

        rint++;
        return rint % 2 == 0
            ? artifact.GetFallbackUrl().SetQueryParam("r", $"{rint}")
            : artifact.GetPublicUrl().SetQueryParam("r", $"{rint}");
    }

    public static List<Artifact> GetArtifacts(this Creative creative)
    {
        if (creative == null)
            return TypeConstants<Artifact>.EmptyList;

        var primary = creative.PrimaryArtifactId != null 
            ? creative.Artifacts.FirstOrDefault(x => x.Id == creative.PrimaryArtifactId)
            : null;
        if (primary == null)
            return creative.Artifacts ?? new();

        var to = new List<Artifact>(creative.Artifacts.Count) { primary };
        to.AddRange(creative.Artifacts.Where(x => x.Id != creative.PrimaryArtifactId).OrderByDescending(x => x.Score));
        return to;
    }

    public static bool HasArtifact(this Album album, Artifact artifact) => 
        album?.Artifacts?.Any(x => x.ArtifactId == artifact.Id) == true;

    public static void AddArtifact(this Album album, Artifact artifact)
    {
        if (!album.HasArtifact(artifact))
        {
            album.Artifacts ??= new();
            album.Artifacts.Add(new AlbumArtifact { 
                AlbumId = album.Id,
                ArtifactId = artifact.Id,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Artifact = artifact,
            });
        }
    }

    public static void RemoveArtifact(this Album album, Artifact artifact)
    {
        if (album.HasArtifact(artifact))
        {
            album.Artifacts.RemoveAll(x => x.ArtifactId == artifact.Id);
        }
    }
}