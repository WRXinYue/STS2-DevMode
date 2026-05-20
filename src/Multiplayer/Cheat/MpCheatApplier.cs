using System;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace DevMode.Multiplayer.Cheat;

/// <summary>Single query surface for Harmony cheat patches (SP + MP).</summary>
public static class MpCheatApplier {
    private static readonly AccessTools.FieldRef<PlayerCombatState, Player> CombatPlayerRef =
        AccessTools.FieldRefAccess<PlayerCombatState, Player>("_player");

    public static bool CheatsActive {
        get {
            if (MpCheatSession.InMultiplayerRun)
                return MpCheatSession.CanUseMultiplayerCheats;
            return DevModeState.CheatsInRun;
        }
    }

    public static bool InfiniteHp(Creature creature) =>
        CheatsActive && creature.Player != null && GetPlayerFlags(creature.Player).InfiniteHp;

    public static bool InfiniteBlock(Creature creature) =>
        CheatsActive && creature.Player != null && GetPlayerFlags(creature.Player).InfiniteBlock;

    public static bool InfiniteEnergy(PlayerCombatState pcs) =>
        CheatsActive && GetPlayerFlags(TryGetPlayer(pcs)).InfiniteEnergy;

    public static bool InfiniteStars(PlayerCombatState pcs) =>
        CheatsActive && GetPlayerFlags(TryGetPlayer(pcs)).InfiniteStars;

    public static bool AlwaysRewardPotion => CheatsActive && MpCheatState.Config.GlobalPlayer.AlwaysRewardPotion;

    public static bool AlwaysUpgradeCardReward => CheatsActive && MpCheatState.Config.GlobalPlayer.AlwaysUpgradeCardReward;

    public static bool MaxCardRewardRarity => CheatsActive && MpCheatState.Config.GlobalPlayer.MaxCardRewardRarity;

    public static float DefenseMultiplier(Player player) {
        if (!CheatsActive) return 1f;
        return GetPlayerFlags(player).DefenseMultiplier;
    }

    public static float GoldMultiplier => CheatsActive ? MpCheatState.Config.GlobalGameplay.GoldMultiplier : 1f;

    public static bool FreeShop => CheatsActive && MpCheatState.Config.GlobalGameplay.FreeShop;

    public static bool FreezeEnemies => CheatsActive && MpCheatState.Config.GlobalEnemy.FreezeEnemies;

    public static bool OneHitKill => CheatsActive && MpCheatState.Config.GlobalEnemy.OneHitKill;

    public static float EnemyDamageMultiplier => CheatsActive ? MpCheatState.Config.GlobalEnemy.DamageMultiplier : 1f;

    public static bool UnknownMapAlwaysTreasure =>
        CheatsActive && MpCheatState.Config.GlobalMap.UnknownMapAlwaysTreasure;

    public static bool FreeTravelFromDevRoomMap =>
        CheatsActive && MpCheatState.Config.GlobalMap.FreeTravelFromDevRoomMap;

    public static bool FrameCheatsAllowed =>
        CheatsActive && !MpCheatSession.InMultiplayerRun;

    private static Player TryGetPlayer(PlayerCombatState pcs) {
        try {
            return CombatPlayerRef(pcs);
        }
        catch {
            var run = MegaCrit.Sts2.Core.Runs.RunManager.Instance;
            var players = run?.DebugOnlyGetState()?.Players;
            return players?.FirstOrDefault() ?? throw new InvalidOperationException("no player");
        }
    }

    private static MpCheatPlayerFlags GetLocalPlayerFlags() {
        if (!MpCheatSession.InMultiplayerRun)
            return MpCheatPlayerFlags.FromDevMode();

        var run = MegaCrit.Sts2.Core.Runs.RunManager.Instance;
        var netId = run?.NetService?.NetId ?? 0;
        return GetPlayerFlags(netId);
    }

    private static MpCheatPlayerFlags GetPlayerFlags(Player player) => GetPlayerFlags(player.NetId);

    private static MpCheatPlayerFlags GetPlayerFlags(ulong netId) {
        var cfg = MpCheatState.Config;
        if (cfg.PerPlayer.TryGetValue(netId, out var per))
            return Merge(cfg.GlobalPlayer, per);
        return cfg.GlobalPlayer;
    }

    private static MpCheatPlayerFlags Merge(MpCheatPlayerFlags global, MpCheatPlayerFlags per) => new() {
        InfiniteHp = per.InfiniteHp || global.InfiniteHp,
        InfiniteBlock = per.InfiniteBlock || global.InfiniteBlock,
        InfiniteEnergy = per.InfiniteEnergy || global.InfiniteEnergy,
        InfiniteStars = per.InfiniteStars || global.InfiniteStars,
        AlwaysRewardPotion = per.AlwaysRewardPotion || global.AlwaysRewardPotion,
        AlwaysUpgradeCardReward = per.AlwaysUpgradeCardReward || global.AlwaysUpgradeCardReward,
        MaxCardRewardRarity = per.MaxCardRewardRarity || global.MaxCardRewardRarity,
        DefenseMultiplier = per.DefenseMultiplier != 1f ? per.DefenseMultiplier : global.DefenseMultiplier,
    };
}
