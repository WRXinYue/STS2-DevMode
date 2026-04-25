using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

internal static partial class DevPanelUI {
    /// <summary>
    /// Creates a full-screen browser overlay shell with rail management and optional backdrop.
    /// </summary>
    /// <param name="globalUi">The global UI context used for rail splicing.</param>
    /// <param name="rootName">Unique name for the root control, used for persistence and identification.</param>
    /// <param name="panelWidth">
    /// Desired panel width in pixels. Use <c>0</c> for full-width panel.
    /// Values greater than <c>0</c> will automatically add a click-outside backdrop.
    /// </param>
    /// <param name="onClose">Callback invoked when the backdrop is clicked or the overlay is dismissed.</param>
    /// <param name="contentSeparation">Vertical spacing between content elements in the panel. Default is <c>10</c>.</param>
    /// <param name="zIndex">Rendering order for the root control. Higher values appear on top. Default is <c>1250</c>.</param>
    /// <param name="backdropWhenFullWidth">
    /// When <c>true</c>, adds a backdrop even if <paramref name="panelWidth"/> is <c>0</c>.
    /// Useful for card browsers and encounter pickers that need click-outside behavior.
    /// </param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description><c>Root</c> - The full-screen container control</description></item>
    /// <item><description><c>Panel</c> - The browser panel with width grip attached</description></item>
    /// <item><description><c>Content</c> - The VBoxContainer for adding browser content</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="globalUi"/>, <paramref name="rootName"/>, or <paramref name="onClose"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="panelWidth"/> is negative.
    /// </exception>
    /// <remarks>
    /// <para>This method manages rail pinning and splicing automatically. The rail is pinned
    /// when the overlay is created and unpinned when the root control exits the scene tree.</para>
    /// <para>Panel width is automatically persisted and restored via <see cref="DevModeSettings"/>.
    /// A width grip is attached to the right edge for user resizing.</para>
    /// </remarks>
    internal static (Control Root, PanelContainer Panel, VBoxContainer Content) CreateBrowserOverlayShell(
        NGlobalUi globalUi,
        string rootName,
        float panelWidth,
        Action onClose,
        int contentSeparation = 10,
        int zIndex = 1250,
        bool backdropWhenFullWidth = false) {
        ArgumentNullException.ThrowIfNull(globalUi);
        ArgumentNullException.ThrowIfNull(rootName);
        ArgumentNullException.ThrowIfNull(onClose);

        if (panelWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(panelWidth), "Panel width cannot be negative");

        var root = CreateAndSetupRoot(globalUi, rootName, zIndex);

        float resolved = ResolveBrowserPanelWidth(rootName, panelWidth, (Node)globalUi);
        if (resolved > 0f || backdropWhenFullWidth)
            root.AddChild(CreateBrowserBackdrop(onClose));

        var panel = CreateBrowserPanel(resolved);
        AddPanelToRoot(root, panel, rootName, globalUi);

        var content = GetPanelContent(panel, contentSeparation);

        return (root, panel, content);
    }

    /// <summary>
    /// Creates a browser overlay shell using a pre-configured <see cref="PanelContainer"/>.
    /// </summary>
    /// <param name="globalUi">The global UI context used for rail splicing.</param>
    /// <param name="rootName">Unique name for the root control, used for persistence and identification.</param>
    /// <param name="panel">
    /// A pre-configured <see cref="PanelContainer"/> instance.
    /// Use this overload for custom panels with specific margins (e.g., card or relic browsers).
    /// </param>
    /// <param name="onClose">Callback invoked when the backdrop is clicked or the overlay is dismissed.</param>
    /// <param name="contentSeparation">Vertical spacing between content elements in the panel.</param>
    /// <param name="addBackdrop">
    /// When <c>true</c> (default), adds a click-outside backdrop.
    /// Set to <c>false</c> for overlays that don't need dismissal behavior.
    /// </param>
    /// <param name="zIndex">Rendering order for the root control. Default is <c>1250</c>.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description><c>Root</c> - The full-screen container control</description></item>
    /// <item><description><c>Panel</c> - The custom panel with width settings applied</description></item>
    /// <item><description><c>Content</c> - The VBoxContainer for adding browser content</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="globalUi"/>, <paramref name="rootName"/>, <paramref name="panel"/>, or <paramref name="onClose"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <paramref name="panel"/> does not contain a child named "Content" of type <see cref="VBoxContainer"/>.
    /// </exception>
    /// <remarks>
    /// <para>Unlike the other overload, this method does not create a new panel but uses the provided one.
    /// Width persistence and grip registration are still applied automatically.</para>
    /// <para>The panel should have a direct child named "Content" of type <see cref="VBoxContainer"/>.</para>
    /// </remarks>
    internal static (Control Root, PanelContainer Panel, VBoxContainer Content) CreateBrowserOverlayShell(
        NGlobalUi globalUi,
        string rootName,
        PanelContainer panel,
        Action onClose,
        int contentSeparation,
        bool addBackdrop = true,
        int zIndex = 1250) {
        ArgumentNullException.ThrowIfNull(globalUi);
        ArgumentNullException.ThrowIfNull(rootName);
        ArgumentNullException.ThrowIfNull(panel);
        ArgumentNullException.ThrowIfNull(onClose);

        var root = CreateAndSetupRoot(globalUi, rootName, zIndex);

        if (addBackdrop)
            root.AddChild(CreateBrowserBackdrop(onClose));

        AddPanelToRoot(root, panel, rootName, globalUi);

        var content = GetPanelContent(panel, contentSeparation);

        return (root, panel, content);
    }

    #region Private Helpers

    private static Control CreateAndSetupRoot(NGlobalUi globalUi, string rootName, int zIndex) {
        var root = new Control { Name = rootName, MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = zIndex };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        SetupRailTransition(globalUi, root);
        return root;
    }

    private static void SetupRailTransition(NGlobalUi globalUi, Control root) {
        PinRail();
        SpliceRail(globalUi, joined: true);
        root.TreeExiting += () => {
            UnpinRail();
            SpliceRail(globalUi, joined: false);
        };
    }

    private static void AddPanelToRoot(Control root, PanelContainer panel, string rootName, NGlobalUi globalUi) {
        root.AddChild(panel);
        ApplyInitialBrowserWidthFromSettings((Node)globalUi, panel, rootName);
        RegisterBrowserPanelWidthGrip(root, panel, rootName);
    }

    private static VBoxContainer GetPanelContent(PanelContainer panel, int contentSeparation) {
        var content = panel.GetNodeOrNull<VBoxContainer>("Content")
            ?? throw new InvalidOperationException($"Panel '{panel.Name}' is missing a 'Content' VBoxContainer child");

        content.AddThemeConstantOverride("separation", contentSeparation);
        return content;
    }

    #endregion
}
