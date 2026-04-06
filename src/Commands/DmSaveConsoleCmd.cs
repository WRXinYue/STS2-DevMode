using System;
using System.Linq;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace DevMode.Commands;

public class DmSaveConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "dmsave";
    public override string Args => "<quick|load|slot> [slotNumber] [name]";
    public override string Description => "[DevMode] Quick save/load and slot save/load";
    public override bool IsNetworked => false;
    public override bool DebugOnly => false;

    private static readonly string[] SubCmds = { "quick", "load", "slot" };

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length < 1)
            return new CmdResult(false, "Usage: dmsave <quick|load|slot> [slotNumber] [name]");

        var sub = args[0].ToLowerInvariant();

        switch (sub)
        {
            case "quick":
            {
                if (SaveSlotManager.QuickSave())
                    return new CmdResult(true, "Quick save successful.");
                return new CmdResult(false, "Quick save failed. Is a run in progress?");
            }
            case "load":
            {
                int slot = 0;
                if (args.Length >= 2 && int.TryParse(args[1], out var s))
                    slot = s;

                if (!SaveSlotManager.HasSlot(slot))
                    return new CmdResult(false, $"Slot {slot} is empty.");

                if (SaveSlotManager.LoadFromSlot(slot))
                    return new CmdResult(true, $"Loading from slot {slot}...");
                return new CmdResult(false, $"Failed to load slot {slot}.");
            }
            case "slot":
            {
                if (args.Length < 2 || !int.TryParse(args[1], out var slot))
                    return new CmdResult(false, "Usage: dmsave slot <0-3> [name]");

                if (slot < 0 || slot > SaveSlotManager.SlotCount)
                    return new CmdResult(false, $"Slot must be 0-{SaveSlotManager.SlotCount}.");

                var name = args.Length >= 3 ? string.Join(" ", args.Skip(2)) : "";
                if (SaveSlotManager.SaveToSlot(slot, name))
                    return new CmdResult(true, $"Saved to slot {slot}" + (string.IsNullOrEmpty(name) ? "." : $" ({name})."));
                return new CmdResult(false, $"Failed to save to slot {slot}. Is a run in progress?");
            }
            default:
                return new CmdResult(false, $"Unknown subcommand: '{sub}'. Use: quick, load, slot");
        }
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
            return CompleteArgument(SubCmds, Array.Empty<string>(), args.FirstOrDefault() ?? "");

        var sub = args[0].ToLowerInvariant();
        if ((sub == "load" || sub == "slot") && args.Length == 2)
        {
            var slots = Enumerable.Range(0, SaveSlotManager.SlotCount + 1).Select(i => i.ToString()).ToList();
            return CompleteArgument(slots, new[] { args[0] }, args[1]);
        }

        return base.GetArgumentCompletions(player, args);
    }
}
