function Get-Sts2DirFromLocalProps {
  param(
    [Parameter(Mandatory)]
    [string]$RepoRoot
  )

  $lp = Join-Path $RepoRoot "local.props"
  if (-not (Test-Path $lp)) { return $null }

  [xml]$doc = Get-Content -Path $lp -Raw
  $n = ($doc.Project.PropertyGroup | Where-Object { $_.Sts2Dir } | Select-Object -First 1).Sts2Dir
  if ($n -and (Test-Path $n)) { return $n }

  return $null
}

function Resolve-Sts2Directory {
  param(
    [Parameter(Mandatory)]
    [string]$RepoRoot
  )

  $fromProps = Get-Sts2DirFromLocalProps -RepoRoot $RepoRoot
  if ($fromProps) { return $fromProps }

  . (Join-Path $PSScriptRoot "dotenv.ps1")
  . (Join-Path $PSScriptRoot "steam.ps1")
  Import-DotEnv -Path (Join-Path $RepoRoot ".env")

  $d = Resolve-Sts2Dir
  if (-not $d) {
    throw "Sts2Dir not found. Run: make init  (or set STS2_DIR in .env)."
  }
  return $d
}
