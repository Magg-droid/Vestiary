namespace Wardrobe;

/// <summary>
/// Singleton accessor for the active theme. Windows use ThemeManager.Current.X
/// without knowing which theme is loaded.
/// </summary>
public static class ThemeManager
{
    public static ITheme Current { get; private set; } = new RoseGoldTheme();

    public static void SetTheme(string themeName)
    {
        Current = themeName switch
        {
            "Ocean" => new OceanTheme(),
            "Midnight Purple" => new MidnightPurpleTheme(),
            "Forest" => new ForestTheme(),
            _ => new RoseGoldTheme(),
        };
    }
}
