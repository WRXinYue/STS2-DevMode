using System;
using Godot;

namespace DevMode.UI;

/// <summary>
/// Full-screen overlay for saving or loading save slots.
/// Slot 0 = Quick Save (special, no rename).  Slots 1-N = normal slots.
/// Left panel: slot list.  Right panel: details + rename + confirm.
/// </summary>
internal static class SaveSlotUI
{
    private const string RootName = "SaveSlotUIRoot";

    private static Control? _root;
    private static ColorRect? _bg;
    private static HBoxContainer? _center;
    private static Action<int>? _onConfirm;
    private static bool _isSaveMode;
    private static bool _showQuickSlot;
    private static int _selectedSlot;
    private static Tween? _rightTween;

    private static Label?    _detailName;
    private static Label?    _detailTime;
    private static Label?    _detailFloor;
    private static Label?    _detailHp;
    private static Label?    _detailGold;
    private static Label?    _detailCards;
    private static Label?    _detailRelics;
    private static LineEdit? _nameInput;
    private static Label?    _nameLabel;
    private static Button?   _confirmBtn;
    private static Control?  _rightPanel;

    private static readonly Button[] _slotBtns = new Button[SaveSlotManager.SlotCount + 1];

    // ──────── Public API ────────

    public static void Show(Node parent, bool saveMode, Action<int> onConfirm, bool showQuickSlot = true)
    {
        parent.GetNodeOrNull<Control>(RootName)?.QueueFree();

        _isSaveMode     = saveMode;
        _showQuickSlot  = showQuickSlot;
        _onConfirm      = onConfirm;
        _selectedSlot   = showQuickSlot ? 0 : 1;

        _root = BuildUI();
        parent.AddChild(_root);

        PlayEnterAnim();
        SelectSlot(_selectedSlot);
    }

    public static void Hide() => PlayExitAnim();

    // ──────── Animations ────────

    private static void PlayEnterAnim()
    {
        if (_root == null || _center == null || _bg == null) return;

        // Backdrop: fade in
        _bg.Color = new Color(0, 0, 0, 0f);
        var bgTween = _root.CreateTween();
        bgTween.TweenProperty(_bg, "color", new Color(0, 0, 0, 0.75f), 0.18f)
               .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        // Panel: scale + fade in
        _center.Scale   = new Vector2(0.93f, 0.93f);
        _center.Modulate = new Color(1, 1, 1, 0f);
        var panelTween = _root.CreateTween().SetParallel();
        panelTween.TweenProperty(_center, "scale",    Vector2.One,            0.18f)
                  .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        panelTween.TweenProperty(_center, "modulate", Colors.White,           0.14f)
                  .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    }

