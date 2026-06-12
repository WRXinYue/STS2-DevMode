using KitLib.Abstractions.Host;
using KitLib.Abstractions.Modding;
using KitLib.Host;
using KitLib.Integration;

namespace KitLib.ModPanelMod;

public static class ModuleEntry {
    public static void Initialize() {
        if (KitLibHost.IsModuleLoaded(KitLibModuleIds.ModPanel)) return;
        KitLibHost.AnnounceModule(KitLibModuleIds.ModPanel);
        KitLibModSettingsUiOps.BuildLogLevelRow = (title, desc, get, set) =>
            KitLibNativeModSettingsUi.CreateLogLevelRow(title, desc, get, set);
        KitLibHost.RegisterModSettingsPanelHost(new ModSettingsPanelHost());
        KitLibNativeModSettingsBootstrap.RegisterKitLibPages();
        KitLibHost.NotifyPerfHudEnabledChanged = KitLibNativeModSettingsUi.RefreshBoolToggles;
        KitLibHarmony.Apply(typeof(ModuleEntry).Assembly, KitLibModuleIds.ModPanel);
        MainFile.Logger.Info("KitLib.ModPanel module initialized.");
    }
}
