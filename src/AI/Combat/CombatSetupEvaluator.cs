using System;
using System.Linq;
using System.Text.Json.Nodes;
using DevMode.AI.Combat.Simulation;
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
        int vulnCost,
        int vulnCardIndex = -1,
        JsonObject? vulnCard = null) {
        if (hand == null || vulnStacks <= 0 || vulnCost > energy)
            return 0;
        if (CombatPowerReader.GetVulnerable(targetEnemy) > 0)
            return 0;

        var attackHand = vulnCardIndex >= 0 ? HandWithoutIndex(hand, vulnCardIndex) : hand;
        var immediateDamage = CombatCardStats.EstimateFollowupAttackDamage(attackHand, energy);
        var energyAfter = energy - vulnCost;
        if (energyAfter < 0) return 0;

        var followupDamage = CombatCardStats.EstimateFollowupAttackDamage(attackHand, energyAfter);
        var vulnHit = vulnCard != null ? CombatCardStats.ResolveDamage(vulnCard) : 0;
        var setupPayoff = vulnHit + (int)Math.Round(followupDamage * 1.5f);
        var value = setupPayoff - immediateDamage + vulnStacks * 4;

        var canLethal = LethalChecker.CanLethal(snapshot, out _);
        var incoming = IntentCalculator.TotalIncomingDamage(snapshot);
        var net = IntentCalculator.NetDamageAfterBlock(snapshot);
        var urgency = IntentCalculator.BlockUrgency(snapshot);

        if (!canLethal && incoming > 0)
            value += urgency / 4 + net / 3;

        if (targetEnemy != null) {
            var hp = targetEnemy["currentHp"]?.GetValue<int>() ?? 0;
            var maxHp = targetEnemy["maxHp"]?.GetValue<int>() ?? 1;
            if (maxHp > 0 && hp <= maxHp * 0.3f && canLethal)
                value = value * 2 / 3;
        }

        return Math.Max(0, value);
    }

    public static int ComputeVulnerableSetupValue(CombatState state, int handIndex, int enemyIndex) {
        if (handIndex < 0 || handIndex >= state.Hand.Count)
            return 0;

        var card = state.Hand[handIndex];
        if (!card.CanPlay || card.Cost > state.Energy)
            return 0;
        if (!AppliesVulnerable(card))
            return 0;

        var stacks = Math.Max(card.Profile.AppliedVulnerable, 1);
        var target = state.Enemies.FirstOrDefault(e => e.IsAlive && e.Index == enemyIndex);
        if (target == null)
            return 0;
        if (target.Vulnerable > 0)
            return 0;

        var hand = state.ToHandJson();
        var attackHand = HandWithoutIndex(hand, handIndex);
        var immediate = CombatCardStats.EstimateFollowupAttackDamage(attackHand, state.Energy);
        var energyAfter = state.Energy - card.Cost;
        if (energyAfter < 0) return 0;

        var followup = CombatCardStats.EstimateFollowupAttackDamage(attackHand, energyAfter);
        var payoff = card.Damage + (int)Math.Round(followup * 1.5f);
        var value = payoff - immediate + stacks * 4;

        if (ThreatModel.IncomingDamage(state) > 0 && !ThreatModel.IsFatalIfUnblocked(state))
            value += stacks * 2;

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
            if (!AppliesVulnerable(profile)) continue;

            var stacks = Math.Max(profile.AppliedVulnerable, 1);
            var cost = card["cost"]?.GetValue<int>() ?? 99;
            if (cost > energy) continue;

            var value = ComputeVulnerableDeferValue(
                snapshot, hand, energy, targetEnemy, stacks, cost, i, card);
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

        return Math.Max(0, deferValue - attackDamage);
    }

    public static int ComputeSetupDebt(CombatState state) {
        if (!state.Enemies.Any(e => e.IsAlive && e.Vulnerable <= 0))
            return 0;

        var hasVulnPlay = state.Hand.Any(c =>
            c.CanPlay && c.Cost <= state.Energy && AppliesVulnerable(c));
        if (!hasVulnPlay)
            return 0;

        var hasOtherAttack = state.Hand.Any(c =>
            c.CanPlay && c.Cost <= state.Energy && c.IsAttack && c.Damage > 0 && !AppliesVulnerable(c));
        if (!hasOtherAttack)
            return 0;

        int debt = 0;
        foreach (var card in state.Hand) {
            if (!card.CanPlay || card.Cost > state.Energy) continue;
            if (!AppliesVulnerable(card)) continue;
            debt += 12 + Math.Max(card.Profile.AppliedVulnerable, 1) * 5;
        }

        return debt;
    }

    static bool AppliesVulnerable(CombatHandCard card) =>
        card.Profile.AppliedVulnerable > 0
        || card.Profile.Flags.HasFlag(CardMechanicFlags.AppliesVulnerable);

    static bool AppliesVulnerable(CardMechanicProfile profile) =>
        profile.AppliedVulnerable > 0
        || profile.Flags.HasFlag(CardMechanicFlags.AppliesVulnerable);

    static JsonArray HandWithoutIndex(JsonArray hand, int skipIndex) {
        var arr = new JsonArray();
        for (int i = 0; i < hand.Count; i++) {
            if (i == skipIndex) continue;
            if (hand[i]?.DeepClone() is JsonNode clone)
                arr.Add(clone);
        }

        return arr;
    }
}
