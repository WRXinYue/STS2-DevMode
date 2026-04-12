using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevMode.Settings;

/// <summary>
/// Loads and saves <see cref="DevModeSettings"/> to <c>settings.json</c> next to the mod assembly.
/// </summary>
public static class SettingsStore {
    private static readonly JsonSerializerOptions JsonOpts = new() {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static string FilePath =>
        Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
            "settings.json");

    public static DevModeSettings Current { get; private set; } = new();

    public static void Load() {
        try {
            if (!File.Exists(FilePath)) return;
            var json = File.ReadAllText(FilePath);
            Current = JsonSerializer.Deserialize<DevModeSettings>(json, JsonOpts) ?? new();
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"SettingsStore load failed: {ex.Message}");
            Current = new();
        }
    }

    public static void Save() {
        try {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Current, JsonOpts));
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"SettingsStore save failed: {ex.Message}");
        }
    }
}
