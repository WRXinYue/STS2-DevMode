using Godot;
using KitLib.Settings;
using KitLib.UI;

namespace KitLib.Hotkeys;

/// <summary>Keyboard shortcuts for DevMode rail shell actions (no InputMap registration).</summary>
internal static class DevPanelHotkeys {
    internal static bool TryHandle(InputEventKey key, Viewport viewport) {
        if (!key.Pressed || key.Echo)
            return false;

        if (!SettingsStore.Current.HotkeysEnabled)
            return false;

        if (!DevPanelUI.IsRailAttached) {
            if (key.CtrlPressed && key.ShiftPressed)
                HotkeyDiagnostics.LogBlocked(nameof(DevPanelHotkeys), "DevPanel rail not attached");
            return false;
        }

        var settings = SettingsStore.Current;

        if (settings.HotkeyClosePanel.Matches(key) && DevPanelUI.HasOpenPanel) {
            HotkeyDiagnostics.LogHandled(nameof(DevPanelHotkeys), HotkeyActionId.ClosePanel);
            DevPanelUI.CloseActivePanel();
            viewport.SetInputAsHandled();
            return true;
        }

        if (settings.HotkeyToggleRail.Matches(key)) {
            HotkeyDiagnostics.LogHandled(nameof(DevPanelHotkeys), HotkeyActionId.ToggleRail);
            DevPanelUI.ToggleRailExpanded();
            viewport.SetInputAsHandled();
            return true;
        }

        if (settings.HotkeyNextTab.Matches(key)) {
            HotkeyDiagnostics.LogHandled(nameof(DevPanelHotkeys), HotkeyActionId.NextTab);
            DevPanelUI.CycleRailTab(+1);
            viewport.SetInputAsHandled();
            return true;
        }

        if (settings.HotkeyPrevTab.Matches(key)) {
            HotkeyDiagnostics.LogHandled(nameof(DevPanelHotkeys), HotkeyActionId.PrevTab);
            DevPanelUI.CycleRailTab(-1);
            viewport.SetInputAsHandled();
            return true;
        }

        if (settings.HotkeyLockRail.Matches(key)) {
            HotkeyDiagnostics.LogHandled(nameof(DevPanelHotkeys), HotkeyActionId.LockRail);
            DevPanelUI.ToggleRailKeyboardPin();
            viewport.SetInputAsHandled();
            return true;
        }

        HotkeyDiagnostics.LogNearMatch(nameof(DevPanelHotkeys), settings.HotkeyToggleRail, key);
        return false;
    }
}
