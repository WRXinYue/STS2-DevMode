using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace DevMode.Actions;

internal static class PowerActions
{
    public static IEnumerable<PowerModel> GetAllPowers() => ModelDb.AllPowers;

    public static async Task AddPower(Player player, PowerModel power, int amount, PowerTarget target)
    {
        if (!CombatManager.Instance.IsInProgress)
        {
            MainFile.Logger.Warn("[DevMode] AddPower: no active combat — powers require an in-progress combat session.");
            return;
        }

        switch (target)
        {
            case PowerTarget.Self:
                await ApplyPower(power, amount, player.Creature, player.Creature);
                break;

            case PowerTarget.AllEnemies:
            {
                var cs = CombatManager.Instance.DebugOnlyGetState();
                if (cs == null) return;
                foreach (var enemy in cs.Enemies.Where(e => e.IsAlive).ToArray())
                    await ApplyPower(power, amount, enemy, player.Creature);
                break;
            }

            case PowerTarget.Allies:
            {
                var cs = CombatManager.Instance.DebugOnlyGetState();
                if (cs == null) return;
                foreach (var ally in cs.Allies.Where(c => c.IsAlive).ToArray())
                    await ApplyPower(power, amount, ally, player.Creature);
                break;
            }

            case PowerTarget.SpecificTarget:
                // No interactive target picker in DevMode: fall back to Self
                await ApplyPower(power, amount, player.Creature, player.Creature);
                break;
        }
    }

    private static async Task ApplyPower(PowerModel power, int amount, Creature target, Creature source)
    {
        try
        {
            // ToMutable(0) avoids the NullRef: ToMutable(amount > 0) calls SetAmount which calls
            // Owner.InvokePowerModified before Owner is set, throwing a NullReferenceException.
            // The actual amount is passed to PowerCmd.Apply and applied correctly via ApplyInternal.
            var mutable = power.ToMutable(0);
            await PowerCmd.Apply(mutable, target, (decimal)amount, source, null, false);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[DevMode] ApplyPower failed ({((AbstractModel)power).Id.Entry}): {ex.Message}");
        }
    }

    public static void RemovePower(Creature creature, PowerModel power)
    {
        var match = creature.Powers.FirstOrDefault(p => p?.Id == power.Id);
        if (match != null)
            TaskHelper.RunSafely(PowerCmd.Remove(match));
    }

    public static void RemoveAllPowers(Creature creature)
    {
        foreach (var p in creature.Powers.ToArray())
        {
            if (p != null)
                TaskHelper.RunSafely(PowerCmd.Remove(p));
        }
    }

    public static string GetPowerDisplayName(PowerModel power)
    {
        try { return power.Title?.GetFormattedText() ?? ((AbstractModel)power).Id.Entry ?? "?"; }
        catch { return ((AbstractModel)power).Id.Entry ?? "?"; }
    }
}
