using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

/// <summary>
/// Public API for external mods: rail-spliced browser panels and helpers.
/// Main-menu modal UI is internal to DevMode (<see cref="DevPanelUI"/>).
/// </summary>
public static class DevPanelModApi {
    /// <inheritdoc cref="DevPanelUI.CreateBrowserPanel(float)" />
    public static PanelContainer CreateBrowserPanel(float fixedWidth = 560f) {
        return DevPanelUI.CreateBrowserPanel(fixedWidth);
    }

    /// <inheritdoc cref="DevPanelUI.CreateBrowserBackdrop(Action)" />
    public static ColorRect CreateBrowserBackdrop(Action onClose) {
        return DevPanelUI.CreateBrowserBackdrop(onClose);
    }

    /// <inheritdoc cref="DevPanelUI.PinRail" />
    public static void PinRail() => DevPanelUI.PinRail();

    /// <inheritdoc cref="DevPanelUI.UnpinRail" />
    public static void UnpinRail() => DevPanelUI.UnpinRail();

    /// <inheritdoc cref="DevPanelUI.SpliceRail(NGlobalUi, bool)" />
    public static void SpliceRail(NGlobalUi globalUi, bool joined) =>
        DevPanelUI.SpliceRail(globalUi, joined);

    /// <inheritdoc cref="DevPanelUI.CreateBrowserOverlayShell(NGlobalUi, string, float, Action, int, int, bool)" />
    public static (Control Root, PanelContainer Panel, VBoxContainer Content) CreateBrowserOverlayShell(
        NGlobalUi globalUi,
        string rootName,
        float panelWidth,
        Action onClose,
        int contentSeparation = 10,
        int zIndex = 1250,
        bool backdropWhenFullWidth = false) =>
        DevPanelUI.CreateBrowserOverlayShell(
            globalUi, rootName, panelWidth, onClose, contentSeparation, zIndex, backdropWhenFullWidth);

    /// <inheritdoc cref="DevPanelUI.CreateBrowserOverlayShell(NGlobalUi, string, PanelContainer, Action, int, bool, int)" />
    public static (Control Root, PanelContainer Panel, VBoxContainer Content) CreateBrowserOverlayShell(
        NGlobalUi globalUi,
        string rootName,
        PanelContainer panel,
        Action onClose,
        int contentSeparation,
        bool addBackdrop = true,
        int zIndex = 1250) =>
        DevPanelUI.CreateBrowserOverlayShell(
            globalUi, rootName, panel, onClose, contentSeparation, addBackdrop, zIndex);
}
