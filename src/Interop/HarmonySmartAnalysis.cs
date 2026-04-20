using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace DevMode.Interop;

/// <summary>
/// Heuristic Harmony insights: owner totals, busiest methods, multi-owner targets, and risk-style hints.
/// </summary>
public static class HarmonySmartAnalysis {
    public const int MaxBusiestMethods = 28;
    public const int MaxMultiOwnerMethods = 36;
    public const int HeavyPrefixThreshold = 4;
    public const int HeavyPostfixThreshold = 4;
    public const int MaxTranspilerRiskRows = 32;
    public const int MaxSamePriorityRows = 32;
    public const int MaxHeavyHookRows = 24;

    public sealed record MethodHotspot(
        string DeclaringType,
        string MethodSignature,
        int TotalHooks,
        int Prefix,
        int Postfix,
        int Transpiler,
        int Finalizer);

    public sealed record MultiOwnerRow(
        string DeclaringType,
        string MethodSignature,
        int DistinctOwners,
        int TotalHooks,
        IReadOnlyList<string> OwnersSorted);

    public sealed record TranspilerStackRisk(
        string DeclaringType,
        string MethodSignature,
        int TranspilerCount,
        IReadOnlyList<string> PatchLines);

    public sealed record SamePriorityRisk(
        string HookKind,
        int Priority,
        string DeclaringType,
        string MethodSignature,
        IReadOnlyList<string> PatchLines);

    public sealed record HeavyHookRisk(
        string HookKind,
        int Count,
        string DeclaringType,
        string MethodSignature,
        IReadOnlyList<string> PatchLines);

    /// <summary>One patch line on an original method (Harmony owner + patch method).</summary>
    public sealed record PatchLine(
        string HookKind,
        string Owner,
        int Priority,
        string PatchMethodRef);

    public sealed record MethodPatchDetail(
        string MethodSignature,
        IReadOnlyList<PatchLine> Lines);

    /// <summary>All patched methods that share the same declaring type (CLR full name).</summary>
    public sealed record DeclaringTypePatchInfo(
        string DeclaringTypeFullName,
        int TotalPatchOperations,
        int DistinctOwnerCount,
        IReadOnlyList<MethodPatchDetail> Methods);

    public sealed record SmartAnalysisResult(
        int PatchedMethodCount,
        int TotalPatchOperations,
        int DistinctOwnerCount,
        IReadOnlyList<(string Owner, int PatchCount)> PatchesByOwner,
        IReadOnlyList<MethodHotspot> BusiestMethods,
        IReadOnlyList<MultiOwnerRow> MultiOwnerMethods,
        IReadOnlyList<TranspilerStackRisk> TranspilerStackRisks,
        IReadOnlyList<SamePriorityRisk> SamePriorityRisks,
        IReadOnlyList<HeavyHookRisk> HeavyHookRisks,
        IReadOnlyList<DeclaringTypePatchInfo> PatchesByDeclaringType);

