using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DevMode.AI.Combat;
using DevMode.AI.Knowledge;

namespace DevMode.AI.Combat.Simulation;

public static class ThreatModel {
    /// <summary>Future enemy rounds compared in line outcome (after the current line resolves).</summary>
    public const int LineFutureHorizonTurns = 3;

    public static int IncomingDamage(CombatState state) =>
        state.Enemies
            .Where(e => e.IsAlive && e.EffectiveIncoming > 0)
            .Sum(e => e.EffectiveIncoming);

    public static int NetDamageAfterBlock(CombatState state) =>
        Math.Max(0, IncomingDamage(state) - state.PlayerBlock);

    public static int EffectiveHp(CombatState state) =>
        Math.Max(1, state.PlayerHp - state.StatusDamage);

    public static bool IsFatalIfUnblocked(CombatState state) =>
        NetDamageAfterBlock(state) >= EffectiveHp(state);

    public static int NextTurnIncoming(CombatState state) =>
        IncomingAtIntentStep(state, 1);

    /// <summary>Sum attack damage at intentSteps[stepIndex] across alive enemies.</summary>
    public static int IncomingAtIntentStep(CombatState state, int stepIndex) {
        if (stepIndex < 0)
            return 0;

        double total = 0;
        foreach (var enemy in state.Enemies.Where(e => e.IsAlive)) {
            if (stepIndex >= enemy.IntentSteps.Length)
                continue;

            var step = enemy.IntentSteps[stepIndex];
            var damage = step.IntentDamage;
            if (step.IsUncertain)
                damage = (int)Math.Round(damage * EnemyThreatWeights.NextTurnUncertainMultiplier);
            total += damage;
        }

        return (int)Math.Round(total);
    }

    /// <summary>Sum non-damage pressure at intentSteps[stepIndex] across alive enemies.</summary>
    public static int NonDamageAtIntentStep(CombatState state, int stepIndex) {
        if (stepIndex < 0)
            return 0;

        int total = 0;
        foreach (var enemy in state.Enemies.Where(e => e.IsAlive)) {
            if (stepIndex >= enemy.IntentSteps.Length)
                continue;
            total += enemy.IntentSteps[stepIndex].NonDamageThreat;
        }

        return total;
    }

    /// <summary>Damage + debuff pressure at one future enemy phase (for line horizon).</summary>
    public static int PressureAtIntentStep(CombatState state, int stepIndex) =>
        IncomingAtIntentStep(state, stepIndex) + NonDamageAtIntentStep(state, stepIndex);

    /// <summary>Intent-chain threat for focus fire over the next enemy phases.</summary>
    public static int HorizonThreatForEnemy(CombatEnemy enemy, int startStep = 0, int stepCount = 3) {
        if (!enemy.IsAlive || startStep < 0)
            return 0;

        int total = 0;
        for (int i = startStep; i < startStep + stepCount && i < enemy.IntentSteps.Length; i++) {
            var step = enemy.IntentSteps[i];
            int damage = step.IntentDamage;
            if (i > startStep && step.IsUncertain)
                damage = (int)Math.Round(damage * EnemyThreatWeights.NextTurnUncertainMultiplier);
            total += damage + step.NonDamageThreat;
        }

        return total;
    }

    /// <summary>How much extra this-turn incoming is acceptable when hitting the horizon focus target.</summary>
    public static int IncomingTradeSlack(CombatEnemy focus) =>
        Math.Clamp(HorizonThreatForEnemy(focus, 0, LineFutureHorizonTurns + 1) / 3, 4, 12);

    /// <summary>Positive when horizon A is better (lower incoming) than horizon B.</summary>
    public static int CompareFutureIncoming(
        int a0, int a1, int a2,
        int b0, int b1, int b2) {
        if (a0 != b0) return b0 - a0;
        if (a1 != b1) return b1 - a1;
        if (a2 != b2) return b2 - a2;
        return 0;
    }

    public static int TotalNonDamageThreat(CombatState state) =>
        state.Enemies.Where(e => e.IsAlive).Sum(e => e.NonDamageThreat);

