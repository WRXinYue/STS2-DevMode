"""NuGet pack/push helpers for DevMode release scripts."""

from __future__ import annotations

import os
import subprocess
from pathlib import Path


def resolve_api_key(explicit: str | None = None) -> str:
    key = (explicit or os.environ.get("NUGET_API_KEY") or "").strip()
    if not key:
        raise RuntimeError("NuGet API key missing. Set NUGET_API_KEY in .env or pass --api-key.")
    return key


def resolve_source(explicit: str | None = None) -> str:
    source = (explicit or os.environ.get("NUGET_SOURCE") or "https://api.nuget.org/v3/index.json").strip()
    if not source:
        raise RuntimeError("NuGet source missing. Set NUGET_SOURCE in .env or pass --source.")
    return source


def run_pack(
    repo_root: Path,
    *,
    package_version: str,
    configuration: str = "Release",
) -> Path:
    dist_dll = repo_root / "build" / "dist" / "KitLib" / "KitLib.dll"
    if not dist_dll.is_file():
        raise RuntimeError(f"Mod dist not found: {dist_dll}\nRun make zip first.")

    out_dir = repo_root / "build" / "nuget"
    out_dir.mkdir(parents=True, exist_ok=True)

    cmd = [
        "dotnet",
        "pack",
        str(repo_root / "src" / "KitLib.Core" / "KitLib.Core.csproj"),
        "-c",
        configuration,
        "--no-build",
        "-o",
        str(out_dir),
        "-p:PackKitLib=true",
        f"-p:PackageVersion={package_version}",
    ]
    subprocess.run(cmd, cwd=repo_root, check=True)

    matches = sorted(out_dir.glob("*.nupkg"), key=lambda p: p.stat().st_mtime, reverse=True)
    if not matches:
        raise RuntimeError(f"dotnet pack produced no .nupkg under {out_dir}")
    return matches[0]


def run_push(package: Path, *, source: str, api_key: str) -> None:
    if not package.is_file():
        raise RuntimeError(f"Package not found: {package}")
    subprocess.run(
        [
            "dotnet",
            "nuget",
            "push",
            str(package),
            "--source",
            source,
            "--api-key",
            api_key,
            "--skip-duplicate",
        ],
        check=True,
    )
