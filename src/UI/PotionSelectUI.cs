using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Actions;

namespace DevMode.UI;

/// <summary>Potion picker — spliced to the DevMode rail, matching card / relic browser layout.</summary>
internal static class PotionSelectUI
{
    private const string RootName = "DevModePotionSelect";
    private const float  PanelW   = 520f;

    public static void Show(NGlobalUi globalUi, Action<PotionModel> onSelected)
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

        BuildNavTab(vbox, I18N.T("potion.nav.title", "Potions"));

        var (searchRow, search) = DevPanelUI.CreateSearchRow(I18N.T("potion.search", "Search potions..."));
        vbox.AddChild(searchRow);

        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 3);
        scroll.AddChild(list);
        vbox.AddChild(scroll);

        var statusLabel = BuildStatusLabel();
        vbox.AddChild(statusLabel);

        var allPotions = PotionActions.GetAllPotions().OrderBy(p => PotionActions.GetPotionDisplayName(p)).ToList();

        void Rebuild(string filter)
        {
            foreach (var child in list.GetChildren()) ((Node)child).QueueFree();
            var filtered = string.IsNullOrWhiteSpace(filter)
                ? allPotions
                : allPotions.Where(p => PotionActions.GetPotionDisplayName(p).Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var potion in filtered)
            {
                var btn = DevPanelUI.CreateListItemButton(PotionActions.GetPotionDisplayName(potion));
                btn.Pressed += () =>
                {
                    onSelected(potion);
                    statusLabel.Text = I18N.T("potion.added", "Added: {0}", PotionActions.GetPotionDisplayName(potion));
                };
                list.AddChild(btn);
            }
            statusLabel.Text = I18N.T("potion.count", "{0} potions", filtered.Count);
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

    private static void BuildNavTab(VBoxContainer vbox, string title)
    {
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
        vbox.AddChild(new ColorRect { CustomMinimumSize = new Vector2(0, 1), Color = new Color(1f, 1f, 1f, 0.06f), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
    }

    private static Label BuildStatusLabel()
    {
        var lbl = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Center };
        lbl.AddThemeFontSizeOverride("font_size", 11);
        lbl.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        return lbl;
    }
}
