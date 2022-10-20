using System;
using System.Collections.Generic;
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

    public int? PrimaryArtifactId { get; set; }
    
    public List<string> ModifiersText { get; set; }
    public List<string> ArtistNames { get; set; }

    [Reference]
    public List<CreativeArtist> Artists { get; set; }
    [Reference]
    public List<CreativeModifier> Modifiers { get; set; }

    [Reference]
    [Format("presentFilesPreview")]
    public List<CreativeArtifact> Artifacts { get; set; }
    
    public string? Error { get; set; }
    
    [References(typeof(AppUser))]
    public int? AppUserId { get; set; }
    public string? Key { get; set; }
    
    public bool Curated { get; set; }
    public int? Rating { get; set; }
    public bool Private { get; set; }
        
    public string RefId { get; set; }
}

[Icon(Svg = Icons.Artifact)]
[AutoApply(Behavior.AuditCreate)]
public class CreativeArtifact : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    [References(typeof(Creative))]
    public int CreativeId { get; set; }

    public string FileName { get; set; }

    [Format(FormatMethods.Attachment)]
    public string FilePath { get; set; }
    public string ContentType { get; set; }

    [Format(FormatMethods.Bytes)]
    public long ContentLength { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }
    public ulong Seed { get; set; }
    public string Prompt { get; set; }
    
    public bool IsPrimaryArtifact { get; set; }
    public bool Nsfw { get; set; }
}

public class QueryCreatives : QueryDb<Creative>
{
    public int? Id { get; set; }
    public int? CreativeId { get; set; }
    public string? CreatedBy { get; set; }
}

[AutoApply(Behavior.AuditCreate)]
[ValidateIsAuthenticated]
public class CreateCreative : ICreateDb<Creative>, IReturn<Creative>
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

[AutoApply(Behavior.AuditModify)]
[ValidateIsAuthenticated]
public class UpdateCreative : IPatchDb<Creative>, IReturn<Creative>
{
    public int Id { get; set; }
    
    public int? PrimaryArtifactId { get; set; }
}

[AutoApply(Behavior.AuditSoftDelete)]
[ValidateIsAuthenticated]
public class DeleteCreative : IDeleteDb<Creative>, IReturnVoid
{
    public int Id { get; set; }
}

[AutoApply(Behavior.AuditDelete)]
[ValidateIsAuthenticated]
public class HardDeleteCreative : IDeleteDb<Creative>, IReturnVoid
{
    public int Id { get; set; }
}

public class QueryCreativeArtifacts : QueryDb<CreativeArtifact>
{
    
}

[AutoApply(Behavior.AuditModify)]
[ValidateIsAuthenticated]
public class UpdateCreativeArtifact : IPatchDb<CreativeArtifact>, IReturn<CreativeArtifact>
{
    public int Id { get; set; }
    
    public bool? Nsfw { get; set; }
}


public class QueryArtists : QueryDb<Artist> {}

public class CreateArtist : ICreateDb<Artist>, IReturn<Artist>
{
    public string? FirstName { get; set; }
    [ValidateNotEmpty, Required]
    public string LastName { get; set; }
    public int? YearDied { get; set; }
    public List<string>? Type { get; set; }
}

public class UpdateArtist : IPatchDb<Artist>, IReturn<Artist>
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? YearDied { get; set; }
    public List<string>? Type { get; set; }
}
public class DeleteArtist : IDeleteDb<Artist>, IReturnVoid 
{
    public int Id { get; set; }
}

[Icon(Svg = Icons.Artist)]
public class Artist
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

public class CreateModifier : ICreateDb<Modifier>, IReturn<Modifier>
{
    [ValidateNotEmpty, Required]
    public string Name { get; set; }
    [ValidateNotEmpty, Required]
    public string Category { get; set; }
    public string? Description { get; set; }
}

public class UpdateModifier : ICreateDb<Modifier>, IReturn<Modifier>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
}

public class DeleteModifier : IDeleteDb<Modifier>, IReturnVoid
{
    public int Id { get; set; }
}


[Icon(Svg = Icons.Modifier)]
public class Modifier
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

public class CreateCreativeArtist : ICreateDb<CreativeArtist>, IReturn<CreativeArtist>
{
    [ValidateGreaterThan(0)]
    public int? CreativeId { get; set; }
    [ValidateGreaterThan(0)]
    public int? ModifierId { get; set; }
}

public class DeleteCreativeArtist : IDeleteDb<CreativeArtist>, IReturnVoid
{
    public int? Id { get; set; }
    public int[]? Ids { get; set; }
}
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

public class CreateCreativeModifier : ICreateDb<CreativeModifier>, IReturn<CreativeModifier>
{
    [ValidateGreaterThan(0)]
    public int? CreativeId { get; set; }
    [ValidateGreaterThan(0)]
    public int? ModifierId { get; set; }
}

public class DeleteCreativeModifier : IDeleteDb<CreativeModifier>, IReturnVoid
{
    public int? Id { get; set; }
    public int[]? Ids { get; set; }
}

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

/// <summary>
/// SQLite FTS table. Uses `rowid` as primary key
/// which is the CreativeArtifact.Id primary key.
///
/// One entry per CreativeArtifact while also
/// using data from Creative.
/// </summary>
public class CreativeArtifactFts
{
    public string rowid { get; set; }
    public int CreativeId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Prompt { get; set; }
    public bool Nsfw { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool Curated { get; set; }
    public int? Rating { get; set; }
    public bool Private { get; set; }
        
    public string RefId { get; set; }
}

