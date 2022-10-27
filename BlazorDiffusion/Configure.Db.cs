using BlazorDiffusion.ServiceInterface;
using CoenM.ImageHash;
using Microsoft.Data.Sqlite;
using ServiceStack.Data;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(BlazorDiffusion.ConfigureDb))]

namespace BlazorDiffusion;

// Database can be created with "dotnet run --AppTasks=migrate"   
public class ConfigureDb : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context,services) => services.AddSingleton<IDbConnectionFactory>(new OrmLiteConnectionFactory(
            context.Configuration.GetConnectionString("DefaultConnection") ?? "App_Data/db.sqlite",
            SqliteDialect.Provider)))
        .ConfigureAppHost(appHost =>
        {
            using var db = appHost.Resolve<IDbConnectionFactory>().OpenDbConnection();
            db.RegisterImgCompare();
            Scores.Load(db);
        });
}
