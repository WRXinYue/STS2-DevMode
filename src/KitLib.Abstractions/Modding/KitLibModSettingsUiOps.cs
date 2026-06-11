using KitLib.Logging;

namespace KitLib.Abstractions.Modding;

/// <summary>Mod settings UI hooks registered by KitLib.ModPanel; content mods invoke from native registry <c>BuildBody</c>.</summary>
public static class KitLibModSettingsUiOps {
    /// <summary>title, description, getLevel, setLevel → Godot Control (<see cref="object"/>).</summary>
    public static Func<string, string?, Func<KitLogLevel>, Action<KitLogLevel>, object>?
        BuildLogLevelRow { get; set; }
}
