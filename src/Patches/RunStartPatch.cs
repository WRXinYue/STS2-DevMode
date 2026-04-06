using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

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

        DevModeState.OnRunStarted();
        MainFile.Logger.Info("DevMode: Dev mode content injected successfully.");
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
