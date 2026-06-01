namespace DevMode.Settings;

internal static class HotkeyActionId {
    internal const string ToggleRail = "toggleRail";
    internal const string ClosePanel = "closePanel";
    internal const string NextTab = "nextTab";
    internal const string PrevTab = "prevTab";
    internal const string LockRail = "lockRail";

    internal static readonly string[] All = {
        ToggleRail, ClosePanel, NextTab, PrevTab, LockRail
    };
}
