namespace DevMode.Multiplayer.Cheat;

/// <summary>Authoritative in-run multiplayer cheat config (not per-frame).</summary>
public static class MpCheatState {
    private static MpCheatConfig _config = new();
    private static long _revision;

    public static long Revision => _revision;

    public static bool IsActive => _config.SessionEnabled;

    public static MpCheatConfig Config => _config;

    public static void ApplySnapshot(MpCheatConfig config, long revision, string reason) {
        _config = config;
        _revision = revision;
        _config.ApplyToDevModeState();
        MainFile.Logger.Info($"[MpCheat] Applied config rev={revision} ({reason}).");
    }

    public static void Clear() {
        _config = new MpCheatConfig();
        _revision = 0;
    }
}
