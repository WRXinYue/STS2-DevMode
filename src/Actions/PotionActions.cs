using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace DevMode.Actions;

internal static class PotionActions {
    private const BindingFlags ReflFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static IEnumerable<PotionModel> GetAllPotions() => ModelDb.AllPotions;

    public static Task AddPotion(Player player, PotionModel canonicalPotion)
        => PotionCmd.TryToProcure(canonicalPotion.ToMutable(), player);

    public static Task DiscardPotion(PotionModel ownedPotion)
        => PotionCmd.Discard(ownedPotion);

    public static string GetPotionDisplayName(PotionModel potion) {
        try { return potion.Title?.GetFormattedText() ?? ((AbstractModel)potion).Id.Entry ?? "?"; }
        catch { return ((AbstractModel)potion).Id.Entry ?? "?"; }
    }

    /// <summary>Formatted potion body text; uses reflection so it survives STS2 renames (<c>Description</c> → <c>DynamicDescription</c>, etc.).</summary>
    public static string? GetPotionDescriptionFormatted(PotionModel potion) {
        foreach (var name in new[] { "DynamicDescription", "Description", "_descriptionLocString" }) {
            try {
                var prop = typeof(PotionModel).GetProperty(name, ReflFlags);
                if (prop?.GetValue(potion) is LocString loc)
                    return loc.GetFormattedText();
                var field = typeof(PotionModel).GetField(name, ReflFlags);
                if (field?.GetValue(potion) is LocString loc2)
                    return loc2.GetFormattedText();
            }
            catch { }
        }

        return null;
    }
}
