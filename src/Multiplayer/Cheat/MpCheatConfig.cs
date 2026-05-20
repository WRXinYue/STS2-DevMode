using System.Collections.Generic;
using DevMode;

namespace DevMode.Multiplayer.Cheat;

/// <summary>Serializable multiplayer cheat snapshot (Tier 0/1 sync).</summary>
public sealed class MpCheatConfig {
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public bool SessionEnabled { get; set; }

    public MpCheatPlayerFlags GlobalPlayer { get; set; } = new();

    public MpCheatEnemyFlags GlobalEnemy { get; set; } = new();

    public MpCheatGameplayFlags GlobalGameplay { get; set; } = new();

    public MpCheatMapFlags GlobalMap { get; set; } = new();

    /// <summary>Per-player overrides keyed by net id (empty = use global only).</summary>
    public Dictionary<ulong, MpCheatPlayerFlags> PerPlayer { get; set; } = new();

    public static MpCheatConfig FromDevModeState() {
        return new MpCheatConfig {
            SessionEnabled = true,
            GlobalPlayer = MpCheatPlayerFlags.FromDevMode(),
            GlobalEnemy = MpCheatEnemyFlags.FromDevMode(),
            GlobalGameplay = MpCheatGameplayFlags.FromDevMode(),
            GlobalMap = MpCheatMapFlags.FromDevMode(),
        };
    }

    public void ApplyToDevModeState() {
        GlobalPlayer.ApplyToDevMode();
        GlobalEnemy.ApplyToDevMode();
        GlobalGameplay.ApplyToDevMode();
        GlobalMap.ApplyToDevMode();
    }
}

public sealed class MpCheatPlayerFlags {
    public bool InfiniteHp { get; set; }
    public bool InfiniteBlock { get; set; }
    public bool InfiniteEnergy { get; set; }
    public bool InfiniteStars { get; set; }
    public bool AlwaysRewardPotion { get; set; }
    public bool AlwaysUpgradeCardReward { get; set; }
    public bool MaxCardRewardRarity { get; set; }
    public float DefenseMultiplier { get; set; } = 1f;

    public static MpCheatPlayerFlags FromDevMode() => new() {
        InfiniteHp = DevModeState.PlayerCheats.InfiniteHp,
        InfiniteBlock = DevModeState.PlayerCheats.InfiniteBlock,
        InfiniteEnergy = DevModeState.PlayerCheats.InfiniteEnergy,
        InfiniteStars = DevModeState.PlayerCheats.InfiniteStars,
        AlwaysRewardPotion = DevModeState.PlayerCheats.AlwaysRewardPotion,
        AlwaysUpgradeCardReward = DevModeState.PlayerCheats.AlwaysUpgradeCardReward,
        MaxCardRewardRarity = DevModeState.PlayerCheats.MaxCardRewardRarity,
        DefenseMultiplier = DevModeState.PlayerCheats.DefenseMultiplier,
    };

    public void ApplyToDevMode() {
        DevModeState.PlayerCheats.InfiniteHp = InfiniteHp;
        DevModeState.PlayerCheats.InfiniteBlock = InfiniteBlock;
        DevModeState.PlayerCheats.InfiniteEnergy = InfiniteEnergy;
        DevModeState.PlayerCheats.InfiniteStars = InfiniteStars;
        DevModeState.PlayerCheats.AlwaysRewardPotion = AlwaysRewardPotion;
        DevModeState.PlayerCheats.AlwaysUpgradeCardReward = AlwaysUpgradeCardReward;
        DevModeState.PlayerCheats.MaxCardRewardRarity = MaxCardRewardRarity;
        DevModeState.PlayerCheats.DefenseMultiplier = DefenseMultiplier;
    }
}

public sealed class MpCheatEnemyFlags {
    public bool FreezeEnemies { get; set; }
    public bool OneHitKill { get; set; }
    public float DamageMultiplier { get; set; } = 1f;

    public static MpCheatEnemyFlags FromDevMode() => new() {
        FreezeEnemies = DevModeState.EnemyCheats.FreezeEnemies,
        OneHitKill = DevModeState.EnemyCheats.OneHitKill,
        DamageMultiplier = DevModeState.EnemyCheats.DamageMultiplier,
    };

    public void ApplyToDevMode() {
        DevModeState.EnemyCheats.FreezeEnemies = FreezeEnemies;
        DevModeState.EnemyCheats.OneHitKill = OneHitKill;
        DevModeState.EnemyCheats.DamageMultiplier = DamageMultiplier;
    }
}

public sealed class MpCheatGameplayFlags {
    public float GoldMultiplier { get; set; } = 1f;
    public bool FreeShop { get; set; }

    public static MpCheatGameplayFlags FromDevMode() => new() {
        GoldMultiplier = DevModeState.GameplayModifiers.GoldMultiplier,
        FreeShop = DevModeState.GameplayModifiers.FreeShop,
    };

    public void ApplyToDevMode() {
        DevModeState.GameplayModifiers.GoldMultiplier = GoldMultiplier;
        DevModeState.GameplayModifiers.FreeShop = FreeShop;
    }
}

public sealed class MpCheatMapFlags {
    public bool UnknownMapAlwaysTreasure { get; set; }
    public bool FreeTravelFromDevRoomMap { get; set; }

    public static MpCheatMapFlags FromDevMode() => new() {
        UnknownMapAlwaysTreasure = DevModeState.MapCheats.UnknownMapAlwaysTreasure,
        FreeTravelFromDevRoomMap = DevModeState.MapCheats.FreeTravelFromDevRoomMap,
    };

    public void ApplyToDevMode() {
        DevModeState.MapCheats.UnknownMapAlwaysTreasure = UnknownMapAlwaysTreasure;
        DevModeState.MapCheats.FreeTravelFromDevRoomMap = FreeTravelFromDevRoomMap;
    }
}
