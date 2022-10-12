using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace BlazorAiGallery.Migrations;

public class Migration1001 : MigrationBase
{
    class Creative
    {
        [AutoIncrement]
        public int Id { get; set; }
    
        public string Name { get; set; }
    
        public string Description { get; set; }
    
        [Reference]
        public List<CreativeTask> CreativeTasks { get; set; }
    }

    class CreativeTask
    {
        [AutoIncrement]
        public int Id { get; set; }
    
        [References(typeof(Creative))]
        public int CreativeId { get; set; }
    
        [Reference]
        public Creative Creative { get; set; }
    
        public string Prompt { get; set; }
    
        public string Style { get; set; }
    
        public string Artist { get; set; }
    
        public string? ImageBasisPath { get; set; }
    
        public int NumberOfImages { get; set; }
    
        public int Width { get; set; }
    
        public int Height { get; set; }
    
        public int Steps { get; set; }
    
        [Reference]
        public List<AiGeneratedFile> Files { get; set; }
    }
    
    class AiGeneratedFile
    {
        [AutoIncrement] 
        public int Id { get; set; }
        
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
    
        [References(typeof(CreativeTask))]
        public int CreativeTaskId { get; set; }
    }
    
    class AiGallery
    {
        public int Id { get; set; }
        public string Topic { get; set; }
    
        public string Description { get; set; }
    
        public string Curator { get; set; }
    }

    class AiGalleryImage
    {
        public int Id { get; set; }
    
        [References(typeof(AiGallery))]
        public int AiGalleryId { get; set; }
        
        [References(typeof(AiGeneratedFile))]
        public int AiGeneratedFileId { get; set; }
    
        public string Name { get; set; }
    
        public string Description { get; set; }
    }
    
    public override void Up()
    {
        Db.CreateTable<Creative>();
        Db.CreateTable<CreativeTask>();
        Db.CreateTable<AiGeneratedFile>();
        
        Db.CreateTable<AiGallery>();
        Db.CreateTable<AiGalleryImage>();
    }

    public override void Down()
    {
        Db.DropTable<AiGeneratedFile>();
        Db.DropTable<CreativeTask>();
        Db.DropTable<Creative>();
        
        Db.DropTable<AiGalleryImage>();
        Db.DropTable<AiGallery>();
        
    }
}