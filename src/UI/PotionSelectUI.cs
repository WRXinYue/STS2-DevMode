using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Actions;

namespace DevMode.UI;

/// <summary>Full-screen overlay for selecting a Potion from ModelDb.</summary>
internal static class PotionSelectUI
{
    private const string RootName = "DevModePotionSelect";

    public static void Show(NGlobalUi globalUi, Action<PotionModel> onSelected)
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

        vbox.AddChild(new Label { Text = I18N.T("potion.select.title", "Select Potion"), HorizontalAlignment = HorizontalAlignment.Center });

        var search = new LineEdit { PlaceholderText = I18N.T("potion.search", "Search..."), ClearButtonEnabled = true };
        vbox.AddChild(search);

        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(list);
        vbox.AddChild(scroll);

        var allPotions = PotionActions.GetAllPotions().OrderBy(p => PotionActions.GetPotionDisplayName(p)).ToList();

        void Rebuild(string filter)
        {
            foreach (var child in list.GetChildren()) ((Node)child).QueueFree();
            var filtered = string.IsNullOrWhiteSpace(filter)
                ? allPotions
                : allPotions.Where(p => PotionActions.GetPotionDisplayName(p).Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var potion in filtered)
            {
                var btn = new Button { Text = PotionActions.GetPotionDisplayName(potion), CustomMinimumSize = new Vector2(0, 30) };
                btn.Pressed += () => onSelected(potion);
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
