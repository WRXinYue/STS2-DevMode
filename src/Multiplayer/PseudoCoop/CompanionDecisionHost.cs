using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevMode.AI;
using DevMode.AI.Core;
using DevMode.AI.Core.Schema;
using DevMode.Companion;
using DevMode.Multiplayer.Cheat;
using DevMode.Settings;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Multiplayer.PseudoCoop;

/// <summary>Runs <see cref="GameLoop"/> for host-driven companions during non-combat phases.</summary>
internal static class CompanionDecisionHost {
    static bool _tickRunning;
    static readonly Dictionary<ulong, GameLoop> _loops = [];

    static readonly GamePhase[] NonCombatPhases = [
        GamePhase.MapSelection,
        GamePhase.CardReward,
        GamePhase.EventChoice,
        GamePhase.Shop,
        GamePhase.RestSite,
        GamePhase.RewardScreen,
        GamePhase.RelicSelection,
        GamePhase.PostCombatTransition,
        GamePhase.TreasureRoom,
        GamePhase.Unknown,
    ];

    public static bool IsEnabled =>
        SettingsStore.Current.MpAiTeammateEnabled
        && MpCheatSession.IsHost
        && MpCheatSession.InMultiplayerRun;

    public static void OnRunEnded() {
        _tickRunning = false;
        _loops.Clear();
    }

    public static void Poll(double delta, ref double accum) {
        if (!IsEnabled) return;

        accum += delta;
        if (accum < 0.6) return;
        accum = 0;

        if (_tickRunning) return;

        var cm = CombatManager.Instance;
        if (cm != null && Sts2CombatCompat.IsCombatPlayPhase(cm))
            return;

        var phase = AiPlayServices.StateProvider.CurrentPhase;
        if (phase is GamePhase.None or GamePhase.Combat or GamePhase.GameOver or GamePhase.Victory)
            return;

        if (!NonCombatPhases.Contains(phase))
            return;

        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state == null) return;

        foreach (var player in SimulatedPeerRegistry.GetMpAiTeammateTargets()) {
            if (!ShouldRunNonCombatFor(player)) continue;

            _tickRunning = true;
            TaskHelper.RunSafely(RunDecisionAsync(player, phase));
            return;
        }
    }

    static bool ShouldRunNonCombatFor(Player player) {
        if (CompanionNonCombatRegistry.IsEnabled(player.NetId))
            return true;

        var characterId = player.Character?.Id.Entry;
        return CharacterAiRegistry.SupportsNonCombat(characterId);
    }

    static async Task RunDecisionAsync(Player player, GamePhase phase) {
        try {
            AiHostContext.ActiveNetId = player.NetId;
            var loop = GetOrCreateLoop(player);
            await loop.OnDecisionPointAsync(phase);
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[CompanionAi] Decision failed netId={player.NetId}: {ex.Message}");
        }
        finally {
            AiHostContext.Clear();
            _tickRunning = false;
        }
    }

    static GameLoop GetOrCreateLoop(Player player) {
        if (_loops.TryGetValue(player.NetId, out var existing))
            return existing;

        var loop = new GameLoop(
            AiPlayServices.StateProvider,
            AiPlayServices.ActionExecutor,
            StrategyResolver.Resolve(player),
            msg => AiDecisionLog.Record("Companion", $"netId={player.NetId} {msg}")) {
            ActionDelayMs = SettingsStore.Current.AutoPlayDelayMs,
        };
        _loops[player.NetId] = loop;
        return loop;
    }
}
