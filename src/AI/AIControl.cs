using System;
using System.Reflection;

namespace DevMode.AI;

/// <summary>
/// Thin wrapper around STS2AI's GameBridge for DevMode integration.
/// Uses reflection so STS2AI.dll is NOT a hard assembly dependency —
/// DevMode loads normally even when STS2AI is absent.
/// </summary>
internal static class AIControl
{
    // Cached reflection handles; null = STS2AI not available
    private static bool _resolved;
    private static PropertyInfo? _isAIEnabled;
    private static PropertyInfo? _strategy;
    private static PropertyInfo? _actionDelayMs;

    // AIStrategy enum values cached as int, and the enum Type for SetValue
    private static Type? _strategyEnumType;
    private static int _stratRuleBased;
    private static int _stratExternalBridge;

    public static bool IsAvailable
    {
        get
        {
            EnsureResolved();
            return _isAIEnabled != null;
        }
    }

    public static bool IsEnabled
    {
        get
        {
            try { return (bool?)_isAIEnabled?.GetValue(null) ?? false; }
            catch { return false; }
        }
    }

    public static void Toggle()
    {
        try
        {
            bool current = (bool?)_isAIEnabled?.GetValue(null) ?? false;
            _isAIEnabled?.SetValue(null, !current);
            MainFile.Logger.Info($"AIControl: AI toggled to {!current}");
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
            object? raw = _strategy?.GetValue(null);
            int val = raw != null ? Convert.ToInt32(raw) : -1;
            if (val == _stratRuleBased)      return "规则";
            if (val == _stratExternalBridge) return "桥接";
            return "?";
        }
        catch { return "N/A"; }
    }

    public static void CycleStrategy()
    {
        try
        {
            object? raw = _strategy?.GetValue(null);
            int current = raw != null ? Convert.ToInt32(raw) : _stratRuleBased;
            int next = current == _stratRuleBased ? _stratExternalBridge : _stratRuleBased;
            // SetValue needs the actual enum type, not a plain int
            object enumValue = Enum.ToObject(_strategyEnumType!, next);
            _strategy?.SetValue(null, enumValue);
            MainFile.Logger.Info($"AIControl: Strategy changed to {GetStrategyName()}");
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
            int current = (int?)_actionDelayMs?.GetValue(null) ?? 500;
            int next = current switch
            {
                <= 200 => 500,
                <= 500 => 800,
                <= 800 => 1500,
                _ => 100,
            };
            _actionDelayMs?.SetValue(null, next);
            MainFile.Logger.Info($"AIControl: Action delay changed to {next}ms");
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
            int ms = (int?)_actionDelayMs?.GetValue(null) ?? 500;
            return ms switch
            {
                <= 200 => "极速",
                <= 500 => "快速",
                <= 800 => "正常",
                _ => "慢速",
            };
        }
        catch { return "N/A"; }
    }

    private static void EnsureResolved()
    {
        if (_resolved) return;
        _resolved = true;

        try
        {
            var asm = Assembly.Load("STS2AI");

            var bridgeType = asm.GetType("STS2AI.API.GameBridge");
            if (bridgeType == null) return;

            _isAIEnabled  = bridgeType.GetProperty("IsAIEnabled",  BindingFlags.Public | BindingFlags.Static);
            _strategy     = bridgeType.GetProperty("Strategy",     BindingFlags.Public | BindingFlags.Static);
            _actionDelayMs = bridgeType.GetProperty("ActionDelayMs", BindingFlags.Public | BindingFlags.Static);

            var strategyType = asm.GetType("STS2AI.AIPlayer.AIStrategy");
            if (strategyType != null)
            {
                _strategyEnumType    = strategyType;
                _stratRuleBased      = (int)(Enum.Parse(strategyType, "RuleBased"));
                _stratExternalBridge = (int)(Enum.Parse(strategyType, "ExternalBridge"));
            }

            MainFile.Logger.Info("AIControl: STS2AI found — AI controls enabled.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"AIControl: STS2AI not available ({ex.Message}), AI controls hidden.");
        }
    }
}
