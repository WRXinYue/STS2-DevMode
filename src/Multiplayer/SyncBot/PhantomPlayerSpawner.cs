using System.Linq;
using DevMode.Multiplayer.PseudoCoop;
using DevMode.Settings;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

namespace DevMode.Multiplayer.SyncBot;

/// <summary>Spawns NetId 1001 after map history exists (not safe during RunManager.Launch postfix).</summary>
internal static class PhantomPlayerSpawner {
    public static bool TrySpawn(RunState? state) {
        if (state == null) return false;
        if (!SettingsStore.Current.SyncBotSpawnPhantomPlayer) return false;
        if (RunManager.Instance?.NetService?.Type != NetGameType.Host) return false;
        if (state.Players.Count != 1) return false;
        if (state.Players.Any(p => p.NetId == MpCheatSyncBot.PhantomPlayerNetId)) return false;
        if (state.CurrentMapPointHistoryEntry == null) return false;

        try {
            var host = state.Players[0];
            if (host.Character == null)
                throw new System.InvalidOperationException("Host character is null.");

            var unlock = host.UnlockState ?? new UnlockState(SaveManager.Instance.Progress);
            var phantom = Player.CreateForNewRun(host.Character, unlock, MpCheatSyncBot.PhantomPlayerNetId);
            state.AddPlayerDebug(phantom, -1);
            MpCheatSyncBot.RefreshSimulatedPeers();
            SimulatedPeerRegistry.Refresh();

            RunManager.Instance?.MapSelectionSynchronizer?.OnLocationChanged(state.MapLocation);
            PseudoCoopLobbyRoster.RegisterSimulatedPeer(MpCheatSyncBot.PhantomPlayerNetId);
            PseudoCoopActionQueue.EnsureQueueForPlayer(phantom);
            PseudoCoopMultiplayerUiRefresh.TryRefreshAfterPlayerJoined(state);

            MainFile.Logger.Info(
                $"[SyncBot] Phantom player spawned netId={MpCheatSyncBot.PhantomPlayerNetId} character={host.Character.Id.Entry}.");
            return true;
        }
        catch (System.Exception ex) {
            MainFile.Logger.Warn($"[SyncBot] Phantom player spawn failed: {ex}");
            return false;
        }
    }
}
