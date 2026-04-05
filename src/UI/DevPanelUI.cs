using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;

namespace DevMode.UI;

internal static class DevPanelUI
{
    private const string RootName   = "DevModeSidebarRoot";
    private const string TopBarName = "DevModeTopBar";
    private const float  PanelW     = 180f;
    private const float  TabW       = 24f;
    private const float  TabH       = 56f;

    private static ImageTexture? _arrowRight;
    private static ImageTexture? _arrowLeft;
    private static Action? _onRefreshPanel;

    public static void Attach(NGlobalUi globalUi, DevPanelActions actions)
    {
        if (((Node)globalUi).GetNodeOrNull<Control>(RootName) != null)
            return;

        _onRefreshPanel = actions.OnRefreshPanel;

        var root = new Control
        {
            Name        = RootName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex      = 1200
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // ── Drawer container (anchored to left edge, full height) ──
        var drawer = new Control
        {
            Name                = "Drawer",
            MouseFilter         = Control.MouseFilterEnum.Pass,
            AnchorLeft          = 0, AnchorRight  = 0,
            AnchorTop           = 0, AnchorBottom = 1,
            OffsetLeft          = -PanelW,
            OffsetRight         = TabW,
            CustomMinimumSize   = new Vector2(PanelW + TabW, 0)
        };
        root.AddChild(drawer);

        // ── Panel ──
        var panel = new PanelContainer
        {
            Name              = "DevModePanel",
            AnchorLeft        = 0, AnchorRight  = 0,
            AnchorTop         = 0, AnchorBottom = 1,
            OffsetLeft        = 0,
            OffsetRight       = PanelW,
        };
        var panelStyle = new StyleBoxFlat
        {
            BgColor              = new Color(0.08f, 0.08f, 0.10f, 0.95f),
            ContentMarginLeft    = 8, ContentMarginRight  = 8,
            ContentMarginTop     = 8, ContentMarginBottom = 8,
            CornerRadiusTopRight    = 0, CornerRadiusBottomRight = 0,
            CornerRadiusTopLeft     = 0, CornerRadiusBottomLeft  = 0,
            BorderWidthRight     = 1,
            BorderColor          = new Color(0.35f, 0.35f, 0.45f, 0.6f)
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        panel.MouseFilter = Control.MouseFilterEnum.Stop;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        vbox.AddChild(CreateButton("卡牌", actions.OnOpenCards));
        vbox.AddChild(CreateButton("遗物", actions.OnOpenRelics));
        vbox.AddChild(CreateButton("敌人", actions.OnOpenEnemies));
        vbox.AddChild(CreateSeparator());
        vbox.AddChild(CreateButton("存档", actions.OnOpenSave));
        vbox.AddChild(CreateButton("读档", actions.OnOpenLoad));

        // Game speed control
        vbox.AddChild(CreateSeparator());
        var gameSpeedBtn = CreatePlainButton($"速度: {actions.GetGameSpeedLabel()}");
        gameSpeedBtn.Pressed += () =>
        {
            actions.OnCycleGameSpeed();
            gameSpeedBtn.Text = $"速度: {actions.GetGameSpeedLabel()}";
        };
        vbox.AddChild(gameSpeedBtn);

        var skipAnimBtn = CreatePlainButton($"跳过动画: {actions.GetSkipAnimLabel()}");
        skipAnimBtn.Pressed += () =>
        {
            actions.OnToggleSkipAnim();
            skipAnimBtn.Text = $"跳过动画: {actions.GetSkipAnimLabel()}";
        };
        vbox.AddChild(skipAnimBtn);

        // AI control section
        if (actions.OnToggleAI != null)
        {
            vbox.AddChild(CreateSeparator());

            var aiBtn = CreatePlainButton("AI: 关闭");
            Button? stratBtn = null;
            Button? speedBtn = null;

            aiBtn.Pressed += () =>
            {
                actions.OnToggleAI();
                bool enabled = actions.IsAIEnabled?.Invoke() ?? false;
                aiBtn.Text = enabled ? "AI: 运行中" : "AI: 关闭";
                if (stratBtn != null) stratBtn.Visible = !enabled;
                if (speedBtn != null) speedBtn.Visible = !enabled;
            };
            vbox.AddChild(aiBtn);

            stratBtn = CreatePlainButton($"策略: {(actions.GetStrategyName?.Invoke() ?? "规则")}");
            stratBtn.Pressed += () =>
            {
                actions.OnCycleStrategy?.Invoke();
                stratBtn.Text = $"策略: {(actions.GetStrategyName?.Invoke() ?? "?")}";
            };
            vbox.AddChild(stratBtn);

            speedBtn = CreatePlainButton($"速度: {(actions.GetSpeedLabel?.Invoke() ?? "正常")}");
            speedBtn.Pressed += () =>
            {
                actions.OnCycleSpeed?.Invoke();
                speedBtn.Text = $"速度: {(actions.GetSpeedLabel?.Invoke() ?? "?")}";
            };
            vbox.AddChild(speedBtn);
        }

        panel.AddChild(vbox);
        drawer.AddChild(panel);

        // ── Arrow tab (sits at the right edge of the drawer, vertically centred) ──
        _arrowRight ??= CreateChevronTexture(true);
        _arrowLeft  ??= CreateChevronTexture(false);

        var tab = new Button
        {
            Name              = "DrawerTab",
            CustomMinimumSize = new Vector2(TabW, TabH),
            AnchorLeft        = 0, AnchorRight  = 0,
            AnchorTop         = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft        = PanelW,
            OffsetRight       = PanelW + TabW,
            OffsetTop         = -TabH / 2f,
            OffsetBottom      = TabH / 2f,
            MouseFilter       = Control.MouseFilterEnum.Stop
        };
        var tabStyle = new StyleBoxFlat
        {
            BgColor                  = new Color(0.15f, 0.15f, 0.18f, 0.92f),
            CornerRadiusTopRight     = 6, CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft      = 0, CornerRadiusBottomLeft  = 0,
            BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            BorderColor = new Color(0.35f, 0.35f, 0.45f, 0.6f)
        };
        tab.AddThemeStyleboxOverride("normal",   tabStyle);
        tab.AddThemeStyleboxOverride("hover",    tabStyle);
        tab.AddThemeStyleboxOverride("pressed",  tabStyle);
        tab.AddThemeStyleboxOverride("focus",    tabStyle);
        tab.FocusMode = Control.FocusModeEnum.None;
        tab.Icon = _arrowRight;
        tab.IconAlignment = HorizontalAlignment.Center;
        drawer.AddChild(tab);

        // ── Slide animation (hover-triggered) ──
        bool open = false;
        Tween? tween = null;
        SceneTreeTimer? closeTimer = null;

        void Slide(bool toOpen)
        {
            if (open == toOpen) return;
            open = toOpen;
            tab.Icon = open ? _arrowLeft : _arrowRight;

            tween?.Kill();
            tween = drawer.CreateTween();
            float target = open ? 0f : -PanelW;
            tween.TweenProperty(drawer, "offset_left",  target,         0.18f)
                 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.Parallel()
                 .TweenProperty(drawer, "offset_right", target + TabW,  0.18f)
                 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }

        void CancelClose()
        {
            if (closeTimer != null)
            {
                closeTimer.Timeout -= OnCloseTimeout;
                closeTimer = null;
            }
        }

        void OnCloseTimeout() => Slide(false);

        void ScheduleClose()
        {
            CancelClose();
            closeTimer = drawer.GetTree().CreateTimer(0.15);
            closeTimer.Timeout += OnCloseTimeout;
        }

        tab.MouseEntered    += () => { CancelClose(); Slide(true); };
        panel.MouseEntered  += CancelClose;
        tab.MouseExited     += ScheduleClose;
        panel.MouseExited   += ScheduleClose;

        // Prevent close when hovering child controls inside the panel
        foreach (var child in vbox.GetChildren())
        {
            if (child is Control ctrl)
            {
                ctrl.MouseEntered += CancelClose;
            }
        }

        ((Node)globalUi).AddChild(root);
    }

    public static void Detach(NGlobalUi globalUi)
    {
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
        RemoveTopBar(globalUi);
        _onRefreshPanel = null;
    }

    // ──────── Dynamic Top Bar ────────

    public static void UpdateTopBar(NGlobalUi globalUi, Func<CardTarget, bool>? cardTargetAvailable = null)
    {
        RemoveTopBar(globalUi);

        if (DevModeState.ActivePanel == ActivePanel.None)
            return;

        // Calculate bar width based on content
        bool isCardView   = DevModeState.ActivePanel == ActivePanel.Cards
            && DevModeState.CardMode == CardMode.View;
        bool showDuration = DevModeState.ActivePanel == ActivePanel.Cards
            && DevModeState.CardMode is CardMode.Add or CardMode.Upgrade or CardMode.Delete;
        float barHalfW = DevModeState.ActivePanel switch
        {
            ActivePanel.Cards   => isCardView ? 130 : showDuration ? 340 : 270,
            ActivePanel.Relics  => 110,
            ActivePanel.Enemies => Actions.CombatEnemyActions.GetCombatState() != null ? 340 : 220,
            _                   => 110
        };

        var bar = new HBoxContainer
        {
            Name        = TopBarName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex      = 1200,
            AnchorLeft  = 0.5f, AnchorRight  = 0.5f,
            AnchorTop   = 0,    AnchorBottom = 0,
            OffsetLeft  = -barHalfW, OffsetRight = barHalfW,
            OffsetTop   = 4,    OffsetBottom = 34
        };
        bar.AddThemeConstantOverride("separation", 0);

        if (DevModeState.ActivePanel == ActivePanel.Cards)
            BuildCardTopBar(bar, cardTargetAvailable);
        else if (DevModeState.ActivePanel == ActivePanel.Enemies)
            BuildEnemyTopBar(bar);
        else
            BuildRelicTopBar(bar);

        ((Node)globalUi).AddChild(bar);
    }

    private static void BuildCardTopBar(HBoxContainer bar, Func<CardTarget, bool>? cardTargetAvailable = null)
    {
        var modeLabels  = new[] { "图鉴", "添加", "升级", "删除" };
        var modes       = new[] { CardMode.View, CardMode.Add, CardMode.Upgrade, CardMode.Delete };
        var modeButtons = new Button[modeLabels.Length];

        bool showTargets  = DevModeState.CardMode is CardMode.Add or CardMode.Upgrade or CardMode.Delete;
        var targetLabels  = new[] { "手牌", "抽牌堆", "弃牌堆", "牌组" };
        var targets       = new[] { CardTarget.Hand, CardTarget.DrawPile, CardTarget.DiscardPile, CardTarget.Deck };
        var targetButtons = showTargets ? new Button[targetLabels.Length] : null;

        bool showDuration   = showTargets;
        var durationLabels  = new[] { "本场", "永久" };
        var durations       = new[] { EffectDuration.Temporary, EffectDuration.Permanent };
        var durationButtons = showDuration ? new Button[durationLabels.Length] : null;

        void Refresh()
        {
            for (int i = 0; i < modeButtons.Length; i++)
            {
                bool active = DevModeState.CardMode == modes[i];
                int corners = (i == 0 ? 1 : 0) | (i == modeButtons.Length - 1 ? 2 : 0);
                ApplyToggleStyle(modeButtons[i], active, corners);
            }
            if (targetButtons != null)
            {
                for (int i = 0; i < targetButtons.Length; i++)
                {
                    bool available = cardTargetAvailable == null || cardTargetAvailable(targets[i]);
                    bool active    = available && DevModeState.CardTarget == targets[i];
                    int  corners   = (i == 0 ? 1 : 0) | (i == targetButtons.Length - 1 ? 2 : 0);
                    targetButtons[i].Disabled = !available;
                    if (available)
                        ApplyToggleStyle(targetButtons[i], active, corners);
                    else
                        ApplyDisabledStyle(targetButtons[i], corners);
                }
            }
            if (durationButtons != null)
            {
                for (int i = 0; i < durationButtons.Length; i++)
                {
                    bool active = DevModeState.EffectDuration == durations[i];
                    int corners = (i == 0 ? 1 : 0) | (i == durationButtons.Length - 1 ? 2 : 0);
                    ApplyToggleStyle(durationButtons[i], active, corners);
                }
            }
        }

        // Mode buttons
        for (int i = 0; i < modeLabels.Length; i++)
        {
            int idx = i;
            var btn = CreateToggleButton(modeLabels[idx]);
            btn.Pressed += () =>
            {
                DevModeState.CardMode = modes[idx];
                Refresh();
                _onRefreshPanel?.Invoke();
            };
            modeButtons[i] = btn;
            bar.AddChild(btn);
        }

        // Target + Duration buttons — only for Upgrade / Delete modes
        if (showTargets && targetButtons != null)
        {
            bar.AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });

            for (int i = 0; i < targetLabels.Length; i++)
            {
                int idx = i;
                var btn = CreateToggleButton(targetLabels[idx]);
                btn.Pressed += () =>
                {
                    DevModeState.CardTarget = targets[idx];
                    Refresh();
                    _onRefreshPanel?.Invoke();
                };
                targetButtons[i] = btn;
                bar.AddChild(btn);
            }
        }

        // Duration buttons (only for Upgrade/Delete modes)
        if (showDuration && durationButtons != null)
        {
            bar.AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });

            for (int i = 0; i < durationLabels.Length; i++)
            {
                int idx = i;
                var btn = CreateToggleButton(durationLabels[idx]);
                btn.Pressed += () =>
                {
                    DevModeState.EffectDuration = durations[idx];
                    Refresh();
                };
                durationButtons[i] = btn;
                bar.AddChild(btn);
            }
        }

        Refresh();
    }

    private static void BuildRelicTopBar(HBoxContainer bar)
    {
        var labels  = new[] { "图鉴", "添加", "删除" };
        var modes   = new[] { RelicMode.View, RelicMode.Add, RelicMode.Delete };
        var buttons = new Button[labels.Length];

        void Refresh()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                bool active = DevModeState.RelicMode == modes[i];
                int corners = (i == 0 ? 1 : 0) | (i == buttons.Length - 1 ? 2 : 0);
                ApplyToggleStyle(buttons[i], active, corners);
            }
        }

        for (int i = 0; i < labels.Length; i++)
        {
            int idx = i;
            var btn = CreateToggleButton(labels[idx]);
            btn.Pressed += () =>
            {
                DevModeState.RelicMode = modes[idx];
                Refresh();
                _onRefreshPanel?.Invoke();
            };
            buttons[i] = btn;
            bar.AddChild(btn);
        }

        Refresh();
    }

    private static void BuildEnemyTopBar(HBoxContainer bar)
    {
        // Left group: encounter override modes
        var labels = new[] { "全局", "按类型", "按楼层", "关闭" };
        var modes  = new[] { EnemyMode.Global, EnemyMode.PerType, EnemyMode.Off, EnemyMode.Off };
        var buttons = new Button[labels.Length];

        void Refresh()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                bool active;
                if (i == 2) // 按楼层 — active when there are floor overrides
                    active = DevModeState.FloorOverrides.Count > 0;
                else if (i == 3) // 关闭
                    active = DevModeState.EnemyMode == EnemyMode.Off && DevModeState.FloorOverrides.Count == 0;
                else
                    active = DevModeState.EnemyMode == modes[i];
                int corners = (i == 0 ? 1 : 0) | (i == buttons.Length - 1 ? 2 : 0);
                ApplyToggleStyle(buttons[i], active, corners);
            }
        }

        for (int i = 0; i < labels.Length; i++)
        {
            int idx = i;
            var btn = CreateToggleButton(labels[idx]);
            btn.Pressed += () =>
            {
                if (idx == 3) // 关闭 = clear all
                {
                    DevModeState.ClearEnemyOverrides();
                    Refresh();
                    return;
                }
                if (idx == 2) // 按楼层
                {
                    _onRefreshPanel?.Invoke();
                    return;
                }
                DevModeState.EnemyMode = modes[idx];
                Refresh();
                _onRefreshPanel?.Invoke();
            };
            buttons[i] = btn;
            bar.AddChild(btn);
        }

        // Right group: combat actions (add monster / kill enemy)
        bool inCombat = Actions.CombatEnemyActions.GetCombatState() != null;
        if (inCombat)
        {
            bar.AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });

            var addBtn = CreateToggleButton("添加怪物");
            ApplyToggleStyle(addBtn, false, 1); // left corners
            addBtn.Pressed += () => _onRefreshPanel?.Invoke(); // handled by DevPanel
            addBtn.SetMeta("combat_action", "add");
            bar.AddChild(addBtn);

            var killBtn = CreateToggleButton("击杀敌人");
            ApplyToggleStyle(killBtn, false, 2); // right corners
            killBtn.Pressed += () =>
            {
                // Signal to DevPanel to open kill picker
                DevModeState.ActivePanel = ActivePanel.Enemies;
                _onCombatKill?.Invoke();
            };
            bar.AddChild(killBtn);
        }

        Refresh();
    }

    // Combat kill callback — set by DevPanel
    private static Action? _onCombatKill;
    public static void SetCombatKillCallback(Action? callback) => _onCombatKill = callback;

    // ──────── Helpers ────────

    private static void RemoveTopBar(NGlobalUi globalUi)
    {
        var old = ((Node)globalUi).GetNodeOrNull<Control>(TopBarName);
        if (old != null)
        {
            ((Node)globalUi).RemoveChild(old);
            old.QueueFree();
        }
    }

    private static Button CreateToggleButton(string text)
    {
        return new Button
        {
            Text                = text,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode           = Control.FocusModeEnum.None,
            MouseFilter         = Control.MouseFilterEnum.Stop
        };
    }

    private static void ApplyDisabledStyle(Button btn, int cornerFlags)
    {
        var s = new StyleBoxFlat
        {
            BgColor           = new Color(0.08f, 0.08f, 0.10f, 0.4f),
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop  = 4,  ContentMarginBottom = 4,
            BorderWidthTop    = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderColor       = new Color(0.25f, 0.25f, 0.30f, 0.4f),
            CornerRadiusTopLeft     = (cornerFlags & 1) != 0 ? 6 : 0,
            CornerRadiusBottomLeft  = (cornerFlags & 1) != 0 ? 6 : 0,
            CornerRadiusTopRight    = (cornerFlags & 2) != 0 ? 6 : 0,
            CornerRadiusBottomRight = (cornerFlags & 2) != 0 ? 6 : 0
        };
        foreach (var state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
            btn.AddThemeStyleboxOverride(state, s);
        btn.AddThemeColorOverride("font_disabled_color", new Color(0.4f, 0.4f, 0.45f, 0.6f));
    }

    private static void ApplyToggleStyle(Button btn, bool active, int cornerFlags)
    {
        var s = new StyleBoxFlat
        {
            BgColor           = active ? new Color(0.25f, 0.4f, 0.6f, 0.9f) : new Color(0.12f, 0.12f, 0.15f, 0.85f),
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop  = 4,  ContentMarginBottom = 4,
            BorderWidthTop    = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderColor       = active ? new Color(0.5f, 0.7f, 0.9f, 0.8f) : new Color(0.35f, 0.35f, 0.45f, 0.6f),
            CornerRadiusTopLeft     = (cornerFlags & 1) != 0 ? 6 : 0,
            CornerRadiusBottomLeft  = (cornerFlags & 1) != 0 ? 6 : 0,
            CornerRadiusTopRight    = (cornerFlags & 2) != 0 ? 6 : 0,
            CornerRadiusBottomRight = (cornerFlags & 2) != 0 ? 6 : 0
        };
        btn.AddThemeStyleboxOverride("normal",  s);
        btn.AddThemeStyleboxOverride("hover",   s);
        btn.AddThemeStyleboxOverride("pressed", s);
        btn.AddThemeStyleboxOverride("focus",   s);
    }

    private static Button CreateButton(string text, Action action)
    {
        var btn = CreatePlainButton(text);
        btn.Pressed += action;
        return btn;
    }

    /// <summary>Sidebar button without an initial <see cref="Button.Pressed"/> handler (Godot rejects null callables).</summary>
    private static Button CreatePlainButton(string text)
    {
        return new Button { Text = text, CustomMinimumSize = new Vector2(0, 40) };
    }

    private static HSeparator CreateSeparator()
    {
        var sep = new HSeparator();
        sep.AddThemeConstantOverride("separation", 8);
        return sep;
    }

    private static ImageTexture CreateChevronTexture(bool pointRight)
    {
        const int w = 12, h = 20;
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        var col = new Color(0.85f, 0.85f, 0.9f);

        int tipX  = pointRight ? w - 3 : 2;
        int baseX = pointRight ? 2 : w - 3;
        int midY  = h / 2;

        DrawThickLine(img, baseX, 2, tipX, midY, col, 2);
        DrawThickLine(img, tipX, midY, baseX, h - 3, col, 2);

        return ImageTexture.CreateFromImage(img);
    }

    private static void DrawThickLine(Image img, int x0, int y0, int x1, int y1, Color col, int thickness)
    {
        int iw = img.GetWidth(), ih = img.GetHeight();
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        int half = thickness / 2;

        while (true)
        {
            for (int ox = -half; ox <= half; ox++)
                for (int oy = -half; oy <= half; oy++)
                {
                    int px = x0 + ox, py = y0 + oy;
                    if (px >= 0 && px < iw && py >= 0 && py < ih)
                        img.SetPixel(px, py, col);
                }
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
}

internal sealed class DevPanelActions
{
    public required Action OnOpenCards     { get; init; }
    public required Action OnOpenRelics   { get; init; }
    public required Action OnOpenEnemies  { get; init; }
    public required Action OnOpenSave     { get; init; }
    public required Action OnOpenLoad     { get; init; }
    public required Action OnRefreshPanel { get; init; }

    // AI control (optional — null if STS2AI mod not available)
    public Action? OnToggleAI       { get; init; }
    public Action? OnCycleStrategy  { get; init; }
    public Action? OnCycleSpeed     { get; init; }
    public Func<bool>? IsAIEnabled  { get; init; }
    public Func<string>? GetStrategyName { get; init; }
    public Func<string>? GetSpeedLabel   { get; init; }

    // Game speed control
    public required Action OnCycleGameSpeed   { get; init; }
    public required Func<string> GetGameSpeedLabel { get; init; }

    // Skip card animation control
    public required Action OnToggleSkipAnim    { get; init; }
    public required Func<string> GetSkipAnimLabel { get; init; }
}