    /// <summary>Aggregates patch graph; <paramref name="error"/> set on failure.</summary>
    public static SmartAnalysisResult? Analyze(out string? error) {
        error = null;
        try {
            var methods = Harmony.GetAllPatchedMethods()
                .OrderBy(m => m.DeclaringType?.FullName ?? "")
                .ThenBy(m => m.Name)
                .ToList();

            var byOwner = new Dictionary<string, int>(StringComparer.Ordinal);
            var perMethod = new List<(MethodBase Method, MethodHotspot Stats, HashSet<string> Owners)>();

            var totalOps = 0;

            foreach (var m in methods) {
                var info = Harmony.GetPatchInfo(m);
                if (info == null) continue;

                var owners = new HashSet<string>(StringComparer.Ordinal);

                void CountPatches(IReadOnlyCollection<Patch> patches) {
                    foreach (var p in patches) {
                        var o = p.owner ?? "?";
                        owners.Add(o);
                        byOwner[o] = byOwner.GetValueOrDefault(o) + 1;
                        totalOps++;
                    }
                }

                CountPatches(info.Prefixes);
                CountPatches(info.Postfixes);
                CountPatches(info.Transpilers);
                CountPatches(info.Finalizers);

                var dt = m.DeclaringType?.FullName ?? "Unknown";
                var sig = GetMethodSignature(m);
                var stats = new MethodHotspot(
                    dt,
                    sig,
                    info.Prefixes.Count + info.Postfixes.Count + info.Transpilers.Count + info.Finalizers.Count,
                    info.Prefixes.Count,
                    info.Postfixes.Count,
                    info.Transpilers.Count,
                    info.Finalizers.Count);

                perMethod.Add((m, stats, owners));
            }

            var busiest = perMethod
                .Select(x => x.Stats)
                .OrderByDescending(s => s.TotalHooks)
                .ThenBy(s => s.DeclaringType)
                .Take(MaxBusiestMethods)
                .ToList();

            var multi = perMethod
                .Where(x => x.Owners.Count >= 2)
                .Select(x => new MultiOwnerRow(
                    x.Stats.DeclaringType,
                    x.Stats.MethodSignature,
                    x.Owners.Count,
                    x.Stats.TotalHooks,
                    x.Owners.OrderBy(o => o).ToList()))
                .OrderByDescending(r => r.DistinctOwners)
                .ThenByDescending(r => r.TotalHooks)
                .Take(MaxMultiOwnerMethods)
                .ToList();

            var byOwnerList = byOwner
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .Select(kv => (kv.Key, kv.Value))
                .ToList();

            var transpilerRisks = new List<TranspilerStackRisk>();
            var samePriRisks = new List<SamePriorityRisk>();
            var heavyRisks = new List<HeavyHookRisk>();

            foreach (var m in methods) {
                var info = Harmony.GetPatchInfo(m);
                if (info == null) continue;

                var dt = m.DeclaringType?.FullName ?? "Unknown";
                var sig = GetMethodSignature(m);

                if (info.Transpilers.Count >= 2) {
                    var lines = info.Transpilers
                        .OrderBy(p => p.priority)
                        .ThenBy(p => p.owner)
                        .Select(FormatPatchLine)
                        .ToList();
                    transpilerRisks.Add(new TranspilerStackRisk(dt, sig, info.Transpilers.Count, lines));
                }

                AddSamePriorityRisks(info.Prefixes, "Prefix", dt, sig, samePriRisks);
                AddSamePriorityRisks(info.Postfixes, "Postfix", dt, sig, samePriRisks);
                AddSamePriorityRisks(info.Transpilers, "Transpiler", dt, sig, samePriRisks);
                AddSamePriorityRisks(info.Finalizers, "Finalizer", dt, sig, samePriRisks);

                if (info.Prefixes.Count >= HeavyPrefixThreshold) {
                    var lines = info.Prefixes
                        .OrderBy(p => p.priority)
                        .ThenBy(p => p.owner)
                        .Select(FormatPatchLine)
                        .ToList();
                    heavyRisks.Add(new HeavyHookRisk("Prefix", info.Prefixes.Count, dt, sig, lines));
                }

                if (info.Postfixes.Count >= HeavyPostfixThreshold) {
                    var lines = info.Postfixes
                        .OrderBy(p => p.priority)
                        .ThenBy(p => p.owner)
                        .Select(FormatPatchLine)
                        .ToList();
                    heavyRisks.Add(new HeavyHookRisk("Postfix", info.Postfixes.Count, dt, sig, lines));
                }
            }

            transpilerRisks = transpilerRisks
                .OrderByDescending(r => r.TranspilerCount)
                .ThenBy(r => r.DeclaringType)
                .Take(MaxTranspilerRiskRows)
                .ToList();

            samePriRisks = samePriRisks
                .OrderByDescending(r => r.PatchLines.Count)
                .ThenBy(r => r.HookKind)
                .Take(MaxSamePriorityRows)
                .ToList();

            heavyRisks = heavyRisks
                .OrderByDescending(r => r.Count)
                .ThenBy(r => r.HookKind)
                .Take(MaxHeavyHookRows)
                .ToList();

            var byDeclaringType = BuildPatchesByDeclaringType(methods);

            return new SmartAnalysisResult(
                methods.Count,
                totalOps,
                byOwner.Count,
                byOwnerList,
                busiest,
                multi,
                transpilerRisks,
                samePriRisks,
                heavyRisks,
                byDeclaringType);
        }
        catch (Exception ex) {
            error = ex.Message;
            return null;
        }
    }

    private static void AddSamePriorityRisks(
        IReadOnlyCollection<Patch> patches,
        string hookKind,
        string dt,
        string sig,
        List<SamePriorityRisk> sink) {
        foreach (var g in patches.GroupBy(p => p.priority)) {
            var distinctOwners = g.Select(p => p.owner ?? "?").Distinct().Count();
            if (distinctOwners < 2) continue;

            var lines = g
                .OrderBy(p => p.owner)
                .ThenBy(p => p.PatchMethod.Name)
                .Select(FormatPatchLine)
                .ToList();
            sink.Add(new SamePriorityRisk(hookKind, g.Key, dt, sig, lines));
        }
    }

