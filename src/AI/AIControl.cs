using System;
using STS2AI.AIPlayer;
using STS2AI.API;

namespace DevMode.AI;

/// <summary>
/// Thin wrapper around STS2AI's GameBridge for DevMode integration.
/// All calls are wrapped in try-catch for graceful degradation if STS2AI is not loaded.
/// </summary>
internal static class AIControl
{
    public static bool IsAvailable
    {
        get
        {
            try { return CheckAvailable(); }
            catch { return false; }
        }
    }

    public static bool IsEnabled
    {
        get { try { return GameBridge.IsAIEnabled; } catch { return false; } }
    }

    public static void Toggle()
    {
        try
        {
            GameBridge.IsAIEnabled = !GameBridge.IsAIEnabled;
            MainFile.Logger.Info($"AIControl: AI toggled to {GameBridge.IsAIEnabled}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"AIControl: Failed to toggle AI: {ex.Message}");
        }
    }

    public static string GetStrategyName()
    {
        try
        {
            return GameBridge.Strategy switch
            {
                AIStrategy.RuleBased => "规则",
                AIStrategy.ExternalBridge => "桥接",
                _ => "?",
            };
        }
        catch { return "N/A"; }
    }

    public static void CycleStrategy()
    {
        try
        {
            GameBridge.Strategy = GameBridge.Strategy switch
            {
                AIStrategy.RuleBased => AIStrategy.ExternalBridge,
                _ => AIStrategy.RuleBased,
            };
            MainFile.Logger.Info($"AIControl: Strategy changed to {GameBridge.Strategy}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"AIControl: Failed to cycle strategy: {ex.Message}");
        }
    }

    public static void CycleSpeed()
    {
        try
        {
            int current = GameBridge.ActionDelayMs;
            GameBridge.ActionDelayMs = current switch
            {
                <= 200 => 500,
                <= 500 => 800,
                <= 800 => 1500,
                _ => 100,
            };
            MainFile.Logger.Info($"AIControl: Action delay changed to {GameBridge.ActionDelayMs}ms");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"AIControl: Failed to cycle speed: {ex.Message}");
        }
    }

    public static string GetSpeedLabel()
    {
        try
        {
            return GameBridge.ActionDelayMs switch
            {
                <= 200 => "极速",
                <= 500 => "快速",
                <= 800 => "正常",
                _ => "慢速",
            };
        }
        catch { return "N/A"; }
    }

    private static bool CheckAvailable()
    {
        _ = GameBridge.IsRunActive;
        return true;
    }
}
