using System.Text.Json.Nodes;
using DevMode.AI.Core.Schema;

namespace DevMode.AI.Core;

/// <summary>Mod-provided combat move score adjustments for <see cref="AutoPlay.Scoring.CombatScorer"/>.</summary>
public interface IAiMoveModifier {
    bool AppliesTo(string? characterId);

    int ModifyScore(JsonObject snapshot, GameAction move, int baseScore);
}
