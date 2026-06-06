using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DevMode.AI.AutoPlay.Scoring;
using DevMode.AI.Combat.Simulation;
using DevMode.AI.Core.Schema;

namespace DevMode.AI.Combat;

/// <summary>Cross-layer combat decision tracing (QuickScore vs beam vs scorer vs sim).</summary>
internal static class CombatDebugTrace {
    public static void LogOpeningDecision(CombatState state, JsonObject snapshot, BeamSearchResult? beam) {
        LogQuickScoreTop(state, snapshot, 10);
        LogScorerTop(snapshot, 6);
        LogSimLineCandidates(state);
        if (beam is { HasResult: true })
            LogBeamWinner(state, beam.Value);
        LogLayerConflict(state, snapshot, beam);
    }

    public static void LogQuickScoreTop(CombatState state, JsonObject snapshot, int topN) {
        var ranked = LegalActionGenerator.Generate(state, snapshot)
            .Where(a => a.Kind != SimActionKind.EndTurn)
            .Select(a => {
                var kind = ClassifyAction(state, a);
                var quickScore = CombatActionHeuristic.QuickScore(state, a, snapshot);
                int? setupSim = null;
                int? transformDelta = null;
                if (a.HandIndex >= 0 && a.HandIndex < state.Hand.Count) {
                    var card = state.Hand[a.HandIndex];
                    if (card.Profile.AppliedVulnerable > 0 && a.EnemyIndex >= 0)
                        setupSim = CombatSetupEvaluator.ComputeVulnerableSetupValue(
                            state, a.HandIndex, a.EnemyIndex);
                    if (CombatTransformSimulator.IsHandAttackTransform(card.Profile))
                        transformDelta = CombatTransformSimulator.EstimateTurnDamageDelta(
                            state.ToHandJson(), card.ToJson(), state.Energy);
                }

                return new {
                    action = FormatAction(state, a),
                    kind,
                    quickScore,
                    setupSim,
                    transformDelta,
                };
            })
            .OrderByDescending(x => x.quickScore)
            .Take(topN)
            .ToList();

        AgentDebugLog.Write("H4", "CombatDebugTrace.QuickScore", "beam expansion order", new {
            energy = state.Energy,
            incoming = ThreatModel.IncomingDamage(state),
            ranked,
        });
    }

    public static void LogScorerTop(JsonObject snapshot, int topN) {
        var ranked = CombatScorer.ScoreLegalMovesDetailed(snapshot)
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .Select(x => new {
                move = CombatMoveScore.FormatMoveLabel(x.Action),
                score = x.Score,
                terms = x.FormatTerms(),
            })
            .ToList();

        AgentDebugLog.Write("H5", "CombatDebugTrace.Scorer", "fallback scorer top", new { ranked });
    }

    public static void LogSimLineCandidates(CombatState state) {
        var lines = new List<object>();

        foreach (var (label, action) in CandidateOpeners(state)) {
            var after = CombatSimulator.Apply(state, action);
            var outcome = CombatSetupEvaluator.EvaluateLine(after);
            lines.Add(new {
                label,
                opener = FormatAction(state, action),
                incoming = outcome.Incoming,
                future0 = outcome.FutureIncoming0,
                future1 = outcome.FutureIncoming1,
                future2 = outcome.FutureIncoming2,
                focusHp = outcome.FocusHp,
                enemyHp = outcome.EnemyHp,
                vulnOutlook = outcome.VulnerableOutlook,
                playerHp = outcome.PlayerHpAfterTurn,
            });
        }

        AgentDebugLog.Write("H6", "CombatDebugTrace.SimLines", "sim line candidates", new {
            energy = state.Energy,
            lines,
        });
    }

    public static void LogBeamWinner(CombatState state, BeamSearchResult beam) {
        var path = beam.Path ?? [];
        AgentDebugLog.Write("H7", "CombatDebugTrace.Beam", "beam winner", new {
            beam.Score,
            beam.Depth,
            line = FormatPathFromRoot(state, path),
            first = path.Count > 0 ? FormatAction(state, path[0]) : null,
        });
    }

