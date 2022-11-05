using NUnit.Framework;
using ServiceStack;
using ServiceStack.Testing;
using BlazorDiffusion.ServiceInterface;
using BlazorDiffusion.ServiceModel;

namespace BlazorDiffusion.Tests;

public class UnitTest
{
    private readonly ServiceStackHost appHost;

    public UnitTest()
    {
        appHost = new BasicAppHost().Init();
        appHost.Container.AddTransient<MyServices>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    [Test]
    public void Can_call_MyServices()
    {
        var service = appHost.Container.Resolve<MyServices>();
    }
}
