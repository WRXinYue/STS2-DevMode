using System;
using System.Globalization;
using System.Reflection;
using Godot;
namespace KitLib.Integration;
/// <summary>
///     Reflection helpers for RitsuLib <c>ModSettingsUiContext</c> and value bindings (no Godot UI).
/// </summary>
internal static class ModSettingsRitsuEntryReflection {
    internal const string EntryDefinitionTypeName = "STS2RitsuLib.Settings.ModSettingsEntryDefinition";
    internal const string LocalizationTypeName = "STS2RitsuLib.Settings.ModSettingsLocalization";
    public static object? GetProp(object target, string name) =>
        target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(target);
    public static object? CallRead(object binding) {
        var m = binding.GetType().GetMethod("Read", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes,
            null);
        return m?.Invoke(binding, null);
    }
    public static void CallWrite(object binding, object? value) {
        foreach (var mi in binding.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
            if (mi.Name != "Write" || mi.GetParameters().Length != 1)
                continue;
            var p = mi.GetParameters()[0].ParameterType;
            object? arg;
            try {
                arg = value == null
                    ? null
                    : p.IsInstanceOfType(value)
                        ? value
                        : Convert.ChangeType(value, p, CultureInfo.InvariantCulture);
            }
            catch {
                arg = value;
            }
            mi.Invoke(binding, new[] { arg });
            return;
        }
    }
    public static void MarkDirty(object context, object binding) {
        context.GetType().GetMethod("MarkDirty", BindingFlags.Public | BindingFlags.Instance)
            ?.Invoke(context, new[] { binding });
    }
    public static void RequestRefresh(object context) {
        context.GetType().GetMethod("RequestRefresh", BindingFlags.Public | BindingFlags.Instance)
            ?.Invoke(context, null);
    }
    public static void RegisterRefresh(object context, GodotObject node, Action apply) {
        var reg = context.GetType().GetMethod("RegisterRefresh", BindingFlags.Public | BindingFlags.Instance);
        reg?.Invoke(context, new object[] { (Action)(() => {
            if (!GodotObject.IsInstanceValid(node))
                return;
            apply();
        }) });
    }
    public static void NavigateToPage(object context, string pageId) {
        if (string.IsNullOrWhiteSpace(pageId))
            return;
        context.GetType().GetMethod("NavigateToPage", BindingFlags.Public | BindingFlags.Instance)
            ?.Invoke(context, new object[] { pageId });
    }
    public static string ResolveSubpageTargetId(object entry) {
        foreach (var name in new[] { "TargetPageId", "ChildPageId", "PageId", "NestedPageId" }) {
            if (GetProp(entry, name) is string s && !string.IsNullOrWhiteSpace(s))
                return s;
        }
        foreach (var p in entry.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (p.PropertyType != typeof(string))
                continue;
            if (!p.Name.Contains("Page", StringComparison.OrdinalIgnoreCase))
                continue;
            if (p.GetValue(entry) is string s2 && !string.IsNullOrWhiteSpace(s2))
                return s2;
        }
        return "";
    }
    public static string ResolveText(Type contextType, object? textObj, Assembly asm) {
        if (textObj == null)
            return ResolveLocalized(asm, "entry.label.empty", "—");
        foreach (var m in contextType.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if (m.Name != "Resolve")
                continue;
            var ps = m.GetParameters();
            if (ps.Length == 0 || !ps[0].ParameterType.IsInstanceOfType(textObj))
                continue;
            try {
                var args = ps.Length >= 2 ? new[] { textObj, string.Empty } : new[] { textObj };
                var s = m.Invoke(null, args) as string;
                return string.IsNullOrWhiteSpace(s) ? ResolveLocalized(asm, "entry.label.empty", "—") : s;
            }
            catch {
                return "—";
            }
        }
        return "—";
    }
    public static string? ResolveTextNullable(Type contextType, object? textObj) {
        if (textObj == null)
            return null;
        foreach (var m in contextType.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if (m.Name != "Resolve")
                continue;
            var ps = m.GetParameters();
            if (ps.Length == 0 || !ps[0].ParameterType.IsInstanceOfType(textObj))
                continue;
            try {
                var args = ps.Length >= 2 ? new[] { textObj, string.Empty } : new[] { textObj };
                return m.Invoke(null, args) as string;
            }
            catch {
                return null;
            }
        }
        return null;
    }
    public static string ResolveLocalized(Assembly asm, string key, string fallback) {
        var loc = asm.GetType(LocalizationTypeName);
        if (loc == null)
            return fallback;
        foreach (var m in loc.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if (m.Name != "Get")
                continue;
            var ps = m.GetParameters();
            if (ps.Length != 2 || ps[0].ParameterType != typeof(string) || ps[1].ParameterType != typeof(string))
                continue;
            try {
                var s = m.Invoke(null, new object[] { key, fallback }) as string;
                return string.IsNullOrWhiteSpace(s) ? fallback : s;
            }
            catch {
                return fallback;
            }
        }
        return fallback;
    }
    public static bool ValuesEqual(object? a, object? b) {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;
        if (a.Equals(b))
            return true;
        try {
            return Convert.ChangeType(a, b.GetType(), CultureInfo.InvariantCulture).Equals(b);
        }
        catch {
            return false;
        }
    }
    public static string FormatWithDelegate(object? formatter, object value, string fallback) {
        if (formatter is not Delegate d)
            return fallback;
        try {
            var r = d.DynamicInvoke(value);
            return r?.ToString() ?? fallback;
        }
        catch {
            return fallback;
        }
    }
    public static void TryInvokeDelegate(object? del, object context) {
        if (del is not Delegate d)
            return;
        try {
            var ps = d.Method.GetParameters();
            if (ps.Length == 0)
                d.DynamicInvoke();
            else if (ps.Length == 1 && ps[0].ParameterType.IsInstanceOfType(context))
                d.DynamicInvoke(context);
            else
                d.DynamicInvoke();
        }
        catch {
            try {
                (del as Action)?.Invoke();
            }
            catch {
                // ignored
            }
        }
    }
}