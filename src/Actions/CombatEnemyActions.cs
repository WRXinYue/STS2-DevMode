using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace DevMode.Actions;

/// <summary>
/// Mid-combat enemy manipulation: add monsters, kill enemies, remove enemies.
/// Uses the game's <see cref="CreatureCmd"/> API (same as boss summon mechanics).
/// </summary>
internal static class CombatEnemyActions {
    /// <summary>Get the current combat state, or null if not in combat.</summary>
    public static CombatState? GetCombatState() {
        if (!RunContext.TryGetRunAndPlayer(out _, out var player)) return null;
        return player.Creature?.CombatState;
    }

    /// <summary>Get current enemies in combat.</summary>
    public static IReadOnlyList<Creature> GetCurrentEnemies() {
        var cs = GetCombatState();
        return cs?.Enemies ?? (IReadOnlyList<Creature>)[];
    }

    /// <summary>Add a monster to the current combat.</summary>
    public static async Task<Creature?> AddMonster(MonsterModel canonicalMonster) {
        var cs = GetCombatState();
        if (cs == null) {
            MainFile.Logger.Info("CombatEnemyActions: Not in combat.");
            return null;
        }

        if (!CombatManager.Instance.IsInProgress) {
            MainFile.Logger.Info("CombatEnemyActions: Combat not in progress.");
            return null;
        }

        var mutable = canonicalMonster.ToMutable();

        // Try to get a slot from the encounter if available
        string? slot = null;
        try {
            slot = cs.Encounter?.GetNextSlot(cs);
            if (string.IsNullOrEmpty(slot)) slot = null;
        }
        catch { /* no slots available */ }

        var creature = await CreatureCmd.Add(mutable, cs, CombatSide.Enemy, slot);
        MainFile.Logger.Info($"CombatEnemyActions: Added {((AbstractModel)canonicalMonster).Id.Entry} to combat");

        // Reposition all enemies if no slot-based scene (official PositionEnemies logic)
        if (slot == null)
            RepositionEnemies(cs);

        return creature;
    }

    /// <summary>Add all monsters from an encounter to the current combat.</summary>
    public static async Task AddEncounterMonsters(EncounterModel encounter) {
        var monsters = encounter.AllPossibleMonsters?.ToList();
        if (monsters == null || monsters.Count == 0) return;

        foreach (var monster in monsters)
            await AddMonster(monster);
    }

    /// <summary>
    /// Reposition all enemy NCreature nodes using the same algorithm as
    /// NCombatRoom.PositionEnemies (auto-layout for encounters without scene slots).
    /// </summary>
    private static void RepositionEnemies(CombatState cs) {
        var combatRoom = NCombatRoom.Instance;
        if (combatRoom == null) return;

        float scaling = cs.Encounter?.GetCameraScaling() ?? 1f;

        // Collect enemy creature nodes (non-player, non-pet, alive)
        var enemies = combatRoom.CreatureNodes
            .Where(n => GodotObject.IsInstanceValid(n)
                     && !n.Entity.IsPlayer
                     && n.Entity.PetOwner == null
                     && !n.Entity.IsDead)
            .ToList();

        if (enemies.Count == 0) return;

        // --- Replicate NCombatRoom.PositionEnemies ---
        float halfScreen = 960f / scaling;
        float padding = 70f;
        float totalCreatureWidth = enemies.Sum(n => n.Visuals.Bounds.Size.X);
        float totalWidth = totalCreatureWidth + (enemies.Count - 1) * padding;
        float startX = (halfScreen - totalWidth) * 0.5f;
        startX = Math.Max(startX, 150f);

        float altY = 0f;
        if (startX + totalWidth > halfScreen) {
            padding = Math.Max((halfScreen - 150f - totalCreatureWidth) / (enemies.Count - 1), 5f);
            totalWidth = totalCreatureWidth + (enemies.Count - 1) * padding;
            startX = (halfScreen - totalWidth) * 0.5f;
            if (padding < 30f)
                altY = float.Lerp(60f, 40f, (padding - 5f) / 25f);
        }

        float x = startX;
        for (int i = 0; i < enemies.Count; i++) {
            var n = enemies[i];
            n.Position = new Vector2(
                x + n.Visuals.Bounds.Size.X * 0.5f,
                200f - ((i % 2 != 0) ? altY : 0f));
            x += n.Visuals.Bounds.Size.X + padding;
        }
    }

    /// <summary>Kill a specific enemy creature.</summary>
    public static async Task KillEnemy(Creature creature) {
        if (creature.IsDead) return;
        await CreatureCmd.Kill(creature, force: true);
        MainFile.Logger.Info($"CombatEnemyActions: Killed {creature.Monster?.Title?.GetFormattedText() ?? "enemy"}");
    }

    /// <summary>Kill all current enemies.</summary>
    public static async Task KillAllEnemies() {
        var enemies = GetCurrentEnemies().Where(e => !e.IsDead).ToList();
        if (enemies.Count == 0) return;
        await CreatureCmd.Kill((IReadOnlyCollection<Creature>)enemies, force: true);
        MainFile.Logger.Info($"CombatEnemyActions: Killed all {enemies.Count} enemies");
    }

    // ── Monster editing enhancements ──

    /// <summary>Set a monster's current HP.</summary>
    public static async Task SetMonsterHp(Creature creature, int hp) {
        await Sts2ApiCompat.SetCurrentHpAsync(creature, hp);
    }

    /// <summary>Set a monster's max HP.</summary>
    public static async Task SetMonsterMaxHp(Creature creature, int maxHp) {
        await Sts2ApiCompat.SetMaxHpAsync(creature, maxHp);
    }

    /// <summary>Clear all powers from a monster.</summary>
    public static void ClearMonsterPowers(Creature creature) {
        foreach (var power in creature.Powers.ToArray()) {
            if (power != null)
                PowerCmd.Remove(power);
        }
    }

    /// <summary>Duplicate a monster in combat (add another copy).</summary>
    public static async Task<Creature?> DuplicateMonster(Creature creature) {
        var monsterModel = creature.Monster;
        if (monsterModel == null) return null;
        return await AddMonster(monsterModel);
    }

    /// <summary>Get display info for a creature.</summary>
    public static string GetCreatureInfo(Creature creature) {
        var name = creature.Monster?.Title?.GetFormattedText() ?? "?";
        return $"{name} (HP: {creature.CurrentHp}/{creature.MaxHp}, Block: {creature.Block}, Powers: {creature.Powers.Count})";
    }
}
