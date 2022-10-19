using System.Threading.Tasks;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.ServiceInterface;

public class ArtifactAppUserLikeService : Service
{
    public async Task Delete(DeleteArtifactAppUserLike request)
    {
        var like = await Db.SingleByIdAsync<ArtifactAppUserLike>(request.Id);
        var session = await GetSessionAsync();
        if (like.AppUserId.ToString() != session.UserAuthId || (await session.HasRoleAsync(RoleNames.Admin, AuthRepositoryAsync)))
        {
            throw HttpError.Unauthorized("Invalid ArtifactAppUserLike Id");
        }

        await Db.DeleteAsync(like);
    }
}