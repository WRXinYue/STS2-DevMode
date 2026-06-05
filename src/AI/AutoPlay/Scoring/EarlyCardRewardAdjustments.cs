using System;
using System.Text.Json.Nodes;
using DevMode.AI.Planning;

namespace DevMode.AI.AutoPlay.Scoring;

/// <summary>Early-run card reward tweaks from playtesting (skip exhaust/dilute picks; favor transition block).</summary>
internal static class EarlyCardRewardAdjustments {
    const int EarlyFloorMax = 12;

    public static int Score(JsonObject card, JsonObject? snapshot) {
        if (snapshot == null) return 0;

        var floor = snapshot["totalFloor"]?.GetValue<int>() ?? 0;
        if (floor > EarlyFloorMax) return 0;

        var id = (card["id"]?.GetValue<string>() ?? "").ToUpperInvariant();
        var deck = snapshot["deck"]?.AsArray();
        var score = 0;

        if (id == "TRUE_GRIT")
            score -= 18;
        if (id == "BURNING_PACT")
            score -= 20;

        if (id == "PRIMAL_FORCE") {
            score -= 10;
            if (DeckContains(deck, "PRIMAL_FORCE"))
                score -= 15;
            var stats = DeckStats.From(deck);
            if (stats.AttackCount >= 4)
                score -= 8;
        }

        if (id == "IRON_WAVE" && floor <= 10)
            score += 6;

        return score;
    }

    static bool DeckContains(JsonArray? deck, string cardId) {
        if (deck == null) return false;
        foreach (var node in deck) {
            if (node is not JsonObject card) continue;
            if (string.Equals(card["id"]?.GetValue<string>(), cardId, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
