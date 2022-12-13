# Blazor Diffusion 

[![](https://servicestack.net/images/whatsnew/v6.5/blazordiffusion.com_splash.png)](https://server.blazordiffusion.com)

> Browse [source code](https://github.com/NetCoreTemplates/blazor-server) or view live demo [server.blazordiffusion.com](https://server.blazordiffusion.com)

[blazordiffusion.com](https://blazordiffusion.com) is a new [ServiceStack.Blazor App](https://servicestack.net/blazor) front-end for [Stable Diffusion](https://stability.ai/blog/stable-diffusion-public-release) - a deep learning text-to-image model that can generate quality images from a text prompt. 

### Effortless Admin Pages

It's a great example of Hybrid Development in action where the entire user-facing UI is a bespoke Blazor App that's optimized for creating, searching, cataloging and discovering Stable Diffusion generated images, whilst all its supporting admin tasks to manage the back office tables that power the UI were effortlessly implemented with [custom AutoQueryGrid components](https://blazor-gallery.jamstacks.net/grid).

To get a glimpse of this in action we've created a video showing how quick it was to build the first few Admin Pages:

[![](https://i3.ytimg.com/vi/tt0ytzVVjEY/maxresdefault.jpg)](https://www.youtube.com/watch?v=tt0ytzVVjEY)

## Live Demo

The [/admin](https://github.com/NetCoreApps/BlazorDiffusion/tree/main/BlazorDiffusion/Pages/admin) pages we're all built using [AutoQueryGrid](https://blazor-gallery.jamstacks.net/grid) for its data management and uses [NavList and Breadcrumbs](https://blazor-gallery.jamstacks.net/gallery/navigation) for its navigation.

To try out the Admin pages on the Live Demo Sign in with user `admin@email.com` and password `p@55wOrd`:

<div class="flex justify-center">
    <a href="https://server.blazordiffusion.com/admin">
        <img src="https://github.com/ServiceStack/docs/raw/master/docs/images/blazor/blazordiffusion-admin-pages.png" style="width:600px">
    </a>
</div>

For a closer look, clone this repo to run a local modifiable copy, after unzipping go to [/BlazorDiffusion](https://github.com/NetCoreApps/BlazorDiffusion/tree/main/BlazorDiffusion) and run:

```bash
$ npm run migrate
$ dotnet run
```

Generating Stable Diffusion requires a [Dream AI API Key](https://beta.dreamstudio.ai/membership?tab=apiKeys) populated in the `DREAMAI_APIKEY` or configured in 
the `DreamStudioClient` in [Configure.AppHost.cs](https://github.com/NetCoreApps/BlazorDiffusion/blob/main/BlazorDiffusion/Configure.AppHost.cs).

The Admin Pages makes extensive usage of [ServiceStack.Blazor Components](https://blazor-gallery.jamstacks.net):

#### EditForm

The following components make use of `<EditForm>` AutoQueryGrid extensibility to display unique forms for their custom workflow requirements:

 - [Creatives.razor](https://github.com/NetCoreApps/BlazorDiffusion/blob/main/BlazorDiffusion/Pages/admin/Creatives.razor)
 - [ArtifactAutoQueryGrid.razor](https://github.com/NetCoreApps/BlazorDiffusion/blob/main/BlazorDiffusion/Shared/admin/ArtifactAutoQueryGrid.razor)
 - [ArtifactReportsAutoQueryGrid.razor](https://github.com/NetCoreApps/BlazorDiffusion/blob/main/BlazorDiffusion/Shared/admin/ArtifactReportsAutoQueryGrid.razor)

```csharp
<AutoQueryGrid @ref=@grid Model="Creative" ConfigureQuery="ConfigureQuery"
               Apis="Apis.AutoQuery<QueryCreatives,UpdateCreative,HardDeleteCreative>()">
    <EditForm>
        <div class="relative z-10" aria-labelledby="slide-over-title" role="dialog" aria-modal="true">
            <div class="pointer-events-none fixed inset-y-0 right-0 flex max-w-full pl-10 sm:pl-16">
                <CreativeEdit Creative="context" OnClose="grid!.OnEditDone" />
            </div>
        </div>
    </EditForm>
</AutoQueryGrid>
```

### SelectInput

The [Modifiers.razor](https://github.com/NetCoreApps/BlazorDiffusion/blob/main/BlazorDiffusion/Pages/admin/Modifiers.razor) admin page uses 
[SelectInput EvalAllowableValues](https://github.com/NetCoreApps/BlazorDiffusion/blob/v0.1/BlazorDiffusion.ServiceModel/Creative.cs#L168-L187) feature to populate its options from a C# [AppData](https://github.com/NetCoreApps/BlazorDiffusion/blob/v0.1/BlazorDiffusion.ServiceModel/AppData.cs) property:

```csharp
public class CreateModifier : ICreateDb<Modifier>, IReturn<Modifier>
{
    [ValidateNotEmpty, Required]
    public string Name { get; set; }
    [ValidateNotEmpty, Required]
    [Input(Type="select", EvalAllowableValues = "AppData.Categories")]
    public string Category { get; set; }
    public string? Description { get; set; }
}
```

<div class="mt-8 flex justify-center">
    <img src="https://github.com/ServiceStack/docs/raw/master/docs/images/blazor/diffusion-CreateModifier.png" class="max-w-screen-md" style="border:1px solid #CACACA">
</div>

### TagInput

The [Artists.razor](https://github.com/NetCoreApps/BlazorDiffusion/blob/main/BlazorDiffusion/Pages/admin/Artists.razor) admin page uses [declarative TagInput](https://github.com/NetCoreApps/BlazorDiffusion/blob/v0.1/BlazorDiffusion.ServiceModel/Creative.cs#L122-L141) to render its AutoQueryGrid Create and Edit Forms:

```csharp
public class UpdateArtist : IPatchDb<Artist>, IReturn<Artist>
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? YearDied { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Type { get; set; }
}
```

<div class="my-8 flex justify-center">
    <img src="https://github.com/ServiceStack/docs/raw/master/docs/images/blazor/blazordiffusion-TagInput.png" class="max-w-screen-md" style="border:1px solid #CACACA">
</div>

<h2 id="litestream" class="mx-auto max-w-screen-md text-center py-8 border-none">
    <a href="https://litestream.io">
        <img src="https://github.com/ServiceStack/docs/raw/master/docs/images/litestream/logo.svg">
    </a>
</h2>

Blazor Diffusion leverages our [support for Litestream](https://docs.servicestack.net/ormlite/litestream) as an example of architecting a production App at minimal cost which avoids paying for expensive managed hosted RDBMS's by effortlessly replicating its SQLite databases to object storage.

<div class="mt-16 mx-auto max-w-7xl px-4">
    <div class="text-center">
        <h3 class="text-4xl tracking-tight font-extrabold text-gray-900 sm:text-5xl md:text-6xl">
            <span class="block xl:inline">Reduce Complexity &amp; Save Costs</span>
        </h3>
        <p class="mt-3 max-w-md mx-auto text-base text-gray-500 sm:text-lg md:mt-5 md:text-xl md:max-w-3xl">
            Avoid expensive managed RDBMS servers, reduce deployment complexity, eliminate 
            infrastructure dependencies & save order of magnitude costs vs production hosting
        </p>
    </div>
    <img src="https://github.com/ServiceStack/docs/raw/master/docs/images/litestream/litestream-costs.svg">
</div>

To make it easy for Blazor Tailwind projects to take advantage of our first-class [Litestream support](https://docs.servicestack.net/ormlite/litestream), we've created a new video combining these ultimate developer experience & value combo solutions that walks through how to deploy a new Blazor Tailwind SQLite + Litestream App to any Linux server with SSH access, Docker and Docker Compose:

[![](https://i3.ytimg.com/vi/fY50dWszpw4/maxresdefault.jpg)](https://www.youtube.com/watch?v=fY50dWszpw4)

### Useful Blazor Litestream Video Links

- [Blazor Litestream Tutorial](https://docs.servicestack.net/blazor-litestream)
- [Blazor](https://servicestack.net/blazor)
- [Litestream](https://servicestack.net/litestream)
- [Docker Install](https://docs.docker.com/engine/install/ubuntu/)
- [Docker Compose Install](https://docs.docker.com/compose/install/linux/#install-using-the-repository)

