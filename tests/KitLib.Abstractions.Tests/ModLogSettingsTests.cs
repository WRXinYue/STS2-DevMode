using KitLib.Logging;

namespace KitLib.Abstractions.Tests;

public sealed class ModLogSettingsTests {
    [Fact]
    public void Load_missing_file_returns_default_level() {
        var path = Path.Combine(Path.GetTempPath(), $"modlog-settings-{Guid.NewGuid():N}.json");

        var settings = ModLogSettings.Load(path, KitLogLevel.Warn);

        Assert.Equal(KitLogLevel.Warn, settings.MinimumLevel);
    }

    [Fact]
    public void Save_and_load_round_trips_level() {
        var path = Path.Combine(Path.GetTempPath(), $"modlog-settings-{Guid.NewGuid():N}.json");
        try {
            var original = new ModLogSettings { MinimumLevel = KitLogLevel.Debug };
            original.Save(path);

            var loaded = ModLogSettings.Load(path);

            Assert.Equal(KitLogLevel.Debug, loaded.MinimumLevel);
        }
        finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Load_invalid_json_returns_default_level() {
        var path = Path.Combine(Path.GetTempPath(), $"modlog-settings-{Guid.NewGuid():N}.json");
        try {
            File.WriteAllText(path, "{ not json");

            var settings = ModLogSettings.Load(path, KitLogLevel.Error);

            Assert.Equal(KitLogLevel.Error, settings.MinimumLevel);
        }
        finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
