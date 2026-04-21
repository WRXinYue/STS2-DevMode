using DevMode.Icons;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

/// <summary>
/// Implement this interface to add a custom tab to the DevMode rail.
/// Register via <see cref="DevPanelRegistry.Register(IDevPanelTab)"/>.
/// </summary>
public interface IDevPanelTab {
    /// <summary>Unique identifier, e.g. "mymod.debug".</summary>
    string Id { get; }

    /// <summary>Icon shown in the rail.</summary>
    MdiIcon Icon { get; }

    /// <summary>Tooltip text for the rail button.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Sort order within its group. Lower values appear higher in the rail.
    /// Built-in tabs use 100, 200, 300 … — leave gaps for insertion.
    /// </summary>
    int Order { get; }

    /// <summary>Which section of the rail this tab belongs to.</summary>
    DevPanelTabGroup Group { get; }

    /// <inheritdoc cref="DevPanelTabKind" />
    DevPanelTabKind Kind => DevPanelTabKind.Cheat;

    /// <summary>Called when the user clicks this tab's icon.</summary>
    void OnActivate(NGlobalUi globalUi);

    /// <summary>Called when the panel is being closed or switched away. Override for cleanup.</summary>
    void OnDeactivate(NGlobalUi globalUi) { }
}

/// <summary>Determines where the tab icon is placed in the rail.</summary>
public enum DevPanelTabGroup {
    /// <summary>Upper section — primary feature panels (Cards, Relics, …).</summary>
    Primary,

    /// <summary>Lower section — utility panels (Save, Settings, AI, …).</summary>
    Utility
}

/// <summary>
/// Rail tab capability. In normal runs with persist dev only (no cheat mode), only <see cref="Developer"/> tabs appear.
/// Full dev runs and normal runs with cheat mode show every tab.
/// </summary>
public enum DevPanelTabKind {
    /// <summary>Inspection and tooling that do not change run or save data (logs, Harmony report, framework bridge).</summary>
    Developer,

    /// <summary>May alter run state, saves, or combat (browsers with grant modes, console, cheats overlay, …).</summary>
    Cheat
}
