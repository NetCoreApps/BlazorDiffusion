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
    
        public int Images { get; set; }
    
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

        Db.Insert(new Creative
        {
            Name = "Test1",
            Description = "Test2",
            Id = 1
        });
        Db.Insert(new CreativeTask
        {
            Height = 512,
            Width = 512,
            Id = 1,
            CreativeId = 1,
            Prompt = "A dream of a distant galaxy, by Caspar David Friedrich, matte painting trending on artstation HQ",
            Images = 4,
            Steps = 50
        });
        Db.Insert(new AiGeneratedFile
        {
            Height = 512,
            Width = 512,
            Prompt = "A dream of a distant galaxy, by Caspar David Friedrich, matte painting trending on artstation HQ",
            Seed = 1134476444,
            ContentLength = 417639,
            Id = 1,
            ContentType = "image/png",
            FileName = "output_1134476444.png",
            FilePath = "/uploads/fs/1/task/1/output_1134476444.png",
            CreativeTaskId = 1
        });
        Db.Insert(new AiGeneratedFile
        {
            Height = 512,
            Width = 512,
            Prompt = "A dream of a distant galaxy, by Caspar David Friedrich, matte painting trending on artstation HQ",
            Seed = 2130171066,
            ContentLength = 415669,
            Id = 2,
            ContentType = "image/png",
            FileName = "output_2130171066.png",
            FilePath = "/uploads/fs/1/task/1/output_2130171066.png",
            CreativeTaskId = 1
        });
        Db.Insert(new AiGeneratedFile
        {
            Height = 512,
            Width = 512,
            Prompt = "A dream of a distant galaxy, by Caspar David Friedrich, matte painting trending on artstation HQ",
            Seed = 2669329965,
            ContentLength = 363970,
            Id = 3,
            ContentType = "image/png",
            FileName = "output_2669329965.png",
            FilePath = "/uploads/fs/1/task/1/output_2669329965.png",
            CreativeTaskId = 1
        });
        Db.Insert(new AiGeneratedFile
        {
            Height = 512,
            Width = 512,
            Prompt = "A dream of a distant galaxy, by Caspar David Friedrich, matte painting trending on artstation HQ",
            Seed = 3635816568,
            ContentLength = 379902,
            Id = 4,
            ContentType = "image/png",
            FileName = "output_3635816568.png",
            FilePath = "/uploads/fs/1/task/1/output_3635816568.png",
            CreativeTaskId = 1
        });

        Db.Insert(new AiGallery
        {
            Id = 1,
            Topic = "Amazing Art",
            Description = "Test",
            Curator = "DR"
        });
        Db.Insert(new AiGalleryImage
        {
            Description = "TEst3",
            Name = "Test",
            Id = 1,
            AiGalleryId = 1,
            AiGeneratedFileId = 2
        });
    }

    public override void Down()
    {
        Db.DropTable<AiGalleryImage>();
        Db.DropTable<AiGeneratedFile>();
        Db.DropTable<AiGallery>();
        Db.DropTable<CreativeTask>();
        Db.DropTable<Creative>();
        
    }
}