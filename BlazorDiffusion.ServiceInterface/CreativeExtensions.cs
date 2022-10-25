using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack;
using ServiceStack.Web;
using BlazorDiffusion.ServiceModel;

namespace BlazorDiffusion;

public static class CreativeExtensions
{
    public static T WithAudit<T>(this T row, IRequest req, DateTime? date = null) where T : AuditBase =>
        row.WithAudit(req.GetSession().UserAuthId, date);

    public static T WithAudit<T>(this T row, string by, DateTime? date = null) where T : AuditBase
    {
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


    public static string GetDownloadUrl(this Artifact artifact) => $"/download/{artifact.RefId}";

    public static string GetPublicUrl(this Artifact artifact) => AppConfig.Instance.AssetsBasePath + artifact.FilePath;
    public static string GetFallbackUrl(this Artifact artifact) => AppConfig.Instance.FallbackAssetsBasePath + artifact.FilePath;

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
        to.AddRange(creative.Artifacts.Where(x => x.Id != creative.PrimaryArtifactId));
        return to;
    }

    public static bool HasArtifact(this Album album, Artifact artifact) => 
        album?.Artifacts?.Any(x => x.ArtifactId == artifact.Id) == true;

    public static void AddArtifact(this Album album, Artifact artifact)
    {
        if (!album.HasArtifact(artifact))
        {
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