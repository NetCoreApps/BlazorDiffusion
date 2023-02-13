using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;

namespace BlazorDiffusion.ServiceModel;

public class AppUser : IUserAuth
{
    [AutoIncrement]
    public int Id { get; set; }
    public string UserName { get; set; }
    public string DisplayName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    [Index(Unique = true)]
    public string? Handle { get; set; }
    public string Company { get; set; }
    [Index]
    public string Email { get; set; }
    public string? ProfileUrl { get; set; }
    [Input(Type="file"), UploadTo("avatars")]
    public string? Avatar { get; set; } //overrides ProfileUrl
    public string? LastLoginIp { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public string BirthDateRaw { get; set; }
    public string Address { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string Culture { get; set; }
    public string FullName { get; set; }
    public string Gender { get; set; }
    public string Language { get; set; }
    public string MailAddress { get; set; }
    public string Nickname { get; set; }
    public string PostalCode { get; set; }
    public string TimeZone { get; set; }
    public Dictionary<string, string> Meta { get; set; }
    public string PrimaryEmail { get; set; }
    [IgnoreDataMember] public string Salt { get; set; }
    [IgnoreDataMember] public string PasswordHash { get; set; }
    [IgnoreDataMember] public string DigestHa1Hash { get; set; }
    public List<string> Roles { get; set; }
    public List<string> Permissions { get; set; }
    public int? RefId { get; set; }
    public string RefIdStr { get; set; }
    public int InvalidLoginAttempts { get; set; }
    public DateTime? LastLoginAttempt { get; set; }
    public DateTime? LockedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
