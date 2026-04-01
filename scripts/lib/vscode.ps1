function Setup-DebugLaunch {
  param([string]$Sts2Dir)

  $appIdPath = Join-Path $Sts2Dir "steam_appid.txt"
  $debugBat  = Join-Path $Sts2Dir "launch_debug.bat"

  try {
    Set-Content -Path $appIdPath -Value "2868840" -Encoding ASCII -NoNewline
    Set-Content -Path $debugBat -Encoding ASCII -Value "@echo off`r`n""%~dp0SlayTheSpire2.exe"" --log --rendering-driver opengl3 %*"
    Write-Host ("Generated " + $debugBat)
  } catch {
    Write-Warning ("Could not write debug files to STS2 dir (try running as admin): " + $_)
  }
}

function Write-VSCodeFiles {
  param(
    [string]$Root,
    [string]$Sts2Dir
  )

  function Escape-Json([string]$Value) {
    return $Value.Replace('\', '\\').Replace('"', '\"')
  }

  $vscodeDir = Join-Path $Root ".vscode"
  New-Item -ItemType Directory -Force -Path $vscodeDir | Out-Null

  $debugBat = Escape-Json (Join-Path $Sts2Dir "launch_debug.bat")

  $tasksJson = @"
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "compile-dll",
      "type": "shell",
      "command": "dotnet build DevMode.sln /p:DeployToGame=true",
      "group": {
        "kind": "build",
        "isDefault": true
      },
      "presentation": {
        "clear": true,
        "panel": "shared"
      },
      "problemMatcher": "`$msCompile"
    },
    {
      "label": "export-pck",
      "type": "shell",
      "command": "dotnet",
      "args": ["publish", "/p:DeployToGame=true", "DevMode.csproj"],
      "group": "build",
      "presentation": {
        "clear": true,
        "panel": "shared"
      },
      "problemMatcher": "`$msCompile"
    },
    {
      "label": "deploy",
      "type": "shell",
      "command": "dotnet",
      "args": [
        "msbuild",
        "DevMode.csproj",
        "-t:DeployRepoBuildToMods",
        "-p:DeployFromRepoBuild=true"
      ],
      "group": "build",
      "presentation": {
        "clear": true,
        "panel": "shared"
      },
      "problemMatcher": "`$msCompile"
    },
    {
      "label": "build",
      "type": "shell",
      "command": "dotnet",
      "args": ["publish", "DevMode.csproj"],
      "group": "build",
      "presentation": {
        "clear": true,
        "panel": "shared"
      },
      "problemMatcher": "`$msCompile"
    },
    {
      "label": "sync",
      "dependsOn": ["build", "deploy"],
      "dependsOrder": "sequence"
    },
    {
      "label": "launch-sts2",
      "type": "shell",
      "command": "cmd /c '$debugBat'",
      "options": {
        "shell": {
          "executable": "powershell",
          "args": ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command"]
        }
      },
      "presentation": {
        "reveal": "always",
        "panel": "dedicated",
        "clear": true
      },
      "isBackground": true
    },
    {
      "label": "sync-launch",
      "dependsOn": ["sync", "launch-sts2"],
      "dependsOrder": "sequence"
    }
  ]
}
"@

  $launchJson = @"
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "STS2: sync -> launch -> attach",
      "type": "coreclr",
      "request": "attach",
      "processName": "SlayTheSpire2",
      "preLaunchTask": "sync-launch"
    }
  ]
}
"@

  Set-Content -Path (Join-Path $vscodeDir "tasks.json")  -Encoding UTF8 -Value $tasksJson.Trim()
  Set-Content -Path (Join-Path $vscodeDir "launch.json") -Encoding UTF8 -Value $launchJson.Trim()
  Write-Host ("Generated " + (Join-Path $vscodeDir "tasks.json"))
  Write-Host ("Generated " + (Join-Path $vscodeDir "launch.json"))
}
