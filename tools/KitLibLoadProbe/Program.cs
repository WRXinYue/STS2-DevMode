using System.Reflection;
using System.Runtime.Loader;

var modDir = args.Length > 0
    ? args[0]
    : @"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\KitLib";
var kitPath = Path.Combine(modDir, "KitLib.dll");
var modInitMarker = Path.Combine(Path.GetTempPath(), "kitlib-modinit.txt");

Console.WriteLine($"Mod dir: {modDir}");
foreach (var dll in new[] { "KitLib.dll", "KitLib.Abstractions.dll", "Semver.dll", "Microsoft.Extensions.Primitives.dll" }) {
    var p = Path.Combine(modDir, dll);
    Console.WriteLine($"  {dll}: {(File.Exists(p) ? "yes" : "MISSING")}");
}

static void ClearModInitMarker(string path) {
    try {
        if (File.Exists(path))
            File.Delete(path);
    } catch {
        // ignore
    }
}

static void ReportModInit(string label, string path) {
    Console.WriteLine($"modinit after {label}: {(File.Exists(path) ? "RAN" : "DID NOT RUN")}");
}

static void Probe(string label, AssemblyLoadContext alc, string kitPath) {
    Console.WriteLine($"\n=== {label} ===");
    var asm = alc.LoadFromAssemblyPath(Path.GetFullPath(kitPath));
    Console.WriteLine($"KitLib ALC: {AssemblyLoadContext.GetLoadContext(asm)?.Name ?? "(null)"}");
    try {
        var count = asm.GetTypes().Length;
        Console.WriteLine($"GetTypes OK: {count} types (Abstractions load succeeded)");
    }
    catch (ReflectionTypeLoadException ex) {
        var abstractionsMissing = ex.LoaderExceptions?.Any(e =>
            e is FileNotFoundException fnf && fnf.FileName?.StartsWith("KitLib.Abstractions", StringComparison.Ordinal) == true) == true;
        Console.WriteLine($"GetTypes: Abstractions missing={abstractionsMissing}, other loader errors={ex.LoaderExceptions?.Count(e => e != null && !(e is FileNotFoundException fnf && fnf.FileName?.StartsWith("sts2", StringComparison.Ordinal) == true))}");
    }
}

ClearModInitMarker(modInitMarker);
Probe("collectible ALC", new AssemblyLoadContext("sts2-mod-collectible", isCollectible: true), kitPath);
ReportModInit("collectible", modInitMarker);

ClearModInitMarker(modInitMarker);
Probe("non-collectible ALC", new AssemblyLoadContext("sts2-mod-sim", isCollectible: false), kitPath);
ReportModInit("non-collectible", modInitMarker);

ClearModInitMarker(modInitMarker);
Probe("Default ALC", AssemblyLoadContext.Default, kitPath);
ReportModInit("Default", modInitMarker);
