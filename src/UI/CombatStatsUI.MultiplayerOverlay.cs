using System.Collections.Generic;
using System.Linq;
using DevMode.CombatStats;
using DevMode.Settings;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.UI;

internal static partial class CombatStatsUI {
    internal const string MultiplayerOverlayRootName = "DevModeCombatStatsMpOverlay";

    private static MultiplayerOverlayHost? _mpOverlay;

    internal static bool IsMultiplayerOverlayActive() => _mpOverlay?.IsPanelVisible ?? false;

    internal static bool IsMultiplayerOverlayEnabled() =>
        SettingsStore.Current.CombatStatsMpOverlayEnabled;

    internal static bool CanShowMultiplayerOverlay() =>
        ShouldUseMultiplayerOverlay() && IsMultiplayerOverlayEnabled();

    internal static void SyncMultiplayerOverlayState() {
        if (CanShowMultiplayerOverlay())
            RefreshMultiplayerOverlay();
        else
            HideMultiplayerOverlay();
    }

    internal static bool ShouldUseMultiplayerOverlay() {
        var run = RunManager.Instance;
        if (run?.IsInProgress != true)
            return false;
        if (run.NetService?.Type == NetGameType.Singleplayer)
            return false;

        var combat = CombatManager.Instance?.DebugOnlyGetState();
        if (combat != null && combat.Players.Count > 1)
            return true;

        var state = run.DebugOnlyGetState();
        return state != null && state.Players.Count > 1;
    }

    internal static void AttachMultiplayerOverlay(NGlobalUi globalUi) {
        if (_mpOverlay != null || ((Node)globalUi).GetNodeOrNull<Control>(MultiplayerOverlayRootName) != null)
            return;

        _mpOverlay = new MultiplayerOverlayHost();
        ((Node)globalUi).AddChild(_mpOverlay);
        _mpOverlay.TreeExiting += () => _mpOverlay = null;
    }

    internal static void DetachMultiplayerOverlay(NGlobalUi globalUi) {
        ((Node)globalUi).GetNodeOrNull<Control>(MultiplayerOverlayRootName)?.QueueFree();
        _mpOverlay = null;
    }

    internal static void RefreshMultiplayerOverlay() => _mpOverlay?.Refresh();

    internal static void HideMultiplayerOverlay() => _mpOverlay?.HidePanel();
}
