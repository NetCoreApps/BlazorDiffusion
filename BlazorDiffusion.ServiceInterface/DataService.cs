using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;
using System.Threading.Tasks;

namespace BlazorDiffusion.ServiceInterface;

public class DataService : Service
{
    public async Task<object> Any(SearchData request)
    {
        return new SearchDataResponse
        {
            CategoryGroups = new() {
                new() { Group = "Scene",     Categories = new[] { "Quality", "Style", "Aesthetic", "Medium", "Setting", "Theme" } },
                new() { Group = "Effects",   Categories = new[] { "Effects", "CGI", "Filters", "Lenses", "Photography", "Lighting", "Color" } },
                new() { Group = "Art Style", Categories = new[] { "Art Movement", "Art Style", "18 Century", "19 Century", "20 Century", "21 Century" } },
                new() { Group = "Mood",      Categories = new[] { "Positive Mood", "Negative Mood" } },
            },
            Artists = await Db.SelectAsync<Artist>(),
            Modifiers = await Db.SelectAsync<Modifier>(),
        };
    }
}
