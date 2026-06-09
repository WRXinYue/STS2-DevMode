using System.Collections.Generic;
using MegaCrit.Sts2.Core.Modding;

namespace KitLib.Modding;

internal static class Sts2ModCatalogDeps {
    internal static string[] CopyDependencies(ModManifest manifest) {
        var deps = manifest.dependencies;
        if (deps == null || deps.Count == 0)
            return [];

        var list = new List<string>(deps.Count);
#if STS2_BETA
        foreach (var dep in deps) {
            if (string.IsNullOrEmpty(dep.id))
                continue;
            list.Add(string.IsNullOrEmpty(dep.minVersion) ? dep.id : $"{dep.id}>={dep.minVersion}");
        }
#else
        foreach (var dep in deps) {
            if (!string.IsNullOrEmpty(dep))
                list.Add(dep);
        }
#endif
        return list.Count == 0 ? [] : list.ToArray();
    }
}
