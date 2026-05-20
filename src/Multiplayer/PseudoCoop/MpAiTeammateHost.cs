using System.Linq;
using System.Threading.Tasks;
using DevMode.AI;
using DevMode.AI.AutoPlay.Strategies;
using DevMode.AI.Core;
using DevMode.AI.Core.Schema;
using DevMode.Multiplayer.Cheat;
using DevMode.Settings;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Multiplayer.PseudoCoop;

/// <summary>Host-only: rule-based combat for simulated remote players (human plays local).</summary>
internal static class MpAiTeammateHost {
    static GameLoop? _loop;
    static bool _tickRunning;

    public static bool IsEnabled =>
        SettingsStore.Current.MpAiTeammateEnabled
        && MpCheatSession.IsHost
        && MpCheatSession.InMultiplayerRun;

    public static void OnRunEnded() {
        AiHostContext.Clear();
        _tickRunning = false;
        _loop = null;
    }

    public static void Poll(double delta, ref double accum) {
        if (!IsEnabled) return;

        accum += delta;
        if (accum < 0.4) return;
        accum = 0;

        if (_tickRunning) return;

        var cm = CombatManager.Instance;
        if (cm is not { IsInProgress: true, IsPlayPhase: true }) return;

        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state == null) return;

        SimulatedPeerRegistry.Refresh();

        foreach (var player in state.Players) {
            if (LocalContext.IsMe(player)) continue;
            if (!SimulatedPeerRegistry.IsSimulatedPeer(player.NetId)) continue;
            if (player.Creature.IsDead) continue;
            if (cm.IsPlayerReadyToEndTurn(player)) continue;
            if (!ShouldActForPlayer(player, cm)) continue;

            _tickRunning = true;
            TaskHelper.RunSafely(RunCombatDecisionAsync(player));
            return;
        }
    }

    static bool ShouldActForPlayer(Player player, CombatManager cm) {
        var hand = player.PlayerCombatState?.Hand?.Cards;
        if (hand == null) return true;

        var cards = hand.ToList();
        if (cards.Count == 0) return true;

        return cards.Any(c => c.CanPlay(out _, out _));
    }

    static async Task RunCombatDecisionAsync(Player player) {
        try {
            EnsureLoop();
            AiHostContext.ActiveNetId = player.NetId;
            await _loop!.OnDecisionPointAsync(GamePhase.Combat);
        }
        catch (System.Exception ex) {
            MainFile.Logger.Warn($"[MpAiTeammate] Decision failed netId={player.NetId}: {ex.Message}");
        }
        finally {
            AiHostContext.Clear();
            _tickRunning = false;
        }
    }

    static void EnsureLoop() {
        _loop ??= new GameLoop(
            AiPlayServices.StateProvider,
            AiPlayServices.ActionExecutor,
            new SimpleStrategy(),
            msg => MainFile.Logger.Info($"[MpAiTeammate] {msg}")) {
            ActionDelayMs = SettingsStore.Current.AutoPlayDelayMs,
        };
    }
}
