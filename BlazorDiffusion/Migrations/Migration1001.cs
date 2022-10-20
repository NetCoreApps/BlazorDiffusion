﻿using System.Diagnostics;
using System.Runtime.Serialization;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Gooseai;
using Microsoft.Data.Sqlite;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.Migrations;

public class Migration1001 : MigrationBase
{
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

    public class ArtifactAppUserLike : AuditBase
    {
        [AutoIncrement]
        public long Id { get; set; }
        
        [References(typeof(CreativeArtifact))]
        public int CreativeArtifactId { get; set; }
        [References(typeof(AppUser))]
        public int AppUserId { get; set; }
    }

    public class ArtifactAppUserReport : AuditBase
    {
        [AutoIncrement]
        public long Id { get; set; }
        
        [References(typeof(CreativeArtifact))]
        public int CreativeArtifactId { get; set; }
        [References(typeof(AppUser))]
        public int AppUserId { get; set; }
        
        public bool? Nsfw { get; set; }
        public bool Other { get; set; }
        public string? Description { get; set; }
    }

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
        public bool? Nsfw { get; set; }

        public Int64? AverageHash { get; set; }
        public Int64? PerceptualHash { get; set; }
        public Int64? DifferenceHash { get; set; }
    }

    public class CreativeArtifactFts
    {
        public int CreativeId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Prompt { get; set; }
        public bool? Nsfw { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool Curated { get; set; }
        public int? Rating { get; set; }
        public bool Private { get; set; }
        
        public string RefId { get; set; }
    }
    
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

    public class AppUser : IUserAuth
    {
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
    
        [Index]
        public string Email { get; set; }
    
        public string? ProfileUrl { get; set; }
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
        public int Id { get; set; }
        public string PrimaryEmail { get; set; }
        [IgnoreDataMember]
        public string Salt { get; set; }
        [IgnoreDataMember]
        public string PasswordHash { get; set; }
        [IgnoreDataMember]
        public string DigestHa1Hash { get; set; }
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
    
    class ImageCompareResult
    {
        public int Id { get; set; }
        public ulong PerceptualHash { get; set; }
        public double Similarity { get; set; }
    }

    public override void Up()
    {
        Db.CreateTable<AppUser>();
        Db.CreateTable<Artist>();
        Db.CreateTable<Modifier>();
        Db.CreateTable<Creative>();
        Db.CreateTable<CreativeArtist>();
        Db.CreateTable<CreativeModifier>();
        Db.CreateTable<CreativeArtifact>();
        Db.CreateTable<ArtifactAppUserLike>();
        Db.CreateTable<ArtifactAppUserReport>();

        var seedDir = Path.GetFullPath(Path.Combine("./App_Data/seed"));

        var Artists = File.ReadAllText(seedDir.CombineWith("artists.csv")).FromCsv<List<Artist>>().Select(x => x.BySystemUser()).ToList();
        Db.InsertAll(Artists);

        foreach (var line in File.ReadAllLines(seedDir.CombineWith("modifiers.txt")))
        {
            var category = line.LeftPart(':').Trim();
            var modifiers = line.RightPart(':').Split(',').Select(x => x.Trim()).ToList();
            foreach (var modifier in modifiers)
            {
                Db.Insert(new Modifier { Name = modifier, Category = category }.BySystemUser());
            }
        }

        var appFiles = new DirectoryInfo("./App_Files");
        if(!appFiles.Exists)
            appFiles.Create();
        var seedFromDirectory = new DirectoryInfo("./App_Files/artifacts");
        if(!seedFromDirectory.Exists)
            seedFromDirectory.Create();
        var filesToLoad = seedFromDirectory.GetMatchingFiles("*metadata.json");
        var creativeEntries = new List<Creative>();
        foreach (var file in filesToLoad)
        {
            var creative = File.ReadAllText(file).FromJson<Creative>();
            creative.Prompt = ConstructPrompt(creative.UserPrompt, creative.ModifiersText, creative.ArtistNames);
            creativeEntries.Add(creative);
        }

        var savedModifiers = new Dictionary<string, int>();
        var allMods = Db.Select<Modifier>();
        foreach (var modifier in allMods)
        {
            var isUnique = savedModifiers.TryAdd(modifier.Name.ToLowerInvariant(), modifier.Id);
            if (!isUnique)
                Console.WriteLine($"Duplicate - {modifier.Category}/{modifier.Name.ToLowerInvariant()}");
        }

        var savedArtistIds = new Dictionary<string, int>();
        var allArtists = Db.Select<Artist>();
        foreach (var a in allArtists)
        {
            var isUnique = savedArtistIds.TryAdd($"{a.FirstName} {a.LastName}".ToLowerInvariant(), a.Id);
            if(!isUnique)
                Console.WriteLine($"Duplicate - {a.FirstName} {a.LastName}");
        }
        
        // reset keys
        foreach (var creative in creativeEntries)
        {
            creative.Id = 0;
            creative.Modifiers = new List<CreativeModifier>();
            creative.ModifiersText ??= new List<string>();
            creative.ArtistNames ??= new List<string>();
            creative.AppUserId = null;
            var id = creative.Id = (int)Db.Insert(creative, selectIdentity: true);
            foreach (var text in creative.ModifiersText)
            {
                var mod = savedModifiers[text.ToLowerInvariant()];
                Db.Insert(new CreativeModifier
                {
                    ModifierId = mod,
                    CreativeId = id
                });
            }

            foreach (var artistName in creative.ArtistNames)
            {
                var artist = savedArtistIds[artistName.ToLowerInvariant()];
                Db.Insert(new CreativeArtist
                {
                    ArtistId = artist,
                    CreativeId = id
                });
            }

            var hashAlgorithm = new PerceptualHash();

            foreach (var artifact in creative.Artifacts)
            {
                artifact.Id = 0;
                artifact.CreativeId = id;
                var filePath = artifact.FilePath.Replace("/uploads/", "./App_Files/");
                var filStream = File.OpenRead(filePath);
                artifact.PerceptualHash = (Int64)hashAlgorithm.Hash(filStream);
                Db.Save(artifact);
            }
        }
        
        // Create virtual tables for SQLite Full Text Search
        Db.ExecuteNonQuery($@"CREATE VIRTUAL TABLE {nameof(CreativeArtifactFts)}
USING FTS5(
{nameof(CreativeArtifactFts.Prompt)},
{nameof(CreativeArtifactFts.CreativeId)},
{nameof(CreativeArtifactFts.Width)},
{nameof(CreativeArtifactFts.Height)},
{nameof(CreativeArtifactFts.Nsfw)},
{nameof(CreativeArtifactFts.CreatedDate)},
{nameof(CreativeArtifactFts.Curated)},
{nameof(CreativeArtifactFts.Private)},
{nameof(CreativeArtifactFts.Rating)},
{nameof(CreativeArtifactFts.RefId)});"
        );

        Db.ExecuteNonQuery($@"INSERT INTO {nameof(CreativeArtifactFts)} 
(rowid,
{nameof(CreativeArtifactFts.Prompt)},
{nameof(CreativeArtifactFts.CreativeId)},
{nameof(CreativeArtifactFts.Width)},
{nameof(CreativeArtifactFts.Height)},
{nameof(CreativeArtifactFts.Nsfw)},
{nameof(CreativeArtifactFts.CreatedDate)},
{nameof(CreativeArtifactFts.Curated)},
{nameof(CreativeArtifactFts.Private)},
{nameof(CreativeArtifactFts.Rating)},
{nameof(CreativeArtifactFts.RefId)})
SELECT 
{nameof(CreativeArtifact)}.{nameof(CreativeArtifact.Id)},
{nameof(CreativeArtifact)}.{nameof(CreativeArtifact.Prompt)},
{nameof(CreativeArtifact)}.{nameof(CreativeArtifact.CreativeId)},
{nameof(CreativeArtifact)}.{nameof(CreativeArtifact.Width)},
{nameof(CreativeArtifact)}.{nameof(CreativeArtifact.Height)},
{nameof(CreativeArtifact)}.{nameof(CreativeArtifact.Nsfw)},
{nameof(CreativeArtifact)}.{nameof(CreativeArtifact.CreatedDate)},
{nameof(Creative.Curated)},
{nameof(Creative.Private)},
{nameof(Creative.Rating)},
{nameof(Creative.RefId)} FROM {nameof(CreativeArtifact)}
join {nameof(Creative)} on {nameof(Creative)}.Id = {nameof(CreativeArtifact)}.CreativeId;");

        var artifactTest = Db.Select<CreativeArtifact>(x => x.Id == 25).First();
        var connection = (SqliteConnection)Db.ToDbConnection();
        connection.CreateFunction(
            "imgcompare",
            (Int64 hash1, Int64 hash2)
                => CompareHash.Similarity((ulong)hash1,(ulong)hash2));
        
        var sw = new Stopwatch();
        sw.Start();
        var result = Db.Select<ImageCompareResult>($@"
select FilePath, PerceptualHash, imgcompare({artifactTest.PerceptualHash},PerceptualHash) as Similarity from CreativeArtifact
order by Similarity desc;
");

        sw.Stop();
    }

    private string ConstructPrompt(string userPrompt, List<string> modifiers, List<string> artists)
    {
        var finalPrompt = userPrompt;
        finalPrompt += $", {modifiers.Select(x => x).Join(",").TrimEnd(',')}";
        var artistsSuffix = artists.Select(x => $"inspired by {x}").Join(",").TrimEnd(',');
        if(artists.Count > 0)
            finalPrompt += $", {artistsSuffix}";
        return finalPrompt;
    }

    public override void Down()
    {
        Db.DropTable<ArtifactAppUserLike>();
        Db.DropTable<ArtifactAppUserReport>();
        Db.DropTable<CreativeArtifact>();
        Db.DropTable<CreativeArtist>();
        Db.DropTable<CreativeModifier>();
        Db.DropTable<Creative>();
        Db.DropTable<AppUser>();
        Db.DropTable<Modifier>();
        Db.DropTable<Artist>();
        Db.DropTable<AppUser>();
        Db.DropTable<CreativeArtifactFts>();
    }

}
