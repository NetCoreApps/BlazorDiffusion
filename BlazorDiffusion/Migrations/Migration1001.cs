using System.Data;
using System.Diagnostics;
using System.Runtime.Serialization;
using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Microsoft.Data.Sqlite;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

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
        public string? Key { get; set; }

        public bool Curated { get; set; }
        public int? Rating { get; set; }
        public bool Private { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; }
        public string RefId { get; set; }
    }

    public class ArtifactLike
    {
        [AutoIncrement]
        public long Id { get; set; }
        
        [References(typeof(Artifact))]
        public int ArtifactId { get; set; }
        [References(typeof(AppUser))]
        public int AppUserId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ArtifactReport
    {
        [AutoIncrement]
        public long Id { get; set; }

        [References(typeof(Artifact))]
        public int ArtifactId { get; set; }
        [References(typeof(AppUser))]
        public int AppUserId { get; set; }

        public ReportType Type { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Notes { get; set; }
        public DateTime? ActionedDate { get; set; }
        public string? ActionedBy { get; set; }
    }

    public enum ReportType
    {
        Nsfw,
        Other,
    }

    public class Artifact : AuditBase
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
        // Dominant Color to show before download
        public string Background { get; set; }
        // Low Quality Image Placeholder for fast load in
        public string Lqip { get; set; }
        // Set Low Quality images to:
        //  - Malformed: -1
        //  - Blurred: -2
        //  - LowQuality: -3
        public int Quality { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; }
        public string RefId { get; set; }
    }

    public class ArtifactFts
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
        [AutoIncrement]
        public int Id { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Handle { get; set; }
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

    public class Album : AuditBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        public List<string> Tags { get; set; }
        public string RefId { get; set; }
        [References(typeof(AppUser))]
        public int OwnerId { get; set; }
        public int? PrimaryArtifactId { get; set; }
        public bool Private { get; set; }
        public int? Rating { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; }
        [Reference]
        public List<AlbumArtifact> Artifacts { get; set; }
    }

    public class AlbumArtifact
    {
        [AutoIncrement]
        public long Id { get; set; }

        [References(typeof(Album))]
        public int AlbumId { get; set; }
        [References(typeof(Artifact))]
        public int ArtifactId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        [Reference]
        public List<Artifact> Artifact { get; set; }
    }

    public class AlbumLike
    {
        [AutoIncrement]
        public long Id { get; set; }

        [References(typeof(Album))]
        public int AlbumId { get; set; }
        [References(typeof(AppUser))]
        public int AppUserId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public enum StatType
    {
        Download,
    }

    public class ArtifactStat
    {
        [AutoIncrement]
        public int Id { get; set; }

        public StatType Type { get; set; }
        public int ArtifactId { get; set; }

        [References(typeof(AppUser))]
        public int? AppUserId { get; set; }
        public string RefId { get; set; }
        public string Source { get; set; }
        public string Version { get; set; }
        public string RawUrl { get; set; }
        public string RemoteIp { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ImageCompareResult
    {
        public int Id { get; set; }
        public long PerceptualHash { get; set; }
        public double Similarity { get; set; }
    }

    OrmLiteAuthRepository<AppUser, UserAuthDetails> CreateAuthRepo() => new(DbFactory) { UseDistinctRoleTables = true };

    public override void Up()
    {
        var authRepo = CreateAuthRepo();
        authRepo.InitSchema(Db);

        void CreateUser(string email, string name, string password, string[]? roles = null)
        {
            var newAdmin = new AppUser { Email = email, DisplayName = name, RefIdStr = Guid.NewGuid().ToString("D") };
            var user = authRepo.CreateUserAuth(Db, newAdmin, password);
            if (roles?.Length > 0)
            {
                authRepo.AssignRoles(Db, user.Id.ToString(), roles);
            }
        }

        CreateUser("admin@email.com", "Admin User", "p@55wOrd", roles: new[] { RoleNames.Admin });
        CreateUser("system@email.com", "System", "p@55wOrd", roles: new[] { AppRoles.Moderator });
        CreateUser("demis@servicestack.com", "Demis", "p@55wOrd", roles: new[] { AppRoles.Moderator });
        CreateUser("darren@servicestack.com", "Darren", "p@55wOrd", roles: new[] { AppRoles.Moderator });
        CreateUser("test@user.com", "Test", "p@55wOrd");
        
        Db.CreateTable<Artist>();
        Db.CreateTable<Modifier>();
        Db.CreateTable<Creative>();
        Db.CreateTable<CreativeArtist>();
        Db.CreateTable<CreativeModifier>();
        Db.CreateTable<Artifact>();
        Db.CreateTable<ArtifactLike>();
        Db.CreateTable<ArtifactReport>();
        Db.CreateTable<Album>();
        Db.CreateTable<AlbumArtifact>();
        Db.CreateTable<AlbumLike>();
        Db.CreateTable<ArtifactStat>();

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

        var allArtifactLikeRefs = File.ReadAllText(seedDir.CombineWith("artifact-likes.csv")).FromCsv<List<ArtifactLikeRef>>();


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
            creative.Prompt = ConstructPrompt(creative.UserPrompt, creative.ModifierNames, creative.ArtistNames);
            creativeEntries.Add(creative);
        }

        var savedModifiers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var allMods = Db.Select<Modifier>();
        foreach (var modifier in allMods)
        {
            var isUnique = savedModifiers.TryAdd(modifier.Name.ToLowerInvariant(), modifier.Id);
            if (!isUnique)
                Console.WriteLine($"Duplicate - {modifier.Category}/{modifier.Name.ToLowerInvariant()}");
        }

        var savedArtistIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var allArtists = Db.Select<Artist>();
        foreach (var a in allArtists)
        {
            var isUnique = savedArtistIds.TryAdd($"{a.FirstName} {a.LastName}".ToLowerInvariant(), a.Id);
            if (!isUnique)
                Console.WriteLine($"Duplicate - {a.FirstName} {a.LastName}");
        }
        
        // reset keys
        foreach (var creative in creativeEntries)
        {
            creative.Id = 0;
            creative.Modifiers = new List<CreativeModifier>();
            creative.ModifierNames ??= new List<string>();
            creative.ArtistNames ??= new List<string>();
            creative.OwnerId = creative.CreatedBy != null ? creative.CreatedBy.ToInt() : 2;
            var id = creative.Id = (int)Db.Insert(creative, selectIdentity: true);
            foreach (var text in creative.ModifierNames)
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
                if (savedArtistIds.TryGetValue(artistName, out var artist))
                {
                    Db.Insert(new CreativeArtist
                    {
                        ArtistId = artist,
                        CreativeId = id
                    });
                }
                else
                {
                    Console.WriteLine($"NotFound: Artist {artistName}");
                }
            }

            var hashAlgorithm = new PerceptualHash();

            var primaryArtifact = creative.PrimaryArtifactId != null 
                ? creative.Artifacts.FirstOrDefault(x => x.Id == creative.PrimaryArtifactId)
                : null;

            foreach (var artifact in creative.Artifacts)
            {
                artifact.Id = 0;
                artifact.CreativeId = id;
                var filePath = $"./App_Files/{artifact.FilePath}";
                var filStream = File.OpenRead(filePath);
                artifact.PerceptualHash = (Int64)hashAlgorithm.Hash(filStream);
                artifact.Id = (int)Db.Insert(artifact, selectIdentity: true);
                
                if (artifact == primaryArtifact)
                {
                    creative.PrimaryArtifactId = artifact.Id;
                    Db.UpdateOnly(() => new Creative { PrimaryArtifactId = artifact.Id }, 
                        where: x => x.Id == creative.Id);
                }

                var artifactLikeRefs = allArtifactLikeRefs.Where(x => x.RefId == artifact.RefId).ToList();
                if (artifactLikeRefs.Count > 0)
                {
                    foreach (var artifactLikeRef in artifactLikeRefs)
                    {
                        var artistLike = X.Apply(artifactLikeRef.ConvertTo<ArtifactLike>(), x => x.ArtifactId = artifact.Id);
                        Db.Insert(artistLike);
                    }
                }
            }
        }
        
        // Create virtual tables for SQLite Full Text Search
        Db.ExecuteNonQuery($@"CREATE VIRTUAL TABLE {nameof(ArtifactFts)}
USING FTS5(
{nameof(ArtifactFts.Prompt)},
{nameof(ArtifactFts.CreativeId)},
{nameof(ArtifactFts.Width)},
{nameof(ArtifactFts.Height)},
{nameof(ArtifactFts.Nsfw)},
{nameof(ArtifactFts.CreatedDate)},
{nameof(ArtifactFts.Curated)},
{nameof(ArtifactFts.Private)},
{nameof(ArtifactFts.Rating)},
{nameof(ArtifactFts.RefId)});"
        );

        Db.ExecuteNonQuery($@"INSERT INTO {nameof(ArtifactFts)} 
(rowid,
{nameof(ArtifactFts.Prompt)},
{nameof(ArtifactFts.CreativeId)},
{nameof(ArtifactFts.Width)},
{nameof(ArtifactFts.Height)},
{nameof(ArtifactFts.Nsfw)},
{nameof(ArtifactFts.CreatedDate)},
{nameof(ArtifactFts.Curated)},
{nameof(ArtifactFts.Private)},
{nameof(ArtifactFts.Rating)},
{nameof(ArtifactFts.RefId)})
SELECT 
{nameof(Artifact)}.{nameof(Artifact.Id)},
{nameof(Artifact)}.{nameof(Artifact.Prompt)},
{nameof(Artifact)}.{nameof(Artifact.CreativeId)},
{nameof(Artifact)}.{nameof(Artifact.Width)},
{nameof(Artifact)}.{nameof(Artifact.Height)},
{nameof(Artifact)}.{nameof(Artifact.Nsfw)},
{nameof(Artifact)}.{nameof(Artifact.CreatedDate)},
{nameof(Creative.Curated)},
{nameof(Creative.Private)},
{nameof(Creative.Rating)},
{nameof(Creative)}.{nameof(Creative.RefId)} FROM {nameof(Artifact)}
join {nameof(Creative)} on {nameof(Creative)}.Id = {nameof(Artifact)}.CreativeId;");

        var artifactTest = Db.Select<Artifact>(x => x.Id == 25).First();

        Db.RegisterImgCompare();
           
        var sw = new Stopwatch();
        sw.Start();
        var result = Db.Select<ImageCompareResult>($@"
select FilePath, PerceptualHash, imgcompare({artifactTest.PerceptualHash},PerceptualHash) as Similarity from Artifact
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
        Db.DropTable<ArtifactStat>();
        Db.DropTable<AlbumLike>();
        Db.DropTable<AlbumArtifact>();
        Db.DropTable<Album>();
        Db.DropTable<ArtifactLike>();
        Db.DropTable<ArtifactReport>();
        Db.DropTable<Artifact>();
        Db.DropTable<CreativeArtist>();
        Db.DropTable<CreativeModifier>();
        Db.DropTable<Creative>();
        Db.DropTable<AppUser>();
        Db.DropTable<Modifier>();
        Db.DropTable<Artist>();
        Db.DropTable<ArtifactFts>();

        var authRepo = CreateAuthRepo();
        authRepo.DropSchema(Db);
    }

}
