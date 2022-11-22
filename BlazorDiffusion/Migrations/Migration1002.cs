using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.Migrations;

[Description("Create FTS Tables")]
public class Migration1002 : MigrationBase
{
    public class ArtifactFts
    {
        public int rowid { get; set; }
        public int CreativeId { get; set; }
        public string Prompt { get; set; }
        public string RefId { get; set; }
    }

    public override void Up()
    {
        // Create virtual tables for SQLite Full Text Search
        Db.ExecuteNonQuery($@"CREATE VIRTUAL TABLE {nameof(ArtifactFts)}
USING FTS5(
{nameof(ArtifactFts.Prompt)},
{nameof(ArtifactFts.CreativeId)},
{nameof(ArtifactFts.RefId)});"
        );

        Db.ExecuteNonQuery($@"INSERT INTO {nameof(ArtifactFts)} 
(rowid,
{nameof(ArtifactFts.Prompt)},
{nameof(ArtifactFts.CreativeId)},
{nameof(ArtifactFts.RefId)})
SELECT 
{nameof(Migration1001.Artifact.Id)},
{nameof(Migration1001.Artifact.Prompt)},
{nameof(Migration1001.Artifact.CreativeId)},
{nameof(Migration1001.Artifact.RefId)} FROM {nameof(Migration1001.Artifact)}");
    }
    public override void Down()
    {
        Db.DropTable<ArtifactFts>();
    }
}
