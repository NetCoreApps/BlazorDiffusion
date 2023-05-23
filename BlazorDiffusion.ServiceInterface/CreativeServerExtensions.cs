using System;
using BlazorDiffusion.ServiceModel;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.Web;
using System.Linq;

namespace BlazorDiffusion;

public static class CreativeServerExtensions
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

    public static AlbumResult ToAlbumResult(this Album album)
    {
        var to = new AlbumResult
        {
            Id = album.Id,
            AlbumRef = album.RefId,
            Name = album.Name,
            Slug= album.Slug,
            OwnerRef = album.OwnerRef,
            PrimaryArtifactId = album.PrimaryArtifactId,
            Score = album.Score,
            // Show latest artifacts added first
            ArtifactIds = album.PrimaryArtifactId == null
                ? album.Artifacts.OrderByDescending(x => x.Id).Map(x => x.ArtifactId)
                : X.Apply(new List<int> { album.PrimaryArtifactId.Value },
                    ids => ids.AddRange(album.Artifacts.Where(x => x.ArtifactId != album.PrimaryArtifactId.Value)
                        .OrderByDescending(x => x.Id).Select(x => x.ArtifactId))),
        };
        return to;
    }

    public static string ConstructPrompt(this string userPrompt, List<Modifier> modifiers, List<Artist> artists)
    {
        var finalPrompt = userPrompt;
        if (modifiers.Count > 0)
            finalPrompt += ", " + modifiers.Select(x => x.Name).Join(", ");
        if (artists.Count > 0)
            finalPrompt += $" by {artists.Select(x => x.GetArtistName()).Join(", ")}";
        return finalPrompt;
    }

    public static string GetArtistName(this Artist artist) => string.IsNullOrEmpty(artist.FirstName)
        ? artist.LastName
        : $"{artist.FirstName} {artist.LastName}";

}
