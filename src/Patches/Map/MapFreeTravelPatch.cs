using DevMode.Map;
using DevMode.Multiplayer.Cheat;
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;

namespace DevMode.Patches.Map;

[HarmonyPatch(typeof(Hook), nameof(Hook.ShouldAllowFreeTravel))]
public static class MapFreeTravelPatch {
    public static bool Prefix(ref bool __result) {
        if (!MapScreenUnlock.IsActive && !MpCheatApplier.FreeTravelFromDevRoomMap)
            return true;
        __result = true;
        return false;
    }
}
