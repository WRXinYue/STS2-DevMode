using System;
using System.Collections.Generic;
using Godot;

namespace DevMode.UI;

/// <summary>
/// Full-screen overlay for saving or loading save slots.
/// Dynamic slot list — users can add and delete slots freely.
/// Left panel: scrollable rich slot cards.  Right panel: detail + actions.
/// </summary>
internal static class SaveSlotUI {
    private const string RootName = "DevModeSaveSlot";

    // ── State ──
    private static Control? _root;
    private static ColorRect? _bg;
    private static HBoxContainer? _center;
    private static Action<int>? _onConfirm;
    private static bool _isSaveMode;
    private static int _selectedSlot = -1;
    private static Tween? _rightTween;

    // ── Left panel ──
    private static VBoxContainer? _slotList;
    private static ScrollContainer? _slotScroll;
    private static readonly Dictionary<int, PanelContainer> _slotCards = new();

    // ── Right panel ──
    private static Control? _rightPanel;
    private static VBoxContainer? _detailContainer;  // all detail content (toggled as a group)
    private static Control? _placeholderPanel;
    private static Label? _detailName;
    private static Label? _detailTime;
    private static Label? _detailFloor;
    private static Label? _detailHp;
    private static Label? _detailGold;
    private static Label? _detailSeed;
    private static Label? _detailCards;
    private static Label? _detailRelics;
    private static Label? _detailMods;
    private static LineEdit? _nameInput;
    private static Label? _nameLabel;
    private static Button? _confirmBtn;
    private static Button? _deleteBtn;

    // ──────── Public API ────────

    public static void Show(Node parent, bool saveMode, Action<int> onConfirm) {
        // Clear stale static references BEFORE QueueFree to avoid ObjectDisposedException
        ClearReferences();
        parent.GetNodeOrNull<Control>(RootName)?.QueueFree();

        _isSaveMode = saveMode;
        _onConfirm = onConfirm;
        _selectedSlot = -1;
        _slotCards.Clear();

        _root = BuildUI();
        parent.AddChild(_root);

        PlayEnterAnim();

        // Auto-select first slot if any exist
        var ids = SaveSlotManager.GetAllSlotIds();
        if (ids.Count > 0)
            SelectSlot(ids[0]);
    }

    public static void Hide() => PlayExitAnim();

    private static void ClearReferences() {
        _root = null; _bg = null; _center = null;
        _rightPanel = null; _detailContainer = null; _placeholderPanel = null;
        _slotList = null; _slotScroll = null;
        _detailName = null; _detailTime = null;
        _detailFloor = null; _detailHp = null; _detailGold = null;
        _detailSeed = null; _detailCards = null; _detailRelics = null; _detailMods = null;
        _nameInput = null; _nameLabel = null; _confirmBtn = null; _deleteBtn = null;
        _rightTween = null;
    }

    // ──────── Animations ────────

    private static void PlayEnterAnim() {
        if (_root == null || _center == null || _bg == null) return;

        _bg.Color = new Color(0, 0, 0, 0f);
        var bgTween = _root.CreateTween();
        bgTween.TweenProperty(_bg, "color", new Color(0, 0, 0, 0.75f), 0.18f)
               .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        _center.Scale = new Vector2(0.93f, 0.93f);
        _center.Modulate = new Color(1, 1, 1, 0f);
        var panelTween = _root.CreateTween().SetParallel();
        panelTween.TweenProperty(_center, "scale", Vector2.One, 0.18f)
                  .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        panelTween.TweenProperty(_center, "modulate", Colors.White, 0.14f)
                  .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    }

    private static void PlayExitAnim() {
        if (_root == null || !GodotObject.IsInstanceValid(_root)) return;

        var root = _root;
        _root = null;

        var tween = root.CreateTween().SetParallel();
        tween.TweenProperty(root, "modulate", new Color(1, 1, 1, 0f), 0.12f)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        if (_center != null)
            tween.TweenProperty(_center, "scale", new Vector2(0.95f, 0.95f), 0.12f)
                 .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

        tween.Chain().TweenCallback(Callable.From(() => {
            if (GodotObject.IsInstanceValid(root))
                root.QueueFree();
        }));
    }

