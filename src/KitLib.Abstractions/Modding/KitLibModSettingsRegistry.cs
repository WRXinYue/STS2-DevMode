namespace KitLib.Abstractions.Modding;

/// <summary>In-process registry for KitLib-native mod settings pages (fast path in ModPanel).</summary>
public static class KitLibModSettingsRegistry {
    static readonly List<KitLibModSettingsPageRegistration> Pages = [];
    static readonly object Gate = new();

    public static void Register(KitLibModSettingsPageRegistration page) {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentException.ThrowIfNullOrWhiteSpace(page.ModId);
        ArgumentException.ThrowIfNullOrWhiteSpace(page.PageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(page.Title);
        ArgumentNullException.ThrowIfNull(page.BuildBody);

        lock (Gate) {
            Pages.RemoveAll(p =>
                string.Equals(p.ModId, page.ModId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.PageId, page.PageId, StringComparison.OrdinalIgnoreCase));
            Pages.Add(page);
        }
    }

    public static bool HasPages(string modId) {
        if (string.IsNullOrWhiteSpace(modId))
            return false;
        lock (Gate) {
            return Pages.Any(p => string.Equals(p.ModId, modId, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static IReadOnlyList<KitLibModSettingsPageRegistration> GetPages(string modId) {
        if (string.IsNullOrWhiteSpace(modId))
            return Array.Empty<KitLibModSettingsPageRegistration>();
        lock (Gate) {
            return Pages
                .Where(p => string.Equals(p.ModId, modId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.PageId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    internal static void ClearForTests() {
        lock (Gate)
            Pages.Clear();
    }
}
