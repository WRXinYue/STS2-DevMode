using KitLib.Abstractions.Host;
using KitLib.Host;

namespace KitLib.User;

internal static class UserTabRegistration {
    internal static void Register() {
        KitLibHost.RegisterTab(new KitLibTabDescriptor {
            Id = "devmode.logs",
            IconKey = "text-box-outline",
            DisplayName = I18N.T("panel.logs", "Logs"),
            Order = 900,
            Group = KitLibTabGroup.Utility,
            Kind = KitLibTabKind.Developer,
            OwningModuleId = KitLibModuleIds.User,
            OnActivate = _ => KitLibUserOps.OpenLogs?.Invoke(),
        });
        KitLibHost.RegisterTab(new KitLibTabDescriptor {
            Id = "devmode.manual",
            IconKey = "book-open-variant",
            DisplayName = I18N.T("panel.manual", "Manual"),
            Order = 950,
            Group = KitLibTabGroup.Utility,
            Kind = KitLibTabKind.Developer,
            OwningModuleId = KitLibModuleIds.User,
            OnActivate = _ => KitLibUserOps.OpenManual?.Invoke(),
        });
    }
}