    private static void AnimateDetailTransition(Action updateContent) {
        if (_rightPanel == null || !GodotObject.IsInstanceValid(_rightPanel)) {
            updateContent();
            return;
        }

        _rightTween?.Kill();
        _rightTween = _rightPanel.CreateTween();

        _rightTween.TweenProperty(_rightPanel, "modulate", new Color(1, 1, 1, 0f), 0.07f)
                   .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        _rightTween.TweenCallback(Callable.From(updateContent));
        _rightTween.TweenProperty(_rightPanel, "modulate", Colors.White, 0.10f)
                   .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    }

    private static void AnimateConfirmButton() {
        if (_confirmBtn == null || !GodotObject.IsInstanceValid(_confirmBtn)) return;

        var tween = _confirmBtn.CreateTween().SetParallel();
        tween.TweenProperty(_confirmBtn, "scale", new Vector2(0.88f, 0.88f), 0.06f)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.Chain().SetParallel();
        tween.TweenProperty(_confirmBtn, "scale", Vector2.One, 0.14f)
             .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    // ──────── Construction ────────

    private static Control BuildUI() {
        var root = new Control { Name = RootName, ZIndex = 200 };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Stop;

        _bg = new ColorRect { Color = new Color(0, 0, 0, 0.75f) };
        _bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(_bg);

        var wrapper = new CenterContainer();
        wrapper.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(wrapper);

        _center = new HBoxContainer();
        _center.AddThemeConstantOverride("separation", 0);
        _center.CustomMinimumSize = new Vector2(860, 520);
        wrapper.AddChild(_center);

        _center.AddChild(BuildLeftPanel());
        _rightPanel = BuildRightPanel();
        _center.AddChild(_rightPanel);

        return root;
    }

    // ══════════════════════════════════════════════════════════════
    //  LEFT PANEL — slot card list
    // ══════════════════════════════════════════════════════════════

    private static PanelContainer BuildLeftPanel() {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(260, 520) };
        panel.AddThemeStyleboxOverride("panel", MakePanel(10, 0, 0, 10));

        var outerVbox = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        outerVbox.AddThemeConstantOverride("separation", 8);

        // Header row: title + mode badge
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 8);

