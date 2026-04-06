using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DevMode.Icons;

/// <summary>
/// Extracts SVG path "d" attribute strings from an SVG body fragment
/// and parses them into a list of <see cref="PathSegment"/> sequences.
/// </summary>
public static partial class SvgPathExtractor
{
    /// <summary>Extract all &lt;path d="..."&gt; values from an SVG body fragment.</summary>
    public static List<List<PathSegment>> ExtractPaths(string svgBody)
    {
        var result = new List<List<PathSegment>>();
        foreach (Match m in DAttrRegex().Matches(svgBody))
        {
            var d = m.Groups[1].Value;
            var segs = ParsePath(d);
            if (segs.Count > 0) result.Add(segs);
        }
        return result;
    }

    [GeneratedRegex(@"\bd=""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex DAttrRegex();
}
