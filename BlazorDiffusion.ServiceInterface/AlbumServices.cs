using System;
using System.Collections.Generic;
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
        var session = await SessionAsAsync<CustomUserSession>();

        var album = request.ConvertTo<Album>();
        album.Name ??= "New Album";
        album.OwnerId = session.UserAuthId.ToInt();
        album.OwnerRef = session.RefIdStr;
        album.RefId = Guid.NewGuid().ToString("D");
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
            // Delete Album if it no longer contains any Artifacts
            if (!await Db.ExistsAsync<AlbumArtifact>(x => x.AlbumId == album.Id))
            {
                await Db.DeleteByIdAsync<Album>(album.Id);
            }
            else if (album.PrimaryArtifactId != null && request.RemoveArtifactIds.Contains(album.PrimaryArtifactId.Value))
            {
                await Db.UpdateOnlyAsync(() => new Album { PrimaryArtifactId = null }, where: x => x.Id == album.Id);
            }
        }
        if (request.PrimaryArtifactId != null)
        {
            if (request.UnpinPrimaryArtifact != true)
                await Db.UpdateOnlyAsync(() => new Album { PrimaryArtifactId = request.PrimaryArtifactId }, where: x => x.Id == album.Id);
            else
                await Db.UpdateOnlyAsync(() => new Album { PrimaryArtifactId = null }, where: x => x.Id == album.Id);
        }

        var crudContext = CrudContext.Create<Album>(Request, Db, request, AutoCrudOperation.Patch);
        await CrudEvents.RecordAsync(crudContext);

        trans.Commit();

        PublishMessage(new BackgroundTasks {
            ArtifactIdsAddedToAlbums = request.AddArtifactIds,
            ArtifactIdsRemovedFromAlbums = request.RemoveArtifactIds,
        });

        return album;
    }

    public async Task<object> Any(GetCreativesInAlbums request)
    {
        var creativeArtifactIds = Db.ColumnDistinct<int>(Db.From<Creative>()
            .Join<Artifact>((c, a) => c.Id == a.CreativeId && c.Id == request.CreativeId)
            .Select<Artifact>(a => a.Id));

        var q = Db.From<Album>()
            .Join<AlbumArtifact>()
            .Where<AlbumArtifact>(artifact => creativeArtifactIds.Contains(artifact.ArtifactId))
            .SelectDistinct<Album,AlbumArtifact>((album,artifact) => new {
                album.Id,
                album.Name,
                album.RefId,
                album.OwnerRef,
                album.PrimaryArtifactId,
                album.Score,
                artifact.ArtifactId,
            });

        var albumRefs = await Db.SelectAsync<AlbumArtifactResult>(q);

        var albumMap = new Dictionary<int, AlbumResult>();
        foreach (var albumRef in albumRefs)
        {
            var album = albumMap.TryGetValue(albumRef.Id, out var existing)
                ? existing
                : albumMap[albumRef.Id] = new AlbumResult { 
                    Id = albumRef.Id,
                    Name = albumRef.Name,
                    AlbumRef = albumRef.RefId,
                    PrimaryArtifactId = albumRef.PrimaryArtifactId,
                    OwnerRef = albumRef.OwnerRef,
                    Score = albumRef.Score,
                    ArtifactIds = albumRef.PrimaryArtifactId != null 
                        ? new() { albumRef.PrimaryArtifactId.Value } 
                        : new(),
                };

            album.ArtifactIds.AddIfNotExists(albumRef.ArtifactId);
        }

        return new GetCreativesInAlbumsResponse
        {
            Results = albumMap.Values.OrderByDescending(x => x.Score).ThenByDescending(x => x.Id).Take(5).ToList(),
        };
    }
}
