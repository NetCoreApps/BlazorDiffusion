using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.Web;

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


    public static string GetDownloadUrl(this Artifact artifact)
    {
        //TODO change to ?download + record stat
        return AppConfig.Instance.AssetsBasePath + artifact.FilePath;
    }

    public static string GetPublicUrl(this Artifact artifact)
    {
        return AppConfig.Instance.AssetsBasePath + artifact.FilePath;
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
        to.AddRange(creative.Artifacts.Where(x => x.Id != creative.PrimaryArtifactId));
        return to;
    }
}