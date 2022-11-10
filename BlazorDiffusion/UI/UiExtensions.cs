using System.Collections.Specialized;

namespace BlazorDiffusion.UI;

public static class UiExtensions
{
    public static int? GetInt(this NameValueCollection query, string name) => 
        X.Map(query[name], x => int.TryParse(x, out var num) ? num : (int?)null);
}
