namespace KitLib.Host;

/// <summary>
/// STS2 loads only the main mod DLL; sibling dependencies must be loaded explicitly.
/// </summary>
internal static class ModDependencyLoader {
    internal static void EnsureLoaded() {
        var modDir = Path.GetDirectoryName(typeof(MainFile).Assembly.Location);
        if (string.IsNullOrEmpty(modDir)) {
            MainFile.Logger.Warn("Cannot resolve mod directory for dependency loading.");
            return;
        }

        ModAssemblyLoader.EnsureResolveHook(modDir);

        var abstractions = Path.Combine(modDir, "KitLib.Abstractions.dll");
        if (!File.Exists(abstractions)) {
            MainFile.Logger.Warn($"Missing dependency DLL: {abstractions}");
            return;
        }

        ModAssemblyLoader.LoadFromModPath(abstractions);
        MainFile.Logger.Info("Loaded mod dependency: KitLib.Abstractions");
    }
}