        var title = new Label {
            Text = _isSaveMode
                ? I18N.T("snapshot.titleSave", "SAVE")
                : I18N.T("snapshot.titleLoad", "LOAD"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        headerRow.AddChild(title);
        outerVbox.AddChild(headerRow);

        outerVbox.AddChild(MakeThinSep());

        // Scrollable slot list
        _slotScroll = new ScrollContainer {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };

        _slotList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _slotList.AddThemeConstantOverride("separation", 6);

        RebuildSlotList();

        _slotScroll.AddChild(_slotList);
        outerVbox.AddChild(_slotScroll);

        // "+ New Slot" button (save mode only shows this, load mode also shows it dimmed)
        var addBtn = new Button {
            Text = I18N.T("snapshot.newSlot", "+ New Slot"),
            CustomMinimumSize = new Vector2(0, 36),
            FocusMode = Control.FocusModeEnum.None,
        };
        addBtn.AddThemeFontSizeOverride("font_size", 13);
        addBtn.Pressed += OnAddSlotPressed;
        if (!_isSaveMode) addBtn.Disabled = true;
        outerVbox.AddChild(addBtn);

        // Cancel button
        var cancelBtn = new Button {
            Text = I18N.T("snapshot.cancel", "Cancel"),
            CustomMinimumSize = new Vector2(0, 36),
            FocusMode = Control.FocusModeEnum.None,
        };
        cancelBtn.AddThemeFontSizeOverride("font_size", 13);
        cancelBtn.Pressed += Hide;
        outerVbox.AddChild(cancelBtn);

        panel.AddChild(outerVbox);
        return panel;
    }

    /// <summary>Rebuilds all slot cards in the left-panel list from disk.</summary>
    private static void RebuildSlotList() {
        if (_slotList == null) return;

        foreach (var child in _slotList.GetChildren())
            ((Node)child).QueueFree();
        _slotCards.Clear();

        var ids = SaveSlotManager.GetAllSlotIds();

        if (ids.Count == 0 && _isSaveMode) {
            // In save mode with no slots, automatically create a new one
            ids.Add(SaveSlotManager.NextSlotId());
        }

        foreach (var id in ids) {
            var card = BuildSlotCard(id);
            _slotList.AddChild(card);
            _slotCards[id] = card;
        }
    }

    private static PanelContainer BuildSlotCard(int slotId) {
        var meta = SaveSlotManager.LoadMeta(slotId);
        bool empty = meta == null;

        var card = new PanelContainer {
            CustomMinimumSize = new Vector2(228, 0),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };

        var normalStyle = MakeSlotCardStyle(false);
        var hoverStyle = MakeSlotCardStyle(false, hover: true);
        card.AddThemeStyleboxOverride("panel", normalStyle);

        // Hover effects
        card.MouseEntered += () => {
            if (_selectedSlot != slotId)
                card.AddThemeStyleboxOverride("panel", hoverStyle);
        };
        card.MouseExited += () => {
            if (_selectedSlot != slotId)
                card.AddThemeStyleboxOverride("panel", normalStyle);
        };

        // Click to select
        card.GuiInput += evt => {
            if (evt is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) {
                SelectSlot(slotId);
            }
            if (evt is InputEventMouseButton { DoubleClick: true, ButtonIndex: MouseButton.Left }) {
                SelectSlot(slotId);
                if (_confirmBtn is not { Disabled: true })
                    OnConfirmPressed();
            }
        };

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);

        if (empty) {
            // Empty slot card
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_top", 8);
            margin.AddThemeConstantOverride("margin_bottom", 8);

            var emptyLabel = new Label {
                Text = I18N.T("snapshot.empty", "(empty)"),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            emptyLabel.AddThemeFontSizeOverride("font_size", 13);
            emptyLabel.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
            margin.AddChild(emptyLabel);
            vbox.AddChild(margin);
        }
        else {
            // Top row: name + time
            var topRow = new HBoxContainer();
            var nameLabel = new Label {
                Text = meta!.DisplayName,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ClipText = true,
            };
            nameLabel.AddThemeFontSizeOverride("font_size", 14);
            nameLabel.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
            topRow.AddChild(nameLabel);

            var timeLabel = new Label {
                Text = meta.FormattedTime,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            timeLabel.AddThemeFontSizeOverride("font_size", 11);
            timeLabel.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
            topRow.AddChild(timeLabel);
            vbox.AddChild(topRow);

            // Stats row: Floor / HP
            var statsRow = new HBoxContainer();
            statsRow.AddThemeConstantOverride("separation", 12);

            var floorLabel = new Label {
                Text = I18N.T("snapshot.floorShort", "F{0}", meta.TotalFloor),
            };
            floorLabel.AddThemeFontSizeOverride("font_size", 12);
            floorLabel.AddThemeColorOverride("font_color", DevModeTheme.TextSecondary);
            statsRow.AddChild(floorLabel);

            var hpLabel = new Label {
                Text = I18N.T("snapshot.hpShort", "{0}/{1}", meta.Hp, meta.MaxHp),
            };
            hpLabel.AddThemeFontSizeOverride("font_size", 12);
            hpLabel.AddThemeColorOverride("font_color", HpColor(meta.Hp, meta.MaxHp));
            statsRow.AddChild(hpLabel);

            var goldLabel = new Label {
                Text = I18N.T("snapshot.goldShort", "{0}g", meta.Gold),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            goldLabel.AddThemeFontSizeOverride("font_size", 12);
            goldLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.3f));
            statsRow.AddChild(goldLabel);
            vbox.AddChild(statsRow);

            // HP bar
            var hpBar = BuildMiniHpBar(meta.Hp, meta.MaxHp);
            vbox.AddChild(hpBar);
        }

        card.AddChild(vbox);
        return card;
    }

    private static Control BuildMiniHpBar(int hp, int maxHp) {
        float ratio = maxHp > 0 ? Mathf.Clamp((float)hp / maxHp, 0f, 1f) : 0f;

        var container = new Control { CustomMinimumSize = new Vector2(0, 4) };

        // Background track
        var bg = new ColorRect {
            Color = new Color(0.2f, 0.2f, 0.22f),
            AnchorRight = 1f,
            AnchorBottom = 1f,
        };
        container.AddChild(bg);

        // Fill
        var fill = new ColorRect {
            Color = HpColor(hp, maxHp),
            AnchorRight = ratio,
            AnchorBottom = 1f,
        };
        container.AddChild(fill);

        return container;
    }

    private static StyleBoxFlat MakeSlotCardStyle(bool selected, bool hover = false) {
        var bg = selected
            ? new Color(DevModeTheme.Accent.R, DevModeTheme.Accent.G, DevModeTheme.Accent.B, 0.15f)
            : hover
                ? new Color(0.18f, 0.18f, 0.22f, 0.9f)
                : new Color(0.13f, 0.13f, 0.16f, 0.8f);

        var border = selected
            ? DevModeTheme.Accent
            : hover
                ? new Color(0.4f, 0.4f, 0.5f, 0.6f)
                : new Color(0.25f, 0.25f, 0.3f, 0.5f);

        return new StyleBoxFlat {
            BgColor = bg,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            BorderWidthLeft = selected ? 3 : 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderColor = border,
        };
    }

    // ══════════════════════════════════════════════════════════════
    //  RIGHT PANEL — detail view
    // ══════════════════════════════════════════════════════════════

    private static PanelContainer BuildRightPanel() {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(600, 520) };
        panel.AddThemeStyleboxOverride("panel", MakePanel(0, 10, 10, 0, new Color(0.07f, 0.07f, 0.09f, 0.98f)));

        var outerVbox = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        outerVbox.AddThemeConstantOverride("separation", 0);

        // ── Detail container (all content, toggled as a single unit) ──
        _detailContainer = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _detailContainer.AddThemeConstantOverride("separation", 8);

        // Fixed header
        var headerRow = new HBoxContainer();
        _detailName = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true };
        _detailName.AddThemeFontSizeOverride("font_size", 20);
        _detailName.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
        headerRow.AddChild(_detailName);

