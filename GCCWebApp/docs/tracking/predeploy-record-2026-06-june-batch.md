# Pre-Deploy Gate Record — June 2026 batch

**Instance of** `docs/tracking/pre-deploy-agent-checklist.md` (the 5-gate standard).
**Created:** 2026-06-02 (governance sweep). **Status:** ⏳ NOT YET CLEARED — gates 1–4 unrun.
**Deploy target:** gccwebapp.com / greencirclecertified.com (Plesk).

> Purpose of this file: the checklist defines *how* to gate a deploy; this file records *whether each gate actually ran* for this specific batch, with evidence links. A deploy should not go out until every row below is ✅ or has a logged waiver. Update this file as gates clear; archive it once the batch deploys.

---

## What's in this batch

Source of truth: `docs/tracking/shipped-2026-06.md` (9 code fixes + 1 WAD) and `defect-register.md`.

| DEF | Surface touched | Files | Risk class |
|---|---|---|---|
| DEF-225 | Cert Edit — product name display | `Views/Certificate/Edit.cshtml` | UI / view |
| DEF-226 | Admin Product Types — usage pill | `wwwroot/js/product-types.js` | UI / view |
| DEF-227 | `_Layout` nav spacer (all roles) | `_Layout.cshtml`, `main.css` | **shared layout / cross-role** |
| DEF-229 | Cert Create — Product ID select-all | `wwwroot/js/product-id-manager.js` | UI / view |
| DEF-230 | Admin Product Types — re-parent | `AdminService.cs` | data-write (admin) |
| DEF-231 | Cert form inline Add-Product categorization | cert form JS + `ProductController` | data-write |
| DEF-232 | Import template encoding (UTF-8 BOM) | `CertificateImportController.cs`, user mgmt template | export |
| DEF-233 | Tile filter-options fetch guard | 3 tile-view JS files | UI / view |
| DEF-234 | **Tile cert-list query** `AsSplitQuery()` + stable paging | `Repositories/CertificateRepository.cs` `GetAdvancedFilteredCertificatesAsync` | **shared query — feeds public/external/internal tile APIs AND touches the method near Partner-facing paths** |
| DEF-236 | **Security — `DownloadSelectedZip` IDOR guard** (added during this gate run) | `Controllers/CertificateController.cs` (`FileAccessAllowed` per-row guard) | **HIGH — broken object-level authorization; fixed + verified 2026-06-02** |
| DEF-107 / "DEF-235-Perms" | **Security headers — remove duplicate IIS injection** (Permissions-Policy etc. dedup; folded in 2026-06-02) | `web.config` (`<customHeaders>` block removed; middleware is single source) | security-header hygiene; localhost verified single headers; **prod dedup verified post-deploy only** |
| DEF-228 | (WAD — no code) | — | none |

**Risk read:** DEF-227 trips Gate 2 (shared layout / cross-role). DEF-234 trips Gate 3 (data-scoping — it edits the shared filtered-cert query) and warrants a Gate 4 glance (the method is repository-level; confirm Partner API JSON shape is untouched). DEF-230/231 are data-write paths. So Gates 1–4 all fire for this batch.

---

## Gate status

