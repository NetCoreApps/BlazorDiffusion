﻿using System;
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
    public int? PrimaryArtifactId { get; set; }
    public bool Private { get; set; }
    public int? Rating { get; set; }
    public int Score { get; set; }
    public int Rank { get; set; }
    [Reference]
    public List<AlbumArtifact> Artifacts { get; set; }
}

public class AlbumArtifact
{
    [AutoIncrement]
    public long Id { get; set; }

    [References(typeof(Album))]
    public int AlbumId { get; set; }
    [References(typeof(Artifact))]
    public int ArtifactId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    [Reference]
    public List<Artifact> Artifact { get; set; }
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

public class QueryAlbums : QueryDb<Album> 
{
    public int? Id { get; set; }
    public List<int>? Ids { get; set; }
}

[ValidateIsAuthenticated]
[AutoPopulate(nameof(Album.RefId), Eval = "nguid")]
[AutoPopulate(nameof(Album.OwnerId), Eval = "userAuthId")]
public class CreateAlbum : ICreateDb<Album>, IReturn<Album>
{
    [ValidateNotEmpty]
    public string Name { get; set; }
    public string Description { get; set; }
    public string? Slug { get; set; }
    public List<string>? Tags { get; set; }
    public int? PrimaryArtifactId { get; set; }
    public List<int>? ArtifactIds { get; set; }
}

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
    public List<int>? AddArtifactIds { get; set; }
    public List<int>? RemoveArtifactIds { get; set; }
}

[ValidateIsAuthenticated]
[AutoPopulate(nameof(Album.OwnerId), Eval = "userAuthId")]
public class DeleteAlbum : IDeleteDb<Album>, IReturnVoid
{
    public int Id { get; set; }
}



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


[AutoFilter(QueryTerm.Ensure, nameof(ArtifactLike.AppUserId), Eval = "userAuthId")]
[ValidateIsAuthenticated]
public class QueryAlbumLikes : QueryDb<AlbumLike>
{
}

[ValidateIsAuthenticated]
public class CreateAlbumLike : ICreateDb<AlbumLike>, IReturn<AlbumLike>
{
    [ValidateGreaterThan(0)]
    public int ArtifactId { get; set; }
}

[AutoPopulate(nameof(ArtifactLike.AppUserId), Eval = "userAuthId")]
[ValidateIsAuthenticated]
public class DeleteAlbumLike : IDeleteDb<AlbumLike>, IReturnVoid
{
    [ValidateGreaterThan(0)]
    public int ArtifactId { get; set; }
}