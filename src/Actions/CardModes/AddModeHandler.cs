using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using DevMode.Navigation;

namespace DevMode.Actions.CardModes;

internal sealed class AddModeHandler : ICardModeHandler
{
    public string Id => "add";
    public bool ShowTargets => true;
    public bool ShowDuration => true;
    public bool RefreshOnTargetChange => false;

    public bool HasRelevantCards(Player player, CardTarget target)
        => target == CardTarget.Deck || player.PlayerCombatState != null;

    public void Execute(NGlobalUi globalUi, DevPanel.ActionSession session, RunState state, Player player)
    {
        RunContext.Begin(state, player);
        if (!NavigationHelper.TryOpenCardLibrary(state))
            RunContext.Clear();
    }

    public bool TryHandleCardSelection(NGlobalUi globalUi, NCardHolder holder,
                                       RunState state, Player player)
    {
        TaskHelper.RunSafely(CardActions.AddCard(state, player, holder.CardModel!));
        return true;
    }

    public void OnLibraryClosed(NGlobalUi globalUi) { }
}
