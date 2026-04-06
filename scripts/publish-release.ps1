<#
.SYNOPSIS
    Build the mod and publish a GitHub Release for the given version.

.PARAMETER Version
    The version to release, e.g. 0.2.0
    If omitted, auto-detected from DevMode.json.

.EXAMPLE
    scripts/publish-release.ps1 -Version 0.2.0
    make publish VERSION=0.2.0
#>
param(
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

# ── Resolve version from DevMode.json if not provided ────────────────────────

if (-not $Version) {
    $manifest = Get-Content -LiteralPath "DevMode.json" -Raw | ConvertFrom-Json
    $Version = $manifest.version
    Write-Host "Version auto-detected from DevMode.json: $Version"
}

# ── Preflight checks ─────────────────────────────────────────────────────────

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) not found. Install: winget install --id GitHub.cli"
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet not found. Make sure .NET SDK is on PATH."
}

# ── Build + zip (reuse make zip) ───────────────────────────────────────────

Write-Host "Building and packaging..."
& make zip
if ($LASTEXITCODE -ne 0) { throw "make zip failed" }

$zipName = "DevMode-v$Version.zip"
$zipPath = "build\$zipName"

if (-not (Test-Path $zipPath)) {
    throw "Zip not found: $zipPath"
}

# ── Extract release notes from CHANGELOG.md ──────────────────────────────────

function Get-ChangelogSection([string]$File, [string]$Ver) {
    if (-not (Test-Path -LiteralPath $File)) { return "" }
    $lines  = [System.IO.File]::ReadAllLines($File, [System.Text.Encoding]::UTF8)
    $found  = $false
    $result = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $lines) {
        if ($line -match "^## \[$([regex]::Escape($Ver))\]") { $found = $true; continue }
        if ($found -and $line -match "^## \[") { break }
        if ($found) { $result.Add($line) }
    }
    while ($result.Count -gt 0 -and $result[0].Trim() -eq "")              { $result.RemoveAt(0) }
    while ($result.Count -gt 0 -and $result[$result.Count - 1].Trim() -eq "") { $result.RemoveAt($result.Count - 1) }
    return $result -join "`n"
}

$notesEN = Get-ChangelogSection -File "CHANGELOG.md" -Ver $Version
$notesZH = Get-ChangelogSection -File "CHANGELOG.zh-CN.md" -Ver $Version

if (-not $notesEN -and -not $notesZH) {
    Write-Warning "No changelog section found for [$Version] - release will have no notes."
    $notes = "Release $Version"
} else {
    $parts = @()
    if ($notesEN) { $parts += $notesEN }
    if ($notesZH) { $parts += "---`n`n$notesZH" }
    $notes = $parts -join "`n`n"
}

$notesFile = [System.IO.Path]::GetTempFileName() + ".md"
[System.IO.File]::WriteAllText($notesFile, $notes, [System.Text.Encoding]::UTF8)

# ── Create GitHub Release ─────────────────────────────────────────────────────

$tag    = "v$Version"
$assets = @($zipPath)

Write-Host "Creating GitHub Release $tag..."
Write-Host "  Assets: $($assets -join ', ')"

$ErrorActionPreference = "SilentlyContinue"
& gh release delete $tag --yes 2>$null
$ErrorActionPreference = "Stop"
& gh release create $tag @assets --title $tag --notes-file $notesFile
$exitCode = $LASTEXITCODE

Remove-Item -LiteralPath $notesFile -Force -ErrorAction SilentlyContinue

if ($exitCode -ne 0) { throw "gh release create failed" }

Write-Host "Done! GitHub Release $tag published."
