using System;
using System.Collections.Generic;
using System.Linq;
using DevMode.Actions;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

/// <summary>Event picker — spliced to the DevMode rail, matching card / relic browser layout.</summary>
internal static class EventSelectUI {
    private const string RootName = "DevModeEventSelect";
    private const float PanelW = 520f;

    public static void Show(NGlobalUi globalUi, Action<EventModel> onSelected) {
        Remove(globalUi);

        var (root, _, vbox) = DevPanelUI.CreateBrowserOverlayShell(
            globalUi, RootName, PanelW, () => Remove(globalUi));

        BuildNavTab(vbox, I18N.T("event.nav.title", "Events"));

        var (searchRow, search) = DevPanelUI.CreateSearchRow(I18N.T("event.search", "Search events..."));
        vbox.AddChild(searchRow);

        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 3);
        scroll.AddChild(list);
        vbox.AddChild(scroll);

        var statusLabel = BuildStatusLabel();
        vbox.AddChild(statusLabel);

        var allEvents = EventActions.GetAllEvents().OrderBy(e => EventActions.GetEventDisplayName(e)).ToList();

        void Rebuild(string filter) {
            foreach (var child in list.GetChildren()) ((Node)child).QueueFree();
            var filtered = string.IsNullOrWhiteSpace(filter)
                ? allEvents
                : allEvents.Where(e => EventActions.GetEventDisplayName(e).Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var evt in filtered) {
                var btn = DevPanelUI.CreateListItemButton(EventActions.GetEventDisplayName(evt));
                btn.Pressed += () => {
                    onSelected(evt);
                    statusLabel.Text = I18N.T("event.triggered", "Triggered: {0}", EventActions.GetEventDisplayName(evt));
                };
                list.AddChild(btn);
            }
            statusLabel.Text = I18N.T("event.count", "{0} events", filtered.Count);
        }

        search.TextChanged += Rebuild;
        Rebuild("");

        ((Node)globalUi).AddChild(root);
        search.GrabFocus();
    }

    public static void Remove(NGlobalUi globalUi) {
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
    }

    private static void BuildNavTab(VBoxContainer vbox, string title) {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);
        var tab = new Button { Text = title, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 32) };
        var flat = new StyleBoxFlat { BgColor = Colors.Transparent, ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 4, ContentMarginBottom = 6 };
        foreach (var s in new[] { "normal", "hover", "pressed", "focus" }) tab.AddThemeStyleboxOverride(s, flat);
        tab.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        tab.AddThemeFontSizeOverride("font_size", 13);
        row.AddChild(tab);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        vbox.AddChild(row);
        vbox.AddChild(new ColorRect { CustomMinimumSize = new Vector2(0, 1), Color = DevModeTheme.Separator, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
    }

    private static Label BuildStatusLabel() {
        var lbl = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Center };
        lbl.AddThemeFontSizeOverride("font_size", 11);
        lbl.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        return lbl;
    }
}
