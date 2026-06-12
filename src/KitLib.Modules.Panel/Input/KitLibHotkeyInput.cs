using Godot;

namespace KitLib.Hotkeys;

/// <summary>Runtime KitLib hotkeys via <c>_UnhandledInput</c>, same phase as official <c>NHotkeyManager</c>.</summary>
internal static class KitLibHotkeyInput {
    internal static void TryHandlePanelAndRun(InputEvent @event, Viewport viewport) {
        if (DevPanelHotkeys.TryHandle(@event, viewport))
            return;
        QuickSlHotkeys.TryHandle(@event, viewport);
    }

    internal static void TryHandleDevPerf(InputEvent @event, Viewport viewport) =>
        DevPerfHotkeys.TryHandle(@event, viewport);
}
