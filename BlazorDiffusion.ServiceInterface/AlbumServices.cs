using System;
using System.Linq;
using System.Threading.Tasks;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.ServiceInterface;

public class AlbumServices : Service
{
    public IAutoQueryDb AutoQuery { get; set; }
    public ICrudEvents CrudEvents { get; set; }

    public async Task<object> Any(CreateAlbum request)
    {
        var session = await GetSessionAsync();

        var album = request.ConvertTo<Album>();
        album.Name ??= "New Album";
        album.OwnerId = session.UserAuthId.ToInt();
        album.WithAudit(session.UserAuthId);

        using var trans = Db.OpenTransaction();

        // TODO CrudEvents.RecordAsync
        album.Id = (int)await Db.InsertAsync(album, selectIdentity: true);

        if (request.ArtifactIds?.Count > 0)
        {
            var albumArtifacts = request.ArtifactIds.Map(x => new AlbumArtifact
            {
                AlbumId = album.Id,
                ArtifactId = x,
                CreatedDate = album.CreatedDate,
                ModifiedDate = album.ModifiedDate,
            });
            await Db.InsertAllAsync(albumArtifacts);
        }

        var crudContext = CrudContext.Create<Album>(Request, Db, request, AutoCrudOperation.Create);
        await CrudEvents.RecordAsync(crudContext);

        trans.Commit();

        return album;
    }

    public async Task<object> Any(UpdateAlbum request)
    {
        var session = await GetSessionAsync();
        
        var album = await Db.LoadSingleByIdAsync<Album>(request.Id);
        if (album == null)
            throw HttpError.NotFound("Album not found");

        if (!await session.IsOwnerOrModerator(AuthRepositoryAsync, album.OwnerId))
            throw HttpError.Forbidden("You don't own this Album");

        album.WithAudit(session.UserAuthId);
        
        using var trans = Db.OpenTransaction();
        await Db.UpdateNonDefaultsAsync(album, x => x.Id == album.Id);

        if (request.AddArtifactIds?.Count > 0)
        {
            var albumArtifacts = request.AddArtifactIds.Where(x => !album.Artifacts.OrEmpty().Any(a => a.ArtifactId == x))
                .Map(x => new AlbumArtifact {
                    AlbumId = album.Id,
                    ArtifactId = x,
                    CreatedDate = album.CreatedDate,
                    ModifiedDate = album.ModifiedDate,
                });
            await Db.InsertAllAsync(albumArtifacts);
        }
        if (request.RemoveArtifactIds?.Count > 0)
        {
            await Db.DeleteAsync<AlbumArtifact>(x => x.AlbumId == album.Id && request.RemoveArtifactIds.Contains(x.ArtifactId));
        }

        var crudContext = CrudContext.Create<Album>(Request, Db, request, AutoCrudOperation.Patch);
        await CrudEvents.RecordAsync(crudContext);

        trans.Commit();

        return album;
    }

}
