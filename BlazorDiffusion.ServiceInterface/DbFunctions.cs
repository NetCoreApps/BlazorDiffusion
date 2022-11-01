using BlazorDiffusion.ServiceModel;
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
        var sqliteConn = (SqliteConnection)db.ToDbConnection();
        sqliteConn.CreateFunction(
            "imgcompare",
            (Int64? hash1, Int64? hash2)
                => hash1 == null || hash2 == null
                    ? 0
                    : CompareHash.Similarity((ulong)hash1, (ulong)hash2));
    }
    public static void RegisterBgCompare(this IDbConnection db)
    {
        var sqliteConn = (SqliteConnection)db.ToDbConnection();
        sqliteConn.CreateFunction("bgcompare", (string a, string b) => ImageUtils.BackgroundCompare(a, b));
    }
}
