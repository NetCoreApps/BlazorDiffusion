using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace BlazorDiffusion.ServiceModel;

[Icon(Svg = Icons.Creative)]
public class Creative : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    public string UserPrompt { get; set; }
    public string Prompt { get; set; }
    
    public int Images { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int Steps { get; set; }

    public int? CuratedArtifactId { get; set; }
    public int? PrimaryArtifactId { get; set; }

    public List<string> ArtistNames { get; set; }
    public List<string> ModifierNames { get; set; }

    [Reference]
    public List<CreativeArtist> Artists { get; set; }
    [Reference]
    public List<CreativeModifier> Modifiers { get; set; }

    [Reference]
    [Format("presentFilesPreview")]
    public List<Artifact> Artifacts { get; set; }
    
    public string? Error { get; set; }
    
    [References(typeof(AppUser))]
    public int? OwnerId { get; set; }
    public string? OwnerRef { get; set; }
    public string? Key { get; set; }

    public bool Curated { get; set; }
    public int? Rating { get; set; }
    public bool Private { get; set; }
    [Default(0)]
    public int Score { get; set; }
    [Default(0)]
    public int Rank { get; set; }
    public string RefId { get; set; }
    public string RequestId { get; set; }
    public string EngineId { get; set; }
}

[Tag(Tag.Creatives)]
public class QueryCreatives : QueryDb<Creative>
{
    public int? Id { get; set; }
    public string? CreatedBy { get; set; }
    public int? OwnerId { get; set; }
}

[Tag(Tag.Creatives)]
public class GetCreative : IGet, IReturn<GetCreativeResponse>
{
    public int? Id { get; set; }
    public int? ArtifactId { get; set; }
}
public class GetCreativeResponse
{
    public Creative Result { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[Tag(Tag.Creatives)]
[AutoApply(Behavior.AuditCreate)]
[ValidateIsAuthenticated]
public class CreateCreative : ICreateDb<Creative>, IReturn<CreateCreativeResponse>
{
    [Required]
    public string UserPrompt { get; set; }
    
    public int? Images { get; set; }
    
    public int? Width { get; set; }
    
    public int? Height { get; set; }
    
    public int? Steps { get; set; }
    public long? Seed { get; set; }
    
    public List<int> ArtistIds { get; set; }
    public List<int> ModifierIds { get; set; }
}
public class CreateCreativeResponse
{
    public Creative Result { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[Tag(Tag.Creatives)]
[AutoApply(Behavior.AuditModify)]
[ValidateIsAuthenticated]
public class UpdateCreative : IPatchDb<Creative>, IReturn<Creative>
{
    public int Id { get; set; }
    
    public int? PrimaryArtifactId { get; set; }
    public bool? UnpinPrimaryArtifact { get; set; }
}

[Tag(Tag.Creatives)]
[AutoApply(Behavior.AuditSoftDelete)]
[ValidateIsAuthenticated]
public class DeleteCreative : IDeleteDb<Creative>, IReturnVoid
{
    public int Id { get; set; }
}

[Tag(Tag.Creatives)]
[AutoApply(Behavior.AuditDelete)]
[ValidateHasRole(AppRoles.Moderator)]
public class HardDeleteCreative : IDeleteDb<Creative>, IReturnVoid
{
    public int Id { get; set; }
}


[Tag(Tag.Creatives)]
public class QueryCreativeArtists : QueryDb<CreativeArtist>
{
    public int? CreativeId { get; set; }
    public int? ModifierId { get; set; }
}

[Tag(Tag.Creatives)]
[ValidateHasRole(AppRoles.Moderator)]
public class CreateCreativeArtist : ICreateDb<CreativeArtist>, IReturn<CreativeArtist>
{
    [ValidateGreaterThan(0)]
    public int? CreativeId { get; set; }
    [ValidateGreaterThan(0)]
    public int? ModifierId { get; set; }
}

[Tag(Tag.Creatives)]
[ValidateHasRole(AppRoles.Moderator)]
public class DeleteCreativeArtist : IDeleteDb<CreativeArtist>, IReturnVoid
{
    public int? Id { get; set; }
    public int[]? Ids { get; set; }
}

[Icon(Svg = Icons.Artist)]
[ValidateHasRole(AppRoles.Moderator)]
public class CreativeArtist
{
    [AutoIncrement]
    public int Id { get; set; }
    [References(typeof(Creative))]
    public int CreativeId { get; set; }
    [References(typeof(Artist))]
    public int ArtistId { get; set; }
    
    [Reference]
    public Artist Artist { get; set; }
}


[Tag(Tag.Creatives)]
public class QueryCreativeModifiers : QueryDb<CreativeModifier> 
{
    public int? CreativeId { get; set; }
    public int? ModifierId { get; set; }
}

[Tag(Tag.Creatives)]
[ValidateHasRole(AppRoles.Moderator)]
public class CreateCreativeModifier : ICreateDb<CreativeModifier>, IReturn<CreativeModifier>
{
    [ValidateGreaterThan(0)]
    public int? CreativeId { get; set; }
    [ValidateGreaterThan(0)]
    public int? ModifierId { get; set; }
}

[Tag(Tag.Creatives)]
[ValidateHasRole(AppRoles.Moderator)]
public class DeleteCreativeModifier : IDeleteDb<CreativeModifier>, IReturnVoid
{
    public int? Id { get; set; }
    public int[]? Ids { get; set; }
}

[Icon(Svg = Icons.Modifier)]
public class CreativeModifier
{
    [AutoIncrement]
    public int Id { get; set; }
    [References(typeof(Creative))]
    public int CreativeId { get; set; }
    [References(typeof(Modifier))]
    public int ModifierId { get; set; }
    
    [Reference]
    public Modifier Modifier { get; set; }
}

[Tag(Tag.Creatives)]
[Route("/creative/metadata/{CreativeId}")]
[ValidateHasRole(AppRoles.Moderator)]
public class ViewCreativeMetadata : IGet, IReturn<Creative>
{
    [ValidateGreaterThan(0)]
    public int CreativeId { get; set; }
}
