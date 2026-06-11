using KitLog.Cli.Services;

namespace KitLog.Cli.Rendering;

internal static class AnsiCodes {
    public const string Reset = "\x1b[0m";
    public const string Grey = "\x1b[90m";
    public const string Red = "\x1b[31m";
    public const string Yellow = "\x1b[33m";
    public const string Cyan = "\x1b[36m";
    public const string White = "\x1b[37m";

    public static string ForLevel(ParsedLogLevel level) => level switch {
        ParsedLogLevel.Error => Red,
        ParsedLogLevel.Warn => Yellow,
        ParsedLogLevel.Debug or ParsedLogLevel.VeryDebug => Grey,
        ParsedLogLevel.Load => Cyan,
        _ => White,
    };

    public static string TrueColorFg(string hexRgb) {
        if (hexRgb.Length != 6)
            return White;

        var r = Convert.ToInt32(hexRgb[..2], 16);
        var g = Convert.ToInt32(hexRgb[2..4], 16);
        var b = Convert.ToInt32(hexRgb[4..6], 16);
        return $"\x1b[38;2;{r};{g};{b}m";
    }

    public static void WriteStatusLine(string message) =>
        Console.WriteLine($"{Grey}{message}{Reset}");
}
