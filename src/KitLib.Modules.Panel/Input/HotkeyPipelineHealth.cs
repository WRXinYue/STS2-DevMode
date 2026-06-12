using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using KitLib.Settings;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace KitLib.Hotkeys;

/// <summary>Startup audit for the KitLib hotkey Harmony hook and official input API surface.</summary>
internal static class HotkeyPipelineHealth {
    const string Scope = "Hotkey";

    static readonly string CriticalPatchType =
        typeof(Patches.NInputManagerProcessShortcutKeyInputPatch).FullName!;

    internal static void ReportAfterHarmony(string harmonyId) {
        var result = KitLibHarmony.GetLastApplyResult(harmonyId);
        if (result == null) {
            KitLog.Warn(Scope, $"Harmony apply result missing for {harmonyId}; hotkey pipeline status unknown.");
            return;
        }

        int failedChecks = 0;

        if (result.WasPatchTypeSkipped(CriticalPatchType)) {
            failedChecks++;
            var reason = result.Skipped.First(s => s.PatchTypeFullName == CriticalPatchType).Reason;
            KitLog.Error(Scope,
                $"CRITICAL: Harmony patch not applied ({ShortName(CriticalPatchType)}): {reason}. " +
                "Hotkeys will not work. Use [HarmonyPatch(typeof(NInputManager), \"ProcessShortcutKeyInput\")].");
        }

        const string methodName = "ProcessShortcutKeyInput";
        if (!TryResolveMethod(typeof(NInputManager), methodName, [typeof(InputEvent)], out var method)) {
            failedChecks++;
            KitLog.Error(Scope,
                $"CRITICAL: game API missing NInputManager.{methodName} — official input surface changed; " +
                "update hotkey patches and eng/api_touchpoints.yaml.");
        }
        else if (!KitLibHarmony.IsMethodPatched(method!)) {
            failedChecks++;
            KitLog.Error(Scope,
                $"CRITICAL: NInputManager.{methodName} has no Harmony prefix — hotkeys will not intercept input.");
        }

        if (failedChecks == 0) {
            KitLog.Info(Scope,
                "Pipeline health OK: ProcessShortcutKeyInput hook applied. " +
                $"Bindings: perf={SettingsStore.Current.HotkeyTogglePerfHud.FormatLabel()}, " +
                $"rail={SettingsStore.Current.HotkeyToggleRail.FormatLabel()}. " +
                "Press a Ctrl+Shift combo to emit probe logs.");
            return;
        }

        KitLog.Error(Scope,
            $"Pipeline health FAILED ({failedChecks} check(s)). See messages above; run `make verify-profiles` after game updates.");
    }

    static bool TryResolveMethod(Type type, string methodName, Type[] parameters, out MethodInfo? method) {
        method = AccessTools.Method(type, methodName, parameters);
        return method != null;
    }

    static string ShortName(string fullName) {
        var idx = fullName.LastIndexOf('.');
        return idx >= 0 ? fullName[(idx + 1)..] : fullName;
    }
}
