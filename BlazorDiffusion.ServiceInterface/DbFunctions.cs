using CoenM.ImageHash;
using Microsoft.Data.Sqlite;
using ServiceStack.OrmLite;
using System;
using System.Data;

namespace BlazorDiffusion.ServiceInterface;
public static class DbFunctions
{
    public static void RegisterImgCompare(this IDbConnection db)
    {
        var connection = (SqliteConnection)db.ToDbConnection();
        connection.CreateFunction(
            "imgcompare",
            (Int64? hash1, Int64? hash2)
                => hash1 == null || hash2 == null
                    ? 0 
                    : CompareHash.Similarity((ulong)hash1, (ulong)hash2));
    }
}
