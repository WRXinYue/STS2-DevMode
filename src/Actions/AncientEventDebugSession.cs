namespace DevMode.Actions;

/// <summary>One-shot flags consumed when an ancient event room generates options.</summary>
internal static class AncientEventDebugSession
{
    /// <summary>
    /// When set, the next <c>Darv.GenerateInitialOptions</c> uses this instead of <c>Rng.NextBool()</c>:
    /// <c>true</c> = 2 boss relics + dusty tome; <c>false</c> = 3 boss relics only.
    /// </summary>
    internal static bool? PendingDarvDustyTomeBranch { get; set; }

    internal static bool ResolveDarvBranch(MegaCrit.Sts2.Core.Random.Rng rng)
    {
        if (PendingDarvDustyTomeBranch is bool forced)
            return forced;
        return rng.NextBool();
    }

    internal static void ClearPendingDarvBranch() => PendingDarvDustyTomeBranch = null;
}
