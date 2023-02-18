using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BlazorDiffusion.UI;
using BlazorDiffusion.ServiceModel;
using System.Linq;

namespace BlazorDiffusion.Pages.ssg;

public class ImageModel : PageModel, IGetPageModel
{
    [FromQuery] public new int Id { get; set; } = 0;
    [FromQuery] public new string? Slug { get; set; }

    public string? UseSlug = null;
    public string? Title = null;
    public Creative? Creative = null;
    public Artifact? Artifact = null;
    public AlbumResult[] CreativeAlbums = Array.Empty<AlbumResult>();
    public List<Artifact> CreativeAlbumArtifacts = new();

    public IEnumerable<AlbumResult> GetArtifactAlbums() => CreativeAlbums.Where(x => x.ArtifactIds.Contains(Id));
    public Artifact? GetAlbumCoverArtifact(AlbumResult album) => CreativeAlbumArtifacts.FirstOrDefault(x => x.Id == album.GetAlbumCoverArtifactId());

    public async Task OnGetAsync()
    {
        var Gateway = HostContext.AppHost.GetServiceGateway();
        Title = "Image Art View";

        var request = new GetCreative { ArtifactId = Id };
        var api = await Gateway.ApiAsync(request);
        if (api.Succeeded)
        {
            Creative = api.Response!.Result;
            Artifact = Creative.Artifacts?.FirstOrDefault(x => x.Id == Id)
                ?? Creative.Artifacts?.FirstOrDefault();
            UseSlug = Slug ??= Creative.UserPrompt.GenerateSlug();

            Title = Creative.UserPrompt;

            var apiAlbums = await Gateway.ApiAsync(new GetCreativesInAlbums { CreativeId = Creative.Id });
            if (apiAlbums.Succeeded)
            {
                CreativeAlbums = (apiAlbums.Response!.Results ?? new()).ToArray();

                var artifactIds = CreativeAlbums.Select(x => x.GetAlbumCoverArtifactId()).ToList();
                var apiAlbumArtifacts = await Gateway.ApiAsync(new QueryArtifacts { Ids = artifactIds });
                if (apiAlbumArtifacts.Succeeded)
                {
                    CreativeAlbumArtifacts = apiAlbumArtifacts.Response?.Results ?? new();
                }
            }
        }
    }


}
