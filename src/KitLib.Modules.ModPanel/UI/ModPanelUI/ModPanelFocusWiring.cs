using System;
using System.Collections.Generic;
using Godot;

namespace KitLib.UI;

internal static class ModPanelFocusWiring {
    public static void Wire(IReadOnlyList<SidebarModRowVm> rows, string selectedModId,
        Control contentRoot, Control? scopeFocusTarget) {
        var selectedRow = FindRow(rows, selectedModId);
        var contentEntry = FindFirstFocusableDescendant(contentRoot);
        if (selectedRow == null)
            return;
        if (contentEntry != null)
            contentEntry.FocusNeighborLeft = selectedRow.GetPath();
        WireSidebarRowNeighbors(rows, scopeFocusTarget);
        if (scopeFocusTarget != null && rows.Count > 0)
            scopeFocusTarget.FocusNeighborTop = rows[^1].Host.GetPath();
    }

    public static void WireSidebarRowNeighbors(IReadOnlyList<SidebarModRowVm> rows, Control? scopeFocusTarget) {
        for (var i = 0; i < rows.Count; i++) {
            var host = rows[i].Host;
            host.FocusNeighborTop = i > 0 ? rows[i - 1].Host.GetPath() : host.GetPath();
            host.FocusNeighborBottom = i < rows.Count - 1
                ? rows[i + 1].Host.GetPath()
                : scopeFocusTarget?.GetPath() ?? host.GetPath();
        }
    }

    private static Control? FindRow(IReadOnlyList<SidebarModRowVm> rows, string selectedModId) {
        foreach (var row in rows) {
            if (string.Equals(row.Id, selectedModId, StringComparison.OrdinalIgnoreCase))
                return row.Host;
        }
        return rows.Count > 0 ? rows[0].Host : null;
    }

    public static Control? FindFirstFocusableDescendant(Control root) {
        if (root.FocusMode != Control.FocusModeEnum.None && root.Visible)
            return root;
        foreach (var child in root.GetChildren()) {
            if (child is not Control c || !c.Visible)
                continue;
            var found = FindFirstFocusableDescendant(c);
            if (found != null)
                return found;
        }
        return null;
    }
}
