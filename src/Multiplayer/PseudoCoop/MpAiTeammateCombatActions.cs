using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Multiplayer.PseudoCoop;

internal static class MpAiTeammateCombatActions {
    public static void SignalEndTurn(Player player) {
        if (SimulatedPeerRegistry.ShouldHostEnqueueCombatAction(player))
            EnqueueEndTurn(player);
        else
            CombatManager.Instance?.SetReadyToEndTurn(player, canBackOut: false);
    }

    public static void EnqueueEndTurn(Player player) {
        var round = CombatManager.Instance?.DebugOnlyGetState()?.RoundNumber ?? 1;
        var action = new EndPlayerTurnAction(player, round);
        RunManager.Instance!.ActionQueueSynchronizer.RequestEnqueue(action);
        MainFile.Logger.Info($"[MpAiTeammate] Enqueued end turn netId={player.NetId} round={round}.");
    }
}
