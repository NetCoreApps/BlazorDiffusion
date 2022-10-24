using System;
using System.Threading.Tasks;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.ServiceInterface;

public class ArtifactServices : Service
{
    public async Task<object> Post(CreateArtifactLike request)
    {
        var session = await GetSessionAsync();
        var userId = session.UserAuthId.ToInt();
        var row = new ArtifactLike
        {
            AppUserId = userId,
            ArtifactId = request.ArtifactId,
            CreatedDate = DateTime.UtcNow,
        };
        row.Id = await base.Db.InsertAsync(row, selectIdentity: true);
        return row;
    }

    public async Task Delete(DeleteArtifactLike request)
    {
        var session = await GetSessionAsync();
        var userId = session.UserAuthId.ToInt();
        await Db.DeleteAsync<ArtifactLike>(x => x.ArtifactId == request.ArtifactId && x.AppUserId == userId);
    }

    public async Task<object> Post(CreateArtifactReport request)
    {
        var session = await GetSessionAsync();
        var userId = session.UserAuthId.ToInt();
        var row = request.ConvertTo<ArtifactReport>();
        row.AppUserId = userId;
        row.CreatedDate = DateTime.UtcNow;
        row.Id = await base.Db.InsertAsync(row, selectIdentity: true);
        return row;
    }

    public async Task Delete(DeleteArtifactReport request)
    {
        var session = await GetSessionAsync();
        var userId = session.UserAuthId.ToInt();
        await Db.DeleteAsync<ArtifactReport>(x => x.ArtifactId == request.ArtifactId && x.AppUserId == userId);
    }

    public async Task<object> Get(Download request)
    {
        var artifact = !string.IsNullOrEmpty(request.RefId)
            ? await Db.SingleAsync<Artifact>(x => x.RefId == request.RefId)
            : null;
        var file = artifact?.RefId != null
            ? VirtualFiles.GetFile(artifact.FilePath)
            : null;

        if (file == null)
            return HttpError.NotFound("File not found");

        var session = await GetSessionAsync();

        await Db.InsertAsync(new ArtifactStat {
            Type = StatType.Download,
            ArtifactId = artifact.Id,
            AppUserId = session.UserAuthId?.ToInt(),
            RefId = artifact.RefId,
            Source = nameof(Download),
            Version = ServiceStack.Text.Env.VersionString,
            RawUrl = Request.RawUrl,
            RemoteIp = Request.RemoteIp,
            CreatedDate = DateTime.UtcNow,
        });

        return new HttpResult(file, asAttachment:true);
    }
}
