using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DevMode.Presets;

/// <summary>A single card entry in a loadout preset.</summary>
public sealed class LoadoutCardEntry
{
    [JsonPropertyName("id")]
    public string CardId { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;

    [JsonPropertyName("upgrade")]
    public int UpgradeLevel { get; set; }
}

/// <summary>
/// Complete loadout preset: deck, relics, gold, HP, energy, stars, orb slots.
/// </summary>
public sealed class LoadoutPreset
{
    [JsonPropertyName("gold")]
    public int Gold { get; set; }

    [JsonPropertyName("currentHp")]
    public int CurrentHp { get; set; }

    [JsonPropertyName("maxHp")]
    public int MaxHp { get; set; }

    [JsonPropertyName("energy")]
    public int Energy { get; set; }

    [JsonPropertyName("maxEnergy")]
    public int MaxEnergy { get; set; }

    [JsonPropertyName("stars")]
    public int Stars { get; set; }

    [JsonPropertyName("orbSlots")]
    public int OrbSlots { get; set; }

    [JsonPropertyName("cards")]
    public List<LoadoutCardEntry> Cards { get; set; } = new();

    [JsonPropertyName("relics")]
    public List<string> Relics { get; set; } = new();
}

/// <summary>Named preset wrapper for serialization.</summary>
public sealed class NamedPreset<T> where T : class, new()
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("data")]
    public T Data { get; set; } = new();
}
