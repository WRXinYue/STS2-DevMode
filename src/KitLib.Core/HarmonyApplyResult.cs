namespace KitLib;

using System;
using System.Collections.Generic;

public sealed record HarmonySkippedPatch(string PatchTypeFullName, string Reason);

/// <summary>Outcome of the most recent <see cref="KitLibHarmony.Apply"/> for a module.</summary>
public sealed class HarmonyApplyResult {
    public required string HarmonyId { get; init; }
    public int AppliedCount { get; init; }
    public int SkippedCount { get; init; }
    public IReadOnlyList<HarmonySkippedPatch> Skipped { get; init; } = [];
    public IReadOnlyList<string> AppliedPatchTypes { get; init; } = [];

    public bool WasPatchTypeSkipped(string patchTypeFullName) {
        foreach (var entry in Skipped) {
            if (string.Equals(entry.PatchTypeFullName, patchTypeFullName, StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
