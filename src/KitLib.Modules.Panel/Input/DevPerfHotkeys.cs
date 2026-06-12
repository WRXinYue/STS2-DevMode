using Godot;
using KitLib.DevPerf;
using KitLib.Settings;
using KitLib.UI;

namespace KitLib.Hotkeys;

internal static class DevPerfHotkeys {
    internal static bool TryHandle(InputEventKey key, Viewport viewport) {
        if (!key.Pressed || key.Echo)
            return false;

        var binding = SettingsStore.Current.HotkeyTogglePerfHud;
        if (!binding.Matches(key)) {
            HotkeyDiagnostics.LogNearMatch(nameof(DevPerfHotkeys), binding, key);
            return false;
        }

        if (!SettingsStore.Current.HotkeysEnabled) {
            HotkeyDiagnostics.LogBlocked(nameof(DevPerfHotkeys), "keyboard shortcuts disabled in settings");
            viewport.SetInputAsHandled();
            return true;
        }

        if (!KitLibState.IsActive) {
            HotkeyDiagnostics.LogBlocked(nameof(DevPerfHotkeys),
                "DevMode inactive (enable DevPanel/Cheat or start a dev test run)");
            viewport.SetInputAsHandled();
            return true;
        }

        bool next = !SettingsStore.Current.PerfHudEnabled;
        SettingsStore.SetPerfHudEnabled(next);
        KitLibRootServices.EnsureRootServicesNode();
        DevPerfOverlayUI.SyncVisibility();
        HotkeyDiagnostics.LogHandled(nameof(DevPerfHotkeys), $"perf overlay {(next ? "ON" : "OFF")}");
        viewport.SetInputAsHandled();
        return true;
    }
}
