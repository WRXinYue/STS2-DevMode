using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DevMode.AI.Combat;
using DevMode.AI.Knowledge;

namespace DevMode.AI.Combat.Simulation;

/// <summary>Fast move ordering for beam expansion — setup before attacks, kills before chip.</summary>
internal static class CombatActionHeuristic {
    public static int QuickScore(CombatState state, SimCombatAction action) {
        if (action.Kind == SimActionKind.EndTurn)
            return ScoreEndTurn(state);

        if (action.HandIndex < 0 || action.HandIndex >= state.Hand.Count)
            return int.MinValue;

        var card = state.Hand[action.HandIndex];
        if (!card.CanPlay || card.Cost > state.Energy)
            return int.MinValue;

        if (CombatTransformSimulator.IsHandAttackTransform(card.Profile))
            return ScoreHandTransform(state, action, card);

        if (card.IsAttack && card.Damage > 0)
            return ScoreAttack(state, action, card);

        if (card.Block > 0)
            return ScoreBlock(state, card);

        if (card.Profile.AppliedVulnerable > 0)
            return ScoreVulnerableSetup(state, card);

        if (card.Profile.AppliedWeak > 0)
            return 28 + card.Profile.AppliedWeak * 6;

        if (MechanicCombatBonus.IsSetupSkill(card.Profile))
            return 22;

        return 8;
    }

    public static bool ShouldPrune(CombatState state, SimCombatAction action) =>
        QuickScore(state, action) <= int.MinValue + 1;

    static int ScoreHandTransform(CombatState state, SimCombatAction action, CombatHandCard card) {
        var hand = state.ToHandJson();
        var delta = CombatTransformSimulator.EstimateTurnDamageDelta(hand, card.ToJson(), state.Energy);
        if (delta <= 0) {
            var after = CombatSimulator.Apply(state, action);
            if (!SimLethalChecker.CanLethal(after, out _))
                return int.MinValue;
            return 120;
        }

        return 40 + delta;
    }

    static int ScoreAttack(CombatState state, SimCombatAction action, CombatHandCard card) {
        var score = card.Damage * 3;
        var target = ResolveTarget(state, action.EnemyIndex);
        if (target != null) {
            var eff = EffectiveDamage(card.Damage, target);
            if (eff >= target.EffectiveHp)
                score += 220;
            score += Math.Max(0, 60 - target.EffectiveHp);
            score += target.IntentDamage * 3;
            if (target.IsMinion)
                score -= 25;
        }

        if (card.IsAoe) {
            int kills = AoeDamageEstimator.EstimateAoeKills(state, card.Damage);
            score += kills * 80;
        }

        if (ThreatModel.IsFatalIfUnblocked(state) && ThreatModel.NetDamageAfterBlock(state) > card.Damage)
            score -= 40;

        return score;
    }

    static int ScoreBlock(CombatState state, CombatHandCard card) {
        var net = ThreatModel.NetDamageAfterBlock(state);
        if (net <= 0) return 5;

        var effective = Math.Min(card.Block, net);
        var score = 25 + effective * 3;
        if (ThreatModel.IsFatalIfUnblocked(state))
            score += 50;
        return score;
    }

    static int ScoreVulnerableSetup(CombatState state, CombatHandCard card) {
        var hand = state.ToHandJson();
        var followup = CombatCardStats.EstimateFollowupAttackDamage(hand, state.Energy) / 2;
        return 35 + card.Profile.AppliedVulnerable * 10 + followup / 3;
    }

    static int ScoreEndTurn(CombatState state) {
        var playable = state.Hand.Count(c => c.CanPlay && c.Cost <= state.Energy);
        if (playable == 0)
            return 50;

        var net = ThreatModel.NetDamageAfterBlock(state);
        if (ThreatModel.IsFatalIfUnblocked(state))
            return int.MinValue;

        if (net > 0 && state.PlayerBlock < net)
            return 5;

        return 15 - playable * 3;
    }

    static CombatEnemy? ResolveTarget(CombatState state, int enemyIndex) {
        if (enemyIndex < 0) return null;
        foreach (var enemy in state.Enemies) {
            if (!enemy.IsAlive) continue;
            if (enemy.Index == enemyIndex)
                return enemy;
        }

        return null;
    }

    static int EffectiveDamage(int damage, CombatEnemy target) =>
        (int)Math.Round(damage * (target.Vulnerable > 0 ? 1.5f : 1f));
}
