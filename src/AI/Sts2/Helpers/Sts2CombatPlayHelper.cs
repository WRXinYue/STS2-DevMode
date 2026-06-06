using System;
using System.Threading.Tasks;
using DevMode.AI.Combat;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace DevMode.AI.Sts2.Helpers;

/// <summary>Waits for <see cref="CardModel.TryManualPlay"/> to finish via the action queue.</summary>
internal static class Sts2CombatPlayHelper {
    public static async Task<bool> WaitForManualPlayAsync(CardModel card, TimeSpan timeout) {
        if (NGame.Instance == null) {
            LogWaitOutcome(card, false, "no_game", stable: false, overlay: null, inHand: true, settled: false);
            return false;
        }

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline) {
            await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);

            bool stable = IsPlayStable(card, out bool inHand, out bool settled);
            string? overlay = DescribeOverlay();

            if (stable) {
                bool ok = overlay == null;
                LogWaitOutcome(card, ok, ok ? "stable_ok" : "stable_overlay", stable, overlay, inHand, settled);
                return ok;
            }

            if (overlay != null) {
                LogWaitOutcome(card, false, "overlay_abort", stable, overlay, inHand, settled);
                return false;
            }
        }

        bool finalStable = IsPlayStable(card, out bool finalInHand, out bool finalSettled);
        string? finalOverlay = DescribeOverlay();
        bool timedOk = finalStable && finalOverlay == null;
        LogWaitOutcome(
            card,
            timedOk,
            timedOk ? "timeout_ok" : "timeout_fail",
            finalStable,
            finalOverlay,
            finalInHand,
            finalSettled);
        return timedOk;
    }

    static bool IsPlayStable(CardModel card, out bool inHand, out bool settled) {
        inHand = card.Pile?.Type == PileType.Hand;
        settled = Sts2WaitHelper.ArePlayerDrivenActionsSettled();

        if (!CombatManager.Instance.IsInProgress)
            return true;

        if (inHand)
            return false;

        return settled;
    }

    static string? DescribeOverlay() {
        var peek = NOverlayStack.Instance?.Peek();
        return peek == null ? null : peek.GetType().Name;
    }

    static void LogWaitOutcome(
        CardModel card,
        bool ok,
        string reason,
        bool stable,
        string? overlay,
        bool inHand,
        bool settled) {
        AgentDebugLog.Write("P1", "Sts2CombatPlayHelper.Wait", "manual play wait", new {
            cardId = card.Id.Entry,
            ok,
            reason,
            stable,
            overlay,
            inHand,
            actionsSettled = settled,
            playPhase = Sts2CombatCompat.IsCombatPlayPhaseActive(),
        });
    }
}
