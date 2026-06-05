using System;
using System.Threading.Tasks;
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
        if (NGame.Instance == null)
            return false;

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline) {
            await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);

            if (IsPlayStable(card))
                return NOverlayStack.Instance?.Peek() == null;

            if (NOverlayStack.Instance?.Peek() != null)
                return false;
        }

        return IsPlayStable(card) && NOverlayStack.Instance?.Peek() == null;
    }

    static bool IsPlayStable(CardModel card) {
        if (!CombatManager.Instance.IsInProgress)
            return true;

        if (card.Pile?.Type == PileType.Hand)
            return false;

        return Sts2WaitHelper.ArePlayerDrivenActionsSettled();
    }
}
