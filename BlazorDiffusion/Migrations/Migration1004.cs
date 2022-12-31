using BlazorDiffusion.ServiceModel;
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
        [Default(0)]
        public int UpVotes { get; set; }
        [Default(0)]
        public int DownVotes { get; set; }
        [Default(0)]
        public int Votes { get; set; }
        public string? FlagReason { get; set; }
        public string? Notes { get; set; }
        public string RefId { get; set; }
        public int AppUserId { get; set; }
    }

    [UniqueConstraint(nameof(ArtifactCommentId), nameof(AppUserId))]
    public class ArtifactCommentVote
    {
        [AutoIncrement]
        public long Id { get; set; }

        [References(typeof(ArtifactComment))]
        public int ArtifactCommentId { get; set; }
        [References(typeof(AppUser))]
        public int AppUserId { get; set; }
        public int Vote { get; set; } // -1 / 1
        public DateTime CreatedDate { get; set; }
    }

    public class ArtifactCommentReport
    {
        [AutoIncrement]
        public long Id { get; set; }

        [References(typeof(ArtifactComment))]
        public int ArtifactCommentId { get; set; }
        [References(typeof(AppUser))]
        public int AppUserId { get; set; }
        public PostReport PostReport { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public enum PostReport
    {
        Offensive,
        Spam,
        Nudity,
        Illegal,
    }

    public override void Up()
    {
        Db.CreateTable<ArtifactComment>();
        Db.CreateTable<ArtifactCommentVote>();
        Db.CreateTable<ArtifactCommentReport>();
    }

    public override void Down()
    {
        Db.DropTable<ArtifactCommentReport>();
        Db.DropTable<ArtifactCommentVote>();
        Db.DropTable<ArtifactComment>();
    }
}
