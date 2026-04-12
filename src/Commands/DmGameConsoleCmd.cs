using System;
using System.Linq;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace DevMode.Commands;

public class DmGameConsoleCmd : AbstractConsoleCmd {
    public override string CmdName => "dmgame";
    public override string Args => "<speed|skipanim|maprewrite> [mode]";
    public override string Description => "[DevMode] Game speed, animation skip, map rewrite";
    public override bool IsNetworked => false;
    public override bool DebugOnly => false;

    private static readonly string[] SubCmds = { "speed", "skipanim", "maprewrite" };
    private static readonly string[] MapModes = { "none", "allchest", "allelite", "allboss" };

    public override CmdResult Process(Player? issuingPlayer, string[] args) {
        if (args.Length < 1)
            return new CmdResult(false, "Usage: dmgame <speed|skipanim|maprewrite> [mode]");

        var sub = args[0].ToLowerInvariant();

        switch (sub) {
            case "speed": {
                    SpeedControl.CycleSpeed();
                    return new CmdResult(true, $"Game speed: {SpeedControl.GetLabel()}");
                }
            case "skipanim": {
                    SkipAnimControl.Toggle();
                    return new CmdResult(true, $"Skip animations: {SkipAnimControl.GetLabel()}");
                }
            case "maprewrite": {
                    if (args.Length < 2) {
                        var current = DevModeState.MapRewriteEnabled
                            ? DevModeState.MapRewriteMode.ToString()
                            : "disabled";
                        return new CmdResult(true, $"Map rewrite: {current}\nUsage: dmgame maprewrite <none|allchest|allelite|allboss>");
                    }

                    var mode = args[1].ToLowerInvariant();
                    switch (mode) {
                        case "none" or "off" or "disable":
                            DevModeState.MapRewriteEnabled = false;
                            DevModeState.MapRewriteMode = MapRewriteMode.None;
                            return new CmdResult(true, "Map rewrite: disabled");
                        case "allchest" or "chest":
                            DevModeState.MapRewriteEnabled = true;
                            DevModeState.MapRewriteMode = MapRewriteMode.AllChest;
                            return new CmdResult(true, "Map rewrite: AllChest");
                        case "allelite" or "elite":
                            DevModeState.MapRewriteEnabled = true;
                            DevModeState.MapRewriteMode = MapRewriteMode.AllElite;
                            return new CmdResult(true, "Map rewrite: AllElite");
                        case "allboss" or "boss":
                            DevModeState.MapRewriteEnabled = true;
                            DevModeState.MapRewriteMode = MapRewriteMode.AllBoss;
                            return new CmdResult(true, "Map rewrite: AllBoss");
                        default:
                            return new CmdResult(false, $"Unknown mode: '{mode}'. Use: {string.Join(", ", MapModes)}");
                    }
                }
            default:
                return new CmdResult(false, $"Unknown subcommand: '{sub}'. Use: {string.Join(", ", SubCmds)}");
        }
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args) {
        if (args.Length <= 1)
            return CompleteArgument(SubCmds, Array.Empty<string>(), args.FirstOrDefault() ?? "");

        if (args[0].Equals("maprewrite", StringComparison.OrdinalIgnoreCase) && args.Length == 2)
            return CompleteArgument(MapModes, new[] { args[0] }, args[1]);

        return base.GetArgumentCompletions(player, args);
    }
}
