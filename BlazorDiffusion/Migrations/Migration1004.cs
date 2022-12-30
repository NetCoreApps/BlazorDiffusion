using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.Migrations;

[Description("Add Comments")]
public class Migration1004 : MigrationBase
{
    public class ArtifactComment : AuditBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int ArtifactId { get; set; }
        public int? ReplyId { get; set; }
        public string Content { get; set; }
        public int VoteUpCount { get; set; }
        public int VoteDownCount { get; set; }
        public string? FlagReason { get; set; }
        public string? Notes { get; set; }
        public string RefId { get; set; }
        public int AppUserId { get; set; }
    }

    public override void Up()
    {
        Db.CreateTable<ArtifactComment>();
    }

    public override void Down()
    {
        Db.DropTable<ArtifactComment>();
    }
}
