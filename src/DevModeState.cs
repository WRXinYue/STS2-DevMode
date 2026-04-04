using System;
using System.Collections.Generic;

namespace DevMode;

public enum CardTarget
{
    DrawPile,
    Hand,
    DiscardPile,
    Deck
}

public enum EffectDuration
{
    Temporary,
    Permanent
}

public enum ActivePanel
{
    None,
    Cards,
    Relics
}

public enum CardMode
{
    View,
    Add,
    Upgrade,
    Delete
}

public enum RelicMode
{
    View,
    Add,
    Delete
}

public static class DevModeState
{
    /// <summary>Whether the user clicked "Developer Mode" on the main menu.</summary>
    public static bool IsActive { get; set; }

    /// <summary>True while inside a dev-mode run (set at launch, cleared on run end).</summary>
    public static bool InDevRun { get; set; }

    /// <summary>True while previewing card library / relic collection from the main menu.</summary>
    public static bool InMenuPreview { get; set; }

    /// <summary>Called when a menu preview submenu closes, to pop the compendium wrapper.</summary>
    public static Action? OnMenuPreviewClosed { get; set; }

    public static int StartingGold { get; set; } = 9999;

    public static int MaxEnergy { get; set; } = 0;

    public static CardTarget CardTarget { get; set; } = CardTarget.Deck;
    public static EffectDuration EffectDuration { get; set; } = EffectDuration.Permanent;
    public static ActivePanel ActivePanel { get; set; } = ActivePanel.None;
    public static CardMode CardMode { get; set; } = CardMode.View;
    public static RelicMode RelicMode { get; set; } = RelicMode.View;

    /// <summary>Current game speed multiplier (1.0 = normal).</summary>
    public static float GameSpeed { get; set; } = 1.0f;

    public static void OnRunStarted()
    {
        InDevRun = IsActive;
        IsActive = false;
    }

    public static void OnRunEnded()
    {
        InDevRun = false;
        GameSpeed = 1.0f;
    }
}
