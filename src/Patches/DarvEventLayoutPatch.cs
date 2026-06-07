using System.Collections.Generic;
using System.Reflection.Emit;
using DevMode.Actions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;

namespace DevMode.Patches;

[HarmonyPatch(typeof(Darv), "GenerateInitialOptions")]
internal static class DarvEventLayoutPatch
{
    public static bool OverrideNextBool(Rng rng)
    {
        if (!DevModeState.IsActive)
            return rng.NextBool();
        return AncientEventDebugSession.ResolveDarvBranch(rng);
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var nextBool = AccessTools.Method(typeof(Rng), nameof(Rng.NextBool), []);
        var overrideMethod = AccessTools.Method(typeof(DarvEventLayoutPatch), nameof(OverrideNextBool));

        foreach (var code in instructions)
        {
            if (code.Calls(nextBool))
                yield return new CodeInstruction(OpCodes.Call, overrideMethod);
            else
                yield return code;
        }
    }

    static void Postfix(Darv __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (!DevModeState.IsActive)
            return;

        AncientEventDebugSession.ClearPendingDarvBranch();
        EnsureDustyTomeSetup(__instance, __result);
    }

    internal static void EnsureDustyTomeSetup(Darv darv, IReadOnlyList<EventOption> options)
    {
        if (darv.Owner is null)
            return;

        foreach (var option in options)
        {
            if (option.Relic is DustyTome tome && tome.AncientCard is null)
                tome.SetupForPlayer(darv.Owner);
        }
    }
}
