using Godot;

namespace KitLib.Settings;

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
        HotkeyBinding.Of(Key.Semicolon, ctrl: true, shift: true);

    internal static readonly HotkeyBinding QuickSave =
        HotkeyBinding.Of(Key.Comma, ctrl: true, shift: true);

    internal static readonly HotkeyBinding QuickLoad =
        HotkeyBinding.Of(Key.Period, ctrl: true, shift: true);

    internal static readonly HotkeyBinding QuickReplayCombat =
        HotkeyBinding.Of(Key.Minus, ctrl: true, shift: true);

    internal static readonly HotkeyBinding QuickReplayTurn =
        HotkeyBinding.Of(Key.Equal, ctrl: true, shift: true);

    internal static readonly HotkeyBinding TogglePerfHud =
        HotkeyBinding.Of(Key.Slash, ctrl: true, shift: true);

    internal static HotkeyBinding For(string actionId) => actionId switch {
        HotkeyActionId.ToggleRail => ToggleRail.Clone(),
        HotkeyActionId.ClosePanel => ClosePanel.Clone(),
        HotkeyActionId.NextTab => NextTab.Clone(),
        HotkeyActionId.PrevTab => PrevTab.Clone(),
        HotkeyActionId.LockRail => LockRail.Clone(),
        HotkeyActionId.QuickSave => QuickSave.Clone(),
        HotkeyActionId.QuickLoad => QuickLoad.Clone(),
        HotkeyActionId.QuickReplayCombat => QuickReplayCombat.Clone(),
        HotkeyActionId.QuickReplayTurn => QuickReplayTurn.Clone(),
        HotkeyActionId.TogglePerfHud => TogglePerfHud.Clone(),
        _ => new HotkeyBinding()
    };

    internal static void ApplyTo(KitLibSettings settings) {
        settings.HotkeyToggleRail = ToggleRail.Clone();
        settings.HotkeyClosePanel = ClosePanel.Clone();
        settings.HotkeyNextTab = NextTab.Clone();
        settings.HotkeyPrevTab = PrevTab.Clone();
        settings.HotkeyLockRail = LockRail.Clone();
        settings.HotkeyQuickSave = QuickSave.Clone();
        settings.HotkeyQuickLoad = QuickLoad.Clone();
        settings.HotkeyQuickReplayCombat = QuickReplayCombat.Clone();
        settings.HotkeyQuickReplayTurn = QuickReplayTurn.Clone();
        settings.HotkeyTogglePerfHud = TogglePerfHud.Clone();
    }
}
