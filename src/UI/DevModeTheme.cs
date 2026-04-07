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
}
