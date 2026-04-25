using DevMode.Patches;
using DevMode.Scripts;
using Godot;

namespace DevMode;

/// <summary>
/// Lightweight Godot Node that hooks into the scene tree's _Process loop.
/// Drives RuntimeStatModifiers, AssetWarmupService, and script hot-reload each frame.
/// </summary>
internal partial class DevModeProcessNode : Node {
    public override void _Process(double delta) {
        GlobalUiReadyPatch.Process(delta);
        ScriptManager.ProcessPendingReload();
    }
}
