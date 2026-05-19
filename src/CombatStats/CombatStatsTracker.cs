using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace DevMode.CombatStats;

/// <summary>
/// Aggregates combat statistics from <see cref="CombatHistory"/> for the DevMode stats panel.
/// </summary>
internal static class CombatStatsTracker {
    private static readonly CombatHistoryTailer _tailer = new();
    private static bool _initialized;

    private static CombatStatsSnapshot _current = new();
    private static CombatStatsSnapshot? _last;

    public static event Action? Changed;

    public static CombatStatsSnapshot Current => _current;
    public static CombatStatsSnapshot? Last => _last;
    public static bool IsTracking => _current.IsActive;

    public static void Initialize() {
        if (_initialized) return;
        _initialized = true;

        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    private static void OnCombatSetUp(CombatState state) {
        if (!DevModeState.IsActive) return;

        _current = new CombatStatsSnapshot {
            EncounterKey = ResolveEncounterKey(state),
            IsActive = true,
        };

        _tailer.Attach(CombatManager.Instance.History, state);
        NotifyChanged();
    }

    private static void OnCombatEnded(CombatRoom room) {
        _tailer.Detach();

        if (!_current.IsActive) return;

        _current.IsActive = false;
        _last = _current.Clone();
        NotifyChanged();
    }

    internal static void RecordDamage(
        CombatState combatState,
        Creature? dealer,
        Creature receiver,
        DamageResult result,
        CardModel? cardSource,
        int roundNumber) {
        if (!_current.IsActive) return;

        _current.MaxTurn = Math.Max(_current.MaxTurn, roundNumber);

        if (dealer != null && (dealer.IsPlayer || dealer.IsPet) && receiver.IsEnemy) {
            var owner = ResolveDamageOwner(dealer);
            var stats = GetOrCreate(owner);
            int total = result.UnblockedDamage + result.BlockedDamage + result.OverkillDamage;
            stats.DamageDealt += total;
            stats.HitCount++;

            AddToDict(stats.DamagePerTurn, roundNumber, total);

            string cardKey = ResolveDamageCardKey(dealer, cardSource);
            AddToDict(stats.DamageByCard, cardKey, total);
        }

        if (receiver.IsPlayer) {
            var stats = GetOrCreate(receiver);
            int taken = result.UnblockedDamage;
            stats.DamageTaken += taken;

            string sourceKey = ResolveDamageSourceKey(dealer);
            AddToDict(stats.DamageTakenBySource, sourceKey, taken);
        }
    }

    internal static void RecordBlockGained(Creature receiver, int amount, CardPlay? cardPlay) {
        if (!_current.IsActive || amount <= 0) return;
        if (!receiver.IsPlayer) return;

        GetOrCreate(receiver).BlockGained += amount;
    }

    internal static void RecordCardPlay(CardPlay cardPlay) {
        if (!_current.IsActive) return;

        var owner = cardPlay.Card.Owner?.Creature;
        if (owner == null || !owner.IsPlayer) return;

        GetOrCreate(owner).CardsPlayed++;
    }

    private static PlayerCombatStats GetOrCreate(Creature creature) {
        string key = creature.Player?.NetId.ToString() ?? creature.GetHashCode().ToString();
        if (_current.Players.TryGetValue(key, out var existing))
            return existing;

        var stats = new PlayerCombatStats {
            Key = key,
            DisplayName = creature.Name,
            CharacterId = creature.Player?.Character.Id.Entry ?? "",
        };
        _current.Players[key] = stats;
        return stats;
    }

    private static Creature ResolveDamageOwner(Creature dealer) {
        if (dealer.IsPet && dealer.PetOwner != null)
            return dealer.PetOwner.Creature;
        return dealer;
    }

    private static string ResolveDamageCardKey(Creature dealer, CardModel? cardSource) {
        if (dealer.IsPet) {
            return dealer.Monster?.Id.Entry ?? "Pet";
        }

        if (cardSource != null) {
            try {
                string title = cardSource.Title;
                if (!string.IsNullOrWhiteSpace(title))
                    return title;
            }
            catch {
                // fall through to id
            }
            return cardSource.Id.Entry;
        }

        return I18N.T("combatStats.source.other", "Other");
    }

    private static string ResolveDamageSourceKey(Creature? dealer) {
        if (dealer == null)
            return I18N.T("combatStats.source.unknown", "Unknown");

        if (dealer.IsMonster) {
            try {
                if (!string.IsNullOrWhiteSpace(dealer.Name))
                    return dealer.Name;
            }
            catch {
                // fall through
            }
            return dealer.Monster?.Id.Entry ?? I18N.T("combatStats.source.enemy", "Enemy");
        }

        return I18N.T("combatStats.source.other", "Other");
    }

    private static string ResolveEncounterKey(CombatState state) {
        try {
            return state.Encounter?.Id.Entry ?? "";
        }
        catch {
            return "";
        }
    }

    private static void AddToDict<TKey>(Dictionary<TKey, int> dict, TKey key, int amount)
        where TKey : notnull {
        dict.TryGetValue(key, out int prev);
        dict[key] = prev + amount;
    }

    private static void NotifyChanged() => Changed?.Invoke();
}
