using System.Runtime.CompilerServices;

namespace KitLib.Logging;

/// <summary>
/// Reusable content-mod logger: local level gate, KitLib pipeline when bound, formatted fallback otherwise.
/// </summary>
public sealed class ModLog {
    readonly string _modId;
    readonly Func<KitLogLevel> _minimumLevel;
    readonly Action<KitLogLevel, string> _fallback;
    readonly bool _includeCallerOnDebug;

    public ModLog(
        string modId,
        Func<KitLogLevel> minimumLevel,
        Action<KitLogLevel, string> fallback,
        bool includeCallerOnDebug = true) {
        if (string.IsNullOrWhiteSpace(modId))
            throw new ArgumentException("Mod id is required.", nameof(modId));
        ArgumentNullException.ThrowIfNull(minimumLevel);
        ArgumentNullException.ThrowIfNull(fallback);

        _modId = modId.Trim();
        _minimumLevel = minimumLevel;
        _fallback = fallback;
        _includeCallerOnDebug = includeCallerOnDebug;
    }

    public ModLogScope Scope(string scope) => new(this, scope);

    public void Debug(
        string message,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) =>
        Write(KitLogLevel.Debug, null, message, member, file, line);

    public void Debug(string scope, string message) =>
        Write(KitLogLevel.Debug, scope, message, member: "", file: "", line: 0);

    public void Info(
        string message,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) =>
        Write(KitLogLevel.Info, null, message, member, file, line);

    public void Info(string scope, string message) =>
        Write(KitLogLevel.Info, scope, message, member: "", file: "", line: 0);

    public void Warn(
        string message,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) =>
        Write(KitLogLevel.Warn, null, message, member, file, line);

    public void Warn(string scope, string message) =>
        Write(KitLogLevel.Warn, scope, message, member: "", file: "", line: 0);

    public void Error(
        string message,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) =>
        Write(KitLogLevel.Error, null, message, member, file, line);

    public void Error(string scope, string message) =>
        Write(KitLogLevel.Error, scope, message, member: "", file: "", line: 0);

    internal void Write(
        KitLogLevel level,
        string? scope,
        string message,
        string member,
        string file,
        int line) {
        if (!ShouldEmit(level, _minimumLevel()))
            return;

        var body = level == KitLogLevel.Debug && _includeCallerOnDebug
            ? KitLibLogFormat.FormatWithCaller(message, member, file, line)
            : message;

        if (KitLibLog.IsAvailable) {
            KitLibLog.Write(level, scope, body);
            return;
        }

        _fallback(level, KitLibLogFormat.FormatLine(_modId, scope, body));
    }

    internal static bool ShouldEmit(KitLogLevel level, KitLogLevel minimum) =>
        level == KitLogLevel.Error || level >= minimum;
}

/// <summary>Fixed scope handle for repeated <c>[mod][scope]</c> logging via <see cref="ModLog"/>.</summary>
public readonly struct ModLogScope {
    readonly ModLog _log;
    readonly string _scope;

    internal ModLogScope(ModLog log, string scope) {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope is required.", nameof(scope));
        _log = log;
        _scope = scope.Trim();
    }

    public void Debug(
        string message,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) =>
        _log.Write(KitLogLevel.Debug, _scope, message, member, file, line);

    public void Info(
        string message,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) =>
        _log.Write(KitLogLevel.Info, _scope, message, member, file, line);

    public void Warn(
        string message,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) =>
        _log.Write(KitLogLevel.Warn, _scope, message, member, file, line);

    public void Error(
        string message,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) =>
        _log.Write(KitLogLevel.Error, _scope, message, member, file, line);
}
