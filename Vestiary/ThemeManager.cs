namespace Vestiary;

/// <summary>
/// Singleton accessor for the active theme. Windows use ThemeManager.Current.X
/// without knowing which theme is loaded.
/// </summary>
public static class ThemeManager
{
    public static ITheme Current { get; private set; } = new OceanTheme();

    public static void SetTheme(string themeName)
    {
        Current = themeName switch
        {
            "Midnight Purple" => new MidnightPurpleTheme(),
            "Champagne" => new ChampagneTheme(),
            "Rose" => new RoseTheme(),
            _ => new OceanTheme(),
        };
    }
}
