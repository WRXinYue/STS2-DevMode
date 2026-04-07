using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using DevMode.Actions;
using DevMode.Presets;

namespace DevMode.UI;

/// <summary>
/// Self-drawn card browser replacing official NCardLibrary / NDeckCardSelectScreen.
/// Center: scrollable card grid. Right: context-sensitive operation panel.
/// </summary>
internal static class CardBrowserUI
{
    private const string RootName = "DevModeCardBrowser";
    private const int GridColumns = 4;
    private const float RightPanelWidth = 300f;

    private static readonly BindingFlags ReflFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // ── Rarity colors ──
    private static readonly Color ColCommon   = new(0.55f, 0.55f, 0.58f);
    private static readonly Color ColUncommon = new(0.35f, 0.55f, 0.85f);
    private static readonly Color ColRare     = new(0.85f, 0.72f, 0.25f);
    private static readonly Color ColSpecial  = new(0.70f, 0.45f, 0.85f);
    private static readonly Color ColCurse    = new(0.75f, 0.30f, 0.30f);

    private static readonly Color ColCardBg        = new(0.14f, 0.14f, 0.17f, 0.95f);
    private static readonly Color ColCardBgHover   = new(0.18f, 0.18f, 0.22f, 0.95f);
    private static readonly Color ColCardBgSelected = new(0.20f, 0.28f, 0.40f, 0.95f);
    private static readonly Color ColCardBorder     = new(1f, 1f, 1f, 0.08f);
    private static readonly Color ColSelectedBorder = new(0.40f, 0.68f, 1f, 0.7f);

    private static readonly Color ColPanelBg     = new(0.11f, 0.11f, 0.14f, 0.96f);
    private static readonly Color ColPanelBorder = new(1f, 1f, 1f, 0.08f);
    private static readonly Color ColSubtle      = new(0.50f, 0.50f, 0.58f);

    // ── Filter / browse state ──
    private enum CardTypeFilter { All, Attack, Skill, Power, Status, Curse }

    /// <summary>Where the card grid sources its cards from.</summary>
    private enum BrowseSource { AllCards, Hand, DrawPile, DiscardPile, Deck }
    private static BrowseSource _browseSource = BrowseSource.AllCards;

    private static CardTarget? BrowseSourceToTarget(BrowseSource src) => src switch
    {
        BrowseSource.Hand        => CardTarget.Hand,
        BrowseSource.DrawPile    => CardTarget.DrawPile,
        BrowseSource.DiscardPile => CardTarget.DiscardPile,
        BrowseSource.Deck        => CardTarget.Deck,
        _                        => null
    };

    private static bool IsLibrarySource => _browseSource == BrowseSource.AllCards;

    // ── Cached card info via reflection ──
    private static string TryGetCardType(CardModel card)
    {
        try
        {
            var prop = card.GetType().GetProperty("CardType", ReflFlags)
                    ?? card.GetType().GetProperty("Type", ReflFlags);
            if (prop != null) return prop.GetValue(card)?.ToString() ?? "";
        }
        catch { }
        return "";
    }

    private static string TryGetCardRarity(CardModel card)
    {
        try
        {
            var prop = card.GetType().GetProperty("Rarity", ReflFlags)
                    ?? card.GetType().GetProperty("CardRarity", ReflFlags);
            if (prop != null) return prop.GetValue(card)?.ToString() ?? "";
        }
        catch { }
        return "";
    }

    private static Color RarityToColor(string rarity)
    {
        return rarity.ToUpperInvariant() switch
        {
            "COMMON" or "BASIC" => ColCommon,
            "UNCOMMON"          => ColUncommon,
            "RARE"              => ColRare,
            "SPECIAL"           => ColSpecial,
            "CURSE" or "STATUS" => ColCurse,
            _                   => ColCommon
        };
    }

    private static bool MatchesTypeFilter(CardModel card, CardTypeFilter filter)
    {
        if (filter == CardTypeFilter.All) return true;
        var type = TryGetCardType(card).ToUpperInvariant();
        return filter switch
        {
            CardTypeFilter.Attack => type.Contains("ATTACK"),
            CardTypeFilter.Skill  => type.Contains("SKILL"),
            CardTypeFilter.Power  => type.Contains("POWER"),
            CardTypeFilter.Status => type.Contains("STATUS"),
            CardTypeFilter.Curse  => type.Contains("CURSE"),
            _                     => true
        };
    }

    // Rail geometry — must match DevPanelUI constants
    private const float RailW      = 52f;
    private const float RailLeft   = 24f;
    private const float PanelLeft  = RailLeft + RailW;              // 76 — flush against rail
    private const float PanelRight = 24f;                           // right-side margin
    private const int   RailRadius = 14;

    // ──────── Rail splice helpers ────────

    /// <summary>
    /// Toggle the rail's right-side corners so the browser panel looks
    /// visually fused with the rail (joined=true) or detached (joined=false).
    /// </summary>
    private static void SpliceRail(NGlobalUi globalUi, bool joined)
    {
        var railRoot = ((Node)globalUi).GetNodeOrNull<Control>("DevModeRailRoot");
        var rail = railRoot?.GetNodeOrNull<PanelContainer>("Rail");
        if (rail == null) return;

        if (rail.GetThemeStylebox("panel") is StyleBoxFlat s)
        {
            int r = joined ? 0 : RailRadius;
            s.CornerRadiusTopRight    = r;
            s.CornerRadiusBottomRight = r;
            s.BorderWidthRight = joined ? 0 : 1;
        }
    }

