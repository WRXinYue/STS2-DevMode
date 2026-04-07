using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using DevMode.UI;

namespace DevMode.Actions.CardModes;

internal sealed class EditModeHandler : ICardModeHandler
{
    public string Id => "edit";
    public bool ShowTargets => true;
    public bool ShowDuration => true;
    public bool RefreshOnTargetChange => true;

    public bool HasRelevantCards(Player player, CardTarget target)
        => CardActions.GetCardsForTarget(player, target).Count > 0;

    public void Execute(NGlobalUi globalUi, DevPanel.ActionSession session, RunState state, Player player)
    {
        var cards = CardActions.GetCardsForTarget(player, DevModeState.CardTarget);
        CardEditUI.Show(globalUi, player, cards);
    }

    public bool TryHandleCardSelection(NGlobalUi globalUi, NCardHolder holder,
                                       RunState state, Player player) => false;

    public void OnLibraryClosed(NGlobalUi globalUi) { }
}
