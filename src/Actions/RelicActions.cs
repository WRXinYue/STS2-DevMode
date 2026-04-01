using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;

namespace DevMode.Actions;

internal static class RelicActions
{
    public static async Task AddRelic(RelicModel relic, Player player)
    {
        await RelicCmd.Obtain(relic.ToMutable(), player, -1);
        MainFile.Logger.Info($"RelicActions: Added relic {((AbstractModel)relic).Id.Entry}");
    }

    public static async Task RemoveRelics(Player player)
    {
        await Task.Yield();
        var relics = player.Relics.ToList();
        if (relics.Count == 0)
        {
            MainFile.Logger.Info("RelicActions: No relics to remove.");
            return;
        }

        var screen = NChooseARelicSelection.ShowScreen((IReadOnlyList<RelicModel>)relics);
        if (screen == null) return;

        var selected = (await screen.RelicsSelected())
            .Where(r => r != null).ToList();

        if (selected.Count == 0) return;

        foreach (var relic in selected)
        {
            var owned = player.Relics.FirstOrDefault(r => r == relic)
                ?? player.GetRelicById(((AbstractModel)relic).Id);
            if (owned == null) continue;

            await RelicCmd.Remove(owned);
        }

        MainFile.Logger.Info($"RelicActions: Removed {selected.Count} relic(s)");
    }
}
