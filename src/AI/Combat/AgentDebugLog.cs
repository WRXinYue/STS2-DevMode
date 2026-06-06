using System;
using System.IO;
using System.Text.Json;

namespace DevMode.AI.Combat;

/// <summary>NDJSON debug session log for agent analysis (debug mode).</summary>
internal static class AgentDebugLog {
    const string SessionId = "737f2b";

    static readonly string[] Paths = {
        @"C:\Users\WRXinYue\Documents\Project\STS2\DevMode\debug-737f2b.log",
        Path.Combine(GameLogFileHydrator.LogsDirectory, "debug-737f2b.log"),
    };

    public static void Write(string hypothesisId, string location, string message, object? data = null) {
        if (!CombatDecisionLog.VerboseEnabled)
            return;

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
    }
}
