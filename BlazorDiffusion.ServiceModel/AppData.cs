using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlazorDiffusion.ServiceModel;

public class AppData
{
    public const string Title = "Blazor Diffusion";

    public const int MaxArtiactSize = 10 * 1024 * 1024;
    public const int MaxAvatarSize = 1024 * 1024;

    public static AppData Instance { get; private set; } = new();
    
    List<NavItem> DefaultLinks { get; set; } = new() { };
    List<NavItem> AdminLinks { get; set; } = new() {
        new NavItem { Label = "Admin", Href = "/admin" },
    };
    public static List<NavItem> GetNavItems(bool isAdmin) => isAdmin ? Instance.AdminLinks : Instance.DefaultLinks;

    public static List<Group> CategoryGroups = new Group[] {
        new() { Name = "Scene",     Items = new[] { "Quality", "Style", "Aesthetic", "Features", "Medium", "Setting", "Theme" } },
        new() { Name = "Effects",   Items = new[] { "Effects", "CGI", "Filters", "Lenses", "Photography", "Lighting", "Color" } },
        new() { Name = "Art Style", Items = new[] { "Art Movement", "Art Style", "18 Century", "19 Century", "20 Century", "21 Century" } },
        new() { Name = "Mood",      Items = new[] { "Positive Mood", "Negative Mood" } },
    }.ToList();

    string[]? categories;
    public string[] Categories => categories ??= CategoryGroups.SelectMany(x => x.Items).OrderBy(x => x).ToArray();
}

public class AppSource
{
    public const string Top = "top";
    public const string User = "user";
    public const string InAlbum = "in";
}