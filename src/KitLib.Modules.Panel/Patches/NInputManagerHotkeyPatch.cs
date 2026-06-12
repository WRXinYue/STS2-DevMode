using Godot;
using HarmonyLib;
using KitLib.Hotkeys;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace KitLib.Patches;

/// <summary>
/// Hooks official shortcut dispatch before keycode-only matching
/// (<see cref="NInputManager.ProcessShortcutKeyInput"/>).
/// </summary>
[HarmonyPatch(typeof(NInputManager), "ProcessShortcutKeyInput")]
internal static class NInputManagerProcessShortcutKeyInputPatch {
    static bool _logged;

    [HarmonyPrefix]
    static bool Prefix(NInputManager __instance, InputEvent inputEvent) {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false })
            return true;

        if (!_logged) {
            _logged = true;
            HotkeyDiagnostics.LogListenersReady(nameof(NInputManagerProcessShortcutKeyInputPatch));
        }

        var viewport = __instance.GetViewport();
        if (viewport == null)
            return true;

        return !KitLibHotkeyInput.TryHandleAll(inputEvent, viewport, nameof(NInputManager));
    }
}
