namespace KitLog.Cli.Rendering;

internal static class LogModColors {
    static readonly string[] Palette =
    {
        "7399f2",
        "85cc85",
        "f2a861",
        "d185eb",
        "7adcdc",
        "eb7a8c",
        "d1c26b",
        "9e99f2",
    };

    internal static string ForId(string modId) {
        unchecked {
            int hash = 17;
            foreach (char c in modId)
                hash = hash * 31 + c;
            return Palette[Math.Abs(hash) % Palette.Length];
        }
    }
}
