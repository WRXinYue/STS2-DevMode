using System;
using System.Text.Json.Nodes;
using DevMode.AI.Knowledge;

namespace DevMode.AI.Combat;

/// <summary>Dynamic setup-vs-attack comparison from live snapshot metrics.</summary>
internal static class CombatSetupEvaluator {
    public static int ComputeVulnerableDeferValue(
        JsonObject snapshot,
        JsonArray? hand,
        int energy,
        JsonObject? targetEnemy,
        int vulnStacks,
        int vulnCost) {
        if (hand == null || vulnStacks <= 0 || vulnCost > energy)
            return 0;
        if (CombatPowerReader.GetVulnerable(targetEnemy) > 0)
            return 0;

        var immediateDamage = CombatCardStats.EstimateFollowupAttackDamage(hand, energy);
        var energyAfter = energy - vulnCost;
        var deferredDamage = CombatCardStats.EstimateFollowupAttackDamage(hand, energyAfter);
        var vulnPayoff = (int)Math.Round(deferredDamage * 1.5f);

        var value = vulnPayoff / 2 + vulnStacks * 2;

        var canLethal = LethalChecker.CanLethal(snapshot, out _);
        var incoming = IntentCalculator.TotalIncomingDamage(snapshot);
        var net = IntentCalculator.NetDamageAfterBlock(snapshot);
        var urgency = IntentCalculator.BlockUrgency(snapshot);

        if (!canLethal && incoming > 0) {
            value += urgency / 5 + net / 4;
        }

        if (targetEnemy != null) {
            var hp = targetEnemy["currentHp"]?.GetValue<int>() ?? 0;
            var maxHp = targetEnemy["maxHp"]?.GetValue<int>() ?? 1;
            if (maxHp > 0 && hp <= maxHp * 0.3f && canLethal)
                value = value * 2 / 3;
        }

        if (immediateDamage > vulnPayoff + 8)
            value = Math.Max(0, value - (immediateDamage - vulnPayoff) / 3);

        return Math.Max(0, value);
    }

    public static int ComputeBestVulnerableDeferValue(
        JsonObject snapshot,
        JsonArray? hand,
        int energy,
        JsonObject? targetEnemy) {
        if (hand == null) return 0;

        var best = 0;
        for (var i = 0; i < hand.Count; i++) {
            var card = hand[i]?.AsObject();
            if (card == null) continue;
            if (card["canPlay"]?.GetValue<bool>() == false) continue;

            var profile = CombatCardStats.ResolveProfile(card);
            if (!profile.Flags.HasFlag(CardMechanicFlags.AppliesVulnerable))
                continue;
            if (profile.AppliedVulnerable <= 0) continue;

            var cost = card["cost"]?.GetValue<int>() ?? 99;
            if (cost > energy) continue;

            var value = ComputeVulnerableDeferValue(
                snapshot, hand, energy, targetEnemy, profile.AppliedVulnerable, cost);
            if (value > best)
                best = value;
        }

        return best;
    }

    public static int ComputeVulnerableDeferOpportunityCost(
        JsonObject snapshot,
        JsonArray? hand,
        int energy,
        JsonObject? targetEnemy,
        int attackDamage) {
        var deferValue = ComputeBestVulnerableDeferValue(snapshot, hand, energy, targetEnemy);
        if (deferValue <= 0)
            return 0;

        return Math.Max(0, (deferValue - attackDamage) / 2);
    }
}
