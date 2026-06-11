namespace KitLib.Abstractions.Modding;

/// <summary>Finds enabled but not loaded mods whose <c>kitlib.compat.toml</c> fails semver checks.</summary>
public static class KitLibCompatStartupScan {
    public static IReadOnlyList<KitLibCompatIssue> Collect(
        IReadOnlyList<KitLibModEntry> entries,
        Func<string, KitLibCompatResult> evaluateByModId) {
        var issues = new List<KitLibCompatIssue>();
        foreach (var entry in entries) {
            if (entry.IsLoaded || !entry.IsEnabledInSettings)
                continue;
            var result = evaluateByModId(entry.Id);
            if (!result.HasSidecar || result.IsCompatible)
                continue;
            issues.Add(new KitLibCompatIssue(entry.Id, entry.DisplayName, result.Flags, result));
        }
        issues.Sort(static (a, b) =>
            string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        return issues;
    }

    public static string JoinDisplayNames(
        IReadOnlyList<KitLibCompatIssue> issues,
        string separator,
        int maxNames = 3) {
        if (issues.Count == 0)
            return string.Empty;
        var take = Math.Min(maxNames, issues.Count);
        var names = new string[take];
        for (var i = 0; i < take; i++)
            names[i] = issues[i].DisplayName;
        var joined = string.Join(separator, names);
        return issues.Count > maxNames ? joined + "…" : joined;
    }
}
