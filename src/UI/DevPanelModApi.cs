using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

/// <summary>
/// Public API for external mods: rail-spliced browser panels and helpers.
/// Main-menu modal UI is internal to DevMode (<see cref="DevPanelUI"/>).
/// </summary>
public static class DevPanelModApi
{
    /// <inheritdoc cref="DevPanelUI.CreateBrowserPanel(float)" />
    public static PanelContainer CreateBrowserPanel(float fixedWidth = 560f)
    {
        return DevPanelUI.CreateBrowserPanel(fixedWidth);
    }

    /// <inheritdoc cref="DevPanelUI.CreateBrowserBackdrop(Action)" />
    public static ColorRect CreateBrowserBackdrop(Action onClose)
    {
        return DevPanelUI.CreateBrowserBackdrop(onClose);
    }

    /// <inheritdoc cref="DevPanelUI.PinRail" />
    public static void PinRail() => DevPanelUI.PinRail();

    /// <inheritdoc cref="DevPanelUI.UnpinRail" />
    public static void UnpinRail() => DevPanelUI.UnpinRail();

    /// <inheritdoc cref="DevPanelUI.SpliceRail(NGlobalUi, bool)" />
    public static void SpliceRail(NGlobalUi globalUi, bool joined) =>
        DevPanelUI.SpliceRail(globalUi, joined);
}
