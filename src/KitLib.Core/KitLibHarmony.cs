using System.Reflection;
using HarmonyLib;

namespace KitLib;

/// <summary>Applies Harmony patches from a single module assembly once per process.</summary>
public static class KitLibHarmony {
    static readonly HashSet<string> Applied = new(StringComparer.OrdinalIgnoreCase);

    public static void Apply(Assembly moduleAssembly, string harmonyId) {
        ArgumentNullException.ThrowIfNull(moduleAssembly);
        if (string.IsNullOrWhiteSpace(harmonyId))
            throw new ArgumentException("Harmony id is required.", nameof(harmonyId));
        if (!Applied.Add(harmonyId))
            return;

        var harmony = new Harmony(harmonyId);
        harmony.PatchAll(moduleAssembly);
        MainFile.Logger.Info($"KitLib Harmony patches applied: {harmonyId}");
    }
}
