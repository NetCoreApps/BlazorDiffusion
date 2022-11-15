using System.Collections.Generic;
using System;

namespace BlazorDiffusion.ServiceModel;

public static class Databases
{
    // Keep heavy writes of stats + analytics in separate DB
    public const string Analytics = nameof(Analytics);
}

public static class Users
{
    public static AppUser Admin = new()
    {
        Id = 1,
        Email = "admin@email.com",
        DisplayName = "Admin User",
        RefIdStr = "b496e043-3e5b-4410-b0e5-1c9cca04c07f",
        Roles = new() { AppRoles.Admin },
    };
    public static AppUser System = new()
    {
        Id = 2,
        Email = "system@email.com",
        DisplayName = "System",
        RefIdStr = "cd1bbe7e-2038-4b43-9086-32c790485588",
        Roles = new() { AppRoles.Moderator },
    };
    public static AppUser Demis = new()
    {
        Id = 3,
        Email = "demis@servicestack.com",
        DisplayName = "Demis",
        RefIdStr = "865d5f4a-4c58-461d-b1b8-2aac005cd2bc",
        Roles = new() { AppRoles.Moderator },
        Handle = "mythz",
        Avatar = "/avatars/86/865d5f4a-4c58-461d-b1b8-2aac005cd2bc/kerrigan_128.png",
    };
    public static AppUser Darren = new()
    {
        Id = 4,
        Email = "darren@servicestack.com",
        DisplayName = "Darren",
        RefIdStr = "16846ea4-2bb6-4c58-a999-985dac3c31a2",
        Roles = new() { AppRoles.Moderator },
    };
    public static AppUser Test = new()
    {
        Id = 5,
        Email = "test@user.com",
        DisplayName = "Test",
        RefIdStr = "3823c5af-d0b6-4738-8601-bd91bf6f9771",
        Handle = "imagineer",
    };

    public static AppUser GetUserById(string? userId) => string.IsNullOrEmpty(userId)
        ? System
        : GetUserById(int.Parse(userId));
    public static AppUser GetUserById(int? userId) => userId switch
    {
        1 => Admin,
        2 => System,
        3 => Demis,
        4 => Darren,
        5 => Test,
        _ => System,
    };

    public static bool IsAdminOrSystem(int? appUserId) => appUserId == 1 || appUserId == 2;
}
