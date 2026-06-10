using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace KitLib;

/// <summary>Applies Harmony patches from a single module assembly once per process.</summary>
public static class KitLibHarmony {
    static readonly HashSet<string> Applied = new(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string, Harmony> Instances = new(StringComparer.OrdinalIgnoreCase);

    public static Harmony GetOrCreate(string harmonyId) {
        if (string.IsNullOrWhiteSpace(harmonyId))
            throw new ArgumentException("Harmony id is required.", nameof(harmonyId));
        if (!Instances.TryGetValue(harmonyId, out var harmony)) {
            harmony = new Harmony(harmonyId);
            Instances[harmonyId] = harmony;
        }
        return harmony;
    }

    public static void Apply(Assembly moduleAssembly, string harmonyId) {
        ArgumentNullException.ThrowIfNull(moduleAssembly);
        if (string.IsNullOrWhiteSpace(harmonyId))
            throw new ArgumentException("Harmony id is required.", nameof(harmonyId));
        if (!Applied.Add(harmonyId))
            return;

        var harmony = GetOrCreate(harmonyId);
        List<Type> patchTypes;
        try {
            patchTypes = AccessTools.GetTypesFromAssembly(moduleAssembly)
                .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length > 0)
                .ToList();
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"KitLib Harmony could not scan {harmonyId}: {ex.Message}");
            return;
        }

        int applied = 0;
        int skipped = 0;
        foreach (var type in patchTypes) {
            try {
                harmony.CreateClassProcessor(type).Patch();
                applied++;
            }
            catch (Exception ex) {
                skipped++;
                MainFile.Logger.Warn($"KitLib Harmony skipped patch type {type.FullName}: {ex.Message}");
            }
        }


        MainFile.Logger.Info(
            $"KitLib Harmony patches applied: {harmonyId} ({applied} types, {skipped} skipped).");
    }
}
