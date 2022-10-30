using BlazorDiffusion.ServiceModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Messaging;

[assembly: HostingStartup(typeof(BlazorDiffusion.ConfigureMq))]

namespace BlazorDiffusion
{
    /**
      Register Services you want available via MQ in your AppHost, e.g:
        var mqServer = container.Resolve<IMessageService>();
        mqServer.RegisterHandler<MyRequest>(ExecuteMessage);
    */
    public class ConfigureMq : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder) => builder
            .ConfigureServices(services => {
                services.AddSingleton((Func<IServiceProvider, IMessageService>)(c => new BackgroundMqService()));
            })
            .ConfigureAppHost(afterAppHostInit: appHost => {
                var mqService = appHost.Resolve<IMessageService>();
                mqService.RegisterHandler<DiskTasks>(appHost.ExecuteMessage);
                mqService.RegisterHandler<BackgroundTasks>(appHost.ExecuteMessage);
                mqService.RegisterHandler<AnalyticsTasks>(appHost.ExecuteMessage);
                mqService.Start();
            });
    }
}
