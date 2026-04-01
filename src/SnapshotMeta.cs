using System;
using System.Collections.Generic;

namespace DevMode;

/// <summary>
/// Lightweight sidecar metadata stored alongside each snapshot slot.
/// Used to populate the slot selection UI without deserializing the full run save.
/// </summary>
internal sealed class SnapshotMeta
{
    public string Name { get; set; } = "";
    public long SaveTime { get; set; }
    public int TotalFloor { get; set; }
    public int Gold { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public string CharacterId { get; set; } = "";
    public List<string> CardTitles { get; set; } = new();
    public List<string> RelicTitles { get; set; } = new();

    public string FormattedTime => SaveTime > 0
        ? DateTimeOffset.FromUnixTimeSeconds(SaveTime).LocalDateTime.ToString("MM/dd HH:mm")
        : "";

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"Slot" : Name;
}
