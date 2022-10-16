using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.OrmLite;
using BlazorDiffusion.ServiceModel;

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
            }.ToList(),

            Artists = (await Db.SelectAsync<Artist>()).OrderBy(x => x.Rank)
                .Select(x => new ArtistInfo { 
                    Id = x.Id, 
                    Name = x.FirstName != null ? $"{x.FirstName} {x.LastName}" : x.LastName
                }).ToList(),
            
            Modifiers = (await Db.SelectAsync<Modifier>()).OrderBy(x => x.Rank)
                .Select(x => new ModifierInfo { Id = x.Id, Name = x.Name, Category = x.Category }).ToList(),
        };
        return to;
    }
}
