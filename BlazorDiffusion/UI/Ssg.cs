using BlazorDiffusion.ServiceModel;

namespace BlazorDiffusion.UI;

public interface IGetPageModel
{
    Task OnGetAsync();
}

public class ArtifactImageParams
{
    public Artifact Artifact { get; set; }
    public string? Class { get; set; }
    public string? ImageClass { get; set; }
    public int? MinSize { get; set; }
}
