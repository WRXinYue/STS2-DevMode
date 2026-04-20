using System;
using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Modding;

namespace DevMode.Modding;

/// <summary>
/// STS2 exposes loaded mods as <c>LoadedMods</c>, <c>GetLoadedMods()</c>, or <c>Mods</c> depending on build; resolve at runtime.
/// </summary>
internal static class ModManagerLoadedMods {
    private static readonly object InitLock = new();
    private static Func<IEnumerable<Mod>>? _enumerator;

    internal static IEnumerable<Mod> Enumerate() {
        EnsureInitialized();
        return _enumerator!();
    }

    private static void EnsureInitialized() {
        if (_enumerator != null)
            return;
        lock (InitLock) {
            if (_enumerator != null)
                return;
            _enumerator = BuildLoadedModsEnumerator();
        }
    }

    private static Func<IEnumerable<Mod>> BuildLoadedModsEnumerator() {
        var t = typeof(ModManager);

        var getLoaded = t.GetMethod("GetLoadedMods", BindingFlags.Public | BindingFlags.Static, null,
            Type.EmptyTypes, null);
        if (getLoaded != null)
            return () => (IEnumerable<Mod>)getLoaded.Invoke(null, null)!;

        var loadedProp = t.GetProperty("LoadedMods", BindingFlags.Public | BindingFlags.Static);
        if (loadedProp != null)
            return () => (IEnumerable<Mod>)loadedProp.GetValue(null)!;

        var modsProp = t.GetProperty("Mods", BindingFlags.Public | BindingFlags.Static);
        if (modsProp != null)
            return () => FilterLoadedModsFromModsList(modsProp);

        throw new InvalidOperationException(
            "ModManager exposes no GetLoadedMods(), LoadedMods, or Mods; cannot enumerate loaded mods.");
    }

    private static IEnumerable<Mod> FilterLoadedModsFromModsList(PropertyInfo modsProp) {
        var raw = modsProp.GetValue(null);
        if (raw is not IEnumerable<Mod> enumerable)
            yield break;

        var modType = typeof(Mod);
        var stateProp = modType.GetProperty("state", BindingFlags.Public | BindingFlags.Instance);
        var wasLoadedField = modType.GetField("wasLoaded", BindingFlags.Public | BindingFlags.Instance);
        var wasLoadedProp = modType.GetProperty("wasLoaded", BindingFlags.Public | BindingFlags.Instance);

        foreach (var m in enumerable)
            if (IsModLoadedForDiscovery(m, stateProp, wasLoadedField, wasLoadedProp))
                yield return m;
    }

    private static bool IsModLoadedForDiscovery(Mod m, PropertyInfo? stateProp, FieldInfo? wasLoadedField,
        PropertyInfo? wasLoadedProp) {
        if (stateProp?.GetValue(m) is { } stateValue)
            return string.Equals(stateValue.ToString(), "Loaded", StringComparison.Ordinal);

        if (wasLoadedProp?.GetValue(m) is bool wp)
            return wp;
        if (wasLoadedField?.GetValue(m) is bool wf)
            return wf;
        return true;
    }
}
