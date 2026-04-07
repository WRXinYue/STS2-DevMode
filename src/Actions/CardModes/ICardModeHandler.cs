using System;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using ActionSession = DevMode.DevPanel.ActionSession;

namespace DevMode.Actions.CardModes;

/// <summary>
/// Strategy interface for card panel modes (View, Add, Upgrade, Delete, Edit).
/// Each handler encapsulates the mode-specific behavior that was previously
/// spread across DevPanel's switch/if chains.
/// </summary>
internal interface ICardModeHandler
{
    string Id { get; }

    // ── TopBar configuration ──

    bool ShowTargets { get; }
    bool ShowDuration { get; }
    bool RefreshOnTargetChange { get; }

    // ── Availability ──

    bool HasRelevantCards(Player player, CardTarget target);

    // ── Lifecycle ──

    void Execute(NGlobalUi globalUi, ActionSession session, RunState state, Player player);

    /// <returns>true if the selection was handled (suppress default behavior).</returns>
    bool TryHandleCardSelection(NGlobalUi globalUi, NCardHolder holder,
                                RunState state, Player player);

    void OnLibraryClosed(NGlobalUi globalUi);
}

/// <summary>
/// Serialized configuration passed from handler to TopBar so the TopBar
/// never needs to know about individual CardMode values.
/// </summary>
internal readonly struct CardTopBarConfig
{
    public readonly bool ShowTargets;
    public readonly bool ShowDuration;
    public readonly bool RefreshOnTargetChange;
    public readonly Func<CardTarget, bool>? TargetAvailable;

    public CardTopBarConfig(ICardModeHandler handler, Player? player)
    {
        ShowTargets = handler.ShowTargets;
        ShowDuration = handler.ShowDuration;
        RefreshOnTargetChange = handler.RefreshOnTargetChange;
        TargetAvailable = ShowTargets && player != null
            ? target => handler.HasRelevantCards(player, target)
            : null;
    }

    public static readonly CardTopBarConfig None = default;
}
