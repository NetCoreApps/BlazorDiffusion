using ServiceStack;
using System;
using System.Collections.Generic;

namespace BlazorDiffusion;

public class HtmlTemplate
{
    public string Contents { get; set; }
    public string PreTitle { get; set; }
    public string PreHead { get; set; }
    public string PreBody { get; set; }
    public string PostBody { get; set; }

    const string TitleMarker = "<!--title-->";
    const string HeadMarker = "<!--head-->";
    const string BodyMarker = "<!--body-->";

    public Dictionary<string, Type> ComponentTypes { get; set; } = new();

    public void RegisterComponent<T>() => ComponentTypes[typeof(T).FullName] = typeof(T);
    public Type? GetComponentType(string typeName) => ComponentTypes.TryGetValue(typeName, out var c) ? c : null;

    public static HtmlTemplate Create(string contents)
    {
        string? preTitle = null;
        string? preHead = null;
        string? preBody = null;
        string? postBody = null;

        var remaining = contents;
        preTitle = remaining.LeftPart(TitleMarker);
        remaining = remaining.RightPart(TitleMarker);
        preHead = remaining.LeftPart(HeadMarker);
        remaining = remaining.RightPart(HeadMarker);
        preBody = remaining.LeftPart(BodyMarker);
        postBody = remaining.RightPart(BodyMarker);

        return new HtmlTemplate
        {
            Contents = contents,
            PreTitle = preTitle,
            PreHead = preHead,
            PreBody = preBody,
            PostBody = postBody,
        };
    }

    public static string CreateMeta(string url = "", string title = "", string description = "", string image = "")
    {
        var useUrl = url.IndexOf("://") >= 0
            ? url
            : "https://blazordiffusion.com".CombineWith(url);

        return $@"<meta name=""twitter:card"" content=""summary"" />
    <meta name=""twitter:site"" content=""blazordiffusion.com"" />
    <meta name=""twitter:creator"" content=""@blazordiffusion"" />
    <meta property=""og:url"" content=""{useUrl}"" />
    <meta property=""og:title"" content=""{title}"" />
    <meta property=""og:description"" content=""{description}"" />
    <meta property=""og:image"" content=""{image}"" />";
    }

    public string Render(string title = "", string head = "", string body = "")
    {
        return PreTitle
            + title 
            + PreHead
            + head 
            + PreBody
            + body
            + PostBody;
    }
}
