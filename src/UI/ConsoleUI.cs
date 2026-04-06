using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

/// <summary>Command reference manual showing all native and DevMode console commands.</summary>
internal static class ConsoleUI
{
    private const string RootName = "DevModeConsole";
    private static readonly ConsoleBridge _bridge = new();

    public static void Show(NGlobalUi globalUi)
    {
        Remove(globalUi);

        var root = new Control { Name = RootName, MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 1300 };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        root.AddChild(DevPanelUI.CreateStandardBackdrop(() => Remove(globalUi)));

        var panel = DevPanelUI.CreateStandardPanel();
        panel.MouseFilter = Control.MouseFilterEnum.Stop;
        root.AddChild(panel);

        var vbox = panel.GetNode<VBoxContainer>("Content");
        vbox.AddThemeConstantOverride("separation", 6);

        vbox.AddChild(new Label
        {
            Text = I18N.T("console.title", "Command Reference"),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // Search filter
        var searchRow = new HBoxContainer();
        searchRow.AddThemeConstantOverride("separation", 4);
        var searchInput = new LineEdit
        {
            PlaceholderText = I18N.T("console.search", "Filter commands..."),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ClearButtonEnabled = true
        };
        searchRow.AddChild(searchInput);
        vbox.AddChild(searchRow);

        // Scrollable command list
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        var listBox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        listBox.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(listBox);
        vbox.AddChild(scroll);

        // Copy hint
        vbox.AddChild(new Label
        {
            Text = I18N.T("console.copyHint", "Click a command to copy to clipboard"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(1, 1, 1, 0.5f)
        });

        // Populate and wire search
        PopulateCommands(listBox, "");
        searchInput.TextChanged += filter => PopulateCommands(listBox, filter);

        ((Node)globalUi).AddChild(root);
        searchInput.GrabFocus();
    }

    public static void Remove(NGlobalUi globalUi)
    {
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
    }

    private static void PopulateCommands(VBoxContainer listBox, string filter)
    {
        // Clear existing
        foreach (var child in listBox.GetChildren())
        {
            if (child is Node n) n.QueueFree();
        }

        if (!_bridge.TryGetCommands(out var commands, out _))
            return;

        var filtered = string.IsNullOrWhiteSpace(filter)
            ? commands
            : commands.Where(c =>
                c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        var native = filtered.Where(c => c.IsOfficial).ToList();
        var devmode = filtered.Where(c => !c.IsOfficial).ToList();

        // Native section
        if (native.Count > 0)
        {
            AddSectionHeader(listBox, I18N.T("console.section.native", "Native Commands"), native.Count);
            foreach (var cmd in native)
                AddCommandEntry(listBox, cmd);
        }

        // DevMode section
        if (devmode.Count > 0)
        {
            AddSectionHeader(listBox, I18N.T("console.section.devmode", "DevMode Commands"), devmode.Count);
            foreach (var cmd in devmode)
                AddCommandEntry(listBox, cmd);
        }

        if (native.Count == 0 && devmode.Count == 0)
        {
            listBox.AddChild(new Label
            {
                Text = I18N.T("console.noResults", "No commands found."),
                HorizontalAlignment = HorizontalAlignment.Center,
                Modulate = new Color(1, 1, 1, 0.5f)
            });
        }
    }

    private static void AddSectionHeader(VBoxContainer parent, string title, int count)
    {
        var header = new Label
        {
            Text = $"── {title} ({count}) ──",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.4f, 0.8f, 1f)
        };
        parent.AddChild(header);
    }

    private static void AddCommandEntry(VBoxContainer parent, ConsoleBridge.CommandInfo cmd)
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 0);

        // Command name + args (clickable, copies to clipboard)
        var nameBtn = new Button
        {
            Text = string.IsNullOrWhiteSpace(cmd.Args) ? cmd.Name : $"{cmd.Name} {cmd.Args}",
            Alignment = HorizontalAlignment.Left,
            Flat = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ClipText = false
        };
        nameBtn.AddThemeColorOverride("font_color", new Color(0.6f, 1f, 0.6f));
        nameBtn.Pressed += () => DisplayServer.ClipboardSet(cmd.Name);
        container.AddChild(nameBtn);

        // Description (wrapping label)
        if (!string.IsNullOrWhiteSpace(cmd.Description))
        {
            var descLabel = new Label
            {
                Text = $"  {cmd.Description}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Modulate = new Color(1, 1, 1, 0.7f)
            };
            container.AddChild(descLabel);
        }

        // Separator
        container.AddChild(new HSeparator { Modulate = new Color(1, 1, 1, 0.15f) });

        parent.AddChild(container);
    }

    // FormatEntry is no longer needed — layout is split into name+args and description
}