    public static void LogBeamLeafUpdate(
        CombatState root,
        CombatState leafState,
        IReadOnlyList<SimCombatAction> path,
        int leafScore,
        int depth,
        string reason) {
        if (depth > 3 || path.Count == 0)
            return;

        AgentDebugLog.Write("H7", "CombatDebugTrace.BeamLeaf", reason, new {
            depth,
            leafScore,
            line = FormatPathFromRoot(root, path),
            incoming = ThreatModel.IncomingDamage(leafState),
            endTurnIncoming = ThreatModel.IncomingDamage(
                CombatTurnResolver.ResolveEndTurn(leafState)),
        });
    }

    public static void LogBeamDepthCandidates(
        CombatState root,
        IEnumerable<(IReadOnlyList<SimCombatAction> Path, int MidScore)> nodes,
        int depth) {
        var ranked = nodes
            .Select(n => new {
                line = FormatPathFromRoot(root, n.Path),
                midScore = n.MidScore,
            })
            .Take(8)
            .ToList();

        AgentDebugLog.Write("H7", "CombatDebugTrace.BeamDepth", $"depth {depth} survivors", new { depth, ranked });
    }

    static void LogLayerConflict(CombatState state, JsonObject snapshot, BeamSearchResult? beam) {
        var quickBest = LegalActionGenerator.Generate(state, snapshot)
            .Where(a => a.Kind != SimActionKind.EndTurn)
            .Select(a => (action: a, score: CombatActionHeuristic.QuickScore(state, a, snapshot)))
            .OrderByDescending(x => x.score)
            .FirstOrDefault();

        var scorerBest = CombatScorer.ScoreLegalMovesDetailed(snapshot)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        SimCombatAction? beamFirst = beam is { Path: { Count: > 0 } p } ? p[0] : null;

        string? quickLabel = quickBest.action != null
            ? FormatAction(state, quickBest.action)
            : null;
        string? scorerLabel = scorerBest != null
            ? CombatMoveScore.FormatMoveLabel(scorerBest.Action)
            : null;
        string? beamLabel = beamFirst != null
            ? FormatAction(state, beamFirst)
            : null;

        var conflict = new {
            quickBest = quickLabel,
            quickScore = quickBest.score,
            scorerBest = scorerLabel,
            scorerScore = scorerBest?.Score,
            beamFirst = beamLabel,
            beamScore = beam?.Score,
            quickVsBeam = quickLabel != null && beamLabel != null && quickLabel != beamLabel,
            scorerVsBeam = scorerLabel != null && beamLabel != null && scorerLabel != beamLabel,
        };

        AgentDebugLog.Write("H8", "CombatDebugTrace.Conflict", "layer disagreement", conflict);
    }

    static IEnumerable<(string Label, SimCombatAction Action)> CandidateOpeners(CombatState state) {
        int? transformIdx = null;
        int? bashIdx = null;
        for (int i = 0; i < state.Hand.Count; i++) {
            var card = state.Hand[i];
            if (!CombatCardCost.CanAfford(card, state)) continue;
            if (CombatTransformSimulator.IsHandAttackTransform(card.Profile))
                transformIdx ??= i;
            if (card.Id.Equals("BASH", System.StringComparison.OrdinalIgnoreCase)
                || card.Profile.AppliedVulnerable > 0)
                bashIdx ??= i;
        }

        if (transformIdx >= 0)
            yield return ("transform-first", new SimCombatAction(SimActionKind.PlayCard, transformIdx.Value, -1));

        if (bashIdx >= 0) {
            var primary = CombatSetupEvaluator.PrimaryAttackTargetIndex(state);
            yield return ("bash-primary", new SimCombatAction(SimActionKind.PlayCard, bashIdx.Value, primary));
        }

        var quickBest = LegalActionGenerator.Generate(state)
            .Where(a => a.Kind != SimActionKind.EndTurn)
            .Select(a => (action: a, score: CombatActionHeuristic.QuickScore(state, a)))
            .OrderByDescending(x => x.score)
            .FirstOrDefault();
        if (quickBest.action != null)
            yield return ("quick-top", quickBest.action);
    }

