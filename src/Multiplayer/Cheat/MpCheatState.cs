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
        if (MpCheatSession.InMultiplayerRun)
            _config.ApplyToLocalDevModeState(MpCheatSession.LocalNetId);
        else
            _config.ApplyToDevModeState();
        MainFile.Logger.Info($"[MpCheat] Applied config rev={revision} ({reason}).");
    }

    /// <summary>Apply local DevModeState to in-memory config before host round-trip (client UI toggles).</summary>
    public static void ApplyOptimisticFromDevModeState() {
        if (!MpCheatSession.InMultiplayerRun) return;
        var netId = MpCheatSession.LocalNetId;
        if (netId == 0) return;
        _config = MpCheatConfig.MergeLocalEdits(_config, netId, MpCheatSession.IsHost);
        MainFile.Logger.Debug("[MpCheat] Optimistic per-player config applied.");
    }

    public static void Clear() {
        _config = new MpCheatConfig();
        _revision = 0;
    }
}
