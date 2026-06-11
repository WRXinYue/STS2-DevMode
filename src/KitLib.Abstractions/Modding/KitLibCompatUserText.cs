using System.Text.RegularExpressions;

namespace KitLib.Abstractions.Modding;

/// <summary>Turns semver range strings from <c>kitlib.compat.toml</c> into player-facing version text.</summary>
public static class KitLibCompatUserText {
    static readonly Regex ExactRange = new(@"^=\s*(.+)$", RegexOptions.CultureInvariant);

    public static string HumanizeVersionRange(string raw) {
        var text = raw.Trim();
        if (text.Length == 0)
            return text;

        var exact = ExactRange.Match(text);
        if (exact.Success)
            return StripVersionPrefix(exact.Groups[1].Value.Trim());

        if (text.StartsWith('^'))
            return StripVersionPrefix(text[1..].Trim()) + "+";

        if (text.StartsWith(">=", StringComparison.Ordinal)) {
            var rest = text[2..].Trim();
            var lt = rest.IndexOf('<');
            if (lt > 0)
                rest = rest[..lt].Trim();
            return StripVersionPrefix(rest) + "+";
        }

        return StripVersionPrefix(text);
    }

    public static string JoinHumanizedRanges(IReadOnlyList<string> ranges, string orSeparator) {
        if (ranges.Count == 0)
            return string.Empty;
        var parts = new List<string>(ranges.Count);
        foreach (var range in ranges) {
            if (string.IsNullOrWhiteSpace(range))
                continue;
            var human = HumanizeVersionRange(range);
            if (human.Length > 0)
                parts.Add(human);
        }
        return parts.Count == 0 ? string.Empty : string.Join(orSeparator, parts);
    }

    public static string StripVersionPrefix(string? raw) {
        if (string.IsNullOrWhiteSpace(raw))
            return "?";
        var text = raw.Trim();
        if (text.StartsWith('v') || text.StartsWith('V'))
            text = text[1..].TrimStart();
        return text;
    }

    public static string FormatModDependencyMismatch(string raw) {
        var eq = raw.IndexOf('=');
        if (eq < 0)
            return raw;
        var modId = raw[..eq].Trim();
        var range = raw[(eq + 1)..].Trim();
        return modId + " " + HumanizeVersionRange(range);
    }
}
