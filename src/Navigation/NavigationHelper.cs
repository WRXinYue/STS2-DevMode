using System;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Navigation;

internal static class NavigationHelper {
    /// <summary>
    /// Deprecated: CardBrowserUI now provides a self-drawn card browser.
    /// Kept for potential external usage (e.g. main menu preview).
    /// </summary>
    [Obsolete("Use CardBrowserUI.Show instead for in-game card browsing")]
    public static bool TryOpenCardLibrary(RunState state) {
        try {
            var submenuStack = NRun.Instance?.GlobalUi?.SubmenuStack;
            if (submenuStack == null) return false;

            if (NCapstoneContainer.Instance?.CurrentCapstoneScreen != (ICapstoneScreen)submenuStack) {
                var screen = submenuStack.ShowScreen(CapstoneSubmenuType.PauseMenu);
                if (screen is NPauseMenu pm)
                    pm.Initialize((IRunState)state);
            }

            var stack = submenuStack.Stack;
            if (stack == null) return false;

            if (((NSubmenuStack)stack).Peek() is not NPauseMenu) {
                var pm = stack.GetSubmenuType<NPauseMenu>();
                pm.Initialize((IRunState)state);
                ((NSubmenuStack)stack).Push((NSubmenu)pm);
            }

            var cardLib = stack.GetSubmenuType<NCardLibrary>();
            cardLib.Initialize((IRunState)state);
            ((NSubmenuStack)stack).Push((NSubmenu)cardLib);

            MainFile.Logger.Info("NavigationHelper: Opened card library picker.");
            return true;
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"NavigationHelper: Failed to open card library: {ex.Message}");
            return false;
        }
    }

    public static bool TryOpenRelicCollection(RunState state) {
        try {
            var submenuStack = NRun.Instance?.GlobalUi?.SubmenuStack;
            if (submenuStack == null) return false;

            if (NCapstoneContainer.Instance?.CurrentCapstoneScreen != (ICapstoneScreen)submenuStack) {
                var screen = submenuStack.ShowScreen(CapstoneSubmenuType.PauseMenu);
                if (screen is NPauseMenu pm)
                    pm.Initialize((IRunState)state);
            }

            var stack = submenuStack.Stack;
            if (stack == null) return false;

            if (((NSubmenuStack)stack).Peek() is not NPauseMenu) {
                var pm = stack.GetSubmenuType<NPauseMenu>();
                pm.Initialize((IRunState)state);
                ((NSubmenuStack)stack).Push((NSubmenu)pm);
            }

            var relicCol = stack.GetSubmenuType<NRelicCollection>();
            ((NSubmenuStack)stack).Push((NSubmenu)relicCol);

            MainFile.Logger.Info("NavigationHelper: Opened relic collection picker.");
            return true;
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"NavigationHelper: Failed to open relic collection: {ex.Message}");
            return false;
        }
    }

    public static void CloseCapstone() {
        try {
            var container = NCapstoneContainer.Instance;
            if (container?.CurrentCapstoneScreen is NCapstoneSubmenuStack)
                container.Close();
        }
        catch { }
    }

    public static void CloseOverlays() {
        try {
            var overlayStack = NOverlayStack.Instance;
            if (overlayStack != null && overlayStack.ScreenCount > 0)
                overlayStack.Clear();
        }
        catch { }
    }

    public static void ClosePauseMenu() {
        try {
            NCapstoneContainer.Instance?.Close();
        }
        catch { }
    }
}
