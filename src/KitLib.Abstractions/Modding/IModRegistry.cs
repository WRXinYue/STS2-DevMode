namespace KitLib.Abstractions.Modding;

/// <summary>All mods the game scanned (<c>ModManager.Mods</c>), including disabled/failed.</summary>
public interface IModRegistry {
    IReadOnlyList<KitLibModEntry> GetAllEntries();

    KitLibModEntry? TryGet(string id, ModEntrySource source);
}