    // ──────── Public API ────────

    public static void Show(NGlobalUi globalUi, RunState state, Player player)
    {
        Remove(globalUi);

        DevPanelUI.PinRail();
        SpliceRail(globalUi, joined: true);

        var root = new Control { Name = RootName, MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 1250 };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        root.TreeExiting += () =>
        {
            DevPanelUI.UnpinRail();
            SpliceRail(globalUi, joined: false);
        };

        var panel = CreateBrowserPanel();
        root.AddChild(panel);

        var content = panel.GetNode<VBoxContainer>("Content");

        // Forward-declare variables captured by SwitchTab local function
        LineEdit searchInput = null!;
        var typeFilter = CardTypeFilter.All;
        GridContainer gridContainer = null!;
        VBoxContainer rightContent = null!;
        Label statusLabel = null!;

        // ── Nav bar: tabs with sliding indicator ──
        var sourceLabels = new[]
        {
            I18N.T("cardBrowser.sourceAll", "All"),
            I18N.T("topbar.card.hand", "Hand"),
            I18N.T("topbar.card.drawPile", "Draw Pile"),
            I18N.T("topbar.card.discardPile", "Discard"),
            I18N.T("topbar.card.deck", "Deck")
        };
        var sources = new[] { BrowseSource.AllCards, BrowseSource.Hand, BrowseSource.DrawPile, BrowseSource.DiscardPile, BrowseSource.Deck };
        var tabButtons = new Button[sourceLabels.Length];
        int activeTabIdx = Array.IndexOf(sources, _browseSource);
        if (activeTabIdx < 0) activeTabIdx = 0;

        var navSection = new Control
        {
            CustomMinimumSize = new Vector2(0, 34),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        var tabRow = new HBoxContainer();
        tabRow.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        tabRow.AddThemeConstantOverride("separation", 0);

        var indicator = new ColorRect
        {
            Color = ColNavAccent,
            AnchorLeft = 0, AnchorRight = 0,
            AnchorTop = 1, AnchorBottom = 1,
            OffsetTop = -2, OffsetBottom = 0
        };

        for (int i = 0; i < sourceLabels.Length; i++)
        {
            int idx = i;
            var tab = CreateNavTab(sourceLabels[idx], idx == activeTabIdx);
            tab.Pressed += () => SwitchTab(idx);
            tabButtons[idx] = tab;
            tabRow.AddChild(tab);
        }
        tabRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        navSection.AddChild(tabRow);
        navSection.AddChild(indicator);

        // Position indicator once layout is computed (deferred so sizes are final)
        navSection.Ready += () =>
            Callable.From(() => MoveIndicator(activeTabIdx, false)).CallDeferred();

        var navOuter = new VBoxContainer();
        navOuter.AddThemeConstantOverride("separation", 0);
        navOuter.AddChild(navSection);
        navOuter.AddChild(new ColorRect
        {
            CustomMinimumSize = new Vector2(0, 1),
            Color = new Color(1f, 1f, 1f, 0.06f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });
        content.AddChild(navOuter);

        // ── Search + type filter row ──
        var filterRow = new HBoxContainer();
        filterRow.AddThemeConstantOverride("separation", 8);

        searchInput = new LineEdit
        {
            PlaceholderText = I18N.T("cardBrowser.search", "Search..."),
            ClearButtonEnabled = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        filterRow.AddChild(searchInput);

        var typeFilterBtn = new OptionButton { CustomMinimumSize = new Vector2(100, 0) };
        typeFilterBtn.AddItem(I18N.T("cardBrowser.filterAll", "All"), 0);
        typeFilterBtn.AddItem(I18N.T("cardBrowser.filterAttack", "Attack"), 1);
        typeFilterBtn.AddItem(I18N.T("cardBrowser.filterSkill", "Skill"), 2);
        typeFilterBtn.AddItem(I18N.T("cardBrowser.filterPower", "Power"), 3);
        typeFilterBtn.AddItem(I18N.T("cardBrowser.filterStatus", "Status"), 4);
        typeFilterBtn.AddItem(I18N.T("cardBrowser.filterCurse", "Curse"), 5);
        filterRow.AddChild(typeFilterBtn);

        content.AddChild(filterRow);

        // ── Body: card grid (left) + right panel ──
        var body = new HSplitContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        body.DraggerVisibility = SplitContainer.DraggerVisibilityEnum.Hidden;
        content.AddChild(body);

        var gridScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        gridContainer = new GridContainer
        {
            Columns = GridColumns,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        gridContainer.AddThemeConstantOverride("h_separation", 6);
        gridContainer.AddThemeConstantOverride("v_separation", 6);
        gridScroll.AddChild(gridContainer);
        body.AddChild(gridScroll);

        var rightPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(RightPanelWidth, 0),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        var rightStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.09f, 0.12f, 0.90f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 12, ContentMarginBottom = 12,
            BorderWidthLeft = 1, BorderColor = new Color(1f, 1f, 1f, 0.06f)
        };
        rightPanel.AddThemeStyleboxOverride("panel", rightStyle);

        var rightScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        rightContent = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        rightContent.AddThemeConstantOverride("separation", 6);

        AddPlaceholder(rightContent);

        rightScroll.AddChild(rightContent);
        rightPanel.AddChild(rightScroll);
        body.AddChild(rightPanel);

        // ── Status bar ──
        statusLabel = new Label { Text = "" };
        statusLabel.AddThemeFontSizeOverride("font_size", 12);
        statusLabel.AddThemeColorOverride("font_color", ColSubtle);
        content.AddChild(statusLabel);

        // ── State + logic ──
        CardModel? selectedCard = null;
        Button? selectedButton = null;

        List<CardModel> GetCards()
        {
            if (IsLibrarySource)
                return ModelDb.AllCards.OrderBy(c => CardEditActions.GetCardDisplayName(c)).ToList();
            var t = BrowseSourceToTarget(_browseSource);
            return t.HasValue
                ? CardActions.GetCardsForTarget(player, t.Value)
                : new List<CardModel>();
        }

        void ShowRightPanel(CardModel card)
        {
            selectedCard = card;
            foreach (var child in rightContent.GetChildren()) ((Node)child).QueueFree();
            BuildRightPanelContent(rightContent, statusLabel, card, state, player, globalUi,
                () => RebuildGrid(searchInput.Text ?? "", typeFilter));
        }

        void ClearRightPanel()
        {
            foreach (var child in rightContent.GetChildren()) ((Node)child).QueueFree();
            AddPlaceholder(rightContent);
            selectedCard = null;
            selectedButton = null;
        }

        void RebuildGrid(string filter, CardTypeFilter tFilter)
        {
            foreach (var child in gridContainer.GetChildren()) ((Node)child).QueueFree();
            selectedButton = null;

            var cards = GetCards();
            var filtered = cards.Where(c =>
            {
                if (!MatchesTypeFilter(c, tFilter)) return false;
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    var name = CardEditActions.GetCardDisplayName(c);
                    if (!name.Contains(filter, StringComparison.OrdinalIgnoreCase)) return false;
                }
                return true;
            }).ToList();

            foreach (var card in filtered)
            {
                var cardBtn = CreateCardGridItem(card);
                cardBtn.Pressed += () =>
                {
                    if (selectedButton != null)
                        ApplyCardItemStyle(selectedButton, false);
                    selectedButton = cardBtn;
                    ApplyCardItemStyle(cardBtn, true);
                    ShowRightPanel(card);
                };
                gridContainer.AddChild(cardBtn);
            }

            statusLabel.Text = string.Format(I18N.T("cardBrowser.count", "{0} / {1} cards"),
                filtered.Count, cards.Count);
        }

        // ── Nav tab switching (in-place, no panel rebuild) ──

        void MoveIndicator(int tabIdx, bool animate)
        {
            if (tabIdx < 0 || tabIdx >= tabButtons.Length) return;
            var btn = tabButtons[tabIdx];
            float left = btn.Position.X;
            float right = left + btn.Size.X;

            if (animate && indicator.IsInsideTree())
            {
                var tween = indicator.CreateTween();
                tween.SetParallel(true);
                tween.TweenProperty(indicator, "offset_left", left, 0.25f)
                     .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
                tween.TweenProperty(indicator, "offset_right", right, 0.25f)
                     .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            }
            else
            {
                indicator.OffsetLeft = left;
                indicator.OffsetRight = right;
            }
        }

        void SwitchTab(int tabIdx)
        {
            if (tabIdx == activeTabIdx) return;
            activeTabIdx = tabIdx;
            _browseSource = sources[tabIdx];

            // Update tab text colors
            for (int i = 0; i < tabButtons.Length; i++)
            {
                bool a = i == tabIdx;
                tabButtons[i].AddThemeColorOverride("font_color",         a ? ColNavActive : ColNavInactive);
                tabButtons[i].AddThemeColorOverride("font_hover_color",   a ? ColNavActive : ColNavHover);
                tabButtons[i].AddThemeColorOverride("font_pressed_color", ColNavActive);
            }

            MoveIndicator(tabIdx, true);
            ClearRightPanel();
            RebuildGrid(searchInput.Text ?? "", typeFilter);
        }

        // ── Wire up events ──

        searchInput.TextChanged += text => RebuildGrid(text, typeFilter);
        typeFilterBtn.ItemSelected += idx =>
        {
            typeFilter = (CardTypeFilter)(int)idx;
            RebuildGrid(searchInput.Text ?? "", typeFilter);
        };

        RebuildGrid("", CardTypeFilter.All);

        ((Node)globalUi).AddChild(root);
    }

    private static void AddPlaceholder(VBoxContainer container)
    {
        var lbl = new Label
        {
            Text = I18N.T("cardBrowser.selectCard", "Select a card"),
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
        };
        lbl.AddThemeColorOverride("font_color", ColSubtle);
        container.AddChild(lbl);
    }

    public static void Remove(NGlobalUi globalUi)
    {
        var parent = (Node)globalUi;
        var node = parent.GetNodeOrNull<Control>(RootName);
        if (node != null)
        {
            parent.RemoveChild(node);   // triggers TreeExiting → UnpinRail
            node.QueueFree();
        }
    }

    // ──────── Card Grid Item ────────

    private static Button CreateCardGridItem(CardModel card)
    {
        var name = CardEditActions.GetCardDisplayName(card);
        var cost = CardEditActions.GetBaseCost(card);
        var rarity = TryGetCardRarity(card);
        var type = TryGetCardType(card);
        var upgradeLevel = 0;
        try { upgradeLevel = card.CurrentUpgradeLevel; } catch { }
        var maxUpgrade = 0;
        try { maxUpgrade = card.MaxUpgradeLevel; } catch { }

        var rarityColor = RarityToColor(rarity);

        var btn = new Button
        {
            CustomMinimumSize = new Vector2(0, 52),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.None,
            ClipText = true
        };

        // Build rich text content
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);
        hbox.MouseFilter = Control.MouseFilterEnum.Ignore;

        // Cost badge
        var costBadge = new Label
        {
            Text = cost?.ToString() ?? "-",
            CustomMinimumSize = new Vector2(26, 26),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        costBadge.AddThemeFontSizeOverride("font_size", 13);
        costBadge.MouseFilter = Control.MouseFilterEnum.Ignore;
        var badgeStyle = new StyleBoxFlat
        {
            BgColor = rarityColor.Darkened(0.5f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 2, ContentMarginRight = 2
        };
        costBadge.AddThemeStyleboxOverride("normal", badgeStyle);
        hbox.AddChild(costBadge);

        // Name + subtitle column
        var textCol = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        textCol.AddThemeConstantOverride("separation", 0);
        textCol.MouseFilter = Control.MouseFilterEnum.Ignore;

        var nameLabel = new Label
        {
            Text = upgradeLevel > 0 ? $"{name} +{upgradeLevel}" : name,
            ClipText = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        textCol.AddChild(nameLabel);

        var subtitleParts = new List<string>();
        if (!string.IsNullOrEmpty(type)) subtitleParts.Add(type);
        if (!string.IsNullOrEmpty(rarity)) subtitleParts.Add(rarity);
        if (maxUpgrade > 0) subtitleParts.Add($"Lv {upgradeLevel}/{maxUpgrade}");

        var subtitleLabel = new Label
        {
            Text = string.Join(" · ", subtitleParts),
            ClipText = true
        };
        subtitleLabel.AddThemeFontSizeOverride("font_size", 10);
        subtitleLabel.AddThemeColorOverride("font_color", ColSubtle);
        subtitleLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        textCol.AddChild(subtitleLabel);

        hbox.AddChild(textCol);

        // Rarity dot
        var rarityDot = new Label
        {
            Text = "●",
            VerticalAlignment = VerticalAlignment.Center
        };
        rarityDot.AddThemeFontSizeOverride("font_size", 10);
        rarityDot.AddThemeColorOverride("font_color", rarityColor);
        rarityDot.MouseFilter = Control.MouseFilterEnum.Ignore;
        hbox.AddChild(rarityDot);

        btn.AddChild(hbox);
        btn.Text = " ";
        ApplyCardItemStyle(btn, false);

        return btn;
    }

    private static void ApplyCardItemStyle(Button btn, bool selected)
    {
        var bg = selected ? ColCardBgSelected : ColCardBg;
        var border = selected ? ColSelectedBorder : ColCardBorder;

        var style = new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 8, ContentMarginRight = 8,
            ContentMarginTop = 4, ContentMarginBottom = 4,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderColor = border
        };
        var hover = new StyleBoxFlat
        {
            BgColor = selected ? ColCardBgSelected : ColCardBgHover,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 8, ContentMarginRight = 8,
            ContentMarginTop = 4, ContentMarginBottom = 4,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderColor = selected ? ColSelectedBorder : new Color(1f, 1f, 1f, 0.15f)
        };
        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus", style);
        btn.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.01f));
        btn.AddThemeColorOverride("font_hover_color", new Color(1, 1, 1, 0.01f));
        btn.AddThemeColorOverride("font_pressed_color", new Color(1, 1, 1, 0.01f));
    }

    // ──────── Nav tab colors ────────

    private static readonly Color ColNavActive     = new(0.40f, 0.68f, 1f);
    private static readonly Color ColNavInactive   = new(0.55f, 0.55f, 0.62f);
    private static readonly Color ColNavHover      = new(0.78f, 0.78f, 0.85f);
    private static readonly Color ColNavAccent     = new(0.40f, 0.68f, 1f, 0.85f);

    private static Button CreateNavTab(string text, bool active)
    {
        var btn = new Button
        {
            Text = text,
            FocusMode = Control.FocusModeEnum.None,
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(0, 32)
        };

        var flat = new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            ContentMarginLeft = 14, ContentMarginRight = 14,
            ContentMarginTop = 4, ContentMarginBottom = 6
        };

        btn.AddThemeStyleboxOverride("normal",  flat);
        btn.AddThemeStyleboxOverride("hover",   flat);
        btn.AddThemeStyleboxOverride("pressed", flat);
        btn.AddThemeStyleboxOverride("focus",   flat);

        btn.AddThemeColorOverride("font_color",         active ? ColNavActive : ColNavInactive);
        btn.AddThemeColorOverride("font_hover_color",   active ? ColNavActive : ColNavHover);
        btn.AddThemeColorOverride("font_pressed_color", ColNavActive);
        btn.AddThemeFontSizeOverride("font_size", 13);

        return btn;
    }

