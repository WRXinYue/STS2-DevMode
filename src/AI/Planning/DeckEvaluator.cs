using System;
using System.Linq;
using System.Text.Json.Nodes;
using DevMode.AI.Knowledge;

namespace DevMode.AI.Planning;

public sealed record DeckMetrics(
    int DeckSize,
    int TotalValue,
    float MeanValue,
    int WorstValue,
    int WorstCardIndex,
    string WorstCardName,
    int RemovalUplift,
    int StarterBloat,
    float ConsistencyScore,
    /// <summary>Strikes above <see cref="DeckPlan.TargetStrikeCount"/> — priority removal targets.</summary>
    int StrikeSurplus,
    /// <summary>Defends above <see cref="DeckPlan.TargetDefendCount"/>.</summary>
    int DefendSurplus,
    /// <summary>Cards over <see cref="DeckPlan.TargetDeckSize"/>.</summary>
    int ThinGap,
    int ExhaustCount,
    /// <summary>
    /// Macro cards still worth removing (shop / exhaust events): thin gap + starter surplus +
    /// non-exhaust filler an exhaust deck cannot burn fast enough.
    /// </summary>
    int CardsNeedingBurn,
    int BlockSourceCount,
    int DrawSourceCount,
    int BlockDeficit,
    int DrawDeficit,
    /// <summary>BlockDeficit×2 + DrawDeficit — macro survivability gap.</summary>
    int SurvivalGap);

/// <summary>Evaluates deck quality and marginal benefit of removing the worst card.</summary>
public static class DeckEvaluator {
    public const int MinRemovalUplift = 11;

    public static DeckMetrics Evaluate(JsonObject snapshot, DeckPlan plan) {
        var deck = snapshot["deck"]?.AsArray();
        var actIndex = snapshot["actIndex"]?.GetValue<int>() ?? 0;
        var floor = snapshot["totalFloor"]?.GetValue<int>() ?? 0;

        if (deck == null || deck.Count == 0) {
            return EmptyMetrics();
        }

        var composition = DeckCardScoring.AnalyzeComposition(deck);
        var survivability = DeckSurvivability.CountSources(deck);
        int blockDeficit = DeckSurvivability.BlockDeficit(plan, survivability.BlockSourceCount);
        int drawDeficit = DeckSurvivability.DrawDeficit(plan, survivability.DrawSourceCount);
        int survivalGap = DeckSurvivability.SurvivalGap(plan, survivability);
        int total = 0;
        int worstValue = int.MaxValue;
        int worstIndex = 0;
        string worstName = "";
        int nonExhaustFiller = 0;

        for (int i = 0; i < deck.Count; i++) {
            if (deck[i] is not JsonObject card) continue;
            int value = DeckCardScoring.ScoreInDeck(card, plan, composition);
            total += value;

            if (IsNonExhaustFiller(card, plan, composition, value))
                nonExhaustFiller++;

            if (value < worstValue) {
                worstValue = value;
                worstIndex = card["index"]?.GetValue<int>() ?? i;
                worstName = card["name"]?.GetValue<string>() ?? $"card {worstIndex}";
            }
        }

        if (worstValue == int.MaxValue)
            worstValue = 0;

        int deckSize = deck.Count;
        float mean = deckSize > 0 ? (float)total / deckSize : 0f;

        int strikeSurplus = Math.Max(0, composition.StrikeCount - plan.TargetStrikeCount);
        int defendSurplus = Math.Max(0, composition.DefendCount - plan.TargetDefendCount);
        int thinGap = Math.Max(0, deckSize - plan.TargetDeckSize);
        int starterBloat = strikeSurplus
            + (int)Math.Round(defendSurplus * 0.8f)
            + composition.CurseCount * 3;
        int cardsNeedingBurn = ComputeCardsNeedingBurn(
            thinGap, strikeSurplus, defendSurplus, nonExhaustFiller,
            composition.ExhaustCount, plan);

        int starterBloatBonus = (int)Math.Round(starterBloat * 4f);
        int dilution = (int)Math.Round(DeckPlanInferer.DilutionPenalty(deckSize, plan));
        int futureThin = FutureThinBonus(actIndex, floor);
        int burnPressure = (int)Math.Round(cardsNeedingBurn * 1.5f);

        int removalUplift = (int)Math.Round(mean - worstValue)
            + starterBloatBonus
            + dilution
            + futureThin
            + burnPressure;

        float consistency = deckSize > 0
            ? Math.Clamp(1f - starterBloat / (float)Math.Max(deckSize, 1), 0f, 1f)
            : 1f;

        return new DeckMetrics(
            deckSize,
            total,
            mean,
            worstValue,
            worstIndex,
            worstName,
            removalUplift,
            starterBloat,
            consistency,
            strikeSurplus,
            defendSurplus,
            thinGap,
            composition.ExhaustCount,
            cardsNeedingBurn,
            survivability.BlockSourceCount,
            survivability.DrawSourceCount,
            blockDeficit,
            drawDeficit,
            survivalGap);
    }

    static DeckMetrics EmptyMetrics() =>
        new(0, 0, 0, 0, -1, "", 0, 0, 1f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    static bool IsNonExhaustFiller(
        JsonObject card,
        DeckPlan plan,
        DeckComposition composition,
        int scoreInDeck) {
        var tags = CardCatalog.ResolveTags(
            card["id"]?.GetValue<string>(),
            card["cardType"]?.GetValue<string>(),
            card["keywords"]?.AsArray());
        if (tags.Contains(AiTag.Exhaust)) return false;

        var id = (card["id"]?.GetValue<string>() ?? "").ToUpperInvariant();
        var rarity = (card["rarity"]?.GetValue<string>() ?? "").ToUpperInvariant();
        if (id.Contains("STRIKE", StringComparison.Ordinal)
            || id.Contains("DEFEND", StringComparison.Ordinal)
            || rarity.Contains("CURSE", StringComparison.Ordinal))
            return true;

        return scoreInDeck < 6 && plan.GetWeight(AiTag.Exhaust) >= 0.8f;
    }

    static int ComputeCardsNeedingBurn(
        int thinGap,
        int strikeSurplus,
        int defendSurplus,
        int nonExhaustFiller,
        int exhaustCount,
        DeckPlan plan) {
        int burn = thinGap + strikeSurplus + defendSurplus;

        if (!plan.IsExhaustFocused)
            return burn;

        // Macro: each exhaust card covers ~2 filler cards over a run; debt = remainder.
        int exhaustRelief = exhaustCount * 2;
        burn += Math.Max(0, nonExhaustFiller - exhaustRelief);
        return burn;
    }

    static int FutureThinBonus(int actIndex, int floor) => actIndex switch {
        0 => floor < 20 ? 5 : 3,
        1 => 2,
        _ => 0,
    };
}
