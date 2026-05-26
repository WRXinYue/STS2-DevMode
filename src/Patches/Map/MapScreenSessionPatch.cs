using DevMode.Map;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace DevMode.Patches.Map;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Open))]
public static class MapScreenOpenPatch {
    public static void Postfix(NMapScreen __instance) {
        if (!DevModeState.IsActive) return;
        MapScreenUnlock.OnOpened(__instance);
    }
}

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Close))]
public static class MapScreenClosePatch {
    public static void Postfix(NMapScreen __instance) {
        if (!DevModeState.IsActive) return;
        MapScreenUnlock.OnClosed(__instance);
    }
}
