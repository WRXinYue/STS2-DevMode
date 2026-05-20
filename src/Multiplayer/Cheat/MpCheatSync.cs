using DevMode.Settings;

namespace DevMode.Multiplayer.Cheat;

/// <summary>Initializes multiplayer cheat sync (Sidecar + run lifecycle).</summary>
public static class MpCheatSync {
    public static void Initialize() {
        MpCheatSession.SetLocalOptIn(SettingsStore.Current.MultiplayerCheatOptIn);
    }

    public static void OnRunStarted() {
        if (!MpCheatSession.InMultiplayerRun) return;

        MpCheatSession.TryArmSession("run_started");
        if (!MpCheatSession.SessionArmed) return;

        MpCheatSidecarBridge.EnsureBootstrapped();

        if (MpCheatSession.IsHost) {
            var config = MpCheatConfig.FromDevModeState();
            config.SessionEnabled = true;
            MpCheatSidecarBridge.HostPublishConfig(config, "run_start");
        }
    }

    public static void OnRunEnded() {
        MpCheatSidecarBridge.Shutdown();
        MpCheatSession.OnRunEnded();
    }

    public static void HostPublishFromDevModeState(string reason) {
        if (!MpCheatSession.CanEditMultiplayerCheats) return;
        var config = MpCheatConfig.FromDevModeState();
        config.SessionEnabled = true;
        MpCheatSidecarBridge.HostPublishConfig(config, reason);
    }

    public static void BroadcastCommand(MpCheatCommandMessage message) =>
        MpCheatSidecarBridge.BroadcastCommand(message);

    public static void TryPersistConfig(MpCheatConfig config) =>
        MpCheatRunSavedData.TryWrite(config);
}
