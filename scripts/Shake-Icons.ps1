<#
.SYNOPSIS
  Pre-build icon tree-shaker for DevMode.
  Scans src/**/*.cs for MdiIcon.XxxYyy references, extracts matching icons
  from icons/mdi/icons.json, writes src/Icons/mdi-used.json.

.DESCRIPTION
  Run before build:  pwsh scripts/Shake-Icons.ps1
  MSBuild calls this automatically via the ShakeIcons target.
#>
param(
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$FullJson = "",
    [string]$OutJson  = ""
)

$ErrorActionPreference = 'Stop'

if (-not $FullJson) { $FullJson = Join-Path $RepoRoot "icons\mdi\icons.json" }
if (-not $OutJson)  { $OutJson  = Join-Path $RepoRoot "src\Icons\mdi-used.json" }

if (-not (Test-Path $FullJson)) {
    Write-Error "icons.json not found at $FullJson`nPlace @iconify-json/mdi icons.json at: icons/mdi/icons.json"
    exit 1
}

# ── Helper: PascalCase -> kebab-case ──
function ConvertTo-Kebab([string]$pascal) {
    $sb = [System.Text.StringBuilder]::new()
    for ($i = 0; $i -lt $pascal.Length; $i++) {
        $c = $pascal[$i]
        if ([char]::IsUpper($c)) {
            if ($i -gt 0) { [void]$sb.Append('-') }
            [void]$sb.Append([char]::ToLowerInvariant($c))
        } else {
            [void]$sb.Append($c)
        }
    }
    $sb.ToString()
}

# ── 1. Scan .cs files ──
$srcDir = Join-Path $RepoRoot "src"
$usagePattern = [regex]'(?<![\w.])MdiIcon\.([A-Z][A-Za-z0-9]+)(?!\s*\()'
$defPattern   = [regex]'static\s+readonly\s+MdiIcon\s+([A-Z][A-Za-z0-9]+)\s*=\s*new\("([^"]+)"\)'
$getPattern   = [regex]'MdiIcon\.Get\(\s*"([^"]+)"'

$usedPascal   = [System.Collections.Generic.HashSet[string]]::new()
$definedIcons = [System.Collections.Generic.Dictionary[string,string]]::new()

Get-ChildItem $srcDir -Filter *.cs -Recurse | ForEach-Object {
    $lines = Get-Content $_.FullName
    foreach ($line in $lines) {
        # Collect definitions (static readonly MdiIcon Xxx = new("xxx"))
        $dm = $defPattern.Match($line)
        if ($dm.Success) {
            $definedIcons[$dm.Groups[1].Value] = $dm.Groups[2].Value
        }

        # Collect usages (skip comments and definition lines)
        $trimmed = $line.TrimStart()
        if ($trimmed.StartsWith("//") -or $trimmed.StartsWith("///") -or
            $trimmed.StartsWith("*") -or $trimmed.StartsWith("/*")) { continue }
        if ($trimmed -match 'static\s+readonly\s+MdiIcon') { continue }

        foreach ($m in $usagePattern.Matches($line)) {
            $name = $m.Groups[1].Value
            if ($name -in @('Name', 'IsAvailable', 'Texture')) { continue }
            [void]$usedPascal.Add($name)
        }
    }
}

# ── 2. Build kebab map ──
$kebabMap = @{}

# Resolve PascalCase usages via definitions
foreach ($p in $usedPascal) {
    if ($definedIcons.ContainsKey($p)) {
        $kebabMap[$definedIcons[$p]] = $p
    } else {
        $kebabMap[(ConvertTo-Kebab $p)] = $p
    }
}

# Scan for direct MdiIcon.Get("kebab-name") calls (skip comments)
Get-ChildItem $srcDir -Filter *.cs -Recurse | ForEach-Object {
    $lines = Get-Content $_.FullName
    foreach ($line in $lines) {
        $trimmed = $line.TrimStart()
        if ($trimmed.StartsWith("//") -or $trimmed.StartsWith("///") -or
            $trimmed.StartsWith("*") -or $trimmed.StartsWith("/*")) { continue }
        foreach ($m in $getPattern.Matches($line)) {
            $kebab = $m.Groups[1].Value
            if (-not $kebabMap.ContainsKey($kebab)) {
                $kebabMap[$kebab] = "Get(`"$kebab`")"
            }
        }
    }
}

# Fallback: if no usages found, bundle all defined icons
if ($kebabMap.Count -eq 0 -and $definedIcons.Count -gt 0) {
    Write-Host "No explicit usages found - bundling all $($definedIcons.Count) defined icons"
    foreach ($kv in $definedIcons.GetEnumerator()) {
        $kebabMap[$kv.Value] = $kv.Key
    }
}

Write-Host "Found $($kebabMap.Count) icon(s) to bundle"
foreach ($k in ($kebabMap.Keys | Sort-Object)) {
    Write-Host "  MdiIcon.$($kebabMap[$k])  ->  mdi:$k"
}

# ── 3. Parse full icons.json and extract used icons ──
$full = Get-Content $FullJson -Raw | ConvertFrom-Json
$prefix = $full.prefix
$viewBox = if ($full.PSObject.Properties['height']) { $full.height } else { 24 }

$extracted = [ordered]@{}
$missing = @()

foreach ($kebab in ($kebabMap.Keys | Sort-Object)) {
    if ($full.icons.PSObject.Properties[$kebab]) {
        $icon = $full.icons.$kebab
        $entry = [ordered]@{ body = $icon.body }
        if ($icon.PSObject.Properties['width'])  { $entry.width  = $icon.width }
        if ($icon.PSObject.Properties['height']) { $entry.height = $icon.height }
        $extracted[$kebab] = $entry
    } else {
        $missing += "  MdiIcon.$($kebabMap[$kebab]) -> mdi:$kebab (NOT FOUND)"
    }
}

if ($missing.Count -gt 0) {
    Write-Warning "$($missing.Count) icon(s) not found in icons.json:"
    $missing | ForEach-Object { Write-Warning $_ }
}

# ── 4. Write minimal output ──
$output = [ordered]@{
    prefix  = $prefix
    viewBox = $viewBox
    icons   = $extracted
}

$outDir = Split-Path $OutJson -Parent
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

$output | ConvertTo-Json -Depth 5 -Compress:$false | Set-Content $OutJson -Encoding UTF8

$totalIcons = ($full.icons.PSObject.Properties | Measure-Object).Count
Write-Host "`nWrote $($extracted.Count) icon(s) to $OutJson"
Write-Host "Full set: $totalIcons icons -> trimmed to $($extracted.Count)"
