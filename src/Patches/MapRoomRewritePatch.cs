using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Patches;

/// <summary>
/// Rewrites map room types based on QoL settings.
/// Can force all rooms to chests, elites, or bosses.
/// </summary>
[HarmonyPatch]
public static class MapRoomRewritePatch {
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod() {
        return AccessTools.Method(typeof(RunManager), "CreateRoom",
            new[] { typeof(RoomType), typeof(MapPointType), typeof(AbstractModel) });
    }

    [HarmonyPrefix]
    public static void Prefix(ref RoomType __0, ref MapPointType __1, ref AbstractModel? __2) {
        if (!DevModeState.CheatsInRun || !DevModeState.MapCheats.MapRewriteEnabled) return;

        // Optionally keep final boss
        if (DevModeState.MapCheats.MapKeepFinalBoss && __0 == RoomType.Boss) return;

        // Only rewrite combat-related rooms (Monster, Elite, Unknown)
        if (__0 != RoomType.Monster && __0 != RoomType.Elite && (int)__0 != 8) return;

        switch (DevModeState.MapCheats.MapRewriteMode) {
            case MapRewriteMode.AllChest:
                __0 = RoomType.Treasure;
                __1 = (MapPointType)3;
                __2 = null;
                break;
            case MapRewriteMode.AllElite:
                __0 = RoomType.Elite;
                __1 = (MapPointType)6;
                __2 = null;
                break;
            case MapRewriteMode.AllBoss:
                __0 = RoomType.Boss;
                __1 = (MapPointType)7;
                __2 = null;
                break;
        }
    }
}
