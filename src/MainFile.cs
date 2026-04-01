using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

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
        var harmony = new Harmony(ModID);
        harmony.PatchAll();
        Logger.Info("DevMode initialized — Harmony patches applied.");
    }
}
