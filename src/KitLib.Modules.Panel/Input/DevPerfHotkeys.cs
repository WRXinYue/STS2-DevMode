using Godot;
using KitLib.DevPerf;
using KitLib.Settings;
using KitLib.UI;

namespace KitLib.Hotkeys;

internal static class DevPerfHotkeys {
    internal static bool TryHandle(InputEvent @event, Viewport viewport) {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
            return false;

        if (!SettingsStore.Current.HotkeyTogglePerfHud.Matches(key))
            return false;

        if (!SettingsStore.Current.HotkeysEnabled) {
            KitLog.Info("Perf", "Perf overlay hotkey ignored: keyboard shortcuts disabled in settings.");
            viewport.SetInputAsHandled();
            return true;
        }

        if (!KitLibState.IsActive) {
            KitLog.Info("Perf", "Perf overlay hotkey ignored: DevMode inactive.");
            viewport.SetInputAsHandled();
            return true;
        }

        bool next = !SettingsStore.Current.PerfHudEnabled;
        SettingsStore.SetPerfHudEnabled(next);
        KitLibRootServices.EnsureRootServicesNode();
        DevPerfOverlayUI.SyncVisibility();
        KitLog.Info("Perf", $"Overlay toggled {(next ? "ON" : "OFF")} via hotkey.");
        viewport.SetInputAsHandled();
        return true;
    }
}
