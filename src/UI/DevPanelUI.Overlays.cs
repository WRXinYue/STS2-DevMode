using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Icons;
using DevMode.Settings;

namespace DevMode.UI;

internal static partial class DevPanelUI
{
    private const string SettingsRootName  = "DevModeSettings";
    private const string SaveLoadRootName  = "DevModeSaveLoad";
    private const string AIRootName        = "DevModeAI";

    // ── Helper: build the standard browser-panel root ──────────────────────

    private static (Control root, VBoxContainer vbox) CreateOverlayRoot(
        NGlobalUi globalUi, string rootName, float panelWidth = 0f)
    {
        PinRail();
        SpliceRail(globalUi, joined: true);

        var root = new Control { Name = rootName, MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 1250 };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.TreeExiting += () =>
        {
            UnpinRail();
            SpliceRail(globalUi, joined: false);
        };

        // Backdrop sits behind the panel; clicking outside the panel closes it
        if (panelWidth > 0f)
            root.AddChild(CreateBrowserBackdrop(() => ((Node)globalUi).GetNodeOrNull<Control>(rootName)?.QueueFree()));

        var panel = CreateBrowserPanel(panelWidth);
        root.AddChild(panel);

        var vbox = panel.GetNode<VBoxContainer>("Content");
        vbox.AddThemeConstantOverride("separation", 10);

        return (root, vbox);
    }

    // ── Settings (Cheats) ──────────────────────────────────────────────────