    public static int NextTurnAttackOn(CombatEnemy enemy) =>
        IncomingAtIntentStepForEnemy(enemy, 1);

    public static int NextTurnPressureOn(CombatEnemy enemy) {
        if (!enemy.IsAlive)
            return 0;

        int total = 0;
        for (int i = 1; i <= LineFutureHorizonTurns && i < enemy.IntentSteps.Length; i++) {
            var step = enemy.IntentSteps[i];
            int damage = step.IntentDamage;
            if (step.IsUncertain)
                damage = (int)Math.Round(damage * EnemyThreatWeights.NextTurnUncertainMultiplier);
            total += damage + step.NonDamageThreat;
        }

        return total;
    }

    static int IncomingAtIntentStepForEnemy(CombatEnemy enemy, int stepIndex) {
        if (!enemy.IsAlive || stepIndex < 0 || stepIndex >= enemy.IntentSteps.Length)
            return 0;

        var step = enemy.IntentSteps[stepIndex];
        var damage = step.IntentDamage;
        if (step.IsUncertain)
            damage = (int)Math.Round(damage * EnemyThreatWeights.NextTurnUncertainMultiplier);
        return damage;
    }

    /// <summary>Next-turn attack weight — full when safe this turn so kill-before-hit is valued.</summary>
    public static int ScaledNextTurnPressure(CombatState state) {
        var next = NextTurnIncoming(state);
        return IncomingDamage(state) > 0 ? next / 2 : next;
    }

    /// <summary>Card-stuff/debuff pressure — prefers deck EV model when piles are available.</summary>
    public static int ScaledNonDamagePressure(CombatState state) =>
        ThreatEconomy.ScaledNonDamagePressure(state);

    public static bool IsViableAttackTarget(CombatState state, CombatEnemy enemy) {
        if (!enemy.IsAlive)
            return false;
        if (!enemy.MechanicFlags.HasFlag(EnemyMechanicFlags.HasIllusionRevive))
            return true;

        return !state.Enemies.Any(e =>
            e.IsAlive && !e.MechanicFlags.HasFlag(EnemyMechanicFlags.HasIllusionRevive));
    }

    public static int AliveThreatCount(CombatState state) =>
        state.Enemies.Count(e => e.IsAlive && e.IntentDamage > 0);

    public static bool CanEliminateAllThreats(CombatState state, int maxSingleTargetDamage) {
        var threats = state.Enemies
            .Where(e => e.IsAlive && e.IntentDamage > 0)
            .ToList();
        if (threats.Count == 0) return true;

        foreach (var threat in threats) {
            if (threat.EffectiveHp > maxSingleTargetDamage)
                return false;
        }

        return NetDamageAfterBlock(state) <= 8;
    }

    public static void OnPrimaryEnemyKilled(IList<CombatEnemy> enemies, int killedIndex) {
        if (killedIndex < 0 || killedIndex >= enemies.Count) return;
        if (enemies[killedIndex].IsMinion) return;
        if (!MinionEngagementPolicy.ShouldSimulateMinionWipe(
                enemies[killedIndex], enemies.ToArray()))
            return;

        for (int i = 0; i < enemies.Count; i++) {
            if (!enemies[i].IsAlive || !enemies[i].IsMinion) continue;
            if (enemies[i].MechanicFlags.HasFlag(EnemyMechanicFlags.HasIllusionRevive))
                continue;
            enemies[i] = enemies[i].MarkDead();
        }
    }

    public static int NonDamageThreatFromJson(JsonObject? enemy) =>
        EnemyMechanicResolver.ResolveNonDamageThreat(enemy);

    // JsonObject bridge for IntentCalculator
    public static int IncomingDamage(JsonObject snapshot) =>
        IncomingDamage(CombatState.FromSnapshot(snapshot));

    public static int NetDamageAfterBlock(JsonObject snapshot) =>
        NetDamageAfterBlock(CombatState.FromSnapshot(snapshot));
}
