using System;
using System.Collections.Generic;
using ServiceStack;

namespace BlazorDiffusion.ServiceModel;

[Tag(Tag.User)]
[ValidateIsAuthenticated]
public class GetUserProfile : IReturn<GetUserProfileResponse> {}
public class GetUserProfileResponse
{
    public UserProfile Result { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

public class UserProfile
{
    public string DisplayName { get; set; }
    public string? Avatar { get; set; }
    public string? Handle { get; set; }
}

[Tag(Tag.User)]
[ValidateIsAuthenticated]
public class UpdateUserProfile : IUpdateDb<AppUser>, IReturn<UserProfile>
{
    [ValidateNotEmpty]
    public string DisplayName { get; set; }
    [Input(Type="File"), UploadTo("avatars")]
    public string? Avatar { get; set; }
    [ValidateMaximumLength(20)]
    public string? Handle { get; set; }
}

[Tag(Tag.User)]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditQuery)]
public class GetArtifactUserData : IGet, IReturn<GetArtifactUserDataResponse>
{
    public int ArtifactId { get; set; }
}
public class GetArtifactUserDataResponse
{
    public int ArtifactId { get; set; }
    public bool Liked { get; set; }
    public List<int> UpVoted { get; set; }
    public List<int> DownVoted { get; set; }
}

[Tag(Tag.User)]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditQuery)]
public class GetAlbumUserData : IGet, IReturn<GetAlbumUserDataResponse>
{
    public int AlbumId { get; set; }
}
public class GetAlbumUserDataResponse
{
    public int AlbumId { get; set; }
    public List<int> LikedArtifacts { get; set; }
}

[Tag(Tag.User)]
public class AnonData : IReturn<AnonDataResponse> { }
public class AnonDataResponse
{
    public List<AlbumResult> TopAlbums { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[Tag(Tag.User)]
[ValidateIsAuthenticated]
public class CheckQuota : IReturn<CheckQuotaResponse>
{
    public int Images { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
public class CheckQuotaResponse
{
    public TimeSpan TimeRemaining { get; set; }
    public int CreditsUsed { get; set; }
    public int CreditsRequested { get; set; }
    public int CreditsRemaining { get; set; }
    public int DailyQuota { get; set; }
    public string RequestedDetails { get; set; }
}

[Tag(Tag.User)]
[ValidateIsAuthenticated]
public class UserData : IReturn<UserDataResponse> {}
public class UserDataResponse
{
    public UserResult User { get; set; }
    public UserProfile Profile { get; set; }
    public List<SignupType> Signups { get; set; }
    public List<string> Roles { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[Tag(Tag.User)]
public class GetUserInfo : IReturn<GetUserInfoResponse>
{
    public string RefId { get; set; }
}
public class GetUserInfoResponse
{
    public UserResult Result { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}


public class Likes
{
    public List<int> ArtifactIds { get; set; }
    public List<int> AlbumIds { get; set; }
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

