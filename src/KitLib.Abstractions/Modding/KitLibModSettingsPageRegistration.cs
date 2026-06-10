namespace KitLib.Abstractions.Modding;

/// <summary>
/// Native KitLib mod settings page (no Ritsu reflection). <see cref="BuildBody"/> returns a Godot <c>Control</c> at runtime.
/// </summary>
public sealed class KitLibModSettingsPageRegistration {
    public required string ModId { get; init; }
    public required string PageId { get; init; }
    public required string Title { get; init; }
    public int SortOrder { get; init; }
    public required Func<object> BuildBody { get; init; }
}
