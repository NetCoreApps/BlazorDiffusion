using ServiceStack;
using System.Collections.Generic;

namespace BlazorDiffusion.ServiceModel;

public class SearchData : IReturn<SearchDataResponse>
{
}

public class SearchDataResponse
{
    public Group[] CategoryGroups { get; set; }
    public KeyValuePair<string, string>[] Artists { get; set; }
    public ModifierInfo[] Modifiers { get; set; }
}

public class Group
{
    public string Name { get; set; }
    public string[] Items { get; set; }
}

public class ModifierInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
}
