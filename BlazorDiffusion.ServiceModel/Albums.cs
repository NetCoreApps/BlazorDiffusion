using System;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace BlazorDiffusion.ServiceModel;

[Icon(Svg = Icons.Album)]
public class Album : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Slug { get; set; }
    public List<string> Tags { get; set; }
    public string RefId { get; set; }
    [References(typeof(AppUser))]
    public int OwnerId { get; set; }
    public string OwnerRef { get; set; }
    public int? PrimaryArtifactId { get; set; }
    public bool Private { get; set; }
    public int? Rating { get; set; }
    public int LikesCount { get; set; } // duplicated aggregate counts
    public int DownloadsCount { get; set; }
    public int SearchCount { get; set; }
    public int Score { get; set; }
    public int Rank { get; set; }
    public int? PrefColumns { get; set; }
    [Reference]
    public List<AlbumArtifact> Artifacts { get; set; }
}

public class AlbumResult
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string AlbumRef { get; set; }
    public string OwnerRef { get; set; }
    public int? PrimaryArtifactId { get; set; }
    public int Score { get; set; }
    public List<int> ArtifactIds { get; set; }
}

public class AlbumArtifactResult
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string RefId { get; set; }
    public string Slug { get; set; }
    public string OwnerRef { get; set; }
    public int? PrimaryArtifactId { get; set; }
    public int Score { get; set; }
    public int ArtifactId { get; set; }
}


[Icon(Svg = Icons.Artifact)]
public class AlbumArtifact
{
    [AutoIncrement]
    public int Id { get; set; }

    [References(typeof(Album))]
    public int AlbumId { get; set; }
    [References(typeof(Artifact))]
    public int ArtifactId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    [Reference]
    public Artifact? Artifact { get; set; }
}

public class AlbumLike
{
    [AutoIncrement]
    public long Id { get; set; }

    [References(typeof(Album))]
    public int AlbumId { get; set; }
    [References(typeof(AppUser))]
    public int AppUserId { get; set; }
    public DateTime CreatedDate { get; set; }
}

[Tag(Tag.Albums)]
public class QueryAlbums : QueryDb<Album>
{
    public int? Id { get; set; }
    public List<int>? Ids { get; set; }
}

[Tag(Tag.Albums)]
public class GetAlbumResults : IReturn<GetAlbumResultsResponse>
{
    public List<int>? Ids { get; set; }
    public List<string>? RefIds { get; set; }
}

public class GetAlbumResultsResponse
{
    public List<AlbumResult> Results { get; set; }
}

[Tag(Tag.Albums)]
[ValidateIsAuthenticated]
public class CreateAlbum : ICreateDb<Album>, IReturn<Album>
{
    [ValidateNotEmpty]
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string>? Tags { get; set; }
    public int? PrimaryArtifactId { get; set; }
    public List<int>? ArtifactIds { get; set; }
}

[Tag(Tag.Albums)]
[ValidateIsAuthenticated]
[AutoPopulate(nameof(Album.OwnerId), Eval = "userAuthId")]
public class UpdateAlbum : IPatchDb<Album>, IReturn<Album>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public List<string>? Tags { get; set; }
    public int? PrimaryArtifactId { get; set; }
    public bool? UnpinPrimaryArtifact { get; set; }
    public List<int>? AddArtifactIds { get; set; }
    public List<int>? RemoveArtifactIds { get; set; }
}

[Tag(Tag.Albums)]
[ValidateIsAuthenticated]
[AutoPopulate(nameof(Album.OwnerId), Eval = "userAuthId")]
public class DeleteAlbum : IDeleteDb<Album>, IReturnVoid
{
    public int Id { get; set; }
}



[Tag(Tag.Albums)]
[ValidateIsAuthenticated]
[AutoPopulate(nameof(Album.OwnerId), Eval = "userAuthId")]
public class UpdateAlbumArtifact : IPatchDb<Album>, IReturn<Album>
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public List<string>? Tags { get; set; }
    public int? PrimaryArtifactId { get; set; }
    public List<int>? AddArtifactIds { get; set; }
    public List<int>? RemoveArtifactIds { get; set; }
}


[Tag(Tag.Albums)]
[AutoFilter(QueryTerm.Ensure, nameof(ArtifactLike.AppUserId), Eval = "userAuthId")]
[ValidateIsAuthenticated]
public class QueryAlbumLikes : QueryDb<AlbumLike>
{
}

[Tag(Tag.Albums)]
[ValidateIsAuthenticated]
public class CreateAlbumLike : ICreateDb<AlbumLike>, IReturn<IdResponse>
{
    [ValidateGreaterThan(0)]
    public int AlbumId { get; set; }
}

[Tag(Tag.Albums)]
[AutoPopulate(nameof(ArtifactLike.AppUserId), Eval = "userAuthId")]
[ValidateIsAuthenticated]
public class DeleteAlbumLike : IDeleteDb<AlbumLike>, IReturnVoid
{
    [ValidateGreaterThan(0)]
    public int AlbumId { get; set; }
}

[Tag(Tag.Albums)]
[ValidateHasRole(AppRoles.Moderator)]
public class QueryAlbumArtifacts : QueryDb<AlbumArtifact> { }


[Tag(Tag.Albums)]
[Description("Retrieve Albums containing at least one of creative Artifacts")]
public class GetCreativesInAlbums : IGet, IReturn<GetCreativesInAlbumsResponse>
{
    public int CreativeId { get; set; }
}
public class GetCreativesInAlbumsResponse
{
    public List<AlbumResult> Results { get; set; }
}

[Tag(Tag.Albums)]
public class GetAlbumIds : IReturn<GetAlbumIdsResponse> { }
public class GetAlbumIdsResponse
{
    public List<int> Results { get; set; }
}

[Tag(Tag.Albums)]
public class GetAlbumRefs : IReturn<GetAlbumRefsResponse> { }
public class GetAlbumRefsResponse
{
    public List<AlbumRef> Results { get; set; }
}

