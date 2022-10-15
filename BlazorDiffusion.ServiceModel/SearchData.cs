using ServiceStack;
using System.Collections.Generic;

namespace BlazorDiffusion.ServiceModel;

public class SearchData : IReturn<SearchDataResponse>
{
}

public class SearchDataResponse
{
    public List<CategoryGroup> CategoryGroups { get; set; }
    public List<Artist> Artists { get; set; }
    public List<Modifier> Modifiers { get; set; }
}

public class CategoryGroup
{
    public string Group { get; set; }
    public string[] Categories { get; set; }
}
