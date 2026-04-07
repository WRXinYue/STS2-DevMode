using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Actions.CardModes;

internal sealed class UpgradeModeHandler : CardSelectorModeHandler
{
    public override string Id => "upgrade";

    protected override List<CardModel> FilterCards(List<CardModel> cards)
        => cards.Where(c => c.CurrentUpgradeLevel < c.MaxUpgradeLevel).ToList();

    protected override Task ExecuteAsync(RunState state, Player player)
        => CardActions.UpgradeCards(player);
}
