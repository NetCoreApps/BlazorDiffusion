using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;
using CoenM.ImageHash;
using Microsoft.Data.Sqlite;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Converters;

[assembly: HostingStartup(typeof(BlazorDiffusion.ConfigureDb))]

namespace BlazorDiffusion;

// Database can be created with "dotnet run --AppTasks=migrate"   
public class ConfigureDb : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context,services) =>
        {
            var dbFactory = new OrmLiteConnectionFactory(
                context.Configuration.GetConnectionString("DefaultConnection") ?? "App_Data/db.sqlite",
                SqliteDialect.Provider);
            dbFactory.RegisterConnection(Databases.Analytics, 
                context.Configuration.GetConnectionString("AnalyticsConnection") ?? "App_Data/analytics.sqlite",
                SqliteDialect.Provider);
            services.AddSingleton<IDbConnectionFactory>(dbFactory);
            ((DateTimeConverter)SqliteDialect.Provider.GetConverter<DateTime>()).DateStyle = DateTimeKind.Utc;
        })
        .ConfigureAppHost(appHost =>
        {
            using var db = appHost.Resolve<IDbConnectionFactory>().OpenDbConnection();
            db.RegisterImgCompare();
        });
}
