namespace BlazorDiffusion.UI;

public static class AppCss
{
    // tailwind needs to see full classes
    static Dictionary<string, string> GridClasses = new()
    {
        ["1"] = "grid-cols-1",
        ["2"] = "grid-cols-2",
        ["3"] = "grid-cols-3",
        ["4"] = "grid-cols-4",
        ["5"] = "grid-cols-5",
        ["6"] = "grid-cols-6",
        ["7"] = "grid-cols-7",
        ["8"] = "grid-cols-8",
        ["9"] = "grid-cols-9",
        ["10"] = "grid-cols-10",
        ["11"] = "grid-cols-11",
        ["12"] = "grid-cols-12",
    };

    public static string GetGridClass(int columns) => GetGridClass(columns.ToString());

    public static string GetGridClass(string columns)
    {
        return GridClasses.TryGetValue(columns, out var cls) 
            ? cls 
            : "grid-cols-6";
    }
}
