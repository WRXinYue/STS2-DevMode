using System;
using Godot;
using HarmonyLib;
using KitLib.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace KitLib.Patches;

[HarmonyPatch(typeof(NMainMenuSubmenuStack), nameof(NMainMenuSubmenuStack.GetSubmenuType), new[] { typeof(Type) })]
public static class ModPanelSubmenuStackPatch {
    private static ModPanelSubmenu? _cached;

    [HarmonyPrefix]
    public static bool Prefix(Type type, NMainMenuSubmenuStack __instance, ref NSubmenu __result) {
        if (type != typeof(ModPanelSubmenu))
            return true;
        if (_cached == null || !GodotObject.IsInstanceValid(_cached)) {
            _cached = new ModPanelSubmenu {
                Visible = false,
            };
            __instance.AddChildSafely(_cached);
        }
        __result = _cached;
        return false;
    }
}
