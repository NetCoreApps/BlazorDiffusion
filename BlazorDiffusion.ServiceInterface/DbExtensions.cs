using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.ServiceInterface;

public static class DbExtensions
{
    public static async Task<UserResult> GetUserResultAsync(this IDbConnection db, int userId)
    {
        var likes = new Likes
        {
            ArtifactIds = await db.ColumnAsync<int>(db.From<ArtifactLike>().Where(x => x.AppUserId == userId).Select(x => x.ArtifactId).OrderByDescending(x => x.Id)),
            AlbumIds = await db.ColumnAsync<int>(db.From<AlbumLike>().Where(x => x.AppUserId == userId).Select(x => x.AlbumId).OrderByDescending(x => x.Id)),
        };

        var userAlbums = await db.LoadSelectAsync<Album>(x => x.OwnerId == userId && x.DeletedDate == null);
        var albums = userAlbums.OrderByDescending(x => x.Artifacts.Max(x => x.Id)).ToList();
        var albumResults = albums.Map(x => x.ToAlbumResult());

        var userInfo = await db.SingleAsync<(string refId, string handle, string avatar, string profileUrl)>(db.From<AppUser>()
            .Where(x => x.Id == userId).Select(x => new { x.RefIdStr, x.Handle, x.Avatar, x.ProfileUrl }));

        return new UserResult
        {
            RefId = userInfo.refId,
            Handle = userInfo.handle,
            Avatar = userInfo.avatar,
            ProfileUrl = userInfo.profileUrl,
            Likes = likes,
            Albums = albumResults,
        };
    }
    public static string GetSlug(this Artifact artifact) => artifact.Prompt.LeftPart(',').GenerateSlug();
    public static string GetHtmlFileName(this Artifact artifact) =>
        $"{artifact.Id.ToString().PadLeft(4, '0')}_{artifact.GetSlug()}.html";
    public static string GetHtmlFilePath(this Artifact artifact) =>
        $"/artifacts/{Math.Floor(artifact.Id / 1000d)}/{artifact.GetHtmlFileName()}";

    public static string GetHtmlFilePath(this AlbumResult album, int pageNo)
    {
        var suffix = pageNo == 1 ? "" : "_" + pageNo;
        return $"/albums/{album.Slug}{suffix}.html";
    }
}
