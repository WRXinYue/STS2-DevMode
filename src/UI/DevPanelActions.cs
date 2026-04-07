using System;

namespace DevMode.UI;

internal sealed class DevPanelActions
{
    public required Action OnOpenCards     { get; init; }
    public required Action OnOpenRelics   { get; init; }
    public required Action OnOpenEnemies  { get; init; }
    public required Action OnOpenSave     { get; init; }
    public required Action OnOpenLoad     { get; init; }
    public required Action OnNewTest      { get; init; }
    public required Action OnRefreshPanel { get; init; }

    public required Action OnOpenPowers   { get; init; }
    public required Action OnOpenPotions  { get; init; }
    public required Action OnOpenEvents   { get; init; }
    public required Action OnOpenConsole  { get; init; }
    public required Action OnOpenPresets  { get; init; }

    public Action? OnToggleAI       { get; init; }
    public Action? OnCycleStrategy  { get; init; }
    public Action? OnCycleSpeed     { get; init; }
    public Func<bool>? IsAIEnabled  { get; init; }
    public Func<string>? GetStrategyName { get; init; }
    public Func<string>? GetSpeedLabel   { get; init; }

    public required Action OnCycleGameSpeed   { get; init; }
    public required Func<string> GetGameSpeedLabel { get; init; }

    public required Action OnToggleSkipAnim    { get; init; }
    public required Func<string> GetSkipAnimLabel { get; init; }
}
