#!/usr/bin/env dotnet-script
// IconTreeShaker.csx — Pre-build script that scans .cs files for MdiIcon.XxxYyy
// references and extracts only those icons from the full @iconify-json/mdi icons.json.
// Output: src/Icons/mdi-used.json  (embedded as resource at build time)
//
// Usage:  dotnet script scripts/IconTreeShaker.csx
//    or:  pwsh scripts/Shake-Icons.ps1   (wrapper)

using System.Text.Json;
using System.Text.RegularExpressions;

var repoRoot  = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."));
var srcDir    = Path.Combine(repoRoot, "src");
var fullJson  = Path.Combine(repoRoot, "icons", "mdi", "icons.json");
var outJson   = Path.Combine(repoRoot, "src", "Icons", "mdi-used.json");

if (!File.Exists(fullJson))
{
    Console.Error.WriteLine($"ERROR: icons.json not found at {fullJson}");
    Console.Error.WriteLine("Place the @iconify-json/mdi icons.json at: icons/mdi/icons.json");
    Environment.Exit(1);
}

// 1. Scan all .cs files for MdiIcon.XxxYyy references
var pattern = new Regex(@"MdiIcon\.([A-Z][A-Za-z0-9]+)", RegexOptions.Compiled);
var usedPascal = new HashSet<string>();

foreach (var cs in Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories))
{
    var text = File.ReadAllText(cs);
    foreach (Match m in pattern.Matches(text))
        usedPascal.Add(m.Groups[1].Value);
}

if (usedPascal.Count == 0)
{
    Console.WriteLine("No MdiIcon.Xxx references found in source. Generating empty icon set.");
}

// 2. Convert PascalCase → kebab-case (e.g. "AccountCheck" → "account-check")
static string ToKebab(string pascal)
{
    var sb = new System.Text.StringBuilder();
    for (int i = 0; i < pascal.Length; i++)
    {
        char c = pascal[i];
        if (char.IsUpper(c))
        {
            if (i > 0) sb.Append('-');
            sb.Append(char.ToLowerInvariant(c));
        }
        else
        {
            sb.Append(c);
        }
    }
    return sb.ToString();
}

var kebabToField = new Dictionary<string, string>();
foreach (var p in usedPascal)
    kebabToField[ToKebab(p)] = p;

Console.WriteLine($"Found {usedPascal.Count} unique icon references:");
foreach (var k in kebabToField.Keys.OrderBy(x => x))
    Console.WriteLine($"  MdiIcon.{kebabToField[k]}  →  mdi:{k}");

// 3. Read full icons.json and extract only used icons
using var doc = JsonDocument.Parse(File.ReadAllText(fullJson));
var root = doc.RootElement;

var prefix = root.GetProperty("prefix").GetString();
var allIcons = root.GetProperty("icons");

var extracted = new Dictionary<string, JsonElement>();
var missing = new List<string>();

foreach (var (kebab, field) in kebabToField)
{
    if (allIcons.TryGetProperty(kebab, out var iconEl))
        extracted[kebab] = iconEl;
    else
        missing.Add($"  MdiIcon.{field} → mdi:{kebab} (NOT FOUND)");
}

if (missing.Count > 0)
{
    Console.Error.WriteLine($"\nWARNING: {missing.Count} icon(s) not found in icons.json:");
    foreach (var m in missing)
        Console.Error.WriteLine(m);
}

// 4. Build minimal output JSON
int viewBox = 24; // MDI default
if (root.TryGetProperty("width", out var wEl))  viewBox = wEl.GetInt32();
if (root.TryGetProperty("height", out var hEl)) viewBox = hEl.GetInt32();

using var ms = new MemoryStream();
using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
{
    w.WriteStartObject();
    w.WriteString("prefix", prefix);
    w.WriteNumber("viewBox", viewBox);
    w.WriteStartObject("icons");
    foreach (var (kebab, el) in extracted.OrderBy(x => x.Key))
    {
        w.WriteStartObject(kebab);
        w.WriteString("body", el.GetProperty("body").GetString());
        if (el.TryGetProperty("width", out var iw))  w.WriteNumber("width", iw.GetInt32());
        if (el.TryGetProperty("height", out var ih)) w.WriteNumber("height", ih.GetInt32());
        w.WriteEndObject();
    }
    w.WriteEndObject();
    w.WriteEndObject();
}

Directory.CreateDirectory(Path.GetDirectoryName(outJson)!);
File.WriteAllBytes(outJson, ms.ToArray());

Console.WriteLine($"\nWrote {extracted.Count} icon(s) to {outJson}");
Console.WriteLine($"Full set: {allIcons.EnumerateObject().Count()} icons → trimmed to {extracted.Count}");
