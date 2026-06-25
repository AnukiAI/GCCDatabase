# Deploy Readiness / Coordination — 2026-06-25

> **STATUS: HOLD — do NOT build the deploy bundle yet.** (Shawn, 2026-06-25: "coordinate with other session" before bundling.)
> Two sessions are editing the same working tree; one is mid-deploy-prep. This note is the single place to converge before a `dotnet publish` (Plesk) zip is built. No bundle exists in `deployment-drops/` yet.

## Gate 2 status (re-run 2026-06-25 16:05) — `GATE 2 FAIL`, but cert-edit is now GREEN
- ✅ **RESOLVED — cert-edit 400 was a GATE TEST BUG, now fixed.** Root cause: the gate hit hardcoded `/Certificate/Edit/1`; `"1"` fails the cert-number route constraint `^[0-9]+-[0-9A-Za-z]+$` (Program.cs:816) → skips `Certificate/Edit/{number}` → default `{id?}` route → `number=null` → `BadRequest` 400. **Fix:** `gate2-predeploy.js` now discovers a real cert number at runtime (`discoverCertNumber`) and substitutes it. Re-run result: **cert-edit PASS for all 4 roles** (analyst/admin → 200 vs real cert `26-01730`); **Section A 16/0, Section B 10/0**. App's Edit was never broken; NOT related to DEF-294.
- 🔴 **Two REAL blockers remain — PRE-EXISTING (also failed the 13:12 run; not introduced by current work):**
  1. **`ext-tile.no-actions-col` (Section A2)** — **INVESTIGATED 2026-06-25: NOT a privilege leak; the gate check is over-strict.** The external tile/list Actions column (and tile cards) contain **only Download PDF + Open-in-new-tab / View Details** — both permitted for external users (they may access their own certs). **No Edit / Delete / Add** is wired anywhere in the external view (internal has Edit at `certificate-tile-view-external.js` cf. internal:1755; external omits it). The `editCertificate` fn is defined + on `window` but attached to no external button, and the server blocks it regardless (`cert-edit external → 401`, verified). So the real RBAC rule (no Add/Edit/Delete for external) **is satisfied**. **Decision needed (product/RBAC, not an objective bug):** (a) external download/view column is intended → **narrow the gate** to forbid only edit/delete/add for external (recommended); or (b) intent is external has NO actions column at all → remove the column + the download/view buttons from the external view (also drop the dead `editCertificate` from the external bundle). Recommend (a). Until decided, this stays a gate red.
  2. **Accessibility (Section C)** — **1 axe-critical + 4 axe-serious** (pre-existing a11y debt). Triaged 2026-06-25:
     | Rule | Impact | Page | Source file | Fix |
     |---|---|---|---|---|
     | `select-name` | 🔴 critical | Create | `Views/Shared/_ProductIdManager.cshtml` (`#filterByType`) | add `aria-label`/`<label>` to the select |
     | `aria-prohibited-attr` | serious | Create/Edit | `Create.cshtml`+`Edit.cshtml` (`#subcategoriesContainer`) | add `role="group"` (or drop `aria-label`) |
     | `color-contrast` | serious | Create | Create form (`<small>Upload a PDF…</small>`) | darken helper-text color |
     | `color-contrast` | serious | Public tiles | shared CSS (`<span>Hide Filters</span>` btn) | meet 4.5:1 contrast |
     | `listitem` ×54 | serious | Public tiles | `CertificateTileView.cshtml` `<ul class="filter-tree" role="group">` | **remove `role="group"`** — it overrides the `<ul>` list role and orphans the `<li>`s; the wrapping `<fieldset>` already groups |
     **Ownership/collision:** the first 3 are on Create/Edit/ProductIdManager — **actively edited by the parallel session (DEF-294/282)**; do NOT edit mid-flight. Hand to the `accessibility-validator` agent once that work lands.
     **✅ The 2 public-tile issues are FIXED (2026-06-25):**
       - **color-contrast** — `.sidebar-collapse-btn` ("Hide Filters") bg `--blue-primary`→`--blue-dark` (`certificatetileview.css`); `.disclaimer` text `#888888`→`#595959` + link `--blue-primary`→`--blue-dark` (`main.css`). **Live-verified via axe: public-page contrast violations = 0.** (Note: also flagged 3 disclaimer nodes the original sample didn't show.) Needs Carly visual sign-off (buttons/links now darker blue).
       - **listitem ×54** — removed the redundant `role="group"` from `<ul class="filter-tree">` in `CertificateTileView.cshtml` (it overrode the list role; the `<fieldset>`/`<legend>` already groups). **✅ VERIFIED after app rebuild/restart (2026-06-25): served HTML now has no `role="group"`; full axe re-scan of the public tile view = 0 violations.**
     **Public tile view is now axe-clean (0 violations).** Remaining Section C debt is the 3 Create/Edit/ProductIdManager items above (parallel-owned). Note: the `dotnet run` rebuild succeeded → the parallel session's in-flight code (DEF-292/294/295, Import History, AuditService) **compiles**.

## Gate fix applied this session
- **(a) ext-tile actions check narrowed** (`gate2-predeploy.js`): replaced the over-strict whole-column check with `ext-tile.no-edit-delete` (forbids only Add/Edit/Delete controls for external; download/view allowed). Syntax-checked; external has 0 edit/delete controls so it will PASS on next run. *(Deferred hygiene: the dead `window.editCertificate` in the external bundle — server-blocked, unused — left in place to avoid a multi-reference edit; remove in a later cleanup.)*

## Why we're (still) holding
1. The two pre-existing Gate-2 blockers above must be resolved (or consciously waived) before bundling.
2. **The working tree mixes both sessions' in-flight work** — a publish ships ALL of it, can't cherry-pick. Each item below must be confirmed complete + verified by its owner before it rides a bundle.

## What's in the working tree (code)
| Area | DEF / CR | Owner | State |
|---|---|---|---|
| Certificate tiles — condense spacing | **DEF-293** | this session | ✅ VERIFIED localhost (all 4 view/viewport combos) |
| Create "Add Certificate" stale toast | DEF-291 | (earlier) | ✅ Fixed locally |
| Upload path resolver / config root | DEF-286 | (earlier) | ✅ Built, pending deploy |
| Import company-reassignment guard | DEF-292 | other session | ⏳ confirm complete + verified |
| Category no-auto-fill (Create/Edit) | DEF-294 | other session | ⏳ confirm complete + verified; **touches Edit.cshtml — prime suspect for the Gate-2 cert-edit 400** |
| CSV column-leak (Partner/External export) | DEF-295 | other session | ⏳ confirm complete + verified |
| Import History view (new) | CR-023 (R5) | other session | ⏳ confirm complete + verified |
| Import undo / backups (`App_Data/uploads/import-backups/`) | CR-023 | other session | ⏳ confirm complete |
| AuditService / ImportBatch changes | (import work) | other session | ⏳ confirm complete |

## Gate checklist before bundling (Gates 0–7, per `pre-deploy-agent-checklist.md`)
- [ ] **Gate 2 GREEN** — cert-edit 400 is a GATE TEST BUG (see #1); fix `gate2-predeploy.js:131` to use a real cert number, then re-run. App's Edit is fine.
- [ ] All parallel DEFs (292/294/295) + CR-023 confirmed complete + verified by the other session.
- [ ] **Gate 7 — migration SQL coupling:** migration `20260624142128_WidenCertificateProductIdProductName` ships with `Scripts/data-ops/2026-06-24-WidenCertificateProductIdProductName-PROD.sql` in the SAME deploy step (prod never auto-migrates).
- [ ] **Gate 4 — Amazon contract diff:** `amazon-contract-diff.js` prints `CONTRACT UNCHANGED`.
- [ ] **Back up prod `uploads/` before publish** ([[feedback_backup_uploads_before_deploy]]) — and confirm the publish profile excludes `App_Data\uploads\**` (DEF-286 note; AccuWeb prod profile still needs that exclusion).
- [ ] Clean build + test suite (note: `GCCWebApp.Api.Tests` had a ctor-arg build break from DEF-278 — confirm resolved).
- [ ] Append shipped items to `shipped-2026-06.md`.

## Housekeeping already settled
- **DEF-numbering:** DEF-293 = certificate tiles (this session). The import-undo work that briefly also used "DEF-293" was renamed to **CR-023 R5** (collision resolved). 9 older duplicate DEF headers are parked for a post-deploy renumber pass.

## Recommended next step
Other session confirms its DEFs/CR-023 are complete → fix/clear the Gate-2 cert-edit 400 → run full Gates 0–7 → only then build the `PleskTestEnvironment` publish zip into `deployment-drops/`. Single session should own the actual bundle build to avoid conflicting zips.
