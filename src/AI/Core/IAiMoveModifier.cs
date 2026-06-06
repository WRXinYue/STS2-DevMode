using System.Text.Json.Nodes;
using DevMode.AI.Core.Schema;

namespace DevMode.AI.Core;

/// <summary>Mod-provided combat move score adjustments for beam/sim ranking (<see cref="Combat.Simulation.SimMoveScoring"/>).</summary>
public interface IAiMoveModifier {
    bool AppliesTo(string? characterId);

    int ModifyScore(JsonObject snapshot, GameAction move, int baseScore);
}
