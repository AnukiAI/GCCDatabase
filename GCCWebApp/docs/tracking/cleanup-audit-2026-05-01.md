# Document Cleanup Audit — 2026-05-01

**Scope:** All document folders under `GCC Database Solution\` excluding source code (.cs/.cshtml/.js/.css), build output, and `.git/`.
**Author:** Shawn West (Anuki Consulting)
**Status:** Catalog only — nothing has been deleted. Use this as the working list before any rm action.

---

## TL;DR

- **~36 MB across 4 document trees**, with **~12–14 MB safe-to-archive** material if you act on the green-tagged items below.
- The single biggest noise source is the **project root** — 13 versions of the same QC log .xlsx + 8 January throwaway test scripts.
- `docs2/exports/` and `docs2/html_exports/` have **stale PDF/HTML iterations** of the Amazon Developer Guide where the canonical version (`_updates_SW3.pdf` per `feedback_amazon_api_immutable.md`) is still in use.
- `TestData/test_results/` contains **3-month-old manual test session dumps** that are regenerable.
- `docs2/archive/` is healthy as an archive — leave it alone.
- **No secrets, credentials, or private keys were observed in filenames.** `.env` files exist in the source tree (expected, gitignored).

---

## 1. Project root (`GCC Database Solution\`)

**33 files, ~0.35 MB**

### 1a. QC Log version chain — 13 files, 0.25 MB

The latest is `Database QC Log_4.27.26_sw.xlsx` (May 1). All earlier versions are superseded by it as the working register.

| File | Date | Status |
|---|---|---|
| `Database QC Log_4.27.26_sw.xlsx` | **5/1** | **KEEP — current** |
| `Database QC Log_4.27.26_sw - Copy.xlsx` | 4/30 | OneDrive auto-copy duplicate |
| `Database QC Log_4.27.26_sw.BACKUP_before_classification.xlsx` | 4/28 | manual backup |
| `Database QC Log_4.27.26.xlsx` | 4/27 | superseded |
| `Database QC Log_4.22.26UpdatesVisual_TestSteps.xlsx` | 4/26 | superseded |
| `Database QC Log_4.22.26UpdatesVisual.xlsx` | 4/24 | superseded |
| `Database QC Log_4.22.26UpdatesVisual_OG.xlsx` | 4/24 | superseded |
| `Database QC Log_4.22.26Updates.xlsx` | 4/22 | superseded |
| `Database QC Log_4.17.26Updates.xlsx` | 4/18 | superseded |
| `Database QC Log_4.7.26Updates.xlsx` | 4/15 | superseded |
| `Database QC Log_4.7.26.xlsx` | 4/8 | superseded |
| `Database QC Log_3.13.26_Updates.xlsx` | 4/5 | superseded |
| `Database QC Log_3.13.26_sw_updates.xlsx` | 3/26 | superseded |

**Recommendation:** Keep current + 1 prior (4/27 baseline) + the 3/13 origin as audit-trail anchors. Archive the other 10 to a `_archive/QC-log-history/` folder. **Risk: low** — the contents are reflected in `docs/tracking/defect-register.md`.

### 1b. Loose Amazon Developer Guide artifacts — 4 files, ~0.9 MB

| File | Date | Verdict |
|---|---|---|
| `GCC Database Partner API - Amazon Developer Guide._old.html` | 4/2 | **stale** — `_old` in name is self-describing |
| `GCC Database Partner API - Amazon Developer Guide_sw.pdf` | 4/2 | superseded by `_swupdates.pdf` (same day, +2 bytes) |
| `GCC Database Partner API - Amazon Developer Guide_swupdates.pdf` | 4/2 | superseded by `_updates_SW3.pdf` in docs2/exports/ |
| `GCC Partner API - Amazon Developer Guide_CP.pdf` | 3/27 | superseded |

**Recommendation:** Move all 4 to `_archive/`. Canonical version is `docs2/exports/GCC Database Partner API - Amazon Developer Guide_updates_SW3.pdf` (4/8) per memory.

### 1c. January test scripts — 8 files, ~0.03 MB

```
test_cookies.txt           test_form_upload.csv      test_form_upload.ps1
test_import.csv            test_import.ps1           test_import2.ps1
test_import_full.ps1       test_login.html           test_login_resp.html
usermanagement.html
```

**Recommendation:** Delete or move to `_archive/jan-throwaway-scripts/`. **Risk: very low** — these are ad-hoc curl/PowerShell runs from January, not part of any test suite.

### 1d. Standalone audit reports — 3 files

| File | Date | Verdict |
|---|---|---|
| `HARDCODED_DATA_AUDIT.md` | 2025-10-17 | **>6 months old**. Findings should be in defect-register if still relevant; otherwise archive. |
| `TEST_ISSUES_FOUND_2026-01-18.md` | 2026-01-18 | Pre-tracking-framework era. Cross-check against current defect-register before archiving. |
| `todo_pendingChangeRequest.md` | 2026-02-19 | Pre-CR-register. CR-008 through CR-014 supersede this. |

**Recommendation:** Verify nothing here is unaddressed, then archive all three.

---

## 2. `docs2\` — 406 files, ~17.6 MB

### 2a. `docs2\exports\` — 35 files, ~5.8 MB

**Confirmed exact duplicates (byte-identical, 25,682 bytes each):**
- `AMAZON_API_DEVELOPER_GUIDE - Copy.html`
- `AMAZON_API_DEVELOPER_GUIDE - Copy (2).html`

Both are also superseded by `AMAZON_API_DEVELOPER_GUIDE.html` (Apr 30, 37,485 bytes — newer, larger). **Recommendation:** Delete both `Copy*.html` files. **Risk: none.**

**Amazon Developer Guide PDF iterations:**
| File | Date | Verdict |
|---|---|---|
| `GCC Database Partner API - Amazon Developer Guide_swupdates.pdf` | 4/2 | superseded |
| `GCC Database Partner API - Amazon Developer Guide_updates_SW2.pdf` | 4/7 | superseded |
| `GCC Database Partner API - Amazon Developer Guide_updates_SW2_CP4.7.pdf` | 4/8 | superseded |
| `GCC Database Partner API - Amazon Developer Guide_updates_SW3.pdf` | **4/8** | **KEEP — canonical contract source** |
| `GCC Partner API - Amazon Developer Guide.pdf` | 3/6 | superseded |

**Recommendation:** Archive everything except `_updates_SW3.pdf`. ~1.4 MB recovered.

**Other stale items in exports/:**
- `screenshot-1775657445731.png` (4/8, 122 KB) — orphaned screenshot, unclear which doc it belonged to
- `AMAZON_API_INTEGRATION_GUIDE.html` (Feb 19, 70 KB) — pre-Phase-1 doc; check if superseded by current `AMAZON_API_DEVELOPER_GUIDE.html`
- `Change Order_ Amazon API & Certification Data Enhancements.pdf` (Feb 12) — old change order; archive once approved CRs are closed
- `GCC Database Solution - Comprehensive UAT Testing Guide.pdf` (Feb 20, 1.9 MB) — large and likely superseded by `qc-readyforuat-2026-05-01.md` flow

### 2b. `docs2\html_exports\` — 3 files, ~1.31 MB

| File | Date | Verdict |
|---|---|---|
| `GCC Certificate Table - Sticky Table & Column Toggle Feature Summary.pdf` | 3/13 | one-off feature summary; archive |
| `GCC Database Partner API - Amazon Developer Guide_swupdates_CP2.pdf` | 4/6 | superseded by SW3 |
| `StickyTable_FeatureSummary.html` | 3/12 | source of the PDF above; archive together |

**Recommendation:** Archive all 3. The folder appears to be a one-time export dump. **Risk: low** — these are presentational summaries of features now in production.

### 2c. `docs2\observations\` — **empty folder**

**Recommendation:** Delete the empty directory. (Confirmed: `ls -la` shows only `.` and `..`.)

### 2d. `docs2\archive\` — 64 files, 0.83 MB

This folder is doing exactly what an archive should do. **Leave it alone.** Two notes:
- `Copy of product categories and csf attributes.xlsx` (Feb 25) is a pre-archive duplicate — fine to leave since this folder is the dumping ground.
- `PARENT_DIRECTORY_CLEANUP_2025-11-24.md` and `PROJECT_REORGANIZATION_2025-11-24.md` are 6 months old — historical record; keep.

### 2e. `docs2\mockups\` — 153 files, ~6.4 MB

Three subfolders: `GCC_FinalDesign/` (53), `FinalDesignMockups/` (86), `decision-docs/` (14).

**Recommendation:** This is large but design-reference material. Don't touch without explicit confirmation. Two of those folders (`GCC_FinalDesign/` vs `FinalDesignMockups/`) likely overlap — worth a one-time diff if you want to consolidate.

### 2f. Active reference folders — keep

`database/` (81 files), `claude-ref/` (7), `standards/` (5), `requirements/` (7), `architecture/` (4), `user-guides/` (3), `api/` (8), `testing/` (11), `tracking/` (6).

These are all current working docs. Do not touch.

---

## 3. `TestData\` — 75 files, ~16.3 MB

### 3a. `TestData\test_results\` — 29 files, 0.25 MB

All from **Jan 23–26** (3+ months old). Manual test session dumps:
- HTML page captures (`login_page.html`, `certs.json`, etc.)
- Three `*_REPORT.md` files (deployment-readiness, data-filtering, multi-partner)

**Recommendation:** Confirm nothing in the three `*_REPORT.md` files is open work, then archive the whole folder. **Risk: very low** — sessions are reproducible by re-running the suites.

### 3b. `TestData\AmazonTestData\` — 10 files, **10.79 MB**

Largest single recoverable footprint. Per memory, you have an active partner key and seeded dev DB, so these CSVs may be regenerable.

**Recommendation:** Verify whether these are required for current Amazon UAT. If yes — keep. If they're old import-test fixtures, move to external storage (or delete if reproducible from `appsettings.Development.json` seed data). **Need confirmation before action.**

### 3c. `TestData\ImportTemplates\` — 30 files, 5.46 MB

Active import templates. **Keep.**

### 3d. `TestData\DataMigration\` — 6 files, 0.05 MB

Migration tracking. **Keep.**

---

## 4. `GCCWebApp\GCCWebApp\docs\tracking\`

20 files including 2 Office lock files (`~$-manual-tests-2026-05-01.md`, `~$-readyforuat-2026-05-01.md`) — these are auto-cleanup artifacts that disappear when the file closes. Currently sitting because the file is open in Word/Excel.

**Recommendation:** Close any open editors on those files; the lockfiles will vanish on their own. No manual action needed.

Everything else in this folder is current and load-bearing (defect-register, shipped logs, cutover-2026-05-01.md, qc-readyforuat-2026-05-01.md). **Keep.**

---

## Cleanup Priority Matrix

| Priority | Action | Files | Size | Risk |
|---|---|---|---|---|
| 🟢 **Safe — do anytime** | Delete `AMAZON_API_DEVELOPER_GUIDE - Copy*.html` (byte-identical dupes) | 2 | 50 KB | None |
| 🟢 **Safe — do anytime** | Delete empty `docs2\observations\` folder | 0 | 0 | None |
| 🟢 **Safe — do anytime** | Archive 8 root-level Jan test scripts (`test_*`, `usermanagement.html`) | 9 | 30 KB | Very low |
| 🟢 **Safe — do anytime** | Archive `_old.html` and `_sw.pdf` Amazon guide files at root | 4 | 0.9 MB | Low |
| 🟡 **Verify first** | Archive 10 superseded QC Log .xlsx files (keep current, 4/27 baseline, 3/13 origin) | 10 | ~0.2 MB | Low — content lives in defect-register |
| 🟡 **Verify first** | Archive 4 superseded Amazon Dev Guide PDFs (keep `_updates_SW3.pdf`) | 4 | ~1.4 MB | Low — confirm no external links |
| 🟡 **Verify first** | Archive `TestData\test_results\` (Jan dumps) | 29 | 0.25 MB | Very low — regenerable |
| 🟡 **Verify first** | Archive 3 standalone root MDs (`HARDCODED_DATA_AUDIT`, `TEST_ISSUES_FOUND`, `todo_pendingChangeRequest`) | 3 | 35 KB | Low — cross-check defect register |
| 🟡 **Verify first** | Archive `docs2\html_exports\` (3 files, one-off summary) | 3 | 1.3 MB | Low |
| 🟡 **Verify first** | Archive old `docs2\exports\` artifacts (Feb HTMLs, screenshot, big UAT PDF) | ~6 | ~2.2 MB | Moderate — some may still be linked |
| 🟠 **Need decision** | Mockups folder consolidation (`GCC_FinalDesign/` vs `FinalDesignMockups/`) | 139 | ~6.4 MB | Moderate — design reference |
| 🟠 **Need decision** | `TestData\AmazonTestData\` regeneratable? | 10 | 10.79 MB | Moderate — active UAT |

**Estimated easy wins (🟢 + low-risk 🟡):** ~5–6 MB and ~70 files.
**With 🟠 decisions:** another ~17 MB.

---

## Suggested workflow when you're ready to act

1. Create `_archive\2026-05-cleanup\` at the project root.
2. Move (don't delete) all 🟢 + 🟡 items there in one sweep.
3. Live with it for a week — if nothing breaks and no doc link 404s, the archive can be zipped and dropped to OneDrive cold storage.
4. 🟠 items get their own conversation — don't bundle them with this pass.

---

## What was NOT scanned

- Source code (`*.cs`, `*.cshtml`, `*.js`, `*.css`)
- `.git\` history
- `bin\` / `obj\` / build output
- `wwwroot\uploads\` (per `feedback_backup_uploads_before_deploy.md`, this is user-data territory and not a cleanup target)

If you want a separate code-side audit (unused views, dead controllers, orphan migrations), say the word — that's a different exercise.
