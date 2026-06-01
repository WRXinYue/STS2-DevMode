using Godot;

namespace DevMode.Settings;

internal static class HotkeyDefaults {
    internal static readonly HotkeyBinding ToggleRail =
        HotkeyBinding.Of(Key.Backslash, ctrl: true, shift: true);

    internal static readonly HotkeyBinding ClosePanel =
        HotkeyBinding.Of(Key.Escape);

    internal static readonly HotkeyBinding NextTab =
        HotkeyBinding.Of(Key.Pagedown, ctrl: true, shift: true);

    internal static readonly HotkeyBinding PrevTab =
        HotkeyBinding.Of(Key.Pageup, ctrl: true, shift: true);

    internal static readonly HotkeyBinding LockRail =
        HotkeyBinding.Of(Key.L, ctrl: true, shift: true);

    internal static HotkeyBinding For(string actionId) => actionId switch {
        HotkeyActionId.ToggleRail => ToggleRail.Clone(),
        HotkeyActionId.ClosePanel => ClosePanel.Clone(),
        HotkeyActionId.NextTab => NextTab.Clone(),
        HotkeyActionId.PrevTab => PrevTab.Clone(),
        HotkeyActionId.LockRail => LockRail.Clone(),
        _ => new HotkeyBinding()
    };

    internal static void ApplyTo(DevModeSettings settings) {
        settings.HotkeyToggleRail = ToggleRail.Clone();
        settings.HotkeyClosePanel = ClosePanel.Clone();
        settings.HotkeyNextTab = NextTab.Clone();
        settings.HotkeyPrevTab = PrevTab.Clone();
        settings.HotkeyLockRail = LockRail.Clone();
    }
}
