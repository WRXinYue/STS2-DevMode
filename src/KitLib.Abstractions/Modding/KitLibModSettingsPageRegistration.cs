namespace KitLib.Abstractions.Modding;

/// <summary>
/// Native KitLib mod settings page (no Ritsu reflection). <see cref="BuildBody"/> returns a Godot <c>Control</c> at runtime.
/// </summary>
public sealed class KitLibModSettingsPageRegistration {
    public required string ModId { get; init; }
    public required string PageId { get; init; }
    /// <summary>English fallback when <see cref="TitleKey"/> is unset or missing from locale files.</summary>
    public required string Title { get; init; }
    /// <summary>Resolved at UI refresh time so tab labels follow the active game locale.</summary>
    public string? TitleKey { get; init; }
    public int SortOrder { get; init; }
    public required Func<object> BuildBody { get; init; }
}
