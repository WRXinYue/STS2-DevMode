using System;
using System.Reflection;
using Godot;
namespace KitLib.Integration;
/// <summary>
/// Keeps a hidden <c>RitsuModSettingsSubmenu</c> alive so <c>ModSettingsUiContext</c> can bind saves/refreshes
/// while DevMode renders settings in its own shell.
/// </summary>
internal static class RitsuModSettingsEmbedHost {
    private const string SubmenuFullName = "STS2RitsuLib.Settings.RitsuModSettingsSubmenu";
    private static Control? _pin;
    private static Node? _submenu;
    public static Node? TryGetSubmenu() => _submenu;
    public static void EnsureAttached(Control shellRoot) {
        if (_submenu != null && GodotObject.IsInstanceValid(_submenu))
            return;
        var asm = RitsuModSettingsBridge.TryGetRitsuAssembly();
        if (asm == null)
            return;
        var submenuType = asm.GetType(SubmenuFullName);
        if (submenuType == null)
            return;
        _pin = new Control {
            Name = "KitLibRitsuSettingsEnginePin",
            CustomMinimumSize = new Vector2(1f, 1f),
            Size = new Vector2(1f, 1f),
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        shellRoot.AddChild(_pin);
        if (Activator.CreateInstance(submenuType) is not Node created) {
            _pin.QueueFree();
            _pin = null;
            return;
        }
        _submenu = created;
        _pin.AddChild(_submenu);
    }
    public static void SyncSubmenuSelection(string modId, string pageId) {
        if (_submenu == null || !GodotObject.IsInstanceValid(_submenu))
            return;
        var t = _submenu.GetType();
        t.GetField("_selectedModId", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_submenu, modId);
        t.GetField("_selectedPageId", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_submenu, pageId);
    }
    public static void FlushDirtyBindings() {
        if (_submenu == null || !GodotObject.IsInstanceValid(_submenu))
            return;
        var m = _submenu.GetType().GetMethod("FlushDirtyBindings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        try {
            m?.Invoke(_submenu, null);
        }
        catch {
            // ignored
        }
    }
    public static void ClearAfterShellDisposed() {
        _pin = null;
        _submenu = null;
    }
}