    private static string FormatPatchLine(Patch p) {
        var o = p.owner ?? "?";
        var pm = p.PatchMethod;
        var cls = pm.DeclaringType?.FullName ?? "?";
        return $"{o}  pri={p.priority}  {cls}.{pm.Name}";
    }

    private static string PatchMethodRef(Patch p) {
        var pm = p.PatchMethod;
        var cls = pm.DeclaringType?.FullName ?? "?";
        return $"{cls}.{pm.Name}";
    }

    private static IReadOnlyList<DeclaringTypePatchInfo> BuildPatchesByDeclaringType(List<MethodBase> methods) {
        var groups = new Dictionary<string, List<MethodBase>>(StringComparer.Ordinal);
        foreach (var m in methods) {
            var key = m.DeclaringType?.FullName ?? "Unknown";
            if (!groups.TryGetValue(key, out var list)) {
                list = new List<MethodBase>();
                groups[key] = list;
            }

            list.Add(m);
        }

        var result = new List<DeclaringTypePatchInfo>();
        foreach (var kv in groups.OrderBy(x => x.Key, StringComparer.Ordinal)) {
            var typeName = kv.Key;
            var inType = kv.Value;
            var owners = new HashSet<string>(StringComparer.Ordinal);
            var methodDetails = new List<MethodPatchDetail>();
            var totalOps = 0;

            foreach (var m in inType.OrderBy(x => x.Name).ThenBy(x => x.MetadataToken)) {
                var info = Harmony.GetPatchInfo(m);
                if (info == null) continue;

                var lines = new List<PatchLine>();

                void AddPatches(IReadOnlyCollection<Patch> patches, string hookKind) {
                    foreach (var p in patches.OrderBy(x => x.priority).ThenBy(x => x.owner)) {
                        var o = p.owner ?? "?";
                        owners.Add(o);
                        totalOps++;
                        lines.Add(new PatchLine(hookKind, o, p.priority, PatchMethodRef(p)));
                    }
                }

                AddPatches(info.Prefixes, "Prefix");
                AddPatches(info.Postfixes, "Postfix");
                AddPatches(info.Transpilers, "Transpiler");
                AddPatches(info.Finalizers, "Finalizer");

                if (lines.Count > 0)
                    methodDetails.Add(new MethodPatchDetail(GetMethodSignature(m), lines));
            }

            result.Add(new DeclaringTypePatchInfo(typeName, totalOps, owners.Count, methodDetails));
        }

        return result;
    }

    private static string GetMethodSignature(MethodBase methodBase) {
        var parameters = methodBase.GetParameters();
        var paramString = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
        return $"{methodBase.Name}({paramString})";
    }

