Update changelogs with recent changes. Arguments (optional): a brief description hint, or leave empty to auto-detect from git log.

Steps:

1. Run `git log` since the last version tag to see what commits haven't been documented yet:
   ```
   git log $(git describe --tags --abbrev=0)..HEAD --oneline
   ```

2. Read the current changelogs to understand the existing format and what's already recorded under `## [Unreleased]`:
   - `CHANGELOG.md`
   - `CHANGELOG.zh-CN.md`

3. Categorize the new player-facing changes under `## [Unreleased]` in both files:
   - **Added** — new features visible to the player
   - **Changed** — behavior changes or balance tweaks
   - **Fixed** — bug fixes
   - Skip: refactors, code style, docs, internal tooling, CI
   - Write English entries in `CHANGELOG.md` and Chinese entries in `CHANGELOG.zh-CN.md`.

4. Update both `CHANGELOG.md` and `CHANGELOG.zh-CN.md`.

5. Commit with message: `docs: 更新 Unreleased changelog`
