using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.AI.Sts2.Helpers;

/// <summary>Waits for <see cref="CardModel.TryManualPlay"/> to finish via the action queue.</summary>
internal static class Sts2CombatPlayHelper {
    public static async Task<bool> WaitForManualPlayAsync(CardModel card, TimeSpan timeout) {
        if (NGame.Instance == null)
            return false;

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline) {
            await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);

            if (IsPlayStable(card))
                return true;

            if (NOverlayStack.Instance?.Peek() != null)
                return false;
        }

        return IsPlayStable(card);
    }

    static bool IsPlayStable(CardModel card) {
        if (!CombatManager.Instance.IsInProgress)
            return true;

        if (card.Pile?.Type == PileType.Hand)
            return false;

        return ArePlayerDrivenActionsSettled();
    }

    static bool ArePlayerDrivenActionsSettled() {
        var running = RunManager.Instance.ActionExecutor.CurrentlyRunningAction;
        if (running != null && ActionQueueSet.IsGameActionPlayerDriven(running))
            return false;

        var ready = RunManager.Instance.ActionQueueSet.GetReadyAction();
        if (ready != null && ActionQueueSet.IsGameActionPlayerDriven(ready))
            return false;

        return true;
    }
}
