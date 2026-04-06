using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

/// <summary>Full-screen overlay for executing official console commands.</summary>
internal static class ConsoleUI
{
    private const string RootName = "DevModeConsole";
    private static readonly ConsoleBridge _bridge = new();
    private static readonly List<string> _history = new();

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

        vbox.AddChild(new Label { Text = I18N.T("console.title", "Console"), HorizontalAlignment = HorizontalAlignment.Center });

        // Output area
        var outputScroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var outputLabel = new RichTextLabel { BbcodeEnabled = true, SizeFlagsVertical = Control.SizeFlags.ExpandFill, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FitContent = true, ScrollFollowing = true };
        outputScroll.AddChild(outputLabel);
        vbox.AddChild(outputScroll);

        // Restore history
        foreach (var line in _history)
            outputLabel.AppendText(line + "\n");

        // Input row
        var inputRow = new HBoxContainer();
        inputRow.AddThemeConstantOverride("separation", 4);
        var input = new LineEdit { PlaceholderText = I18N.T("console.input", "Enter command..."), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClearButtonEnabled = true };
        var runBtn = new Button { Text = I18N.T("console.run", "Run"), CustomMinimumSize = new Vector2(60, 28) };

        void Execute()
        {
            var cmd = input.Text?.Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            string line;
            if (_bridge.TryExecute(cmd, out var msg, out var success))
                line = $"[color={(success ? "green" : "yellow")}]> {cmd}[/color]\n  {msg}";
            else
                line = $"[color=red]> {cmd}[/color]\n  {msg}";

            _history.Add(line);
            outputLabel.AppendText(line + "\n");
            input.Text = "";
        }

        runBtn.Pressed += Execute;
        input.TextSubmitted += _ => Execute();

        inputRow.AddChild(input);
        inputRow.AddChild(runBtn);
        vbox.AddChild(inputRow);

        // Command list button
        var listBtn = new Button { Text = I18N.T("console.listCommands", "List Commands") };
        listBtn.Pressed += () =>
        {
            if (_bridge.TryGetCommands(out var commands, out var error))
            {
                var text = string.Join("\n", commands.Select(c => $"  {c.Name} — {c.Description}"));
                var line = $"[color=cyan]{I18N.T("console.availableCommands", "Available commands:")}[/color]\n{text}";
                _history.Add(line);
                outputLabel.AppendText(line + "\n");
            }
            else
            {
                outputLabel.AppendText($"[color=red]{error}[/color]\n");
            }
        };
        vbox.AddChild(listBtn);

        ((Node)globalUi).AddChild(root);
        input.GrabFocus();
    }

    public static void Remove(NGlobalUi globalUi)
    {
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
    }
}
