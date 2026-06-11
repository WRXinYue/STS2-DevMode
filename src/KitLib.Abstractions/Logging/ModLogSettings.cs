using System.Text.Json;

namespace KitLib.Logging;

/// <summary>Minimal JSON persistence for a mod's local <see cref="ModLog"/> minimum level.</summary>
public sealed class ModLogSettings {
    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public KitLogLevel MinimumLevel { get; set; } = KitLogLevel.Info;

    public static ModLogSettings Load(string path, KitLogLevel defaultLevel = KitLogLevel.Info) {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new ModLogSettings { MinimumLevel = defaultLevel };

        try {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<ModLogSettings>(json, JsonOpts);
            return loaded ?? new ModLogSettings { MinimumLevel = defaultLevel };
        }
        catch {
            return new ModLogSettings { MinimumLevel = defaultLevel };
        }
    }

    public void Save(string path) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, JsonOpts));
        File.Move(tmp, path, overwrite: true);
    }

    public ModLog CreateModLog(string modId, Action<KitLogLevel, string> fallback, bool includeCallerOnDebug = true) =>
        new(modId, () => MinimumLevel, fallback, includeCallerOnDebug);
}
