Update changelogs with recent changes. Arguments (optional): a brief description hint, or leave empty to auto-detect from git log.

Steps:

1. Run `git log` since the last version tag to see what commits haven't been documented yet:
   ```
   git log $(git describe --tags --abbrev=0)..HEAD --oneline
   ```

2. Read the current changelog to understand the existing format and what's already recorded under `## [Unreleased]`:
   - `CHANGELOG.md`

3. Categorize the new player-facing changes under `## [Unreleased]`:
   - **Added** — new features visible to the player
   - **Changed** — behavior changes or balance tweaks
   - **Fixed** — bug fixes
   - Skip: refactors, code style, docs, internal tooling, CI

4. Update `CHANGELOG.md`.

5. Commit with message: `docs: 更新 Unreleased changelog`