    static CombatState SimulateGreedyPlaysPublic(CombatState state) {
        var s = state;
        for (int i = 0; i < s.Hand.Count; i++) {
            var card = s.Hand[i];
            if (!CombatCardCost.CanAfford(card, s)) continue;
            if (!CombatTransformSimulator.IsHandAttackTransform(card.Profile)) continue;
            if (CombatTransformSimulator.EstimateTurnDamageDelta(
                    s.ToHandJson(), card.ToJson(), s.Energy) <= 0)
                continue;
            s = CombatSimulator.Apply(s, new SimCombatAction(SimActionKind.PlayCard, i, -1));
            break;
        }

        return GreedyAttacksPublic(s);
    }

    static CombatState GreedyAttacksPublic(CombatState state) {
        var s = state;
        while (true) {
            SimCombatAction? best = null;
            int bestIncoming = int.MaxValue;
            int bestEnemyHp = int.MaxValue;

            for (int i = 0; i < s.Hand.Count; i++) {
                var card = s.Hand[i];
                if (!CombatCardCost.CanAfford(card, s) || !card.IsAttack || card.Damage <= 0)
                    continue;

                var targets = card.IsAoe
                    ? new[] { -1 }
                    : s.Enemies.Where(e => ThreatModel.IsViableAttackTarget(s, e)).Select(e => e.Index);

                foreach (var enemyIdx in targets) {
                    var next = CombatSimulator.Apply(
                        s, new SimCombatAction(SimActionKind.PlayCard, i, enemyIdx));
                    int incoming = ThreatModel.IncomingDamage(next);
                    int enemyHp = next.Enemies.Where(e => e.IsAlive).Sum(e => e.EffectiveHp);
                    if (incoming < bestIncoming || (incoming == bestIncoming && enemyHp < bestEnemyHp)) {
                        bestIncoming = incoming;
                        bestEnemyHp = enemyHp;
                        best = new SimCombatAction(SimActionKind.PlayCard, i, enemyIdx);
                    }
                }
            }

            if (best == null)
                break;
            s = CombatSimulator.Apply(s, best);
        }

        return s;
    }

    static (int Incoming, int EnemyHp, int PlayerHpAfterTurn) EvaluateOutcomePublic(CombatState mid) {
        var after = CombatTurnResolver.ResolveEndTurn(mid);
        return (
            ThreatModel.IncomingDamage(mid),
            after.Enemies.Where(e => e.IsAlive).Sum(e => e.CurrentHp),
            after.PlayerHp);
    }

    static string ClassifyAction(CombatState state, SimCombatAction action) {
        if (action.HandIndex < 0 || action.HandIndex >= state.Hand.Count)
            return "unknown";
        var card = state.Hand[action.HandIndex];
        if (CombatTransformSimulator.IsHandAttackTransform(card.Profile))
            return "transform";
        if (card.Profile.AppliedVulnerable > 0)
            return "vuln";
        if (card.IsAttack && card.Damage > 0)
            return "attack";
        if (card.Block > 0)
            return "block";
        return "other";
    }

    public static string FormatAction(CombatState state, SimCombatAction action) {
        if (action.Kind == SimActionKind.EndTurn)
            return "EndTurn";
        if (action.Kind == SimActionKind.UsePotion)
            return $"Potion#{action.PotionSlot}→e{action.EnemyIndex}";

        if (action.HandIndex < 0 || action.HandIndex >= state.Hand.Count)
            return "?";

        var card = state.Hand[action.HandIndex];
        return action.EnemyIndex >= 0
            ? $"{card.Id}→e{action.EnemyIndex}"
            : card.Id;
    }

    static string FormatPathFromRoot(CombatState root, IReadOnlyList<SimCombatAction> path) {
        var parts = new List<string>(Math.Min(path.Count, 5));
        var s = root;
        foreach (var action in path.Take(5)) {
            parts.Add(FormatAction(s, action));
            s = CombatSimulator.Apply(s, action);
        }

        if (path.Count > 5)
            parts.Add($"...+{path.Count - 5}");
        return string.Join(">", parts);
    }
}
