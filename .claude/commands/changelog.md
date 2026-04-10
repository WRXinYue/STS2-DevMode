Update changelogs with recent changes. Arguments (optional): a brief description hint, or leave empty to auto-detect from git log.

Steps:

1. Run `git log` since the last version tag to see what commits haven't been documented yet:
   ```
   git log $(git describe --tags --abbrev=0)..HEAD --oneline
   ```
   (On Windows PowerShell, use: `git log "$(git describe --tags --abbrev=0)..HEAD" --oneline`.)

2. Read the current changelogs to understand the existing format and what's already recorded under `## [Unreleased]`:
   - Main mod: `CHANGELOG.md`, `CHANGELOG.zh-CN.md`
   - Patch mod (if present): `CHANGELOG.patch.md`, `CHANGELOG.patch.zh-CN.md`

3. Categorize the new player-facing changes under `## [Unreleased]`:
   - **Added** — new features visible to the player
   - **Changed** — behavior changes or balance tweaks
   - **Fixed** — bug fixes
   - Skip: refactors, code style, docs, internal tooling, CI
   - Write English entries in `CHANGELOG.md` and Chinese entries in `CHANGELOG.zh-CN.md` (same meaning, aligned wording).

4. **Writing style (audience: players and mod users, not implementers):**
   - Prefer **what the player notices**: new behavior, fixed symptom, balance outcome.
   - For **Fixed**, say **what was wrong in gameplay terms** (e.g. crash when playing a card, wrong tooltip, synergy not firing)—not how the code was patched (avoid overload names, Harmony patch class names, long internal API explanations).
   - One short line is enough unless the fix needs a minimal in-world clarification (e.g. “empty draw pile no longer summons a vessel”).
   - Technical detail is optional; add at most a brief clause if it helps modders, otherwise omit.

5. Update the appropriate changelog files:
   - Main mod: `CHANGELOG.md` (English), `CHANGELOG.zh-CN.md` (Chinese, 同义翻译，保持一致)
   - Patch mod: `CHANGELOG.patch.md` (English), `CHANGELOG.patch.zh-CN.md` (Chinese, 同义翻译，保持一致)

   **Routing rules — which changelog to write to:**
   - Main mod changelog: general gameplay changes, card balance, bug fixes, new features — anything safe for all audiences
   - Patch mod changelog ONLY: anything involving adult/sex content (card art, animations, CGs, sex toggles), patch-specific mechanics, or any wording that could be flagged as sensitive
   - When in doubt, write to the patch changelog, not the main one
   - Never write sensitive wording into `CHANGELOG.md` or `CHANGELOG.zh-CN.md`

   If patch changelog files do not exist in the repo, skip them; do not create empty placeholder files unless the project already uses that layout.

6. Commit with message:
   - Main mod: `docs: 更新 Unreleased changelog`
   - Patch mod: `docs: 更新 Patch changelog`

When editing an already-released section (e.g. polishing wording for an existing version), use the same player-facing rules; the commit message above still applies if the only edits are changelog documentation.
