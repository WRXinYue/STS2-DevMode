using Godot;
using DevMode.Patches;

namespace DevMode;

/// <summary>
/// Lightweight Godot Node that hooks into the scene tree's _Process loop.
/// Drives RuntimeStatModifiers and AssetWarmupService each frame.
/// </summary>
internal partial class DevModeProcessNode : Node
{
    public override void _Process(double delta)
    {
        if (!DevModeState.InDevRun && !DevModeState.AlwaysEnabled) return;
        GlobalUiReadyPatch.Process(delta);
    }
}
