# DevMode

**English** | [中文](./README.zh-CN.md)

All-in-one in-game toolkit for Slay the Spire 2 — test builds, cheat, script, and debug mods without leaving the game.

![DevMode](https://raw.githubusercontent.com/WRXinYue/STS2-DevMode/main/assets/devmode.png)

## Panels

### Gameplay & content

- **Cheats** — God mode, infinite energy/block, damage multipliers, enemy freeze, stat locks, map overrides, reward tweaks
- **Cards** — Full card library; filter by type/rarity/cost/pool/character; edit stats; add to any pile; upgrade preview; filters persist across sessions
- **Relics** — Browse and add relics
- **Powers** — Apply powers (self, all enemies, specific, allies); one-click Auto-Apply hooks
- **Potions** — Visual grid; one-click Auto-Apply hooks
- **Enemies** — Replace encounters by room or map node; preview content; idle animation preview
- **Events** — Browse and trigger event flows
- **Rooms** — Inspect and jump between room types
- **Presets** — Save/load combat and run snapshots (hand, deck, relics, etc.)

### Automation & AI

- **Hooks** — Trigger → Condition → Action rules (e.g. add a card on combat start, apply a power on draw)
- **Scripts** — SpireScratch visual scripting (Blockly); live reload via WebSocket
- **AI Host** — Rule-based bot for **solo** runs (map, combat, rewards). Disabled during multiplayer hand-play to avoid desync; use Pseudo Co-op / LAN presets instead (see below)

### Developer & debug

- **Enemy intents** — Live overlay of enemy move intents
- **Combat stats** — Per-combat damage/block/heal breakdown by player
- **Console** — Searchable reference for native and DevMode commands
- **Logs** — In-game log stream with noise-filter rules
- **Harmony analysis** — Inspect active patches; filter by owner; smart summary
- **Frameworks** — Loaded mod framework snapshot
- **Mod feedback** — Export ZIP bug reports (logs, mod list, Harmony dump); privacy mode strips paths

### Utility

- **Save / Load** — Named slots; carry cards/relics/gold into a new seed; slot detail view
- **Manual** — In-game documentation browser
- **Settings** — Theme (Dark / OLED / Light / Warm), game speed, skip animations, rail layout

## Multiplayer & co-op testing (dev)

These features are **opt-in** from DevPanel → **AI Host**. They do not change vanilla solo hand-play or draw speed unless you enable AI / cheats yourself.

| Mode | What it does | When to use |
| --- | --- | --- |
| **AI Host (solo)** | `SimpleStrategy` drives your character locally | Single-player automation |
| **SyncBot** | Simulates remote peer ACKs and default choices on one machine; optional phantom player (NetId 1001) | Host-only co-op smoke tests without a second client |
| **Pseudo Co-op preset** | Hand-play host + AI teammate for phantom/offline peers via action queue | Solo host with simulated teammate |
| **LAN host-drive + AFK** | Host hand-plays local player; AI enqueues combat for connected ENet client; client AFK blocks local combat input; map votes mirrored | Two game instances on one PC (auto preset on dual launch) |

**Dual-instance LAN (recommended):** launch host + client on the same machine → presets apply automatically; host logs `LAN host preset applied`, client logs `AFK client enabled`.

Detailed architecture, verification checklist, and desync history: **[docs/lan-host-drive-afk.md](./docs/lan-host-drive-afk.md)** · [docs index](./docs/README.md)

## Contributing

See **[CONTRIBUTING.md](CONTRIBUTING.md)** for collaboration norms, K&R brace style, formatting commands, and localization, or open an issue / PR on [GitHub](https://github.com/WRXinYue/STS2-DevMode).

## Changelog

See [CHANGELOG.md](https://github.com/WRXinYue/STS2-DevMode/blob/main/CHANGELOG.md) for version history.

## Acknowledgments

- [STS2-KaylaMod](https://github.com/mugongzi520/STS2-KaylaMod)

## License

[MIT](https://github.com/WRXinYue/STS2-DevMode/blob/main/LICENSE)
