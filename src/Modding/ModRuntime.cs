using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Modding;

namespace DevMode.Modding;

/// <summary>Stable snapshot of one loaded mod (manifest-backed).</summary>
public readonly record struct DevModeModInfo(string Id, string DisplayName, string Version);

/// <summary>
/// Read-only view of mods the game has already scanned and loaded.
/// Other assemblies can depend on DevMode and use <see cref="ModRuntime.Catalog"/> for a shared implementation.
/// </summary>
public interface IModCatalog {
    /// <summary>Copies current loaded-mod entries that have a non-empty manifest <c>id</c>.</summary>
    IReadOnlyList<DevModeModInfo> GetSnapshot();

    /// <summary>Fast membership checks (e.g. log line attribution). Empty if no mods loaded.</summary>
    HashSet<string> GetIdSet(StringComparer? comparer = null);
}

/// <summary>Default <see cref="IModCatalog"/> backed by the game's loaded mod enumeration.</summary>
public sealed class ModCatalog : IModCatalog {
    public static IModCatalog Default { get; } = new ModCatalog();

    private ModCatalog() { }

    public IReadOnlyList<DevModeModInfo> GetSnapshot() {
        var mods = ModManagerLoadedMods.Enumerate().ToList();
        if (mods.Count == 0)
            return Array.Empty<DevModeModInfo>();

        var list = new List<DevModeModInfo>(mods.Count);
        foreach (var m in mods) {
            var man = m.manifest;
            if (man == null) continue;
            var id = man.id;
            if (string.IsNullOrEmpty(id)) continue;
            var name = string.IsNullOrEmpty(man.name) ? id : man.name;
            var ver = man.version ?? "";
            list.Add(new DevModeModInfo(id, name, ver));
        }

        return list;
    }

    public HashSet<string> GetIdSet(StringComparer? comparer = null) {
        comparer ??= StringComparer.Ordinal;
        var set = new HashSet<string>(comparer);
        foreach (var m in ModManagerLoadedMods.Enumerate()) {
            var id = m.manifest?.id;
            if (!string.IsNullOrEmpty(id))
                set.Add(id);
        }

        return set;
    }
}

/// <summary>
/// Public hooks for other mods: catalog access and a single safe point after every <c>[ModInitializer]</c> has run.
/// </summary>
public static class ModRuntime {
    /// <summary>Game-backed mod list; safe to call from main thread after mod load.</summary>
    public static IModCatalog Catalog => ModCatalog.Default;

    /// <summary>
    /// Same timing as <see cref="UI.DevPanelRegistry.RegisterPanelWhenReady"/> — queued until all mod initializers finish,
    /// then invoked on the same thread immediately before <c>LocManager.Initialize</c>.
    /// </summary>
    public static void RegisterAfterAllModsLoaded(Action registration)
        => ModLoadCoordinator.Register(registration);
}

/// <summary>Shared coordinator for post-mod-load work (DevMode UI and third-party callbacks).</summary>
internal static class ModLoadCoordinator {
    private static readonly object Sync = new();
    private static readonly List<Action> Queue = new();
    private static bool _phaseDone;

    public static void Register(Action registration) {
        ArgumentNullException.ThrowIfNull(registration);

        lock (Sync) {
            if (_phaseDone) {
                Run(registration);
                return;
            }

            Queue.Add(registration);
        }
    }

    public static void Flush() {
        lock (Sync) {
            if (_phaseDone)
                return;

            _phaseDone = true;
            foreach (var action in Queue)
                Run(action);
            Queue.Clear();
        }
    }

    private static void Run(Action registration) {
        try {
            registration();
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"ModLoadCoordinator: {ex.Message}");
        }
    }
}
