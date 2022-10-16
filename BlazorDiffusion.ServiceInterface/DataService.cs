using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorDiffusion.ServiceInterface;

public class DataService : Service
{
    public async Task<object> Any(SearchData request)
    {
        var to = new SearchDataResponse
        {
            CategoryGroups = new Group[] {
                new() { Name = "Scene",     Items = new[] { "Quality", "Style", "Aesthetic", "Medium", "Setting", "Theme" } },
                new() { Name = "Effects",   Items = new[] { "Effects", "CGI", "Filters", "Lenses", "Photography", "Lighting", "Color" } },
                new() { Name = "Art Style", Items = new[] { "Art Movement", "Art Style", "18 Century", "19 Century", "20 Century", "21 Century" } },
                new() { Name = "Mood",      Items = new[] { "Positive Mood", "Negative Mood" } },
            },
            Artists = (await Db.SelectAsync<Artist>()).OrderBy(x => x.Rank)
                .Select(x => new KeyValuePair<string, string>($"{x.Id}", x.FirstName != null ? $"{x.FirstName} {x.LastName}" : x.LastName)).ToArray(),
            Modifiers = (await Db.SelectAsync<Modifier>()).OrderBy(x => x.Rank)
                .Select(x => new ModifierInfo { Id = x.Id, Name = x.Name, Category = x.Category }).ToArray(),
        };
        return to;
    }
}
