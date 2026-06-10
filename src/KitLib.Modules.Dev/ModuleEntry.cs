using KitLib;
using KitLib.Abstractions.Host;
using KitLib.Host;

namespace KitLib.Dev;

public static class ModuleEntry {
    public static void Initialize() {
        if (KitLibHost.IsModuleLoaded(KitLibModuleIds.Dev))
            return;

        KitLibHost.AnnounceModule(KitLibModuleIds.Dev);
    }
}
