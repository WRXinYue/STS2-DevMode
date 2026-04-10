using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using DevMode.UI;

namespace DevMode.Patches;

/// <summary>
/// Flushes <see cref="DevPanelRegistry.RegisterPanelWhenReady"/> once, after every mod initializer has run.
/// </summary>
[HarmonyPatch(typeof(LocManager), nameof(LocManager.Initialize))]
internal static class LocManagerExternalPanelRegistrationPatch
{
    private static bool _done;

    private static void Prefix()
    {
        if (_done)
            return;
        _done = true;
        DevPanelRegistry.FlushPostModLoadRegistrations();
    }
}
