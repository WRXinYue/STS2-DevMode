using System.Collections.Generic;
using System.Linq;
using DevMode.Multiplayer.Cheat;
using DevMode.Multiplayer.SyncBot;
using DevMode.Settings;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Multiplayer.PseudoCoop;

/// <summary>Unified roster for SyncBot ACKs vs in-process teammate simulation.</summary>
internal static class SimulatedPeerRegistry {
    static HashSet<ulong> _simulatedPeerNetIds = [];

    public static bool IsHostMultiplayer =>
        MpCheatSession.InMultiplayerRun && MpCheatSession.IsHost;

    public static bool IsRegistryActive =>
        IsHostMultiplayer
        && (SettingsStore.Current.SyncBotEnabled || SettingsStore.Current.MpAiTeammateEnabled);

    /// <summary>True when <paramref name="netId"/> is a real ENet lobby peer (not phantom/debug).</summary>
    public static bool IsLiveEnetPeer(ulong netId) {
        if (netId == 0 || netId == MpCheatSyncBot.PhantomPlayerNetId) return false;
        if (RunManager.Instance?.NetService is not NetHostGameService host) return false;
        return host.ConnectedPeers.Any(p => p.peerId == netId);
    }

    /// <summary>Run players that need in-process votes/choices (phantom 1001, etc.).</summary>
    public static IEnumerable<Player> GetPeersNeedingSimulation() {
        var run = RunManager.Instance;
        var state = run?.DebugOnlyGetState();
        var hostNetId = run?.NetService?.NetId ?? 0;
        if (state == null || hostNetId == 0) return [];

        return state.Players.Where(p => p.NetId != hostNetId && !IsLiveEnetPeer(p.NetId));
    }

    /// <summary>Remote peers in the run (non-host).</summary>
    public static HashSet<ulong> GetRemoteRunNetIds() {
        var run = RunManager.Instance;
        var hostNetId = run?.NetService?.NetId ?? 0;
        var state = run?.DebugOnlyGetState();
        if (state == null || hostNetId == 0) return [];

        return state.Players
            .Select(p => p.NetId)
            .Where(id => id != hostNetId)
            .ToHashSet();
    }

    /// <summary>All remote run peers — used for MpCheat prepare ACK injection when SyncBot is on.</summary>
    public static HashSet<ulong> GetAckPeerNetIds() {
        if (!SettingsStore.Current.SyncBotEnabled
            || !IsHostMultiplayer
            || !MpCheatSession.CanUseMultiplayerCheats)
            return [];
        return GetSimulatedPeerNetIds();
    }

    /// <summary>Net ids for auto-vote / combat sync injection.</summary>
    public static HashSet<ulong> GetSimulatedPeerNetIds() {
        if (!IsRegistryActive) return [];
        return GetPeersNeedingSimulation().Select(p => p.NetId).ToHashSet();
    }

    public static void Refresh() {
        _simulatedPeerNetIds = GetSimulatedPeerNetIds();
    }

    public static bool IsSimulatedPeer(ulong netId) =>
        IsRegistryActive && _simulatedPeerNetIds.Contains(netId);

    public static void OnRunEnded() => _simulatedPeerNetIds.Clear();
}
