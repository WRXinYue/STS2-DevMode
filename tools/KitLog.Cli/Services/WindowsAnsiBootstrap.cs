using System.Runtime.InteropServices;
using Spectre.Console;

namespace KitLog.Cli.Services;

internal static class WindowsAnsiBootstrap {
    const int StdOutputHandle = -11;
    const uint EnableVirtualTerminalProcessing = 0x0004;

    public static void EnableIfNeeded() {
        if (!OperatingSystem.IsWindows())
            return;

        try {
            var handle = GetStdHandle(StdOutputHandle);
            if (GetConsoleMode(handle, out var mode))
                SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
        }
        catch {
            // Best-effort; Spectre may still render without VT on older hosts.
        }

        try {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings {
                Ansi = AnsiSupport.Yes,
            });
        }
        catch {
            // Keep default Spectre console if custom settings fail.
        }
    }

    [DllImport("kernel32.dll")]
    static extern nint GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);
}
