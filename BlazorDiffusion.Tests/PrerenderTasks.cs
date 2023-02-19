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
    IDbConnectionFactory ResolveDbFactory() => new ConfigureDb().ConfigureAndResolve<IDbConnectionFactory>();

    public PrerenderTasks()
    {
        Context = new();
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        ClientDir = config[nameof(ClientDir)]
            ?? throw new Exception($"{nameof(ClientDir)} not defined in appsettings.json");
        // FileSystemVirtualFiles.RecreateDirectory(PrerenderDir);
    }

    Dictionary<string, string> ProdLibs = new()
    {
        ["https://unpkg.com/vue@3/dist/vue.esm-browser.js"] = "https://unpkg.com/vue@3/dist/vue.esm-browser.prod.js",
        ["https://unpkg.com/@servicestack/client/dist/servicestack-client.mjs"] = "https://unpkg.com/@servicestack/client/dist/servicestack-client.min.mjs",
    };

    [Test]
    public void Update_to_prod_refs()
    {
        var jsDir = WwrootDir.CombineWith("js");
        var files = new DirectoryInfo(jsDir).GetFiles("*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var js = file.ReadAllText();
            var prodJs = js;
            foreach (var entry in ProdLibs)
            {
                prodJs = prodJs.Replace(entry.Key, entry.Value);
            }
            if (js != prodJs)
            {
                $"Updating {file.Name}...".Print();
                File.WriteAllText(file.FullName, prodJs);
            }
        }
    }

    [Test]
    public void Generate_all_artifact_slugs()
    {
        using var db = ResolveDbFactory().OpenDbConnection();
        var artifactsCount = db.RowCount(db.From<Artifact>());
        var emptyPrompts = db.RowCount(db.From<Artifact>().Where(x => x.Prompt == null || x.Prompt == ""));

        var prompts = db.ColumnDistinct<string>(db.From<Artifact>().SelectDistinct(x => x.Prompt));
        $"Checking {prompts.Count} unique prompts, {emptyPrompts} empty from {artifactsCount} artifacts...".Print();

        foreach (var prompt in prompts)
        {
            try
            {
                var slug = Ssg.GenerateSlug(prompt);
            }
            catch (Exception e)
            {
                $"ERROR: {e.Message} for '{prompt}'".Print();
            }
        }

        var artifacts = db.Select(db.From<Artifact>());
        $"Checking {artifacts.Count} artifacts paths...".Print();
        foreach (var artifact in artifacts)
        {
            try
            {
                var path = Ssg.GetArtifact(artifact);
            }
            catch (Exception e)
            {
                $"ERROR: {e.Message} for #{artifact.Id} '{artifact.Prompt}'".Print();
            }
        }
    }

}