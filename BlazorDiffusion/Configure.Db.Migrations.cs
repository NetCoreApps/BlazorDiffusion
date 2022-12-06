using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using BlazorDiffusion.Migrations;
using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;

[assembly: HostingStartup(typeof(BlazorDiffusion.ConfigureDbMigrations))]

namespace BlazorDiffusion;

public class ConfigureDbMigrations : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureAppHost(afterAppHostInit:appHost => {
            var migrator = new Migrator(appHost.Resolve<IDbConnectionFactory>(), typeof(Migration1000).Assembly);
            AppTasks.Register("migrate", _ => migrator.Run());
            AppTasks.Register("migrate.revert", args => migrator.Revert(args[0]));

            AppTasks.Register("migrate.localprep", args => migrator.FetchDemoAssets());
            AppTasks.Run();

            using var db = migrator.DbFactory.OpenDbConnection();
            Scores.Load(db);
            using var dbAnalytics = migrator.DbFactory.OpenDbConnection(Databases.Analytics);
            Scores.LoadAnalytics(dbAnalytics);
        });
}
