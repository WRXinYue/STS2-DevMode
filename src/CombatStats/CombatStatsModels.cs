using System.Collections.Generic;
using System.Linq;

namespace DevMode.CombatStats;

internal sealed class PlayerCombatStats {
    public string Key { get; init; } = "";
    public string DisplayName { get; set; } = "";
    public string CharacterId { get; set; } = "";

    public int DamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public int BlockGained { get; set; }
    public int CardsPlayed { get; set; }
    public int HitCount { get; set; }

    public Dictionary<string, int> DamageByCard { get; } = new();
    public Dictionary<string, int> DamageTakenBySource { get; } = new();
    public Dictionary<int, int> DamagePerTurn { get; } = new();
}

internal sealed class CombatStatsSnapshot {
    public string EncounterKey { get; set; } = "";
    public bool IsActive { get; set; }
    public int MaxTurn { get; set; }
    public Dictionary<string, PlayerCombatStats> Players { get; } = new();

    public PlayerCombatStats? PrimaryPlayer {
        get {
            if (Players.Count == 0) return null;
            return Players.Values.FirstOrDefault()
                   ?? null;
        }
    }

    public CombatStatsSnapshot Clone() {
        var copy = new CombatStatsSnapshot {
            EncounterKey = EncounterKey,
            IsActive = IsActive,
            MaxTurn = MaxTurn,
        };
        foreach (var (key, src) in Players) {
            var dst = new PlayerCombatStats {
                Key = src.Key,
                DisplayName = src.DisplayName,
                CharacterId = src.CharacterId,
                DamageDealt = src.DamageDealt,
                DamageTaken = src.DamageTaken,
                BlockGained = src.BlockGained,
                CardsPlayed = src.CardsPlayed,
                HitCount = src.HitCount,
            };
            CopyDict(src.DamageByCard, dst.DamageByCard);
            CopyDict(src.DamageTakenBySource, dst.DamageTakenBySource);
            CopyDict(src.DamagePerTurn, dst.DamagePerTurn);
            copy.Players[key] = dst;
        }
        return copy;
    }

    private static void CopyDict<TKey>(Dictionary<TKey, int> src, Dictionary<TKey, int> dst)
        where TKey : notnull {
        foreach (var (k, v) in src)
            dst[k] = v;
    }
}
