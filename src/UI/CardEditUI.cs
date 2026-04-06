using System;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Actions;

namespace DevMode.UI;

/// <summary>Full-screen overlay for deep card editing.</summary>
internal static class CardEditUI
{
    private const string RootName = "DevModeCardEdit";

    public static void Show(NGlobalUi globalUi, Player player)
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
        panel.OffsetLeft = -400; panel.OffsetRight = 400;
        panel.OffsetTop = -320; panel.OffsetBottom = 320;
        var style = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.12f, 0.97f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8, ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 12, ContentMarginBottom = 12 };
        panel.AddThemeStyleboxOverride("panel", style);
        panel.MouseFilter = Control.MouseFilterEnum.Stop;
        root.AddChild(panel);

        // Split: left = card list, right = editor
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(hbox);

        // Left: card list
        var leftVbox = new VBoxContainer { CustomMinimumSize = new Vector2(250, 0), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        leftVbox.AddChild(new Label { Text = I18N.T("cardEdit.title", "Card Editor"), HorizontalAlignment = HorizontalAlignment.Center });

        var search = new LineEdit { PlaceholderText = I18N.T("cardEdit.search", "Search..."), ClearButtonEnabled = true };
        leftVbox.AddChild(search);

        var cardScroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var cardList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        cardList.AddThemeConstantOverride("separation", 2);
        cardScroll.AddChild(cardList);
        leftVbox.AddChild(cardScroll);
        hbox.AddChild(leftVbox);

        // Right: editor panel
        var rightVbox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        rightVbox.AddThemeConstantOverride("separation", 4);
        rightVbox.AddChild(new Label { Text = I18N.T("cardEdit.properties", "Properties"), HorizontalAlignment = HorizontalAlignment.Center });

        var editorScroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var editorContent = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        editorContent.AddThemeConstantOverride("separation", 4);
        editorScroll.AddChild(editorContent);
        rightVbox.AddChild(editorScroll);
        hbox.AddChild(rightVbox);

        var statusLabel = new Label { Text = "" };
        rightVbox.AddChild(statusLabel);

        CardModel? selectedCard = null;

        void ShowEditor(CardModel card)
        {
            selectedCard = card;
            foreach (var child in editorContent.GetChildren()) ((Node)child).QueueFree();

            editorContent.AddChild(new Label { Text = CardEditActions.GetCardDisplayName(card) });

            // Cost
            AddIntEditor(editorContent, I18N.T("cardEdit.cost", "Base Cost"), CardEditActions.GetBaseCost(card) ?? 0,
                v => { CardEditActions.TrySetBaseCost(card, v); statusLabel.Text = "Cost set."; });

            // Replay
            AddIntEditor(editorContent, I18N.T("cardEdit.replay", "Replay Count"), CardEditActions.GetReplayCount(card) ?? 0,
                v => { CardEditActions.TrySetReplayCount(card, v); statusLabel.Text = "Replay set."; });

            // Damage
            AddIntEditor(editorContent, I18N.T("cardEdit.damage", "Base Damage"), CardEditActions.GetDamage(card) ?? 0,
                v => { CardEditActions.TrySetDamage(card, v); statusLabel.Text = "Damage set."; });

            // Block
            AddIntEditor(editorContent, I18N.T("cardEdit.block", "Base Block"), CardEditActions.GetBlock(card) ?? 0,
                v => { CardEditActions.TrySetBlock(card, v); statusLabel.Text = "Block set."; });

            // Keywords
            AddBoolToggle(editorContent, I18N.T("cardEdit.exhaust", "Exhaust"), CardEditActions.GetExhaust(card) ?? false,
                v => { CardEditActions.TrySetExhaust(card, v); statusLabel.Text = "Exhaust toggled."; });
            AddBoolToggle(editorContent, I18N.T("cardEdit.ethereal", "Ethereal"), CardEditActions.GetEthereal(card) ?? false,
                v => { CardEditActions.TrySetEthereal(card, v); statusLabel.Text = "Ethereal toggled."; });
            AddBoolToggle(editorContent, I18N.T("cardEdit.unplayable", "Unplayable"), CardEditActions.GetUnplayable(card) ?? false,
                v => { CardEditActions.TrySetUnplayable(card, v); statusLabel.Text = "Unplayable toggled."; });

            // Enchantment
            var enchantTypes = CardEditActions.GetEnchantmentTypes();
            if (enchantTypes.Count > 0)
            {
                editorContent.AddChild(new HSeparator());
                editorContent.AddChild(new Label { Text = I18N.T("cardEdit.enchantment", "Enchantment") });

                var enchantRow = new HBoxContainer();
                enchantRow.AddThemeConstantOverride("separation", 4);

                var enchantDropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                for (int i = 0; i < enchantTypes.Count; i++)
                    enchantDropdown.AddItem(enchantTypes[i].Name, i);
                enchantRow.AddChild(enchantDropdown);

                var applyBtn = new Button { Text = I18N.T("cardEdit.applyEnchant", "Apply"), CustomMinimumSize = new Vector2(60, 26) };
                applyBtn.Pressed += () =>
                {
                    int idx = enchantDropdown.Selected;
                    if (idx >= 0 && idx < enchantTypes.Count)
                    {
                        bool ok = CardEditActions.TryApplyEnchantment(card, enchantTypes[idx]);
                        statusLabel.Text = ok ? "Enchantment applied." : "Failed to apply.";
                    }
                };
                enchantRow.AddChild(applyBtn);

                var forceBtn = new Button { Text = I18N.T("cardEdit.forceEnchant", "Force"), CustomMinimumSize = new Vector2(60, 26) };
                forceBtn.Pressed += () =>
                {
                    int idx = enchantDropdown.Selected;
                    if (idx >= 0 && idx < enchantTypes.Count)
                    {
                        bool ok = CardEditActions.TryApplyEnchantment(card, enchantTypes[idx], force: true);
                        statusLabel.Text = ok ? "Enchantment force-applied." : "Failed.";
                    }
                };
                enchantRow.AddChild(forceBtn);

                editorContent.AddChild(enchantRow);

                var clearEnchantBtn = new Button { Text = I18N.T("cardEdit.clearEnchant", "Clear Enchantment"), CustomMinimumSize = new Vector2(0, 26) };
                clearEnchantBtn.Pressed += () =>
                {
                    CardEditActions.TryClearEnchantment(card);
                    statusLabel.Text = "Enchantment cleared.";
                };
                editorContent.AddChild(clearEnchantBtn);
            }
        }

        void RebuildCardList(string filter)
        {
            foreach (var child in cardList.GetChildren()) ((Node)child).QueueFree();
            var cards = CardEditActions.GetDeckCards(player);
            var filtered = string.IsNullOrWhiteSpace(filter)
                ? cards
                : cards.Where(c => CardEditActions.GetCardDisplayName(c).Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();

            foreach (var card in filtered)
            {
                var btn = new Button { Text = CardEditActions.GetCardDisplayName(card), CustomMinimumSize = new Vector2(0, 28) };
                btn.Pressed += () => ShowEditor(card);
                cardList.AddChild(btn);
            }
        }

        search.TextChanged += RebuildCardList;
        RebuildCardList("");

        ((Node)globalUi).AddChild(root);
    }

    public static void Remove(NGlobalUi globalUi)
    {
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
    }

    private static void AddIntEditor(VBoxContainer parent, string label, int currentValue, Action<int> onApply)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(100, 0) });
        var spin = new SpinBox { MinValue = -999, MaxValue = 9999, Value = currentValue, Step = 1, CustomMinimumSize = new Vector2(80, 26) };
        row.AddChild(spin);
        var btn = new Button { Text = I18N.T("cardEdit.apply", "Set"), CustomMinimumSize = new Vector2(40, 26) };
        btn.Pressed += () => onApply((int)spin.Value);
        row.AddChild(btn);
        parent.AddChild(row);
    }

    private static void AddBoolToggle(VBoxContainer parent, string label, bool currentValue, Action<bool> onToggle)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        var check = new CheckBox { Text = label, ButtonPressed = currentValue };
        check.Toggled += v => onToggle(v);
        row.AddChild(check);
        parent.AddChild(row);
    }
}
