using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DevMode.AI.Core;
using DevMode.AI.Core.Schema;

namespace DevMode.AI.AutoPlay.Scoring;

/// <summary>Scores legal combat moves from a JSON snapshot (vanilla heuristics + mod modifiers).</summary>
public static class CombatScorer {
    const int EndTurnBaseScore = -10;

    public static GameAction? PickBestCombatMove(JsonObject snapshot) {
        var best = ScoreLegalMoves(snapshot)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();
        return best.Action;
    }

    public static IEnumerable<(GameAction Action, int Score)> ScoreLegalMoves(JsonObject snapshot) {
        var combat = snapshot["combat"]?.AsObject();
        if (combat == null) {
            yield return (EndTurn("No combat"), EndTurnBaseScore);
            yield break;
        }

        if (!HasAliveEnemy(combat)) {
            yield return (EndTurn("No enemies"), EndTurnBaseScore);
            yield break;
        }

        var hand = combat["hand"]?.AsArray();
        var energy = combat["currentEnergy"]?.GetValue<int>() ?? 0;
        var hp = snapshot["currentHp"]?.GetValue<int>() ?? 0;
        var maxHp = snapshot["maxHp"]?.GetValue<int>() ?? 1;
        var lowHp = hp < maxHp * 0.4;
        var enemies = combat["enemies"]?.AsArray();

        if (hand != null) {
            for (var i = 0; i < hand.Count; i++) {
                var card = hand[i]?.AsObject();
                if (card == null) continue;
                if (card["canPlay"]?.GetValue<bool>() == false) continue;

                var cost = card["cost"]?.GetValue<int>() ?? 99;
                if (cost > energy) continue;

                var cardType = card["cardType"]?.GetValue<string>() ?? "";
                var targetType = card["targetType"]?.GetValue<string>() ?? "";
                var isAttack = cardType.Contains("Attack", StringComparison.OrdinalIgnoreCase);
                var isSkill = cardType.Contains("Skill", StringComparison.OrdinalIgnoreCase);

                if (targetType is "AnyEnemy" or "AllEnemy") {
                    var targetCount = enemies?.Count ?? 1;
                    for (var t = 0; t < Math.Max(targetCount, 1); t++) {
                        var move = PlayCard(i, t, card);
                        var score = ScoreCard(snapshot, move, card, isAttack, isSkill, lowHp, enemies, t);
                        score = AiMoveModifierHub.ApplyModifiers(snapshot, move, score);
                        yield return (move, score);
                    }
                }
                else {
                    var move = PlayCard(i, -1, card);
                    var score = ScoreCard(snapshot, move, card, isAttack, isSkill, lowHp, enemies, -1);
                    score = AiMoveModifierHub.ApplyModifiers(snapshot, move, score);
                    yield return (move, score);
                }
            }
        }

        var end = EndTurn("End turn");
        var endScore = AiMoveModifierHub.ApplyModifiers(snapshot, end, EndTurnBaseScore);
        yield return (end, endScore);
    }

    static int ScoreCard(
        JsonObject snapshot,
        GameAction move,
        JsonObject card,
        bool isAttack,
        bool isSkill,
        bool lowHp,
        JsonArray? enemies,
        int targetIndex) {
        var cost = card["cost"]?.GetValue<int>() ?? 1;
        var score = 0;

        if (lowHp && isSkill)
            score += 40;

        if (isAttack) {
            score += 20 + cost * 5;
            score += TargetEnemyBonus(enemies, targetIndex);
        }
        else if (isSkill)
            score += 15 + cost * 2;
        else
            score += 10;

        score -= Math.Max(0, cost - 1) * 2;
        return score;
    }

    static int TargetEnemyBonus(JsonArray? enemies, int targetIndex) {
        if (enemies == null || enemies.Count == 0)
            return 0;

        JsonObject? target = null;
        if (targetIndex >= 0 && targetIndex < enemies.Count)
            target = enemies[targetIndex]?.AsObject();

        target ??= enemies[0]?.AsObject();
        if (target == null) return 0;

        var hp = target["currentHp"]?.GetValue<int>() ?? 0;
        var maxHp = target["maxHp"]?.GetValue<int>() ?? 1;
        var lowEnemy = maxHp > 0 && hp <= maxHp * 0.25;
        return lowEnemy ? 25 : 5;
    }

    static bool HasAliveEnemy(JsonObject combat) {
        var enemies = combat["enemies"]?.AsArray();
        if (enemies == null) return true;
        if (enemies.Count == 0) return false;
        return enemies.Any(e => e?["isAlive"]?.GetValue<bool>() != false);
    }

    static GameAction PlayCard(int handIndex, int targetIndex, JsonObject card) => new() {
        Type = ActionType.PlayCard,
        TargetIndex = handIndex,
        SecondaryIndex = targetIndex,
        Reason = $"Scored play [{card["name"]?.GetValue<string>()}]",
    };

    static GameAction EndTurn(string reason) => new() {
        Type = ActionType.EndTurn,
        Reason = reason,
    };
}
