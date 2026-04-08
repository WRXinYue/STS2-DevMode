using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using DevMode.Patches;
using DevMode.Scripts;
using DevMode.Settings;

namespace DevMode;

[ModInitializer(nameof(Initialize))]
public class MainFile
{
    public const string ModID = "DevMode";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModID, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("DevMode initializing...");

        // Load persisted settings (theme, etc.) before anything else
        SettingsStore.Load();

        // Initialize localization before anything else
        I18N.Initialize();

        ScriptManager.Initialize();
        ScriptBridge.Start();

        var harmony = new Harmony(ModID);
        harmony.PatchAll();
        ScriptCardPlayedPatch.TryApply(harmony);
        Logger.Info("DevMode initialized — Harmony patches applied.");
    }
}
