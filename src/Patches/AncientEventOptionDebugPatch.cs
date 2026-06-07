using System.Collections.Generic;
using DevMode.Patches;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;

namespace DevMode.Patches;

/// <summary>
/// When <see cref="AncientEventModel.DebugOption"/> pins dusty tome from <c>AllPossibleOptions</c>,
/// run <see cref="DustyTome.SetupForPlayer"/> so the ancient card name and obtain path work.
/// </summary>
[HarmonyPatch(typeof(AncientEventModel), "GenerateInitialOptionsWrapper")]
internal static class AncientEventOptionDebugPatch
{
    static void Postfix(AncientEventModel __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (!DevModeState.IsActive || __result.Count == 0)
            return;

        if (__instance is Darv darv)
            DarvEventLayoutPatch.EnsureDustyTomeSetup(darv, __result);
        else
            EnsureDustyTomeSetup(__instance, __result);
    }

    static void EnsureDustyTomeSetup(AncientEventModel ancient, IReadOnlyList<EventOption> options)
    {
        if (ancient.Owner is null)
            return;

        foreach (var option in options)
        {
            if (option.Relic is DustyTome tome && tome.AncientCard is null)
                tome.SetupForPlayer(ancient.Owner);
        }
    }
}
