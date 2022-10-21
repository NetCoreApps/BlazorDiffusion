namespace BlazorDiffusion.UI;

/// <summary>
/// Ensure only the Active target component receives Nav Keys
/// </summary>
public class KeyboardNavigation
{
    public Func<string, Task>? Active { get; set; }

    public async Task SendKeyAsync(string key)
    {
        if (Active != null)
            await Active.Invoke(key);
    }

    public void Register(Func<string, Task> target) => Active = target;
    public void Deregister(Func<string, Task> target)
    {
        if (Active == target)
            Active = null;
    }
}
