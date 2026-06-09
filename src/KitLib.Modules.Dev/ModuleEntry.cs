using KitLib;
using KitLib.Abstractions.Host;
using KitLib.AI.Combat.Simulation;
using KitLib.CombatStats;
using KitLib.EnemyIntent;
using KitLib.Host;
using KitLib.Interop;
using KitLib.Mcp;
using KitLib.Scripts;

namespace KitLib.Dev;

public static class ModuleEntry {
    public static void Initialize() {
        if (KitLibHost.IsModuleLoaded(KitLibModuleIds.Dev)) return;
        KitLibHost.AnnounceModule(KitLibModuleIds.Dev);
        KitLibHost.IsDualInstanceActive = KitLibInstanceRegistry.IsDualInstanceActive;
        KitLibInstanceRegistry.Register();
        ScriptManager.Initialize();
        ScriptBridge.Start();
        McpBridge.Start();
        FrameworkBridge.Initialize();
        CombatStatsTracker.Initialize();
        MonsterIntentOverlayTracker.Initialize();
        MonsterIntentOverrides.Initialize();
        KitLibHost.CaptureMonsterIntentSteps = (enemy, targets, pressure) =>
            MonsterIntentReader.CaptureIntentSteps(enemy, targets, (CombatState)pressure);
        DevTabRegistration.Register();

        KitLibHarmony.Apply(typeof(ModuleEntry).Assembly, KitLibModuleIds.Dev);
        MainFile.Logger.Info("KitLib.Dev module initialized.");
    }
}
