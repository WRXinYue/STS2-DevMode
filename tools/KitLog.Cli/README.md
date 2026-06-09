# KitLog CLI

Cross-platform log tail for KitLib session files (`mod_data/KitLib/instances/{pid}/session.log`).

Optional companion to the in-game log viewer — distributed separately from the main mod zip (like `KitLib.Mcp`).

## Build

```bash
make build-kitlog
# or
dotnet publish tools/KitLog.Cli/KitLog.Cli.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output: `build/tools/KitLog.Cli/<rid>/publish/kitlog` (or `kitlog.exe` on Windows).

Package: `make zip-kitlog` → `build/KitLog.Cli-vX.X.X-<rid>.zip`

## Commands

```bash
kitlog list
kitlog path [--pid 12345]
kitlog tail -f --tail 40 --filter ai --pid 12345
kitlog tail -f --tail 40 --sync-viewer --pid 12345
kitlog tail --file "C:/path/to/session.log" --level warn
```

### Filters

- `--filter ai` — preset for `[AutoPlay|AiHost|MpAi|LanLocal|Companion]`
- `--filter` also accepts a .NET regex

### Log locations

KitLog scans STS2 user data:

- Windows: `%APPDATA%/SlayTheSpire2/steam/*/mod_data/KitLib/instances/*/session.log`
- Linux: `~/.local/share/SlayTheSpire2/...` (and common Flatpak paths)
- macOS: `~/Library/Application Support/SlayTheSpire2/...`

Falls back to `logs/godot.log` when no instance log exists.

## In-game integration

The in-game log viewer **kitlog** button launches `kitlog tail -f --sync-viewer` so the terminal mirrors viewer filters (level, text, mod source, suppress rules). Filter changes in-game are written to `instances/{pid}/log-viewer-filter.json` and picked up live by `kitlog`.

The AI panel **Open kitlog tail** button uses `--filter ai` instead. Both launch `kitlog` from `PATH` or `mods/KitLib/tools/`.

## Content mods

Use `KitLog.Info("MyMod", "message")` from `KitLib.dll` at runtime (writes to game log + session mirror).
