using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace DevMode.Actions;

internal static class PotionActions {
    public static IEnumerable<PotionModel> GetAllPotions() => ModelDb.AllPotions;

    public static Task AddPotion(Player player, PotionModel canonicalPotion)
        => PotionCmd.TryToProcure(canonicalPotion.ToMutable(), player);

    public static Task DiscardPotion(PotionModel ownedPotion)
        => PotionCmd.Discard(ownedPotion);

    public static string GetPotionDisplayName(PotionModel potion) {
        try { return potion.Title?.GetFormattedText() ?? ((AbstractModel)potion).Id.Entry ?? "?"; }
        catch { return ((AbstractModel)potion).Id.Entry ?? "?"; }
    }
}
