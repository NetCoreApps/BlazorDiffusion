using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorDiffusion.ServiceModel;

public static class AppData
{
    public const int MaxArtiactSize = 10 * 1024 * 1024;
    public const int MaxAvatarSize = 1024 * 1024;

    static List<NavItem> DefaultLinks { get; set; } = new() { };
    static List<NavItem> AdminLinks { get; set; } = new() {
        new NavItem { Label = "Admin", Href = "/admin" },
    };
    public static List<NavItem> GetNavItems(bool isAdmin) => isAdmin ? AdminLinks : DefaultLinks;

    public static List<Group> CategoryGroups = new Group[] {
        new() { Name = "Scene",     Items = new[] { "Quality", "Style", "Aesthetic", "Features", "Medium", "Setting", "Theme" } },
        new() { Name = "Effects",   Items = new[] { "Effects", "CGI", "Filters", "Lenses", "Photography", "Lighting", "Color" } },
        new() { Name = "Art Style", Items = new[] { "Art Movement", "Art Style", "18 Century", "19 Century", "20 Century", "21 Century" } },
        new() { Name = "Mood",      Items = new[] { "Positive Mood", "Negative Mood" } },
    }.ToList();
}

public class AppSource
{
    public const string Top = "top";
    public const string User = "user";
    public const string InAlbum = "in";
}