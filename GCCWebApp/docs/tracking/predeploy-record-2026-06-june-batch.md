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
| DEF-228 | (WAD — no code) | — | none |

**Risk read:** DEF-227 trips Gate 2 (shared layout / cross-role). DEF-234 trips Gate 3 (data-scoping — it edits the shared filtered-cert query) and warrants a Gate 4 glance (the method is repository-level; confirm Partner API JSON shape is untouched). DEF-230/231 are data-write paths. So Gates 1–4 all fire for this batch.

---

## Gate status

| Gate | Owner agent | Fires for this batch? | Status | Evidence |
|---|---|---|---|---|
| **0 — Back up server uploads** | manual | Always | ⬜ TODO | Zip `wwwroot/uploads/` on the server before publish. |
| **1 — UI / Design-Guide compliance** | `ux-design-compliance` | ✅ yes (DEF-225/226/227/229/232/233 touch views/JS/CSS) | ⬜ NOT RUN | — |
| **2 — Cross-role + responsive regression** | `accessibility-validator` + Playwright | ✅ yes (DEF-227 = shared `_Layout` + main.css) | 🟡 PARTIAL | DEF-227 verified on 4 roles per observations_log 2026-06-01; full 5-viewport AxeCore sweep on the *batch as a whole* not recorded. |
| **3 — Data-scoping & endpoint coverage** | `security-specialist` | ✅ yes (DEF-234 edits shared filtered-cert query; DEF-230/231 data-write) | ⬜ NOT RUN | Must confirm DEF-234's `AsSplitQuery` change did not alter row-scoping on external/partner tile surfaces. |
| **4 — Partner API contract integrity** | `security-specialist` | ✅ yes (DEF-234 is in `CertificateRepository` — adjacent to Partner-facing query paths) | ⬜ NOT RUN | Confirm `/api/v1/certificates*` JSON shape byte-identical; `status="Active"` literal still enforced. |
| **5 — Traceability audit** | `governance-manager` | Always | ✅ DONE 2026-06-02 | This sweep. All 9 fixes trace to DEF rows; shipped-2026-06.md reconciled; DEF-234 status corrected; DEF-235 opened for the DEF-107 prod regression. See observations_log 2026-06-02. |

---

## Blockers that must clear before this batch deploys

1. **🔴 Smoke baseline is RED.** Latest roll-up `Scripts/smoke-prod/_results/summary-2026-06-02T00-32-22-220Z.json` = `green:false`, 4 FAIL on `cert-tile-public` (cold-start timeout = **DEF-234 itself**). The DEF-234 fix is now in the tree; a **warm-server re-run** is expected to go green. **Do not deploy on a red on-disk baseline** — re-run `node Scripts/smoke-prod/_views/run-all.js` against a warm app and confirm green first.
2. **🟡 DEF-235 (Permissions-Policy dupe)** is an open prod regression. Not introduced by this batch, but if a security-header consolidation is folded in, it rides Gate 4. Otherwise: decide *fix-now* vs *log-a-waiver* (don't let it deploy silently un-addressed a second time).
3. **🟡 Data-cleanup must NOT ride along.** DEF-231 / CR-019 REMEDIATE SQL (`Scripts/data-ops/2026-06-01-product-miscategorization-REMEDIATE.sql`) is gated on GCC written approval + DB backup. Confirm it is excluded from the publish zip — the code fix deploys; the SQL does not.

---

## Waiver log (for this batch)

If any gate is shipped past on purpose, record it here (and mirror to the master checklist's waiver log).

| Date | Gate waived | Why | Approved by | Follow-up |
|------|-------------|-----|-------------|-----------|
| | | | | |
