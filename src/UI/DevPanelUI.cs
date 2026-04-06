using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;

namespace DevMode.UI;

internal static class DevPanelUI
{
    private const string RootName   = "DevModeSidebarRoot";
    private const string TopBarName = "DevModeTopBar";
    private const float  DefaultPanelW = 280f;
    private const float  MinPanelW     = 200f;
    private const float  MaxPanelW     = 500f;
    private const float  ResizeHandleW = 6f;
    private const float  TabW       = 24f;
    private const float  TabH       = 56f;

    private static float _panelW = DefaultPanelW;
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
            OffsetLeft          = -_panelW,
            OffsetRight         = TabW,
            CustomMinimumSize   = new Vector2(_panelW + TabW, 0)
        };
        root.AddChild(drawer);

        // ── Panel ──
        var panel = new PanelContainer
        {
            Name              = "DevModePanel",
            AnchorLeft        = 0, AnchorRight  = 0,
            AnchorTop         = 0, AnchorBottom = 1,
            OffsetLeft        = 0,
            OffsetRight       = _panelW,
        };
        var panelStyle = new StyleBoxFlat
        {
            BgColor              = new Color(0.08f, 0.08f, 0.10f, 0.95f),
            ContentMarginLeft    = 8, ContentMarginRight  = 4,
            ContentMarginTop     = 8, ContentMarginBottom = 8,
            CornerRadiusTopRight    = 0, CornerRadiusBottomRight = 0,
            CornerRadiusTopLeft     = 0, CornerRadiusBottomLeft  = 0,
            BorderWidthRight     = 1,
            BorderColor          = new Color(0.35f, 0.35f, 0.45f, 0.6f)
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        panel.MouseFilter = Control.MouseFilterEnum.Stop;

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical   = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };

        var contentMargin = new MarginContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        contentMargin.AddThemeConstantOverride("margin_right", 6);
        contentMargin.AddThemeConstantOverride("margin_left", 0);
        contentMargin.AddThemeConstantOverride("margin_top", 0);
        contentMargin.AddThemeConstantOverride("margin_bottom", 0);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 4);

        // ── Section: Actions ──
        vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.actions", "Actions")));
        vbox.AddChild(CreateButton(I18N.T("panel.cards", "Cards"), actions.OnOpenCards));
        vbox.AddChild(CreateButton(I18N.T("panel.relics", "Relics"), actions.OnOpenRelics));
        vbox.AddChild(CreateButton(I18N.T("panel.enemies", "Enemies"), actions.OnOpenEnemies));
        vbox.AddChild(CreateButton(I18N.T("panel.powers", "Powers"), actions.OnOpenPowers));
        vbox.AddChild(CreateButton(I18N.T("panel.potions", "Potions"), actions.OnOpenPotions));
        vbox.AddChild(CreateButton(I18N.T("panel.events", "Events"), actions.OnOpenEvents));
        vbox.AddChild(CreateButton(I18N.T("panel.cardEdit", "Card Editor"), actions.OnOpenCardEdit));
        vbox.AddChild(CreateButton(I18N.T("panel.console", "Console"), actions.OnOpenConsole));
        vbox.AddChild(CreateButton(I18N.T("panel.presets", "Presets"), actions.OnOpenPresets));

        // ── Section: Save ──
        vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.save", "Save")));
        vbox.AddChild(CreateButton(I18N.T("panel.save", "Save"), actions.OnOpenSave));
        vbox.AddChild(CreateButton(I18N.T("panel.load", "Load"), actions.OnOpenLoad));

        // ── Section: Player ──
        vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.player", "Player")));
        vbox.AddChild(CreateCheatToggle(
            I18N.T("cheat.infiniteHp", "Infinite HP"),
            I18N.T("cheat.infiniteHp.desc", "Player cannot lose HP"),
            () => DevModeState.InfiniteHp,
            v => DevModeState.InfiniteHp = v));
        vbox.AddChild(CreateCheatToggle(
            I18N.T("cheat.infiniteBlock", "Infinite Shield"),
            I18N.T("cheat.infiniteBlock.desc", "Block refills to 999 after loss"),
            () => DevModeState.InfiniteBlock,
            v => DevModeState.InfiniteBlock = v));
        vbox.AddChild(CreateCheatToggle(
            I18N.T("cheat.infiniteEnergy", "Infinite Energy"),
            I18N.T("cheat.infiniteEnergy.desc", "Energy refills after spending"),
            () => DevModeState.InfiniteEnergy,
            v => DevModeState.InfiniteEnergy = v));
        vbox.AddChild(CreateCheatToggle(
            I18N.T("cheat.infiniteStars", "Infinite Stars"),
            I18N.T("cheat.infiniteStars.desc", "Stars refill after spending"),
            () => DevModeState.InfiniteStars,
            v => DevModeState.InfiniteStars = v));
        vbox.AddChild(CreateCheatToggle(
            I18N.T("cheat.alwaysPotion", "Always Reward Potion"),
            null,
            () => DevModeState.AlwaysRewardPotion,
            v => DevModeState.AlwaysRewardPotion = v));
        vbox.AddChild(CreateCheatToggle(
            I18N.T("cheat.alwaysUpgrade", "Always Upgrade Reward"),
            I18N.T("cheat.alwaysUpgrade.desc", "Card rewards are always upgraded"),
            () => DevModeState.AlwaysUpgradeCardReward,
            v => DevModeState.AlwaysUpgradeCardReward = v));
        vbox.AddChild(CreateCheatToggle(
            I18N.T("cheat.maxRarity", "Max Card Reward Rarity"),
            I18N.T("cheat.maxRarity.desc", "All card rewards are Rare"),
            () => DevModeState.MaxCardRewardRarity,
            v => DevModeState.MaxCardRewardRarity = v));
        vbox.AddChild(CreateCheatSlider(
            I18N.T("cheat.defenseMultiplier", "Defense Multiplier"),
            I18N.T("cheat.defenseMultiplier.desc", "Multiply block gained"),
            0, 10, 0.5f,
            () => DevModeState.DefenseMultiplier,
            v => DevModeState.DefenseMultiplier = v));

        // ── Section: Inventory ──
        vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.inventory", "Inventory")));
        vbox.AddChild(CreateCheatNumberEdit(
            I18N.T("cheat.editGold", "Edit Gold"),
            0, 99999,
            () =>
            {
                if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0;
                return p.Gold;
            },
            v =>
            {
                if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return;
                p.Gold = (int)v;
            }));
        vbox.AddChild(CreateCheatSlider(
            I18N.T("cheat.goldMultiplier", "Gold Multiplier"),
            I18N.T("cheat.goldMultiplier.desc", "Multiply gold gained"),
            0, 10, 0.5f,
            () => DevModeState.GoldMultiplier,
            v => DevModeState.GoldMultiplier = v));
        vbox.AddChild(CreateCheatToggle(
            I18N.T("cheat.freeShop", "Free Shop"),
            I18N.T("cheat.freeShop.desc", "All shop purchases are free"),
            () => DevModeState.FreeShop,
            v => DevModeState.FreeShop = v));

        // ── Section: Status ──
        vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.status", "Status")));
        vbox.AddChild(CreateCheatNumberEdit(
            I18N.T("cheat.editEnergyCap", "Edit Energy Cap"),
            0, 99,
            () =>
            {
                if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0;
                return p.MaxEnergy;
            },
            v =>
            {
                if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return;
                p.MaxEnergy = (int)v;
            }));
        vbox.AddChild(CreateCheatNumberEdit(
            I18N.T("cheat.editPotionSlots", "Edit Potion Slots"),
            0, 20,
            () =>
            {
                if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0;
                return p.MaxPotionCount;
            },
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
            () => DevModeState.MaxScore,
            v => DevModeState.MaxScore = v));
        vbox.AddChild(CreateCheatSlider(
            I18N.T("cheat.scoreMultiplier", "Score Multiplier"),
            I18N.T("cheat.scoreMultiplier.desc", "Multiply score gained"),
            0, 10, 0.5f,
            () => DevModeState.ScoreMultiplier,
            v => DevModeState.ScoreMultiplier = v));

        // ── Section: Enemy ──
        vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.enemy", "Enemy")));
        vbox.AddChild(CreateCheatToggle(
            I18N.T("cheat.freezeEnemies", "Freeze Enemies"),
            I18N.T("cheat.freezeEnemies.desc", "Enemies skip their turns"),
            () => DevModeState.FreezeEnemies,
            v => DevModeState.FreezeEnemies = v));
        vbox.AddChild(CreateCheatToggle(
            I18N.T("cheat.oneHitKill", "One-Hit Kill"),
            I18N.T("cheat.oneHitKill.desc", "Deal massive damage to enemies"),
            () => DevModeState.OneHitKill,
            v => DevModeState.OneHitKill = v));
        vbox.AddChild(CreateCheatSlider(
            I18N.T("cheat.damageMultiplier", "Damage Multiplier"),
            I18N.T("cheat.damageMultiplier.desc", "Multiply damage dealt to enemies"),
            0, 10, 0.5f,
            () => DevModeState.DamageMultiplier,
            v => DevModeState.DamageMultiplier = v));

        // ── Section: Game ──
        vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.game", "Game")));
        vbox.AddChild(CreateCheatToggle(
            I18N.T("cheat.unknownTreasure", "Unknown → Treasure"),
            I18N.T("cheat.unknownTreasure.desc", "Unknown map nodes always give treasure"),
            () => DevModeState.UnknownMapAlwaysTreasure,
            v => DevModeState.UnknownMapAlwaysTreasure = v));

        // ── Map Rewrite ──
        vbox.AddChild(CreateCheatToggle(
            I18N.T("mapRewrite.enabled", "Enable Map Rewrite"),
            "",
            () => DevModeState.MapRewriteEnabled,
            v => DevModeState.MapRewriteEnabled = v));

        var mapModeBtn = CreatePlainButton(I18N.T("mapRewrite.mode", "Mode") + ": " + GetMapRewriteLabel());
        mapModeBtn.Pressed += () =>
        {
            DevModeState.MapRewriteMode = DevModeState.MapRewriteMode switch
            {
                MapRewriteMode.None => MapRewriteMode.AllChest,
                MapRewriteMode.AllChest => MapRewriteMode.AllElite,
                MapRewriteMode.AllElite => MapRewriteMode.AllBoss,
                MapRewriteMode.AllBoss => MapRewriteMode.None,
                _ => MapRewriteMode.None
            };
            mapModeBtn.Text = I18N.T("mapRewrite.mode", "Mode") + ": " + GetMapRewriteLabel();
        };
        vbox.AddChild(mapModeBtn);

        vbox.AddChild(CreateCheatToggle(
            I18N.T("mapRewrite.keepFinalBoss", "Keep Final Boss"),
            "",
            () => DevModeState.MapKeepFinalBoss,
            v => DevModeState.MapKeepFinalBoss = v));

        var gameSpeedBtn = CreatePlainButton(I18N.T("panel.speed", "Speed: {0}", actions.GetGameSpeedLabel()));
        gameSpeedBtn.Pressed += () =>
        {
            actions.OnCycleGameSpeed();
            gameSpeedBtn.Text = I18N.T("panel.speed", "Speed: {0}", actions.GetGameSpeedLabel());
        };
        vbox.AddChild(gameSpeedBtn);

        var skipAnimBtn = CreatePlainButton(I18N.T("panel.skipAnim", "Skip Anim: {0}", actions.GetSkipAnimLabel()));
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
            I18N.T("runtime.extraDrawAmount", "Extra Draw Amount"),
            1, 20,
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

        // ── Section: AI (optional) ──
        if (actions.OnToggleAI != null)
        {
            vbox.AddChild(CreateSectionHeader(I18N.T("panel.section.ai", "AI")));

            var aiBtn = CreatePlainButton(I18N.T("panel.ai.off", "AI: Off"));
            Button? stratBtn = null;
            Button? speedBtn = null;

            aiBtn.Pressed += () =>
            {
                actions.OnToggleAI();
                bool enabled = actions.IsAIEnabled?.Invoke() ?? false;
                aiBtn.Text = enabled ? I18N.T("panel.ai.running", "AI: Running") : I18N.T("panel.ai.off", "AI: Off");
                if (stratBtn != null) stratBtn.Visible = !enabled;
                if (speedBtn != null) speedBtn.Visible = !enabled;
            };
            vbox.AddChild(aiBtn);

            stratBtn = CreatePlainButton(I18N.T("panel.ai.strategy", "Strategy: {0}", actions.GetStrategyName?.Invoke() ?? I18N.T("ai.strategy.rule", "Rule")));
            stratBtn.Pressed += () =>
            {
                actions.OnCycleStrategy?.Invoke();
                stratBtn.Text = I18N.T("panel.ai.strategy", "Strategy: {0}", actions.GetStrategyName?.Invoke() ?? "?");
            };
            vbox.AddChild(stratBtn);

            speedBtn = CreatePlainButton(I18N.T("panel.ai.speed", "Speed: {0}", actions.GetSpeedLabel?.Invoke() ?? I18N.T("ai.speed.normal", "Normal")));
            speedBtn.Pressed += () =>
            {
                actions.OnCycleSpeed?.Invoke();
                speedBtn.Text = I18N.T("panel.ai.speed", "Speed: {0}", actions.GetSpeedLabel?.Invoke() ?? "?");
            };
            vbox.AddChild(speedBtn);
        }

        contentMargin.AddChild(vbox);
        scroll.AddChild(contentMargin);
        panel.AddChild(scroll);
        drawer.AddChild(panel);

        // ── Resize handle (right edge of panel, draggable) ──
        var resizeHandle = new Control
        {
            Name              = "ResizeHandle",
            AnchorLeft        = 0, AnchorRight  = 0,
            AnchorTop         = 0, AnchorBottom = 1,
            OffsetLeft        = _panelW - ResizeHandleW,
            OffsetRight       = _panelW,
            MouseFilter       = Control.MouseFilterEnum.Stop,
            MouseDefaultCursorShape = Control.CursorShape.Hsize
        };
        drawer.AddChild(resizeHandle);

        // ── Arrow tab (sits at the right edge of the drawer, vertically centred) ──
        _arrowRight ??= CreateChevronTexture(true);
        _arrowLeft  ??= CreateChevronTexture(false);

        var tab = new Button
        {
            Name              = "DrawerTab",
            CustomMinimumSize = new Vector2(TabW, TabH),
            AnchorLeft        = 0, AnchorRight  = 0,
            AnchorTop         = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft        = _panelW,
            OffsetRight       = _panelW + TabW,
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

        // ── Slide animation (hover-triggered) + lock ──
        bool open = false;
        bool locked = false;
        Tween? tween = null;
        SceneTreeTimer? closeTimer = null;

        var tabStyleNormal = tabStyle;
        var tabStyleLocked = new StyleBoxFlat
        {
            BgColor                  = new Color(0.25f, 0.4f, 0.6f, 0.92f),
            CornerRadiusTopRight     = 6, CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft      = 0, CornerRadiusBottomLeft  = 0,
            BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            BorderColor = new Color(0.5f, 0.7f, 0.9f, 0.8f)
        };

        void ApplyTabStyle(bool isLocked)
        {
            var s = isLocked ? tabStyleLocked : tabStyleNormal;
            tab.AddThemeStyleboxOverride("normal",  s);
            tab.AddThemeStyleboxOverride("hover",   s);
            tab.AddThemeStyleboxOverride("pressed", s);
            tab.AddThemeStyleboxOverride("focus",   s);
        }

        void UpdateLayout()
        {
            panel.OffsetRight = _panelW;
            resizeHandle.OffsetLeft  = _panelW - ResizeHandleW;
            resizeHandle.OffsetRight = _panelW;
            tab.OffsetLeft  = _panelW;
            tab.OffsetRight = _panelW + TabW;
            drawer.CustomMinimumSize = new Vector2(_panelW + TabW, 0);
            if (open)
            {
                drawer.OffsetLeft  = 0f;
                drawer.OffsetRight = _panelW + TabW;
            }
        }

        void Slide(bool toOpen)
        {
            if (open == toOpen) return;
            open = toOpen;
            tab.Icon = open ? _arrowLeft : _arrowRight;

            tween?.Kill();
            tween = drawer.CreateTween();
            float targetLeft  = open ? 0f : -_panelW;
            float targetRight = open ? _panelW + TabW : TabW;
            tween.TweenProperty(drawer, "offset_left",  targetLeft,  0.18f)
                 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.Parallel()
                 .TweenProperty(drawer, "offset_right", targetRight, 0.18f)
                 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }

        // ── Tab click → toggle lock ──
        tab.Pressed += () =>
        {
            if (!open)
            {
                // Panel is closed — open and lock immediately
                locked = true;
                Slide(true);
            }
            else
            {
                // Panel is open — toggle lock
                locked = !locked;
                if (!locked)
                {
                    // Unlocked: schedule close so it behaves like normal hover
                    ScheduleClose();
                }
            }
            ApplyTabStyle(locked);
            tab.TooltipText = locked
                ? I18N.T("panel.unlock", "Click to unlock panel")
                : I18N.T("panel.lock", "Click to lock panel open");
        };

        // ── Resize drag logic ──
        bool dragging = false;
        float dragStartX = 0;
        float dragStartW = 0;

        resizeHandle.GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    dragging = true;
                    dragStartX = mb.GlobalPosition.X;
                    dragStartW = _panelW;
                }
                else
                {
                    dragging = false;
                }
            }
            else if (@event is InputEventMouseMotion mm && dragging)
            {
                float delta = mm.GlobalPosition.X - dragStartX;
                _panelW = Math.Clamp(dragStartW + delta, MinPanelW, MaxPanelW);
                UpdateLayout();
            }
        };

        // Also keep resize handle from triggering panel close
        resizeHandle.MouseEntered += () => CancelClose();

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
            if (locked) return; // locked → never auto-close
            CancelClose();
            closeTimer = drawer.GetTree().CreateTimer(0.15);
            closeTimer.Timeout += OnCloseTimeout;
        }

        tab.MouseEntered    += () => { CancelClose(); Slide(true); };
        panel.MouseEntered  += CancelClose;
        scroll.MouseEntered += CancelClose;
        tab.MouseExited     += ScheduleClose;
        panel.MouseExited   += ScheduleClose;

        // Wire the scrollbar once it's ready — it's a child of ScrollContainer
        scroll.Ready += () =>
        {
            var vScrollBar = scroll.GetVScrollBar();
            if (vScrollBar != null)
            {
                vScrollBar.MouseEntered += CancelClose;
            }
        };

        // Prevent close when hovering child controls inside the panel
        void WireMouseEntered(Control parent)
        {
            foreach (var child in parent.GetChildren())
            {
                if (child is Control ctrl)
                {
                    ctrl.MouseEntered += CancelClose;
                    WireMouseEntered(ctrl);
                }
            }
        }
        WireMouseEntered(vbox);

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
        // Left group: encounter override modes
        var labels = new[] { I18N.T("topbar.enemy.global","Global"), I18N.T("topbar.enemy.byType","By Type"), I18N.T("topbar.enemy.byFloor","By Floor"), I18N.T("topbar.enemy.off","Off") };
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

            var addBtn = CreateToggleButton(I18N.T("topbar.enemy.addMonster", "Add Monster"));
            ApplyToggleStyle(addBtn, false, 1); // left corners
            addBtn.Pressed += () => _onRefreshPanel?.Invoke(); // handled by DevPanel
            addBtn.SetMeta("combat_action", "add");
            bar.AddChild(addBtn);

            var killBtn = CreateToggleButton(I18N.T("topbar.enemy.killEnemy", "Kill Enemy"));
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

    private static Control CreateSectionHeader(string text)
    {
        var container = new HBoxContainer();
        container.AddThemeConstantOverride("separation", 6);

        var line1 = new HSeparator
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter
        };
        line1.AddThemeStyleboxOverride("separator", new StyleBoxFlat
        {
            BgColor            = new Color(0.35f, 0.35f, 0.45f, 0.4f),
            ContentMarginTop   = 0, ContentMarginBottom = 0,
            ContentMarginLeft  = 0, ContentMarginRight  = 0
        });
        line1.AddThemeConstantOverride("separation", 1);

        var label = new Label
        {
            Text                = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.65f));

        var line2 = new HSeparator
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter
        };
        line2.AddThemeStyleboxOverride("separator", new StyleBoxFlat
        {
            BgColor            = new Color(0.35f, 0.35f, 0.45f, 0.4f),
            ContentMarginTop   = 0, ContentMarginBottom = 0,
            ContentMarginLeft  = 0, ContentMarginRight  = 0
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
            MinValue = min,
            MaxValue = max,
            Step = step,
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

        var minusBtn = new Button { Text = "−", CustomMinimumSize = new Vector2(26, 26), FocusMode = Control.FocusModeEnum.None };
        row.AddChild(minusBtn);

        var spinBox = new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = 1,
            Value = getter(),
            CustomMinimumSize = new Vector2(50, 26),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            Alignment = HorizontalAlignment.Center
        };
        row.AddChild(spinBox);

        var plusBtn = new Button { Text = "+", CustomMinimumSize = new Vector2(26, 26), FocusMode = Control.FocusModeEnum.None };
        row.AddChild(plusBtn);

        var applyBtn = new Button { Text = "✓", CustomMinimumSize = new Vector2(26, 26), FocusMode = Control.FocusModeEnum.None };
        var applyStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.5f, 0.4f, 0.9f),
            ContentMarginLeft = 4, ContentMarginRight = 4,
            ContentMarginTop = 2, ContentMarginBottom = 2,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        };
        applyBtn.AddThemeStyleboxOverride("normal", applyStyle);
        applyBtn.AddThemeStyleboxOverride("hover", applyStyle);
        applyBtn.AddThemeStyleboxOverride("pressed", applyStyle);
        row.AddChild(applyBtn);

        minusBtn.Pressed += () => spinBox.Value = Math.Max(min, spinBox.Value - 1);
        plusBtn.Pressed  += () => spinBox.Value = Math.Min(max, spinBox.Value + 1);
        applyBtn.Pressed += () => setter((int)spinBox.Value);

        // Refresh value when panel reopens
        row.VisibilityChanged += () =>
        {
            if (row.Visible) spinBox.Value = getter();
        };

        return row;
    }

    /// <summary>Same as CreateCheatToggle but for RuntimeStatModifiers toggles.</summary>
    private static Control CreateRuntimeToggle(string label, string? tooltip, Func<bool> getter, Action<bool> setter)
    {
        return CreateCheatToggle(label, tooltip, getter, setter);
    }

    /// <summary>A row with a checkbox (lock toggle) and a SpinBox (locked value).</summary>
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
            MapRewriteMode.None => I18N.T("mapRewrite.none", "None"),
            MapRewriteMode.AllChest => I18N.T("mapRewrite.allChest", "All Chest"),
            MapRewriteMode.AllElite => I18N.T("mapRewrite.allElite", "All Elite"),
            MapRewriteMode.AllBoss => I18N.T("mapRewrite.allBoss", "All Boss"),
            _ => "?"
        };
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

    // New action panels
    public required Action OnOpenPowers   { get; init; }
    public required Action OnOpenPotions  { get; init; }
    public required Action OnOpenEvents   { get; init; }
    public required Action OnOpenConsole  { get; init; }
    public required Action OnOpenPresets  { get; init; }
    public required Action OnOpenCardEdit { get; init; }

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
