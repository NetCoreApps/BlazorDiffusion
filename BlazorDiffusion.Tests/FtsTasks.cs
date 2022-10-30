using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.Text;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using BlazorDiffusion.ServiceModel;
using System.Diagnostics;
using Amazon.DynamoDBv2.DocumentModel;
using ServiceStack.OrmLite.Legacy;

namespace BlazorDiffusion.Tests;

[Explicit]
public class FtsTasks
{
    IDbConnectionFactory ResolveDbFactory() => new ConfigureDb().ConfigureAndResolve<IDbConnectionFactory>();
    public string GetHostDir()
    {
        JsConfig.Init(new Config {
            TextCase = TextCase.CamelCase,
        });

        var appSettings = JSON.parse(File.ReadAllText(Path.GetFullPath("appsettings.json")));
        return appSettings.ToObjectDictionary()["HostDir"].ToString()!;
    }


    [Test]
    public void Can_populate_and_query_ArtifactFts()
    {
        var DbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        var Db = DbFactory.OpenDbConnection();

        var testHostDir = Path.GetFullPath("../../..");
        TestDatabase.CreateDatabase(DbFactory, Db, testHostDir);

        var sw = Stopwatch.StartNew();

        Db.ExecuteNonQuery($@"CREATE VIRTUAL TABLE {nameof(ArtifactFts)}
USING FTS5(
{nameof(ArtifactFts.Prompt)},
{nameof(ArtifactFts.CreativeId)},
{nameof(ArtifactFts.RefId)});"
        );

        var rowsAdded = Db.ExecuteNonQuery($@"INSERT INTO {nameof(ArtifactFts)} 
(rowid,
{nameof(ArtifactFts.Prompt)},
{nameof(ArtifactFts.CreativeId)},
{nameof(ArtifactFts.RefId)})
SELECT 
{nameof(Artifact.Id)},
{nameof(Artifact.Prompt)},
{nameof(Artifact.CreativeId)},
{nameof(Artifact.RefId)} FROM {nameof(Artifact)}");

        $"ArtifactFts rowsAdded: {rowsAdded}, took {sw.ElapsedMilliseconds}ms".Print();

        OrmLiteUtils.PrintSql();

        var query = "\"salma\"*"; //escape

        var q = Db.From<Artifact>()
            .Join<Creative>((a, c) => c.Id == a.CreativeId && a.Id == c.PrimaryArtifactId);

        q.OrderByDescending(x => x.Quality);

        //q.Where<Creative>(x => x.Prompt.Contains(query));
        q.Join<ArtifactFts>((a,f) => a.Id == f.rowid);
        q.Where(q.Column<ArtifactFts>(x => x.Prompt, prefixTable:true) + " match {0}", query);
        q.ThenBy(q.Column<ArtifactFts>("Rank", prefixTable: true));

        q.ThenByDescending(x => new { x.Score, x.Id });
        // Need distinct else Blazor @key throws when returning dupes
        q.SelectDistinct<Artifact, Creative>((a, c) => new { a, c.UserPrompt, c.ArtistNames, c.ModifierNames, c.PrimaryArtifactId });

        var results = Db.LoadSelect(q);
        results.PrintDump();
    }
}

