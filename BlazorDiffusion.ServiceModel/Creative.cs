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
    public int Score { get; set; }
    public int Rank { get; set; }
    public string RefId { get; set; }
    public string RequestId { get; set; }
    public string EngineId { get; set; }
}


public class QueryCreatives : QueryDb<Creative>
{
    public int? Id { get; set; }
    public int? CreativeId { get; set; }
    public string? CreatedBy { get; set; }
    public int? OwnerId { get; set; }
}

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

[AutoApply(Behavior.AuditModify)]
[ValidateIsAuthenticated]
public class UpdateCreative : IPatchDb<Creative>, IReturn<Creative>
{
    public int Id { get; set; }
    
    public int? PrimaryArtifactId { get; set; }
    public bool? UnpinPrimaryArtifact { get; set; }
}

[AutoApply(Behavior.AuditSoftDelete)]
[ValidateIsAuthenticated]
public class DeleteCreative : IDeleteDb<Creative>, IReturnVoid
{
    public int Id { get; set; }
}

[AutoApply(Behavior.AuditDelete)]
[ValidateHasRole(AppRoles.Moderator)]
public class HardDeleteCreative : IDeleteDb<Creative>, IReturnVoid
{
    public int Id { get; set; }
}

public class QueryArtists : QueryDb<Artist> {}

[ValidateHasRole(AppRoles.Moderator)]
public class CreateArtist : ICreateDb<Artist>, IReturn<Artist>
{
    public string? FirstName { get; set; }
    [ValidateNotEmpty, Required]
    public string LastName { get; set; }
    public int? YearDied { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Type { get; set; }
}

[ValidateHasRole(AppRoles.Moderator)]
public class UpdateArtist : IPatchDb<Artist>, IReturn<Artist>
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? YearDied { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Type { get; set; }
}
[ValidateHasRole(AppRoles.Moderator)]
public class DeleteArtist : IDeleteDb<Artist>, IReturnVoid 
{
    public int Id { get; set; }
}

[Icon(Svg = Icons.Artist)]
public class Artist : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string LastName { get; set; }
    public int? YearDied { get; set; }
    public List<string>? Type { get; set; }
    public int Score { get; set; }
    public int Rank { get; set; }
}


public class QueryModifiers : QueryDb<Modifier> { }

[ValidateHasRole(AppRoles.Moderator)]
[AutoApply(Behavior.AuditCreate)]
public class CreateModifier : ICreateDb<Modifier>, IReturn<Modifier>
{
    [ValidateNotEmpty, Required]
    public string Name { get; set; }
    [ValidateNotEmpty, Required]
    public string Category { get; set; }
    public string? Description { get; set; }
}

[ValidateHasRole(AppRoles.Moderator)]
[AutoApply(Behavior.AuditModify)]
public class UpdateModifier : IPatchDb<Modifier>, IReturn<Modifier>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
}

[ValidateHasRole(AppRoles.Moderator)]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteModifier : IDeleteDb<Modifier>, IReturnVoid
{
    public int Id { get; set; }
}


[Icon(Svg = Icons.Modifier)]
public class Modifier : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string? Description { get; set; }
    public int Score { get; set; }
    public int Rank { get; set; }
}


public class QueryCreativeArtists : QueryDb<CreativeArtist>
{
    public int? CreativeId { get; set; }
    public int? ModifierId { get; set; }
}

[ValidateHasRole(AppRoles.Moderator)]
public class CreateCreativeArtist : ICreateDb<CreativeArtist>, IReturn<CreativeArtist>
{
    [ValidateGreaterThan(0)]
    public int? CreativeId { get; set; }
    [ValidateGreaterThan(0)]
    public int? ModifierId { get; set; }
}

[ValidateHasRole(AppRoles.Moderator)]
public class DeleteCreativeArtist : IDeleteDb<CreativeArtist>, IReturnVoid
{
    public int? Id { get; set; }
    public int[]? Ids { get; set; }
}
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


public class QueryCreativeModifiers : QueryDb<CreativeModifier> 
{
    public int? CreativeId { get; set; }
    public int? ModifierId { get; set; }
}

[ValidateHasRole(AppRoles.Moderator)]
public class CreateCreativeModifier : ICreateDb<CreativeModifier>, IReturn<CreativeModifier>
{
    [ValidateGreaterThan(0)]
    public int? CreativeId { get; set; }
    [ValidateGreaterThan(0)]
    public int? ModifierId { get; set; }
}

[ValidateHasRole(AppRoles.Moderator)]
public class DeleteCreativeModifier : IDeleteDb<CreativeModifier>, IReturnVoid
{
    public int? Id { get; set; }
    public int[]? Ids { get; set; }
}

[ValidateHasRole(AppRoles.Moderator)]
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
