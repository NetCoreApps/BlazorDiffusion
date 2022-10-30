namespace BlazorDiffusion.ServiceModel;

public static class Databases
{
    // Keep heavy writes of stats + analytics in separate DB
    public const string Analytics = nameof(Analytics);
}

