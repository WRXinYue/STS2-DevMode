namespace DevMode.Settings;

public sealed partial class DevModeSettings {
    public HotkeyBinding HotkeyToggleRail { get; set; } = HotkeyDefaults.ToggleRail.Clone();
    public HotkeyBinding HotkeyClosePanel { get; set; } = HotkeyDefaults.ClosePanel.Clone();
    public HotkeyBinding HotkeyNextTab { get; set; } = HotkeyDefaults.NextTab.Clone();
    public HotkeyBinding HotkeyPrevTab { get; set; } = HotkeyDefaults.PrevTab.Clone();
    public HotkeyBinding HotkeyLockRail { get; set; } = HotkeyDefaults.LockRail.Clone();

    internal HotkeyBinding GetHotkey(string actionId) => actionId switch {
        HotkeyActionId.ToggleRail => HotkeyToggleRail,
        HotkeyActionId.ClosePanel => HotkeyClosePanel,
        HotkeyActionId.NextTab => HotkeyNextTab,
        HotkeyActionId.PrevTab => HotkeyPrevTab,
        HotkeyActionId.LockRail => HotkeyLockRail,
        _ => new HotkeyBinding()
    };

    internal void SetHotkey(string actionId, HotkeyBinding binding) {
        var copy = binding.Clone();
        switch (actionId) {
            case HotkeyActionId.ToggleRail: HotkeyToggleRail = copy; break;
            case HotkeyActionId.ClosePanel: HotkeyClosePanel = copy; break;
            case HotkeyActionId.NextTab: HotkeyNextTab = copy; break;
            case HotkeyActionId.PrevTab: HotkeyPrevTab = copy; break;
            case HotkeyActionId.LockRail: HotkeyLockRail = copy; break;
        }
    }
}
