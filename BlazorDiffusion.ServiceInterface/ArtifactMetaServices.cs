using System;
using System.Threading.Tasks;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.ServiceInterface;

public class ArtifactMetaServices : Service
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
}
