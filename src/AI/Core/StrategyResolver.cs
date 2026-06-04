using DevMode.AI.AutoPlay.Strategies;
using DevMode.Companion;
using MegaCrit.Sts2.Core.Entities.Players;

namespace DevMode.AI.Core;

/// <summary>Resolves <see cref="IDecisionMaker"/> for a companion or player (netId → characterId → fallback).</summary>
public static class StrategyResolver {
    static readonly IDecisionMaker Fallback = new SimpleStrategy();

    public static IDecisionMaker Resolve(ulong netId, Player? player) {
        if (netId != 0 && CompanionRegistry.TryGet(netId, out var perNet))
            return perNet;

        var characterId = player?.Character?.Id.Entry;
        if (CharacterAiRegistry.TryGet(characterId, out var byCharacter))
            return byCharacter;

        return Fallback;
    }

    public static IDecisionMaker Resolve(Player player) => Resolve(player.NetId, player);
}