    internal static void ShowCheatsOverlay(NGlobalUi globalUi, DevPanelActions actions)
    {
        var existing = ((Node)globalUi).GetNodeOrNull<Control>(SettingsRootName);
        if (existing != null)
        {
            ((Node)globalUi).RemoveChild(existing);
            existing.QueueFree();
        }

        var (root, vbox) = CreateOverlayRoot(globalUi, SettingsRootName, 640f);

        // Nav tab header
        AddBrowserNavTab(vbox, I18N.T("panel.settings", "Settings"));

        // Scrollable content
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical    = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal  = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        var inner = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        inner.AddThemeConstantOverride("separation", 4);

        // ── Section: Appearance ──
        inner.AddChild(CreateSectionHeader(I18N.T("appearance.title", "Appearance")));
        inner.AddChild(CreateAppearanceSection(() => ShowCheatsOverlay(globalUi, actions)));

        // ── Section: Player ──
        inner.AddChild(CreateSectionHeader(I18N.T("panel.section.player", "Player")));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.infiniteHp", "Infinite HP"), I18N.T("cheat.infiniteHp.desc", "Player cannot lose HP"), () => DevModeState.InfiniteHp, v => DevModeState.InfiniteHp = v));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.infiniteBlock", "Infinite Shield"), I18N.T("cheat.infiniteBlock.desc", "Block refills to 999 after loss"), () => DevModeState.InfiniteBlock, v =>
        {
            DevModeState.InfiniteBlock = v;
            if (v && RunContext.TryGetRunAndPlayer(out _, out var bp))
            {
                var c = bp.Creature;
                if (c.Block < 999) c.GainBlockInternal(999 - c.Block);
            }
        }));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.infiniteEnergy", "Infinite Energy"), I18N.T("cheat.infiniteEnergy.desc", "Energy refills after spending"), () => DevModeState.InfiniteEnergy, v => DevModeState.InfiniteEnergy = v));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.infiniteStars", "Infinite Stars"), I18N.T("cheat.infiniteStars.desc", "Stars refill after spending"), () => DevModeState.InfiniteStars, v => DevModeState.InfiniteStars = v));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.alwaysPotion", "Always Reward Potion"), null, () => DevModeState.AlwaysRewardPotion, v => DevModeState.AlwaysRewardPotion = v));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.alwaysUpgrade", "Always Upgrade Reward"), I18N.T("cheat.alwaysUpgrade.desc", "Card rewards are always upgraded"), () => DevModeState.AlwaysUpgradeCardReward, v => DevModeState.AlwaysUpgradeCardReward = v));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.maxRarity", "Max Card Reward Rarity"), I18N.T("cheat.maxRarity.desc", "All card rewards are Rare"), () => DevModeState.MaxCardRewardRarity, v => DevModeState.MaxCardRewardRarity = v));
        inner.AddChild(CreateCheatSlider(I18N.T("cheat.defenseMultiplier", "Defense Multiplier"), I18N.T("cheat.defenseMultiplier.desc", "Multiply block gained"), 0, 10, 0.5f, () => DevModeState.DefenseMultiplier, v => DevModeState.DefenseMultiplier = v));

        // ── Section: Inventory ──
        inner.AddChild(CreateSectionHeader(I18N.T("panel.section.inventory", "Inventory")));
        inner.AddChild(CreateCheatNumberEdit(I18N.T("cheat.editGold", "Edit Gold"), 0, 99999,
            () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.Gold; },
            v => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return; p.Gold = (int)v; }));
        inner.AddChild(CreateCheatSlider(I18N.T("cheat.goldMultiplier", "Gold Multiplier"), I18N.T("cheat.goldMultiplier.desc", "Multiply gold gained"), 0, 10, 0.5f, () => DevModeState.GoldMultiplier, v => DevModeState.GoldMultiplier = v));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.freeShop", "Free Shop"), I18N.T("cheat.freeShop.desc", "All shop purchases are free"), () => DevModeState.FreeShop, v => DevModeState.FreeShop = v));

        // ── Section: Status ──
        inner.AddChild(CreateSectionHeader(I18N.T("panel.section.status", "Status")));
        inner.AddChild(CreateCheatNumberEdit(I18N.T("cheat.editEnergyCap", "Edit Energy Cap"), 0, 99,
            () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.MaxEnergy; },
            v => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return; p.MaxEnergy = (int)v; }));
        inner.AddChild(CreateCheatNumberEdit(I18N.T("cheat.editPotionSlots", "Edit Potion Slots"), 0, 20,
            () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.MaxPotionCount; },
            v =>
            {
                if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return;
                int current = p.MaxPotionCount;
                int diff = (int)v - current;
                if (diff > 0) { p.AddToMaxPotionCount(diff); }
                else if (diff < 0)
                {
                    for (int i = current - 1; i >= current + diff; i--)
                    {
                        var potion = p.GetPotionAtSlotIndex(i);
                        if (potion != null) p.DiscardPotionInternal(potion);
                    }
                    p.SubtractFromMaxPotionCount(-diff);
                }
            }));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.maxScore", "Max Score"), I18N.T("cheat.maxScore.desc", "Enable max score tracking"), () => DevModeState.MaxScore, v => DevModeState.MaxScore = v));
        inner.AddChild(CreateCheatSlider(I18N.T("cheat.scoreMultiplier", "Score Multiplier"), I18N.T("cheat.scoreMultiplier.desc", "Multiply score gained"), 0, 10, 0.5f, () => DevModeState.ScoreMultiplier, v => DevModeState.ScoreMultiplier = v));

        // ── Section: Enemy ──
        inner.AddChild(CreateSectionHeader(I18N.T("panel.section.enemy", "Enemy")));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.freezeEnemies", "Freeze Enemies"), I18N.T("cheat.freezeEnemies.desc", "Enemies skip their turns"), () => DevModeState.FreezeEnemies, v => DevModeState.FreezeEnemies = v));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.oneHitKill", "One-Hit Kill"), I18N.T("cheat.oneHitKill.desc", "Deal massive damage to enemies"), () => DevModeState.OneHitKill, v => DevModeState.OneHitKill = v));
        inner.AddChild(CreateCheatSlider(I18N.T("cheat.damageMultiplier", "Damage Multiplier"), I18N.T("cheat.damageMultiplier.desc", "Multiply damage dealt to enemies"), 0, 10, 0.5f, () => DevModeState.DamageMultiplier, v => DevModeState.DamageMultiplier = v));

        // ── Section: Game ──
        inner.AddChild(CreateSectionHeader(I18N.T("panel.section.game", "Game")));
        inner.AddChild(CreateCheatToggle(I18N.T("cheat.unknownTreasure", "Unknown → Treasure"), I18N.T("cheat.unknownTreasure.desc", "Unknown map nodes always give treasure"), () => DevModeState.UnknownMapAlwaysTreasure, v => DevModeState.UnknownMapAlwaysTreasure = v));
        inner.AddChild(CreateCheatToggle(I18N.T("mapRewrite.enabled", "Enable Map Rewrite"), "", () => DevModeState.MapRewriteEnabled, v => DevModeState.MapRewriteEnabled = v));

        var mapModeBtn = CreatePlainButton(I18N.T("mapRewrite.mode", "Mode") + ": " + GetMapRewriteLabel(), MdiIcon.Map);
        mapModeBtn.Pressed += () =>
        {
            DevModeState.MapRewriteMode = DevModeState.MapRewriteMode switch
            {
                MapRewriteMode.None     => MapRewriteMode.AllChest,
                MapRewriteMode.AllChest => MapRewriteMode.AllElite,
                MapRewriteMode.AllElite => MapRewriteMode.AllBoss,
                MapRewriteMode.AllBoss  => MapRewriteMode.None,
                _                       => MapRewriteMode.None
            };
            mapModeBtn.Text = I18N.T("mapRewrite.mode", "Mode") + ": " + GetMapRewriteLabel();
        };
        inner.AddChild(mapModeBtn);
        inner.AddChild(CreateCheatToggle(I18N.T("mapRewrite.keepFinalBoss", "Keep Final Boss"), "", () => DevModeState.MapKeepFinalBoss, v => DevModeState.MapKeepFinalBoss = v));

        var gameSpeedBtn = CreatePlainButton(I18N.T("panel.speed", "Speed: {0}", actions.GetGameSpeedLabel()), MdiIcon.SpeedometerMedium);
        gameSpeedBtn.Pressed += () =>
        {
            actions.OnCycleGameSpeed();
            gameSpeedBtn.Text = I18N.T("panel.speed", "Speed: {0}", actions.GetGameSpeedLabel());
        };
        inner.AddChild(gameSpeedBtn);

        var skipAnimBtn = CreatePlainButton(I18N.T("panel.skipAnim", "Skip Anim: {0}", actions.GetSkipAnimLabel()), MdiIcon.AnimationPlay);
        skipAnimBtn.Pressed += () =>
        {
            actions.OnToggleSkipAnim();
            skipAnimBtn.Text = I18N.T("panel.skipAnim", "Skip Anim: {0}", actions.GetSkipAnimLabel());
        };
        inner.AddChild(skipAnimBtn);

        // ── Section: Runtime Stats ──
        inner.AddChild(CreateSectionHeader(I18N.T("panel.section.runtime", "Runtime Stats")));
        inner.AddChild(CreateRuntimeToggle(I18N.T("runtime.godMode", "God Mode"), I18N.T("runtime.godMode.desc", "Auto-heal to max HP every frame"), () => DevModeState.StatModifiers?.GodMode ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.GodMode = v; }));
        inner.AddChild(CreateRuntimeToggle(I18N.T("runtime.killAll", "Kill All Enemies"), I18N.T("runtime.killAll.desc", "Continuously kill all enemies"), () => DevModeState.StatModifiers?.KillAllEnemies ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.KillAllEnemies = v; }));
        inner.AddChild(CreateRuntimeToggle(I18N.T("runtime.infiniteEnergy", "Infinite Energy (Runtime)"), I18N.T("runtime.infiniteEnergy.desc", "Keep energy at 99+"), () => DevModeState.StatModifiers?.InfiniteEnergy ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.InfiniteEnergy = v; }));
        inner.AddChild(CreateRuntimeToggle(I18N.T("runtime.alwaysPlayerTurn", "Always Player Turn"), I18N.T("runtime.alwaysPlayerTurn.desc", "Force combat to player turn"), () => DevModeState.StatModifiers?.AlwaysPlayerTurn ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.AlwaysPlayerTurn = v; }));
        inner.AddChild(CreateRuntimeToggle(I18N.T("runtime.drawToLimit", "Draw to Hand Limit"), I18N.T("runtime.drawToLimit.desc", "Auto-draw to 10 cards"), () => DevModeState.StatModifiers?.DrawToHandLimit ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.DrawToHandLimit = v; }));
        inner.AddChild(CreateRuntimeToggle(I18N.T("runtime.extraDraw", "Extra Draw Each Turn"), I18N.T("runtime.extraDraw.desc", "Draw extra cards at turn start"), () => DevModeState.StatModifiers?.ExtraDrawEachTurn ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.ExtraDrawEachTurn = v; }));
        inner.AddChild(CreateCheatNumberEdit(I18N.T("runtime.extraDrawAmount", "Extra Draw Amount"), 1, 20, () => DevModeState.StatModifiers?.ExtraDrawEachTurnAmount ?? 1, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.ExtraDrawEachTurnAmount = (int)v; }));
        inner.AddChild(CreateRuntimeToggle(I18N.T("runtime.autoAlly", "Auto-Act Friendly Monsters"), I18N.T("runtime.autoAlly.desc", "Auto-execute friendly monster turns"), () => DevModeState.StatModifiers?.AutoActFriendlyMonsters ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.AutoActFriendlyMonsters = v; }));
        inner.AddChild(CreateRuntimeToggle(I18N.T("runtime.negateDebuffs", "Negate Debuffs"), I18N.T("runtime.negateDebuffs.desc", "Continuously remove all debuffs"), () => DevModeState.StatModifiers?.NegateDebuffs ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.NegateDebuffs = v; }));

        // ── Stat Locks ──
        inner.AddChild(CreateSectionHeader(I18N.T("statLock.title", "Stat Locks")));
        inner.AddChild(CreateStatLockRow(I18N.T("statLock.gold", "Lock Gold"), 0, 99999, () => DevModeState.StatModifiers?.LockGold ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockGold = v; }, () => DevModeState.StatModifiers?.LockedGoldValue ?? 0, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedGoldValue = (int)v; }, () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.Gold; }));
        inner.AddChild(CreateStatLockRow(I18N.T("statLock.currentHp", "Lock Current HP"), 1, 9999, () => DevModeState.StatModifiers?.LockCurrentHp ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockCurrentHp = v; }, () => DevModeState.StatModifiers?.LockedCurrentHpValue ?? 1, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedCurrentHpValue = (int)v; }, () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 1; return p.Creature.CurrentHp; }));
        inner.AddChild(CreateStatLockRow(I18N.T("statLock.maxHp", "Lock Max HP"), 1, 9999, () => DevModeState.StatModifiers?.LockMaxHp ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockMaxHp = v; }, () => DevModeState.StatModifiers?.LockedMaxHpValue ?? 1, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedMaxHpValue = (int)v; }, () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 1; return p.Creature.MaxHp; }));
        inner.AddChild(CreateStatLockRow(I18N.T("statLock.currentEnergy", "Lock Current Energy"), 0, 99, () => DevModeState.StatModifiers?.LockCurrentEnergy ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockCurrentEnergy = v; }, () => DevModeState.StatModifiers?.LockedCurrentEnergyValue ?? 0, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedCurrentEnergyValue = (int)v; }, () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.PlayerCombatState?.Energy ?? 0; }));
        inner.AddChild(CreateStatLockRow(I18N.T("statLock.maxEnergy", "Lock Max Energy"), 1, 99, () => DevModeState.StatModifiers?.LockMaxEnergy ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockMaxEnergy = v; }, () => DevModeState.StatModifiers?.LockedMaxEnergyValue ?? 1, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedMaxEnergyValue = (int)v; }, () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 1; return p.MaxEnergy; }));
        inner.AddChild(CreateStatLockRow(I18N.T("statLock.stars", "Lock Stars"), 0, 999, () => DevModeState.StatModifiers?.LockStars ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockStars = v; }, () => DevModeState.StatModifiers?.LockedStarsValue ?? 0, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedStarsValue = (int)v; }, () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.PlayerCombatState?.Stars ?? 0; }));
        inner.AddChild(CreateStatLockRow(I18N.T("statLock.orbSlots", "Lock Orb Slots"), 0, 10, () => DevModeState.StatModifiers?.LockOrbSlots ?? false, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockOrbSlots = v; }, () => DevModeState.StatModifiers?.LockedOrbSlotsValue ?? 0, v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedOrbSlotsValue = (int)v; }));

        scroll.AddChild(inner);
        vbox.AddChild(scroll);

        ((Node)globalUi).AddChild(root);
    }

    // ── Appearance (theme) controls ───────────────────────────────────────

    private static Control CreateAppearanceSection(Action rebuild)
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 6);

        // ── Mode toggle row: label + single icon button ──
        var modeRow = new HBoxContainer();
        modeRow.AddThemeConstantOverride("separation", 8);

        var modeLbl = new Label
        {
            Text = ThemeManager.IsDarkMode
                ? I18N.T("appearance.mode.dark",  "Dark Mode")
                : I18N.T("appearance.mode.light", "Light Mode"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        modeLbl.AddThemeFontSizeOverride("font_size", 12);
        modeLbl.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
        modeRow.AddChild(modeLbl);

        // Icon button: shows sun (→ switch to dark) when in light mode,
        //              shows moon (→ switch to light) when in dark mode
        var modeIcon = ThemeManager.IsDarkMode ? MdiIcon.WeatherNight : MdiIcon.WeatherSunny;
        var modeBtn = new Button
        {
            CustomMinimumSize = new Vector2(36, 36),
            FocusMode         = Control.FocusModeEnum.None,
            Icon              = modeIcon.Texture(20, DevModeTheme.Accent),
            TooltipText       = ThemeManager.IsDarkMode
                ? I18N.T("appearance.mode.light", "Light Mode")
                : I18N.T("appearance.mode.dark",  "Dark Mode")
        };
        var modeBtnStyle = new StyleBoxFlat
        {
            BgColor = DevModeTheme.ButtonBgNormal,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 6, ContentMarginRight = 6,
            ContentMarginTop = 6, ContentMarginBottom = 6
        };
        var modeBtnHover = new StyleBoxFlat
        {
            BgColor = DevModeTheme.ButtonBgHover,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 6, ContentMarginRight = 6,
            ContentMarginTop = 6, ContentMarginBottom = 6
        };
        modeBtn.AddThemeStyleboxOverride("normal",  modeBtnStyle);
        modeBtn.AddThemeStyleboxOverride("hover",   modeBtnHover);
        modeBtn.AddThemeStyleboxOverride("pressed", modeBtnHover);
        modeBtn.AddThemeStyleboxOverride("focus",   modeBtnStyle);
        modeBtn.Pressed += () =>
        {
            ThemeManager.SetDarkMode(!ThemeManager.IsDarkMode);
            Callable.From(rebuild).CallDeferred();
        };
        modeRow.AddChild(modeBtn);
        col.AddChild(modeRow);

        // ── Dark theme selector ──
        var darkThemeBtn = CreatePlainButton(
            I18N.T("appearance.darkTheme", "Dark Theme: {0}",
                I18N.T("theme." + SettingsStore.Current.DarkThemeName.ToLowerInvariant(),
                    SettingsStore.Current.DarkThemeName)),
            MdiIcon.WeatherNight);
        darkThemeBtn.Pressed += () =>
        {
            ThemeManager.CycleDarkTheme();
            Callable.From(rebuild).CallDeferred();
        };
        col.AddChild(darkThemeBtn);

        // ── Light theme selector ──
        var lightThemeBtn = CreatePlainButton(
            I18N.T("appearance.lightTheme", "Light Theme: {0}",
                I18N.T("theme." + SettingsStore.Current.LightThemeName.ToLowerInvariant(),
                    SettingsStore.Current.LightThemeName)),
            MdiIcon.WeatherSunny);
        lightThemeBtn.Pressed += () =>
        {
            ThemeManager.CycleLightTheme();
            Callable.From(rebuild).CallDeferred();
        };
        col.AddChild(lightThemeBtn);

        return col;
    }

    // ── Save / Load ────────────────────────────────────────────────────────

    internal static void ShowSaveLoadOverlay(NGlobalUi globalUi, DevPanelActions actions)
    {
        ((Node)globalUi).GetNodeOrNull<Control>(SaveLoadRootName)?.QueueFree();

        var (root, vbox) = CreateOverlayRoot(globalUi, SaveLoadRootName, 520f);

        AddBrowserNavTab(vbox, I18N.T("panel.section.save", "Save / Load"));

        var btnBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        btnBox.AddThemeConstantOverride("separation", 6);

        var newTestBtn = CreateListItemButton(I18N.T("panel.newTest", "New Test"));
        newTestBtn.Icon = MdiIcon.Plus.Texture(16);
        newTestBtn.Alignment = HorizontalAlignment.Left;
        newTestBtn.Pressed += () => { ((Node)globalUi).GetNodeOrNull<Control>(SaveLoadRootName)?.QueueFree(); actions.OnNewTest(); };
        btnBox.AddChild(newTestBtn);

        var saveBtn = CreateListItemButton(I18N.T("panel.save", "Save"));
        saveBtn.Icon = MdiIcon.ContentSave.Texture(16);
        saveBtn.Alignment = HorizontalAlignment.Left;
        saveBtn.Pressed += () => { ((Node)globalUi).GetNodeOrNull<Control>(SaveLoadRootName)?.QueueFree(); actions.OnOpenSave(); };
        btnBox.AddChild(saveBtn);

        var loadBtn = CreateListItemButton(I18N.T("panel.load", "Load"));
        loadBtn.Icon = MdiIcon.FolderOpen.Texture(16);
        loadBtn.Alignment = HorizontalAlignment.Left;
        loadBtn.Pressed += () => { ((Node)globalUi).GetNodeOrNull<Control>(SaveLoadRootName)?.QueueFree(); actions.OnOpenLoad(); };
        btnBox.AddChild(loadBtn);

        vbox.AddChild(btnBox);
        vbox.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });

        ((Node)globalUi).AddChild(root);
    }

    // ── AI Control ────────────────────────────────────────────────────────

    internal static void ShowAIOverlay(NGlobalUi globalUi, DevPanelActions actions)
    {
        ((Node)globalUi).GetNodeOrNull<Control>(AIRootName)?.QueueFree();

        var (root, vbox) = CreateOverlayRoot(globalUi, AIRootName, 520f);

        AddBrowserNavTab(vbox, I18N.T("panel.section.ai", "AI Control"));

        var inner = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        inner.AddThemeConstantOverride("separation", 6);

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
        inner.AddChild(aiBtn);

        stratBtn = CreatePlainButton(I18N.T("panel.ai.strategy", "Strategy: {0}", actions.GetStrategyName?.Invoke() ?? I18N.T("ai.strategy.rule", "Rule")), MdiIcon.Cog);
        stratBtn.Pressed += () =>
        {
            actions.OnCycleStrategy?.Invoke();
            stratBtn.Text = I18N.T("panel.ai.strategy", "Strategy: {0}", actions.GetStrategyName?.Invoke() ?? "?");
        };
        inner.AddChild(stratBtn);

        speedBtn = CreatePlainButton(I18N.T("panel.ai.speed", "Speed: {0}", actions.GetSpeedLabel?.Invoke() ?? I18N.T("ai.speed.normal", "Normal")), MdiIcon.FastForward);
        speedBtn.Pressed += () =>
        {
            actions.OnCycleSpeed?.Invoke();
            speedBtn.Text = I18N.T("panel.ai.speed", "Speed: {0}", actions.GetSpeedLabel?.Invoke() ?? "?");
        };
        inner.AddChild(speedBtn);

        vbox.AddChild(inner);
        vbox.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });

        ((Node)globalUi).AddChild(root);
    }

    // ── Shared helpers ────────────────────────────────────────────────────

    private static void AddBrowserNavTab(VBoxContainer vbox, string title)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);
        var tab = new Button { Text = title, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 32) };
        var flat = new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 4, ContentMarginBottom = 6
        };
        foreach (var s in new[] { "normal", "hover", "pressed", "focus" })
            tab.AddThemeStyleboxOverride(s, flat);
        tab.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        tab.AddThemeFontSizeOverride("font_size", 13);
        row.AddChild(tab);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        vbox.AddChild(row);
        vbox.AddChild(new ColorRect
        {
            CustomMinimumSize = new Vector2(0, 1),
            Color = DevModeTheme.Separator,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });
    }

    private static string GetMapRewriteLabel() => DevModeState.MapRewriteMode switch
    {
        MapRewriteMode.None     => I18N.T("mapRewrite.none",     "None"),
        MapRewriteMode.AllChest => I18N.T("mapRewrite.allChest", "All Chest"),
        MapRewriteMode.AllElite => I18N.T("mapRewrite.allElite", "All Elite"),
        MapRewriteMode.AllBoss  => I18N.T("mapRewrite.allBoss",  "All Boss"),
        _                       => "?"
    };
}
