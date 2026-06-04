using MegaCrit.Sts2.Core.Entities.Players;

namespace DevMode.Companion;

public sealed record CompanionSpawnResult(
    bool Ok,
    ulong NetId,
    string? Error,
    Player? Player = null);
