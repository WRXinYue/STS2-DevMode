using System;
using System.Linq;

namespace DevMode.CombatStats;

/// <summary>
/// SW-inspired combat contribution score. Direct damage is the baseline (1 pt / 1 dmg);
/// mitigation and setup are discounted but still count toward the total.
/// </summary>
internal static class CombatScoreCalculator {
    /// <summary>Block / shield value vs damage (SW meters often use ~50–70%).</summary>
    public const float BlockWeight = 0.65f;

    /// <summary>Per stack of debuff applied (Vulnerable, Weak, etc.).</summary>
    public const int DebuffPerStack = 12;

    /// <summary>Per stack of buff applied (slightly below debuff — buffs are self-value).</summary>
    public const int BuffPerStack = 8;

    public const int PotionBase = 25;

    /// <summary>Floor for a non-attack card play (skills like Panic / setup).</summary>
    public const int UtilityBaseline = 3;

    /// <summary>Each energy spent on a utility card (SW: expensive setup still matters).</summary>
    public const int UtilityPerEnergy = 5;

    public static int UtilityPlayScore(int energySpent) =>
        UtilityBaseline + Math.Max(0, energySpent) * UtilityPerEnergy;

    public static int DamageScore(int amount) => amount;

    public static int BlockScore(int amount) => (int)Math.Round(amount * BlockWeight);

    public static int DebuffScore(int stacks) => stacks * DebuffPerStack;

    public static int BuffScore(int stacks) => stacks * BuffPerStack;

    public static int PotionScore() => PotionBase;

    /// <summary>Effective damage from debuff/buff synergy (Vulnerable bonus, Weak mitigation).</summary>
    public static int SynergyScore(int amount) => amount;

    public static int TotalScore(PlayerCombatStats player) =>
        player.Events.Sum(e => e.ScorePoints);

    public static CombatScoreBreakdown Breakdown(PlayerCombatStats player) {
        var bd = new CombatScoreBreakdown();
        foreach (var ev in player.Events) {
            switch (ev.Kind) {
                case CombatStatEventKind.DamageDealt:
                    bd.Damage += ev.ScorePoints;
                    break;
                case CombatStatEventKind.BlockGained:
                    bd.Block += ev.ScorePoints;
                    break;
                case CombatStatEventKind.DebuffApplied:
                    bd.Debuff += ev.ScorePoints;
                    break;
                case CombatStatEventKind.BuffApplied:
                    bd.Buff += ev.ScorePoints;
                    break;
                case CombatStatEventKind.CardPlayed:
                    bd.Utility += ev.ScorePoints;
                    break;
                case CombatStatEventKind.PotionUsed:
                    bd.Potion += ev.ScorePoints;
                    break;
                case CombatStatEventKind.PowerSynergy:
                    bd.Synergy += ev.ScorePoints;
                    break;
            }
        }
        return bd;
    }

    public static string FormatTimelineLine(CombatStatEvent ev) {
        string line = $"T{ev.Turn} · {ev.Text}";
        return ev.ScorePoints > 0 ? $"{line}  (+{ev.ScorePoints})" : line;
    }
}

internal sealed class CombatScoreBreakdown {
    public int Damage { get; set; }
    public int Block { get; set; }
    public int Debuff { get; set; }
    public int Buff { get; set; }
    public int Utility { get; set; }
    public int Potion { get; set; }
    public int Synergy { get; set; }
    public int Total => Damage + Block + Debuff + Buff + Utility + Potion + Synergy;
}
