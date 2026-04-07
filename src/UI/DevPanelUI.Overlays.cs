using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Icons;

namespace DevMode.UI;

internal static partial class DevPanelUI
{
    private static void ShowCheatsOverlay(NGlobalUi globalUi, DevPanelActions actions)
    {
        ToggleOverlay(globalUi, "cheats", content =>
        {
            var title = new Label
            {
                Text = I18N.T("panel.settings", "Settings"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            title.AddThemeFontSizeOverride("font_size", 16);
            title.AddThemeColorOverride("font_color", new Color(0.92f, 0.92f, 0.96f));
            content.AddChild(title);

            content.AddChild(CreateOverlaySeparator());

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
                () => DevModeState.InfiniteBlock, v =>
                {
                    DevModeState.InfiniteBlock = v;
                    if (v && RunContext.TryGetRunAndPlayer(out _, out var bp))
                    {
                        var c = bp.Creature;
                        if (c.Block < 999) c.GainBlockInternal(999 - c.Block);
                    }
                }));
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
                    if (diff > 0)
                    {
                        p.AddToMaxPotionCount(diff);
                    }
                    else if (diff < 0)
                    {
                        for (int i = current - 1; i >= current + diff; i--)
                        {
                            var potion = p.GetPotionAtSlotIndex(i);
                            if (potion != null)
                                p.DiscardPotionInternal(potion);
                        }
                        p.SubtractFromMaxPotionCount(-diff);
                    }
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
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedGoldValue = (int)v; },
                () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.Gold; }));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.currentHp", "Lock Current HP"), 1, 9999,
                () => DevModeState.StatModifiers?.LockCurrentHp ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockCurrentHp = v; },
                () => DevModeState.StatModifiers?.LockedCurrentHpValue ?? 1,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedCurrentHpValue = (int)v; },
                () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 1; return p.Creature.CurrentHp; }));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.maxHp", "Lock Max HP"), 1, 9999,
                () => DevModeState.StatModifiers?.LockMaxHp ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockMaxHp = v; },
                () => DevModeState.StatModifiers?.LockedMaxHpValue ?? 1,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedMaxHpValue = (int)v; },
                () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 1; return p.Creature.MaxHp; }));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.currentEnergy", "Lock Current Energy"), 0, 99,
                () => DevModeState.StatModifiers?.LockCurrentEnergy ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockCurrentEnergy = v; },
                () => DevModeState.StatModifiers?.LockedCurrentEnergyValue ?? 0,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedCurrentEnergyValue = (int)v; },
                () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.PlayerCombatState?.Energy ?? 0; }));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.maxEnergy", "Lock Max Energy"), 1, 99,
                () => DevModeState.StatModifiers?.LockMaxEnergy ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockMaxEnergy = v; },
                () => DevModeState.StatModifiers?.LockedMaxEnergyValue ?? 1,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedMaxEnergyValue = (int)v; },
                () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 1; return p.MaxEnergy; }));
            vbox.AddChild(CreateStatLockRow(I18N.T("statLock.stars", "Lock Stars"), 0, 999,
                () => DevModeState.StatModifiers?.LockStars ?? false,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockStars = v; },
                () => DevModeState.StatModifiers?.LockedStarsValue ?? 0,
                v => { if (DevModeState.StatModifiers != null) DevModeState.StatModifiers.LockedStarsValue = (int)v; },
                () => { if (!RunContext.TryGetRunAndPlayer(out _, out var p)) return 0; return p.PlayerCombatState?.Stars ?? 0; }));
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

            var btnBox = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter
            };
            btnBox.AddThemeConstantOverride("separation", 12);

            var newTestBtn = CreateOverlayButton(I18N.T("panel.newTest", "New Test"), MdiIcon.Plus);
            newTestBtn.Pressed += () => { CloseOverlay(globalUi); actions.OnNewTest(); };
            btnBox.AddChild(newTestBtn);

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
