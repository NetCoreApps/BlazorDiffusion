using ServiceStack;
using System;
using System.Collections.Generic;

namespace BlazorDiffusion.ServiceModel;

public class SearchData : IReturn<SearchDataResponse>
{
}

public class SearchDataResponse
{
    public List<ArtistInfo> Artists { get; set; }
    public List<ModifierInfo> Modifiers { get; set; }
}

public class FindSimilarArtifacts
{
    public string ArtifactId { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
}

public class FindSimilarArtifactsResponse
{
    public List<Artifact> Results { get; set; }
}

public class ImageCompareResult
{
    public int Id { get; set; }
    public Int64 PerceptualHash { get; set; }
    public double Similarity { get; set; }
}

public class Group
{
    public string Name { get; set; }
    public string[] Items { get; set; }
}

public class ArtistInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Type { get; set; }

    public override bool Equals(object? obj) => obj is ArtistInfo info && Id == info.Id && Name == info.Name;
    public override int GetHashCode() => HashCode.Combine(Id, Name);
}

public class ModifierInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }

    public override bool Equals(object? obj) => obj is ModifierInfo info &&
        Id == info.Id && Name == info.Name && Category == info.Category;
    public override int GetHashCode() => HashCode.Combine(Id, Name, Category);
}

public class AnonData : IReturn<AnonDataResponse> { }

public class AnonDataResponse
{
    public List<AlbumResult> TopAlbums { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}


[ValidateIsAuthenticated]
public class UserData : IReturn<UserDataResponse> {}

public class Likes
{
    public List<int> ArtifactIds { get; set; }
    public List<int> AlbumIds { get; set; }
}

public class UserDataResponse
{
    public UserResult User { get; set; }
    public List<SignupType> Signups { get; set; }
    public List<string> Roles { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}


public class GetUserInfo : IReturn<GetUserInfoResponse>
{
    public string RefId { get; set; }
}

public class UserResult
{
    public string RefId { get; set; }
    public string? Handle { get; set; }
    public string? Avatar { get; set; }
    public string? ProfileUrl { get; set; }
    public Likes Likes { get; set; }
    public List<AlbumResult> Albums { get; set; }
}
public class GetUserInfoResponse
{
    public UserResult Result { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}
