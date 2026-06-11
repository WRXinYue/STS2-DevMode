using KitLib.Logging;

namespace KitLib.Abstractions.Tests;

[Collection("KitLibLog")]
public class ModLogTests {
    [Fact]
    public void FormatWithCaller_builds_expected_shape() {
        var line = KitLibLogFormat.FormatWithCaller("loaded", "Initialize", @"C:\proj\Main.cs", 42);
        Assert.Equal("Main.cs:42 Initialize | loaded", line);
    }

    [Fact]
    public void ShouldEmit_Error_always_true() {
        Assert.True(ModLog.ShouldEmit(KitLogLevel.Error, KitLogLevel.Warn));
    }

    [Fact]
    public void ShouldEmit_minimum_Info_suppresses_Debug() {
        Assert.False(ModLog.ShouldEmit(KitLogLevel.Debug, KitLogLevel.Info));
        Assert.True(ModLog.ShouldEmit(KitLogLevel.Info, KitLogLevel.Info));
        Assert.True(ModLog.ShouldEmit(KitLogLevel.Warn, KitLogLevel.Info));
    }

    [Fact]
    public void Write_unbound_uses_fallback_with_FormatLine() {
        KitLogLevel? level = null;
        string? line = null;
        var log = new ModLog("my-mod", () => KitLogLevel.Debug, (l, formatted) => {
            level = l;
            line = formatted;
        });

        log.Info("Save", "checksum ok");

        Assert.Equal(KitLogLevel.Info, level);
        Assert.Equal("[my-mod][Save] checksum ok", line);
    }

    [Fact]
    public void Write_unbound_Debug_includes_caller_when_enabled() {
        string? line = null;
        var log = new ModLog("my-mod", () => KitLogLevel.Debug, (_, formatted) => line = formatted);
        log.Debug("trace", member: "Run", file: "Combat.cs", line: 9);
        Assert.StartsWith("[my-mod] Combat.cs:9 Run | trace", line);
    }

    [Fact]
    public void Write_unbound_Info_omits_caller() {
        string? line = null;
        var log = new ModLog("my-mod", () => KitLogLevel.Debug, (_, formatted) => line = formatted);
        log.Info("ready", member: "Run", file: "Combat.cs", line: 9);
        Assert.Equal("[my-mod] ready", line);
    }

    [Fact]
    public void Write_respects_minimum_level() {
        var count = 0;
        var log = new ModLog("my-mod", () => KitLogLevel.Warn, (_, _) => count++);
        log.Debug("skip");
        log.Info("skip");
        log.Warn("keep");
        Assert.Equal(1, count);
    }

    [Fact]
    public void Write_bound_forwards_to_KitLibLog() {
        KitLogLevel? level = null;
        string? scope = null;
        string? message = null;
        KitLibLog.Bind((l, s, m) => {
            level = l;
            scope = s;
            message = m;
        });

        try {
            var log = new ModLog("my-mod", () => KitLogLevel.Debug, (_, _) => { });
            log.Warn("Combat", "turn start");
            Assert.Equal(KitLogLevel.Warn, level);
            Assert.Equal("Combat", scope);
            Assert.Equal("turn start", message);
        }
        finally {
            KitLibLog.Bind(null);
        }
    }

    [Fact]
    public void Scope_forwards_scope_to_writer() {
        string? scope = null;
        KitLibLog.Bind((_, s, _) => scope = s);
        try {
            var log = new ModLog("my-mod", () => KitLogLevel.Debug, (_, _) => { });
            log.Scope("Save").Error("failed");
            Assert.Equal("Save", scope);
        }
        finally {
            KitLibLog.Bind(null);
        }
    }

    [Fact]
    public void CreateModLog_from_settings_respects_minimum_level() {
        var settings = new ModLogSettings { MinimumLevel = KitLogLevel.Warn };
        var count = 0;
        var log = settings.CreateModLog("my-mod", (_, _) => count++);

        log.Debug("skip");
        log.Info("skip");
        log.Warn("keep");

        Assert.Equal(1, count);
    }

    [Fact]
    public void Constructor_requires_mod_id_and_fallback() {
        Assert.Throws<ArgumentException>(() => new ModLog("  ", () => KitLogLevel.Info, (_, _) => { }));
        Assert.Throws<ArgumentNullException>(() => new ModLog("mod", () => KitLogLevel.Info, null!));
    }
}
