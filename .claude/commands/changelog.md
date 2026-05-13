Update changelogs with recent changes. Arguments (optional): a brief description hint, or leave empty to auto-detect from git log.

**Do not `git commit` changelog edits.** Update the markdown files only; the human bundles changelog with the feature/fix commit (or commits separately). Never run a commit whose sole purpose is documentation/changelog.

Steps:

1. Run `git log` since the last version tag to see what commits have not been documented yet:
   ```
   git log $(git describe --tags --abbrev=0)..HEAD --oneline
   ```
   (On Windows PowerShell, use: `git log "$(git describe --tags --abbrev=0)..HEAD" --oneline`.)

2. Read `CHANGELOG.md` and `CHANGELOG.zh-CN.md` for format and what is already under `## [Unreleased]`.

3. Categorize new player-facing changes under `## [Unreleased]`:
   - **Added** — new features visible to the player
   - **Changed** — behavior changes or balance tweaks
   - **Fixed** — bug fixes
   - Skip: refactors, code style, unrelated docs, internal tooling, CI
   - English in `CHANGELOG.md`, Chinese in `CHANGELOG.zh-CN.md` (same meaning, aligned wording).

4. **Writing style (audience: players and mod users, not implementers):**
   - Prefer **what the player notices**: new behavior, fixed symptom, balance outcome.
   - For **Fixed**, describe the symptom in gameplay terms — not patch class names, overloads, or long internal API detail.
   - One short line is enough unless a single clause clarifies an edge case.

5. Edit only `CHANGELOG.md` and `CHANGELOG.zh-CN.md` as needed. Do not create other changelog files unless the repo already uses them.

When editing an already-released section (e.g. wording polish for a shipped version), use the same player-facing rules and still **do not commit** from this command.
