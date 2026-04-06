using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using DevMode.Actions;
using DevMode.AI;
using DevMode.Navigation;
using DevMode.UI;

namespace DevMode;

/// <summary>
/// Facade / coordinator — delegates to specialized modules:
///   UI       → <see cref="DevPanelUI"/>
///   Cards    → <see cref="CardActions"/>
///   Relics   → <see cref="RelicActions"/>
///   Nav      → <see cref="NavigationHelper"/>
///   Context  → <see cref="RunContext"/>
/// </summary>
internal static class DevPanel
{
    // ──────── ActionSession ────────
    // Runs one async action at a time. Calling Cancel() before a new Run()
    // prevents the old action's completion callback from firing, eliminating
    // the race between TryDismissCurrent and in-flight async wrappers.

    private sealed class ActionSession
    {
        private int  _gen;
        public bool IsBusy { get; private set; }

        /// <summary>Invalidate the in-flight action (if any).</summary>
        public void Cancel()
        {
            _gen++;
            IsBusy = false;
        }

        /// <summary>
        /// Run <paramref name="work"/> asynchronously.
        /// <paramref name="onCompleted"/> is only called when the action finishes
        /// naturally — not when it was superseded by Cancel() or a newer Run().
        /// </summary>
        public void Run(Func<Task> work, string label, Action onCompleted)
        {
            IsBusy = true;
            int myGen = ++_gen;
            TaskHelper.RunSafely(Execute(work, label, myGen, onCompleted));
        }

        private async Task Execute(Func<Task> work, string label, int myGen, Action onCompleted)
        {
            try   { await work(); }
            catch (Exception ex) { MainFile.Logger.Warn($"DevPanel: {label} failed: {ex.Message}"); }
            finally
            {
                IsBusy = false;
                if (_gen == myGen)
                    onCompleted();
            }
        }
    }

    // ──────── State ────────

    private static readonly ActionSession _session = new();
    private static NGlobalUi? _globalUi;

    // ──────── Lifecycle ────────

    public static void Attach(NGlobalUi globalUi)
    {
        try
        {
            _globalUi = globalUi;

            var actions = new DevPanelActions
            {
                OnOpenCards   = OpenCards,
                OnOpenRelics  = OpenRelics,
                OnOpenEnemies = OpenEnemies,
                OnOpenPowers  = OpenPowers,
                OnOpenPotions = OpenPotions,
                OnOpenEvents  = OpenEvents,
                OnOpenConsole = OpenConsole,
                OnOpenPresets = OpenPresets,
                OnOpenCardEdit = OpenCardEdit,
                OnOpenSave    = () => SaveSlotUI.Show(globalUi, saveMode: true,
                                    slot => SaveSlotManager.SaveToSlot(slot)),
                OnOpenLoad    = () => SaveSlotUI.Show(globalUi, saveMode: false,
                                    slot => SaveSlotManager.LoadFromSlot(slot)),
                OnRefreshPanel = RefreshPanel,
                OnToggleAI      = AIControl.IsAvailable ? AIControl.Toggle : null,
                OnCycleStrategy = AIControl.IsAvailable ? AIControl.CycleStrategy : null,
                OnCycleSpeed    = AIControl.IsAvailable ? AIControl.CycleSpeed : null,
                IsAIEnabled     = AIControl.IsAvailable ? () => AIControl.IsEnabled : null,
                GetStrategyName = AIControl.IsAvailable ? AIControl.GetStrategyName : null,
                GetSpeedLabel   = AIControl.IsAvailable ? AIControl.GetSpeedLabel : null,
                OnCycleGameSpeed   = SpeedControl.CycleSpeed,
                GetGameSpeedLabel  = SpeedControl.GetLabel,
                OnToggleSkipAnim   = SkipAnimControl.Toggle,
                GetSkipAnimLabel   = SkipAnimControl.GetLabel,
            };

            DevPanelUI.Attach(globalUi, actions);
            ((Node)globalUi).TreeExiting += () => Detach(globalUi);

            MainFile.Logger.Info("DevPanel: Sidebar attached.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"DevPanel: Failed to attach sidebar: {ex.Message}");
        }
    }

    public static void Detach(NGlobalUi globalUi)
    {
        try
        {
            DevPanelUI.Detach(globalUi);
            ClearState();
            SpeedControl.Reset();
            SkipAnimControl.Reset();
            _globalUi = null;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"DevPanel: Failed to detach: {ex.Message}");
        }
    }

    // ──────── Panel Openers ────────