        _detailTime = new Label { HorizontalAlignment = HorizontalAlignment.Right };
        _detailTime.AddThemeFontSizeOverride("font_size", 13);
        _detailTime.AddThemeColorOverride("font_color", DevModeTheme.TextSecondary);
        headerRow.AddChild(_detailTime);
        _detailContainer.AddChild(headerRow);

        _detailContainer.AddChild(MakeThinSep());

        // Stats badges row
        var badgeRow = new HBoxContainer();
        badgeRow.AddThemeConstantOverride("separation", 10);

        _detailFloor = MakeBadgeLabel();
        _detailHp = MakeBadgeLabel();
        _detailGold = MakeBadgeLabel();
        badgeRow.AddChild(MakeBadge(_detailFloor, new Color(0.35f, 0.55f, 0.85f, 0.2f)));
        badgeRow.AddChild(MakeBadge(_detailHp, new Color(0.85f, 0.35f, 0.35f, 0.2f)));
        badgeRow.AddChild(MakeBadge(_detailGold, new Color(1f, 0.85f, 0.3f, 0.2f)));
        _detailContainer.AddChild(badgeRow);

        // Seed
        _detailSeed = new Label();
        _detailSeed.AddThemeFontSizeOverride("font_size", 12);
        _detailSeed.AddThemeColorOverride("font_color", DevModeTheme.TextSecondary);
        _detailContainer.AddChild(_detailSeed);

        _detailContainer.AddChild(MakeThinSep());

        // Scrollable detail body
        var scroll = new ScrollContainer {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };

