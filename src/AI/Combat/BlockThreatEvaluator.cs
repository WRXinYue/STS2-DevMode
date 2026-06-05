using System;
using System.Text.Json.Nodes;

namespace DevMode.AI.Combat;

/// <summary>Shared incoming-damage threat checks for block scoring and transform suppression.</summary>
public static class BlockThreatEvaluator {
    public const int EarlyFloorMax = 15;
    public const int EarlyBlockThreshold = 6;
    public const int LateBlockThreshold = 8;
    public const int SafeLethalNetMax = 8;
    public const float SuppressTransformScale = 0.25f;

    public static bool HasIncomingDamage(JsonObject snapshot) =>
        IntentCalculator.TotalIncomingDamage(snapshot) > 0;

    public static int EarlyBlockThresholdFor(JsonObject snapshot) {
        var floor = snapshot["totalFloor"]?.GetValue<int>() ?? 0;
        return floor <= EarlyFloorMax ? EarlyBlockThreshold : LateBlockThreshold;
    }

    public static bool ShouldScoreBlock(JsonObject snapshot) {
        if (IntentCalculator.NeedsBlock(snapshot))
            return true;

        var net = IntentCalculator.NetDamageAfterBlock(snapshot);
        return net >= EarlyBlockThresholdFor(snapshot);
    }

    public static bool ShouldSuppressTransform(JsonObject snapshot) {
        if (!ShouldScoreBlock(snapshot))
            return false;

        if (LethalChecker.CanLethal(snapshot, out _)
            && !IntentCalculator.IsFatalIfUnblocked(snapshot))
            return false;

        return true;
    }

    public static bool IsStarterDefend(string? cardId, string? rarity = null) {
        if (string.IsNullOrWhiteSpace(cardId))
            return false;

        var idUpper = cardId.ToUpperInvariant();
        if (idUpper.Contains("DEFEND", StringComparison.Ordinal))
            return true;

        if (!string.IsNullOrWhiteSpace(rarity)
            && rarity.ToUpperInvariant().Contains("STARTER", StringComparison.Ordinal))
            return true;

        return false;
    }
}
