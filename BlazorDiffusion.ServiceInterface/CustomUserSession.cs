
using ServiceStack;

namespace BlazorDiffusion.ServiceInterface;

// Add any additional metadata properties you want to store in the Users Typed Session
public class CustomUserSession : AuthUserSession
{
    public string RefIdStr { get; set; }
}
