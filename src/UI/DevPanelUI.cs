using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using DevMode.Icons;

namespace DevMode.UI;

internal static class DevPanelUI
{
    private const string RootName        = "DevModeRailRoot";
    private const string TopBarName      = "DevModeTopBar";
    private const string OverlayName     = "DevModeOverlay";
    private const float  RailW           = 52f;
    private const float  IconBtnSize     = 36f;
    private const float  OverlayW        = 560f;
    private const int    Radius          = 14;

    private static Action? _onRefreshPanel;
    private static string? _activeOverlayId;
    private static int _pinRailCount;

    /// <summary>Pin the rail visible (e.g. while an external overlay is open). Call Unpin when done.</summary>
    public static void PinRail()  => _pinRailCount++;
    public static void UnpinRail() => _pinRailCount = Math.Max(0, _pinRailCount - 1);

    // ── Apple-style colour palette ──
    private static readonly Color ColRailBg       = new(0.10f, 0.10f, 0.12f, 0.88f);
    private static readonly Color ColRailBorder    = new(1f, 1f, 1f, 0.06f);
    private static readonly Color ColIconNormal    = new(0.62f, 0.62f, 0.68f);
    private static readonly Color ColIconHover     = new(0.85f, 0.85f, 0.92f);
    private static readonly Color ColIconActive    = new(0.40f, 0.68f, 1f);
    private static readonly Color ColIconActiveBg  = new(0.40f, 0.68f, 1f, 0.15f);
    private static readonly Color ColOverlayBg     = new(0.11f, 0.11f, 0.14f, 0.96f);
    private static readonly Color ColOverlayBorder = new(1f, 1f, 1f, 0.08f);
    private static readonly Color ColBackdrop      = new(0f, 0f, 0f, 0.50f);
    private static readonly Color ColSectionText   = new(0.50f, 0.50f, 0.58f);
    private static readonly Color ColSeparator     = new(1f, 1f, 1f, 0.06f);

    // ──────── Attach ────────
    public static void Attach(NGlobalUi globalUi, DevPanelActions actions)
    {
        if (((Node)globalUi).GetNodeOrNull<Control>(RootName) != null)
            return;

        _onRefreshPanel = actions.OnRefreshPanel;
        _activeOverlayId = null;

        var root = new Control
        {
            Name        = RootName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex      = 1200
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // ── Icon Rail (left edge, full height, rounded right corners) ──
        var rail = new PanelContainer
        {
            Name        = "Rail",
            MouseFilter = Control.MouseFilterEnum.Stop,
            AnchorLeft  = 0, AnchorRight  = 0,
            AnchorTop   = 0.15f, AnchorBottom = 0.85f,
            OffsetLeft  = 24, OffsetRight  = 24 + RailW,
            OffsetTop   = 0, OffsetBottom = 0
        };
        var railStyle = new StyleBoxFlat
        {
            BgColor                 = ColRailBg,
            CornerRadiusTopLeft     = Radius, CornerRadiusBottomLeft  = Radius,
            CornerRadiusTopRight    = Radius, CornerRadiusBottomRight = Radius,
            ContentMarginLeft       = 6, ContentMarginRight  = 6,
            ContentMarginTop        = 12, ContentMarginBottom = 12,
            BorderWidthRight        = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            BorderColor             = ColRailBorder,
            ShadowColor             = new Color(0, 0, 0, 0.25f),
            ShadowSize              = 8
        };
        rail.AddThemeStyleboxOverride("panel", railStyle);

        var railVBox = new VBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        railVBox.AddThemeConstantOverride("separation", 2);

        // ── Top group: action icons ──
        void AddIcon(MdiIcon icon, string tooltip, Action onClick)
        {
            var btn = CreateRailIcon(icon, tooltip);
            btn.Pressed += () =>
            {
                CloseAllOverlays(globalUi);
                onClick();
            };
            railVBox.AddChild(btn);
        }

        AddIcon(MdiIcon.Cards,       I18N.T("panel.cards", "Cards"),       actions.OnOpenCards);
        AddIcon(MdiIcon.Diamond,      I18N.T("panel.relics", "Relics"),     actions.OnOpenRelics);
        AddIcon(MdiIcon.Skull,        I18N.T("panel.enemies", "Enemies"),   actions.OnOpenEnemies);
        AddIcon(MdiIcon.Flash,        I18N.T("panel.powers", "Powers"),     actions.OnOpenPowers);
        AddIcon(MdiIcon.Potion,       I18N.T("panel.potions", "Potions"),   actions.OnOpenPotions);
        AddIcon(MdiIcon.CalendarStar, I18N.T("panel.events", "Events"),     actions.OnOpenEvents);
        AddIcon(MdiIcon.Pencil,       I18N.T("panel.cardEdit", "Card Editor"), actions.OnOpenCardEdit);
        AddIcon(MdiIcon.Console,      I18N.T("panel.console", "Console"),   actions.OnOpenConsole);
        AddIcon(MdiIcon.BookOpen,     I18N.T("panel.presets", "Presets"),    actions.OnOpenPresets);

        // ── Spacer ──
        railVBox.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });

        // ── Separator line ──
        var sep = new HSeparator();
        sep.AddThemeStyleboxOverride("separator", new StyleBoxFlat
        {
            BgColor = ColSeparator,
            ContentMarginTop = 0, ContentMarginBottom = 0,
            ContentMarginLeft = 4, ContentMarginRight = 4
        });
        sep.AddThemeConstantOverride("separation", 8);
        railVBox.AddChild(sep);

        // ── Bottom group: Save, Cheats, AI ──
        var saveBtn = CreateRailIcon(MdiIcon.ContentSave, I18N.T("panel.save", "Save / Load"));
        saveBtn.Pressed += () => { CloseAllOverlays(globalUi); ShowSaveLoadOverlay(globalUi, actions); };
        railVBox.AddChild(saveBtn);

        var cheatsBtn = CreateRailIcon(MdiIcon.Cog, I18N.T("panel.cheats", "Settings & Cheats"));
        cheatsBtn.Pressed += () => { CloseAllOverlays(globalUi); ShowCheatsOverlay(globalUi, actions); };
        railVBox.AddChild(cheatsBtn);

