using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DevMode.AI.Combat;
using DevMode.AI.Core.Schema;
using DevMode.AI.Knowledge;
using DevMode.AI.Planning;

namespace DevMode.AI.AutoPlay.Scoring;

/// <summary>Scores all held potions in combat and picks the best use above threshold.</summary>
public static class PotionScorer {
    public const int UseThreshold = 25;

    public static GameAction? TryUsePotion(JsonObject snapshot) {
        var potions = snapshot["potions"]?.AsArray();
        if (potions == null || potions.Count == 0) return null;

        var plan = DeckPlanInferer.Infer(snapshot);
        var candidates = new List<(int Slot, int Score, string Label)>();

        for (int i = 0; i < potions.Count; i++) {
            if (potions[i] is not JsonObject potion) continue;
            var slot = potion["slot"]?.GetValue<int>() ?? i;
            var id = potion["id"]?.GetValue<string>() ?? "";
            var score = ScoreCombatUse(potion, snapshot, plan);
            if (score > 0)
                candidates.Add((slot, score, ShortId(id)));
        }

        if (candidates.Count == 0) return null;

        candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
        LogCandidates(candidates);

        var best = candidates[0];
        if (best.Score < UseThreshold) return null;

        return new GameAction {
            Type = ActionType.UsePotion,
            TargetIndex = best.Slot,
            Reason = $"Combat potion [{best.Label}] score={best.Score}",
        };
    }

    public static int ScoreCombatUse(JsonObject potion, JsonObject snapshot, DeckPlan? plan = null) {
        plan ??= DeckPlanInferer.Infer(snapshot);

        var id = potion["id"]?.GetValue<string>() ?? "";
        var profile = PotionMechanicIndex.GetOrDefault(id);
        var category = ParseCategory(potion, profile);
        var retain = potion["retainScore"]?.GetValue<int>() ?? PotionTierCatalog.GetRetainScore(id);

        var hpRatio = IntentCalculator.HpRatio(snapshot);
        var incoming = IntentCalculator.TotalIncomingDamage(snapshot);
        var net = IntentCalculator.NetDamageAfterBlock(snapshot);
        var needsBlock = IntentCalculator.NeedsBlock(snapshot);
        var fatal = IntentCalculator.IsFatalIfUnblocked(snapshot);
        var enemies = IntentCalculator.AliveEnemyCount(snapshot);
        var energy = snapshot["combat"]?.AsObject()?["currentEnergy"]?.GetValue<int>() ?? 0;
        var hand = snapshot["combat"]?.AsObject()?["hand"]?.AsArray();
        var canLethal = LethalChecker.CanLethal(snapshot, out _);
        var maxOffense = hand != null ? LethalChecker.EstimateMaxDamage(hand, energy, 0) : 0;
        var minPlayCost = MinPlayableCost(hand, energy);

        int score = 0;

        switch (category) {
            case PotionCategory.Heal:
                if (hpRatio < 0.35f) score += 40;
                else if (hpRatio < 0.55f && fatal) score += 30;
                else if (fatal) score += 20;
                break;

            case PotionCategory.Block:
                if (fatal) score += 45;
                else if (needsBlock && incoming >= 20) score += 30;
                else if (needsBlock) score += 20;
                score += profile.EstimatedBlock;
                break;

            case PotionCategory.DamageSingle:
            case PotionCategory.DamageAoE:
                if (canLethal) score += 15;
                if (enemies >= 2 && category == PotionCategory.DamageAoE) score += 25;
                if (!canLethal && maxOffense > 0) {
                    var gap = EstimateLethalGap(snapshot);
                    if (gap > 0 && gap <= profile.EstimatedDamage + 8) score += 30;
                }
                score += profile.EstimatedDamage;
                break;

            case PotionCategory.Energy:
                if (!canLethal && minPlayCost > energy && minPlayCost <= energy + 2)
                    score += 35;
                else if (canLethal) score += 10;
                break;

            case PotionCategory.Draw:
                if (needsBlock && energy <= 1) score += 15;
                if (enemies >= 2) score += 10;
                break;

            case PotionCategory.Buff:
                score += (int)Math.Round(plan.GetWeight(AiTag.Attack) * 12f);
                if (enemies >= 2) score += 8;
                break;

            case PotionCategory.Debuff:
                score += (int)Math.Round(plan.GetWeight(AiTag.Attack) * 8f);
                if (enemies >= 2) score += 10;
                break;

            case PotionCategory.Random:
                score += 18;
                if (canLethal || needsBlock) score += 8;
                break;

            case PotionCategory.Utility:
                score += 5;
                break;
        }

        score += SynergyBonus(category, plan);
        score -= WastePenalty(retain, hpRatio, fatal, needsBlock, snapshot);

        var usage = potion["usage"]?.GetValue<string>() ?? profile.Usage;
        if (!IsCombatUsable(usage))
            score = 0;

        return score;
    }

