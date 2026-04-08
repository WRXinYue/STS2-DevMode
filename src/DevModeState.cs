using System;
using System.Collections.Generic;
using DevMode.Presets;
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
    Enemies,
    Powers,
    Potions,
    Events,
    Rooms,
    Console,
    Presets,
    CardEdit,
    Hooks,
    Scripts
}

public enum PowerTarget
{
    Self,
    AllEnemies,
    SpecificTarget,
    Allies
}

public enum MapRewriteMode
{
    None,
    AllChest,
    AllElite,
    AllBoss
}

public enum CardMode
{
    View,
    Add,
    Upgrade,
    Edit,
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

    /// <summary>
    /// When true, normal (non-dev) runs also get the DevPanel sidebar and cheat patches.
    /// Toggled from the Developer Mode menu on the main menu.
    /// </summary>
    public static bool AlwaysEnabled { get; set; }

    /// <summary>True while previewing card library / relic collection from the main menu.</summary>
    public static bool InMenuPreview { get; set; }

    /// <summary>Called when a menu preview submenu closes, to pop the compendium wrapper.</summary>
    public static Action? OnMenuPreviewClosed { get; set; }

    public static int MaxEnergy { get; set; } = 0;

    public static CardTarget CardTarget { get; set; } = CardTarget.Deck;
    public static EffectDuration EffectDuration { get; set; } = EffectDuration.Permanent;
    public static ActivePanel ActivePanel { get; set; } = ActivePanel.None;
    public static CardMode CardMode { get; set; } = CardMode.View;
    public static RelicMode RelicMode { get; set; } = RelicMode.View;

    /// <summary>Current game speed multiplier (1.0 = normal).</summary>
    public static float GameSpeed { get; set; } = 1.0f;

    // ── Player cheats ──

    public static bool InfiniteHp { get; set; }
    public static bool InfiniteBlock { get; set; }
    public static bool InfiniteEnergy { get; set; }
    public static bool InfiniteStars { get; set; }
    public static bool AlwaysRewardPotion { get; set; }
    public static bool AlwaysUpgradeCardReward { get; set; }
    public static bool MaxCardRewardRarity { get; set; }
    public static float DefenseMultiplier { get; set; } = 1.0f;

    // ── Inventory cheats ──

    public static float GoldMultiplier { get; set; } = 1.0f;
    public static bool FreeShop { get; set; }

    // ── Status cheats ──

    public static bool MaxScore { get; set; }
    public static float ScoreMultiplier { get; set; } = 1.0f;

    // ── Enemy cheats ──

    public static bool FreezeEnemies { get; set; }
    public static bool OneHitKill { get; set; }
    public static float DamageMultiplier { get; set; } = 1.0f;

    // ── Game cheats ──

    public static bool UnknownMapAlwaysTreasure { get; set; }

    // ── Map rewrite (QoL) ──

    public static bool MapRewriteEnabled { get; set; }
    public static MapRewriteMode MapRewriteMode { get; set; } = MapRewriteMode.None;
    public static bool MapKeepFinalBoss { get; set; } = true;

    // ── Restart-with-Seed pending state ──

    /// <summary>Cards/Relics to carry over into the next run. Consumed in RunStartPatch.</summary>
    public static LoadoutPreset? PendingRestartPreset { get; set; }

    /// <summary>Which parts of <see cref="PendingRestartPreset"/> to apply.</summary>
    public static PresetContents PendingRestartScope { get; set; }

    /// <summary>Gold amount to carry over. Null = don't carry gold.</summary>
    public static int? PendingRestartGold { get; set; }

    /// <summary>
    /// Seed to inject into the next run via a Harmony prefix on NGame.StartNewSingleplayerRun.
    /// Null = let the game generate a random seed as usual.
    /// Note: NGame.DebugSeedOverride is overwritten/cleared by NCharacterSelectScreen.BeginRun,
    /// so we inject the seed directly into the StartNewSingleplayerRun call instead.
    /// </summary>
    public static string? PendingRestartSeed { get; set; }

    // ── Runtime stat modifiers ──

    public static RuntimeStatModifiers? StatModifiers { get; set; }

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
        InDevRun = IsActive || AlwaysEnabled;
        IsActive = false;
    }

    /// <summary>
    /// When true, <see cref="MainMenuPatch"/> will automatically push character-select
    /// as soon as the main menu is ready, bypassing the Dev menu submenu.
    /// Used by "Restart with Seed" so the user doesn't have to click twice.
    /// </summary>
    public static bool AutoProceedToCharSelect { get; set; }

    /// <summary>Clear pending restart state after it has been consumed (or the run was abandoned).</summary>
    public static void ClearPendingRestart()
    {
        PendingRestartPreset     = null;
        PendingRestartScope      = PresetContents.None;
        PendingRestartGold       = null;
        PendingRestartSeed       = null;
        AutoProceedToCharSelect  = false;
    }

    public static void OnRunEnded()
    {
        InDevRun = false;
        GameSpeed = 1.0f;
        ClearEnemyOverrides();
        ResetCheats();
        // Note: PendingRestart is NOT cleared here — OnRunEnded fires when the *current* run
        // is torn down (CleanUp), which happens right before starting the new run. Clearing
        // here would discard the just-captured carry-over state. It is cleared in
        // ApplyPendingRestart() after being consumed, or on the next run end if unused.
    }

    private static void ResetCheats()
    {
        InfiniteHp = false;
        InfiniteBlock = false;
        InfiniteEnergy = false;
        InfiniteStars = false;
        AlwaysRewardPotion = false;
        AlwaysUpgradeCardReward = false;
        MaxCardRewardRarity = false;
        DefenseMultiplier = 1.0f;
        GoldMultiplier = 1.0f;
        FreeShop = false;
        MaxScore = false;
        ScoreMultiplier = 1.0f;
        FreezeEnemies = false;
        OneHitKill = false;
        DamageMultiplier = 1.0f;
        UnknownMapAlwaysTreasure = false;
        MapRewriteEnabled = false;
        MapRewriteMode = MapRewriteMode.None;
        MapKeepFinalBoss = true;
        StatModifiers = null;
    }
}
