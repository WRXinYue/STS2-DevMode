using System;
using System.Threading.Tasks;
using DevMode;
using DevMode.AI.Combat;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace DevMode.AI.Sts2.Helpers;

/// <summary>Waits for <see cref="CardModel.TryManualPlay"/> to finish via the action queue.</summary>
internal static class Sts2CombatPlayHelper {
    public static async Task<bool> WaitForManualPlayAsync(CardModel card, TimeSpan timeout) {
        if (NGame.Instance == null)
            return false;

        var deadline = DateTime.UtcNow + timeout;
        var started = DateTime.UtcNow;
        var frames = 0;
        while (DateTime.UtcNow < deadline) {
            await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            frames++;

            if (IsPlayStable(card, out var inHand, out var settled)) {
                var overlay = DescribeOverlay();
                var ok = overlay == null;
                LogWaitEnd(card, ok ? "stable_ok" : "overlay_on_stable", frames, started, inHand, settled, overlay);
                return ok;
            }

            if (DescribeOverlay() is { } blockingOverlay) {
                IsPlayStable(card, out inHand, out settled);
                LogWaitEnd(card, "overlay_abort", frames, started, inHand, settled, blockingOverlay);
                return false;
            }

            if (frames % 60 == 0)
                LogWaitProgress(card, frames, started);
        }

        IsPlayStable(card, out var inHandEnd, out var settledEnd);
        var overlayEnd = DescribeOverlay();
        var timedOutOk = overlayEnd == null && IsPlayStable(card, out _, out _);
        LogWaitEnd(card, timedOutOk ? "timeout_ok" : "timeout_fail", frames, started, inHandEnd, settledEnd, overlayEnd);
        return timedOutOk;
    }

    static bool IsPlayStable(CardModel card, out bool inHand, out bool settled) {
        inHand = IsCardInHand(card);
        settled = Sts2WaitHelper.ArePlayerDrivenActionsSettled();

        if (!CombatManager.Instance.IsInProgress)
            return true;

        if (inHand)
            return false;

        // Instant/skip-anim mode completes card logic without waiting on animation-driven action cleanup.
        if (SkipAnimControl.IsSkipping)
            return true;

        return settled;
    }

    static bool IsCardInHand(CardModel card) {
        if (RunContext.TryGetRunAndPlayer(out _, out var player)) {
            var hand = player.PlayerCombatState?.Hand?.Cards;
            if (hand != null) {
                foreach (var c in hand) {
                    if (ReferenceEquals(c, card))
                        return true;
                }

                return false;
            }
        }

        return card.Pile?.Type == PileType.Hand;
    }

    static string? DescribeOverlay() {
        var peek = NOverlayStack.Instance?.Peek();
        return peek == null ? null : peek.GetType().Name;
    }

    static void LogWaitProgress(CardModel card, int frames, DateTime started) {
        IsPlayStable(card, out var inHand, out var settled);
        AgentDebugLog.Write("P1", "Sts2CombatPlayHelper.Wait", "manual play waiting", new {
            cardId = card.Id.Entry,
            frames,
            elapsedMs = (int)(DateTime.UtcNow - started).TotalMilliseconds,
            inHand,
            actionsSettled = settled,
            skipAnim = SkipAnimControl.IsSkipping,
            fastMode = SaveManager.Instance.PrefsSave.FastMode.ToString(),
            playPhase = Sts2CombatCompat.IsCombatPlayPhaseActive(),
            overlay = DescribeOverlay(),
        });
    }

    static void LogWaitEnd(
        CardModel card,
        string reason,
        int frames,
        DateTime started,
        bool inHand,
        bool settled,
        string? overlay) {
        AgentDebugLog.Write("P1", "Sts2CombatPlayHelper.Wait", "manual play wait", new {
            cardId = card.Id.Entry,
            reason,
            frames,
            elapsedMs = (int)(DateTime.UtcNow - started).TotalMilliseconds,
            inHand,
            actionsSettled = settled,
            skipAnim = SkipAnimControl.IsSkipping,
            fastMode = SaveManager.Instance.PrefsSave.FastMode.ToString(),
            playPhase = Sts2CombatCompat.IsCombatPlayPhaseActive(),
            overlay,
        });
    }
}
