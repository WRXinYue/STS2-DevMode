using MegaCrit.Sts2.Core.Models;

namespace DevMode.Companion;

public sealed record CompanionInfo(
    ulong NetId,
    ModelId CharacterId,
    bool IsAiDriven,
    bool IsAlive);
