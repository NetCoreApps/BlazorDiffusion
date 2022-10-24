using BlazorDiffusion.ServiceModel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorDiffusion.Tests;

[Explicit]
public class ArtifactTests
{
    [Test]
    public async Task Can_update_Quality()
    {
        var client = new JsonApiClient("https://localhost:5001");
        await client.PostAsync(new Authenticate
        {
            provider = "credentials",
            UserName = "admin@email.com",
            Password = "p@55wOrd",
        });

        var api = await client.ApiAsync(new UpdateArtifact
        {
            Id = 571,
            Quality = -1,
        });
        if (api.Succeeded)
        {
            api.Response.PrintDump();
        }
    }
}
