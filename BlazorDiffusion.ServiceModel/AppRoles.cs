namespace BlazorDiffusion;

public static class AppRoles
{
    public const string Admin = nameof(Admin);
    public const string Moderator = nameof(Moderator);
    public const string Creator = nameof(Creator);

    public static string[] All { get; set; } = { Admin, Moderator, Creator };
}