        if (actions.OnToggleAI != null)
        {
            var aiBtn = CreateRailIcon(MdiIcon.Robot, I18N.T("panel.ai", "AI Control"));
            aiBtn.Pressed += () => { CloseAllOverlays(globalUi); ShowAIOverlay(globalUi, actions); };
            railVBox.AddChild(aiBtn);
        }

        rail.AddChild(railVBox);
        root.AddChild(rail);

        // ── Peek tab (small arrow visible when rail is hidden) ──
        var peekTab = new Button
        {
            Name              = "RailPeekTab",
            CustomMinimumSize = new Vector2(14, 48),
            AnchorLeft        = 0, AnchorRight  = 0,
            AnchorTop         = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft        = 0, OffsetRight  = 14,
            OffsetTop         = -24, OffsetBottom = 24,
            FocusMode         = Control.FocusModeEnum.None,
            MouseFilter       = Control.MouseFilterEnum.Stop,
            IconAlignment     = HorizontalAlignment.Center,
            Icon              = MdiIcon.ChevronRight.Texture(12, ColIconNormal)
        };
        var peekStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.15f, 0.6f),
            CornerRadiusTopLeft = 0, CornerRadiusBottomLeft = 0,
            CornerRadiusTopRight = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 0, ContentMarginRight = 0,
            ContentMarginTop = 0, ContentMarginBottom = 0
        };
        peekTab.AddThemeStyleboxOverride("normal",  peekStyle);
        peekTab.AddThemeStyleboxOverride("hover",   peekStyle);
        peekTab.AddThemeStyleboxOverride("pressed", peekStyle);
        peekTab.AddThemeStyleboxOverride("focus",   peekStyle);
        root.AddChild(peekTab);

        // ── Auto-hide: timer-based mouse position polling ──
        float hiddenX  = -(24 + RailW);
        float visibleX = 24f;
        bool  railShown = false;
        Tween? railTween = null;

        // Start hidden
        rail.OffsetLeft  = hiddenX;
        rail.OffsetRight = hiddenX + RailW;
        rail.Modulate    = new Color(1, 1, 1, 0);

        void SlideRail(bool show)
        {
            if (railShown == show) return;
            railShown = show;

            railTween?.Kill();
            railTween = rail.CreateTween();

            float targetLeft  = show ? visibleX : hiddenX;
            float targetRight = targetLeft + RailW;
            float targetAlpha = show ? 1f : 0f;

            railTween.TweenProperty(rail, "offset_left",  targetLeft,  0.2f)
                     .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            railTween.Parallel()
                     .TweenProperty(rail, "offset_right", targetRight, 0.2f)
                     .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            railTween.Parallel()
                     .TweenProperty(rail, "modulate:a",   targetAlpha, 0.15f)
                     .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

            // Hide/show peek tab inversely
            peekTab.Visible = !show;
        }

        // Poll timer: check mouse position every 0.1s
        var pollTimer = new Timer
        {
            Name      = "RailPollTimer",
            WaitTime  = 0.1f,
            Autostart = true
        };
        // Hit zone: left 80px strip covering rail area + some margin
        float hitZoneRight = visibleX + RailW + 16f;

        pollTimer.Timeout += () =>
        {
            // Don't auto-hide while an overlay panel is open or rail is pinned
            if (_activeOverlayId != null || _pinRailCount > 0)
            {
                if (!railShown) SlideRail(true);
                return;
            }

            var mousePos = root.GetViewport().GetMousePosition();
            var railRect = rail.GetGlobalRect();
            bool inHitZone = mousePos.X < hitZoneRight
                          && mousePos.Y > railRect.Position.Y - 20
                          && mousePos.Y < railRect.End.Y + 20;
            bool overRail = railShown && railRect.Grow(8).HasPoint(mousePos);

            if (inHitZone || overRail)
                SlideRail(true);
            else if (railShown)
                SlideRail(false);
        };
        root.AddChild(pollTimer);

        // Clicking peek tab also shows rail
        peekTab.Pressed += () => SlideRail(true);

        ((Node)globalUi).AddChild(root);
    }

    // ──────── Detach ────────
    public static void Detach(NGlobalUi globalUi)
    {
        _activeOverlayId = null;
        _pinRailCount = 0;
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
        RemoveTopBar(globalUi);
        _onRefreshPanel = null;
    }

    // ──────── Close all known overlays (internal + external UIs) ────────
    private static readonly HashSet<string> _keepNodes = new() { RootName, TopBarName };

    /// <summary>
    /// Close the internal overlay (cheats/save/ai) and remove all DevMode external
    /// panels from globalUi.  Uses the "DevMode" naming convention so new panels are
    /// picked up automatically — no list to maintain.
    /// </summary>
    public static void CloseAllOverlays(NGlobalUi globalUi)
    {
        // Close our internal overlay (cheats/save/ai)
        CloseOverlay(globalUi);

        // Remove every DevMode child except the rail root and top bar
        var parent = (Node)globalUi;
        foreach (var child in parent.GetChildren())
        {
            if (child is Control ctrl
                && ctrl.Name.ToString().StartsWith("DevMode", StringComparison.Ordinal)
                && !_keepNodes.Contains(ctrl.Name))
            {
                parent.RemoveChild(ctrl);
                ctrl.QueueFree();
            }
        }
    }

    // ──────── Overlay: toggle / close ────────
    private static void ToggleOverlay(NGlobalUi globalUi, string id, Action<Control> buildContent)
    {
        if (_activeOverlayId == id)
        {
            CloseOverlay(globalUi);
            return;
        }

        CloseOverlay(globalUi);
        _activeOverlayId = id;

        // Panel lives inside the same root as Rail — same z-level, no backdrop blocking
        var root = ((Node)globalUi).GetNodeOrNull<Control>(RootName);
        if (root == null) return;

        // Clickaway: transparent, doesn't cover Rail area
        var clickaway = new Control
        {
            Name        = "OverlayClickaway",
            MouseFilter = Control.MouseFilterEnum.Stop,
            AnchorLeft  = 0, AnchorRight  = 1,
            AnchorTop   = 0, AnchorBottom = 1,
            OffsetLeft  = RailW + 32, OffsetRight = 0,
            OffsetTop   = 0, OffsetBottom = 0
        };
        clickaway.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true })
                CloseOverlay(globalUi);
        };
        // Insert before Rail so Rail stays on top
        root.AddChild(clickaway);
        root.MoveChild(clickaway, 0);

        // Panel — same vertical anchors as Rail, positioned to the right of Rail
        var panel = CreateOverlayPanel();
        panel.Name = OverlayName;
        root.AddChild(panel);

        var content = panel.GetNode<VBoxContainer>("Content");
        buildContent(content);
        // Animation is handled automatically by CreateStandardPanel's Ready callback
    }

    private static void CloseOverlay(NGlobalUi globalUi)
    {
        var root = ((Node)globalUi).GetNodeOrNull<Control>(RootName);
        if (root == null) { _activeOverlayId = null; return; }

        // Remove clickaway immediately
        var clickaway = root.GetNodeOrNull<Control>("OverlayClickaway");
        if (clickaway != null)
        {
            root.RemoveChild(clickaway);
            clickaway.QueueFree();
        }

        // Remove panel immediately (no lingering animation)
        var panel = root.GetNodeOrNull<PanelContainer>(OverlayName);
        if (panel != null)
        {
            root.RemoveChild(panel);
            panel.QueueFree();
        }
        _activeOverlayId = null;
    }

    // ──────── Overlay builders ────────
    private static void ShowCheatsOverlay(NGlobalUi globalUi, DevPanelActions actions)
    {
        ToggleOverlay(globalUi, "cheats", content =>
        {
            // Title
            var title = new Label
            {
                Text = I18N.T("panel.cheats", "Settings & Cheats"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            title.AddThemeFontSizeOverride("font_size", 16);
            title.AddThemeColorOverride("font_color", new Color(0.92f, 0.92f, 0.96f));
            content.AddChild(title);

            content.AddChild(CreateOverlaySeparator());

            // Scrollable area for all cheats
            var scroll = new ScrollContainer
            {
                SizeFlagsVertical    = Control.SizeFlags.ExpandFill,
                SizeFlagsHorizontal  = Control.SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
            };
            var vbox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            vbox.AddThemeConstantOverride("separation", 4);

            // ── Section: Player ──
            vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.player", "Player")));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.infiniteHp", "Infinite HP"),
                I18N.T("cheat.infiniteHp.desc", "Player cannot lose HP"),
                () => DevModeState.InfiniteHp, v => DevModeState.InfiniteHp = v));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.infiniteBlock", "Infinite Shield"),
                I18N.T("cheat.infiniteBlock.desc", "Block refills to 999 after loss"),
                () => DevModeState.InfiniteBlock, v => DevModeState.InfiniteBlock = v));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.infiniteEnergy", "Infinite Energy"),
                I18N.T("cheat.infiniteEnergy.desc", "Energy refills after spending"),
                () => DevModeState.InfiniteEnergy, v => DevModeState.InfiniteEnergy = v));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.infiniteStars", "Infinite Stars"),
                I18N.T("cheat.infiniteStars.desc", "Stars refill after spending"),
                () => DevModeState.InfiniteStars, v => DevModeState.InfiniteStars = v));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.alwaysPotion", "Always Reward Potion"), null,
                () => DevModeState.AlwaysRewardPotion, v => DevModeState.AlwaysRewardPotion = v));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.alwaysUpgrade", "Always Upgrade Reward"),
                I18N.T("cheat.alwaysUpgrade.desc", "Card rewards are always upgraded"),
                () => DevModeState.AlwaysUpgradeCardReward, v => DevModeState.AlwaysUpgradeCardReward = v));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.maxRarity", "Max Card Reward Rarity"),
                I18N.T("cheat.maxRarity.desc", "All card rewards are Rare"),
                () => DevModeState.MaxCardRewardRarity, v => DevModeState.MaxCardRewardRarity = v));
            vbox.AddChild(CreateCheatSlider(
                I18N.T("cheat.defenseMultiplier", "Defense Multiplier"),
                I18N.T("cheat.defenseMultiplier.desc", "Multiply block gained"),
                0, 10, 0.5f,
                () => DevModeState.DefenseMultiplier, v => DevModeState.DefenseMultiplier = v));

            // ── Section: Inventory ──
            vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.inventory", "Inventory")));
            vbox.AddChild(CreateCheatNumberEdit(
                I18N.T("cheat.editGold", "Edit Gold"), 0, 99999,
                () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.Gold; },
                v => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return; p.Gold = (int)v; }));
            vbox.AddChild(CreateCheatSlider(
                I18N.T("cheat.goldMultiplier", "Gold Multiplier"),
                I18N.T("cheat.goldMultiplier.desc", "Multiply gold gained"),
                0, 10, 0.5f,
                () => DevModeState.GoldMultiplier, v => DevModeState.GoldMultiplier = v));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.freeShop", "Free Shop"),
                I18N.T("cheat.freeShop.desc", "All shop purchases are free"),
                () => DevModeState.FreeShop, v => DevModeState.FreeShop = v));

            // ── Section: Status ──
            vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.status", "Status")));
            vbox.AddChild(CreateCheatNumberEdit(
                I18N.T("cheat.editEnergyCap", "Edit Energy Cap"), 0, 99,
                () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.MaxEnergy; },
                v => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return; p.MaxEnergy = (int)v; }));
            vbox.AddChild(CreateCheatNumberEdit(
                I18N.T("cheat.editPotionSlots", "Edit Potion Slots"), 0, 20,
                () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.MaxPotionCount; },
                v =>
                {
                    if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return;
                    int current = p.MaxPotionCount;
                    int diff = (int)v - current;
                    if (diff > 0) p.AddToMaxPotionCount(diff);
                    else if (diff < 0) p.SubtractFromMaxPotionCount(-diff);
                }));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.maxScore", "Max Score"),
                I18N.T("cheat.maxScore.desc", "Enable max score tracking"),
                () => DevModeState.MaxScore, v => DevModeState.MaxScore = v));
            vbox.AddChild(CreateCheatSlider(
                I18N.T("cheat.scoreMultiplier", "Score Multiplier"),
                I18N.T("cheat.scoreMultiplier.desc", "Multiply score gained"),
                0, 10, 0.5f,
                () => DevModeState.ScoreMultiplier, v => DevModeState.ScoreMultiplier = v));

            // ── Section: Enemy ──
            vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.enemy", "Enemy")));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.freezeEnemies", "Freeze Enemies"),
                I18N.T("cheat.freezeEnemies.desc", "Enemies skip their turns"),
                () => DevModeState.FreezeEnemies, v => DevModeState.FreezeEnemies = v));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.oneHitKill", "One-Hit Kill"),
                I18N.T("cheat.oneHitKill.desc", "Deal massive damage to enemies"),
                () => DevModeState.OneHitKill, v => DevModeState.OneHitKill = v));
            vbox.AddChild(CreateCheatSlider(
                I18N.T("cheat.damageMultiplier", "Damage Multiplier"),
                I18N.T("cheat.damageMultiplier.desc", "Multiply damage dealt to enemies"),
                0, 10, 0.5f,
                () => DevModeState.DamageMultiplier, v => DevModeState.DamageMultiplier = v));

            // ── Section: Game ──
            vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.game", "Game")));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("cheat.unknownTreasure", "Unknown → Treasure"),
                I18N.T("cheat.unknownTreasure.desc", "Unknown map nodes always give treasure"),
                () => DevModeState.UnknownMapAlwaysTreasure, v => DevModeState.UnknownMapAlwaysTreasure = v));
            vbox.AddChild(CreateCheatToggle(
                I18N.T("mapRewrite.enabled", "Enable Map Rewrite"), "",
                () => DevModeState.MapRewriteEnabled, v => DevModeState.MapRewriteEnabled = v));

            var mapModeBtn = CreatePlainButton(I18N.T("mapRewrite.mode", "Mode") + ": " + GetMapRewriteLabel(), MdiIcon.Map);
            mapModeBtn.Pressed += () =>
            {
                DevModeState.MapRewriteMode = DevModeState.MapRewriteMode switch
                {
                    MapRewriteMode.None     => MapRewriteMode.AllChest,
                    MapRewriteMode.AllChest  => MapRewriteMode.AllElite,
                    MapRewriteMode.AllElite  => MapRewriteMode.AllBoss,
                    MapRewriteMode.AllBoss   => MapRewriteMode.None,
                    _                        => MapRewriteMode.None
                };
                mapModeBtn.Text = I18N.T("mapRewrite.mode", "Mode") + ": " + GetMapRewriteLabel();
            };
            vbox.AddChild(mapModeBtn);

            vbox.AddChild(CreateCheatToggle(
                I18N.T("mapRewrite.keepFinalBoss", "Keep Final Boss"), "",
                () => DevModeState.MapKeepFinalBoss, v => DevModeState.MapKeepFinalBoss = v));

            var gameSpeedBtn = CreatePlainButton(I18N.T("panel.speed", "Speed: {0}", actions.GetGameSpeedLabel()), MdiIcon.SpeedometerMedium);
            gameSpeedBtn.Pressed += () =>
            {
                actions.OnCycleGameSpeed();
                gameSpeedBtn.Text = I18N.T("panel.speed", "Speed: {0}", actions.GetGameSpeedLabel());
            };
            vbox.AddChild(gameSpeedBtn);

            var skipAnimBtn = CreatePlainButton(I18N.T("panel.skipAnim", "Skip Anim: {0}", actions.GetSkipAnimLabel()), MdiIcon.AnimationPlay);
            skipAnimBtn.Pressed += () =>
            {
                actions.OnToggleSkipAnim();
                skipAnimBtn.Text = I18N.T("panel.skipAnim", "Skip Anim: {0}", actions.GetSkipAnimLabel());
            };
            vbox.AddChild(skipAnimBtn);

            // ── Section: Runtime Stats ──
            vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.runtime", "Runtime Stats")));
            vbox.AddChild(CreateRuntimeToggle(
                I18N.T("runtime.godMode", "God Mode"),
                I18N.T("runtime.godMode.desc", "Auto-heal to max HP every frame"),
                () => DevModeState.StatModifiers?.GodMode ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.GodMode = v; }));
            vbox.AddChild(CreateRuntimeToggle(
                I18N.T("runtime.killAll", "Kill All Enemies"),
                I18N.T("runtime.killAll.desc", "Continuously kill all enemies"),
                () => DevModeState.StatModifiers?.KillAllEnemies ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.KillAllEnemies = v; }));
            vbox.AddChild(CreateRuntimeToggle(
                I18N.T("runtime.infiniteEnergy", "Infinite Energy (Runtime)"),
                I18N.T("runtime.infiniteEnergy.desc", "Keep energy at 99+"),
                () => DevModeState.StatModifiers?.InfiniteEnergy ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.InfiniteEnergy = v; }));
            vbox.AddChild(CreateRuntimeToggle(
                I18N.T("runtime.alwaysPlayerTurn", "Always Player Turn"),
                I18N.T("runtime.alwaysPlayerTurn.desc", "Force combat to player turn"),
                () => DevModeState.StatModifiers?.AlwaysPlayerTurn ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.AlwaysPlayerTurn = v; }));
            vbox.AddChild(CreateRuntimeToggle(
                I18N.T("runtime.drawToLimit", "Draw to Hand Limit"),
                I18N.T("runtime.drawToLimit.desc", "Auto-draw to 10 cards"),
                () => DevModeState.StatModifiers?.DrawToHandLimit ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.DrawToHandLimit = v; }));
            vbox.AddChild(CreateRuntimeToggle(
                I18N.T("runtime.extraDraw", "Extra Draw Each Turn"),
                I18N.T("runtime.extraDraw.desc", "Draw extra cards at turn start"),
                () => DevModeState.StatModifiers?.ExtraDrawEachTurn ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.ExtraDrawEachTurn = v; }));
            vbox.AddChild(CreateCheatNumberEdit(
                I18N.T("runtime.extraDrawAmount", "Extra Draw Amount"), 1, 20,
                () => DevModeState.StatModifiers?.ExtraDrawEachTurnAmount ?? 1,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.ExtraDrawEachTurnAmount = (int)v; }));
            vbox.AddChild(CreateRuntimeToggle(
                I18N.T("runtime.autoAlly", "Auto-Act Friendly Monsters"),
                I18N.T("runtime.autoAlly.desc", "Auto-execute friendly monster turns"),
                () => DevModeState.StatModifiers?.AutoActFriendlyMonsters ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.AutoActFriendlyMonsters = v; }));
            vbox.AddChild(CreateRuntimeToggle(
                I18N.T("runtime.negateDebuffs", "Negate Debuffs"),
                I18N.T("runtime.negateDebuffs.desc", "Continuously remove all debuffs"),
                () => DevModeState.StatModifiers?.NegateDebuffs ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.NegateDebuffs = v; }));

            // ── Stat Locks ──
            vbox.AddChild(CreateSectionHeader(I18N.T("statLock.title", "Stat Locks")));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.gold", "Lock Gold"), 0, 99999,
                () => DevModeState.StatModifiers?.LockGold ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockGold = v; },
                () => DevModeState.StatModifiers?.LockedGoldValue ?? 0,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedGoldValue = (int)v; }));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.currentHp", "Lock Current HP"), 1, 9999,
                () => DevModeState.StatModifiers?.LockCurrentHp ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockCurrentHp = v; },
                () => DevModeState.StatModifiers?.LockedCurrentHpValue ?? 1,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedCurrentHpValue = (int)v; }));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.maxHp", "Lock Max HP"), 1, 9999,
                () => DevModeState.StatModifiers?.LockMaxHp ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockMaxHp = v; },
                () => DevModeState.StatModifiers?.LockedMaxHpValue ?? 1,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedMaxHpValue = (int)v; }));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.currentEnergy", "Lock Current Energy"), 0, 99,
                () => DevModeState.StatModifiers?.LockCurrentEnergy ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockCurrentEnergy = v; },
                () => DevModeState.StatModifiers?.LockedCurrentEnergyValue ?? 0,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedCurrentEnergyValue = (int)v; }));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.maxEnergy", "Lock Max Energy"), 1, 99,
                () => DevModeState.StatModifiers?.LockMaxEnergy ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockMaxEnergy = v; },
                () => DevModeState.StatModifiers?.LockedMaxEnergyValue ?? 1,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedMaxEnergyValue = (int)v; }));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.stars", "Lock Stars"), 0, 999,
                () => DevModeState.StatModifiers?.LockStars ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockStars = v; },
                () => DevModeState.StatModifiers?.LockedStarsValue ?? 0,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedStarsValue = (int)v; }));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.orbSlots", "Lock Orb Slots"), 0, 10,
                () => DevModeState.StatModifiers?.LockOrbSlots ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockOrbSlots = v; },
                () => DevModeState.StatModifiers?.LockedOrbSlotsValue ?? 0,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedOrbSlotsValue = (int)v; }));

            scroll.AddChild(vbox);
            content.AddChild(scroll);
        });
    }

    private static void ShowSaveLoadOverlay(NGlobalUi globalUi, DevPanelActions actions)
    {
        ToggleOverlay(globalUi, "saveload", content =>
        {
            var title = new Label
            {
                Text = I18N.T("panel.section.save", "Save / Load"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            title.AddThemeFontSizeOverride("font_size", 16);
            title.AddThemeColorOverride("font_color", new Color(0.92f, 0.92f, 0.96f));
            content.AddChild(title);

            content.AddChild(CreateOverlaySeparator());

            // Centered button group
            var btnBox = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter
            };
            btnBox.AddThemeConstantOverride("separation", 12);

            var saveBtn = CreateOverlayButton(I18N.T("panel.save", "Save"), MdiIcon.ContentSave);
            saveBtn.Pressed += () => { CloseOverlay(globalUi); actions.OnOpenSave(); };
            btnBox.AddChild(saveBtn);

            var loadBtn = CreateOverlayButton(I18N.T("panel.load", "Load"), MdiIcon.FolderOpen);
            loadBtn.Pressed += () => { CloseOverlay(globalUi); actions.OnOpenLoad(); };
            btnBox.AddChild(loadBtn);

            content.AddChild(btnBox);
        });
    }

    private static void ShowAIOverlay(NGlobalUi globalUi, DevPanelActions actions)
    {
        ToggleOverlay(globalUi, "ai", content =>
        {
            var title = new Label
            {
                Text = I18N.T("panel.section.ai", "AI Control"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            title.AddThemeFontSizeOverride("font_size", 16);
            title.AddThemeColorOverride("font_color", new Color(0.92f, 0.92f, 0.96f));
            content.AddChild(title);

            content.AddChild(CreateOverlaySeparator());

            var vbox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            vbox.AddThemeConstantOverride("separation", 8);

            var aiBtn = CreatePlainButton(I18N.T("panel.ai.off", "AI: Off"), MdiIcon.Robot);
            Button? stratBtn = null;
            Button? speedBtn = null;

            aiBtn.Pressed += () =>
            {
                actions.OnToggleAI!();
                bool enabled = actions.IsAIEnabled?.Invoke() ?? false;
                aiBtn.Text = enabled ? I18N.T("panel.ai.running", "AI: Running") : I18N.T("panel.ai.off", "AI: Off");
                if (stratBtn != null) stratBtn.Visible = !enabled;
                if (speedBtn != null) speedBtn.Visible = !enabled;
            };
            vbox.AddChild(aiBtn);

            stratBtn = CreatePlainButton(
                I18N.T("panel.ai.strategy", "Strategy: {0}", actions.GetStrategyName?.Invoke() ?? I18N.T("ai.strategy.rule", "Rule")),
                MdiIcon.Cog);
            stratBtn.Pressed += () =>
            {
                actions.OnCycleStrategy?.Invoke();
                stratBtn.Text = I18N.T("panel.ai.strategy", "Strategy: {0}", actions.GetStrategyName?.Invoke() ?? "?");
            };
            vbox.AddChild(stratBtn);

            speedBtn = CreatePlainButton(
                I18N.T("panel.ai.speed", "Speed: {0}", actions.GetSpeedLabel?.Invoke() ?? I18N.T("ai.speed.normal", "Normal")),
                MdiIcon.FastForward);
            speedBtn.Pressed += () =>
            {
                actions.OnCycleSpeed?.Invoke();
                speedBtn.Text = I18N.T("panel.ai.speed", "Speed: {0}", actions.GetSpeedLabel?.Invoke() ?? "?");
            };
            vbox.AddChild(speedBtn);

            content.AddChild(vbox);
        });
    }

    // ──────── Dynamic Top Bar (unchanged) ────────
    public static void UpdateTopBar(NGlobalUi globalUi, Func<CardTarget, bool>? cardTargetAvailable = null)
    {
        RemoveTopBar(globalUi);

        if (DevModeState.ActivePanel == ActivePanel.None)
            return;

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
        var modeLabels  = new[] { I18N.T("topbar.card.view","View"), I18N.T("topbar.card.add","Add"), I18N.T("topbar.card.upgrade","Upgrade"), I18N.T("topbar.card.delete","Delete") };
        var modes       = new[] { CardMode.View, CardMode.Add, CardMode.Upgrade, CardMode.Delete };
        var modeButtons = new Button[modeLabels.Length];

        bool showTargets  = DevModeState.CardMode is CardMode.Add or CardMode.Upgrade or CardMode.Delete;
        var targetLabels  = new[] { I18N.T("topbar.card.hand","Hand"), I18N.T("topbar.card.drawPile","Draw Pile"), I18N.T("topbar.card.discardPile","Discard"), I18N.T("topbar.card.deck","Deck") };
        var targets       = new[] { CardTarget.Hand, CardTarget.DrawPile, CardTarget.DiscardPile, CardTarget.Deck };
        var targetButtons = showTargets ? new Button[targetLabels.Length] : null;

        bool showDuration   = showTargets;
        var durationLabels  = new[] { I18N.T("topbar.card.temporary","Temp"), I18N.T("topbar.card.permanent","Perm") };
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
        var labels  = new[] { I18N.T("topbar.relic.view","View"), I18N.T("topbar.relic.add","Add"), I18N.T("topbar.relic.delete","Delete") };
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
        var labels = new[] { I18N.T("topbar.enemy.global","Global"), I18N.T("topbar.enemy.byType","By Type"), I18N.T("topbar.enemy.byFloor","By Floor"), I18N.T("topbar.enemy.off","Off") };
        var modes  = new[] { EnemyMode.Global, EnemyMode.PerType, EnemyMode.Off, EnemyMode.Off };
        var buttons = new Button[labels.Length];

        void Refresh()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                bool active;
                if (i == 2)
                    active = DevModeState.FloorOverrides.Count > 0;
                else if (i == 3)
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
                if (idx == 3)
                {
                    DevModeState.ClearEnemyOverrides();
                    Refresh();
                    return;
                }
                if (idx == 2)
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

        bool inCombat = Actions.CombatEnemyActions.GetCombatState() != null;
        if (inCombat)
        {
            bar.AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });

            var addBtn = CreateToggleButton(I18N.T("topbar.enemy.addMonster", "Add Monster"));
            ApplyToggleStyle(addBtn, false, 1);
            addBtn.Pressed += () => _onRefreshPanel?.Invoke();
            addBtn.SetMeta("combat_action", "add");
            bar.AddChild(addBtn);

            var killBtn = CreateToggleButton(I18N.T("topbar.enemy.killEnemy", "Kill Enemy"));
            ApplyToggleStyle(killBtn, false, 2);
            killBtn.Pressed += () =>
            {
                DevModeState.ActivePanel = ActivePanel.Enemies;
                _onCombatKill?.Invoke();
            };
            bar.AddChild(killBtn);
        }

        Refresh();
    }

    // Combat kill callback
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

    private static Button CreateRailIcon(MdiIcon icon, string tooltip)
    {
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(IconBtnSize, IconBtnSize),
            FocusMode         = Control.FocusModeEnum.None,
            MouseFilter       = Control.MouseFilterEnum.Stop,
            TooltipText       = tooltip,
            IconAlignment     = HorizontalAlignment.Center,
            Icon              = icon.Texture(20, ColIconNormal)
        };

        // Apple-style: subtle rounded bg, no border
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 0, ContentMarginRight = 0,
            ContentMarginTop = 0, ContentMarginBottom = 0
        };
        var hover = new StyleBoxFlat
        {
            BgColor = new Color(1f, 1f, 1f, 0.08f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 0, ContentMarginRight = 0,
            ContentMarginTop = 0, ContentMarginBottom = 0
        };
        btn.AddThemeStyleboxOverride("normal",  normal);
        btn.AddThemeStyleboxOverride("hover",   hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus",   normal);

        return btn;
    }

    private static void ApplyRailIconStyle(Button btn, bool active)
    {
        var bg = active ? ColIconActiveBg : new Color(0, 0, 0, 0);
        var s = new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
        };
        btn.AddThemeStyleboxOverride("normal", s);
        btn.Icon = btn.Icon; // force redraw
    }

    private static PanelContainer CreateOverlayPanel()
    {
        return CreateStandardPanel(OverlayW);
    }

    /// <summary>Shared panel factory for all DevMode overlays. Ensures consistent Apple-style appearance.
    /// Auto-plays slide-down animation when added to the scene tree.</summary>
    public static PanelContainer CreateStandardPanel(float width = 560f)
    {
        float halfW = width / 2f;
        var panel = new PanelContainer
        {
            Name        = "OverlayPanel",
            MouseFilter = Control.MouseFilterEnum.Stop,
            AnchorLeft  = 0.5f, AnchorRight  = 0.5f,
            OffsetLeft  = -halfW, OffsetRight = halfW,
            AnchorTop   = 0.15f, AnchorBottom = 0.85f,
            OffsetTop   = 0, OffsetBottom = 0
        };

        var style = new StyleBoxFlat
        {
            BgColor                 = ColOverlayBg,
            CornerRadiusTopLeft     = Radius, CornerRadiusTopRight    = Radius,
            CornerRadiusBottomLeft  = Radius, CornerRadiusBottomRight = Radius,
            ContentMarginLeft       = 24, ContentMarginRight  = 24,
            ContentMarginTop        = 20, ContentMarginBottom = 20,
            BorderWidthTop          = 1, BorderWidthBottom = 1,
            BorderWidthLeft         = 1, BorderWidthRight  = 1,
            BorderColor             = ColOverlayBorder,
            ShadowColor             = new Color(0, 0, 0, 0.40f),
            ShadowSize              = 20
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var content = new VBoxContainer { Name = "Content" };
        content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        content.SizeFlagsVertical   = Control.SizeFlags.ExpandFill;
        content.AddThemeConstantOverride("separation", 8);
        panel.AddChild(content);

        // Auto slide-down animation on enter
        panel.Ready += () =>
        {
            float slideOffset = 40f;
            panel.OffsetTop    -= slideOffset;
            panel.OffsetBottom -= slideOffset;
            panel.Modulate = new Color(1, 1, 1, 0);

            var tween = panel.CreateTween();
            tween.TweenProperty(panel, "offset_top", 0f, 0.22f)
                 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.Parallel()
                 .TweenProperty(panel, "offset_bottom", 0f, 0.22f)
                 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.Parallel()
                 .TweenProperty(panel, "modulate:a", 1f, 0.18f)
                 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        };

        return panel;
    }

    /// <summary>Shared backdrop for all DevMode overlays. Transparent click-to-close layer (no dimming). Auto-pins rail.
    /// Leaves the left Rail area uncovered so Rail remains clickable.</summary>
    public static ColorRect CreateStandardBackdrop(Action onClose)
    {
        bool closed = false;
        void SafeClose()
        {
            if (closed) return;
            closed = true;
            onClose();
        }

        // Offset left edge past the Rail so it doesn't block Rail clicks
        var backdrop = new ColorRect
        {
            Color       = new Color(0, 0, 0, 0),
            MouseFilter = Control.MouseFilterEnum.Stop,
            AnchorLeft  = 0, AnchorRight  = 1,
            AnchorTop   = 0, AnchorBottom = 1,
            OffsetLeft  = RailW + 32, OffsetRight = 0,
            OffsetTop   = 0, OffsetBottom = 0
        };

        // Pin rail while this backdrop exists
        PinRail();
        backdrop.TreeExited += UnpinRail;

        backdrop.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true })
                SafeClose();
        };

        return backdrop;
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

    private static Button CreateButton(string text, Action action, MdiIcon? icon = null)
    {
        var btn = CreatePlainButton(text, icon);
        btn.Pressed += action;
        return btn;
    }

    private static Button CreatePlainButton(string text, MdiIcon? icon = null)
    {
        var btn = new Button
        {
            Text              = text,
            CustomMinimumSize = new Vector2(0, 36),
            FocusMode         = Control.FocusModeEnum.None
        };
        if (icon is { } ic)
        {
            btn.Icon = ic.Texture(16);
            btn.IconAlignment = HorizontalAlignment.Left;
            btn.Alignment = HorizontalAlignment.Left;
        }
        // Apple-style rounded button
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(1f, 1f, 1f, 0.06f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 4, ContentMarginBottom = 4
        };
        var hover = new StyleBoxFlat
        {
            BgColor = new Color(1f, 1f, 1f, 0.10f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 4, ContentMarginBottom = 4
        };
        btn.AddThemeStyleboxOverride("normal",  normal);
        btn.AddThemeStyleboxOverride("hover",   hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus",   normal);
        btn.AddThemeFontSizeOverride("font_size", 13);
        return btn;
    }

    private static Button CreateOverlayButton(string text, MdiIcon icon)
    {
        var btn = new Button
        {
            Text              = text,
            CustomMinimumSize = new Vector2(200, 48),
            FocusMode         = Control.FocusModeEnum.None,
            Icon              = icon.Texture(20),
            IconAlignment     = HorizontalAlignment.Left,
            Alignment         = HorizontalAlignment.Center
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(1f, 1f, 1f, 0.08f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 8, ContentMarginBottom = 8
        };
        var hoverStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.40f, 0.68f, 1f, 0.18f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 8, ContentMarginBottom = 8
        };
        btn.AddThemeStyleboxOverride("normal",  style);
        btn.AddThemeStyleboxOverride("hover",   hoverStyle);
        btn.AddThemeStyleboxOverride("pressed", hoverStyle);
        btn.AddThemeStyleboxOverride("focus",   style);
        btn.AddThemeFontSizeOverride("font_size", 14);
        return btn;
    }

    private static HSeparator CreateOverlaySeparator()
    {
        var sep = new HSeparator();
        sep.AddThemeStyleboxOverride("separator", new StyleBoxFlat
        {
            BgColor = ColSeparator,
            ContentMarginTop = 0, ContentMarginBottom = 0,
            ContentMarginLeft = 0, ContentMarginRight = 0
        });
        sep.AddThemeConstantOverride("separation", 4);
        return sep;
    }

    private static Control CreateSectionHeader(string text)
    {
        var container = new HBoxContainer();
        container.AddThemeConstantOverride("separation", 8);

        var line1 = new HSeparator
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter
        };
        line1.AddThemeStyleboxOverride("separator", new StyleBoxFlat
        {
            BgColor = ColSeparator,
            ContentMarginTop = 0, ContentMarginBottom = 0,
            ContentMarginLeft = 0, ContentMarginRight = 0
        });
        line1.AddThemeConstantOverride("separation", 1);

        var label = new Label
        {
            Text                = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeColorOverride("font_color", ColSectionText);

        var line2 = new HSeparator
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter
        };
        line2.AddThemeStyleboxOverride("separator", new StyleBoxFlat
        {
            BgColor = ColSeparator,
            ContentMarginTop = 0, ContentMarginBottom = 0,
            ContentMarginLeft = 0, ContentMarginRight = 0
        });
        line2.AddThemeConstantOverride("separation", 1);

        container.AddChild(line1);
        container.AddChild(label);
        container.AddChild(line2);
        return container;
    }

    private static Control CreateCheatToggle(string label, string? tooltip, Func<bool> getter, Action<bool> setter)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        row.CustomMinimumSize = new Vector2(0, 30);

        var lbl = new Label
        {
            Text = label,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ClipText = true
        };
        lbl.AddThemeFontSizeOverride("font_size", 12);
        if (tooltip != null) lbl.TooltipText = tooltip;
        row.AddChild(lbl);

        string onText  = I18N.T("cheat.off", "Off");
        string offText = I18N.T("cheat.on", "On");

        var offBtn = new Button { Text = onText, CustomMinimumSize = new Vector2(36, 26), FocusMode = Control.FocusModeEnum.None };
        var onBtn  = new Button { Text = offText, CustomMinimumSize = new Vector2(36, 26), FocusMode = Control.FocusModeEnum.None };

        void Refresh()
        {
            bool active = getter();
            ApplyToggleStyle(offBtn, !active, 1);
            ApplyToggleStyle(onBtn,  active,  2);
        }

        offBtn.Pressed += () => { setter(false); Refresh(); };
        onBtn.Pressed  += () => { setter(true);  Refresh(); };

        row.AddChild(offBtn);
        row.AddChild(onBtn);
        Refresh();
        return row;
    }

    private static Control CreateCheatSlider(string label, string? tooltip, float min, float max, float step,
        Func<float> getter, Action<float> setter)
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 2);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);

        var lbl = new Label { Text = label, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true };
        lbl.AddThemeFontSizeOverride("font_size", 12);
        if (tooltip != null) lbl.TooltipText = tooltip;
        row.AddChild(lbl);

        var valLabel = new Label { Text = getter().ToString("0.#"), CustomMinimumSize = new Vector2(28, 0) };
        valLabel.AddThemeFontSizeOverride("font_size", 12);
        valLabel.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(valLabel);

        col.AddChild(row);

        var slider = new HSlider
        {
            MinValue = min, MaxValue = max, Step = step,
            Value = getter(),
            CustomMinimumSize = new Vector2(0, 20),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        slider.ValueChanged += v =>
        {
            setter((float)v);
            valLabel.Text = ((float)v).ToString("0.#");
        };
        col.AddChild(slider);
        return col;
    }

    private static Control CreateCheatNumberEdit(string label, int min, int max, Func<int> getter, Action<int> setter)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        row.CustomMinimumSize = new Vector2(0, 30);

        var lbl = new Label { Text = label, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true };
        lbl.AddThemeFontSizeOverride("font_size", 12);
        row.AddChild(lbl);

        var minusBtn = new Button { CustomMinimumSize = new Vector2(26, 26), FocusMode = Control.FocusModeEnum.None };
        minusBtn.Icon = MdiIcon.Minus.Texture(14);
        row.AddChild(minusBtn);

        var spinBox = new SpinBox
        {
            MinValue = min, MaxValue = max, Step = 1,
            Value = getter(),
            CustomMinimumSize = new Vector2(50, 26),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            Alignment = HorizontalAlignment.Center
        };
        row.AddChild(spinBox);

        var plusBtn = new Button { CustomMinimumSize = new Vector2(26, 26), FocusMode = Control.FocusModeEnum.None };
        plusBtn.Icon = MdiIcon.Plus.Texture(14);
        row.AddChild(plusBtn);

        var applyBtn = new Button { CustomMinimumSize = new Vector2(26, 26), FocusMode = Control.FocusModeEnum.None };
        applyBtn.Icon = MdiIcon.Check.Texture(14);
        var applyStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.5f, 0.4f, 0.9f),
            ContentMarginLeft = 4, ContentMarginRight = 4,
            ContentMarginTop = 2, ContentMarginBottom = 2,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        };
        applyBtn.AddThemeStyleboxOverride("normal", applyStyle);
        applyBtn.AddThemeStyleboxOverride("hover", applyStyle);
        applyBtn.AddThemeStyleboxOverride("pressed", applyStyle);
        row.AddChild(applyBtn);

        minusBtn.Pressed += () => spinBox.Value = Math.Max(min, spinBox.Value - 1);
        plusBtn.Pressed  += () => spinBox.Value = Math.Min(max, spinBox.Value + 1);
        applyBtn.Pressed += () => setter((int)spinBox.Value);

        row.VisibilityChanged += () =>
        {
            if (row.Visible) spinBox.Value = getter();
        };

        return row;
    }

    private static Control CreateRuntimeToggle(string label, string? tooltip, Func<bool> getter, Action<bool> setter)
    {
        return CreateCheatToggle(label, tooltip, getter, setter);
    }

    private static Control CreateStatLockRow(string label, int min, int max,
        Func<bool> lockGetter, Action<bool> lockSetter,
        Func<int> valueGetter, Action<int> valueSetter)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        row.CustomMinimumSize = new Vector2(0, 30);

        var check = new CheckBox { Text = label, ButtonPressed = lockGetter() };
        check.AddThemeFontSizeOverride("font_size", 12);
        check.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        check.ClipText = true;
        row.AddChild(check);

        var spinBox = new SpinBox
        {
            MinValue = min, MaxValue = max, Step = 1,
            Value = valueGetter(),
            CustomMinimumSize = new Vector2(70, 26),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            Alignment = HorizontalAlignment.Center
        };
        row.AddChild(spinBox);

        check.Toggled += v => lockSetter(v);
        spinBox.ValueChanged += v => valueSetter((int)v);

        row.VisibilityChanged += () =>
        {
            if (row.Visible)
            {
                check.ButtonPressed = lockGetter();
                spinBox.Value = valueGetter();
            }
        };

        return row;
    }

    private static string GetMapRewriteLabel()
    {
        return DevModeState.MapRewriteMode switch
        {
            MapRewriteMode.None     => I18N.T("mapRewrite.none", "None"),
            MapRewriteMode.AllChest => I18N.T("mapRewrite.allChest", "All Chest"),
            MapRewriteMode.AllElite => I18N.T("mapRewrite.allElite", "All Elite"),
            MapRewriteMode.AllBoss  => I18N.T("mapRewrite.allBoss", "All Boss"),
            _                       => "?"
        };
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

    public required Action OnOpenPowers   { get; init; }
    public required Action OnOpenPotions  { get; init; }
    public required Action OnOpenEvents   { get; init; }
    public required Action OnOpenConsole  { get; init; }
    public required Action OnOpenPresets  { get; init; }
    public required Action OnOpenCardEdit { get; init; }

    public Action? OnToggleAI       { get; init; }
    public Action? OnCycleStrategy  { get; init; }
    public Action? OnCycleSpeed     { get; init; }
    public Func<bool>? IsAIEnabled  { get; init; }
    public Func<string>? GetStrategyName { get; init; }
    public Func<string>? GetSpeedLabel   { get; init; }

    public required Action OnCycleGameSpeed   { get; init; }
    public required Func<string> GetGameSpeedLabel { get; init; }

    public required Action OnToggleSkipAnim    { get; init; }
    public required Func<string> GetSkipAnimLabel { get; init; }
}
