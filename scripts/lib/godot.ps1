function Resolve-GodotPath {
  if ($env:GODOT_PATH -and (Test-Path $env:GODOT_PATH)) { return $env:GODOT_PATH }

  $searchRoots = @("C:\tools", "$env:USERPROFILE", "C:\dev")
  foreach ($r in $searchRoots) {
    if (-not (Test-Path $r)) { continue }
    $found = Get-ChildItem -Path $r -Recurse -Depth 3 -ErrorAction SilentlyContinue |
             Where-Object { $_.Name -match "^(MegaDot|Godot_v4\.5\.1).*mono.*\.exe$" } |
             Select-Object -First 1
    if ($found) { return $found.FullName }
  }

  foreach ($name in @("godot.exe", "Godot_mono.exe")) {
    $cmd = Get-Command $name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
  }

  return $null
}

function Resolve-Sts2ModAnalyzersPath {
  if ($env:STS2_ANALYZERS_PATH -and (Test-Path $env:STS2_ANALYZERS_PATH)) { return $env:STS2_ANALYZERS_PATH }

  $searchRoots = @("$env:USERPROFILE", "C:\dev", "Z:\Projects")
  foreach ($r in $searchRoots) {
    if (-not (Test-Path $r)) { continue }
    $found = Get-ChildItem -Path $r -Recurse -Depth 6 -Filter "Sts2ModAnalyzers.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) { return $found.FullName }
  }

  return $null
}
