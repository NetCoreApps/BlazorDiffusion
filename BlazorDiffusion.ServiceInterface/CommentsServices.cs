using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.ServiceInterface;

public class CommentsServices : Service
{
    public IAutoQueryDb AutoQuery { get; set; } = default!;
    public ICrudEvents CrudEvents { get; set; }

    public async Task<object> Any(QueryArtifactComments query)
    {
        using var db = AutoQuery.GetDb(query, base.Request);
        var q = AutoQuery.CreateQuery(query, base.Request, db);
        var response = await AutoQuery.ExecuteAsync(query, q, base.Request, db);
        return response;
    }

    public async Task<object> Any(CreateArtifactComment request)
    {
        var session = await SessionAsAsync<CustomUserSession>();
        string userAuthId = session.UserAuthId;
        var userId = userAuthId.ToInt();

        var row = request.ConvertTo<ArtifactComment>().WithAudit(Request);
        row.RefId = Guid.NewGuid().ToString("D");
        row.AppUserId = userId;

        using var trans = Db.OpenTransaction();
        row.Id = (int)await Db.InsertAsync(row, selectIdentity: true);

        var crudContext = CrudContext.Create<ArtifactComment>(Request, Db, request, AutoCrudOperation.Create);
        await CrudEvents.RecordAsync(crudContext);

        trans.Commit();

        return row;
    }
}
