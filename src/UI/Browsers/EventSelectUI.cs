using System;
using System.Collections.Generic;
using System.Linq;
using DevMode.Actions;
using DevMode.Modding;
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

        var allEvents = EventActions.GetAllEvents().OrderBy(e => EventActions.GetEventDisplayName(e)).ToList();
        var activeModSourceFilters = new HashSet<string>(StringComparer.Ordinal);
        var excludedModSourceFilters = new HashSet<string>(StringComparer.Ordinal);

        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 3);
        scroll.AddChild(list);

        var statusLabel = BuildStatusLabel();

        void Rebuild(string filter) {
            foreach (var child in list.GetChildren()) ((Node)child).QueueFree();
            var filtered = allEvents.Where(e => {
                if (!ContentModResolver.MatchesModSourceFilter(
                        ContentModResolver.Resolve(e),
                        activeModSourceFilters,
                        excludedModSourceFilters))
                    return false;
                if (string.IsNullOrWhiteSpace(filter))
                    return true;
                return EventActions.GetEventDisplayName(e)
                    .Contains(filter, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            foreach (var evt in filtered) {
                var displayName = EventActions.GetEventDisplayName(evt);
                var modSource = ContentModResolver.Resolve(evt);
                list.AddChild(CreateEventListRow(displayName, modSource, () => {
                    onSelected(evt);
                    statusLabel.Text = I18N.T("event.triggered", "Triggered: {0}", displayName);
                }));
            }
            statusLabel.Text = I18N.T("event.count", "{0} events", filtered.Count);
        }

        var modSourceRow = BrowserDetailHelpers.TryCreateModSourceFilterRow(
            ContentModResolver.BuildFilterEntries(allEvents.Cast<AbstractModel>()),
            activeModSourceFilters,
            excludedModSourceFilters,
            () => Rebuild(search.Text ?? ""));
        if (modSourceRow != null)
            vbox.AddChild(modSourceRow);

        vbox.AddChild(scroll);
        vbox.AddChild(statusLabel);

        search.TextChanged += Rebuild;
        Rebuild("");

        ((Node)globalUi).AddChild(root);
        search.GrabFocus();
    }

    private static Control CreateEventListRow(string displayName, ContentModSource modSource, Action onPressed) {
        var panel = new PanelContainer {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(0, 44)
        };
        ApplyListItemPanelStyles(panel);

        var margin = new MarginContainer {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 5);
        margin.AddThemeConstantOverride("margin_bottom", 5);

        var col = new VBoxContainer {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        col.AddThemeConstantOverride("separation", 1);

        var nameLabel = new Label {
            Text = displayName,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
        nameLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        col.AddChild(nameLabel);

        var sourceLabel = new Label {
            Text = string.Format(I18N.T("browser.modSource.label", "Source: {0}"), modSource.DisplayLabel),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        sourceLabel.AddThemeFontSizeOverride("font_size", 10);
        sourceLabel.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        sourceLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        col.AddChild(sourceLabel);

        margin.AddChild(col);
        panel.AddChild(margin);
        panel.TooltipText = modSource.ModId ?? modSource.Key;

        panel.GuiInput += evt => {
            if (evt is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                return;
            onPressed();
            panel.AcceptEvent();
        };

        return panel;
    }

    private static void ApplyListItemPanelStyles(PanelContainer panel) {
        StyleBoxFlat MakeStyle(Color bg, Color border) => new() {
            BgColor = bg,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderColor = border
        };

        var accent = DevModeTheme.Accent;
        var bgNormal = DevModeTheme.ButtonBgNormal;
        var borderNormal = new Color(bgNormal.R, bgNormal.G, bgNormal.B, bgNormal.A * 0.8f);
        var borderHover = new Color(accent.R, accent.G, accent.B, 0.30f);

        panel.AddThemeStyleboxOverride("panel", MakeStyle(bgNormal, borderNormal));
        panel.MouseEntered += () =>
            panel.AddThemeStyleboxOverride("panel", MakeStyle(DevModeTheme.ButtonBgHover, borderHover));
        panel.MouseExited += () =>
            panel.AddThemeStyleboxOverride("panel", MakeStyle(bgNormal, borderNormal));
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
        var lbl = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        lbl.AddThemeFontSizeOverride("font_size", 11);
        lbl.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        return lbl;
    }
}
