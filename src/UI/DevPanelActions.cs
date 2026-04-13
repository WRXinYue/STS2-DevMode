using System;

namespace DevMode.UI;

/// <summary>
/// Callbacks consumed by built-in overlay panels (Save/Load, Settings).
/// Panel-open actions have moved to <see cref="DevPanelRegistry"/>.
/// </summary>
internal sealed class DevPanelActions {
    // Save / Load overlay
    public required Action OnOpenSave { get; init; }
    public required Action OnOpenLoad { get; init; }
    public required Action OnNewTest { get; init; }

    // UI coordination
    public required Action OnRefreshPanel { get; init; }

    // Settings overlay
    public required Action OnCycleGameSpeed { get; init; }
    public required Func<string> GetGameSpeedLabel { get; init; }
    public required Action OnToggleSkipAnim { get; init; }
    public required Func<string> GetSkipAnimLabel { get; init; }
}
