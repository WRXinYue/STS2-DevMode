using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using DevMode.Navigation;

namespace DevMode.Actions;

internal static class CardActions
{
    public static async Task RemoveCards(RunState state, Player player)
    {
        await Task.Yield();

        var target = DevModeState.CardTarget;
        var duration = DevModeState.EffectDuration;

        var cards = CollectCardsForTarget(player, target);
        if (cards.Count == 0)
        {
            MainFile.Logger.Info("CardActions: No cards to remove.");
            return;
        }

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1, cards.Count)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        var screen = NDeckCardSelectScreen.Create((IReadOnlyList<CardModel>)cards, prefs);
        var overlayStack = NOverlayStack.Instance;
        if (overlayStack == null) return;

        overlayStack.Push((IOverlayScreen)screen);
        var selected = (await screen.CardsSelected())
            .Where(c => c != null).Distinct().ToList();

        if (selected.Count == 0) return;

        if (target == CardTarget.Deck)
        {
            // RemoveFromDeck handles preview animation + permanent state removal.
            try
            {
                await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)selected, true);
            }
            catch
            {
                foreach (var card in selected)
                {
                    card.RemoveFromState();
                    if (state.ContainsCard(card))
                        state.RemoveCard(card);
                }
            }
        }
        else
        {
            // Combat piles (Hand / DrawPile / DiscardPile):
            // RemoveFromCombat handles the hand-UI visual update and animation.
            await CardPileCmd.RemoveFromCombat(selected);

            if (duration == EffectDuration.Permanent)
            {
                // Also purge from the permanent deck.
                foreach (var card in selected)
                {
                    if (state.ContainsCard(card))
                        state.RemoveCard(card);
                }
            }
        }

        MainFile.Logger.Info($"CardActions: Removed {selected.Count} card(s) ({duration})");
    }

    public static async Task UpgradeCards(Player player)
    {
        await Task.Yield();

        var target = DevModeState.CardTarget;
        var cards = CollectCardsForTarget(player, target);
        var upgradable = cards
            .Where(c => c.CurrentUpgradeLevel < c.MaxUpgradeLevel)
            .ToList();

        if (upgradable.Count == 0)
        {
            MainFile.Logger.Info("CardActions: No upgradable cards found.");
            return;
        }

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1, upgradable.Count)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        var screen = NDeckCardSelectScreen.Create((IReadOnlyList<CardModel>)upgradable, prefs);
        var overlayStack = NOverlayStack.Instance;
        if (overlayStack == null) return;

        overlayStack.Push((IOverlayScreen)screen);
        var selected = (await screen.CardsSelected())
            .Where(c => c != null).Distinct().ToList();

        if (selected.Count == 0) return;

        // Close the selection overlay so the upgrade animation is visible
        NavigationHelper.CloseOverlays();
        await Task.Yield(); // let scene tree settle

        CardCmd.Upgrade(selected, CardPreviewStyle.HorizontalLayout);

        // CardCmd.Upgrade only creates NCardUpgradeVfx for PileType.Deck cards.
        // For combat piles (DrawPile, Hand, DiscardPile), manually add the VFX.
        var previewContainer = NRun.Instance?.GlobalUi?.CardPreviewContainer;
        if (previewContainer != null)
        {
            foreach (var card in selected)
            {
                if (card.Pile?.Type != PileType.Deck)
                    previewContainer.AddChildSafely(NCardUpgradeVfx.Create(card));
            }
        }

        MainFile.Logger.Info($"CardActions: Upgraded {selected.Count} card(s)");
    }

    public static async Task AddCard(RunState state, Player player, CardModel canonicalCard)
    {
        var target   = DevModeState.CardTarget;
        var duration = DevModeState.EffectDuration;

        if (target == CardTarget.Deck)
        {
            var card = state.CreateCard(canonicalCard.CanonicalInstance, player);
            var result = await CardPileCmd.Add(card, PileType.Deck);
            CardCmd.PreviewCardPileAdd(result);
        }
        else
        {
            var combatState = player.Creature.CombatState;
            if (combatState == null)
            {
                MainFile.Logger.Info("CardActions: Cannot add to combat pile — not in combat.");
                return;
            }

            var pileType = target switch
            {
                CardTarget.Hand        => PileType.Hand,
                CardTarget.DrawPile    => PileType.Draw,
                CardTarget.DiscardPile => PileType.Discard,
                _                      => PileType.Draw
            };

            var combatCard = combatState.CreateCard(canonicalCard.CanonicalInstance, player);
            await CardPileCmd.AddGeneratedCardToCombat(combatCard, pileType, true);

            // AddGeneratedCardToCombat silently calls AddInternal() for brand-new cards added to
            // Draw/Discard without creating any VFX. The pile-count UI (NCombatCardPile) only
            // updates via CardAddFinished, which is normally fired by the fly animation (NCardFlyVfx /
            // NCardFlyShuffleVfx). For the silent path we must fire it manually.
            if (pileType is PileType.Draw or PileType.Discard)
                combatCard.Pile?.InvokeCardAddFinished();

            if (duration == EffectDuration.Permanent)
            {
                var deckCard = state.CreateCard(canonicalCard.CanonicalInstance, player);
                await CardPileCmd.Add(deckCard, PileType.Deck, skipVisuals: true);
            }
        }

        MainFile.Logger.Info($"CardActions: Added {canonicalCard.Id.Entry} to {target} ({duration})");
    }

    public static bool HasRelevantCards(Player player, CardTarget target, CardMode mode)
    {
        if (mode == CardMode.Add)
            return target == CardTarget.Deck || player.PlayerCombatState != null;
        var cards = CollectCardsForTarget(player, target);
        if (mode == CardMode.Upgrade)
            return cards.Any(c => c.CurrentUpgradeLevel < c.MaxUpgradeLevel);
        return cards.Count > 0;
    }

    public static List<CardModel> GetCardsForTarget(Player player, CardTarget target)
    {
        return CollectCardsForTarget(player, target);
    }

    private static List<CardModel> CollectCardsForTarget(Player player, CardTarget target)
    {
        if (target == CardTarget.Deck)
            return player.Deck.Cards.ToList();

        var combatState = player.PlayerCombatState;
        if (combatState == null) return new List<CardModel>();

        return target switch
        {
            CardTarget.DrawPile    => combatState.DrawPile?.Cards.ToList() ?? new List<CardModel>(),
            CardTarget.Hand        => combatState.Hand?.Cards.ToList()     ?? new List<CardModel>(),
            CardTarget.DiscardPile => combatState.DiscardPile?.Cards.ToList() ?? new List<CardModel>(),
            _ => new List<CardModel>()
        };
    }
}
