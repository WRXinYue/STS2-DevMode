function Import-DotEnv {
  param(
    [string]$Path
  )

  if (-not $Path) { return }
  if (-not (Test-Path -LiteralPath $Path)) { return }

  $lines = Get-Content -LiteralPath $Path
  foreach ($line in $lines) {
    if (-not $line) { continue }
    $trimmed = $line.Trim()
    if (-not $trimmed) { continue }
    if ($trimmed.StartsWith("#")) { continue }

    $idx = $trimmed.IndexOf("=")
    if ($idx -lt 1) { continue }

    $key = $trimmed.Substring(0, $idx).Trim()
    if (-not $key) { continue }

    $value = $trimmed.Substring($idx + 1)
    if ($null -eq $value) { $value = "" }
    $value = $value.Trim()

    if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
      if ($value.Length -ge 2) { $value = $value.Substring(1, $value.Length - 2) }
    }

    [Environment]::SetEnvironmentVariable($key, $value, "Process")
  }
}
