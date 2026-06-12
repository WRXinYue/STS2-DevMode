using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace KitLib.Settings;

/// <summary>
/// KitLib hotkey validation against live official keyboard bindings
/// (<see cref="NInputManager.GetShortcutKey"/> / <c>ProcessShortcutKeyInput</c>).
/// </summary>
internal static class OfficialGameInput {
    /// <summary>
    /// True when <paramref name="key"/> matches a bound official action keycode
    /// (modifiers ignored, same as <c>ProcessShortcutKeyInput</c>).
    /// </summary>
    internal static bool UsesPlayerKeyboardShortcut(Key key) {
        var mgr = NInputManager.Instance;
        if (mgr == null)
            return false;

        foreach (var input in NInputManager.remappableKeyboardInputs) {
            if (mgr.GetShortcutKey(input) == key)
                return true;
        }

        return false;
    }

    /// <summary>Bare F1–F12 keys reserved for official tools (console, feedback, trailer debug).</summary>
    internal static bool UsesOfficialReservedFunctionKey(HotkeyBinding binding) {
        if (binding.Ctrl || binding.Shift || binding.Alt)
            return false;
        return binding.Keycode is >= Key.F1 and <= Key.F12;
    }
}
