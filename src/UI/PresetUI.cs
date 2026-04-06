using System;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Presets;

namespace DevMode.UI;

/// <summary>Full-screen overlay for managing loadout presets.</summary>
internal static class PresetUI
{
    private const string RootName = "DevModePresets";

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

        vbox.AddChild(new Label { Text = I18N.T("preset.title", "Preset Manager"), HorizontalAlignment = HorizontalAlignment.Center });

        // Action buttons row
        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 4);

        // Save name input
        var nameInput = new LineEdit { PlaceholderText = I18N.T("preset.name", "Preset name..."), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        actionRow.AddChild(nameInput);

        var saveBtn = new Button { Text = I18N.T("preset.save", "Save Current"), CustomMinimumSize = new Vector2(100, 28) };
        actionRow.AddChild(saveBtn);
        vbox.AddChild(actionRow);

        // Import/Export row
        var ioRow = new HBoxContainer();
        ioRow.AddThemeConstantOverride("separation", 4);
        var importBtn = new Button { Text = I18N.T("preset.import", "Import (Clipboard)"), CustomMinimumSize = new Vector2(140, 28) };
        var exportLabel = new Label { Text = "", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        ioRow.AddChild(importBtn);
        ioRow.AddChild(exportLabel);
        vbox.AddChild(ioRow);

        // Preset list
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 3);
        scroll.AddChild(list);
        vbox.AddChild(scroll);

        // Status label
        var statusLabel = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Center };
        vbox.AddChild(statusLabel);

        void RebuildList()
        {
            foreach (var child in list.GetChildren()) ((Node)child).QueueFree();

            foreach (var kvp in PresetManager.Loadouts.All.OrderBy(k => k.Key))
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 4);

                var label = new Label { Text = kvp.Key, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                row.AddChild(label);

                var info = new Label
                {
                    Text = $"{kvp.Value.Cards.Sum(c => c.Count)}c {kvp.Value.Relics.Count}r {kvp.Value.Gold}g",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    CustomMinimumSize = new Vector2(100, 0)
                };
                row.AddChild(info);

                var loadBtn = new Button { Text = I18N.T("preset.load", "Load"), CustomMinimumSize = new Vector2(50, 26) };
                var name = kvp.Key;
                var preset = kvp.Value;
                loadBtn.Pressed += async () =>
                {
                    await PresetManager.ApplyToRunAsync(preset);
                    statusLabel.Text = I18N.T("preset.applied", "Preset applied: {0}", name);
                };
                row.AddChild(loadBtn);

                var exportBtn = new Button { Text = I18N.T("preset.export", "Export"), CustomMinimumSize = new Vector2(55, 26) };
                exportBtn.Pressed += () =>
                {
                    PresetManager.ExportToClipboard(name, preset);
                    statusLabel.Text = I18N.T("preset.exported", "Exported to clipboard: {0}", name);
                };
                row.AddChild(exportBtn);

                var delBtn = new Button { Text = I18N.T("preset.delete", "Del"), CustomMinimumSize = new Vector2(40, 26) };
                delBtn.Pressed += () =>
                {
                    PresetManager.Loadouts.Delete(name);
                    statusLabel.Text = I18N.T("preset.deleted", "Deleted: {0}", name);
                    RebuildList();
                };
                row.AddChild(delBtn);

                list.AddChild(row);
            }
        }

        saveBtn.Pressed += () =>
        {
            var name = nameInput.Text?.Trim();
            if (string.IsNullOrEmpty(name)) { statusLabel.Text = I18N.T("preset.error.noName", "Enter a name first."); return; }

            var preset = PresetManager.CaptureFromRun();
            if (preset == null) { statusLabel.Text = I18N.T("preset.error.noRun", "No active run."); return; }

            PresetManager.Loadouts.Set(name, preset);
            statusLabel.Text = I18N.T("preset.saved", "Saved: {0}", name);
            nameInput.Text = "";
            RebuildList();
        };

        importBtn.Pressed += () =>
        {
            var (name, preset) = PresetManager.ImportFromClipboard();
            if (name == null || preset == null) { statusLabel.Text = I18N.T("preset.error.import", "Invalid clipboard data."); return; }

            PresetManager.Loadouts.Set(name, preset);
            statusLabel.Text = I18N.T("preset.imported", "Imported: {0}", name);
            RebuildList();
        };

        RebuildList();
        ((Node)globalUi).AddChild(root);
    }

    public static void Remove(NGlobalUi globalUi)
    {
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
    }
}
