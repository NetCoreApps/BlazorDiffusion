using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;
using System.Threading.Tasks;

namespace BlazorDiffusion.ServiceInterface;

public class MqServices : Service
{
    public IStableDiffusionClient StableDiffusionClient { get; set; }

    public async Task Any(SaveMetadata request)
    {
        var creative = request.Creative ?? (request.CreativeId != null
            ? await Db.LoadSingleByIdAsync<Creative>(request.CreativeId)
            : null);

        if (creative != null)
        {
            await StableDiffusionClient.SaveCreativeAsync(creative);
        }
    }

    public async Task<object> Get(ViewCreativeMetadata request)
    {
        var creative = await Db.SingleByIdAsync<Creative>(request.CreativeId);
        var metadataFile = creative != null ? StableDiffusionClient.GetMetadataFile(creative) : null;
        if (metadataFile == null)
            return HttpError.NotFound("Creative not found");

        var json = metadataFile.ReadAllText();
        var metadataCreative = json.FromJson<Creative>();
        return metadataCreative;
    }

    public async Task Any(BackgroundTasks request)
    {
        if (request.RecordArtifactStat != null)
            await Db.InsertAsync(request.RecordArtifactStat);
        if (request.RecordSearchStat != null)
            await Db.InsertAsync(request.RecordSearchStat);
    }
}
