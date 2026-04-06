using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace DevMode.Actions;

internal static class PotionActions
{
    public static IEnumerable<PotionModel> GetAllPotions() => ModelDb.AllPotions;

    public static void AddPotion(Player player, PotionModel potion)
    {
        // Try reflection to find the correct Obtain/Add method
        var methods = typeof(PotionCmd).GetMethods(BindingFlags.Static | BindingFlags.Public);
        var obtainMethod = methods.FirstOrDefault(m => m.Name is "Obtain" or "Add" or "Give");
        if (obtainMethod != null)
        {
            var parameters = obtainMethod.GetParameters();
            var args = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var pt = parameters[i].ParameterType;
                if (typeof(PotionModel).IsAssignableFrom(pt)) args[i] = potion;
                else if (typeof(Player).IsAssignableFrom(pt)) args[i] = player;
                else if (parameters[i].HasDefaultValue) args[i] = parameters[i].DefaultValue;
                else args[i] = null;
            }
            obtainMethod.Invoke(null, args);
            return;
        }

        MainFile.Logger.Warn("PotionCmd: No suitable Obtain/Add method found.");
    }

    public static void RemovePotion(Player player, PotionModel potion)
    {
        var methods = typeof(PotionCmd).GetMethods(BindingFlags.Static | BindingFlags.Public);
        var removeMethod = methods.FirstOrDefault(m => m.Name is "Remove" or "Discard" or "Delete");
        if (removeMethod != null)
        {
            var parameters = removeMethod.GetParameters();
            var args = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var pt = parameters[i].ParameterType;
                if (typeof(PotionModel).IsAssignableFrom(pt)) args[i] = potion;
                else if (typeof(Player).IsAssignableFrom(pt)) args[i] = player;
                else if (parameters[i].HasDefaultValue) args[i] = parameters[i].DefaultValue;
                else args[i] = null;
            }
            removeMethod.Invoke(null, args);
            return;
        }

        MainFile.Logger.Warn("PotionCmd: No suitable Remove method found.");
    }

    public static string GetPotionDisplayName(PotionModel potion)
    {
        try { return potion.Title?.GetFormattedText() ?? ((AbstractModel)potion).Id.Entry ?? "?"; }
        catch { return ((AbstractModel)potion).Id.Entry ?? "?"; }
    }
}