    // ──────── Right Panel Content (unified) ────────

    private static void BuildRightPanelContent(VBoxContainer container, Label statusLabel,
        CardModel card, RunState state, Player player, NGlobalUi globalUi, Action onGridRefresh)
    {
        var cardName = CardEditActions.GetCardDisplayName(card);

        // ── Card header ──
        var headerLabel = new Label
        {
            Text = cardName,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        headerLabel.AddThemeFontSizeOverride("font_size", 16);
        container.AddChild(headerLabel);

        var infoLines = new List<string>();
        var type = TryGetCardType(card);
        var rarity = TryGetCardRarity(card);
        if (!string.IsNullOrEmpty(type)) infoLines.Add(type);
        if (!string.IsNullOrEmpty(rarity)) infoLines.Add(rarity);
        var costVal = CardEditActions.GetBaseCost(card);
        if (costVal.HasValue) infoLines.Add($"Cost: {costVal.Value}");

        if (infoLines.Count > 0)
        {
            var infoLabel = new Label
            {
                Text = string.Join("  ·  ", infoLines),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            infoLabel.AddThemeFontSizeOverride("font_size", 11);
            infoLabel.AddThemeColorOverride("font_color", ColSubtle);
            container.AddChild(infoLabel);
        }

        var desc = CardEditActions.GetDescriptionText(card);
        if (!string.IsNullOrWhiteSpace(desc))
        {
            var descLabel = new Label
            {
                Text = desc,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            descLabel.AddThemeFontSizeOverride("font_size", 12);
            descLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.85f));
            container.AddChild(descLabel);
        }

        container.AddChild(new HSeparator());

        if (IsLibrarySource)
        {
            // ── Library card → Add action ──
            BuildAddSection(container, statusLabel, card, state, player);
        }
        else
        {
            // ── Owned card → Upgrade / Remove actions ──
            BuildOwnedCardActions(container, statusLabel, card, state, player, globalUi, onGridRefresh);
        }

        // ── Edit section (always available) ──
        container.AddChild(new HSeparator());
        BuildEditSection(container, statusLabel, card);
    }

    // ── Add section (for library source) ──

    private static void BuildAddSection(VBoxContainer container, Label statusLabel,
        CardModel card, RunState state, Player player)
    {
        // Target picker
        var targetRow = new HBoxContainer();
        targetRow.AddThemeConstantOverride("separation", 4);
        var targetLbl = new Label { Text = I18N.T("cardBrowser.sidebarTarget", "Target") };
        targetLbl.AddThemeFontSizeOverride("font_size", 12);
        targetRow.AddChild(targetLbl);
        var targetPicker = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        targetPicker.AddItem(I18N.T("topbar.card.hand", "Hand"), 0);
        targetPicker.AddItem(I18N.T("topbar.card.drawPile", "Draw Pile"), 1);
        targetPicker.AddItem(I18N.T("topbar.card.discardPile", "Discard"), 2);
        targetPicker.AddItem(I18N.T("topbar.card.deck", "Deck"), 3);
        targetPicker.Selected = DevModeState.CardTarget switch
        {
            CardTarget.Hand => 0, CardTarget.DrawPile => 1,
            CardTarget.DiscardPile => 2, CardTarget.Deck => 3, _ => 3
        };
        targetPicker.ItemSelected += idx =>
        {
            DevModeState.CardTarget = idx switch
            {
                0 => CardTarget.Hand, 1 => CardTarget.DrawPile,
                2 => CardTarget.DiscardPile, _ => CardTarget.Deck
            };
        };
        targetRow.AddChild(targetPicker);
        container.AddChild(targetRow);

        // Duration picker
        var durRow = new HBoxContainer();
        durRow.AddThemeConstantOverride("separation", 4);
        var durLbl = new Label { Text = I18N.T("cardBrowser.sidebarDuration", "Duration") };
        durLbl.AddThemeFontSizeOverride("font_size", 12);
        durRow.AddChild(durLbl);
        var durPicker = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        durPicker.AddItem(I18N.T("topbar.card.temporary", "Temp"), 0);
        durPicker.AddItem(I18N.T("topbar.card.permanent", "Perm"), 1);
        durPicker.Selected = DevModeState.EffectDuration == EffectDuration.Permanent ? 1 : 0;
        durPicker.ItemSelected += idx =>
        {
            DevModeState.EffectDuration = idx == 1 ? EffectDuration.Permanent : EffectDuration.Temporary;
        };
        durRow.AddChild(durPicker);
        container.AddChild(durRow);

        container.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });

