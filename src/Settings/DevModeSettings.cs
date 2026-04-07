namespace DevMode.Settings;

/// <summary>
/// Persistent user preferences for DevMode appearance. Serialized to settings.json.
/// </summary>
public sealed class DevModeSettings
{
    public bool   DarkMode      { get; set; } = true;
    public string DarkThemeName { get; set; } = ThemeNames.Dark;
    public string LightThemeName{ get; set; } = ThemeNames.Light;
}

public static class ThemeNames
{
    public const string Dark  = "Dark";
    public const string Oled  = "OLED";
    public const string Light = "Light";
    public const string Warm  = "Warm";
}
