using DevMode.Multiplayer.SyncBot;
using DevMode.Settings;

namespace DevMode.Multiplayer.PseudoCoop;

/// <summary>One-click preset: host hand-plays, phantom + SyncBot + AI teammate.</summary>
internal static class PseudoCoopBootstrap {
    public static void ApplyPreset() {
        var s = SettingsStore.Current;
        s.AutoPlayEnabled = false;
        s.SyncBotEnabled = true;
        s.SyncBotSpawnPhantomPlayer = true;
        s.SyncBotAutoEndTurn = true;
        s.MpAiTeammateEnabled = true;
        SettingsStore.Save();
        SimulatedPeerRegistry.Refresh();
        MpCheatSyncBot.RefreshSimulatedPeers();
        MainFile.Logger.Info("[PseudoCoop] Preset applied (hand-play host + AI teammate + SyncBot).");
    }

    public static void TryAutoPresetOnLaunch() {
        if (!SettingsStore.Current.PseudoCoopAutoPresetOnLaunch) return;
        ApplyPreset();
    }
}
