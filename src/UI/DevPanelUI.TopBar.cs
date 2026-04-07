using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Actions.CardModes;

namespace DevMode.UI;

internal static partial class DevPanelUI
{
    private static Action? _onCombatKill;
    public static void SetCombatKillCallback(Action? callback) => _onCombatKill = callback;

    public static void UpdateTopBar(NGlobalUi globalUi, CardTopBarConfig cardConfig = default)
    {
        RemoveTopBar(globalUi);

        if (DevModeState.ActivePanel == ActivePanel.None)
            return;

        float barHalfW = DevModeState.ActivePanel switch
        {
            ActivePanel.Cards   => !cardConfig.ShowTargets ? 130 : cardConfig.ShowDuration ? 340 : 270,
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
            BuildCardTopBar(bar, cardConfig);
        else if (DevModeState.ActivePanel == ActivePanel.Enemies)
            BuildEnemyTopBar(bar);
        else
            BuildRelicTopBar(bar);

        ((Node)globalUi).AddChild(bar);
    }

    private static void BuildCardTopBar(HBoxContainer bar, CardTopBarConfig config)
    {
        var modeLabels  = new[] { I18N.T("topbar.card.view","View"), I18N.T("topbar.card.add","Add"), I18N.T("topbar.card.upgrade","Upgrade"), I18N.T("topbar.card.edit","Edit"), I18N.T("topbar.card.delete","Delete") };
        var modes       = new[] { CardMode.View, CardMode.Add, CardMode.Upgrade, CardMode.Edit, CardMode.Delete };
        var modeButtons = new Button[modeLabels.Length];

        var targetLabels  = new[] { I18N.T("topbar.card.hand","Hand"), I18N.T("topbar.card.drawPile","Draw Pile"), I18N.T("topbar.card.discardPile","Discard"), I18N.T("topbar.card.deck","Deck") };
        var targets       = new[] { CardTarget.Hand, CardTarget.DrawPile, CardTarget.DiscardPile, CardTarget.Deck };
        var targetButtons = config.ShowTargets ? new Button[targetLabels.Length] : null;

        var durationLabels  = new[] { I18N.T("topbar.card.temporary","Temp"), I18N.T("topbar.card.permanent","Perm") };
        var durations       = new[] { EffectDuration.Temporary, EffectDuration.Permanent };
        var durationButtons = config.ShowDuration ? new Button[durationLabels.Length] : null;

        bool refreshOnTargetChange = config.RefreshOnTargetChange;
        var  targetAvailable       = config.TargetAvailable;

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
                    bool available = targetAvailable == null || targetAvailable(targets[i]);
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

        if (config.ShowTargets && targetButtons != null)
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
                    if (refreshOnTargetChange)
                        _onRefreshPanel?.Invoke();
                };
                targetButtons[i] = btn;
                bar.AddChild(btn);
            }
        }

        if (config.ShowDuration && durationButtons != null)
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

    private static void RemoveTopBar(NGlobalUi globalUi)
    {
        var old = ((Node)globalUi).GetNodeOrNull<Control>(TopBarName);
        if (old != null)
        {
            ((Node)globalUi).RemoveChild(old);
            old.QueueFree();
        }
    }
}
