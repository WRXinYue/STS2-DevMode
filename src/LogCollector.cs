using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;

namespace DevMode;

/// <summary>
/// Captures log entries emitted by the game's logging system into an in-memory ring buffer.
/// Subscribe via <see cref="Log.LogCallback"/> so every Logger instance is covered.
/// </summary>
internal static class LogCollector {
    public const int MaxEntries = 2000;

    public readonly record struct Entry(LogLevel Level, string Text, DateTime Time);

    private static readonly List<Entry> _entries = new();
    private static readonly object _lock = new();
    private static volatile bool _dirty;

    /// <summary>True when new entries have arrived since the last <see cref="MarkClean"/> call.</summary>
    public static bool IsDirty => _dirty;

    public static void Initialize() {
        Log.LogCallback += OnLogReceived;
    }

    private static void OnLogReceived(LogLevel level, string text, int _) {
        lock (_lock) {
            _entries.Add(new Entry(level, text, DateTime.Now));
            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);
        }
        _dirty = true;
    }

    /// <summary>Returns a snapshot of current entries (thread-safe copy).</summary>
    public static List<Entry> GetSnapshot() {
        lock (_lock)
            return new List<Entry>(_entries);
    }

    public static void Clear() {
        lock (_lock)
            _entries.Clear();
        _dirty = true;
    }

    public static void MarkClean() => _dirty = false;
}