        var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 6);

        body.AddChild(SectionHeader(I18N.T("snapshot.cards", "Cards")));
        _detailCards = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _detailCards.AddThemeFontSizeOverride("font_size", 12);
        _detailCards.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
        body.AddChild(_detailCards);

        body.AddChild(SectionHeader(I18N.T("snapshot.relics", "Relics")));
        _detailRelics = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _detailRelics.AddThemeFontSizeOverride("font_size", 12);
        _detailRelics.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
        body.AddChild(_detailRelics);

        body.AddChild(SectionHeader(I18N.T("snapshot.mods", "Mods")));
        _detailMods = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _detailMods.AddThemeFontSizeOverride("font_size", 11);
        _detailMods.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        body.AddChild(_detailMods);

        scroll.AddChild(body);
        _detailContainer.AddChild(scroll);

        // Fixed footer
        _detailContainer.AddChild(MakeThinSep());

        var footerRow = new HBoxContainer();
        footerRow.AddThemeConstantOverride("separation", 8);

        _nameLabel = new Label { Text = I18N.T("snapshot.nameLabel", "Name:") };
        _nameLabel.AddThemeFontSizeOverride("font_size", 14);
        _nameLabel.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
        footerRow.AddChild(_nameLabel);

        _nameInput = new LineEdit {
            PlaceholderText = I18N.T("snapshot.namePlaceholder", "optional name"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _nameInput.AddThemeFontSizeOverride("font_size", 14);
        footerRow.AddChild(_nameInput);

        _confirmBtn = new Button {
            Text = _isSaveMode ? I18N.T("snapshot.confirmSave", "Save") : I18N.T("snapshot.confirmLoad", "Load"),
            CustomMinimumSize = new Vector2(80, 0),
            PivotOffset = new Vector2(40, 20),
            FocusMode = Control.FocusModeEnum.None,
        };
        _confirmBtn.AddThemeFontSizeOverride("font_size", 15);
        _confirmBtn.Pressed += OnConfirmPressed;
        footerRow.AddChild(_confirmBtn);

        _deleteBtn = new Button {
            Text = I18N.T("snapshot.delete", "Delete"),
            CustomMinimumSize = new Vector2(70, 0),
            FocusMode = Control.FocusModeEnum.None,
        };
        _deleteBtn.AddThemeFontSizeOverride("font_size", 13);
        _deleteBtn.AddThemeColorOverride("font_color", new Color(0.9f, 0.35f, 0.35f));
        _deleteBtn.Pressed += OnDeletePressed;
        footerRow.AddChild(_deleteBtn);

        _detailContainer.AddChild(footerRow);

        outerVbox.AddChild(_detailContainer);

        // ── Placeholder (shown when no slot selected) ──
        _placeholderPanel = new CenterContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var placeholderLabel = new Label {
            Text = I18N.T("snapshot.noSelection", "Select a save slot"),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        placeholderLabel.AddThemeFontSizeOverride("font_size", 16);
        placeholderLabel.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        _placeholderPanel.AddChild(placeholderLabel);
        outerVbox.AddChild(_placeholderPanel);

        // Start with detail hidden, placeholder visible
        SetDetailVisible(false);

        panel.AddChild(outerVbox);
        return panel;
    }

    /// <summary>Toggle between detail view and placeholder. No tree navigation needed.</summary>
    private static void SetDetailVisible(bool visible) {
        if (_detailContainer != null) _detailContainer.Visible = visible;
        if (_placeholderPanel != null) _placeholderPanel.Visible = !visible;
    }

    // ──────── Interaction ────────

    private static void SelectSlot(int slotId) {
        int prevSlot = _selectedSlot;
        _selectedSlot = slotId;

        // Update card styles
        foreach (var (id, card) in _slotCards) {
            if (!GodotObject.IsInstanceValid(card)) continue;
            card.AddThemeStyleboxOverride("panel", MakeSlotCardStyle(id == slotId));
        }

        SetDetailVisible(true);
        AnimateDetailTransition(() => RefreshDetail(slotId));
    }

    private static void RefreshDetail(int slotId) {
        var meta = SaveSlotManager.LoadMeta(slotId);
        bool empty = meta == null;

        if (_nameLabel != null) _nameLabel.Visible = _isSaveMode;
        if (_nameInput != null) _nameInput.Visible = _isSaveMode;
        if (_confirmBtn != null)
            _confirmBtn.Disabled = !_isSaveMode && empty;
        if (_deleteBtn != null) {
            _deleteBtn.Visible = !empty;
            _deleteBtn.Text = I18N.T("snapshot.delete", "Delete");
        }

        if (empty) {
            SetDetail(
                I18N.T("snapshot.emptySlot", "Empty Save"),
                "",
                I18N.T("snapshot.floorDash", "Floor —"),
                I18N.T("snapshot.hpDash", "HP —"),
                I18N.T("snapshot.goldDash", "Gold —"),
                "", "", "", "");
            if (_nameInput != null) _nameInput.Text = "";
            return;
        }

        var seedText = string.IsNullOrEmpty(meta!.Seed)
            ? ""
            : I18N.T("snapshot.seed", "Seed: {0}", meta.Seed);
        var modsText = meta.ModList.Count > 0
            ? string.Join("\n", meta.ModList)
            : I18N.T("snapshot.modsNone", "(none)");

        SetDetail(
            meta.DisplayName,
            meta.FormattedTime,
            I18N.T("snapshot.floor", "Floor {0}", meta.TotalFloor),
            I18N.T("snapshot.hp", "HP  {0} / {1}", meta.Hp, meta.MaxHp),
            I18N.T("snapshot.gold", "Gold  {0}", meta.Gold),
            seedText,
            modsText,
            string.Join("  ", meta.CardTitles),
            string.Join("  ", meta.RelicTitles)
        );

        if (_nameInput != null)
            _nameInput.Text = meta.Name;
    }

    private static void SetDetail(string name, string time, string floor, string hp, string gold,
        string seed, string mods, string cards, string relics) {
        if (_detailName != null) _detailName.Text = name;
        if (_detailTime != null) _detailTime.Text = time;
        if (_detailFloor != null) _detailFloor.Text = floor;
        if (_detailHp != null) _detailHp.Text = hp;
        if (_detailGold != null) _detailGold.Text = gold;
        if (_detailSeed != null) _detailSeed.Text = seed;
        if (_detailMods != null) _detailMods.Text = mods;
        if (_detailCards != null) _detailCards.Text = cards;
        if (_detailRelics != null) _detailRelics.Text = relics;
    }

    private static void OnConfirmPressed() {
        if (_selectedSlot < 0) return;

        AnimateConfirmButton();

        if (_isSaveMode)
            SaveSlotManager.RenameSlot(_selectedSlot, _nameInput?.Text ?? "");

        _onConfirm?.Invoke(_selectedSlot);

        if (_isSaveMode) {
            // Refresh both the card and detail after saving
            RebuildSlotList();
            HighlightSlotCard(_selectedSlot);
            AnimateDetailTransition(() => RefreshDetail(_selectedSlot));
        }
        else {
            Hide();
        }
    }

    private static void OnDeletePressed() {
        if (_selectedSlot < 0) return;
        if (_deleteBtn == null) return;

        // Two-click confirmation: first click changes text, second actually deletes
        if (_deleteBtn.Text == I18N.T("snapshot.deleteConfirm", "Confirm Delete")) {
            SaveSlotManager.DeleteSlot(_selectedSlot);
            _selectedSlot = -1;

            RebuildSlotList();
            SetDetailVisible(false);

            // Auto-select first remaining slot
            var ids = SaveSlotManager.GetAllSlotIds();
            if (ids.Count > 0)
                SelectSlot(ids[0]);
        }
        else {
            _deleteBtn.Text = I18N.T("snapshot.deleteConfirm", "Confirm Delete");
        }
    }

    private static void OnAddSlotPressed() {
        int newId = SaveSlotManager.NextSlotId();
        RebuildSlotList();

        // Ensure the new empty slot card appears
        if (!_slotCards.ContainsKey(newId)) {
            var card = BuildSlotCard(newId);
            _slotList?.AddChild(card);
            _slotCards[newId] = card;
        }

        SelectSlot(newId);

        // Scroll to bottom to show the new card
        if (_slotScroll != null)
            _slotScroll.CallDeferred("set_v_scroll", (int)_slotScroll.GetVScrollBar().MaxValue);
    }

    /// <summary>Re-apply the selected style to the given slot's card after a rebuild.</summary>
    private static void HighlightSlotCard(int slotId) {
        foreach (var (id, card) in _slotCards) {
            if (!GodotObject.IsInstanceValid(card)) continue;
            card.AddThemeStyleboxOverride("panel", MakeSlotCardStyle(id == slotId));
        }
    }

    // ──────── Widget helpers ────────

    private static Label SectionHeader(string text) {
        var lbl = new Label { Text = text };
        lbl.AddThemeFontSizeOverride("font_size", 13);
        lbl.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        return lbl;
    }

    private static Label MakeBadgeLabel() {
        var lbl = new Label();
        lbl.AddThemeFontSizeOverride("font_size", 13);
        lbl.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
        return lbl;
    }

    private static PanelContainer MakeBadge(Label label, Color bgColor) {
        var badge = new PanelContainer();
        var style = new StyleBoxFlat {
            BgColor = bgColor,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 2,
            ContentMarginBottom = 2,
        };
        badge.AddThemeStyleboxOverride("panel", style);
        badge.AddChild(label);
        return badge;
    }

    private static HSeparator MakeThinSep() => new();

    private static Color HpColor(int hp, int maxHp) {
        if (maxHp <= 0) return DevModeTheme.Subtle;
        float ratio = (float)hp / maxHp;
        if (ratio > 0.6f) return new Color(0.35f, 0.78f, 0.35f);
        if (ratio > 0.3f) return new Color(0.9f, 0.75f, 0.2f);
        return new Color(0.85f, 0.3f, 0.3f);
    }

    private static StyleBoxFlat MakePanel(
        float tl = 10, float tr = 10, float br = 10, float bl = 10, Color? color = null) {
        return new StyleBoxFlat {
            BgColor = color ?? new Color(0.10f, 0.10f, 0.13f, 0.98f),
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 16,
            ContentMarginBottom = 16,
            CornerRadiusTopLeft = (int)tl,
            CornerRadiusTopRight = (int)tr,
            CornerRadiusBottomRight = (int)br,
            CornerRadiusBottomLeft = (int)bl,
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderColor = new Color(0.3f, 0.3f, 0.38f, 0.6f),
        };
    }
}
