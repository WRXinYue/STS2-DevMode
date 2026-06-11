using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KitLib.Abstractions.Host;
using KitLib.Abstractions.Modding;
using KitLib.Host;
using KitLib.Modding;
using MegaCrit.Sts2.Core.Modding;

namespace KitLib.UI;

internal static class ModPanelCompatProbe {
    internal static string? TryFormatWarningForModId(string modId)
        => FormatWarning(EvaluateDirectory(ModPanelModBanner.TryResolveModDirectory(modId)));

    internal static KitLibCompatResult EvaluateDirectory(string? modDirectory) {
        if (string.IsNullOrWhiteSpace(modDirectory))
            return new KitLibCompatResult();
        var directory = modDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!KitLibCompatTomlReader.TryReadFile(directory, out var document) || document == null)
            return new KitLibCompatResult();
        return KitLibCompatEvaluator.Evaluate(document, BuildRuntime());
    }

    internal static KitLibCompatResult Evaluate(Mod? mod) => EvaluateDirectory(mod?.path);

    internal static string? FormatWarning(KitLibCompatResult result) {
        if (result.IsCompatible)
            return null;
        var orSep = I18N.T("modpanel.userText.or", " or ");
        var parts = new List<string>();
        if (result.Flags.HasFlag(KitLibCompatFlags.GameVersionMismatch)) {
            parts.Add(string.Format(
                I18N.T("modpanel.compat.gameVersion",
                    "This game version is not supported (needs {0})."),
                JoinUserGameVersions(result.GameVersionRanges, orSep)));
        }
        if (result.Flags.HasFlag(KitLibCompatFlags.KitLibVersionMismatch)) {
            parts.Add(string.Format(
                I18N.T("modpanel.compat.kitlibVersion",
                    "Needs KitLib {0}."),
                KitLibCompatUserText.JoinHumanizedRanges(result.KitLibVersionRanges, orSep)));
        }
        if (result.Flags.HasFlag(KitLibCompatFlags.MissingKitLibModule)) {
            parts.Add(string.Format(
                I18N.T("modpanel.compat.missingModule",
                    "Requires KitLib module(s): {0}."),
                string.Join(", ", result.MissingModules)));
        }
        if (result.Flags.HasFlag(KitLibCompatFlags.ModDependencyVersionMismatch)) {
            var deps = result.ModDependencyMismatches
                .Select(KitLibCompatUserText.FormatModDependencyMismatch);
            parts.Add(string.Format(
                I18N.T("modpanel.compat.modDependency",
                    "Incompatible mod dependency: {0}."),
                string.Join("; ", deps)));
        }
        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    internal static string FormatStartupIssueSummary(
        KitLibCompatIssue issue,
        string orSeparator,
        bool richText = false) {
        var result = issue.Result;
        var modLabel = FormatStartupIssueModLabel(issue.DisplayName, richText);
        var currentGame = KitLibCompatUserText.StripVersionPrefix(
            ModPanelModBanner.TryResolveGameBuildVersion());
        if (result.Flags.HasFlag(KitLibCompatFlags.GameVersionMismatch)) {
            var required = JoinUserGameVersions(result.GameVersionRanges, orSeparator);
            return string.Format(
                I18N.T("modpanel.startup.gameVersionOne",
                    "Mod {0} does not match this game version (needs {1}, yours is {2})"),
                modLabel,
                required,
                currentGame);
        }
        if (result.Flags.HasFlag(KitLibCompatFlags.KitLibVersionMismatch)) {
            var required = KitLibCompatUserText.JoinHumanizedRanges(result.KitLibVersionRanges, orSeparator);
            return string.Format(
                I18N.T("modpanel.startup.kitlibVersionOne",
                    "Mod {0} needs KitLib {1}"),
                modLabel,
                required);
        }
        if (result.Flags.HasFlag(KitLibCompatFlags.MissingKitLibModule)) {
            return string.Format(
                I18N.T("modpanel.startup.missingModuleOne",
                    "Mod {0} needs KitLib modules: {1}"),
                modLabel,
                string.Join(", ", result.MissingModules));
        }
        if (result.Flags.HasFlag(KitLibCompatFlags.ModDependencyVersionMismatch)) {
            var deps = result.ModDependencyMismatches
                .Select(KitLibCompatUserText.FormatModDependencyMismatch);
            return string.Format(
                I18N.T("modpanel.startup.modDependencyOne",
                    "Mod {0} has incompatible dependencies ({1})"),
                modLabel,
                string.Join("; ", deps));
        }
        return string.Format(
            I18N.T("modpanel.startup.genericOne",
                "Mod {0} has a version mismatch"),
            modLabel);
    }

    static string FormatStartupIssueModLabel(string displayName, bool richText) =>
        richText ? $"[red]{displayName}[/red]" : displayName;

    internal static string FormatStartupIssueDetailTechnical(KitLibCompatResult result) {
        if (result.Flags.HasFlag(KitLibCompatFlags.GameVersionMismatch))
            return "game " + string.Join(" or ", result.GameVersionRanges);
        if (result.Flags.HasFlag(KitLibCompatFlags.KitLibVersionMismatch))
            return "kitlib " + string.Join(" or ", result.KitLibVersionRanges);
        if (result.Flags.HasFlag(KitLibCompatFlags.MissingKitLibModule))
            return "modules " + string.Join(", ", result.MissingModules);
        if (result.Flags.HasFlag(KitLibCompatFlags.ModDependencyVersionMismatch))
            return "deps " + string.Join("; ", result.ModDependencyMismatches);
        return "mismatch";
    }

    internal static IReadOnlyList<KitLibCompatIssue> CollectStartupIssues()
        => KitLibCompatStartupScan.Collect(
            ModRuntime.Registry.GetAllEntries(),
            id => EvaluateDirectory(ModPanelModBanner.TryResolveModDirectory(id)));

    static string JoinUserGameVersions(IReadOnlyList<string> ranges, string orSeparator)
        => KitLibCompatUserText.JoinHumanizedRanges(ranges, orSeparator);

    static KitLibCompatRuntime BuildRuntime() {
        var modVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? kitLibVersion = null;
        foreach (var loaded in ModManagerLoadedMods.Enumerate()) {
            var id = loaded.manifest?.id;
            var version = loaded.manifest?.version;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version))
                continue;
            modVersions[id] = version;
            if (string.Equals(id, KitLibModuleIds.Core, StringComparison.OrdinalIgnoreCase))
                kitLibVersion = version;
        }
        return new KitLibCompatRuntime {
            GameVersion = ModPanelModBanner.TryResolveGameBuildVersion(),
            KitLibVersion = kitLibVersion,
            IsModuleLoaded = KitLibHost.IsModuleLoaded,
            ResolveModVersion = id => modVersions.TryGetValue(id, out var version) ? version : null,
        };
    }
}
