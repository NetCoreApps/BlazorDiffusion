using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;
using BlazorDiffusion.ServiceInterface;
using Microsoft.AspNetCore.Components;

[assembly: HostingStartup(typeof(BlazorDiffusion.ConfigureUi))]

namespace BlazorDiffusion;

public class ConfigureUi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddSingleton<IComponentRenderer>(c => new ComponentRenderer(
                typeof(Pages.Index).Assembly.GetTypes().Where(x => typeof(ComponentBase).IsAssignableFrom(x))));
        });
}

public class ComponentRenderer : IComponentRenderer
{
    public List<Type> Types { get; }

    public ComponentRenderer(IEnumerable<Type> types)
    {
        Types = types.ToList();
    }

    public Task<string> RenderComponentAsync(string typeName, HttpContext httpContext, Dictionary<object, object>? args = null)
    {
        var type = typeName.IndexOf('.') < 0 
            ? Types.FirstOrDefault(x => x.Name == typeName)
            : Types.FirstOrDefault(x => x.FullName == typeName);
        if (type == null)
            throw HttpError.NotFound("Component Not Found");
        
        return RenderComponentAsync(type, httpContext, args);
    }

    public Task<string> RenderComponentAsync<T>(HttpContext httpContext, Dictionary<object, object>? args = null) =>
        RenderComponentAsync(typeof(T), httpContext, args);

    public async Task<string> RenderComponentAsync(Type type, HttpContext httpContext, Dictionary<object, object>? args = null)
    {
        var componentTagHelper = new ComponentTagHelper
        {
            ComponentType = type,
            RenderMode = RenderMode.Static,
            Parameters = new Dictionary<string, object>(), //TODO: Overload and pass in parameters
            ViewContext = new ViewContext { HttpContext = httpContext },
        };

        var tagHelperContext = new TagHelperContext(
            new TagHelperAttributeList(),
            args ?? new Dictionary<object, object>(),
            "uniqueid");

        var tagHelperOutput = new TagHelperOutput(
            "tagName",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        await componentTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

        using var stringWriter = new StringWriter();

        tagHelperOutput.Content.WriteTo(stringWriter, HtmlEncoder.Default);

        return stringWriter.ToString();
    }
}