    /// <summary>Plain-text report for UI (section titles localized by caller).</summary>
    public static string FormatReport(
        SmartAnalysisResult r,
        string title,
        string secRisk,
        string riskIntro,
        string riskNone,
        string riskSubTranspiler,
        string riskSubSamePri,
        string riskSubHeavy,
        string riskHintFooter,
        string secOwners,
        string secBusiest,
        string secMulti,
        string noneMulti,
        string disclaimer,
        string colHooks,
        string colPx,
        string colPo,
        string colTr,
        string colFi,
        string colOwners,
        HarmonyPatchRegistry? patchRegistry = null,
        string? secPatchDocs = null,
        string? patchDocsIntro = null,
        string? patchDocsMissing = null) {
        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine($"Patched methods: {r.PatchedMethodCount}  |  Total patch ops: {r.TotalPatchOperations}  |  Distinct owners: {r.DistinctOwnerCount}");
        sb.AppendLine(new string('─', 56));
        sb.AppendLine();

        sb.AppendLine(secRisk);
        sb.AppendLine(riskIntro);
        sb.AppendLine();

        var anyRisk = r.TranspilerStackRisks.Count > 0 || r.SamePriorityRisks.Count > 0 || r.HeavyHookRisks.Count > 0;
        if (!anyRisk)
            sb.AppendLine($"  {riskNone}");
        else {
            if (r.TranspilerStackRisks.Count > 0) {
                sb.AppendLine(riskSubTranspiler);
                foreach (var row in r.TranspilerStackRisks) {
                    sb.AppendLine($"  [{row.DeclaringType}]");
                    sb.AppendLine($"    {row.MethodSignature}");
                    sb.AppendLine($"    ({row.TranspilerCount} transpilers — IL rewrite order can break mods)");
                    foreach (var line in row.PatchLines)
                        sb.AppendLine($"      {line}");
                    sb.AppendLine();
                }
            }

            if (r.SamePriorityRisks.Count > 0) {
                sb.AppendLine(riskSubSamePri);
                foreach (var row in r.SamePriorityRisks) {
                    sb.AppendLine($"  [{row.DeclaringType}]");
                    sb.AppendLine($"    {row.MethodSignature}");
                    sb.AppendLine($"    {row.HookKind}  pri={row.Priority}  (multiple owners — execution order vs other same-priority patches may be unclear)");
                    foreach (var line in row.PatchLines)
                        sb.AppendLine($"      {line}");
                    sb.AppendLine();
                }
            }

            if (r.HeavyHookRisks.Count > 0) {
                sb.AppendLine(riskSubHeavy);
                foreach (var row in r.HeavyHookRisks) {
                    sb.AppendLine($"  [{row.DeclaringType}]");
                    sb.AppendLine($"    {row.MethodSignature}");
                    sb.AppendLine($"    {row.HookKind} count={row.Count}  (deep stack — harder to reason about)");
                    foreach (var line in row.PatchLines)
                        sb.AppendLine($"      {line}");
                    sb.AppendLine();
                }
            }
        }

        sb.AppendLine(riskHintFooter);
        sb.AppendLine();
        sb.AppendLine(new string('─', 56));
        sb.AppendLine();

        sb.AppendLine(secOwners);
        foreach (var (owner, n) in r.PatchesByOwner) {
            if (patchRegistry != null && patchRegistry.Count > 0 && patchRegistry.TryGet(owner, out var doc)) {
                var cat = doc.Category ?? "?";
                sb.AppendLine($"  {n,5}  {owner}  [{cat}]");
            }
            else {
                sb.AppendLine($"  {n,5}  {owner}");
            }
        }

        sb.AppendLine();

        if (patchRegistry != null && patchRegistry.Count > 0 && !string.IsNullOrEmpty(secPatchDocs)) {
            sb.AppendLine(secPatchDocs);
            if (!string.IsNullOrEmpty(patchDocsIntro))
                sb.AppendLine(patchDocsIntro);
            sb.AppendLine();

            var anyDoc = false;
            foreach (var (owner, n) in r.PatchesByOwner) {
                if (!patchRegistry.TryGet(owner, out var doc))
                    continue;
                anyDoc = true;
                var docTitle = doc.DisplayName ?? owner;
                sb.AppendLine($"  {docTitle}  [{doc.Category ?? "?"}]  — {n} patch ops");
                if (!string.IsNullOrEmpty(doc.Summary))
                    sb.AppendLine($"    {doc.Summary}");
                if (!string.IsNullOrEmpty(doc.DocUrl))
                    sb.AppendLine($"    {doc.DocUrl}");
                sb.AppendLine();
            }

            if (!anyDoc && !string.IsNullOrEmpty(patchDocsMissing))
                sb.AppendLine(patchDocsMissing);
            else
                sb.AppendLine();
        }

        sb.AppendLine(secBusiest);
        foreach (var h in r.BusiestMethods) {
            sb.AppendLine($"  [{h.DeclaringType}]");
            sb.AppendLine($"    {h.MethodSignature}");
            sb.AppendLine(
                $"    {colHooks} {h.TotalHooks}  ({colPx} {h.Prefix}, {colPo} {h.Postfix}, {colTr} {h.Transpiler}, {colFi} {h.Finalizer})");
            sb.AppendLine();
        }

        sb.AppendLine(secMulti);
        if (r.MultiOwnerMethods.Count == 0)
            sb.AppendLine($"  ({noneMulti})");
        else {
            foreach (var row in r.MultiOwnerMethods) {
                sb.AppendLine($"  [{row.DeclaringType}]");
                sb.AppendLine($"    {row.MethodSignature}");
                sb.AppendLine($"    {colOwners} {row.DistinctOwners}  |  {colHooks} {row.TotalHooks}");
                sb.AppendLine($"    → {string.Join(", ", row.OwnersSorted)}");
                sb.AppendLine();
            }
        }

        sb.AppendLine(disclaimer);
        return sb.ToString();
    }
}
