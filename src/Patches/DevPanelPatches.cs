using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;

namespace DevMode.Patches;

[HarmonyPatch(typeof(NGlobalUi), "_Ready")]
public static class GlobalUiReadyPatch {
    // Track the instance we already attached to avoid duplicate panels on re-entry
    private static NGlobalUi? _attached;
    private static AssetWarmupService? _warmup;

    public static void Postfix(NGlobalUi __instance) {
        if (!DevModeState.InDevRun && DevModeState.DebugMode == DebugMode.Off) return;
        if (_attached == __instance) return;
        _attached = __instance;
        DevPanel.Attach(__instance);

        // Initialize RuntimeStatModifiers
        if (DevModeState.StatModifiers == null)
            DevModeState.StatModifiers = new RuntimeStatModifiers();

        // Initialize AssetWarmupService
        if (_warmup == null) {
            _warmup = new AssetWarmupService();
            _warmup.Ready();
        }

        // Hook into _Process via a helper node
        var processNode = ((Node)__instance).GetNodeOrNull<Node>("DevModeProcessNode");
        if (processNode == null) {
            processNode = new DevModeProcessNode { Name = "DevModeProcessNode" };
            ((Node)__instance).AddChild(processNode);
        }
    }

    internal static void Process(double delta) {
        DevModeState.StatModifiers?.Update(delta);
        _warmup?.Process(delta);
    }
}

[HarmonyPatch(typeof(NCardLibrary), "ShowCardDetail")]
public static class CardLibraryShowCardDetailPatch {
    public static bool Prefix(NCardHolder holder) {
        return !DevPanel.TryHandleCardSelection(holder);
    }
}

[HarmonyPatch(typeof(NRelicCollectionCategory), "OnRelicEntryPressed")]
public static class RelicCollectionEntryPressedPatch {
    public static bool Prefix(NRelicCollectionEntry entry) {
        return !DevPanel.TryHandleRelicSelection(entry);
    }
}

[HarmonyPatch(typeof(NCardLibrary), "OnSubmenuClosed")]
public static class CardLibraryClosedPatch {
    public static void Postfix() {
        DevPanel.NotifyCardLibraryClosed();
        if (DevModeState.InMenuPreview) {
            DevModeState.OnMenuPreviewClosed?.Invoke();
            DevModeState.OnMenuPreviewClosed = null;
            DevModeState.InMenuPreview = false;
        }
    }
}

[HarmonyPatch(typeof(NRelicCollection), "OnSubmenuClosed")]
public static class RelicCollectionClosedPatch {
    public static void Postfix() {
        DevPanel.NotifyRelicCollectionClosed();
        if (DevModeState.InMenuPreview) {
            DevModeState.OnMenuPreviewClosed?.Invoke();
            DevModeState.OnMenuPreviewClosed = null;
            DevModeState.InMenuPreview = false;
        }
    }
}

[HarmonyPatch(typeof(NCardLibraryGrid), "GetCardVisibility")]
public static class CardVisibilityPatch {
    public static void Postfix(ref ModelVisibility __result) {
        if (!DevModeState.InDevRun && !DevModeState.InMenuPreview) return;
        __result = ModelVisibility.Visible;
    }
}

[HarmonyPatch(typeof(NRelicCollectionCategory), "LoadRelicNodes")]
public static class RelicVisibilityPatch {
    public static void Prefix(
        IEnumerable<RelicModel> relics,
        ref HashSet<RelicModel> seenRelics,
        ref HashSet<RelicModel> unlockedRelics) {
        if (!DevModeState.InDevRun && !DevModeState.InMenuPreview) return;
        foreach (var relic in relics) {
            seenRelics.Add(relic);
            unlockedRelics.Add(relic);
        }
    }
}

[HarmonyPatch(typeof(NRelicCollectionCategory), "LoadRelics")]
public static class RelicCategoryVisibilityPatch {
    public static void Prefix(
        ref HashSet<RelicModel> seenRelics,
        ref HashSet<RelicModel> allUnlockedRelics) {
        if (!DevModeState.InDevRun && !DevModeState.InMenuPreview) return;
        foreach (var relic in ModelDb.AllRelics) {
            seenRelics.Add(relic);
            allUnlockedRelics.Add(relic);
        }
    }
}

public static class AncientUnlockPatch {
    public static void Postfix(ActModel __instance, ref IEnumerable<AncientEventModel> __result) {
        if (!DevModeState.InDevRun && !DevModeState.InMenuPreview) return;
        __result = __instance.AllAncients;
    }
}

[HarmonyPatch(typeof(Glory), nameof(Glory.GetUnlockedAncients))]
public static class GloryAncientPatch {
    public static void Postfix(ActModel __instance, ref IEnumerable<AncientEventModel> __result)
        => AncientUnlockPatch.Postfix(__instance, ref __result);
}

[HarmonyPatch(typeof(Hive), nameof(Hive.GetUnlockedAncients))]
public static class HiveAncientPatch {
    public static void Postfix(ActModel __instance, ref IEnumerable<AncientEventModel> __result)
        => AncientUnlockPatch.Postfix(__instance, ref __result);
}

[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.GetUnlockedAncients))]
public static class OvergrowthAncientPatch {
    public static void Postfix(ActModel __instance, ref IEnumerable<AncientEventModel> __result)
        => AncientUnlockPatch.Postfix(__instance, ref __result);
}

[HarmonyPatch(typeof(Underdocks), nameof(Underdocks.GetUnlockedAncients))]
public static class UnderdocksAncientPatch {
    public static void Postfix(ActModel __instance, ref IEnumerable<AncientEventModel> __result)
        => AncientUnlockPatch.Postfix(__instance, ref __result);
}
