using System.Runtime.InteropServices;

namespace KitLog.Cli.Services;

internal static class WindowsAnsiBootstrap {
    const int StdOutputHandle = -11;
    const int StdErrorHandle = -12;
    const uint EnableVirtualTerminalProcessing = 0x0004;

    public static void EnableIfNeeded() {
        if (!OperatingSystem.IsWindows())
            return;

        EnableVt(GetStdHandle(StdOutputHandle));
        EnableVt(GetStdHandle(StdErrorHandle));

        try {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch {
            // Best-effort only.
        }
    }

    static void EnableVt(nint handle) {
        try {
            if (GetConsoleMode(handle, out var mode))
                SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
        }
        catch {
            // Best-effort only.
        }
    }

    [DllImport("kernel32.dll")]
    static extern nint GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);
}
