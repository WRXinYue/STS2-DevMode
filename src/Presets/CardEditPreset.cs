using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace DevMode.Presets;

public sealed class CardEditTemplate {
    [JsonPropertyName("baseCost")]
    public int? BaseCost { get; set; }

    [JsonPropertyName("replayCount")]
    public int? ReplayCount { get; set; }

    [JsonPropertyName("damage")]
    public int? Damage { get; set; }

    [JsonPropertyName("block")]
    public int? Block { get; set; }

    [JsonPropertyName("dynamicVars")]
    public Dictionary<string, int>? DynamicVars { get; set; }

    [JsonPropertyName("exhaust")]
    public bool? Exhaust { get; set; }

    [JsonPropertyName("ethereal")]
    public bool? Ethereal { get; set; }

    [JsonPropertyName("unplayable")]
    public bool? Unplayable { get; set; }

    [JsonPropertyName("exhaustOnNextPlay")]
    public bool? ExhaustOnNextPlay { get; set; }

    [JsonPropertyName("singleTurnRetain")]
    public bool? SingleTurnRetain { get; set; }

    [JsonPropertyName("singleTurnSly")]
    public bool? SingleTurnSly { get; set; }

    public bool HasAnyPatch() =>
        BaseCost.HasValue
        || ReplayCount.HasValue
        || Damage.HasValue
        || Block.HasValue
        || Exhaust.HasValue
        || Ethereal.HasValue
        || Unplayable.HasValue
        || ExhaustOnNextPlay.HasValue
        || SingleTurnRetain.HasValue
        || SingleTurnSly.HasValue
        || DynamicVars is { Count: > 0 };

    /// <summary>Merges non-null fields from <paramref name="patch"/> into this template.</summary>
    public void MergePatch(CardEditTemplate patch) {
        if (patch.BaseCost.HasValue) BaseCost = patch.BaseCost;
        if (patch.ReplayCount.HasValue) ReplayCount = patch.ReplayCount;
        if (patch.Damage.HasValue) Damage = patch.Damage;
        if (patch.Block.HasValue) Block = patch.Block;
        if (patch.Exhaust.HasValue) Exhaust = patch.Exhaust;
        if (patch.Ethereal.HasValue) Ethereal = patch.Ethereal;
        if (patch.Unplayable.HasValue) Unplayable = patch.Unplayable;
        if (patch.ExhaustOnNextPlay.HasValue) ExhaustOnNextPlay = patch.ExhaustOnNextPlay;
        if (patch.SingleTurnRetain.HasValue) SingleTurnRetain = patch.SingleTurnRetain;
        if (patch.SingleTurnSly.HasValue) SingleTurnSly = patch.SingleTurnSly;
        if (patch.DynamicVars is not { Count: > 0 }) return;
        DynamicVars ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in patch.DynamicVars)
            DynamicVars[kv.Key] = kv.Value;
    }
}

public sealed class CardEditNamedPreset {
    [JsonPropertyName("cardId")]
    public string CardId { get; set; } = "";

    [JsonPropertyName("template")]
    public CardEditTemplate Template { get; set; } = new();
}

internal static class CardEditPresetManager {
    private static string PresetsDir => DataPaths.PresetsDir;

    private static readonly System.Lazy<PresetStore<CardEditNamedPreset>> _store = new(() => {
        var store = new PresetStore<CardEditNamedPreset>(Path.Combine(PresetsDir, "card-edit-presets.json"));
        store.Load();
        return store;
    });

    public static PresetStore<CardEditNamedPreset> Store => _store.Value;
}
