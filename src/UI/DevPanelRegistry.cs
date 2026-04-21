using System;
using System.Collections.Generic;
using System.Linq;
using DevMode.Icons;
using DevMode.Modding;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

/// <summary>
/// Central registry for DevMode rail tabs.
/// Both built-in panels and external mods register here.
/// </summary>
public static class DevPanelRegistry {
    private static readonly List<IDevPanelTab> _tabs = new();
    private static bool _dirty = true;

    /// <summary>
    /// Queues a callback to run once, after all mods have finished <see cref="MegaCrit.Sts2.Core.Modding.ModManager"/> initialization
    /// and immediately before <see cref="MegaCrit.Sts2.Core.Localization.LocManager.Initialize"/> runs (same timing as safe external registration).
    /// If called after that phase, <paramref name="registration"/> runs immediately.
    /// </summary>
    /// <remarks>
    /// Prefer this over <see cref="Register"/> from another mod's <c>[ModInitializer]</c> when load order is uncertain.
    /// For compile-time references to DevMode, list <c>DevMode</c> in your mod manifest <c>dependencies</c> so DevMode loads first.
    /// Equivalent to <see cref="ModRuntime.RegisterAfterAllModsLoaded"/>; use whichever reads clearer in your mod.
    /// </remarks>
    public static void RegisterPanelWhenReady(Action registration)
        => ModLoadCoordinator.Register(registration);

    /// <summary>Register a tab. If a tab with the same <see cref="IDevPanelTab.Id"/> already exists, it is replaced.</summary>
    public static void Register(IDevPanelTab tab) {
        if (tab == null) throw new ArgumentNullException(nameof(tab));
        _tabs.RemoveAll(t => t.Id == tab.Id);
        _tabs.Add(tab);
        _dirty = true;
    }

    /// <summary>Convenience overload — register with lambdas, no need to implement <see cref="IDevPanelTab"/>.</summary>
    public static void Register(string id, MdiIcon icon, string displayName,
        int order, DevPanelTabGroup group, Action<NGlobalUi> onActivate,
        Action<NGlobalUi>? onDeactivate = null,
        DevPanelTabKind kind = DevPanelTabKind.Cheat) {
        Register(new LambdaTab(id, icon, displayName, order, group, onActivate, onDeactivate, kind));
    }

    /// <summary>Remove a previously registered tab by id. Returns true if found.</summary>
    public static bool Unregister(string id) {
        int removed = _tabs.RemoveAll(t => t.Id == id);
        if (removed > 0) _dirty = true;
        return removed > 0;
    }

    /// <summary>Get all tabs for a given group, sorted by <see cref="IDevPanelTab.Order"/> (stable).</summary>
    public static IReadOnlyList<IDevPanelTab> GetTabs(DevPanelTabGroup group) {
        if (_dirty) {
            _tabs.Sort((a, b) => a.Order.CompareTo(b.Order));
            _dirty = false;
        }
        return _tabs.Where(t => t.Group == group).ToList();
    }

    /// <summary>Get all registered tabs across all groups, sorted by order.</summary>
    public static IReadOnlyList<IDevPanelTab> GetAllTabs() {
        if (_dirty) {
            _tabs.Sort((a, b) => a.Order.CompareTo(b.Order));
            _dirty = false;
        }
        return _tabs.AsReadOnly();
    }

    /// <summary>Deactivate all tabs and clear the registry.</summary>
    internal static void DeactivateAll(NGlobalUi globalUi) {
        foreach (var tab in _tabs) {
            try { tab.OnDeactivate(globalUi); }
            catch (Exception ex) { MainFile.Logger.Warn($"DevPanelRegistry: OnDeactivate({tab.Id}) failed: {ex.Message}"); }
        }
    }

    // ── Private lambda wrapper ──

    private sealed class LambdaTab : IDevPanelTab {
        public string Id { get; }
        public MdiIcon Icon { get; }
        public string DisplayName { get; }
        public int Order { get; }
        public DevPanelTabGroup Group { get; }
        public DevPanelTabKind Kind { get; }

        private readonly Action<NGlobalUi> _onActivate;
        private readonly Action<NGlobalUi>? _onDeactivate;

        public LambdaTab(string id, MdiIcon icon, string displayName,
            int order, DevPanelTabGroup group,
            Action<NGlobalUi> onActivate, Action<NGlobalUi>? onDeactivate,
            DevPanelTabKind kind = DevPanelTabKind.Cheat) {
            Id = id;
            Icon = icon;
            DisplayName = displayName;
            Order = order;
            Group = group;
            Kind = kind;
            _onActivate = onActivate;
            _onDeactivate = onDeactivate;
        }

        public void OnActivate(NGlobalUi globalUi) => _onActivate(globalUi);
        public void OnDeactivate(NGlobalUi globalUi) => _onDeactivate?.Invoke(globalUi);
    }
}
