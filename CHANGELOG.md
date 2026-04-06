# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-04-07

### Added

- Sidebar panel with categorized sections: Player, Inventory, Status, Enemy, and Game.
  - Player: invincible, infinite block/energy/stars, defense multiplier.
  - Inventory: edit gold, gold multiplier, free shop, edit energy cap, edit potion slots.
  - Status: always reward potion, always upgrade card rewards, max card reward rarity, max score, score multiplier.
  - Enemy: freeze enemies, one-hit kill, damage multiplier.
  - Game: unknown nodes → treasure, game speed.
- Runtime stat modifiers: god mode, kill all enemies, infinite energy, always player turn, draw to hand limit, extra draw each turn, auto-act friendly monsters, negate debuffs.
- Stat locks: lock gold, current/max HP, current/max energy, stars, orb slots.
- Map rewrite: force all rooms to chest, elite, or boss; keep final boss option.
- Power select panel with 4 target modes (self, all enemies, specific, allies).
- Potion select and event select panels.
- Card editor: edit base cost, replay, damage, block, exhaust, ethereal, unplayable, enchantments.
- Preset manager: save/load/export/import loadout presets.
- Console command reference UI with search, native and DevMode command sections.
- 10 new console command modules (card, cheat, enemy, event, game, potion, power, relic, runtime, save).
- Redesigned DevPanel as Apple-style icon rail with unified overlay system and slide-down animations.
- Iconify MDI adapter with build-time tree-shaking; replaced all text icons with real MDI icons.
- Click-to-lock toggle for sidebar panel.
- Always-enable DevMode toggle for normal (non-dev) runs.
- New test button in save/load panel.
- Multiplayer compatibility patch (filter mod signatures, normalize ModelDb hash).
- Asset warmup service with frame-budgeted texture/scene preloader.
- Cross-version API compatibility layer (Sts2ApiCompat).

### Changed

- Removed StartingGold override; uses game default gold instead.

### Fixed

- Infinite block not refilling correctly after loss.
- Potion slot removal and stat lock values not updating live.
- Overlay panels stacking on tab switch instead of closing.
- UTF-8 encoding for changelog read/write to prevent Chinese garbling.

## [0.0.1] - 2026-04-06

### Added

- Developer Mode panel accessible from the main menu.
- Customizable relics, cards, gold, and encounter selection for testing.
- Enemy encounter system with unified select UI, combat monster spawning, and idle animation preview.
- i18n support with English and Simplified Chinese localization.
- STS2AI integration panel with AI control, speed, and animation controls.
