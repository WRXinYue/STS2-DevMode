using Godot;
using KitLib;
using KitLib.Settings;
using KitLib.UI;

namespace KitLib.Integration;

internal static class KitLibNativeModSettingsUi {
    internal static Control CreateBoolToggle(string title, string? description, Func<bool> get, Action<bool> set) {
        var cb = new CheckBox {
            ButtonPressed = get(),
            FocusMode = Control.FocusModeEnum.All,
        };
        DevModeFormChrome.ApplyToggle(cb);
        cb.Toggled += on => set(on);
        return DevModeFormChrome.CreateLabeledValueRow(title, description, cb);
    }

    internal static Control CreateNormalRunModeRow() {
        var ob = new OptionButton {
            FocusMode = Control.FocusModeEnum.All,
            CustomMinimumSize = new Vector2(DevModeFormChrome.Metrics.ChoiceRowMinWidth,
                DevModeFormChrome.Metrics.ValueColumnMinHeight),
        };
        DevModeFormChrome.ApplyOptionButton(ob);
        ob.AddItem(I18N.T("modpanel.kitlib.normalRun.disabled", "Normal run: disabled"), (int)NormalRunMode.Disabled);
        ob.AddItem(I18N.T("modpanel.kitlib.normalRun.devPanel", "Normal run: Dev panel"), (int)NormalRunMode.DevPanel);
        ob.AddItem(I18N.T("modpanel.kitlib.normalRun.cheat", "Normal run: cheat tools"), (int)NormalRunMode.Cheat);
        ob.Selected = (int)KitLibState.NormalRunMode;
        ob.ItemSelected += idx => SettingsStore.SetNormalRunMode((NormalRunMode)(int)idx);
        return DevModeFormChrome.CreateLabeledValueRow(
            I18N.T("modpanel.kitlib.normalRun.title", "In-run DevMode level"),
            I18N.T("modpanel.kitlib.normalRun.desc",
                "Controls whether the DevPanel rail and cheat tools are available during normal (non test) runs."),
            ob);
    }
}
