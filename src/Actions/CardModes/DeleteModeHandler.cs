using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Actions.CardModes;

internal sealed class DeleteModeHandler : CardSelectorModeHandler
{
    public override string Id => "delete";

    protected override List<CardModel> FilterCards(List<CardModel> cards) => cards;

    protected override Task ExecuteAsync(RunState state, Player player)
        => CardActions.RemoveCards(state, player);
}
