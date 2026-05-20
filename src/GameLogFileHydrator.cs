using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace DevMode;

/// <summary>
/// Reads and parses the current session log file under <c>user://logs/</c> for Log Viewer hydration.
/// </summary>
internal static class GameLogFileHydrator {
    private const int MaxReadBytes = 2 * 1024 * 1024;

    private static readonly Regex TimestampLine = new(
        @"^(?<time>\d{2}:\d{2}:\d{2})\s+(?<level>INFO|WARN|WARNING|ERROR|DEBUG|LOAD|VERYDEBUG|VDB|DBG)\s+(?<text>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BracketLevelLine = new(
        @"^\[(?<level>INFO|WARN|WARNING|ERROR|DEBUG|LOAD|VERYDEBUG|VDB|DBG)\]\s+(?<text>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal static string LogsDirectory => Path.Combine(OS.GetUserDataDir(), "logs");

    internal static List<LogCollector.Entry> ReadSessionLogEntries() {
        var path = FindSessionLogPath();
        if (path == null)
            return [];

        try {
            var referenceDate = File.GetLastWriteTime(path).Date;
            return ParseFile(path, referenceDate);
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[LogViewer] Failed to read session log file: {ex.Message}");
            return [];
        }
    }

    internal static string? FindSessionLogPath() {
        try {
            var logsDir = LogsDirectory;
            if (!Directory.Exists(logsDir))
                return null;

            string? bestPath = null;
            DateTime bestTime = DateTime.MinValue;

            foreach (var path in Directory.EnumerateFiles(logsDir)) {
                var writeTime = File.GetLastWriteTime(path);
                if (writeTime < bestTime)
                    continue;

                bestTime = writeTime;
                bestPath = path;
            }

            return bestPath;
        }
        catch {
            return null;
        }
    }

    private static List<LogCollector.Entry> ParseFile(string path, DateTime referenceDate) {
        var lines = ReadTailLines(path);
        var entries = new List<LogCollector.Entry>(lines.Count);

        foreach (var line in lines) {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!TryParseLine(line, referenceDate, out var entry))
                continue;

            entries.Add(entry);
        }

        return entries;
    }

    private static List<string> ReadTailLines(string path) {
        using var fs = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
        var truncated = fs.Length > MaxReadBytes;
        if (truncated)
            fs.Seek(-MaxReadBytes, SeekOrigin.End);

        using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        if (truncated)
            reader.ReadLine();

        var content = reader.ReadToEnd();
        if (string.IsNullOrEmpty(content))
            return [];

        var lines = new List<string>();
        foreach (var line in content.Split('\n')) {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.Length > 0)
                lines.Add(trimmed);
        }

        return lines;
    }

    private static bool TryParseLine(string line, DateTime referenceDate, out LogCollector.Entry entry) {
        entry = default;

        Match match = TimestampLine.Match(line);
        DateTime time;
        string levelToken;
        bool hasWallClockTime;

        if (match.Success) {
            if (!TimeSpan.TryParse(match.Groups["time"].Value, out var tod))
                return false;

            time = referenceDate + tod;
            if (time > DateTime.Now.AddMinutes(1))
                time = time.AddDays(-1);

            levelToken = match.Groups["level"].Value;
            hasWallClockTime = true;
        }
        else {
            match = BracketLevelLine.Match(line);
            if (match.Success) {
                time = default;
                levelToken = match.Groups["level"].Value;
                hasWallClockTime = false;
            }
            else {
                entry = new LogCollector.Entry(
                    LogLevel.Info,
                    line,
                    default,
                    IsFromFile: true,
                    HasWallClockTime: false);
                return true;
            }
        }

        if (!TryParseLevel(levelToken, out var level))
            return false;

        // Text is the verbatim disk line; Level/Time are parsed only for filtering and live merge.
        entry = new LogCollector.Entry(level, line, time, IsFromFile: true, HasWallClockTime: hasWallClockTime);
        return true;
    }

    private static bool TryParseLevel(string token, out LogLevel level) {
        level = LogLevel.Info;
        switch (token.ToUpperInvariant()) {
            case "ERROR":
                level = LogLevel.Error;
                return true;
            case "WARN":
            case "WARNING":
                level = LogLevel.Warn;
                return true;
            case "INFO":
                level = LogLevel.Info;
                return true;
            case "LOAD":
                level = LogLevel.Load;
                return true;
            case "DEBUG":
            case "DBG":
                level = LogLevel.Debug;
                return true;
            case "VERYDEBUG":
            case "VDB":
                level = LogLevel.VeryDebug;
                return true;
            default:
                return false;
        }
    }
}
