using Godot;

namespace DevMode.UI;

/// <summary>
/// Shared color and sizing constants for the DevMode UI (rail, browser, overlays).
/// All color values delegate to <see cref="ThemeManager.Current"/> so they update
/// automatically when the user switches themes.
/// </summary>
internal static class DevModeTheme
{
    // ── Panel / overlay backgrounds ──
    public static Color PanelBg     => ThemeManager.Current.PanelBg;
    public static Color PanelBorder => ThemeManager.Current.PanelBorder;

    // ── Text / label tones ──
    public static Color Subtle    => ThemeManager.Current.Subtle;
    public static Color Separator => ThemeManager.Current.Separator;

    // ── Accent (shared active / highlight) ──
    public static Color Accent      => ThemeManager.Current.Accent;
    public static Color AccentAlpha => ThemeManager.Current.AccentAlpha;

    // ── Widget surface / text ──
    public static Color TextPrimary    => ThemeManager.Current.TextPrimary;
    public static Color TextSecondary  => ThemeManager.Current.TextSecondary;
    public static Color ButtonBgNormal => ThemeManager.Current.ButtonBgNormal;
    public static Color ButtonBgHover  => ThemeManager.Current.ButtonBgHover;

    // ── Rarity colors — fixed across all themes ──
    public static readonly Color RarityCommon   = new(0.55f, 0.55f, 0.58f);
    public static readonly Color RarityUncommon = new(0.35f, 0.55f, 0.85f);
    public static readonly Color RarityRare     = new(0.85f, 0.72f, 0.25f);
    public static readonly Color RaritySpecial  = new(0.70f, 0.45f, 0.85f);
    public static readonly Color RarityCurse    = new(0.75f, 0.30f, 0.30f);

    // ── Game BBCode → standard Godot BBCode conversion ──

    private static readonly (string tag, string hex)[] ColorTags =
    {
        ("gold",   "#EFC851"),
        ("blue",   "#87CEEB"),
        ("red",    "#FF5555"),
        ("green",  "#7FFF00"),
        ("purple", "#EE82EE"),
        ("orange", "#FFA518"),
        ("aqua",   "#2AEBBE"),
        ("pink",   "#FF78A0"),
    };

    private static readonly string[] EffectOnlyTags =
        { "sine", "jitter", "fade_in", "fly_in", "thinky_dots", "ancient_banner" };

    /// <summary>
    /// Converts the game's custom BBCode tags ([gold], [blue], [red], etc.)
    /// into standard Godot [color=...] tags that RichTextLabel understands natively.
    /// Animation-only tags (sine, jitter, etc.) are stripped.
    /// </summary>
    public static string ConvertGameBbcode(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        foreach (var (tag, hex) in ColorTags)
        {
            text = text.Replace($"[{tag}]", $"[color={hex}]");
            text = text.Replace($"[/{tag}]", "[/color]");
        }

        foreach (var tag in EffectOnlyTags)
        {
            text = text.Replace($"[{tag}]", "");
            text = text.Replace($"[/{tag}]", "");
        }

        return text;
    }

    /// <summary>
    /// Creates a RichTextLabel with BBCode enabled, ready for text
    /// processed through <see cref="ConvertGameBbcode"/>.
    /// </summary>
    public static RichTextLabel CreateGameBbcodeLabel()
    {
        return new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
    }
}
