# Git hooks

Tracked hooks for this repo. To activate them in your local clone, run **once**:

```sh
git config core.hooksPath .githooks
```

That tells git to look here instead of `.git/hooks/`. The setting is per-clone, so each team member runs it once after cloning.

## Hooks

### `pre-commit`

Blocks commits that introduce UTF-8 → Windows-1252 mojibake into:

- Razor views (`**/*.cshtml`)
- Server source (`**/*.cs`)
- Authored client scripts (`**/*.js`)

Excludes build output (`bin/`, `obj/`, `deploy-out/`), JS deps (`node_modules/`), and vendored libraries (`wwwroot/lib/`).

Triggered by DEF-133 (2026-05-03), where corrupted glyphs (`â–¶`, `â€¢`, `â€"`, `â†'`) reached production source via an editor that re-encoded UTF-8 files as Windows-1252.

To bypass for a single commit (rare; investigate first): `git commit --no-verify`.
