using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Presets;

/// <summary>
/// Manages all preset types: loadout, card, relic, power, potion, event, encounter, monster.
/// Provides capture-from-run and apply-to-run operations.
/// </summary>
internal static class PresetManager
{
    private static string PresetsDir => Path.Combine(
        Path.GetDirectoryName(typeof(PresetManager).Assembly.Location) ?? ".",
        "presets");

    private static readonly Lazy<PresetStore<LoadoutPreset>> _loadouts = new(() =>
    {
        var store = new PresetStore<LoadoutPreset>(Path.Combine(PresetsDir, "loadouts.json"));
        store.Load();
        return store;
    });

    public static PresetStore<LoadoutPreset> Loadouts => _loadouts.Value;

    /// <summary>Capture current run state into a LoadoutPreset.</summary>
    public static LoadoutPreset? CaptureFromRun()
    {
        if (!RunContext.TryGetRunAndPlayer(out var runState, out var player)) return null;

        var preset = new LoadoutPreset
        {
            Gold = player.Gold,
            CurrentHp = player.Creature.CurrentHp,
            MaxHp = player.Creature.MaxHp,
            Energy = player.PlayerCombatState?.Energy ?? 0,
            MaxEnergy = player.MaxEnergy,
            Stars = player.PlayerCombatState?.Stars ?? 0,
            OrbSlots = player.BaseOrbSlotCount
        };

        // Capture deck
        var deckCards = player.Deck?.Cards;
        if (deckCards != null)
        {
            var grouped = deckCards
                .Where(c => c != null)
                .GroupBy(c => new { Id = ((AbstractModel)c).Id.Entry, Upgrade = c.CurrentUpgradeLevel })
                .Select(g => new LoadoutCardEntry
                {
                    CardId = g.Key.Id,
                    Count = g.Count(),
                    UpgradeLevel = g.Key.Upgrade
                });
            preset.Cards.AddRange(grouped);
        }

        // Capture relics
        var relics = player.Relics;
        if (relics != null)
        {
            foreach (var relic in relics)
            {
                if (relic != null)
                    preset.Relics.Add(((AbstractModel)relic).Id.Entry);
            }
        }

        return preset;
    }

    /// <summary>Apply a LoadoutPreset to the current run.</summary>
    public static async Task ApplyToRunAsync(LoadoutPreset preset)
    {
        if (!RunContext.TryGetRunAndPlayer(out var runState, out var player)) return;

        // Check not in combat
        if (MegaCrit.Sts2.Core.Combat.CombatManager.Instance?.IsInProgress == true)
        {
            MainFile.Logger.Warn("Cannot apply preset during combat.");
            return;
        }

        try
        {
            // Set gold
            await PlayerCmd.SetGold((decimal)preset.Gold, player);

            // Set HP
            await Sts2ApiCompat.SetMaxHpAsync(player.Creature, preset.MaxHp);
            await Sts2ApiCompat.SetCurrentHpAsync(player.Creature, preset.CurrentHp);

            // Set energy
            player.MaxEnergy = preset.MaxEnergy;

            // Set orb slots
            player.BaseOrbSlotCount = preset.OrbSlots;

            // Remove all relics
            foreach (var relic in player.Relics.ToArray())
            {
                if (relic != null)
                    await RelicCmd.Remove(relic);
            }

            // Add preset relics
            foreach (var relicId in preset.Relics)
            {
                var model = ModelDb.AllRelics.FirstOrDefault(r => ((AbstractModel)r).Id.Entry == relicId);
                if (model != null)
                    await RelicCmd.Obtain(model.ToMutable(), player, -1);
            }

            // Remove all deck cards
            foreach (var card in player.Deck.Cards.ToArray())
            {
                if (card != null)
                    await CardPileCmd.RemoveFromDeck(card, false);
            }

            // Add preset cards
            foreach (var entry in preset.Cards)
            {
                var model = ModelDb.AllCards.FirstOrDefault(c => ((AbstractModel)c).Id.Entry == entry.CardId);
                if (model == null) continue;

                for (int i = 0; i < entry.Count; i++)
                {
                    var card = Sts2ApiCompat.CreateCardForCurrentContext(runState, model, player, false);
                    for (int u = 0; u < entry.UpgradeLevel; u++)
                        CardCmd.Upgrade(card);
                    await CardPileCmd.Add(card, PileType.Deck, skipVisuals: true);
                }
            }

            MainFile.Logger.Info("Preset applied successfully.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Preset apply failed: {ex.Message}");
        }
    }

    /// <summary>Export a preset to clipboard as JSON.</summary>
    public static string ExportToClipboard(string name, LoadoutPreset preset)
    {
        var payload = new { name, preset };
        var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        Godot.DisplayServer.ClipboardSet(json);
        return json;
    }

    /// <summary>Import a preset from clipboard JSON.</summary>
    public static (string? name, LoadoutPreset? preset) ImportFromClipboard()
    {
        try
        {
            var json = Godot.DisplayServer.ClipboardGet();
            if (string.IsNullOrWhiteSpace(json)) return (null, null);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
            var presetJson = root.TryGetProperty("preset", out var p) ? p.GetRawText() : null;
            if (presetJson == null) return (null, null);

            var preset = System.Text.Json.JsonSerializer.Deserialize<LoadoutPreset>(presetJson);
            return (name, preset);
        }
        catch
        {
            return (null, null);
        }
    }
}
