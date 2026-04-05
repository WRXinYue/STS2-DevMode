using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

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
    Relics,
    Enemies
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

public enum EnemyMode
{
    /// <summary>Set a global override — all combat rooms use this encounter.</summary>
    Global,
    /// <summary>Set per-room-type overrides (Monster / Elite / Boss separately).</summary>
    PerType,
    /// <summary>Clear all overrides.</summary>
    Off
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

    // ── Enemy override state ──

    public static EnemyMode EnemyMode { get; set; } = EnemyMode.Off;

    /// <summary>Global encounter override (used when <see cref="EnemyMode"/> == Global).</summary>
    public static EncounterModel? GlobalEncounterOverride { get; set; }

    /// <summary>Per-room-type encounter overrides (used when <see cref="EnemyMode"/> == PerType).</summary>
    public static Dictionary<RoomType, EncounterModel?> RoomTypeOverrides { get; } = new()
    {
        [RoomType.Monster] = null,
        [RoomType.Elite]   = null,
        [RoomType.Boss]    = null,
    };

    /// <summary>Per-floor encounter overrides. Key = floor index (0-based). Takes highest priority.</summary>
    public static Dictionary<int, EncounterModel?> FloorOverrides { get; } = new();

    /// <summary>Try to resolve an encounter override for the given room type and floor.</summary>
    public static EncounterModel? ResolveOverride(RoomType roomType, int floor)
    {
        // Floor-specific override has highest priority
        if (FloorOverrides.TryGetValue(floor, out var floorEnc) && floorEnc != null)
            return floorEnc;

        return EnemyMode switch
        {
            EnemyMode.Global  => GlobalEncounterOverride,
            EnemyMode.PerType => RoomTypeOverrides.TryGetValue(roomType, out var enc) ? enc : null,
            _                 => null
        };
    }

    public static void ClearEnemyOverrides()
    {
        EnemyMode = EnemyMode.Off;
        GlobalEncounterOverride = null;
        RoomTypeOverrides[RoomType.Monster] = null;
        RoomTypeOverrides[RoomType.Elite]   = null;
        RoomTypeOverrides[RoomType.Boss]    = null;
        FloorOverrides.Clear();
    }

    public static void OnRunStarted()
    {
        InDevRun = IsActive;
        IsActive = false;
    }

    public static void OnRunEnded()
    {
        InDevRun = false;
        GameSpeed = 1.0f;
        ClearEnemyOverrides();
    }
}
