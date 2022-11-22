using BlazorDiffusion.ServiceModel;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.Migrations;

[Description("Add Analytics tables")]
[NamedConnection(Databases.Analytics)]
public class Migration1003 : MigrationBase
{
    [NamedConnection(Databases.Analytics)]
    public class StatBase
    {
        public string RefId { get; set; }
        public int? AppUserId { get; set; }
        public string RawUrl { get; set; }
        public string RemoteIp { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public enum StatType
    {
        Download,
    }

    public class ArtifactStat : StatBase
    {
        [AutoIncrement]
        public int Id { get; set; }

        public StatType Type { get; set; }
        public int ArtifactId { get; set; }
        public string Source { get; set; }
        public string Version { get; set; }
    }

    public class SearchStat : StatBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string? Query { get; set; }
        public string? Similar { get; set; }
        public string? User { get; set; }
        public string? Modifier { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public string? Show { get; set; }
        public string? Source { get; set; }

        public int? ArtifactId { get; set; }
        public int? AlbumId { get; set; }
        public int? ModifierId { get; set; }
        public int? ArtistId { get; set; }
    }

    public enum SignupType
    {
        Updates,
        Beta,
    }
    public class Signup : StatBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public SignupType Type { get; set; }
        public string Email { get; set; }
        public string? Name { get; set; }
        public DateTime? CancelledDate { get; set; }
    }

    public override void Up()
    {
        Db.CreateTable<Signup>();
        Db.CreateTable<ArtifactStat>();
        Db.CreateTable<SearchStat>();
    }

    public override void Down()
    {
        Db.DropTable<SearchStat>();
        Db.DropTable<ArtifactStat>();
        Db.DropTable<Signup>();
    }
}
