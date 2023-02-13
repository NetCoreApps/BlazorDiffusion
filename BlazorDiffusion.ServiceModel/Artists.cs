using System.Collections.Generic;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace BlazorDiffusion.ServiceModel;

[Tag(Tag.Artists)]
public class QueryArtists : QueryDb<Artist> {}

[Tag(Tag.Artists)]
[ValidateHasRole(AppRoles.Moderator)]
[AutoApply(Behavior.AuditCreate)]
public class CreateArtist : ICreateDb<Artist>, IReturn<Artist>
{
    public string? FirstName { get; set; }
    [ValidateNotEmpty, Required]
    public string LastName { get; set; }
    public int? YearDied { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Type { get; set; }
}

[Tag(Tag.Artists)]
[ValidateHasRole(AppRoles.Moderator)]
[AutoApply(Behavior.AuditModify)]
public class UpdateArtist : IPatchDb<Artist>, IReturn<Artist>
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? YearDied { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Type { get; set; }
}

[Tag(Tag.Artists)]
[ValidateHasRole(AppRoles.Moderator)]
[AutoApply(Behavior.AuditSoftDelete)]
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
    [Default(0)]
    public int Score { get; set; }
    [Default(0)]
    public int Rank { get; set; }
}

