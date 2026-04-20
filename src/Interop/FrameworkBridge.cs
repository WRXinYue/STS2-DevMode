using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STS2RitsuLib;
using STS2RitsuLib.Settings;

namespace DevMode.Interop;

/// <summary>
/// Read-only RitsuLib diagnostics for DevMode (no config sync).
/// </summary>
public static class FrameworkBridge {
    private const int MaxPageInventoryLines = 48;
    private static IDisposable? _ritsuLifecycleSub;

    public readonly record struct FrameworkBridgeSnapshot(
        // RitsuLib — manifest / identity
        string RitsuDisplayName,
        string RitsuManifestVersion,
        string RitsuLibFrameworkModId,
        string RitsuLibAssemblyVersion,
        string RitsuSettingsRootKey,
        string RitsuSettingsFileName,
        // RitsuLib — runtime
        bool RitsuLibInitialized,
        bool RitsuLibActive,
        bool RitsuLibHasModSettingsPages,
        int RitsuLibModSettingsPageCount,
        int RitsuLibDistinctOwningModCount,
        int RitsuLibTotalSectionCount,
        string RitsuLibPagesInventoryLines,
        // Harmony (process-wide)
        HarmonyPatchSummary.Stats HarmonyStats);

    /// <summary>Subscribe once to RitsuLib lifecycle (replayable) and log a one-line snapshot.</summary>
    public static void Initialize() {
        try {
            _ritsuLifecycleSub = RitsuLibFramework.SubscribeLifecycle<FrameworkInitializedEvent>(evt => {
                MainFile.Logger.Info(
                    $"[DevMode Bridge] RitsuLib event: modId={evt.FrameworkModId}, active={evt.IsActive}");
            });
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[DevMode Bridge] RitsuLib lifecycle subscribe failed: {ex.Message}");
        }

        try {
            var s = CaptureSnapshot();
            MainFile.Logger.Info(
                $"[DevMode Bridge] Ritsu init={s.RitsuLibInitialized} active={s.RitsuLibActive} " +
                $"pages={s.RitsuLibModSettingsPageCount} mods={s.RitsuLibDistinctOwningModCount} | " +
                $"harmony methods={s.HarmonyStats.PatchedMethodCount}");
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[DevMode Bridge] snapshot failed: {ex.Message}");
        }
    }

    public static FrameworkBridgeSnapshot CaptureSnapshot() {
        bool ritsuInit = RitsuLibFramework.IsInitialized;
        bool ritsuActive = RitsuLibFramework.IsActive;
        bool ritsuHasPages = RitsuLibFramework.HasRegisteredModSettings;
        var pages = RitsuLibFramework.GetRegisteredModSettings()
            .OrderBy(p => p.ModId)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .ToList();

        int pageCount = pages.Count;
        int distinctMods = pages.Select(p => p.ModId).Distinct().Count();
        int totalSections = pages.Sum(p => p.Sections.Count);
        string inventory = BuildRitsuPageInventory(pages);

        string ritsuVer = typeof(RitsuLibFramework).Assembly.GetName().Version?.ToString() ?? "?";

        var harmony = HarmonyPatchSummary.Aggregate();

        return new FrameworkBridgeSnapshot(
            Const.Name,
            Const.Version,
            Const.ModId,
            ritsuVer,
            Const.SettingsKey,
            Const.SettingsFileName,
            ritsuInit,
            ritsuActive,
            ritsuHasPages,
            pageCount,
            distinctMods,
            totalSections,
            inventory,
            harmony);
    }

    private static string BuildRitsuPageInventory(IReadOnlyList<ModSettingsPage> pages) {
        if (pages.Count == 0)
            return "—";

        var sb = new StringBuilder();
        var n = Math.Min(pages.Count, MaxPageInventoryLines);
        for (var i = 0; i < n; i++) {
            var p = pages[i];
            var parent = string.IsNullOrEmpty(p.ParentPageId) ? "—" : p.ParentPageId;
            string titleHint = "";
            try {
                titleHint = p.Title?.Resolve() ?? "";
            }
            catch {
                titleHint = "";
            }

            if (titleHint.Length > 42)
                titleHint = titleHint[..39] + "…";

            // owning_mod | page_id | sections | sort | parent | title (trimmed)
            sb.Append(p.ModId);
            sb.Append(" | ");
            sb.Append(p.Id);
            sb.Append(" | ");
            sb.Append(p.Sections.Count);
            sb.Append(" | ");
            sb.Append(p.SortOrder);
            sb.Append(" | ");
            sb.Append(parent);
            if (!string.IsNullOrEmpty(titleHint)) {
                sb.Append(" | ");
                sb.Append(titleHint.Replace('\n', ' ').Replace('\r', ' '));
            }

            sb.Append('\n');
        }

        if (pages.Count > MaxPageInventoryLines)
            sb.Append($"(+{pages.Count - MaxPageInventoryLines} more)");

        return sb.ToString().TrimEnd();
    }
}