        var addBtn = CreateActionButton(
            I18N.T("cardBrowser.addCard", "Add Card"),
            new Color(0.25f, 0.55f, 0.35f, 0.9f));
        addBtn.Pressed += () =>
        {
            TaskHelper.RunSafely(CardActions.AddCard(state, player, card));
            statusLabel.Text = string.Format(I18N.T("cardBrowser.addedCard", "Added: {0}"),
                CardEditActions.GetCardDisplayName(card));
        };
        container.AddChild(addBtn);
    }

    // ── Owned card actions (Upgrade + Remove) ──

    private static void BuildOwnedCardActions(VBoxContainer container, Label statusLabel,
        CardModel card, RunState state, Player player, NGlobalUi globalUi, Action onGridRefresh)
    {
        // Upgrade
        int upgradeLevel = 0, maxUpgrade = 0;
        try { upgradeLevel = card.CurrentUpgradeLevel; maxUpgrade = card.MaxUpgradeLevel; } catch { }
        bool canUpgrade = upgradeLevel < maxUpgrade;

        var upgradeRow = new HBoxContainer();
        upgradeRow.AddThemeConstantOverride("separation", 4);
        var upgradeLbl = new Label
        {
            Text = $"Lv {upgradeLevel}/{maxUpgrade}",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        upgradeLbl.AddThemeFontSizeOverride("font_size", 12);
        upgradeRow.AddChild(upgradeLbl);

        var upgradeBtn = new Button
        {
            Text = I18N.T("cardBrowser.upgradeCard", "Upgrade"),
            Disabled = !canUpgrade,
            CustomMinimumSize = new Vector2(70, 30)
        };
        ApplySmallActionStyle(upgradeBtn, canUpgrade ? new Color(0.30f, 0.50f, 0.70f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.5f));
        upgradeBtn.Pressed += () =>
        {
            try
            {
                CardCmd.Upgrade(new[] { card }, CardPreviewStyle.HorizontalLayout);
                statusLabel.Text = string.Format(I18N.T("cardBrowser.upgraded", "Upgraded: {0}"),
                    CardEditActions.GetCardDisplayName(card));
                onGridRefresh();
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Upgrade failed: {ex.Message}";
            }
        };
        upgradeRow.AddChild(upgradeBtn);
        container.AddChild(upgradeRow);

        // Remove
        var target = BrowseSourceToTarget(_browseSource);
        var removeBtn = CreateActionButton(
            I18N.T("cardBrowser.deleteCard", "Remove Card"),
            new Color(0.65f, 0.25f, 0.25f, 0.9f));
        removeBtn.Pressed += () =>
        {
            try
            {
                if (target == CardTarget.Deck)
                {
                    TaskHelper.RunSafely(CardPileCmd.RemoveFromDeck(
                        (IReadOnlyList<CardModel>)new[] { card }, true));
                }
                else if (target.HasValue)
                {
                    TaskHelper.RunSafely(CardPileCmd.RemoveFromCombat(new[] { card }));
                    if (state.ContainsCard(card))
                        state.RemoveCard(card);
                }

                statusLabel.Text = string.Format(I18N.T("cardBrowser.deleted", "Removed: {0}"),
                    CardEditActions.GetCardDisplayName(card));
                onGridRefresh();
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Remove failed: {ex.Message}";
            }
        };
        container.AddChild(removeBtn);
    }

    // ── Edit section (inline property editor) ──

    private static void BuildEditSection(VBoxContainer container, Label statusLabel, CardModel card)
    {
        // Cost
        AddIntEditor(container, I18N.T("cardEdit.cost", "Base Cost"),
            CardEditActions.GetBaseCost(card) ?? 0,
            v => { CardEditActions.TrySetBaseCost(card, v); statusLabel.Text = I18N.T("cardBrowser.costSet", "Cost set."); });

        // Replay
        AddIntEditor(container, I18N.T("cardEdit.replay", "Replay Count"),
            CardEditActions.GetReplayCount(card) ?? 0,
            v => { CardEditActions.TrySetReplayCount(card, v); statusLabel.Text = I18N.T("cardBrowser.replaySet", "Replay set."); });

        // Damage
        AddIntEditor(container, I18N.T("cardEdit.damage", "Base Damage"),
            CardEditActions.GetDamage(card) ?? 0,
            v => { CardEditActions.TrySetDamage(card, v); statusLabel.Text = I18N.T("cardBrowser.damageSet", "Damage set."); });

        // Block
        AddIntEditor(container, I18N.T("cardEdit.block", "Base Block"),
            CardEditActions.GetBlock(card) ?? 0,
            v => { CardEditActions.TrySetBlock(card, v); statusLabel.Text = I18N.T("cardBrowser.blockSet", "Block set."); });

        // Keywords
        AddBoolToggle(container, I18N.T("cardEdit.exhaust", "Exhaust"),
            CardEditActions.GetExhaust(card) ?? false,
            v => { CardEditActions.TrySetExhaust(card, v); statusLabel.Text = I18N.T("cardBrowser.exhaustToggled", "Exhaust toggled."); });
        AddBoolToggle(container, I18N.T("cardEdit.ethereal", "Ethereal"),
            CardEditActions.GetEthereal(card) ?? false,
            v => { CardEditActions.TrySetEthereal(card, v); statusLabel.Text = I18N.T("cardBrowser.etherealToggled", "Ethereal toggled."); });
        AddBoolToggle(container, I18N.T("cardEdit.unplayable", "Unplayable"),
            CardEditActions.GetUnplayable(card) ?? false,
            v => { CardEditActions.TrySetUnplayable(card, v); statusLabel.Text = I18N.T("cardBrowser.unplayableToggled", "Unplayable toggled."); });
        AddBoolToggle(container, I18N.T("cardEdit.exhaustOnNextPlay", "Exhaust On Next Play"),
            CardEditActions.GetExhaustOnNextPlay(card) ?? false,
            v => { CardEditActions.TrySetExhaustOnNextPlay(card, v); statusLabel.Text = "Exhaust-on-next-play toggled."; });
        AddBoolToggle(container, I18N.T("cardEdit.singleTurnRetain", "Single-Turn Retain"),
            CardEditActions.GetSingleTurnRetain(card) ?? false,
            v => { CardEditActions.TrySetSingleTurnRetain(card, v); statusLabel.Text = "Single-turn retain toggled."; });
        AddBoolToggle(container, I18N.T("cardEdit.singleTurnSly", "Single-Turn Sly"),
            CardEditActions.GetSingleTurnSly(card) ?? false,
            v => { CardEditActions.TrySetSingleTurnSly(card, v); statusLabel.Text = "Single-turn sly toggled."; });

        // Dynamic vars
        var dynamicKeys = CardEditActions.GetDynamicVarKeys(card);
        if (dynamicKeys.Count > 0)
        {
            container.AddChild(new HSeparator());
            container.AddChild(new Label { Text = I18N.T("cardEdit.dynamicVars", "Dynamic Vars") });
            foreach (var key in dynamicKeys)
            {
                var displayKey = CardEditActions.GetDynamicVarDisplayName(key);
                AddIntEditor(container, displayKey, CardEditActions.GetDynamicVar(card, key) ?? 0,
                    v => { CardEditActions.TrySetDynamicVar(card, key, v); statusLabel.Text = $"{displayKey} set."; });
            }
        }

        container.AddChild(new HSeparator());

        // Name/description override
        AddTextEditor(container, I18N.T("cardEdit.titleText", "Name Override"),
            CardEditActions.GetTitleText(card),
            v => { CardEditActions.TrySetTitleText(card, v); statusLabel.Text = "Name override set."; });
        AddTextEditor(container, I18N.T("cardEdit.descText", "Description Override"),
            CardEditActions.GetDescriptionText(card),
            v => { CardEditActions.TrySetDescriptionText(card, v); statusLabel.Text = "Description override set."; });

        // Enchantment
        var enchantTypes = CardEditActions.GetEnchantmentTypes();
        if (enchantTypes.Count > 0)
        {
            container.AddChild(new HSeparator());
            container.AddChild(new Label { Text = I18N.T("cardEdit.enchantment", "Enchantment") });

            var enchantRow = new HBoxContainer();
            enchantRow.AddThemeConstantOverride("separation", 4);

            var enchantDropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            for (int i = 0; i < enchantTypes.Count; i++)
                enchantDropdown.AddItem(enchantTypes[i].Name, i);
            enchantRow.AddChild(enchantDropdown);

            var applyBtn = new Button { Text = I18N.T("cardEdit.applyEnchant", "Apply"), CustomMinimumSize = new Vector2(50, 26) };
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

            var forceBtn = new Button { Text = I18N.T("cardEdit.forceEnchant", "Force"), CustomMinimumSize = new Vector2(50, 26) };
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

            container.AddChild(enchantRow);

            var clearBtn = new Button { Text = I18N.T("cardEdit.clearEnchant", "Clear Enchantment"), CustomMinimumSize = new Vector2(0, 26) };
            clearBtn.Pressed += () =>
            {
                CardEditActions.TryClearEnchantment(card);
                statusLabel.Text = "Enchantment cleared.";
            };
            container.AddChild(clearBtn);
        }

        // Presets
        container.AddChild(new HSeparator());
        BuildPresetRow(container, statusLabel, card);
    }

    // ──────── Preset Row ────────

    private static void BuildPresetRow(VBoxContainer container, Label statusLabel, CardModel card)
    {
        var presetRow = new HBoxContainer();
        presetRow.AddThemeConstantOverride("separation", 4);

        var presetPicker = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var presetNameInput = new LineEdit
        {
            PlaceholderText = I18N.T("cardEdit.presetName", "Preset name..."),
            CustomMinimumSize = new Vector2(100, 0)
        };
        var saveBtn = new Button { Text = I18N.T("cardEdit.savePreset", "Save"), CustomMinimumSize = new Vector2(50, 26) };
        var applyBtn = new Button { Text = I18N.T("cardEdit.applyPreset", "Apply"), CustomMinimumSize = new Vector2(50, 26) };
        var delBtn = new Button { Text = I18N.T("cardEdit.deletePreset", "Delete"), CustomMinimumSize = new Vector2(50, 26) };

        void RebuildPresets()
        {
            presetPicker.Clear();
            var names = CardEditPresetManager.Store.All.Keys
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var n in names) presetPicker.AddItem(n);
            applyBtn.Disabled = names.Length == 0;
            delBtn.Disabled = names.Length == 0;
        }

        saveBtn.Pressed += () =>
        {
            var pName = presetNameInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(pName)) { statusLabel.Text = "Enter preset name."; return; }
            var cardId = ((AbstractModel)card).Id.Entry ?? "";
            var payload = new CardEditNamedPreset { CardId = cardId, Template = CardEditActions.CaptureTemplate(card) };
            CardEditPresetManager.Store.Set(pName, payload);
            statusLabel.Text = $"Preset saved: {pName}";
            RebuildPresets();
        };

        applyBtn.Pressed += () =>
        {
            if (presetPicker.ItemCount == 0) { statusLabel.Text = "No preset."; return; }
            var pName = presetPicker.GetItemText(presetPicker.Selected);
            if (!CardEditPresetManager.Store.TryGet(pName, out var preset)) { statusLabel.Text = "Preset not found."; return; }
            CardEditActions.ApplyTemplate(card, preset.Template);
            statusLabel.Text = $"Preset applied: {pName}";
        };

        delBtn.Pressed += () =>
        {
            if (presetPicker.ItemCount == 0) { statusLabel.Text = "No preset."; return; }
            var pName = presetPicker.GetItemText(presetPicker.Selected);
            if (CardEditPresetManager.Store.Delete(pName))
            {
                statusLabel.Text = $"Preset deleted: {pName}";
                RebuildPresets();
            }
        };

        presetRow.AddChild(presetPicker);
        presetRow.AddChild(presetNameInput);
        presetRow.AddChild(saveBtn);
        presetRow.AddChild(applyBtn);
        presetRow.AddChild(delBtn);
        container.AddChild(presetRow);

        RebuildPresets();
    }

    // ──────── Widget Helpers ────────

    private static void AddStatRow(VBoxContainer parent, string label, string value)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        var lbl = new Label { Text = label, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        lbl.AddThemeFontSizeOverride("font_size", 12);
        row.AddChild(lbl);
        var val = new Label { Text = value, HorizontalAlignment = HorizontalAlignment.Right };
        val.AddThemeFontSizeOverride("font_size", 12);
        row.AddChild(val);
        parent.AddChild(row);
    }

    private static void AddIntEditor(VBoxContainer parent, string label, int currentValue, Action<int> onApply)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(80, 0) });
        var spin = new SpinBox { MinValue = -999, MaxValue = 9999, Value = currentValue, Step = 1, CustomMinimumSize = new Vector2(70, 26) };
        row.AddChild(spin);
        var btn = new Button { Text = I18N.T("cardEdit.apply", "Set"), CustomMinimumSize = new Vector2(36, 26) };
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

    private static void AddTextEditor(VBoxContainer parent, string label, string currentValue, Action<string> onApply)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(80, 0) });
        var input = new LineEdit { Text = currentValue ?? string.Empty, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(input);
        var btn = new Button { Text = I18N.T("cardEdit.apply", "Set"), CustomMinimumSize = new Vector2(36, 26) };
        btn.Pressed += () => onApply(input.Text ?? string.Empty);
        row.AddChild(btn);
        parent.AddChild(row);
    }

    private static Button CreateActionButton(string text, Color bgColor)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 40),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        var style = new StyleBoxFlat
        {
            BgColor = bgColor,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 6, ContentMarginBottom = 6
        };
        var hover = new StyleBoxFlat
        {
            BgColor = bgColor.Lightened(0.15f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 6, ContentMarginBottom = 6
        };
        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus", style);
        btn.AddThemeFontSizeOverride("font_size", 14);
        return btn;
    }

    private static void ApplySmallActionStyle(Button btn, Color bgColor)
    {
        var s = new StyleBoxFlat
        {
            BgColor = bgColor,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 8, ContentMarginRight = 8,
            ContentMarginTop = 3, ContentMarginBottom = 3
        };
        var h = new StyleBoxFlat
        {
            BgColor = bgColor.Lightened(0.15f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 8, ContentMarginRight = 8,
            ContentMarginTop = 3, ContentMarginBottom = 3
        };
        btn.AddThemeStyleboxOverride("normal", s);
        btn.AddThemeStyleboxOverride("hover", h);
        btn.AddThemeStyleboxOverride("pressed", h);
        btn.AddThemeStyleboxOverride("focus", s);
        btn.AddThemeFontSizeOverride("font_size", 12);
    }

    private static PanelContainer CreateBrowserPanel()
    {
        var panel = new PanelContainer
        {
            Name = "BrowserPanel",
            MouseFilter = Control.MouseFilterEnum.Stop,
            AnchorLeft = 0, AnchorRight = 1,
            OffsetLeft = PanelLeft, OffsetRight = -PanelRight,
            AnchorTop = 0.15f, AnchorBottom = 0.85f,
            OffsetTop = 0, OffsetBottom = 0
        };

        var style = new StyleBoxFlat
        {
            BgColor = ColPanelBg,
            CornerRadiusTopLeft = 0, CornerRadiusBottomLeft = 0,
            CornerRadiusTopRight = RailRadius, CornerRadiusBottomRight = RailRadius,
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 12, ContentMarginBottom = 16,
            BorderWidthLeft = 0,
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthRight = 1,
            BorderColor = ColPanelBorder,
            ShadowColor = new Color(0, 0, 0, 0.40f),
            ShadowSize = 20
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var content = new VBoxContainer { Name = "Content" };
        content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.AddThemeConstantOverride("separation", 8);
        panel.AddChild(content);

        // Slide-right animation
        float finalLeft = PanelLeft;
        panel.Ready += () =>
        {
            float slideOffset = 60f;
            panel.OffsetLeft = finalLeft - slideOffset;
            panel.Modulate = new Color(1, 1, 1, 0);

            var tween = panel.CreateTween();
            tween.TweenProperty(panel, "offset_left", finalLeft, 0.25f)
                 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.Parallel()
                 .TweenProperty(panel, "modulate:a", 1f, 0.18f)
                 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        };

        return panel;
    }

    // Used by NGlobalUi to access the internal node name
    internal static readonly string NodeName = RootName;
}
