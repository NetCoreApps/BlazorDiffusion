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

    public static List<CreativeArtifact> GetArtifacts(this Creative creative)
    {
        var primary = creative.PrimaryArtifactId != null 
            ? creative.Artifacts.FirstOrDefault(x => x.Id == creative.PrimaryArtifactId)
            : null;
        if (primary == null)
            return creative.Artifacts ?? new();

        var to = new List<CreativeArtifact>(creative.Artifacts.Count) { primary };
        to.AddRange(creative.Artifacts.Where(x => x.Id != creative.PrimaryArtifactId));
        return to;
    }
}