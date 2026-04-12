using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace DevMode.Patches;

/// <summary>
/// Silently skips JSON files that lack an "id" field when the game scans for mod manifests,
/// suppressing the Error log spam caused by data files (settings, snapshots, scripts, etc.)
/// being mistakenly treated as mod manifests.
/// </summary>
[HarmonyPatch(typeof(ModManager), "ReadModManifest")]
internal static class ModManifestScanPatch {
    private static readonly AccessTools.FieldRef<IModManagerFileIo?> FileIoRef =
        AccessTools.StaticFieldRefAccess<IModManagerFileIo?>(
            AccessTools.Field(typeof(ModManager), "_fileIo"));

    private static bool Prefix(string filename, ref Mod? __result) {
        var fileIo = FileIoRef();
        if (fileIo == null)
            return true;

        try {
            using var stream = fileIo.OpenStream(filename, FileAccess.ModeFlags.Read);
            using var doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("id", out _)) {
                __result = null;
                return false;
            }
        }
        catch {
            // 解析失败交给原方法处理
            return true;
        }

        return true;
    }
}
