using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

internal static partial class DevPanelUI {
    /// <summary>
    /// Standard rail-spliced browser shell: pins the rail, joins it to the panel edge, full-screen root,
    /// optional click-outside backdrop, and a <see cref="CreateBrowserPanel(float)"/> instance.
    /// <see cref="Control.TreeExiting"/> restores rail splice and unpin.
    /// </summary>
    /// <param name="panelWidth">
    /// 0 = full-width browser panel. &gt; 0 = fixed width; backdrop is added automatically.
    /// </param>
    /// <param name="backdropWhenFullWidth">
    /// When <paramref name="panelWidth"/> is 0, set true to still add the backdrop (card browser / encounter picker).
    /// </param>
    internal static (Control Root, PanelContainer Panel, VBoxContainer Content) CreateBrowserOverlayShell(
        NGlobalUi globalUi,
        string rootName,
        float panelWidth,
        Action onClose,
        int contentSeparation = 10,
        int zIndex = 1250,
        bool backdropWhenFullWidth = false) {
        PinRail();
        SpliceRail(globalUi, joined: true);

        var root = new Control { Name = rootName, MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = zIndex };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.TreeExiting += () => {
            UnpinRail();
            SpliceRail(globalUi, joined: false);
        };

        if (panelWidth > 0f || backdropWhenFullWidth)
            root.AddChild(CreateBrowserBackdrop(onClose));

        var panel = CreateBrowserPanel(panelWidth);
        root.AddChild(panel);

        var content = panel.GetNode<VBoxContainer>("Content");
        content.AddThemeConstantOverride("separation", contentSeparation);

        return (root, panel, content);
    }

    /// <summary>
    /// Same as <see cref="CreateBrowserOverlayShell(NGlobalUi, string, float, Action, int, int, bool)"/>,
    /// but uses a custom <see cref="PanelContainer"/> (e.g. card/relic browsers with different margins).
    /// </summary>
    internal static (Control Root, PanelContainer Panel, VBoxContainer Content) CreateBrowserOverlayShell(
        NGlobalUi globalUi,
        string rootName,
        PanelContainer panel,
        Action onClose,
        int contentSeparation,
        bool addBackdrop = true,
        int zIndex = 1250) {
        PinRail();
        SpliceRail(globalUi, joined: true);

        var root = new Control { Name = rootName, MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = zIndex };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.TreeExiting += () => {
            UnpinRail();
            SpliceRail(globalUi, joined: false);
        };

        if (addBackdrop)
            root.AddChild(CreateBrowserBackdrop(onClose));

        root.AddChild(panel);

        var content = panel.GetNode<VBoxContainer>("Content");
        content.AddThemeConstantOverride("separation", contentSeparation);

        return (root, panel, content);
    }
}
