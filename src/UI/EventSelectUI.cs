using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Actions;

namespace DevMode.UI;

/// <summary>Full-screen overlay for selecting an Event from ModelDb.</summary>
internal static class EventSelectUI
{
    private const string RootName = "DevModeEventSelect";

    public static void Show(NGlobalUi globalUi, Action<EventModel> onSelected)
    {
        Remove(globalUi);

        var root = new Control { Name = RootName, MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 1300 };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var backdrop = new ColorRect { Color = new Color(0, 0, 0, 0.7f), MouseFilter = Control.MouseFilterEnum.Stop };
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        backdrop.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true }) Remove(globalUi); };
        root.AddChild(backdrop);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        panel.OffsetLeft = -280; panel.OffsetRight = 280;
        panel.OffsetTop = -250; panel.OffsetBottom = 250;
        var style = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.12f, 0.97f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8, ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 12, ContentMarginBottom = 12 };
        panel.AddThemeStyleboxOverride("panel", style);
        panel.MouseFilter = Control.MouseFilterEnum.Stop;
        root.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        panel.AddChild(vbox);

        vbox.AddChild(new Label { Text = I18N.T("event.select.title", "Select Event"), HorizontalAlignment = HorizontalAlignment.Center });

        var search = new LineEdit { PlaceholderText = I18N.T("event.search", "Search..."), ClearButtonEnabled = true };
        vbox.AddChild(search);

        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(list);
        vbox.AddChild(scroll);

        var allEvents = EventActions.GetAllEvents().OrderBy(e => EventActions.GetEventDisplayName(e)).ToList();

        void Rebuild(string filter)
        {
            foreach (var child in list.GetChildren()) ((Node)child).QueueFree();
            var filtered = string.IsNullOrWhiteSpace(filter)
                ? allEvents
                : allEvents.Where(e => EventActions.GetEventDisplayName(e).Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var evt in filtered)
            {
                var btn = new Button { Text = EventActions.GetEventDisplayName(evt), CustomMinimumSize = new Vector2(0, 30) };
                btn.Pressed += () => onSelected(evt);
                list.AddChild(btn);
            }
        }

        search.TextChanged += Rebuild;
        Rebuild("");

        ((Node)globalUi).AddChild(root);
    }

    public static void Remove(NGlobalUi globalUi)
    {
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
    }
}
