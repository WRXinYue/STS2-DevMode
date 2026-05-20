using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Multiplayer.PseudoCoop;

/// <summary>Phantom players join mid-run after <see cref="ActionQueueSet"/> was built with one queue.</summary>
internal static class PseudoCoopActionQueue {
    static readonly FieldInfo QueuesField =
        AccessTools.Field(typeof(ActionQueueSet), "_actionQueues")!;

    static readonly Type QueueType = AccessTools.Inner(typeof(ActionQueueSet), "ActionQueue")!;

    static readonly FieldInfo OwnerIdField = AccessTools.Field(QueueType, "ownerId")!;
    static readonly FieldInfo ActionsField = AccessTools.Field(QueueType, "actions")!;

    internal static void EnsureQueueForPlayer(Player player) {
        var set = RunManager.Instance?.ActionQueueSet;
        if (set == null) return;

        if (QueuesField.GetValue(set) is not IList queues) return;

        foreach (var q in queues) {
            if ((ulong)OwnerIdField.GetValue(q)! == player.NetId)
                return;
        }

        var queue = Activator.CreateInstance(QueueType)!;
        OwnerIdField.SetValue(queue, player.NetId);
        ActionsField.SetValue(queue, new List<GameAction>());
        queues.Add(queue);

        MainFile.Logger.Info($"[PseudoCoop] Action queue added for netId={player.NetId}.");
    }
}
