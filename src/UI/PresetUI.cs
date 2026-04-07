using System;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Presets;

namespace DevMode.UI;

/// <summary>Preset manager — spliced to the DevMode rail, matching card / relic browser layout.</summary>
internal static class PresetUI
{
    private const string RootName = "DevModePresets";
    private const float  PanelW   = 560f;

    public static void Show(NGlobalUi globalUi)
    {
        Remove(globalUi);

        DevPanelUI.PinRail();
        DevPanelUI.SpliceRail(globalUi, joined: true);

        var root = new Control { Name = RootName, MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 1250 };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.TreeExiting += () =>
        {
            DevPanelUI.UnpinRail();
            DevPanelUI.SpliceRail(globalUi, joined: false);
        };

        root.AddChild(DevPanelUI.CreateBrowserBackdrop(() => Remove(globalUi)));
        var panel = DevPanelUI.CreateBrowserPanel(PanelW);
        root.AddChild(panel);

        var vbox = panel.GetNode<VBoxContainer>("Content");
        vbox.AddThemeConstantOverride("separation", 10);

        // ── Nav tab ──
        BuildNavTab(vbox);

        // ── Save row ──
        var saveRow = new HBoxContainer();
        saveRow.AddThemeConstantOverride("separation", 6);

        var nameInput = new LineEdit
        {
            PlaceholderText = I18N.T("preset.name", "Preset name..."),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        saveRow.AddChild(nameInput);

        var saveBtn = DevPanelUI.CreateListItemButton(I18N.T("preset.save", "Save"));
        saveBtn.Alignment = HorizontalAlignment.Center;
        saveBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        saveBtn.CustomMinimumSize = new Vector2(80, 34);
        saveRow.AddChild(saveBtn);
        vbox.AddChild(saveRow);

        // ── Import ──
        var importBtn = DevPanelUI.CreateListItemButton(I18N.T("preset.import", "Import from Clipboard"));
        importBtn.Alignment = HorizontalAlignment.Center;
        vbox.AddChild(importBtn);

        vbox.AddChild(DevPanelUI.CreateSectionHeader(I18N.T("preset.savedPresets", "Saved Presets")));

        // ── Preset list ──
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(list);
        vbox.AddChild(scroll);

        // ── Status ──
        var statusLabel = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Center };
        statusLabel.AddThemeFontSizeOverride("font_size", 11);
        statusLabel.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        vbox.AddChild(statusLabel);

        void RebuildList()
        {
            foreach (var child in list.GetChildren()) ((Node)child).QueueFree();

            var all = PresetManager.Loadouts.All.OrderBy(k => k.Key).ToList();
            if (all.Count == 0)
            {
                var empty = new Label { Text = I18N.T("preset.empty", "No saved presets."), HorizontalAlignment = HorizontalAlignment.Center };
                empty.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
                list.AddChild(empty);
                return;
            }

            foreach (var kvp in all)
            {
                var row = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                var rowStyle = new StyleBoxFlat
                {
                    BgColor = new Color(1f, 1f, 1f, 0.04f),
                    CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                    CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
                    ContentMarginLeft = 10, ContentMarginRight = 6,
                    ContentMarginTop = 6, ContentMarginBottom = 6,
                    BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                    BorderColor = new Color(1f, 1f, 1f, 0.05f)
                };
                row.AddThemeStyleboxOverride("panel", rowStyle);

                var inner = new HBoxContainer();
                inner.AddThemeConstantOverride("separation", 6);

                var nameLabel = new Label
                {
                    Text = kvp.Key,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    VerticalAlignment = VerticalAlignment.Center,
                    ClipText = true
                };
                nameLabel.AddThemeFontSizeOverride("font_size", 13);
                nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f));
                inner.AddChild(nameLabel);

                var stats = new Label
                {
                    Text = $"{kvp.Value.Cards.Sum(c => c.Count)}c  {kvp.Value.Relics.Count}r  {kvp.Value.Gold}g",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    CustomMinimumSize = new Vector2(90, 0)
                };
                stats.AddThemeFontSizeOverride("font_size", 11);
                stats.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
                inner.AddChild(stats);

                var name = kvp.Key;
                var preset = kvp.Value;

                inner.AddChild(MakeSmallBtn(I18N.T("preset.load", "Load"),   new Color(0.22f, 0.48f, 0.32f, 0.90f), async () =>
                {
                    await PresetManager.ApplyToRunAsync(preset);
                    statusLabel.Text = I18N.T("preset.applied", "Preset applied: {0}", name);
                }));
                inner.AddChild(MakeSmallBtn(I18N.T("preset.export", "Export"), new Color(0.22f, 0.30f, 0.45f, 0.90f), () =>
                {
                    PresetManager.ExportToClipboard(name, preset);
                    statusLabel.Text = I18N.T("preset.exported", "Exported to clipboard: {0}", name);
                }));
                inner.AddChild(MakeSmallBtn(I18N.T("preset.delete", "Del"), new Color(0.45f, 0.18f, 0.18f, 0.90f), () =>
                {
                    PresetManager.Loadouts.Delete(name);
                    statusLabel.Text = I18N.T("preset.deleted", "Deleted: {0}", name);
                    RebuildList();
                }));

                row.AddChild(inner);
                list.AddChild(row);
            }
        }

        saveBtn.Pressed += () =>
        {
            var n = nameInput.Text?.Trim();
            if (string.IsNullOrEmpty(n)) { statusLabel.Text = I18N.T("preset.error.noName", "Enter a name first."); return; }
            var p = PresetManager.CaptureFromRun();
            if (p == null) { statusLabel.Text = I18N.T("preset.error.noRun", "No active run."); return; }
            PresetManager.Loadouts.Set(n, p);
            statusLabel.Text = I18N.T("preset.saved", "Saved: {0}", n);
            nameInput.Text = "";
            RebuildList();
        };

        importBtn.Pressed += () =>
        {
            var (n, p) = PresetManager.ImportFromClipboard();
            if (n == null || p == null) { statusLabel.Text = I18N.T("preset.error.import", "Invalid clipboard data."); return; }
            PresetManager.Loadouts.Set(n, p);
            statusLabel.Text = I18N.T("preset.imported", "Imported: {0}", n);
            RebuildList();
        };

        RebuildList();
        ((Node)globalUi).AddChild(root);
    }

    public static void Remove(NGlobalUi globalUi)
    {
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
    }

    private static void BuildNavTab(VBoxContainer vbox)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);
        var tab = new Button { Text = I18N.T("preset.title", "Preset Manager"), FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 32) };
        var flat = new StyleBoxFlat { BgColor = Colors.Transparent, ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 4, ContentMarginBottom = 6 };
        foreach (var s in new[] { "normal", "hover", "pressed", "focus" }) tab.AddThemeStyleboxOverride(s, flat);
        tab.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        tab.AddThemeFontSizeOverride("font_size", 13);
        row.AddChild(tab);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        vbox.AddChild(row);
        vbox.AddChild(new ColorRect { CustomMinimumSize = new Vector2(0, 1), Color = new Color(1f, 1f, 1f, 0.06f), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
    }

    private static Button MakeSmallBtn(string text, Color bg, Action onPress)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(52, 28), FocusMode = Control.FocusModeEnum.None };
        StyleBoxFlat MakeStyle(Color c) => new()
        {
            BgColor = c,
            CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5,
            ContentMarginLeft = 6, ContentMarginRight = 6,
            ContentMarginTop = 2, ContentMarginBottom = 2
        };
        btn.AddThemeStyleboxOverride("normal",  MakeStyle(bg));
        btn.AddThemeStyleboxOverride("hover",   MakeStyle(bg.Lightened(0.15f)));
        btn.AddThemeStyleboxOverride("pressed", MakeStyle(bg.Lightened(0.20f)));
        btn.AddThemeStyleboxOverride("focus",   MakeStyle(bg));
        btn.AddThemeColorOverride("font_color", new Color(0.88f, 0.88f, 0.92f));
        btn.AddThemeFontSizeOverride("font_size", 12);
        btn.Pressed += onPress;
        return btn;
    }
}
