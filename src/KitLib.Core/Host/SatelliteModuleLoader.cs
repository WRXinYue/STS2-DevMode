using System.Reflection;
using KitLib.Abstractions.Host;
using MegaCrit.Sts2.Core.Modding;

namespace KitLib.Host;

/// <summary>
/// Loads optional KitLib satellite DLLs from <c>mods/KitLib/modules/</c>.
/// Skips a module when it is missing, already initialized externally, has unmet
/// prerequisites, or fails to load (conflict / init error).
/// </summary>
internal static class SatelliteModuleLoader {
    internal const string ModulesSubdir = "modules";
    sealed record ModuleSpec(
        string ModuleId,
        string AssemblyName,
        string? EntryTypeName,
        string[] Requires);

    static readonly ModuleSpec[] LoadOrder = [
        new(KitLibModuleIds.User, "KitLib.User", "KitLib.User.ModuleEntry", []),
        new(KitLibModuleIds.Ai, "KitLib.AI", "KitLib.AI.ModuleEntry", []),
        new(KitLibModuleIds.Panel, "KitLib.Panel", "KitLib.PanelMod.ModuleEntry", []),
        new(KitLibModuleIds.Cheat, "KitLib.Cheat", "KitLib.Cheat.ModuleEntry", [KitLibModuleIds.Panel]),
        new(KitLibModuleIds.Dev, "KitLib.Dev", "KitLib.Dev.ModuleEntry", [KitLibModuleIds.Panel]),
    ];

    internal static void LoadBundledModules() {
        var modDir = Path.GetDirectoryName(typeof(MainFile).Assembly.Location);
        if (string.IsNullOrEmpty(modDir)) {
            MainFile.Logger.Warn("[KitLib] Satellite loader: cannot resolve mod directory.");
            return;
        }

        foreach (var spec in LoadOrder) {
            TryLoadModule(modDir, spec);
        }
    }

    static void TryLoadModule(string modDir, ModuleSpec spec) {
        if (ModuleCatalog.IsLoaded(spec.ModuleId)) {
            MainFile.Logger.Info($"[KitLib] Module {spec.ModuleId} already active — skipping bundled load.");
            return;
        }

        if (IsExternallyInstalled(spec.ModuleId)) {
            MainFile.Logger.Info($"[KitLib] Module {spec.ModuleId} installed as separate mod — skipping bundled load.");
            return;
        }

        foreach (var required in spec.Requires) {
            if (!ModuleCatalog.IsLoaded(required)) {
                MainFile.Logger.Warn(
                    $"[KitLib] Module {spec.ModuleId} skipped — prerequisite {required} is not loaded.");
                return;
            }
        }

        try {
            var assembly = LoadAssembly(modDir, spec.AssemblyName);
            if (assembly == null) {
                MainFile.Logger.Info($"[KitLib] Module {spec.ModuleId} not present ({spec.AssemblyName}.dll).");
                return;
            }

            if (spec.EntryTypeName == null) {
                ModuleCatalog.Announce(spec.ModuleId);
                MainFile.Logger.Info($"[KitLib] Loaded passive module {spec.ModuleId}.");
                return;
            }

            var entryType = assembly.GetType(spec.EntryTypeName, throwOnError: false);
            if (entryType == null) {
                MainFile.Logger.Warn($"[KitLib] Module {spec.ModuleId} skipped — entry type {spec.EntryTypeName} not found.");
                return;
            }

            var init = entryType.GetMethod(
                "Initialize",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (init == null) {
                MainFile.Logger.Warn($"[KitLib] Module {spec.ModuleId} skipped — Initialize() not found.");
                return;
            }

            init.Invoke(null, null);
            if (!ModuleCatalog.IsLoaded(spec.ModuleId))
                ModuleCatalog.Announce(spec.ModuleId);
        }
        catch (TargetInvocationException ex) {
            MainFile.Logger.Warn(
                $"[KitLib] Module {spec.ModuleId} init failed — skipped ({ex.InnerException?.Message ?? ex.Message}).");
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[KitLib] Module {spec.ModuleId} load conflict — skipped ({ex.Message}).");
        }
    }

    static Assembly? LoadAssembly(string modDir, string assemblyName) {
        var modulesDir = Path.Combine(modDir, ModulesSubdir);
        var path = Path.Combine(modulesDir, assemblyName + ".dll");
        if (!File.Exists(path)) {
            var legacyPath = Path.Combine(modDir, assemblyName + ".dll");
            if (!File.Exists(legacyPath))
                return null;
            path = legacyPath;
            MainFile.Logger.Info(
                $"[KitLib] Loading {assemblyName} from mod root (legacy layout). Run make sync-full to move it under {ModulesSubdir}/.");
        }

        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));

        if (loaded != null) {
            var loadedPath = loaded.Location;
            if (!string.IsNullOrEmpty(loadedPath)
                && !PathsEqual(loadedPath, path)) {
                throw new InvalidOperationException(
                    $"assembly already loaded from {loadedPath} (wanted {path})");
            }
            return loaded;
        }

        return Assembly.LoadFrom(path);
    }

    static bool IsExternallyInstalled(string moduleId) {
        if (string.Equals(moduleId, KitLibModuleIds.Core, StringComparison.OrdinalIgnoreCase))
            return false;

        foreach (var mod in EnumerateLoadedMods()) {
            var id = mod.manifest?.id;
            if (!string.Equals(id, moduleId, StringComparison.OrdinalIgnoreCase))
                continue;
            return true;
        }

        return false;
    }

    static IEnumerable<Mod> EnumerateLoadedMods() {
        var method = typeof(ModManager).GetMethod(
            "GetLoadedMods",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        if (method == null)
            return [];
        return (IEnumerable<Mod>)method.Invoke(null, null)!;
    }

    static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
}