| Gate | Owner agent | Fires for this batch? | Status | Evidence |
|---|---|---|---|---|
| **0 — Back up server uploads** | manual | Always | ⬜ TODO | Zip `wwwroot/uploads/` on the server before publish (Shawn's manual step). |
| **1 — UI / Design-Guide compliance** | `ux-design-compliance` | ✅ yes (DEF-225/226/227/229/232/233 touch views/JS/CSS) | ✅ **PASS** (2026-06-02) | Notification/modal patterns clean; cert # (not ID) displayed; labels unchanged; Import/Export arrows intact. Agent flagged a "duplicate `<h1>` in Edit.cshtml:31" — **resolved/false-alarm:** `admin-mockup-override.css:407` `body:has(.admin-container) .page-header{display:none!important}` hides the layout h1 on admin pages, so only `.admin-page-title` renders (consistent admin pattern, out of the a11y tree). Non-blocking nits: 2 `alert()` fallbacks in `Edit.cshtml:954,962` (dead unless AdminCore missing); some 11–12px caption text. |
| **2 — Cross-role + responsive regression** | `accessibility-validator` + Playwright | ✅ yes (DEF-227 = shared `_Layout` + main.css) | ✅ **PASS-WITH-NITS** (2026-06-02) | DEF-227 spacer = 32px on all 4 roles (admin-only regression does NOT reproduce); External restrictions intact (no Add/Edit, no Actions col, no internal tags); 0 horizontal-scroll across desktop/tablet/mobile; AxeCore 0 violations on layout chrome. 21 screenshots at `_results/screenshots/gate2-2026-06-02T21-25-23/`. Pre-existing non-blocking nits (NOT from this batch): skip-link class conflict (`admin-sr-only`+`skip-to-main`); mobile toggle missing `aria-expanded`. → log as a follow-up a11y DEF. |
| **3 — Data-scoping & endpoint coverage** | `security-specialist` | ✅ yes (DEF-234 edits shared filtered-cert query; DEF-230/231 data-write) | ✅ **PASS** (2026-06-02, after fix) | **Batch changes clean** (DEF-234 query-shape only; External tile API uses a separate query; DEF-230/231 writes Admin/Analyst-gated). Audit surfaced a live HIGH IDOR in `DownloadSelectedZip` (external user POST `[1..60]` → 32 cross-company PDFs from a 2-cert scope). **FIXED + verified same day → DEF-236:** added the `FileAccessAllowed` per-row guard inside the zip loop; re-probe (`def236-idor-verify.js`) → POST `[1..60]` now returns ZIP = 2 entries / 112 KB (exactly the user's authorized set). Build clean. Leak closed. |
| **4 — Partner API contract integrity** | `security-specialist` | ✅ yes (DEF-234 is in `CertificateRepository` — adjacent to Partner-facing query paths) | ✅ **PASS** (2026-06-02) | DEF-234's changed method (`GetAdvancedFilteredCertificatesAsync`) has 3 callers — none is the v1 controller; every `/api/v1/certificates*` endpoint projects directly with its own inline shape (`git show 086e9c1 --stat`: v1 controller untouched). JSON shape byte-identical; `status="Active"` literal + `PublicStatuses` filter enforced on list/search/GS1/by-id/by-number/PDF. No client→partner CR required. |
| **5 — Traceability audit** | `governance-manager` | Always | ✅ DONE 2026-06-02 | This sweep. All 9 fixes trace to DEF rows; shipped-2026-06.md reconciled; DEF-234 status corrected; DEF-235 opened for the DEF-107 prod regression. See observations_log 2026-06-02. |

---

## Blockers that must clear before this batch deploys

1. **✅ CLEARED — Smoke baseline now GREEN.** The prior RED (4 FAIL on `cert-tile-public`, cold-start = DEF-234 itself) is resolved: warm-server batch run 2026-06-02 = **694 PASS / 0 FAIL / 6 WARN GREEN** (`SMOKE_BATCH_RESTART=1`), and DEF-234 cold-boot re-verified (first tile-API request 54ms vs 23s; warmup logged 1027ms). Tier 2 state-matrix also GREEN (27/27). No longer a blocker.
2. **✅ CLEARED — Gate 3 IDOR fixed (DEF-236).** The `DownloadSelectedZip` cross-company PDF leak (external user → 32 PDFs from a 2-cert scope) was fixed same-day with the `FileAccessAllowed` per-row guard and verified (`def236-idor-verify.js`: POST `[1..60]` → 2 entries, the user's authorized set). Logged as DEF-236; ships with this batch. Gate 3 now PASS.
3. **✅ INCLUDED — Permissions-Policy duplicate-header fix (DEF-107 / "DEF-235-Perms").** Decision: **fold in** (not waiver). The fix already lives in the working tree's `web.config` — the `<httpProtocol><customHeaders>` block that double-injected the security headers was removed (lines 22-31 now an explanatory comment), leaving `Middleware/SecurityHeadersMiddleware.cs` as the single source. `web.config` ships with the FileSystem publish, so it deploys with this batch automatically. Localhost re-verified 2026-06-02: `HEAD /` emits exactly **1** of each security header (`Permissions-Policy count=1`, `X-Frame-Options: DENY`, single CSP). **Gate 4 impact: none** — this removes duplicate HTTP response headers; it does not touch `/api/v1` JSON shape (verified Gate 4 PASS independently). **Post-deploy verification REQUIRED** (Kestrel ignores `web.config`, so the IIS dedup can only be confirmed on prod): re-run `qc159-live-check/older-defs-grouped-sweep.js` (or `curl -sI https://gccwebapp.com/ | grep -ci permissions-policy` → expect 1, was 2).
   > ⚠️ **DEF-number collision to resolve:** there are TWO `DEF-235` entries in the register — line ~115 (sustained-load `ERR_CONNECTION_REFUSED`) and line ~6364 (this Permissions-Policy regression). Both were opened in the 2026-06-02 governance sweep. They need distinct numbers; proposed: keep connection-refused as DEF-235 (more cross-referenced — smoke-plan, batch-restart mitigation), renumber the Permissions-Policy one to **DEF-237**. Flagged for Shawn — not renumbered yet (ripples across register + this record + governance note).
4. **🟡 Data-cleanup must NOT ride along.** DEF-231 / CR-019 REMEDIATE SQL (`Scripts/data-ops/2026-06-01-product-miscategorization-REMEDIATE.sql`) is gated on GCC written approval + DB backup. Confirm it is excluded from the publish zip — the code fix deploys; the SQL does not. (FileSystem web publish does not include `Scripts/`.)

---

## Waiver log (for this batch)

If any gate is shipped past on purpose, record it here (and mirror to the master checklist's waiver log).

| Date | Gate waived | Why | Approved by | Follow-up |
|------|-------------|-----|-------------|-----------|
| | | | | |