    static bool IsCombatUsable(string usage) {
        if (string.IsNullOrEmpty(usage)) return true;
        if (usage.Contains("Any", StringComparison.OrdinalIgnoreCase)) return true;
        if (usage.Contains("Combat", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static PotionCategory ParseCategory(JsonObject potion, PotionMechanicProfile profile) {
        var raw = potion["category"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(raw) && Enum.TryParse<PotionCategory>(raw, out var parsed))
            return parsed;
        return profile.Category;
    }

    static int SynergyBonus(PotionCategory category, DeckPlan plan) => category switch {
        PotionCategory.Debuff when plan.GetWeight(AiTag.Attack) > 0.9f => 8,
        PotionCategory.Buff when plan.GetWeight(AiTag.Scaling) > 0.8f => 10,
        PotionCategory.Random when plan.GetWeight(AiTag.Draw) > 0.7f => 6,
        _ => 0,
    };

    static int WastePenalty(int retain, float hpRatio, bool fatal, bool needsBlock, JsonObject snapshot) {
        if (fatal || needsBlock) return 0;

        var room = (snapshot["roomType"]?.GetValue<string>() ?? "").ToUpperInvariant();
        var isBigFight = room.Contains("ELITE") || room.Contains("BOSS");

        if (!isBigFight && hpRatio > 0.65f)
            return retain + 15;

        if (!isBigFight && hpRatio > 0.45f)
            return retain / 2;

        return 0;
    }

    static int EstimateLethalGap(JsonObject snapshot) {
        var combat = snapshot["combat"]?.AsObject();
        var hand = combat?["hand"]?.AsArray();
        var energy = combat?["currentEnergy"]?.GetValue<int>() ?? 0;
        var enemies = combat?["enemies"]?.AsArray();
        if (hand == null || enemies == null) return 0;

        var maxDamage = LethalChecker.EstimateMaxDamage(hand, energy, 0);
        var minHp = int.MaxValue;
        foreach (var node in enemies) {
            if (node is not JsonObject enemy) continue;
            if (enemy["isAlive"]?.GetValue<bool>() == false) continue;
            var hp = (enemy["currentHp"]?.GetValue<int>() ?? 0) + (enemy["block"]?.GetValue<int>() ?? 0);
            if (hp < minHp) minHp = hp;
        }

        if (minHp == int.MaxValue) return 0;
        return Math.Max(0, minHp - maxDamage);
    }

    static int MinPlayableCost(JsonArray? hand, int energy) {
        if (hand == null) return 99;
        var min = 99;
        foreach (var node in hand) {
            if (node is not JsonObject card) continue;
            if (card["canPlay"]?.GetValue<bool>() != true) continue;
            var cost = card["cost"]?.GetValue<int>() ?? 99;
            if (cost < min) min = cost;
        }
        return min == 99 ? energy + 1 : min;
    }

    static void LogCandidates(List<(int Slot, int Score, string Label)> candidates) {
        var top = candidates.Take(3)
            .Select(c => $"{c.Label}:+{c.Score}")
            .ToArray();
        AiDecisionLog.Record("AutoPlay", $"potion candidates [{string.Join("] [", top)}]");
    }

    static string ShortId(string id) {
        if (string.IsNullOrEmpty(id)) return "?";
        var s = id;
        if (s.StartsWith("POTION.", StringComparison.OrdinalIgnoreCase))
            s = s["POTION.".Length..];
        return s;
    }
}
