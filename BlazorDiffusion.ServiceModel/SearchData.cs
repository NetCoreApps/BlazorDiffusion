using ServiceStack;
using System;
using System.Collections.Generic;

namespace BlazorDiffusion.ServiceModel;

public class SearchData : IReturn<SearchDataResponse>
{
}

public class SearchDataResponse
{
    public List<Group> CategoryGroups { get; set; }
    public List<ArtistInfo> Artists { get; set; }
    public List<ModifierInfo> Modifiers { get; set; }
}

public class Group
{
    public string Name { get; set; }
    public string[] Items { get; set; }
}

public class ArtistInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }

    public override bool Equals(object? obj) => obj is ArtistInfo info && Id == info.Id && Name == info.Name;
    public override int GetHashCode() => HashCode.Combine(Id, Name);
}

public class ModifierInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }

    public override bool Equals(object? obj) => obj is ModifierInfo info &&
        Id == info.Id && Name == info.Name && Category == info.Category;
    public override int GetHashCode() => HashCode.Combine(Id, Name, Category);
}
