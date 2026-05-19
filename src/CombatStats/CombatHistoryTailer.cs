using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;

namespace DevMode.CombatStats;

/// <summary>
/// Incrementally consumes <see cref="CombatHistory"/> entries and forwards them to
/// <see cref="CombatStatsTracker"/>.
/// </summary>
internal sealed class CombatHistoryTailer {
    private CombatHistory? _history;
    private CombatState? _combatState;
    private int _lastSeenIndex;

    public void Attach(CombatHistory history, CombatState combatState) {
        Detach();
        _history = history;
        _combatState = combatState;
        _lastSeenIndex = 0;
        _history.Changed += OnChanged;
        Drain();
    }

    public void Detach() {
        if (_history != null)
            _history.Changed -= OnChanged;
        _history = null;
        _combatState = null;
        _lastSeenIndex = 0;
    }

    private void OnChanged() => Drain();

    private void Drain() {
        if (_history == null || _combatState == null) return;

        var entries = _history.Entries.ToList();
        if (entries.Count < _lastSeenIndex)
            _lastSeenIndex = 0;

        for (int i = _lastSeenIndex; i < entries.Count; i++)
            DispatchEntry(entries[i]);

        _lastSeenIndex = entries.Count;
    }

    private void DispatchEntry(CombatHistoryEntry entry) {
        if (_combatState == null) return;

        try {
            switch (entry) {
                case DamageReceivedEntry dmg:
                    CombatStatsTracker.RecordDamage(_combatState, dmg.Dealer, dmg.Receiver,
                        dmg.Result, dmg.CardSource, entry.RoundNumber);
                    break;
                case BlockGainedEntry block:
                    CombatStatsTracker.RecordBlockGained(block.Receiver, block.Amount, block.CardPlay);
                    break;
                case CardPlayFinishedEntry play:
                    CombatStatsTracker.RecordCardPlay(play.CardPlay);
                    break;
            }
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"CombatHistoryTailer: dispatch failed ({entry.GetType().Name}): {ex.Message}");
        }
    }
}
