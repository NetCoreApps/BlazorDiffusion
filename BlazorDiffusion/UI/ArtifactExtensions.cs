using BlazorDiffusion.ServiceModel;
using System.Security.Claims;
using ServiceStack.Blazor;

namespace BlazorDiffusion.UI;

public static class ArtifactExtensions
{

    public static bool CanExploreSimilarTo(this Artifact artifact) => artifact.PerceptualHash != null;

    public static bool IsModerated(this Artifact artifact) => artifact.Nsfw == true || artifact.Quality < 0;

    public static string GetBorderColor(this Artifact artifact, int? activeId, UserState userState)
    {
        return artifact.Id == activeId
            ? "border-cyan-500"
            : userState.HasLiked(artifact)
                ? "border-red-700"
                : userState.IsModerator() && artifact.IsModerated()
                    ? "border-gray-500"
                    : userState.HasArtifactInAlbum(artifact) 
                      ? "border-green-700"
                      : artifact.Background != null ? "border-black" : "border-transparent";
    }
}