    private static void OpenCards()
    {
        if (!TryDismissCurrent()) return;
        DevModeState.ActivePanel = ActivePanel.Cards;
        UpdateTopBar();

        switch (DevModeState.CardMode)
        {
            case CardMode.View:
                if (!RunContext.TryGetRunAndPlayer(out var state, out _)) return;
                NavigationHelper.TryOpenCardLibrary(state);
                break;

            case CardMode.Add:
                if (!RunContext.TryGetRunAndPlayer(out state, out var addPlayer)) return;
                RunContext.Begin(state, addPlayer);
                if (!NavigationHelper.TryOpenCardLibrary(state))
                    ClearState();
                break;

            case CardMode.Upgrade:
                NavigationHelper.ClosePauseMenu();
                if (!RunContext.TryGetRunAndPlayer(out _, out var player)) return;
                _session.Run(
                    () => CardActions.UpgradeCards(player),
                    "Upgrade cards",
                    onCompleted: ResetPanel
                );
                break;

            case CardMode.Delete:
                NavigationHelper.ClosePauseMenu();
                if (!RunContext.TryGetRunAndPlayer(out state, out player)) return;
                _session.Run(
                    () => CardActions.RemoveCards(state, player),
                    "Remove cards",
                    onCompleted: ResetPanel
                );
                break;
        }
    }

    private static void OpenRelics()
    {
        if (!TryDismissCurrent()) return;
        DevModeState.ActivePanel = ActivePanel.Relics;
        UpdateTopBar();

        switch (DevModeState.RelicMode)
        {
            case RelicMode.View:
                if (!RunContext.TryGetRunAndPlayer(out var state, out _)) return;
                NavigationHelper.TryOpenRelicCollection(state);
                break;

            case RelicMode.Add:
                if (!RunContext.TryGetRunAndPlayer(out state, out var player)) return;
                RunContext.Begin(state, player);
                if (!NavigationHelper.TryOpenRelicCollection(state))
                    ClearState();
                break;

            case RelicMode.Delete:
                NavigationHelper.ClosePauseMenu();
                if (!RunContext.TryGetRunAndPlayer(out _, out player)) return;
                _session.Run(
                    () => RelicActions.RemoveRelics(player),
                    "Remove relics",
                    onCompleted: ResetPanel
                );
                break;
        }
    }

    private static void OpenEnemies()
    {
        if (_globalUi == null) return;
        DevModeState.ActivePanel = ActivePanel.Enemies;
        UpdateTopBar();

        // Wire up the combat kill callback
        DevPanelUI.SetCombatKillCallback(() =>
        {
            if (_globalUi != null)
                EnemySelectUI.ShowEnemyKillPicker(_globalUi);
        });

        // Check if we're in combat — if so, default to showing the monster picker
        var combatState = CombatEnemyActions.GetCombatState();

        switch (DevModeState.EnemyMode)
        {
            case EnemyMode.Global:
                if (combatState != null)
                {
                    // In combat: show encounter picker to add enemies
                    EnemySelectUI.Show(_globalUi, null, enc =>
                    {
                        TaskHelper.RunSafely(CombatEnemyActions.AddEncounterMonsters(enc));
                    });
                }
                else
                {
                    EnemySelectUI.Show(_globalUi, null, enc =>
                    {
                        EnemyActions.SetGlobalOverride(enc);
                        UpdateTopBar();
                    });
                }
                break;

            case EnemyMode.PerType:
                ShowRoomTypePicker();
                break;

            case EnemyMode.Off:
                if (combatState != null)
                {
                    // In combat with no override mode: show encounter picker
                    EnemySelectUI.Show(_globalUi, null, enc =>
                    {
                        TaskHelper.RunSafely(CombatEnemyActions.AddEncounterMonsters(enc));
                    });
                }
                else
                {
                    EnemySelectUI.ShowFloorPicker(_globalUi);
                }
                break;
        }
    }

    private static void ShowRoomTypePicker()
    {
        if (_globalUi == null) return;

        // Show encounter selector filtered by each room type in sequence
        // For simplicity, show the full selector with filter tabs
        EnemySelectUI.Show(_globalUi, RoomType.Monster, enc =>
        {
            EnemyActions.SetRoomTypeOverride(enc.RoomType, enc);
            UpdateTopBar();
        });
    }

    private static void OpenPowers()
    {
        if (_globalUi == null) return;
        TryDismissCurrent();
        DevModeState.ActivePanel = ActivePanel.Powers;
        UpdateTopBar();

        if (!RunContext.TryGetRunAndPlayer(out _, out var player)) return;

        PowerSelectUI.Show(_globalUi, (power, amount, target) =>
        {
            TaskHelper.RunSafely(PowerActions.AddPower(player, power, amount, target));
        });
    }

    private static void OpenPotions()
    {
        if (_globalUi == null) return;
        TryDismissCurrent();
        DevModeState.ActivePanel = ActivePanel.Potions;
        UpdateTopBar();

        if (!RunContext.TryGetRunAndPlayer(out _, out var player)) return;

        PotionSelectUI.Show(_globalUi, potion =>
        {
            PotionActions.AddPotion(player, potion);
        });
    }

    private static void OpenEvents()
    {
        if (_globalUi == null) return;
        TryDismissCurrent();
        DevModeState.ActivePanel = ActivePanel.Events;
        UpdateTopBar();

        EventSelectUI.Show(_globalUi, evt =>
        {
            EventActions.TryForceEnterEvent(evt);
        });
    }

