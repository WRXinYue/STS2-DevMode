using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Actions;

namespace DevMode.UI;

/// <summary>Power picker — spliced to the DevMode rail, matching card / relic browser layout.</summary>
internal static class PowerSelectUI
{
    private const string RootName  = "DevModePowerSelect";
    private const float  PanelW    = 520f;

    public static void Show(NGlobalUi globalUi, Action<PowerModel, int, PowerTarget> onSelected)
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

        // ── Nav tabs (mimic relic/card browser) ──
        BuildNavTabs(vbox);
        vbox.AddChild(new ColorRect
        {
            CustomMinimumSize = new Vector2(0, 1),
            Color = new Color(1f, 1f, 1f, 0.06f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });

        // ── Target row ──
        var targetRow = new HBoxContainer();
        targetRow.AddThemeConstantOverride("separation", 8);
        var targetLbl = new Label
        {
            Text = I18N.T("power.target.label", "Target"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        targetLbl.AddThemeFontSizeOverride("font_size", 12);
        targetLbl.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        targetRow.AddChild(targetLbl);

        var currentTarget = PowerTarget.Self;
        var targetValueLbl = new Label { Text = GetTargetLabel(currentTarget), VerticalAlignment = VerticalAlignment.Center };
        targetValueLbl.AddThemeFontSizeOverride("font_size", 13);
        targetValueLbl.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        targetRow.AddChild(targetValueLbl);

        var cycleBtn = DevPanelUI.CreateFilterChip(I18N.T("power.target.cycle", "Cycle"));
        cycleBtn.ToggleMode = false;
        cycleBtn.Pressed += () =>
        {
            currentTarget = (PowerTarget)(((int)currentTarget + 1) % 4);
            targetValueLbl.Text = GetTargetLabel(currentTarget);
        };
        targetRow.AddChild(cycleBtn);
        vbox.AddChild(targetRow);

        // ── Amount row ──
        var amountRow = new HBoxContainer();
        amountRow.AddThemeConstantOverride("separation", 8);
        var amountLbl = new Label
        {
            Text = I18N.T("power.amount", "Amount"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        amountLbl.AddThemeFontSizeOverride("font_size", 12);
        amountLbl.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        amountRow.AddChild(amountLbl);
        var amountInput = new SpinBox { MinValue = 1, MaxValue = 999, Value = 1, Step = 1, CustomMinimumSize = new Vector2(80, 28) };
        amountRow.AddChild(amountInput);
        vbox.AddChild(amountRow);

        // ── Search ──
        var (searchRow, search) = DevPanelUI.CreateSearchRow(I18N.T("power.search", "Search powers..."));
        vbox.AddChild(searchRow);

        // ── Scroll list ──
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 3);
        scroll.AddChild(list);
        vbox.AddChild(scroll);

        // ── Status ──
        var statusLabel = BuildStatusLabel();
        vbox.AddChild(statusLabel);

        var allPowers = PowerActions.GetAllPowers().OrderBy(p => PowerActions.GetPowerDisplayName(p)).ToList();

        void Rebuild(string filter)
        {
            foreach (var child in list.GetChildren()) ((Node)child).QueueFree();
            var filtered = string.IsNullOrWhiteSpace(filter)
                ? allPowers
                : allPowers.Where(p => PowerActions.GetPowerDisplayName(p).Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var power in filtered)
            {
                var btn = DevPanelUI.CreateListItemButton(PowerActions.GetPowerDisplayName(power));
                btn.Pressed += () =>
                {
                    onSelected(power, (int)amountInput.Value, currentTarget);
                    statusLabel.Text = I18N.T("power.applied", "Applied: {0}", PowerActions.GetPowerDisplayName(power));
                };
                list.AddChild(btn);
            }
            statusLabel.Text = I18N.T("power.count", "{0} powers", filtered.Count);
        }

        search.TextChanged += Rebuild;
        Rebuild("");

        ((Node)globalUi).AddChild(root);
        search.GrabFocus();
    }

    public static void Remove(NGlobalUi globalUi)
    {
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
    }

    private static string GetTargetLabel(PowerTarget t) => t switch
    {
        PowerTarget.Self           => I18N.T("power.target.self",       "Self"),
        PowerTarget.AllEnemies     => I18N.T("power.target.allEnemies", "All Enemies"),
        PowerTarget.SpecificTarget => I18N.T("power.target.specific",   "Specific"),
        PowerTarget.Allies         => I18N.T("power.target.allies",     "Allies"),
        _                          => "?"
    };

    private static void BuildNavTabs(VBoxContainer vbox)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);
        var tab = new Button
        {
            Text = I18N.T("power.nav.title", "Powers"),
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(0, 32)
        };
        var flat = new StyleBoxFlat { BgColor = Colors.Transparent, ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 4, ContentMarginBottom = 6 };
        foreach (var s in new[] { "normal", "hover", "pressed", "focus" }) tab.AddThemeStyleboxOverride(s, flat);
        tab.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        tab.AddThemeFontSizeOverride("font_size", 13);
        row.AddChild(tab);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        vbox.AddChild(row);
    }

    private static Label BuildStatusLabel()
    {
        var lbl = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Center };
        lbl.AddThemeFontSizeOverride("font_size", 11);
        lbl.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        return lbl;
    }
}
