using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace DevMode.AI.Sts2.Helpers;

/// <summary>Waits for <see cref="MegaCrit.Sts2.Core.Models.PotionModel.EnqueueManualUse"/> to finish.</summary>
internal static class Sts2PotionUseHelper {
    public static async Task<bool> WaitForManualUseAsync(
        Player player,
        int potionSlot,
        string potionId,
        TimeSpan timeout) {
        return await Sts2WaitHelper.Until(
            () => IsUseStable(player, potionSlot, potionId),
            timeout);
    }

    static bool IsUseStable(Player player, int potionSlot, string potionId) {
        if (NOverlayStack.Instance?.Peek() != null)
            return false;

        if (!Sts2WaitHelper.ArePlayerDrivenActionsSettled())
            return false;

        if (!CombatManager.Instance.IsInProgress)
            return true;

        var current = player.GetPotionAtSlotIndex(potionSlot);
        if (current == null)
            return true;

        var currentId = current.Id.Entry ?? "";
        return !string.Equals(currentId, potionId, StringComparison.OrdinalIgnoreCase);
    }
}
