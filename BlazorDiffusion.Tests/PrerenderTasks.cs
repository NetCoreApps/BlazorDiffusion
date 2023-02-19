using System;
using System.IO;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using BlazorDiffusion.ServiceModel;

namespace BlazorDiffusion.Tests;

[TestFixture, Category("prerender"), Explicit]
public class PrerenderTasks
{
    Bunit.TestContext Context;
    string ClientDir;
    string WwrootDir => ClientDir.CombineWith("wwwroot");
    string PrerenderDir => WwrootDir.CombineWith("prerender");

    public PrerenderTasks()
    {
        Context = new();
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        ClientDir = config[nameof(ClientDir)]
            ?? throw new Exception($"{nameof(ClientDir)} not defined in appsettings.json");
        // FileSystemVirtualFiles.RecreateDirectory(PrerenderDir);
    }

}