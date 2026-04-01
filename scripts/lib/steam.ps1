function Get-SteamRoot {
  $steamRoot = $null
  try { $steamRoot = (Get-ItemProperty -Path "HKCU:\Software\Valve\Steam" -Name "SteamPath" -ErrorAction SilentlyContinue).SteamPath } catch {}
  if (-not $steamRoot) { try { $steamRoot = (Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam" -Name "InstallPath" -ErrorAction SilentlyContinue).InstallPath } catch {} }
  if (-not $steamRoot) { try { $steamRoot = (Get-ItemProperty -Path "HKLM:\SOFTWARE\Valve\Steam" -Name "InstallPath" -ErrorAction SilentlyContinue).InstallPath } catch {} }
  if (-not $steamRoot) {
    try {
      $steamProc = Get-Process steam -ErrorAction SilentlyContinue | Where-Object { $_.Path } | Select-Object -First 1
      if ($steamProc -and $steamProc.Path) {
        $steamRoot = Split-Path -Parent $steamProc.Path
      }
    } catch {}
  }
  return $steamRoot
}

function Get-SteamAppsCandidates {
  $steamRoot = Get-SteamRoot
  $candidates = New-Object System.Collections.Generic.List[string]

  if ($steamRoot) {
    $defaultSteamApps = Join-Path $steamRoot "steamapps"
    $candidates.Add($defaultSteamApps)

    $libraryFile = Join-Path $defaultSteamApps "libraryfolders.vdf"
    if (Test-Path $libraryFile) {
      $content = Get-Content -Path $libraryFile -Raw
      $matches = [regex]::Matches($content, '"path"\s+"([^"]+)"')
      foreach ($m in $matches) {
        $p = $m.Groups[1].Value
        if ($p) {
          $p = $p -replace "\\\\", "\"
          $candidates.Add((Join-Path $p "steamapps"))
        }
      }
    }
  }

  return ($candidates | Where-Object { $_ } | Select-Object -Unique)
}

function Resolve-Sts2Dir {
  if ($env:STS2_DIR -and (Test-Path $env:STS2_DIR)) { return $env:STS2_DIR }

  foreach ($cand in (Get-SteamAppsCandidates)) {
    $probe = Join-Path $cand "common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll"
    if (Test-Path $probe) {
      return (Join-Path $cand "common\Slay the Spire 2")
    }
  }

  return $null
}
