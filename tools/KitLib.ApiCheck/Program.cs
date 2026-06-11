using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KitLib.ApiCheck;

internal sealed record ManifestDocument(
    Dictionary<string, string> Profiles,
    List<TouchpointDocument> Touchpoints);

internal sealed record TouchpointDocument
{
    public string Id { get; init; } = "";
    public string? Type { get; init; }
    public string? Member { get; init; }
    public string? Kind { get; init; }
    public bool Dynamic { get; init; }
    public Dictionary<string, ProfileMemberDocument>? Profiles { get; init; }
    public List<string>? Sources { get; init; }
}

internal sealed record ProfileMemberDocument
{
    public string? Member { get; init; }
}

internal static class Program
{
    static int Main(string[] args)
    {
        string? dllPath = null;
        string? profile = null;
        string? manifestPath = null;

        for (var i = 0; i < args.Length; i++) {
            switch (args[i]) {
                case "--dll" when i + 1 < args.Length:
                    dllPath = args[++i];
                    break;
                case "--profile" when i + 1 < args.Length:
                    profile = args[++i];
                    break;
                case "--manifest" when i + 1 < args.Length:
                    manifestPath = args[++i];
                    break;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    PrintUsage();
                    return 1;
            }
        }

        if (string.IsNullOrWhiteSpace(dllPath)
            || string.IsNullOrWhiteSpace(profile)
            || string.IsNullOrWhiteSpace(manifestPath)) {
            PrintUsage();
            return 1;
        }

        return Run(new FileInfo(dllPath), profile, new FileInfo(manifestPath));
    }

    static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: KitLib.ApiCheck --dll <sts2.dll> --profile stable|beta --manifest eng/api_touchpoints.yaml");
    }

    static int Run(FileInfo dll, string profile, FileInfo manifest)
    {
        if (!dll.Exists) {
            Console.Error.WriteLine($"DLL not found: {dll.FullName}");
            return 1;
        }
        if (!manifest.Exists) {
            Console.Error.WriteLine($"Manifest not found: {manifest.FullName}");
            return 1;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        ManifestDocument doc;
        try {
            doc = deserializer.Deserialize<ManifestDocument>(File.ReadAllText(manifest.FullName))
                ?? new ManifestDocument(new Dictionary<string, string>(), []);
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"Failed to parse manifest: {ex.Message}");
            return 1;
        }

        Assembly asm;
        try {
            asm = Assembly.LoadFrom(dll.FullName);
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"Failed to load assembly: {ex.Message}");
            return 1;
        }

        var fails = new List<string>();
        var warns = new List<string>();
        var skipped = 0;
        var checkedCount = 0;

        foreach (var tp in doc.Touchpoints) {
            if (tp.Dynamic) {
                skipped++;
                warns.Add($"[SKIP] {profile} {tp.Id} (HarmonyTargetMethod — runtime resolved)");
                continue;
            }

            if (string.IsNullOrWhiteSpace(tp.Type) || string.IsNullOrWhiteSpace(tp.Member)) {
                skipped++;
                continue;
            }

            var member = ResolveMemberName(tp, profile);
            if (!TryResolveType(asm, tp.Type!, out var resolvedType)) {
                fails.Add(FormatFail(profile, tp, member, $"type not found: {tp.Type}"));
                continue;
            }

            if (!MemberExists(resolvedType!, member, tp.Kind ?? "method")) {
                fails.Add(FormatFail(profile, tp, member, "member missing"));
                continue;
            }

            checkedCount++;
        }

        Console.WriteLine($"Profile: {profile}");
        Console.WriteLine($"DLL: {dll.FullName}");
        if (doc.Profiles.TryGetValue(profile, out var pinned))
            Console.WriteLine($"Pinned game version: {pinned}");
        Console.WriteLine($"Checked: {checkedCount}, skipped: {skipped}, failed: {fails.Count}");

        foreach (var w in warns)
            Console.WriteLine(w);
        foreach (var f in fails)
            Console.Error.WriteLine(f);

        return fails.Count == 0 ? 0 : 1;
    }

    static string ResolveMemberName(TouchpointDocument tp, string profile)
    {
        if (tp.Profiles != null
            && tp.Profiles.TryGetValue(profile, out var alias)
            && !string.IsNullOrWhiteSpace(alias.Member))
            return alias.Member!;
        return tp.Member!;
    }

    static string FormatFail(string profile, TouchpointDocument tp, string member, string reason)
    {
        var src = tp.Sources is { Count: > 0 } ? tp.Sources[0] : "?";
        return $"[FAIL] {profile} {tp.Id} ({member}) — {reason} [{src}]";
    }

    static bool TryResolveType(Assembly asm, string typeName, out Type? resolved)
    {
        resolved = asm.GetType(typeName, throwOnError: false, ignoreCase: false);
        if (resolved != null)
            return true;

        var shortName = typeName.Contains('.') ? typeName.Split('.')[^1] : typeName;
        foreach (var t in asm.GetTypes()) {
            if (string.Equals(t.Name, shortName, StringComparison.Ordinal)) {
                resolved = t;
                return true;
            }
        }
        return false;
    }

    static bool MemberExists(Type type, string member, string kind)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        return kind.ToLowerInvariant() switch {
            "property" => type.GetProperty(member, flags) != null,
            "field" => type.GetField(member, flags) != null,
            _ => type.GetMethods(flags).Any(m => string.Equals(m.Name, member, StringComparison.Ordinal))
                 || type.GetMethod(member, flags) != null,
        };
    }
}
