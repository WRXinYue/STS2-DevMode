using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Multiplayer.PseudoCoop.Patches;

[HarmonyPatch(typeof(NRun), nameof(NRun._Process))]
internal static class MpAiTeammatePollPatch {
    static double _accum;

    [HarmonyPostfix]
    static void Postfix(double delta) => MpAiTeammateHost.Poll(delta, ref _accum);
}