    private static void OpenConsole()
    {
        if (_globalUi == null) return;
        TryDismissCurrent();
        DevModeState.ActivePanel = ActivePanel.Console;
        UpdateTopBar();

        ConsoleUI.Show(_globalUi);
    }

    private static void OpenPresets()
    {
        if (_globalUi == null) return;
        TryDismissCurrent();
        DevModeState.ActivePanel = ActivePanel.Presets;
        UpdateTopBar();

        PresetUI.Show(_globalUi);
    }

    private static void OpenCardEdit()
    {
        if (_globalUi == null) return;
        TryDismissCurrent();
        DevModeState.ActivePanel = ActivePanel.CardEdit;
        UpdateTopBar();

        if (!RunContext.TryGetRunAndPlayer(out _, out var player)) return;

        CardEditUI.Show(_globalUi, player);
    }

    private static void RefreshPanel()
    {
        switch (DevModeState.ActivePanel)
        {
            case ActivePanel.Cards:    OpenCards();    break;
            case ActivePanel.Relics:   OpenRelics();   break;
            case ActivePanel.Enemies:  OpenEnemies();  break;
            case ActivePanel.Powers:   OpenPowers();   break;
            case ActivePanel.Potions:  OpenPotions();  break;
            case ActivePanel.Events:   OpenEvents();   break;
            case ActivePanel.Console:  OpenConsole();  break;
            case ActivePanel.Presets:  OpenPresets();  break;
            case ActivePanel.CardEdit: OpenCardEdit(); break;
        }
    }

    // ──────── Panel Switching ────────

    private static bool TryDismissCurrent()
    {
        if (_session.IsBusy) RunContext.Clear();
        _session.Cancel();                     // invalidate before closing overlays
        NavigationHelper.CloseCapstone();
        NavigationHelper.CloseOverlays();

        // Dismiss all DevMode overlay panels in one shot
        if (_globalUi != null)
            DevPanelUI.CloseAllOverlays(_globalUi);

        return true;
    }

    // ──────── Interception Handlers (called from Harmony patches) ────────

    public static bool TryHandleCardSelection(NCardHolder holder)
    {
        if (DevModeState.ActivePanel != ActivePanel.Cards || DevModeState.CardMode != CardMode.Add)
            return false;

        if (holder?.CardModel == null) return true;

        if (!RunContext.TryResolvePending(out var state, out var player))
        {
            ClearState();
            return true;
        }

        TaskHelper.RunSafely(CardActions.AddCard(state, player, holder.CardModel));
        return true;
    }

    public static bool TryHandleRelicSelection(NRelicCollectionEntry entry)
    {
        if (DevModeState.ActivePanel != ActivePanel.Relics || DevModeState.RelicMode != RelicMode.Add)
            return false;

        if (entry?.relic == null) return true;

        if (!RunContext.TryResolvePending(out _, out var player))
        {
            ClearState();
            return true;
        }

        TaskHelper.RunSafely(RelicActions.AddRelic(entry.relic.CanonicalInstance, player));
        return true;
    }

    // ──────── Lifecycle Notifications ────────

    public static void NotifyCardLibraryClosed()
    {
        if (DevModeState.ActivePanel != ActivePanel.Cards) return;
        if (DevModeState.CardMode is CardMode.View)
        {
            ResetPanel();
            ClearState();
            // TryOpenCardLibrary opens NPauseMenu as a backing screen; close the
            // whole capstone so the pause menu doesn't linger after the library exits.
            NavigationHelper.CloseCapstone();
        }
        else if (DevModeState.CardMode is CardMode.Add)
        {
            ResetPanel();
            ClearState();
        }
    }

    public static void NotifyRelicCollectionClosed()
    {
        if (DevModeState.ActivePanel != ActivePanel.Relics) return;
        if (DevModeState.RelicMode is RelicMode.View)
        {
            ResetPanel();
            ClearState();
            NavigationHelper.CloseCapstone();
        }
        else if (DevModeState.RelicMode is RelicMode.Add)
        {
            ResetPanel();
            ClearState();
        }
    }

    // ──────── Private ────────

    private static void ResetPanel()
    {
        DevModeState.ActivePanel = ActivePanel.None;
        UpdateTopBar();
    }

    private static void UpdateTopBar()
    {
        if (_globalUi == null) return;

        Func<CardTarget, bool>? cardTargetAvailable = null;
        if (DevModeState.ActivePanel == ActivePanel.Cards
            && DevModeState.CardMode != CardMode.View
            && RunContext.TryGetRunAndPlayer(out _, out var player))
        {
            var mode = DevModeState.CardMode;
            cardTargetAvailable = target => CardActions.HasRelevantCards(player, target, mode);
        }

        DevPanelUI.UpdateTopBar(_globalUi, cardTargetAvailable);
    }

    private static void ClearState()
    {
        RunContext.Clear();
    }
}
