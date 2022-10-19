using System.Runtime.Serialization;
using Gooseai;
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
        
        public bool Nsfw { get; set; }
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
        public bool IsPrimaryArtifact { get; set; }
        public bool Nsfw { get; set; }
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

            foreach (var artifact in creative.Artifacts)
            {
                artifact.Id = 0;
                artifact.CreativeId = id;
                Db.Save(artifact);
            }
        }
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
    }

}
