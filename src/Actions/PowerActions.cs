using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace DevMode.Actions;

internal static class PowerActions
{
    public static IEnumerable<PowerModel> GetAllPowers() => ModelDb.AllPowers;

    public static async Task AddPower(Player player, PowerModel power, int amount, PowerTarget target)
    {
        switch (target)
        {
            case PowerTarget.Self:
                await ApplyPower(power, amount, player.Creature, player.Creature);
                break;

            case PowerTarget.AllEnemies:
                var combatState = CombatManager.Instance.DebugOnlyGetState();
                if (combatState == null) return;
                foreach (var enemy in combatState.Enemies.Where(e => e.IsAlive).ToArray())
                    await ApplyPower(power, amount, enemy, player.Creature);
                break;

            case PowerTarget.Allies:
                var cs = CombatManager.Instance.DebugOnlyGetState();
                if (cs == null) return;
                foreach (var ally in cs.Allies.Where(c => c.IsAlive).ToArray())
                    await ApplyPower(power, amount, ally, player.Creature);
                break;
        }
    }

    private static async Task ApplyPower(PowerModel power, int amount, Creature target, Creature source)
    {
        // Try the full overload: Apply(MutablePowerModel, Creature target, decimal amount, Creature source, CardModel? card, bool silent)
        try
        {
            var mutable = power.ToMutable(amount);
            await PowerCmd.Apply(mutable, target, (decimal)amount, source, (CardModel?)null, false);
        }
        catch
        {
            // Fallback: try reflection for simpler overloads
            var methods = typeof(PowerCmd).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.Name == "Apply").ToArray();
            foreach (var method in methods)
            {
                try
                {
                    var result = method.Invoke(null, BuildPowerApplyArgs(method, power, amount, target, source));
                    if (result is Task task) await task;
                    return;
                }
                catch { continue; }
            }
        }
    }

    private static object?[] BuildPowerApplyArgs(MethodInfo method, PowerModel power, int amount, Creature target, Creature source)
    {
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var pt = parameters[i].ParameterType;
            if (pt == typeof(PowerModel)) args[i] = power;
            else if (pt.Name.Contains("MutablePower")) args[i] = power.ToMutable(amount);
            else if (pt == typeof(Creature)) args[i] = i == 0 || parameters[i].Name?.Contains("target", StringComparison.OrdinalIgnoreCase) == true ? target : source;
            else if (pt == typeof(int)) args[i] = amount;
            else if (pt == typeof(decimal)) args[i] = (decimal)amount;
            else if (pt == typeof(bool)) args[i] = false;
            else if (pt == typeof(CardModel)) args[i] = null;
            else if (parameters[i].HasDefaultValue) args[i] = parameters[i].DefaultValue;
            else args[i] = null;
        }
        return args;
    }

    public static void RemovePower(Creature creature, PowerModel power)
    {
        var match = creature.Powers.FirstOrDefault(p => p?.Id == power.Id);
        if (match != null)
            PowerCmd.Remove(match);
    }

    public static void RemoveAllPowers(Creature creature)
    {
        foreach (var power in creature.Powers.ToArray())
        {
            if (power != null)
                PowerCmd.Remove(power);
        }
    }

    public static string GetPowerDisplayName(PowerModel power)
    {
        try { return power.Title?.GetFormattedText() ?? ((AbstractModel)power).Id.Entry ?? "?"; }
        catch { return ((AbstractModel)power).Id.Entry ?? "?"; }
    }
}
