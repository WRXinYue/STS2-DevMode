namespace DevMode.Actions;

/// <param name="PinFirstOptionToken">
/// Matches <see cref="MegaCrit.Sts2.Core.Events.EventOption.TextKey"/> substring
/// (e.g. <c>DUSTY_TOME</c>), same as vanilla <c>ancient DARV DUSTY_TOME</c>.
/// </param>
/// <param name="DarvIncludeDustyTome">
/// <c>true</c> = 2 boss relics + dusty tome; <c>false</c> = 3 boss relics; <c>null</c> = vanilla RNG.
/// </param>
internal readonly record struct AncientEventEnterRequest(
    string? PinFirstOptionToken = null,
    bool? DarvIncludeDustyTome = null);
