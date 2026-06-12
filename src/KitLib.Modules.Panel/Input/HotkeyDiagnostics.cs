using Godot;
using KitLib;
using KitLib.DevPerf;
using KitLib.Host;
using KitLib.Settings;
using KitLib.UI;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace KitLib.Hotkeys;

/// <summary>Rate-limited hotkey pipeline logging for in-game diagnosis.</summary>
internal static class HotkeyDiagnostics {
    const string Scope = "Hotkey";
    const double ProbeIntervalSeconds = 1.5;

    static bool _snapshotLogged;
    static double _lastProbeLogMs;
    static bool _firstProbeKeyLogged;

    internal static void LogListenersReady(string source) {
        KitLog.Info(Scope,
            $"Listener ready ({source}): panelLoaded={ModuleCatalog.IsLoaded(ModuleIds.Panel)}, " +
            $"inputManager={NInputManager.Instance != null}, " +
            $"hotkeysEnabled={SettingsStore.Current.HotkeysEnabled}, " +
            $"devActive={KitLibState.IsActive}, railAttached={DevPanelUI.IsRailAttached}, " +
            $"processNode={KitLib.KitLibProcessNode.Instance != null}, " +
            $"rootServices={KitLibRootServices.Instance != null}");
        LogBindingSnapshot();
    }

    internal static void LogBindingSnapshot() {
        if (_snapshotLogged)
            return;
        _snapshotLogged = true;

        var s = SettingsStore.Current;
        KitLog.Info(Scope,
            "Bindings: " +
            $"perf={s.HotkeyTogglePerfHud.FormatLabel()}, " +
            $"rail={s.HotkeyToggleRail.FormatLabel()}, " +
            $"save={s.HotkeyQuickSave.FormatLabel()}, " +
            $"load={s.HotkeyQuickLoad.FormatLabel()}, " +
            $"close={s.HotkeyClosePanel.FormatLabel()}");
    }

    internal static void LogKeyReceived(string pipeline, InputEventKey key) {
        if (!key.Pressed || key.Echo)
            return;
        if (!IsProbeKey(key))
            return;

        if (!_firstProbeKeyLogged) {
            _firstProbeKeyLogged = true;
            KitLog.Info(Scope, $"First probe key [{pipeline}]: {DescribeKey(key)}");
            return;
        }

        double now = Time.GetTicksMsec();
        if (now - _lastProbeLogMs < ProbeIntervalSeconds * 1000)
            return;
        _lastProbeLogMs = now;

        KitLog.Info(Scope,
            $"Key received [{pipeline}]: {DescribeKey(key)}");
    }

    internal static void LogNearMatch(string handler, HotkeyBinding expected, InputEventKey key) {
        if (key.Keycode != expected.Keycode)
            return;
        KitLog.Info(Scope,
            $"{handler}: keycode matches {expected.FormatLabel()} but modifiers differ " +
            $"(got {DescribeKey(key)})");
    }

    internal static void LogHandled(string handler, string action) =>
        KitLog.Info(Scope, $"{handler}: triggered {action}");

    internal static void LogBlocked(string handler, string reason) =>
        KitLog.Info(Scope, $"{handler}: blocked — {reason}");

    static bool IsProbeKey(InputEventKey key) =>
        key.CtrlPressed || key.ShiftPressed || key.AltPressed
        || key.Keycode is >= Key.F1 and <= Key.F12;

    static string DescribeKey(InputEventKey key) => HotkeyBinding.From(key).FormatLabel();
}
