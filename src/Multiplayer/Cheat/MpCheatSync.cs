using DevMode.Settings;

namespace DevMode.Multiplayer.Cheat;

/// <summary>Initializes multiplayer cheat sync (INetMessage + run lifecycle).</summary>
public static class MpCheatSync {
    public static void Initialize() {
        MpCheatSession.SetLocalOptIn(SettingsStore.Current.MultiplayerCheatOptIn);
    }

    public static void OnRunStarted() {
        if (!MpCheatSession.InMultiplayerRun) return;

        MpCheatSession.TryArmSession("run_started");
        if (!MpCheatSession.SessionArmed) return;

        MpCheatNetBus.TryRegisterHandlers();

        if (MpCheatSession.IsHost) {
            var netId = MpCheatSession.LocalNetId;
            var config = MpCheatConfig.MergeLocalEdits(new MpCheatConfig(), netId, includeSharedGlobals: true);
            MpCheatNetBus.HostPublishConfig(config, "run_start");
        }
    }

    public static void OnRunEnded() {
        MpCheatNetBus.Reset();
        MpCheatSession.OnRunEnded();
    }

    public static void HostPublishFromDevModeState(string reason) {
        if (!MpCheatSession.CanEditMultiplayerCheats) return;
        var netId = MpCheatSession.LocalNetId;
        if (netId == 0) return;
        var config = MpCheatConfig.MergeLocalEdits(MpCheatState.Config, netId, includeSharedGlobals: true);
        MpCheatNetBus.HostPublishConfig(config, reason);
    }

    public static void BroadcastCommand(MpCheatCommandMessage message) =>
        MpCheatNetBus.BroadcastCommand(message);

    public static void TryPersistConfig(MpCheatConfig config) =>
        MpCheatRunSavedData.TryWrite(config);
}
