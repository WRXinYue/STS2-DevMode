using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;

namespace DevMode.UI;

internal static partial class EnemySelectUI {
    internal sealed class MainBrowserState {
        public required NGlobalUi GlobalUi;
        public required VBoxContainer ContentHost;
        public RoomType? EncounterFilter;
        public Label StatusLabel = null!;
    }

    public static void ShowMain(NGlobalUi globalUi) {
        Hide(globalUi);

        var (root, _, vbox) = DevPanelUI.CreateBrowserOverlayShell(
            globalUi, RootName, 0f, () => Hide(globalUi), contentSeparation: 8, backdropWhenFullWidth: true);

        var state = new MainBrowserState {
            GlobalUi = globalUi,
            ContentHost = new VBoxContainer {
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            },
            EncounterFilter = null,
        };
        state.ContentHost.AddThemeConstantOverride("separation", 8);

        BuildMainNav(vbox);
        vbox.AddChild(DevPanelUI.CreateOverlaySeparator());
        vbox.AddChild(state.ContentHost);

        state.StatusLabel = new Label { Text = "" };
        state.StatusLabel.AddThemeFontSizeOverride("font_size", 11);
        state.StatusLabel.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        state.StatusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(state.StatusLabel);

        SwitchMainView(state);
        ((Node)globalUi).AddChild(root);
    }

    private static void BuildMainNav(VBoxContainer vbox) {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var title = new Label {
            Text = I18N.T("panel.enemies", "Enemies"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        row.AddChild(title);

        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        vbox.AddChild(row);
    }

    internal static void SwitchMainView(MainBrowserState state) {
        foreach (var child in state.ContentHost.GetChildren())
            ((Node)child).QueueFree();

        BuildMapTab(state);
        state.StatusLabel.Text = I18N.T(
            "enemy.mapHint",
            "Click combat nodes on the map to edit. Run rules apply to this run only.");
    }
}
