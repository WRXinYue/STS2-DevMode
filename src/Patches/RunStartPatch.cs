using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using DevMode.Presets;

namespace DevMode.Patches;

[HarmonyPatch(typeof(RunManager))]
public static class RunStartPatch
{
    /// <summary>
    /// Disable save persistence for dev-mode runs.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(RunManager.SetUpNewSinglePlayer))]
    public static void DisableSaveForDevRun(ref bool shouldSave)
    {
        if (DevModeState.IsActive)
        {
            shouldSave = false;
            MainFile.Logger.Info("DevMode: Save disabled for dev run.");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(RunManager.Launch))]
    public static void InjectDevContent(RunState __result)
    {
        if (!DevModeState.IsActive) return;

        MainFile.Logger.Info("DevMode: Injecting dev mode content into run...");

        foreach (var player in __result.Players)
        {
            InjectForPlayer(player);
        }

        ApplyPendingRestart(__result);

        DevModeState.OnRunStarted();
        MainFile.Logger.Info("DevMode: Dev mode content injected successfully.");
    }

    private static void ApplyPendingRestart(RunState runState)
    {
        // Apply carried-over gold (direct, synchronous)
        if (DevModeState.PendingRestartGold.HasValue)
        {
            var gold = DevModeState.PendingRestartGold.Value;
            foreach (var player in runState.Players)
                player.Gold = gold;
            MainFile.Logger.Info($"[DevMode] Restart: applied gold {gold}.");
        }

        // Apply carried-over cards / relics (async via game command queue)
        if (DevModeState.PendingRestartPreset != null)
        {
            var preset = DevModeState.PendingRestartPreset;
            var scope  = DevModeState.PendingRestartScope;
            MainFile.Logger.Info($"[DevMode] Restart: scheduling preset apply (scope: {scope}).");
            TaskHelper.RunSafely(PresetManager.ApplyToRunAsync(preset, scope));
        }

        DevModeState.ClearPendingRestart();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(RunManager.OnEnded))]
    public static void OnRunEnded()
    {
        DevModeState.OnRunEnded();
    }

    private static void InjectForPlayer(Player player)
    {
        if (DevModeState.MaxEnergy > 0)
        {
            player.MaxEnergy = DevModeState.MaxEnergy;
            MainFile.Logger.Info($"DevMode: Set max energy to {DevModeState.MaxEnergy}");
        }
    }
}

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveProgressFile))]
public static class SaveProgressPatch
{
    public static bool Prefix()
    {
        if (DevModeState.InDevRun)
        {
            MainFile.Logger.Info("DevMode: Skipping progress save for dev run.");
            return false;
        }
        return true;
    }
}

/// <summary>
/// Intercepts NGame.StartNewSingleplayerRun to inject PendingRestartSeed.
/// NGame.DebugSeedOverride cannot be used here because NCharacterSelectScreen.BeginRun
/// overwrites it from its own settings (and clears it) before the run launches.
/// </summary>
[HarmonyPatch(typeof(NGame), nameof(NGame.StartNewSingleplayerRun))]
public static class SeedInjectPatch
{
    public static void Prefix(ref string seed)
    {
        if (DevModeState.PendingRestartSeed == null) return;

        var canonicalized = SeedHelper.CanonicalizeSeed(DevModeState.PendingRestartSeed);
        MainFile.Logger.Info($"[DevMode] SeedInject: overriding seed '{seed}' → '{canonicalized}'.");
        seed = canonicalized;

        // Consumed — clear so a subsequent normal run is not affected.
        DevModeState.PendingRestartSeed = null;
    }
}
