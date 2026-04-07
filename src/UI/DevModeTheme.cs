using Godot;

namespace DevMode.UI;

/// <summary>
/// Shared color and sizing constants for the DevMode UI (rail, browser, overlays).
/// Centralises values previously duplicated across DevPanelUI and CardBrowserUI.
/// </summary>
internal static class DevModeTheme
{
    // ── Panel / overlay backgrounds ──
    public static readonly Color PanelBg     = new(0.11f, 0.11f, 0.14f, 0.96f);
    public static readonly Color PanelBorder = new(1f, 1f, 1f, 0.08f);

    // ── Text / label tones ──
    public static readonly Color Subtle   = new(0.50f, 0.50f, 0.58f);
    public static readonly Color Separator = new(1f, 1f, 1f, 0.06f);

    // ── Accent (shared active / highlight blue) ──
    public static readonly Color Accent      = new(0.40f, 0.68f, 1f);
    public static readonly Color AccentAlpha = new(0.40f, 0.68f, 1f, 0.85f);

    // ── Rarity colors ──
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
