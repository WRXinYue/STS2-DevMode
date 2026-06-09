using KitLog.Cli.Services;
using Spectre.Console;

namespace KitLog.Cli.Rendering;

internal static class LogLineRenderer {
    public static void WriteLine(ParsedLogLine line, bool color, LogViewerFilterState? viewerState = null) {
        if (!color) {
            Console.WriteLine(line.DisplayText);
            return;
        }

        try {
            AnsiConsole.MarkupLine(BuildMarkup(line, viewerState));
        }
        catch (Exception) {
            Console.WriteLine(line.DisplayText);
        }
    }

    static string BuildMarkup(ParsedLogLine line, LogViewerFilterState? viewerState) {
        var levelColor = LevelColor(line.Level);
        var body = BuildBodyMarkup(line.RawLine, viewerState);
        return $"[{levelColor}]{body}[/]";
    }

    static string BuildBodyMarkup(string rawLine, LogViewerFilterState? viewerState) {
        if (viewerState == null
            || viewerState.LoadedModIds.Count == 0
            || !LogViewerFilterMatcher.TryFindModTagSpan(
                rawLine,
                viewerState.LoadedModIds,
                viewerState.ModIdAliases,
                out int tagStart,
                out int tagEnd,
                out var modId)) {
            return Markup.Escape(rawLine);
        }

        var modHex = LogModColors.ForId(modId);
        var prefix = Markup.Escape(rawLine[..tagStart]);
        var tag = Markup.Escape(rawLine[tagStart..tagEnd]);
        var suffix = Markup.Escape(rawLine[tagEnd..]);

        if (tagStart == 0)
            return $"[#{modHex}]{tag}[/]{suffix}";

        if (tagEnd >= rawLine.Length)
            return $"{prefix}[#{modHex}]{tag}[/]";

        return $"{prefix}[#{modHex}]{tag}[/]{suffix}";
    }

    static string LevelColor(ParsedLogLevel level) => level switch {
        ParsedLogLevel.Error => "red",
        ParsedLogLevel.Warn => "yellow",
        ParsedLogLevel.Debug or ParsedLogLevel.VeryDebug => "grey",
        ParsedLogLevel.Load => "cyan",
        _ => "white",
    };
}
