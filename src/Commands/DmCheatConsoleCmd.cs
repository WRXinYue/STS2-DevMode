using System;
using System.Linq;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace DevMode.Commands;

public class DmCheatConsoleCmd : AbstractConsoleCmd {
    public override string CmdName => "dmcheat";
    public override string Args => "<toggle> [on|off|value]";
    public override string Description => "[DevMode] Toggle cheat flags (godmode, infinitehp, onehitkill, ...)";
    public override bool IsNetworked => false;
    public override bool DebugOnly => false;

    private static readonly string[] Toggles =
    {
        "godmode", "infinitehp", "infiniteblock", "infiniteenergy", "infinitestars",
        "freezeenemies", "onehitkill", "freeshop", "alwayspotion", "alwaysupgrade",
        "maxrarity", "unknowntreasure", "maxscore"
    };

    private static readonly string[] Multipliers =
    {
        "damagemult", "defensemult", "goldmult", "scoremult"
    };

    private static readonly string[] AllSubs = Toggles.Concat(Multipliers).ToArray();

    public override CmdResult Process(Player? issuingPlayer, string[] args) {
        if (args.Length < 1)
            return new CmdResult(false, $"Usage: dmcheat <toggle> [on|off|value]\nToggles: {string.Join(", ", Toggles)}\nMultipliers: {string.Join(", ", Multipliers)}");

        var sub = args[0].ToLowerInvariant();
        bool? flag = args.Length >= 2 ? ParseBool(args[1]) : null;

        switch (sub) {
            case "godmode":
            case "infinitehp": {
                    var v = flag ?? !DevModeState.InfiniteHp;
                    DevModeState.InfiniteHp = v;
                    return new CmdResult(true, $"Infinite HP: {OnOff(v)}");
                }
            case "infiniteblock": {
                    var v = flag ?? !DevModeState.InfiniteBlock;
                    DevModeState.InfiniteBlock = v;
                    return new CmdResult(true, $"Infinite Block: {OnOff(v)}");
                }
            case "infiniteenergy": {
                    var v = flag ?? !DevModeState.InfiniteEnergy;
                    DevModeState.InfiniteEnergy = v;
                    return new CmdResult(true, $"Infinite Energy: {OnOff(v)}");
                }
            case "infinitestars": {
                    var v = flag ?? !DevModeState.InfiniteStars;
                    DevModeState.InfiniteStars = v;
                    return new CmdResult(true, $"Infinite Stars: {OnOff(v)}");
                }
            case "freezeenemies": {
                    var v = flag ?? !DevModeState.FreezeEnemies;
                    DevModeState.FreezeEnemies = v;
                    return new CmdResult(true, $"Freeze Enemies: {OnOff(v)}");
                }
            case "onehitkill": {
                    var v = flag ?? !DevModeState.OneHitKill;
                    DevModeState.OneHitKill = v;
                    return new CmdResult(true, $"One-Hit Kill: {OnOff(v)}");
                }
            case "freeshop": {
                    var v = flag ?? !DevModeState.FreeShop;
                    DevModeState.FreeShop = v;
                    return new CmdResult(true, $"Free Shop: {OnOff(v)}");
                }
            case "alwayspotion": {
                    var v = flag ?? !DevModeState.AlwaysRewardPotion;
                    DevModeState.AlwaysRewardPotion = v;
                    return new CmdResult(true, $"Always Reward Potion: {OnOff(v)}");
                }
            case "alwaysupgrade": {
                    var v = flag ?? !DevModeState.AlwaysUpgradeCardReward;
                    DevModeState.AlwaysUpgradeCardReward = v;
                    return new CmdResult(true, $"Always Upgrade Reward: {OnOff(v)}");
                }
            case "maxrarity": {
                    var v = flag ?? !DevModeState.MaxCardRewardRarity;
                    DevModeState.MaxCardRewardRarity = v;
                    return new CmdResult(true, $"Max Card Reward Rarity: {OnOff(v)}");
                }
            case "unknowntreasure": {
                    var v = flag ?? !DevModeState.UnknownMapAlwaysTreasure;
                    DevModeState.UnknownMapAlwaysTreasure = v;
                    return new CmdResult(true, $"Unknown → Treasure: {OnOff(v)}");
                }
            case "maxscore": {
                    var v = flag ?? !DevModeState.MaxScore;
                    DevModeState.MaxScore = v;
                    return new CmdResult(true, $"Max Score: {OnOff(v)}");
                }

            // Multipliers
            case "damagemult":
                return SetMultiplier(args, "Damage Multiplier", DevModeState.DamageMultiplier, v => DevModeState.DamageMultiplier = v);
            case "defensemult":
                return SetMultiplier(args, "Defense Multiplier", DevModeState.DefenseMultiplier, v => DevModeState.DefenseMultiplier = v);
            case "goldmult":
                return SetMultiplier(args, "Gold Multiplier", DevModeState.GoldMultiplier, v => DevModeState.GoldMultiplier = v);
            case "scoremult":
                return SetMultiplier(args, "Score Multiplier", DevModeState.ScoreMultiplier, v => DevModeState.ScoreMultiplier = v);

            default:
                return new CmdResult(false, $"Unknown toggle: '{sub}'. Available: {string.Join(", ", AllSubs)}");
        }
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args) {
        if (args.Length <= 1)
            return CompleteArgument(AllSubs, Array.Empty<string>(), args.FirstOrDefault() ?? "");

        var sub = args[0].ToLowerInvariant();
        if (Toggles.Contains(sub))
            return CompleteArgument(new[] { "on", "off" }, new[] { args[0] }, args.Length > 1 ? args[1] : "");

        return base.GetArgumentCompletions(player, args);
    }

    private static CmdResult SetMultiplier(string[] args, string label, float current, Action<float> setter) {
        if (args.Length < 2)
            return new CmdResult(true, $"{label}: {current:F1}");
        if (!float.TryParse(args[1], out var val) || val < 0)
            return new CmdResult(false, $"Invalid value. Usage: dmcheat {args[0]} <float>");
        setter(val);
        return new CmdResult(true, $"{label}: {val:F1}");
    }

    private static string OnOff(bool v) => v ? "ON" : "OFF";

    private static bool? ParseBool(string s) => s.ToLowerInvariant() switch {
        "on" or "true" or "1" or "yes" => true,
        "off" or "false" or "0" or "no" => false,
        _ => null
    };
}
