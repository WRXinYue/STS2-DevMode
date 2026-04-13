using DevMode.Modding;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace DevMode.Patches;

/// <summary>
/// Flushes <see cref="ModLoadCoordinator"/> once, after every mod initializer has run
/// (covers <see cref="UI.DevPanelRegistry.RegisterPanelWhenReady"/> and <see cref="ModRuntime.RegisterAfterAllModsLoaded"/>).
/// </summary>
[HarmonyPatch(typeof(LocManager), nameof(LocManager.Initialize))]
internal static class LocManagerExternalPanelRegistrationPatch {
    private static bool _done;

    private static void Prefix() {
        if (_done)
            return;
        _done = true;
        ModLoadCoordinator.Flush();
    }
}
