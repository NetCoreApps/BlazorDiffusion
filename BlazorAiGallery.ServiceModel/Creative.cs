using System.Collections.Generic;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace BlazorAiGallery.ServiceModel;

public class Creative
{
    [AutoIncrement]
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    [Reference]
    public List<CreativeTask> CreativeTasks { get; set; }
}

public class CreativeTask
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
    [Format("presentFilesPreview")]
    public List<AiGeneratedFile> Files { get; set; }
}

public class AiGeneratedFile
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

public class QueryCreative : QueryDb<Creative>
{
    public int? Id { get; set; }    
}

public class CreateCreative : ICreateDb<Creative>, IReturn<Creative>
{
    public string Name { get; set; }
    
    public string Description { get; set; }
}

public class UpdateCreative : IPatchDb<Creative>, IReturn<Creative>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class DeleteCreative : IDeleteDb<Creative>, IReturnVoid
{
    public int Id { get; set; }
}

public class QueryCreativeTask : QueryDb<CreativeTask>
{
    public int? Id { get; set; }
    public int? CreativeId { get; set; }
}

public class CreateCreativeTask : ICreateDb<CreativeTask>, IReturn<CreativeTask>
{
    public int CreativeId { get; set; }

    [Required]
    public string Prompt { get; set; }
    
    public string? Style { get; set; }
    
    public string? Artist { get; set; }
    
    [AutoDefault(Value = 4)]
    public int? Images { get; set; }
    
    [AutoDefault(Value = 512)]
    public int? Width { get; set; }
    
    [AutoDefault(Value = 512)]
    public int? Height { get; set; }
    
    [AutoDefault(Value = 50)]
    public int? Steps { get; set; }

    public long? Seed { get; set; }
}

public class DeleteCreativeTask : IDeleteDb<CreativeTask>, IReturnVoid
{
    public int Id { get; set; }
}

public class QueryAiGeneratedFile : QueryDb<AiGeneratedFile>
{
    
}

