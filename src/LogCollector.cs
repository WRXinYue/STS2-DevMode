using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;

namespace DevMode;

/// <summary>
/// Captures log entries emitted by the game's logging system into an in-memory ring buffer.
/// Subscribe via <see cref="Log.LogCallback"/> so every Logger instance is covered.
/// Opening the log viewer also hydrates from the on-disk session log under <c>user://logs/</c>.
/// </summary>
internal static class LogCollector {
    public const int MaxLiveEntries = 2000;
    public const int MaxMergedEntries = 4000;

    /// <summary>Injected at init; splits pre-DevMode file history from live callback capture.</summary>
    public const string SessionBoundaryMarker = "── DevMode log capture started ──";

    public readonly record struct Entry(
        LogLevel Level,
        string Text,
        DateTime Time,
        bool IsFromFile = false,
        bool HasWallClockTime = true);

    private static readonly List<Entry> _liveEntries = new();
    private static List<Entry> _fileEntries = [];
    private static readonly object _lock = new();
    private static volatile bool _dirty;

    /// <summary>True when new entries have arrived since the last <see cref="MarkClean"/> call.</summary>
    public static bool IsDirty => _dirty;

    public static bool IsSessionBoundary(in Entry entry)
        => entry.Text.Contains(SessionBoundaryMarker, StringComparison.Ordinal);

    public static void Initialize() {
        Log.LogCallback += OnLogReceived;
        MainFile.Logger.Info(SessionBoundaryMarker);
    }

    /// <summary>
    /// Re-reads the current session log file and merges it with live callback entries on the next snapshot.
    /// </summary>
    public static void RefreshFileSnapshot() {
        var parsed = GameLogFileHydrator.ReadSessionLogEntries();
        lock (_lock) {
            _fileEntries = parsed;
            _dirty = true;
        }
    }

    private static void OnLogReceived(LogLevel level, string text, int _) {
        lock (_lock) {
            _liveEntries.Add(new Entry(level, text, DateTime.Now));
            if (_liveEntries.Count > MaxLiveEntries)
                _liveEntries.RemoveAt(0);
        }
        _dirty = true;
    }

    /// <summary>Returns a merged snapshot of file-hydrated and live entries (thread-safe copy).</summary>
    public static List<Entry> GetSnapshot() {
        lock (_lock)
            return MergeEntries(_fileEntries, _liveEntries);
    }

    public static void Clear() {
        lock (_lock) {
            _liveEntries.Clear();
            _fileEntries.Clear();
        }
        _dirty = true;
    }

    public static void MarkClean() => _dirty = false;

    private static List<Entry> MergeEntries(List<Entry> fileEntries, List<Entry> liveEntries) {
        int fileBoundaryIndex = FindLastBoundaryIndex(fileEntries);

        var merged = new List<Entry>(fileEntries.Count + liveEntries.Count);

        if (fileBoundaryIndex < 0) {
            AppendUnique(merged, fileEntries);
            AppendUnique(merged, liveEntries, skipFingerprints: merged);
        }
        else {
            for (int i = 0; i < fileBoundaryIndex; i++)
                merged.Add(fileEntries[i]);

            if (liveEntries.Count > 0) {
                AppendUnique(merged, liveEntries);
            }
            else {
                for (int i = fileBoundaryIndex; i < fileEntries.Count; i++)
                    merged.Add(fileEntries[i]);
            }
        }

        TrimToMaxEntries(merged);

        return merged;
    }

    /// <summary>
    /// Caps total size while preserving the full pre-boundary file history; only trims the live tail.
    /// </summary>
    private static void TrimToMaxEntries(List<Entry> merged) {
        if (merged.Count <= MaxMergedEntries)
            return;

        int boundaryIdx = FindLastBoundaryIndex(merged);
        if (boundaryIdx < 0) {
            merged.RemoveRange(0, merged.Count - MaxMergedEntries);
            return;
        }

        int preserveThroughBoundary = boundaryIdx + 1;
        int maxLiveEntries = MaxMergedEntries - preserveThroughBoundary;
        if (maxLiveEntries < 0)
            maxLiveEntries = 0;

        int liveStart = preserveThroughBoundary;
        int liveCount = merged.Count - liveStart;
        if (liveCount > maxLiveEntries)
            merged.RemoveRange(liveStart, liveCount - maxLiveEntries);
    }

    private static int FindLastBoundaryIndex(List<Entry> entries) {
        for (int i = entries.Count - 1; i >= 0; i--) {
            if (IsSessionBoundary(entries[i]))
                return i;
        }

        return -1;
    }

    private static void AppendUnique(List<Entry> merged, List<Entry> entries, List<Entry>? skipFingerprints = null) {
        var seen = skipFingerprints != null
            ? BuildFingerprintSet(skipFingerprints)
            : BuildFingerprintSet(merged);

        foreach (var entry in entries) {
            if (seen.Add(Fingerprint(entry)))
                merged.Add(entry);
        }
    }

    private static HashSet<string> BuildFingerprintSet(List<Entry> entries) {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
            seen.Add(Fingerprint(entry));
        return seen;
    }

    private static string Fingerprint(Entry entry)
        => $"{(int)entry.Level}|{entry.Text.Trim()}";
}
