using System;
using System.Linq;
using DevMode.Actions;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace DevMode.Commands;

public class DmEventConsoleCmd : AbstractConsoleCmd {
    public override string CmdName => "dmevent";
    public override string Args => "<force|list> [eventId]";
    public override string Description => "[DevMode] Force events or list all event IDs";
    public override bool IsNetworked => false;
    public override bool DebugOnly => false;

    private static readonly string[] SubCmds = { "force", "list" };

    public override CmdResult Process(Player? issuingPlayer, string[] args) {
        if (args.Length < 1)
            return new CmdResult(false, "Usage: dmevent <force|list> [eventId]");

        var sub = args[0].ToLowerInvariant();

        switch (sub) {
            case "list": {
                    var events = EventActions.GetAllEvents().ToList();
                    var names = events.Select(e => ((AbstractModel)e).Id.Entry).OrderBy(n => n);
                    return new CmdResult(true, $"Events ({events.Count}):\n{string.Join(", ", names)}");
                }
            case "force": {
                    if (args.Length < 2)
                        return new CmdResult(false, "Usage: dmevent force <eventId>");

                    var eventId = args[1];
                    var evt = EventActions.GetAllEvents().FirstOrDefault(e =>
                        string.Equals(((AbstractModel)e).Id.Entry, eventId, StringComparison.OrdinalIgnoreCase));
                    if (evt == null)
                        return new CmdResult(false, $"Event not found: '{eventId}'");

                    if (EventActions.TryForceEnterEvent(evt))
                        return new CmdResult(true, $"Forcing event: {eventId}");
                    else
                        return new CmdResult(false, $"Failed to force event: {eventId}. Is a run in progress?");
                }
            default:
                return new CmdResult(false, $"Unknown subcommand: '{sub}'. Use: force, list");
        }
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args) {
        if (args.Length <= 1)
            return CompleteArgument(SubCmds, Array.Empty<string>(), args.FirstOrDefault() ?? "");

        if (args[0].Equals("force", StringComparison.OrdinalIgnoreCase) && args.Length == 2) {
            var ids = EventActions.GetAllEvents().Select(e => ((AbstractModel)e).Id.Entry).ToList();
            return CompleteArgument(ids, new[] { args[0] }, args[1]);
        }

        return base.GetArgumentCompletions(player, args);
    }
}
