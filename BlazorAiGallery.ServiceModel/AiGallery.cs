using ServiceStack;
using ServiceStack.DataAnnotations;

namespace BlazorAiGallery.ServiceModel;

public class AiGallery
{
    [AutoIncrement]
    public int Id { get; set; }
    
    public string Topic { get; set; }
    
    public string Description { get; set; }
    
    public string Curator { get; set; }
}

public class AiGalleryImage
{
    [AutoIncrement]
    public int Id { get; set; }
    
    [References(typeof(AiGallery))]
    public int AiGalleryId { get; set; }

    [References(typeof(CreativeArtifact))]
    public int AiGeneratedFileId { get; set; }
    
    public string Name { get; set; }
    
    public string Description { get; set; }
}

public class QueryAiGallery : QueryDb<AiGallery>
{
    public int? Id { get; set; }
}

public class CreateAiGallery : ICreateDb<AiGallery>, IReturn<AiGallery>
{
    [Required]
    public string Topic { get; set; }
    
    public string Description { get; set; }
    
    public string Curator { get; set; }
}

public class UpdateAiGallery : IUpdateDb<AiGallery>, IReturn<AiGallery>
{
    [Required]
    public int Id { get; set; }
    public string? Topic { get; set; }
    
    public string? Description { get; set; }
    
    public string? Curator { get; set; }
}

public class DeleteAiGallery : IDeleteDb<AiGallery>, IReturnVoid
{
    [Required]
    public int Id { get; set; }
}

public class QueryAiGalleryImage : QueryDb<AiGalleryImage>
{
    public int? Id { get; set; }    
}

public class CreateAiGalleryImage : ICreateDb<AiGalleryImage>, IReturn<AiGalleryImage>
{
    [Required]
    public int AiGalleryId { get; set; }
    
    [Required]
    public int AiGeneratedFileId { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    public string Description { get; set; }
}

public class UpdateAiGalleryImage : IUpdateDb<AiGalleryImage>, IReturn<AiGalleryImage>
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    public int AiGalleryId { get; set; }

    public int AiGeneratedFileId { get; set; }
    
    public string? Name { get; set; }
    
    public string? Description { get; set; }
}

public class DeleteAiGalleryImage : IDeleteDb<AiGalleryImage>, IReturnVoid
{
    [Required]
    public int Id { get; set; }
}



