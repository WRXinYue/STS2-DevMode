using System;
using System.IO;
using System.Text.Json;

namespace DevMode.AI;

/// <summary>NDJSON session log for debug mode (session b912ce).</summary>
internal static class DbgSessionLog {
    const string SessionId = "b912ce";

    static readonly string[] Paths = {
        @"C:\Users\WRXinYue\Documents\Project\STS2\DevMode\debug-b912ce.log",
        Path.Combine(GameLogFileHydrator.LogsDirectory, "debug-b912ce.log"),
    };

    public static void Write(string hypothesisId, string location, string message, object? data = null) {
        // #region agent log
        try {
            var payload = JsonSerializer.Serialize(new {
                sessionId = SessionId,
                hypothesisId,
                location,
                message,
                data,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });
            foreach (var path in Paths) {
                try {
                    File.AppendAllText(path, payload + Environment.NewLine);
                }
                catch {
                    // try next path
                }
            }
        }
        catch {
            // ignore logging failures
        }
        // #endregion
    }
}
