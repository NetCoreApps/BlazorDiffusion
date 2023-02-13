using ServiceStack;
using ServiceStack.DataAnnotations;

namespace BlazorDiffusion.ServiceModel;


[Tag(Tag.Modifiers)]
public class QueryModifiers : QueryDb<Modifier> { }

[Tag(Tag.Modifiers)]
[ValidateHasRole(AppRoles.Moderator)]
[AutoApply(Behavior.AuditCreate)]
public class CreateModifier : ICreateDb<Modifier>, IReturn<Modifier>
{
    [ValidateNotEmpty, Required]
    public string Name { get; set; }
    [ValidateNotEmpty, Required]
    [Input(Type="select", EvalAllowableValues = "AppData.Categories")]
    public string Category { get; set; }
    public string? Description { get; set; }
}

[Tag(Tag.Modifiers)]
[ValidateHasRole(AppRoles.Moderator)]
[AutoApply(Behavior.AuditModify)]
public class UpdateModifier : IPatchDb<Modifier>, IReturn<Modifier>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    [Input(Type = "select", EvalAllowableValues = "AppData.Categories")]
    public string? Category { get; set; }
    public string? Description { get; set; }
}

[Tag(Tag.Modifiers)]
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
    [Default(0)]
    public int Score { get; set; }
    [Default(0)]
    public int Rank { get; set; }
}
