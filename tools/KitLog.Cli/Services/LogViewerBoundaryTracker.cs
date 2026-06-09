using System.Text;

namespace KitLog.Cli.Services;

/// <summary>
/// Mirrors the in-game viewer: lines before the last session boundary are shown without viewer filters.
/// </summary>
internal sealed class LogViewerBoundaryTracker {
    readonly bool _useBoundarySplit;
    bool _applyViewerFilters;

    public LogViewerBoundaryTracker(string logPath, long tailStartOffset) {
        if (IsInstanceSessionLog(logPath)) {
            _useBoundarySplit = false;
            _applyViewerFilters = true;
            return;
        }

        _useBoundarySplit = true;
        var lastBoundaryEnd = FindLastBoundaryEndOffset(logPath);
        if (lastBoundaryEnd < 0) {
            _applyViewerFilters = false;
            return;
        }

        _applyViewerFilters = tailStartOffset >= lastBoundaryEnd;
    }

    public bool ApplyViewerFilters => _applyViewerFilters;

    public void OnLine(string line) {
        if (!_useBoundarySplit)
            return;

        if (LogViewerFilterMatcher.IsSessionBoundary(line))
            _applyViewerFilters = true;
    }

    static long FindLastBoundaryEndOffset(string logPath) {
        try {
            using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            long lastEnd = -1;
            string? line;
            while ((line = reader.ReadLine()) != null) {
                if (!LogViewerFilterMatcher.IsSessionBoundary(line))
                    continue;

                lastEnd = fs.Position;
            }

            return lastEnd;
        }
        catch {
            return -1;
        }
    }

    static bool IsInstanceSessionLog(string path) {
        if (string.IsNullOrEmpty(path))
            return false;

        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/mod_data/KitLib/instances/", StringComparison.OrdinalIgnoreCase)
               && normalized.EndsWith("/session.log", StringComparison.OrdinalIgnoreCase);
    }
}