    private static void PlayExitAnim()
    {
        if (_root == null || !GodotObject.IsInstanceValid(_root)) return;

        var root = _root; // capture before null
        _root = null;

        var tween = root.CreateTween().SetParallel();
        tween.TweenProperty(root, "modulate", new Color(1, 1, 1, 0f), 0.12f)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        if (_center != null)
            tween.TweenProperty(_center, "scale", new Vector2(0.95f, 0.95f), 0.12f)
                 .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

        tween.Chain().TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(root))
                root.QueueFree();
        }));
    }

    /// <summary>Animate right panel content out → update text → animate back in.</summary>
    private static void AnimateDetailTransition(Action updateContent)
    {
        if (_rightPanel == null || !GodotObject.IsInstanceValid(_rightPanel))
        {
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

    private static void AnimateConfirmButton()
    {
        if (_confirmBtn == null || !GodotObject.IsInstanceValid(_confirmBtn)) return;

        var tween = _confirmBtn.CreateTween().SetParallel();
        tween.TweenProperty(_confirmBtn, "scale", new Vector2(0.88f, 0.88f), 0.06f)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.Chain().SetParallel();
        tween.TweenProperty(_confirmBtn, "scale", Vector2.One, 0.14f)
             .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    // ──────── Construction ────────

    private static Control BuildUI()
    {
        var root = new Control { Name = RootName, ZIndex = 200 };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Stop;

        _bg = new ColorRect { Color = new Color(0, 0, 0, 0.75f) };
        _bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(_bg);

        // CenterContainer fills root and automatically centers its child regardless of size timing
        var wrapper = new CenterContainer();
        wrapper.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(wrapper);

        _center = new HBoxContainer();
        _center.AddThemeConstantOverride("separation", 0);
        _center.CustomMinimumSize = new Vector2(700, 460);
        wrapper.AddChild(_center);

        _center.AddChild(BuildLeftPanel());
        _rightPanel = BuildRightPanel();
        _center.AddChild(_rightPanel);

        return root;
    }

    private static PanelContainer BuildLeftPanel()
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(190, 460) };
        panel.AddThemeStyleboxOverride("panel", MakePanel(8, 0, 0, 8));

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);

        var title = new Label
        {
            Text = _isSaveMode ? I18N.T("snapshot.titleSave", "SAVE") : I18N.T("snapshot.titleLoad", "LOAD"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color("FFF6E2"));
        vbox.AddChild(title);
        vbox.AddChild(HSep());

        if (_showQuickSlot)
        {
            var quickBtn = new Button
            {
                Text = QuickSlotLabel(),
                CustomMinimumSize = new Vector2(162, 70),
                ToggleMode = true
            };
            quickBtn.AddThemeFontSizeOverride("font_size", 14);
            quickBtn.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.3f));
            quickBtn.Pressed += () => SelectSlot(0);
            quickBtn.GuiInput += evt => OnSlotGuiInput(evt, 0);
            _slotBtns[0] = quickBtn;
            vbox.AddChild(quickBtn);
            vbox.AddChild(HSep());
        }

        for (int i = 1; i <= SaveSlotManager.SlotCount; i++)
        {
            int slot = i;
            var btn = new Button
            {
                Text = SlotLabel(slot),
                CustomMinimumSize = new Vector2(162, 70),
                ToggleMode = true
            };
            btn.AddThemeFontSizeOverride("font_size", 14);
            btn.Pressed += () => SelectSlot(slot);
            btn.GuiInput += evt => OnSlotGuiInput(evt, slot);
            _slotBtns[slot] = btn;
            vbox.AddChild(btn);
        }

        vbox.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });

        var cancelBtn = new Button { Text = I18N.T("snapshot.cancel", "Cancel"), CustomMinimumSize = new Vector2(0, 40) };
        cancelBtn.Pressed += Hide;
        vbox.AddChild(cancelBtn);

        panel.AddChild(vbox);
        return panel;
    }

    private static PanelContainer BuildRightPanel()
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(510, 460) };
        panel.AddThemeStyleboxOverride("panel", MakePanel(0, 8, 8, 0, new Color(0.08f, 0.08f, 0.10f, 0.98f)));

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);

        var headerRow = new HBoxContainer();
        _detailName = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _detailName.AddThemeFontSizeOverride("font_size", 20);
        _detailName.AddThemeColorOverride("font_color", new Color("FFF6E2"));
        headerRow.AddChild(_detailName);

        _detailTime = new Label { HorizontalAlignment = HorizontalAlignment.Right };
        _detailTime.AddThemeFontSizeOverride("font_size", 14);
        _detailTime.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        headerRow.AddChild(_detailTime);
        vbox.AddChild(headerRow);

        vbox.AddChild(HSep());

        var statsRow = new HBoxContainer();
        statsRow.AddThemeConstantOverride("separation", 24);
        _detailFloor = StatLabel();
        _detailHp    = StatLabel();
        _detailGold  = StatLabel();
        statsRow.AddChild(_detailFloor);
        statsRow.AddChild(_detailHp);
        statsRow.AddChild(_detailGold);
        vbox.AddChild(statsRow);

        vbox.AddChild(HSep());
        vbox.AddChild(SectionHeader(I18N.T("snapshot.cards", "Cards")));

        _detailCards = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 60)
        };
        _detailCards.AddThemeFontSizeOverride("font_size", 12);
        _detailCards.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        vbox.AddChild(_detailCards);

        vbox.AddChild(SectionHeader(I18N.T("snapshot.relics", "Relics")));

        _detailRelics = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 40)
        };
        _detailRelics.AddThemeFontSizeOverride("font_size", 12);
        _detailRelics.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        vbox.AddChild(_detailRelics);

        vbox.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        vbox.AddChild(HSep());

        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", 8);

        _nameLabel = new Label { Text = I18N.T("snapshot.nameLabel", "Name:") };
        _nameLabel.AddThemeFontSizeOverride("font_size", 14);
        _nameLabel.AddThemeColorOverride("font_color", new Color("FFF6E2"));
        bottomRow.AddChild(_nameLabel);

        _nameInput = new LineEdit
        {
            PlaceholderText = I18N.T("snapshot.namePlaceholder", "optional name"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _nameInput.AddThemeFontSizeOverride("font_size", 14);
        bottomRow.AddChild(_nameInput);

        _confirmBtn = new Button
        {
            Text = _isSaveMode ? I18N.T("snapshot.confirmSave", "Save") : I18N.T("snapshot.confirmLoad", "Load"),
            CustomMinimumSize = new Vector2(80, 0),
            PivotOffset = new Vector2(40, 20)   // centre for scale anim
        };
        _confirmBtn.AddThemeFontSizeOverride("font_size", 16);
        _confirmBtn.Pressed += OnConfirmPressed;
        bottomRow.AddChild(_confirmBtn);

        vbox.AddChild(bottomRow);
        panel.AddChild(vbox);
        return panel;
    }

    // ──────── Interaction ────────

    private static void OnSlotGuiInput(InputEvent evt, int slot)
    {
        if (evt is InputEventMouseButton { DoubleClick: true, ButtonIndex: MouseButton.Left })
        {
            SelectSlot(slot);
            if (_confirmBtn is not { Disabled: true })
                OnConfirmPressed();
        }
    }

    private static void SelectSlot(int slot)
    {
        _selectedSlot = slot;

        for (int i = 0; i < _slotBtns.Length; i++)
        {
            var btn = _slotBtns[i];
            if (GodotObject.IsInstanceValid(btn))
                btn.ButtonPressed = (i == slot);
        }

        AnimateDetailTransition(() => RefreshDetail(slot));
    }

    private static void RefreshDetail(int slot)
    {
        bool isQuick = slot == 0;
        var meta = SaveSlotManager.LoadMeta(slot);
        bool empty = meta == null;

        if (_nameLabel != null) _nameLabel.Visible = !isQuick;
        if (_nameInput != null) _nameInput.Visible = !isQuick;
        if (_confirmBtn != null)
            _confirmBtn.Disabled = !_isSaveMode && empty;

        if (empty)
        {
            SetDetail(
                isQuick ? I18N.T("snapshot.quickSave", "⚡ Quick Save") : I18N.T("snapshot.emptySlot", "Empty Slot"),
                "",
                I18N.T("snapshot.floorDash", "Floor —"),
                I18N.T("snapshot.hpDash", "HP —"),
                I18N.T("snapshot.goldDash", "Gold —"),
                "", "");
            if (_nameInput != null) _nameInput.Text = "";
            return;
        }

        SetDetail(
            isQuick ? I18N.T("snapshot.quickSave", "⚡ Quick Save") : meta!.DisplayName,
            meta!.FormattedTime,
            I18N.T("snapshot.floor", "Floor {0}", meta.TotalFloor),
            I18N.T("snapshot.hp", "HP  {0} / {1}", meta.Hp, meta.MaxHp),
            I18N.T("snapshot.gold", "Gold  {0}", meta.Gold),
            string.Join("  ", meta.CardTitles),
            string.Join("  ", meta.RelicTitles)
        );

        if (_nameInput != null)
            _nameInput.Text = meta.Name;
    }

    private static void SetDetail(string name, string time, string floor, string hp, string gold, string cards, string relics)
    {
        if (_detailName  != null) _detailName.Text  = name;
        if (_detailTime  != null) _detailTime.Text  = time;
        if (_detailFloor != null) _detailFloor.Text = floor;
        if (_detailHp    != null) _detailHp.Text    = hp;
        if (_detailGold  != null) _detailGold.Text  = gold;
        if (_detailCards != null) _detailCards.Text = cards;
        if (_detailRelics != null) _detailRelics.Text = relics;
    }

    private static void OnConfirmPressed()
    {
        AnimateConfirmButton();

        bool isQuick = _selectedSlot == 0;
        if (_isSaveMode && !isQuick)
            SaveSlotManager.RenameSlot(_selectedSlot, _nameInput?.Text ?? "");

        _onConfirm?.Invoke(_selectedSlot);

        if (_isSaveMode)
        {
            AnimateDetailTransition(() => RefreshDetail(_selectedSlot));
            if (_slotBtns[_selectedSlot] is { } btn && GodotObject.IsInstanceValid(btn))
                btn.Text = isQuick ? QuickSlotLabel() : SlotLabel(_selectedSlot);
        }
        else
        {
            Hide();
        }
    }

    // ──────── Helpers ────────

    private static string QuickSlotLabel()
    {
        var meta = SaveSlotManager.LoadMeta(0);
        var qs = I18N.T("snapshot.quickSave", "⚡ Quick Save");
        var empty = I18N.T("snapshot.empty", "[empty]");
        return meta == null ? $"{qs}\n{empty}" : $"{qs}\n{meta.FormattedTime}";
    }

    private static string SlotLabel(int slot)
    {
        var meta = SaveSlotManager.LoadMeta(slot);
        var slotStr = I18N.T("snapshot.slot", "Slot {0}", slot);
        var empty = I18N.T("snapshot.empty", "[empty]");
        return meta == null ? $"{slotStr}\n{empty}" : $"{slotStr}  {meta.DisplayName}\n{meta.FormattedTime}";
    }

    private static Label SectionHeader(string text)
    {
        var lbl = new Label { Text = text };
        lbl.AddThemeFontSizeOverride("font_size", 13);
        lbl.AddThemeColorOverride("font_color", new Color("FFF6E2"));
        return lbl;
    }

    private static Label StatLabel()
    {
        var lbl = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        lbl.AddThemeFontSizeOverride("font_size", 14);
        lbl.AddThemeColorOverride("font_color", new Color("FFF6E2"));
        return lbl;
    }

    private static HSeparator HSep() => new();

    private static StyleBoxFlat MakePanel(
        float tl = 8, float tr = 8, float br = 8, float bl = 8, Color? color = null)
    {
        return new StyleBoxFlat
        {
            BgColor = color ?? new Color(0.12f, 0.12f, 0.15f, 0.98f),
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 16,  ContentMarginBottom = 16,
            CornerRadiusTopLeft     = (int)tl,
            CornerRadiusTopRight    = (int)tr,
            CornerRadiusBottomRight = (int)br,
            CornerRadiusBottomLeft  = (int)bl,
            BorderWidthBottom = 1, BorderWidthTop  = 1,
            BorderWidthLeft   = 1, BorderWidthRight = 1,
            BorderColor = new Color(0.35f, 0.35f, 0.45f, 0.7f)
        };
    }
}
