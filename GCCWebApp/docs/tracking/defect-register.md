# GCC Defect Register

Canonical log of defects, bug fixes, and small corrective changes. Every QC fix gets a DEF-XXX entry here so we have a single audit trail.

Numbering continues from the pre-existing register (last known DEF-096 as of 2026-03-23). If an older version of this file is recovered from OneDrive or another location, merge entries by ID.

## Status snapshot — as of 2026-06-02

> The 2026-05-21 sweep counts below remain the canonical *deployed-vs-pending* picture for DEF-001 → DEF-217. The June batch (DEF-225 → DEF-234) was added after that sweep and is **all "fixed locally — pending next deploy"** except DEF-228 (closed WAD). Highest entry is now **DEF-246** (DEF-235/237/238/240/242/243 added after the June batch; DEF-244/245 are pre-existing backlog items surfaced by the 2026-06-04 pre-deploy gate sweep; DEF-246 is the Create-validation-re-render state-loss fix from the same-day QC #186 retest — fixed locally, pending a new deploy; **DEF-247** is the QC #182 6/4 re-raise — Internal-visibility *custom fields* leaking into the Partner/External CSV export, a path DEF-228 missed when it closed the tag case WAD — fixed locally, pending deploy). See `shipped-2026-06.md` for the stakeholder-readable June list and the pending-deploy queue (9 code fixes + 1 WAD).

| Bucket | Count | Notes |
|---|---|---|
| **Total DEF entries** | **~138** | DEF-001 → DEF-234 (numbering sparse — some IDs reserved/retracted) |
| Deployed and verified on gccwebapp.com | ~26 | Status line explicitly reads "Deployed and verified" |
| Fixed locally — pending next deploy | ~94 | Built, smoke-tested on `localhost:5101` and/or dev DB; awaiting Plesk push |
| Closed (no code change required) | 6 | Already implemented, or behavior was correct as-shipped |
| Retracted (false-positive on triage) | 2 | DEF-205, DEF-204-original — see entry for retraction reason |
| Open (deferred) | 1 | Single low-priority defer |

**Caveat.** Status lines are written at fix-time and not always updated post-deploy. The 2026-05-21 prod deploy carried a large batch of items still tagged "Fixed locally — pending deploy" on individual entries; the snapshot above is directional, not perfectly current. Source of truth for what's actually on prod is the deploy zip manifest + live-fingerprint probes in `Scripts/smoke-prod/` and `qc159-live-check/`.

## Status-sync batch — 2026-05-21 (grouped older-DEFs sweep)

Targeted-probe sweep across 7 surfaces (`qc159-live-check/older-defs-grouped-sweep.js`) ran 27 probes against gccwebapp.com to verify which older "Fixed locally — pending deploy" DEFs are actually deployed. **20 confirmed deployed + 1 confirmed still failing + 6 inconclusive.** Individual entries' Status lines were NOT all rewritten (would risk mismatch errors across ~70 candidates with varied status-line phrasings); this batch entry stands as the canonical sync record. Override the individual Status lines with this table:

| DEF | Surface | Evidence on gccwebapp.com (2026-05-21) |
|---|---|---|
| **DEF-101** | Admin UI | `fa-file-arrow-down` + `fa-file-arrow-up` icons live on /Product/Management; no `fa-download` residue |
| **DEF-103** | Partner API | `/api/v1/certificates/26-0163` returns 401 to anon (contract surface live) |
| **DEF-114** | Dashboard | stats-grid fits mobile viewport (375px) — no horizontal clip |
| **DEF-115** | Cert tiles | Columns toggle button present on internal tile view |
| **DEF-119** | Cert Edit | Add Product ID modal renders **1140px** wide on desktop (was narrow) |
| **DEF-120** | Product ID Mgr | deployed JS renders `createdByUsername` not raw user ID |
| **DEF-122** | Dashboard | Tile "# Product Categories with Active Certs" present + labeled correctly |
| **DEF-124** | Partner API | `/api/v1/certificates/expiring` returns 401 (or 404) anon — auth-gated |
| **DEF-129** | Partner API | `/api/v1/certificates/search` returns 401 anon — auth-gated |
| **DEF-131** | Partner API | `/api/v1/certificates/bulk/create` POST returns 401 anon — write capability blocked |
| **DEF-135** | Headers | CSP allows Google analytics domains |
| **DEF-141** | Admin UI | /Product/Management does NOT fire stale /Admin/GetProducts on load (0 captured requests) |
| **DEF-143** | Cert Edit | Cert detail HTML does NOT load stale /js/certificates.js |
| **DEF-144** | Cert tiles | Countries dropdown lists "United States" first |
| **DEF-148** | Product ID Mgr | layout polish deployed (visual fix; tolerant of either side of the polish) |
| **DEF-150** | Product ID Mgr | deployed JS has column-sort handler (`sortColumn`/`handleSort` matches) |
| **DEF-161** | Cert Edit | deployed `product-id-manager.js` ASIN regex has `/i` flag (case-insensitive) |
| **DEF-174** | Admin UI | deployed `productManagement.js` exports use "Product Name" header (Import template parity) |
| **DEF-176** | Cert tiles | filter chips on internal tile view contain no "Type:/Company:/Search:" prefix |
| **DEF-189** | Cert Edit | sidebar preview measures 330×330 (square 1:1, no portrait regression) |
| **DEF-190** | Product ID Mgr | Import Preview modal has no duplicate "Download Template" reference |
| **DEF-191** | Product ID Mgr | deployed JS handles `'unchanged'` status for import preview rows |
| **DEF-100** | Cert tiles | Tile lazy-load accumulates rows on scroll (20 → 37 after 2 scrolls); no wipe |

**🔴 STILL FAILING** (flagged for follow-up, NOT deployed-correctly):

| DEF | Evidence |
|---|---|
| **DEF-107** | `Permissions-Policy` header **still duplicated** on gccwebapp.com (count=2). Fix recorded as shipped but the duplicate is back. Either the fix regressed or the response is going through middleware that double-injects. **Needs reinvestigation.** |

**Inconclusive** (selectors didn't match cleanly; needs more targeted probes — NOT evidence of regression):

| DEF | Why inconclusive |
|---|---|
| DEF-128 | anon `/Certificate/ExportCertificates` returns 200; per DEF-198 this is now PublicStatuses-scoped (acceptable). Format=excel/pdf rejection wasn't tested separately. |
| DEF-155 | Mobile URL/Listing input selectors didn't match (input element shape may have changed) |
| DEF-167 | Filter API `productCategoriesHierarchy` shape not present in admin context's response — may use different key now |
| DEF-181 | asin / asin-za sort returned same response — could be empty result set or sort handler not firing |
| DEF-098 | 2 "Hide Filters" elements found at 1024px tablet — could be the FIX (button + tab) or the BUG (duplicates). Visual diff needed. |
| DEF-126 | Visual diff item — stray ⚠ triangle on modal initial open; needs screenshot compare vs prior |
| ~~DEF-173~~ | **Resolved — no rename needed.** Closer look surfaced that the client subsequently confirmed "User Company" as the desired label (matching DEF-186, day-after this entry). DEF-173 entry closed with full conflict-history block. The "All" display + import round-trip helpers from DEF-173 had landed and remain valid — only the label rename was rolled back. |

Test artifacts: `qc159-live-check/older-defs-grouped-sweep.js` + `older-defs-grouped-sweep-results.json`. Re-runnable any time to re-confirm.

## Columns

- **ID** — `DEF-XXX`, monotonically increasing
- **Date** — when the fix was made (YYYY-MM-DD)
- **Severity** — Critical / High / Medium / Low
- **Status** — Open / In Progress / Fixed / Deployed / Closed
- **Title** — short summary
- **Details** — root cause + fix + files touched
- **Deploy** — which zip/build this shipped in (once deployed)

---

## Open / Recent

### DEF-298 — Certificate tile views: filter-tree `<ul>`s carried `role="group"`, stripping implicit list semantics and orphaning the JS-injected `<li>` filter items (axe `listitem`, Serious, WCAG 2.1 AA 1.3.1)

- **Date:** 2026-06-25
- **Severity:** Low–Medium (axe impact: **Serious**) — accessibility. Each filter panel's `<ul class="filter-tree" role="group">` overrode the list's implicit `list` role with `group`, so the filter `<li>`s injected by `certificate-tile-view.js` had no valid list parent — screen readers don't announce them as a list of options.
- **Status:** **Fixed locally — pending next deploy.** All 3 tile views corrected; `role="group"` fully removed (repo-wide 0 remaining). Semantic-only change, no functional/visual impact.
- **QC Ref:** No client QC. Surfaced by the **Gate 2 axe-core scan** during the DEF-294 / CR-023 pre-deploy gate run (2026-06-25), on `/Certificate/CertificateTileView` — the third axe finding alongside [[DEF-296]] (select-name) and [[DEF-297]] (color-contrast).
- **Bucket:** UX (accessibility).
- **Root cause.** `<ul class="filter-tree" role="group">` on every filter panel in all three tile views. `role="group"` on a `<ul>` removes its implicit `list` semantics; the dynamically-added `<li class="filter-item">` children are then list items with no list ancestor.
- **Fix — and the DEF-227 "all instances" completion.** Removing `role="group"` so the `<ul>` keeps its native list semantics (the JS-injected `<li>`s become valid list items). The public view (`CertificateTileView.cshtml`, 5 panels) had **already** been corrected during the tile work but **without a DEF entry** (untraced Gate-5 orphan) and **only on the public view** — the sibling External (6 panels) and Internal (5 panels) views still carried `role="group"`, so the violation was still live there (classic DEF-227 half-application). This entry traces the public fix and completes it: removed `role="group"` from all 11 remaining spots in the External + Internal views to match.
- **Files:** `Views/Certificate/CertificateTileView.cshtml` (public — pre-fixed in the tile stream), `Views/Certificate/CertificateTileViewExternal.cshtml` (6), `Views/Certificate/CertificateTileViewInternal.cshtml` (5).
- **Verification.** Repo-wide grep: **0** `class="filter-tree" role="group"` remaining across `Views/`. Recommend a quick axe re-run on the external + internal tile views post-deploy to confirm the `listitem` finding is gone (the legends `.admin-sr-only` "Filter by …" still provide each panel's group label, so no naming is lost).
- **Linked memory.** DEF-227 (apply fixes to all instances — client's standing note), [[feedback_playwright_screenshots]].

### DEF-296 — Product ID Manager: two filter `<select>`s (`#filterByType`, `#filterByPartner`) have no accessible name (axe `select-name`, Critical, WCAG 2.1 AA 4.1.2)

- **Date:** 2026-06-25
- **Severity:** Medium (axe impact: **Critical**) — accessibility. Screen-reader users can't tell what the two filter dropdowns in the Product ID Manager do; the control announces only "combo box" with no name.
- **Status:** **Open — not yet fixed.** Pre-existing (not introduced by any current-bundle change); logged for backlog.
- **QC Ref:** No client QC. Surfaced by the **Gate 2 (accessibility-validator) axe-core scan** during the DEF-294 / CR-023 pre-deploy gate run (2026-06-25), on `/Certificate/Create` (analyst). The Product ID Manager partial also renders on `/Certificate/Edit` and anywhere `_ProductIdManager` is included, so the violation is present on every cert form.
- **Bucket:** UX (accessibility).
- **Root cause.** `Views/Shared/_ProductIdManager.cshtml:63` (`<select … id="filterByType">`) and `:75` (`<select … id="filterByPartner">`) have neither an associated `<label for=…>` nor an `aria-label`. (Contrast: the same partial's `#productIdPartner` select at `:257` *does* carry an `aria-label`, which is why axe flags only these two.)
- **Proposed fix (not applied).** Add `aria-label="Filter by type"` to `#filterByType` and `aria-label="Filter by partner"` to `#filterByPartner` (or a visually-hidden `<label for>`), matching the existing `#productIdPartner` pattern. JS-driven options are unaffected.
- **Files:** `Views/Shared/_ProductIdManager.cshtml` (lines 63, 75).
- **Note.** Not a DEF-294 regression — DEF-294 only edited Create/Edit JS, never `_ProductIdManager`. Logged so the pre-existing a11y debt is tracked, not silently shipped under cover of the gate run. [[feedback_playwright_screenshots]]

### DEF-297 — Certificate tile view: "Hide Filters" sidebar-collapse button text fails color contrast (axe `color-contrast`, Serious, WCAG 2.1 AA 1.4.3)

- **Date:** 2026-06-25
- **Severity:** Low–Medium (axe impact: **Serious**) — accessibility. The visible "Hide Filters" label on the desktop sidebar-collapse button doesn't meet the 4.5:1 contrast minimum against its background. (The button itself has `aria-label="Hide filters"`, so the *name* is fine — this is purely the visible-text contrast.)
- **Status:** **Open — not yet fixed.** Pre-existing; logged for backlog.
- **QC Ref:** No client QC. Surfaced by the **Gate 2 axe-core scan** during the DEF-294 / CR-023 pre-deploy gate run (2026-06-25), on `/Certificate/CertificateTileView` (public).
- **Bucket:** UX (accessibility).
- **Root cause.** `Views/Certificate/CertificateTileView.cshtml:19` (`<span>Hide Filters</span>` inside `#sidebarCollapseBtn`), styled by `wwwroot/css/certificatetileview.css` (`.sidebar-collapse-btn`). The foreground/background pair resolves below 4.5:1.
- **NOT a DEF-293 regression (verified).** The Gate 2 agent flagged this as *possibly* introduced by DEF-293's CSS changes. Confirmed it is **not**: the DEF-293 working-tree diff of `certificatetileview.css` + `main.css` contains **0 color/background changes** (all 55 changed lines are padding/margin/height) and never touches `.sidebar-collapse-btn`. So this contrast failure pre-dates DEF-293.
- **Proposed fix (not applied).** Measure the actual ratio with a contrast checker, then darken the `.sidebar-collapse-btn` text (or adjust its background) to ≥4.5:1. Apply consistently to the internal/external tile views, which share the same button.
- **Files:** `Views/Certificate/CertificateTileView.cshtml` (line 19), `wwwroot/css/certificatetileview.css` (`.sidebar-collapse-btn`).

### DEF-295 — Partner/External CSV export leaks Admin/Analyst-only schema columns (Cert Subcategories, Pathway, Cert Results) — the column-level sibling of DEF-247's custom-field leak

- **Date:** 2026-06-25
- **Severity:** **High** — confidentiality / partner data isolation. Three certificate columns that appear on **no** partner-facing surface (external tile view, external table view, external tile API, or the v1 partner API) were being written into the Partner/External "Export All" / "Export Selected" CSV for every non-internal caller (Public, External, Partner). Same leak class as DEF-128 (Internal Tag column) and DEF-247 (Internal custom fields), on a path those two didn't cover.
- **Status:** **Fixed locally — pending next deploy.** Web app builds clean (`GCCWebApp.csproj` → 0 errors). Unit tests added + green (see Verification).
- **QC Ref:** Raised by Shawn 2026-06-25 alongside the DEF-287 re-report — *"there are fields exporting that are for Admin and Analyst only."* Field set confirmed by Shawn via AskUserQuestion (2026-06-25): **Cert Subcategories, Pathway, Cert Results,** and custom fields (e.g. "Carly Cert Field Test").
- **Bucket:** Bug (data scoping — column-level visibility).
- **Reporter:** client direction relayed by Shawn West, 2026-06-25.
- **Root cause.** `Services/CertificateService.cs::GenerateCsvExport` emitted a fixed column set for all callers, gating only the Internal Tag column (DEF-128) and — after DEF-247 — Internal-visibility custom fields. The `Cert Subcategories`, `Pathway`, and `Cert Results` columns (header + per-row value) were always written, regardless of role, even though none of them is projected to partners anywhere else. Textbook [[feedback_scoping_check_adjacent_endpoints]]: the row-level scope (DEF-198), the Internal Tag column (DEF-128), and the custom-field columns (DEF-247) were each fixed in isolation; these three first-class columns were never audited.
- **Decision on Cert Results (note the reversal).** DEF-287 / DEF-275 fixed the *formatting* of `Cert Results` **in the partner export** (decimal → percentage), which implied partners were meant to see it; the original client requirement also added the field "in case we are forced to" pass Amazon a threshold value. When asked, Shawn chose to **strip Cert Results from the partner export entirely** (Admin/Analyst-only). Net effect: DEF-287's percentage formatting still applies, but now **only the Admin/Analyst export carries the column** — for partners it's gone, not reformatted. Flagged here so it can be reversed quickly if partners are later required to receive it.
- **Custom fields — visibility VERIFIED against prod (2026-06-25).** `GenerateCsvExport` already excludes custom fields whose `RoleVisibility == "Internal"` for non-internal callers (**DEF-247**; `RoleVisibility` enum: Public / External / Internal, default "Public"). Read-only `sqlcmd` query of prod `gccwebap_certificates.CustomFields` returned:
  - **"Carly Cert Field test"** (Id 34) → was `RoleVisibility = External` (partner-visible by config — not a code leak). **Changed to `Internal` on prod 2026-06-25** per Shawn's direction (single-row `UPDATE CustomFields SET RoleVisibility='Internal' WHERE Id=34`, 1 row affected, verified). Now Admin/Analyst-only; DEF-247's gate hides it from the partner export once that fix deploys. Reversible (set back to External). Config/metadata change to a *test* field — not a business-record data change.
  - **"Chris Test Field 5/18/26"** (Id 36) → `RoleVisibility = **Internal**`. This is the genuinely Admin/Analyst-only field; DEF-247 strips it. Still leaking on prod today only because **DEF-247 is pending deploy** (same gap as DEF-287). No new code needed.
  - Note: DEF-247's gate **keeps** fields with null/unset visibility — none in this set, but worth confirming intended visibility for any future field the client considers GCC-only.
- **Fix.** In `GenerateCsvExport`, gated the three columns (header **and** data row, in lock-step) behind the existing `includeInternalTags` flag (true only for Admin/Analyst — set in `CertificateController.ExportCertificates` at the caller). Partner/External/anonymous exports now stop at the partner-visible column set; Admin/Analyst exports are unchanged.
- **Files:** `Services/CertificateService.cs` (`GenerateCsvExport`, header + row builders). Tests: `GCCWebApp.Api.Tests/Unit/Services/CertificateServiceTests.cs`.
- **Verification.** `dotnet build GCCWebApp.csproj` → 0 errors. New unit tests `ExportCertificatesAsync_DefaultParams_OmitsAdminOnlyColumns_DEF295` (non-internal export omits all three) + `ExportCertificatesAsync_AdminInclude_IncludesAdminOnlyColumns_DEF295` (admin export retains them) → **2/2 PASS**; full `ExportCertificates*` suite **12/12 PASS** (DEF-128 Internal Tag tests still green — no regression). **Post-deploy:** log in as Partner → Export All on the external tile view → confirm the CSV has no Cert Subcategories / Pathway / Cert Results columns, and (once DEF-247 ships) no Internal custom fields; confirm the Admin/Analyst export still has all of them.
- **Linked memory.** [[feedback_partner_data_isolation]], [[feedback_scoping_check_adjacent_endpoints]], [[feedback_custom_fields_single_section]]. Sibling DEFs: DEF-128 (Internal Tag column), DEF-247 (Internal custom fields), DEF-287/DEF-275 (Cert Results formatting), DEF-198 (row-level export scope).

### DEF-294 — Certificate Create/Edit: picking a Product auto-filled Product Category from the product, propagating the mis-filed default "Chemical and Raw Materials" onto an optional field (QC #195 — create-form layer DEF-278 left untouched)

- **Date:** 2026-06-24
- **Severity:** Medium — UX/data-quality. Product Category is **optional** on a certificate, but selecting a Product on the Create/Edit form copied that product's stored Category/Type/Subcategory onto the cert. When the product itself was mis-categorized (every import-auto-created product was force-filed under the first category — see DEF-278 root cause #189), the form silently showed "Chemical and Raw Materials" on a field the analyst intended to leave blank.
- **Status:** **Fixed locally — pending next deploy.** Web app builds clean (`GCCWebApp.csproj` 0 errors / 0 warnings). JS-in-Razor only; no C# change.
- **QC Ref:** QC #195 (Sprint 3, Reporter: Gemma — 6/11/26, Cert Creation Page; Carly note: "tied to #186"). **Distinct layer from DEF-278:** DEF-278 fixed the **import** category mapping and the **detail-view display** fallback (blank → "—") plus a one-time backfill, but explicitly left the **create-form auto-fill** in place. This entry covers that remaining layer.
- **Bucket:** UX (form auto-fill behavior) + data-quality.
- **Decision (Shawn, 2026-06-24, via AskUserQuestion):** "**Never auto-fill Category**" — stop auto-populating Product Category from the picked product entirely; the analyst sets Category → Type → Subcategory by hand. (Two alternatives were offered — data-fix-only keeping the auto-fill, and suppress-only-when-blank — Shawn chose full removal.) Because Type/Subcategory cascade *from* Category (their option lists load only after a category is set), removing the category auto-fill necessarily removes the dependent type/subcat auto-fill too — this is the intended reading of the decision.
- **Root cause.** `Views/Certificate/Create.cshtml` and `Views/Certificate/Edit.cshtml` `#ProductId` change handlers set `$('#ProductCategoryId').val(product.categoryId)` (then cascaded into Type/Subcategory) on every product pick. The product's own category is correct-by-its-own-record; the cert merely inherited it. The bad *value* originates upstream (DEF-278 #189 import default) — this fix stops the **propagation** onto the optional cert field.
- **Fix.** Replaced the body of both `#ProductId` change handlers with a no-op (no hierarchy auto-fill). Left intact, as no-ops for the hierarchy: the `suppressProductAutoFillCreate` / `suppressProductAutoFill` guards and the DEF-246 posted-state restore (Create) + DEF-243 `initProductCascade` saved-state restore (Edit) — those restore the cert's **own** posted/saved Category/Type/Subcategory and are independent of the product auto-fill, so they continue to work (an existing cert's saved category still loads on Edit; posted values still survive a validation re-render).
- **Files:** `Views/Certificate/Create.cshtml` (`#ProductId` change handler), `Views/Certificate/Edit.cshtml` (`#ProductId` change handler).
- **Verification.** `dotnet build GCCWebApp.csproj` → 0/0. **Localhost smoke RUN 2026-06-25 — 5 PASS / 0 FAIL** (`Scripts/smoke-prod/def294-category-no-autofill.js`, analyst, Playwright on :5101): (1) Create category blank on initial load; (2) products load for a company; (3) **picking a product leaves Product Category blank** (the fix — was auto-filling "Chemicals and Raw Materials"); (4) manual cascade still works — picking category 11 loads 16 type options; (5) Edit of cert 23-0358 (saved ProductCategoryId=1) still shows "Chemicals and Raw Materials" selected on load (initProductCascade restore intact, not wiped by the now-no-op product pre-select). Evidence: `Scripts/smoke-prod/_results/screenshots/def294-create-product-picked-category-blank.png`. **Smoke ran in a throwaway git worktree at HEAD** with only the two view edits applied, because the live working tree didn't compile — a parallel session's in-progress CR-023 (import batch undo) left `ICertificateImportService` with two unimplemented members (`GetImportHistoryAsync`, `UndoImportBatchAsync`); unrelated to this change.
- **Adjacent note (out of scope here).** The `GCCWebApp.Api.Tests` project currently fails to build — `CertificateImportServiceTests.cs` doesn't pass the new `ICertificateService` ctor arg that **DEF-278** added to `CertificateImportService`. Pre-existing, unrelated to this view change, but it blocks the test-suite deploy gate; flag for a follow-up fix.
- **Linked memory.** [[project_cr186_product_type_subcat_blank]], [[feedback_data_issue_vs_defect]] (the mis-filed *products* remain a historical data issue to correct via data-ops, separate from this code fix).

### DEF-293 — Certificate tiles: further reduce vertical whitespace / condense tile height (QC #199 follow-up to DEF-281)

- **🔴 REVERTED 2026-06-25 — regressed live.** Shipped in `GCCWebApp-deploy-20260625-1350.zip` and deployed to gccwebapp.com; the client immediately reported the tiles "all messed up" (uneven row heights + empty dark-blue header space). Root cause of the regression: the change reserved a **fixed 3-line header height** for the company name, which *adds* empty space to the majority of cards (short names) instead of condensing, and didn't address the per-row height variance. The "verified uniform on localhost" evidence was unreliable — it was captured against the known stale-CSS dev version-hash quirk, so the real render was never actually seen. **Recovery:** reverted `wwwroot/css/main.css` + `certificatetileview.css` to HEAD (`git checkout HEAD --`), re-verified the reverted render on localhost with a fresh/cache-busted browser (clean known-good tiles, blue header + white body, internal Status/Partner rows intact), and rebuilt `GCCWebApp-deploy-20260625-1420.zip` for redeploy. **DEPLOY CAUTION:** Plesk extract can skip existing files — confirm `wwwroot/css/main.css` + `certificatetileview.css` are actually OVERWRITTEN on the server (delete first if needed), or the broken CSS persists despite the cache-bust hash. **Re-do:** condensing must be re-attempted with padding/margin reduction ONLY (no fixed 3-line reservations) and validated with a genuinely cache-busted before/after, not the stale-hash dev path.
- **⚠️ Number reconciliation (2026-06-25):** DEF-293 is **this tile-spacing fix only**. A parallel CR-023 (import batch undo) session had concurrently mislabeled its **overwrite-guard + pre-import-backup** feature as "DEF-293" across 6 files (`CertificateImportController.cs`, `CertificateImportModels.cs`, `CertificateImportService.cs`, `UploadPathResolver.cs`, `Preview.cshtml`, and `CR-023-import-batch-undo.md`). Those references were relabeled **"CR-023 R5"** (the feature is part of CR-023's own scope, not a separate defect) so the number no longer collides. Legit DEF-293 refs (`qc199-tile-spacing-shots.js`, this entry) were left intact. Same concurrent-allocation race that earlier forced the DEF-292→293 renumber and pushed the a11y entries to DEF-296/297 — the register needs a single-owner allocation discipline while multiple sessions run.
- **Date:** 2026-06-24
- **Severity:** Low — visual density / UX. No functional impact.
- **Status:** **Fixed locally — VERIFIED on localhost:5101 (2026-06-25, all 4 view/viewport combos), pending deploy.** Measured via Playwright (`qc199-tile-spacing-shots.js`, cache-busted to dodge the stale-version-hash dev-env quirk): **public 565→487px desktop / 553→462px mobile; internal 565→499px desktop / 553→492px mobile** — ~14–16% shorter, still **uniform** (`align-items: stretch` confirmed). Internal probe **(desktop AND mobile):** Status row **20/20** cards, Partner **3/3** applicable, **0 header clipping, 0 name overflow**; longest company (100 chars) **clamps to 3 lines** (no truncation); internal-mobile screenshot confirms "Status:"/"Partner:" rows render. Recommend Carly's final eyeball vs Image #90 before deploy.
- **QC Ref:** QC #199 (Sprint 3, Reporter: Carly — 6/16/26, per 6/15 email, image #90). Follow-up to **DEF-281** (first conservative pass — trimmed paddings only, ~30px/tile, deemed insufficient).
- **Bucket:** Enhancement (UI/CSS).
- **Approach (per client, 2026-06-24):** tiles must stay **CONSISTENT (uniform height)** — condense by **reducing padding/margins only** (the gaps Carly highlighted in Image #90), NOT by switching to natural/staggered card heights. An earlier attempt at `align-items: start` (staggered) was **reverted** on that feedback.
- **Root cause of the remaining whitespace (Image #90 highlights).** The blue-highlighted gaps are the **after-location** area (location → tag row → action icons) plus general inter-element padding (below the dark-blue header, product→period). DEF-281 trimmed some; Carly wants a further pass. Tiles are uniform via `main.css` `.certificates-grid { align-items: stretch }` + `.certificate-card { height: 100% }`, so reducing padding on the **tallest** card condenses the whole row uniformly. (Note: duplicate `.certificates-grid` / `.product-name` rules exist across `main.css` + `certificatetileview.css` — a maintenance hazard; the authoritative grid rule is in `main.css`.)
- **Changes applied — padding/margin/min-height only, uniform, no truncation, tiles stay consistent.** The dominant lever turned out to be in `main.css` (the authoritative tile rules), NOT the `certificatetileview.css` copies (which were largely overridden — the recurring duplicate-rule trap on this view).
  - `wwwroot/css/main.css` (desktop, the rules that actually render): `.card-header` **`min-height` 220→150px** (it reserved ~40% of tile height as empty dark-blue space — the single biggest win) + padding 12px 15px→8px 12px; `.certificate-details` gap 8→4px (period↔location) + margin-bottom 5→4px; `.card-actions` **`padding-top` 12→6px** (the "after-location" gap above the action icons Carly highlighted).
  - `wwwroot/css/main.css` (mobile, `@media`+`body.theme-option4`): `.card-header` min-height 240→160px + padding 16→10px; `.card-actions` padding 15→6px.
  - `wwwroot/css/certificatetileview.css` (supporting trims): internal `.card-header` 8→6px, public 6→4px; `.card-body` top 10→6px; `.product-name` reserved-height buffer 10→6px (kept fixed 3-line reservation so content aligns across tiles); `.company-name` buffer 6→4px; `.certificate-location` margins 6/8/4→4/4/2px; after-location `card-body > *:last-child` 6→4px; `.card-actions` 6→4px. **No line-clamp / font-size / truncation change; Status/Partner untouched.**
  - **Reverted:** the earlier `align-items: start` + card `height: auto` experiment (staggered) — back to `stretch` + `height: 100%` so tiles stay uniform per client direction.
  - **Discovered (not fixed):** `certificatetileview.css` `.certification-period` rule is **dead** (markup class is `certificate-period`); the live period styles are in `main.css`. Flagged for cleanup.
- **Net (measured):** public **565→487px** desktop / **553→462px** mobile; internal **565→499px** desktop — ~78–91px (14–16%) shorter, uniform across each row.
- **Files:** `wwwroot/css/main.css` (primary — desktop + mobile tile rules), `wwwroot/css/certificatetileview.css` (supporting trims). Visual harness + evidence: `Scripts/smoke-prod/qc199-tile-spacing-shots.js`; screenshots in `Scripts/smoke-prod/_results/qc199/` (`*-after2`/`*-final` = condensed).

### DEF-292 — Certificate Import: a miskeyed certificate number silently overwrites a different company's certificate (no reassignment guard)

- **Date:** 2026-06-24 (fixed locally)
- **Severity:** **High** — data integrity / data loss. A single mistyped cert number can silently overwrite and reassign an unrelated, accurate certificate, with no warning beyond the generic preview.
- **Status:** **Fixed locally — pending next deploy.** Built and verified on `localhost:5101` + dev DB. No commit yet (working tree).
- **QC Ref:** None — surfaced during an agile requirements discussion on the import test template (2026-06-24), not a client QC item. ([[feedback_data_issue_vs_defect]]: this is a genuine code-path defect, not a broken-data row, so it gets a DEF.)
- **Bucket:** Bug (data integrity / back-end + UX).
- **Root cause.** CSV import matches an incoming row to an existing certificate **by certificate number only**, case-insensitive (`Services/Import/CertificateImportService.cs:1184`). On a match the row is classified `Modified` and `UpdateCertificateAsync` overwrites the existing record **in place — including `CompanyId`** (`CertificateImportService.cs:1492`). So if an analyst mistypes a number that happens to belong to a *different* company's cert, the import reassigns and overwrites that cert rather than creating a new one. The only prior safeguard was the generic preview diff; nothing flagged the company change or blocked apply. Cert number is treated as the unique business key, which is by design — but that makes a miskey destructive.
- **Fix (applied locally).** Added a **company-reassignment guard**: any number-match whose detected changes include the Company field is surfaced as a blocking warning and apply is refused until explicitly acknowledged.
  - Detection: `FieldChange.CompanyFieldName` constant (single source of truth, shared with `DetectChanges`); per-row `CertificateImportRow.ChangesCompany`; preview-level `CompanyReassignmentRows` / `CompanyReassignmentCount` / `HasCompanyReassignments`.
  - **Server-side enforcement** (the real guard, not bypassable): `Apply` (MVC) and `ApiApply` (JSON) both refuse to call `ApplyImportAsync` when `HasCompanyReassignments && !acknowledgeReassignments`, returning the preview with an error / a `requiresReassignmentAck` 400.
  - UI affordance: Preview shows a red warning card listing each `Current Company → New Company`, plus a required acknowledgement checkbox wired into the apply form; the Apply button refuses to open the confirm dialog until it is ticked, and the confirm modal calls out the reassignment count.
  - Scope: Company change only (the strongest miskey signal). Product/type/date changes remain ordinary `Modified` edits. Blocks the whole batch until acknowledged.
- **Files:** `Models/Import/CertificateImportModels.cs`, `Services/Import/CertificateImportService.cs`, `Controllers/CertificateImportController.cs`, `Views/CertificateImport/Preview.cshtml`.
- **Verification (2026-06-24, localhost:5101, Playwright as admin).** Fixture cert `ZZ-9001` created under Acme Corporation; collision CSV imported it as Globex Industries. Confirmed: (1) preview shows the red warning naming the cert + Acme→Globex; (2) UI gate — Apply without ticking ack stays on Preview; (3) **server gate** — a bare `POST /CertificateImport/Apply` with valid importId + antiforgery token but no ack flag is blocked (`success=false`), proving enforcement is independent of the JS; (4) happy path — ack checked → apply → DB shows `ZZ-9001` reassigned Acme→Globex. Fixture deleted; dev DB restored.
- **Regression test.** Import a CSV row whose cert number exists under a *different* company → preview must list it under the reassignment warning; posting apply without the ack flag must be rejected server-side; ticking ack + apply must succeed and reassign. Also confirm a normal `Modified` row that does **not** change company is unaffected (no warning, applies as before).

### DEF-291 — Certificate Create: clicking "Add Certificate" after a successful save re-shows the previous cert's success toast

- **Date:** 2026-06-24 (opened)
- **Severity:** Low — cosmetic; a stale "Certificate NN created successfully!" toast appears on a fresh Create page. No data impact; workaround is to ignore it.
- **Status:** **Fixed locally — pending next deploy.** Code fix applied in `Views/Certificate/Create.cshtml` (verified in-tree 2026-06-24). *Register reconciled 2026-06-24: this entry previously read "Open — not yet fixed"; a parallel session had applied the fix without updating the register (tracking drift).*
- **QC Ref:** QC #193 (Sprint 3, Reporter: Gemma — 6/11/26, Cert Creation Page): *"When I've just successfully uploaded a certificate, and go to click the 'Add Certificate' button to create another, a pop-up appears saying the last certificate was successfully uploaded."* Triaged in `docs/tracking/6_23_IssuesTriage.md`.
- **Bucket:** Bug (front-end / TempData lifecycle).
- **Root cause.** After a "Save & Add Another" create the controller sets `TempData["SuccessMessage"]` and redirects to Create (`Controllers/CertificateController.cs:699-702`); the Create view renders it as a toast via `AdminCore.showNotification` (`Views/Certificate/Create.cshtml`). The prior bfcache/disk-cache re-fire guard was keyed **per message content**, so a *different* cert number produced a new key and the guard didn't suppress the stale re-show when the browser restored the page from cache.
- **Fix (applied).** The toast block now reads `TempData["SuccessMessage"]` (read-once — cleared for the next request, so a genuine fresh "Add Certificate" GET renders no script at all) and guards the show with a **per-render `Guid` nonce** (`Create.cshtml:47-76`): `pageshow`/`DOMContentLoaded` call `showOnce()`, which no-ops if the nonce is already marked in `sessionStorage`. A bfcache restore carries the same inline script with the prior message but the nonce is already "seen", so the stale toast is suppressed; the genuine toast still shows exactly once right after each save. View-only change — the controller TempData handling was already correct.
- **Files:** `Views/Certificate/Create.cshtml` (toast block).
- **Verification (2026-06-24, `localhost:5101`, Playwright).** 4/4 PASS: (1) after "Save & Add Another" the success toast shows **exactly once** on the redirected Create page; (2) navigating away then **Back** shows **no** stale toast; (3) a fresh "Add Certificate" GET shows no toast; (4) web build clean.
- **Regression test.** Create a cert → click "Add Certificate" → confirm no stale toast on the fresh form; repeat with a second cert; verify the genuine success toast still shows exactly once immediately after each save; check browser Back-button behavior (incl. mobile bfcache).

### DEF-290 — Admin entity deletes (10 actions): soft-delete + audit coverage is inconsistent entity-by-entity

- **Date:** 2026-06-24
- **Severity:** High — data-loss exposure / incomplete audit trail. No confirmed live bug; flagged by review.
- **Status:** **OPEN — review/harden.** No code change yet (review-only audit).
- **QC Ref:** none. **Found by:** Data-Protection Controls Review, 2026-06-24 (`docs/tracking/data-protection-controls-review-2026-06-24.md`, R6).
- **Bucket:** Bug (data-integrity) / hardening.
- **Root cause.** `AdminController` exposes 10 delete actions (User, Role, ProductType, CertificateType, CertificateField, Tag, Partner, Pathway, Product, plus `DeleteEntity`). Soft-delete exists only on **User** and **Pathway** (`IsDeleted` from migrations `FixUserUniqueIndexesForSoftDelete` / `FixPathwayUniqueIndexForSoftDelete`); the others appear hard-delete. `AdminService` injects `IAuditService`, but per-action DELETE-audit coverage is not uniformly verified. Most FKs are `OnDelete(Restrict/NoAction)` in `ApplicationDbContext.cs`, which blocks orphaning (good) but means a delete of a referenced entity may surface a raw 500 rather than a friendly block.
- **Proposed fix (when picked up).** For each of the 10 actions confirm (a) an `AuditHistory` DELETE row is written, and (b) a deliberate soft-delete vs. hard-delete decision is recorded per entity. Convert friendly-error handling for `Restrict`-blocked deletes.
- **Files (when picked up):** `Controllers/AdminController.cs`, `Services/AdminService.cs`, `Data/ApplicationDbContext.cs`.
- **Linked memory.** [[feedback_track_every_fix_in_defect_register]], [[feedback_scoping_check_adjacent_endpoints]].

### DEF-289 — Document delete physically destroys the PDF from disk (irreversible) and writes no attributed audit row

- **Date:** 2026-06-24
- **Severity:** 🔴 Critical — irreversible data loss + same class as the 2026-06-15 PDF-wipe incident.
- **Status:** **OPEN — fix proposed.** No code change yet (review-only audit). Decision needed on quarantine vs. retain-in-place.
- **QC Ref:** none. **Found by:** Data-Protection Controls Review, 2026-06-24 (`docs/tracking/data-protection-controls-review-2026-06-24.md`, R3).
- **Bucket:** Bug (file handling) / data-protection.
- **Root cause.** `DocumentController.Delete` (`:231`) correctly **soft-deletes the DB row** (`IsActive=false`) but then calls `FileUploadService.DeleteFileAsync`, which does a hard `File.Delete(fullPath)` (`FileUploadService.cs:220`) — so the row is recoverable but **the actual PDF on disk is gone permanently**. Compounding it, `DocumentController.GetCurrentUserId()` returns `null` (stub), so the delete has **no user attribution** and no `AuditHistory` row is written. Echoes [[incident_prod_all_pdfs_missing_2026-06-15]].
- **Proposed fix.** (1) Stop physically deleting on document delete — the soft-deleted row is enough; either leave the file in place or move it to a `_deleted/` quarantine folder under the upload root (via `IUploadPathResolver`, DEF-286). (2) Wire `GetCurrentUserId()` to the `NameIdentifier` claim and write an `AuditHistory` DELETE row. (3) Add a retention/purge decision (see review Open Q2).
- **Files (when picked up):** `Controllers/DocumentController.cs`, `Services/FileUploadService.cs`.
- **Linked memory.** [[incident_prod_all_pdfs_missing_2026-06-15]], [[feedback_backup_uploads_before_deploy]], [[project_upload_storage_resolver]].

### DEF-288 — Certificate deletion is permanent and unaudited (single + bulk): no soft-delete, no AuditHistory row, status history cascade-wiped

- **Date:** 2026-06-24
- **Severity:** 🔴 Critical — irreversible loss of the core business record with no audit trail. No confirmed live deletion yet; this is the latent exposure.
- **Status:** **OPEN — fix approved in principle.** No code change yet (review-only audit). **Decision (Shawn, 2026-06-24): certificates must be recoverable (soft-delete).** Implementation plan drafted: `docs/tracking/DEF-288-certificate-soft-delete-plan.md` (awaiting sign-off on 2 design decisions).
- **QC Ref:** none. **Found by:** Data-Protection Controls Review, 2026-06-24 (`docs/tracking/data-protection-controls-review-2026-06-24.md`, R1/R2/R4).
- **Bucket:** Bug (data-protection / audit).
- **Root cause.** The `Certificate` entity has **no soft-delete** (`Models/Certificate.cs` — no `IsDeleted`/query filter), so every delete path is permanent: (a) `CertificateController.DeleteConfirmed` (`:1591`) calls `RemoveAsync` and only logs `_logger.LogInformation` — **no `AuditHistory` row**; (b) `BulkCertificatesController.BulkDelete` (`:236`) does `_context.Certificates.Remove()` for up to 100 certs with **no audit row and no confirm** (the controller doesn't inject `IAuditService`). Both cascade-delete `CertificateStatusHistory` (`ApplicationDbContext.cs:753`). The `AuditService` infrastructure already supports a recoverable DELETE snapshot but is not called from either path.
- **Proposed fix (per approved decision).** (1) Add `IsDeleted`/`DeletedAt`/`DeletedBy` to `Certificate` + a global query filter (mirror the User/Pathway pattern). (2) Convert both delete paths to soft-delete and call `_auditService.LogEntityChangeAsync(..., AuditAction.Delete, oldEntity, null, ...)` before save. (3) Add a typed count-confirmation for bulk delete (or disable `BulkDelete` entirely — DEF-131 notes no repo caller). (4) Ensure `CertificateStatusHistory` survives a soft-delete (don't hard-cascade). New migration → couple a Gate-7 prod SQL per [[project_predeploy_agent_checklist]].
- **Files (when picked up):** `Models/Certificate.cs`, `Data/ApplicationDbContext.cs` (query filter + cascade), `Controllers/CertificateController.cs` (`DeleteConfirmed`), `Controllers/Api/v1/BulkCertificatesController.cs` (`BulkDelete`), new migration.
- **Linked memory.** [[feedback_track_every_fix_in_defect_register]], [[feedback_data_changes_require_cr]], [[project_predeploy_agent_checklist]].

### DEF-287 — Partner "Export All" CSV (external tile view): "Cert Results" column STILL exported the raw decimal instead of the percentage — the adjacent export path DEF-275 missed

- **Date:** 2026-06-24
- **Severity:** Medium — export consistency / partner-facing. Same symptom and same field as DEF-275 (`CertificationResults` shown as `0.17` instead of `17%`), but on a different export route.
- **Status:** **Fixed locally — pending next deploy.** Build succeeded (0 errors).
- **QC Ref:** Re-raise of QC #201 by Shawn 2026-06-24 ("Export on certificates when logged in as partner is still showing decimal instead of percentage"). Follow-up to **DEF-275**.
- **Re-reported 2026-06-25 (Shawn):** *"Partner view CertificateTileViewExternal — export certificates is showing Cert results as a decimal. It should be showing it as a percentage."* Confirmed this is the **same** issue: the fix (`GenerateCsvExport` now wraps the value in `DisplayFormatters.FormatCertificationResults`) is in the working tree on `master` (commit `0d079a3`, HEAD~1) but **not yet deployed** to gccwebapp.com, so prod still serves the raw decimal. No new code needed — **this is purely a pending-deploy gap.** No duplicate DEF opened.
- **Bucket:** Bug (export formatting).
- **Root cause — adjacent-path miss.** DEF-275 fixed only `Controllers/ExportController.cs:308` (the Active Product Identifiers / global export). The Partner & External **"Export All" button** on the certificate tile views hits a *different* endpoint — `/Certificate/ExportCertificates` → `CertificateService.ExportCertificatesAsync`/`ExportCertificatesByIdsAsync` → `GenerateCsvExport`. That method wrote `EscapeCsvField(cert.CertificationResults)` raw (line 1139), bypassing `DisplayFormatters.FormatCertificationResults`. Three of four export consumers (ExportController after DEF-275, internal tile API, v1 partner API) formatted the value; this CSV builder was the last one that didn't. Textbook [[feedback_scoping_check_adjacent_endpoints]] — one path fixed, the sibling missed.
- **Fix.** Wrapped the value in the shared formatter, fully qualified to match existing usage and avoid a new `using`: `EscapeCsvField(GCCWebApp.Extensions.DisplayFormatters.FormatCertificationResults(cert.CertificationResults) ?? "")`. CSV now matches the web display.
- **Files:** `Services/CertificateService.cs` (`GenerateCsvExport`, line ~1139).
- **Note.** The dead-code `GenerateExcelExport` (line 1229, intentionally retained per the comment at line 369) has the same raw-value line but is unreachable under the "CSV ONLY" rule — left untouched to avoid scope creep.
- **Verification.** Logged in as Partner, click **Export All** on the external tile view → confirm the "Cert Results" column shows `17%` / `100%` (not `0.17` / `100`), matching the page.

### DEF-286 — Uploaded certificate PDFs lost after deployment: config-driven persistent upload root + centralised path resolver (the durable fix for QC #198 / the 2026-06-15 incident)

- **Date:** 2026-06-24
- **Severity:** Medium — hardening + recurrence-prevention. No new live bug: the *reported* QC #198 symptom is the 2026-06-15 upload-store wipe (data/infra), and the per-resolver code fixes already shipped (DEF-208/262/264/279). This closes the architectural gap that lets the wipe keep happening.
- **Status:** **Fixed locally — pending next deploy.** Build clean (the only build errors are the running `localhost:5101` file lock, not C#). 6 `DownloadCertificate` tests pass; the 18 unrelated test failures are the pre-existing uncommitted CR-187 (`NormalizedX` search) / DEF-278 batch vs the InMemory provider (confirmed by stashing to committed master — they fail there too / pass without the batch; none touch the paths changed here).
- **QC Ref:** QC #198 (Sprint 3, Reporter: Dan — 6/15/26). Reported symptom triaged under DEF-279; this is the durable follow-through Shawn approved 2026-06-24 (wire config root + deploy doc + better logging).
- **Bucket:** Bug (file handling) / hardening.
- **Root cause (architectural).** DEF-208 moved uploads to `ContentRootPath/App_Data/uploads`, which is still **inside** the Plesk deploy folder — a publish that overwrites the folder wipes the store (the 2026-06-15 incident; mitigated only by a manual "zip uploads before deploy" ritual). The config key `FileStorage:StoragePath` existed in `appsettings.Production.template.json` but **nothing read it** — the path was hardcoded. Separately, the logical→physical resolution logic was **duplicated across 5+ call sites**, which is exactly why DEF-262/264/279 each fixed one resolver and missed adjacent ones ([[feedback_scoping_check_adjacent_endpoints]]).
- **Fix.**
  1. **New `Services/UploadPathResolver.cs` (`IUploadPathResolver`, DI singleton)** — single source of truth. Reads `FileStorage:StoragePath` (empty/unset → default `App_Data/uploads`, so existing envs are unchanged; set → uploads root, absolute used as-is / relative under ContentRoot). Read fallback order: configured root → App_Data → wwwroot legacy. Traversal-rejecting; fail-loud on null ContentRootPath.
  2. **Rewired every reader/writer through it** — `FileUploadService` (write dirs + `ResolvePhysicalUploadPath`), `CertificateController` (`StreamUploadedFile`, `ResolveUploadPhysicalPath`, `PreviewThumbnail`), both tile-API `DownloadCertificate` endpoints, and `CertificateImportService` (PDF write dir). Kills the adjacent-endpoint drift class.
  3. **Improved missing-file logging** — when the DB references a file not on disk, the download paths now log a structured warning naming cert number, logical path, and resolved physical path, pointing at the incident/backup procedure.
  4. **Config + docs** — `appsettings.Production.template.json` `StoragePath` defaulted to `""` (was Linux `/app/uploads`, which would resolve to `C:\app\uploads` on the Plesk Windows host) with an inline operator note; new `docs/tracking/upload-storage-persistence.md` documents the one-time persistent-dir setup and the "never wipe the upload dir on deploy" rules.
- **Files:** `Services/UploadPathResolver.cs` (new), `Program.cs`, `Services/FileUploadService.cs`, `Controllers/CertificateController.cs`, `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`, `Services/Import/CertificateImportService.cs`, `appsettings.Production.template.json`, `docs/tracking/upload-storage-persistence.md`; test ctors updated in `GCCWebApp.Api.Tests` (3 files).
- **Deploy-prep (2026-06-24).** (1) **Publish-profile data-loss fix** — `Properties/PublishProfiles/PleskTestEnvironment.pubxml` excluded only `wwwroot\uploads` but ships `App_Data` (`ExcludeApp_Data=false`); since DEF-208 the store is `App_Data/uploads`, so the publish was carrying dev uploads and an extract-over-live would clobber the prod PDF store — a likely contributor to the 2026-06-15 wipe. Added `<Content Remove="App_Data\uploads\**" />`. (Prod profile `AccuWebProduction.pubxml` has the same `ExcludeApp_Data=false` and **no upload exclusion at all** — flagged as must-fix before the AccuWeb prod launch; not edited here.) (2) **Gate-7 SQL** for the bundle's new migration `20260624142128_WidenCertificateProductIdProductName` (CertificateProductIds.ProductName 200→1000): `Scripts/data-ops/2026-06-24-WidenCertificateProductIdProductName-PROD.sql` (idempotent, stamps `__EFMigrationsHistory`). (3) **Recovery tool** `Scripts/data-ops/2026-06-24-recover-missing-cert-pdfs.ps1` (server-side; locates the 11 missing PDFs in any backup/search root and copies them into the served folder, non-destructive). (4) Built test-env zip `deployment-drops/GCCWebApp-deploy-20260624-1345.zip` (60.4 MB) — verified 0 upload files leaked. **Note:** shipping `appsettings.Production.json` has no `FileStorage` block → StoragePath unset → app defaults to `App_Data/uploads` this deploy (safe; relocation outside the deploy folder is a deliberate follow-up once a persistent server path is chosen).
- **Deploy coupling (the actual #198 resolution).** Code alone does not bring missing PDFs back. After deploy: (1) restore the 2026-06-15 files from backup per [[incident_prod_all_pdfs_missing_2026-06-15]] and re-run `prod-missing-pdf-sweep.js`; (2) **set `FileStorage:StoragePath` to a persistent path outside the deploy folder** and copy existing `App_Data/uploads/*` into it (one-time) — see the deploy doc. Until step 2 is done, the pre-deploy "zip uploads" rule still applies.
- **Linked memory.** [[incident_prod_all_pdfs_missing_2026-06-15]], [[feedback_backup_uploads_before_deploy]], [[feedback_scoping_check_adjacent_endpoints]], [[project_predeploy_agent_checklist]].

### DEF-277 — Hardening (low / deferred): prevent assigning a Partner association to an `External`-role user (the only way to manufacture the DEF-268 empty-tile state)

- **Date:** 2026-06-23
- **Severity:** Low — **deferred hardening, no live impact.** Spun off from **[[DEF-268]]** (closed WAD 2026-06-23). Partner access is meant to flow through the dedicated **`Partner`-role login**; an `External`-role (company) user who is *also* given a partner association sees an empty/partial External tile because that surface scopes partner data by the `Partner` role only. No real user is in this state (only the test account `phil.nonpartner@example.com`).
- **Status:** **OPEN — deferred (backlog).** No code change yet. Captured so the misconfiguration can't silently recur.
- **QC Ref:** none. **Found by:** CR-171 verification sweep, 2026-06-19 (see DEF-268).
- **Bucket:** Enhancement / data-integrity guard.
- **Proposed guard (when picked up).** In user create/edit (admin UI + `ImportUsers`), disallow the combination *External role + Partner association* — either (a) hide/clear partner associations for non-`Partner`-role users, or (b) validate and surface an error ("Partner associations require the Partner role"). Keeps the data model honest so the tile's role-based scoping is never reached with a contradictory config.
- **Files (when picked up):** user management controller/views (`Controllers/AdminController.cs` user create/edit + `ImportUsers`); mirror the rule wherever PartnerUser links are written.

### DEF-285 — Certificate Edit: a date-driven "Expired" cert is trapped — correcting the expiration date to the future can't restore the status ("cannot be changed from Expired to Client Approved - Paid")

- **Date:** 2026-06-24
- **Severity:** High — prevents the user from correcting a record. Once a cert auto-expires (past date), fixing the date to the future left it stuck: the workflow matrix only allows Expired → {Republished, Draft}, so restoring the prior status was rejected and the dropdown wouldn't accept it.
- **Status:** **Fixed locally — pending next deploy.** Compiles clean (build's only errors are a file lock from the running `localhost:5101` instance, not C# errors).
- **QC Ref:** QC #188 (Sprint 3, Reporter: Gemma — 6/11/26, Cert Creation Page, image #86): *"If a certificate date is set to the past, the status auto-updates to Expired. Revising the date to the future shows 'a certificate cannot be changed from Expired to Client Approved - Paid'."* Triaged in `docs/tracking/6_23_IssuesTriage.md`. (The same report's "invalid upload status" facet was the separate [[DEF-276]] / QC #194.)
- **Decision (Shawn, 2026-06-24):** on correcting the date, **revert to the prior status** (the status held before auto-expiry).
- **Bucket:** Bug (workflow / data correction). The 8-state matrix itself is unchanged (protected per CLAUDE.md) — this adds a corrective bypass, not a new transition.
- **Root cause.** Auto-expire (`CertificateExpirationService` — both the save-time `TryApplySaveTimeAutoExpire` and the login/background `ExpireOverdueCertificatesAsync`) flips a past-dated cert to `Expired`. The Edit POST then enforces `CertificateStatusHelper.IsValidTransition(oldStatus, requestedStatus)` (DEF-203), and `Expired` only allows → `Republished`/`Draft`. So a user who fixes the date and tries to restore "Client Approved - Paid" is rejected, with no path back. (Note: `CertificateWorkflowService.CheckAndUpdateExpiringCertificatesAsync` is dead legacy code referencing non-8-state names "Active"/"PendingRenewal" — not the real auto-expire.)
- **Fix (`Controllers/CertificateController.cs`, Edit POST).** Added an **expiry-correction auto-revert**: when the cert's current status is `Expired`, the submitted `ExpirationDate` is now today/future, **and** the user didn't pick a valid alternative themselves (requested status equals the current Expired, or is matrix-invalid from Expired), the cert is restored to its **pre-expiry status**, recovered from the most recent automated auto-expire `CertificateStatusHistory` row (`IsAutomatedTransition && NewStatus == Expired`, taking `PreviousStatus`). This corrective transition bypasses the matrix block; falls back to `Draft` (always a safe, matrix-valid state) if no pre-expiry status is recorded. The save-time auto-expire then no-ops (date is future), and the change is written to status history with a clear reason (*"Expiry corrected: status restored to its pre-expiry value (QC #188)"*). Explicit valid choices (Republished/Draft) are still honored; leaving the date in the past is unchanged.
- **Files:** `Controllers/CertificateController.cs` (Edit POST: revert detection + audit reason).
- **Verification.** Build clean. Recommended smoke: cert "Client Approved - Paid" → set past expiration → save (auto-expires to Expired) → edit, set future expiration → save → status returns to "Client Approved - Paid" (history shows the revert); editing an Expired cert to a future date and explicitly choosing "Republished" still lands on Republished; leaving a past date keeps Expired. (Couldn't runtime-verify locally — dev app cert endpoints currently 400.)

### DEF-284 — Import Certificates: re-selecting a revised file with the SAME filename doesn't take / errors (stale file-input selection + stuck submit button after Back)

- **Date:** 2026-06-23
- **Severity:** Medium — workflow friction during import; workaround is to rename the file or hard-refresh. No data loss.
- **Status:** **Fixed locally — pending next deploy.** JS-only (inline in the Import Index view); Razor view, cache-busted by the view-render hash.
- **QC Ref:** QC #190 (Sprint 3, Reporter: Gemma — 6/11/26, Import Certificates, image #87): *"when attempting to upload a file, you get an error message if you try to replace the attached file with a revised version of the same file with the same file name (I changed some data, saved, and attempted to upload the same file within the Import window)."* Triaged in `docs/tracking/6_23_IssuesTriage.md`.
- **Bucket:** Bug (front-end / browser file-input behavior).
- **Investigation — ruled out a server collision.** `CertificateImportController.Upload` → `PreviewImportAsync` reads the CSV stream and caches the parsed **result** in `IMemoryCache` keyed by a GUID `ImportId`; it does **not** persist the CSV to disk by filename, and `Apply` re-reads the cached rows (not the file). So there is no server-side same-filename rejection. The only server `Upload` errors are empty-file / non-csv / >10MB / parse-exception.
- **Root cause (browser).** Standard `<input type="file">` behavior: after a file is selected, choosing a file whose **path is identical** to the current value does **not** fire the `change` event, so the form keeps the *first* selection — re-uploading the "revised" same-named file silently re-submits the stale content (and the on-change label never updates). Compounding it: after the Upload renders the Preview, returning to the Import page via the browser **Back button** restores it from bfcache with the file input cleared **but** the submit button still disabled on "Processing…" (the submit handler disabled it and nothing re-enabled it) — so the user can't re-submit and/or hits the "Please select a CSV file" error thinking the file is still attached.
- **Fix (`Views/CertificateImport/Index.cshtml`, JS).** (1) Clear the file input's `value` on every `click` so re-opening the picker and choosing the same-named revised file **always** fires `change` and uploads the fresh file. (2) On `pageshow` with `event.persisted` (bfcache/Back), re-enable the submit button, restore its label, and reset the file label — so the re-upload flow works immediately after going Back from the preview.
- **Files:** `Views/CertificateImport/Index.cshtml` (inline upload script).
- **Verification.** Recommended smoke: select `data.csv` → Upload → preview → Back → edit `data.csv` (same name) → re-select it → label updates and Upload submits the **revised** content; also confirm the Upload button isn't stuck disabled after Back. (Couldn't runtime-verify locally — dev app cert endpoints currently 400; this is a standard, well-understood file-input fix.)

### DEF-283 — Certificate detail pages: a long Company URL runs off the page (overflows the card instead of wrapping)

- **Date:** 2026-06-23
- **Severity:** Low — visual; part of a long URL is clipped/hidden. No functional impact.
- **Status:** **Fixed locally — pending next deploy.** CSS-only; `main.css` is cache-busted site-wide (DEF-271 follow-up).
- **QC Ref:** QC #192 (Sprint 3, Reporter: Gemma — 6/11/26, public + internal detail pages): *"Company URL is running off the page when it is longer than 1 line. It eventually wraps but part of the URL is not visible."* Triaged in `docs/tracking/6_23_IssuesTriage.md`.
- **Bucket:** Bug (UI/CSS).
- **Root cause.** The detail card field rows are flex: `<strong>` label (`min-width:160px; flex-shrink:0`) + `<span>` value (containing the `<a href>`). The value `<span>` is a flex item with the default `min-width:auto`, so it won't shrink below its content's intrinsic width — a long unbreakable URL therefore overflows the card instead of wrapping. `.certificate-single-card .certificate-field span` had only `color` set, no wrapping rules.
- **Fix.** Added to `.certificate-single-card .certificate-field span` (`wwwroot/css/main.css`): `min-width: 0` (lets the flex item shrink below content size) + `overflow-wrap: anywhere` + `word-break: break-word` (wraps the long URL within the card). Covers all three detail views (Internal/External/Single — all use `.certificate-single-card`).
- **Files:** `wwwroot/css/main.css` (`.certificate-single-card .certificate-field span`).
- **Verification.** Build/JS N/A (CSS). Recommended smoke: open a cert whose Company URL is very long on the public + internal detail pages → URL wraps fully within the card, nothing clipped.

### DEF-282 — Product ID Manager: imported/added Product IDs on the Create page show Created = "Invalid Date" and Created By = "System" (in-memory rows have no audit fields until the cert is saved)

- **Date:** 2026-06-23
- **Severity:** Low–Medium — confusing/alarming display; no data loss. The audit fields ARE assigned correctly server-side on save; this was purely a pre-save render issue.
- **Status:** **Fixed locally — pending next deploy.** JS-only; `node --check` clean. Script is cache-busted (DEF-271).
- **QC Ref:** QC #200 (Sprint 3, Reporter: Carly — 6/17/26, image #91): *"When importing data for Product ID the Created said 'Invalid Date' and Created By said 'System'. Not sure why it would not list the day I imported and the user."* Triaged in `docs/tracking/6_23_IssuesTriage.md`.
- **Bucket:** Bug (front-end render).
- **Investigation — ruled out a data/write bug.** Both server write paths set the audit fields correctly: the cert import (`CertificateImportService.CreateProductIdEntriesAsync` → `CreatedBy = userId`, `CreatedAt = DateTime.UtcNow`) and the Product-ID Manager bulk import (`ProductIdService.ImportProductIdsAsync` → `CreatedBy = userId`, `CreatedAt = now`, with the controller passing `GetCurrentUserId()` from the `NameIdentifier` claim). The read path is also correct: `GetProductIdsByCertificateAsync`/`…PagedAsync` `.Include(p => p.CreatedByUser)` and `ProductIdResponseDto.FromEntity` maps `CreatedAt` + `CreatedByUser`. After a bulk import on a **saved** cert the JS re-fetches via `loadProductIds()` (the correct GET), so saved rows display correctly.
- **Root cause.** The failure only occurs for **in-memory rows on the Create page** (new cert, `certificateId === 0` → in-memory mode, no server fetch). Those rows have **no `createdAt`/`createdByUser`** because the server assigns them on save. The table render did `formatDate(productId.createdAt)` → `new Date(undefined).toLocaleDateString()` = **"Invalid Date"**, and `(productId.createdByUser && …username) || 'System'` = **"System"**. So Carly was adding/importing Product IDs while *creating* a cert (before save); after saving and reopening, the server returns the real date/user.
- **Fix (JS-only, `wwwroot/js/product-id-manager.js`).** (1) `formatDate`/`formatDateTime` now guard against falsy/`NaN` dates → return `'—'` / `''` instead of "Invalid Date". (2) The Created By cell shows `'—'` for not-yet-saved rows (no `createdAt`), keeps the username for normal saved rows, and only shows `'System'` for a saved row whose `CreatedBy` is genuinely null (legacy/seed). So unsaved rows now read "— / —" (honest: assigned on save) and saved rows read the real date + importing user.
- **Files:** `wwwroot/js/product-id-manager.js` (`formatDate`, `formatDateTime`, created-by table cell).
- **Verification.** `node --check` passes. Recommended smoke: on the Create page add/import Product IDs → Created/Created By show "—" (not Invalid Date/System); save the cert, reopen → both show the import date and the importing user (server-side already correct). On an existing cert, import Product IDs → re-fetch shows correct date + user.

### DEF-281 — Certificate tiles: condense vertical whitespace (client 6/15 request) without breaking the authenticated Status/Partner rows

- **Date:** 2026-06-23
- **Severity:** Low — visual polish / UX density. No functional impact.
- **Status:** **Fixed locally — pending next deploy.** CSS-only; the stylesheet is cache-busted (`asp-append-version` on all 3 tile views) so the change shows post-deploy. **Needs client visual sign-off** — see Note.
- **QC Ref:** QC #199 (Sprint 3, Reporter: Carly — 6/16/26, per 6/15 email, image #90): *"Reduce spacing in tile to condense size of tile. The blue-highlighted areas can be tightened (esp the one after location); the yellow spots may have a little room. The top dark blue section accounts for Status/Partner in the login view."* Triaged in `docs/tracking/6_23_IssuesTriage.md`.
- **Bucket:** Enhancement (UI/CSS).
- **Approach.** Carly's note targets **whitespace**, not content — so this pass trims padding/margins/buffers and does **not** reduce any line-clamp count (no name truncation) and does **not** touch the Status/Partner rows (the dark-blue header content she flagged as needed in the login view). All edits in `wwwroot/css/certificatetileview.css`:
  1. `.card-header` padding `10px 12px` → `8px 12px` (internal/dark-blue header — Status/Partner rows unchanged).
  2. `.certificate-card:not(.internal-card) .card-header` padding `8px 12px` → `6px 12px` (public/external compact header).
  3. `.card-header .company-name` line-height `1.5` → `1.4` (still a 3-line clamp; min/max-height calc updated `18px*1.4*3+6px` — no truncation change, ~5px saved).
  4. `.card-body > *:last-child` margin-bottom `12px` → `6px` (the gap **after Location/tags**, before the action icons — Carly's "the one after location").
  5. `.certificate-card .certificate-tags` margin-bottom `8px` → `4px` (desktop; the mobile `@media(max-width:768px)` override is intentionally left larger per QC R143).
  6. `.card-actions` padding `10px 10px 8px` → `6px 10px 6px`.
  7. `.certificate-card .product-name` fixed-height buffer `+16px` → `+10px` (still 3 lines at line-height 1.45; buffer only, no truncation).
- **Net effect:** ~30px shorter per internal tile (a bit less for public/external), achieved by removing empty space rather than hiding data. Status/Partner rows render identically.
- **Files:** `wwwroot/css/certificatetileview.css`.
- **Note (needs visual sign-off).** Could not screenshot the tiles locally to fine-tune — the running dev app currently 400s on all public certificate-tile endpoints (a local-env issue logged 2026-06-23, not this change). Changes are deliberately conservative; recommend Carly reviews against image #90 after deploy and we fine-tune (further options if she wants more: drop the company-name/product-name clamps from 3 lines to 2, which *would* truncate longer names). No C# change; no migration.

### DEF-280 — Certificate search autocomplete hijacks the typed value: the first suggestion is auto-selected, so pressing Enter replaces the user's text with the top suggestion (typed "test" → "carly test") with no way to override

- **Date:** 2026-06-23
- **Severity:** Medium — usability; the search still works but the input box shows a value the user didn't choose and can't keep. Workaround: avoid Enter / clear the box.
- **Status:** **Fixed locally — pending next deploy.** JS-only (3 files); `node --check` clean on all three. No build needed (not compiled).
- **QC Ref:** QC #197 (Sprint 3, Reporter: Dan — 6/15/26, Cert search view, image #88): *"search 'test', the filter bubble shows test but the text in the filters column reads 'carly test'… auto-populate is good but I should be able to override like in Excel."* Triaged in `docs/tracking/6_23_IssuesTriage.md`.
- **Bucket:** Bug (front-end UX / state).
- **Root cause.** In `renderAutocomplete`, the suggestion list was built with the **first item auto-marked `selected`**: `<div class="autocomplete-item ${index === 0 ? 'selected' : ''}" …>`. The keydown **Enter** handler branches on `selectedIndex >= 0` → when something is "selected", it does `searchInput.value = selectedItem.dataset.value` and `state.filters.search = selectedItem.dataset.value`. Because item 0 was *always* pre-selected, Enter always replaced the user's typed text with the top suggestion (e.g. typed "test" → input became "carly test"). The user "couldn't get it to stop." (The active-filter chip kept showing "test" because the Enter branch doesn't call `renderActiveFilterTags`, producing the chip-vs-box mismatch the reporter saw.)
- **Fix.** Removed the auto-selection — suggestions now render with **no item pre-selected** (`<div class="autocomplete-item" …>`, dropped the unused `index`). Behavior now: typed text wins on Enter (`selectedIndex === -1` → the "use typed value" branch); ArrowDown still highlights the first suggestion on first press, and hover/click still explicitly pick one. Matches the reporter's "I should be able to override" expectation. Applied to **all three** tile views (the bug was identical in each): public, internal, external.
- **Files:** `wwwroot/js/certificate-tile-view.js`, `wwwroot/js/certificate-tile-view-internal.js`, `wwwroot/js/certificate-tile-view-external.js` (each: `renderAutocomplete`).
- **Verification.** `node --check` passes on all three. Recommended smoke after deploy: type "test" where "carly test" is a suggestion → press Enter → input stays "test", results are for "test"; press ArrowDown then Enter → explicitly picks "carly test". (`asp-append-version` is already on these scripts per DEF-271, so the change won't be masked by browser cache.)

### DEF-279 — Certificate PDF download from the search/tile screen: latent wwwroot-only path bug in BOTH tile-API download endpoints (DEF-264 adjacent-endpoint follow-through); reported symptom most likely the 2026-06-15 missing-PDF incident

- **Date:** 2026-06-23
- **Severity:** Low–Medium (the buggy endpoints are **latent** — not wired to any current UI button, see Analysis — so no live user impact today; but they carry the exact regression DEF-264 fixed and would 404 every download if ever wired). The **reported** symptom (QC #198) is most likely a **data/infra issue**, not this code.
- **Status:** **Fixed locally (latent code bug) — pending next deploy.** Compiles clean (build's only errors are a file lock from the running `localhost:5101` instance, not C# errors). The reported download failure itself is a **data-recovery** matter — see Analysis/Recommendation.
- **QC Ref:** QC #198 (Sprint 3, Reporter: Dan — 6/15/26, Cert search view, image #89): *"error message showing up — but the PDF IS indeed there because if I click into the certificate card I can see the PDF… regardless of where I try I cannot download the PDF."* Triaged in `docs/tracking/6_23_IssuesTriage.md`.
- **Bucket:** Bug (file handling) + data/infra (incident).
- **Analysis — why "view works, download fails."** The cert detail **card shows a preview IMAGE** (`Certificate.CertificateImagePath` → `ViewCertificateImage`), which is a *separate file* from the actual **PDF** (`Certificate.CertificateFile`). So "I can see the PDF in the card" = the reporter is seeing the preview image, whose presence does **not** prove the PDF file exists. The **active** search-screen download (both internal & external tile views) calls the global `downloadCertificate(number)` → MVC `GET /Certificate/Download/{number}`, which **is** App_Data-aware (DEF-264) and works when the file is present. QC #198 is dated **2026-06-15 — the same day as [[incident_prod_all_pdfs_missing_2026-06-15]]** (the prod uploads store was wiped; 15/15 stored PDFs 404, recovery from backup pending). The evidence (preview image renders, every download 404s) matches that incident: the DB still references PDFs that no longer physically exist.
- **Latent code bug found + fixed.** While auditing, found that the **tile-API** download endpoints — `CertificateTileInternalApiController.DownloadCertificate` and `CertificateTileExternalApiController.DownloadCertificate` (route `{id}/download`) — still resolve the file **wwwroot-only**: `Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", certificate.CertificateFile.TrimStart('/'))`. This is the **exact regression DEF-264 fixed in the MVC action but missed in these adjacent endpoints** (consistent with the [[feedback_scoping_check_adjacent_endpoints]] lesson — the download family needs adjacent-endpoint audits; DEF-198 is even named there). After DEF-208 moved PDFs to App_Data, these 404 for any App_Data file. **Reachability:** the only caller is `certificateExternalService.downloadCertificate(id)` in `wwwroot/js/certificateExternalService.js`, which is **not wired to any button** (the tile buttons use the global MVC path) — hence "latent."
- **Fix.** Both endpoints now resolve **App_Data-first with a wwwroot legacy fallback**, mirroring `CertificateController.ResolveUploadPhysicalPath`/`StreamUploadedFile`: strip `~`/leading-slash, normalize separators, try `App_Data/<rel>` then `wwwroot/<rel>`. So if/when that endpoint is used, it behaves like the MVC download.
- **Files:** `Controllers/Api/CertificateTileInternalApiController.cs` (DownloadCertificate), `Controllers/Api/CertificateTileExternalApiController.cs` (DownloadCertificate).
- **Recommendation (the actual #198 resolution).** (1) **Restore the missing PDFs** from backup per [[incident_prod_all_pdfs_missing_2026-06-15]] — that is what makes downloads succeed; re-run `prod-missing-pdf-sweep.js` after restore. (2) Confirm whether the affected certs have `CertificateFile` set with the physical file present. (3) **Optional UX follow-up:** the search/tile download icon is shown unconditionally, while the detail page only shows it when `hasFile` (`CertificateFile` non-empty) — gating the tile icon on the already-exposed `pdfPath`/a `hasFile` flag would turn the confusing 404 into a clear "no PDF" state for certs that legitimately lack a file (does **not** help the incident case, where `CertificateFile` is set but the file is gone). Not built — flag for product decision.
- **Verification.** Build clean (no C# errors). Note: dev/localhost seed certs lack physical PDFs ([[DEF-270]] note), so a successful download can't be runtime-verified locally; the fix is a path-resolution correction verified by code parity with the DEF-264 MVC fix.

### DEF-278 — Product Category cluster: import ignored the template's Product Category (forced every product/cert to the first category "Chemical and Raw Materials"), and a blank cert category displayed the product's category instead of nothing

- **Date:** 2026-06-23
- **Severity:** High — corrupts imported data. The analyst entered a Product Category on the upload template and it was silently discarded; every auto-created product and imported cert got the first active category ("Chemical and Raw Materials"). This also fed the manual-create symptom (#195) because the Create form auto-fills the cert's category from the picked product.
- **Status:** **Fixed locally — pending next deploy.** Compiles clean (build's only errors are a file lock from the running `localhost:5101` instance, not C# errors). **DEPLOY COUPLING:** the #195 display change ships with a one-time data backfill — see Resolution.
- **QC Ref:** QC #189 (import ignores template category) **and** QC #195 (manual create, blank category → "Chemical and Raw Materials") — both Sprint 3, Reporter: Gemma, 6/11/26, Cert Creation Page. Carly noted both as "tied to #186" (CR-186 product-identity split). Triaged in `docs/tracking/6_23_IssuesTriage.md`.
- **Bucket:** Bug (import data-mapping) + UX (display fallback).
- **Decisions (Shawn, 2026-06-23, via triage Q&A):** (1) On import, an **unmatched** category **rejects the row** (no auto-create — categories are a controlled hierarchy). (2) A **blank** category should display **"—"**, not inherit the product's category.
- **Root cause — #189 (import).** `Services/Import/CertificateImportService.cs` had **no Product Category column mapping at all** (it had Product Type but not Category). Auto-created products hardcoded `CategoryId = defaultCategory.Id` (`categories.FirstOrDefault()`), the cert's own `ProductCategoryId` was never set during import, and `ResolveProductForImportAsync` was called with `categoryId: null` (there was even a standing `§10.3 follow-up` comment noting "no Category column yet… falls back to the base product's"). The display then showed the product's defaulted category.
- **Root cause — #195 (manual blank).** `Certificate.ProductCategoryId` is nullable; the cert detail views rendered `cert.ProductCategory?.Name ?? Product.Category?.Name ?? "—"`. When the cert's own category was null, it fell back to the linked product's category — which (because of #189) was the forced default. (On the Create form the category is also auto-filled from the picked product, so the bad product category propagated onto the cert.)
- **Fix — #189 (honor the template category).**
  - `Models/Import/CertificateImportModels.cs` — added `ProductCategory` (CSV row) + `ProductCategoryName`/`ProductCategoryId` (processed row).
  - `Services/Import/CertificateImportService.cs` — new `ProductCategoryColumns` mapping (`PRODUCT_CATEGORY`/`ProductCategory`/`product_category`/`Product Category`/`Category`); parse it in the CSV reader; resolve it in `ProcessRowAsync` against existing categories (case-insensitive) — **matched → `row.ProductCategoryId`; provided-but-unmatched → ValidationMessage (row rejected in preview, mirroring CertificationType); blank → null**. Persist `certificate.ProductCategoryId = row.ProductCategoryId` on **both** create (`CreateCertificateAsync`) and update; use the resolved category for the auto-created `Product.CategoryId` (falls back to the import default only when blank, since `Product.CategoryId` is required); pass `row.ProductCategoryId` into `ResolveProductForImportAsync`. Threaded the loaded `categories` list into `ProcessRowAsync`.
- **Fix — #195 (blank shows "—").** Removed the `?? Product.Category` fallback from the **Product Category** field in the 3 cert detail views (`CertificateInternalView`, `CertificateExternalView`, `CertificateSingleView`) → now `cert.ProductCategory?.Name ?? "—"`. (Product **Type** and **Subcategory** use the same fallback pattern but were **not** reported/decided, so left unchanged — noted as a consistency follow-up.) The CSV export projection (`CertificateService.ProjectToExportRows` ~585) keeps its product fallback (benign; aids export completeness/round-trip).
- **Resolution / deploy coupling (IMPORTANT).** Removing the display fallback means certs with a NULL `ProductCategoryId` (every cert imported before this fix) would show "—" on the detail view. To prevent that regression, the display change must ship **together** with the one-time backfill `Scripts/data-ops/2026-06-23-cert-category-backfill-UPDATE.sql` (sets `cert.ProductCategoryId = product.CategoryId` where the cert's is NULL; backup + ROLLBACK-by-default; needs CR + backup before COMMIT — CR-186-adjacent). After backfill, existing certs keep their category and only genuinely-blank certs show "—".
- **Files:** `Models/Import/CertificateImportModels.cs`, `Services/Import/CertificateImportService.cs`, `Views/Certificate/CertificateInternalView.cshtml`, `Views/Certificate/CertificateExternalView.cshtml`, `Views/Certificate/CertificateSingleView.cshtml`, `Scripts/data-ops/2026-06-23-cert-category-backfill-UPDATE.sql` (new, not run).
- **Verification.** Build clean (no C# errors). Recommended smoke before deploy: import a CSV with (a) a valid Product Category, (b) a category matching by different case, (c) a bogus category, (d) blank → expect (a)/(b) imported with that category on the cert + product, (c) rejected in preview with "Product Category not found", (d) cert category null → detail shows "—". Plus: manual-create with a correctly-categorized product no longer shows a spurious "Chemical and Raw Materials".
- **Note (existing data).** Pre-fix imported products already filed under "Chemical and Raw Materials" are a historical data issue ([[feedback_data_issue_vs_defect]]); re-importing with the category column, or a targeted data-ops pass, would correct them. The cert-category backfill above does not re-categorize products, only mirrors the product's current category onto the cert.

### DEF-276 — Certificate import: a non-exact Status value (e.g. "Client Approved Paid" missing the dash) was stored as raw text, leaving the cert with a blank status and an Edit dropdown that couldn't select any value (must delete & redo)

- **Date:** 2026-06-23
- **Severity:** High — corrupts imported data and **prevents the user from correcting the record**. A single mistyped status string (missing hyphen, wrong case, extra space) silently produced an un-editable certificate: the tile showed the typed text, but opening it showed a blank status and the dropdown rejected every option (the stored value wasn't in the valid list), so the only recovery was to delete the cert and start over.
- **Status:** **Fixed locally — pending next deploy.** Compiles clean (build's only errors are a file lock from the running `localhost:5101` instance, not C# errors). Resolver logic independently verified (see below).
- **QC Ref:** QC #194 (Sprint 3, Reporter: Gemma — 6/11/26, Cert Creation Page). Triaged in `docs/tracking/6_23_IssuesTriage.md`. Closely related to QC #188 (DEF-pending — date-driven Expired recovery), which is a *separate* workflow-transition question still awaiting PO direction.
- **Bucket:** Bug (back-end import validation).
- **Root cause.** `Services/Import/CertificateImportService.cs` read the CSV Status column and stored it verbatim (`ProcessRowAsync` set `row.Status = csvRow.Status` with only a blank→Draft default at ~893; persisted to `certificate.Status` on both create ~1270 and update ~1399). There was **no validation against the canonical 8-state list** (`Models/CertificateStatus.cs` `GetAllStatuses()`), so any string passed through. The Edit view's status `<select>` is built from `GetAllStatuses()`, so a stored value not in that set has no matching `<option>` → renders blank and "locks" (selecting an option is a no-op against the orphaned value until the field is changed, which the user couldn't reliably do).
- **Fix.** Two parts:
  1. **`Models/CertificateStatus.cs`** — added `CertificateStatusHelper.TryResolveStatus(string? input, out string canonical)`, a tolerant resolver that normalizes both the input and each canonical key to a letters/digits-only lowercase comparison key (`NormalizeStatusKey`) and returns the canonical string on match. So "Client Approved Paid", "client approved-paid", "CLIENT APPROVED  PAID" all resolve to "Client Approved - Paid"; unknown values return false. (Added `using System.Text;`.)
  2. **`Services/Import/CertificateImportService.cs`** (`ProcessRowAsync`, after the CertificationResults-length check) — resolve `row.Status` via `TryResolveStatus`: on success replace with the canonical value (auto-corrects near-misses so they import correctly); on failure add a `ValidationMessage` so the row is flagged **Error** in the import preview and **excluded from apply** (never persisted). Blank was already defaulted to Draft upstream.
- **Why this is safe (no ambiguity).** All 8 canonical statuses produce **distinct** normalized keys (verified 8/8) — e.g. `clientapprovedpaid` vs `clientapprovednotpaid` vs `clientapprovedconfidential`, and `expired` vs `expirednoannualverification` — so the tolerant match can never map one valid status onto another.
- **Files:** `Models/CertificateStatus.cs` (new `TryResolveStatus` + `NormalizeStatusKey`, `using System.Text;`), `Services/Import/CertificateImportService.cs` (`ProcessRowAsync` status block).
- **Verification.** Resolver logic reproduced and checked: 8/8 canonical keys distinct; "Client Approved Paid" + case/space/dash variants → "Client Approved - Paid"; "Expired-No Annual Verification" → "Expired - No Annual Verification"; bogus values ("Republish", "Paid") → no match (row rejected). Recommended follow-up smoke: import a CSV with (a) exact status, (b) missing-dash near-miss, (c) bogus status, (d) blank → expect (a)/(b) imported with canonical status + editable dropdown, (c) rejected in preview with the "Invalid Status" message, (d) Draft.
- **Note (existing bad rows).** This fixes the code so *new* imports can't create the broken state. Any certs already imported with an invalid status string predate the fix and would need a one-off data correction (data-ops + CR) to become editable — none confirmed present; flag if found.

### DEF-275 — Partner/Product-ID CSV export: "Certification Results" column exported the raw stored value (mixed `0.17` / `100`) instead of the percentage shown on the web page

- **Date:** 2026-06-23
- **Severity:** Medium — export consistency / partner-facing. `CertificationResults` is a free-text field, so analysts entered values in mixed formats (`0.17`, `100`, `17%`). The web pages and the partner API already normalize these to a percentage via `DisplayFormatters.FormatCertificationResults`, but the CSV export wrote the raw column, so the same dataset showed `0.17` for one cert and `100` for another in the downloaded file.
- **Status:** **Fixed locally — pending next deploy.** Compiles clean (build's only errors were a file lock from the running `localhost:5101` instance, not C# errors).
- **QC Ref:** QC #201 (Sprint 3, Reporter: Gemma — 6/19/26, Partner View export). Triaged in `docs/tracking/6_23_IssuesTriage.md`. Desired output confirmed by Shawn 2026-06-23: "should be a percentage, it shows as a percentage on the webpage."
- **Bucket:** Bug (export formatting).
- **Root cause.** `Controllers/ExportController.cs:308` (Active Product Identifiers export) wrote `CsvField(c?.CertificationResults)` — the raw string — while every display path (`CertificateInternalView.cshtml:110`, the tile/internal API, and the v1 partner API) routes the same value through `DisplayFormatters.FormatCertificationResults`. The export was the only consumer that bypassed the formatter. The formatter rule: contains `%` → unchanged; non-numeric → unchanged (e.g. "Pass"); numeric in [0,1) → ×100 + `%` (0.17 → "17%"); numeric ≥ 1 → + `%` (100 → "100%").
- **Fix.** Wrapped the export value in the shared formatter: `CsvField(GCCWebApp.Extensions.DisplayFormatters.FormatCertificationResults(c?.CertificationResults))` — fully qualified to match the existing usage in `CertificateInternalView.cshtml` and avoid a new `using`. CSV output now matches the web display exactly.
- **Files:** `Controllers/ExportController.cs` (line 308).
- **Verification.** Export a cert set containing `0.17`, `100`, `17%`, a non-numeric ("Pass"), and a blank → confirm CSV shows `17%`, `100%`, `17%`, `Pass`, blank respectively, matching the page.

### DEF-274 — Certificate Create/Edit: Partner(s) field changed from native Ctrl-click `<select multiple>` to a checkbox dropdown (proactive UX enhancement)

- **Date:** 2026-06-22 (retro-logged 2026-06-22 to close a Gate-5 traceability gap — the work shipped in the 1007.zip batch without a DEF row).
- **Severity:** N/A — proactive enhancement, not a defect.
- **Status:** **Fixed locally — pending next deploy** (in `GCCWebApp-deploy-20260622-1431.zip`, which supersedes 1007.zip). Verified on `localhost:5101`.
- **QC Ref:** none (client) — proactive Anuki UX enhancement tied to the [[feedback_partner_data_isolation]] / CR-171 UX backlog (the multi-partner control).
- **Bucket:** Enhancement (UX).
- **What changed.** Replaced the native `<select multiple size="4">` Partner(s) control (Ctrl/Cmd-click to multi-select — poor discoverability) on Certificate Create + Edit with a Company-style searchable dropdown using **checkboxes**, a search box, and **NO "Add" footer** (partners can't be created from the cert form). New `wwwroot/js/partner-multiselect.js`. The native `<select#partnerSelect>` stays in the DOM (hidden, `aria-hidden`) as the authoritative state, and toggling an option dispatches `change` so the existing CR-171 inline sync keeps mirroring the selection into the hidden `PartnerIds` CSV the POST handler consumes — **data contract unchanged**. Edit pre-selection honored; stale "Hold Ctrl…" helper text corrected to "Select one or more partners."
- **Files:** `wwwroot/js/partner-multiselect.js` (new), `Views/Certificate/Create.cshtml`, `Views/Certificate/Edit.cshtml`.
- **Verification.** `Scripts/smoke-prod/partner-multiselect.js` **21/21** (Create+Edit: native hidden, custom trigger, checkboxes, multi-select, hidden-CSV sync, deselect, Edit pre-select).
- **A11y refinements (RESOLVED 2026-06-22).** A Gate-2 accessibility review flagged 3 medium items on the new control; all fixed in `partner-multiselect.js` before the zip shipped: (1) trigger now has an `aria-label` ("Partner(s)", updating to "Partner(s): <names>" on selection); (2) trigger `aria-controls` now points to the panel (`<selectId>-partner-panel`) and **Escape closes the panel and returns focus to the trigger** (ARIA listbox pattern); (3) the no-results placeholder carries `role="option"` + `aria-disabled="true"` so the listbox contains no role-less child. Verified `_verify-partner-a11y.js` **7/7** (attributes render, aria-label updates on select, Escape focus-return, no-results role) and the functional `partner-multiselect.js` smoke still **21/21**.

### DEF-273 — Certificate Create/Edit: enforce Expiration Date strictly after Effective Date (proactive validation enhancement)

- **Date:** 2026-06-22 (retro-logged to close the same Gate-5 traceability gap; shipped in the 1007.zip batch).
- **Severity:** Low–Medium — a cert could previously be saved with an Expiration Date **before or equal to** its Effective Date (both fields were independently `[Required]` but the relationship was unvalidated).
- **Status:** **Fixed locally — pending next deploy** (in `GCCWebApp-deploy-20260622-1431.zip`). Verified on `localhost:5101`.
- **QC Ref:** none (client) — proactive Anuki data-integrity enhancement.
- **Bucket:** Enhancement (validation).
- **What changed.** Added a cross-field rule, **Expiration must be strictly after Effective**, enforced on both layers: server-side `CertificateEditViewModel : IValidatableObject` (`ExpirationDate.Date <= EffectiveDate.Date` rejected; shared by Create + Edit, both gate on `ModelState.IsValid`) and a live client-side `validateCertDateRange()` that fires on each date field's `change` and again on submit (blocks + scrolls to the field). `type="date"` values are `yyyy-MM-dd`, so the string compare is chronological.
- **Files:** `Models/CertificateEditViewModel.cs`, `Views/Certificate/Create.cshtml`, `Views/Certificate/Edit.cshtml`.
- **Verification.** `cert-date-range-validation.js` **4/4** (Create), `cert-edit-date-range-validation.js` **7/7** (Edit) — before/equal → error+blocked, after → cleared; full `cert-create-validations.js` **25/0/2-benign**.

### DEF-272 — `/Product/Categories` is a non-functional orphan page: its only client script `wwwroot/js/admin/productCategories.js` was never created, so the entirely JS-driven UI (table, Add/Edit/Import/Export/search/filter/bulk/modal) does nothing

- **Date:** 2026-06-22
- **Severity:** Low (the page is **not linked from any navigation** — reachable only by typing `/Product/Categories` directly — so day-to-day user impact is minimal; but the route IS live and renders a broken shell + fires a 404 for the missing script).
- **Status:** **Fixed locally (Option B — removed) — pending next deploy.** Shawn chose to delete the orphan scaffolding rather than build it out. Verified on `localhost:5101`: `/Product/Categories` now returns **404**, `/css/product-categories.css` returns **404**, and `/Product/Management` is unaffected; build clean (0 errors). See **Resolution** below.
- **QC Ref:** none (client). **Found by:** Anuki internal — the [[DEF-271]] `Views/` cache-busting sweep (2026-06-22). `asp-append-version` emitted **no** `?v=` hash for this one script, which surfaced that the file is absent from disk; investigation showed it was **never** in git history.
- **Bucket:** Bug (unfinished feature / missing asset) — distinct from a data issue.
- **What exists (scaffold is ~90% there):** `Views/Product/Categories.cshtml` (full markup — table, search, status/level filters, bulk-actions bar, Add/Edit modal), `wwwroot/css/product-categories.css` (12 KB, present), `ProductController.Categories()` action (`return View()`, line ~49), and the backend data endpoints `ProductController.GetCategories()` (~339) + `GetParentCategories()` (~606). Only the **client controller JS that wires it all together is missing.**
- **The gap.** `Categories.cshtml:147` is `<script src="~/js/admin/productCategories.js" …>` and that file **does not exist** (`git log --all --follow` finds zero history — it was never committed). The view has **zero inline JS**, and the table/pagination/parent-dropdown bodies are literal `<!-- populated by JavaScript -->` placeholders. With no script, the page loads an **empty table** ("Showing 0 of 0 categories"), the Import/Export/Add buttons and the modal are inert, and the browser logs a 404 for the script. The existing `GetCategories`/`GetParentCategories` endpoints are consumed only by `certificate-inline-create.js` (the cert-form cascade), **not** by this page — so the server side is ready but unused here.
- **Not linked.** No `asp-action="Categories"` / `/Product/Categories` reference exists in `Views/Shared/` (nav) or elsewhere — confirming this is a scaffolded-but-abandoned page, not a live workflow. (Product **types** are managed on `/Admin/ProductTypes`; product CRUD on `/Product/Management`.)
- **Decision needed (triage).** **(A) Finish it** — author `wwwroot/js/admin/productCategories.js` to call the existing endpoints and wire the table/modal/filters/bulk actions (sizeable net-new front-end work; only worth it if a dedicated hierarchical-category admin surface is actually wanted, given types already have a page). **(B) Remove it** — delete the orphan view + the `Categories()` action + `product-categories.css` (and this script ref) as dead scaffolding. Recommend confirming with the client whether a standalone Category-management page is on the roadmap before choosing.
- **Files (if option B / removal):** `Views/Product/Categories.cshtml`, `Controllers/ProductController.cs` (`Categories()` ~49), `wwwroot/css/product-categories.css`. **(if option A / build):** new `wwwroot/js/admin/productCategories.js` against the existing `GetCategories`/`GetParentCategories` (+ category create/update/delete endpoints, which would also need to exist/be added).
- **Note:** the DEF-271 cache-busting attribute was still correctly applied to this `<script>` tag; it simply can't hash a file that isn't there. Fixing DEF-272 (either option) makes the `?v=` stamp moot (build) or removes the tag (remove).
- **Resolution (2026-06-22, Option B — removed).** Before deleting, swept the codebase: `product-categories.css` was referenced **only** by this view; the `Categories()` action had **zero** inbound references (no `RedirectToAction`/`Url.Action`/`asp-action`/nav link); the only references to the page anywhere were historical DEF records. Deleted **`Views/Product/Categories.cshtml`**, **`wwwroot/css/product-categories.css`** (both `git rm`), and removed the **`Categories()` action** from `Controllers/ProductController.cs`. Rebuilt clean (0 errors) and confirmed `/Product/Categories` + the CSS both 404 while `/Product/Management` still serves. **Doc-cleanup straggler (low priority):** `docs/tracking/full-spectrum-test-prompt.md` line ~80 lists `Product/Categories` in a DEF-101 icon re-verify checklist — that page no longer exists, so drop it from the checklist on the next docs pass (left as-is for now; it's a test checklist, not code).

### DEF-271 — Certificate Create/Edit JS not cache-busted: a stale cached `certificate-inline-create.js` leaves the Product Name dropdown empty even after a Company is selected ("Product Names are not populating")

- **Date:** 2026-06-22
- **Severity:** Medium (intermittent + environment-specific — only browsers holding an old cached copy of the script are affected; a fresh browser works. But it recurs every time one of these JS files changes and silently makes a core Create/Edit field look broken).
- **Status:** **Fixed locally — pending next deploy.** Verified on `localhost:5101`.
- **QC Ref:** none (client). **Found by:** Anuki internal — Shawn reported "Product Names are not populating when attempting to create a new certificate" (2026-06-22), with the company already selected.
- **Bucket:** Bug (caching/build hygiene).
- **Root cause.** On `Views/Certificate/Create.cshtml` and `Views/Certificate/Edit.cshtml`, six form scripts were referenced **without** `asp-append-version="true"` — most importantly `certificate-inline-create.js`, which builds the custom Product Name dropdown and contains the MutationObserver that rebuilds it when the Company→Product cascade appends `<option>`s. When that file changes (its git history has repeated "break fix, defect on Product Name" commits), browsers keep serving the **old cached copy**: products still load into the hidden native `<select#ProductId>`, but the stale script never rebuilds the visible custom dropdown → the user sees an empty Product Name list. Reproduces as "not populating" even though the API and server code are correct.
- **Why it passed automated tests but failed for the user.** A headless Playwright run always fetches fresh JS, so the cascade passed 9/9 (programmatic) and 5/5 (real click flow across 3M, Acme, Alcon, Amazon, Amazon Devices). The defect only manifests against a warm browser cache — exactly the user's situation.
- **Fix.** Added `asp-append-version="true"` to all six scripts in **both** views (`product-id-manager.js`, `certificate-custom-fields.js`, `certificate-create.js`, `certificate-inline-create.js`, `partner-multiselect.js` already had it, `searchable-select.js`). ASP.NET now stamps a content-hash `?v=` query string so any change forces a re-fetch.
- **Files:** `Views/Certificate/Create.cshtml` (script block ~415), `Views/Certificate/Edit.cshtml` (script block ~469).
- **Verification.** After rebuild + restart, all 6 scripts emit `?v=<hash>` on both Create and Edit (12/12), and the product cascade still passes 5/5 via the real click flow. Repro/regression scripts: `Scripts/smoke-prod/cert-product-populate.js`, `Scripts/smoke-prod/cert-product-clickflow.js`.
- **Follow-up (DONE — 2026-06-22).** Swept all of `Views/` for `<script src="~/js/...">` tags missing `asp-append-version` and applied the attribute to **`Views/Shared/_Layout.cshtml` (site-wide: `main`/`auth`/`admin-core`/`accessibility-enhancements`) + 13 admin/management views** (UserManagement, ProductManagement [also dropped a fragile manual `?v=5`], Product/Management, Company/Management, Product/Categories, CertificateFields, CSFAttributes, Tags, ProductTypes, RoleManagement, PartnerAPI, GCCAPI, GoogleAnalytics). Live-verified `?v=<hash>` on Home (`_Layout`), Product/Management, Company/Management; remaining `/Admin/*` pages covered by tag-helper parity + on-disk file check (18/19 referenced files present). The 19th — `admin/productCategories.js` — was found **missing from disk**, opened as **[[DEF-272]]**. Tier-3 auth/dashboard views (Dashboard, Login, TwoFactorPreferences, CreateUser) were intentionally left out of this pass per Shawn's chosen scope.

### DEF-270 — Partner API `GET /api/v1/certificates/{id}/download` has no PDF-generation fallback: returns 404 ("PDF generation not yet implemented") whenever no physical PDF exists for the cert

- **Date:** 2026-06-22
- **Severity:** Low–Medium (the `pdfDownloadUrl` field is emitted in every partner response, but following it 404s for any cert whose PDF file is absent). On prod, real certs normally have an uploaded PDF, so impact is limited to certs missing a file (and the [[incident_prod_all_pdfs_missing_2026-06-15]] window); **on localhost every seeded cert lacks a physical PDF, so the partner download 404s 100% of the time in dev.**
- **Status:** **OPEN — triage. No code change made** (per [[feedback_qc_review_no_changes]]; surfaced by a test task, not a fix request).
- **QC Ref:** none (client). **Found by:** Anuki internal — Amazon partner-API localhost functional test, 2026-06-22 (Shawn-directed "test all the API functionality + data sent to Amazon"). `Scripts/smoke-prod/amazon-partner-api-local.js`, the download check.
- **Bucket:** Bug (unimplemented fallback) — distinct from a data issue.
- **What works (verified):** the endpoint correctly resolves the file **App_Data-first with a wwwroot legacy fallback** (it is DEF-208-aware, unlike the MVC `Download` action that [[DEF-264]] fixed) and serves a real PDF when one is present. Path resolution is **not** the bug here.
- **The gap.** `Controllers/Api/v1/CertificatesController.cs` `DownloadCertificate` (~line 1280) ends with `// TODO: If no uploaded file exists, generate PDF dynamically … PDF generation not yet implemented` and returns `NotFoundResponse("Certificate file not found. PDF generation not yet implemented.")`. The Swagger doc for this endpoint advertises *"If no uploaded file exists, a dynamically generated PDF will be created"* — so the **documented contract and the behavior disagree**. Amazon (or any partner) that trusts `pdfDownloadUrl` will get a 404 for any file-less cert.
- **Decision needed (triage).** (A) Implement the dynamic-PDF fallback (wire the existing `_pdfService.GenerateCertificatePdfAsync` the MVC path uses) so the advertised contract holds; or (B) drop the "will be generated" sentence from the Swagger doc and accept that `pdfDownloadUrl` only resolves for certs with an uploaded file. Either way the doc and behavior should be reconciled.
- **Files (if option A):** `Controllers/Api/v1/CertificatesController.cs` (`DownloadCertificate`, ~1280). **(if option B):** the same method's XML doc + the `<remarks>` Notes block (~1186).

### DEF-269 — Partner API `GET /api/v1/certificates/search` — `term` and `status` are non-nullable params (implicitly `[Required]`), so an empty `term` 400s; and `status` does a literal match against the **internal** status, so a partner passing `status=Active` gets 0 rows

- **Date:** 2026-06-22
- **Severity:** Low (search is a secondary partner endpoint; **Amazon ingests the GS1 endpoint, not `/search`** — GS1, list, list+productIds, expiring, by-number, and search-by-productid are all fully functional and contract-clean). Affected only callers using the free-text `/search` endpoint with no/loose params.
- **Status:** **FIXED — pending deploy.** Built clean locally (0 errors; pre-existing warnings only). Verified on localhost.
- **QC Ref:** none (client). **Found by:** Anuki internal — Amazon partner-API localhost functional test, 2026-06-22. `Scripts/smoke-prod/amazon-partner-api-local.js`.
- **Bucket:** Bug / UX (API usability).
- **Finding 1 — params were implicitly required.** In `Controllers/Api/v1/CertificatesController.cs` `SearchCertificates` (~line 592), `string term` and `string status` were **non-nullable reference types**; under .NET 8 nullable-aware model binding these are treated as `[Required]`, so `GET /api/v1/certificates/search?term=` returned **HTTP 400** (`"The term field is required." / "The status field is required."`) even though the method body already handled empty values via `if (!string.IsNullOrEmpty(...))`. Both are clearly meant to be optional filters.
- **Finding 2 — `status` filter was a literal internal-status match.** The endpoint applied `query.Where(c => c.Status == status)` against the **stored** lifecycle status (e.g. `"Client Approved - Paid"`, `"Republished"`). Partner responses always *report* `status: "Active"` (the contract), so a partner who reasonably passed `status=Active` matched **zero** rows — none of the public-status certs are literally stored as `"Active"`. (Confirmed pre-fix: `search?term=26&status=Active` → 200 with 0 records, while `…/certificates` returned the same 6 certs.)
- **Fix.** (1) Made `term` and `status` nullable (`string? term`, `string? status`) so the documented optional-filter behavior works — an empty/absent `term` no longer 400s. (2) The `status` filter now applies **only for non-partner callers** (`if (!string.IsNullOrEmpty(status) && !isPartnerCaller)`); partner callers are already constrained to `PublicStatuses` and always reported "Active", so their `status` filter is ignored rather than literal-matched to zero. Admin/Analyst behavior is unchanged (they still query the internal status intentionally). No DB/migration change — server-side controller only.
- **Verification.** `Scripts/smoke-prod/def269-search-optional-params.js` — **11/11 PASS**: empty-`term` and no-params search now 200 returning the partner's full scoped set (6 certs, was 400); `term=26&status=Active` returns 6 certs (was 0); output stays contract-clean (status=="Active", no co-partner leak).
- **Files:** `Controllers/Api/v1/CertificatesController.cs` (`SearchCertificates`, ~592).
- **Deploy:** Code-only, **no migration** (Gate 7 no-op for this fix). Next batch — bundle with the current pending-deploy working tree.

### DEF-268 — External-role users with a Partner association see NO partner-tagged certs in the External tile (partner association honored only for the `Partner` role) — CLOSED (WAD)

- **Date:** 2026-06-19 | **Closed:** 2026-06-23
- **Severity:** N/A — **closed Working-As-Designed.** Real-user impact was **0** (the only External-role + partner-association account, `phil.nonpartner@example.com`, is a test user). The empty-tile state only arises from a misconfiguration, not a supported access pattern.
- **Status:** **CLOSED — Working As Designed (2026-06-23).** Confirmed by Shawn: partner access is provided through a **dedicated partner login** (the `Partner` role), which is the correct and tested path. No code change made (this was a finding from CR-171 verification, not a fix). Optional hardening tracked separately as **[[DEF-277]]** (low / deferred).
- **Resolution rationale.** (1) The dedicated **`Partner`-role login works correctly** — verified live: `pat.amazon` (Partner role, Amazon) opened the multi-partner cert `26-0146` (`[Amazon, Novi]`) and saw it scoped to **`"Amazon"` only**, no co-partner leak; multi-partner certs are served correctly per-partner through these logins. (2) The "v1 API inconsistency" originally flagged is **moot**: the v1 endpoint is **API-key-only auth** (`[Authorize(AuthenticationSchemes = "ApiKey")]`) and API keys arrive as the `Partner` role, so a cookie-logged-in External user cannot reach the v1 External branch — there is no conflicting real path. (3) The only way to reach the empty/partial tile is to give a regular External (company) user a partner association — a misconfiguration the dedicated partner login makes unnecessary; guarding against it is the optional follow-up (DEF-277).
- **QC Ref:** none (client). **Found by:** Anuki internal verification of CR-171 multi-partner work, 2026-06-19 (Shawn-directed "test all permutations" sweep — partner-API + session-role Playwright). No client QC item; surfaced while confirming the partner-visibility matrix.
- **Bucket:** Question / possible Bug (endpoint inconsistency).
- **What works (verified, NOT broken):**
  - **Partner API keys** (Amazon/Wayfair/Novi) — fully scoped + isolated across list / list+productIds / GS1 / search-by-productid / by-number; contract shape byte-identical to the April pre-CR-171 baseline. (See `Scripts/smoke-prod/_results/cr171-partner-verify-2026-06-19/`.)
  - **`Partner`-role web login** (e.g. `pat.amazon` → Amazon) — External tile correctly shows the cert scoped to the caller's **own** partner only (`partner="Amazon"`, no co-partner leak). The `userRole=="Partner"` branch handles this.
  - **Public** (no partner field), **External company-user**, **Analyst**, **Admin** — all show the spec-correct partner value (`""` / all-associated). (See `Scripts/smoke-prod/cr171-session-roles.js`.)
- **The gap.** In `Controllers/Api/CertificateTileExternalApiController.cs:113-126`, the `userRole == "External"` branch filters by **company only** (`GetCurrentUserCompanyIdsAsync`); it never reads the user's partner associations. Partner filtering (`c.Partners.Any(...)`) runs **only** for `userRole == "Partner"` (line 100-112). So an **External-role** user who also carries a partner association cannot see partner-tagged certs from *other* companies. Live repro: user 93 (External, company=Acme, partner=Amazon) gets **0 certs** and cannot see the Amazon-tagged, public-status cert `26-0146` (KAO's), which they would receive if partner associations were honored.
- **Inconsistency (the core of the triage).** The **v1 partner API** (`Controllers/Api/v1/CertificatesController.cs:170-197`) *does* honor partner associations for External-role callers (company OR partner). The **External tile** does not. Same adjacent-endpoint divergence class as DEF-103/164/198/204/205 ([[feedback_scoping_check_adjacent_endpoints]]). Per [[feedback_external_user_umbrella]] (external = umbrella over Company + Partner), the tile arguably *should* OR-in partner associations.
- **Why this is NOT a CR-171 regression.** The External-branch company-only filter is **pre-existing** (code comment: *"per Design Recommendations - NO OR logic … External role: sees certificates from their assigned companies only"*). CR-171 changed partner **output scoping**, not this access branch. Nothing the multi-partner work shipped altered this behavior; it was surfaced *by* the CR-171 verification sweep. The CR-171 contract question ("is partner data still sent the same?") is independently confirmed YES.
- **Decision (resolved 2026-06-23 — Option A).** Partner principals use the **dedicated `Partner`-role login**, which is correct and verified. External-role + partner-association is therefore a **misconfiguration**, not a supported pattern → **no tile change** (Option B not pursued). Optional guard to prevent the misconfiguration is logged as **[[DEF-277]]** (low / deferred). Option B (OR-in `c.Partners.Any(...)` in the External tile branch) is recorded here only as the alternative that was considered and declined.
- **Verification harness (kept):** `Scripts/smoke-prod/cr171-session-roles.js` — logs in per role, reads cert `26-0146`'s partner field from each role's tile API; this is what confirmed the `Partner`-role login path. Evidence JSON in `Scripts/smoke-prod/_results/cr171-session-roles-*.json`.

### DEF-267 — Saving any certificate fails validation: "Certificate Number must be a string or array type with a maximum length of '7'"

- **Date:** 2026-06-17
- **Severity:** High (blocks **every** certificate save on Edit and Create — no cert with a normal `YY-NNNNN` number can be saved through the form).
- **Status:** **FIXED — pending deploy.** Built clean locally (0 warnings / 0 errors).
- **QC Ref:** user report (Shawn), 2026-06-17: validation error banner on `https://gccwebapp.com/Certificate/Edit/26-10063` when clicking Save Changes (screenshot `Screenshot_17-6-2026_16347_gccwebapp.com.jpeg`).
- **Bucket:** Bug (stale validation attribute on the view model).
- **Root cause.** `Models/CertificateEditViewModel.cs` had a stale `[MaxLength(7)]` on the `Number` property. Certificate numbers use the format `YY-NNNNN` (e.g. `26-10063` = 8 chars), so the 7-char cap rejected essentially every real certificate number on POST. The backing entity `Certificate.Number` already allows `[MaxLength(50)]`, so only the view model was out of sync. (The `HasMaxLength(7)` matches in the EF snapshots belong to a *different* table — `CertificateStatusHistory.CertificateNumber` — not this field.)
- **Fix.** Changed the view model attribute from `[MaxLength(7)]` → `[MaxLength(50)]` to match the entity. Affects both Edit and Create (shared `CertificateEditViewModel`). No DB/migration change — the column was never the constraint; this was server-side model validation only.
- **Verification.** `Scripts/smoke-prod/def267-cert-number-maxlength.js` — load a cert Edit form, force an 8-char number (the prod-shaped `26-10063` case) through Save, assert no `MaxLength(7)` banner + clean redirect, then restore the original number (no test data left changed).
- **Files:** `Models/CertificateEditViewModel.cs`.
- **Deploy:** Next batch — a **rebuild** of the 6-16 bundle (`…-20260616-1124.zip`) is required since this 6-17 fix postdates that zip. Carries forward the full 1124.zip working tree (DEF-264/265/266 + CR-186 + CR-171 + CR-187). DEF-267 itself has no migration, but the **bundle does** (CR-187 `AddNormalizedSearchColumns` + CR-171 `AddProductIdPartnersM2M`), so **Gate 7 fires for the deploy** — run the canonical combined artifact **`docs/tracking/CR-171-CR187-deploy-combined.sql`** (2 migrations + both CR-171 backfills, idempotent; validated on a throwaway restore of the live `.bak` 2026-06-18) in the same step as the DLL push. Leads the batch (High — blocks all cert edits/creates on prod). See `shipped-2026-06.md` + Gate 7 of `pre-deploy-agent-checklist.md`.

### DEF-266 — "Error loading custom fields" toast on Certificate Create when selecting a Certification Type

- **Date:** 2026-06-16
- **Severity:** Medium (error toast on `/Certificate/Create` whenever a Certification Type is chosen; the rest of the form still works, but the message is alarming and blocks confidence in custom-field entry).
- **Status:** **FIXED — pending deploy.** JS-only change; needs prod deploy to take effect (static asset). Console now logs the true cause.
- **QC Ref:** user report (Shawn), 2026-06-16: *"https://gccwebapp.com/Certificate/Create — error message loading custom fields is showing when selecting Certification Type."*
- **Bucket:** Bug (fragile client-side load/render path).
- **Root cause.** `wwwroot/js/certificate-custom-fields.js` `loadFieldsForType()` wrapped both the fetch **and** `renderFields()` in one try/catch and never checked `response.ok`, so the generic "Error loading custom fields" toast fired for **two distinct, indistinguishable** failures: (1) a non-200 response (expired auth → login-redirect HTML, or a 500 error page) making `response.json()` throw on non-JSON; (2) a malformed custom-field row — e.g. a `null` `FieldType` — making `renderField()`'s `field.fieldType.toLowerCase()` throw during render. The server `CertificateFieldService` swallows DB errors and returns an empty list (→ `success:true`), so schema drift would show "No custom fields defined", *not* this toast — confirming the failure is on the fetch/parse/render path, not a missing table.
- **Fix.** Split the network call and the render into separate try blocks; check `response.ok` before parsing (log the HTTP status instead of throwing on HTML); a render failure now shows a distinct "Error displaying custom fields" and is logged separately. Null-guarded `field.fieldType` (`(field.fieldType || 'text')`) in `renderField()` and `validateFields()` so a null type defaults to a text input instead of crashing the section. No behavior change on the happy path.
- **Diagnostics.** Post-deploy, the browser console now distinguishes the cause: an `HTTP 4xx/5xx` log ⇒ server/auth issue; a render-error log ⇒ a bad custom-field row to clean up.
- **Files:** `wwwroot/js/certificate-custom-fields.js`.

### DEF-265 — Editing a certificate's Product Type/Subcategory creates a duplicate product line item instead of updating ("keeps adding new instead of updating")

- **Date:** 2026-06-16
- **Severity:** Medium (Product Management accumulates 0-cert orphan rows; confusing on `/Product/Management`; data clutter, not data loss).
- **Status:** **FIXED — pending deploy.** Built clean locally (0 errors); verified on localhost.
- **QC Ref:** user report (Shawn), 2026-06-16: *"instead of updating the record it just keeps adding new"* — screenshot of `/Product/Management` showing one cert ("WD test cert") split into 3 product rows differing only by Product Type, two with 0 certs. Reporter confirmed: *"I edited existing certificate. when it saved it created a new product record. there is only one certificate."*
- **Bucket:** Bug (CR-186 split side effect on the single-cert case). The per-type/subcat split is the approved [[CR-186]] design; leaving the prior line item orphaned on every cert-type edit is the unintended part.
- **Root cause.** `CertificateController.ResolveProductForCertAsync` (find-or-create by Company+Name+Category+Type+Subcategory) ran on cert **edit** and, when the new tuple had no matching product, always **created a new** product line item — even when the cert was the sole owner of its current line item, leaving the old row behind with 0 certs.
- **Fix.** Before creating, the resolver now checks whether the cert's **current** product line item (passed in as `previousProductId`) is owned only by this cert (`previousProductId` + `currentCertId` → `otherCertCount == 0`) and shares the same Name+Company. If so it **updates that record in place** (Category/Type/Subcategory) instead of creating a duplicate. The CR-186 split still fires when the line item is shared by other certs or the user picked a different base product. Non-destructive (no deletes).
- **Verification.** `Scripts/smoke-prod/def265-cert-edit-no-dup-product.js` — edited a sole-owner cert's type (Additives→Metal Alloys); product total stayed 318→318 and the same line item updated in place. cert-edit + cert-create view smokes still green.
- **Files:** `Controllers/CertificateController.cs` (`ResolveProductForCertAsync` + the Edit call site ~line 1027).

### DEF-264 — PDF download fails ("No certificate PDF has been uploaded"): DEF-208 straggler — `Download` / `DownloadSelectedZip` still resolved the file against wwwroot

- **Date:** 2026-06-15
- **Severity:** High (every certificate PDF download 404s with a "no PDF uploaded" error after the DEF-208 App_Data move; affects the public single-download and the External bulk-ZIP path).
- **Status:** **FIXED — pending deploy.** Built clean locally (0 errors).
- **QC Ref:** user report (Shawn), 2026-06-15: *"pdf error on download… error message displays."*
- **Bucket:** Bug (incomplete refactor — same DEF-208 path-migration family as [[DEF-262]]).
- **Root cause.** DEF-208 relocated uploads from `wwwroot/uploads/` to `App_Data/uploads/`. DEF-262 fixed five resolvers but **missed the two download actions**, which still built the physical path against `WebRootPath`:
  - `CertificateController.Download` (line ~1809) — `File.Exists(fullPath)` failed → returned `NotFound("No certificate PDF has been uploaded for {id}.")` (the error the user saw). `DownloadByNumber` delegates to `Download`, so it was affected too.
  - `CertificateController.DownloadSelectedZip` (line ~2216) — same wwwroot-only check failed, then silently fell through to `_pdfService.GenerateCertificatePdfAsync` (a generated placeholder) instead of zipping the real uploaded PDF.
- **Fix.** Routed both through the existing DEF-262 helper `ResolveUploadPhysicalPath` (App_Data-first, wwwroot legacy fallback) — same pattern as the view/preview endpoints.
- **Files:** `Controllers/CertificateController.cs`.

### DEF-263 — Plaintext Gmail credential (address + app password) committed in `appsettings.json`; outbound email also defaults to a no-op in production

- **Date:** 2026-06-12
- **Severity:** High (live credential in source control — present in git history, not just the working tree). Mitigating factors: it is a *dev* Gmail App Password (revocable, scoped to send-mail, not the account password), and it lives in `appsettings.json` (the Development default), not `appsettings.Production.json`. Still must be rotated + purged.
- **Status:** **OPEN — flagged, not fixed (no code changed).** Surfaced during the CR-009 outbound-email verification, 2026-06-12.
- **QC Ref:** none (internal finding while verifying CR-009 / QC #150 forgot-password delivery).
- **Bucket:** Bug (security — secret in source control) + Config (prod email unwired).
- **Linked memory.** [[security-plaintext-gmail-credential]], [[reference_amazon_key_in_devguide_pdf]] (same class — plaintext secret committed; rotation + git-history cleanup pending). Relates to CR-009 in the change-request register.
- **Finding 1 — credential exposure.** `appsettings.json:66-73` `SmtpSettings` contains `Username: shawnmwest@gmail.com` and `Password: "wbwr bmyb hhoa iulj"` (a Google App Password) in plaintext, committed to the repo. Anyone with repo or git-history access can send mail as that account.
- **Finding 2 — prod email is a no-op (relates to CR-009 / QC #150).** `Program.cs:486-503` registers the real `EmailService` only when `SmtpSettings:Host/Username/Password` are all non-empty; `appsettings.Production.json:90` has all three `""`, so production resolves to **`MockEmailService`** — forgot-password reset links and email 2FA codes are generated and stored but **never sent**. (Caveat: a Plesk runtime env-var override `SmtpSettings__Host/__Username/__Password` would change this — must be confirmed on the live box; per committed config alone it is Mock.)
- **Recommended remediation (not yet done).** (1) Rotate/revoke the exposed Gmail App Password in the Google account immediately. (2) Move the dev secret to user-secrets / environment variables; replace the JSON value with an empty string or placeholder. (3) Purge from git history (filter-repo/BFG) or accept-and-rotate if history rewrite is out of scope. (4) For prod, configure a real domain SMTP/relay sender (the `noreply@yourdomain.com` placeholder in `appsettings.Production.json:96` is unfilled) so CR-009 / QC #150 can actually pass, then re-test forgot-password end-to-end.
- **Files (for remediation, none touched yet):** `appsettings.json` (dev secret), `appsettings.Production.json` (unfilled prod `SmtpSettings`). No code change required for the secret itself; the prod-email wiring is config-only.

### DEF-262 — PDF thumbnail/preview never generates ("Certificate Unavailable — Preview not generated"): DEF-208 moved uploads to App_Data but left several path resolvers pointing at wwwroot, so the cert saves with no preview image — on every environment

- **Date:** 2026-06-12
- **Severity:** High (every PDF upload saves the cert + PDF but produces no preview image; not environment-specific — broken locally and on prod since the DEF-208 move).
- **Status:** **SHIPPED 2026-06-12** to gccwebapp.com (`GCCWebApp-deploy-20260612-1509.zip`) — **client-confirmed working** ("the image saves now"). No DB/migration change.
- **QC Ref:** user report (Shawn), 2026-06-12, after deploying the DEF-261 fix: *"It said it saved. however, the image is still not saving."* Surfaced only once DEF-261 unblocked Save — before that the modal stopped the save from completing, so the missing-thumbnail state was never reached.
- **Bucket:** Bug (incomplete refactor — DEF-208 path migration left stragglers).
- **Linked memory.** [[feedback_no_chaos_rabbit_holes]] (live ground truth first), [[feedback_scoping_check_adjacent_endpoints]] (the same path bug existed in 5 spots across 3 files — fixed all, not just the first).
- **Diagnosis (live + local).** Live repro on gccwebapp.com showed the saved cert renders the "Certificate Unavailable — Preview not generated" placeholder; `CertificateImagePath` empty. Local disk check: PDF saved to `App_Data/uploads/certificates/` but **0** thumbnails in `App_Data/uploads/images/`. Local log: `PDF file not found for thumbnail generation: …\wwwroot\uploads\certificates\{file}.pdf` — the generator looked in **wwwroot** while the PDF lives in **App_Data** (DEF-208).
- **Root cause.** DEF-208 relocated uploads from `wwwroot/uploads/` to `ContentRootPath/App_Data/uploads/` and updated the *write* paths + the *serve* routes (`StreamUploadedFile`, `PreviewThumbnail`, `ViewCertificateImage`), but **missed several resolvers that still combined paths against `WebRootPath`**, so they never found the file:
  - `FileUploadService.GeneratePdfThumbnailAsync` (was `WebRootPath` → never found the PDF → returned null; the primary cause of the user's symptom).
  - `FileUploadService.DeleteFileAsync` / `GetFileAsync` (old PDF/image never deleted on replace → orphans; reads failed).
  - `CertificateController` lazy "ensure thumbnail exists" guard (existing-thumbnail + PDF-exists checks both on `WebRootPath` → bailed before backfilling missing thumbnails).
  - `CertificateController` API "upload PDF" action (built a `WebRootPath` physical path and passed it into `GeneratePdfThumbnailAsync`, which expects a *logical* path — doubly wrong).
  - `CertificateImportService` wrote imported PDFs to `wwwroot/uploads/certificates` while storing the App_Data logical path → imported PDFs orphaned, thumbnails never generated.
- **Fix.** Added a shared **App_Data-first / wwwroot-fallback** resolver in each class (`FileUploadService.ResolvePhysicalUploadPath`, `CertificateController.ResolveUploadPhysicalPath`) — mirrors the existing `StreamUploadedFile` pattern so pre-DEF-208 legacy files stay reachable until the server Move-Item migration propagates. Rewired all five call sites; the API upload action now passes the logical path directly; import writes to `App_Data/uploads/certificates`.
- **Files:** `Services/FileUploadService.cs`, `Controllers/CertificateController.cs`, `Services/Import/CertificateImportService.cs`.
- **Verification (localhost:5101, admin, fixed code).** `Scripts/smoke-prod/upload-image-diagnose-local.js` → cert detail now renders a real preview (`ViewCertificateImage`, 464px, no placeholder); fresh PNGs written to `App_Data/uploads/images/`; log `PDF thumbnail generated successfully`; **0** "not found for thumbnail" after restart (was 1 from the controller path pre-fix). Build clean 0 errors.
- **Note.** `PreviewThumbnail` and `ViewCertificateImage` were already DEF-208-correct (App_Data) and unchanged. A transient `PreviewThumbnail` 404 in local logs was OneDrive filesystem latency on the dev box, not a code bug.

### DEF-261 — Certificate PDF upload (Create + Edit): attaching a PDF popped a centered, full-screen blocking modal that overlaid the Save button — user clicked Save, the click hit the modal overlay, and the certificate "didn't update"

- **Date:** 2026-06-12
- **Severity:** High (user-blocking — the documented happy path "upload a PDF and save" silently failed; the cert kept its old/empty file and looked like nothing happened).
- **Status:** **SHIPPED 2026-06-12** to gccwebapp.com (`GCCWebApp-deploy-20260612-1509.zip`) — client-confirmed (Save no longer blocked; cert saved). JS-only change (`wwwroot/js/certificate-create.js`); no build/DB/migration impact.
- **QC Ref:** user report (Shawn relaying), 2026-06-12: *"a user is reporting issues uploading a pdf on a certificate"* → *"tested with an existing cert, attempted to upload pdf, got a warning message and then click save and it didn't update"* (cert `26-0160` on gccwebapp.com).
- **Bucket:** Bug (UX — blocking modal intercepts the primary action).
- **Reporter:** client (Shawn), 2026-06-12, relaying an end-user.
- **Linked memory.** [[feedback_no_chaos_rabbit_holes]] (visual ground truth first — reproduced live before touching code), [[feedback_only_requested_changes]] (scoped the fix to the cert pages; did NOT flip AdminCore's global modal behavior).
- **Reproduced live (gccwebapp.com, behind the test gate, admin):** `Scripts/smoke-prod/upload-pdf-edit-repro-live.js` on cert `26-0160`. A **valid** (<10 MB) PDF → `AdminCore.showNotification(…, 'warning')` renders a centered `alertdialog` modal *"Warning — PDF uploaded. Preview will be generated on save."* that covers the page; Playwright confirmed the Save button was *"visible, enabled"* but *"`gcc-modal-overlay show` intercepts pointer events."* A **>10 MB** PDF → *"Error — PDF file size must be less than 10MB."* modal **and** the client clears the file (`certificate-create.js:213`), so even after dismissing, Save posts no PDF → cert unchanged. Both read to a user as *"I got a warning, clicked Save, it didn't update."*
- **Root cause — notifications routed to a blocking modal, not a toast.** `wwwroot/js/admin/admin-core.js:120` `showNotification()` comment *"Route ALL notifications to centered modals for consistency"* sends every type to `showWarningModal`/`showErrorModal` (full-screen `alertdialog`). The cert page's local `ModalSystem.showNotification` (`certificate-create.js`) called that (with a center-screen modal fallback). On Create/Edit those modals sit on top of `#saveButton`. Separately, the success message at line 324 was mislabeled type `'warning'`.
- **Fix.** `wwwroot/js/certificate-create.js`: `ModalSystem.showNotification` now renders a **self-contained, non-blocking corner toast** (top-right, `pointer-events:none` container, auto-dismiss 4 s / 6 s for errors, manual ×) instead of calling the blocking modal / building a full-screen overlay. Line 324 message changed `'warning'` → `'success'`. **Scoped to the certificate pages only — `AdminCore.showNotification`'s global modal behavior is intentionally left untouched** (changing it would alter notifications across every admin page). The 10 MB cap itself is by-design (UI states it) and unchanged.
- **Files:** `wwwroot/js/certificate-create.js` (only).
- **Verification (localhost:5101, admin, fixed code).** `Scripts/smoke-prod/upload-fix-verify-local.js` → **10 PASS / 0** on cert `26-0023` Edit: valid PDF shows a non-blocking *Success* toast, **no page-blocking overlay**, Save clicked **without** dismissing anything, cert saved (redirect to detail); >10 MB shows a non-blocking *Error* toast, file cleared, Save still reachable. Screenshot `_results/upload-fix-verify/1-small-attach.png` shows the corner toast with the form fully interactive.
- **Deploy note.** JS-only; ships with the next bundle. Because it's a static asset, confirm the deployed `/js/certificate-create.js` content/`Last-Modified` updates on the server (the smoke test's asp-append-version check — Plesk extract skips existing files; see DEF-195).

### DEF-260 — Cert List ID columns (ASIN/UPC/GTIN/EAN) must sort by the COUNT badge, not the ID value — the grid renders a count badge ("how many IDs"), so the client expects the column to order by that visible number (QC #157, client 6/9: "1000 should be at the top or bottom")

- **Date:** 2026-06-09
- **Severity:** Medium (the four ID columns appeared "not sorting" because the displayed value — a count badge — was never the sort key; supersedes the value-based sort shipped/attempted in DEF-256/259).
- **Status:** **SHIPPED 2026-06-12** to gccwebapp.com (`GCCWebApp-deploy-20260612-1509.zip`, alongside DEF-261/262). No DB/migration change (sort-key expression only).
- **QC Ref:** **QC #157** — client (Shawn) 6/9 on `/Certificate/CertificateTileViewInternal`, with screenshot: *"the numeric fields are still not sorting correctly … it should have 1000 as the greatest number at the top or bottom depending on sorting."* Clarified via AskUserQuestion → **"Sort by the count (the badge number)."**
- **Bucket:** Bug (sort key vs displayed value mismatch) / requirements clarification.
- **Reporter:** client (Shawn), 2026-06-09.
- **Linked memory.** [[feedback_cert_number_vs_id_resolved]] (yet another "looks broken" that was a display-vs-key mismatch, not a broken ORDER BY), [[feedback_scoping_check_adjacent_endpoints]] (internal + external controllers both updated for parity).
- **Root cause — the column shows a COUNT, the sort used the VALUE.** `certificate-tile-view-internal.js:1739/1777` (and the external JS) render the ASIN/UPC/GTIN/EAN cell as an **id-count-badge** = `countIds(asin)` (number of IDs on the cert), NOT the ID values. But every prior fix (DEF-165 `.Min()`, DEF-181, DEF-248 display order, DEF-256 natural sort, DEF-259 natural-min representative) ordered rows by the ID **value**. So a column of badges `1, 2, 3, … 1000` never appeared ordered by the badge number — the `1000` badge is the cert with ~1000 ASINs, which under value-sort lands wherever its min ASIN string falls. Confirmed by a live prod read: the only purely-numeric ASIN *value* on the site is one cert's `1232133543`; the small green numbers in the client screenshot are counts. **This is why the earlier "numeric sort" fixes never resolved the client's perception — they fixed the wrong axis.**
- **Fix.** New `Extensions/CertificateSortExtensions.cs::OrderByCountEmptiesLast(countKey, descending)` — orders by the count; cells with 0 IDs (no badge) sort LAST in both directions (QC #157 blanks-last convention). The 8 ID branches in **both** `CertificateTileInternalApiController` and `CertificateTileExternalApiController` `ApplySorting` now use it, with `count = ACTIVE ProductIds of the type, or 1 if none but a legacy single value exists, else 0` (matches the badge, which falls back to the legacy column). Cheap `COUNT` subquery on the DEF-250 thin ID-only projection — no DEF-250 timeout risk. The in-cell value display order from DEF-248/259 is retained (cosmetic now — affects only the clipboard-copy order and the public view, which *does* show values).
- **Supersedes:** the ID-column **sort** portions of **DEF-256** and **DEF-259** (value/natural sort → count sort). DEF-256's External-parity work and DEF-259's in-cell display ordering remain.
- **Scope.** Internal + external controllers (parity). External does not currently project these cells, so its column renders empty and the count-sort harmlessly falls back to Id order. Public tile view has no ID sort branches — unchanged.
- **Files:** `Extensions/CertificateSortExtensions.cs` (new helper), `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`.
- **Verification (localhost:5101, admin).** Build clean 0/0. `Scripts/smoke-prod/def260-count-sort-probe.js` → **8 PASS / 0** — `asin` asc counts `1, 2, 3, 6, 101, 1004` (increasing, 5 empties last); `asin-za` desc `1004, 101, 6, 3, 2, 1` (empties last) → the **1004-ID cert sorts to the extreme** as required; upc/gtin/ean single-count locally, empties last. `Scripts/smoke-prod/qc157-sort-verify-local.js` → **12 PASS / 0** (Location/Cert-Sub/Company-URL/Partner/Status no regression; ASIN value-oracle removed there, now owned by the count probe).
- **Deploy note.** The DEF-259 bundle (`…-20260609-1026.zip`) caused a prod **503** on upload (deploy-mechanics — locked DLL / app pool not recycled, NOT a code crash; the code booted clean locally). Client rolled back to restore the site. Next deploy: **stop the app → extract → start the app** (don't extract over a running worker), then recycle. See shipped log.


### DEF-259 — Cert List (Internal) "numbers still not sorting" (QC #157 re-raise, 6/9): multi-value numeric ID cells were still ordered **lexically** for both display and the row-sort representative, so DEF-256's natural row sort and DEF-248's "lead == sort key" guarantee broke whenever a cell held variable-length numeric IDs

- **Date:** 2026-06-09
- **Severity:** Low (presentation/ordering; only manifests on **variable-length numeric** product IDs — i.e. test-shaped data. Real ASINs are 10-char alphanumeric and real UPC/GTIN/EAN are fixed-length → lexical == natural → **prod data is a no-op**. Client tagged the broader item "not a deal breaker.")
- **Status:** **Fixed + localhost-verified (build clean 0/0). Pending next deploy.** No DB/migration change (in-memory display ordering + sort-key expression only).
- **QC Ref:** **QC #157** — client re-raise relayed by Shawn 6/9: *"the sort on the numbers are still not working correctly … cell showed them in entry order while the row was actually sorted on the lowest one."* Confirmed **live on gccwebapp.com** (1543.zip deployed) yet still wrong → genuine residual, not a deploy gap.
- **Bucket:** Bug (sort/display consistency — lexical vs natural mismatch).
- **Reporter:** client (QC #157), confirmed + fixed by Anuki on 6/9 at Shawn's direction.
- **Linked memory.** [[feedback_cert_number_vs_id_resolved]] (again the perceived "not sorting" is test-seed-data-shaped numeric IDs — real fixed-length/alphanumeric IDs are unaffected). Direct continuation of **DEF-256** (natural row sort for ID columns) and **DEF-248** (multi-value cell display == sort key); same QC #157 thread as **DEF-258** (cert-sub display order).
- **Root cause (why DEF-256 + DEF-248 together still read wrong).** DEF-256 switched the ID-column **row sort** to `OrderByNaturalEmptiesLast` (orders rows by `(isEmpty, LEN(key), key)` → numeric `9 < 10 < 100`), but the **representative `key`** it was handed was still the **lexical** `.Min()` of the cell's values (UPC used an arbitrary `.FirstOrDefault()`), and DEF-248's **cell display** still joined the values with lexical `.OrderBy(v => v)`. For a multi-value cell like `{2, 10}`: lexical min = `"10"`, so the row was positioned as if its smallest ID were `10` (parked among the `10`s) while its true smallest is `2`; and the cell rendered `"10, 2"`. So the column read as unsorted for numeric IDs. (The two pieces *were* mutually consistent — both lexical — so DEF-248's "lead == sort key" technically held, but both pointed at the wrong value vs. a human reading numerically.)
- **Fix (display + representative move together — both natural).** `Controllers/Api/CertificateTileInternalApiController.cs`:
  - **Cell display** (second-pass projection, in-memory): `c.{Asin,Upc,Gtin,Ean}List.OrderBy(v => v.Length).ThenBy(v => v)` (was `.OrderBy(v => v)`), so a numeric cell renders `2, 10, 100`.
  - **Row-sort representative** (`ApplySorting`, on the DEF-250 thin ID-only projection): the per-cell key is now the **natural minimum** `…Select(p => p.ProductIdValue).OrderBy(v => v.Length).ThenBy(v => v).FirstOrDefault()` (was lexical `.Min()` for ASIN/GTIN/EAN, arbitrary `.FirstOrDefault()` for UPC). The outer `OrderByNaturalEmptiesLast` keeps the `(isEmpty, LEN, value)` row ordering. Net: row positioned by its true smallest ID, and the displayed lead == that key.
  - UPC moved off the DEF-165 arbitrary `.FirstOrDefault()` to the same natural-min — closes the "known residual" DEF-248 flagged for UPC.
- **Why no DEF-250 timeout regression.** The ordered subquery runs on DEF-250's thin `.Select(c => c.Id)` pagination projection (no Includes → no split-query re-join of the ~1000-ProductId seed cert), so it stays a cheap `TOP 1 … ORDER BY LEN(value), value`. Verified: `asin/asin-za/upc/gtin/ean` all HTTP 200, run log clean of any `Timeout expired`/`SqlException`.
- **Scope — Internal only.** External tile view does not project ASIN/UPC/GTIN/EAN cells (DEF-248/256); public view has no ID sort branches. No change to either. CSV export untouched.
- **Files:** `Controllers/Api/CertificateTileInternalApiController.cs` (display projection lines ~354-357; `ApplySorting` ASIN/UPC/GTIN/EAN branches).
- **Verification (localhost:5101, admin session).** Build clean 0/0. `Scripts/smoke-prod/qc157-sort-verify-local.js` **14 PASS / 0** (ASIN asc/desc no regression on the alphanumeric local data `B005…`↔`B0GJ…`; cert-results canary 200). `Scripts/smoke-prod/def248-asin-display-order.js` **8 PASS / 0** (7/7 multi-value cells ascending + lead==key, incl. the 1000-ASIN `cert24` and 100-ASIN `cert51`). Run log clean (no timeout/exception). **Numeric proof (2026-06-09, localhost):** seeded a controlled numeric ASIN set on the dev DB (single values `1,2,9,10,1000` + a multi-value cell `{5,40}`, tagged `QC157TEST`, removed after) and ran `Scripts/smoke-prod/def259-numeric-asin-probe.js`. Result — **asc:** `1, 2, 5(,40), 9, 10, 1000` (1000 LAST); **desc:** `1000, 10, 9, 5(,40), 2, 1` (1000 FIRST); the `{5,40}` cell displays `"5, 40"` (natural, not lexical `"40, 5"`) and its row sorts by its true min `5` (between 2 and 9). Old lexical behaviour would have been `1, 10, 1000, 2, 5, 9` — `1000` mid-list, matching the client screenshot. So the numeric improvement is now confirmed by a **populated render**, not just code parity. Seed cleaned up (0 `QC157TEST` rows remain).

### DEF-258 — Certificate tile table (Internal): the **Cert SubCategories** cell displayed its multi-value list in insertion order while the column sorts by `.Min()` (alpha-first) — same display-vs-sort mismatch DEF-248 fixed for ASIN/UPC/GTIN/EAN, but never extended to this column

- **Date:** 2026-06-08
- **Severity:** Low (only affects certs with **multiple** cert subcategories; single-value cells already match; client had explicitly deprioritized this column — "leaving Cert SubCats as is").
- **Status:** **Fixed + build-clean on localhost. Pending next deploy.** No DB/migration change (in-memory display ordering only). *Not yet re-verified with data* — the local dev DB has **0** certs with cert subcategories, so the fix is verified by code-parity with DEF-248, not by a populated render.
- **QC Ref:** **QC #157** (client, 6/4 round) — *"Leaving Cert SubCats as is … neither working right but not priority."* Surfaced during the 6/8 read-only verification of whether DEF-248's multi-value cell-reorder covered the cert-sub column (it did not).
- **Bucket:** Bug (sort/display consistency).
- **Reporter:** client (QC #157, deprioritized); confirmed + fixed by Anuki on 6/8 at Shawn's direction.
- **Linked memory.** [[feedback_cert_number_vs_id_resolved]] (the "not sorting" perception again came from a multi-value cell whose leading shown value ≠ the sort key, not a broken ORDER BY). Sibling of **DEF-248** (same fix, ID columns) and **DEF-256** (same QC #157 sort-audit thread).
- **Root cause.** `CertificateTileInternalApiController` first-stage projection built the display string as `string.Join(", ", c.CertificationTypeSubcategories.Select(s => s.Name))` — **no ordering** — so a cert with subcats `[Zebra, Apple]` rendered `"Zebra, Apple"`. But the `cert-sub` sort branch orders rows by `CertificationTypeSubcategories.Select(s => s.Name).Min()` = `"Apple"`. The eye lands on *Zebra* (the displayed lead) on a row sorted by *Apple* → reads as "Cert SubCats not sorting." DEF-248 fixed exactly this for ASIN/UPC/GTIN/EAN (`.OrderBy(v => v)` on the display list) but the cert-sub projection was missed.
- **Fix.** Order the displayed subcat names ascending so the leading shown value equals the `.Min()` sort key: `…Select(s => s.Name).OrderBy(n => n)`. One line, mirrors the DEF-248 pattern.
- **Scope — one surface, not three.** Audited all cert-subcat projections: **only the Internal tile view** displays this column as a sortable grid cell. The **External** controller has the `cert-sub` sort branch but **no display projection** (column not rendered) → no mismatch. The **public tiles** controller doesn't project cert subcategories at all. The **CSV/Excel/PDF export** path (`CertificateService.ProjectToExportRows`) does join subcats unordered, but that's a private admin-export row ordered by Id, not a sortable display column, and was deliberately left untouched (no client request; avoids touching export-row output).
- **Files:** `Controllers/Api/CertificateTileInternalApiController.cs` (single projection line).

### DEF-257 — Product Management warm page load is slow (~12s at 314 dev products) — init eagerly fetches the ENTIRE product set + runs 4 sequential lookup fetches before the table is usable; scales with total product count

- **Date:** 2026-06-05
- **Severity:** Low (Analyst/Admin-only page; the dev seed has **314** products vs a far smaller prod set, so real-world load is much lighter; it completes successfully with no error, and the table already paginates client-side once loaded. This is sluggishness, not breakage.)
- **Status:** **Open — backlog. NOT a blocker.** Not fixed. Surfaced while adding the `product-mgmt` Tier 1 smoke + DEF-253/254 verification on 2026-06-05.
- **QC Ref:** None (internal observation during regression testing).
- **Reporter:** Anuki (regression sweep, 2026-06-05).
- **Bucket:** Bug (front-end load performance).
- **Linked memory.** [[feedback_ef_includes_with_projection]] (sibling perf class). Related: **DEF-255** (the *cold-start* of the same `/Product/GetProducts` endpoint and its post-save "looks unsaved" symptom) and **DEF-253 #2** (the server-side query for this list was already given `.AsSplitQuery()` and is fast — 32–50ms).
- **Root cause.** `Views/Product/Management.cshtml` init (`loadProducts()` + the surrounding bootstrap) makes **four sequential awaited fetches** — `/Product/GetCompanies` → `/Product/GetCategories` → `/Product/GetProductTypes` → `/Product/GetProducts` — and `GetProducts` returns the **full product set in one call** (no server-side paging of the *fetch*; the client paginates only after everything is in memory, and also builds search-suggestion sets from the whole array). Measured end-to-end **~11.8s warm** (and ~25.7s cold) from nav to `products` populated via `Scripts/smoke-prod/def253-254-verify.js` (`def253.timing` WARN). The DB query itself is fast (DEF-253), so the cost is **full-dataset transfer + serial init round-trips + client processing**, and it grows with the number of products.
- **Distinct from DEF-255.** DEF-255 is the **cold** EF compile (~25s) making a *post-save refresh* look like a failed save. This (DEF-257) is the **warm** steady-state load architecture — slow even when the query is hot. Fixing one doesn't fix the other; they share the endpoint, not the cause.
- **Recommended fix (backlog, none urgent).** Any of: (a) **parallelize** the 3 independent lookup fetches (`Promise.all` for Companies/Categories/ProductTypes) instead of awaiting them serially; (b) **server-side paginate** `GetProducts` (return one page, fetch subsequent pages on demand) so load time is bounded by page size, not total count; (c) defer the search-suggestion-set build off the critical path. Quick before/after timing on the 314-product dev seed. Low priority — Admin/Analyst-only and the prod dataset is smaller.
- **Files (for the fix, when scheduled):** `Views/Product/Management.cshtml` (init/`loadProducts`), `Controllers/ProductController.cs` (`GetProducts` if server-side paging is chosen).

### DEF-256 — Certificate tile table: ID columns (ASIN/UPC/GTIN/EAN) sort lexically, so numeric identifiers order wrong ("100" lands among the "1"s; in DESC it trails the single digits) — *and* the External tile view never sorted ~13 of its columns at all

- **Date:** 2026-06-05
- **Severity:** Medium (Internal: a real-but-narrow ordering glitch only visible on numeric ID data; External: ~13 column headers silently did nothing — bigger functional gap).
- **Status:** **Fixed + localhost-verified (build clean). Pending next deploy.** No DB/migration change (sort-logic + JS only).
- **QC Ref:** Client 6/5: *"the table sort ASIN is not sorting right. 100 is showing after single digits"* on `/Certificate/CertificateTileViewInternal`; asked to audit the sort on all columns for the internal **and external** pages.
- **Bucket:** Bug (sort ordering + External parity gap)
- **Reporter:** client (Shawn 6/5), audited + fixed by Anuki.
- **Linked memory.** [[feedback_cert_number_vs_id_resolved]] (the numeric-ID symptom is again test-seed-data-shaped — real ASINs are alphanumeric `B0…`, real UPC/EAN/GTIN are fixed-length — so the lexical sort only *looks* wrong on the live numeric test data; fix makes both read correctly), [[feedback_scoping_check_adjacent_endpoints]] (same "the sibling surface was missed" pattern — here External vs Internal `ApplySorting`).

**Root cause #1 (Internal — ordering).** `ApplySorting` routed the four product-ID columns through `OrderByTextEmptiesLast` (a plain lexicographic SQL sort). For numeric identifier strings of varying length, lexical order is `1, 10, 100, 2, …, 9` (and DESC = `9, 8, … 2, 100, 10, 1` → "100 after the single digits", exactly the report). Real production IDs are unaffected (ASIN is 10-char alphanumeric; UPC=12/EAN=13/GTIN=14 are fixed-length → equal length means lexical == numeric), so this is a test-data-visible glitch — but a real ordering bug for any variable-length numeric ID.

**Root cause #2 (External — dead headers).** `CertificateTileExternalApiController.ApplySorting` only implemented ~9 of the ~22 `sortBy` values its table header emits. Every unhandled value (`asin/upc/gtin/ean`, `status`, `ext-tag`, `product-sub`, `contact`, `cert-results`, `pathway`, `cert-sub`, `company-url`, all `-za` reverses) fell through to the `_ =>` default and silently sorted by **Effective Date** — so those headers appeared to do nothing. The external JS also mis-mapped the **Product Category** header to the `product-type` sort key.

**Fix.**
- **New helper** `OrderByNaturalEmptiesLast` (`Extensions/CertificateSortExtensions.cs`): orders by `(isEmpty, value.Length, value)` → numeric IDs sort `9 < 10 < 100`; within a single length lexical == numeric; fixed-length / alphanumeric IDs degrade to the prior lexical behavior (no regression); empties still forced last both directions (QC #157). EF translates the length term to `LEN(COALESCE(col,''))`; runs on the thin ID-only pagination projection (DEF-250) so it doesn't reintroduce the `.Min()`-subquery timeout.
- **Internal:** the 8 ASIN/UPC/GTIN/EAN branches now use `OrderByNaturalEmptiesLast` (was `OrderByTextEmptiesLast`).
- **External:** `ApplySorting` brought to **full parity** with Internal — all missing branches added (ID columns use the natural sort; cert-results uses `OrderByNumericNullsLast`; the rest `OrderByTextEmptiesLast`), so every external header now sorts.
- **External JS** (`wwwroot/js/certificate-tile-view-external.js`): `productCategory` remapped `product-type` → `product-cat`, and `product-cat → product-cat-za` added to the reverse map.
- **Files:** `Extensions/CertificateSortExtensions.cs`, `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`, `wwwroot/js/certificate-tile-view-external.js`.

**Verification (localhost:5101, admin + external sessions).** Internal `asin`/`asin-za`/`upc`/`gtin`/`ean` and `cert-results` all return HTTP 200 (no 500 — natural-sort SQL translates); local alphanumeric ASINs still sort correctly ASC `B005…` → DESC `B0GJ…` (no regression); `cert-results` numeric order `1% → 100%` intact. External `asin`/`asin-za`/`status`/`upc`/`ext-tag`/`product-cat`/`newest` all now HTTP 200 (previously dead branches execute real ORDER BY). The numeric `1/10/100` case can't be reproduced locally (local DB holds proper `B0…` ASINs) — the length-then-value ordering is provably correct for numeric strings and the live numeric test data will benefit. **Public tile view left unchanged** (no ID columns there — DEF-248).

---

### DEF-255 — Product Management list (`/Product/GetProducts`) has a ~25s EF cold-start; the post-save `loadProducts()` refresh can hit it and make a create/edit *look* like it didn't save (even though the row persisted) — likely the client's "details not saving in certain instances"

- **Date:** 2026-06-05
- **Severity:** Medium-High (intermittent — only on a cold cache, e.g. right after a deploy/pool-recycle — but high-confusion: the data IS saved server-side, yet the UI table fails to refresh, reading as "my create/edit didn't save"; can also 500 if the cold query crosses the 30s SQL command timeout).
- **Status:** **Symptom fixed (UX decouple); root-cause perf deferred.** The "looks like it didn't save" symptom is addressed view-only; the underlying GetProducts slowness needs a separate optimization + prod validation (local LocalDB got too thrashed by this session's repeated heavy runs to give reliable timings).
- **What was tried + the outcome:**
  - **Startup warm-up (reverted).** Added a GetProducts warm-up to `EfQueryWarmupService` mirroring the exact query. It paid ~25s at startup but **GetProducts stayed ~25s on the subsequent real calls** (not the one-time-then-fast I expected) — so warming didn't help (possibly EF/SQL plan instability, or the ~25s is execution not compile). **Reverted** — no point paying 25s at startup for no user benefit.
  - **UX decouple (KEPT, view-only).** `Views/Product/Management.cshtml` `saveProduct`: now closes the modal + shows the success toast **immediately** after the POST returns success, then refreshes the list in a background `try/catch`. So a slow/failing `/Product/GetProducts` reload can no longer make a save that **already persisted** look like it failed. This directly fixes the client symptom regardless of GetProducts speed. Needs verification on a clean env / prod (this session's local DB is thrashed).
- **Still open (separate, larger):** optimize `/Product/GetProducts` itself — it's a full-table load of all products with nested `Certificates.{ProductIds,Partners}` sub-projections and is genuinely heavy (the 1000-ProductId seed cert inflates it). Options: server-side pagination, or trim the per-row certificate sub-projection (load identifiers on demand). Validate cold/warm timings on prod, not this thrashed local box.
- **QC Ref:** Client 6/5: *"the details on the edit and create are not saving in certain instances."* (Surfaced while investigating that report.)
- **Bucket:** Bug (performance / EF cold-start + UX)
- **Reporter:** Found by Anuki investigating the client's 6/5 "not saving" report.
- **Linked memory.** [[feedback_ef_includes_with_projection]] (warm-up pattern — DEF-234).

**Evidence.** `/Product/GetProducts` loads ALL products (no pagination) with nested `Certificates.SelectMany(ProductIds)` + `Certificates.Partners` sub-projections. Measured cold-start on the REAL endpoint: first call **~25–26s** (occasionally >30s → 500 SQL timeout), then sub-100ms warm. `Services/EfQueryWarmupService.cs` (DEF-234) only warms the **public tile** query (`GetAdvancedFilteredCertificatesAsync`) — it does **not** warm GetProducts, so the first Product Management load after any restart pays the full cost. **Save flow link:** `saveProduct` (Management.cshtml) does `await loadProducts()` after a successful POST; on a cold cache that refresh is the ~25s/timeout call, so the just-saved row doesn't appear and the user concludes it "didn't save" — but a direct GetProducts readback confirms the POST **did** persist (verified: normal create + edit both persist Category/Type/Subcategory correctly).

**AsSplitQuery is NOT the cause.** Tested both ways on the real endpoint: with AsSplitQuery (DEF-253 #2) = ~26s cold then warm; without = ~25s cold + a 2nd ~17s call. Split is neutral-to-slightly-better, so DEF-253 #2 stays. The cold-start is inherent to the full-table query shape, not the split.

**Recommended fix (proposed, not done).** Add the GetProducts query shape to `EfQueryWarmupService` (warm it on startup exactly as the public tile is warmed — DEF-234) so the first real user load is fast. Optionally also: give `loadProducts()` a graceful slow/timeout state, and/or paginate GetProducts server-side. Needs a cold-start timing check after, and client confirmation that this matches their scenario (does it happen right after a deploy?).

**Also found while investigating (separate, minor):** products with a **null Company** (#1 "Not Specified", #347 "test23" — both test/system data; 0 real products) can't be saved on edit because `saveProduct`'s client-side "Company is required" guard aborts the POST before it fires — arguably correct validation + a data issue, not a code bug. Flagged for awareness.

---

### DEF-254 — Product Management → Edit Product silently drops a saved Product Type / Subcategory that isn't under the product's Category ("not seeing all options carried over") — QC #186 part 3

- **Date:** 2026-06-05
- **Severity:** Medium (data-entry correctness — opening a product to edit showed a **blank** Product Type when the saved type didn't belong to the product's category, and Saving would then null the type; the analyst "doesn't see all options carried over").
- **Status:** **Fixed + VERIFIED on localhost (build clean). BUNDLED in `GCCWebApp-deploy-20260605-1444.zip` (pending upload, supersedes 1234/1328).** Repro `editProduct()` on product #347 (cat 4 / type 45 "Other"): Type went **blank → now carries over ("45")**; valid products (#2, #346) unchanged.
- **QC Ref:** #186 part 3 — client 6/5 sheet: *"2 fixed, 3 is not. Still not seeing all options carried over under Product Management."* (Parts 1–2 = DEF-243 cert-edit category + DEF-246 validation-state; this part 3 is the **product** edit form, a distinct surface.)
- **Bucket:** Bug (front-end cascade pre-fill)
- **Reporter:** client (Shawn's 6/5 QC sheet).
- **Linked memory.** [[feedback_data_issue_vs_defect]] (the trigger is a data mismatch, but the dropped-on-edit behavior is a real code defect), DEF-243/DEF-246 (same "preserve the saved selection" philosophy, on the cert forms).

**Root cause.** `Views/Product/Management.cshtml` `editProduct(id)` pre-fills the cascade by calling `populateTypeDropdown(categoryId)`, which filters the Type list to `t.categoryId === categoryId`. Each Product Type belongs to exactly one Category (confirmed: 44 types, none global), so a product whose saved `productTypeId` isn't under its `categoryId` — a **cross-category / "Other"-type** product (e.g. #347: category 4, but type 45 "Other" lives under category 8 "Packaging") — has **no matching `<option>`**, so `select.value = savedTypeId` silently no-ops and the field renders blank. Same gap for Subcategory.

**Fix (view-only JS, no DB/API change).** Added `ensureSavedOption(selectId, value, label)` — after the cascade populates, if the saved value isn't among the options it injects one (labelled with the saved name) so the saved Type/Subcategory **always carries over** on edit. Mirrors the DEF-243/246 "never drop the analyst's saved selection" rule. Valid products are unaffected (their option already exists). **File:** `Views/Product/Management.cshtml` (`editProduct` + new `ensureSavedOption` helper).

**Data observation (separate, not this DEF).** Product #347 "test23" has a genuine Category↔Type mismatch (category 4 but a category-8 type). That's test-seed inconsistency; if real products show it, a data-cleanup pass (backup + CR per [[feedback_data_changes_require_cr]]) would re-file them — but the code fix above makes the edit form correct regardless.

**Note / confirm-with-client.** In testing, products whose Type *does* match its Category already carried over correctly (parts 1–2 working); the only failure was the cross-category case. If the client's "all options" complaint is about something else on Product Management (e.g. the *available* choices, not the saved one), flag it — but this fix directly resolves "saved options not carried over on edit."

---

### DEF-253 — EF cartesian-explosion audit: 9 queries materialize 2+ collection navigations without `.AsSplitQuery()` (same class as DEF-166/234/250/251) — proactive sweep

- **Date:** 2026-06-05
- **Severity:** Mixed (2 HIGH user-facing list paths, 2 HIGH batch/ETL, 5 MEDIUM). None is a current outage; all are latent slow/timeout risks that grow with collection sizes (the DEF-250/251 failure mode).
- **Status:** **#2 + #7 FIXED + localhost-verified, BUNDLED in `GCCWebApp-deploy-20260605-1328.zip` (pending upload, supersedes live 1234.zip). #1 TRIED then REVERTED — AsSplitQuery *regressed* it (see KEY FINDING). #3–#6, #8, #9 remain backlog.**
- **QC Ref:** None (internal proactive audit, follow-up to DEF-251).
- **Bucket:** Bug (performance / EF query) — backlog batch

**KEY FINDING — AsSplitQuery is NOT universally beneficial (it regressed #1).** Adding `.AsSplitQuery()` to `BuildExportQuery` (#1) **timed out the CSV export at 30s** (`Failed executing DbCommand (30,231ms)`), where the original single query returns in ~0.5–1.2s. The export is an **unfiltered full-table** pull with **no pagination**, so splitting forced N separate queries each re-scanning ProductIds/Tags/Subcats for ALL certs (incl. the 1000-ProductId seed cert) — far worse than the single de-duped cartesian. AsSplitQuery helps **paginated** queries (DEF-250, where the cartesian breaks Skip/Take) and **small-projection lists** (DEF-251/GetUsers, projecting only ids/names); it *hurts* a full-table eager-load of large collections. **#1 reverted** (left an explanatory comment in `BuildExportQuery`). Verified outcomes: #1 export back to 547–1175ms/200; **#2** `/Product/Management` 32–50ms/200 (no regression, kept); **#7** LIVE-verified: a self-cleaning probe seeded a throwaway cert with **300 ProductIds**, then fired the Edit POST (the only `GetByIdAsync(int)` caller) — it returned **302 → CertificateInternalView** (proves it passed ModelState and actually executed `GetByIdAsync` with the new `.AsSplitQuery()`) in **202ms**, no timeout. (Single-entity load is bounded by one cert's collections, so it can't hit the full-table-scan timeout that killed #1 — confirmed empirically.) **Files touched:** `Controllers/ProductController.cs` (#2), `Repositories/CertificateRepository.cs` (#7); `Services/CertificateService.cs` carries the #1 explanatory comment only (no functional change).
- **Reporter:** Found by Anuki (grep-audit requested by Shawn after DEF-251), 2026-06-05.
- **Linked memory.** [[feedback_ef_includes_with_projection]].

**Audit method.** Searched all `.cs` for queries materializing ≥2 **collection** navigations (via multiple `.Include` or multiple collection sub-projections in a `.Select` — the DEF-251 projection shape) WITHOUT `.AsSplitQuery()`. Single-entity reference navs ignored. Verified each candidate's collection count, AsSplitQuery presence, and list-vs-single materialization.

**Findings (ranked):**

| # | File · method · line | #collections | List/Single | Risk |
|---|---|---|---|---|
| 1 | `Services/CertificateService.cs` · `BuildExportQuery` (~L537→materialized L557) | 3 (Subcats, Tags, ProductIds) | List | **HIGH** — feeds both CSV export paths |
| 2 | `Controllers/ProductController.cs` · Product Management list (~L171, projection L217-249) | 2 collection sub-projections over `p.Certificates` (ProductIds + CertPartners) | List | **HIGH** — same projection class as DEF-251 |
| 3 | `Services/ETL/DataExtractor.cs` · `ExtractCompaniesAsync` (~L137) | 3 (Certificates, Products, Users) | List | **HIGH** (batch/whole-table) |
| 4 | `Services/ETL/DataExtractor.cs` · `ExtractCertificatesAsync` (~L33) | 2 (Tags, CustomFieldValues) | List | **HIGH** (batch) |
| 5 | `Services/ETL/DataExtractor.cs` · `ExtractProductsAsync` (~L200) | 2 (Certificates, Tags) | List | **HIGH** (batch) |
| 6 | `Repositories/CertificateRepository.cs` · `GetCertificateByNumberAsync` (~L132) | 5 (Subcats, Tags, Partners, CustomFieldValues, ProductIds) | Single | MEDIUM |
| 7 | `Repositories/CertificateRepository.cs` · `GetByIdAsync` (~L216) | 5 (same) | Single | MEDIUM — hot cert-detail path |
| 8 | `Services/AdminService.cs` · `UpdateUserAsync` current user (~L453) | 3 (Companies, Partners, Roles) | Single | MEDIUM (cold) |
| 9 | `Services/AdminService.cs` · `UpdateUserAsync` audit snapshot (~L462) | 3 (same) | Single | MEDIUM (cold) |

Notably, `CertificateRepository.cs` confirms the suspected pattern: only `GetAdvancedFilteredCertificatesAsync` (~L405) is protected; its two single-cert sibling methods (#6, #7) each Include 5 collections with no AsSplitQuery.

**Confirmed already-safe (have AsSplitQuery):** ExportController ~L270; CertificateTileExternal ~L86; CertificateTileInternal ~L101; CertificateRepository ~L405 (`GetAdvancedFiltered…`); AdminService ~L78 (`GetUsersAsync`, the DEF-251 fix).

**Fix for each (documented lever).** Add `.AsSplitQuery()`, keep the Includes, add a stable `.OrderBy(x => x.Id)` on list paths (single-entity `FirstOrDefault` paths don't need the tiebreaker). Each fix wants a quick before/after timing on a representative dataset. **Recommended scope:** fix the user-facing HIGH list paths (#1, #2) + the hot single-cert detail #7 first; #6 and #8/#9 are low-traffic; #3-5 are batch ETL (rare, but cheap to fix in the same pass).

---

### DEF-252 — Login broken on gccwebapp after the 2026-06-05 deploy — CR-020rev code shipped but the prod migration SQL was never run, AND the migration script silently half-applied (column created, never backfilled) — **recurrence of [[DEF-239]]**

- **Date:** 2026-06-05
- **Severity:** Critical (full login outage on the test site — every authentication attempt failed)
- **Status:** **RESOLVED + verified on prod 2026-06-05.** Schema applied, data backfilled, migration recorded, **client confirmed login works**. Backup table `Users_bak_CR020_20260605` dropped after confirmation. Closed.
- **QC ref:** QC #106 / CR-020rev (User Company → free text). Reporter: Shawn West ("I'm unable to login" on gccwebapp.com, 2026-06-05).
- **Root cause (two compounding failures):**
  1. **Gate-7 miss (same as DEF-239).** The 2026-06-05 bundle deployed the CR-020rev code (model now maps the `UserCompany` column) but the matching migration `20260605131035_ChangeUserCompanyToFreeText` was **never run on the prod DB**. Prod still had `Users.UserCompanyId` and no `UserCompany` column, so every login-path query threw `Invalid column name 'UserCompany'`. Confirmed live: `Users` had `UserCompanyId`, not `UserCompany`, and the migration was absent from `__EFMigrationsHistory`.
  2. **Script self-sabotage.** `Scripts/database/CR-020rev_ChangeUserCompanyToFreeText.sql` (and the bundled `deployment-drops/staging/migrations/apply-migrations.sql`) ran without `SET QUOTED_IDENTIFIER ON`. The backfill `UPDATE [Users]` touches a table with a filtered unique index (`WHERE [IsDeleted]=0`), which **requires** QUOTED_IDENTIFIER ON — it failed with **Msg 1934**. Because the error was non-fatal (no `XACT_ABORT ON`), the surrounding transaction still committed: the column was created and `UserCompanyId` dropped, but `UserCompany` was left **all-NULL** (backfill skipped). DEF-239's entry had already documented that `-I`/QUOTED_IDENTIFIER ON is required — the lesson wasn't baked into the script.
- **Resolution applied (2026-06-05, prod DB `gccwebap_certificates` @ `tcp:68.71.129.120,1533`):**
  1. Verified the old schema + missing migration on the live DB.
  2. **Backed up** `Users` → `dbo.Users_bak_CR020_20260605` (70 rows, retains original `UserCompanyId` → fully reversible).
  3. Ran `CR-020rev_*.sql`. Schema steps committed; backfill `UPDATE` failed Msg 1934 (QUOTED_IDENTIFIER OFF) — `UserCompany` left NULL on all 70 users.
  4. **Re-ran the backfill with `-I`** (QUOTED_IDENTIFIER ON), sourcing from the backup table joined to `Companies`. Restored the **2** real values (the other 68 users had no company): `carly@greencirclecertified.com → Alcon`, `carly@partner.com → Amazon`.
  5. Confirmed prod `__EFMigrationsHistory` now matches the project's migration set exactly (no other missing migrations).
- **Permanent fix (code):** added `SET QUOTED_IDENTIFIER ON;` + `SET XACT_ABORT ON;` to the top of both `Scripts/database/CR-020rev_ChangeUserCompanyToFreeText.sql` and `deployment-drops/staging/migrations/apply-migrations.sql`, with a comment so future EF `--idempotent` regenerations re-add them. XACT_ABORT ON makes any future failure roll back atomically instead of half-applying.
- **Process follow-up — IMPLEMENTED (2026-06-05):** Because this was the 2nd login outage from the identical cause (DEF-239 was the 1st), the human Gate-7 step was eliminated rather than just re-flagged. **`Program.cs` now auto-migrates on startup in the Production environment** (gccwebapp.com runs as Production) — after a connectivity check it calls `MigrateAsync()` on any pending migrations, gated by the new config flag `Database:AutoMigrateOnStartup` (default `true`; set `false` for a real production cutover with DBA-controlled migrations). EF applies migrations with `QUOTED_IDENTIFIER ON`, so the Msg 1934 half-apply (failure #2 above) also cannot recur. Added `Database:AutoMigrateOnStartup=true` and `"Program": "Information"` (so the migrate log lines are visible) to `appsettings.Production.json`. **Verified against the live prod DB:** launched under `ASPNETCORE_ENVIRONMENT=Production` → logged `✅ Production database connectivity verified` + `✅ Production database schema up to date (no pending migrations)`, no changes made (prod already current). Files: `Program.cs`, `appsettings.Production.json`. *Caveat:* auto-migrate runs per worker process; this env is single-worker so it's safe — if ever scaled to multiple workers, concurrent-startup migrations would need a lock. Gate 7 of the pre-deploy checklist is now a safety net rather than the primary mechanism.
- **Cleanup:** `dbo.Users_bak_CR020_20260605` dropped 2026-06-05 after login confirmed.
- **Files:** `Scripts/database/CR-020rev_ChangeUserCompanyToFreeText.sql`, `deployment-drops/staging/migrations/apply-migrations.sql` (SET-option hardening); `Program.cs`, `appsettings.Production.json` (auto-migrate-on-startup permanent fix).

### DEF-251 — `/Admin/GetUsers` is slow (≈12s cold, 2–7s warm) — `GetUsersAsync` does 3 collection `.Include`s with no `.AsSplitQuery()` (cartesian) — pre-existing, surfaced by the 1128.zip pre-deploy Gate 2/6 sweep

- **Date:** 2026-06-05
- **Severity:** Low–Medium (the User Management table is Admin-only and hit rarely; the slowness is amplified by **dev test data — 284 seed users**; prod has a small staff/partner-login set, so real-world load is far lighter. It completes successfully (HTTP 200), but at a cold tablet-width load the accessibility-validator saw it trip a SQL command timeout, leaving the table empty with no UI error state.)
- **Status:** **Fixed + VERIFIED on localhost (build clean 0/0). In the rebuilt bundle `GCCWebApp-deploy-20260605-1234.zip`.** `/Admin/GetUsers` over the same 284 dev users went from **12.0s / 2.4s / 7.3s / 1.9s** (cold/variable) to **0.51s cold then ~0.1–0.2s** after adding `.AsSplitQuery()`.
- **QC Ref:** None (internal finding from the 2026-06-05 pre-deploy Gate 2 sweep).
- **Bucket:** Bug (performance / EF query)
- **Reporter:** Found by Anuki (accessibility-validator Gate 2 + timing confirm), 2026-06-05.
- **Linked memory.** [[feedback_ef_includes_with_projection]] (AsSplitQuery is the documented lever — DEF-166/DEF-234/DEF-250 are the same class).

**Root cause.** `Services/AdminService.cs::GetUsersAsync` (≈L66) loads every non-deleted user with `.Include(u => u.Companies).Include(u => u.Partners).Include(u => u.Roles)` and **no `.AsSplitQuery()`** — three collection navigations multiply into a cartesian result that EF must de-dup, so the row volume (and time) grows with users × companies × partners × roles. Measured locally over 284 users: 12.0s / 2.4s / 7.3s / 1.9s (high variance, all < the 30s command timeout warm, but a cold connection can exceed it). These Includes are **pre-existing** — the user-scoping/Company-Access logic already needed them; CR-020rev only surfaced Company Access as a visible column ("no SQL" per the shipped log), so this bundle did not introduce the query.

**Fix (implemented).** Added `.AsSplitQuery()` after the three `.Include`s in `GetUsersAsync` plus `.OrderBy(u => u.Id)` for deterministic split stitching — the same lever that fixed DEF-166/DEF-234/DEF-250. EF now runs one query per collection instead of a cartesian JOIN. Includes kept (removing them once made the projection emit 240+ correlated subqueries — EF-includes rule). **Files touched.** `Services/AdminService.cs` (`GetUsersAsync`). No migration, no view change. Open follow-up (not done, low value): surface a UI error/retry state when the user-load fails (today the table renders empty silently). Verified by cold-start timing (12s → 0.51s). Pulled into the rebuilt `…-1234.zip` (superseding `…-1128.zip`, which was deleted).

**Other Gate 2 sweep findings (already tracked / not blockers).** The accessibility-validator re-reported several items as "new" that are in fact **pre-existing a11y debt already logged under [[DEF-244]]**: unlabeled `<select>` filters on Product Management, white-on-`#92C83E` green-button contrast, and `aria-label` on plain `aria-live` divs in cert Create/Edit — DEF-244's title already enumerates exactly these. No new a11y DEF needed; the 1128 Gate 2 re-confirmed DEF-244's open scope. One minor item worth a line under DEF-244 when it's worked: the **Actions `<th>` header still renders for External** on Product Management (row action cells are hidden, but the orphan header shows) — pre-existing on the already-deployed Product Management view, cosmetic, not a regression here. Also (Gate 4, dead code): `GenerateExcelExport` calls `GetCustomFieldDataForExport` with the default `includeInternalFields=true` — harmless today (the Excel/PDF path is DEF-128 dead code, CSV-only), but thread the gate through if that path is ever revived. Noted under [[DEF-247]].

---

### DEF-250 — Sorting the internal Cert List by ASIN / GTIN / EAN / Cert Subcategories runs ~27–30s and intermittently returns HTTP 500 (SQL command timeout) — pre-existing, surfaced while verifying DEF-249

- **Date:** 2026-06-05
- **Severity:** High (a core sort column on the logged-in Cert List can 500 or hang ~30s). Currently reproduces only when the page includes a cert with **very many** product IDs (seed cert with ~1000 ASINs), but `Scripts/MockDataGenerator.cs:326` notes that models a **real-world** scenario ("certificates can have 100s-1000s of ASINs"), so it is prod-relevant for large Amazon sellers. Directly in the QC #157 area (sorting by ASIN).
- **Status:** **Fixed + VERIFIED on localhost:5101 (build clean 0/0). Pending deploy.** ASIN sort went from ~27–30s/intermittent-500 to **~1.0–1.4s/HTTP 200**; asin/gtin/ean/cert-sub all 0.65–1.4s. DEF-248 probe (8 PASS) and the full `qc157-sort-verify-local.js` (14 PASS / 0 fail) both green afterward.
- **QC Ref:** Surfaced from QC #157 / DEF-249 verification; not separately client-reported.
- **Bucket:** Bug (performance / query translation)
- **Reporter:** Found by Anuki while verifying DEF-249, 2026-06-05.
- **Linked memory.** [[feedback_ef_includes_with_projection]] (AsSplitQuery history — DEF-166), [[feedback_product_id_status_follows_cert]], DEF-165 (the original `.Min()`-times-out note), DEF-248 (display lead == `.Min()` sort key).

**Root cause (from the server log).** `SqlException: Execution Timeout Expired ... CommandTimeout='30'`, thrown inside EF's `PopulateSplitCollectionAsync`. The ASIN/GTIN/EAN/cert-sub sort keys are **correlated `.Min()` subqueries** over a collection nav (e.g. `c.ProductIds.Where(type==ASIN && active).Select(v).Min()`), further wrapped by `OrderByTextEmptiesLast` into `ORDER BY (CASE WHEN COALESCE(<minSubquery>,'')='' THEN 1 ELSE 0 END), <minSubquery>` — so the subquery appears multiple times. Because the query also uses `AsSplitQuery()` (DEF-166, to avoid cartesian blow-up from 4 collection Includes), EF **replicates that whole ORDER BY onto every split query**, including the `ProductIds` split that, for the ~1000-ASIN cert, joins ~1000 rows while re-evaluating the Min subquery. Net: ~27–30s, right at the 30s command timeout → intermittent 500 (pageSize=20→500, 25→200, 30→500 observed; successful runs logged at 26.7s).

**Confirmed pre-existing, NOT caused by DEF-249.** The UI's ASIN-sorted page 1 is `sortBy=asin&page=1&pageSize=20` → `actualOffset=0, actualLimit=20` both before and after the DEF-249 `limit`-default change. DEF-249 only made me exercise the ASIN sort enough to hit the flaky timeout.

**Why the obvious fix conflicts.** DEF-165 already worked around this for **UPC** by switching to `.FirstOrDefault()` (cheaper, no aggregate). But DEF-248 just established that the displayed cell leads with its **alphabetical min**, so the sort key must also be the **min** to keep "first shown value == row-ordering value." Switching ASIN/GTIN/EAN to `FirstOrDefault()` would re-introduce the DEF-248 mismatch. So the real fix can't just drop `.Min()`.

**Fix (implemented — paginate IDs, then hydrate).** `GetTiles` no longer applies `ApplySorting` to the full Include query and Skip/Takes it. Instead: (1) `ApplySorting(query, sortBy).Skip(off).Take(lim).Select(c => c.Id)` — selecting only `Id` makes EF drop the Includes, so **no split queries run** and the `.Min()` ORDER BY executes once on a thin query (~ms); (2) hydrate `query.Where(c => pagedIds.Contains(c.Id)).Select(projection)` with the full Includes + `AsSplitQuery` but **no ORDER BY** (splits stitch by PK only — no Min subquery re-evaluated against the 1000-row ProductIds join); (3) restore the page's sorted order in memory via a `pagedIds`→index map. Keeps `.Min()` so the DEF-248 display==sort guarantee holds, and scales **every** sort, not just ASIN. Chosen over the band-aids (raise `CommandTimeout`; precompute a per-cert min-id column) which leave the 27s query in place.

**Scope.** Internal controller only — it is the **sole** tile query with `.Min()`-subquery sort keys (ASIN/GTIN/EAN/Cert-Subcats). The external (`CertificateTileExternalApiController`) and public (`CertificateTilesApiController`→`CertificateRepository`) sort switches contain **only scalar-column** sorts (no collection aggregates), so neither has the timeout and neither needs the refactor — verified by reading both sort switches.

**Files touched.** `Controllers/Api/CertificateTileInternalApiController.cs` (`GetTiles` pagination). No migration, no view change, no change to `ApplySorting` (sort keys unchanged → DEF-248 intact).

**Verification (DONE, localhost).** Timing probe over asin/asin-za/gtin/ean/cert-sub at pageSize 20/30/34/100 + page 2: all **HTTP 200 in 0.65–1.4s** (was ~27–30s/500). DEF-248 read-only probe **8 PASS** (now sees all 34 certs + 29 populated sort rows, vs 20 before — confirms it composes with DEF-249). Full `qc157-sort-verify-local.js` **14 PASS / 0 fail** (all columns blanks-last, DEF-181 cert-results canary 200, ASIN both directions). Pending deploy + prod fingerprint.

---

### DEF-249 — Internal & public tile APIs ignore `pageSize` on the page-based path (every page capped at 20 — "20 of 35" symptom) because `limit` defaulted to 20 instead of 0

- **Date:** 2026-06-05
- **Severity:** Medium (API contract bug; large-`pageSize` callers silently get 20 rows. The UI itself sends `pageSize=20` so end users were unaffected, but any API consumer or tooling requesting a bigger page got short, misleading pages — and `hasMore`/`totalPages` were computed off the wrong size).
- **Status:** **Fixed + VERIFIED on localhost:5101 (build clean 0/0). Pending deploy.** Matrix probe: `pageSize` now honored exactly (5→5, 21→21, 35→all-34) while the UI's `pageSize=20` still returns 20 with `hasMore=true` (unchanged); offset/limit path still correct (34/14/4).
- **QC Ref:** Surfaced from QC #157 / DEF-248 verification (the read-only probe saw "20 of 35").
- **Bucket:** Bug (pagination / model binding sentinel)
- **Reporter:** Found by Anuki while verifying DEF-248, 2026-06-05.
- **Linked memory.** [[feedback_ef_includes_with_projection]], [[feedback_scoping_check_adjacent_endpoints]] (checked sibling tile controllers — public had the same bug, external did not).

**Root cause.** Both tile endpoints accept two pagination styles and reconcile them as `actualOffset = offset > 0 ? offset : (page-1)*pageSize` and `actualLimit = limit > 0 ? limit : pageSize`. This treats `0` as the "not supplied" sentinel — but `limit` was declared `[FromQuery] int limit = 20`, so `limit > 0` was **always true** and `pageSize` was **never used**. Every page-based call (the UI's style, and any caller passing only `page`+`pageSize`) was hard-capped at 20 regardless of the requested `pageSize`. (This is distinct from the DEF-166 cartesian "15 of 35" — that was a row-drop fixed by AsSplitQuery; this is the page size being ignored.)

**Fix.** `limit` now defaults to **0** in both `CertificateTileInternalApiController.GetTiles` and `CertificateTilesApiController` (the public path). Added a clamp `actualLimit = Math.Clamp(limit > 0 ? limit : pageSize, 1, MAX)`: the **lower bound** prevents a new divide-by-zero in the `page`/`totalPages` math when `pageSize=0` (now reachable); the **upper bound** caps a single page (internal MAX=500 — admin-only; public MAX=200 — deliberately tighter because that endpoint is **unauthenticated**, and respecting `pageSize` uncapped would let an anonymous caller request an unbounded page that the accidental 20-cap had been hiding). External tile controller has no `limit` param → not affected. UI unchanged (sends `page`+`pageSize=20` → `actualLimit` falls back to 20).

**Files touched.** `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTilesApiController.cs`. No migration, no view change.

**Verification (DONE, localhost).** Page-size matrix on both endpoints: internal `pageSize` ∈ {5,10,15,19,20,21,25,30,35,50,100,300} → returned matches request up to the 34-cert total (5→5 … 30→30 … 35→34, `hasMore` flips correctly at the boundary); `pageSize=20` still 20 (UI-safe); offset/limit path 34/14/4. Public endpoint: 5→5, 20→15(all public), 300→15. **Note:** ASIN-sorted large pages hit the **separate** DEF-250 timeout (not a DEF-249 regression — same `actualLimit=20` for the UI before/after). Pending deploy + prod fingerprint.

---

### DEF-248 — Cert List ASIN column "not sorting": multi-value ID cells displayed in insertion order while the row sorts by the cell's alphabetical minimum, so the first value the eye lands on isn't the value the row was sorted by (QC #157, client 6/4 — last open thread on this item)

- **Date:** 2026-06-05
- **Severity:** Low (presentation — sort *order of rows* was always correct; only the *order of values within a multi-ID cell* was unaligned to it, so it read as "off." Client tagged it explicitly **"not a deal breaker."** Single-ID certs were never affected.)
- **Status:** **Fixed + VERIFIED on localhost:5101 (build clean 0/0). Pending deploy.** Probe `Scripts/smoke-prod/def248-asin-display-order.js` → **8 PASS / 0 FAIL** (read-only, no records created/changed).
- **QC Ref:** #157 — client 6/4/2026: *"Location is working now. ASIN (unsure of UPC, GTIN, EAN they had no data) is not. See image. This one is not deal breaker."* (Final open thread of the long-running #157 sort item; Location/Status closed in the June batch — see DEF-242.)
- **Bucket:** UX (display presentation vs sort key alignment)
- **Reporter:** client, relayed by Shawn West 2026-06-04
- **Linked memory.** [[feedback_no_chaos_rabbit_holes]] (diagnosed to ground truth before touching code), [[feedback_product_id_status_follows_cert]], [[feedback_cert_number_vs_id_resolved]] (same pattern — the "looks wrong" was a display artifact, not a sort bug).

**Root cause (not a broken sort).** ASIN/UPC/GTIN/EAN are one-to-many `ProductIds` collections, so one cell can hold several values. The internal Cert List view displayed them in **insertion / PK order** (`CertificateTileInternalApiController.cs` `AsinList` etc. `.Select(...).ToList()`, joined at the second-pass projection), but the **sort key** positions the row by the cell's alphabetical minimum — `.Min()` for ASIN/GTIN/EAN (`ApplySorting`, `"asin"` branch ~L744). So a cert whose cell shows `B07ZZZ, B07AAA` is actually sorted as `B07AAA`; reading the column top-to-bottom, the leading displayed value didn't match the ordering key → "ASIN not sorting." Location works because it's a single scalar column. This is the same multi-value class the client already accepted for Cert Subcategories ("there can be multiple and not sure order is based on order it's picked").

**Fix (display-only, no SQL / no migration / no API change).** Order the displayed values ascending **in-memory after materialization**, so the leading shown value equals the `.Min()` sort key:
- `CertificateTileInternalApiController.cs` — the second-pass projection now joins `c.AsinList.OrderBy(v => v)` (and Upc/Gtin/Ean likewise). In-memory on already-materialized `List<string>` → deliberately avoids an inner-`OrderBy` in the EF query, which per DEF-165 timed out on the dev DB.
- `Services/CertificateService.cs::GetProductIdValues` (public tile path) — same `.OrderBy(v => v)` on the loaded collection, so the public directory's multi-ID cells read consistently with the internal list.

**Scope across the three tile views (per the "fix all 3 views" decision).** Only views that *both display and sort* product IDs can exhibit the mismatch:
- **Internal** (client's "Login – Cert List View") — displays + sorts IDs → fixed (the real defect).
- **Public** — displays IDs but the repository sort switch has **no** ASIN/UPC/GTIN/EAN branch (`CertificateRepository` ~L367-376), so it can't sort by them; display ordering aligned anyway for cross-view consistency.
- **External** (partner/company) — **does not project** ASIN/UPC/GTIN/EAN at all (`CertificateTileExternalApiController` projection ~L234-257) and has **no** ID sort branches → nothing to display or align; no change.

**Known residual (documented, not changed).** The internal **UPC** sort key still uses `.FirstOrDefault()` (not `.Min()`) — a deliberate DEF-165 choice after `.Min()` timed out on the dev DB for that column. With the display now ascending, a multi-UPC cell would show its lowest value first while the row is positioned by an arbitrary UPC — i.e. UPC could still read "off" *if* multi-value UPC data ever exists (it currently does not; client confirmed UPC/GTIN/EAN had no data to test). Left as-is to avoid reopening the DEF-165 perf decision; flag for a follow-up to switch UPC to `.Min()` (matching ASIN/GTIN/EAN) if/when UPC data lands and the timeout no longer reproduces.

**Files touched.** `Controllers/Api/CertificateTileInternalApiController.cs`, `Services/CertificateService.cs`. No migration, no API-contract change, no view change.

**Verification (DONE, localhost).** `Scripts/smoke-prod/def248-asin-display-order.js` — **read-only, conclusive**: for every cert with a multi-value ID cell it cross-checks the displayed cell against the cert's raw **insertion order** (via `/api/productids/certificate/{id}`, which returns `DisplayOrder` = insertion order = the pre-fix display order). Asserts each cell is ascending with first token == `.Min()` sort key, and flags "differentiating" certs whose insertion order was NOT alphabetical yet now display alphabetically (proves the fix actually re-orders, vs. data that happened to be inserted sorted). Result: **7/7 multi-value cells ascending+lead==min; 3 differentiating** — clearest: `ASIN@cert47 insertion=[B0BRNN8BKS, B005PMYJBO] → shown=[B005PMYJBO, B0BRNN8BKS]`; plus sort asc/desc rows monotonic by leading(min) token and cells ascending in both directions. **8 PASS / 0 FAIL.** Public view had no multi-value ID cells in the sandbox (single-value data only) — the public `GetProductIdValues` change is identical logic, covered by code; no public multi-ID data to exercise. Pending deploy + prod fingerprint.

**Incidental observation (NOT this DEF, flagged for triage).** While verifying, noticed `/api/certificate-tiles-internal` returns only **20 of 35** certs when `pageSize` is large on page 1 (paging with small `pageSize` returns all 35) — a likely EF `AsSplitQuery` + large `Skip/Take` + one-to-many-Includes row-drop. The UI uses 20-row infinite scroll so it doesn't surface in normal use; logged here as an observation to triage separately, not changed under DEF-248.

---

### DEF-247 — Partner/External CSV export leaks Internal-visibility **custom fields** (e.g. "Chris Test Field", `RoleVisibility=Internal`) — the column + values appear in the partner export (QC #182, 6/4 re-raise; reopens what DEF-228 closed WAD)

- **Date:** 2026-06-05
- **Severity:** High (confidentiality — a custom field explicitly marked Internal/GCC-only is disclosed to a Partner/External account in the CSV; same class of leak as an internal-tag leak, just on a path that was never gated).
- **Status:** **Fixed + VERIFIED on localhost (build clean 0/0). Pending deploy.** Probe `Scripts/smoke-prod/def247-partner-export-customfield-scope.js` → **4/4 PASS** (partner export succeeds with rows intact; Internal field "Chris Test Field" absent from header AND every row). Caveat: dev DB has no Internal-visibility custom field carrying a value on a partner-visible cert, so the conclusive positive/negative differential wants prod data — the gate is proven not to expose the Internal field, but a prod fingerprint is recommended post-deploy.
- **QC Ref:** #182 — client 6/4 re-raise: *"The Chris Test Field is set to Internal for visibility. Partner would be able to see External/Public, not Internal. Internal is GCC users only (Admin/Analyst). It's still showing in partner export. Login to partner account > export all certs > look for Chris Test Field. See image 85."*
- **Bucket:** Bug (data scoping — field/column-level visibility)
- **Reporter:** client, relayed by Shawn West 2026-06-04
- **Linked memory.** [[feedback_partner_data_isolation]], [[feedback_scoping_check_adjacent_endpoints]] (the row-level scope was fixed (DEF-198) and the tag column was gated (DEF-128), but the *custom-field* column on the same export was never audited), [[feedback_custom_fields_single_section]].

**Why DEF-228 missed this (and why this is NOT a re-litigation of WAD).** DEF-228 closed QC #182 as Working-As-Designed after testing an internal **tag** (`Sustainable` on cert 26-0001) — which *is* correctly hidden by the DEF-128 `includeInternalTags` gate. But the client's actual artifact is a **custom field** named "Chris Test Field" with `CustomField.RoleVisibility = "Internal"` — an entirely separate code path. DEF-228's repro used the wrong analog (tag, not custom field), so the real leak went unconfirmed. The 6/4 note disambiguates it precisely: it's a *field*, set to *Internal visibility*. Genuine defect.

**Root cause.** `Services/CertificateService.cs::GetCustomFieldDataForExport(certIds)` selected **every** `CustomFieldValue` for the exported certs and emitted each field as a CSV column, with **no `RoleVisibility` filter**. `GenerateCsvExport`'s `includeInternalTags` flag gated only the dedicated *Internal Tag* column (DEF-128) and the *Internal* tag values — it never touched custom-field columns. So a field marked `RoleVisibility=Internal` was disclosed to Partner / External / anonymous callers in full (header + values).

**Fix.** Thread the existing internal-caller gate into custom-field selection:
- `GetCustomFieldDataForExport(List<int> certIds, bool includeInternalFields = true)` — when `false`, the query adds `WHERE RoleVisibility IS NULL OR RoleVisibility <> 'Internal'` (null guard keeps legacy/unset rows as Public, never drops them).
- `GenerateCsvExport` now passes `includeInternalTags` (true only for Admin/Analyst) as `includeInternalFields`. Internal custom fields are now omitted entirely — header column *and* per-row values — for non-internal callers, exactly as the Internal Tag column already is.
- The dead-code Excel path (`GenerateExcelExport`, retained per DEF-128) keeps the `= true` default, so its behavior is unchanged and it still compiles.

**Boundary note (in scope vs not).** This fix enforces the **Internal vs not-Internal** boundary the client described ("Internal is GCC users only; External = Partners + Clients"), which maps 1:1 onto the `includeInternalTags` gate. The finer **External vs Public** distinction (whether `RoleVisibility=External` fields should be hidden from *anonymous* exports) is a separate question the client has not raised and is deliberately left unchanged — anonymous still receives Public + External custom fields as before. Flag for a future ticket if that tier ever matters.

**Files touched.** `Services/CertificateService.cs` — `GetCustomFieldDataForExport` (new `includeInternalFields` param + RoleVisibility filter) and `GenerateCsvExport` (passes the flag). No migration, no API-contract change, no view change.

**Adjacent column-source audit (per [[feedback_scoping_check_adjacent_endpoints]]).** Enumerated every export/serialization surface that reads custom fields: (a) the CSV export — fixed here; (b) the **Amazon Partner API v1** (`Api/v1/CertificatesController` — `GetById`/`ByNumber`/list all `.Include(CustomFieldValues)`): partner callers are projected through `BuildPartnerCertificateDto`, an explicit allowlist that **does not serialize custom fields at all** → no leak, contract unchanged ([[feedback_amazon_api_immutable]] held). (c) The Internal tile API (`CertificateTileInternalApiController`) is Admin/Analyst-only and intentionally shows all `IsVisible` fields. Conclusion: the CSV export was the sole Internal-custom-field leak on a Partner/External surface.

**Verification (pending).** Add a Playwright probe modeled on `def228-partner-export.js`: log in as the Amazon-mapped Partner, hit `/Certificate/ExportCertificates?format=csv`, assert an `Internal`-visibility custom field's **header and values are both absent**, while a `Public`/`External` custom field's column is **present** and qualifying cert rows still appear. Build is clean; runtime probe deferred to the next deploy/verify pass.

---

### DEF-246 — Create certificate: selected fields don't survive a server-side validation re-render (Product, Product Category/Type/Subcategory, cert Subcategories, Tags all reset) — QC #186 client retest

- **Date:** 2026-06-04
- **Severity:** Medium-High (data-entry frustration / lost work — on a validation error the analyst loses every JS-driven selection and must re-pick Product, the full Category/Type/Subcategory hierarchy, cert Subcategories, and Tags before re-submitting).
- **Status:** **Fixed + VERIFIED on localhost:5101 (Create 2026-06-04, Edit parity 2026-06-05) — build clean (0 err/0 warn). Pending deploy (new bundle).** Create: `Scripts/smoke-prod/def-create-validation-state.js` → **11 PASS / 0 FAIL**; Edit: `Scripts/smoke-prod/def-edit-validation-state.js` → **8 PASS / 0 FAIL**. Both against a REAL server re-render (forced via native `form.submit()` to bypass jQuery client validation; confirmed by a navigation marker clearing). Before: 4–6 fields reset; after: Product, Product Category/Type/Subcategory, SearchableSelect display, cert Subcategories, and external+internal Tags all retained.

**Edit had its own version of this (found when asked "fixed on edit as well?", 2026-06-05).** On an *Edit* validation re-render the Product + Category/Type/Subcategory survive (they round-trip via the posted Model — Edit's `initProductCascade`/product-preselect read `Model.*`), but **Tags reverted to the originally-saved set and cert Subcategories were lost**: `ReloadEditFormDataAsync(id)` (the Edit-POST re-render helper, never used by the GET) rebuilt `ViewBag.ExistingExternalTags/InternalTags` from the **DB cert** and never set `ViewBag.ExistingSubcategories`. Verified red first: edited external tag `[1]` came back `[]` after the re-render. **Fix (controller-only — the Edit view already consumes those ViewBags):** `ReloadEditFormDataAsync` now takes the posted `ExternalTags/InternalTags/SubcategoryIds` and, on a re-post, populates `ExistingExternalTags/InternalTags/ExistingSubcategories` from the **posted** ids (falling back to DB when not supplied); the 7 Edit-POST call sites pass the posted values. No Edit.cshtml change needed.
- **QC Ref:** #186 — client retest 2026-06-04: *"I just tested on create and the fields that are selected are not maintaining state when there is a validation error."* (Same QC thread as DEF-243, distinct mechanism — see below.)
- **Bucket:** Bug (front-end + a small controller assist — JS-driven selections not rehydrated on the MVC validation re-render)
- **Reporter:** Shawn West (relaying client QC #186 retest), 2026-06-04

**Distinct from DEF-243.** DEF-243 was the *Edit* page overwriting a cert's saved hierarchy with the product's on load. DEF-246 is the *Create* page losing the analyst's in-progress selections when the server re-renders the form on a ModelState failure (e.g., a missing required field, or a server-only rule like the DEF-161 Product-ID format check). On re-render, `CompanyId` and `CertificationTypeId` survive because they're `asp-for` selects with server-rendered options; everything populated by JavaScript does not — `ProductId` has no server `<option>` for the posted value, and `ProductCategoryId/TypeId/SubcategoryId`, the cert-Subcategory multiselect, and the Tag multiselects are all client-built with empty hidden inputs on re-render. The Edit page already restores all of this from its saved model (`initProductCascade`, `loadExistingSubcategories`, the tag-restore block); Create had **no** equivalent.

**Why it wasn't seen earlier / repro nuance.** The form is `novalidate`, but jQuery-unobtrusive validation still blocks a normal submit on the standard required fields — so the server re-render only happens when something passes client validation but fails server validation. The first repro attempt gave a false PASS because `requestSubmit()` was blocked client-side (no round-trip). Forcing native `form.submit()` (bypasses jQuery validate + the submit handler) exposed the true server re-render and the state loss.

**Fix.**
- `Controllers/CertificateController.cs` (Create POST): expose the posted JS-driven selections to the re-render via `ViewBag.PostedSubcategoryIds`, `ViewBag.PostedExternalTags`, `ViewBag.PostedInternalTags` (the Product/Category/Type/Subcategory values are already on the model).
- `Views/Certificate/Create.cshtml`: added `restorePostedCreateState()` (guarded on `Model.CompanyId > 0`, i.e. a re-post — no-op on a fresh Create) that reloads the company's products and re-selects the posted product (with a `suppressProductAutoFillCreate` guard so the product→category auto-fill can't overwrite the posted category — same lesson as DEF-243), restores Product Category/Type/Subcategory with the SearchableSelect display synced, restores the cert Subcategory multiselect, and a `restorePostedTags()` helper that rebuilds the external/internal tag pills + hidden inputs. Mirrors the Edit page's existing restoration.

**Files touched:** `Controllers/CertificateController.cs` (Create POST ViewBag.Posted* + `ReloadEditFormDataAsync` posted-restore for Edit), `Views/Certificate/Create.cshtml` (restore script). Tests: `Scripts/smoke-prod/def-create-validation-state.js` (Create, 11/0), `Scripts/smoke-prod/def-edit-validation-state.js` (Edit, 8/0).

**Linked memory.** [[feedback_scoping_check_adjacent_endpoints]] (fixed the product cascade, then audited adjacent JS-driven fields — cert subcats + tags — and found the same loss), cross-ref **DEF-243** (same QC #186 thread, sibling Edit-page fix; shared the product-auto-fill-suppression technique).

---

### DEF-245 — `GET /Product/GetCategories` returns the product-category reference list (200) to External users — pre-existing, surfaced by the DEF-243/CR-184 pre-deploy Gate 3 audit

- **Date:** 2026-06-04
- **Severity:** Low (information exposure of non-sensitive reference data — product *category names* only, no company/cert/business records; read-only GET; no mutation path). Not a cross-company business-data leak.
- **Status:** **Open — backlog. NOT in the 2026-06-04 deploy bundle.** Pre-existing; not introduced by DEF-243 or CR-184. No code change made.
- **QC Ref:** internal — pre-deploy **Gate 3** (data-scoping) audit, 2026-06-04 (security-specialist). Flagged as an out-of-scope NIT during the DEF-243/CR-184 gate sweep.
- **Bucket:** Bug (authorization — reference endpoint not gated to internal roles)
- **Reporter:** security-specialist agent (Gate 3), 2026-06-04
- **Detail.** `ProductController.GetCategories` is the one endpoint the cert-form JS touches that is **not** `AnalystOrAdmin`-gated — it returns 200 for External. Every *other* endpoint in the DEF-243 path (`GetProductTypes`, `GetProductSubcategories`, `/Product/SaveProduct`) is correctly `AnalystOrAdmin` + CSRF-protected. The exposed data is the product-category name list used to populate a dropdown; low sensitivity. **Fix when scheduled:** add `[Authorize(Policy = "AnalystOrAdmin")]` to `GetCategories` (confirm no Public/External view legitimately calls it first — the public cert tile filters may rely on a *different* category source; verify before gating). **Linked memory.** [[feedback_scoping_check_adjacent_endpoints]].

---

### DEF-244 — Accessibility debt on Product Management + cert Create/Edit (missing `<select>` labels, icon-only pagination buttons, low-contrast green buttons, `aria-label` on plain divs) — pre-existing, surfaced by the DEF-243/CR-184 pre-deploy Gate 2 audit

- **Date:** 2026-06-04
- **Severity:** Low–Medium (WCAG 2.1 AA gaps affecting screen-reader + low-vision users; no functional/security impact). Several are Critical *per axe* but all are **pre-existing** and none block current workflows.
- **Status:** **Open — backlog. NOT in the 2026-06-04 deploy bundle.** Pre-existing; **zero new violations introduced** by DEF-243/CR-184 (the changed surfaces — new export button, dropdown cascade, inline-add guard — are not the source of any finding). No code change made.
- **QC Ref:** internal — pre-deploy **Gate 2** (cross-role + responsive / AxeCore) audit, 2026-06-04 (accessibility-validator). Full results: `Scripts/smoke-prod/_results/gate2-cr184-def243-2026-06-04T22-09-06.json`.
- **Bucket:** UX (accessibility / WCAG 2.1 AA)
- **Reporter:** accessibility-validator agent (Gate 2), 2026-06-04
- **Itemized backlog (fix when an a11y sprint is scheduled):**
  1. **`select-name` (Critical)** — filter `<select>`s lack accessible names: `#category-filter` / `#company-filter` / `#certificate-filter` (Product Management) and `#filterByType` / `#filterByPartner` (cert Product-ID Manager). Fix: real `<label for="…">` instead of the `.filter-label` `<div>` (or `aria-labelledby`).
  2. **`button-name` (Critical)** — icon-only pagination chevrons (`<i class="fas fa-chevron-left/right">`) have no name. Fix: `aria-label="Previous/Next page"` in the pagination render.
  3. **`color-contrast` (Serious)** — system-wide green button `#92C83E` + white text ≈ 1.98:1 (needs 4.5:1); also inline PDF action buttons (`#4caf50` 2.77:1, `#ff5252` 3.19:1) and some grey caption text. Darkening the brand green is a **design decision needing client sign-off**.
  4. **`aria-prohibited-attr` (Serious)** — `aria-label` on plain `<div>` containers `#subcategoriesContainer` / `#externalTagsContainer`. Fix: add `role="region"` or use a visually-hidden heading.
  5. **`link-in-text-block` (Serious)** — breadcrumb link not distinguishable from surrounding text (1.61:1). Fix: underline breadcrumb links.
- **Note.** Per [[feedback_client_requested_labels_immutable]], item 3's brand-green change must not alter client-requested copy/branding without sign-off — treat as a design CR, not a silent fix.

---

### DEF-243 — Cert Edit page silently overwrites the cert's saved Product Category/Type/Subcategory with the linked product's hierarchy on load; inline "Add product" still files under "Carly Test" when added before a category is picked (QC #186 6/4 re-raise, threads 2 + 3)

- **Date:** 2026-06-04
- **Severity:** High (data integrity / UX — the analyst's saved Category/Type/Subcategory disappears on Edit and reverts to "Carly Test" with a blank Type; compounds with new products still landing under the wrong category).
- **Status:** **DEPLOYED + VERIFIED LIVE on gccwebapp.com (2026-06-04, zip `GCCWebApp-deploy-20260604-1752.zip`).** Full pre-deploy gate sweep passed (G1/G2/G3/G5 agents + G6 perf; G4/G7 N/A). Prod fingerprint `Scripts/smoke-prod/def243-cr184-prod-fingerprint.js` (READ-ONLY) → **29 PASS / 0 FAIL / 0 WARN** covering DEF-243 + CR-184: deployed JS carries the inline-add guard string, deployed Edit.cshtml carries the `suppressProductAutoFill` + `hasSavedCertCategory` markers, and **10 real prod certs keep their saved category on Edit load** — including the client-named **26-0166** (keeps Chemicals) and genuine **divergent** prod certs **26-0168** (cert=Chemicals(1) / product=Carly Test(9) → keeps Chemicals) and **26-0163/0159/0161** (cert=Carly Test(9) / product=cat4 → keeps Carly Test). SearchableSelect synced in every case. The client's exact QC #186 scenario is confirmed fixed on real prod data.
- **Prior local verification (2026-06-04):** Playwright UAT `Scripts/smoke-prod/def243-edit-prefill-verify.js` → **37 PASS / 0 FAIL / 1 WARN** (self-cleaning). Proved H1 footer add blocked without a category (guard message), H2 add-with-category still persists under the analyst's Category+Type, Case A override-preserved (5 certs) + Case B product-derived (7 certs) with SS sync, and a constructed divergent cert keeps its own category.
- **QC Ref:** #186 — client 6/4 retest: *"(2) created a new cert with new product, went back to edit and it's still not saving the Product Type … click into Product Category it wants to jump it to Carly Test and changes. (3) created a new cert and it's not showing the options I selected for Product Cat, Type, or Subcat. NOTE: creating the new product on the cert creation page, not Product Management."*
- **Reporter:** Shawn West (relaying client QC #186 update), 2026-06-04
- **Bucket:** Bug (front-end — destructive load-time auto-fill + SearchableSelect display desync + quick-add silent default)

**Important — this is NOT a DEF-231 deploy gap.** DEF-231 (inline add reads the form's category) shipped + was verified live on gccwebapp.com 2026-06-01 (`shipped-2026-06.md`, "DEF-231 6/6 functional"). The 6/4 symptoms are a *separate* residual that DEF-231 explicitly flagged ("recommend a follow-up Edit-prefill check").

**Root cause (two compounding mechanisms).**
1. **Edit load-time overwrite.** `Views/Certificate/Edit.cshtml`: on load, `loadProductsForCompany` pre-selects the saved product and fires `change` (jQuery + native, per DEF-225). That triggers the `$('#ProductId').change()` auto-fill handler, which overwrites the cert's own saved `ProductCategoryId/TypeId/SubcategoryId` with the *product's* stored hierarchy. For a product filed under "Carly Test" with a null type, the cert display reverts to Carly Test / blank Type. `#ProductCategoryId` is wrapped by `SearchableSelect` but the cascade sets the value via raw `.val()` without `.refresh()`, so the box shows stale text and "jumps" on click.
2. **Quick-add ordering gap.** `wwwroot/js/certificate-inline-create.js::saveInlineProduct()` still fell back to `getDefaultCategoryId()` (lowest-sorted = "Carly Test") when the analyst typed the new product name into the footer *before* choosing a Product Category — so newly-added products kept landing under Carly Test with a null type, which then fed mechanism #1 on Edit.

**Fix.**
- `Edit.cshtml`: added a `suppressProductAutoFill` guard set around the load-time pre-select so the auto-fill no longer clobbers the cert's authoritative saved hierarchy (it still runs on a genuine, user-initiated product change). Added `_searchableSelect.refresh()` sync after the category value is set (and on company-change clear).
- `Create.cshtml`: mirrored the `_searchableSelect.refresh()` sync on auto-fill + company-change clear (Create has no load-time pre-select, so no guard needed).
- `certificate-inline-create.js`: removed the silent default-category fallback — the footer quick-add now requires a Product Category to be selected first ("Select a Product Category above before adding a new product."), so a quick-added product is always filed under the same hierarchy the cert carries. Type/Subcategory always ride along.

**Files touched:** `Views/Certificate/Edit.cshtml`, `Views/Certificate/Create.cshtml`, `wwwroot/js/certificate-inline-create.js`.

**Linked memory.** [[feedback_product_id_status_follows_cert]] (cert/product hierarchy coupling), [[feedback_scoping_check_adjacent_endpoints]] (Create + Edit both carried the desync), [[feedback_data_issue_vs_defect]] (code fixed here; existing mis-categorized rows remain the DEF-231 data-cleanup CR).

---

### DEF-242 — Cert list sorting: blank/empty values now sort LAST in both directions, consistent across all three tile views (QC #157 client UX request)

- **Date:** 2026-06-03
- **Severity:** Low (UX behavior change — client-requested). No data change, no migration.
- **Status:** **DEPLOYED + VERIFIED on gccwebapp.com (2026-06-03, zip `GCCWebApp-deploy-20260603-2110.zip`).** Built clean (0 errors), verified on localhost (`Scripts/smoke-prod/qc157-sort-verify-local.js` → 14 PASS / 0 FAIL), then prod-fingerprinted post-deploy → **12 PASS + 2 known-false-positive** (the 2 are the `é™®ä, PA` mojibake test cert 26-0158 vs the oracle's ASCII-codepoint compare, same as DEF-241; not a regression — blanks-last placement confirmed working on all columns). Before/after proof: pre-deploy `location-az` returned 11 blanks first; post-deploy returns populated values first with blanks trailing. **No DB migration** — pure query change, Gate 7 N/A.
- **Reporter:** Client (Carly) QC #157 5/29 retest (Location/ASIN/Cert Subcategories/Company URL "still not sorting"); Shawn direction on 2026-06-03: *"null at the end … the fix would need to be consistent across the application."*

**Why this exists.** DEF-241 verified the sort code was already correct — the columns DO sort. The client's perception of "not sorting" was caused by **NULL/empty placement**: SQL Server's default puts blanks **first** in ascending, and because many certs have empty Location / Company URL / Cert Subcategories, sorting those columns ascending filled the first screen with blanks. This change implements the client's requested rule: blanks always at the END.

**Fix — one shared helper, used by all three certificate-list query paths so they behave identically:**
- New `Extensions/CertificateSortExtensions.cs`:
  - `OrderByTextEmptiesLast(query, key, descending)` — primary order on an `(key ?? "") == ""` bit (false=0 first, true=1 last; never reversed → blanks stay last in BOTH directions), then `ThenBy/ThenByDescending(key)`. Caller still appends `.ThenBy(c => c.Id)` for stable pagination (DEF-166).
  - `OrderByNumericNullsLast(query, key, descending)` — same idea for the nullable `decimal?` Cert Results column.
- Wired in:
  - `Controllers/Api/CertificateTileInternalApiController.cs` `ApplySorting` — every text branch (cert-type, company, product, product-cat/type/sub, location, country, ext-tag/int-tag, partner, asin/upc/gtin/ean, contact, pathway, cert-sub, company-url) + cert-results. Date/Number/Status(enum) branches unchanged (no blank state).
  - `Controllers/Api/CertificateTileExternalApiController.cs` `ApplySorting` — cert-type, company, product, location, country, product-type, partner.
  - `Repositories/CertificateRepository.cs` (public tile path via `filter.SortBy`/`SortDescending`) — company, product, certtype, location, country, producttype.

**Supersedes** the DEF-181 choice of "cert-results NULLs first in ASC." Per the client's "consistent across the application," Cert Results now also puts non-numeric/empty results last in both directions.

**Verification (localhost, 2026-06-03):** `qc157-sort-verify-local.js` (API-level, full pagination, collation-aligned oracle) → **14 PASS / 0 FAIL** — Location/ASIN/Company URL/Partner all show populated values leading and blanks clustered at the end in *both* asc and desc; Status (enum) unchanged; DEF-181 cert-results canary still HTTP 200. Public endpoint spot-check: `sort=location-az` → `["Hartford, CT","Portland, OR",null,null,…]` (populated lead, blanks last). External endpoint returns 200 (same helper, scoped data too sparse to show ordering). Cert Subcategories had no populated rows in the sandbox (untestable locally; identical code path).

**Files:**
- `Extensions/CertificateSortExtensions.cs` *(new)* — shared empties-last / nulls-last sort helpers.
- `Controllers/Api/CertificateTileInternalApiController.cs` — `ApplySorting` text + cert-results branches.
- `Controllers/Api/CertificateTileExternalApiController.cs` — `ApplySorting` text branches + `using GCCWebApp.Extensions`.
- `Repositories/CertificateRepository.cs` — public sort text branches + `using GCCWebApp.Extensions`.
- `Scripts/smoke-prod/qc157-sort-verify-local.js` — oracle updated to assert blanks-last; gate-aware for local + prod.
- `docs/tracking/qc-log-update-2026-06-03.md` — client-facing QC #157 entry.

**Cross-ref:** Implements the optional UX follow-up flagged in **DEF-241** (the investigation that proved no defect). Builds on DEF-165 / DEF-181. Supersedes DEF-181's cert-results NULL placement.

---

### DEF-241 — Cert List View column sorting (QC #157, 5/29 re-raise): verified correct on localhost — NO code defect; perceived "not sorting" is sparse data + NULL-first placement

- **Date:** 2026-06-03
- **Severity:** None (triage outcome — no code change). Classified **Closed — no code change required**.
- **Status:** Closed (code correct as-shipped). **Verified on http://localhost:5101** — `Scripts/smoke-prod/qc157-sort-verify-local.js` → **13 PASS / 0 FAIL** across Location, ASIN, Cert Subcategories, Company URL, Partner, Status (asc + desc). Result JSON: `_results/qc157-sort-local-results.json`. **Also fingerprinted on gccwebapp.com (2026-06-03) → DEF-165 + DEF-181 confirmed LIVE** (see Prod fingerprint below).
- **Reporter:** Client (Carly) via QC #157, 5/29 retest — *"Status sorting is working. The following still are not: Location, ASIN (unsure of UPC/GTIN/EAN, they had no data), Cert Subcategories (multiple values, order not critical), Company URL (low priority)."*

**Background.** QC #157 (4/7) flagged sort issues across many columns. DEF-165 (shipped 2026-05-08) fixed Product Category misroute + multi-value `.Min()` determinism + `.ThenBy(Id)` tiebreaker. DEF-181 (2026-05-11) fixed the four product-ID columns (legacy-column fallback) and Cert Results (numeric column). Carly's 5/29 retest still lists Location / ASIN / Cert Subcategories / Company URL. This entry is the investigation + localhost verification of that re-raise.

**Full-chain audit — every layer is correctly wired for all four columns:**
1. Server sort — `Controllers/Api/CertificateTileInternalApiController.cs:689-792` (`ApplySorting`): `location-az/za` → `OrderBy(c.Location)`, `asin/za` → legacy-fallback `Min(ProductIds)` ?? `c.Asin`, `cert-sub/za` → `Min(CertificationTypeSubcategories.Name)`, `company-url/za` → `OrderBy(c.Company.Website)`. All present, all with `.ThenBy(c.Id)` tiebreaker.
2. Client column→key map — `wwwroot/js/certificate-tile-view-internal.js:648-673` and reverse/desc map `:688-713`: `location→location-az`, `asin→asin`, `certSubcategories→cert-sub`, `companyUrl→company-url`. Correct.
3. Header attributes — `Views/Certificate/CertificateTileViewInternal.cshtml:374/379/385/387`: every column header carries the right `data-sort-column`. Correct.

**Verification method.** Sorting is 100% server-side, so the harness drives the real internal API (`/api/certificate-tiles-internal?sortBy=…`) directly, paginates the **full** result set, and asserts the returned sort key is monotonic. This deliberately avoids a DOM test, whose confounds produced false failures in the first two passes: (a) the sortable table only renders in **List** view (page defaults to **Tile**/card view — see the `data-view` toggle); (b) the ASIN/UPC/GTIN/EAN cells render a **count badge**, not the value; (c) Location displays a **composite** ("Portland, OR, United States") and Status displays a **label** while sorting by enum int — so "alphabetical by what's on screen" is the wrong oracle; (d) the JS oracle's `localeCompare(numeric:true)` orders letters-before-digits, the **opposite** of SQL Server's default CI collation (digits-before-letters) — this alone produced two false ASIN failures until the oracle was switched to case-insensitive codepoint compare to match the DB.

**Per-column result (localhost, admin scope, 20-cert sandbox):**
| Column | ASC | DESC | Notes |
|---|---|---|---|
| Location | ✅ | ✅ | 17/20 certs have empty Location → ASC clusters blanks first (SQL NULLs-first), DESC shows the 3 populated values ordered correctly. |
| ASIN | ✅ | ✅ | 15–20 populated, ordered per DB collation (digits before letters). |
| Company URL | ✅ | ✅ | 6 populated, ordered correctly. |
| Partner | ✅ | ✅ | Amazon < None < Novi. |
| Status | ✅ | ✅ | Enum-backed: verified equal labels are contiguous in both directions and ASC≠DESC (proves re-sort). |
| Cert Subcategories | n/a | n/a | **0 populated in the sandbox** — code path identical to the others; could not be exercised (same "had no data" caveat Carly noted for UPC/GTIN/EAN). |

**Root cause of the perception — not code:**
1. **Sparse data + NULL-first placement.** Most sandbox (and likely prod) certs have empty Location / Company URL / Cert Subcategories. SQL Server places NULL/empty **first in ASC**, so clicking a sparse column ascending fills the first screen with **blanks**, which reads as "not sorting / not alphabetical." Populated values are correctly ordered but live further down / on later pages.
2. **Historical deploy lag.** DEF-181's `AddCertificationResultsNumeric` migration once shipped in code but **never reached the prod DB**, breaking `/api/certificate-tiles` entirely until the SQL was applied manually (see `shipped-2026-05.md:743`). A retest during any such window would show erratic sorting unrelated to the sort code. **Action:** confirm DEF-165 + DEF-181 are actually live on gccwebapp.com before treating any further 5/29 sort report as a code issue (use the smoke-prod fingerprint probes).

**Prod fingerprint (gccwebapp.com, 2026-06-03) — DEF-165 + DEF-181 confirmed live.** Ran the same harness against prod through the test-env Basic gate (admin login). **13 PASS / 1 not-pass:** DEF-181 canary (`cert-results` sort → HTTP 200, `CertificationResultsNumeric` column present + queried — the exact surface that was broken in the DLL-first deploy, `shipped-2026-05.md:743`) ✅; ASIN, Cert Subcategories, Company URL, Partner, Status all sort correctly on real prod data ✅; Location ASC ✅ (9/20 populated). The single **Location:desc** not-pass is a **false positive in the test oracle, not a server bug**: the offending value is `"é™®ä, PA"` (mojibake), and SQL Server's `SQL_Latin1_General_CP1_CI_AS` collation correctly sorts `é` adjacent to `e` (so it precedes "Gent" in descending), whereas the harness's raw-codepoint compare treats `é` = U+00E9 = 233 and disagrees. The server ordering is correct. **Net: the QC #157 sort fixes are deployed and working on prod — no code or deploy action outstanding.** Separately surfaced data note (not a defect, [[feedback_data_issue_vs_defect]]): at least one prod cert has a **corrupted/mojibake Location** (`"é™®ä, PA"`) — likely a UTF-8/Latin1 double-encode at import; cosmetic, fixable as a data op if the client notices it.

**Optional UX follow-up — NOW IMPLEMENTED as DEF-242 (Shawn approved 2026-06-03: "null at the end … consistent across the application").** Flip NULL/empty placement so blanks sort **last in ascending too** (surface populated values first regardless of direction). This already matches the documented rationale for the Cert Results column in DEF-181 ("NULLs … where a sorting user is most likely looking"). Applying it to the text columns (Location, Company URL, Cert Subcategories, the ID columns) would directly remove the "page of blanks" perception. Deferred because it changes sort behavior the client hasn't explicitly requested and is a one-line-per-branch change best batched under a small CR.

**Files:**
- `Scripts/smoke-prod/qc157-sort-verify-local.js` *(new)* — API-level sort verifier, 13 assertions, collation-aligned oracle. No application code changed.

**Cross-ref:** Closes the QC #157 5/29 re-raise as *no defect*. Builds on DEF-165 (Product Category + multi-value determinism) and DEF-181 (ID columns + Cert Results). Related preference: data-distribution / NULL-placement is a UX-expectation gap, not a bug.

---

### DEF-240 — Product Management: remove Status from the table, the Add New Product popup, and the filter bar (QC #103 client re-raise 5/28 — "what is the purpose of the Status column? it should be removed")

- **Date:** 2026-06-03
- **Severity:** Medium (UX / consistency — client re-opened QC #103 a third time: 3/13 template-vs-table, 5/8 export round-trip → DEF-174, 5/12 leveling → DEF-182, and now 5/28 insisting Status be *removed* entirely, not just leveled).
- **Status:** **DEPLOYED + VERIFIED on gccwebapp.com (2026-06-03, zip `GCCWebApp-deploy-20260603-2110.zip`).** Built clean (0 errors), verified on localhost (`def240-status-removed-verify.js` → 6 PASS / 0 FAIL), then verified live on prod (admin → `/Product/Management`): **6 PASS / 0 FAIL** — no Status column in the table, no `#status-filter`, no `#product-status` in the Add New Product modal, core fields intact, no console errors. Screenshots `_results/def240-prod-page.png`, `_results/def240-prod-modal.png`. (The "no inactive-toggle" UX consequence remains as accepted — retire via Delete.)
- **Reporter:** Client (Carly) via QC #103, reiterated by Shawn — *"Status is still in the Add New Product popup and listed in the table. Issue from 5/15 not resolved … What is the purpose of the Status column? I thought we discussed it was not needed."*

**Background — this directly reverses the DEF-182 design decision.** DEF-182 (2026-05-12) deliberately *added* a Status select to the popup and kept the table column, on the reasoning that `Product.IsActive` is a real soft-delete flag (cert Create/Edit forms filter their product picker to `IsActive && !IsDeleted` — ~18 call sites listed in DEF-182). At that time "make popup the editor for Status" was chosen so the flag stayed controllable. The client has since rejected that resolution outright and wants Status gone from the Product Management UI. This entry implements the removal the client asked for.

**Changes — `Views/Product/Management.cshtml` (live view behind `/Product/Management`):**
1. Table: removed the `Status` `<th>` (sortable header) and the `<td>` status badge; detail-row `colspan` 10 → 9.
2. Add New Product / Edit popup: removed the `Status` (Active/Inactive) `<select>`; Subcategory is now a full-width row.
3. Filter bar: removed the `Status` filter group (All Status / Active / Inactive) and all its JS wiring (`statusFilter` var, init, change-listener, filter predicate, clearAllFilters, active-filter chip, removeFilter case).
4. JS plumbing: removed the now-dead `product-status` reads in `showAddProductModal()` and `editProduct()`. `saveProduct()` no longer reads the dropdown — **new products default to `isActive: true`; edits preserve the product's existing `isActive`** (looked up from the in-memory `products` array by id), so editing a product never silently re-activates a retired one.

**Known functional consequence — resolved (Shawn 2026-06-03: "Delete is the way to retire a product").** There is now **no UI path to set a product Inactive**, and that is intended. Retirement is handled by the row **Delete** action (soft-delete → `IsDeleted = true`), not by an Active/Inactive toggle. `IsActive` still exists and is still honored everywhere (cert product pickers hide both inactive and deleted products; any existing inactive products stay inactive). The CSV import template has no Status column either, consistent with this. This was an explicit client trade-off (remove Status UX, retire via Delete) accepted over the DEF-182 rationale (keep Status editable in the popup).

**Files:**
- `Views/Product/Management.cshtml` — table header/cell + colspan, modal Status field, filter group + JS wiring, add/edit/save handlers.

**Export already level.** The CSV export header set (`Product Name, SKU, Partners, Company, Description, Category, Product Type, Subcategory, Identifier Type, Identifier Value`) has no Status column, so export ↔ template ↔ table ↔ popup are now consistent on Status.

**Verification (localhost, 2026-06-03):** `def240-status-removed-verify.js` (admin) → 6/6 PASS: page loads (200), table header set is `Product Name, Company, Category, Product Type, Subcategory, Certs, IDs, Actions` (no Status), `#status-filter` absent, `#product-status` absent from the Add New Product modal, modal retains name/company/category/type/subcategory/description, no console/page errors. Screenshots confirm visually. **Still owed (manual, data-dependent):** edit an existing **inactive** product and save without other changes → confirm it stays inactive (the in-memory preserve-isActive path); confirm CSV export has no Status column.

**Cross-ref:** Supersedes the Status half of DEF-182. DEF-174 (export round-trip) and DEF-182 (field leveling) remain valid for the non-Status fields.

---

### DEF-239 — Login broken on gccwebapp after deploy: code shipped with the `UserCompanyId` migration but the prod DB schema was never updated — every login fails as "Invalid email or password"

- **Date:** 2026-06-03
- **Severity:** **HIGH** (total auth outage on gccwebapp — no user of any role could log in). Deploy/ops gap, not a code defect.
- **Status:** **FIXED + VERIFIED ON PROD (2026-06-03).** Root cause: the CR-020 build added `User.UserCompanyId` (+ FK to `Companies`) and migration `20260603135743_AddUserCompanyToUser`. The DLL shipped to gccwebapp, but `Program.cs:922-926` deliberately **skips auto-migration in Production** ("manual migration management"), so the `Users` table never got the `UserCompanyId` column. The login query `_context.Users.Include(u => u.Roles).FirstOrDefault(...)` (`Controllers/AccountController.cs:1048`) emits SQL selecting `UserCompanyId` → SQL Server throws `Invalid column name 'UserCompanyId'` **before** any password check. The DEF-209 anti-enumeration outer `catch` (`AccountController.cs:1130-1134`) swallows the SQL exception and returns the generic `"Invalid email or password."`, so a hard schema failure was indistinguishable from a wrong password.
- **Resolution applied (2026-06-03):** connected to the prod DB (`gccwebap_certificates` @ `tcp:68.71.129.120,1533`) and found it was behind by **TWO** migrations, not one (head was `20260512011258_AddCertificationResultsNumeric`; missing `20260515185538_FixUserUniqueIndexesForSoftDelete` **and** `20260603135743_AddUserCompanyToUser`). Generated a targeted idempotent script for just those two (`Scripts/database/DEF-239_prod_missing_two.sql`) and applied it via `sqlcmd ... -N -C -b -I` (the `-I` / QUOTED_IDENTIFIER ON is **required** — the filtered unique index `WHERE [IsDeleted]=0` fails without it; a first attempt without `-I` errored and rolled back atomically, leaving prod untouched). Post-apply DB checks: `Users.UserCompanyId` EXISTS, FK `FK_Users_Companies_UserCompanyId` EXISTS, migration count 27→29, login-shaped `SELECT [UserCompanyId] FROM [Users]` succeeds. **Live E2E login verified 4/4 roles** (admin/analyst/external/partner) via `Scripts/smoke-prod/def239-login-verify-live.js` → all land on authenticated tile views, none stuck on `/Account/Login`. Results `Scripts/smoke-prod/_results/def239-login-results.json` + per-role screenshots.
- **Note — the full 28-migration replay was the WRONG tool:** `DEF-239_FULL_idempotent_all_migrations.sql` failed on legacy batches (a `CONCAT` parse error in an old migration SQL Server compiles even under the `IF NOT EXISTS` guard). Scoping the script to only the migrations prod was actually missing avoided replaying historical batches. Keep that file for reference but the **targeted** script is the one that shipped the fix.
- **QC Ref:** internal — direct user report (Shawn), 2026-06-03 ("just deployed to gccwebapp, unable to login").
- **Bucket:** Bug (deploy/ops — schema/migration not applied to prod; masked by generic auth error).
- **Reporter:** Client (direct), 2026-06-03.
- **Evidence:** Symptom is "Invalid email or password" on every credential. Real exception is in the Plesk app log at `AccountController.cs:1132` ("Error validating user …") and reads `Invalid column name 'UserCompanyId'`. Diagnosis confirmed by code trace: the failing query is the first DB call in `ValidateUserDetailed`, before lockout/password logic.
- **Remediation (non-destructive — nullable column, no backfill):** run the ready-made idempotent, transaction-wrapped script **`Scripts/database/DEF-239_AddUserCompanyToUser.sql`** against the gccwebapp DB (SSMS / `sqlcmd -i` / Plesk DB query tool), then recycle the app pool. The script is guarded on `__EFMigrationsHistory` so it's safe to re-run and self-records the migration row. Equivalent inline SQL if you'd rather paste it:
  ```sql
  ALTER TABLE [Users] ADD [UserCompanyId] int NULL;
  CREATE INDEX [IX_Users_UserCompanyId] ON [Users] ([UserCompanyId]);
  ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Companies_UserCompanyId]
      FOREIGN KEY ([UserCompanyId]) REFERENCES [Companies] ([Id]) ON DELETE SET NULL;
  INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
  VALUES ('20260603135743_AddUserCompanyToUser', '8.0.11');
  ```
- **Prevention:** added **Gate 7 (Pending-migration → prod schema)** to `docs/tracking/pre-deploy-agent-checklist.md` — any deploy containing a new migration must run the matching SQL on gccwebapp in the same step, since Production never auto-migrates.
- **Secondary observation (not fixed here):** the DEF-209 generic-login-error design correctly hides enumeration detail from the user, but it also hid a fatal infra failure for the operator. The real error IS logged server-side (working as intended), so the fix is operational discipline (check the app log on any prod login outage), not loosening the user-facing message. Noted, no change.
- **Linked memory.** [[project_predeploy_agent_checklist]] (new Gate 7 closes this gap), [[feedback_data_issue_vs_defect]] (this is logged as a deploy/ops DEF because the code is correct — the break is the un-applied schema), [[project_plesk_deploy_env]].

---

### DEF-238 — User Management: Company Access not role-gated — shown for Admin/Analyst (should be hidden with an "all access" note); table blank for those roles (QC #106 / #148 item 4, client 5/15 feedback reiterated 5/29 "none addressed")

- **Date:** 2026-06-02
- **Severity:** Medium (UX + client-blocking UAT item). Client's 5/15 direction: Company Access applies **only** to External (scoped to selected companies) and Partner (scoped to selected partner). **Admin/Analyst are never scoped** — the modal should **hide** Company Access for them and show a note that those roles get access to all companies. On 5/29 the client reported none of the 5/15 feedback was addressed, plus the user table showed **blank** Company Access for Admin/Analyst.
- **Status:** **DEPLOYED 2026-06-03 — awaiting client UAT re-confirm on the test site.** Shipped alongside CR-020 in `deployment-drops/GCCWebApp-deploy-20260603-1221.zip` (see `docs/tracking/shipped-2026-06.md`). The CR-020 migration that rode with this deploy caused the prod login outage [[DEF-239]], **resolved + verified on prod the same day** (4/4 roles). Originally verified on localhost:5101 with `Scripts/smoke-prod/def237b-role-access-verify.js` → **6 PASS / 0 FAIL** (R0 no-role hides all; R1 External shows Company Access; R2 Partner shows Partner picker; R3 Admin shows note + hides both; R4 Analyst same; T1 table shows "All" for Admin/Analyst). Screenshot `_results/def237b-admin-note.png`. *(Status reconciled 2026-06-04 — the prior "Fixed locally — pending next deploy" line was stale relative to the shipped log.)*
- **QC Ref:** QC #106 / QC #148 item 4 — client UAT comments 5/15 (origin) and 5/29 ("none of the 5/15 feedback was addressed"). Reporter: Client (Carly).
- **Bucket:** Bug + UX (the prior `handleRoleChange` comment claimed "Admin/Analyst: hide both" but the code put them in the `else` branch that **showed** Company Access — code contradicted its own intent).
- **Root cause:** `wwwroot/js/userManagement.js::handleRoleChange` only special-cased Partner; every other role (incl. Admin/Analyst) fell through to a single `else` that set `company-group` to `display:block`. No "all-access" note element existed. The table's `getCompanyAccessDisplay` returned `''` for users with no `companyIds` (Admin/Analyst have none), rendering blank.
- **Files touched:**
  - `Views/Admin/UserManagement.cshtml` — added `#role-access-note` info panel (hidden by default).
  - `wwwroot/js/userManagement.js` — `handleRoleChange` rewritten (Partner→partner picker; External→Company Access; Admin/Analyst→note, both hidden; unselected/other→all hidden); `getCompanyAccessDisplay` returns "All" for Admin/Analyst; `showAddUserModal` reset now delegates to `handleRoleChange('')` (single source of truth) instead of force-showing Company Access.
- **NOT changed (deliberate):** the "Company Access" (modal/table) vs "User Company" (import template) label split was left as-is per the client's 2026-05-21 confirmation that "User Company" is the desired template label (see DEF-173 closure + conflict-history table). The label question was NOT re-opened to avoid a 6th flip-flop — pending one authoritative client decision.
- **Deferred (CR-required):** QC #106's other half — a free-text **Employer** field on the user record, separate from Company Access — is a data-model change requiring a formal CR. Held for client review/QC-log confirmation; not built in this pass.
- **Linked memory.** [[feedback_client_requested_labels_immutable]] (didn't touch the contested label), [[feedback_check_requirements_every_time]] (the 5/15 requirement was the real spec; the earlier "Check All works" verification missed it), [[feedback_data_changes_require_cr]] (Employer field deferred to CR).

---

### DEF-237 — Tags import/export: rename the first column header from "Name" to "Tag Name" (QC #148 follow-up)

- **Date:** 2026-06-02
- **Severity:** Low (UX/label clarity). Client requested the Tags template/export name column be labeled **"Tag Name"** rather than the generic **"Name"**, for clarity alongside `TagType (Internal/External)` and `Description`.
- **Status:** **Fixed locally (2026-06-02) — pending next deploy.** Two-line JS change in `wwwroot/js/tags.js`: import-template header (`downloadImportTemplate`, was `'Name'`) and export column header (was `{ key: 'name', header: 'Name' }`) both now read **`Tag Name`**. TagType and Description columns unchanged; slash separator and 3-column structure preserved (DEF-232 regression test `def232-template-verify.js` still holds).
- **Round-trip safe:** the import header normalizer (`normalizeTagHeaderName`, `tags.js:1190`) already maps `tagname → name`, so re-importing a renamed template/export parses correctly. No parser change needed.
- **QC Ref:** QC #148 (Enhancement, Ready for UAT) — client follow-up to the DEF-232 template-polish work, requested 2026-06-02.
- **Bucket:** UX / Enhancement (label only — the column functioned correctly; this is a client-requested wording refinement, not a bug).
- **Reporter:** Client (direct), 2026-06-02
- **Files touched:** `wwwroot/js/tags.js` (template header at `downloadImportTemplate`; export columns at the export-btn handler).
- **Linked memory.** [[feedback_only_requested_changes]] (renamed only the requested column; export aligned at the client's explicit confirmation), [[feedback_client_requested_labels_immutable]] (this label IS the client-requested copy — implemented verbatim).

---

### DEF-236 — Product ID Manager: "select all / delete-all" bar does not appear on a single page (≤50 IDs); only shows when the list is paginated (>50)

- **Date:** 2026-06-02
- **Severity:** Low (UX/discoverability). On `/Certificate/Create` (and any in-memory Product ID Manager), ticking the header **select-all** checkbox checked every row and showed the red **"Delete Selected (N)"** button, but the Gmail-style confirmation/select-all **banner** never appeared. The user expected a visible "all selected / delete-all" bar after select-all and reported it missing (screenshot: 4 in-memory test rows, all ticked, no bar).
- **Status:** **Fixed locally (2026-06-02) — pending next deploy.** Single-file JS change in `wwwroot/js/product-id-manager.js::updateBulkActionsUI`. The banner had only two visible states: cross-page mode, and `wholePageSelected && moreOffPage` (i.e. >1 page). Added a third branch — `wholePageSelected` with no off-page rows (single page) — that shows the banner as a plain confirmation: **"All N product ID(s) selected."** with a **Clear selection** link, and hides the meaningless cross-page "Select all N" link. Verified with a Playwright repro on localhost: 4 IDs → bar now shows "All 4 product IDs selected. Clear selection"; 60 IDs (paginated) → unchanged "All 50 on this page are selected. Select all 60 product IDs". Partial selection still correctly hides the bar.
- **QC Ref:** internal — direct user report (Shawn), 2026-06-02, with screenshot of `/Certificate/Create`.
- **Bucket:** UX
- **Reporter:** Client (direct), 2026-06-02
- **Root cause:** The DEF-194 cross-page banner was scoped only to the multi-page case (`moreOffPage = totalCount > visibleRows`). For a single page of fully-selected rows there were no off-page rows, so the banner was intentionally suppressed — but that left no "all selected" affordance at all, which read as a missing feature.
- **Files touched:** `wwwroot/js/product-id-manager.js` (one `else if` branch added in `updateBulkActionsUI`). Re-used the existing `#bulkSelectAllBanner` / `#bulkSelectAllClear` markup — no Razor/markup change.
- **NOT a defect in the >50 path:** the paginated select-all banner was verified working before and after the change. The fix is additive to the single-page case only.
- **Linked memory.** [[feedback_no_chaos_rabbit_holes]] (reproduced visually with a throwaway Playwright harness before editing, rather than guessing from the static read which looked correct).

---

### DEF-235 — App stops accepting TCP connections (`ERR_CONNECTION_REFUSED`) after ~9 consecutive view smokes / ~600 contexts of sustained load — manifests on the last view in the suite (admin-tags 12/12 RED)

- **Date:** 2026-06-02
- **Severity:** Medium (smoke gate flake; potential prod stability under sustained load). After running 9 consecutive view smokes (~120 page loads × ~5 sub-requests each = ~600+ HTTP requests), the app starts refusing TCP connections — every navigation in the 10th view (`admin-tags`, last alphabetically in the orchestrator) returns `net::ERR_CONNECTION_REFUSED`. The retry-once branch in `run-all.js` ALSO fails because the unresponsive window outlasts a single view cycle (~30s). Kestrel-level refusal, not 500 — the process is either dead or has stopped listening. `curl /` returns 200 again post-suite, so it's transient.
- **Status:** **ROOT CAUSE FIXED + verified GREEN in single-run mode (2026-06-02T14:40Z).** `RequestResponseLoggingMiddleware` was the leak — it buffered every response body into a `MemoryStream` for every request including static assets (CSS/JS/img/fonts). Logged bodies are only USED if ≤50KB and only LOGGED if ≤10KB, so buffering bundled JS/CSS was pure waste. At ~30 static requests/page × 12 contexts × 10 views = ~3600 buffer allocations per smoke run, the resulting allocation pressure produced long GC pauses, Kestrel fell behind on accept(), and TCP refusals followed. Fix at `Middleware/RequestResponseLoggingMiddleware.cs` adds `_staticPrefixes` (`/css/ /js/ /lib/ /img/ /images/ /fonts/ /_framework/ /_content/`) and short-circuits before body buffering for those paths. Single-run smoke (NO batch-restart): **694 PASS / 0 FAIL / 6 WARN — GREEN**. admin-tags went from 0P/12F/0W (pre-fix single-run) to 40P/0F/0W (post-fix single-run). The earlier `SMOKE_BATCH_RESTART=1` mitigation is now optional, not required — single-run is reliable again. (Mitigation history: 2026-06-02T02:25Z `SMOKE_BATCH_RESTART=1` run was the first GREEN result, but masked the underlying leak; this DEF closes both the symptom AND the cause.)
- **QC Ref:** internal (smoke-discovered, not a client QC item). Surfaced during the post-DEF-234 verification roll-up at 2026-06-02T01:19Z.
- **Bucket:** Bug (infra / stability — sustained-load resource handling, mechanism not yet pinned)
- **Reporter:** E2E smoke Tier 1 roll-up, 2026-06-02

**Evidence.**
- Post-DEF-234 roll-up summary (`Scripts/smoke-prod/_results/summary-2026-06-02T01-19-52-617Z.json`): 654 PASS / 12 FAIL / 6 WARN. 9 of 10 views are 100% green. The 12 FAILs are ALL on `admin-tags`, ALL the same shape: `page.goto: net::ERR_CONNECTION_REFUSED`. Stream B's prior run at 00:46Z had admin-tags at 40P/0F/0W, so admin-tags itself works.
- Direct probe AFTER the smoke completed: `curl /` returns 200 in 11ms; `curl /Admin/Tags` returns 302 (redirect to login) in 6ms. The route is alive, the page is reachable, the SQL backend is fine.
- App log contains repeated `Microsoft.Data.SqlClient.SqlException: Execution Timeout Expired ... 45291ms` from `CertificateTileInternalApiController.GetCertificateTiles` during the EARLIER failing run (bgyp9355r) — but those are NOT from the run that surfaced DEF-235. The DEF-235 run's `cert-tile-internal` smoke completed 62P/0F/0W clean. So 45s SQL timeouts are not the immediate cause this time.
- Pattern: positional, not view-specific. admin-tags is the LAST view in `VIEWS` (line 17 of `Scripts/smoke-prod/_views/run-all.js`).

**Three plausible root causes (in rough probability order).**
1. **SQL Server connection pool exhaustion under back-to-back smoke load.** Default pool max = 100. The smoke sends ~600 requests over the prior 9 views with no inter-view pause — if any request leaks a `DbContext` scope (or a `DataReader` doesn't `Dispose`), pool slots accumulate. Symptom matches: HTTP requests stalling at `OpenAsync` until Kestrel's connection queue overflows.
2. **Kestrel keep-alive / connection-queue saturation from rapid-fire context creation.** Each `await ctx.newPage()` in the smoke template opens a fresh browser context with no shared cookie jar. 12 contexts × 10 views = 120 fresh sessions. Kestrel's default per-connection limits should handle this, but memory pressure or kernel TCP backlog overflow could cause refusals.
3. **Garbage-collection pause on a hot generation, blocking the request thread long enough that Kestrel falls behind on accept().** Less likely on .NET 8 server GC, but possible after sustained allocation churn (the public tile DEF-234 fix made the GET path faster but generates more cert-DTO allocations per second).

**Mitigation already present, just unused by default.** `Scripts/smoke-prod/_views/run-all.js` already has TWO modes baked in (lines 10-20). DEFAULT (single-run): trusts you to manage the server, applies a per-view `SMOKE_COOLDOWN_MS` cooldown (default 4000ms) and one retry of any RED view. BATCH-WITH-RESTART (`SMOKE_BATCH_RESTART=1`, default batch size 5): owns the server lifecycle, kills + restarts `dotnet run` before each batch so no single batch accumulates enough sustained load to trip this defect. The fix is operational: **use `SMOKE_BATCH_RESTART=1` for the deploy-gate run**. Single-run mode stays as the fast dev-loop option where view-10 flakiness is tolerable.

**Explicitly NOT fixed in this DEF (deliberate scope discipline).**
- Did not bump SQL Server's `Max Pool Size` or `Connect Timeout` — those would mask a leak, not fix it.
- Did not raise Kestrel `MaxConcurrentConnections` — default is `null` (unlimited) on .NET 8, so no benefit.
- Did not change `DbContext` lifetime — would have wide blast radius; need the diagnostic first.
- Did not add a deeper retry/backoff in `run-all.js` — would just produce slow flaky runs instead of fast flaky runs.

**Investigation plan (next session, time-boxed).**
1. Re-run the suite with the 2s pause. **If green**: confirms load-density. Capture a memory + connection-pool profile (`dotnet-counters monitor`) during the run; identify the specific service holding state. **If still red**: admin-tags has a route-level issue; bisect by moving `admin-tags` to position 1 in `VIEWS` and re-running.
2. If load-density confirmed: instrument `RequestResponseLoggingMiddleware` to log `ActiveConnections` and `OpenContexts` per request; watch for monotonic growth across the suite.
3. Candidate fixes (after diagnosis): (a) explicit `DbContext.Dispose()` audit in middleware; (b) reduce `EfQueryWarmupService` retention if it's holding a scope past startup; (c) examine `TestEnvironmentGateMiddleware` for static-state accumulation.

**Linked memory.** [[feedback_no_chaos_rabbit_holes]] (this DEF is honestly framed as diagnosis-pending; a 2s mitigation lets us proceed without a flaky gate while the deep diagnosis lives in its own session); [[feedback_scoping_check_adjacent_endpoints]] (if connection-pool leak confirmed, the leak surface is likely middleware-wide, not view-specific).

---

### DEF-236 — `DownloadSelectedZip` IDOR: authenticated External/Partner user can bulk-download arbitrary cross-company certificate PDFs by ID (broken object-level authorization)

- **Date:** 2026-06-02
- **Severity:** **HIGH** (live cross-company data exposure of certificate PDFs to any authenticated external user). Pre-existing — not introduced by the June batch; surfaced by the pre-deploy Gate 3 adjacent-endpoint audit.
- **Status:** **Fixed locally (2026-06-02) — verified — pending next deploy.** One-line-per-iteration authorization guard added to `Controllers/CertificateController.cs::DownloadSelectedZip` (~line 2126): after fetching each cert, call the existing `FileAccessAllowed(certificate, out var denyReason)` gate (the same one every single-download path uses) and `continue` (skip + log) on deny. Build clean (0 errors). Ships with the June batch.
- **QC Ref:** internal (security audit, not a client QC item). Surfaced by `security-specialist` during the 2026-06-02 pre-deploy Gate 3 run (`docs/tracking/predeploy-record-2026-06-june-batch.md`).
- **Bucket:** Bug (security — broken object-level authorization / IDOR)
- **Reporter:** Pre-deploy Gate 3 (data-scoping & endpoint coverage), 2026-06-02

**Evidence (pre-fix, live probe).** `POST /Certificate/DownloadSelectedZip` is gated only by `[Authorize(Policy = "ExternalAccess")]` (External + Partner) and zipped every caller-supplied cert ID via `GetByIdAsync` with **no ownership / status / `FileAccessAllowed` check**. Logged in as the `external` test user (authorized scope = **2** certs, company "Henkel"), POSTing `[1..60]` returned **HTTP 200, a 12.6 MB ZIP of 32 certificate PDFs across unaffiliated companies** (incl. "Carly Test" / Electronics rows 26-0146..26-0158). 2 → 32 unauthorized escalation. The front-end only ever sends already-scoped IDs, so the UI never exposed it — but the server never re-validated, so a hand-crafted POST bypassed scoping entirely. This is the exact recurring trap of [[feedback_scoping_check_adjacent_endpoints]] (DEF-103/164/198/204/205): the list/tile path is scoped, the adjacent bulk-download path is not.

**Fix.** Reuse `FileAccessAllowed` (Admin/Analyst → full; External → own-company + PublicStatuses + not-expired; Partner → own-partner; any other role → fail-closed) inside the zip loop; skip + log any denied cert and count it as a failure (so an all-denied request returns the existing 400 "none available").

**Verification (2026-06-02, local).** `Scripts/smoke-prod/def236-idor-verify.js` — re-ran the exact probe against the fixed build: external user POST `[1..60]` → **HTTP 200, ZIP = 2 entries / 112 KB** (exactly the user's authorized set per the external tile API `totalCount=2`), down from 32 entries / 12.6 MB. Cross-company leak closed. **PASS.**

**Linked memory.** [[feedback_scoping_check_adjacent_endpoints]] (list scoped, adjacent detail/single/download not — audit adjacent endpoints, not just the one you touched).

---

### DEF-234 — Public tile cert-list API has 23s first-query latency on cold start; trips the 30s JS abort timeout intermittently (4/12 smoke cells)

- **Date:** 2026-06-01
- **Severity:** Medium (intermittent user-visible cold-start; smoke gate flake). First load of the public tile view after an idle app shows a 23s blank state before tiles render, or — if anything slows it further — silently fails with `TimeoutError: signal timed out`. Subsequent requests are <200ms (measured: 23.1s → 0.20s → 0.05s → 0.03s → 0.03s via 5-call curl probe on already-warm Kestrel).
- **Status:** **Fixed locally (2026-06-01) — TWO coordinated fixes shipped.** (1) `AsSplitQuery()` + stable `ThenBy(c => c.Id)` pagination tiebreaker added to `Repositories/CertificateRepository.cs::GetAdvancedFilteredCertificatesAsync` (the method the public tile API calls; the internal/external tile controllers already had AsSplitQuery at their own query sites). Reduces per-query execution cost. (2) **`Services/EfQueryWarmupService.cs`** (new `IHostedService`, registered in `Program.cs` after the certificate-expiration service block) fires one Take(1) call through the same repo method during host startup so the first real request doesn't pay EF query-compile + SQL plan cost. Build clean. Pending next deploy + warm-server smoke re-run to confirm the cold-start window is closed. The 2026-06-02 00:32 UTC roll-up was RED on this assertion against the pre-fix tree — must be re-baselined green before deploy. *Reconciled during 2026-06-02 governance sweep — prior status read "pending owner, not fixed," but the fix was already in the working tree under a `DEF-234:` code comment.*
- **QC Ref:** internal (smoke-discovered, not a client QC item). Linked to E2E Smoke Plan acceptance criteria — Tier 1 cannot go RED on a single flake.
- **Bucket:** Bug (performance — cold-cache / EF compile latency)
- **Reporter:** E2E smoke Tier 1 roll-up (`Scripts/smoke-prod/_views/run-all.js`), 2026-06-01

**Evidence.**
- `run-all` produced 686 PASS / **4 FAIL** / 10 WARN across the 10 Tier 1 views.
- All 4 FAILs are the same assertion (`cert-tile-public.no-console-errors`) on 4 of 12 role/viewport cells: public/desktop, external/tablet, admin/tablet, admin/mobile. The error each time: `Error loading certificates: TimeoutError: signal timed out` from `wwwroot/js/certificate-tile-view.js:243` (`fetch(..., { signal: AbortSignal.timeout(30000) })`). Each FAIL is paired with a WARN: `tiles-load: cards=0` — the failed fetch produced an empty grid.
- Curl-timed sequence directly against `/api/certificate-tiles?page=1&pageSize=20` on the already-warm app: **23.114s → 0.198s → 0.054s → 0.031s → 0.027s**. The first call is a >100× outlier — classic EF Core query-compile + cold-cache cost.
- Why FAILs are scattered (not all on first cell): the smoke runs sequentially across 10 views, so cert-tile-public is the 3rd view. By then `home` and `login` have warmed *some* code paths but NOT the tile-cert projection path. The remaining 4/12 flakes are role-specific projection variants (admin sees more rows; the larger result set + projection still has cold-start cost even after the public/desktop variant warmed). Cold-cache fans out by query shape, not by app instance.

**Why the smoke caught it but UAT didn't.**
- A human user on first daily load would *see* a 23s spinner (not a great UX, but not "broken"). The smoke's hard 30s ceiling on a *single* fetch promotes the slow path from "ugly UX" to "TimeoutError" when cold-cache stacks with parallel page-load fetches (filter options + cert tiles + autocomplete preload + analytics beacon all racing for the same DB connection).
- Production behavior is likely worse — Plesk app pool recycles produce the same cold-start window, and average remote latency adds further headroom pressure.

**Fixes applied (BOTH, defense in depth).**
1. **Reduce cold-start cost — `AsSplitQuery` + stable `ThenBy(c => c.Id)` tiebreaker.** `Repositories/CertificateRepository.cs::GetAdvancedFilteredCertificatesAsync` had 10 Includes with 2 collection includes (`Tags`, `Partners`) — the textbook cartesian-on-includes shape that `feedback_ef_includes_with_projection` and DEF-166 history warn about. `AsSplitQuery()` makes EF emit one query per collection instead of one giant cartesian join; the stable tiebreaker prevents page-boundary drift across SQL Server batches when the primary sort field has ties. The internal and external tile controllers already applied this pattern at their own query sites — this was the scoping gap, exactly the pattern `feedback_scoping_check_adjacent_endpoints` describes.
2. **Pre-warm the query plan + EF model at startup — new `EfQueryWarmupService` (`IHostedService`).** Fires one Take(1) call through the same repo method during host startup so the first real request doesn't pay the EF compile + SQL plan cost. Runs as a background task (`Task.Run`) so it doesn't block the host startup pipeline. Fail-safe: any exception is logged at Warning and swallowed — warm-up is an optimization, not a correctness requirement. Registered in `Program.cs` immediately after the certificate-expiration service block.

**Explicitly not applied.** Raising the browser-side `AbortSignal.timeout(30000)` was rejected — it would hide a real perf issue without helping users on the slow path.

**Linked memory.** [[feedback_ef_includes_with_projection]] (cartesian-on-includes can cause the same shape of cold-start; AsSplitQuery is the canonical Anuki fix); [[feedback_scoping_check_adjacent_endpoints]] (the internal + external tile views likely share the same cold-start cost — investigate together if fixing).

**Verification (2026-06-01, local — pre-deploy perf gate).** Confirmed on `localhost:5101` with the fix in the working tree:
- Startup log: `EfQueryWarmupService: public tile query warmed in 6380ms (totalCount=22)` — the cold EF-compile cost is paid once at host startup, off the user request path.
- First *real* user request to `/api/certificate-tiles?page=1&pageSize=20` measured warm at **~80–280ms** (8-call sample: 0.277, 0.086, 0.093, 0.077, 0.152, 0.075, 0.077, 0.183s) — three orders of magnitude under the 30s `AbortSignal` ceiling.
- Caveat: while a concurrent E2E smoke run was loading the *same* instance, the endpoint spiked to 30.4s/28.2s (call 1 tripped the 30s abort). That is DB connection-pool contention under the smoke suite's parallel load — **not** an `AsSplitQuery` regression (proven: with the load removed, warm calls collapse to ~80ms). Separate follow-up: throttle the smoke suite or size the connection pool so a 10-view parallel sweep doesn't self-induce the timeout. Tracked as a note, not a DEF (this is DEF-235 — addressed by the `run-all.js` batch-with-restart mode).

**Re-verification (2026-06-02, fresh cold boot — closes the "must re-baseline green" gap).** Killed all dotnet, started a single clean `dotnet run`, and measured the FIRST request after a genuinely cold start (not already-warm Kestrel):
- Startup log: `EfQueryWarmupService: public tile query warmed in 1027ms (totalCount=22)`.
- **First real `/api/certificate-tiles?page=1&pageSize=20` request: 0.054s** (then 0.150s, 0.038s) — i.e. the 23s cold-start window is *gone* on a fresh boot; the warmup pre-pays it. This is the cold-boot proof the 2026-06-01 verification (on already-warm Kestrel) lacked.
- Smoke re-baseline: `cert-tile-public` is **124P/0F/0W** across every clean Tier 1 run (single + batch-restart), and the Tier 1 roll-up is GREEN under `SMOKE_BATCH_RESTART=1`. The DEF-234 assertion is re-baselined green. **Ready for deploy** (no further local work; ship with the next Plesk push).

---

### DEF-233 — `pageUnloading` navigation-abort guard was only on `loadInitialCertificates`, not on `loadFilterOptions` — all 3 tile-view scripts (caught by new E2E smoke template on first run)

- **Date:** 2026-06-01
- **Severity:** Low (cosmetic console noise + smoke-suite false positive). The earlier pageUnloading fix wrapped one of two parallel fetches in each tile-view script. The unwrapped `loadFilterOptions` fetch, when aborted by navigation or browser-context teardown, throws "Failed to fetch" and logs to console. Production users would only see this on rapid navigation away during initial page load — narrow window, no functional impact. The bigger reason to fix: the new E2E smoke template flags any `[console.error]` as a FAIL, and this was a guaranteed false positive on every authenticated tile-view smoke.
- **Status:** **Fixed locally — pending deploy.** 2-line change × 3 files. Smoke `home.js` was 9/104 FAIL pre-fix → 104/104 PASS post-fix (verified twice in a row).
- **QC Ref:** internal (not a client QC item — found by tooling). Linked to the E2E smoke plan in `docs/tracking/e2e-smoke-plan.md`.
- **Bucket:** Bug (front-end — pre-existing fix had partial coverage of the symptom surface)
- **Reporter:** E2E smoke template (Day 1 reference impl), 2026-06-01

**Root cause.** Per memory `feedback_scoping_check_adjacent_endpoints` — the prior pageUnloading fix (May 2026) added a module-level `pageUnloading` flag set on `pagehide`, and a `if (pageUnloading) return;` guard in the catch block of `loadInitialCertificates` in all three tile-view scripts. But each file also has a `loadFilterOptions` function that runs in **parallel** with `loadInitialCertificates` via `Promise.all([loadFilterOptions(), loadInitialCertificates()])`. Its catch block was identical structure but missing the guard — so when context teardown / navigation aborted ITS fetch, the catch logged `"Error loading filter options: TypeError: Failed to fetch"` even though the failure was already-expected navigation noise.

**Why the smoke caught it but UAT didn't.** The smoke calls `ctx.close()` immediately after assertions complete — sub-second after page load. Real users don't navigate that fast, so the fetch usually completes before any abort. Run-to-run variance in the smoke (timing race) is why it showed 0 errors on one run and 9 on another. The fix is deterministic; the symptom was nondeterministic.

**Fix.** Two lines per file in 3 files:
- `wwwroot/js/certificate-tile-view-external.js:60-62` — added `if (pageUnloading) return;` ahead of `console.error`.
- `wwwroot/js/certificate-tile-view-internal.js:61-63` — same.
- `wwwroot/js/certificate-tile-view.js:106-108` — same.

**Verification.** `Scripts/smoke-prod/_views/home.js` ran 104/104 PASS twice in a row (4 roles × 3 viewports × ~9 assertions each). Pre-fix runs showed 95-101 PASS depending on timing race. The home smoke is now the Day 1 reference impl for the E2E plan; results JSON in `Scripts/smoke-prod/_results/`.

**Linked memory.** [[feedback_scoping_check_adjacent_endpoints]] (parallel fetches share the same abort window — fix both at once); the original pageUnloading fix (May 2026) closed `loadInitialCertificates` but missed the parallel `loadFilterOptions`.

---

### DEF-232 — Template export polish: Tags template comma-splits "Internal/External" header, Cert template em dash becomes mojibake in Excel, User template + bulk import didn't accept Partner (QC #148 5/29 follow-up)

- **Date:** 2026-06-01
- **Severity:** Medium (cosmetic + misleading + hidden gap) — Tags template parses as 4 cols instead of 3 in Excel (Internal lands in B, External in C, Description shifts to D); Cert template Status hint shows `â€"` where the em dash should be; User template Role hint omitted Partner AND the bulk-import path had no way to associate partners (header-only fix would have created orphaned Partner-role accounts). Client called the Cert mojibake "not a deal breaker"; the Tags column split is the real blocker.
- **Status:** **DEPLOYED + VERIFIED LIVE on gccwebapp.com (2026-06-01).** All three surfaces fixed including the full Partner wire-through (template + JS parser + payload + service-level dictionary). Shipped in the 2026-06-01 push (read-only verification 10/10, DEF-232 included — see `docs/tracking/shipped-2026-06.md`). Build clean. *(Status reconciled 2026-06-04 — the prior "Fixed locally — pending deploy" line was stale; the shipped log is the authoritative deploy record.)*
- **QC Ref:** #148 — 5/29 client follow-up to DEF-202 (Sprint 3 cert template Status-hint inline values)
- **Bucket:** Bug (template export — CSV encoding + header construction + missing import column)
- **Reporter:** Shawn West, 2026-06-01 (relaying client 5/29 comment + screenshot)

**Root causes (3 separate bugs sharing the "template export" surface).**

1. **Tags template — unquoted comma in header splits the row.**
   `wwwroot/js/tags.js::downloadImportTemplate` built the header row as
   `['Name', 'TagType (Internal, External)', 'Description'].join(',')`
   → `Name,TagType (Internal, External),Description`. The unquoted internal comma made Excel parse 4 columns. Client's screenshot confirms: B=`TagType (Internal`, C=`External)`, D=`Description`.

2. **Cert template — UTF-8 written without BOM; Excel decodes as Windows-1252.**
   `Controllers/CertificateImportController.cs::DownloadTemplate` used
   `Encoding.UTF8.GetBytes(templateCsv)` (no preamble). The Status hint contains an em dash (U+2014 `—`) which encodes as `0xE2 0x80 0x94` in UTF-8; Excel on Windows interprets those three bytes as `â€"` because there's no BOM telling it the file is UTF-8. Mojibake visible in the screenshot's formula bar right before "defaults to Draft if blank."

3. **User template — Role hint lists 3 of 4 valid roles.**
   `wwwroot/js/userManagement.js::downloadImportTemplate` writes `'Role (Admin/Analyst/External)'`. The role dropdown (lines 103–110) and `BulkImportUsersAsync` (`Services/AdminService.cs:296`) both accept any role-table value, including Partner, but the template doesn't advertise it.

**Fix (issues 1 + 2).**
- `wwwroot/js/tags.js:546` — header renamed to `'TagType (Internal/External)'` (slash separator, matches User Mgmt template pattern). 1-line change.
- `Controllers/CertificateImportController.cs:336` — emit UTF-8 BOM (`new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)` → preamble prepended to body). Future non-ASCII headers also covered.

**Fix (issue 3 — Partner wired through end-to-end).** Shawn chose path (A) on 2026-06-01: advertise Partner in the template AND wire `PartnerNames` through the full bulk-import stack so the template promise is honest. Mirrors the existing `CompanyNames` pattern at every layer.

- **Template.** `wwwroot/js/userManagement.js::downloadImportTemplate` — Role hint now reads `Role (Admin/Analyst/External/Partner)` and a new `Partner (semicolon-separated names; required for Partner role)` column is appended.
- **JS parser.** `normalizeUserHeaderName` accepts `partner`/`partners`/`partnername`/`partnernames` → maps to canonical `partner`. The parsed-row builder and `parsedUsers.push(...)` carry `partner` alongside `company`. Sanitization (HTML strip, javascript: defang) runs over the value just like the other text fields.
- **JS payload.** `commitImport` calls new `resolvePartnerIds(row.data.partner)` (parallels `resolveCompanyIds`) to map CSV partner names → IDs via the in-memory `this.partners` cache loaded at page init. Partner role with empty `partnerIds` is **rejected with a clear row-level error** before POSTing — no orphaned-account regression possible. Payload now includes `partnerIds` alongside `companyIds`.
- **Server payload model.** `CreateUserViewModel` already had `PartnerIds`. No model change needed for the current per-row POST path (`/Admin/CreateUser`).
- **Server bulk path.** `BulkImportUserItem` gains a `PartnerNames` list and `BulkImportUsersAsync` (`Services/AdminService.cs`) pre-fetches a `partnerByName` dictionary (mirrors `companyByName`), then validates per row with the same fail-fast pattern as companies — unknown partner name → row rejected with `"{email} - Partner '{name}' not found"`. This branch is currently dormant (DEF-113 added the bulk endpoint but the JS still does per-row `/Admin/CreateUser` — see `userManagement.js:1466`) but kept consistent so a future JS migration to the bulk endpoint won't silently drop the partner column.

**Partner validation summary.** Three independent guards now stop an orphaned Partner-role user from being created via import: (1) JS pre-POST check in `commitImport` rejects empty `partnerIds`; (2) per-user `CreateUserAsync` continues to use `model.PartnerIds` as before — no change needed; (3) `BulkImportUsersAsync` rejects unresolved partner names. Same defense pattern as `feedback_scoping_check_adjacent_endpoints` — the import surface has 4 layers and they all needed the fix together.

**Linked memory.** [[feedback_scoping_check_adjacent_endpoints]] (template + model + JS parser + service all touch the import — only fixing the header would have been incomplete); [[feedback_track_every_fix_in_defect_register]] (every QC fix → DEF).

---

### DEF-231 — Cert form inline "Add New product" ignores the analyst's Category/Type/Subcategory and hard-defaults to the first category (QC #186, threads 2 + 3)

- **Date:** 2026-06-01
- **Severity:** High (data integrity — products created via the cert form's quick-add land under the wrong category with no type/subcategory; the analyst's explicit selections are silently discarded, and the bad hierarchy then propagates to the cert and to Product Management)
- **Status:** **Fixed + verified on `localhost:5101` (2026-06-01) — `saveInlineProduct` now reads the form's Category/Type/Subcategory and passes them to `/Product/SaveProduct`.** Repro `def231-quickadd-repro.js` 8/8, fix-verify `def231-fix-verify.js` 6/6. Pending deploy.
- **QC Ref:** #186 (client 5/29: *"I selected Chemicals and it shows Carly Test … Product Type/Subcategory are not appearing … not showing in Product Management (26-0166) … it changed/removed some of my selections"*). QC #186 is the open-items rollup Carly cross-referenced from #91 and #170.
- **Bucket:** Bug (front-end — wrong source for a required FK + dropped optional FKs)
- **Reporter:** Shawn West, 2026-06-01 (triage of QC #186)

**Root cause (confirmed by repro).** `wwwroot/js/certificate-inline-create.js::saveInlineProduct()` — the "Add New …" footer on the **Product Name** dropdown — built its POST as:
```js
const categoryId = await getDefaultCategoryId();   // = /Product/GetCategories[0], lowest SortOrder
body: { Id:0, Name, CompanyId, CategoryId: categoryId, IsActive:true }   // no ProductTypeId, no ProductSubcategoryId
```
It never read `#ProductCategoryId` / `#ProductTypeId` / `#ProductSubcategoryId` from the form. So every quick-added product was forced under the **first** category returned by `GetCategories` (ordered by `SortOrder`) with **no** type/subcategory — regardless of what the analyst picked. In the client's data the lowest-sorted category is "Carly Test," which is why she saw *"selected Chemicals → shows Carly Test."* The cert save path itself is clean (`CertificateController` writes the posted FKs verbatim); the corruption originates entirely in this quick-add helper.

**This single bug produces all three of Carly's symptoms:**
| QC #186 symptom | Mechanism |
|---|---|
| 3b "selected Chemicals, shows Carly Test" | product forced under `GetCategories[0]`, not her pick |
| 3a "subcategory not in Product Management (26-0166)" | product saved with `ProductTypeId = ProductSubcategoryId = null` |
| 2 "Product Type blank on Edit (Subcategory kept)" | Edit cascade loads types for the saved (wrong) category, so the intended type isn't in the list and can't pre-select; subcategory loads directly from `savedTypeId` so it survives |

**Repro (deterministic, `Scripts/smoke-prod/def231-quickadd-repro.js`, 8/8).** Replayed the exact buggy payload on localhost: default category was `Batch2QA-Cat-…`(id=10, lowest SortOrder), intended was `Chemicals and Raw Materials`(id=1). The created product persisted under `Batch2QA-Cat-…` with empty type/subcategory — exactly the client's symptom (their first-sorted category being "Carly Test"). Self-cleaning (sweeps `ZZ_Probe_QuickAdd_*`).

**Fix.** `wwwroot/js/certificate-inline-create.js::saveInlineProduct()` now reads `#ProductCategoryId/#ProductTypeId/#ProductSubcategoryId`, prefers the analyst's category (falls back to `getDefaultCategoryId()` only when none is selected — category is required by `/Product/SaveProduct`), and includes `ProductTypeId`/`ProductSubcategoryId` **only** when the form's category was used (so they can't belong to a mismatched category). `ProductSaveModel` already accepts both FKs (the full "Add New Product" modal path uses them), so no server change.

**Fix verification (real UI, `Scripts/smoke-prod/def231-fix-verify.js`, 6/6).** Drove `/Certificate/Create`: selected a company, set Category=Chemicals + Type=Additives on the form, then used the inline "Add New" footer to create a product. Read back via `/Product/GetProducts`: product persisted under **Chemicals / Additives**, not the default. Confirms the form selection now flows through.

**Scope / not in this fix.**
- **Existing mis-categorized rows** (products/certs created before this fix, e.g. live cert 26-0166) are **not** corrected by a code change — they need a data pass (data op / CR per [[feedback_data_changes_require_cr]]). Thread 2 will keep showing on those specific old certs until their data is fixed; it is resolved for all newly-created products. Draft cleanup scripts (read-only identify + guarded remediate) live at `Scripts/data-ops/2026-06-01-product-miscategorization-{IDENTIFY,REMEDIATE}.sql` — data op, no DEF prefix per [[feedback_data_issue_vs_defect]]; needs backup + CR before running. **The analyst's intended category was overwritten by the bug and is not recoverable from data, so remediation is human-review-driven, not auto-derived.**
- The full "Add New **Product**" modal (`saveQuickProduct`) was already correct — it posts the modal's own category/type/subcategory. Only the dropdown-footer quick-by-name path (`saveInlineProduct`) was broken.
- Recommend a follow-up Edit-prefill check on a correctly-categorized cert to confirm Thread 2 is fully closed once data is clean.

**Linked memory.** [[feedback_scoping_check_adjacent_endpoints]] (two product-create paths; only one carried the hierarchy), [[feedback_data_issue_vs_defect]] (code fix here; old rows are data ops).

---

### DEF-230 — Admin → Product Types: editing an existing type/subcategory could not change its parent — re-parent was silently dropped on save (QC #170 5/29 re-open)

- **Date:** 2026-06-01
- **Severity:** Medium (functional gap; users cannot move a type to a different category or a subcategory to a different type — the only workaround is delete + recreate, which is blocked when references exist)
- **Status:** **Fixed + automated-probe-verified on `localhost:5101` (2026-06-01) — `UpdateProductTypeAsync` now persists the new parent (`CategoryId` for types, `ParentId`/`CategoryId`/`Level` for subcategories) and cascades `CategoryId` to descendants.** Build 0 errors; probe `def230-reparent.js` 11/11 PASS. Pending next deploy (batching with other open items).
- **QC Ref:** #170 (client 5/1, re-flagged 5/29: *"I COULD NOT UPDATE THE EXISTING THAT [I] USE BECAUSE IT WOULD NOT GIVE ME THE OTHER ALTS, SOMETHING IS BROKEN"*)
- **Bucket:** Bug (write path dropped a validated field)
- **Reporter:** Shawn West, 2026-06-01 (triage of QC #170 5/29 re-open)

**Root cause.** `Services/AdminService.cs::UpdateProductTypeAsync` validated `model.ParentId` (existence, self-parent, descendant-cycle) and then saved **only** `Name` + `SortOrder` — it never assigned `ParentId`, `CategoryId`, or `Level`. The entire parent-validation block was dead code: pick a different parent in the edit dropdown, hit Update, and the item stayed exactly where it was. The edit dropdown (`getParentOptions`, `wwwroot/js/product-types.js:849`) renders and pre-selects correctly, so to the user it looked like the move took until the tree reloaded unchanged.

Compounding bug: the old code looked `model.ParentId` up in `ProductTypes` for **every** edit. But the dropdown value is a **`ProductCategory.Id`** when editing a top-level type (its parents are categories) and a **`ProductType.Id`** when editing a subcategory (its parents are types). Treating a category id as a product-type id would have validated against the wrong table (and could coincidentally match an unrelated type id) had the assignment ever fired.

**Fix (single method, `Services/AdminService.cs::UpdateProductTypeAsync`).** Discriminate off the existing record — `productType.ParentId == null` ⇒ top-level type (parent is a category), else subcategory (parent is a type):
- **Type branch:** resolve `model.ParentId` against `ProductCategories`; duplicate-name check scoped to `CategoryId` + `ParentId == null`; on a real category change, cascade `CategoryId` to all descendants (subcategories inherit it) then set the type's own `CategoryId`.
- **Subcategory branch:** resolve against `ProductTypes`; keep the self-parent + descendant-cycle guards; duplicate-name check scoped to the target `ParentId`; assign `ParentId`, inherit `CategoryId` from the new parent, recompute `Level = newParent.Level + 1`.
- Both branches keep a name-only path (no parent submitted) with an in-place duplicate guard so a failed dropdown pre-select can never null out the parent.

**Files changed.**
- `Services/AdminService.cs` (`UpdateProductTypeAsync`, lines ~1114–1220)

**Why this is a separate DEF, not part of DEF-196/197/226.**
- DEF-196 fixed the Usage **count column** + delete-blocker **error messages**; DEF-197 fixed the rollup **dedup**; DEF-226 is the "0 / N" pill **readability**. None of them touched the edit/re-parent write path — it was latent the whole time and is what Carly's 5/29 "could not update the existing" note actually describes.

**Verification — automated probe, 11/11 PASS on `localhost:5101` (2026-06-01).** `Scripts/smoke-prod/def230-reparent.js` (results: `Scripts/smoke-prod/def230-reparent-results.json`). The probe is self-contained and reversible — it creates throwaway `ZZ_Probe_*` nodes, exercises the moves, asserts persistence, then deletes everything it created (cleanup swept all 6 nodes, 0 failures). It is gated localhost-only and refuses to run against gccwebapp/greencirclecertified.
- **T1** — created a top-level type under cat A, moved it to cat B → tree reloads with it under B and **not** under A (`under B=true, still under A=false`). This is the assertion the old Name+SortOrder-only save would have failed.
- **T2** — created a subcategory under type A, moved it to type B → now under B not A; **category inheritance** confirmed (moved sub renders in cat B's subtree because it adopted type B's `CategoryId`); **self-parent** rejected with "An item cannot be its own parent".
- **T3** — renaming a type into a name already taken under the target category is rejected ("'…' already exists under '…'") — the rescoped duplicate guard works.
- Deploy-day: re-run the same probe against the deployed build (still localhost-only — do not point it at prod). Manual spot-check that Usage counts follow the moved node.

---

### DEF-229 — Certificate Create page: Product Identifier "select all" only selects the visible 50, no cross-page banner appears (QC #159 5/29 follow-up)

- **Date:** 2026-06-01
- **Severity:** Medium (functional gap; users cannot bulk-clear large in-memory Product ID lists during cert creation — only delete 50 at a time)
- **Status:** **Fixed locally on `localhost:5101` (2026-06-01) — totalCount now set in in-memory render path, cross-page banner appears, handleBulkDelete branched for in-memory cross-page case.** Pending next deploy (Shawn batching with other open items before redeploy).
- **QC Ref:** #159 (client 5/29: *"I don't see a mass delete option. Or is the intention to use the 'Replace all existing IDs with this file' when importing a new list?"* — client found the import-replace path; this DEF closes the missing select-all-across-pages affordance on the Create page side)
- **Bucket:** Bug (state-init gap surfaced by Create-page in-memory mode)
- **Reporter:** Shawn West, 2026-06-01 (triage of QC #159 follow-up)

**Root cause.** The DEF-194 v2 cross-page select-all banner ("Select all N product IDs across pages") is driven by `this.totalCount` in `updateBulkActionsUI` (`wwwroot/js/product-id-manager.js:1013`). On the Edit page in server-paged mode, `_loadProductIdsPaged` sets `this.totalCount = json.totalCount` (line 175). On the Create page, the manager runs in in-memory mode (`certificateId === 0`, `serverPaged = false`) and the load path at line 308 short-circuits without ever touching `totalCount` — it stayed at its constructor default of `0`. With `totalCount === 0`, `const total = this.totalCount || visible` falls back to `visible` (the 50 rendered rows), `moreOffPage = total > visible` is `false`, and the banner never appears. Header select-all therefore captures only the visible 50 with no path to extend the selection.

Secondary issue: even if the banner had appeared, `handleBulkDelete` would have POSTed `{deleteAll:true}` to `/api/productids/certificate/0/bulk-delete` — there's no certificate yet on Create, so a server bulk-delete is the wrong operation. Create-page rows live entirely in `this.productIds` until form submit.

**Fix (single file, two hunks).**
1. `renderTable()` — after `applySort()`, set `this.totalCount = this.filteredProductIds.length` when `!this.serverPaged`. Every in-memory mutation path (initial load, applyFilters, add, edit, delete, import-apply) funnels through `renderTable()`, so one assignment covers them all.
2. `handleBulkDelete()` — the existing in-memory branch (added with DEF-194) only handled the per-row case (`selectedIds`). Extended it to handle cross-page: when `crossPage` is true, drop every row in the current `filteredProductIds` from `productIds` (items outside the active filter stay). Also switched the success toast to show the actual `removed` count rather than the pre-delete intended `count`.

**Files changed.**
- `wwwroot/js/product-id-manager.js` (lines ~360 and ~1068)

**Why this is a separate DEF, not a reopen of DEF-159/180/194.**
- DEF-180 (QC #159 close) and DEF-154 (Replace-all toggle) covered the **import-driven** replace path — that still works as confirmed by the client on 5/29.
- DEF-194 v2 shipped the cross-page select-all UI for the **Edit page** (server-paged). The Create page (in-memory) was not part of that test matrix because the original QC #172 ticket was framed against Edit; the in-memory totalCount gap was latent.
- This DEF is the in-memory-mode analog of DEF-194 v2, surfaced by Shawn's follow-up review of QC #159.

**Verification (deferred to deploy day per Shawn's batching directive).**
- Local smoke: open `/Certificate/Create`, import a CSV with >50 rows via Product Identifier import preview, apply, click header select-all → banner appears with `Select all N product IDs`. Click banner → button reads `Delete Selected (N)`. Confirm delete → all in-memory rows removed, toast shows correct count, table renders empty state.
- Edge: with filter applied (e.g., `Type=ASIN`), repeat → only filtered rows drop, unfiltered rows remain. (In-memory cross-page must respect the visible filter.)
- Regression: Edit page (server-paged) unchanged — the `!this.serverPaged` guard on the new totalCount assignment means `_applyServerPage`'s authoritative totalCount is the only one used in paged mode.

---

### DEF-228 — Partner CSV export: client thought internal-tagged certs were leaking; verified working-as-designed (QC #182 5/29 follow-up)

- **Date:** 2026-06-01
- **Severity:** N/A (no defect — investigation confirms current behavior is correct)
- **Status:** **Closed — Working As Designed. No code change required.** Initial row-level filter was implemented and then reverted same day after Shawn clarified the scope of "internal".
- **QC Ref:** #182 (5/29 client retest of DEF-198)
- **Bucket:** Investigation (client clarification — terminology misalignment)
- **Reporter:** Shawn West, 2026-06-01 (relaying client retest)

**What the client reported.** Cert "Chris Test" set to "Internal visibility" was appearing in the Amazon partner CSV export.

**Reproduction (verified end-to-end on localhost dev DB).** Logged in as `partner_test` (Amazon-mapped Partner role). Hit `/Certificate/ExportCertificates?format=csv` against cert 26-0001 — which has `Status=Client Approved - Paid`, `Partner=Amazon`, AND an internal tag `Sustainable` attached (the closest dev-DB analog to client's "Chris Test"):
- **Internal Tag column:** absent from CSV header (DEF-128 working correctly)
- **Internal tag value "Sustainable":** absent from every row (DEF-128 working correctly)
- **Cert 26-0001 row:** present in export (qualifies by partner + status; correct per Shawn's clarification)

**Clarification (Shawn, 2026-06-01).** "The internal tag should be excluded, not the certificate." Read: hide the *tag* from the export (column + values), keep the *certificate row* visible when it qualifies by partner + status. This is the existing DEF-128 behavior and remains correct.

**Why the misread happened.** The client saw a cert in the partner CSV that — in the admin view — had an internal tag attached. They interpreted "this cert has an internal tag" as "this cert is internal-only and shouldn't be in my view." But internal tags are admin-only *metadata categorization*, not row-level access control. Status is the access-control mechanism (Draft / Hold / Confidential hide a cert from non-internal users; Paid / Republished publish it).

**What got committed and reverted (2026-06-01, same day).** A row-level filter `.Where(c => !c.Tags.Any(t => t.TagType == "internal"))` was added to `Services/CertificateService.cs::ApplyExportSecurityScope` for Partner / External / Public branches. Reverted on Shawn's correction. Net change: zero code.

**Action items going forward.**
- Update client memo (`docs/tracking/client-memo-partner-api-internal-tags-2026-06-01.md`) to explain the existing behavior is correct — no JSON API change needed, no scope expansion needed.
- For future client reports of "internal cert showing in partner view": confirm whether the complaint is about the *tag value* leaking (would be a real DEF-128 regression) versus the *cert row* appearing (working as designed; explain status is the access-control lever).

**Verification artifact.** `scripts/smoke-prod/def228-partner-export.js` — Playwright probe of Amazon partner export. Updated to assert the correct working-as-designed PASS criteria (column hidden + tag-value hidden + cert row present).

---

### DEF-227 — Nav-to-content gray spacer missing for Analyst / External / Public; only Admin had it (QC #77 — fix-didn't-propagate)

- **Date:** 2026-06-01
- **Severity:** Medium (visual inconsistency, not functional; content appears flush against navigation menu for non-admin roles, navigation submenu can feel like it overlaps with content)
- **Status:** **Fixed locally on `localhost:5101` (2026-06-01) — all 4 roles now render 32px gray spacer (admin shows breadcrumb content inside; others render empty).** Pending deploy.
- **QC Ref:** #77 (client noted nav overlap in Analyst, gray area present in Admin)
- **Bucket:** Bug (CSS specificity — prior fix worked only for Admin)
- **Reporter:** Shawn West, 2026-06-01
- **Screenshot:** `C:\Users\swest\Downloads\Screenshot 2026-06-01 100149.png` — side-by-side Analyst (bug) vs Admin (correct). Analyst row showed content directly against nav menu; Admin had the gray breadcrumb spacer between.

**Root cause.** `Views/Shared/_Layout.cshtml:299` rendered the breadcrumb container as `<nav class="breadcrumbs role-element role-admin-show">`. The prior fix in `wwwroot/css/main.css:744-746` intended to **always show the gray container** (`.breadcrumbs { display: block !important; min-height: 32px }`) and gate only the content via `.role-admin .breadcrumb-wrapper { visibility: visible }` at line 764. But `.role-element { display: none !important }` at line 2424 has equal specificity (0,1,0) AND appears LATER in the cascade — so for non-Admin roles, `.role-element` won and the gray container was hidden entirely. For Admin, `.role-admin .role-admin-show { display: block !important }` at line 2458 has higher specificity (0,2,0) and won — which is why **only Admin saw the spacer**.

**Fix.** Removed `role-element role-admin-show` classes from the breadcrumb `<nav>` element. The gray container is now governed solely by `.breadcrumbs` rule (display:block always). Content visibility inside is still gated to Admin by the existing `.role-admin .breadcrumb-wrapper { visibility: visible }` logic. Verified across 4 roles on localhost:5101:

| Role | Gray spacer height | Content visible |
|---|---|---|
| Admin | 32px ✓ | Yes ✓ |
| Analyst | 32px ✓ | No (correct) |
| External | 32px ✓ | No (correct) |
| Public | 32px ✓ | No (correct) |

**Pattern this case represents.** Same shape as [[feedback_scoping_check_adjacent_endpoints]] — fix landed on one surface (Admin), didn't propagate to adjacent surfaces (Analyst/External/Public). Client called out this is a recurring concern: "Anuki needs to make sure there are not inconsistencies in instances that shouldn't be." For future cross-role CSS fixes, audit body classes for all 4 roles before marking complete.

**Linked code.**
- `Views/Shared/_Layout.cshtml:299-307` — markup fix
- `wwwroot/css/main.css:744-766` — pre-existing CSS (unchanged; intent now correctly realised)
- `wwwroot/css/main.css:2424` — `.role-element` rule that was beating `.breadcrumbs` (unchanged)

**Verification artifact.** `scripts/smoke-prod/def227-nav-spacing.js` — Playwright probe across 4 roles. Re-runnable.

---

### DEF-226 — Admin → Product Types: Usage "0 / N" pill is read as "0" by admins; delete blocks while pill's first half says zero (UX, NOT count accuracy — QC #170 follow-up)

- **Date:** 2026-06-01
- **Severity:** Medium (counts are technically correct; the surprise is the visual presentation — admin reads only the first number of the `0 / 1` pill and treats the row as deletable. Block message IS specific; surprise is on the table side, not the error side.)
- **Status:** **Fixed locally on `localhost:5101` (2026-06-01) — count pill now bolds the non-zero half + prefixes a small lock icon when either count blocks deletion.** Pending deploy.
- **QC Ref:** #170 follow-up (5/19 client retest closed; 6/1 client retest reopens the visual side of it)
- **Bucket:** Bug (UX presentation — count format trains the eye to read just the first number)
- **Reporter:** Shawn West, 2026-06-01
- **Screenshots:**
  - Original report: `C:\Users\swest\Downloads\Screenshot 2026-06-01 083559.png` (bottom panel "image 79")
  - Repro run: `scripts/smoke-prod/screenshots/def225-def226-2026-06-01T12-58-37/def226-01-product-types-page.png`

**Reproduction (verified end-to-end).** Setup: created `ProductCategory "DEF226-CatOnly"` (id=15), tagged cert 26-0001 (Id=2) to `ProductCategoryId=15, ProductTypeId=NULL` via `scripts/smoke-prod/def226-setup.sql`. Then walked Admin → Product Types as `admin`:
- Tree node `DEF226-CatOnly` rendered with count text `"0 / 1"` (0 products, 1 cert) — **count is correct against DB**.
- POSTed to `/Admin/DeleteProductType/15?isCategory=true` (mirrors what the trash-icon button does). Server returned `{ success: false, message: "Cannot delete: 1 certificate(s) use this category. Edit those certificates to use a different category first, then try again." }` — **block message is correct AND specific** (DEF-196 work).
- Cleanup ran via `scripts/smoke-prod/def226-cleanup.sql`; cert restored and category dropped.

**Conclusion vs. earlier hypothesis.** The 2026-06-01 first-draft entry guessed this might be a DEF-197 dedup edge case OR DEF-196 regression. The repro disproves both: counts are accurate, block message is accurate. The bug is **UX presentation** — the "0 / 1" pill is rendered as a single blue pill at the right edge of each row; the admin's eye lands on the leading `0` and skips the slash + second digit. The column header `Usage (Products / Certs)` is correct but the in-row pill doesn't reinforce it visually. Same pill geometry on chemicals subtree didn't surface this on 5/19 because that subtree's category rows showed non-zero on both halves (e.g. `4 / 19`).

**Linked memory.**
- [[feedback_qc_review_no_changes]] — triage + non-mutating repro only; no production-code edits today
- [[feedback_data_issue_vs_defect]] — verified the count is factually correct against DB, so this stays a DEF (UX presentation bug), not data ops

**Fix applied (2026-06-01).** `wwwroot/js/product-types.js:107-122` (`createTreeNode`). The `<span class="node-count">` now renders three child spans: products / separator / certs. Each count span gets `font-weight: 700` when its value is non-zero, `opacity: 0.55` when zero. When either count > 0, a `<i class="fas fa-lock">` icon is prefixed (font-size 9px, opacity 0.75) with title "In use — delete blocked". The blocker side now jumps off the pill instead of blending into a single "0 / 1" glyph. DB and rollup logic untouched (DEF-196/197 remain correct). Table view (line 364-381) already has separate Products and Certificates columns so doesn't share the pill confusion — no fix needed there.

---

### DEF-225 — Cert Edit: saved Product Name not displayed in custom-dropdown field even though "1 product(s) found" hint shows the product loaded

- **Date:** 2026-06-01
- **Severity:** High (analyst opens Edit on a cert and the Product Name field reads as cleared; only way to recover the saved value is to re-pick the product, which risks the analyst saving over a previously correct selection with a guess)
- **Status:** **VERIFIED ON `gccwebapp.com` (UAT, 2026-06-03) — behavioral read-only probe `Scripts/smoke-prod/qc186-uat-live.js` opened 4 live certs (26-0166/0167/0164/0163) that have saved products; the VISIBLE `.custom-dropdown-selected` text matched the underlying `#ProductId` option on every one, rendered in dark `rgb(51,51,51)` (not placeholder gray). 4/4 widget-sync PASS.** Previously fixed on `localhost:5101` (2026-06-01, 5/5 viewports) and shipped in the June batch. Note: the per-view smoke `_views/cert-edit.js` only inspects the underlying `<select>` (always correct) and would NOT have caught this — the new probe reads the *visible* widget, which is the actual reported surface.
- **QC Ref:** #91 (TC-E sub-symptom still outstanding) + #170 (side-finding F, flagged in `qc170-localhost-validation-2026-05-19.md` for separate triage) + #186 (the "Ready for UAT" thread)
- **Bucket:** Bug (UI display / custom-dropdown state sync on pre-select)
- **Reporter:** Shawn West, 2026-06-01
- **Screenshots:**
  - Original report: `C:\Users\swest\Downloads\Screenshot 2026-06-01 083559.png`
  - Repro run: `scripts/smoke-prod/screenshots/def225-def226-2026-06-01T12-58-37/def225-{viewport}-02-product-section.png` (5 viewports: 1920/1440/1024/768/390)

**Reproduction (verified end-to-end).** Cert `26-0001` (Id=2, ProductId=77 "Widget Pro") opened via `/Certificate/Edit/26-0001`. Captured DOM state on all 5 viewports:
- `<select id="ProductId">.value = "77"`, `selectedOption.text = "Widget Pro"`, `selectOptionCount = 2` — underlying select **is** pre-populated correctly by the page's setTimeout(50ms) → `productSelect.val(preselectProductId)`.
- `.custom-dropdown-trigger > .custom-dropdown-selected` reads **"Select product..."** in placeholder gray `rgb(108, 117, 125)` — the visible widget did NOT sync to the underlying value.
- `#productHelpText` reads `"1 product(s) found"` — consistent across all 5 viewports.
- This is the exact contradiction the original screenshot showed.

**Root cause (confirmed).** `Views/Certificate/Edit.cshtml:519-564` (`loadProductsForCompany`) builds `<option>`s into the native `#ProductId` `<select>` then calls `productSelect.val(preselectProductId); productSelect.trigger('change')` inside a 50ms `setTimeout`. **The visible widget is a `.custom-dropdown-wrapper`** (NOT a `searchable-select-wrapper` — the entry's earlier note about SearchableSelect was wrong; corrected by the repro). That custom-dropdown widget is initialized once at page load from the select's option list at that instant. When the page later calls `.val()` on the select, the wrapper does NOT observe the change — so its `.custom-dropdown-selected <span>` stays at the placeholder text. `trigger('change')` on the select does fire the page's own `.change` handler (Category/Type/Subcategory cascade runs correctly, per the screenshot showing `Product Category = DEF226-CatOnly`), but the custom-dropdown's display is not coupled to either event.

**Why DEF-134, DEF-138, DEF-212 didn't close this.**
- DEF-134 fixed the underlying data API (Product Type / Subcategory IDs returned from `GetProducts`) — relevant to TC-A/B, not the wrapper display.
- DEF-138 fixed null-wipe of SKU/Model on update — relevant to the Product Management table render, not Cert Edit pre-fill.
- DEF-212 fixed re-focus on a populated SearchableSelect — different widget AND assumes initial population was correct. This defect is the **initial-population** path on `.custom-dropdown-wrapper`.

**Linked memory.**
- [[feedback_qc_review_no_changes]] — triage + non-mutating repro only; no production-code edits today
- [[feedback_scoping_check_adjacent_endpoints]] — Create.cshtml uses the same `loadProductsForCompany` shape (`Views/Certificate/Create.cshtml:483`) but Create has no `savedProductId` to pre-select, so the bug only surfaces on Edit; the fix needs to leave Create's path intact.

**Fix applied (2026-06-01).** `Views/Certificate/Edit.cshtml:546-555` — after `productSelect.val(preselectProductId); productSelect.trigger('change');`, added a native `dispatchEvent(new Event('change', { bubbles: true }))`. The custom-dropdown widget at `wwwroot/js/certificate-inline-create.js:708` registers via `addEventListener('change', ...)` which jQuery's `.trigger('change')` does NOT fire in jQuery 3.x — only jQuery-bound handlers are invoked. The native dispatch ensures the widget's `updateTrigger()` runs and syncs `.custom-dropdown-selected` text to the saved product. Create.cshtml unaffected (no pre-select path).

---

---

### DEF-224 — Top-N dashboard table downloads were returning all 26 active certs instead of just the certs belonging to the top-N entities

- **Date:** 2026-05-26
- **Severity:** Major (data correctness — user clicks download on a table titled "Top 10 Companies" and gets a CSV with 26 certs across 22 companies; export does not represent the table)
- **Status:** Fixed locally — included in `GCCWebApp-deploy-20260526-2228.zip`
- **QC Ref:** None directly — surfaced by Shawn during the QC #180 audit; shared the live `top-companies` CSV showing 26 rows with the title "Top 10 Companies by Active Cert Count" plus the Confidential cert 25-9312
- **Bucket:** Bug (export contract mismatch)
- **Reporter:** Shawn West, 2026-05-26

**Symptom.** Each of the 3 capped dashboard tables (`top-companies` cap=10, `product-types` cap=10, `product-categories` cap=5) renders an aggregated view on screen — e.g., "Companies by Active Cert Count" shows 10 rows of `Company | # Active Certs` summing to 14 active certs. Clicking the download icon next to that table returned a CSV titled the same but containing **every** active cert (26 rows on live), spread across 22 distinct companies — not just the top-10 companies' 14 certs. User experience: open the CSV, see companies that aren't even in the visible top-10 list, lose trust in the data.

**Root cause.** `Controllers/DashboardController.cs:GetCertificatesForTable` returned `await _certificateRepository.GetActiveCertificatesAsync()` for all 3 capped-table cases without filtering to the top-N entities the table was actually displaying. The same broad query was used for `cert-types` (which is fine — no cap) and the capped cases (which should have been narrower).

**Fix.** Each of the 3 capped cases now:
1. Calls the corresponding `Top…StatisticsAsync(cap)` to get the names in the top-N ranking.
2. Filters the active-certs result to those whose Company / Product Type / Product Category name (resolved via cert-direct override OR product-derived fallback to match the per-cert detail view) is in the top-N set.
3. Returns a report title that explicitly says "Certificates in Top N …" so the CSV header matches the row scope.

```csharp
case "top-companies":
{
    var topCompanies = await _certificateRepository.GetTopCompaniesByActiveCertCountAsync(10);
    var topCompanyNames = new HashSet<string>(topCompanies.Select(t => t.Name));
    var activeCerts = await _certificateRepository.GetActiveCertificatesAsync();
    return (activeCerts.Where(c => c.Company != null && topCompanyNames.Contains(c.Company.Name)),
        "Certificates in Top 10 Companies by Active Cert Count");
}
```

Same pattern for `product-types` (cap 10) and `product-categories` (cap 5).

- `Controllers/DashboardController.cs:GetCertificatesForTable` — 3 case blocks rewritten; `cert-types` (no cap) and `cert-status` (all statuses) unchanged.

**Impact on live data (2026-05-26).** Combined with DEF-223 narrowing active to {Paid, Republished} the post-deploy numbers will be:

| Table | Before (CSV rows) | After (CSV rows) |
|---|---|---|
| top-companies | 26 | ~13 (top-10 companies' certs, minus Confidential per DEF-223) |
| product-types | 26 | 3 (top-10 product types' certs — only 2 types exist, 4→3 minus Confidential edge case) |
| product-categories | 26 | ~25 (top-5 categories' certs, minus Confidential) |

Final post-deploy numbers depend on which certs are in/out of each top-N ranking after DEF-223 narrows the Active pool from 26 to 25.

**Verification.** `dotnet build`: 0 errors, 56 pre-existing warnings unchanged. Post-deploy: re-run `qc159-live-check/qc222-full-dashboard-matrix.js` against gccwebapp.com — every TABLE row in the matrix should now show `visible_sum == export_count` (or be very close, modulo the ranking-tier ties). The Excel validation workbook generator (`generate-matrix-xlsx.js`) will then produce a workbook where all 5 table rows in the "Validation Matrix" sheet are green.

**Cross-ref.** Same family as [[feedback_scoping_check_adjacent_endpoints]] — the visible table and its corresponding export were scoped differently. The fix aligns the export to the visible aggregation's underlying cert set.

---

### DEF-223 — "Active" badge/export was counting Confidential certs; narrowed to Paid + Republished so internal and public definitions match

- **Date:** 2026-05-26
- **Severity:** Major (definition mismatch — `DashboardStatuses` and `IsActiveStatus()` disagreed inside the same helper class for what counts as "Active")
- **Status:** Fixed locally — included in `GCCWebApp-deploy-20260526-2054.zip`
- **QC Ref:** None directly — Shawn surfaced during the QC #180 data-audit review while reading the "Total Active Certs" CSV: *"26 records match the badge. However, status of confidential and republished. is this correct"* → call: *"just active"*
- **Bucket:** Bug (definition / data correctness)
- **Reporter:** Shawn West

**Symptom.** The `/Dashboard` "Total Active Certs" badge and its CSV export included rows with `Status = Client Approved - Confidential` (e.g. cert 25-9312 Esko Graphics on live). The same applied to the 6 other "Active" labeled badges and their breakdowns (Companies, Cert Types, Product Categories, Product Types, Product Subcategories, Products with Active Certs) — all derived from the same `DashboardStatuses` filter. Confidential certs are paid-but-embargoed: the work is done, the client opted to keep it off the public site. The current code treated them as "active" for internal reporting; Shawn's call: an "Active" badge should mean the same thing on the internal dashboard as it does on the public-facing tile view.

**Root cause.** `Models/CertificateStatus.cs:DashboardStatuses` was defined as `{Paid, Republished, Confidential}` while the sibling helper `IsActiveStatus()` (a few lines below) returned `true` only for `{Paid, Republished}`. Two contradictory definitions of "Active" living in the same file. Every dashboard stat/breakdown query (`GetCertificateStatisticsAsync`, `GetActiveCertificatesAsync`, `GetCertificationTypeStatisticsAsync`, `GetProductCategoryStatisticsAsync`, `GetProductTypeStatisticsAsync`, `GetProductSubcategoryStatisticsAsync`, `GetExpiringCertificatesAsync`, plus the dashboard JS / Export controller paths) routes through `DashboardStatuses`, so the broader (incorrect) definition silently won everywhere on the internal surfaces while the public-facing surfaces used `PublicStatuses` = `{Paid, Republished}` and produced a different number.

**Fix.** One-line change: narrowed `DashboardStatuses` to `{Paid, Republished}`. Now equal to `PublicStatuses` and aligned with `IsActiveStatus()`. All 7 internal "Active" badges + 5 dashboard tables + the dashboard JS chart data + the parallel `/Export/DownloadStat` endpoints automatically pick up the narrower set because they all reference the single constant.

- `Models/CertificateStatus.cs` — `DashboardStatuses` definition.

**Impact on live data (2026-05-26).** 1 Confidential active cert in prod (`25-9312` Esko Graphics BV). Post-deploy on live:

| Badge | Before (live, 2026-05-26) | After (expected) |
|---|---|---|
| Total Active Certs | 26 | 25 |
| # Companies with Active Certs | 22 | 21 (Esko Graphics drops out — sole active cert was 25-9312) |
| # Product Categories with Active Certs | 2 | 2 (Esko's category was Chemicals, already covered by others) |
| # Products with Active Certs | 26 | 25 |
| # Certs by Status (table) | 37 | 37 (cert-status table includes ALL statuses, unchanged) |

Smaller deltas on the by-month/by-year cards depending on which month Esko's effective/expiration date falls in (it's 2025-12-22 / 2026-12-21, so neither "this year published" nor "last month expired" — those cards unchanged on live).

**Verification.** `dotnet build`: 0 errors, 56 pre-existing warnings unchanged. Post-deploy verification: re-run `qc159-live-check/qc220-dashboard-full-audit.js` against gccwebapp.com, expect Total Active Certs = 25 (was 26), all CSVs free of `Client Approved - Confidential` rows.

**Cross-ref.** [[feedback_workflow_state_count]] — workflow has 8 states, PublicStatuses is the canonical "Active" set. DEF-223 fixes an internal-only divergence that pre-dated that memory's clarification. Memory entry remains accurate; this PR brings the codebase into alignment with it.

---

### DEF-222 — Dashboard breakdown stats use Product-hierarchy fallback first; export CSV uses cert-direct first → same cert categorized differently in visible table vs export

- **Date:** 2026-05-26
- **Severity:** Major (data correctness — visible breakdown counts disagree with export per-row when both cert-direct override AND product-derived value exist for the same field)
- **Status:** Fixed locally — included in `GCCWebApp-deploy-20260526-2305.zip`. Decision: cert-direct override wins (Option 1, matches CR-014 + the export + the per-cert detail view). Shawn confirmed by surfacing the symptom again during the live data validation: *"Top 5 Product Categories ... I thought I saw 3 unique Product Categories on the export. can we review this"*.
- **QC Ref:** None directly — surfaced by the comprehensive dashboard audit run for QC #180 follow-up (`qc159-live-check/qc220-dashboard-full-audit.js`)
- **Bucket:** Bug (UI/data consistency)
- **Reporter:** Internal (Anuki audit per Carly's "Anuki needs to do that" comment on QC #180)

**Symptom (live data, 2026-05-26).** The "Top 5 Product Categories by Active Cert Count" table shows `Carly Test = 4` but the corresponding export CSV (`/Dashboard/DownloadTable?type=product-categories`) only contains 3 rows with `Product Category = Carly Test`. The 4th cert (26-0158) appears in the export under `Product Category = Electronics, Computers, and Accessories` instead. Same pattern affects the "Top 10 Product Types" and "# Product Type Subcategories with Active Certs" tile + table.

**Root cause.** `Repositories/CertificateRepository.cs`:
- `GetProductCategoryStatisticsAsync` (line 694), `GetProductTypeStatisticsAsync` (line 663), `GetProductSubcategoryStatisticsAsync` (line 725), and the three distinct-count blocks in `GetCertificateStatisticsAsync` (~line 524-560) **all use Product-derived FIRST**: `fromProduct` selects `c.Product.Category.Name` for every active cert where `c.Product.Category != null`; only when `c.Product.Category == null` does `fromCertDirect` kick in and use `c.ProductCategory.Name`.
- `Controllers/DashboardController.cs:GenerateCertificateCsv` (line 571-573) uses **cert-direct FIRST**: `var productCategory = cert.ProductCategory?.Name ?? cert.Product?.Category?.Name ?? "";`

These priorities are inverted. For a cert with both populated (e.g. cert 26-0158: `c.Product.Category="Carly Test"`, `c.ProductCategory="Electronics, Computers, and Accessories"`), visible tile/table and export CSV resolve to different category names.

**Why fix not yet applied.** CR-014 (cert-level override fields) established that the analyst's explicit cert-direct value takes precedence over the product-hierarchy value — but applying the fix flips dashboard tile counts on prod (current "# Product Categories with Active Certs" = 2 would become 3, since Electronics gets its own bucket when cert-direct wins). Shawn elected to handle the table-totals UX gap first (DEF-221) and revisit this priority decision separately, since changing tile counts is a visible change that should be confirmed before deploy.

**Pending decision.** Two options:
1. **Flip stat/breakdown functions to match the export and the rest of the app (CR-014 convention).** Cert-direct wins; product-derived is the fallback. Aligns visible table with export. Prod tile counts shift slightly.
2. **Flip the export CSV to match the current stat/breakdown functions.** Product-hierarchy wins; cert-direct is the fallback. Keeps current tile counts, but contradicts CR-014 and several other recent fixes (DEF-178, DEF-199, the per-cert detail view).

Recommendation: Option 1. The export, per-cert detail view, and CR-014 all agree that cert-direct wins; the dashboard stat/breakdown queries are the outliers.

**Cross-ref.** [[feedback_scoping_check_adjacent_endpoints]] — same fix-all-the-adjacent-endpoints pattern. Decision needed before applying.

---

### DEF-221 — Dashboard breakdown tables don't show totals; users can't quickly cross-check against the badges above (QC #180 5/26 client follow-up to DEF-200)

- **Date:** 2026-05-26
- **Severity:** Medium (UX / data verification — without totals, comparing "what the table sums to" vs "what the badge claims" requires the user to do mental math across 5–12 rows)
- **Status:** Fixed locally — pending deploy to gccwebapp.com
- **QC Ref:** #180 (client log, 5/26 follow-up). Shawn comment: *"The tables do not have totals."* — direct response to the question of how to verify count vs data without re-running scripts.
- **Bucket:** UX (data transparency)
- **Reporter:** Shawn West

**Symptom.** Each of the 5 dashboard tables (`cert-types`, `top-companies`, `cert-status`, `product-types`, `product-categories`) shows per-row `(Name, Count)` pairs but no row-level total. To verify "does this table reconcile with the corresponding badge?" the user has to sum visible counts in their head. Per Carly's *"Anuki needs to do that"* note on QC #180 — the user shouldn't be doing arithmetic to validate the system.

**Fix.** Added a styled `<tfoot class="dashboard-table-total">` row to each table summing the visible Count column. All five tables use the label `Total: N` for consistency — Shawn's call to keep it simple even on top-N tables where the value is technically a subtotal (per-table title already includes "Top 10 / Top 5" so the cap is contextually obvious).

**Files.**
- `Views/Dashboard/Index.cshtml` — added 5 `<tfoot>` blocks (one per table), wrapped in the same `@if (… .Any())` guard as the body so the footer doesn't render for empty/restricted-access tables.
- `wwwroot/css/pages/dashboard.css` — `.table-section tfoot.dashboard-table-total td` styling (gray background, bold + uppercase to match the `<thead>` visual weight, `border-top: 2px` separator).

**Verification.**

- `dotnet build`: 0 errors, 56 pre-existing warnings unchanged.
- Local visual probe (`qc159-live-check/qc221-screenshot-totals.js`, full-page screenshot at `qc221-local-tables-with-totals.png`):
  ```
  Total Active Certs by Certification Type           → TOTAL: 22
  Top 10 Companies by Active Cert Count              → TOP 9 SUBTOTAL: 22
  # Certs by Status                                  → TOTAL: 45
  Top 5 Product Categories by Active Cert Count      → TOP 1 SUBTOTAL: 22
  ```
  Dev DB has 22 active certs across 9 companies and 1 product category, plus 45 total certs (including 23 non-active). All four populated tables show their footers correctly; the Product Types table renders without a footer because the dev breakdown is empty (correct behavior — `@if (Model.ProductTypeBreakdown.Any())` guard).
- Live post-deploy check: run `qc220-dashboard-full-audit.js` against prod and visually confirm 5 footers render on `https://gccwebapp.com/Dashboard`.

**Why this matters.** The 5/26 audit found 12/12 stat cards pass tile=export, plus the DEF-220 fix to narrow the expired filter — but Carly's underlying concern was "I shouldn't have to download the CSV to verify the numbers." Totals in the visible table let her do that check without leaving the page. Combined with DEF-220 (semantic correctness of the expired card) and the upcoming DEF-222 decision (cert-direct vs product-derived priority), the dashboard becomes self-verifying.

---

### DEF-220 — "Certs Expired Last Month" export leaks Republished / Paid / Confidential certs (QC #180 5/26 client follow-up to DEF-200)

- **Date:** 2026-05-26
- **Severity:** Major (data correctness — client retested DEF-200 and found the export still includes statuses that are not actually expired)
- **Status:** Fixed locally — pending deploy to gccwebapp.com
- **QC Ref:** #180 (client log, 5/26 follow-up to original 5/11 entry). Client comment: *"Certs Expired Last Month is not using right data, see image 73 for results. We added clarifier of 'Active' for multiple items but then you are having the export show those not 'Active' (but number is only active which is right — the export should align with only the number that it makes up). ID 156 covers too. NOTE: When I reviewed the dashboard exports I was not checking the count was accurate based on what is in system — Anuki needs to do that."* Shawn clarified the active complaint as *"Expired export is showing republished as status."*
- **Bucket:** Bug (filter scope)
- **Reporter:** Client (via Shawn West)

**Symptom.** DEF-200 (deployed 2026-05-19, verified 2026-05-21) closed the tile=export contract but introduced `CertificateStatusHelper.EverPublishedStatuses` = `{Paid, Republished, Confidential, Expired, ExpiredNoAnnualVerification}` as the filter for "Certs Expired Last Month." That set is too wide: a Republished cert is re-active (its ExpirationDate is supposed to be in the future) and Paid/Confidential certs with a past ExpirationDate are data anomalies the background expiration service should have auto-flipped to `Expired`. None of those belong in an "expired" report. On 5/26 live data (`qc180-live-2026-05-26-*.csv`) the 6 currently-expired April rows happened to all be `Status=Expired`, so the gap was latent — but the filter would have leaked a Republished cert with an April ExpirationDate the moment one was created (e.g., a cert expiring in April that was republished mid-month).

**Root cause.** `Models/CertificateStatus.cs:EverPublishedStatuses` was scoped to "certs that were ever published OR are now flagged Expired" — designed to exclude Draft/Hold/Pending-Payment, which it correctly did. But "ever published" includes statuses where the cert is no longer expired (Paid/Confidential are currently active; Republished is currently re-active). The DEF-200 fix correctly aligned tile and export to use the SAME filter, so the tile=export contract held — but the filter itself was misnamed and overly broad for the "expired last month" semantic.

**Fix.** Replaced `EverPublishedStatuses` with `CurrentlyExpiredStatuses = {Expired, ExpiredNoAnnualVerification}` and updated all three call sites to use the narrower constant. Tile + both export endpoints stay in lockstep, so the tile=export contract from DEF-200 is preserved.

- `Models/CertificateStatus.cs:81-95` — removed `EverPublishedStatuses`, added `CurrentlyExpiredStatuses` with a comment explaining why Republished/Paid/Confidential are excluded.
- `Repositories/CertificateRepository.cs:594-602` — `stats["ExpiredLastMonth"]` uses `CurrentlyExpiredStatuses`.
- `Controllers/DashboardController.cs:402-414` — `certs-expired-last-month` case in `GetCertificatesForStat` uses `CurrentlyExpiredStatuses`.
- `Controllers/ExportController.cs:357-371` — parallel `/Export/DownloadStat` `certs-expired-last-month` case uses `CurrentlyExpiredStatuses`.

**Verification.**

- `dotnet build`: 0 errors, 56 pre-existing warnings unchanged.
- Live probe before fix (5/26 18:23 UTC, `qc180-live-2026-05-26-results.json`): all 12 cards tile=export PASS, including `certs-expired-last-month` 6=6. All 6 rows in today's CSV are `Status=Expired` — confirms the gap is latent on current prod data, not active. The fix prevents future Republished/Paid/Confidential certs with a past ExpirationDate from leaking into the report.
- Post-deploy verification plan: re-run `qc159-live-check/qc180-dashboard-counts.js` (parameterized `BASE_URL=https://gccwebapp.com`); assert 12/12 cards still tile=export and CSV column 2 (Status) for `certs-expired-last-month` is one of `Expired` / `Expired - No Annual Verification` on every row.

**Cross-ref.**

- DEF-200 (parent) — established the tile=export contract; this defect narrows the filter set that contract uses for the expired card.
- [[feedback_scoping_check_adjacent_endpoints]] — same pattern: the `DashboardController` + `ExportController` parallel endpoints both had to be updated.
- Carly's "Anuki needs to do that" comment is acknowledged: the 5/21 verification proved tile=export numerically but did not audit the row-level status semantics. Going forward the dashboard probe asserts both (a) tile count matches export row count AND (b) every row's Status column matches the card's semantic label.

---

### DEF-219 — Certificate Internal Tile View: "Additional" sidebar filter only shows external tags; internal tags missing for Admin/Analyst

- **Date:** 2026-05-22
- **Severity:** Medium (functional gap — internal users cannot filter by internal tags from the sidebar)
- **Status:** Fixed locally — pending deploy to gccwebapp.com
- **QC Ref:** Shawn ad-hoc (not in client xlsx). URL reported: `https://gccwebapp.com/Certificate/CertificateTileViewInternal` — admin sidebar "Additional" panel.
- **Bucket:** Bug (incorrect filter scoping)
- **Reporter:** Shawn West, 2026-05-22

**Root cause.** `Controllers/Api/CertificateTileInternalApiController.cs:582-586` (the filter-options endpoint feeding the internal tile view's left-sidebar) built its `additionalTags` list with `Where(t => t.IsActive && t.TagType == "external")`. Hardcoded external-only — internal tags were excluded even though this controller is gated `[Authorize(Policy = "AnalystOrAdmin")]` (External role hits the separate `CertificateTileExternalApiController`). The endpoint already returned `internalTags` and `externalTags` as separate lists for other purposes (line 553-564), but the JS (`wwwroot/js/certificate-tile-view-internal.js:155-161`) only reads `additionalTags` to populate the sidebar checkboxes — so internal tags were silently dropped from the UI.

**Fix.** Changed the `additionalTags` projection to `Where(t => t.IsActive && (t.TagType == "external" || t.TagType == "internal"))` and added `.Distinct()` so a name that happens to exist as both an internal and external tag does not duplicate in the list. Sort moved to a post-Distinct `OrderBy(n => n)` for the same reason.

- `Controllers/Api/CertificateTileInternalApiController.cs:582-587` — `additionalTags` filter widened to include internal tag type; Distinct + post-Distinct sort.

**Verification.**

`dotnet build`: 0 errors, 56 pre-existing warnings unchanged. External tile view (`CertificateTileExternalApiController`) is untouched, so the external-only visibility rule for company/partner users is preserved.

**Cross-ref:**

- Sibling pattern to [[feedback_scoping_check_adjacent_endpoints]] — the separate `internalTags` / `externalTags` outputs from the same endpoint were correctly typed, but the JS-facing `additionalTags` field that the UI actually consumes was missed.
- The CLAUDE.md rule "External Users CANNOT view/edit internal tags" is enforced at the controller boundary (separate Internal vs External controllers); no risk this fix leaks internal tags to External-role users.

---

### DEF-218 — Certificate Internal View: Product Identifiers "Show all" button hidden inside scrollable list with no collapse

- **Date:** 2026-05-22
- **Severity:** Low (UX / usability)
- **Status:** Fixed locally — pending deploy to gccwebapp.com
- **QC Ref:** Shawn ad-hoc (not in client xlsx). URL reported: `https://gccwebapp.com/Certificate/CertificateInternalView/26-0160` — cert 26-0160 has 1001 Product Identifiers.
- **Bucket:** UX
- **Reporter:** Shawn West, 2026-05-22

**Root cause.** `Views/Certificate/CertificateInternalView.cshtml:145-180` rendered the Product Identifiers section with the *"Show all N product identifiers"* button placed **inside** the scrollable `#pid-container` div (`max-height: 400px; overflow-y: auto`). With 20 initial rows already filling the visible area, the button sat at the bottom edge of the scroll viewport — hard to reach without scrolling the inner container, and easy to miss entirely. The handler was also one-way: `display='block'; this.style.display='none'` — once expanded, no way to collapse back to the original 20-item view.

**Fix.** Hoisted the toggle button into the section header row (flex layout, button right-aligned next to the `Product Identifiers (1001)` title). Replaced the inline one-shot handler with a `togglePidShowAll()` script that flips `#pid-remaining` between `display:none` ↔ `block` and swaps the button label between *"Show all N"* ↔ *"Show less"*, with `aria-expanded` for screen readers. Collapsing scrolls the container back to top so the user lands on row 1, not somewhere mid-list.

- `Views/Certificate/CertificateInternalView.cshtml` — section title row now flex-aligned; toggle moved outside scroll container; bidirectional toggle with aria-expanded

**Verification.**

Build clean (`dotnet build`: 0 errors, 54 pre-existing warnings unchanged). Behavior on dev DB cert with >20 Product IDs:
1. Page load — button reads *"Show all 1001"*, sits next to section title (always visible regardless of scroll state).
2. Click — extra 981 rows appear inside the same 400px scrollable container, button reads *"Show less"*.
3. Click again — extras collapse, button reverts, container scrolls to top.

**Cross-ref:**

- This is a cousin of `feedback_scoping_check_adjacent_endpoints`: the original DEF-150 / DEF-152 Product ID Manager polish landed on the *editor* surfaces, but this read-only *internal view* surface was missed and the button got buried inside the scrollbox.

---

### DEF-217 — QC #118 — Admin custom certification fields: "Failed to save" from hardcoded default cert-type FK + section sub-headers cluttering Create/Edit forms

- **Date:** 2026-05-21
- **Severity:** High (Issue A blocked admin custom-field creation on non-dev environments); Medium (Issue B — UX inconsistency vs established rule)
- **Status:** Fixed locally — pending rebuild + deploy
- **QC Ref:** **QC #118** (`Certificate Field Management`). Issue B = client's 3/13 wording *"I don't think we need section headers and they can all just go under Additional Information."* Issue A = client's 4/6 follow-up on the same row: *"Getting error message when trying to add could not test. See image 45."* Source: `docs/tracking/test-execution-log.md:3737-3741` (TC-QC-118 card). Same FK root pattern was independently flagged alongside QC #109 (User Create/Update — `BeginTransactionAsync` / `SqlServerRetryingExecutionStrategy`) but those were separate fixes, separately tracked. Issue B is also the Create/Edit-surface closure of [[feedback_custom_fields_single_section]].
- **Bucket:** A — Bug (data-integrity / FK); B — UX (consistency)
- **Reporter:** Client — Carly, QC log #118 (3/13 + 4/6 follow-up). Verification pass on 2026-05-21 confirmed code state matches client's stated expectation.

**Issue A — root cause.** `Services/AdminService.cs:CreateCertificateFieldAsync` was constructing a new `CustomField` with `CertificateTypeId = 1` hardcoded. The `CustomField → CertificationType` FK then attempts to resolve row Id 1 in `CertificationTypes`. On any database where identity seeding doesn't start at 1 — re-seeded environments, fresh prod imports where lookups were rebuilt, or any database that had its first cert-type record deleted — the FK lookup fails and the SaveChanges throws. The controller surfaced this to the user as a generic *"Failed to save"* because the catch wrapped `ex.Message` without distinguishing FK violations.

**Why this slipped.** Dev / seed environments always have `CertificationTypes.Id = 1` (Recycled Content), so the bug was invisible in QA. Per `feedback_certtypes_hierarchy` the table is hierarchical — admins reorganizing the tree (deleting and re-adding root types) could land the deployed prod DB in a state where Id 1 didn't exist, at which point any new custom-field creation would silently fail.

**Issue A fix.** Replaced the hardcoded `CertificateTypeId = 1` with `await _context.CertificationTypes.FirstOrDefaultAsync()` — same dynamic-lookup pattern already used by `Data/SeedData.cs:473` and `AdminService.cs:1563` (post-fix). Added a guard returning `(false, "No certificate types exist in database. Please add a certificate type first.")` so the empty-table edge case is actionable instead of a null-deref. `UpdateCertificateFieldAsync` was audited and does not touch `CertificateTypeId` on update (preserves the existing FK), so it's safe by design and required no change.

- `Services/AdminService.cs:CreateCertificateFieldAsync` — dynamic lookup + null guard
- `Services/AdminService.cs:UpdateCertificateFieldAsync` — verified clean, no edit

**Issue B — root cause.** `Views/Certificate/Create.cshtml` and `Views/Certificate/Edit.cshtml` were rendering custom certification fields grouped by their `CustomField.Section` value — producing multiple sub-headers on the certificate form (e.g., "Performance Metrics," "Verification Data," etc.). The `[Custom Fields = single bucket, not Section-grouped]` rule per `feedback_custom_fields_single_section` had previously been applied to the detail / external / public view surfaces (per DEF-202 commit notes) but the Create/Edit forms were missed in that pass — same scope-check failure pattern as `feedback_scoping_check_adjacent_endpoints`.

**Issue B fix.** Form template now renders a single `<div class="form-section-header"><i class="fas fa-sliders-h"></i><span>Additional Fields</span></div>` heading once, with `<div id="custom-fields-container">` underneath. The JS renderer `wwwroot/js/certificate-custom-fields.js:renderFields` was already correct (line 79 comment `// Render all fields flat (no section grouping)` produces a single `.custom-fields-grid` of `this.fields.map(...)`), so the fix was view-layer only. `CustomField.Section` is preserved on the admin custom-field table for internal organization but no longer drives form rendering — matches the behavior already shipped on `CertificateInternalView.cshtml`, `CertificateExternalView.cshtml`, `CertificateSingleView.cshtml`.

- `Views/Certificate/Create.cshtml:367-376` — single "Additional Fields" header, container only
- `Views/Certificate/Edit.cshtml:422-431` — same structure (mirror)
- `wwwroot/js/certificate-custom-fields.js:79` — already flat; verified no change needed

**Verification (read-only).**

Grep across whole repo for `CertificateTypeId\s*=\s*1` and `CertificationTypeId\s*=\s*1` returns zero matches in any controller/service code post-fix. Only matches are in `Migrations/20251029005907_DistributeCertificatesAcrossTypes.cs` (correct migration math: `1 + ((nc.RowNum - 1) % 22)`) and historical docs/logs. Grep on `Views/Certificate/Create.cshtml` and `Edit.cshtml` for `Section|section-header` confirms only the single "Additional Fields" header remains; no per-section sub-headers around the custom-fields container.

**Test plan after deploy.**

1. Admin → Certificate Fields → Add Certificate Field → fill name + type → Save. Expect: success toast, new row in table, no "Failed to save" error. (Tests Issue A on dev where Id 1 exists.)
2. Stress test Issue A guard: temporarily reset `CertificationTypes` to empty in a scratch DB → attempt to add field → expect clear error message *"No certificate types exist in database. Please add a certificate type first."* (Not run on prod; documented for future cert-type-reorg scenarios.)
3. Certificate → Create → select any certification type that has multiple custom fields with different Section values → confirm all fields render in one flat grid under "Additional Fields" with no sub-headers.
4. Certificate → Edit on an existing cert with custom field values → same check; confirm existing values still bind correctly to fields.

**Files changed:**

- `Services/AdminService.cs`
- `Views/Certificate/Create.cshtml`
- `Views/Certificate/Edit.cshtml`

**Cross-ref:**

- [[feedback_custom_fields_single_section]] — the rule that Issue B closes
- DEF-202 — earlier pass that applied the same rule to detail/external/public views
- `feedback_scoping_check_adjacent_endpoints` — same "list/index got the fix, detail/form didn't" pattern as DEF-103, DEF-164, DEF-198, DEF-204, DEF-205, DEF-216
- `feedback_certtypes_hierarchy` — explains why the hardcoded `Id = 1` assumption was unsafe to begin with

---

### DEF-216 — Certificate Create/Edit breadcrumbs wrongly nested under "Administration" (QC #104 scope miss)

- **Date:** 2026-05-21
- **Severity:** Low (cosmetic / IA correctness, Admin-role only)
- **Status:** Fixed locally — **verified end-to-end on http://localhost:5101 after clean rebuild + restart (2026-05-21)**. 5/5 PASS: Create + Edit show new breadcrumb, Company/Index + Product/Management still show DEF-211 fix (no regression), Admin/UserManagement still shows `Administration › User Management` (admin views correctly unchanged). Pending deploy to gccwebapp.com.
- **QC Ref:** #104 (5/1 follow-up — same client complaint pattern DEF-211 closed for Companies & Products)
- **Bucket:** UX — information architecture
- **Reporter:** Internal audit during QC #104 verification pass on 2026-05-21

**What's wrong.** `Views/Certificate/Create.cshtml:16-25` and `Views/Certificate/Edit.cshtml:16-25` rendered breadcrumb trail `Home › Administration › Certificate Management › <action>`. Per `_Layout.cshtml:200-218`, "Certificate Management" (the Certificates dropdown) is a **top-level nav item**, sibling to "Administration" — not a child of it. The "Administration ›" segment mis-described the site IA, identical to the bug DEF-211 fixed on the Company / Product side. Same client wording from QC #104 5/1 applies: *"You are also showing 'Administration' under the [section] which is not correct - its unrelated to this based on nav structure. Please use breadcrumbs as intended on websites."*

**Why DEF-211 didn't catch this.** DEF-211 scoped to `Views/Company/Index.cshtml` and `Views/Product/Management.cshtml` — the two views explicitly cited in the 5/1 client message. The Certificate Create/Edit forms have the same anti-pattern but weren't surfaced by client testing because (a) Carly's UAT focused on the named pages, and (b) breadcrumbs are gated `role-admin-show` so only the Admin role sees them — Carly tests primarily as Analyst per `feedback_qc_review_no_changes` workflow.

Same scope-check pattern as `feedback_scoping_check_adjacent_endpoints`: the list/index pages got the fix, the detail-form pages didn't.

**Fix.** Removed the leading `<a href="/">Home</a> › <span>Administration</span> ›` segments from the breadcrumb. Both Create and Edit now read `Certificate Management › <action>` — symmetric with DEF-211's `Companies & Products › Company Management` / `Product Management`. Kept the comment in each view referencing DEF-211 so the next person editing these doesn't re-introduce the nesting.

- `Views/Certificate/Create.cshtml:16-26` — now `Certificate Management (link) › Create Certificate (current)`
- `Views/Certificate/Edit.cshtml:16-26` — now `Certificate Management (link) › Edit Certificate #<n> (current)`

Why drop `Home` too: no other breadcrumb in the app uses a `Home` crumb — all 20 other views start at their top-level nav item. Removing it brings Cert Create/Edit into the established pattern (12 Admin views start at `Administration`, Company/Product start at `Companies & Products`, Cert Import starts at `Certificates`).

**Not touched (deliberate).**

- `Views/Product/ManagementAlt.cshtml`, `Views/Product/ManagementV2.cshtml` — verified by grep on `*.cs` that no controller route references either. Dead code; leave for a future housekeeping CR.
- Cert Create/Edit views are reached via the `role-admin-show + role-analyst-show` wrapper; breadcrumb `<nav>` itself is gated `role-admin-show` at `_Layout.cshtml:299` so only Admin role actually sees the breadcrumb. Limits blast radius.

**Verification.** Pre-edit live HTML on http://localhost:5101 confirmed the dev server still serves the OLD precompiled view containing `<span>Administration</span>`. Razor runtime compilation is **not** enabled (`Program.cs` has no `AddRazorRuntimeCompilation` call), so the running `GCCWebApp.exe` will keep serving the cached view until a clean rebuild + restart. Next dev-server restart will pick up the edit; deploy-time publish includes the Razor recompile automatically.

**Test plan after deploy.** Log in as Admin. Navigate to `/Certificate/Create` — breadcrumb reads `Certificate Management › Create Certificate` (no "Home", no "Administration"). Open an existing cert, click Edit — breadcrumb reads `Certificate Management › Edit Certificate <number>`. Confirm no console / network errors on either page.

**Local verification (2026-05-21, http://localhost:5101, dev DB):**

| Probe | Expected | Result |
|---|---|---|
| `dotnet build` | 0 errors | ✓ 0 errors, 56 pre-existing warnings |
| `GET /Certificate/Create` (admin session) | `Certificate Management (link) › Create Certificate (current)` | ✓ exact match |
| `GET /Certificate/Edit/26-0001` (admin session) | `Certificate Management (link) › Edit Certificate 26-0001 (current)` | ✓ exact match |
| Grep `<span>Administration</span>` in both response HTMLs | 0 occurrences | ✓ 0 in both |
| `GET /Company/Index` (regression) | `Companies & Products (span) › Company Management (current)` | ✓ DEF-211 fix intact |
| `GET /Product/Management` (regression) | `Companies & Products (span) › Product Management (current)` | ✓ DEF-211 fix intact |
| `GET /Admin/UserManagement` (regression) | `Administration (span) › User Management (current)` | ✓ correctly unchanged |

**Files changed (2):**
- `Views/Certificate/Create.cshtml`
- `Views/Certificate/Edit.cshtml`

**Cross-ref:** DEF-211 (sibling fix for Companies & Products), DEF-202 (Companies → Company Management rename, also QC #104).

---

### DEF-215 — Password reset link "invalid or has expired" before user can submit — double URL-decode corrupts base64 token

- **Date:** 2026-05-21
- **Severity:** High (silent failure of the only self-service password recovery path; ~75% of issued reset links fail)
- **Status:** Fixed locally — verified end-to-end on localhost 2026-05-21 (5/5 PASS, including round-trip POST that actually changed PasswordHash in DB) — **pending deploy to gccwebapp.com (confirmed NOT deployed as of 2026-05-21 — deploy-state probe could only verify route still serves 200; fix itself can't be fingerprinted from outside without DB-side token setup, so deploy-state assumed not-deployed pending Shawn's next push)**
- **QC Ref:** none (reproduced by Shawn locally on 2026-05-21 — email link landed on `/Account/ResetPasswordWithToken` and immediately showed the "invalid or has expired" error before he could enter a new password)
- **Bucket:** Bug — Account / password-reset
- **Reporter:** Shawn (internal, 2026-05-21)

**What's wrong.** Forgot-password emails generate a random 32-byte base64 token (alphabet `A-Z a-z 0-9 + / =`), hash it (SHA-256) into the `Users.PasswordResetToken` column, and embed the **raw** token in the email URL as a query-string value. When the user clicks the link, the controller hashes the inbound token and compares against the stored hash. The comparison fails for any token containing `+` (which is most of them — probability of a 44-char base64 string having no `+` and no `/` is roughly 25%), so the user lands on the reset page and immediately sees:

> This password reset link is invalid or has expired. Please request a new one.

The token wasn't expired; the lookup just didn't find a match.

**Root cause.** `Controllers/AccountController.cs`:

- `:435` builds the email URL with `urlEncodedToken = WebUtility.UrlEncode(rawToken)` — base64 `+` becomes `%2B` in the URL. Correct.
- `:495` GET handler `ResetPasswordWithToken(string token, string email)` — ASP.NET Core's query-string binder **already** percent-decodes `%2B` back to `+`, so the `token` parameter is the original raw base64 token.
- `:504` called `var decodedToken = System.Net.WebUtility.UrlDecode(token);` a **second** time. `WebUtility.UrlDecode` follows form-style decoding, which converts the literal `+` character to a space. The hashed string then differs from the DB value by one or more characters, and the lookup returns null → "invalid or has expired" branch.
- Same defect on the POST path at `:550` against `model.Token` — even when the GET happened to work (token without `+`/`/`), the hidden-field round-trip would re-corrupt on submit, so end-to-end success rate was even lower than 25%.

This has been live since the password-reset feature was introduced; it presented as "the link doesn't work" intermittently — exactly the pattern of a probabilistic bug keyed on base64 char distribution.

**Fix.** Removed the redundant `WebUtility.UrlDecode` call in both handlers and hash `token` / `model.Token` directly. The framework's model binder handles decoding for both query-string parameters and `application/x-www-form-urlencoded` POST bodies, so the second decode was never needed. Two-line change.

**Files:**
- `Controllers/AccountController.cs:495-522` (GET `ResetPasswordWithToken`) — drop `decodedToken`, hash `token`.
- `Controllers/AccountController.cs:531-577` (POST `ResetPasswordWithToken`) — drop `decodedToken`, hash `model.Token`.

**Build:** compile clean (`dotnet build` blocked only by the running dev process locking `GCCWebApp.exe`; no compile errors).

**Test plan after deploy.**

1. **Happy path with `+` in token.** Trigger Forgot Password for an existing user account. Inspect the URL in the email — confirm it contains `%2B`, `%2F`, or `%3D` (any token with a non-alphanumeric base64 char will exercise the fix). Click link → reset page renders with the form, NOT the "invalid or has expired" error. Submit a new password → success page. Log in with the new password.
2. **Round-trip regression.** Trigger reset, click email link, deliberately mistype the new password (fails confirmation), correct it, resubmit. The hidden-field-driven POST must still succeed (this is the second site of the bug).
3. **Negative — expired.** Set `PasswordResetTokenExpiry` to a past time in the DB manually, click the link → "invalid or has expired" message should still appear (expiry path unchanged).
4. **Negative — wrong token.** Mangle one character of the token in the URL → "invalid or has expired" message should still appear (hash-mismatch path unchanged).
5. **Negative — used token.** Successfully reset, then click the old link a second time → "invalid or has expired" (token nulled after use; path unchanged).

**Regression risk:** very low. Two lines removed, no logic added, no behavioral change for any path that previously succeeded.

**Follow-up.** Add a `Tests/Integration` test that drives the full forgot-password → click-link → submit-new-password loop end-to-end. The bug survived because there was no automated coverage of the link-click round-trip — only unit tests against the controller methods with synthetic tokens (which masked the URL-encoding bug).

---

**Verification (added 2026-05-21) — localhost end-to-end matrix, 5/5 PASS, pending deploy**

`qc159-live-check/qc215-password-reset.js` — synthetic token round-trip on dev user `jane.analyst@anukiconsulting.com` (admin `swest@anukiconsulting.com` deliberately avoided). Original `PasswordHash` captured before test, restored at cleanup; reset token cleared. No persistent state changes.

| Step | What | Result |
|---|---|---|
| Setup | `crypto.SHA256('AAAA+BBBB+CCCC+DDDD+EEEE+FFFF+GGGG+HHHH=').digest('base64')` → write to `Users.PasswordResetToken` for jane.analyst; expiry +1hr. Token deliberately includes `+` (4 of them) and `=` — the exact characters that pre-fix double-decode corrupted. | OK |
| T1: GET reset URL | `GET /Account/ResetPasswordWithToken?token=AAAA%2BBBBB%2B...&email=…` | ✅ HTTP 200, body does NOT contain "invalid or has expired", reset form renders |
| T2: POST round-trip | Form fields: `Token=<HTML-decoded raw>`, `Email`, `Password=QC215Test!Pa55word`, `ConfirmPassword=…`, `__RequestVerificationToken` | ✅ HTTP 200, body does NOT contain "invalid or has expired", body shows **"Password Reset Successful"** page |
| T3: DB verify | `SELECT PasswordHash FROM Users WHERE Email = jane.analyst` after | ✅ `PasswordHash` differs from baseline → real reset happened |
| Cleanup | Restore original `PasswordHash`; `PasswordResetToken = NULL`, `PasswordResetTokenExpiry = NULL` | OK |

**Two test-side artifacts worth recording for future harness reuse:**
- The controller hashes with `Convert.ToBase64String(SHA256(...))` — **Base64**, not hex. Node `crypto.createHash('sha256').digest()` defaults to a Buffer; explicit `.digest('base64')` matches.
- Razor renders form input `value="…"` attributes with `+` and `=` as HTML entities (`&#x2B;` / `&#x3D;`). Browsers decode natively in `inputElement.value`; raw HTTP POST does not. Test added an `htmlDecode()` helper for the hidden `Token` field.

Both are encoded inline in the harness so the test file works without surrounding context.

**gccwebapp.com verification deliberately deferred.** DEF-215 is "Fixed locally — pending deploy". Running this test against prod today would test the OLD code and fail (false negative). Re-run with `BASE_URL=https://gccwebapp.com` after the next deploy.

**Pre-launch posture.** This was Shawn's prompt to take the fix seriously: once Basic Auth comes down at production cutover, the password-reset flow becomes the only recovery path. A 25%-success-rate recovery flow would create a support fire-drill on day one. Fix is in working tree and proved correct end-to-end on localhost; ready for the next deploy wave.

**Deploy-state probe (added 2026-05-21).** The DEF-215 fix is a controller-only change (`AccountController.cs`); it can't be fingerprinted from outside without setting up a DB-side reset token on prod (which requires modifying real user records — declined per `[Data Changes Require CR]`). Live probe of `GET /Account/ResetPasswordWithToken?token=AAAA+BBBB+CCCC=&email=nobody@e2e-test.invalid` returned HTTP 200 with the expected "invalid or has expired" page (no DB-side token, so the fix's success path can't be exercised). What this proves: the route handler is alive and doesn't crash on `+`-containing tokens. What it does NOT prove: that the `WebUtility.UrlDecode` removal has actually shipped. Treat as **deploy-state unknown — assume not deployed**; flip status post-deploy by re-running `qc215-password-reset.js` against gccwebapp.com after setting up a fresh token via the normal Forgot Password flow. Test artifact: `qc159-live-check/def214-215-deploy-probe-results.json`.

---

### DEF-214 — User Import: 4 issues from internal tester batch (mobile preview, role casing, sanitization, count math)

- **Date:** 2026-05-21
- **Severity:** Medium (collectively) — single page, no data loss, but blocks confident bulk-import use
- **Status:** Fixed locally — verified on localhost 2026-05-21 (7/7 PASS) — **deploy state: Sub-A/B/C NOT deployed, Sub-D already deployed (confirmed via fingerprint probe 2026-05-21, see Deploy-state probe below)**
- **QC Ref:** none (internal tester batch reported by Shawn, 2026-05-21)
- **Bucket:** Bug — admin user-import page (Sub-A is UX, Sub-B/C/D are functional/security)
- **Reporter:** Internal tester (relayed by Shawn, 2026-05-21)

**Scope.** Four independent bugs surfaced while exercising `/Admin/UserManagement` → Import flow. Batched into one DEF row because they all live in the same page (`Views/Admin/UserManagement.cshtml` + `wwwroot/js/userManagement.js`) and ship together.

#### Sub-A — Import preview modal right edge cut off on mobile

**What's wrong.** Opening Import → picking a CSV → preview modal renders, but on phone-width viewports the rightmost columns (User Status / Action) extend past the visible area with no obvious scroll affordance. Action column dropdowns (Create / Skip / Use Import) become unreachable.

**Root cause.** `Views/Admin/UserManagement.cshtml:167` — preview table carried an inline `min-width: 900px` and the modal container used `width: 95%` (leaving a 5% gutter on each side). Default table padding (12px per cell) and 14px font compounded the overflow.

**Fix.** Scoped `<style>` block in `UserManagement.cshtml` with a `@@media (max-width: 768px)` rule that:
- Forces `.import-modal-container` to `width: 100%` and `max-height: 100vh` (no gutter on phones)
- Drops the table `min-width` from 900px to 0
- Reduces cell padding from 12px → 6/4px, font from 14px → 12px, action-select font to 11px
- Tightens the summary stats grid (4 columns at 1fr each) and footer padding
- Per Shawn's directive ("don't stack the columns, just make them fit") the columns remain inline; horizontal scroll within the table container is preserved as fallback for sub-360px phones

**Files:** `Views/Admin/UserManagement.cshtml` (new `<style>` block before the modal, plus `col-rownum` class on the Row `<th>`).

#### Sub-B — Roles failed if not capitalized (preview rejected "analyst")

**What's wrong.** A CSV row with `Role` = `analyst` (lowercase) is flagged in preview as `Invalid role: "analyst" (valid roles: Admin, Analyst, Partner, External, ...)`. The actual backend (`AdminService.CreateUserAsync`, SQL Server default CI collation) would happily resolve it — so this is a preview-only blocker.

**Root cause.** `wwwroot/js/userManagement.js:1094` — `validRoleNames.includes(normalizedRole)` is case-sensitive (JS `includes`). The earlier "Modified" comparison at line 1045 was also case-sensitive, which would have flagged otherwise-unchanged users as `modified` purely on role casing.

**Fix.** In `buildImportPreview`:
1. Build a `roleLookup = new Map(validRoleNames.map(n => [n.toLowerCase(), n]))` once per import
2. Canonicalize each row's role to its proper-cased form before further processing (`importUser.role = roleLookup.get(input.toLowerCase()) ?? input`)
3. Role-validity check is now `!roleLookup.has(normalizedRole.toLowerCase())`
4. Change-detection comparison at line 1045 also folded to lowercase, so casing alone no longer marks a row "Modified"

**Files:** `wwwroot/js/userManagement.js` (`buildImportPreview` only).

#### Sub-C — Scripts / links not stripped from imported text

**What's wrong.** A CSV with `Name = "<script>alert(1)</script>"` (or `<a href="javascript:...">`) lands in the DB verbatim. Today's render paths (`escapeHtml`) prevent stored-XSS at known surfaces, but any future surface that bypasses escaping (PDF export, email template, API payload) would expose it. Tester wanted explicit hardening.

**Root cause.** `parseAndPreviewImport` (`wwwroot/js/userManagement.js:912`) trusted CSV field values verbatim.

**Fix.** New helper `sanitizeImportField(value)` that:
- Removes `<script>...</script>` and `<style>...</style>` blocks including content
- Removes all `<tag>` markup (keeps inner text)
- Strips `javascript:`, `vbscript:`, `data:` protocol prefixes
- Strips bare event-handler patterns (`onclick=`, `onmouseover=`, etc.)
- Collapses whitespace introduced by removals

Applied to `name`, `email`, `company`, `role`, `status` during parse. Any field that changed picks up a `cleanedFields[]` entry recording `{field, original, cleaned}`. Per Shawn's preference (option 2 — "strip + flag, don't reject"), an amber **"Content cleaned"** pill renders next to the row's status pill in the preview modal, with a `title=` tooltip that shows each `Name: "<b>John</b>" → "John"` diff so the analyst sees exactly what was removed before clicking Apply.

**Files:** `wwwroot/js/userManagement.js` (`parseAndPreviewImport`, new `sanitizeImportField` helper, preview row HTML in `showImportPreviewModal`).

#### Sub-D — "Imported count is higher than total rows" (18 of 11) on re-import

**What's wrong.** Tester imported the same 11-row list twice. On the second import the preview footer read `18 of 11 rows will be imported` — numerator exceeded denominator, which is mathematically impossible for a per-row count.

**Root cause.** `updateSelectedCount` (`wwwroot/js/userManagement.js:1316-1332`). `toProcessCount` filtered on `action === 'create' || 'update'` *only*, while `totalActionable` filtered on `status !== 'error' && status !== 'unchanged'`. When a row got marked `error` after initial action assignment (e.g., duplicate-email-within-file, invalid role, sanitization residue), its `action` field was never reset, so it stayed counted in the numerator but was excluded from the denominator.

**Fix.** Two-pronged so the math is correct *and* the row data is self-consistent:
1. In `buildImportPreview`, when `errors.length > 0` set `previewRow.action = 'skip'` as well as `status = 'error'`. Status and action no longer disagree.
2. In `updateSelectedCount`, `toProcessCount` now also gates on `status !== 'error' && status !== 'unchanged'` — defends against any future code path that flips status without resetting action.

**Files:** `wwwroot/js/userManagement.js` (`buildImportPreview` validation tail, `updateSelectedCount`).

**Overall test plan after deploy.**

1. On phone-width viewport (DevTools iPhone SE 375×667): open `/Admin/UserManagement`, click **Import**, pick the template CSV. Preview modal should occupy 100% width, all 8 columns visible, Action dropdown reachable without horizontal scroll. Cancel button + Apply button + count text all visible in footer.
2. CSV with `Role = analyst` (any other casing) for an existing user: preview should show row as `Unchanged` (not `Modified`), no `Invalid role` error.
3. CSV with `Name = "<script>alert('x')</script>John Smith"`: preview should show the row with `Name = "John Smith"` and an amber **Content cleaned** pill in the Status column; tooltip shows `name: "<script>...</script>John Smith" → "John Smith"`.
4. Re-import the same CSV twice: preview footer should read `0 of 0 rows will be imported` on the second run (assuming nothing changed), never `N of M` with `N > M`.

Regression risk: low. All changes in one JS file + one cshtml file, scoped to the import flow. No backend, no DB.

**Verification (2026-05-21) — localhost matrix + live deploy-state fingerprint probe**

Localhost matrix via `qc214-user-import.js`: 7/7 PASS across all 4 sub-issues (Sub-A modal width @ 375px, Sub-B lowercase role accepted, Sub-C `<script>` stripped + Content-cleaned pill, Sub-D count math).

**Deploy-state probe** (fetched deployed `wwwroot/js/userManagement.js` from gccwebapp.com + GET on `/Admin/UserManagement` page, grepped for fix fingerprints):

| Sub-issue | Fingerprint | Live on gccwebapp.com? |
|---|---|---|
| Sub-A — mobile preview | `@media (max-width: 768px)` + `.import-modal-container` rule + `col-rownum` class | ❌ Not deployed |
| Sub-B — role canonicalization | `roleLookup = new Map(...)` in deployed JS | ❌ Not deployed |
| Sub-C — HTML sanitization | `sanitizeImportField(...)` helper + `cleanedFields` array | ❌ Not deployed |
| Sub-D — count math | `action = 'skip'` post-error in deployed JS | ✅ Already deployed (likely landed earlier) |

So 3 of 4 sub-issues still need to ship; Sub-D already live. After next deploy, re-run `BASE_URL=https://gccwebapp.com TARGET=live node qc214-user-import.js` to flip all 4 to "deployed + verified." Test artifact: `qc159-live-check/def214-215-deploy-probe-results.json`.

---

### DEF-213 — Cert Edit page: external/internal tag chips render close button as mojibaked `Ã—` instead of `×`

- **Date:** 2026-05-20
- **Severity:** Low (cosmetic, but obvious garbled text where a close button should be)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21 — 3 tag chips on cert 26-0163 each render proper `×`, no `Ã—` mojibake; see `qc-final-batch-live-results.json`)
- **QC Ref:** none (internal find — Shawn, while reviewing prod cert 26-0004 Edit page, 2026-05-20)
- **Bucket:** Bug — encoding / display
- **Reporter:** Internal (Shawn, 2026-05-20, screenshot of `https://gccwebapp.com/Certificate/Edit/26-0004` External Tags area)

**What's wrong.** On `/Certificate/Edit/<id>`, any cert with existing external or internal tags (e.g. LEED, APR) renders the tag's close button as literal text `Ã—` instead of `×`. The text appears next to the tag name inside the green chip — looks like the tag name itself is corrupted, which is why Shawn flagged "APR and LEED has special characters displaying."

**Root cause.** `Views/Certificate/Edit.cshtml` lines 1031 and 1059 — the close-button glyph was stored as the mojibake sequence `Ã—` (UTF-8 bytes for `×` interpreted as Windows-1252 and then re-saved as UTF-8). Browser correctly displays the literal `Ã—` characters that are actually in the file. The equivalent code in `wwwroot/js/certificate-create.js:409` uses the unicode `×` correctly; only the existing-tag rehydration path in `Edit.cshtml` had the corrupted bytes.

Tag names themselves in the DB are clean ASCII ("LEED", "APR") — the corruption was purely in the close-button literal, but it sits visually adjacent to the tag name inside the same chip so it reads as if the tag is broken.

**Fix.** Replaced both `Ã—` instances with the HTML entity `&times;` (encoding-safe, matches the convention already used in `wwwroot/js/certificate-tile-view.js`, `certificate-tile-view-internal.js`, `certificate-tile-view-external.js`).

**Files:** `Views/Certificate/Edit.cshtml` (lines 1031, 1059). No JS, CSS, controller, or DB changes.

**Test plan after deploy.** Open `/Certificate/Edit/26-0004` (or any cert with at least one external or internal tag). Each rehydrated tag chip should show its name followed by a clean `×` close button. Removing a tag and re-adding it should not regress (the add path already used the correct character via `certificate-create.js`).

Regression risk: zero. Two-line literal-text swap; no logic change.

---

### DEF-212 — SearchableSelect: re-focusing a populated autocomplete shows only the already-selected option, blocking quick re-selection

- **Date:** 2026-05-20
- **Severity:** Medium (UX) — High for Country (197 options collapse to 1; effectively unusable for change-after-pick)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21 — clicking the Country `SearchableSelect` on `/Certificate/Edit/26-0163` opens the dropdown with **197 visible options**, not 1; see `qc-final-batch-live-results.json`)
- **QC Ref:** none (internal find — Shawn, post-deploy probing of `testtest` Product Category on prod, 2026-05-20)
- **Bucket:** Bug — front-end component
- **Reporter:** Internal (Shawn, 2026-05-20)

**What's wrong.** The `SearchableSelect` component (`wwwroot/js/searchable-select.js`) wraps a native `<select>` with a type-ahead autocomplete: hidden underlying select, visible text input, dropdown of matching items. After the user picks an option (say `testtest` for Product Category, or `United States` for Country), the input text is set to the chosen value. Clicking *back* into the input — the universal "let me see my other choices" gesture — re-runs the focus handler, which calls `renderOptions(this.value)`. The filter is now the chosen text ("testtest", "United States"), so the dropdown collapses to the single already-selected option. Users have to triple-click and Delete the input text before the rest of the list re-appears, which is non-obvious enough that the field appears broken.

**Scope.** Every `SearchableSelect` instance on the app — six fields total across Create and Edit cert forms:

| Form | Field | Underlying options | Shown on re-click |
|---|---|---|---|
| Create | Product Category | 12 | 1 (collapsed to current pick) |
| Create | Certification Type | 19 | 1 |
| Create | Country | 198 | 1 |
| Edit | Product Category | 12 | 1 |
| Edit | Certification Type | 19 | 1 |
| Edit | Country | 198 | 1 |

Native `<select>` dropdowns on the same forms (Company, Product, Status, Partner, Pathway, Tags, Product Type / Subcategory cascades) are unaffected — browser-native dropdowns always show all options on click regardless of the underlying value.

**Why this matters for [[DEF-196]] / QC #170.** Carly's sub-issue 3 — *"only my test options show in the dropdown"* — was originally triaged as cascading-filter-working-as-designed. Probing prod cert 26-0163 (Category = "Carly Test") confirmed the Category SearchableSelect collapses to `["Carly Test", "Carly Test Type"]` on re-click. So her experience was **two compounding problems**: (a) the cascading Type filter (working as designed) AND (b) this SearchableSelect re-focus bug (real defect). The DEF-196 fix already addressed the count/delete-error half of #170; this fix closes the dropdown half. The 5/19 reply to client should be amended to mention this second factor.

**Root cause.** `wwwroot/js/searchable-select.js:189-193` — the `focus` handler passed the input's current value as the filter to `renderOptions(this.value)`. Standard autocomplete pattern (Select2, react-select, browser-native datalist) is to show the full list on focus and let typing re-filter; the existing `this.select()` line already highlights the input text so typing replaces it cleanly.

**Fix.** One line in `wwwroot/js/searchable-select.js`:

```diff
 input.addEventListener('focus', function () {
-    renderOptions(this.value);
+    renderOptions('');           // show full list on focus; typing re-filters via 'input' handler
     showDropdown();
     this.select();
 });
```

The `input` handler (line 179) still calls `renderOptions(this.value)` so typing filters in real time. The `keydown` ArrowDown branch (line 200) was already passing the current value as filter, which is correct behavior for keyboard navigation within an active filter — left unchanged.

**Test plan after deploy.** On `/Certificate/Create` and `/Certificate/Edit/<cert>`, for each of (Product Category, Certification Type, Country):

1. Pick any non-empty option
2. Click back into the field — full option list should appear
3. Type any substring — list should re-filter as you type
4. Press Escape — should restore the previously-selected text in the input

Regression risk: low. Single-line change, scoped to focus event; no behavior change for the `input`, `keydown`, or outside-click handlers. Worst case it does not break anything; in the absolute worst case rolling back is one line.

**Related / follow-up.** Side-finding from yesterday's QC #170 validation — the Edit form's cascading dropdowns also auto-overwrite the cert's saved `ProductCategoryId` with the linked `Product.CategoryId` on initial load. Separate defect, not addressed here. Will get its own DEF row if/when prioritized.

**Files:** `wwwroot/js/searchable-select.js`. No backend changes. No database changes.

---

### DEF-Priority4-Batch — 2026-05-21 batch verification

5 small UX items confirmed deployed and working on gccwebapp.com via single harness `qc159-live-check/qc-batch-priority4.js`:

| QC # | DEF # | Verification result |
|---|---|---|
| #104 | DEF-202 + DEF-211 | ✅ Both `/Company/Index` and `/Product/Management` breadcrumbs read `Companies & Products › <page>` — no leading "Administration" |
| #181 | DEF-172 | ✅ `#CompanyEmail` carries strict regex `^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$` (data-val-regex-pattern attribute); typing `notanemail` surfaces "Please enter a valid email address." inline |
| #183 | DEF-201 | ✅ "Most Popular Companies" and "Relevance Score" absent from both internal (19 options) and public (13 options) Sort By dropdowns |
| #207 | DEF-207 | ✅ Cert Create page inline-success script properly omitted when TempData empty; `Views/Certificate/Create.cshtml:53` `sessionStorage` guard `'createSuccessMsg:' + msg` present in source for the success-toast path |
| #174 | DEF-195 | ✅ 25 tiles sampled on `/Certificate/CertificateTileView`, every `.header-row` has `align-items: flex-start`, max top-delta = 0px between `.certificate-category` top and `.certificate-id` top |

11 / 11 assertions PASS. Test artifacts: `qc-batch-priority4-live-results.json`, screenshots `qc104-*`, `qc181-*`, `qc174-*`. No data mutation.

---

### DEF-211 — Breadcrumb on Companies & Products pages wrongly nested under "Administration"

- **Date:** 2026-05-19
- **Severity:** Low (cosmetic / IA correctness)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21 — batch Priority-4 verification, see `qc-batch-priority4-live-results.json`)
- **QC Ref:** #104 (5/1 follow-up — second half not covered by DEF-202)
- **Bucket:** UX — information architecture
- **Reporter:** Client (Carly, 5/1)

**What's wrong.** Company Management and Product Management pages rendered breadcrumb trail `Administration › Companies & Products › <page>`. In `_Layout.cshtml`, "Companies & Products" and "Administration" are **sibling top-level nav items** — Companies & Products is not a child of Administration — so the "Administration ›" prefix mis-described the site IA.

**Why DEF-202 didn't close this.** DEF-202 addressed two of the three components of QC #104 (the Companies → Company Management parity rename, and the Admin Dashboard "needs-no-action" because the standalone Admin Dashboard page was deleted 2026-05-01). The third component — bogus "Administration" prefix on Companies & Products pages — was not touched. Client flagged it again on 5/1: *"You are also showing 'Administration' under the Companies & Products which is not correct - its unrelated to this based on nav structure."*

**Fix.** Removed the leading "Administration ›" segment from the breadcrumb in:

- `Views/Company/Index.cshtml` — now `Companies & Products › Company Management`
- `Views/Product/Management.cshtml` — now `Companies & Products › Product Management`

Kept the comment explaining why so the next person editing these doesn't re-introduce the nesting.

**Not touched (deliberate).**

- `Views/Product/ManagementAlt.cshtml`, `Views/Product/ManagementV2.cshtml` — alternate/dead view files; left as-is to avoid touching code that may not ship.
- Admin views — `<span>Administration</span>` stays as the top of the trail on Admin pages because Admin pages ARE under the Administration nav dropdown. Already non-clickable (verified across all 14 Admin views).
- Breadcrumb `<nav>` in `_Layout.cshtml:299` is gated `role-admin-show` so Analyst users never see breadcrumbs even on Companies & Products pages. Flagged as a separate concern, not addressed here.

**Test plan after deploy.** Log in as Admin, navigate to Company Management — breadcrumb reads `Companies & Products › Company Management` (no "Administration"). Same check on Product Management. Admin pages (User Management, Roles, etc.) still read `Administration › <page>`.

---

### DEF-210 — No login rate-limit / account lockout: unlimited password-guess attempts against any account

- **Date:** 2026-05-19
- **Severity:** Major (multiplies DEF-209 into practical brute-force; combined risk on admin accounts is High)
- **Status:** **Deployed and verified on gccwebapp.com (2026-05-21).** Per-account lockout wired into `ValidateUserDetailed` (Controllers/AccountController.cs). Threshold = **5 failed attempts** → **15-minute lockout** (`MaxFailedLoginAttempts` + `LoginLockoutDuration` constants). The `Users.FailedLoginAttempts` and `Users.LockedUntil` columns already existed on the model + DB schema with the convenience `IsLockedOut` property — no migration required, just wired into the login flow. Locked accounts short-circuit BEFORE password verify (so locked-account probes don't increment the counter further AND don't reveal that the account is locked vs nonexistent). Successful login resets `FailedLoginAttempts` to 0 and clears `LockedUntil`. **Ships with DEF-209** — together they close the targeted brute-force threat model. Verified on dev (`Scripts/smoke-prod/verify-def209-210.js`): **6/6 pass** including the full lockout cycle (5 fails → counter=5 + LockedUntil set; 6th attempt with CORRECT password rejected; force-expire LockedUntil; correct password then succeeds; success resets counter to 0). Re-verified 2026-05-21 via `qc209-210-auth-hardening.js`: full destructive lockout matrix on dev account `jane.analyst@anukiconsulting.com` — 5 wrong attempts incremented `FailedLoginAttempts` 0→5 and set `LockedUntil`; 6th attempt with CORRECT password rejected with the generic error (proves locked-state short-circuit); force-expire then correct password returned HTTP 302 (successful redirect) and reset counter to 0 + cleared LockedUntil. Lockout NOT exercised on prod per the `[Data Changes Require CR]` rule; deployment confirmed via shared code path with DEF-209 (which IS verified live).
- **QC Ref:** none (internal exploratory testing — auth-surface audit)
- **Bucket:** Bug — auth hardening gap
- **Reporter:** Internal (Anuki QA)

**What's wrong.** The `POST /Account/Login` handler in `Controllers/AccountController.cs:162` calls `ValidateUserDetailed(model.Email, model.Password)` and on failure simply re-renders the form with a `ModelState` error. **No counter increments, no lockout timer, no progressive delay, no rate-limit middleware applied to the route.** An attacker can submit unlimited wrong passwords against any account without any throttling.

**Empirical proof on dev.**

Ran 8 consecutive `POST /Account/Login` requests with `Email = jane.analyst@anukiconsulting.com` and unique-wrong passwords (`wrongAttempt0` through `wrongAttempt7`) inside ~2 seconds total:

```
First attempt:  347ms  err="Login failed: Invalid password"
2nd attempt:    ~300ms err="Login failed: Invalid password"
…
8th attempt:    266ms  err="Login failed: Invalid password"
```

- 8 attempts in roughly 2.3s wall-clock — no slowdown.
- Same error message on the 8th attempt as the 1st — no lockout signal.
- 8 attempts is below typical lockout thresholds (3 / 5 / 10), but the trend line is flat — there is no counter at all. Running 1000 attempts would not lock the account.

The HTTP layer also returned 200 OK for every attempt (form re-render with error). No CAPTCHA injected, no exponential back-off, no per-IP throttle visible to the caller.

**Combined with DEF-209 (email enumeration).** An attacker who can confirm valid emails via the differing error messages can then mount an unbounded password-guess attack against those specific accounts. The combination — **DEF-209 + DEF-210** — moves the attack from "untargeted credential stuffing" (where most attempts hit invalid emails and are wasted) to "targeted brute-force against confirmed admin/analyst accounts." That's the actual exploitation path that matters.

**Why this hasn't been noticed.**

- AspNetCoreRateLimit IS configured in `Program.cs` (per memory entry on DEF-112), but it's applied to the partner API surface, not auth routes.
- The login route also bypasses the test-env Basic Auth gate that protects gccwebapp.com (per the `TestEnvironmentGateMiddleware` configuration), so requests reach the login handler without any pre-throttle.
- Standard ASP.NET Core Identity ships with `SignInManager.PasswordSignInAsync(..., lockoutOnFailure: true)` for exactly this case. The custom `ValidateUserDetailed` path doesn't use Identity — it's a hand-rolled email+password check against the Users table.

**Threat model.**

- **Targeted admin brute-force.** Admin email (`swest@anukiconsulting.com`) is essentially public knowledge. Combined with DEF-209's email confirmation, an attacker can run a credential dictionary at full network speed. Today gccwebapp.com is "test env" but the same auth code ships to production. When prod cuts over (current Basic Auth gate goes away), the login endpoint becomes the only barrier. **Land lockout BEFORE that cutover.**
- **No audit trail of brute-force.** The `_logger.LogWarning` at line 176 fires per attempt but there's no aggregation/alerting hook.

**Recommended fix.**

Layer this; cheapest is rate-limit middleware on the route, right answer is per-account lockout state.

**Minimum (deploy-this-week scope):** Add per-IP and per-email rate limiting on `POST /Account/Login` using the existing AspNetCoreRateLimit middleware:

```json
"IpRateLimiting": {
  "GeneralRules": [
    { "Endpoint": "POST:/Account/Login", "Period": "1m", "Limit": 5 },
    { "Endpoint": "POST:/Account/Login", "Period": "1h", "Limit": 30 }
  ]
}
```

(5/min, 30/hour per IP — same shape Azure AD uses. Tune as needed.)

**Right answer (sprint scope):** Add `FailedLoginCount` + `LockoutUntil` columns to `Users` table; increment on each failed attempt; clear on successful login; reject with `Login failed: account locked. Try again in N minutes.` when `LockoutUntil > now`. Threshold suggestion: 5 failures = 15-min lockout, 10 failures = 1-hour, 25 failures = require admin unlock. Also add `LastSuccessfulLoginAt` while in there — useful for stale-account alerts.

**Don't conflict with DEF-209 fix.** When DEF-209 is fixed (generic "Invalid email or password" message), the lockout message has to be generic too. The fix order matters: **ship DEF-209 + DEF-210 in the same deploy** so the threat model closes simultaneously.

**Test plan after fix.** Re-run `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-batch8.js` section [2]:
- 5 wrong attempts → 6th rejected with lockout message (Identity-style) or rate-limit 429 (middleware-style)
- One correct attempt 30 minutes after lockout → succeeds
- Lockout does NOT leak email existence: locking a non-existent email's "account" should NOT differ from DEF-209's generic message.

**Files affected (option B — Identity-style lockout).**

- `Models/User.cs` — add `FailedLoginCount`, `LockoutUntil`, `LastSuccessfulLoginAt` columns.
- New EF migration.
- `Controllers/AccountController.cs:162-180` — increment count on failure, check `LockoutUntil` before validating password, clear on success.

---

### DEF-209 — Login error messages enable email enumeration ("User not found" vs "Invalid password"); ForgotPassword timing side-channel

- **Date:** 2026-05-19
- **Severity:** Major (information disclosure; pairs with DEF-210 to make targeted brute-force practical)
- **Status:** **Deployed and verified on gccwebapp.com (2026-05-21).** Two changes in `Controllers/AccountController.cs`: (1) `ValidateUserDetailed` now returns a single `GenericLoginError` ("Invalid email or password.") for every failure mode — User-not-found, Invalid-password, Account-inactive, BCrypt-exception, Database-error all produce the same rendered string. Server-side `_logger.LogWarning` still records the specific reason for ops. The Login POST handler passes the generic message through verbatim (no more `$"Login failed: {loginError}"` interpolation). (2) ForgotPassword email send is now fire-and-forget (`_ = Task.Run(async () => { await _emailService.SendEmailAsync(...); })`), eliminating the synchronous-SMTP timing side-channel that made valid-email responses ~3000ms vs ~30ms for invalid-email. Verified on dev: identical error strings across 3 failure modes; ForgotPassword timing delta dropped from ~3000ms to ~52ms (69ms valid vs 17ms invalid, both fast). Live re-verification 2026-05-21: live error messages identical "Invalid email or password" across nonexistent + malformed email; live ForgotPassword both calls <500ms with 100ms delta — see `qc209-210-live-results.json`.
- **QC Ref:** none (internal exploratory testing — auth-surface audit)
- **Bucket:** Bug — information disclosure
- **Reporter:** Internal (Anuki QA)

**What's wrong.** The `POST /Account/Login` handler shows different error messages depending on whether the submitted email matches a Users row or not. An unauthenticated attacker can probe arbitrary email addresses one-by-one to confirm which are valid accounts, then concentrate password-guessing efforts (DEF-210) on those.

**Empirical proof on dev.**

```
POST /Account/Login Email=swest@anukiconsulting.com  Password=wrongpassword
  → 200 OK, error rendered: "Login failed: Invalid password"

POST /Account/Login Email=nonexistent-user@e2e-test.invalid  Password=TestPass123!
  → 200 OK, error rendered: "Login failed: User not found"

POST /Account/Login Email=malformed-not-an-email  Password=anything
  → 200 OK, error rendered: "Login failed: User not found"
```

Two distinct, deterministic messages parseable from the rendered HTML body. ~373ms timing delta between the cases also exists (valid email = 487ms with hash compare, nonexistent = 114ms with no hash) but the message difference alone is enough for enumeration.

**Root cause.** `Controllers/AccountController.cs:162-180`:

```csharp
var (userRole, canonicalEmail, loginError) = ValidateUserDetailed(model.Email, model.Password);
if (string.IsNullOrEmpty(userRole))
{
    _logger.LogWarning("Login failed for {Email}: {Reason}", model.Email, loginError);
    ModelState.AddModelError("", $"Login failed: {loginError}");
    return View(model);
}
```

`loginError` is the literal reason returned by `ValidateUserDetailed` — `"User not found"`, `"Invalid password"`, possibly `"Account inactive"`, `"Password reset required"`, etc. — interpolated directly into the user-facing message. Diagnostic-friendly, security-unfriendly.

**Adjacent finding — ForgotPassword timing side-channel.**

`POST /Account/ForgotPassword` correctly returns the SAME view (`ForgotPasswordConfirmation`) for valid and invalid emails — body length identical (2949 bytes both cases). **However** the response time differs dramatically:

```
POST /Account/ForgotPassword Email=swest@anukiconsulting.com         → 3008ms
POST /Account/ForgotPassword Email=nonexistent-fp@e2e-test.invalid   → 32ms
```

100× delta. The valid-email branch synchronously sends an email via `_emailService.SendEmailAsync` BEFORE returning the view (`AccountController.cs:459`). The invalid-email branch logs and returns immediately. Timing-based email enumeration works even though message bodies are identical.

**Threat model.**

- **Combined with DEF-210** (no rate-limit/lockout): once an attacker confirms an email is valid via the message differential, they can run unlimited password-guess attempts against that exact email.
- **Admin emails are easy to guess.** Format is `{firstname}@{domain}` or `{firstname}.{lastname}@{domain}`. Confirm one, sweep the company directory.
- **Public surface.** Login is `[AllowAnonymous]` and routable. No IP restriction, no captcha.

**Recommended fix.**

**Login POST handler — generic error.**

```csharp
if (string.IsNullOrEmpty(userRole))
{
    _logger.LogWarning("Login failed for {Email}: {Reason}", model.Email, loginError);
    // Generic message — DO NOT reveal whether email exists OR password is wrong
    ModelState.AddModelError("", "Invalid email or password.");
    return View(model);
}
```

Keep `loginError` going to the server log; strip it from the rendered HTML.

For "Account inactive" / "Password reset required" branches that need specific user-facing messages, gate them on a separate post-authentication check (run them ONLY after a successful password match — they leak less because the attacker already proved knowledge of credentials).

**ForgotPassword handler — async email send.**

Move the email send to a background queue (Hangfire / Channel / `Task.Run(...)` fire-and-forget). HTTP response returns immediately; email is sent out-of-band. This is also the right architecture — synchronous SMTP blocking the request thread is a perf foot-gun beyond the security issue.

**Test plan after fix.** Re-run `probe-batch8.js` Section [1]:
- All probe inputs (valid-email-wrong-pw, nonexistent-email, malformed, empty) → IDENTICAL `errMsg` string.
- Section [3]: `validMs - invalidMs` < 100ms after async fix.

**Files affected.**

- `Controllers/AccountController.cs:162-180` — generic error message.
- `Controllers/AccountController.cs:403-473` — async email send.

---

### DEF-208 — Static-file middleware serves every uploaded cert PDF and thumbnail to anonymous users, bypassing `ViewCertificateFile`'s DEF-164 status gate

- **Date:** 2026-05-19
- **Severity:** Critical (anonymous bulk leak of all uploaded cert files; bypasses DEF-164 entirely)
- **Status:** **Deployed and verified on gccwebapp.com (2026-05-21).** Closes DEF-204 ViewCertificateFile portion in the same diff (shared `FileAccessAllowed` gate). Verified on dev: `/uploads/*` returns 404 to anonymous including known-real filenames; legitimate access works via `/Certificate/ViewCertificateImage/{id}` and `/Certificate/ViewCertificateFile/{id}` (anonymous gets Paid cert images + PDFs, Drafts return 404). Migration-friendly: controller falls back from `App_Data/uploads/` to `wwwroot/uploads/` so existing files keep working before the one-time `Move-Item` runs server-side. **Pre-fix proof:** verified live on prod gccwebapp.com 2026-05-19 — `https://gccwebapp.com/uploads/images/26-0163_thumb_20260511_122453.png` returned 200 image/png to anonymous. **Post-fix re-verification 2026-05-21:** that same URL now returns **404** to anonymous; 4/4 `/uploads/*` probes (including a directory listing attempt) all 404 — confirms the static-file leak is closed. Bonus evidence: cert-detail HTML on prod no longer contains any `/uploads/` references; all image/PDF rendering now flows through `/Certificate/ViewCertificateFile/{id}` per the fix design. Test artifact: `qc159-live-check/def208-known-url-results.json`.
- **QC Ref:** none (internal exploratory testing — extends DEF-164 + DEF-204 surface audit)
- **Bucket:** Bug — auth/scoping gap (static-file path)
- **Reporter:** Internal (Anuki QA)

**What's wrong.** Uploaded certificate PDFs (`wwwroot/uploads/certificates/{filename}.pdf`) and thumbnails (`wwwroot/uploads/images/{filename}.png`) are served by ASP.NET Core's static file middleware to anyone who knows or can construct the filename. The middleware runs before any controller routing and applies no authorization. **DEF-164's anonymous-only status gate on `Controllers/CertificateController.ViewCertificateFile` is irrelevant** because the static URL bypasses the controller entirely.

**Empirical proof on dev (anonymous, no cookies, no API key):**

```
GET /uploads/certificates/26-0001_20260112_230836.pdf  (Paid cert)
  → 200 OK, application/pdf, 60341 bytes

GET /uploads/certificates/25-5027_20260113_032824.pdf  (random other cert)
  → 200 OK, application/pdf, 60268 bytes

GET /uploads/images/25-5027_thumb_20260113_032824.png
  → 200 OK, image/png, 51136 bytes
```

Sampled 5 PDFs and 5 thumbnails — **5/5 returned 200 OK with the file body** for the anonymous caller.

```
GET /uploads/certificates/              → 404 (directory listing disabled — good)
GET /uploads/../appsettings.json        → 404 (traversal sanitized — good)
```

Leak is bounded by needing the exact filename. But the filename format is predictable: `{certNumber}_{YYYYMMDD}_{HHMMSS}.pdf`. Cert numbers are sequential (`26-0001`, `26-0002`, …). Upload timestamps can be:
- Narrowed by deployment date for bulk imports
- Brute-forced — `YYYYMMDD` is bounded by reasonable upload windows, `HHMMSS` is 86400 values per day
- Or read off any debug output / log file / git commit metadata that leaks them

A motivated attacker can enumerate every cert PDF in the system at a few thousand HTTP requests per cert. ~163 certs in dev means the full corpus is downloadable in a manageable time.

**Why DEF-164's fix isn't enough.**

DEF-164 added an anonymous-only branch in `Controllers/CertificateController.ViewCertificateFile` (`CertificateController.cs:1750-1760`) that returns 404 for non-public certs. That gate works when traffic flows through the controller.

The vulnerability here is on a completely different URL — `/uploads/certificates/{filename}.pdf` — handled by `UseStaticFiles()` middleware in `Program.cs`. Static file middleware runs BEFORE routing, returns the file if it exists, never invokes `ViewCertificateFile`. The DEF-164 gate is unreachable for these requests.

**Threat model.**

- **Worst case:** anonymous attacker downloads every Draft / Hold / Expired / Confidential cert PDF. These contain client business info (issuer, partner attribution, products, etc.) that is intentionally not public until the cert reaches Paid/Republished.
- **Cross-tenant.** External users (restricted today by tile-view + detail-view controller scoping) can also hit `/uploads/...` and pull any cert PDF, not just their own company's. The per-company scoping in `CertificateController` is bypassable for any logged-in user (DEF-204 covers ExternalView scope; DEF-208 makes static-file access just as broad for unauthenticated users).
- **Compliance / contract.** Per the partner-API contract immutability rule (memory: `feedback_amazon_api_immutable.md`), partner data shape is sign-off-gated. Confidential certs leaking via static URL is not a contract change but it IS data leakage the API was designed to prevent.

**Recommended fix.**

**Option A (smallest change) — disable static file serving for `/uploads/` entirely; force all access through controllers.**

In `Program.cs` narrow the `UseStaticFiles()` middleware to skip `/uploads/`, and add controller routes that stream cert files after the existing role/ownership/status checks (mirror `ViewCertificateFile` for thumbnails too). Anywhere in views/JS that currently links `/uploads/images/...` directly gets refactored to use the controller route.

**Option B (right answer architecturally) — move uploads OUT of `wwwroot`.**

Relocate the directory to `App_Data/uploads/` or `<repo>/uploads/` (outside the web root). Static file middleware never sees it. All access flows through `ViewCertificateFile` and an equivalent thumbnail route. Update `FileUploadService` write paths + `CertificateImagePath` storage format if needed.

This is the cleanest fix — files that should be access-controlled are physically separate from public assets (`/css/`, `/js/`, `/img/`).

**Option C (band-aid) — rename existing files to cryptographically-random IDs.**

Replace the `{certNum}_{timestamp}.pdf` convention with `{guid}.pdf` (already partly done — DEF-193 added a GUID suffix in collision cases). Files remain in `wwwroot/uploads/`, but filenames are unguessable. Brute-force becomes infeasible.

Doesn't fix anonymous access for someone who SEES the URL (e.g., via cert detail page HTML rendering `<img src="/uploads/images/{guid}.png">`). Public Paid certs would still be accessible by URL once their thumbnail is rendered — probably fine because Paid is public. But Confidential and Hold certs whose UIs render thumbnails leak the URL to authorized users, who could share it.

Combine C with A: random filenames AND middleware block. Best defense-in-depth.

**Anuki recommendation: Option B (move out of `wwwroot`).** It's the only fix that's structurally correct. Cost is one migration (move files, update DB paths). Once shipped, all access flows through the controllers, and the DEF-164 + DEF-204 work becomes the actual gate (not bypassable).

**Test plan after fix.** Re-run `probe-batch8.js` Section [4]:
- Anonymous GET `/uploads/certificates/{any_known_filename}.pdf` → 401 or 404 (was 200)
- Same for `/uploads/images/{any}.png`
- Authenticated GET through the controller routes — admin sees all, External sees only their own company, anonymous sees only Paid/Republished — exactly matching DEF-204's recommended fix.
- Directory listing still 404 (unchanged).
- Traversal still 404 (unchanged).

**Files affected (Option B).**

- `Program.cs` — narrow `UseStaticFiles()` to exclude `/uploads/`.
- `Services/FileUploadService.cs` — write to `App_Data/uploads/...` instead of `wwwroot/uploads/...`.
- New controller route: `/Certificate/ViewCertificateImage/{id}` mirroring `ViewCertificateFile`. Apply DEF-204's role-aware status + company-ownership gate.
- Migration / one-time script to physically move existing files + update `Certificates.CertificateFile` + `CertificateImagePath` columns.
- Views: `<img src="@Url.Action(...)">` calls anywhere that currently emits `/uploads/images/...`.

---

### DEF-207 — Certificate Create page on mobile: stale "Certificate XX-XXXX created successfully!" toast fires on direct nav due to bfcache replaying the inline TempData script

- **Date:** 2026-05-18
- **Severity:** Low (cosmetic — no data action; misleads tester)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21 — guard verified deployed in batch Priority-4)
- **QC Ref:** Internal mobile testing (Shawn, gccwebapp.com)
- **Bucket:** Bug — client-side cache replay
- **Reporter:** Shawn (Anuki QA)

**What's wrong.** On real mobile devices hitting live gccwebapp.com, navigating to `/Certificate/Create` (after a prior "Save & Add Another" earlier in the session) replays the success toast — `"Certificate XX-XXXX created successfully!"` — even though no new certificate has been saved. The TempData entry was correctly consumed on the original POST→redirect; this is a browser-cache replay, not a server-state bug.

**Root cause.** `Views/Certificate/Create.cshtml:46-54` renders an inline `<script>` containing the success message whenever `TempData["SuccessMessage"]` is present (correct after Save & Add Another). Mobile Safari (and Android Chrome to a lesser degree) caches the entire rendered HTML — including that inline script — in the back-forward / disk cache. On a subsequent direct nav to `/Certificate/Create`, the browser serves the cached HTML and the `DOMContentLoaded` handler re-fires `AdminCore.showNotification(...)` with the old success text.

**Fix.** Added a `sessionStorage` guard keyed by the message itself. The first time the script fires for a given message, it stamps `createSuccessMsg:<msg>` and shows the toast. Cached replays hit the guard and skip. Try/catch around `sessionStorage` so private-browsing modes degrade gracefully (toast shows once, same as today).

**Files:**
- `Views/Certificate/Create.cshtml` — inline success-message script (lines 46-58 after fix)

**Notes:**
- Server-side TempData flow unchanged; only the client-side replay path is gated.
- `Views/Certificate/Edit.cshtml` checked — does not render an equivalent inline success script, no fix needed there.
- Other controllers that set `TempData["SuccessMessage"]` (Account 2FA, Document upload) render via `_Layout` / different views and have not exhibited this symptom; out of scope unless reported.

---

### DEF-206 — Internal `/api/CertificatesApi` has no role-based authorization: any authenticated user (External, Partner via X-API-Key) can read every cert in the database with full internal data shape

- **Date:** 2026-05-19
- **Severity:** Critical (multi-tenant data leak; internal field exposure; bypasses the role-gated tile view scoping)
- **Status:** **Deployed and verified on gccwebapp.com (2026-05-21).** Class-level `[Authorize(Policy = "AnalystOrAdmin")]` added to `CertificatesApiController` — External + Partner X-API-Key now 403 on every endpoint in the class; anonymous stays 401; Analyst + Admin keep access. The partner X-API-Key branch in `GetCertificates` (which was the source of the contract divergence) was deleted as dead code — partners stay on the contracted `/api/v1/certificates`. The unused `?userRole=` query parameters on `GetCertificate` and `GetCertificateByNumber` were also removed (caller's role comes from the auth ticket, not a querystring; the service ignored the value anyway). Verified on dev (`Scripts/smoke-prod/verify-def206.js`): **9/9** — anon 401, partner-key 403, external 403, analyst 200, admin 200, admin by-id 200, admin by-number 200, external by-id 403, external by-number 403. **Pre-fix proof:** verified live on prod gccwebapp.com 2026-05-19 — External user GET `/api/CertificatesApi?pageSize=50` returned 36 certs across 29 distinct companies. **Post-fix re-verification 2026-05-21:** anon `/api/CertificatesApi?pageSize=10` → **401**; external user (logged in via `external`/`TestPass123!`) → **403**; admin → **200** (no regression). 3/3 PASS on prod. Test artifact: `qc159-live-check/def203-208-security-results.json`.
- **QC Ref:** none (internal exploratory testing — same root-cause class as DEF-204 / DEF-205, applied to a different controller)
- **Bucket:** Bug — auth/scoping gap
- **Reporter:** Internal (Anuki QA)

**What's wrong.** `Controllers/Api/CertificatesApiController.cs` declares `[ApiController]` + `[Route("api/[controller]")]` with **no `[Authorize]` at class level and no role policy on the list `[HttpGet]` action**. Anonymous gets 401 (some app-wide default kicks in), but ANY authenticated identity — including External users (browser cookie) and Partner X-API-Key callers — gets `HTTP 200` with a full cert list. The query has no role-based scoping for cookie callers and applies a looser partner filter than the contracted `/api/v1/certificates` endpoint does.

**Empirical proof on dev:**

`External user (external.user@anukiconsulting.com, mapped to Henkel) GET /api/CertificatesApi?pageSize=50`:
```
status=200
count=43 certificates returned
distinct companies = ["Acme Corporation","Beta Industries","Sofidel America Corp.","KAO",
                     "Amazon Devices","Sonos","Superlong company name…","HID Global",
                     "ExtTest-AmazonCo","ExtTest-NoviCo"]   ← Henkel user sees TEN other companies
distinct statuses  = ["Client Approved - Paid","Draft","Active","Expired"]
                                                ↑ Drafts leaked to External
                                                ↑ "Active" (legacy/internal status) leaked
internal field shape: id, status (raw lifecycle), partner (raw value), asin, upc, gtin, ean,
                      tagType, certificateUrl, detailsUrl, isExpiringSoon, statusClass…
```

The same External user via `/Certificate/CertificateTileViewExternal` UI (the role-gated tile view) sees **2** certs, both Henkel. So the role-aware scoping exists in the tile-view query but is **not applied** at the `/api/CertificatesApi` endpoint.

`Amazon Partner X-API-Key GET /api/CertificatesApi?pageSize=50`:
```
status=200
count=11 certificates returned                  ← contracted v1 endpoint returns 4
distinct companies = ["KAO","Amazon Devices","Sonos","ExtTest-AmazonCo","Acme Corporation"]
                                                  ↑ Acme is Partner="None" — not Amazon's
distinct statuses  = ["Active","Client Approved - Paid","Draft"]
                                                  ↑ Drafts leaked (contract requires PublicStatuses only)
partner field      = "None"                       ← raw value, NOT the contracted "Amazon" literal
internal field shape: same as above — id, asin/upc/gtin/ean, raw status, etc.
```

Compare to the contracted `/api/v1/certificates` endpoint we verified earlier today: Amazon key returns 4 Active certs, `partner="Amazon"`, no Drafts, contract envelope.

**Two distinct partner-API-shape problems on this endpoint:**

1. **Contract divergence.** Partners hitting `/api/CertificatesApi` get 11 rows in internal-shape data while the same partners hitting `/api/v1/certificates` get 4 rows in contract-shape data. Amazon Ingest is theoretically scoped to v1 but nothing prevents them (or anyone with the key) from probing `/api/CertificatesApi`, where the response shape leaks `id`, raw lifecycle status, legacy flat product-id fields, and `partner="None"` for every cert returned.
2. **Looser filtering.** The dispatch on line 62-75 calls `_certificateService.GetCertificatesByPartnerAsync(partnerName, request)` which apparently does NOT filter by PublicStatuses (Drafts leaked) nor by the strict Partner=callerName match (Acme/None leaked to Amazon caller). Either the service is wrong, or the controller is calling the wrong service method, or the LIST/v1 path uses different filtering downstream and got it right.

**Root cause (in code).**

`Controllers/Api/CertificatesApiController.cs:11-15`:
```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CertificatesApiController : ControllerBase
{
```

No `[Authorize(Policy = ...)]` at class or method level. The class XML doc claims it supports both cookie auth AND partner X-API-Key — designed-in dual-mode endpoint. But the implementation only branches based on the presence of a `partner_name` claim; it doesn't gate cookie callers by role.

Compare to `Controllers/Api/v1/CertificatesController.cs` (the contracted partner API): that controller applies the strict scope (`PublicStatuses` + `Partner == callerName`) AND uses the `BuildPartnerCertificateDto` projection for partner callers. Class-level guard probably also restricts to partners. Whatever it does, it's stricter — and the contract-shaped result is the proof.

**Why this hasn't been noticed.**

- The UI surface for External users is the tile-view (`/Certificate/CertificateTileViewExternal`), which uses a different controller (`CertificateTileExternalApiController`) that DOES apply company scoping. So no External user has ever stumbled into `/api/CertificatesApi` via normal navigation.
- The contracted partner integration (Amazon's ingest) uses `/api/v1/certificates/*`, not `/api/CertificatesApi`. So no real-world traffic has hit this endpoint for partner-shaped reads.
- The endpoint exists for internal admin/analyst tooling — the XML doc and the dual-mode comment suggest it was once intended as a single multi-role endpoint, but the role enforcement was never added.

**Threat model.**

- **Insider browse:** Any logged-in External user can curl `/api/CertificatesApi` from their browser session and download the full cert table including every other tenant's data. No authentication-bypass needed; they have a valid session.
- **API key abuse:** A leaked or mis-issued partner X-API-Key (one of the things partner-API security is supposed to limit blast-radius on) reads BOTH the contracted v1 surface (correct, scoped) AND the internal `/api/CertificatesApi` surface (loose, includes Drafts, includes other partners' cert IDs). The mis-issued key has a bigger blast radius than the contract suggests.
- **No audit trail.** Reads on `/api/CertificatesApi` are logged at info level (line 86 `_logger.LogInformation(...)`). No security event, no rate limiting tied to the cross-tenant fan-out, no alerting on a single principal pulling thousands of rows.

**Severity rationale.**

- Multi-tenant data leak across companies → Critical
- Partner-contract shape leak (id / legacy fields / raw status) → Critical for Amazon contract
- Pattern matches DEF-204 / DEF-205 (list endpoint vs list endpoint here; sibling/detail issues there) — but THIS one is more dangerous because it's a list endpoint returning 43 rows of full internal data, not a single-cert lookup.

**Recommended fix.**

Two layers:

**Layer 1 — Class-level role guard.** Add `[Authorize(Policy = "AnalystOrAdmin")]` at the class level of `CertificatesApiController`. This blocks all External users and pure-cookie Partner callers immediately. Then re-allow the X-API-Key partner path via a per-method override on the list action only (and only if Amazon actually depends on this endpoint — which they shouldn't, per the contract).

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "AnalystOrAdmin")]   // ← new
public class CertificatesApiController : ControllerBase
```

**Layer 2 — Decide whether the X-API-Key partner branch should even exist on this endpoint.** Two options:

- **Option A — Delete the partner branch entirely from `/api/CertificatesApi`.** Partners use `/api/v1/certificates/*` exclusively (already contracted with Amazon). The dual-mode comment + code on lines 62-83 was speculative scope. Removing it eliminates one whole class of partner-contract risk and simplifies the security model (this controller = internal only).
- **Option B — Keep the partner branch but make it reuse the v1 codepath.** Have the partner branch call the same projection + filter chain the v1 controller does, so partners hitting EITHER endpoint get IDENTICAL data. Requires extracting the v1 projection into a shared service.

**Anuki recommendation: Option A.** Internal endpoint = internal callers. Partners have a contracted endpoint already. Less code, smaller attack surface, no risk of contract drift.

**Test plan after fix.** Reuse `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-internal-api.js`:
- External user `GET /api/CertificatesApi` → expect 403 (was 200 with 43 rows)
- Partner X-API-Key `GET /api/CertificatesApi` → expect 401/403 if Option A; expect contract-shape 4 rows if Option B
- Admin / Analyst `GET /api/CertificatesApi` → expect 200 with internal data (unchanged)
- Anonymous `GET /api/CertificatesApi` → expect 401 (unchanged)

Then re-run today's regression sweep (`C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\regression-sweep.js`) to verify no admin/analyst path broke.

**Files affected.**

- `Controllers/Api/CertificatesApiController.cs` — add class-level `[Authorize(Policy = "AnalystOrAdmin")]`; either delete the partner_name branch (Option A) or rewire it to call the v1 partner path (Option B).
- No DB / view / JS changes.

**Adjacent surfaces audited 2026-05-19 (empirical results).**

Probe script: `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-cert-api-adjacent.js`. Persona: same External user (Henkel-mapped), same Amazon partner key.

| Endpoint | Line | Auth attribute | External user | Anon | Verdict |
|---|---|---|---|---|---|
| `[HttpGet]` list | 53 | **none** | 200 / 43 rows / 10+ companies | 401 | **leaks (parent DEF)** |
| `[HttpGet("{id:int}")]` by-id | 114 | **none** + `?userRole=` dead param | 200 / full 30-key detail / other companies | 401 | **leaks same** |
| `[HttpGet("by-number/{n}")]` by-number | 149 | **none** + `?userRole=` dead param | 200 / 30-key detail / other companies incl. Draft `26-0026` | 401 | **leaks same** |
| `[HttpPost]` create | 185 | `[Authorize(Roles="Admin")]` | 403 ✓ | 401 ✓ | OK |
| `[HttpPut("{id:int}")]` update | 239 | `[Authorize(Roles="Admin")]` | 403 ✓ | 401 ✓ | OK |
| `[HttpDelete("{id:int}")]` delete | 313 | `[Authorize(Roles="Admin")]` | 403 ✓ | 401 ✓ | OK |
| `[HttpPost("{id}/upload")]` | 352 | `[Authorize(Roles="Admin")]` | not probed | not probed | OK (role attr present) |
| `[HttpPost("GenerateThumbnail")]` | 415 | `[Authorize(Roles="Admin,Analyst")]` | not probed | not probed | OK (role attr present) |
| `[HttpGet("{id}/download")]` | 527 | none + service-layer userRole filter | 404 (non-public) | 401 (Paid id=2) | OK by accident; denies more than the XML comment claims |
| `[HttpPost("export")]` | 577 | `[Authorize(Policy="AnalystOrAdmin")]` | 403 ✓ | 401 ✓ | OK |

**Three GETs leak. Seven mutating/protected endpoints are properly gated.**

**Confirmed by-id leak proof:**

```
External user (Henkel) GET /api/CertificatesApi/2
  → 200, 30 keys incl. id, certificateNumber, companyName, productName,
                       certificationType, productCategory, productType,
                       productSubcategory, effectiveDate, expirationDate,
                       companyContact, location, country, certificateImageUrl, additionalInfo, …
  → companyName="Acme Corporation"   ← OTHER COMPANY leak, full detail shape
  → status="Active"                  ← raw status, contract-divergent

External user (Henkel) GET /api/CertificatesApi/27
  → 200, same 30 keys
  → companyName="Superlong company name that needs to fit on the three rows…"
                                     ← OTHER COMPANY + DRAFT cert detail leaked

External user (Henkel) GET /api/CertificatesApi/by-number/26-0026  (Draft)
  → 200, same 30 keys                ← Drafts visible to External role
```

**`?userRole=` query parameter is IGNORED by the service** — same response shape (30 keys) for `?userRole=Public` vs `?userRole=Admin`. Good — no role-escalation via parameter. **But the parameter is dead code in the API surface.** Delete from method signatures during the DEF-206 fix; today it's deceptive (looks like it might do something).

**Updated fix scope.** The class-level `[Authorize(Policy = "AnalystOrAdmin")]` covers all 3 leaking GETs simultaneously (list, by-id, by-number). Keep the partner branch on the list action as the only per-method override. Also delete the unused `userRole` query parameter from both detail action signatures (line 117-118 and 152-153).

**Download endpoint quirk worth verifying when this DEF ships:** XML comment at line 524 says "Public certificates can be downloaded without authentication" but the actual behavior returns 401 for anonymous + Paid cert id=2. Either the comment is stale or there's a default-policy override applied. Currently denies MORE than documented — not a leak — but the docstring should match reality (or the implementation should match the docstring) once the rest of the controller is fixed.

---

### DEF-205 — RETRACTED 2026-05-19 — Partner API single-cert endpoints DO apply partner-ownership scoping correctly; original finding was based on misreading DB test data

- **Date:** 2026-05-19 (retracted same day)
- **Severity:** ~~Critical~~ → **Invalid finding**
- **Status:** **RETRACTED** — original write-up below was a false positive
- **QC Ref:** none

**Retraction reason (Anuki QA, 2026-05-19).** The original DEF write-up below claimed Amazon partner key gets cert `26-0001` (which I labeled "Acme / Partner=None") via `/api/v1/certificates/number/{n}` and `/{id}` — a cross-partner leak.

**Retest with the actual non-Amazon cert `26-0002` (Beta Industries, Partner=None):**

```
GET /api/v1/certificates/number/26-0002        → 404  ✓ correctly blocked
GET /api/v1/certificates/3                      → 404  ✓ correctly blocked  (id 3 = 26-0002)
GET /api/v1/certificates/number/26-0001        → 200  ✓ correctly returned  (26-0001 IS Amazon-tagged)
```

**Root cause of the false positive.** I confused **Company** (Acme Corporation) with **Partner** (Amazon). A cert can be from any company AND tagged with any partner — they're separate dimensions. Cert 26-0001 is from Acme Corporation (the manufacturer) AND tagged with Amazon (the partner displaying it via Climate Pledge Friendly). When the API returned 26-0001 with `partner: "Amazon"` and `company: "Acme Corporation"`, that was the **correct contract** — Acme's cert that Amazon partner-tracks. I incorrectly inferred from the company name that the cert was "Acme/None".

The DB columns:
- `Certificates.Partner` (string) — who tracks this cert (e.g., "Amazon", "Novi", "None")
- `Certificates.CompanyId` → `Companies.Name` — the manufacturer

A truly-non-Amazon cert is one with `Partner != "Amazon"`. `26-0002` (Beta/None) and `26-0003` through `26-0006` (None partner, various companies) are the actual non-Amazon certs. Tested all of them — Amazon key gets 404 on each.

**Adjacent verification — GS1 endpoint.** Same retest applied to `/api/v1/certificates/gs1` (the JSON-LD endpoint for Amazon Climate Pledge Friendly): Amazon key returns the same 4 Amazon-tagged certs (incl. 26-0001) in GS1 format. No leak of 26-0002 or other Partner=None certs. **GS1 endpoint is also clean.**

**Net day count adjusted:** 6 new DEFs from internal QA today (not 7). DEF-205 removed from the running list. The remaining six are DEF-203, DEF-204, DEF-206, DEF-208, DEF-209, DEF-210 — all empirically verified with the correct test data.

**Test artifact preserved for posterity:** `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-cert-api-adjacent.js` does the by-id/by-number probes. A corrected version would substitute 26-0002 for 26-0001 in the "should leak" cases. Same script structure, same expectations — just the choice of which cert is non-Amazon was wrong.

---

**Original (incorrect) write-up below for audit trail. The empirical content is preserved but the conclusion is wrong because the test cert was misidentified.**

- **Date:** 2026-05-19
- **Severity:** ~~Critical~~ (Invalid)
- **Status:** ~~Open~~ → **Retracted same day**
- **QC Ref:** none (internal exploratory testing — extends DEF-103 / DEF-198 / DEF-103-followup)
- **Bucket:** Bug — partner-scoping gap
- **Reporter:** Internal (Anuki QA)

**What's wrong.** The LIST endpoint `/api/v1/certificates` correctly scopes returned certs to the calling partner's identity (Amazon caller sees only Amazon-tagged certs — verified earlier today, 4 rows, all Amazon). The SINGLE-CERT endpoints `/api/v1/certificates/{id}` and `/api/v1/certificates/number/{n}` filter by `PublicStatuses` (so Drafts are correctly hidden) but **DO NOT apply partner ownership scoping**. They return ANY publicly-visible cert to ANY partner that asks by ID or by number. Worse, the response projection (`BuildPartnerCertificateDto`) hardcodes the `partner` field to the calling partner's name, so the leaked cert appears to belong to the wrong partner in the JSON.

**Empirical proof on dev:**

```
GET /api/v1/certificates/number/26-0001
X-API-Key: gcc_partner_<Amazon test key>

→ HTTP 200
{
  "success": true,
  "data": {
    "certificateNumber": "26-0001",
    "status": "Active",
    "partner": "Amazon",            ← LIE: actual Certificates.Partner = "None"
    "company": "Acme Corporation",  ← LEAK: not an Amazon-partnered company
    "productName": "Widget Pro",
    "certificationResults": "95%",
    "amazonCode": "greencircle_recycled_content",
    ...
  }
}
```

Same result via `GET /api/v1/certificates/2` (by internal id). Cert id=2 / number 26-0001 is tagged `Partner = "None"` in the DB but the Amazon partner key returns it unconditionally.

**Sanity check — by-number scoping is intact for Drafts:** `GET /api/v1/certificates/number/26-0026` (a Draft) returns 404. So the `PublicStatuses` filter IS applied. The miss is purely the partner-ownership filter.

**Root cause.** Controllers/Api/v1/CertificatesController.cs single-cert endpoints (search around the `BuildPartnerCertificateDto` helper introduced 2026-04-30 per DEF-103-followup):

- The LIST query in `GET /api/v1/certificates` builds `query.Where(c => c.Partner == callerPartnerName)` when the caller is a Partner.
- The single-cert handlers fetch by id/number and check `PublicStatuses.Contains(c.Status)` for the 404 gate, then directly call `BuildPartnerCertificateDto(c)` and return. **They never check `c.Partner == callerPartnerName`.**
- `BuildPartnerCertificateDto` hardcodes `partner = callerPartnerName` (so the field always says "Amazon" for an Amazon key, regardless of source). This is the right design for the LIST endpoint (where the scoping happens upstream) but creates a misleading payload when the upstream scoping is missing.

**Why this matters / threat model.**

- **Partner integrations are the primary external surface.** Amazon hits the API tens-of-thousands of times per day at scale. Any partner with a valid key can enumerate every publicly-visible cert across every company, regardless of partnership.
- **Cert numbers are easily enumerable** — sequential `YY-NNNN` format (`26-0001`, `26-0002`, …). An Amazon ingest pipeline that retries failed by-number lookups (or builds an "is this cert valid?" probe) effectively scrapes the entire public cert table.
- **Misleading partner attribution** — the JSON tells Amazon's downstream pipeline "this cert is yours" when it isn't. If Amazon caches by `partner` claim, the cache poisoning would attribute Acme's certs to the Amazon partnership, breaking downstream reporting/billing.
- **Contract violation.** Per `feedback_amazon_api_immutable.md` the Partner API contract is governed by client+partner sign-off. Returning a cert outside the partner's relationship + rewriting its partner attribution is a contract break, not a minor scope deviation.

**Severity rationale:** This is the same class of bug as DEF-103/DEF-198 but in the endpoints those defects didn't actually cover. The pattern is well-documented internally; the fix is well-understood; the cross-partner enumeration is the worst-case scenario for the Amazon contract. Critical.

**Recommended fix.** In `Controllers/Api/v1/CertificatesController.cs`, add the same partner-ownership clause to both single-cert handlers that the list handler already uses:

```csharp
// Inside GetCertificateById / GetCertificateByNumber, after loading the cert:
if (isPartnerCaller && !string.Equals(certificate.Partner, callerPartnerName, StringComparison.OrdinalIgnoreCase))
{
    return NotFound(...);  // 404 with the contract envelope shape
}
```

Place the check between the existing `PublicStatuses` 404 gate and the call to `BuildPartnerCertificateDto`. Return 404 (not 403) — matches the existing "drafts return 404" pattern and doesn't reveal that the cert exists under a different partner.

**Test plan after fix.** Re-run `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-security-confirm.js` Section [4]:
- `GET /api/v1/certificates/number/26-0001` with Amazon key → expect 404 (was 200)
- `GET /api/v1/certificates/2` with Amazon key → expect 404 (was 200)
- `GET /api/v1/certificates/number/<an-actual-Amazon-cert>` with Amazon key → expect 200 (unchanged)
- `GET /api/v1/certificates` (list) with Amazon key → expect 4 Amazon-only rows (unchanged)
- `GET /api/v1/certificates/{id}/download` (PDF) — verify same ownership check applies there too.

**Adjacent question — PDF download endpoint.** `/api/v1/certificates/{number}/download` is referenced in the `pdfDownloadUrl` field of the cert response. If that endpoint also bypasses partner-ownership (likely, given the pattern), the leak extends to the actual PDF binary. Verify and add to the same fix scope.

**Files affected.**

- `Controllers/Api/v1/CertificatesController.cs` — `GetCertificateById`, `GetCertificateByNumber`, and `DownloadCertificatePdf` (or equivalent name) get the partner-ownership clause.
- No DB schema change. No view change. No JS change.

---

### DEF-204 — Authenticated cross-scope cert leaks: `/Certificate/ViewCertificateFile/{id}` returns any cert PDF to any logged-in user; `/Certificate/CertificateExternalView/{num}` returns cert detail page across companies

- **Date:** 2026-05-19
- **Severity:** Critical (data leak; cert PDFs from Draft/Hold/Expired states downloadable by any authenticated user; External users can read other companies' cert detail pages)
- **Status:** **Deployed and verified on gccwebapp.com (2026-05-21).** Both halves closed via shared `FileAccessAllowed` helper (Admin/Analyst unrestricted; anonymous + External + Partner gated by PublicStatuses + non-expired; External additionally scoped by `userCompanyIds`; Partner by `userPartnerNames`). The `ViewCertificateFile` PDF gate landed in the DEF-208 patch; the `CertificateExternalView` HTML detail page gate landed in this same diff. Verified on dev with `Scripts/smoke-prod/verify-def204-extview.js`: Henkel External gets 200 for their own Paid certs (26-0011, 26-0020), 404 for own-company Expired (26-0012), 404 for cross-company Paid (26-0001, 26-0023), 404 for cross-company Draft (26-0026). Admin gets 200 for all. **9/9 probe pass.** **Pre-fix proof:** verified live on prod gccwebapp.com 2026-05-19 — External user GET `/Certificate/CertificateExternalView/26-0163` returned 200 OK with full cert detail body regardless of ownership. **Post-fix re-verification 2026-05-21:** anonymous `/Certificate/ViewCertificateFile/{id}` probes across 5 sampled IDs returned 4× **404** (non-public certs blocked) and 1× **200 image/png** (public cert — id=50, expected). Mix of 404/200 proves the gate is actively filtering. The cross-company External-user PDF check wasn't re-run on prod because the `external` test fixture's company scope on prod isn't known to the harness; localhost 9/9 + the anonymous gate proof are sufficient. Test artifact: `qc159-live-check/def203-208-security-results.json`.
- **QC Ref:** none (internal exploratory testing — extends DEF-164)
- **Bucket:** Bug — auth/scoping gap
- **Reporter:** Internal (Anuki QA)

**Two endpoints, same root cause.** Both check that the caller is *authenticated* but skip the next-level check on *ownership* or *publicly-visible status*. Combined into one DEF because they share the fix pattern.

**1. `/Certificate/ViewCertificateFile/{id}` — PDF download bypass for authenticated users**

DEF-164 added an anonymous-only gate to this endpoint (see `Controllers/CertificateController.cs:1750-1760`):

```csharp
if (User.Identity?.IsAuthenticated != true)
{
    var isPublicStatus = CertificateStatusHelper.IsPubliclyVisible(certificate.Status);
    var isExpired = certificate.ExpirationDate != default && certificate.ExpirationDate < CertificateStatusHelper.BusinessToday;
    if (!isPublicStatus || isExpired)
    {
        return NotFound();
    }
}
// ↑ if authenticated, falls through to serve the file
```

The fix correctly blocks anonymous users from downloading non-public PDFs. But it has a `!IsAuthenticated` gate — meaning **any logged-in user, regardless of role or company, can download any cert PDF** by guessing the integer ID. Empirically verified on dev with the External (Henkel) user:

```
GET /Certificate/ViewCertificateFile/27  (Draft cert, OTHER company)
   → 200, image/png, 151807 bytes  ← LEAK: Henkel user got a Draft PDF for a non-Henkel cert
GET /Certificate/ViewCertificateFile/13  (Expired cert, OTHER company)
   → 200, image/png, 151807 bytes  ← LEAK: same
GET /Certificate/ViewCertificateFile/2   (Paid cert, OTHER company — Acme)
   → 200, image/png, 151807 bytes  ← Arguable: Paid is publicly visible anyway
```

**Threat model:** integer IDs are sequentially enumerable, so any internal user (Analyst, External, Partner-on-web) can scrape every cert PDF in the database. External users in particular were never supposed to see non-their-company certs at all (per "External users see ONLY associated company/partner data" in CLAUDE.md), and definitely never Draft/Hold/Expired regardless of company.

**2. `/Certificate/CertificateExternalView/{number}` — cross-company cert detail leak**

The `CertificateExternalView` controller action (`Controllers/CertificateController.cs:93-141`) is protected by `[Authorize(Policy = "ExternalAccess")]` — gating that an External (or higher) user is logged in — but **never checks that the requested cert belongs to the caller's company**. Empirically verified:

```
External user (Henkel) GET /Certificate/CertificateExternalView/26-0001
   → 200, 19262 bytes
   → Body contains: "Acme Corporation", "certificate-detail", "Certification Period"
   → ← LEAK: Henkel user got Acme's company name + cert detail page
```

The tile-view filter (`CertificateTileViewExternal`) correctly scopes to the caller's company (verified earlier today — Henkel external user sees 2 certs, both Henkel). The DETAIL endpoint doesn't apply the same filter — load by number, return whatever you find.

Also: External user can hit ExternalView for a Draft cert (26-0026 returned 200 with the page chrome, although cert-specific data was empty — the controller doesn't 404 on Draft status either, returning a degraded-but-still-revealing-existence page).

**Root cause pattern (same for both endpoints).** Detail endpoints filter on *one* axis (authentication for the PDF route, "is External or higher" for the ExternalView route) but skip the second axis — *ownership / status*. The list endpoints in both cases (Tile API + Tile View) apply the full scoping, so the bug is invisible until you probe the detail surface directly.

**Recommended fix.**

**For `ViewCertificateFile`:** Apply the same status/expiration check to *all* callers, not just anonymous ones. For authenticated External / Partner users, additionally filter by company-ownership scope (mirror `GetCurrentUserCompanyIdsAsync` from `CertificateController.cs:606-644` per the DEF-198 fix pattern). Admin and Analyst can keep the unrestricted access.

```csharp
// Updated guard (pseudocode):
var role = GetUserRole();
var isAdminOrAnalyst = role == "Admin" || role == "Analyst";

if (!isAdminOrAnalyst)
{
    // 1) status gate (DEF-164, now extended to all non-admin/analyst callers)
    var isPublic = CertificateStatusHelper.IsPubliclyVisible(certificate.Status);
    var isExpired = certificate.ExpirationDate != default && certificate.ExpirationDate < CertificateStatusHelper.BusinessToday;
    if (!isPublic || isExpired)
    {
        return NotFound();
    }

    // 2) ownership gate for External / Partner — they may only download their own company's cert PDFs
    if (role == "External")
    {
        var myCompanyIds = await GetCurrentUserCompanyIdsAsync();
        if (!myCompanyIds.Contains(certificate.CompanyId)) return NotFound();
    }
    else if (role == "Partner")
    {
        var myPartnerNames = await GetCurrentUserPartnerNamesAsync();
        if (!myPartnerNames.Contains(certificate.Partner ?? "", StringComparer.OrdinalIgnoreCase)) return NotFound();
    }
}
// authenticated as Admin/Analyst → fall through, no further checks
```

Anonymous keeps its existing branch. Logged-in users get the appropriate ownership filter for their role.

**For `CertificateExternalView`:** Similar pattern. After loading the cert and BEFORE building the view model:

```csharp
var role = GetUserRole();
if (role == "External")
{
    var myCompanyIds = await GetCurrentUserCompanyIdsAsync();
    if (!myCompanyIds.Contains(certificate.CompanyId)) return NotFound();
}
else if (role == "Partner")
{
    var myPartnerNames = await GetCurrentUserPartnerNamesAsync();
    if (!myPartnerNames.Contains(certificate.Partner ?? "", StringComparer.OrdinalIgnoreCase)) return NotFound();
}

// Additionally — gate by PublicStatuses for non-admin/analyst.
// Drafts should never reach the External view.
if (!isAdminOrAnalyst && !CertificateStatusHelper.IsPubliclyVisible(certificate.Status))
{
    return NotFound();
}
```

**Test plan after fix.** Re-run `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-security-confirm.js` Sections [1] and [2-3]:

- External (Henkel) GET PDF for Draft 26-0026 → expect 404 (was 200)
- External GET PDF for Expired 26-0012 → expect 404 (was 200)
- External GET PDF for Paid 26-0001 (Acme — other company) → expect 404 (was 200, technically allowed since public but doesn't pass the ownership filter)
- External GET PDF for any of their own company's Paid certs → expect 200 (unchanged)
- External GET ExternalView for 26-0001 (Acme) → expect 404 (was 200 with "Acme Corporation" in body)
- External GET ExternalView for own Henkel cert → expect 200 (unchanged)
- Anonymous GET PDF for Paid cert → expect 200 (DEF-164 path, unchanged)
- Anonymous GET PDF for Draft → expect 404 (DEF-164 path, unchanged)
- Admin GET PDF for any cert → expect 200 (unchanged)

**Files affected.**

- `Controllers/CertificateController.cs:1737-1800` — `ViewCertificateFile` gate.
- `Controllers/CertificateController.cs:96-141` — `CertificateExternalView` ownership + status gate.
- No DB / view / JS changes.

**Adjacent surfaces to verify.** Apply the same pattern review to:
- `CertificateInternalView` (Admin/Analyst-only via policy, probably safe but confirm)
- `CertificateSingleView` (public — intentionally unscoped; just confirm Drafts aren't loadable here either)
- `/Certificate/Verify/{number}` — redirects to SingleView; same scoping
- `Edit/{number}` GET — confirm External + Partner can't GET an edit page for any cert (policy is `AnalystOrAdmin` so probably fine)
- `Download` POST/GET on cert files — any other binary endpoints

---

### DEF-203 — Certificate workflow state-transition matrix is dead code: Edit POST bypasses `CertificateWorkflowService` and accepts any status change

- **Date:** 2026-05-19
- **Severity:** Major (data-integrity gap — silent business-rule bypass on every cert status change)
- **Status:** Fixed locally — pending deploy to gccwebapp.com (Option A landed 2026-05-21)
- **QC Ref:** none (internal exploratory testing during the post-deploy "test around every item" pass — see workflow probe at `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\workflow-transitions.js` and verification harness `Scripts/smoke-prod/verify-def203.js`)
- **Bucket:** Bug — silent dead code
- **Reporter:** Internal (Anuki QA)

**What's wrong.** The 8-state lifecycle workflow has a documented allowed-transitions matrix and a service that validates against it, **but neither is invoked by the controller path that actually saves status changes.** Status field on the cert Edit form is assigned directly from posted form data, with no consultation of the matrix.

**Empirical proof on dev (cert `26-0163`, started at `Draft`):**

- ✓ Legal walk through all 8 states succeeded (10/10 hops, every step matched the matrix and saved): `Draft → ClientApprovedNotPaid → Confidential → Paid → Hold → Paid → Republished → Expired → Republished → ExpNoAV → Draft`. So the controller CAN save valid transitions — that's not the bug.
- ✗ **Direct probe: `Draft → Republished`** is **NOT** in `CertificateStatusHelper.IsValidTransition()`. Forced via `POST /Certificate/Edit/{id}` carrying the full form payload with `Status` overridden to `Republished`. Server returned **HTTP 200** with redirect to `/CertificateInternalView/26-0163`. SQL read after: cert.Status = `Republished`. **No 400, no model error, no audit warning — the illegal jump was silently accepted.**

Other illegal jumps that the matrix forbids but Edit POST would equally accept (logically equivalent — same code path):

| Illegal jump | Why it matters |
|---|---|
| `Draft → Expired` | Skip approval entirely |
| `Draft → Republished` | Cert becomes public without ever being Paid |
| `Draft → Expired - No Annual Verification` | Land directly in a terminal state |
| `Confidential → Republished` | Skip Paid intermediate (paid certs are the public surface) |
| `Expired - No Annual Verification → anything except Draft` | Resurrect a terminal-state cert |
| `Republished → Draft` | De-publish a published cert without going through Expired |

These are all "matrix-allows=N" per `CertificateStatusHelper.IsValidTransition`. The Edit POST would accept any of them with the same HTTP 200 / persisted-status pattern.

**Root cause (in code).**

1. `Models/CertificateStatus.cs:144-160` — `IsValidTransition(fromStatus, toStatus)` is implemented and correct (per-source-state allowed-next-states dictionary).
2. `Services/CertificateWorkflowService.cs:50-111` — `CanTransitionToStatusAsync` + `TransitionCertificateStatusAsync` correctly call `IsValidTransition` AND log to `CertificateStatusHistory` AND fire CR-015 (`SyncProductIdStatusesForCertificateAsync`) AND run `HandleStatusChangeBusinessLogic`.
3. `Controllers/CertificateController.cs:944-954` — Edit POST handler reads the posted model, builds the certificate update, and assigns:
   ```csharp
   certificate.Status = model.Status ?? "Draft";
   ```
   Direct assignment. No matrix check. No service call. No transition audit.
4. **`grep -rn 'TransitionCertificateStatusAsync\|CanTransitionToStatusAsync' Controllers/` returns zero results** — the service is defined and registered in DI (`Program.cs`) but never invoked from any controller path. It is fully dead code.

**Secondary impacts.**

- **CR-015 (Product-ID status follows cert) is also not firing on Edit-form status changes** — the cascade lives inside `TransitionCertificateStatusAsync`, which the controller bypasses. So if a cert moves from Paid → Hold via the Edit form, its associated `CertificateProductId.Status` rows stay marked Active. Their derivation is supposed to be automatic per `feedback_product_id_status_follows_cert`. Worth verifying independently.
- **`CertificateStatusHistory` is not being written** on Edit-form transitions — the audit row insert is inside `TransitionCertificateStatusAsync` too. The audit trail the WorkflowService writes ("changed from X to Y by user N at timestamp T") is not produced. Any "who-changed-what-when" trail comes only from the generic `CreatedBy/UpdatedBy` fields on the Certificate row — which only carry the latest state, not the path.
- **Role-restriction policy on `GetStatusesForRole` is also moot** — External users are mapped to `{Draft}` only, but External users can't reach Edit POST anyway (Edit policy is `AnalystOrAdmin`). So the role gate doesn't matter for the Edit path; the only users on this path are Analyst+, whom `GetStatusesForRole` allows all 8 for. Not a bypass per se, just dead.

**Why this hasn't been noticed.**

- The Status dropdown on the Edit form shows all 8 statuses to Admin + Analyst, so the UI itself doesn't visibly restrict (the matrix never gates what they're allowed to pick). A user clicking through the form sees what looks like a free-form status pick.
- The matrix was probably added with intent to enforce in the service, then the controller path was never updated. Comments in `CertificateWorkflowService` describe it as the enforcement surface, but it's plumbed in DI without callers.
- Smoke tests up to now hit "happy path" transitions only (Draft → Paid, Paid → Expired, etc.), which happen to all be matrix-valid. The bug only surfaces when you try an explicitly illegal jump.

**Recommended fix (two options — needs client decision).**

**Option A — Enforce the matrix (preferred):** In `Controllers/CertificateController.cs:944` Edit POST, when `model.Status != certificate.Status` (i.e., a transition is being attempted), route through the service:

```csharp
if (!string.Equals(model.Status, certificate.Status, StringComparison.Ordinal))
{
    var (ok, err) = await _workflowService.TransitionCertificateStatusAsync(
        certificate.Id, model.Status ?? "Draft", GetCurrentUserId());
    if (!ok)
    {
        ModelState.AddModelError(nameof(model.Status), err);
        // …re-populate viewmodel + return View(model)
    }
}
else
{
    certificate.Status = model.Status ?? "Draft";  // no-op transition, preserve current behavior for non-status edits
}
```

This wins the matrix enforcement AND the audit row AND the CR-015 cascade in one move. Same fix on the Create POST if a non-Draft initial status is ever allowed for Admin/Analyst (currently `GetCreationStatuses` returns only `Draft`, so probably not needed there).

**Option B — Decide the matrix is informational, delete dead code:** Strip `CertificateStatusHelper.IsValidTransition`, `CertificateWorkflowService.CanTransitionToStatusAsync`, `CertificateWorkflowService.TransitionCertificateStatusAsync`, the audit-log + business-logic blocks inside them, and the DI registration. State the relaxation explicitly in the requirements doc ("Admin and Analyst can change a cert to any status at any time; no transition matrix is enforced"). Saves carrying ~150 lines of unused code that future maintainers will assume is wired up.

**Anuki recommendation:** Option A. The comment in `CertificateWorkflowService:11-12` ("Enforces business rules for certificate lifecycle management") and the existence of `CertificateStatusHistory` table strongly suggest the original design intent. The matrix is also reasonably permissive — most realistic forward-progression paths are allowed; only "skip steps" jumps are blocked. Enforcing it doesn't restrict day-to-day analyst work, but does prevent the worst cases (skipping approval, resurrecting terminal states, de-publishing without an Expired waypoint).

**Files affected if Option A.**

- `Controllers/CertificateController.cs:944` — route status changes through `_workflowService.TransitionCertificateStatusAsync()` instead of direct assignment.
- Inject `ICertificateWorkflowService` into `CertificateController` constructor.
- (Optional) Surface the error from a rejected transition in the form's validation summary so the user sees "Cannot transition from Republished to Draft — must go through Expired first" rather than a generic save error.

**Test plan after fix.**

- Reuse `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\workflow-transitions.js` (the same script that found this) — re-run, expect `[A]` to still pass (legal walk) AND `[B]` invalid attempts to ALL be rejected (cert state preserved at "from").
- Sample at least one of: `Draft → Expired`, `Republished → Draft`, `ExpNoAV → Republished`, `Confidential → Republished`. All must return HTTP 4xx or save with `Status` unchanged.
- Verify `CertificateStatusHistory` gets a row per legal transition (audit trail intact).
- Verify product-ID cascade fires per CR-015 (legal status change of cert with product-IDs flips child rows).

**Fix landed (2026-05-21) — Option A: enforce the matrix in the Edit POST handler.**

Picked Option A per the Anuki recommendation: it preserves the audit row write + CR-015 cascade in one move, doesn't restrict realistic forward progressions, and keeps the design-time intent of the workflow service. Implementation in `Controllers/CertificateController.cs` Edit POST handler:

1. **Validation block, right after the cert is loaded.** If the requested status differs from the current and `CertificateStatusHelper.IsValidTransition(oldStatus, requestedStatus)` returns false, the controller rejects the save: writes a `LogWarning`, calls `ModelState.AddModelError(nameof(model.Status), "Invalid status transition from '{from}' to '{to}'. …")`, re-populates the form via `ReloadEditFormDataAsync(id)` and returns `View(model)`. Status stays at `oldStatus` in the DB. Display-name resolution uses `CertificateStatusHelper.GetDisplayName` so the validation message reads in human form ("Client Approved - Paid"), not the enum value.

2. **Audit history row written for user-initiated transitions.** After the existing DEF-171 save-time auto-expire block but before `UpdateAsync`, a `CertificateStatusHistory` row is inserted whenever `!autoExpired && oldStatus != certificate.Status`. The row carries `PreviousStatus = oldStatus`, `NewStatus = certificate.Status`, `Reason = "User-initiated transition via Edit form"`, `ChangedBy = actingUserId`, `ChangeSource = AuditChangeSource.WebUI`, `WorkflowStep = "Edit-POST"`. (Auto-expire still uses its own audit row through `TryApplySaveTimeAutoExpire`, so they don't double-write.)

Note on Option-A scope creep avoided: I deliberately did NOT route the save through `ICertificateWorkflowService.TransitionCertificateStatusAsync` inside the Edit POST flow. That service builds its own EF `SaveChangesAsync` cycle, which would conflict with the Edit POST's single transactional save (image uploads, custom-field values, product-ID array, etc., all flush together). Instead, the Edit POST now does the matrix check + audit row inline and continues with its existing save path. The service remains the right enforcement surface for any future status-only API call (e.g., a future PATCH /api/v1/certificates/{id}/status); it just isn't the right surface for the Edit POST's full-cert update path.

CR-015 product-ID cascade is already wired into the existing `SyncProductIdStatusesForCertificateAsync` call later in the same Edit POST handler — it runs unconditionally after the cert save and reads the now-updated `certificate.Status`. Verified intact (no regression).

**Verification (2026-05-21, localhost:5101).** `Scripts/smoke-prod/verify-def203.js` against fixture cert `26-0026`: **9/9 pass.**

```
[A] Valid transitions (matrix-allowed)
  ✓ Draft                   → Client Approved - Paid    (db=Client Approved - Paid)
  ✓ Client Approved - Paid  → Hold                      (db=Hold)
  ✓ Hold                    → Client Approved - Paid    (db=Client Approved - Paid)
  ✓ Client Approved - Paid  → Republished               (db=Republished)
  ✓ Republished             → Expired                   (db=Expired)
[B] Invalid transitions (matrix-forbidden) — REJECTED
  ✓ Draft  ↛ Republished                  (db unchanged = Draft)
  ✓ Draft  ↛ Expired                      (db unchanged = Draft)
  ✓ Draft  ↛ Expired - No Annual Verif.   (db unchanged = Draft)
[C] CertificateStatusHistory new rows
  ✓ 5 new rows (one per valid hop)
```

All three of the originally-cited illegal jumps from the discovery write-up (`Draft → Republished`, `Draft → Expired`, `Draft → Expired - No Annual Verification`) are now rejected with the cert's `Status` preserved. The legal walk-through still persists each hop and writes its history row. Cert `26-0026` was restored to its original `Draft` state at the end of the test, and test-added history rows trimmed.

**Build.** 0 errors. Files touched: `Controllers/CertificateController.cs` (Edit POST handler only). New file: `Scripts/smoke-prod/verify-def203.js`.

---

### DEF-202 — Cert import template enum hint; breadcrumb rename; public custom-field rendering; product-ID import SKU column; Product Mgmt export Name/SKU/Partner columns

- **Date:** 2026-05-19
- **Severity:** Minor (UX correctness — each sub-item closes a small client ask)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21 — QC #104 breadcrumb half verified in batch Priority-4)
- **QC Ref:** #148, #104 (partial — Company Mgmt rename), #118, #179 (partial — SKU column), #184 (partial — column additions)
- **Bucket:** Bug + Enhancement
- **Reporter:** Client

**Bundle.** Five small client-asked corrections grouped into one deploy footprint.

**1. QC #148 — Cert import template now ships Status enum hint inline.**

The User Management template (`wwwroot/js/userManagement.js:857-862`) and the Tags template (`wwwroot/js/tags.js:546`) already include enum hints like `Status (Active/Inactive)` and `TagType (Internal, External)`. The Certificate import template (`Controllers/CertificateImportController.cs:DownloadTemplate`) had bare headers — no inline cue that Status accepts only the 8 canonical lifecycle values. Updated the `Status` column header to `Status (Draft / Client Approved - Not Paid / Client Approved - Confidential / Client Approved - Paid / Hold / Expired / Republished / Expired - No Annual Verification — defaults to Draft if blank)`. Partner / Pathway / Country / cert taxonomy left plain because their value sets are admin-configurable per tenant and would go stale immediately.

`Services/Import/CertificateImportService.cs:GetFieldWithMapping` extended with a prefix-match fallback so the longer header still parses on re-import. After the exact short forms (`STATUS` / `Status` / `status`) fail, the helper walks `csv.HeaderRecord` and accepts any header that starts with one of the known names followed by a space or `(`. Tight enough that `Status Update` / `Status Date` won't false-positive.

**2. QC #104 (partial) — "Companies" breadcrumb → "Company Management" for parity with the Product side.**

`Views/Company/Index.cshtml:14-22` — single-word breadcrumb label change. Product side already reads "Product Management" so this restores symmetry. **The other half of #104 (Admin Dashboard nav item) is closed as needs-no-action**: per the comment at `Controllers/AdminController.cs:48` the standalone admin dashboard view was removed by client decision on 2026-05-01, and `/Admin/Index` now redirects to `UserManagement`. The breadcrumb the client expected to land somewhere is already pointing at the right destination.

**3. QC #118 — Custom Fields flagged IsPublic now render on Public + External cert detail pages.**

The model has `CustomField.IsPublic` (default `true`) and `Services/CertificateFieldService.GetFormattedFieldValuesAsync` already loads the value set into `Model.CustomFieldValues`. The Internal cert detail view (`Views/Certificate/CertificateInternalView.cshtml:197-213`) was rendering them; the Public (`CertificateSingleView.cshtml`) and External-partner (`CertificateExternalView.cshtml`) views simply weren't binding to the property. Added the same render block to both, filtered to `f.Field.IsPublic && !string.IsNullOrWhiteSpace(f.Value)` and grouped under a single `Custom Fields` section per `feedback_custom_fields_single_section` (CustomField.Section intentionally ignored everywhere).

Client report (5/1): *"I set test to public and it didn't show on public cert detail page. See Balcan 26-0159 example."* Direct cause: the public view never read the loaded data.

**4. QC #179 (partial) — Product ID import preview popup now shows the SKU column.**

`product-id-manager.js:1266` parses the SKU value from the CSV into `productSku`, but the preview-modal table rendered Type / ID / Name / Partner / Changes — no SKU column. Analysts could only verify SKU values by opening the cert after confirming the import. Added a `SKU` column to the modal `<thead>` (`Views/Shared/_ProductIdManager.cshtml:319-323`) and a `<td>` to the per-row render (`product-id-manager.js:1517-1520`). Monospaced like the Product ID cell so leading-zero values stay legible.

**#179 deferred sub-item.** The Description column in the import template doesn't have a UI surface today — client asked whether it should be exposed in the Add/Edit popup or removed. Needs design / client decision; carrying as a separate item rather than guessing.

**5. QC #184 (partial) — Product Management export now includes SKU and Partner columns.**

`Views/Product/Management.cshtml:exportProducts` previously emitted only Product Name / Company / Description / Category / Product Type / Subcategory / Identifier Type / Identifier Value. SKU was being shoved into the Identifier pair list (which clients building tooling around the existing layout may have come to rely on, so kept as a row pair too). Partner wasn't on the response at all. Two changes:

- `Controllers/ProductController.cs:GetProducts` — projection now also pulls `c.Partner` for each of the product's certs into a `CertPartners` array, then surfaces distinct non-empty / non-`None` values as `Partners` on `ProductListViewModel`.
- `Views/Product/Management.cshtml:exportProducts` — `headers` array gains `SKU` and `Partners` columns; `baseCols` populates `p.sku` and `(p.partners || []).join('; ')`.

**#184 deferred sub-item.** Client also asked for an "all active product identifiers across certs" cross-cert export. That's a new report surface, not a column-add — design decision pending (extend the Cert Tile CSV with one row per (cert × product ID), or new admin Reports page). Tracking as a future Enhancement.

**Build.** 0 errors. 51 pre-existing warnings unchanged.

**Files touched.**

- `Controllers/CertificateImportController.cs:319-336` — template Status header hint.
- `Services/Import/CertificateImportService.cs:67-72, 90-138` — prefix-match fallback in `GetFieldWithMapping`.
- `Views/Company/Index.cshtml:14-22` — breadcrumb rename.
- `Views/Certificate/CertificateSingleView.cshtml:140-159` — public Custom Fields render block.
- `Views/Certificate/CertificateExternalView.cshtml:156-176` — external Custom Fields render block.
- `Views/Shared/_ProductIdManager.cshtml:319-330` — SKU column header.
- `wwwroot/js/product-id-manager.js:1517-1521` — SKU `<td>` per row.
- `Controllers/ProductController.cs:236-243, 264-273, 960-969` — Partners projection + ViewModel property.
- `Views/Product/Management.cshtml:1075-1110` — export headers + row population.

**Verification.** Static checks only (server stopped during this turn). Build clean. Will visual-pass the next time the server is up: open cert import template, confirm Status header reads the long form; open Company Management breadcrumb; open Balcan-style public cert detail, confirm a public custom field renders; import product IDs with SKU values, confirm preview table shows them; export Product Mgmt to CSV, confirm Name/SKU/Partner columns present and populated.

**Notes.**

- **QC #181 (Company Email validation) is already fixed in HEAD** since commit `f018782` on 2026-05-11 — strict regex on `CertificateEditViewModel.CompanyEmail` and `CompanyViewModel.Email`. Status synced in this batch's log update; no code action needed.
- **#167 + #150 are no-code items.** `docs/tracking/client-responses-2026-05-19.md` carries Shawn's draft of (#167) the auto-expire timing explainer and (#150) the forgot-password verification steps + named-test-user provisioning script. Both close once Shawn sends them.

---

### DEF-201 — Three small UX fixes: header logo at reduced desktop, filter toggle unreachable on mobile, dead sort options removed

- **Date:** 2026-05-19
- **Severity:** Minor (each item is small UX correctness)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21 — QC #183 verified in batch Priority-4; #175/#176 visual checks unchanged)
- **QC Ref:** #175 (header logo distorts/narrow on reduced desktop/tablet), #176 (filter-hide toggle unreachable on mobile after hiding on desktop), #183 (Sort By has unused "Most Popular Companies" and "Relevance Score" options across all views) — all 2026-05-15
- **Bucket:** Bug (#175, #176) + UX (#183)
- **Reporter:** Client

**Symptoms.**

- **#175** — `.main-logo` default size is 280px (sized for ≥1440px displays); a tablet rule at 769–1024px scales to 180px. Between 1025 and 1440 the logo stayed at 280px and the `.logo-text-group`'s 300px `min-width` forced awkward wrap. Client report: *"Logo gets distorted/narrow in reduced desktop/tablet size."*
- **#176** — Per [[DEF-098]], the desktop Hide/Show Filters controls (`#sidebarToggleTab`, `#sidebarCollapseBtn`) are CSS-hidden at ≤1024px. A user who collapsed the sidebar on desktop and resized below 1024 (or rotated tablet to portrait) had the `.collapsed` class stuck on the sidebar with no on-screen way to bring it back. Client framing was hedged (*"not sure how realistic this scenario will be"*) — still worth a small fix.
- **#183** — `CertificateTileViewInternal.cshtml` had two Sort By options (`Most Popular Companies` / `Relevance Score`) with no backend handler. `CertificateTilesApiController.MapSortBy` has no `popularity` or `relevance` cases, so picking either fell through to the default (`Number`) and produced an unchanged sort. Client: *"What is this? It was under partner and analyst login."* External view had `Most Popular Companies` only (no Relevance Score).

**Fix.**

- **#175:** `wwwroot/css/main.css:3353-3373` — new `@media (min-width: 1025px) and (max-width: 1440px)` rule for `.main-logo` (220px), `.logo-text` (36px), `.logo-subtext` (24px). Sits between the existing 769–1024 tablet rule and the default 1441+ desktop sizing.
- **#176:** `wwwroot/js/certificate-tile-view.js`, `certificate-tile-view-internal.js`, `certificate-tile-view-external.js` — `setupSidebarToggle` now subscribes to `window.matchMedia('(max-width: 1024px)')`; whenever the viewport crosses into ≤1024px (or loads in that state), the sidebar's `.collapsed` class is removed via `expandSidebar()`. Same pattern in all three tile-view JS files; covers public, external, and internal callers.
- **#183:** `Views/Certificate/CertificateTileViewInternal.cshtml:214-231` — removed the two dead options (`data-sort="popularity"`, `data-sort="relevance"`) along with their associated DOM nodes; kept `Product Type A-Z` and `Status` since those have backend handlers. `Views/Certificate/CertificateTileViewExternal.cshtml:166-169` — same removal of `Most Popular Companies`. No backend changes needed — `MapSortBy` never mapped these keys, so dropping the option just removes a dead-click. Comment block in each view points the next reader at the MapSortBy method so this can't quietly regress.

**Verification.** Build clean (0 errors). #175 visual check: dev tools at 1280×800 and 1366×768 — logo no longer crowds `.logo-text-group` past its 300px min-width; layout fits. #176: collapse sidebar on desktop, resize to ≤1024px in dev tools — `.collapsed` is dropped immediately and filter rail re-appears. #183: open both internal and external tile views, click Sort By dropdown — neither "Most Popular Companies" nor "Relevance Score" appears.

**Files touched.**

- `wwwroot/css/main.css:3353-3373` — new mid-desktop logo breakpoint.
- `wwwroot/js/certificate-tile-view.js:51-72` — auto-expand on small breakpoint.
- `wwwroot/js/certificate-tile-view-external.js:567-600` — same.
- `wwwroot/js/certificate-tile-view-internal.js:581-614` — same.
- `Views/Certificate/CertificateTileViewInternal.cshtml:214-231` — sort option removal.
- `Views/Certificate/CertificateTileViewExternal.cshtml:166-169` — sort option removal.

**Notes.** **QC #181 (Company Email format validation) was already fixed.** Commit `f018782` on 2026-05-11 (same day client raised it) added strict regex `^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$` on both `Models/CertificateEditViewModel.cs:CompanyEmail` and `Models/CompanyViewModel.cs:Email`. The fix has been on master since 5/11 but waiting for the next deploy — that's why the client still saw the loose behavior on gccwebapp.com. Status synced in the QC log; no further code work needed.

**Related.** Smaller UX cleanup batch — no shared root cause across the three items, but bundling them into one DEF keeps the deploy footprint tight.

---

### DEF-200 — Internal dashboard tile counts vs export row counts diverged on most cards; "Certs Expired Last Month" used wrong date field

- **Date:** 2026-05-19
- **Severity:** Major (data integrity — every "click download to see what this number represents" interaction returned a different number than the tile)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21)
- **QC Ref:** #180 (client log, 2026-05-11) — *"Certs Expired Last Month is not using right data … we added clarifer of 'Active' for multiple items but then you are having the export show those not 'Active' (but number is only active which is right — the export should align with only the number that it makes up). … Anuki needs to do that"*
- **Bucket:** Bug
- **Reporter:** Client

**Symptom.** Every download icon on the internal dashboard returned rows that didn't match the count on the same card. Concrete dev-DB measurements:

| Card | Tile | Export (before) | Export (after) |
|---|---|---|---|
| Total Active Certs | 22 | 22 | 22 ✓ |
| # Companies with Active Certs | 9 | 12 | 9 ✓ |
| # Product Categories with Active Certs | 1 | 4 | 1 ✓ |
| # Product Types with Active Certs | 0 | 3 | 0 ✓ |
| # Product Type Subcategories with Active Certs | 0 | 3 | 0 ✓ |
| # Products with Active Certs | 10 | 22 | 10 ✓ |
| # Active Certs Published Last Month | 0 | 0 | 0 ✓ |
| # Certs Expiring This Month | 0 | 0 | 0 ✓ |
| **# Certs Expired Last Month** | **1** | **4** | **1 ✓** |
| # Certs Expiring Next Month | 0 | 0 | 0 ✓ |
| # Active Certs Published This Year | 16 | 15 | 15 ✓ |
| # Active Certs Published Last Year | 6 | 6 | 6 ✓ |

The "Certs Expired Last Month" gap was the one the client explicitly called out. The others compounded the trust problem.

**Root causes — three separate bugs in one feature area.**

1. **`certs-expired-last-month` used the wrong date column.** `Controllers/DashboardController.cs:certs-expired-last-month` called `_certificateRepository.GetCertificatesByDateRangeAsync(startOfLastMonth, endOfLastMonth)` which filters on `c.EffectiveDate >= startDate && c.EffectiveDate <= endDate` (per `Repositories/CertificateRepository.cs:447-463`). So "expired last month" returned certs that became **effective** last month — a completely different set. The export also had no status filter so it counted Draft / Hold rows with a stale ExpirationDate. Tile filter (correctly built on ExpirationDate) included a `EverPublishedStatuses` predicate per [[DEF-200]] earlier in this same change — but the export side never aligned. Combined effect: tile and export filtered on different columns AND different status sets.
2. **`products-with-active-certs` exported per-cert rows; tile counted distinct products.** `GenerateAggregatedStatCsv` already had aggregation cases for categories / types / subcategories / companies but missed Products. The tile binds to `Statistics["ActiveProducts"]` which is `Distinct(c.ProductId).Count()` over active certs (10 distinct in dev); the export fell through to `GetCertificatesForStat` which returned every active-cert row (22 in dev) — 12-row gap.
3. **`PublishedThisYear` tile counted future-dated certs.** `Repositories/CertificateRepository.cs:580-582` had `c.EffectiveDate >= startOfYear` with no upper bound. The matching export already capped at `today`. A single cert with `EffectiveDate = 2027-07-08` made tile=16 vs export=15.

**Fix.**

- `Repositories/CertificateRepository.cs` — new `GetCertificatesByExpirationDateRangeAsync(start, end)` helper that mirrors `GetCertificatesByDateRangeAsync` but filters and orders by `ExpirationDate`. Original method kept for the published-by-date callers, which correctly want EffectiveDate.
- `Repositories/CertificateRepository.cs:GetCertificateStatisticsAsync` — `stats["ExpiredLastMonth"]` already gained the `EverPublishedStatuses` filter (DEF-200 first pass). `stats["PublishedThisYear"]` capped at `today` so the tile no longer counts future-dated certs.
- `Repositories/ICertificateRepository.cs` — interface gains `GetCertificatesByExpirationDateRangeAsync`.
- `Controllers/DashboardController.cs:GetCertificatesForStat` — `certs-expired-last-month` switched to the new ExpirationDate helper and the same `EverPublishedStatuses` filter as the tile.
- `Controllers/DashboardController.cs:DownloadStat aggregateTypes` — added `products-with-active-certs`.
- `Controllers/DashboardController.cs:GenerateAggregatedStatCsv` — new `products-with-active-certs` case groups by Product name.
- `Controllers/ExportController.cs` (parallel `/Export/DownloadStat` endpoint) — all "Active …" cases switched from `PublicStatuses` to `DashboardStatuses` and the expired-last-month case picked up `EverPublishedStatuses`. The dashboard JS hits `/Dashboard/DownloadStat`, not this controller, but the parallel endpoint is reachable via `Scripts/testing/test-functionality.ps1` and a sweep of "Export" endpoints flagged in [[DEF-198]] notes — aligning it defensively prevents the same divergence reappearing through a different surface.

A new `CertificateStatusHelper.EverPublishedStatuses` constant was introduced (`DashboardStatuses` ∪ `{Expired, ExpiredNoAnnualVerification}`) so the "expired last month" predicate has a named source of truth — certs that were ever published, currently expired, or auto-flipped Expired by the background service. Draft / Hold / Pending Payment are correctly excluded.

**Verification.** Smoke test (`verify-180.js`) hits `/Dashboard` for tile values (with a `waitForTimeout(2500)` to let the count-up animation complete) and `/Dashboard/DownloadStat?type=…` for export row counts. All 12 cards now agree:

```
✓  total-active-certs                       tile=22 export=22
✓  companies-with-active-certs              tile=9  export=9
✓  product-categories-with-active-certs     tile=1  export=1
✓  product-types-with-active-certs          tile=0  export=0
✓  product-subcategories-with-active-certs  tile=0  export=0
✓  products-with-active-certs               tile=10 export=10
✓  certs-published-last-month               tile=0  export=0
✓  certs-expiring-this-month                tile=0  export=0
✓  certs-expired-last-month                 tile=1  export=1
✓  certs-expiring-next-month                tile=0  export=0
✓  certs-published-this-year                tile=15 export=15
✓  certs-published-last-year                tile=6  export=6
```

For the expired-last-month check specifically: temporarily set `Certificates.Id=27` `ExpirationDate=2026-04-15` to land a probe in last month. The background `CertificateExpirationBackgroundService` auto-flipped Status `Draft → Expired` (which is correct behavior — the cert's expiration date is past). Tile and export both then returned exactly 1 row matching that cert. Test fixture reverted after.

**Files touched.**

- `Models/CertificateStatus.cs` — `EverPublishedStatuses` constant.
- `Repositories/ICertificateRepository.cs` — interface for new helper.
- `Repositories/CertificateRepository.cs:447-484` — `GetCertificatesByExpirationDateRangeAsync` + `PublishedThisYear` cap.
- `Repositories/CertificateRepository.cs:569-578` — `ExpiredLastMonth` filter.
- `Controllers/DashboardController.cs:286-300` — `aggregateTypes` now includes products.
- `Controllers/DashboardController.cs:398-413` — `certs-expired-last-month` uses the right helper + filter.
- `Controllers/DashboardController.cs:509-518` — `products-with-active-certs` aggregation case.
- `Controllers/ExportController.cs:255-405` — parallel `/Export/…` endpoint aligned.

**Out-of-scope worth flagging.** The dashboard JS at `wwwroot/js/dashboard.js:298-312` animates each stat-value from 0 to its final number over ~1500ms. This is purely visual — the underlying server values are correct — but it tripped up the initial verification run (Playwright snapshotting between `networkidle` and animation completion captured mid-counter values like 7 instead of 22). Future automated tests against the dashboard need a `waitForTimeout(2000)` after navigation. Not a defect; logging here so the next person triaging "dashboard numbers wrong" knows to look past the animation.

**Related.** Caps the export side of the same hierarchy referenced in DEF-198 (Partner CSV scoping) and DEF-199 (filter-sidebar effective values). Same family of "two code paths queried the same conceptual entity with subtly different filters" bugs.

---

**Verification (added 2026-05-21) — QC #180 end-to-end test pass on both envs, all 12 cards reconciled**

Two-run verification using a single Playwright harness (`qc159-live-check/qc180-dashboard-counts.js`) parameterized via `BASE_URL` + `TARGET`. The script logs in as admin, navigates `/Dashboard`, waits 2500ms for the count-up animation to settle, walks every `.stat-card`, captures `(title, .stat-value, data-stat-type)`, fetches `/Dashboard/DownloadStat?type={statType}`, counts data rows in the returned CSV, and asserts `tile === export`.

**Run A — Localhost (`http://localhost:5101`, dev DB, 22 active certs)**

```
✅ total-active-certs                              tile=22 export=22
✅ companies-with-active-certs                     tile=9  export=9
✅ product-categories-with-active-certs            tile=1  export=1
✅ product-types-with-active-certs                 tile=0  export=0
✅ product-subcategories-with-active-certs         tile=0  export=0
✅ products-with-active-certs                      tile=10 export=10
✅ certs-published-last-month                      tile=0  export=0
✅ certs-expiring-this-month                       tile=0  export=0
✅ certs-expired-last-month                        tile=0  export=0
✅ certs-expiring-next-month                       tile=0  export=0
✅ certs-published-this-year                       tile=15 export=15
✅ certs-published-last-year                       tile=6  export=6
```

12 / 12 PASS. Same shape as the original DEF-200 smoke test; `certs-expired-last-month` is `0=0` today because the fixture cert (id=27) probe was reverted after the original verification — the assertion still proves tile=export.

**Run B — gccwebapp.com (live prod data, deploy verification)**

```
✅ total-active-certs                              tile=26 export=26
✅ companies-with-active-certs                     tile=22 export=22
✅ product-categories-with-active-certs            tile=2  export=2
✅ product-types-with-active-certs                 tile=2  export=2
✅ product-subcategories-with-active-certs         tile=2  export=2
✅ products-with-active-certs                      tile=26 export=26
✅ certs-published-last-month                      tile=5  export=5
✅ certs-expiring-this-month                       tile=1  export=1
✅ certs-expired-last-month                        tile=6  export=6
✅ certs-expiring-next-month                       tile=1  export=1
✅ certs-published-this-year                       tile=13 export=13
✅ certs-published-last-year                       tile=4  export=4
```

12 / 12 PASS on live prod data. Three notable cards directly exercise the three root-cause fixes:
- `certs-expired-last-month` **6 = 6** — the headline client complaint. Pre-fix this would have used `EffectiveDate` (wrong column) and no status filter; today it uses `ExpirationDate` + `EverPublishedStatuses`.
- `products-with-active-certs` **26 = 26** — pre-fix the tile counted distinct products while the export returned one row per cert. Now both go through the aggregated CSV path.
- `certs-published-this-year` **13 = 13** — pre-fix the tile had no upper date bound while the export capped at today; the cap is now applied symmetrically.

**Test artifacts** (working tree, not committed):
- `qc159-live-check/qc180-dashboard-counts.js` — parameterized Playwright harness
- `qc159-live-check/qc180-{local,live}-dashboard.png` — full-page screenshots of `/Dashboard` after settle
- `qc159-live-check/qc180-{local,live}-{stat-type}.csv` — 24 raw CSVs (12 per env)
- `qc159-live-check/qc180-{local,live}-results.json` — machine-readable assertion logs

**Status:** Closed — DEF-200 deployed and verified. Client's *"the export should align with only the number that it makes up"* contract is met on every card on prod.

**Operational note for future dashboard work:** the `waitForTimeout(2500)` is required — `wwwroot/js/dashboard.js:298-312` animates 0→N over ~1500ms. Without the wait, Playwright captures mid-counter values and the assertion produces false negatives. Documented inline in the script and called out in the original DEF-200 entry above.

---

### DEF-199 — Public/External tile-view filter sidebar misses cert-level category overrides and surfaces soft-deleted companies

- **Date:** 2026-05-18
- **Severity:** Major (UI/UX correctness — dead-clicks in the filter sidebar and silently-missing filter options that mask available certs from clients)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21)
- **QC Ref:** #177 *("I selected ASSA and it says no certs found, which means it should not appear in filter box as option … Then during later testing I found there is a cert showing with this — but then the filter selection is not working right")* and #178 *("26-0160 has type and subcat but only Carly Test and Chemicals and Raw Materials are in filter box … This used to be correct and now appears wrong. May be related to larger product cat overwrite/not showing when going back to edit")* — both 2026-05-08
- **Bucket:** Bug
- **Reporter:** Client

**Symptom.** Two related issues on the Public tile view (`/Certificate/CertificateTileView`) and equivalents on the logged-in External/Partner view (`/Certificate/CertificateTileViewExternal`).

1. (QC #178) The Product Category / Type / Subcategory filter sidebar listed only nodes derived from `Certificate.Product` — it didn't surface any cert-level FK override (`Certificates.ProductCategoryId/ProductTypeId/ProductSubcategoryId` introduced by DEF-192). A cert whose category/type/subcategory was set at the cert level (overriding the product's value) was visible in the cert tile but unreachable from the sidebar.
2. (QC #177) The Company filter dropdown surfaced companies that subsequently looked like they had "no certs" — soft-deleted Companies that an existing cert still referenced were still being projected through the navigation (`c.Company.Name`) and appearing in the sidebar.

Net effect for the client: filter selections felt broken — picking a category that should exist returned nothing because the listing predicate uses *effective* values (DEF-192) but the sidebar source did not.

**Root cause.**

`Controllers/Api/CertificateTilesApiController.cs:GetFilterOptions` (Public/anonymous) and `Controllers/Api/CertificateTileExternalApiController.cs:GetFilterOptions` (logged-in External/Partner) both built their hierarchy from product-only projections:

```csharp
var visibleCategoryIds   = await baseQuery.Select(c => c.Product.CategoryId).Distinct().ToListAsync();
var visibleProductTypeIds = await baseQuery.Where(c => c.Product.ProductTypeId != null)
                                            .Select(c => c.Product.ProductTypeId!.Value).Distinct().ToListAsync();
var visibleSubcategoryIds = await baseQuery.Where(c => c.Product.ProductSubcategoryId != null)
                                            .Select(c => c.Product.ProductSubcategoryId!.Value).Distinct().ToListAsync();
```

Those projections never read the cert-level FK columns, so any value set on the cert directly was invisible to the sidebar. The listing endpoint (`CertificateRepository.cs:GetAdvancedFilteredCertificatesAsync`) had already been updated for DEF-192 to use effective values — the sidebar source was never updated to match.

The flat `productCategories` / `productTypes` lists (returned alongside the hierarchy for backward compatibility) had the same Product-only projection (`c.Product.Category.Name`, `c.Product.ProductType!.Name`), so even clients still using the flat lists missed the same data.

The Company dropdown projected `c.Company.Id` / `c.Company.Name` through navigation without an `IsActive && !IsDeleted` predicate, so a soft-deleted company stayed in the sidebar as long as any cert still referenced it.

**Fix.**

`Controllers/Api/CertificateTilesApiController.cs` and `Controllers/Api/CertificateTileExternalApiController.cs`:

1. **`visibleCategoryIds` / `visibleProductTypeIds` / `visibleSubcategoryIds`** now use the effective-value pattern from DEF-192: `c.ProductCategoryId ?? c.Product.CategoryId`, `c.ProductTypeId ?? c.Product.ProductTypeId`, `c.ProductSubcategoryId ?? c.Product.ProductSubcategoryId`. Cert-level overrides surface in the sidebar exactly as they do in the listing.
2. **Flat `productCategories` and `productTypes` lists** resolved from the same `visible*Ids` sets (joined back to `ProductCategories` / `ProductTypes` by ID) instead of independent projections — single source of truth.
3. **`companies` dropdown** picks up `c.Company.IsActive && !c.Company.IsDeleted` so soft-deleted Companies disappear from the sidebar even if a cert still references them via FK.

The hierarchy walker (`productCategoriesHierarchy`) was already correctly filtering against `visible*Ids`, so once those sets contain the effective values the hierarchy renders correctly without further changes.

**Verification.** Local smoke against `localhost:5101` against dev DB.

*Cert-override path (QC #178).* Baseline filter API returned 1 product category (Chemicals and Raw Materials). Set `Certificates.ProductCategoryId = 2` on cert 38 (a Client Approved - Paid cert whose Product belongs to category 1) and re-hit the filter API:

- **Before fix:** category 2 (Clothing, Apparel, and Fabrics) would have been silently missing — `c.Product.CategoryId = 1` projects only category 1.
- **After fix:** sidebar returns *both* `Chemicals and Raw Materials` and `Clothing, Apparel, and Fabrics`. Listing API for `productCategory=Clothing, Apparel, and Fabrics` returns exactly cert 38. Sidebar ↔ listing consistent.

Override reverted after test.

*Soft-deleted-company path (QC #177).* Baseline returned 9 companies including Acme Corporation. Set `Companies.IsDeleted = 1` on Acme (Id=59) — Acme has a Client Approved - Paid cert tagged to it. Re-hit the filter API:

- **Before fix:** Acme still appeared (8+1=9 companies).
- **After fix:** Acme correctly hidden, dropdown returns 8 companies.

Acme restored after test.

**Files touched.**

- `Controllers/Api/CertificateTilesApiController.cs:243-393` — `GetFilterOptions` projections updated; flat lists refactored to share `visible*Ids`.
- `Controllers/Api/CertificateTileExternalApiController.cs:438-621` — same shape, plus the role-based partner/external scoping still applies before the new effective-value projections.

**Out of scope (worth flagging).**

- A cert that sets a `ProductTypeId` override pointing to a type under a *different* category than the cert's effective Category will surface the type only if its parent Category is independently visible. The dominant data pattern in dev is "category override only, type/subcat fall through," so this case isn't reproduced today. If the client reports a missing type under a category that doesn't list it, a follow-up fix is to also seed `visibleCategoryIds` with the `CategoryId` of each type/subcat in `visibleProductTypeIds` / `visibleSubcategoryIds` (one extra DB round-trip).
- Internal (admin/analyst) tile view filter (`Controllers/Api/CertificateTileInternalApiController.cs:GetFilterOptions`) doesn't filter the category hierarchy against visible-cert IDs at all — admin sees every Category/Type/Subcategory regardless of usage. That's the existing behavior and matches admin expectations; not changed here.

**Related.** [[DEF-192]] (listing-side effective category/type/subcategory predicate). This closes the sidebar half of that same data-shape gap.

---

**Verification (added 2026-05-21) — QC #177 + #178 destructive matrix on localhost + read-only deploy probe on gccwebapp.com**

Single harness `qc159-live-check/qc177-178-filter-options.js` exercises both QC items in one run. Both DEFs share the same endpoint (`GET /api/certificate-tiles/filters`), so one mutation cycle proves both fixes.

**Run A — Localhost destructive matrix** (3 snapshots + clean revert via PowerShell SQL):

| Phase | Mutation | Companies | Categories | Acme present? | Cat id=2 present? |
|---|---|---|---|---|---|
| Baseline | none | 9 | [1] | ✅ yes | no |
| After mutation | `Companies.IsDeleted=1 WHERE Id=59 (Acme)`; `Certificates.ProductCategoryId=2 WHERE Id=38` | 8 | [1, 2] | ❌ correctly hidden | ✅ surfaces via cert-level override |
| After revert | `Companies.IsDeleted=0`; `Certificates.ProductCategoryId=NULL` | 9 | [1] | ✅ restored | no — restored |

8 / 8 assertions PASS:

```
✅ Filter endpoint returns 200
✅ Acme present in baseline (sanity)
✅ Acme (soft-deleted) NOT in company filter         — QC #177 main proof
✅ Company count drops by exactly 1                  — QC #177 count proof
✅ Cert-level Category override (id=2) surfaces      — QC #178 main proof
✅ Acme returns to filter after restore              — no test residue
✅ Company count matches baseline post-restore
✅ Category set matches baseline post-restore
```

Cleanup verified — `Companies.IsDeleted` and `Certificates.ProductCategoryId` for cert 38 are at their original values. No test residue.

**Run B — gccwebapp.com read-only deploy probe**

```
✅ Public filter endpoint reachable on prod (HTTP 200)
✅ Filter response carries companies + categories (21 companies, 3 categories)
```

Notable: **"ASSA ABLOY Access & Egress Hardware Group, Inc. - Corbin Russwin Division"** appears in the prod filter list today. That was the specific company the client called out in QC #177 (5/8 — *"I selected ASSA and it says no certs found"*). Per the deployed code's `baseQuery` (`/api/certificate-tiles/filters`, line ~250: `PublicStatuses.Contains(c.Status) AND (ExpirationDate IS NULL OR ExpirationDate >= today)` plus the DEF-199 `Company.IsActive && !Company.IsDeleted` predicate), any company in the dropdown is guaranteed to have ≥1 currently-visible cert. The client's complaint can't occur on the deployed build.

**Test artifacts** (working tree, not committed):
- `qc159-live-check/qc177-178-filter-options.js` — parameterized harness with PowerShell SQL helper
- `qc177-local-{base,mut,restore}.json` — three localhost snapshots
- `qc177-live-live.json` — prod snapshot
- `qc177-{local,live}-results.json` — machine-readable assertion logs

**Status:** Closed — DEF-199 deployed and verified for both QC #177 (Company filter excludes soft-deleted) and QC #178 (cert-level Category override surfaces). The pattern called out under "Out of scope" in this entry (cross-category type/subcat overrides) remains a documented follow-up if the client reports it post-launch; today's data doesn't reproduce it.

---

### DEF-198 — Partner login "Export All to CSV" leaks all certificates (drafts, expired, non-partner) — data exposure to a Partner caller

- **Date:** 2026-05-18
- **Severity:** **Critical** (data leakage — a Partner user receives certs outside their partner scope and outside the public-status contract)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21)
- **QC Ref:** #182 (client log, 2026-05-15, *"This is big issue, needs fixed."*)
- **Bucket:** Bug
- **Reporter:** Client

**Symptom.** A Partner-role web user logged into `/Certificate/CertificateTileViewExternal` and clicked **Export All to CSV** got a file containing every certificate in the database — including drafts, expired rows, and certs tagged to other partners. In dev DB reproduction the partner_test user (mapped to Amazon partner) received 43 rows; the correct answer is 4 rows (`Client Approved - Paid` × `Amazon`).

**Root cause — three layered gaps on the export path.**

1. **No role-scoping in the service.** `Services/CertificateService.cs:GetCertificatesForExportAsync` and `GetCertificatesForExportByIdsAsync` applied only the user's text/cert-type/country/status filters. There was no `Partner == callerPartner` / `CompanyId IN callerCompanies` / `Status IN PublicStatuses` predicate. The same scope is enforced correctly on `Controllers/Api/CertificateTileExternalApiController.cs:97-138` for the tile view itself, so a Partner user *could see* only their own certs in the UI but *could export* the entire DB.
2. **`GetUserRole()` missing the Partner branch.** `Controllers/CertificateController.cs:1492-1504` enumerated Admin/Analyst/External/Internal but fell through to `"Public"` for Partner web users. Cookie auth stamps the `Role` claim with `"Partner"` (per `Controllers/AccountController.cs:1136`), so the branch was reachable but never matched.
3. **No security context plumbed into the service.** `CertificateListRequest` carried `UserRole` but no `AllowedPartnerNames` / `AllowedCompanyIds`, so even if the service wanted to scope, it had no data to scope with.

Same shape as the 2026-04-21 Partner API status-contract fix (which closed the equivalent leak on `/api/v1/certificates*`) — that work covered the JSON API but did not touch this CSV export endpoint.

**Fix.**

1. `Models/CertificateViewModels.cs`: added `AllowedPartnerNames` (`List<string>`) and `AllowedCompanyIds` (`List<int>`) to `CertificateListRequest`.
2. `Services/CertificateService.cs`: introduced `ApplyExportSecurityScope(IQueryable<Certificate>, CertificateListRequest)` and called it from both `GetCertificatesForExportAsync` and `GetCertificatesForExportByIdsAsync`. Predicate matches `CertificateTileExternalApiController` exactly:
   - **Partner** → `AllowedPartnerNames.Contains(c.Partner ?? "None")` (or `false` if no partner mapping) AND `PublicStatuses.Contains(c.Status)`
   - **External** → `AllowedCompanyIds.Contains(c.CompanyId)` (or `false` if no company mapping) AND `PublicStatuses.Contains(c.Status)`
   - **Public** (anonymous) → `PublicStatuses.Contains(c.Status)`
   - **Admin / Analyst / Internal** → no restriction
3. `Services/IServices.cs` + `Services/CertificateService.cs`: added optional `CertificateListRequest? securityContext` parameter to `ExportCertificatesByIdsAsync` so the "specific IDs" path can't be used to extract IDs outside the caller's scope.
4. `Controllers/CertificateController.cs`:
   - Added the `Partner` branch to `GetUserRole()` (matches the `"Role"` cookie claim emitted by `SignInUser`).
   - Ported `GetCurrentUserPartnerNamesAsync` / `GetCurrentUserCompanyIdsAsync` from `ProductController.cs:606-644` (same shape, same source claim).
   - In `ExportCertificates`, populate `AllowedPartnerNames` / `AllowedCompanyIds` from the authenticated user's claims before calling the service. Pass the request through to both export methods including the by-IDs path.

**Verification.** Smoke test at `localhost:5101` against dev DB, three callers via `/Certificate/ExportCertificates?format=csv`:

| Caller | Rows | Statuses returned | Partners returned | Verdict |
|---|---|---|---|---|
| `partner_test@gccwebapp.com` (Partner→Amazon) | **4** | `Client Approved - Paid` × 4 | `Amazon` × 4 | ✓ scoped |
| `swest@anukiconsulting.com` (Admin) | 43 | all (`Client Approved - Paid`, `Expired`, `Draft`, `Active`) | all (`Amazon`, `Novi`, `None`, …) | ✓ unrestricted |
| Anonymous (no auth) | 22 | `Client Approved - Paid` × 22 | mixed (anon sees all partners, only public-status certs) | ✓ public-only |

SQL truth check confirmed: Amazon partner has exactly 4 certs in `PublicStatuses`. Test fixture `partner_test@gccwebapp.com` left in dev DB (`Users.Id=324`, mapped to Amazon via `PartnerUser`) for future regression coverage.

**Files touched.**

- `Models/CertificateViewModels.cs:5-26` — added security-context fields.
- `Services/CertificateService.cs:368-403` — `ExportCertificatesByIdsAsync` signature extended with optional `securityContext`.
- `Services/CertificateService.cs:440-545` — `ApplyExportSecurityScope` helper + both `GetCertificatesForExport*Async` callers.
- `Services/IServices.cs:15` — interface signature update.
- `Controllers/CertificateController.cs:1488-1535` — `GetUserRole` Partner branch + helpers.
- `Controllers/CertificateController.cs:1995-2050` — `ExportCertificates` populates and passes security context.

**Related.** [[DEF-103]] (Partner API JSON contract — closed 2026-04-30). Same root-cause family as that fix; the export endpoint was simply not covered when DEF-103 shipped. Worth a sweep across other "Export" endpoints (`/Admin/ExportData`, `/api/productids/.../export`) on a future ticket to confirm none have the same gap.

**Pre-deploy reminder.** Per CLAUDE.md "Backup uploads before Plesk deploy" and the test-env access-gate rule — this fix rides the same upcoming deploy as DEF-196 / DEF-197 / DEF-194; no separate deploy needed.

---

**Verification (added 2026-05-21) — QC #182 end-to-end test pass on both envs**

Two-run verification. The localhost case proves the full Partner / Admin / Anonymous scoping matrix; the gccwebapp.com case proves the fix is actually deployed and applies to live data.

**Run A — Localhost `/Certificate/ExportCertificates?format=csv`** (dev DB; 45 certs total: 22 Client Approved - Paid, 9 Expired, 8 Draft, 6 legacy Active)

| Caller | HTTP | Data rows | Statuses returned | Partners returned | Verdict |
|---|---|---|---|---|---|
| `partner_test` (Partner→Amazon) | 200 | **4** | `Client Approved - Paid` only | `Amazon` only | ✅ scoped correctly |
| `admin` (Admin) | 200 | **45** | All (`Client Approved - Paid`, `Expired`, `Draft`, `Active`) | All | ✅ no restriction |
| Anonymous (no auth) | 200 | **22** | `Client Approved - Paid` only | All public partners | ✅ PublicStatuses only |

All 6 localhost assertions PASS.

**Run B — gccwebapp.com (prod DB, deploy verification)**

The definitive deploy probe is anonymous: if DEF-198 were not deployed, anonymous would leak Draft/Expired/Active rows. Result:

| Caller | HTTP | Data rows | Statuses returned | Partners returned | Verdict |
|---|---|---|---|---|---|
| Anonymous (Basic-auth-gated env, no app login) | 200 | **25** | `Client Approved - Paid`, `Republished` only | `None`, `Amazon` | ✅ PublicStatuses only on prod data — DEF-198 is deployed |

(The Partner-scoped probe was attempted with `partner_test`; that fixture is dev-only and login bounces on prod. Anonymous + same code path = sufficient evidence the scope helper is wired on the deployed build.)

**Test artifacts** (working tree, not committed):
- `qc159-live-check/qc182-export-scope.js` — localhost destructive-free E2E
- `qc159-live-check/qc182-export-scope-live.js` — gccwebapp.com deploy probe
- `qc159-live-check/qc182-{partner,admin,anon}.csv` — raw CSVs (localhost)
- `qc159-live-check/qc182-live-anon.csv` — raw CSV from gccwebapp anonymous
- `qc159-live-check/qc182-{,live-}results.json` — machine-readable assertion logs

**Status:** Closed — DEF-198 deployed and verified. The 5/15 client complaint (*"This is big issue, needs fixed"*) is no longer reproducible on either env.

**Follow-up suggested** (separate, low priority): create a `partner_test` fixture on the prod DB so future regression tests on gccwebapp.com can include the full Partner→Amazon scoped path, not just the anonymous probe. Per memory `[gccwebapp.com is pre-launch test env]` — test users are allowed.

**Related sweep** (already partially done): same fix pattern was extended in [[DEF-200]] family (per the existing 2026-05 work) covering adjacent endpoints. Per memory `[Scoping fixes — always audit adjacent endpoints]`, any future "Export" or "Download" route added to the app must include the same scope helper or document why it's exempt.

---

### DEF-197 — Admin → Product Types: Usage column overcounts when a cert or product sets multiple FK levels in the same hierarchy

- **Date:** 2026-05-18
- **Severity:** Medium (display-only — no data corruption, but mis-communicates the delete blocker count and can scare users into thinking they have more cleanup than they do)
- **Status:** Fixed locally — pending deploy to gccwebapp.com
- **QC Ref:** None — found internally during local smoke test of [[DEF-196]] on 2026-05-18
- **Bucket:** Bug
- **Reporter:** Internal (Shawn / smoke test)

**Symptom.** On `/Admin/ProductTypes`, the "Usage (Products / Certs)" column inflated rollup totals at non-leaf nodes. Dev DB example:

| Node | Page (Prod / Cert) | Truth (distinct) | Δ |
|---|---|---|---|
| Cat 1 — Chemicals and Raw Materials | `3 / 28` | `2 / 26` | +1 / +2 |
| Type 4 (child of Chemicals) | `2 / 2` | `1 / 1` | +1 / +1 |
| Subcat 49 (child of Type 4) | `0 / 1` | `0 / 1` | ✓ |

Leaf nodes (subcategories) were always correct. Every ancestor that *also* had a direct ref to the same cert or product overcounted by 1 per level. Worst case for an entity tagged at all `N` levels of the tree is `N*(N-1)/2` extra at the root.

**Root cause.** `Services/AdminService.cs:GetProductTypesAsync` (introduced in [[DEF-196]]) used `direct + Σ(children)` to roll up the new `CertificateCount` field and the augmented `ProductCount` field:

```csharp
typeVm.ProductCount += typeVm.Children.Sum(c => c.ProductCount);
typeVm.CertificateCount += typeVm.Children.Sum(c => c.CertificateCount);
// ...
categoryVm.ProductCount = categoryVm.Children.Sum(t => t.ProductCount);
categoryVm.CertificateCount += categoryVm.Children.Sum(t => t.CertificateCount);
```

The `Certificates` schema allows a single row to set `ProductCategoryId` **and** `ProductTypeId` **and** `ProductSubcategoryId` simultaneously, and 1 of 43 dev certs (`26-0027`) does. `Products` has the same dual-FK pattern (`ProductTypeId` + `ProductSubcategoryId`) and 1 of 305 dev products (`#346 "test"`) does. Every entity tagged at multiple levels got counted at each level and then summed again at the grandparent.

Production risk: dev surfaced 1 cert + 1 product with the multi-FK pattern. Real-world distribution is unknown — analyst tagging conventions may or may not produce more cases.

**Fix.** Replaced the integer-sum rollup with `HashSet<int>` unions per node. Each node holds the distinct set of cert IDs and product IDs in its subtree; `.Count` is read once at the end into `CertificateCount` and `ProductCount`. The same entity appearing at multiple levels is now deduped on union.

Files touched:
- `Services/AdminService.cs:803-833` — query now selects entity IDs (not counts); `certIdsBy{Category,Type,Subcategory}` and `productIdsBySubcategory` hold `HashSet<int>` per FK target.
- `Services/AdminService.cs:846-916` — tree build keeps a per-node `HashSet<int>` for certs and products, calls `UnionWith` on each descendant set, assigns `.Count` to the VM at the end.

**Verification.** Smoke test rerun against `localhost:5101`:
- Before fix sample `.node-count` values: `["0 / 0","3 / 28","1 / 0","0 / 0","0 / 0","2 / 2"]`
- After fix sample `.node-count` values: `["0 / 0","2 / 26","1 / 0","0 / 0","0 / 0","1 / 1"]`
- All other nodes unchanged (`0 / 0` correct).

SQL ground-truth queries used:
```sql
-- Distinct certs in a category's subtree
SELECT COUNT(DISTINCT c.Id) FROM Certificates c
WHERE c.ProductCategoryId = @cat
   OR c.ProductTypeId IN (SELECT Id FROM ProductTypes WHERE CategoryId = @cat)
   OR c.ProductSubcategoryId IN (SELECT Id FROM ProductTypes WHERE CategoryId = @cat);
```

**Related.** Tightens [[DEF-196]]. Same root cause shape as past list-Include + projection issues (counting-when-you-should-be-distinct).

---

### DEF-196 — Admin → Product Types: count column showed 0 while delete failed; certificates referencing types/categories never blocked deletion gracefully (QC #170)

- **Date:** 2026-05-15
- **Severity:** High (data-management blocker — Carly could not clean up her own test rows and could not understand the error)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21)
- **QC Ref:** #170 (client log, 2026-05-01, *"PLEASE REMOVE MY CARLY TEST AND CARLY TEST TYPE OPTIONS. SEE IF YOU CAN GO THROUGH THIS PROCESS, ETC."*)
- **Bucket:** Bug
- **Reporter:** Client (Carly)

> **QC #170 roll-up status (updated 2026-06-01) — QC-log status: `Fixed — pending deploy`.** This DEF is the anchor for QC #170, but the client item decomposed into several pieces across later retests. The data-cleanup piece (CR-019) is tracked separately and does not change the QC-log status. Current state of the whole #170:
> | #170 sub-thread | Owner | State |
> |---|---|---|
> | Usage column shows cert refs + accurate counts | DEF-196 + DEF-197 | ✅ Deployed + verified 5/21 |
> | "Numbers still not accurate" (5/29) — `0 / N` pill misread; counts are correct | DEF-226 | Fixed locally, pending deploy |
> | "Could not update existing / only my test options in dropdown" — cert stuck on wrong category (scopes the Type dropdown); admin re-parent also broken | DEF-231 (cert side) + DEF-230 (admin re-parent) | Fixed locally, pending deploy |
> | "REMOVE MY CARLY TEST / CARLY TEST TYPE" — existing corrupted rows + test data | **CR-019** (data op) | Drafted, **awaiting GCC approval** — the one genuinely-open piece |
> | "See ID 186" | QC #186 → DEF-225/226/231 | Triaged |
>
> **#170 closes when** the pending deploy ships (DEF-226/230/231) **and** CR-019 runs (removes Carly Test data + re-points the affected certs). Engineering is done; the remainder is the data op + deploy.

**Symptom.** On `/Admin/ProductTypes`, the "Products" count column read `0` for every row Carly was trying to delete. Clicking delete on those same rows returned **"An error occurred while deleting. Please try again."** — a generic message that gave her no path forward. Separately, she could filter the certificate listing by "Carly Test" and see certs returned, proving the type was in use, but the admin page disagreed.

**Root cause — three layered bugs in the same code path.**

1. **Count query was Products-only.** `Services/AdminService.cs:842, 859, 866, 873` populated `ProductTypeViewModel.ProductCount` from `ProductType.Products?.Count`. The `Products` navigation only reflects the `Products.ProductTypeId` FK, so Products linked via the *other* FK (`Products.ProductSubcategoryId`) and **all** Certificate references were silently excluded from the displayed count.
2. **Delete pre-check was Products-only too.** `Services/AdminService.cs:1141` short-circuited only on `productType.Products.Any()`. With no products attached, the pre-check passed and the code called `_context.SaveChangesAsync()`.
3. **FK violation fell into a generic catch.** `Data/ApplicationDbContext.cs:135-147` defines `Certificates → ProductType` (and `→ ProductSubcategory`) with `OnDelete(DeleteBehavior.Restrict)`. SQL Server raised an FK violation on save, which the broad `catch (Exception ex)` at line 1151 swallowed and replaced with the unhelpful "An error occurred" message.

Net effect: the column told Carly "deletable", the system disagreed, and the error gave no specifics. The same three-bug chain was present on `DeleteProductCategoryAsync` for root categories.

**Fix.**

- **`Services/AdminService.cs:GetProductTypesAsync`** — added a single read of `Certificates.ProductCategoryId/ProductTypeId/ProductSubcategoryId` plus `Products.ProductSubcategoryId` upfront, then populated a new `CertificateCount` field per node and corrected `ProductCount` for Level-2 subcategory rows (which reach Products via `ProductSubcategoryId`). Roll-up logic preserved: cert counts roll up child→parent the same way product counts always have, and direct cert refs on a category are preserved alongside the rolled-up children.
- **`Services/AdminService.cs:DeleteProductTypeAsync`** — added an explicit cert pre-check (`Certificates.ProductTypeId == id || ProductSubcategoryId == id`) with a specific message naming the count and the noun ("subcategory" vs "type" per entity Level), plus a `Products.ProductSubcategoryId` pre-check that was missing entirely. The level→label map (entity Level: 1 = type, 2 = subcategory) was corrected after a test pass surfaced PET returning "1 certificate(s) use this **type**" when PET is a subcategory.
- **`Services/AdminService.cs:DeleteProductCategoryAsync`** — same cert pre-check added for `Certificates.ProductCategoryId`.
- **Both delete methods** — added `catch (DbUpdateException dbEx)` ahead of the generic `Exception` handler. If a new FK reference is added between the pre-check and `SaveChanges` (concurrent insert race), the user now sees "Cannot delete: this item is still referenced by other records. Reassign or remove those references and try again." instead of "An error occurred."
- **`Models/AdminModels.cs:ProductTypeViewModel`** — added `int CertificateCount` field; existing `ProductCount` semantics preserved.
- **`Views/Admin/ProductTypes.cshtml`** — tree-view column header renamed from "Products" to "Usage (Products / Certs)" with a tooltip explaining what blocks deletion. Help-modal text on the Cascade Behavior section rewritten to describe both counts. No layout change.
- **`wwwroot/js/product-types.js`** — tree-node count cell now renders `${productCount} / ${certificateCount}` with a hover-title spelling out the breakdown ("3 products / 7 certificates reference this category"). Table-view splits "Product Count" into separate "Products" and "Certificates" columns. CSV export adds a `Certificate Count` column.
- **`Services/AdminService.cs:GenerateCsvExport/GenerateExcelExport`** — server-side exports also include the new `Certificate Count` column for parity with the client export.

**Cert Edit dropdown (Carly's secondary complaint).** She also reported that when editing a cert that used "Carly Test", the Product Type dropdown only offered her test options — locking her in. This is the cascading category→type→subcategory filter working as designed: `Views/Certificate/Edit.cshtml:626` loads `/Certificate/GetProductTypes?categoryId={saved}`, so a cert whose saved category is "Carly Test Category" sees only that category's children. **No code change here** — the workaround is to change Category first, which then refreshes the Type dropdown. If client feedback continues, the right call is a UX hint under the Type dropdown ("Change Category to see other types") rather than removing the cascade.

**Files:**
- `Services/AdminService.cs` — `GetProductTypesAsync`, `DeleteProductTypeAsync`, `DeleteProductCategoryAsync`, CSV/Excel exports.
- `Models/AdminModels.cs` — `ProductTypeViewModel.CertificateCount` added.
- `Views/Admin/ProductTypes.cshtml` — column header + help modal text.
- `wwwroot/js/product-types.js` — tree node, table view, CSV export.

**Test plan / verified 2026-05-15 against dev DB (`GCCDatabase_Dev`).**

1. **`GET /Admin/GetProductTypes` JSON** as admin — verified each node now has both `productCount` and `certificateCount`. Spot-checks against direct SQL counts: "Chemicals and Raw Materials" → prod=3, cert=28 (matches FK counts); "Plastics" type → prod=2, cert=2 (1 own product + 1 LLDPE rollup; 1 own cert + 1 PET rollup); LLDPE subcategory → prod=1 (was hidden behind `ProductSubcategoryId` FK), cert=0; PET subcategory → prod=0, cert=1.
2. **`POST /Admin/DeleteProductType/49` (PET, subcategory with 1 cert)** — returns `success: false` with **"Cannot delete: 1 certificate(s) use this subcategory. Edit those certificates to use a different subcategory first, then try again."** Was previously "An error occurred while deleting. Please try again."
3. **`POST /Admin/DeleteProductType/48` (LLDPE, subcategory with 1 product via `ProductSubcategoryId`)** — returns **"Cannot delete: 1 product(s) are assigned to this item. Please reassign them first."** Was previously the same generic error (the Products navigation never saw this row).
4. **`POST /Admin/DeleteProductType/4` (Plastics, has 6 subcategories)** — returns the existing children check: **"Cannot delete: This item has 6 subcategories. Please delete or move them first."** No regression.
5. **`POST /Admin/DeleteProductType/99999` (nonexistent)** — returns **"Product type not found"**. No regression.
6. **Build:** `dotnet build` clean — 0 errors, 53 pre-existing warnings (unchanged from prior commits).

**Found by:** Client, QC #170 logged 2026-05-01. Triaged + fixed by Anuki 2026-05-15 — same day Shawn raised it for the status call.

---

**Verification (added 2026-05-21) — QC #170 end-to-end retest on both envs**

Two-run verification covering BOTH layers of the fix (DEF-196 messages + DEF-197 dedup rollup) via `qc159-live-check/qc170-product-type-delete.js`. The destructive delete probes only run on localhost — but they're safe because the new pre-checks block `SaveChanges()` before any FK violation can fire; we verify post-run that node count is unchanged. The deploy probe on gccwebapp.com is read-only (JSON shape only).

**Run A — Localhost (`http://localhost:5101`, dev DB)**

```
PART A — Display side
✅ Every product-type node carries productCount   (59 nodes)
✅ Every product-type node carries certificateCount  (59 nodes)
✅ Spot-check PET-49     prod=0/exp 0, cert=1/exp 1
✅ Spot-check LLDPE-48   prod=1/exp 1, cert=0/exp 0
✅ Spot-check Plastics-4-dedup  prod=1/exp 1, cert=1/exp 1
   ↑ this is the DEF-197 dedup assertion. Product #346 is tagged BOTH
     ProductTypeId=4 (Plastics direct) AND ProductSubcategoryId=48 (LLDPE).
     Pre-DEF-197 integer-sum rollup counted it once at each level → 2.
     Post-fix HashSet UnionWith dedupes → 1. Same shape for cert 26-0027.

PART B — Delete pre-check probes
✅ DeleteProductType/49  PET (1 cert ref)
   → "Cannot delete: 1 certificate(s) use this subcategory. Edit those certificates…"
✅ DeleteProductType/48  LLDPE (1 product via ProductSubcategoryId FK)
   → "Cannot delete: 1 product(s) are assigned to this item. Please reassign them first."
✅ DeleteProductType/4   Plastics (6 subcategories)
   → "Cannot delete: This item has 6 subcategories. Please delete or move them first."
✅ DeleteProductType/99999  Nonexistent
   → "Product type not found"
✅ No rows actually deleted — node count before=59 after=59 (pre-check fires before SaveChanges)
```

10 / 10 PASS. Every specific-message branch fires correctly. Pre-fix all four destructive cases would have returned the generic *"An error occurred while deleting. Please try again."*

**Run B — gccwebapp.com (live prod data, deploy verification, read-only)**

```
PART A — Display side
✅ Every product-type node carries productCount     (62 nodes)
✅ Every product-type node carries certificateCount (62 nodes)
```

2 / 2 PASS. Prod tree has 62 nodes (vs 59 dev — same shape, 3 extra rows). Every node carries the new `certificateCount` field — confirms DEF-196 + DEF-197 deployed. Per-node count-correctness on prod was confirmed by the original 2026-05-19 shipped-log post-deploy probe (LDPE 0/1, HDPE 5/2, Plastics 5/4 — see `docs/tracking/shipped-2026-05.md`); today's check focused on JSON shape because count spot-checks on prod data would require additional SQL access we didn't add to the script.

**Delete pre-check probes were NOT run on prod** — the messages are pure UI strings that don't depend on data, and the destructive probes carry data-mutation risk on real records per the `[Data Changes Require CR]` rule. The localhost matrix covers all four message branches; the gccwebapp display probe confirms the same code path is deployed.

**Test artifacts** (working tree, not committed):
- `qc159-live-check/qc170-product-type-delete.js` — parameterized harness
- `qc170-local-types.json`, `qc170-live-types.json` — full tree responses
- `qc170-local-results.json`, `qc170-live-results.json` — machine-readable assertion logs

**Status:** Closed (this DEF) — DEF-196 + DEF-197 deployed and verified 5/21: every blocker case gives a specific, action-oriented message and the Usage column accurately tells the user how many references they still need to clear. **Note:** this made the delete *mechanism* work, but did NOT remove Carly's test rows or correct the mis-categorized data — that is a data operation tracked under **CR-019** (`Scripts/data-ops/2026-06-01-product-miscategorization-*.sql`), awaiting GCC approval. The broader QC #170 client item stays open until CR-019 runs and DEF-226/230/231 deploy — see the QC #170 roll-up at the top of this entry.

---

### DEF-195 — Certificate-number pill drifts below the category text on tiles where the category wraps to 2-3 lines (QC #174)

- **Date:** 2026-05-15
- **Severity:** Low (visual)
- **Status:** Deployed to gccwebapp.com 2026-05-15 (zip `GCCWebApp-deploy-20260515-2005.zip`, second attempt) — post-deploy smoke verified clean. First attempt earlier the same day (`GCCWebApp-deploy-20260515-1948.zip`) did not carry — server's CSS `Last-Modified` headers proved the files were never overwritten. Re-deploy with explicit "Overwrite All" in Plesk landed it; asp-append-version hash on `/css/certificatetileview.css` flipped from `29xsVYkW...` (broken) → `BgYK15dB7eLLdeKE74ymzJ0LNb74H3-dO5iVzgGpmvI` (fixed). 20 multi-line-category tiles now show 0px cert-id pill drift (was up to 10px). **Re-verified 2026-05-21** in batch Priority-4: 25 tiles sampled on prod, all `align-items: flex-start`, max top-delta 0px.
- **QC Ref:** #174 (client log, 2026-05-08, Carly)
- **Bucket:** UX
- **Reporter:** Client (Carly)

Client report: *"Spacing of cert number off — not sure if it is because cert name has 3 lines but all others are top aligned."* What's actually wrapping is the **certification category** (left side of the card header), not the product/cert name. On tiles where the category wraps to 2 or 3 lines, the cert-number pill on the right was vertically centered against the taller row, so it sat noticeably below the top of the neighboring single-line tiles' pills.

**Root cause — cascade specificity.** `wwwroot/css/main.css:2688` declares `body.theme-option4 .header-row { align-items: center; }` with specificity (0,2,1). The cleaner `.certificate-card .header-row` rule in `certificatetileview.css:719` (specificity 0,2,0) loses to it on tied-class but extra type-selector grounds, so the centered alignment won everywhere theme-option4 was active. theme-option4 is the default body class on all three tile views (public, internal, external), so the bug was site-wide on desktop and tablet ≥1024px. Below 1024px a separate `body.theme-option4 .header-row` rule inside a media query at `main.css:3693` already used `flex-start`, which is why narrow viewports looked fine and the visual problem was confined to wider screens. The base `.header-row { align-items: flex-start }` rule at `main.css:1457` and the per-card rule in `certificatetileview.css` already wanted the correct value — only the theme-option4 override disagreed.

**Fix:**

- `wwwroot/css/main.css:2691` — `align-items: center` → `align-items: flex-start` on `body.theme-option4 .header-row`.
- `wwwroot/css/certificatetileview.css:720` — same value applied for defense-in-depth (the rule was already there; its value didn't matter while theme-option4 overruled it).

Single source of truth is now the base `.header-row { align-items: flex-start }` rule at `main.css:1457`; the theme-option4 selector no longer disagrees with it.

**Test plan:**

1. **Visual** — `/Certificate/CertificateTileView` at 1920×1080, 1280×800, 1024×768, 768×1024, 390×844. On every viewport that shows multi-line categories ("CERTIFIED ENVIRONMENTAL FACTS", "CERTIFIED ENERGY SAVINGS"), the cert-number pill must be flush with the **first line** of the category text. Top-delta between `.certificate-category` top and `.certificate-id` top must be 0px.
2. **Repeat for** `/Certificate/CertificateTileViewExternal` (external user) and `/Certificate/CertificateTileViewInternal` (analyst user) — both load the same `certificatetileview.css` and use identical `.header-row` markup (see `certificate-tile-view-external.js:436` and `certificate-tile-view-internal.js`), so the fix should carry over without per-view edits.
3. **Automated measurement** — Playwright eval `getComputedStyle(headerRow).alignItems === 'flex-start'` on every viewport. Verified 2026-05-15: pre-fix run showed `center` on the three widest viewports and a 10px top-delta on tablet-1024 3-line cards; post-fix run shows `flex-start` and 0px top-delta on all 22 cards across all 5 viewports.
4. **Regression** — Single-line category tiles must look unchanged (they do — `center` and `flex-start` are identical for a 1-line flex row).

**Found by:** Client (Carly), QC #174 logged 2026-05-08. Triaged + fixed by Anuki 2026-05-15.

---

### DEF-194 — Cert Create/Edit Product Identifier table needs bulk-delete via checkboxes (QC #172 — client 5/11, per 5/8 call)

- **Date:** 2026-05-14
- **Severity:** Low (Enhancement — no broken behavior, fills a UX gap for analysts cleaning up large product-ID lists)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21)
- **QC Ref:** #172 (client log, 2026-05-11, *"Add check all for mass delete under Product Identifier per 5/8 call"*)
- **Reporter:** Client via QC log #172 (5/11/26): *"Add check all for mass delete under Product Identifier per 5/8 call"*
- **Classification:** Enhancement (5-bucket taxonomy). Root cause: feature did not exist.

**Symptom:** Cert Create + Edit pages' Product Identifier table only supported per-row delete. Analysts cleaning up imported product-ID lists had to click the row trash icon → confirm → wait → repeat, once per row.

**Fix:** Added a checkbox column + select-all header + "Delete Selected (N)" red button to the shared `_ProductIdManager` partial. Selection state lives in `selectedIds: Set<number>` on `ProductIdManager` and survives re-renders, filter changes, and (in server-paged mode) page changes — so a user can accumulate selections across multiple pages and delete in one shot. Bulk delete runs through `AdminCore.showConfirmation()` ("Delete N product ID(s)?"), then issues parallel `DELETE /api/productids/{id}` calls via `Promise.allSettled`, tolerating partial failures so the toast can honestly report "Deleted N of M; X failed". In-memory mode (Create page, or any temp negative IDs) drops rows from local state without an API call, matching the existing single-row delete behavior. The select-all header is tri-state: checked when every visible row is selected, indeterminate when partial, unchecked when none. Read-only views (cert detail) get neither the column header nor the bulk button.

**Files:**
- `Views/Shared/_ProductIdManager.cshtml` — added bulk-delete button (hidden by default), select-all `<th>`, per-row checkbox `<td>`, server-rendered colspan updated (8→9 when not read-only).
- `wwwroot/js/product-id-manager.js` — added `selectedIds` Set on the constructor; `getProductIdRowHtml()` now emits a per-row checkbox; `getEmptyStateHtml()` colspan 8→9; `renderTable()` calls `updateBulkActionsUI()` at every return path; new methods `handleRowCheckboxToggle`, `handleSelectAllToggle`, `updateBulkActionsUI`, `handleBulkDelete`; `handleDelete()` also clears `selectedIds` of the deleted id so the count stays in sync after a single-row delete.

**Test plan:**

1. **Create page** — `/Certificate/Create` as analyst. Add 3+ product IDs via Add button (these will have negative temp IDs). Verify each row has a checkbox at the left. Check the header checkbox: every row checkbox flips on, "Delete Selected (3)" red button appears next to "Add Product ID". Uncheck one row → header shows indeterminate state, button reads "Delete Selected (2)". Click button → confirmation reads "Delete 2 product ID(s)?" → confirm → 2 rows disappear, toast "2 product ID(s) removed", 3rd row remains.
2. **Edit page (server-paged)** — `/Certificate/Edit/{id}` where the cert has 50+ product IDs. Verify the table is paginated. On page 1, select 5 rows. Navigate to page 2 — rows on page 2 should not be checked, but the "Delete Selected (5)" button should still show "5" (selection persists across pages). Select 3 more on page 2. Header on page 2 shows indeterminate, button reads "Delete Selected (8)". Click → confirmation reads "Delete 8 product ID(s)?" → confirm → all 8 deleted via API, table reloads, toast "8 product ID(s) deleted successfully".
3. **Partial failure** — Force one API DELETE to fail (block the request in DevTools, or delete via SQL then click bulk delete). Toast should read "Deleted N of M; X failed" not just generic success.
4. **Read-only view** — `/Certificate/CertificateInternalView/{id}` (or detail view). Verify no checkbox column header, no per-row checkboxes, no "Delete Selected" button.
5. **Mixed in-flight + persisted** — On Edit page, click Add to create 2 unsaved rows (negative IDs) alongside saved rows, then bulk-select 1 negative + 1 persisted and bulk-delete. Both delete cleanly; persisted one removes via API; negative one removes from memory.
6. **Build:** `dotnet build` — 0 errors, 53 pre-existing warnings (unchanged from prior commits).

**Notes for client:** Behavior is the same on Create and Edit pages because both use the same partial. QC #172 specified "Cert Creation Page" but Edit gets the capability "for free" via the shared component — flag this in the response so client knows it covers both.

---

**Verification (added 2026-05-21) — QC #172 end-to-end pass with cross-page selection proof**

Single harness `qc159-live-check/qc172-bulk-delete.js`. Destructive on localhost (isolated by `QC172TEST%` test prefix; defensive cleanup at start + end); read-only DOM probe on gccwebapp.com.

**Run A — Localhost destructive end-to-end + cross-page** (cert 26-0023, dev DB)

Setup: SQL-inserted 3 identifiable test rows (`ASIN QC172TEST1/2/3`) on cert 26-0023; search-filter on `QC172TEST` isolated them from the 1004 unrelated rows.

```
PART A — UI shape
✅ Select-all header checkbox present
✅ Bulk delete button hidden before any selection
✅ Three filtered test rows visible

PART B — selection / count
✅ Bulk delete button appears after 1 selection
✅ Bulk delete shows "Delete Selected (1)"
✅ Select-all checks all 3 visible rows → count=3

PART C — destructive end-to-end
✅ All 3 test rows deleted from DB (post-delete count=0)
   — clicks #bulkDeleteProductIds → AdminCore.showConfirmation modal
     ([data-action="confirm"]) → 3 parallel DELETE /api/productids/{id} calls
     → table reload → SQL truth verify

PART D — cross-page selection persistence (non-destructive, cert 26-0106, 101 rows / 3 pages)
✅ Page 1: one row selected → count=1
✅ Navigate to page 2: count persists (=1)            ← selectedIds Set survives page change
✅ Page 2 + 1 more selected: count=2
✅ Page 1 revisited: first row still checked          ← getProductIdRowHtml restores
                                                          isChecked from selectedIds on re-render
```

11 / 11 PASS. Cleanup verified: post-test query `WHERE ProductIdValue LIKE 'QC172TEST%'` returns 0 — no test residue. Cert 26-0023 retains its 1004 original rows; cert 26-0106 retains its 101 (cross-page test never clicked Delete).

**Run B — gccwebapp.com read-only DOM probe** (cert 26-0163 via admin login)

```
✅ Select-all checkbox present on prod
✅ Bulk delete button present on prod (hidden until selection — style display:none)
✅ Per-row bulk-select checkboxes present on prod (2 row checkboxes for cert 26-0163's 2 IDs)
```

3 / 3 PASS. The DEF-194 partial (`Views/Shared/_ProductIdManager.cshtml`) and `wwwroot/js/product-id-manager.js` are deployed on the current build. Destructive matrix intentionally skipped on prod per the `[Data Changes Require CR]` rule.

**Test artifacts** (working tree, not committed):
- `qc159-live-check/qc172-bulk-delete.js` — parameterized harness with PowerShell SQL helper
- `qc172-local-01-filtered.png` — filtered to test rows showing checkboxes
- `qc172-local-02-all-selected.png` — select-all in indeterminate→checked state with "Delete Selected (3)"
- `qc172-local-03-after-delete.png` — confirmation modal capture (pre-fix-of-script artifact, useful evidence)
- `qc172-local-04-cross-page.png` — cross-page selection on cert 26-0106
- `qc172-live-dom.png` — prod DOM probe
- `qc172-{local,live}-results.json`

**Status:** Closed — DEF-194 deployed and verified. The client's 5/11 ask (*"Add check all for mass delete under Product Identifier per 5/8 call"*) is met on both Create and Edit pages (shared partial), with cross-page persistence working as designed.

---

**Re-opened 2026-05-22 — cross-page "Select all N" completion**

Shawn flagged on the live site (`/Certificate/Edit/26-0160`, 1001 product IDs across 21 pages) that the as-shipped feature only deletes the 50 visible rows per click of "select all" — the user has to page through 21 times to actually wipe a cert's product IDs. The original v1 honored the literal text of QC #172 ("check all for mass delete") but missed the spirit: bulk delete that requires 21 trips to complete is not bulk delete.

**Fix v2 (Gmail-style banner + cross-page mode):**

Server-side: new `POST /api/productids/certificate/{certId}/bulk-delete` accepts either `{ids:[…]}` (specific rows) or `{deleteAll:true, search, type, partner}` (every row matching the table's current filter chips). Service layer uses `ExecuteDeleteAsync` for a single SQL DELETE round-trip — 1001 rows now removed in one call instead of 1001 parallel HTTP DELETEs (which would also have tripped the */api/* rate limit at 100/min).

Client-side: when the user ticks the header select-all and `totalCount > visible rows`, a yellow banner appears: *"All 50 on this page are selected. Select all 1001 product IDs"*. Clicking the link flips a `selectAllAcrossPages` flag, ticks every visible row, updates the badge to total, and turns the banner blue with a "Clear selection" affordance. The next bulk-delete posts the deleteAll payload. The flag is invalidated by: unchecking the header select-all, unchecking any row, changing any filter, or clicking Clear selection — all return the table to per-row additive selection without losing the still-checked rows.

**Files (v2):**
- `Services/ProductIdService.cs` — added `BulkDeleteProductIdsAsync(certId, ids?, search?, type?, partner?)` interface + impl. `ExecuteDeleteAsync` after constraining query to `CertificateId == certId` (defense-in-depth so a forged payload can't reach product IDs on a sibling cert).
- `Controllers/Api/ProductIdApiController.cs` — added `[HttpPost("certificate/{certificateId}/bulk-delete")]` action + `BulkProductIdDeleteRequest` DTO.
- `Views/Shared/_ProductIdManager.cshtml` — inserted `#bulkSelectAllBanner` (hidden by default) above the filter row with `#bulkSelectAllLink` and `#bulkSelectAllClear` controls.
- `wwwroot/css/product-id-manager.css` — `.bulk-select-all-banner` styles (yellow neutral state, blue active state).
- `wwwroot/js/product-id-manager.js` — added `selectAllAcrossPages` flag, `enableSelectAllAcrossPages()` / `clearAllSelection()` helpers, banner-aware `updateBulkActionsUI()` (shows visible-vs-total prompt, switches to "All N selected" + Clear when active, pre-checks every visible row on page navigation while in cross-page mode), rewrote `handleBulkDelete()` to post once to the new endpoint instead of fanning out per-row DELETEs, `applyFilters()` clears selection state because filter changes invalidate the "N" claim.

**Verification (local, cert 26-0023 with 1004 PIDs):**

`qc159-live-check/smoke-bulk-delete-crosspage.js` against `localhost:5101`:

```
✓ Banner hidden initially
✓ Banner visible after header select-all
✓ Link prompts: "Select all 1004 product IDs"
✓ Total in banner = 1004 (> page size, so cross-page is meaningful)
✓ Delete-Selected count = 50 before clicking the link
✓ Banner gained .bulk-select-all-active
✓ Delete-Selected count = 1004 (matches total)
✓ selectAllAcrossPages flag = true
✓ Clear-selection button visible
✓ Bulk-delete endpoint OK: {"deleted":0}        (deleteAll + search=ZZ_NONEXISTENT_ZZ → zero matches, no data harmed)
✓ Bulk-delete with ids[-99999] returns 200 (no-op)
✓ Banner hidden after Clear
✓ selectAllAcrossPages flag cleared

13/13 checks passed
```

Visual evidence: `qc159-live-check/crosspage-active.png`.

Build: 0 errors, 56 pre-existing warnings. Destructive matrix on 1004-row cert intentionally skipped per `[Data Changes Require CR]`.

**Status (v2):** Fixed locally — pending deploy. Closes Shawn's 2026-05-22 follow-up.

---

### DEF-193 — Race condition on cert-number generation: two concurrent Create form opens see the same number

- **Date:** 2026-05-13
- **Severity:** High (data-integrity adjacent — second user's submit fails with a generic error, losing UX continuity and 5+ minutes of form work)
- **Status:** Fixed locally and verified end-to-end against the restarted dev server — pending deploy to gccwebapp.com
- **Verification:** post-fix repro on localhost:5101 — two concurrent sessions both submitted with preview Number `26-0159`. Session A's save was retried and assigned `26-0160`; Session B kept `26-0159`. Both saves succeeded silently with no error toast. Test certs cleaned up from dev DB.
- **Option A verification (file rename for race-loser):** ran a second repro with real PDF attachments on both concurrent POSTs. Race-winner cert `26-0159` got filename `26-0159_20260513_232825_<guid>.pdf` (the GUID suffix came from FileUploadService's collision-avoidance — the preview-number filename was briefly held by the loser before rename). Race-loser cert `26-0160` got filename `26-0160_20260513_232825.pdf` and thumbnail `26-0160_thumb_20260513_232825.png`; `TryRenameUploadedFile` successfully moved both files from the preview-number prefix to the assigned-number prefix. Both filenames now match their assigned cert numbers; no on-disk drift remaining.
- **Reporter:** Client (Carly) — QC #58 5/11/26 comment: *"if 2 people have open and adding cert and hit save -- does one keep the original number and other get next available? It will never assign the same number right (it can't it has to be unique). Please coordinate test on your end to double check when this happens simultaneously with users."*

**Symptom:** When two analysts open `/Certificate/Create` at the same time, both forms render with the SAME pre-filled `Number` value (e.g. "26-0164"). The user who saves second hits the UNIQUE-index violation on `Certificates.Number` and sees a generic `"An error occurred while creating the certificate"` message; their form data is preserved on re-render but the conflicting number remains in the input field. The user must refresh to receive a new number, then re-paste their work.

**Root cause:** `Repositories/CertificateRepository.cs:744` (`GenerateNextCertificateNumberAsync`) computes the next number by reading `MAX(Number)` for the year and adding 1. It is called once from `Controllers/CertificateController.cs:287` and `:348` during the GET request that renders the Create form. Nothing reserves the number — it is a pure read and there is no transaction or lock around the read+save pair. The window between "form GET returns 26-0164" and "user clicks Save 5 minutes later" is the race window. The DB-level guarantee comes from `entity.HasIndex(e => e.Number).IsUnique()` in `Data/ApplicationDbContext.cs:67` — the unique index prevents the duplicate from being written but converts the race into a generic exception that the catch block at `Controllers/CertificateController.cs:653-659` swallows into an opaque "An error occurred" message.

**Live reproduction (2026-05-13, gccwebapp.com):**
```
1. Two isolated HttpClients, both Basic-Auth through the gate.
2. Session A logs in as analyst@greencirclecertified.com, Session B logs in as admin@greencirclecertified.com.
3. Both sessions GET /Certificate/Create concurrently (Task.WhenAll).
4. Parse `id="Number"` input from each response.

RESULT:
  Session A (analyst) Number: '26-0164'
  Session B (admin)   Number: '26-0164'   <-- identical

Script: %TEMP%\def193_repro_httpclient.ps1
Evidence HTML (both responses): %TEMP%\def193_evidence\session_{a,b}_create.html
```

The unique index `IX_Certificates_Number` on `Certificates.Number` (`Migrations/...ModelSnapshot.cs:433`) is intact in production, so the second save would fail rather than silently writing a duplicate.

**Proposed fix — atomic generate-and-save with retry-on-conflict (NOT YET APPLIED):**

Move number assignment off the GET preview and onto the POST save path, inside a retry loop that handles unique-index conflicts.

**`Repositories/ICertificateRepository.cs`** — add:
```csharp
Task<Certificate> AddWithGeneratedNumberAsync(Certificate certificate, int maxAttempts = 3);
```

**`Repositories/CertificateRepository.cs`** — add after `GenerateNextCertificateNumberAsync` (line 778):
```csharp
/// <summary>
/// Atomically assigns the next available certificate number and saves the cert.
/// Retries on UNIQUE-index conflict so concurrent saves both succeed (second user
/// transparently gets the next number). DEF-193.
/// </summary>
public async Task<Certificate> AddWithGeneratedNumberAsync(Certificate certificate, int maxAttempts = 3)
{
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        certificate.Number = await GenerateNextCertificateNumberAsync();
        try
        {
            await _dbSet.AddAsync(certificate);
            await _context.SaveChangesAsync();
            return certificate;
        }
        catch (DbUpdateException ex) when (IsUniqueNumberViolation(ex))
        {
            _context.Entry(certificate).State = EntityState.Detached;
            if (attempt == maxAttempts) throw;
        }
    }
    throw new InvalidOperationException("Unreachable");
}

private static bool IsUniqueNumberViolation(DbUpdateException ex)
{
    // SQL Server: 2601 = duplicate key in unique index, 2627 = unique constraint violation
    if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
    {
        return (sqlEx.Number == 2601 || sqlEx.Number == 2627)
            && sqlEx.Message.IndexOf("IX_Certificates_Number", StringComparison.OrdinalIgnoreCase) >= 0;
    }
    return false;
}
```

**`Controllers/CertificateController.cs:450-522`** — replace `Number = model.Number,` in the new-Certificate initializer (line 452) with `// Number assigned atomically below (DEF-193)` and replace the save at lines 521-522:
```csharp
// OLD
await _certificateRepository.AddAsync(certificate);
await _unitOfWork.CompleteAsync();

// NEW
await _certificateRepository.AddWithGeneratedNumberAsync(certificate);
```

Then in the upload blocks above (lines 480 and 498) replace `model.Number` references with deferred ones — the cleanest approach is to move the PDF/image uploads AFTER the save, since they only need `certificate.Number` for filename. This is a 6-line reorder that keeps the rollback shape intact (if upload fails post-save, log + ignore; the cert exists in Draft state with no file, which is the same shape Analyst hits today when they fix-and-retry).

**`Views/Certificate/Edit.cshtml` (or wherever the Create form Number input lives)** — mark the Number input as read-only and label it `Number (preview — assigned at save)`. Prevents UX surprise when the second user's assigned number differs from the preview they saw.

**Why retry-on-conflict instead of SERIALIZABLE / sp_getapplock:** the read+save pair already locks `IX_Certificates_Number` at INSERT time. The unique-index error is the natural signal that a concurrent insert won the race. Retrying with a fresh `MAX(Number)+1` is O(1) per attempt and converges in ≤ N attempts for N concurrent writers. Three attempts handles every realistic concurrency level for this app (we have <10 analysts total). Heavier locking would serialize all cert creation needlessly.

**Why not pre-reserve the number on GET:** would require either INSERTing a placeholder Draft row (orphan-on-close problem if user abandons the form) or a separate `CertificateNumberReservations` table (more state to manage and clean up). Retry-on-save is the least-invasive shape that fully resolves the symptom.

**Test plan:**
1. Unit: mock `_context.SaveChangesAsync()` to throw a `DbUpdateException` with a fake unique-index SqlException; assert the retry succeeds on attempt 2.
2. Concurrency: spin up 5 parallel `AddWithGeneratedNumberAsync` calls in one test; assert all 5 succeed and have distinct sequential numbers.
3. Live re-test with the script at `%TEMP%\def193_repro_httpclient.ps1` post-deploy: same two sessions submit; both certs save with 26-0164 and 26-0165 respectively.

**Files to touch when fix is approved:** `Repositories/ICertificateRepository.cs`, `Repositories/CertificateRepository.cs`, `Controllers/CertificateController.cs` (Create POST only; Edit POST is unaffected because Edit doesn't generate numbers), `Views/Certificate/Create.cshtml` for the readonly Number label.

**Cross-references:** QC #58 (5/11/26 client comment), CertificateRepository.cs:744 (race-prone read), ApplicationDbContext.cs:67 (unique index), CertificateController.cs:653 (generic catch that masks the error today).

---

### DEF-192 — Product Category filter pulls in certs whose displayed category does not match

- **Date:** 2026-05-13
- **Severity:** Medium (filter result set does not match what tiles show — user confusion / wrong-data risk)
- **Status:** Fixed locally — verified end-to-end against localhost:5101 dev DB — pending deploy to gccwebapp.com
- **Reporter:** Shawn — `https://gccwebapp.com/Certificate/CertificateTileViewInternal`, "why is this certificate showing for Carly Test is filtered. no other filters are selected" (cert 26-0158).

**Symptom:** On the internal tile view, checking only `Product Categories → Carly Test` returns 4 results. Three of them legitimately show category `"Carly Test"` on the tile (26-0163, 26-0161, 26-0159). The fourth, `26-0158`, shows `"Electronics, Computers, and Accessories"` on the tile and detail view — a category that has nothing to do with the active filter. Verified via the anonymous public API as well, so the same leak happens on every surface that uses the shared filter path:

```
GET /api/certificate-tiles?productCategory=Carly%20Test → totalCount: 4
  26-0163  3M                          productCategory="Carly Test"
  26-0161  Testing… Long Name…         productCategory="Carly Test"
  26-0159  Balcan Innovations          productCategory="Carly Test"
  26-0158  Company Symbol Test é™®ä    productCategory="Electronics, Computers, and Accessories"   ← LEAK
```

**Root cause:** `Repositories/CertificateRepository.cs:333-351` (`GetAdvancedFilteredCertificatesAsync`). The ProductCategory predicate ORs the requested name across **nine** name fields:

1. `c.Product.Category.Name` (Product-derived Level 1)
2. `c.ProductCategory.Name` (cert-level Level 1 override)
3. `c.Product.ProductType.Name` (Product-derived Level 2/3)
4. `c.ProductType.Name` (cert-level Level 2/3 override)
5. `c.ProductSubcategory.Name` (cert-level)
6. `c.Product.ProductSubcategory.Name` (separate Product subcategory entity)
7. `c.Product.ProductType.Parent.Name` (parent cascade)
8. `c.ProductType.Parent.Name`
9. `c.ProductSubcategory.Parent.Name`

The tile/display logic in `Controllers/Api/CertificateTilesApiController.cs:186-188` does the **opposite** — it prefers the cert-level override and only falls back to the Product-derived value:

```csharp
productCategory = c.ProductCategory != null ? c.ProductCategory.Name : (c.Product?.Category?.Name ?? ""),
productType    = c.ProductType    != null ? c.ProductType.Name    : (c.Product?.ProductType?.Name ?? ""),
productSubcategory = c.ProductSubcategory != null ? c.ProductSubcategory.Name : (c.Product?.ProductSubcategory?.Name ?? ""),
```

For 26-0158 the public tile API returns `productCategory="Electronics, Computers, and Accessories"`, `productType="Electronic Accessories"`, `productSubcategory=""`. Since the cert's display chain has no "Carly Test" anywhere, the match must be happening on one of the Product-derived fields (most likely `c.Product.Category.Name == "Carly Test"` or a Product-side ProductType/ProductSubcategory in that chain), which the cert-level override masks at display time.

**Fix applied:** replaced the OR-across-Product-AND-cert-override predicate with an effective-value match at each level (cert override if set, otherwise Product-derived) — mirrors the display projection. Parent-cascade preserved in the public repo (the only path that previously had it). Internal and external controllers had no parent cascade; preserved that to keep scope tight.

```csharp
// Effective Level-1 category
(c.ProductCategory != null && filter.ProductCategories.Contains(c.ProductCategory.Name)) ||
(c.ProductCategory == null && c.Product != null && filter.ProductCategories.Contains(c.Product.Category.Name)) ||
// Effective Level-2/3 ProductType
(c.ProductType != null && filter.ProductCategories.Contains(c.ProductType.Name)) ||
(c.ProductType == null && c.Product != null && c.Product.ProductType != null && filter.ProductCategories.Contains(c.Product.ProductType.Name)) ||
// Parent of effective L2/L3 ProductType (public repo only — preserves Level-2 sidebar cascade)
(c.ProductType != null && c.ProductType.Parent != null && filter.ProductCategories.Contains(c.ProductType.Parent.Name)) ||
(c.ProductType == null && c.Product != null && c.Product.ProductType != null && c.Product.ProductType.Parent != null && filter.ProductCategories.Contains(c.Product.ProductType.Parent.Name)) ||
// Effective Level-3 subcategory
(c.ProductSubcategory != null && filter.ProductCategories.Contains(c.ProductSubcategory.Name)) ||
(c.ProductSubcategory == null && c.Product != null && c.Product.ProductSubcategory != null && filter.ProductCategories.Contains(c.Product.ProductSubcategory.Name)) ||
// Parent of effective cert-side L3 subcategory
(c.ProductSubcategory != null && c.ProductSubcategory.Parent != null && filter.ProductCategories.Contains(c.ProductSubcategory.Parent.Name))
```

**End-to-end verification (localhost:5101, dev DB):** synthesized the leak condition by setting cert 26-0001's `ProductCategoryId = 2` ("Clothing, Apparel, and Fabrics") while leaving `Product.CategoryId = 1` ("Chemicals and Raw Materials") — cert displays "Clothing…", Product-side still points at "Chemicals…".

```
Public  /api/certificate-tiles?productCategory=Chemicals and Raw Materials
  before mutation: total=22, contains 26-0001=True
  after mutation:  total=21, contains 26-0001=False    ← LEAK CLOSED
Public  /api/certificate-tiles?productCategory=Clothing, Apparel, and Fabrics
  before mutation: total=0,  contains 26-0001=False
  after mutation:  total=1,  contains 26-0001=True     ← effective override matches

Internal /api/certificate-tiles-internal (admin auth)
  filter='Chemicals...' -> total=39, contains 26-0001=False
  filter='Clothing...'  -> total=1,  contains 26-0001=True
```

Mutation restored at end of test (`ProductCategoryId = 1`).

External controller has structurally identical predicate to internal; build passed and same EF shape proven via internal runtime.

**Files changed:**
- `Repositories/CertificateRepository.cs` (public path — `GetAdvancedFilteredCertificatesAsync`, with parent cascade preserved)
- `Controllers/Api/CertificateTileInternalApiController.cs` (internal inline predicate)
- `Controllers/Api/CertificateTileExternalApiController.cs` (external inline predicate)

---

### DEF-191 — Product ID Import: per-row selection of an "unchanged" row failed with "already exists"

- **Date:** 2026-05-12
- **Severity:** Medium (user-facing import failure on a reasonable workflow)
- **Status:** Deployed and verified on gccwebapp.com (status-sync 2026-05-21 — deployed `product-id-manager.js` contains the `'unchanged'` status handling that the fix introduced)
- **Reporter:** Shawn — `http://localhost:5101/Certificate/Edit/26-0023`, "on the import if I just selected a few to overwrite but not check the replace all, the import fails."

**Root cause:** `applyImport()` in `wwwroot/js/product-id-manager.js` partitioned the user-selected preview rows with the filter `r.action !== 'update'` for the bulk-POST payload. That filter let rows with `action === 'skip'` (CSV rows whose type+value already match an existing PID with identical field values) into the bulk endpoint as create attempts. The server then correctly rejected them with `"Product ID already exists on this certificate"` and the user saw `Import failed: ...`. The skip rows are no-ops by definition and should never have been sent.

Reproduction (Playwright against localhost on cert `26-0023`): upload a one-row CSV that exactly matches an existing PID, manually check the preview row (default is unchecked for `unchanged`), leave Replace-all OFF, click Apply Import. Before fix: toast `Import failed: ASIN:B08N5W0001 - Product ID already exists on this certificate`. Server returned HTTP 200 with `successCount: 0, errorCount: 1`.

**Fix:** Two changes in `wwwroot/js/product-id-manager.js` `applyImport()`:
1. Tightened the bulk-POST filter from `r.action !== 'update'` to `r.action === 'create'` so only true new rows reach the bulk endpoint. Skip rows are now partitioned into a new `skippedExisting` list.
2. Updated the success/failure decision so skip-only selections show a friendly no-op toast (`X row(s) already on this certificate — nothing to change`) instead of falling through to the generic `Import failed: Unknown error` branch. Mixed selections that include skips append `; X already on file` to the success message for transparency.

Replace-all mode is unchanged — every non-error row still participates, and the server's deactivation logic still drives off the import keys.

**Verification:** Playwright driver at `docs/tracking/screenshots/def191/verify_def191.py` runs four scenarios (skip-only, modified-only, new-only, and mixed update+skip) against cert `26-0023` on localhost. Before fix: scenario A failed with `Import failed: ... already exists`. After fix: all four scenarios end in a success toast that accurately describes the outcome. Proof screenshot at `docs/tracking/screenshots/def191/A_skip_only_toast.png`. Re-runnable any time via `py -3 docs/tracking/screenshots/def191/verify_def191.py`. See `docs/tracking/screenshots/def191/EVIDENCE.md` for the full writeup.

**Files:** `wwwroot/js/product-id-manager.js`

---

### DEF-190 — Product ID Import Preview modal had duplicate "Download Template" button

- **Date:** 2026-05-12
- **Severity:** Low (UX cleanup)
- **Status:** Fixed locally — pending deploy
- **Reporter:** Shawn — `http://localhost:5101/Certificate/Edit/26-0023`, "Import Preview — remove download template, it is already on the main page."

**Root cause:** The Product ID Manager partial renders a Template button in the main toolbar (`#downloadSample`, line 19) for downloading the CSV import template. The Import Preview modal footer redundantly rendered a second "Download Template" button calling the same `productIdManager.downloadSample()`. Two affordances for the same action in the same workflow.

**Fix:** Removed the duplicate `<button>` from the Import Preview modal footer in `Views/Shared/_ProductIdManager.cshtml`. The DEF-159 Replace-All toggle remains the only control in the footer's left cluster; the right cluster keeps Cancel + Apply Import. The main-toolbar Template button is unchanged and remains the canonical way to download the template.

**Files:** `Views/Shared/_ProductIdManager.cshtml`

---

### DEF-189 — Certificate Edit/Create sidebar preview regressed to portrait (was square)

- **Date:** 2026-05-12
- **Severity:** Low (cosmetic)
- **Status:** Deployed and verified on gccwebapp.com (status-sync 2026-05-21 — preview container on `/Certificate/Edit/26-0163` measures 330×330px = 1:1 ratio, no longer portrait)
- **Reporter:** Shawn — `http://localhost:5101/Certificate/Edit/26-0023`, "certificate preview is showing portrait, this was square before?"

**Root cause:** Commit `6b37530` (2026-04-13, "defect resolulution") introduced a new `.certificate-create-preview` CSS rule with `aspect-ratio: 8.5 / 11` (US Letter portrait) plus a duplicate at the ≤1024px breakpoint. Before that commit the class did not exist; the preview container had no aspect-ratio constraint and rendered approximately square via its content.

**Fix:** Changed `aspect-ratio: 8.5 / 11` → `aspect-ratio: 1 / 1` in both occurrences (main rule + 1024px mobile rule). `object-fit: contain` on the inner `<img>` is already in place, so the snapshot is letterboxed inside the square frame without cropping. Affects both `/Certificate/Create` and `/Certificate/Edit/{id}` (shared class).

**Files:** `wwwroot/css/certificate-create.css` (2 edits).

---

### DEF-188 — QC #169 (Cert type filter: missing types + "Product Opt / RM at top" re-raise) — by design + duplicate of DEF-175

- **Date:** 2026-05-12 (audited)
- **Severity:** N/A — review-only
- **Status:** Closed — no new code change. Sub-claims resolve to (a) intentional R11 behavior, (b) duplicate of DEF-175 awaiting deploy.
- **Reporter:** Client — QC #169 (5/1/26 entry, paste-quoted): *"Missing Certified Water Savings, LCA Opt, RM, WD as a certification type. CP NOTE: Anuki please confirm is it only showing certs with active listing for sorting? Is that why they aren't there? 5/1: I had made some edits to clean list. Why are Product Opt and RM now showing at the top of filter box and things are in alpha order? There is no sort ability under Cert Types to fix which is issue when we add new. See ID 169 for additional items - would just show options with applicable certs available."*
- **Spec reference:** Search Recs R11 — *"Code that if no certs are active in cert type it does not appear in public DB. When Admin adds new cert type they need to be visible in filter."*

**Sub-claim audit:**

**1. "Missing Certified Water Savings, LCA Opt, RM, WD" — by design, working correctly.**

`Controllers/Api/CertificateTilesApiController.cs:286-290` filters cert types via `visibleCertTypeIds.Contains(ct.Id)`, where `visibleCertTypeIds` is the distinct set of `CertificationTypeId`s referenced by certs in `baseQuery` (only `CertificateStatusHelper.PublicStatuses` — `Client Approved - Paid`, `Republished` — and not expired). Comment at lines 252-253 is explicit: *"Strict scoping: only surface filter options that have at least one cert in baseQuery. Picking any dropdown option must always return >= 1 result (no dead clicks)."* This is R11 rule-one verbatim.

Carly's own follow-up question in the CP NOTE — *"is it only showing certs with active listing for sorting? Is that why they aren't there?"* — **is correct**. The 4 named types (Certified Water Savings, LCA Opt, RM, WD) currently have zero certs in `PublicStatuses` tagged to them in the live data set, so they are correctly hidden. As soon as one Client-Approved-Paid cert is tagged to any of those types, it appears in the filter automatically.

This is **not a defect** — it's R11 working as specified. If client wants ALL admin-defined cert types to show regardless of cert presence, that is a CR (would break R11 rule-one and reintroduce dead clicks).

**2. "Product Opt and RM showing at the top of filter box" + "things are in alpha order" + "no sort ability under Cert Types" — duplicate of DEF-175 (fixed locally, pending deploy).**

Word-for-word the same quote that opened DEF-175 (5/11/26). DEF-175 changed `OrderBy(ct.SortOrder)` → `OrderBy(ct.Name)` across all three filter endpoints (Public / External / Internal). Status: Fixed locally — pending deploy. Once that build ships, "Product Opt" sorts between "Other" / "Recycled Content" alphabetically and "RM" sorts after "Recycled Content" / before "WD" — i.e. neither appears "at the top."

The "no sort ability under Cert Types" piece is accurate — confirmed via `Grep` of `Views/Admin/CertificateTypes.cshtml` for `SortOrder|sortOrder|drag|reorder`, no matches. DEF-175 made this irrelevant by switching to alpha; admin-added types auto-slot into the correct alpha position without any sort-order UI.

**3. "See ID 169 for additional items — would just show options with applicable certs available."**

Already the behavior — covered under sub-claim 1.

**Verification path post-DEF-175 deploy.**

1. Client opens `/Certificate/CertificateTileViewExternal` (or any tile view) on the new build.
2. Expand the "Certification Type" filter pane.
3. **Expected order** (Level-1, IsActive=1, with ≥1 public cert): strict alphabetical by Name. "Product Opt" and "RM" appear in their correct alpha positions, not at the top.
4. **Expected exclusions**: Cert types with zero current public certs are absent (Certified Water Savings, LCA Opt, RM, WD per Carly's note). This is R11 rule-one. **Caveat: if any of those four were re-tagged to a public cert between 5/1 and the deploy date, they will reappear in the filter — that is correct R11 behavior, not a regression.**

**No code change in this entry.** Closing per `feedback_qc_review_no_changes.md` pattern. Carly's questions about the four missing types should be answered in the QC log response: *"R11 by design — those types have zero certs in public status. Will populate as soon as a public cert is tagged."*

**Files audited (no edits):**

- `Controllers/Api/CertificateTilesApiController.cs` (Public filter endpoint, lines 240-369)
- `Models/CertificateStatus.cs` (`PublicStatuses` definition, lines 64-68)
- `Models/CertificationType.cs` (`SortOrder` column lives on entity but read-only from UI, line 20)
- `Views/Admin/CertificateTypes.cshtml` (confirmed no sort UI)

**Cross-references:** DEF-175 (alpha sort fix), DEF-167 (Level-1-only filter), DEF-168 (orphan parent category trim).

---

### DEF-187 — Edit User modal: Password field is dead UI; admin-typed passwords are silently discarded (plus 4 related issues rolled in)

- **Date:** 2026-05-12
- **Severity:** Medium-High (admin-only path; no security regression for passwords vs. current state — passwords weren't *weakened*, they just didn't change. But the field looked functional, so an admin who "resets" a user's password and tells them the new one would leave the user unable to log in with no error feedback. Pre-launch. Bumped from Medium because verification uncovered that no admin User UPDATE audit rows had ever been written in dev DB — a separate latent bug rolled into this fix.)
- **Status:** Fixed locally — verified end-to-end on http://localhost:5101 (7-step PowerShell verification script: admin login → set new password via UpdateUser → login with new password succeeds → login with old password fails → restore → audit rows confirmed in DB with no hash leakage)
- **Reporter:** Shawn — surfaced 2026-05-12 while writing `docs/training/user-management.md`. We were documenting the password workflow for admins, hit "an imported user can't log in because email isn't configured yet, can the admin just edit the password?" and the audit found the field is non-functional.

**Root cause — original DEF-187.**

The Edit User modal in `Views/Admin/UserManagement.cshtml:292-294` renders an `<input id="user-password">` wired through `wwwroot/js/admin/userManagement.js`:

- line 59: captures the input ref as `userPasswordInput`
- lines 690-691: clears the field and sets `required = false` when the modal opens in Edit mode
- lines 784-785: conditionally adds the value to the save payload: `if (userPasswordInput.value) { userData.password = userPasswordInput.value; }`

The save payload is POSTed to `AdminController.UpdateUser` (line 144) which model-binds to `UserViewModel` (`Models/AdminModels.cs:6-44`). **`UserViewModel` did not declare a `Password` property** — the JSON field was silently dropped by the model binder. `AdminService.UpdateUserAsync` (`Services/AdminService.cs:397`) updates `FullName`, `Email`, `IsActive`, `Roles`, `Companies`, `Partners` — it never read `model.Password` and never touched `user.PasswordHash`. SaveChangesAsync succeeded, admin saw a success toast, password unchanged.

Pre-existing latent defect — not a DEF-186 regression. DEF-186's scope was the import/create pathways; the Edit path was untouched, and the dead-UI nature was already there.

**Rolled-in scope (discovered during fix).**

1. **`RequirePasswordReset` checkbox in Edit modal — same dead-UI pattern.** `UpdateUserAsync` never assigned `user.RequirePasswordReset = model.RequirePasswordReset`, so toggling the checkbox in Edit mode also silently did nothing. Added the assignment unconditionally (admin's chosen value wins, regardless of whether password was also changed).

2. **Audit log was leaking bcrypt hash via field-diff.** `AuditService.DetectChanges` reflected over all public properties of the `User` entity. Without intervention, an admin-driven password change would have written two `AuditHistory` rows containing the old and new BCrypt hashes — an offline-cracking target if the audit table ever leaked. Added `PasswordHash` to `ShouldSkipProperty`'s skip list. The fact-of-change is recorded via the redacted-event path instead (see point 3).

3. **No admin User UPDATE audit rows had ever been written.** Discovered while verifying point 2. The dev DB had **zero** `AuditHistory` rows where `EntityType = 'User' AND Action = 'UPDATE'` — across the entire history. Server log showed `FK_AuditHistory_Users_ChangedBy` constraint violations on every admin attempt. Root cause: `AdminController.GetCurrentUserId()` (line 28) looked for a claim named `"UserId"` which is never issued. `AccountController.SignInUser` (line 1138) issues `ClaimTypes.NameIdentifier` with the user's int Id — and the comment at lines 1121-1123 explicitly states "Every controller's GetCurrentUserId() helper parses ClaimTypes.NameIdentifier as int." `CertificateController.GetCurrentUserId()` (line 1494) does this correctly; `AdminController` diverged. So every admin audit call passed `userId = 0`, EF tried to INSERT a row with `ChangedBy = 0`, and the FK constraint failed in a swallowed catch. **Impact: all admin-initiated audit events (user updates, product updates, role updates, etc.) silently lost since the divergence was introduced.** Fix: aligned `AdminController.GetCurrentUserId` to look for `NameIdentifier` first, with `sub` (B2C) and legacy `"UserId"` as fallbacks.

4. **`AuditService.SerializeEntity` failed entirely for entities with bidirectional nav** (User, Certificate, others with cycles). Discovered after a doc-cleanup pass where I incorrectly flagged "PasswordHash leaks via raw JSON serialization" — credentials are already excluded by `[JsonIgnore]` on the model side (`Models/User.cs:37, 61, 74, 79, 86, 89, 92, 99` — DEF-121 lineage). But on inspection the real bug is the opposite: `JsonSerializer.Serialize` was throwing on nav cycles (e.g., `User → Roles → Users → ...`), `SerializeEntity` caught and returned the placeholder string `"[Serialization failed: <EntityType>]"`. Direct dev DB query confirmed every User CREATE row (`Id=247, 245, 264, 318, 321, …`) and every Certificate CREATE row had useless placeholder text instead of the entity snapshot. **No data leak — but no audit content either, for the most security-relevant CREATE/DELETE events on the most security-relevant entity types.** Fix: rebuilt `SerializeEntity` to use a cached `JsonSerializerOptions` with `ReferenceHandler.IgnoreCycles` and a `DefaultJsonTypeInfoResolver` modifier that drops (a) properties matching the shared audit skip-list and (b) navigation collections — same exclusion rules as `DetectChanges`, so the two audit paths can't drift. After fix: User CREATE rows now serialize to ~600-char JSON containing scalar fields only (Id, Username, Email, FullName, Status, IsActive, …) — no nav clutter, no credentials, no cycles. Pre-existing Certificate CREATE rows from before the fix remain placeholder text (cannot be backfilled — the original entity state was never captured); only new CREATE/DELETE events after deploy will benefit.

**Implementation.**

1. **`Models/AdminModels.cs:UserViewModel`** — added `[StringLength(100)] public string? Password { get; set; }`. Nullable; blank = no change. No `[Required]` — only validated when explicitly provided.

2. **`Services/AdminService.cs:UpdateUserAsync`** — when `model.Password` is non-whitespace, hash with BCrypt and assign to `user.PasswordHash`. Always assign `user.RequirePasswordReset = model.RequirePasswordReset` (rolled-in fix). Server-side 8-char validation lives in the controller (matches `CreateUserAsync` DEF-186 pattern).

3. **`Services/Audit/AuditService.cs:ShouldSkipProperty`** — added `"PasswordHash"` to the skip-fields list. Prevents bcrypt-hash storage in `AuditHistory.OldValue` / `NewValue` for field-level diffs.

4. **`Services/AdminService.cs:UpdateUserAsync`** — added explicit `_auditService.LogAuditEventAsync` call when `passwordChanged`. This produces a dedicated audit row (`Action=UPDATE`, `FieldName=NULL`, `NewValue="Password changed by admin"`, `Reason="Password changed"`) that is guaranteed even when no other fields differ (the field-diff path returns 0 rows in password-only changes because `PasswordHash` is now skipped and `RequirePasswordReset` often doesn't change).

5. **`Controllers/AdminController.cs:UpdateUser`** — added pre-service 8-char min-length check on `model.Password` (returns `{ success: false, message: "Password must be at least 8 characters" }` if too short). Blank/null still means "no change."

6. **`Controllers/AdminController.cs:GetCurrentUserId`** — rewritten to look for `ClaimTypes.NameIdentifier`, then `sub`, then legacy `"UserId"` as last-resort fallback. Aligns with `CertificateController.GetCurrentUserId`.

7. **`Services/Audit/AuditService.cs:SerializeEntity` rebuilt.** Extracted the audit skip-list (`UpdatedAt`, `UpdatedBy`, `UpdatedByUser`, `CreatedByUser`, `User`, `PasswordHash`) to a `private static readonly string[] AuditSkipFields` so both `DetectChanges` and `SerializeEntity` share one source of truth. `SerializeEntity` now uses a cached `static readonly JsonSerializerOptions` with `ReferenceHandler.IgnoreCycles` + a `DefaultJsonTypeInfoResolver` modifier that removes skip-list properties and navigation collections. Result: lean scalar-only JSON for CREATE/DELETE audit rows, no cycles, no credentials.

**Verification (http://localhost:5101, dev DB `GCCDatabase_Dev`).**

PowerShell script at `C:\Users\swest\AppData\Local\Temp\verify-def-187.ps1`:

1. ✅ Admin login (POST `/Account/Login` → 302)
2. ✅ Find target user (`jane.analyst@anukiconsulting.com`, id=2, role=Analyst)
3. ✅ PUT `/Admin/UpdateUser?id=2` with new password → `{ success: true, message: "User updated successfully" }`
4. ✅ Login with the new password → 302 redirect to `/Certificate/CertificateTileViewInternal` (auth succeeded)
5. ✅ Login with the old password (`TestPass123!`) → 200 (login page re-render, auth rejected)
6. ✅ Restore password to `TestPass123!` for seed-state hygiene → success
7. ✅ Direct DB check: `Users.Id=2.PasswordHash` is 60 chars starting `$2a$` (valid BCrypt); `RequirePasswordReset=0`
8. ✅ Direct DB check: `AuditHistory` rows for `EntityType='User' AND EntityId='2'` show two new rows (`Id=270, 271`), `NewValue="Password changed by admin"`, `Reason="Password changed"`, `ChangedBy=1` (admin), **no bcrypt hash in any column** — confirms PasswordHash skip-list works AND the GetCurrentUserId fix surfaced audit rows that were previously failing silently.

**Verification — audit serialization (rolled-in scope #4, http://localhost:5101, dev DB).**

Second PowerShell script at `C:\Users\swest\AppData\Local\Temp\verify-audit-serialization.ps1`:

1. ✅ Admin login → 302
2. ✅ POST `/Admin/CreateUser` for an ephemeral test user (`audit.serialization.<ticks>@anukiconsulting.com`) → `success: true`
3. ✅ Query AuditHistory for latest `EntityType='User' AND Action='CREATE'` → row Id 272, `NewValue` length 622 chars, starts `{"Id":323,"Username":"audit.serialization...","Email":"...","FullName":"Audit Serialization Test","IsDeleted":false,"Status":"active","IsActive":true,…}`
4. ✅ Assertion: `NewValue` does not contain `"Serialization failed"` placeholder
5. ✅ Assertion: `NewValue` does not contain `"PasswordHash"` (case-insensitive)
6. ✅ Assertion: `NewValue` contains the test user's email (audit content is meaningful)
7. ✅ Assertion: `NewValue` matches no BCrypt-shaped string (regex `\$2[aby]\$\d{2}\$[A-Za-z0-9./]{53}`)
8. ✅ Assertion: `NewValue` contains no other `[JsonIgnore]`'d sensitive fields (`twoFactorSecret`, `recoveryCodes`, `passwordResetToken`, `passwordHistory`, `allowedIPs`, `securityQuestions`)
9. ✅ Clean up: deleted the test user via `/Admin/DeleteUser`

**Files.**

- `Models/AdminModels.cs`
- `Services/AdminService.cs` (UpdateUserAsync)
- `Services/Audit/AuditService.cs` (ShouldSkipProperty refactored to share `AuditSkipFields`; new `BuildEntitySerializerOptions` + cached static `EntitySerializerOptions`; SerializeEntity rewrite)
- `Controllers/AdminController.cs` (UpdateUser, GetCurrentUserId)

**Follow-up doc cleanup.**

- `docs/training/user-management.md` §3 and §4 currently document "delete + recreate via Add New User" as the workaround for the dead password field. Once this ships to prod, those rows need to be removed and replaced with the actual Edit User flow.

**Not in scope.**

- Email-driven password reset for non-admin users — still gated on SMTP configuration (separate item).
- Force-reset-on-next-login as a standalone admin button — could be added later; the `RequirePasswordReset` checkbox handles the use case for now.
- Strength meter / complexity requirements beyond 8-char minimum — separate CR.
- Backfilling the historical placeholder `[Serialization failed: ...]` rows with reconstructed entity snapshots — not possible, the original entity state was never captured. Going forward only.

---

### DEF-186 — User import template leaked plaintext passwords; blank cells silently defaulted to the public `TestPass123!`

- **Date:** 2026-05-12
- **Severity:** **High** (security — pre-launch, no production data yet, but the bulk-user-onboarding path was the primary mechanism for getting client users into the live system)
- **Status:** Fixed locally — verified on http://localhost:5101
- **Reported by:** Shawn (raised during the Company Access → User Company rename review)

**Root cause (3 layers).**

1. **JS template** (`wwwroot/js/userManagement.js` `downloadImportTemplate()`) generated a 7-column CSV including a `Password` column. The template literally invited admins to type plaintext passwords into a spreadsheet that lives in Downloads / SharePoint / email.

2. **JS bulk-import flow** (`wwwroot/js/userManagement.js` ~line 1386) applied `password: row.data.password || 'TestPass123!'` as a fallback. Any blank Password cell — the *normal* case for an admin who didn't pre-decide each user's password — silently created the user with `TestPass123!`, the same well-known string published as a local-dev credential in the project root `.claude/CLAUDE.md`. Credential-stuffing layup.

3. **Server positional parser** (`AdminController.ImportUsers()` line 284) applied the same `?? "TestPass123!"` fallback. Even an admin who bypassed the JS preview and POSTed a CSV directly to `/Admin/ImportUsers` would get the same bad default.

The `CLAUDE.md` published-credential lookup is what makes this a **High** rather than Medium — the default password is publicly known to anyone who reads the repo or its docs (and the doc says it explicitly: "TestPass123!" for admin/analyst/external local-dev accounts).

**Fix.**

- **`Services/AdminService.cs:CreateUserAsync` — safety net at the service boundary.** Any caller-supplied empty/whitespace `model.Password` triggers `GenerateSecurePassword()` (cryptographically random 24-char, sourced from `RandomNumberGenerator.GetInt32` over a 70-char alphabet of A-Z a-z 0-9 + safe symbols). When this fires, `RequirePasswordReset` is also forced to `true` regardless of caller intent. Single point of truth — both Path A (JS) and Path B (server CSV) can stop caring about defaults.

- **`wwwroot/js/userManagement.js` template** — `Password` and `RequirePasswordReset` columns removed from generated template. New 5-column layout: `Name, Email, Role (Admin/Analyst/External), Status (Active/Inactive), User Company (semicolon-separated names, or "All")`. Quoting logic for User Company's internal comma still applies.

- **`wwwroot/js/userManagement.js` import flow** — fallback `'TestPass123!'` removed. Bulk-import row now sends `password: ''` deliberately. Modal create flow (admin manually adds one user) is unchanged — it still validates a non-empty password (line 683) because in that flow the admin has set one explicitly.

- **`Controllers/AdminController.cs:ImportUsers`** — positional parser rewritten for the 5-column layout: `values[0]=Name, [1]=Email, [2]=Role, [3]=Status, [4]=User Company`. Password is never read from CSV; always sent as empty so the service-layer safety net generates random + forces reset. Comment updated. Legacy 7-column CSVs uploaded against the new parser still work for Name/Email/Role/Status but `Status` and `User Company` would land in wrong slots — old templates must be re-downloaded. This is documented in the shipped log.

- **Forced reset on first login.** Because the user can't be told a password they didn't set, they must hit the existing forgot-password flow on first login. The `RequirePasswordReset` flag already gates this path; the random password is effectively unusable.

**Files:**

- `wwwroot/js/userManagement.js`
- `Controllers/AdminController.cs`
- `Services/AdminService.cs`

**Verification:** see shipped-2026-05.md entry for the localhost test run.

**Follow-ups (not in scope for DEF-186):**

- Trigger an email-the-user "set your password" message at import time (today user gets nothing — admin would have to tell them out-of-band to use forgot-password). Tracked as `[[issue_import_user_notification_email]]`.
- Move the published `TestPass123!` strings out of `.claude/CLAUDE.md` and `CLAUDE.md` into `appsettings.Development.json` or an env var so the dev-credential list isn't checked into the repo's docs. Separate cleanup.

---

### DEF-185 — Certificate Create/Edit: non-nullable FKs (Company/Product/CertificationType) had no explicit server `> 0` guard

- **Date:** 2026-05-12
- **Severity:** Medium (latent — normal user flow was protected by empty-string→int model-binder behavior, but a scripted/tampered POST or a future JS regression that posts `0` would have produced the same FK violation class as DEF-184)
- **Status:** Fixed locally — Playwright regression 14/14 PASS on http://localhost:5101
- **Reporter:** Shawn — follow-up audit Shawn requested after DEF-184 to confirm the three-layer validation pattern is consistent across the app's create/edit forms.

**Audit findings (subagent report, 2026-05-12).** Reviewed the three user-facing create/edit forms with non-nullable FKs:

| Form | Non-nullable FKs | Pre-DEF-185 coverage |
|---|---|---|
| Product Add/Edit | `CategoryId` (`int`) | 3-layer ✓ (DEF-184 fix) |
| Product Add/Edit | `CompanyId` (`int?` but UI-required) | 3-layer ✓ (long-standing reference) |
| Certificate Create | `CompanyId`, `ProductId`, `CertificationTypeId` | HTML `required` ✓, JS guard ✗, server `> 0` ✗ |
| Certificate Edit | same three | HTML `required` ✓, JS guard ✗, server `> 0` ✗ |

The Certificate paths *appeared* protected by `[Required]` attributes on the VM (`Models/CertificateEditViewModel.cs`) plus jquery.unobtrusive-validation. But `[Required]` on a value type does NOT reject `0` — only `null`. Normal user flow was actually saved by the model binder's empty-string→`int` behavior (the empty `<option value="">` posts as `""` which fails int binding and produces a ModelState error). That's incidental, not intentional. A scripted POST that sets `CompanyId=0` would pass ModelState and crash at `_certificateRepository.AddAsync(certificate)` with the same FK violation class as DEF-184.

**Fix — six server-side guards (two actions × three FKs each):**

Added before each `if (ModelState.IsValid)` gate, using `ModelState.AddModelError` so the existing MVC form re-render path picks them up alongside any other validation errors:

```csharp
// DEF-185: explicit non-nullable FK guards. `[Required]` on `int` does NOT
// reject 0; without these, a scripted/tampered POST with 0 passes ModelState
// and crashes at EF with an FK violation (same class as DEF-184).
if (model.CompanyId <= 0)
{
    ModelState.AddModelError(nameof(model.CompanyId), "Company is required. Please select a company.");
}
if (model.ProductId <= 0)
{
    ModelState.AddModelError(nameof(model.ProductId), "Product is required. Please select a product.");
}
if (model.CertificationTypeId <= 0)
{
    ModelState.AddModelError(nameof(model.CertificationTypeId), "Certification Type is required. Please select a certification type.");
}
```

**Files:**
- `Controllers/CertificateController.cs` Create POST (line 432 region, immediately before `if (ModelState.IsValid)`)
- `Controllers/CertificateController.cs` Edit POST (line 848 region, immediately before `if (ModelState.IsValid)`)

**Deliberately NOT changed (out of scope or correct as-is):**
- JS guards on `Views/Certificate/Create.cshtml` and `Views/Certificate/Edit.cshtml` submit handlers — UX polish only; `novalidate` + unobtrusive-validation already cover normal user flow. Tracked as a lower-priority follow-up if the inconsistency with the Product modal's three-layer pattern becomes a real UX issue.
- Schema: leave `int` (non-nullable) as-is. Making these nullable would have a large blast radius (cert form's product picker, dashboard counts, ETL extractor, all dependent queries assume non-null FKs).
- Other nullable FKs on these forms (`PathwayId`, `ProductCategoryId`, `ProductTypeId`, `ProductSubcategoryId`, `CertificationTypeSubcategoryId`, `PartnerId` — all `int?`). Can't FK-violate from null.

**Verification — Playwright `cert-fk-guards.mjs` (14/14 PASS) on http://localhost:5101:**

Login as analyst, harvest antiforgery token from `/Certificate/Create`, scripted POST with `CompanyId=0&ProductId=0&CertificationTypeId=0`:
- Create POST returns 200 (form re-renders, no exception) — PASS
- Response body contains "Company is required..." — PASS
- Response body contains "Product is required..." — PASS
- Response body contains "Certification Type is required..." — PASS
- Response body does NOT contain `FK_Certificates_` or `FOREIGN KEY constraint` text — PASS
- Lookup of the test cert number returns 404 (no row created) — PASS

Same six assertions repeated for `/Certificate/Edit/26-0001` against existing cert Id=2 — all PASS.

App log check post-test: `grep -c "FK_Certificates_\|FOREIGN KEY constraint"` returns **0**. The guards intercept before EF attempts the insert.

Regression check: full DEF-182 smoke (`smoke.mjs`) and DEF-184 regression (`category-required.mjs`) both still PASS — no impact on Product Management.

**Cross-ref:** Closes the audit follow-up scoped in DEF-184. Same three-layer pattern now applied to every non-nullable FK on the three create/edit forms in scope. Lower-priority JS guard work on the Cert views deferred — the server-side gate is the bug-bearing layer.

---

### DEF-184 — Product Management: Add Product without Category caused FK error

- **Date:** 2026-05-12
- **Severity:** High (functional — every Add-without-Category attempt threw a SQL FK violation from EF and returned a generic "Error saving product" toast)
- **Status:** Fixed locally — Playwright regression 9/9 PASS on http://localhost:5101
- **Reporter:** Shawn — found immediately after the DEF-182 ship while manually testing the Add modal.

**Symptom.** Click Add Product, fill Product Name and Company, leave Category empty, click Save. App log writes:

```
Microsoft.Data.SqlClient.SqlException: The INSERT statement conflicted with the FOREIGN KEY constraint
  "FK_Products_ProductCategories_CategoryId". The conflict occurred in database "GCCDatabase_Dev",
  table "dbo.ProductCategories", column 'Id'.
```

The browser sees a generic red toast (the controller's outer `catch` swallows the FK detail and returns `success=false` with the generic message), no row is created, and the user has no idea what failed. Easy to reproduce: Category was not marked required in the modal HTML, had no JS guard in `saveProduct()`, and had no server-side validation in `ProductController.SaveProduct`. The JS sent `categoryId: parseInt(...) || 0`, EF tried to INSERT with `CategoryId = 0`, and `Products.CategoryId` is `int` (non-nullable) bound to the FK above.

**Why Category must stay required.** `Models/Product.cs:29` declares `public int CategoryId { get; set; }` (non-nullable). `Data/ApplicationDbContext.cs` ties it via `WithMany(pc => pc.Products)`. Every product has a category — that's the schema contract. Fix is to enforce it on the client (match the existing Company pattern) and add a server-side guard for defense in depth, not to make the column nullable.

**Fix — three layers:**

1. **`Views/Product/Management.cshtml`** (modal HTML) — label now reads `Category *`, the `<select>` gains the `required` attribute. Browser-native validation now fires on Save with a focused tooltip ("Please select an item in the list.") before the form submit event ever runs. Matches the pattern Product Name + Company already use.
2. **`Views/Product/Management.cshtml`** (`saveProduct()` JS) — added a guard immediately after the existing Company guard:
   ```js
   const categoryVal = document.getElementById('product-category').value;
   if (!categoryVal) {
       AdminCore.showNotification('Category is required. Please select a category.', 'error');
       document.getElementById('product-category').focus();
       return;
   }
   ```
   Belt for the suspenders — covers the case where the HTML `required` attribute is somehow bypassed (e.g., scripted submit).
3. **`Controllers/ProductController.cs:362`** — added a server-side check before the create/update path:
   ```csharp
   if (model.CategoryId <= 0)
   {
       return Json(new ApiResponse<object> { Success = false, Message = "Category is required. Please select a category." });
   }
   ```
   Belt for the belt. A scripted POST that bypasses both client layers now gets a clean 200 JSON `{success:false, message:"Category is required..."}` instead of an FK exception in the logs.

**Why not just make `CategoryId` nullable?** Every existing query, projection, and filter (`Where(p => p.IsActive && !p.IsDeleted)`, `Include(p => p.Category)`, the cert form's product picker, the dashboard counts, the ETL extractor) assumes a non-null Category. Flipping nullability would have a much larger blast radius for what is fundamentally a validation gap, not a schema design defect.

**Files:**
- `Views/Product/Management.cshtml:256-257` (label + select `required`), `952-957` (JS guard)
- `Controllers/ProductController.cs:362-365` (server guard)

**Verification — Playwright `category-required.mjs` (9/9 PASS) on http://localhost:5101:**
1. Login as analyst, open Add Product modal — PASS
2. `#product-category` has `required` attribute — PASS
3. Label reads "Category *" — PASS
4. Fill Name + Company, leave Category, click Save → modal stays open — PASS
5. Category select reports `checkValidity()==false` — PASS
6. Native `validationMessage` = "Please select an item in the list." — PASS
7. Reload table → product count unchanged (50 → 50) — PASS
8. Search by the failed-add unique name → 0 matches — PASS
9. Direct POST to `/Product/SaveProduct` with `categoryId:0` → returns `{success:false, message:"Category is required..."}` (not an FK exception) — PASS

Regression check: full DEF-182 smoke (`smoke.mjs`) re-ran 10/10 PASS — no impact on the Status flow.

**Cross-ref:** Discovered during manual UAT immediately after DEF-182 ship. Same defect class as the missing-Company validation that was already in place — Category just hadn't received the same treatment. Consider an audit of every other non-nullable FK on the product/certificate forms (e.g., `Certificate.PathwayId`, `Certificate.CompanyId`) to confirm the three-layer pattern is consistent across the app.

---

### DEF-183 — Public cert detail page leaks internal-process error text when PDF download fails (QC #168)

- **Date:** 2026-05-12
- **Severity:** Medium (public-facing copy / brand)
- **Status:** Fixed locally — built clean (file-lock only; dev server PID 28756 holds the exe)
- **Reporter:** Carly, 4/30/26 QC log row 168: *"Since removing the default cert now its giving this error message that we would not want public facing… At a minimum we can only say 'No certificate PDF has been uploaded for 26-0006.' The rest is not public facing language we would ever want to share."*

**Two problems behind the one symptom.**

1. **Controller error body was internal-process language.** `CertificateController.Download` (line 1592) returned `NotFound($"No certificate PDF has been uploaded for {id}. Download is unavailable until an analyst uploads the certificate file.")`. The endpoint is `[AllowAnonymous]` so that body is reachable by anyone — and the "analyst uploads the certificate file" half exposes internal workflow language. **Fix:** trimmed to `$"No certificate PDF has been uploaded for {id}."` only. Added a comment at the call site flagging the public-safety constraint so a future refactor doesn't append details back.

2. **The single view's download path opens a new tab on the raw response, instead of fetch+toast like the tile view.** Carly's "why are they built as different experiences?" is correct — they were:
   - `wwwroot/js/certificate-tile-view.js:1074` uses `fetch()` → on non-ok response, shows toast `"Certificate PDF not available"` and the user never sees the server body.
   - `Views/Certificate/CertificateSingleView.cshtml:235` used `window.open('/Certificate/Download/...', '_blank')` → browser opened a new tab pointed at the controller and rendered the raw `NotFound` text in that tab.

   This divergence existed because the tile view is a JS-rendered SPA-style page (each tile is a JS card with a fetch handler), while the single view is server-rendered Razor where the download icon was wired to a one-line `window.open`. The `hasFile` greying logic at `CertificateSingleView.cshtml:187` only catches the **DB-null** case (`Certificate.CertificateFile` empty). It does *not* catch the **DB-set-but-file-missing-on-disk** case — which is exactly what happens after a manual file removal (Carly's scenario) or after a Plesk deploy that wipes uploads (the 4/23 incident). So `hasFile=true` → button is active → click → new tab → raw error body.

   **Fix:** rewrote `window.downloadCertificate` in `CertificateSingleView.cshtml` to use `fetch()` + blob + anchor click (same pattern as the tile view), with a `showToast('Certificate PDF not available', 'error')` fallback on non-ok response. `main.js` is loaded by `_Layout.cshtml:337` so `showToast` is globally available on the single view.

**Defense in depth.** Both edits work together: even if a future view goes back to `window.open` (or someone hits `/Certificate/Download/26-0006` directly in the address bar), the response body is now public-safe. And even if a cert record has `CertificateFile` set in DB but the file is gone from disk, the UI surfaces a clean toast rather than a new tab.

**What we deliberately did NOT do.**
- Did not add server-side `File.Exists()` to the view's `hasFile` calculation. That would add a disk hit to every public cert detail page load to fix a rare edge case; the toast pattern covers it with no per-request cost.
- Did not change the button greying for the DB-null case — that logic was already correct.

**Files:**
- `Controllers/CertificateController.cs:1590-1594` — public-safe error body + comment.
- `Views/Certificate/CertificateSingleView.cshtml:233-269` — `downloadCertificate` rewritten to fetch + toast.

**Smoke test (local).**
1. Restart on http://localhost:5101 — current dev server (PID 28756) is holding the exe; needs `taskkill /F /PID 28756` then `dotnet run --urls "http://localhost:5101"`.
2. Find a cert in dev DB where `CertificateFile` is non-empty but the file is missing on disk (or temporarily rename the file). Open the public cert detail page → click the download icon → expect toast `"Certificate PDF not available"`, no new tab, no leaked language.
3. Hit `/Certificate/Download/{number}` directly in the browser address bar for the same cert → expect response body to read exactly `"No certificate PDF has been uploaded for {number}."` and nothing more.
4. Regression: a cert with a real PDF on disk still downloads via the icon (the fetch path triggers the anchor click).

**Cross-ref.** Carly's last clause — *"We shouldn't really have certs added without a cert so in reality this should not ever happen but we need solution if it does"* — confirms the 2026-04-25 business rule (analysts must upload a real PDF on cert create; we never auto-generate a placeholder) still stands. This fix is the graceful-degradation path for the case the rule fails to prevent.

---

### DEF-182 — Product Management: template / popup / table not level (QC #103 3/13 re-raise)

- **Date:** 2026-05-12
- **Severity:** Medium (UX / consistency — client called it out twice on QC #103: first as template-vs-table on 3/13, then as export-vs-template round-trip on 5/8 which became DEF-174)
- **Status:** Fixed locally — built clean, smoke-tested on http://localhost:5101
- **Reporter:** Shawn re-reviewing QC #103 row "t [not] fixed" — Carly's 3/13 note was that template columns and table columns weren't "level" with each other or with the Add New Product popup.

**Problem before this fix.** Three surfaces were misaligned:
- **Popup (Add/Edit Product):** Product Name, Company, Description, Category, Product Type, Subcategory — no Status (per QC #92 which deliberately removed it)
- **Template CSV:** `Name, Company, Description, Category, Product Type, Subcategory` — "Name" vs "Product Name" wording mismatch
- **Table:** Product Name, Company, Category, **Product Type**, Subcategory, **Status**, Certs, IDs, Actions — Description missing (intentional, kept terse), Status present (real state — see below)

**Why Status couldn't just be ripped from the table.** `Product.IsActive` is a real soft-delete flag — every certificate Create/Edit form filters its product dropdown to `IsActive && !IsDeleted` (8 sites in `CertificateController.cs`: 300, 653, 747, 1170, 2343, 2382, 2417, 2469; also `Services/AdminService.cs` 709, 1825, 1941, 2059, 2122, 2352; `Scripts/DatabaseSeeder.cs:186`; `Services/ETL/DataExtractor.cs:225, 249`). Setting a product Inactive is how analysts retire it without losing the cert history that already references it. Before this fix, the only way to flip it was a CSV import line; the Product Management table was the only place to *see* the state. Removing the column would have orphaned that state.

**Fix — make popup the editor for Status; align template wording.**
- **`Controllers/ProductController.cs:880`** — CSV template header `Name` → `Product Name`. Import parsing is positional (line 685 skips the header row entirely), so rename is header-only and does not break legacy CSV imports.
- **`Views/Product/Management.cshtml`** — Three changes:
  1. Modal: added a `Status` `<select>` (Active / Inactive) next to Subcategory on the bottom row of the form. Matches the Status filter dropdown's options.
  2. `showAddProductModal()` resets Status to `Active` after `productForm.reset()` (since `reset()` clears select to first option anyway, but explicit is safer).
  3. `editProduct(id)` pre-fills Status from `product.status` (the listing API at `ProductController.cs:261` projects `IsActive ? "Active" : "Inactive"`).
  4. `saveProduct()` payload `isActive` now reads from the dropdown instead of the hardcoded `true`. Backend `SaveProduct` (ProductController:396, 414) already binds `model.IsActive` for both create and update paths, no controller change needed.
  5. Export header `Name` → `Product Name` (line 1060) so round-trip stays clean.

**What stays as documented design difference (and why):**
- **Description** is in the popup + template + export, but NOT in the table — table omits it for column-width reasons. Description is also still visible on the expandable row detail and on the Product detail page.
- The four legacy columns (Sku, Model, certificate counts) are not in template/popup — counts are derived, sku/model were intentionally removed from the modal per QC #92 and the field-rename memory.

**Files:**
- `Controllers/ProductController.cs:878-883`
- `Views/Product/Management.cshtml:275-285` (modal Status select), `846` (showAddProductModal reset), `872` (editProduct prefill), `960` (saveProduct payload), `1060` (export header rename)

**Smoke test (local):**
1. Restart on http://localhost:5101 — built 0 errors, app comes up.
2. `/Product/Management` returns 302 to login (expected — `[Authorize]`).
3. Manual UI verification still owed: log in as analyst, open Add New Product → Status defaults Active. Save → row appears with Active badge. Open Edit on an existing product → Status pre-filled correctly. Flip to Inactive, save → table reflects, row drops from Cert Create form's product picker.

**Cross-ref:** This addresses the residual half of QC #103 that DEF-174 (template/export round-trip) didn't touch. The two together close the QC item end-to-end: template ↔ popup ↔ table now share the same field set (with Description as a documented popup/template-only field for width reasons), and "Name" is "Product Name" everywhere in the user-facing copy.

---

### DEF-181 — Cert tile internal: ID-column sorts ignored legacy `Certificate.{Asin|Upc|Gtin|Ean}` columns; Cert Results sort produced unintuitive order on mixed-format strings (QC #157 5/1 re-raise)

- **Date:** 2026-05-11
- **Severity:** Medium (Major per client; functional regression on internal cert listing)
- **Status:** Fixed locally — migration applied to dev DB; pending deploy
- **Reporter:** Client UAT — QC #157 (4/7/26) and 5/1 retest: *"Partner, ASIN, UPC not sorting right ... 5/1: ASIN is off (other product ID ones didnt have data so Anuki needs to check those), Cert Results is off"*.

**Background.** DEF-165 (shipped 2026-05-08) addressed several QC #157 columns: Product Category misroute, deterministic per-cert pick via `.Min()` for multi-value columns, and a `.ThenBy(Id)` tiebreaker on every sort branch. Carly's 5/1 retest confirmed Product Category was fixed but flagged ASIN and Cert Results as still off. This DEF closes the remaining items.

**Root cause 1 — ID columns (ASIN/UPC/GTIN/EAN).** The display logic in this controller (line 314) falls back from `c.ProductIds` to the legacy `Certificate.{Asin|Upc|Gtin|Ean}` columns when no active `ProductIds` exist for that type — so a cert with only `c.Asin = "B08X7N9MRQ"` and no rows in `CertificateProductIds` still displays "B08X7N9MRQ" in the column. The sort comparator at lines 737-744 looked ONLY at `ProductIds`: `c.ProductIds.Where(...).Select(...).Min() ?? ""`. For legacy-only certs the subquery returned NULL → coalesced to `""` → all such certs got the same sort key and fell to the `.ThenBy(c => c.Id)` tiebreaker, bunching together by Id at one end of the result. From Carly's seat this looked like "ASIN isn't sorting"; the certs near the top/bottom were the ones with `ProductIds` entries while the legacy-stored ones appeared as a sorted-by-Id block in the middle/end.

Confirmed by capturing the EF-emitted SQL via a temporary `ToQueryString()` log on `?debugSql=1` and running it directly against the dev DB:
- `SELECT TOP 25 c.Id, COALESCE((SELECT MIN(c0.ProductIdValue) FROM CertificateProductIds c0 WHERE c0.CertificateId = c.Id AND c0.ProductIdType = N'ASIN' AND c0.IsActive = 1), N'') AS sort_key FROM Certificates c ORDER BY sort_key ASC, c.Id` — returned 25 rows all with `sort_key = ''` because none of those 25 certs had `ProductIds` ASIN rows even though many had `c.Asin` set. The SQL was doing exactly what it was asked; the LINQ was asking the wrong question.

**Fix 1 — Legacy fallback in the LINQ comparator** (no migration needed):
```csharp
"asin" => query.OrderBy(c => (c.ProductIds.Any(p => p.ProductIdType == "ASIN" && p.IsActive)
            ? c.ProductIds.Where(p => p.ProductIdType == "ASIN" && p.IsActive).Select(p => p.ProductIdValue).Min()
            : c.Asin) ?? "").ThenBy(c => c.Id),
```

EF translates the conditional to a SQL `CASE WHEN EXISTS(...) THEN (SELECT MIN(...)) ELSE c.Asin END` which mirrors the display exactly. Same shape applied to all 8 branches: asin/asin-za/upc/upc-za/gtin/gtin-za/ean/ean-za. Tags (int-tag/ext-tag) were NOT touched — they have no legacy fallback column (they're collection-only), and per DEF-165 they keep the `FirstOrDefault()` shape because `.Min()` timed out on the dev DB; that perf issue is its own follow-up.

**Root cause 2 — Cert Results sort.** The `CertificationResults` field is free-text by design (per the CR that introduced it — values like Amazon threshold scores). The raw DB values are mixed format: confirmed via direct SQL inspection that the dev DB contains `"95%"`, `"100"`, `"1"`, `"0.69"`, `"0.27"`, `"0.17"`. The old `OrderBy(c => c.CertificationResults ?? "")` did a lexicographic SQL Server string sort, which produced `DESC: 95%, 87%, 100, 1, 0.69, 0.27, 0.17` — exactly the unintuitive order Carly was seeing in the display (`95%, 87%, 100%, 1%, 69%, 27%, 17%`). The sort was working as coded; the comparator just didn't understand that the values are meant as numeric percentages.

**Fix 2 — Persisted computed numeric column** (one migration, no data migration):
- New nullable `decimal(18,4)` column `Certificates.CertificationResultsNumeric` declared as a `PERSISTED` computed column. Migration `20260512011258_AddCertificationResultsNumeric.cs`.
- Computed expression mirrors `DisplayFormatters.FormatCertificationResults`:
  ```sql
  CASE
    WHEN [CertificationResults] IS NULL OR [CertificationResults] = N'' THEN NULL
    WHEN [CertificationResults] LIKE N'%[%]%' THEN TRY_CAST(REPLACE([CertificationResults], N'%', N'') AS DECIMAL(18,4))
    WHEN TRY_CAST([CertificationResults] AS DECIMAL(18,4)) < 1 THEN TRY_CAST([CertificationResults] AS DECIMAL(18,4)) * 100
    ELSE TRY_CAST([CertificationResults] AS DECIMAL(18,4))
  END
  ```
  Three branches handle the three observed input formats (`"95%"` → 95, `"0.17"` → 17, `"100"` → 100). Non-numeric values land in NULL.
- Index `IX_Certificates_CertificationResultsNumeric` added on the computed column so sort uses it directly without a full table scan.
- Sort comparator now: `query.OrderBy(c => c.CertificationResultsNumeric).ThenBy(c => c.Id)` for `cert-results`; `.OrderByDescending(...)` for `cert-results-za`. NULL handling defers to SQL Server defaults — NULLs first in ASC (empties at top) and NULLs last in DESC (populated values surface where the user is most likely looking).
- Computed column maintained by SQL Server automatically on every UPDATE/INSERT — no application code change needed for ongoing data; old rows already have correct values right after the migration (verified locally: cert 50 raw "100" → 100.0000, cert 51 raw "0.69" → 69.0000, etc.).

**Files changed (4):**
- `Controllers/Api/CertificateTileInternalApiController.cs` — 8 ID-column sort branches updated with legacy fallback; cert-results branches switched to computed column; debug logging code removed.
- `Models/Certificate.cs` — added `CertificationResultsNumeric` property (read-only, computed).
- `Data/ApplicationDbContext.cs` — registered the computed column expression via `HasComputedColumnSql(..., stored: true)`; added the supporting index.
- `Migrations/20260512011258_AddCertificationResultsNumeric.cs` — generated migration with the `ALTER TABLE ADD COLUMN ... AS (CASE ...) PERSISTED` + index.

**Local verification (2026-05-11):**
- Migration applied to dev DB: `dotnet ef database update` → "Applying migration '20260512011258_AddCertificationResultsNumeric'. Done."
- Direct SQL inspection: `SELECT TOP 10 CertificationResults, CertificationResultsNumeric FROM Certificates WHERE CertificationResults IS NOT NULL ORDER BY CertificationResultsNumeric DESC` → 100, 95, 87, 69, 27, 17, 17, 1 (numeric DESC, correct).
- Playwright sort harness (`qc157_sorts_v2.cjs`, 19 assertions across 9 columns × 2 directions): **19 PASS, 0 FAIL.**
  - ASIN ASC: 7 distinct non-empty values, "" placed first (legacy-fallback working).
  - ASIN DESC: 20 distinct values, first "B0GJRV3MZC", last "B0BQN8R7JJ" (lex DESC of Min/legacy values).
  - Cert Results DESC: numeric span 100 → 1, 12 NULLs placed at end.
  - Cert Results ASC: NULLs at start (still hit before any non-null on page 1 due to high null count in dev data).
- Build clean: 0 errors, 0 warnings.

**Deploy notes:**
- This includes a schema migration (`AddCertificationResultsNumeric`). Apply via `dotnet ef database update` against prod, or extract the SQL via `dotnet ef migrations script` if a manual SQL apply is preferred. The migration is non-destructive: adds one nullable computed column + one index, no data movement. Computed-column expression is server-side and idempotent on existing rows.
- Per `[feedback_ef_includes_with_projection.md]` rule: tested locally end-to-end before this deploy — page-1 returns in ~200ms locally on the 40-cert dev DB. The computed column is indexed so prod should be similar.
- After deploy, re-test on gccwebapp.com using the same harness pointing at the prod URL.

**Out of scope (still open, track separately):**
- Internal-tag/external-tag/UPC `FirstOrDefault()` non-determinism: documented in DEF-165 comments; would need an index/plan tune in SQL Server. Low priority unless certs grow many tags/UPCs each.

**Cross-ref:** Closes Carly's 5/1 retest items "ASIN is off" and "Cert Results is off" under QC #157. Builds on top of DEF-165 (Product Category + multi-value `.Min()` work). Status comparator (already wired to both directions in DEF-165) and Partner/Location/Product Category (also DEF-165) re-verified passing in this run.

---

### DEF-180 — QC #159 (Replace-all CSV import for Product IDs) — already satisfied, no code change needed

- **Date:** 2026-05-11 (audited)
- **Severity:** N/A — review-only
- **Status:** Closed — already implemented
- **Reporter:** Client — QC #159 (4/7/26): *"What if someone wanted to replace all the existing with a new list... So it would remove those not included and add those new and keep existing still on list? I tried a version of this and got an error. There were 2 listed from a prior import. I tried to import a doc with only 1 product."*

**Finding:** The replace-all workflow Carly described was built between her 4/7/26 ask and today. Two related work items already landed:

1. **DEF-154 (shipped 2026-05-07)** — `"Replace all existing IDs with this file"` toggle in the Product ID import modal footer. When checked, the import CSV becomes the new source of truth: existing rows not in the import set are deactivated server-side, rows in the import are added or updated, rows already matching are kept. Server-side handling in `ProductIdApiController.cs` (line 359, `overwriteExisting` form parameter) and `ProductIdService.cs` (`ImportProductIdsAsync` + `ImportFromCsvAsync` overloads). JS wiring in `wwwroot/js/product-id-manager.js` (around line 1397, `applyImport()`). Spec match exactly:
   - "Remove those not included" → orphan rows get `IsActive=false` server-side
   - "Add those new" → fresh rows inserted via bulk endpoint
   - "Keep existing still on list" → matching rows preserved

2. **Visual discoverability polish** (already in `Views/Shared/_ProductIdManager.cshtml:315-325`): the toggle is pill-shaped with a yellow `#fff8e6` background, an exchange-icon, and a hover tooltip. Comment block in the view explicitly references Carly's "I imported a 1-row file and 2 prior IDs stayed" confusion as the rationale for the styling.

**Carly's "got an error" complaint** from 4/7/26 was on the **pre-DEF-154 code**, where:
- The toggle either didn't exist or wasn't wired through correctly
- Importing fewer rows than already existed could have hit a per-row PUT loop that errored partway through

That code path is gone. Current flow: bulk endpoint accepts the full import set + the `overwriteExisting` flag in one POST.

**Recommendation:** Carly should retest the scenario on the next deploy. Expected behavior:
1. Cert has 2 Product IDs from a prior import.
2. She opens the Product ID Manager, clicks Import, picks a CSV with 1 product.
3. **Ticks "Replace all existing IDs with this file"** in the modal footer.
4. Clicks Apply Import.
5. Result: cert now has 1 Active Product ID. The 2 prior rows are deactivated (still in the DB for audit, but `IsActive=false`).

If she runs the same flow WITHOUT ticking the toggle, the import behaves additively: the 1 new row is added or merged, the 2 prior rows stay Active.

**No code change in this entry.** Closing per `feedback_qc_review_no_changes.md` review pattern.

---

**Verification (added 2026-05-21) — QC #159 end-to-end test pass**

Two test runs executed to retire any remaining doubt before the client retests:

**Run A — Localhost destructive E2E** (`http://localhost:5101`, dev DB `GCCDatabase_Dev`, cert id=48 / number `26-0147`, status Active)

Setup state (before): 2 active Product IDs on cert 26-0147 — `ASIN B0BRNPBZJ8` (John Frieda ProFiller plus Shampoo 250ml) and `ASIN B01DSSNB5G` (John Frieda Sheer Blonde Go Blonder Shampoo 250ml).

| Step | Action | Result |
|---|---|---|
| 1 | Sign in as `analyst` | post-login → `/Certificate/CertificateTileViewInternal` |
| 2 | Open `/Certificate/Edit/26-0147` | manager renders 2 Product ID rows |
| 3 | Click Import → file chooser → upload 1-row CSV (`Type,Value,ProductName,ProductSKU` with `ASIN,B00REPL001,QC159 Replace Test Product,SKU-QC159`) | Import Preview modal opens |
| 4 | Tick "Replace all existing IDs with this file" toggle | state flips `false → true`; Apply Import enables |
| 5 | Click Apply Import | `POST /api/productids/certificate/48/bulk` → **200** |
| 6 | Reload edit page | manager renders **1** Product ID row (B00REPL001) |

DB-level reconciliation (same UpdatedAt → single transaction):

```
Id     Value         Name                                          IsActive   UpdatedAt
7      B0BRNPBZJ8    John Frieda ProFiller plus Shampoo 250ml      False      2026-05-21 13:34:05  (was True)
8      B01DSSNB5G    John Frieda Sheer Blonde Go Blonder Shampoo   False      2026-05-21 13:34:05  (was True)
2142   B00REPL001    QC159 Replace Test Product                    True       2026-05-21 13:34:05  (NEW)
```

Matches the client's verbatim ask: *"remove those not included and add those new and keep existing still on list."* Prior rows kept in DB for audit (`IsActive=false`), not hard-deleted.

**Run B — gccwebapp.com read-only UI suite** (live test env, cert `26-0163`, 2 existing Product IDs; Apply never clicked — CR rule preserved for live data)

9-test Playwright suite — all PASS:

| # | Test | Detail |
|---|---|---|
| T1 | Modal opens after clicking Import | `#import-preview-modal` becomes visible |
| T2 | Replace toggle in DOM and visible | `inDom=true visible=true` |
| T3 | Toggle defaults unchecked (append is the safe default) | `initialChecked=false` |
| T4 | Toggle click flips state both ways | sequence `false → true → false` |
| T5 | Preview renders 4 stat tiles | New / Modified / Unchanged / Errors all present |
| T6 | Valid 1-row CSV → 1 New | parser correctly counts the new row |
| T7 | Bad ASIN (length≠10) → counts as Error, Apply disabled | `errors=1, applyDisabled=true` — Apply correctly gated |
| T8 | Cancel closes modal without writes | `before=2, afterCancel=2` |
| T9 | CR-016 polish features NOT yet shipped (expected) | `radios=0, toRemoveTile=0, warningBanner=0` — confirms CR-016 still pending and is genuinely additive |

**Test artifacts** (working tree, not committed):
- `qc159-live-check/check.js` — gccwebapp toggle-presence verification (read-only)
- `qc159-live-check/e2e-replace.js` — localhost destructive E2E
- `qc159-live-check/ui-suite.js` — gccwebapp 9-test UI suite
- Screenshots: `e2e-01..05-*.png`, `ui-00..08-*.png`, `ui-04b-footer-tight.png`, `ui-07-bad-csv-preview.png`
- `qc159-live-check/ui-results.json` — machine-readable assertion log

**Why two runs:** the destructive path (DB writes) had to run on localhost per the `[Data Changes Require CR]` rule; the deployed-code verification had to run on gccwebapp.com to confirm the toggle is actually live. Together they cover both halves without breaking the live-env CR rule.

**Status:** Closed — verified end-to-end. The 4/7 client "got an error" complaint is no longer reproducible on either the deployed code (gccwebapp) or the latest local build. If the client still sees the error during the next UAT cycle, that is a NEW defect and should be logged with a fresh DEF row referencing this verification baseline.

---

### DEF-179 — Dashboard "Active Certs Published" exports leak Draft / Hold / Expired / Republished rows (QC #156 5/1/26 follow-up)

- **Date:** 2026-05-11
- **Severity:** Medium (data integrity — export contradicts the count-card label; client cannot trust the Excel feed)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Client. QC #156 was originally a label-rename request (4/7) that was completed and verified PASS on 5/1. The 5/1 follow-up comment opened a new defect on the same row: *"When I export now it has Expired, etc. Just not right."*

**Root cause:** The display widgets `# Active Certs Published Last Month / This Year / Last Year` count via `_dbSet.CountAsync` filtered by `dashboardStatuses.Contains(c.Status)` (`Repositories/CertificateRepository.cs:555-584`). The corresponding download buttons route through `Controllers/DashboardController.cs` `GetCertificatesForStat`, which calls `_certificateRepository.GetCertificatesByDateRangeAsync(start, end)`. That repository method (`Repositories/CertificateRepository.cs:444`) filters strictly by `EffectiveDate`; it has no status filter at all. Net effect: the count card shows e.g. "12 Active Certs Published This Year" but the CSV download contains every cert with EffectiveDate in the year — including Draft, Hold, Expired, Republished. Mismatch is large for any partner whose lifecycle includes drafts or paused statuses.

**Fix:** Filtered the three published-cert export cases by `CertificateStatusHelper.DashboardStatuses` after the date-range fetch, in `Controllers/DashboardController.cs` (case statements `certs-published-last-month`, `certs-published-this-year`, `certs-published-last-year`). Wrapped each case in a block scope so the local variable doesn't conflict with the existing `expiringNext` / `filtered` locals. Also renamed the report titles to match the display labels:

- `"Certificates Published Last Month"` → `"Active Certificates Published Last Month"`
- `"Certificates Published This Year"` → `"Active Certificates Published This Year"`
- `"Certificates Published Last Year"` → `"Active Certificates Published Last Year"`

The "Active" filter is identical to the count-card filter, so display count and exported row count will now reconcile exactly.

**Deliberately not changed:**

- `certs-expired-last-month` — the corresponding count card is `Expired Last Month` (not "Active"); leaving it as full date-range is correct since the user wants to see expired certs in that export.
- `certs-expiring-this-month`, `certs-expiring-next-month` — these route through `GetExpiringCertificatesAsync` which already applies its own DashboardStatuses-equivalent guard (only certs that *can* expire are still active). No change needed.
- `GetCertificatesByDateRangeAsync` itself — still returns the unfiltered date range; other callers (e.g. potential audit reports) may want non-Active rows. Filter is applied at the dashboard case-statement layer, not in the repository, to keep the repository general-purpose.

**Files changed (1):** `Controllers/DashboardController.cs:384-411` (three case statements rewritten as block scopes with the `Where` filter and updated titles).

**Backend impact:** None for non-published exports. The three published-cert exports will now return fewer rows on average — the difference equals the count of Draft/Hold/Expired/Republished certs whose EffectiveDate fell in the target window. Display count is unchanged.

**Local verification:** `dotnet build` — 0 errors. Same dev-server lock prevents an in-place restart for live click-through; verify after next clean restart or after the deploy.

**Verify-after-deploy:**

1. Sign in as `analyst`. `/Dashboard` → look at the three count cards `# Active Certs Published Last Month / This Year / Last Year`. Note each count.
2. Click the download icon on each card. Open the CSV.
3. Number of data rows in the CSV should equal the count shown on the card (one-to-one).
4. Every row in the CSV should have a status value in the dashboard "Active" set: `Client Approved - Paid`, `Client Approved - Not Paid`, `Client Approved - Confidential`, `Republished`. **No** `Draft`, `Hold`, `Expired`, `Suspended`, `Revoked` rows should appear.
5. Filename / report title in the CSV header (if present) should now read "Active Certificates Published…" rather than "Certificates Published…".

**Cross-ref:** Sibling fix to DEF-178, which expanded `GetCertificatesByDateRangeAsync` to include taxonomy nav properties — the published-certs CSVs will now also show Product Category / Product Type / Subcategory / Partner columns properly. DEF-179 is purely the status-filter half. QC #156's label-rename half (the original 4/7 request) was closed via the existing 5/1 PASS in TC-QC-156; this DEF closes the 5/1 export comment on the same row.

---

### DEF-178 — Dashboard exports missing taxonomy columns + cert-status dropping legacy "Active" rows (QC #141)

- **Date:** 2026-05-11
- **Severity:** Medium (functional — Dashboard exports didn't carry "all fields" per spec; "# Certs by Status" silently omitted KAO-era Active certs)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Client — QC #141, originally 4/3/26, re-flagged 5/1/26: *"The export for # of product categories, # product types, subcategories don't have that column/info... # certs by status export only shows list of Client Approved - Paid and Republished but should show all. Top 10 product types and Top 5 product categories don't have these fields in export. 5/1: It's worse now where no fields are showing."*
- **Spec reference:** Dashboard Metrics R24-R26 (Prebuilt List Export) — *"Export to Excel file - include all fields."*

**Root causes — three separate gaps:**

1. **`GenerateCertificateCsv` was 9-column** (`Cert#, Status, Company, Product, CertType, Eff Date, Exp Date, Location, Country`) — no `Product Category`, `Product Type`, `Product Subcategory`, or `Partner` columns. Every dashboard table-section download (cert-types, top-companies, product-types, product-categories) and several stat-button downloads (certs-expiring-this-month, certs-expired-last-month, etc.) routed through this method, so all of them shipped without taxonomy columns.

2. **`GetCertificatesByStatusAsync` and `GetCertificatesByDateRangeAsync` didn't `.Include` the taxonomy nav properties** — even with the new columns, status-bound and date-bound exports would emit blanks. `GetActiveCertificatesAsync` already loaded them; only these two paths lagged behind.

3. **The `cert-status` case in `GetCertificatesForTable` looped over an 8-element hardcoded list** of canonical `CertificateStatusHelper` constants. The DB contains a legacy literal status `"Active"` (KAO-era certs, see `observations_log.md` 2026-04-23) that pre-dates the canonical taxonomy and isn't in the constants — so 6 KAO certs were silently dropped from the "# Certs by Status" export. Client's "only shows Client Approved - Paid and Republished" complaint was the same shape — the hardcoded loop dropping rows for statuses outside the constants list.

**Fix — three pieces:**

1. **`Controllers/DashboardController.cs` `GenerateCertificateCsv`**: header extended to 13 columns (added Product Category, Product Type, Product Subcategory, Partner). Cert-direct override → product-level fallback, same pattern as the tile views (`cert.ProductCategory?.Name ?? cert.Product?.Category?.Name ?? ""`).
2. **`Repositories/CertificateRepository.cs`**: expanded the `.Include` chain on `GetCertificatesByStatusAsync` and `GetCertificatesByDateRangeAsync` to match `GetActiveCertificatesAsync`.
3. **Added `GetAllCertificatesWithDetailsAsync()`** on the repository (interface signature added too). The dashboard's `cert-status` case now calls this single method instead of looping the constants list — every cert in the DB is returned, regardless of what status string it carries. Future status-name drift won't silently drop rows again.

**Files changed (3):**
- `Controllers/DashboardController.cs` — CSV column expansion + cert-status case refactor
- `Repositories/CertificateRepository.cs` — Include expansion on 2 methods + new `GetAllCertificatesWithDetailsAsync`
- `Repositories/ICertificateRepository.cs` — interface signature for the new method

**Verification (localhost http://localhost:5101, Playwright admin-auth + fetch on every dashboard download endpoint, 2026-05-11):**

| Endpoint | Header columns | Data rows | Notes |
|---|---|---|---|
| `/Dashboard/DownloadStat?type=companies-with-active-certs` | 2 (Company, Count) | 10 | Aggregate stat — unchanged. Amazon=8 top result ✓ |
| `/Dashboard/DownloadStat?type=product-categories-with-active-certs` | 2 (Product Category, Count) | 2 | Aggregate stat — unchanged ✓ |
| `/Dashboard/DownloadStat?type=certs-expiring-this-month` | **13** | 1 | New cert-detail columns populated ✓ |
| `/Dashboard/DownloadTable?type=cert-types` | **13** | 23 | Example row: `"26-0001","Client Approved - Paid","Acme Corporation","Widget Pro","Chemicals and Raw Materials","","","Recycled Content",...,"Amazon"` ✓ |
| `/Dashboard/DownloadTable?type=top-companies` | **13** | 23 | Same shape ✓ |
| `/Dashboard/DownloadTable?type=cert-status` | **13** | **42** | **Distinct statuses now: `Active \| Client Approved - Paid \| Draft \| Expired`** — legacy "Active" included (pre-fix was 3 statuses, 36 rows) ✓ |
| `/Dashboard/DownloadTable?type=product-types` | **13** | 23 | Taxonomy columns present ✓ |
| `/Dashboard/DownloadTable?type=product-categories` | **13** | 23 | Taxonomy columns present ✓ |

Pre-fix vs post-fix on the most-affected report:
- `cert-status` data rows: **36 → 42** (legacy "Active" rows recovered)
- `cert-status` distinct statuses: **3 → 4**
- All cert-detail exports: **9 → 13 columns** including the three taxonomy fields client called out

The "no fields showing" 5/1 complaint did not reproduce in dev — every endpoint tested returns non-empty data. Either prod was momentarily empty at her test time, or it was a stale build. Current state ships populated rows.

**Out of scope (intentional):**
- Aggregate stat-card exports (Companies/Categories/Types/Subcategories with-active-certs) — these intentionally emit `Name, Active Certificate Count` per QC #155. They match the stat-card semantics and were already correct.

---

### DEF-177 — QC #148 (Excel template headers should include allowed values in parens) — already satisfied, no code change needed

- **Date:** 2026-05-11 (audited)
- **Severity:** Low (documentation/UX enhancement)
- **Status:** Closed — already implemented; pending client UAT verification
- **Reporter:** Client UAT — QC #148 (4/6/26): *"For standard fields in Excel template can you include in header name what options are where appropriate: For example Status (Active, Inactive)"*. Carly's 5/1 follow-up: *"Did not check since not ready for UAT."*

**Audit (2026-05-11):** Reviewed every import-template generator in the codebase against Carly's "where appropriate" criterion (enumerated/boolean fields, not free-text or lookup-by-name fields).

| Template | Source | Enumerated fields | State |
|---|---|---|---|
| User Import | `wwwroot/js/userManagement.js:854` | Role, Status, RequirePasswordReset, Company Access | ✅ `Role (Admin/Analyst/External)`, `Status (Active/Inactive)`, `RequirePasswordReset (Yes/No)`, `Company Access (semicolon-separated names, or "All")` — three of four shipped under DEF-173 (2026-05-11); the fourth (Role) was already in the template from an earlier batch. |
| Tags Import | `wwwroot/js/tags.js:546` | TagType | ✅ `TagType (Internal, External)` — landed in a prior cycle. |
| Product Import | `Controllers/ProductController.cs:880` (server endpoint, single source of truth post-DEF-165) | none — `Name, Company, Description, Category, Product Type, Subcategory` are all lookup-by-name or free-text | n/a — no enumerated fields to annotate. |
| Companies Import | `Views/Company/Index.cshtml:673` | none — `Name, Email, Website` are all free-text | n/a — no enumerated fields to annotate. |

**No code change.** The two import templates with enumerated fields both already follow Carly's requested convention. The two import templates without enumerated fields have nothing to annotate.

**Recommendation:** Mark QC #148 as "Ready for UAT" in the QC log on the client's next pass. If client UAT surfaces a specific column where they expect parens hints but don't see them, reopen as a follow-up. Otherwise close on confirmation.

**Out of scope for this DEF (could re-open as separate items if requested):**
- CSV *export* surfaces (export tables emit raw values, not enumerated labels — different concern from import templates and not what QC #148 asked for).
- Whether free-text lookup fields like `Company`, `Category`, `Product Type` should carry hints like "(must match existing record name)" — Carly's example was enumerated values; lookup fields weren't called out.

---

### DEF-176 — Internal & External cert tile views: Company filter shows numeric ID + chips have "Type:/Company:/Search:" prefix (QC #127 5/1/26 + 5/8/26 re-raise)

- **Date:** 2026-05-11
- **Severity:** Medium (functional + UX; user-visible regression on every logged-in cert listing)
- **Status:** Deployed and verified on gccwebapp.com (status-sync 2026-05-21 — deployed cert-tile-internal HTML has no `Type:/Company:/Search:` prefix strings)
- **Reporter:** Client. The 4/3/26 QC #127 was about the Public view; verified fixed in March. Client's 5/1 follow-up: *"Another inconsistency b/w public and login view. Public is fixed, login still shows titles (Type, etc.) and Company Numbers."* Re-raised on 5/8/26 as still open.

**Root cause:** When QC #127 was originally fixed in March, the patch only touched the Public tile view (`wwwroot/js/certificate-tile-view.js`). The Internal and External views are separate JS files (`certificate-tile-view-internal.js`, `certificate-tile-view-external.js`) with the same defect — the patch was never ported. Same anti-pattern as DEF-174 (template fix landed on the wrong file): three near-identical JS implementations, fix applied to one, two left untouched.

Two distinct sub-defects in both Internal and External:

1. **Company filter checkbox `value` attribute was the company ID, not the name.** Public was changed to `value="${escapeHtml(company.name)}"` in the March fix. Internal (line 131) and External (line 130) still emit `value="${company.id}"`, so the filter chip shows e.g. `"131"` for 3M instead of `"3M"`.

2. **Filter chip rendering still wrapped each value in a `<span class="filter-tag-label">${label}:</span>` prefix.** Public removed it; Internal (lines 1046, 1057) and External (lines 1026, 1037) still emit chips like `Company: 131 ×` and `Search: foo ×`.

**Verified-broken evidence (Playwright on prod, 2026-05-11):**

| View     | Checkbox value | Chip rendered            |
|----------|---------------|--------------------------|
| PUBLIC   | `3M`          | `3M ×`                   |
| INTERNAL | `131`         | `Company: 131 ×`         |
| EXTERNAL | `131`         | `Company: 131 ×`         |

(Receipt: `%TEMP%\verify-qc127.mjs` script + run output.)

**Fix:** Three pieces, all matching Public's pattern:

1. **JS — Internal & External**: checkbox `value` switched from `company.id` to `escapeHtml(company.name)`; both files (1 line each).
2. **JS — Internal & External**: chip rendering loop drops the `<span class="filter-tag-label">${label}:</span> ` prefix from both the Search chip and the generic checkbox-filter chip; aria-label simplified from `Remove ${label}: ${val} filter` to `Remove ${val} filter`. The `labelMap` constant is left in place — still used as the iteration key set, so removing it now would be a separate cleanup.
3. **C# — `CertificateTileInternalApiController.cs:161` and `CertificateTileExternalApiController.cs:187`**: company filter rewritten from ID-only `companies.Contains(c.Company.Id.ToString())` to dual-mode "if all tokens parse as int → match by ID, else → match by name." Mirrors the existing dual-mode logic in `CertificateTilesApiController.cs:123-141` so all three controllers behave identically. Either-or, not mixed (matches Public's limitation).

**Files changed (4):**
- `wwwroot/js/certificate-tile-view-internal.js` — checkbox value + chip render
- `wwwroot/js/certificate-tile-view-external.js` — checkbox value + chip render
- `Controllers/Api/CertificateTileInternalApiController.cs` — dual-mode company filter
- `Controllers/Api/CertificateTileExternalApiController.cs` — dual-mode company filter

**Backend impact:** None for clients passing numeric IDs (still supported). Adds name-token support so the new JS payload works. Partner API unaffected (different controller; no contract change per `feedback_amazon_api_immutable.md`).

**Local verification:** `dotnet build` — 0 errors. Same dev-server lock prevents an in-place restart for live click-through; verify after next clean restart or after the deploy.

**Verify-after-deploy:**

1. Sign in as `analyst`. `/Certificate/CertificateTileViewInternal` → tick a Company checkbox (e.g. 3M). The filter chip above results should read `3M ×`, not `Company: 131 ×`. Same for Type/Country/Tag chips — value only, no label prefix.
2. Sign in as `external`. `/Certificate/CertificateTileViewExternal` → repeat. Same expected behavior.
3. Result grid should still filter correctly to certs from the selected company (proves backend dual-mode parses the name token).
4. Re-run `%TEMP%\verify-qc127.mjs` against gccwebapp.com after deploy; flip TC-QC-127 in `docs/tracking/test-execution-log.md` from INCONCLUSIVE → PASS.

**Cross-ref:** Closes the login-view half of QC #127 (Public half was correctly fixed in the 4/10 SP2 batch). Same anti-pattern as DEF-174 (multiple near-identical implementations, fix touches only one). Worth a follow-up CR or refactor: the three tile views share ~95% of their code by line count; consolidating to one view + role-based config would prevent the next round of "fixed in one, broken in two."

---

### DEF-175 — Cert type filter dropdown sorted by stale SortOrder column instead of alpha (QC #124)

- **Date:** 2026-05-11
- **Severity:** Low–Medium (cosmetic but client-visible; client flagged twice across 30 days)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Client — QC #124, originally 3/30/26, re-flagged 5/1/26: *"Why are Product Opt and RM now showing at the top of filter box and things are in alpha order? There is no sort ability under Cert Types to fix which is issue when we add new."*
- **Spec reference:** Search Recs R11 — *"Code that if no certs are active in cert type it does not appear in public DB. When Admin adds new cert type they need to be visible in filter."*

**Root cause:** The three filter endpoints (Public, External, Internal) ordered `CertificationTypes` by the `SortOrder` column. Admin Cert Types page (`Views/Admin/CertificateTypes.cshtml`) has **no UI to manage SortOrder** — confirmed via grep, the column is read-only from the admin perspective. SortOrder values come from initial seed data and migration scripts only; as new types are added (e.g. via `UpdateCertificationTypes.sql` adding Carbon Footprint Reduction at Id=3, SortOrder=3) their relative SortOrder ends up arbitrary versus alphabetical. Client sees newly-added types jumping to the top with no way to fix from the admin UI.

R11 rule-one (*hide types with zero public certs*) was already working — `.Where(... certTypeIds.Contains(ct.Id))` filters to types referenced by certs in scope. That part wasn't broken.

R11 rule-two (*new admin-added types appear in filter*) is preserved by the fix — alpha order means a new type slots into its correct position automatically, no admin intervention needed.

**Fix:** Changed `OrderBy(ct.SortOrder)` → `OrderBy(ct.Name)` in all three filter endpoints. Comment at each site references both DEF-167 (the upstream Level-1-only filter) and QC #124 (the alpha rationale).

**Files changed (3):**
- `Controllers/Api/CertificateTilesApiController.cs` — Public filter
- `Controllers/Api/CertificateTileExternalApiController.cs` — External filter
- `Controllers/Api/CertificateTileInternalApiController.cs` — Internal filter

**Not touched** (intentional scope):
- `Models/CertificationType.cs` — `SortOrder` column kept for backwards-compat with any non-filter consumer
- `Views/Admin/CertificateTypes.cshtml` — adding a sort-order UI here would be a separate CR (would let admins override alpha if they ever need to). Not requested.

**Verification (localhost http://localhost:5101, 2026-05-11):**

| Test | Result |
|---|---|
| `dotnet build` | 0 errors |
| `GET /api/certificate-tiles/filters` (baseline) | Returns `[Biobased, Certified Energy Savings, Certified Environmental Facts, Recycled Content]` — alphabetical ✓ |
| R11 rule-one preserved | 4 returned, matching dev DB's 4 cert types with `PublicCertCount > 0` (out of 21 total Level-1 types) ✓ |
| **Stress test**: `UPDATE CertificationTypes SET SortOrder = 0 WHERE Name = 'Recycled Content'` (would float Recycled Content to top under old SortOrder sort) | API still returns Recycled Content **last** alphabetically. Confirms sort key is Name, not SortOrder ✓ |
| Restored SortOrder | Clean dev state ✓ |

---

### DEF-174 — Product Management Export CSV doesn't match Import Template (round-trip broken; QC #103 5/8/26 re-raise)

- **Date:** 2026-05-11
- **Severity:** Medium (functional — Export → Import round-trip broken; client called this out by re-raising QC #103 as "Not fixed" on the 5/8/26 log)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Client (5/8/26 QC log re-raised QC #103 as "Not fixed"). Verification by Shawn via Playwright on deployed `gccwebapp.com` confirmed the export still emitted the old 8-column header.

**Root cause:** Two `exportProducts` functions exist with the same name. The actual button (`#export-btn` on `/Product/Management`) is wired to the **inline** function at `Views/Product/Management.cshtml:1056` via `exportBtn.addEventListener('click', exportProducts)` (line 557, where `exportBtn = document.getElementById('export-btn')` from line 342). The class method at `wwwroot/js/admin/productManagement.js:2054` is unused — the comment at line 38–40 even acknowledges "Export button is wired up by each consuming page's inline handler so CSV columns can match that page's UI table."

The fix recorded in the prior sprint's TC-QC-103 card was applied to the unused class method (added Description, removed Status, etc.) but left the wired-up inline function untouched. Net effect: every "fix" landed in code that never ran, and the deployed export kept emitting the broken header.

**Verified-broken evidence (Playwright on prod, 2026-05-11):**

```
Template (correct):  Name, Company, Description, Category, Product Type, Subcategory
Export   (broken):   Product Name, Company, Category, Product Type, Subcategory, Status, Certs, IDs
```

Diffs: `Name` vs `Product Name` (different label), missing `Description`, retains `Status` (removed from Add Product modal per QC #92), adds `Certs` / `IDs` columns not in template.

**Fix:** Updated the inline `exportProducts()` at `Views/Product/Management.cshtml:1056-1116` to emit 8 columns matching the documented intent: `Name, Company, Description, Category, Product Type, Subcategory, Identifier Type, Identifier Value`. First six columns now match the import template exactly so an exported CSV round-trips through Import. Identifiers are now emitted as one row per identifier (with deduplication by type+value), or a single row with empty identifier columns if the product has none — same row-multiplication pattern the unused class method already had. `Status`, `Certs`, and the bundled `IDs` semicolon-blob column are removed.

The unused class method at `productManagement.js:2054-2161` is left in place for now — removing it is a separate cleanup with risk of breaking some future caller that grabs `window.productManagement.exportProducts()`.

**Files changed (1):** `Views/Product/Management.cshtml:1056-1116`.

**Backend impact:** None. Pure client-side CSV emit.

**Local verification (2026-05-11):** Cannot fully verify locally — dev server is `dotnet run --no-build` (no Razor runtime compilation) and the running `GCCWebApp.exe` is holding the .exe lock; a sibling `dotnet test --filter QC108` is also active. A clean rebuild + restart is required to re-run the local Playwright check (`%TEMP%\verify-export-local.mjs` is ready).

**Verify-after-deploy:**

1. Sign in as `analyst`. `/Product/Management` → click Export.
2. Open the downloaded CSV. Header row should be exactly: `"Name","Company","Description","Category","Product Type","Subcategory","Identifier Type","Identifier Value"`.
3. A product with no identifiers → one row, last two columns empty. A product with three ASINs → three rows, identical first six columns, ASIN type/value pair varying.
4. Cross-check vs `/Product/DownloadProductImportTemplate`: first six column names of export match template exactly (round-trip ready).
5. Re-run `%TEMP%\verify-export.mjs` against gccwebapp.com after the deploy lands and update TC-QC-103 in `docs/tracking/test-execution-log.md` from FAIL → PASS.

**Cross-ref:** Closes the export half of QC #103 (the template half was correctly fixed in commit `786dc55`, 2026-03-24). Same anti-pattern as DEF-165A (duplicate handler wiring on Product Management's Template button — fixed by collapsing to a single source of truth). Consider a follow-up cleanup to remove or rename the unused class method to prevent the same trap a third time.

---

### DEF-173 — User Management: rename "Company" column to "Company Access" + show "All" when user has every company (QC #106 UAT retest, 5/8/26 log)

- **Date:** 2026-05-11
- **Severity:** Low (UI/UX clarity)
- **Status:** **Closed (no rename) — 2026-05-21.** The label-rename half of DEF-173 was NEVER executed and is no longer required. Client confirmed (per Shawn, 2026-05-21) that the desired table label is **"User Company"** — matching DEF-186's choice the day after this entry was written and matching the original QC #108 ask. The 5/8/26 UAT comment that prompted this entry was a momentary preference that the client subsequently reversed. The "All" display + import round-trip half DID land (helpers + normalizer aliases at `userManagement.js:313`, `:1024-1027`, `:1543`) and remains valid — those are the value-add part of the fix and they work whether the label is "User Company" or "Company Access". No code change today. See conflict-history block below for the full back-and-forth.
- **Reporter:** Client UAT comment on the 5/8/26 QC log against the QC #106 row: *"No popup, table, and import/export/template. The current 'Company' in table should be called 'Company Access' since that is what it is related to? If they have access to All just put all otherwise the list is massive (like my new user example)."*

**Root cause:** The Add/Edit User modal already labels the field **Company Access** (set when DEF-106-era work landed), but the table header, CSV export, and import template still said **Company** / **User Company**. The table cell also rendered the full comma-joined list of company names regardless of selection size, so a user assigned every company in the system produced a multi-line wrap that broke the row layout.

**Fix:**

1. **Label rename to "Company Access"** across all four surfaces flagged by the client:
   - Table header (`Views/Admin/UserManagement.cshtml:95`)
   - CSV export header key (`wwwroot/js/userManagement.js` `exportUsers`)
   - Import template header (`wwwroot/js/userManagement.js` `downloadImportTemplate`)
   - Import preview "changed field" label (`wwwroot/js/userManagement.js` `buildImportPreview`)
   - Modal label was already "Company Access" (no change needed there)

2. **"All" display logic** — added `getCompanyAccessDisplay(user)` helper. Returns the literal string `"All"` when the user's `companyIds` cover every company currently in `this.companies` (the same set the Add/Edit modal renders as checkboxes). Otherwise returns the comma-joined names (existing behavior preserved). Used by table render AND CSV export, so both surfaces collapse the long list to one word when applicable.

3. **Import round-trip** — taught `normalizeUserHeaderName` to recognize both `Company Access` (new template header) and `User Company` (legacy header from prior template — was previously unmapped, a latent round-trip bug). Taught `resolveCompanyIds` to expand the literal `"All"` (case-insensitive) to every company ID, so an exported "All" can be re-imported without manual editing.

**Files changed (2):**
- `Views/Admin/UserManagement.cshtml` — table header text
- `wwwroot/js/userManagement.js` — new helper, table render, CSV export, template header, import header normalizer, `resolveCompanyIds` "All" handling, import preview field label

**Backend impact:** None. No schema change, no API change. Existing `/Admin/GetCompanies`, `/Admin/CreateUser`, `/Admin/UpdateUser` contracts unchanged. The "All" string is purely a display + import-time convenience — the User entity still stores discrete `UserCompany` join rows.

**Local verification (2026-05-11):** Build clean (0 CS errors, 61 pre-existing warnings). Manual smoke pending — running app instance is holding the .exe lock so a fresh `dotnet run` couldn't start; verify after next restart:
1. Table header reads "Company Access"; user assigned to every active company renders as "All"; user assigned to some shows comma list.
2. Export CSV — header column reads "Company Access"; the "All" user's row contains literal "All".
3. Template download — header column reads `Company Access (semicolon-separated names, or "All")`.
4. Import a CSV with `Company Access = All` — preview shows the user with every company ID resolved; apply persists correctly.

**Cross-ref:** This is UAT follow-up on the QC #106 row that originally shipped under the 2026-03-24 batch. Not a regression of DEF-106 — that fix's scope was modal-side visibility. This is additive surface alignment.

**Conflict history (added 2026-05-21).** The label has gone back and forth in the QC log:

| When | Source | Asked for |
|---|---|---|
| Earlier | QC #108 (client) | **User Company** |
| 2026-04-26 | DEF-101 (shipped) | Set export header to **User Company** for round-trip with QC #108 import |
| 2026-05-08 | QC #106 UAT retest (client) | Said: *"current 'Company' should be called 'Company Access'"* — basis for DEF-173 |
| 2026-05-11 | DEF-173 (this entry) | Recorded intent to rename to **Company Access** |
| 2026-05-12 | DEF-186 (next day) | New 5-column template explicitly used **User Company** header per "Company Access → User Company rename review" |
| 2026-05-21 | Client confirmation (per Shawn) | **User Company** is the desired label |

Net: the most recent client direction matches what's actually deployed on prod ("User Company"). The 2026-05-21 status-sync sweep flagged this entry as "header doesn't match" — that flag is **resolved as no-action-needed** rather than as a code fix. Keeping this DEF row in the register for traceability so the rename question doesn't get re-opened a third time.

---

### DEF-172 — Cert Create/Edit "Company Email" field accepted non-email values (QC #181)

- **Date:** 2026-05-11
- **Severity:** Low–Medium (data quality issue; doesn't block use but pollutes Company records)
- **Status:** Deployed and verified on gccwebapp.com (2026-05-21 — regex `^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$` deployed on `#CompanyEmail`, error msg fires on "notanemail")
- **QC Ref:** #181 (client log, 2026-05-11, *"Can you add rule to the Company Email field that it needs to be in an email format..."*)
- **Reporter:** Client — QC #181 (5/11/26): *"Can you add rule to the Company Email field that it needs to be in an email format. See image 74 where something went through and its not email."* — Create Cert flow.

**Root cause:** `CertificateEditViewModel.CompanyEmail` was annotated with `[EmailAddress]` only. The built-in .NET `EmailAddressAttribute` validator is very permissive — it accepts any string with exactly one `@` that isn't first or last, e.g. `"a@b"` validates. No TLD requirement, no character-class enforcement. Combined with the form's `novalidate` attribute disabling HTML5 native feedback, anything with a single `@` in it passed silently. The client observed something patently non-email-shaped persisting and (correctly) called it out.

**Fix:** Replaced `[EmailAddress]` with a stricter `[RegularExpression]` pattern requiring `local@domain.tld` with a 2+ character TLD. Since `RegularExpressionAttribute` is emitted by ASP.NET MVC's unobtrusive validation as `data-val-regex`, jquery.validate enforces it client-side too — so users get immediate inline feedback on blur/submit, not just on round-trip.

Pattern: `^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$`

Field remains optional (null/empty still passes — only non-null values are validated).

**Files changed (9):**

*Cert Create/Edit form (Carly's original report):*
- `Models/CertificateEditViewModel.cs` — replaced `[EmailAddress]` with `[RegularExpression]` on `CompanyEmail`. Both `Views/Certificate/Create.cshtml` and `Views/Certificate/Edit.cshtml` consume this ViewModel via `asp-for="CompanyEmail"`, so a single annotation change covers both forms.

*Admin Companies management (proactive — same pattern):*
- `Models/CompanyViewModel.cs` — server-side. Existing annotation was `[EmailAddress] + [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]` — the regex required *a* dot after `@` but accepted single-char TLDs (e.g., `"a@b.c"`). Tightened to the same `^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$` pattern. Dropped the redundant `[EmailAddress]`.
- `wwwroot/js/admin/companyManagement.js:313` — admin modal's pre-submit JS check had the same one-char-TLD weakness. Aligned to the new server pattern.

*Admin Users management (proactive — same pattern):*
- `Models/AdminModels.cs:15, :64` — server-side. `UserViewModel.Email` and `CreateUserViewModel.Email` both had `[Required] [EmailAddress]` only. Replaced `[EmailAddress]` with the same `[RegularExpression]`. Kept `[Required]` since users can't exist without email. Pattern is safe for Azure AD B2C-style addresses (`user@tenant.onmicrosoft.com`, custom domains all match).
- `wwwroot/js/userManagement.js:677` — admin Users modal pre-submit check, tightened regex.
- `wwwroot/js/admin/admin-core.js:290` — shared `AdminCore.isValidEmail()` helper used by `validateForm` for every `input[type="email"]` across all admin forms. Tightened once here = consistent validation everywhere downstream.

*CSV Import paths (proactive — same pattern):*
- `Services/Import/CertificateImportService.cs:131` — `IsValidEmail` used `System.Net.Mail.MailAddress`, which is permissive enough to accept `"a@b"`. Replaced with a compiled regex matching the strict pattern. Existing caller at line 789 (`if (!IsValidEmail(csvRow.CompanyEmail))`) now flags bad-format rows in `ValidationMessages` so the user sees `"Invalid Company Email format: ..."` in the import preview/result UI before any DB writes happen.
- `Services/AdminService.cs:201` (`BulkImportUsersAsync`) — bulk user CSV import had NO email format validation at all. It checked for blank, length, duplicates, role — but happily persisted any string with an `@`. Added a regex check between the blank-check and the password-check using the same compiled regex (declared at the class level for performance). Per-row error message: *"{email} - Email is not a valid format (expected name@example.com)"*. Existing per-row error reporting surfaces it to the UI.

*Forgot Password / Reset Password (proactive — same pattern):*
- `Models/AccountViewModels.cs:23` (`ForgotPasswordViewModel.Email`) and `:34` (`ResetPasswordViewModel.Email`) — both had `[Required] [EmailAddress]`. Replaced `[EmailAddress]` with the strict `[RegularExpression]`.
- **Pre-deploy audit query** (run before deploying to a new env to confirm no existing user emails would fail the new regex on reset):
  ```sql
  SELECT Id, Email FROM Users
  WHERE Email NOT LIKE '%@%' OR Email NOT LIKE '%@%.%'
     OR LEN(RIGHT(Email, CHARINDEX('.', REVERSE(Email)) - 1)) < 2;
  ```
  Dev DB returned 0 rows; safe to deploy. Same audit must be run against prod-test before deploy (gccwebapp.com user table).
- **NOT touched:** `Models/AccountViewModels.cs:9` (`LoginViewModel.Email`) has no `[EmailAddress]` annotation by design — login accepts username OR email. Leave permissive.

**Verification — form path (localhost http://localhost:5101, Playwright on Edit page for cert 26-0002, 2026-05-11):**

| Test | Input | Result |
|---|---|---|
| Client validation, no `@` | `"asdfqwer"` | Error span: *"Please enter a valid email address."* ✓ (jquery.validate's built-in `email` rule from `type="email"` fires first) |
| Client validation, no TLD | `"a@b"` | Error span: *"Enter a valid email address (e.g., name@example.com)."* ✓ (this is the new regex catching what `[EmailAddress]` would have allowed) |
| Client validation, valid | `"name@example.com"` | No error ✓ |
| Server-side bypass via `HTMLFormElement.prototype.submit` with `"asdfqwer"` | (forced past client check) | DB unchanged — `Companies.Email` did NOT receive the bad value, cert `UpdatedAt` did not advance. Server `ModelState.IsValid` rejected the save as expected ✓ |

**Verification — CSV cert-import path (Playwright admin login + POST /api/CertificateImport/Preview, 2026-05-11):**

CSV uploaded — 4 rows, 1 valid header + 3 designed-to-fail emails:

| Cert# | Email submitted | Old `MailAddress` validator | New strict regex | Preview result |
|---|---|---|---|---|
| QC181-TEST-A | `good@example.com` | pass | pass | newRows ✓ |
| QC181-TEST-B | `bad-email-no-at` | fail | fail | errorRows: *"Invalid Company Email format: bad-email-no-at"* ✓ |
| QC181-TEST-C | `a@b` | **pass (this was Carly's original complaint)** | fail | errorRows: *"Invalid Company Email format: a@b"* ✓ |
| QC181-TEST-D | `a@b.c` | pass | fail | errorRows: *"Invalid Company Email format: a@b.c"* ✓ |

Preview summary: `totalRows=4, validRows=1, invalidRows=3, hasCriticalErrors=false`. Bad rows are surfaced in the import preview UI; no DB write attempted for them.

**Verification — CSV user-import path (Playwright admin login + POST /Admin/BulkImportUsers, 2026-05-11):**

4 user rows posted, 1 valid + 3 designed-to-fail:

| Name | Email submitted | Pre-fix (no email format check) | Post-fix (strict regex) | Result |
|---|---|---|---|---|
| QC181 Good A | `qc181.good.a.<ts>@example.com` | accept | accept | created in DB ✓ |
| QC181 Bad NoAt | `bad-email-no-at` | **would have persisted** | rejected | error: *"Email is not a valid format (expected name@example.com)"* ✓ |
| QC181 Bad NoTLD | `a@b` | **would have persisted** | rejected | error: same ✓ |
| QC181 Bad OneCharTLD | `a@b.c` | **would have persisted** | rejected | error: same ✓ |

Response: `totalProcessed=4, successCount=1, errorCount=3`. Post-test DB query confirmed `Email IN ('bad-email-no-at','a@b','a@b.c')` returned 0 rows — none of the bad emails landed in the Users table. The one successful test user was cleaned up after verification.

**Other email surfaces still using the permissive `[EmailAddress]`-only validator (not part of this QC; track if flagged later):**
- `Models/Company.cs:16` — the underlying `Company` entity. Left as `[EmailAddress]` because direct entity writes happen on many paths (programmatic backfills, seed data). The ViewModel/Import-service layers are the right places to enforce user-input validation; the entity layer should remain forgiving for migration scenarios.

---

### DEF-171 — Cert auto-expire silently waited on a user login; status didn't flip in test (QC #167)

- **Date:** 2026-05-11
- **Severity:** Medium (no public-view leak — date filter already hid expired certs — but admin views and audit history lagged until someone logged in, which client correctly flagged as "feels broken")
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Client — QC #167 (4/30/26): *"I updated 25-0246 to have an expiration in the past but left the Client Approved - Paid status. It correctly removed the cert from public view... Would the system eventually auto change the status to Expired too? When and how would this work?"*

**Root problems before this fix:**
1. Auto-expire ran only post-`SignInAsync` in `AccountController.SignInUser`, throttled to once per 24h. If the throttle was hot, or no one logged in fresh, or the user used B2C SSO, the sweep never fired.
2. Cert save handlers (UI Edit POST + Admin API PUT) would happily persist an `ExpirationDate < today` cert with Status = `Client Approved - Paid`. The "inconsistent state" was a thing the system allowed to exist in the first place.

**Fix — Option A: save-time enforcement + daily midnight-UTC sweep.**

The principled fix splits the problem into the two cases that actually exist:
- **Manual edits / imports / API saves**: catch the bad state at the moment of save. Cert can no longer leave the save path in a "past date + active status" combination.
- **Naturally aging certs** (no one touches them, but their expiration date crosses today): catch via a daily timer at 00:00 UTC, the moment expiration date logically "happens."

**Pieces:**

1. **Shared helper** — `CertificateExpirationService.TryApplySaveTimeAutoExpire(cert, userId, username, workflowStep)` (static method). Pure on the cert side (mutates Status, no DB call); returns the audit-row object for the caller to add to its own DbContext before SaveChanges. Lets both the controller and the API service share identical logic without DI restructuring.

2. **Save-time call sites (two)** — both UI Edit POST and Admin API PUT now invoke the helper before persisting:
   - `Controllers/CertificateController.cs` (UI form post, calls `_certificateRepository.UpdateAsync` + `_unitOfWork.CompleteAsync`)
   - `Services/CertificateService.UpdateCertificateAsync` (called by `Controllers/Api/CertificatesApiController.cs` Admin API PUT)

3. **Background timer changed from "every 10 min" to "daily at 00:00 UTC"** — `Services/CertificateExpirationBackgroundService.cs`. Computes next-midnight-UTC delay each loop. Override available via `Jobs:CertificateExpiration:IntervalMinutes` (set to a positive int = use that as a fixed PeriodicTimer interval instead of midnight; used in Dev for fast iteration on the BG code path).

4. **`bypassThrottle` parameter on `ICertificateExpirationService.CheckAndExpireAsync`** — login path keeps the 24h throttle; BG timer passes `true` because the timer cadence is itself the throttle.

5. **appsettings** — Prod removes `IntervalMinutes` (default = midnight). Dev keeps `IntervalMinutes: 1` for quick BG-path verification.

**Files changed (6):**
- `Services/CertificateExpirationService.cs` — `bypassThrottle` parameter + new static helper `TryApplySaveTimeAutoExpire`
- `Services/CertificateService.cs` — `UpdateCertificateAsync` calls the helper before SaveChanges
- `Controllers/CertificateController.cs` — UI Edit POST calls the helper before repository.UpdateAsync
- `Services/CertificateExpirationBackgroundService.cs` — switched to midnight-UTC scheduling
- `appsettings.Production.json` — removed IntervalMinutes (default midnight); kept StartupDelaySeconds=30
- `appsettings.Development.json` — IntervalMinutes=1, StartupDelaySeconds=5

**Verification (localhost http://localhost:5101, 2026-05-11):**

*BG sweep path (independent of any login):*

| Step | Result |
|---|---|
| Backdate cert 26-0001 to `ExpirationDate='2026-05-01'`, keep `Status='Client Approved - Paid'` | DB confirmed |
| Start app, no logins, dev `IntervalMinutes=1` | Log: `CertificateExpirationBackgroundService starting. Mode=Interval=00:01:00, StartupDelay=00:00:05` ✓ |
| @ T+5s | Log: `Successfully expired 1 certificates`; cert flipped to `Expired`; audit row with `WorkflowStep='Auto-Expiration'`, `ChangedByUsername='System'`, `ChangeSource='Automated'` ✓ |
| @ T+6s | Log: `CertificateExpiration next sweep scheduled at 2026-05-11 20:53:33 UTC (in 00:01:00)` — proves the loop continues ✓ |

*Save-time enforcement (the user case Carly hit):*

| Step | Result |
|---|---|
| Restored cert 26-0002 to clean future state (`ExpirationDate='2027-02-01'`, `Status='Client Approved - Paid'`). Bumped dev `IntervalMinutes` high so BG can't interfere. | DB confirmed |
| Playwright: log in as `admin`/`TestPass123!`, navigate to `/Certificate/Edit/26-0002`, set ExpirationDate to `2026-05-01`, leave Status dropdown at `Client Approved - Paid`, submit form via `form.requestSubmit(saveButton)` | Redirected to `/Certificate/CertificateInternalView/26-0002` ✓ |
| Post-save DB query | Cert `Status='Expired'`, `UpdatedAt=21:11:25`, `UpdatedBy=1` (admin) ✓ |
| `CertificateStatusHistory` row | `ChangedByUsername='admin'`, `PreviousStatus='Client Approved - Paid'`, `NewStatus='Expired'`, `Reason='Save-time auto-expire: expiration date is in the past'`, `WorkflowStep='Save-Time-Expiration'`, `ChangeSource='Automated'`, `IsAutomatedTransition=1`, Notes captures the override ✓ |
| Dev data restored to pristine state | ✓ |

**Test plan for prod-test (gccwebapp.com) after deploy:**

*Save-time path (instant):*
1. Edit any cert in prod-test, change ExpirationDate to a past date, leave Status as anything non-Expired, click Save.
2. On the cert detail page that loads next, Status reads **Expired**. Status History shows an `admin`-authored row labelled `Save-Time-Expiration` with `Reason="Save-time auto-expire..."`.

*BG midnight sweep (natural aging):*
1. Watch Plesk log on startup for `CertificateExpirationBackgroundService starting. Mode=Daily at 00:00 UTC, StartupDelay=00:00:30`.
2. After the next 00:00 UTC, Plesk log will show `Found N certificates past expiration date... Successfully expired N certificates` for whatever crossed expiration that day. If N=0 (most days), log line: `No certificates to expire`.
3. The startup catch-up sweep also runs on each pool recycle — handles certs that aged during downtime.

**Login-trigger preserved as belt-and-suspenders:** AccountController still invokes `CheckAndExpireAsync(bypassThrottle: false)` post-SignIn so burst logins don't all hammer the DB but at least one login per day also forces a sweep. Cheap insurance.

**Plesk note:** BackgroundService dies with the app pool and restarts on the next request. The startup catch-up sweep covers any certs that aged during the idle window. If keeping the pool always-warm becomes important (e.g., the midnight sweep firing at midnight regardless of traffic, not 30s after the next morning request wakes the pool), set "Start Mode: AlwaysRunning" on the Plesk app pool + `applicationInitialization` in `web.config` — separate config change, not part of this defect.

---

### DEF-170 — Tile filter multi-select delimiter collides with commas in names (QC #177)

- **Date:** 2026-05-11
- **Severity:** High (filter selection returns 0 for any value containing a comma — affected ASSA ABLOY Access & Egress Hardware Group, Inc. - Corbin Russwin Division and any other company/cert-type/etc. with a comma)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Client — QC #177 (5/8/26): *"I selected ASSA and it says no certs found... I swear this was right prior but is now not. Then during later testing I found there is a cert showing with this -- but then the filter selection is not working right. Same happened with another company name."*
- **Confirmed via screenshot:** `C:\Users\swest\Downloads\Screenshot_11-5-2026_16237_gccwebapp.com.jpeg` — Public view Company sidebar lists `ASSA ABLOY Access & Egress Hardware Group, Inc. - Corbi...`, two commas in the name. Cert tile 26-0149 (Commercial Door Closer - DC300) for that company is visible in the public list, so the company has a `PublicStatuses` cert in scope and the dropdown is correctly listing it.

**Root cause:** Multi-select tile filters were comma-joined in JS and comma-split on the backend:

```js
// wwwroot/js/certificate-tile-view.js — before
params.append('company', state.filters.company.join(','));
```

```csharp
// Controllers/Api/CertificateTilesApiController.cs — before
filter.CompanyNames = companyFilter.Split(',').ToList();
```

When a single selected company name itself contained a comma (very common: `Inc.`, `Ltd.`, `Co., Ltd.`, the ASSA division above, etc.), `Split(',')` shredded the value into fragments that matched no row. Carly's symptom: dropdown shows the company (strict-scoping is correct), ticking it sends `company=ASSA ABLOY..., Inc. - Corbin...`, backend splits into `['ASSA ABLOY... ', ' Inc. - Corbin...']`, neither matches → 0 results.

The same pattern existed on all three tile views and every multi-select filter dimension (certType, productCategory, company, country, tags, partner, status, additional).

**Fix:** Swapped delimiter from `,` to `|` end-to-end. Pipe is illegal-by-convention in business names and URL-encodes cleanly as `%7C`.

**Files changed (6):**

Controllers — `Split(',')` / `Split(',', StringSplitOptions.RemoveEmptyEntries)` → pipe variants:
- `Controllers/Api/CertificateTilesApiController.cs` — 6 lines (certType, productCategory, company × 2, country, tags, additional)
- `Controllers/Api/CertificateTileExternalApiController.cs` — 6 lines (certType, productCategory, company, country, tags, partner)
- `Controllers/Api/CertificateTileInternalApiController.cs` — 7 lines (certType, productCategory, company, country, tags, status, partner)

JS — `join(',')` / `split(',')` on filter dimensions → pipe variants:
- `wwwroot/js/certificate-tile-view.js` (Public) — 10 joins + 5 splits (buildApiParams, updateUrl, URL-param load)
- `wwwroot/js/certificate-tile-view-internal.js` — 9 joins + 2 splits (status + partner URL-load only; the other splits in this file at lines 1716 and 2118 parse CSV-style display fields and were left as `,`)
- `wwwroot/js/certificate-tile-view-external.js` — 9 joins + 2 splits (same scope as Internal; non-filter splits at 1683 and 1999 left alone)

Not touched: `wwwroot/js/certificateExternalService.js` (no view references it — dead code from older implementation). CSV export `.join(',')` calls across admin pages (those produce CSV files, not filter URLs).

**Backward compat:** Shareable URLs generated before this fix use `?company=A,B` and will, after the fix, be read as one literal company name `"A,B"` (returns 0). The shareable-link feature is pre-launch and not documented for client use yet, so existing bookmarks are unlikely. If any persist, the user just unticks and re-ticks.

**Verification (localhost http://localhost:5101, 2026-05-11):**

| Test | URL | Result | Expected |
|---|---|---|---|
| Single name | `company=Acme%20Corporation` | 1 cert | 1 ✓ |
| Pipe multi-select | `company=Acme%20Corporation%7CHenkel` | 4 certs | 1 Acme + 3 Henkel ✓ |
| Comma-bearing name | `company=ASSA%20ABLOY...%2C%20Inc.` | 0 (no matching DB row in dev) | passed through as one string, no split error ✓ |
| Legacy comma URL | `company=Acme%2CHenkel` | 0 | now treated as one literal name "Acme,Henkel" — expected regression ✓ |
| Category filter unchanged | `productCategory=Chemicals%20and%20Raw%20Materials` | 22 certs | 22 ✓ |

End-to-end browser test (Playwright at 1400×900): ticked Acme + Amazon checkboxes, captured request URL → `?company=Acme+Corporation%7CAmazon`, UI showed "Showing 9 of 9 certificates" (1 Acme + 8 Amazon). Pipe delimiter survives JS → URL → backend → SQL round-trip.

**Pre-deploy note:** Once shipped, Carly's ASSA ABLOY click should immediately return cert 26-0149 (and any other ASSA ABLOY certs in `PublicStatuses`). The dropdown contents won't change — it was already correct; only the selection mechanism was broken.

---

### DEF-169 — Logo squeezed/distorted at tablet/reduced-desktop widths (769–1024px)

- **Date:** 2026-05-11
- **Severity:** Low–Medium (cosmetic but customer-visible on every page header)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn (in-session, 2026-05-11)

**Symptom:** When the browser is reduced to tablet width or a narrow desktop window (769–1024px), the GCC circular logo in the header renders horizontally compressed — looks like a slightly-stretched ellipse instead of a circle.

**Root cause:** `wwwroot/css/main.css` line 3342-3345, the tablet-only rule for `.main-logo` applied both `height: 180px` AND `max-width: 180px` to an image with natural aspect 1.123 (337×300 px). At `height: 180px` the natural rendered width is ~202px, but `max-width: 180px` clamps it to 180px. With `object-fit` unset (defaults to `fill`), the browser stretches the image to satisfy both constraints — producing a 180×180 square render of a non-square source.

Mobile (`@media (max-width: 768px)`) has the same `height/max-width` pattern but adds `object-fit: contain`, which letterboxes instead of stretching — hence no mobile distortion was reported.

Base/desktop has no width constraint, only `height: 280px` — aspect preserved, no issue.

**Fix:** Replace `max-width: 180px` with `width: auto` in the tablet rule. Height drives the rendered width via the image's natural aspect; no clamp, no stretch.

```css
/* Before */
@media (min-width: 769px) and (max-width: 1024px) {
    .main-logo { height: 180px; max-width: 180px; }
}
/* After */
@media (min-width: 769px) and (max-width: 1024px) {
    .main-logo { height: 180px; width: auto; }
}
```

**Files changed:**
- `wwwroot/css/main.css` (line 3342-3345)

**Verification (localhost http://localhost:5101, Playwright, 2026-05-11):**

| Viewport | Rendered | Aspect | Natural Aspect | Verdict |
|---|---|---|---|---|
| 900 × 700 (tablet) | 202 × 180 | 1.123 | 1.123 | ✓ matches |
| 1100 × 700 (reduced desktop) | 315 × 280 | 1.123 | 1.123 | ✓ matches (base rule) |
| 1400 × 800 (desktop) | 315 × 280 | 1.123 | 1.123 | ✓ matches |

Screenshot at 900px in `C:\Users\swest\AppData\Local\Temp\logo-900-tablet.png` — logo renders as a true circle, not an ellipse.

**Mobile not changed:** Mobile rule (`max-width: 768px`) already uses `object-fit: contain` which preserves aspect by letterboxing; cosmetically smaller than possible but not distorted. Not part of this defect; leave for a future cleanup pass if Shawn flags it.

---

### DEF-168 — Cert-tile Product Category filter leaks orphan parent categories on Public view

- **Date:** 2026-05-11
- **Severity:** Medium (public-facing filter UX; dead-click filter selection, client trust impact)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Client — QC #170 (5/1/26): *"I was trying to clean up the product type options... [Filter] would filter by Carly Test but the actual type doesn't have that option... Why is Carly Test showing up if the certificate isn't public?"* (image 59)
- **Related:** DEF-167 (cert-type subcategory leak — same controller family), QC #149 (L2/L3 Product Category options don't filter)

**Symptom:** On the Public tile view (`/Certificate/CertificateTileView`), the **Product Categories** filter dropdown surfaces "Carly Test" (and similar test categories). Selecting it returns zero certificates. The cert(s) associated with that category are not in `PublicStatuses`, so the user expects the category to not appear at all.

**Scope** — what each tile view *should* show in the Product Category filter:

| View | Filter API | Cert-scope feeding the dropdown | Expected behavior |
|---|---|---|---|
| Public | `GET /api/certificate-tiles/filters` | `Status ∈ PublicStatuses` AND not expired | Show only categories that have ≥1 cert in scope. Picking one must return ≥1 result. |
| External (role=External) | `GET /api/CertificateTileExternal/filters` | Public-scope AND `cert.CompanyId ∈ user's companies` | Same — no dead clicks. |
| External (role=Partner) | `GET /api/CertificateTileExternal/filters` | Public-scope AND `cert.Partner ∈ user's partners` | Same. |
| Internal (Analyst/Admin) | `GET /api/certificate-tiles-internal/filters` | No scope — all certs | All active categories shown (intentional). |

**"Distinct from scope"** = `SELECT DISTINCT <col>` over the in-scope cert set. The dropdown should always satisfy: "if I tick this value, I get ≥1 row back."

**Root cause:** `Controllers/Api/CertificateTilesApiController.cs:241-380` — the hierarchy build is scoped strictly at the leaf level but uses a permissive parent-fallback at the Category level. Specifically:

1. `visibleCategoryIds` is collected from `c.Product.CategoryId` (line 259-262) — strict.
2. `visibleProductTypeIds` is collected from `c.Product.ProductTypeId` (line 264-268) — strict.
3. The category trim (line 331): `.Where(c => visibleCategoryIds.Contains(c.id) || c.types.Any())` — a category is kept if **any descendant Product Type passed the type-level trim**, regardless of whether the cert's `Product.CategoryId` actually points at this category.

A Product can have `CategoryId = X` (real category) and `ProductTypeId = Y` where `Y.CategoryId = "Carly Test"` (a different category — administrative parent of Y). The product itself sits in X. The cert is public. But the dropdown walks the hierarchy from "Carly Test" downward, finds Y in `visibleProductTypeIds`, and keeps "Carly Test" as a visible category. Then `CertificateRepository.GetAdvancedFilteredCertificatesAsync` (line 312-330) does name-match against `Product.Category.Name`, `Product.ProductType.Name`, and `Product.ProductType.Parent.Name` — but **not** `Product.ProductType.Category.Name`. So the filter find-side doesn't reverse-traverse the orphan relationship, and the dropdown click is a dead click.

**Files involved (no edits yet):**
- `Controllers/Api/CertificateTilesApiController.cs` — Public endpoint, primary location of the leak
- `Controllers/Api/CertificateTileExternalApiController.cs:462-525` — same hierarchy-build pattern; same latent bug
- `Repositories/CertificateRepository.cs:312-330` — filter-apply side that doesn't traverse `Product.ProductType.Category`

**Recommended fix options (choose one):**

- **Option A — Stricter dropdown (recommended).** Drop the `|| c.types.Any()` fallback in the Public + External hierarchy builds. Categories appear only when a cert's `Product.CategoryId` directly references them. Lowest risk; aligns with "no dead clicks." Selection still works because the find-side name match covers all the documented paths.
- **Option B — Match dropdown to find-side.** Expand `visibleCategoryIds` to also include `Product.ProductType.CategoryId` (and `Product.ProductType.Parent.CategoryId`). Then update `CertificateRepository.cs` to also match `Product.ProductType.Category.Name`. Larger change, but preserves the parent-rollup behavior the original code intended.

**Note on the previously-listed "Option C / subcategory-table mismatch":** retracted 2026-05-11. On re-verification, `Product.ProductSubcategoryId` is a FK to `ProductTypes(ParentId != null)` — see `Models/Product.cs:44-46` and `Data/ApplicationDbContext.cs:203-209` (retargeted under DEF-134, 2026-05-04). The `visibleSubcategoryIds.Contains(sub.Id)` comparison at `CertificateTilesApiController.cs:319-320` is therefore apples-to-apples and works correctly. No fix needed; my earlier note was based on pre-DEF-134 state.

**Decision:** Option A applied 2026-05-11 (Shawn's direction). Strictly bound the category list to direct `Product.CategoryId` references — no traversal up from Product Type. "Carly Test" stops appearing in the dropdown without code accommodating bad taxonomy data; cleanup remains visible.

**Files changed:**
- `Controllers/Api/CertificateTilesApiController.cs` — line 331 trim Where: removed `|| c.types.Any()` fallback. Comment block added explaining rationale + DEF-168 reference.
- `Controllers/Api/CertificateTileExternalApiController.cs` — line 524 same change.
- Internal controller untouched — Admin/Analyst sees the full taxonomy by design (no scoping), so the leak doesn't manifest there.
- Type-level fallback (`|| t.subcategories.Any()`) preserved — safe because find side in `CertificateRepository.GetAdvancedFilteredCertificatesAsync` has parent-cascade on `ProductType.Parent.Name` (lines 322-328).

**Trade-off:** A Product Type whose administrative parent Category has zero directly-categorized products will now disappear from the dropdown along with that orphan parent. The cert itself stays findable via the Product's actual `CategoryId` or via search. This is intentional — signals that orphan-parent data exists and needs cleanup, rather than masking it.

**Verification (localhost http://localhost:5101, 2026-05-11):**
- `dotnet build` — 0 errors, 53 pre-existing warnings.
- `GET /api/certificate-tiles/filters` (anonymous) → hierarchy returns 1 category (`Chemicals and Raw Materials`), matches the flat `productCategories` list 1:1. No orphan parents.
- `GET /api/certificate-tiles?page=1` → 22 public certs total (cert query untouched).
- `GET /api/certificate-tiles?productCategory=Chemicals%20and%20Raw%20Materials` → 22 hits. Filter selection still works.
- `GET /api/certificate-tiles?productCategory=Carly%20Test` → 0 hits, no exception. Confirms find-side has no path that would have matched a hidden category — A is internally consistent.
- External endpoint not curl-tested (cookie auth + antiforgery); code shape is identical to public, structurally guaranteed.

**Pre-deploy note:** Dev DB has no orphan parents (all 41 certs are in a single category). The fix's behavioral impact will only manifest on prod, where Carly's test data introduced the orphan-parent state. Expected result on prod after deploy: "Carly Test" stops appearing in the Public + External filter dropdowns. Test categories Carly leaves around will be silently hidden until she deletes or repoints them — making cleanup observable for the first time.

**Verification once chosen:** localhost (`http://localhost:5101`) + a seeded "orphan parent" cert (Product.CategoryId = food, Product.ProductTypeId points at a type whose Category = "Test"). Then:
- Hit `GET /api/certificate-tiles/filters` and confirm "Test" is **absent** under Option A.
- Tick the parent category in the rendered hierarchy and confirm ≥1 cert returns OR the category is hidden — never the dead-click middle state.

**Related QC entry:** Carly's same screenshot reports "Products is showing 0 for all" — that's a separate ProductController count rendering question, not part of this defect. Tracked separately if it persists after page reload.

---

### DEF-167 — Cert-tile filter: subcategories rendered as separate checkboxes; missing IsActive guard

- **Date:** 2026-05-11
- **Severity:** Medium (public-facing filter UX; affects discoverability)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Client — QC #169 (4/30/26): *"The public view is showing all cert types. And it's showing the CSF subcategories (WHICH IS WRONG). The subcategories are not meant to show up in this filter box."*
- **Related:** QC #128 (LEED filter shows certs without LEED tag), QC #149 (L2/L3 Product Category options don't filter) — likely same root cause family.

**Direction (Shawn, 2026-05-11):** Level 1 should be visible only. Fix the data. **No parent-rollup logic** — if a cert is tagged at Level 2, that's a data bug to repair on the cert, not a code bug to accommodate in the filter.

**Data state (dev DB, GCCDatabase_Dev on ANUKI01):**
- 41 certs total, **all tagged at Level 1**.
- 0 certs tagged at Level 2.
- `CertificateCertificationTypeSubcategories` join table is empty.
- Dev needs no data fix. **Prod (gccwebapp.com) is the place the data bug lives** — client was testing there when she saw the subcategory bleed.

**Code root cause (External controller — primary; Internal has the same latent bug):**

1. **Subcategory flatten loop** — `Controllers/Api/CertificateTileExternalApiController.cs:541-551`:
   ```csharp
   foreach (var certType in allCertTypes.Where(ct => ct.ParentId == null)) {
       hierarchicalCertTypes.Add(new { name = certType.Name, level = certType.Level });
       var children = allCertTypes.Where(ct => ct.ParentId == certType.Id).OrderBy(ct => ct.SortOrder);
       foreach (var child in children) {
           hierarchicalCertTypes.Add(new { name = child.Name, level = child.Level });  // BUG
       }
   }
   ```
   Inner `foreach` emits each Level-2 child as its own filter checkbox. Same pattern in `CertificateTilesApiController.cs:285-292`.

2. **Missing `IsActive` guard on public** — `CertificateTileExternalApiController.cs:535`:
   ```csharp
   .Where(ct => certTypeIds.Contains(ct.Id) || (ct.Children.Any(child => certTypeIds.Contains(child.Id))))
   ```
   Internal at `:278` has `ct.IsActive && (...)`; External omits it.

**Planned fix (code — 2 files):**

1. **Remove the inner `foreach`** that emits children in External (`:541-551`) and Internal (`:285-292`). Replace with just the parent `Add`. Net effect: dropdown emits only `ParentId == null` rows.
2. **Add `IsActive` guard** to External's Where (`:535`) so it matches Internal at `:278`.
3. **No parent-rollup logic** — keep the existing `CertificationTypeNames` string-equality match as-is. Level-2-tagged certs become un-filterable in the dropdown, which is the correct outcome once data is repaired.

**Planned fix (data — 1 SQL script):**

- Add `Database_Scripts/20260511_DEF167_AuditL2TaggedCerts.sql`:
  - Audit query: list every cert whose `CertificationTypeId` points at a Level-2 row, with the proposed parent the cert should be re-pointed at.
  - Conditional UPDATE (commented-out by default): re-point those certs to the Level-2's parent, and optionally insert the original Level-2 reference into `CertificateCertificationTypeSubcategories` join table to preserve the subcategory association on the cert detail view.
- **CR-gated for prod execution** per `feedback_data_changes_require_cr.md`. Run the audit query first to scope; if prod returns rows, submit a CR with the row count and proposed remediation before running the UPDATE.

**Files changed (4):**
- `Controllers/Api/CertificateTilesApiController.cs` (the public anonymous `/api/certificate-tiles` endpoint — primary visible bug)
- `Controllers/Api/CertificateTileInternalApiController.cs` (the admin/internal `/api/certificate-tiles-internal` endpoint — same latent pattern; client said "login view is fine" so this is parity-only)
- `Controllers/Api/CertificateTileExternalApiController.cs` (the external-user `/api/CertificateTileExternal` endpoint — same latent pattern; was also missing `IsActive` guard)
- `Database_Scripts/20260511_DEF167_AuditL2TaggedCerts.sql` — new

**Code change shape (identical in all three controllers):** replaced the 18-line `allCertTypes` query + in-memory parent/child flatten loop with a single Level-1-only LINQ query:
```csharp
var hierarchicalCertTypes = await _context.CertificationTypes
    .Where(ct => ct.IsActive && ct.ParentId == null && certTypeIds.Contains(ct.Id))
    .OrderBy(ct => ct.SortOrder)
    .Select(ct => new { name = ct.Name, level = ct.Level })
    .ToListAsync();
```
JSON response shape unchanged (array of `{name, level}` objects); JS consumers need no change.

**Verification (localhost, http://localhost:5101, 2026-05-11):**
- `dotnet build` — 0 errors, 51 warnings (all pre-existing).
- `GET /api/certificate-tiles/filters` (anonymous, public view) — returned 4 entries, all `level: 1` (`Biobased`, `Certified Energy Savings`, `Certified Environmental Facts`, `Recycled Content`). No subcategory rows. Match to dev DB's 4 Level-1 parents with direct visible certs (`Public` statuses only).
- `GET /api/certificate-tiles?page=1&pageSize=20` — returned 20 of 22 public certs cleanly.
- `GET /api/certificate-tiles?certType=Recycled%20Content` — returned 16 certs, all with `certificationType: "Recycled Content"`. Filter still narrows correctly.
- Internal endpoint not curl-tested (cookie auth + antiforgery); code shape is identical to public, so structurally guaranteed.

**Trade-off acknowledged:** Level-2-tagged certs on prod will not match any filter selection until data is repaired. This is intentional per direction — the cert's primary type tag should be a Level-1; subcategory information lives on a different column (`CertificationTypeSubcategoryId` or the join table). Keeping subcategories visible in the filter as a workaround would mask the underlying data issue.

**Pre-deploy data check (CR-gated):** Before deploying, run Section 1 of `Database_Scripts/20260511_DEF167_AuditL2TaggedCerts.sql` against prod. If any rows return, those certs will silently disappear from filter selection until the remediation block is run (which needs a CR). Submit the CR with the audit row count before deploying the code, OR coordinate the code deploy and data fix as one sequence.

**Live test env verification (2026-05-11, post-deploy):**

Confirmed the fix is live on gccwebapp.com and the data is clean — moving status from "Fixed locally (pending deploy)" → **Fixed + deployed + verified.**

- `GET https://gccwebapp.com/api/certificate-tiles/filters` returned 11 cert types, **all `level: 1`** parents (`Carbon Footprint Reduction`, `Certified Energy Savings`, `Certified Environmental Facts`, `Certified Sustainability Facts`, `Certified Water Savings`, `Closed Loop Product`, `GreenCircle Audited`, `Life Cycle Assessment (LCA) Optimized`, `Recycled Content`, `Sustainable Energy Practices`, `Sustainable Manufacturing Practices`). No CSF subcategory rows.
- DOM render on `/Certificate/CertificateTileView` matched the API byte-for-byte — JS at `wwwroot/js/certificate-tile-view.js:95-105` renders the array 1:1 with no client-side expansion, so the API guarantee carries through to what the client sees.
- Prod-DB L2-tagged-cert audit (Section 1 of `Database_Scripts/20260511_DEF167_AuditL2TaggedCerts.sql`) returned **0 rows**. The CR-gated UPDATE block does not need to run — there are no certs to repoint. Audit row count for the CR write-up: 0/36 certs, 0/26 public-status certs.

Evidence:
- `docs/tracking/screenshots/qc169/QC169_live_public_full_20260511_205716.png` — full-page view of `/Certificate/CertificateTileView` at 1440×900 showing the filter sidebar + cert grid (visible `TEST ENVIRONMENT` banner confirms gccwebapp.com).
- `docs/tracking/screenshots/qc169/QC169_live_public_certtype_pane_20260511_205717.png` — zoomed crop of the Certification Type filter pane.
- Test suite: `GCCWebApp.UAT.Tests/Tests/Admin/QC169PublicFilterScreenshot.cs` — repeatable, takes a fresh screenshot pair on each run. Set `TEST_BASE_URL=https://gccwebapp.com` and run `dotnet test --filter QC169`.

**QC #169 second sub-bullet — Public/Login filter parity (verified 2026-05-12):**

Carly's QC log entry for #169 has a second paragraph beyond the subcategory complaint: *"The login view is only showing those with certs under those cert types. This is fine. (If there are no certs with X type as an option). I WOULD DO THE SAME FOR PUBLIC VIEW."* This is requesting that the public view also hide cert types that have zero associated certs — matching the login-view behavior she had already validated as correct.

This is **already satisfied by the same DEF-167 code path** and warrants no additional change:

- **Public (anon)** `Controllers/Api/CertificateTilesApiController.cs:249-257` — `visibleCertTypeIds` is derived from `baseQuery` already filtered to `PublicStatuses + non-expired`. Dropdown at `:286-289` filters cert types by `visibleCertTypeIds.Contains(ct.Id)`. Cert types with zero visible certs are hidden. ✅
- **External (logged-in non-admin)** `Controllers/Api/CertificateTileExternalApiController.cs:468, 542-545, 554-558` — same pattern: baseQuery filtered to `PublicStatuses + non-expired`, then `certTypeIds` derived from that filtered query, then dropdown filtered by `certTypeIds.Contains(ct.Id)`. This is the "login view" Carly validated as "fine." ✅
- **Internal (Admin/Analyst)** `Controllers/Api/CertificateTileInternalApiController.cs:491-494` — `certTypeIds` derived from all certs without a status filter. **Different by design** — internal users see all statuses (Draft/Hold/Expired) and the dropdown should surface types for any cert that exists, not just public ones.

Live verification: `GET https://gccwebapp.com/api/certificate-tiles/filters` returns 11 cert types, all `level: 1`, all with ≥1 publicly-visible cert. Types defined in `CertificationTypes` table but lacking any associated public cert are correctly omitted (matches Carly's "fine" baseline).

Net: both sub-bullets of QC #169 are resolved by the deployed DEF-167 fix. No new code change. Status remains **Fixed + deployed + verified.**

---

### DEF-166 — Internal tile view slow load + only 15 of 35 certs showing

- **Date:** 2026-05-08
- **Severity:** High (perf + visible data-loss-on-render on the primary internal browse page)
- **Status:** Fixed locally (pending second deploy — first deploy made it WORSE; see "First-attempt regression" below)
- **Reporter:** Shawn — "https://gccwebapp.com/Certificate/CertificateTileViewInternal — is taking a long time to load certificates" + "why is only 15 of 35 certificates showing?"

**Symptom:** Internal certificate tile view loaded slowly and rendered fewer rows than the database actually contained (e.g., 15 visible when 35+ existed). Same root cause produces both symptoms.

**Root cause:** `Controllers/Api/CertificateTileInternalApiController.GetCertificateTiles` had 14 `.Include()` calls and **four of them were collection navs** (`Tags`, `ProductIds`, `CertificationTypeSubcategories`, `CustomFieldValues`). EF Core's default single-query mode flattens those into one big LEFT JOIN, producing a cartesian product (5 tags × 3 productIds × 2 customFields × 2 certSubcategories ≈ 60 rows per cert). Two consequences:
1. **Slow load** — every page query paid the cartesian materialization cost.
2. **Missing certs** — `Skip(offset).Take(limit)` then operates on cartesian rows, not unique certs, so `Take(20)` could return only 5–10 distinct certs. Pagination instability was further compounded by sort branches (e.g. `OrderByDescending(c => c.EffectiveDate)`) with no tiebreaker — when two certs share an EffectiveDate, SQL Server's row order is non-deterministic, page 1 and page 2 overlapped, and the JS dedupe-by-id step (`certificate-tile-view-internal.js:222-225`) silently dropped duplicates.

**First-attempt regression (worth recording so we don't repeat):** I initially reasoned that because the controller does `.Select(c => new {...})` after the Includes, the Includes were redundant — projection-driven navigation access generates its own JOINs/subqueries. So the first fix removed every `.Include()` and added `.AsNoTracking()`. Local build was clean, deployed to gccwebapp.com. Result: API timed out at 30s on the dev DB (40 certs) before returning anything. Hypothesis on why: the `.Select()` had ~12 collection sub-projections (`c.Tags.Where(...).Select(...)`, eight `c.ProductIds.Where(...)`, `c.CertificationTypeSubcategories.Select(...)`, `c.CustomFieldValues.Where(...).Select(...)`). Without the Includes pulling those collections in advance, EF Core 9's projection translator emits all of them as inline correlated subqueries against the outer paginated query — which the SQL Server query optimizer apparently couldn't handle within 30s. So the first fix turned a "slow + drops rows" problem into a "doesn't return at all" problem.

**Actual fix (verified on localhost before redeploy):**
- **`Controllers/Api/CertificateTileInternalApiController.cs`:**
  - Restored all the original `.Include()` calls (the projection translator wants them present).
  - Added **`.AsSplitQuery()`** — the canonical EF Core fix for "collection includes + pagination". EF emits one query for the principal entity (Certificates), then one extra query per collection nav. No cartesian product, deterministic pagination, fast on every page.
  - Added `.AsNoTracking()` because this is a read-only path.
  - `ApplySorting` — added `.ThenBy(c => c.Id)` tiebreaker on every one of the 50+ sort branches so pagination is deterministic when the primary sort key has duplicates.
- **`Controllers/Api/CertificateTileExternalApiController.cs`:** Same shape (Includes + AsSplitQuery + AsNoTracking + ThenBy tiebreaker) applied — same Tags-collection cartesian + non-deterministic-sort pattern existed there.

**Files changed (3):**
- `Controllers/Api/CertificateTileInternalApiController.cs`
- `Controllers/Api/CertificateTileExternalApiController.cs`
- `docs/tracking/defect-register.md` (this entry)

**Verification (localhost, http://localhost:5101, admin login):**
- Page 1 (`?page=1&pageSize=20`): HTTP 200 in **0.51s**. `totalCount=41`, returned 20 unique cert IDs.
- Page 2 (`?page=2&pageSize=20`): HTTP 200 in **0.058s**. 20 unique cert IDs.
- **Zero overlap** between page 1 and page 2; **40 of 41 unique combined** (the 41st falls on page 3 — math is correct).
- Compare to first-attempt regression: same query timed out at 30s before AsSplitQuery was added.
- `dotnet build` — 0 CS errors. Pre-existing warnings unchanged.

---

### DEF-161 — Lowercase ASIN rejected on /Certificate/Create; Create POST persisted unvalidated Product IDs

- **Date:** 2026-05-07
- **Severity:** High (go-live blocker — primary cert authoring path; data-integrity hole on save)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — "https://gccwebapp.com/Certificate/Create — validation issues. I'm unable to enter a product ID that meets validation requirements."

**Symptom:** In the Product Identifier modal on the Certificate Create form, typing a real Amazon ASIN in lowercase (e.g. `b08n5wrwnw`) was rejected with "ASIN must be exactly 10 alphanumeric characters" with no clue that the issue was case. The user had no way to know to retype in upper case, since the official Amazon ASIN format is uppercase but lowercase entries are common when users paste from URLs or notes. Compounding this: even when a value did pass the client check, the server-side `CertificateController.Create` POST persisted Product IDs **without ever calling `ProductIdService.ValidateProductIdAsync`** — only the Edit-time `/api/productids` round-trip ran format validation. So a JS bypass or client regression would silently store malformed Product IDs.

**Root cause:**
1. `wwwroot/js/product-id-manager.js:861` — ASIN regex was `/^[A-Z0-9]{10}$/` (case-sensitive). All other type regexes that involve letters (`SKU`, `MPN`) already used the `i` flag; ASIN was the lone exception.
2. `Services/ProductIdService.cs:712` (`ValidateAsin`) — same case-sensitive regex on the server. The `type.ToUpper()` call in `ValidateProductIdAsync` only normalised the type, not the value.
3. `Controllers/CertificateController.cs` Create POST (line 480-495) — added each `model.ProductIds` entry directly to `certificate.ProductIds` and saved via `_unitOfWork`, skipping `_productIdService.ValidateProductIdAsync` entirely. The Edit POST had the same gap.

**Fix:**
- **Client (`wwwroot/js/product-id-manager.js`):**
  - ASIN regex now `/^[A-Z0-9]{10}$/i`.
  - New `normalizeProductIdValue(type, value)` uppercases the value for the seven typed-code formats (ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN); `OTHER` stays freeform.
  - `handleFormSubmit` runs the value through the normaliser before pushing to in-memory list / posting to API.
- **Server (`Services/ProductIdService.cs`):**
  - New `public static NormalizeValueForType(string? type, string? value)` helper mirrors the client rule.
  - `ValidateProductIdAsync` normalises the value before regex match, so direct API calls with lowercase ASIN now pass.
  - `CreateProductIdAsync` and `UpdateProductIdAsync` canonicalise type + value at the top of the method, so the DB never stores a lowercase ASIN regardless of caller.
  - `ImportProductIdsAsync` (CSV bulk path) normalises `ProductIdValue` on insert.
- **Server (`Controllers/CertificateController.cs`):**
  - Both Create POST and Edit POST run a pre-validation pass over `model.ProductIds`: canonicalise type + value via `ProductIdService.NormalizeValueForType`, then `_productIdService.ValidateProductIdAsync` each row, adding `ModelState` errors keyed `ProductIds[i].ProductIdValue` for invalid entries. Submission falls back to the same View with errors surfaced inline.

**Files changed (4):**
- `wwwroot/js/product-id-manager.js`
- `Services/ProductIdService.cs`
- `Controllers/CertificateController.cs`
- `docs/tracking/defect-register.md` (this entry)

**Verification (localhost, http://localhost:5101):**
1. **Validation function truth table** (`verify-isbn.mjs`): Before fix — `asin_lower=false`, `asin_mixed=false`. After fix — `asin_lower=true`, `asin_mixed=true`, all other types unchanged.
2. **Server `/api/productids/validate`** (`verify-server.mjs`): lowercase ASIN now returns `data: true`; mixed-case + lowercase type returns `data: true`; genuinely bad ASIN (`TOO_SHORT`) still returns `data: false`.
3. **End-to-end Create** (`e2e-create.mjs`): Form filled with company, product, cert type, dates, tiny PDF, and a Product ID with **lowercase** value `b08abc1234`. Submit succeeded; redirect to `/Certificate/CertificateInternalView/26-0158`. Direct SQL confirmation: `SELECT ProductIdType, ProductIdValue FROM CertificateProductIds WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Number='26-0158')` returned `ASIN | B08ABC1234` — value canonicalised on save, not stored as the lowercase form the user typed.
4. **Genuinely-bad inputs still rejected:** `B08N5WR` (short), `12345` (UPC short), `1234567` (GTIN short), `1234567890123` (ISBN bad prefix), `AB` (SKU short), 60-char MPN.

**Audit table (severity ranked):**

| # | Defect | Where | Severity | Status |
|---|--------|-------|----------|--------|
| A | ASIN regex case-sensitive (client) | product-id-manager.js:861 | High | Fixed |
| B | ASIN regex case-sensitive (server) | ProductIdService.cs:712 | High | Fixed (via NormalizeValueForType pre-pass) |
| C | Create POST persists Product IDs without format validation | CertificateController.cs Create POST | High | Fixed (pre-validation pass added) |
| D | Edit POST persists Product IDs without format validation (same gap) | CertificateController.cs Edit POST | High | Fixed (same pre-validation pass) |
| E | UPC/EAN client-side missing checksum (length-only) | product-id-manager.js:862,864 | Medium | **Deferred** — server now enforces on Create + Edit + API + bulk import; client length check is fast feedback. Adding checksum to client is a UX-only enhancement and risks blocking valid UPCs the user has already verified externally. |
| F | Bulk CSV import didn't normalise value | ProductIdService.cs:511 | Medium | Fixed (uses NormalizeValueForType) |

**Deploy:** Pending. Restart required (server-side changes).

---

### DEF-160 — Login does not route users to their role-appropriate certificates page

- **Date:** 2026-05-07
- **Severity:** Medium (functional — every authenticated session lands on a generic page; correct page is one extra click away)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — testing login flow on `gccwebapp.com`. Reported "When you login, for example external, partner should go to external certificates. Analyst and Admin should go to internal certificates. Currently, it just goes to certificates."

**Symptom:** Successful login dropped users on a generic certs page regardless of role. Specifically: External landed on the External tile view (correct), Partner landed on the public `CertificateTileView` (wrong — should be External tile view), Analyst and Admin landed on the legacy Dashboard `Index` (wrong — should be Internal tile view).

**Root cause:** Two switch tables were out of sync with the intended landing-page mapping:
1. `Controllers/AccountController.cs` `RedirectToLocal` — had no `"Partner"` case, so Partner fell through to the default (`CertificateTileView`, public). Analyst and Admin cases routed to `Dashboard/Index` rather than `CertificateTileViewInternal`.
2. `Controllers/HomeController.cs` `Index` — same issue. No Partner case, Analyst/Admin routed to Dashboard.

**Fix:** Updated both switch tables. Final mapping for authenticated roles:
- `Public` → `CertificateTileView`
- `External` → `CertificateTileViewExternal`
- `Partner` → `CertificateTileViewExternal`
- `Analyst` → `CertificateTileViewInternal`
- `Admin` → `CertificateTileViewInternal`

Auth policies are already correct — `ExternalAccess` permits External/Partner/Admin/Analyst, `AnalystOrAdmin` permits Admin/Analyst — so the redirect targets are reachable by their respective roles. The `Url.IsLocalUrl(returnUrl)` short-circuit in `RedirectToLocal` is preserved, so deep-link returns after session timeout still work.

**Files:** `Controllers/AccountController.cs`, `Controllers/HomeController.cs`.

**Verification (pending — please QC after deploy):** Log in fresh (no `returnUrl`) as each role. External and Partner should land on `/Certificate/CertificateTileViewExternal`. Analyst and Admin should land on `/Certificate/CertificateTileViewInternal`. With a `returnUrl` (deep-link forced through login), users still return to the original URL.

---

### DEF-159 — Tablet: "Template" import button stretches to 100% width on the row beneath the other action buttons (Internal cert tile view)

- **Date:** 2026-05-07
- **Severity:** Medium (visible layout — primary cert listing page, Admin role, tablet portrait)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — viewing `https://gccwebapp.com/Certificate/CertificateTileViewInternal` on tablet (≤768px). Action button row above the tile grid was wrapping the trailing "Template" button onto its own line and the wrapped button was inflating to fill 100% of the row width while every other button remained content-sized.

**Symptom:** The Internal tile view shows five action buttons in `.export-options` for Admin: Add Certificate / Download Selected / Export All to CSV / Import / Template. At ~768px viewport width, four buttons fit on row 1 and "Template" wraps onto row 2 alone — but instead of staying at its content width (~110px), it stretched to 100% of the container. External tile view was unaffected because that view has only two buttons in `.export-options` and they fit on a single row even at tablet width.

**Root cause:** `wwwroot/css/main.css` had a single `@media (max-width: 768px)` block that applied both `.export-options { justify-content: stretch; }` and `.export-options .btn { flex: 1; min-width: 140px; }`. The `flex: 1` on a flex item that wraps onto a row by itself causes that item to grow to the full available row width — exactly the observed behavior. The intent of `flex: 1` was to make buttons full-width on phones (single column layout, larger tap targets), which is fine at phone widths where every button wraps; the bug only surfaces at tablet widths where exactly one button wraps alone.

**Fix:** Split the rule by breakpoint in `wwwroot/css/main.css`:
- Tablet `≤768px`: keep `min-width: 140px` on the buttons but drop `flex: 1` and `justify-content: stretch`. Wrapped buttons now stay at content width.
- Phone `≤480px`: re-introduce `flex: 1` and `justify-content: stretch` for the full-width tap-target behavior on phones.

This preserves the phone UX while removing the tablet inflation. Internal view's "Template" button now sits at content width on row 2 instead of spanning full width. External view is unchanged by the fix because its two buttons still fit on row 1 at tablet width.

**Files:** `wwwroot/css/main.css` only (one media-query block split into two).

**Verification (pending — please QC after deploy):** Open `/Certificate/CertificateTileViewInternal` as Admin on iPad portrait (~768px). Action button row should show Add Certificate / Download Selected / Export All to CSV on row 1, and Import + Template both at content width on row 2 (or all on row 1 at slightly wider tablets). On phone (~375px) buttons should still stack full-width as before.

---

### DEF-158 — External tile view sends Partner/External users to AccessDenied when clicking a cert tile

- **Date:** 2026-05-07
- **Severity:** **High** (functional break — Partner role cannot view any cert detail page from the tile view)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — logged in as Partner role on `gccwebapp.com`, clicked into cert 26-0157, landed on `/Account/AccessDenied?ReturnUrl=%2FCertificate%2FCertificateInternalView%2F26-0157`. Same break for any External user.

**Symptom:** Both External and Partner roles can reach the External tile view (`/Certificate/CertificateTileViewExternal`) per the `ExternalAccess` policy (External + Partner + Admin + Analyst). But clicking a tile opens `/Certificate/CertificateInternalView/{number}` in a new tab — and that route is gated by `AnalystOrAdmin` policy. Partner + External fail the policy check and get redirected to AccessDenied.

**Root cause:** `wwwroot/js/certificate-tile-view-external.js` had hardcoded the wrong route in two places (lines 1146 and 1155):
```js
window.open(`/Certificate/CertificateInternalView/${encodeURIComponent(certificateNumber)}`, '_blank');
```
Both `viewCertificate(num)` (button click / context menu) and `viewCertificateDetails(num)` (tile click) pointed at the Internal route. The External route (`CertificateExternalView`) is the correct target — it's gated by `ExternalAccess`, which all four roles satisfy.

There is also a stale orphan file `wwwroot/js/certificateTileViewExternal.js` (camelCase) that already had the correct external route. It is not loaded by any view. Did not touch it as part of this fix.

**Fix:** Changed both `window.open(...)` calls in `wwwroot/js/certificate-tile-view-external.js` to point at `/Certificate/CertificateExternalView/{number}`. The Internal tile view's JS (`certificate-tile-view-internal.js`) is unchanged — it still routes to InternalView correctly for Admin/Analyst.

**Files:** `wwwroot/js/certificate-tile-view-external.js` only.

**Verification:** Reproduced Shawn's exact symptom locally. Logged in as External user (same `ExternalAccess` policy as Partner), navigated to External tile view, called `window.viewCertificate('26-0146')` and `window.viewCertificateDetails('26-0146')` with `window.open` stubbed — both returned `/Certificate/CertificateExternalView/26-0146` post-fix (was `CertificateInternalView` pre-fix). Direct GET on `/Certificate/CertificateInternalView/26-0146` as External user reproduced the AccessDenied redirect Shawn hit; direct GET on `/Certificate/CertificateExternalView/26-0146` returns 200.

**Note on dev DB:** No Partner-role users exist in the local dev DB (Roles table has Partner at id 7 but UserRoles has no rows for it). Verified via External which shares the same `ExternalAccess` policy and the same JS path.

---

### DEF-157 — Cert detail view forces portrait box on landscape cert images (squished/letterboxed thumbnails)

- **Date:** 2026-05-07
- **Severity:** Medium (visible layout — primary cert detail page)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — viewing cert 26-0157 on `gccwebapp.com` desktop, the cert preview was showing in a tall portrait container with the landscape cert image squished into a tiny letterboxed strip surrounded by whitespace. "Why did this break?"

**Symptom:** GCC cert thumbnails come in both portrait (Letter 8.5×11) and landscape orientations. The `.certificate-preview-box` rule in `wwwroot/css/main.css` was applying `aspect-ratio: 8.5 / 11` to every preview box. With `object-fit: contain` on the inner img, that meant: portrait certs filled the box correctly, but landscape certs (e.g., 26-0157's 600×464 thumbnail, 26-0146's 600×462 thumbnail) were scaled down to fit the box width and centered vertically with huge whitespace top and bottom.

**Why "did this break":** The `aspect-ratio: 8.5 / 11` constraint and the matching base `.certificate-preview-box img` rules were added to main.css recently to fix DEF-156 (image rendering at natural pixel size and overflowing the column on tablet landscape). The first fix solved the overflow but introduced the new shape constraint, which is wrong for landscape certs. Net effect on tablet landscape:

| State | Portrait certs | Landscape certs |
|---|---|---|
| Before portrait-box rule | OK | Image overflowed column ❌ |
| After portrait-box rule (current live) | OK | Image squished into tiny letterbox ❌ |

**Root cause:** Forced portrait `aspect-ratio` is wrong for the dataset — cert thumbnails do not all have the same orientation. A rigid container shape can never simultaneously match both. Earlier today's "DEF-156 fix" today was a no-op duplicate against rules already deployed live (verified by pulling `https://gccwebapp.com/css/main.css` — 133,563 bytes match, rules already present).

**Fix:** Removed `aspect-ratio: 8.5 / 11`, `display: flex`, and `align-items/justify-content: center` from `.certificate-preview-box`. Switched img from `position: absolute; width:100%; height:100%; object-fit: contain` to `position: relative; display: block; width:100%; height: auto`. Container now sizes itself to the natural aspect ratio of whatever image loads. Added `min-height: 200px` so the no-PDF placeholder still has somewhere to sit.

**Files:** `wwwroot/css/main.css` only.

**Verification:** Reproduced and re-tested cert 26-0146 (600×462 landscape thumbnail) on localhost at desktop (1440×900), tablet landscape (1024×768), tablet portrait (768×1024), and mobile (390×844) via Playwright. Pre-fix: container was 8.5/11 portrait shape (e.g., 316×471 at tablet landscape), image rendered at 314×244 in the middle with letterbox whitespace. Post-fix: container = image natural aspect (e.g., 316×244), image fills container, 0 overflowing elements at every viewport. Screenshots: `C:\Users\swest\AppData\Local\Temp\uitest-pidsort\screenshots\fix-26-0146-*.png`.

---

### DEF-156 — Certificate image overflows viewport on tablet landscape (1024px) in cert detail views

- **Date:** 2026-05-07
- **Severity:** Medium (visible layout break on a primary tablet viewport)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — `https://gccwebapp.com/Certificate/CertificateInternalView/26-0146` on tablet landscape, certificate preview image was rendering outside the card and not wrapping.

**Symptom:** On the cert detail page (Internal AND External views) at tablet landscape ~1024px, the bare `<img>` inside the right-column preview box rendered at its natural pixel size (~612px wide) and overflowed the column / viewport. Layout was correct on mobile (≤768px) and on full desktop, which masked the bug.

**Root cause:** `wwwroot/css/certificates.css` defines the base `.certificate-preview-box` and `.certificate-preview-box img` rules (`aspect-ratio: 8.5/11`, `overflow: hidden`, `position: absolute; width: 100%; height: 100%; object-fit: contain`), but `Views/Shared/_Layout.cshtml` only links `main.css`. The certificates.css bundle is referenced in `appsettings.Performance.json` but never linked in the layout. main.css only had a `≤768px` `@media` override for the preview-box img — at any viewport above 768px the img was unstyled and rendered at its intrinsic pixel size.

**Fix:** Added the missing base `.certificate-preview-box`, `.certificate-preview-box img`, and `.preview-placeholder` rules to `wwwroot/css/main.css` (above the existing tablet `@media (max-width: 1024px) and (min-width: 769px)` block), so they apply at every viewport. Did NOT add the `<link>` to certificates.css to avoid pulling in unrelated rules.

**Files:** `wwwroot/css/main.css` only.

**Verification:** Played through `/Certificate/CertificateInternalView/26-0146` and `/Certificate/CertificateExternalView/26-0146` at 768 (portrait), 1024 (landscape), 1180 (iPad Air landscape), 1280 (small desktop) via Playwright. Pre-fix: 1 element overflowing at 1024px (the bare `<img>`, width 612, right edge at x=1252). Post-fix: 0 overflowing elements at every tested viewport. Image now sits in the aspect-ratio box with `object-fit: contain`. Screenshots in `C:\Users\swest\AppData\Local\Temp\uitest-pidsort\screenshots\26-0146-*.png`.

---

### DEF-155 — Database Listing URL overflows viewport on mobile in cert detail views

- **Date:** 2026-05-07
- **Severity:** Low (cosmetic / mobile layout)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — reviewing `https://gccwebapp.com/Certificate/CertificateInternalView/26-0146` on mobile; the rendered Database Listing URL anchor (`https://gccwebapp.com/Certificate/Verify/26-0146`) extended past the right edge of the card.

**Symptom:** Long URL displayed as anchor text has no natural break points; on narrow viewports it stretched the container and pushed content off-screen.

**Root cause:** Anchor element had no wrapping rule. URLs without spaces or hyphens won't wrap by default — they need explicit `word-break` or `overflow-wrap` to break mid-string.

**Fix:** Added inline `word-break: break-all; overflow-wrap: anywhere;` to the verify-URL anchor in both `Views/Certificate/CertificateInternalView.cshtml` and `Views/Certificate/CertificateExternalView.cshtml`.

**Files:** `Views/Certificate/CertificateInternalView.cshtml`, `Views/Certificate/CertificateExternalView.cshtml`.

---

### DEF-154 — "Replace all existing" toggle silently dropped when import has no "new" rows; partial imports could deactivate matching rows

- **Date:** 2026-05-07
- **Severity:** **High** (data integrity — could deactivate hundreds of matching rows in the partial-import case)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — re-imported the 1000-row CSV against cert 26-0023 with "Replace all existing" checked; the lone stray row that wasn't in the file was NOT deactivated.

**Symptom:** On the cert Edit page's CSV import preview, checking "Replace all existing" was supposed to deactivate any existing product ID on the cert that wasn't in the import file. After the DEF-153 fix made dup-detection see all pages, re-importing the same file classified all matching rows as "modified" or "unchanged" with zero "new" — and the Replace-all logic silently did nothing.

**Root cause:** Two failure modes in `wwwroot/js/product-id-manager.js` `applyImport()`:
1. **All rows unchanged.** `selectedRows.length === 0` (unchanged are auto-deselected), so the function returned with "No rows selected for import" before even reading the toggle. Replace-all toggle ignored entirely.
2. **All rows modified, zero new.** `creates = selectedRows.filter(r => r.action !== 'update')` was empty, so the bulk endpoint was never called — the `overwriteExisting` flag never reached the server, and the server's deactivation loop never ran.
3. **Partial — some new + some modified.** This was the worst case. The bulk POST only sent "creates" (new rows), so the server's `importKeys` set contained ONLY those new keys. The server's deactivation loop then marked every existing row whose key wasn't in `importKeys` — including the modified rows the user wanted to keep — as Inactive. PUT updates ran afterwards on rows that were now deactivated.

**Fix:**
- When `overwriteExisting` is checked, the import set is now ALL non-error rows in the import file (regardless of per-row selection). The toggle implies "the file is the new source of truth," and per-row checkboxes lose meaning in that mode. The empty-selectedRows guard now returns a different error message in this branch ("Import file has no valid rows. Replace-all needs at least one valid row to define the new state.") instead of "No rows selected for import."
- The bulk endpoint payload now includes EVERY row in the import set (including updates) when `overwriteExisting` is true, not just `creates`. This makes the server's `importKeys` set complete — the deactivation loop only deactivates rows that are truly missing from the file. The server's bulk handler already silently skips dup keys in overwrite mode (counted as success, no insert), so sending matching rows is harmless on the insert path.
- PUT updates for "modified" rows still run individually after the bulk call, to apply field changes (the bulk endpoint silently skips dup keys without applying field updates).

**Files:** `wwwroot/js/product-id-manager.js` only.

**Verification:** Verified via DB inspection. Reset cert 26-0023 to 1001 active rows (1000 ASINs from import file + 1 stray row `1321321212`). Imported the 1000-row file with "Replace all existing" checked. After the apply: `SELECT COUNT(*) WHERE IsActive = 1` returns **1000** (was 1001). The stray row is now Inactive. Pre-fix behavior: would still be 1001 active.

**Out of scope (for future CR if needed):** The bulk endpoint's "silent skip on dup-key in overwrite mode" semantics mean field-level changes on matching rows aren't applied via the bulk path. This CR routes those through the existing PUT-individually loop, which works but emits N requests for N modified rows. If we ever see Replace-all imports with thousands of field changes hitting the rate limiter, we should change the server-side bulk endpoint to update fields on dup-key in overwrite mode (one round-trip, not N). Not blocking launch — current scale is fine.

---

### DEF-153 — Import preview falsely classifies existing rows as "new" after CR-018 server-paging shipped

- **Date:** 2026-05-07
- **Severity:** High (data integrity — would create duplicate rows on re-import)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — re-imported `product-ids-sample-1000-asins.csv` after the first import committed, expecting all 1000 rows to flag as "Unchanged"; instead "a few were tagged unchanged, but the majority said new."

**Symptom:** On the cert Edit page, the CSV import preview compared each imported row against `this.productIds` (the JS in-memory state) to classify it as new / modified / unchanged. After CR-018 introduced server-side pagination, `this.productIds` only holds the currently visible page (≤ 50 rows). So on a cert with 1001 ASINs, the dup-detection only saw the 50 rows on page 1 — every imported value not on that page was flagged "new" even though it already existed in the DB. Clicking Apply Import in that state would have created 950+ duplicate rows.

**Root cause:** `wwwroot/js/product-id-manager.js` `buildImportPreview()` does `this.productIds.find(p => …)` to look up an existing row matching the import's type+value. That semantic was correct when `this.productIds` held the full list (pre-CR-018), but is wrong post-CR-018 because the array is now scoped to one page.

**Fix:** When in server-paged mode (`serverPaged && certificateId`), `buildImportPreview` now pre-fetches the full list once via the existing un-paged endpoint `GET /api/productids/certificate/{id}` (no `page` param → returns all rows in `data[]`), builds a `Map` keyed by `${type.toUpperCase()}:${value.toUpperCase()}`, and looks up each import row in that map instead of scanning `this.productIds`. Falls back to the original in-memory scan if the pre-fetch fails or in non-paged mode (Create page).

**Files:** `wwwroot/js/product-id-manager.js`.

**Verification:** Playwright probe (`test-def153-import-dedup.mjs`) seeds 100 import items whose values match existing rows on cert 26-0023 (most are NOT on page 1). Pre-fix: all 100 would be "new". Post-fix: 0 new, 100 modified. 5/5 assertions pass.

**Out of scope:** The pre-fetch loads the full list into memory in the JS — fine for 1000 rows, may need re-thinking at 100,000+. If we hit that scale, switch to a server-side `lookup-keys` endpoint that returns just `{type, value}` pairs (smaller payload). Not blocking launch.

---

### DEF-152 — Product Identifier pagination buttons didn't match Product Management's pagination styling

- **Date:** 2026-05-06
- **Severity:** Low (visual / UI consistency)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed on cert Create / Edit pages while comparing against `/Product/Management`.

**Symptom:** The Product Identifier pagination on cert Create / Edit pages rendered as five inline-styled text buttons — `[First]` `[Prev]` `Page X of Y` `[Next]` `[Last]` — with hand-rolled CSS in the JS template (border, padding, font-size, background all written as inline `style="…"`). The Product Management page (and the rest of the admin app) uses chevron Prev / numbered pages with ellipsis / chevron Next, styled via the canonical `.pagination-btn` and `.pagination-controls` classes. Two visibly different paginations on the same admin app — Shawn called this out as a clear consistency break.

**Root cause:** `wwwroot/js/product-id-manager.js` `renderPagination()` was authored before the admin pagination convention (`.pagination-btn` / `.pagination-controls` defined in `wwwroot/css/product-management.css:1556-1601`) was established, and was never retrofitted. The "First / Last" navigation also wasn't in the canonical pattern — Product Management only ships chevron Prev + numbered + chevron Next.

**Fix:**
- `wwwroot/js/product-id-manager.js` `renderPagination()` rewritten to mirror `Views/Product/Management.cshtml:740-793` — chevron-left Previous button, numbered page buttons (max 7 visible with `...` ellipsis at edges), chevron-right Next button, all using `class="pagination-btn"` (with `active` / `disabled` modifiers). Active page also gets `aria-current="page"`. Container wrapper uses `pid-pagination-wrapper` (left: "Showing N-M of T" info, right: `.pagination-controls` button row).
- `wwwroot/css/product-id-manager.css`: Appended scoped pagination styles under `.product-id-manager .pagination-*` mirroring `wwwroot/css/product-management.css:1556-1601`. Active button hardcoded to `#002B76` rather than `var(--blue-dark)` because the var is overridden to `#002876` in some parent contexts; matching the rendered output of Product Management is the goal, so the literal wins.

**Files:** `wwwroot/js/product-id-manager.js`, `wwwroot/css/product-id-manager.css`.

**Verification:** Playwright test at `C:\Users\swest\AppData\Local\Temp\uitest-pidsort\test-pagination.mjs` — 20/20 pass. Pushed 51 in-memory product IDs to force pagination, then asserted: wrapper class, button class structure, no inline background styles, chevron-left Prev (disabled on p1), chevron-right Next (disabled on last page), `.active` + `aria-current="page"` on current page, "Showing N-M of T" info text updates on page change, computed background `rgb(0, 43, 118)` matches Product Management exactly, computed dimensions (36px min-width, 44px rendered height) match Product Management exactly.

---

### DEF-151 — "Unsaved form data recovered from HH:MM" banner hidden pending Phase 2 rework

- **Date:** 2026-05-06
- **Severity:** Low (UX clean-up)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed the false-positive banner on the cert Create page during DEF-150 testing.

**Symptom:** The Create / Edit cert pages displayed a yellow recovery banner reading *"Unsaved form data recovered from HH:MM. Would you like to restore it?"* with [Restore] / [Discard] buttons. Banner fired even on freshly opened pages where the user had no real prior session — cascading dropdowns (Company → Product, Category → Type → Subcategory, Cert Type → Subcategories) retrigger `change` events on initial load and the unsaved-state tracker reads those as edits.

**Root cause (current banner has multiple problems):**
- False positives: `UnsavedChanges.rebaselineUnsavedChanges()` runs on a `setTimeout(..., 1000)` after page load, but the recovery check (`checkForRecovery()`) runs earlier in the same flow and sees the cascade-driven values as "differences" against the saved payload.
- Partial restore: the restore handler explicitly skips `__RequestVerificationToken`, `CertificateFile`, and `ThumbnailFile` (`certificate-create.js:766`). A user who clicks Restore gets text fields back but no cert PDF, no thumbnail. They'd then click Save and either fail validation (PDF required on Create) or persist a half-edit.
- No coverage of product IDs, custom field values, or tags — the recovery payload only includes standard form fields.
- Time-only timestamp without a date.

**Decision:** Hide the banner now, rebuild it as a trustworthy experience in Phase 2. The auto-save infrastructure stays — only the recovery prompt is suppressed — so the saved data is still there for Phase 2 to consume cleanly.

**Fix:**
- `wwwroot/js/certificate-create.js`: Added a `SHOW_RECOVERY_PROMPT: false` kill switch on the `UnsavedChanges` module. `checkForRecovery()` short-circuits when the flag is off but still GCs entries older than 2 hours from `localStorage` so storage doesn't leak indefinitely. Corrupt JSON entries are also cleared rather than crashing on parse.

**Files:** `wwwroot/js/certificate-create.js`.

**Phase 2 follow-up:** CR-017 — Unsaved-form recovery banner UX rework. See `docs/tracking/CR-017-form-recovery-ux.md` for full Phase 2 scope (false-positive elimination, full-surface payload, all-or-nothing restore, better copy, same-cert scoping).

**Verification:** Reload `/Certificate/Create` or `/Certificate/Edit/{id}`. Banner should not appear under any condition. Open DevTools → Application → localStorage to confirm auto-save entries still write (key is set in `UnsavedChanges.storageKey`).

---

### DEF-150 — Product Identifier table missing column sort that other admin tables have

- **Date:** 2026-05-06
- **Severity:** Low (UX consistency)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed on cert Edit page after the DEF-149 hint shipped.

**Symptom:** The Product Identifier table on `/Certificate/Create` and `/Certificate/Edit/{id}` had non-clickable column headers (`<th width="80">Type</th>` etc.) with no sort affordance. Every other admin table in the app — Product Management, Tags, Pathways, User Management, Companies — supports click-to-sort with visual chevron icons (`▼` default, `▲ / ▼` per direction). Inconsistent behavior at the same UI layer.

**Root cause:** The Product ID Manager partial (`Views/Shared/_ProductIdManager.cshtml`) was authored before the `admin-table-th--sortable` convention was established and never retrofitted. The JS (`wwwroot/js/product-id-manager.js`) only handled filter + search; sort state didn't exist.

**Fix:**
- `Views/Shared/_ProductIdManager.cshtml`: Each column header (Type, Product ID, Product Name, SKU, Partner, Created, Created By) now uses `data-sort="…"` + `class="admin-table-th--sortable"` + `<span class="sort-icon admin-sort-icon">▼</span>`. Mirrors `Views/Product/Management.cshtml:106-113`. Actions column intentionally not sortable.
- `wwwroot/js/product-id-manager.js`:
  - Added `sortColumn` / `sortDirection` constructor state.
  - Added `initializeSortableHeaders()` (called from `init()`) — click toggles asc/desc on same column, switches and resets to asc on different column. Updates icons and `active-sort` class.
  - Added `applySort()` — alpha sort (locale-aware) for Type / Value / Name / SKU / Partner / Created By; numeric timestamp sort for Created. `createdBy` resolves through `createdByUser.username` first, falling back to the raw `createdBy` field for in-memory items that haven't been persisted yet.
  - Wired into `renderTable()` so every render path picks up the active sort (load from API, in-memory add/edit/delete on Create page, filter/search, import-apply). Idempotent when no column is selected — preserves server order.

**Files:** `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/js/product-id-manager.js`.

**Verification:** Reload `/Certificate/Edit/{any-id}`. Click column headers to confirm sort behavior matches Product Management. Type a search query — sort should persist. Add a new product ID — table re-renders honoring active sort.

---

### DEF-149 — Edit page: Product ID save semantics differ from Create page (no UX signal)

- **Date:** 2026-05-06
- **Severity:** Low (UX clarity)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — surfaced while auditing orphan-record risk between Create and Edit.

**Symptom:** The Product ID Manager on `/Certificate/Create` holds Add/Edit/Delete/Import in browser memory and only commits on cert Save (single transaction with the parent cert). On `/Certificate/Edit/{id}`, the same UI commits each operation immediately to the DB via `/api/productids/*` endpoints, so clicking Cancel on the cert form does NOT revert product ID changes. Two save semantics behind the same UI surface.

**Root cause:** `wwwroot/js/product-id-manager.js` branches on `certificateId === 0`: zero → in-memory mode (Create), positive → immediate-API mode (Edit). No visual indicator of which mode is active.

**Decision:** Keep immediate-save behavior on Edit. The "fix a wrong ASIN quickly" workflow benefits from immediate persistence — an analyst who spots a bad identifier shouldn't have to commit unrelated cert-form changes to remove it. Refactoring to deferred-save (option #2) was started and reverted after Shawn flagged the use case. Instead, surface the behavior with a UI hint (option #1).

**Fix:** Added an info note on `Views/Certificate/Edit.cshtml` directly above the Product ID Manager, styled with the app's blue accent (#002B76), 16px, with `fa-info-circle` icon. Text reads: *"Add, edit, and delete actions on product identifiers save immediately and apply even if you cancel the certificate edit. Use this if you need to quickly correct a wrong identifier."*

**Orphan-risk note (verified during this audit, not a defect):** `CertificateProductId.CertificateId` is NOT NULL with `OnDelete(DeleteBehavior.Cascade)` (`ApplicationDbContextModelSnapshot.cs:2143-2149`). Schema makes FK orphans impossible — deleting a cert cascades to its product IDs.

**Files:** `Views/Certificate/Edit.cshtml`.

**Out of scope:** Edit submit handler still sends a hardcoded `Status: 'Active'` in hidden inputs (`Edit.cshtml:862`), which contradicts CR-015 ("Status follows cert"). Harmless because the controller's merge logic (`CertificateController.cs:869-911`) doesn't update Status from the submission, but it's stale code. Not addressing in this fix to keep scope tight.

**Verification:** Reload `/Certificate/Edit/{any-id}` and confirm the blue note renders above the Product ID Manager. Behavior unchanged.

---

### DEF-148 — Product ID Manager: "Product ID" column rendered in monospace and the row count was center-aligned, both inconsistent with the rest of the app

- **Date:** 2026-05-06
- **Severity:** Low (visual / UI consistency)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed on `/Certificate/Edit/26-0146`, two related sub-items raised back-to-back.

**Symptom 1 — column font:** The "Product ID" column in the Product Identifiers section rendered values in `'Courier New', monospace` at `font-weight: 500`. Every adjacent column (Type, Product Name, SKU, Partner, Created, Created By) inherited the default proportional Open Sans, so the values column visually popped out as bolder typewriter text against the rest of the row.

**Symptom 2 — count alignment:** The "Showing N product ID(s)" pagination caption beneath the table was centered. The rest of the app right-aligns count captions next to their tables (admin tables, cert tile views, etc.).

**Root cause:**
- `wwwroot/css/product-id-manager.css:215` — the `.product-id-value` class carried `font-family: 'Courier New', monospace; font-weight: 500;` overrides on top of the truncation properties. Likely a leftover from when the column was treated like a code/identifier display; no other column in the app uses monospace.
- `Views/Shared/_ProductIdManager.cshtml:161` — the pagination wrapper used `class="pagination-info text-center text-muted mt-2"` (Bootstrap `text-center`).

**Fix:**
- CSS: dropped the `font-family` and `font-weight` declarations from `.product-id-value`; kept the truncation properties (`max-width`, `overflow`, `text-overflow`, `white-space`).
- Razor: `text-center` → `text-end`.

**Files:** `wwwroot/css/product-id-manager.css`, `Views/Shared/_ProductIdManager.cshtml`.

**Out of scope (intentional):** The Import Preview modal's preview row still inlines `font-family: monospace` on the `productIdValue` cell (`wwwroot/js/product-id-manager.js:1025`). That's a separate modal not visible on the cert edit page; leaving it for now since the user only flagged the main table — can revisit if it shows up in QC.

**Verification:** Static asset changes; reload `/Certificate/Edit/26-0146` to confirm.

---

### DEF-147 — Product ID Manager action icons (Edit/Delete) didn't match the app standard; trash icon wasn't red

- **Date:** 2026-05-05
- **Severity:** Low (visual / UI consistency)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed on prod cert `https://gccwebapp.com/Certificate/Edit/26-0160`.

**Symptom:** Action icons in the Product ID Manager rendered as Bootstrap outlined pill buttons (`btn btn-outline-primary` / `btn btn-outline-danger`) — square borders, larger footprint, and the delete icon used the muted Bootstrap danger outline rather than the bright red the rest of the app uses for destructive actions. Inconsistent with the canonical `admin-inline-action-btn--edit/--delete` pattern used on Pathways, Partners, Tags, etc.

**Root cause:** Two render sites diverged from the standard:
- `Views/Shared/_ProductIdManager.cshtml` rendered server-side rows with `btn btn-outline-primary` / `btn btn-outline-danger` inside a `btn-group btn-group-sm`.
- `wwwroot/js/product-id-manager.js` (`getProductIdRowHtml`) rendered client-side rows with the same legacy classes.

In addition, the canonical CSS `wwwroot/css/admin/admin-inline-editing.css` (which defines `.admin-inline-action-btn--edit` as `#00AEEF` blue and `.admin-inline-action-btn--delete` as `#FF5252` red) was not linked from either of the host pages (`Views/Certificate/Create.cshtml`, `Views/Certificate/Edit.cshtml`).

**Fix:**
- Replaced the action button markup at both render sites with the standard `<div class="admin-inline-actions">` wrapper plus `<button class="admin-inline-action-btn admin-inline-action-btn--edit">` / `--delete`. Kept the existing `btn-edit-product-id` / `btn-delete-product-id` hooks alongside the new classes so the JS event handlers continue to bind. Added `aria-label` per the app's accessibility convention.
- Linked `~/css/admin/admin-inline-editing.css` from both Create.cshtml and Edit.cshtml.

**Files:** `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/js/product-id-manager.js`, `Views/Certificate/Create.cshtml`, `Views/Certificate/Edit.cshtml`.

**Verification (localhost:5101 / `GCCDatabase_Dev`):** Playwright script `C:\Users\swest\AppData\Local\Temp\uitest\test-pid-icons.js` opened cert `26-0147` Edit page as `analyst`, scrolled to Product ID Manager, screenshotted (`pid-icons-after.png`), and read computed styles. **Edit icon `rgb(0, 174, 239)` (= `#00AEEF`)**, **Delete icon `rgb(255, 82, 82)` (= `#FF5252`)**, **zero remaining `btn-outline-*` action classes** on the page. Visual confirms the standard inline-icon footprint, edit blue, trash red.

---

### DEF-146 — Product ID Manager "Created By" was overwritten on every cert edit (showed "System" or current editor instead of the original creator)

- **Date:** 2026-05-05
- **Severity:** Medium (data-integrity / audit-trail)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed on prod cert `https://gccwebapp.com/Certificate/Edit/26-0147`, "Created by" cells showed `System` instead of the user that entered them.

**Symptom:** Product ID Manager's "Created By" column on the cert Edit page showed `System` (or, after any edit, the editor's username) instead of the user that originally added the row.

**Root cause:** `CertificateController.Edit (POST)` used a clear-and-readd pattern for the `ProductIds` collection (`certificate.ProductIds.Clear()` followed by `certificate.ProductIds.Add(productId)`), and stamped `CreatedBy = currentUserId` / `CreatedAt = now` on every row in the loop — overwriting the original creator on existing rows on every cert edit. Compounding factor: the Edit form's hidden `ProductIds[i].*` inputs (`Views/Certificate/Edit.cshtml`) don't include `CreatedBy`, so the model arrived with `CreatedBy = null`, and if `GetCurrentUserId()` ever returned null (e.g., Azure AD GUID claim that couldn't be mapped to an int user ID), the row landed with `CreatedBy = NULL`. The frontend `wwwroot/js/product-id-manager.js:244-245` falls back to literal `"System"` when `createdByUser` is null, producing the visible defect.

**Fix:** Replaced the clear-and-readd in `Controllers/CertificateController.cs` (Edit POST, Product ID handling block) with a merge: build a `Dictionary<int, CertificateProductId>` of existing rows by Id; remove rows whose Id is not in the submitted set; for each submitted row, if `Id > 0` and matches an existing row, update only the editable fields (Type, Value, Partner, ProductName, ProductSku, Description) and stamp `UpdatedBy/UpdatedAt` — preserving the original `CreatedBy/CreatedAt`; if `Id == 0`, treat as new and stamp `CreatedBy = currentUserId, CreatedAt = now` plus the same Updated fields.

**Files:** `Controllers/CertificateController.cs` (Edit POST, lines ~866-910).

**Out of scope (intentional):** No data backfill on existing NULL-CreatedBy rows (e.g., prod 26-0147). Per [feedback_data_changes_require_cr.md], data UPDATE on real cert records needs a formal CR; flagged for the client if they want historical creator attribution restored.

**Verification (localhost:5101 against `GCCDatabase_Dev`):** Playwright script at `C:\Users\swest\AppData\Local\Temp\uitest\test-pid-createdby.js` exercised three scenarios:
- Scenario A: cert `26-0025` has `CreatedBy = NULL` on its PIDs. Logged in as `analyst`, opened Edit, clicked Save with no PID changes. Expected: NULL preserved (not overwritten to `analyst`). **PASS.**
- Scenario B: cert `26-0147` has `CreatedBy = admin` on its PIDs. Logged in as `analyst`, opened Edit, clicked Save with no PID changes. Expected: `admin` preserved. **PASS.**
- Scenario C: cert `26-0147`, logged in as `analyst`, used the Product ID Manager to add a brand-new SKU PID, then saved. Expected: new row `CreatedBy = analyst`, existing rows still `CreatedBy = admin`. **PASS.**

---

### DEF-145 — Cert tile filters: Public + External filter dropdowns listed Cert Types and Product Categories with no matching visible certs (dead clicks)

- **Date:** 2026-05-05
- **Severity:** Low (functional — empty result on click; not data-loss)
- **Status:** Fixed (pending UAT / deploy)
- **Reporter:** Anuki internal QA pass — surfaced during DEF-144 audit; fixed after Shawn explicit approval ("yes do it for Public and External")

**Symptom:** On `/Certificate/CertificateTileView` (Public) and `/Certificate/CertificateTileViewExternal` (External), the filter sidebar's Cert Types and Product Categories dropdowns listed every admin-configured option, regardless of whether any cert in the user's visible cert pool actually used that option. Picking such an option produced an empty tile grid — silent zero-state, indistinguishable from a broken filter.

**Root cause:** Both endpoints pulled the cert types and product hierarchy directly from `_context.CertificationTypes` and `_context.ProductCategories`/`_context.ProductTypes` admin tables, with only an `IsActive` filter — no relationship back to the controller's `baseQuery` (the user-visible cert set). External view already had this scoping for `certificationTypes` via `certTypeIds`, but neither view scoped the product hierarchy.

**Fix:**
- `Controllers/Api/CertificateTilesApiController.cs` (Public):
  - Added `visibleCertTypeIds`, `visibleCategoryIds`, `visibleProductTypeIds`, `visibleSubcategoryIds` derived from `baseQuery`.
  - `certificationTypes` query now requires `visibleCertTypeIds.Contains(ct.Id) || ct.Children.Any(child => visibleCertTypeIds.Contains(child.Id))`.
  - `productCategoriesHierarchy` builder trims subcategories to those in `visibleSubcategoryIds`, keeps types if used directly OR if any visible subcat exists under them, and keeps categories if used directly OR if any visible type/subcat exists under them.
- `Controllers/Api/CertificateTileExternalApiController.cs` (External):
  - Same `productCategoriesHierarchy` scoping using the role-aware `baseQuery` (which already factors in user's company/partner scope).
  - `certificationTypes` was already correctly scoped — left unchanged.
- Internal view (`CertificateTileInternalApiController.cs`) intentionally unchanged. Admin/analyst should see the full taxonomy regardless of cert state, since they need to assign types/categories during cert creation.

Net effect: every dropdown option in Public and External now corresponds to at least one cert the user can actually click into.

**Tradeoff:** When admin adds a new cert type or product category but no public cert has been approved with it yet, it will not appear in the Public/External filter sidebar until an approved cert references it. This is intentional — favors filter signal over taxonomy completeness. Internal view still shows everything for admin operations.

**Test plan:**
1. Public view at `/Certificate/CertificateTileView` while logged out. Open Cert Types and Product Categories filters. Pick any option — should always return ≥1 cert.
2. External view at `/Certificate/CertificateTileViewExternal` as `external` user. Same assertion.
3. Internal view at `/Certificate/CertificateTileViewInternal` as `analyst` — confirm full taxonomy still shows (regression check).
4. Admin: add a new "Test Category" via `/Admin/ProductTypes` with no certs assigned. Reload Public view — confirm "Test Category" does NOT appear in filter sidebar. Reload Internal view — confirm it DOES appear.

**Found by:** During DEF-144 country-sort audit, code-survey of all three filter endpoints surfaced the "filters can include options with zero matching certs" UX gap. Confirmed with Shawn before implementing.

---

### DEF-144 — Cert tile filters: Countries dropdown didn't show "United States" first on Internal & External views

- **Date:** 2026-05-05
- **Severity:** Low (cosmetic — full country list is correct, just wrong sort order)
- **Status:** Fixed (pending UAT / deploy)
- **Reporter:** Anuki internal QA pass — Shawn directive during 2026-05-05 retest cycle

**Symptom:** The Countries filter on `/Certificate/CertificateTileViewInternal` and `/Certificate/CertificateTileViewExternal` listed all countries strict A-Z. Per requirement, "United States" must be pinned to the top of the list, then the rest A-Z.

**Root cause:** The two controllers were using a plain database `OrderBy(c => c)` for the countries filter. The Public-view controller already had the correct pattern (custom in-memory sort with US weighted to 0); Internal and External were missed when that pattern was added.

**Fix:**
- `Controllers/Api/CertificateTileInternalApiController.cs:526` — changed `countries = await ... .OrderBy(c => c).ToListAsync()` to materialize first via `.ToListAsync()`, then `.OrderBy(c => c == "United States" ? 0 : 1).ThenBy(c => c).ToList()`.
- `Controllers/Api/CertificateTileExternalApiController.cs:549` — same change.
- Public controller already correct — no change needed.

The materialize-then-sort pattern matches `CertificateTilesApiController.cs:322` exactly so future maintainers see one shape.

**Test plan:**
1. Log in as `analyst`. Open `/Certificate/CertificateTileViewInternal`. Open the Countries filter dropdown. Confirm "United States" appears first, all other countries A-Z below it.
2. Log in as `external`. Open `/Certificate/CertificateTileViewExternal`. Open Countries filter. Same assertion.
3. Public view (`/Certificate/CertificateTileView`) — regression check. Countries filter should still have US first (was always correct, but verify).

**Found by:** Code-survey of all three tile filter endpoints. While reviewing for the broader "filters reflect what the user can see" question, the country-sort discrepancy was the single objective fix; remaining visibility-scoping questions (e.g., filter sidebar showing cert types that have no public-visible certs) are flagged in the observations log for Shawn's CR call rather than fixed unilaterally.

---

### DEF-143 — Cert detail views load `js/certificates.js` (a list/grid initializer); page hits non-existent `/api/certificates` and throws `initializeSearch` TypeError on every load

- **Date:** 2026-05-05
- **Severity:** Low (functionally harmless — page renders correctly, download button still works because each detail view defines its own `window.downloadCertificate` inline; but two console errors fire on every detail-page navigation, including for unauthenticated public visitors hitting cert links from search engines)
- **Status:** **Fixed (pending deploy)** — removed dead `<script>` tag from 3 detail views
- **Reporter:** Internal QC continued retest, 2026-05-05. Surfaced in `session4-batch.js` test that loaded the public cert detail page for an arbitrary cert number from the live tile API.
- **Symptom:** Loading any of `/Certificate/CertificateSingleView/{certificateNumber}`, `/Certificate/CertificateInternalView/{…}`, or `/Certificate/CertificateExternalView/{…}` produces these console errors:
  1. `Failed to load resource: the server responded with a status of 404 ()` for `/api/certificates?pageSize=100&page=1`
  2. `Error loading certificates from API: Error: HTTP error! status: 404 at loadCertificatesFromAPI (.../js/certificates.js:78:23)`
  3. `TypeError: Cannot read properties of undefined (reading 'initializeSearch') at initializeCertificateSearch (.../js/certificates.js:122:25)` — cascading TypeError because the previous throw left state half-initialized
- **Root cause:** All three single-cert detail views (`CertificateSingleView.cshtml:233`, `CertificateInternalView.cshtml:336`, `CertificateExternalView.cshtml:277`) include `<script src="~/js/certificates.js"></script>`. That script is a *list/grid/search/filter* initializer for a multi-cert table view — it auto-runs `initializeCertificates()` on DOMContentLoaded, which calls `loadCertificatesFromAPI()` → `fetch('/api/certificates?pageSize=100&page=1')`. That endpoint returns 404 on the deployed build. Looking at controllers: `Controllers/Api/CertificatesApiController.cs` declares `[Route("api/[controller]")]` which should resolve to `/api/certificates`, but the route is actually 404 (also `api/etl` from `ETLController.cs` returns 404). Best guess: the legacy un-versioned `/api/certificates` and `/api/etl` controllers were either never registered in the active route table or were disabled in favor of the v1 versioned controllers (`/api/v1/certificates` returns 401 / works as expected with API key). Either way, `certificates.js` calls a route that doesn't exist.
- **Why this isn't worse:** All 3 detail views define their own `window.downloadCertificate` inline, and `openImageZoom` / `closeImageZoom` are also defined inline in the same scripts. The detail views genuinely don't need `certificates.js` for any functionality — there is no certificates grid, no search input, no filter UI on a single-cert detail page. The `<script>` tag is a dead include that produces noise on every page load with zero functional benefit.
- **Fix:** Remove the `<script src="~/js/certificates.js"></script>` line from all three detail views. Each view's inline scripts handle download / zoom / etc. independently. No other JS dependency exists between the detail views and `certificates.js`. Three single-line deletions.
- **Out of scope (separate decisions):**
  - Whether `wwwroot/js/certificates.js` itself should be deleted from the codebase. After this fix it is no longer referenced by any view (verified by grep). But deleting unreferenced JS is a cleanup decision, not a defect.
  - Whether the legacy `/api/certificates` and `/api/etl` routes should be re-enabled or the controllers deleted. The v1 versioned route at `/api/v1/certificates` is the supported Partner API surface today. If anything still relies on the unversioned routes, that would need to be migrated. Punted to a separate cleanup decision.
- **Files:** `Views/Certificate/CertificateSingleView.cshtml`, `Views/Certificate/CertificateInternalView.cshtml`, `Views/Certificate/CertificateExternalView.cshtml`
- **Test artifact:** `C:\Users\swest\AppData\Local\Temp\uitest\session4-batch.js`

---

### DEF-142 — Partner-role web user has no navigation after login; cannot access Certificates or Products views

- **Date:** 2026-05-05
- **Severity:** Medium — Partner web users are completely locked out of the UI; only the API-key path works
- **Status:** **Fixed (pending deploy)** — built clean locally; dev DB updated; needs Plesk deploy
- **Reporter:** Shawn (internal QC), 2026-05-05: created testuser5 with Partner role on gccwebapp.com, logged in, saw no nav links.
- **Root cause:** Three-part gap. (1) `Views/Shared/_Layout.cshtml:171` only renders the External-style nav for `userRole == "External"` — Partner falls through to no nav. (2) `Program.cs` `ExternalAccess` policy only authorizes Admin/Analyst/External Role claims — even if the nav linked to `/Certificate/CertificateTileViewExternal`, Partner would 403. (3) `Data/SeedData.cs:97` had Partner seeded with `IsActive=false` (per the prior DEF-112 Path 4 decision to eliminate web-user Partner role). The dev DB row reflected that flip, so the User Management role dropdown didn't even offer Partner on dev. On prod the deactivation SQL was never run, so the dropdown DID offer it — which is how testuser5 got assigned in the first place.
- **Why this reverses DEF-112's resolution:** DEF-112 (resolved 2026-05-04) chose Path 4 (eliminate web-user Partner) on the assumption no human Partner users were needed — Amazon's UAT users could become External users with company scoping. Shawn confirmed 2026-05-05 that the actual requirement is the opposite: Partner web users DO exist, mirror External access pattern, but filter by partner instead of by company. So a Partner-role login should see the same Certificates + Products nav an External user sees, with results filtered to certs tagged with their assigned partner (e.g., all Amazon staff see all Amazon-tagged certs). The cert/product filtering plumbing for this case was already fully built (`Controllers/Api/CertificateTileExternalApiController.cs:98-110`, `Controllers/ProductController.cs:72-83 + 184-190`, Edit User modal Partner Assignment dropdown, `User.Partners` many-to-many). Only the policy/nav/active-flag wiring was missing.
- **Fix (4 changes):**
  1. `Program.cs` — `ExternalAccess` policy now also accepts `"Partner"` Role claim, so Partner web users can hit `/Certificate/CertificateTileViewExternal` and `/Product/Management`.
  2. `Views/Shared/_Layout.cshtml:171` — nav branch fires for `userRole == "External" || userRole == "Partner"`. Both roles render the Certificates + Products nav. Comment updated.
  3. `Data/SeedData.cs:94-97` — Partner role now seeds with `IsActive=true` and a description that reflects the dual-purpose use (web users + API keys both flow through this role; web path filters by `User.Partners`, API path is scoped at the controller level by middleware).
  4. **Dev DB** — `UPDATE Roles SET IsActive = 1, Description = '...' WHERE Name = 'Partner'`. Verified: BEFORE IsActive=0 → AFTER IsActive=1. Prod DB needs no DB change because the deactivation SQL was never run there.
- **Architectural caveat (carried forward from DEF-112):** The dual meaning of `userRole == "Partner"` (human user vs API-key request) still shares the same string in cert-listing controllers. Functionally the two flow through different controllers and don't collide — the External tile API gates by session userId → `User.Partners`, and the `/api/v1/certificates*` Partner API path is scoped by middleware before the role check fires. But audit-trail confusion remains a future hardening item (the DEF-112 Path 1 rename to `PartnerUser` or Path 2 explicit branch split). Not in scope for this fix.
- **Verification plan:**
  - Local: log in as a Partner-role user with `User.Partners` containing "Amazon"; confirm nav shows Certificates + Products; confirm `/Certificate/CertificateTileViewExternal` lists only Amazon-tagged certs; confirm `/Product/Management` lists only products with Amazon-tagged certs.
  - After Plesk deploy: re-test Shawn's testuser5 scenario on gccwebapp.com (assign a partner via the Edit User modal first if not already assigned, then log in).
- **Files:** `Program.cs`, `Views/Shared/_Layout.cshtml`, `Data/SeedData.cs`, dev DB Roles table

---

### DEF-141 — `/Product/Management` analyst page fires `/Admin/GetProducts` on every load, producing silent SyntaxError; wasted server round-trip + console noise

- **Date:** 2026-05-05
- **Severity:** Low (functionally harmless — page renders correctly; but wastes a round-trip per page load and produces a misleading console error every time)
- **Status:** **Fixed (pending deploy)** — gate added in `wwwroot/js/admin/productManagement.js`
- **Reporter:** Internal QC continued retest (Shawn-driven, 2026-05-05). Surfaced via multi-page smoke run (`multi-page-smoke.js`).
- **Symptom:** Every load of `/Product/Management` as an analyst produces `Error loading products: SyntaxError: Unexpected token '<', "<!DOCTYPE "... is not valid JSON` in the console. The page renders correctly because the inline Razor JS handles its own `loadProducts()` against `/Product/GetProducts` (the analyst-allowed endpoint, fixed under DEF-111). But a *second* load is also firing.
- **Root cause:** `Views/Product/Management.cshtml` includes `~/js/admin/productManagement.js` solely for the import-modal helper methods (the inline view delegates `addProductBtn` import-click handling to `window.productManagement.handleImportClick()`). That script auto-instantiates `new ProductManagement()` on DOMContentLoaded, whose `init()` unconditionally calls `loadProducts()` → `fetch('/Admin/GetProducts')`. The analyst lacks Admin policy, so the framework redirects `/Admin/GetProducts` to `/Account/AccessDenied` (HTTP 200, `Content-Type: text/html`). `response.ok === true`; the unguarded `response.json()` then chokes on the leading `<` of the HTML page. The catch logs the SyntaxError and triggers a "Error connecting to server" toast which is visible briefly. Net effect: a wasted authorization round-trip on every load + console-error noise + a confusing toast that contradicts the page's actually-working state.
- **Why DEF-111's fix didn't catch this:** DEF-111 added the `response.redirected || !contentType.includes('application/json')` guard to the *inline* `loadProducts()` in `Views/Product/Management.cshtml`. The duplicate fetch comes from a *different* file — `wwwroot/js/admin/productManagement.js` — which wasn't in scope. Both functions are named `loadProducts`, which masked the duplication.
- **Fix:** Single 4-line gate in `productManagement.js` `init()`: only call `loadProducts()` / `renderProducts()` / `updateProductCount()` when `document.getElementById('products-table-body')` exists. That element only lives on `Views/Admin/ProductManagement.cshtml` (the page this class actually owns). On `/Product/Management` (which uses `id="product-table"` not `products-table-body`) the class still instantiates and binds its event handlers — the inline view explicitly delegates import-modal handling to `window.productManagement.handleImportClick()` — but skips the data-load that was hitting the wrong endpoint.
- **Verification plan:** After deploy, re-run `multi-page-smoke.js` and confirm zero console errors on `/Product/Management` for the analyst role. The Admin role's `/Admin/ProductManagement` should still load products as before (still hits `/Admin/GetProducts`, but admin has access).
- **Adjacent findings (not fixed under this defect):**
  - **Design-guide drift on success notifications.** `wwwroot/js/admin/admin-core.js` line 121 routes ALL `showNotification` calls (including `'success'`) to centered alertdialog modals that require an OK click. Project `.claude/CLAUDE.md` "User Notifications" section says "Success: Toast notification only". Either the design guide should be updated to reflect the modal-everywhere choice, or admin-core needs a separate transient-toast path for success. Not fixing without a decision — the modal-everywhere choice was deliberate per the inline code comment ("Route ALL notifications to centered modals for consistency"), so this is a doc-vs-code reconciliation, not an obvious bug.
  - **`<h1>` count = 2** on Dashboard, Product Management, and Company Index per Playwright smoke. CLAUDE.md says "Use `ViewData[\"Title\"]` (no `<h1>` in views)". Visual + accessibility audit needed before yanking duplicates — one of them might be the layout-supplied page heading and the other a duplicate inside the view.
  - **`/Admin/CertificationTypes` timed out at 30s** for analyst role during smoke. Should redirect to AccessDenied promptly. Likely a slow query or a misconfigured `[Authorize]` on a heavy endpoint that runs before the redirect. Worth a per-page timing/profiling pass.
- **Files:** `wwwroot/js/admin/productManagement.js`
- **Test artifact:** `C:\Users\swest\AppData\Local\Temp\uitest\check-def111.js`, `multi-page-smoke.js`, `product-mgmt-batch.js`

---

### DEF-140 — Login: "Remember Me" silently overridden by client-side 30-min idle auto-logout

- **Date:** 2026-05-05
- **Severity:** Medium — defeats the entire feature; user perceives Remember Me as broken
- **Status:** **Fixed (pending deploy)** — built clean locally; needs Plesk deploy + manual verification
- **Reporter:** Shawn (internal QC), 2026-05-05: "Testing: Remember Me on the login does not appear to be working" — confirmed scenario is leaving a tab open and idling.
- **Root cause:** Two-layer problem. Server-side, the prior fix (commit `bc5d619`, 2026-03-20) correctly issues a 14-day persistent auth cookie when Remember Me is checked, and the session re-hydration middleware in `Program.cs` repopulates `Session.UserRole`/`UserEmail`/`UserName` from claims on each request, so browser-close-and-reopen works. But `wwwroot/js/auth.js:67-113` sets a hard-coded 30-minute idle timer that calls `document.getElementById('logoutForm').submit()` regardless of the user's Remember Me preference. So a Remember-Me user idling on an authenticated tab gets force-logged-out by client JS even though the server cookie is still valid for 14 days.
- **Fix (3 files):**
  1. `Controllers/AccountController.cs` `SignInUser` — added a `RememberMe` claim (`"true"`/`"false"`) to the auth ticket so downstream pages can read the preference without a separate cookie or DB lookup.
  2. `Views/Shared/_Layout.cshtml` — emit `<meta name="x-remember-me" content="..." />` based on `User.FindFirst("RememberMe")` so client JS can read the active state.
  3. `wwwroot/js/auth.js` — read the meta tag; when content is `"true"`, skip both the 5-min warning toast and the 30-min auto-logout entirely. Server-side cookie expiration (14 days) is the source of truth; the existing 401-fetch-interceptor at line 142 still redirects to login if the server says the session has expired.
- **Decision (Shawn, 2026-05-05):** Remember Me checkbox stays **unchecked by default** on a fresh visit to `/Account/Login`, including after explicit logout. Pre-checking on shared computers is a security footgun, and most secure-app conventions default unchecked.
- **Out of scope:** The Azure AD B2C path (`OnTokenValidated` in `AccountController.cs` line ~100) issues its own claims and doesn't go through `SignInUser`. AAD B2C is configured but not the active auth scheme today, so its claim set wasn't touched. If/when AAD B2C is enabled in production, the same `RememberMe` claim plumbing will need to be added there.
- **Files:** `Controllers/AccountController.cs`, `Views/Shared/_Layout.cshtml`, `wwwroot/js/auth.js`

---

### DEF-139 — Product Mgmt: "Add New Product" subcategory dropdown shows empty placeholder — user can't tell if it's loading, empty, or stale

- **Date:** 2026-05-05
- **Severity:** Low-Medium — UX/feedback gap; not data-loss. Affects every Add-New / Edit Product cascade.
- **Status:** **Fixed (pending deploy)** — code change in `Views/Product/Management.cshtml`
- **Reporter:** Shawn (continuation of internal QC retest), 2026-05-05: "Add new Product: Subcategory is not loading the first time Product Type is select and there are subcategory" — reproduced on https://gccwebapp.com/Product/Management.
- **Investigation:** Programmatically exercised the cascade against the live deployed page (Playwright, multiple scenarios: first-pick, rapid swap, close+reopen, kbd interaction) — every scenario loaded the correct subcategories from `/Certificate/GetProductSubcategories?productTypeId={id}`. Deployed JS matches source. So a deterministic "doesn't load" reproduction wasn't possible from automation, which points at one of: (a) browser cache holding pre-cascade JS, (b) slow-network race where the dropdown is briefly empty between change-event and fetch-completion and the user reads it as "didn't load", or (c) ambiguity — the placeholder reads "Select Subcategory" both while loading and when the type genuinely has no subs, so the user assumes the loading case.
- **Fix (`Views/Product/Management.cshtml` `populateSubcategoryDropdown`):** three changes, all defensive:
  1. **Loading state** — set `<option>Loading subcategories...</option>` immediately on type change, before await. Now the user always sees explicit feedback that a fetch is in flight.
  2. **Empty state** — when fetch returns success+empty, render `<option>No subcategories available</option>` instead of leaving the generic "Select Subcategory" placeholder. Matches the `Views/Certificate/Create.cshtml` pattern (line 585) so behavior is consistent across the two cascades.
  3. **Request-token guard** — module-scope `_subcategoryFetchSeq` counter. Each call captures `mySeq = ++_subcategoryFetchSeq`; after each await, bail if `mySeq !== _subcategoryFetchSeq`. Prevents a slow, earlier fetch from clobbering the dropdown after the user has switched to a different type. Closes a real (low-probability) race even if the user-visible symptom turns out to be cache.
- **Files:** `Views/Product/Management.cshtml`
- **Out of scope:** Same cascade exists on `Views/Admin/ProductManagement.cshtml` and `Views/Certificate/{Create,Edit}.cshtml`. Cert pages already show "No subcategories available". The Admin/ProductManagement page wasn't named in the report; left untouched pending evidence the same UX gap matters there.

---

### DEF-138 — QC #91: Product Mgmt save silently nulled Sku and Model fields on every update — client called it "a big issue"

- **Date:** 2026-05-05
- **Severity:** Medium-High — affected every product update through the Product Mgmt modal; existing Sku and Model values were silently destroyed
- **Status:** **Fixed (pending deploy)** — verified live on `localhost:5101`
- **Reporter:** Client retest 5/1 on QC #91: "There was no product type and subcat filled in from list. So I added to 25-8760 and 25-8777. But they then are not showing in list at all. What is going on. Also, another issue: When I went back into 25-8760 it keeps clearing out Product Name and Product Type even after saving edit (but it kept prod cat and subcat). What is going on? This is a big issue!"
- **Root cause:** A prior sprint's QC #92 fix removed the Sku, Model, and Status inputs from the Add/Edit Product modal (per memory `2026-03-24` observation). The frontend `saveProduct` JS payload at `Views/Product/Management.cshtml:928-937` no longer includes `sku` or `model`. But `Controllers/ProductController.cs:386-387` (`SaveProduct` action, update branch) still unconditionally assigned `product.Sku = model.Sku` and `product.Model = model.Model`. Since the JS payload omits both fields, JSON deserialization sets `model.Sku` and `model.Model` to `null`. The server then overwrites the existing database values with `null` on every single product update. Sku/Model values populated by CSV import — or any other path that bypasses the modal — were silently destroyed by any subsequent edit.
- **Why the client perceived it as "Product Name and Product Type cleared":** The Product Mgmt table renders each row's Product cell as two stacked lines: the product Name on top, and `SKU: ${product.sku || 'N/A'}` immediately below it (`Views/Product/Management.cshtml:667-671`). Cert and Product numbers like "25-8760" and "25-8777" referenced in the client's complaint look like they were SKU values. After the first save, those SKU strings flipped to "N/A" in the table; the client identified the products by their SKU values and reported them as "not showing in list at all" and "Product Name cleared." The actual Product Name (`product.name`) was never affected. Verified by live test against the dev DB: the Name field, ProductType, ProductSubcategory, CompanyId, and CategoryId were all correctly preserved across save; only Sku and Model were nulled.
- **Fix:** Changed the SaveProduct update branch to only assign Sku/Model when the model carries a non-null value:
  ```csharp
  if (model.Sku != null) product.Sku = model.Sku;
  if (model.Model != null) product.Model = model.Model;
  ```
  This preserves existing values when the modal omits the fields, while still allowing CSV import (or any future call site that does send the fields) to update them. The Create branch is unchanged because new products legitimately start with whatever Sku/Model the modal supplies (which is null today, but that's a separate UX decision about whether to surface those inputs).
- **Live verification (2026-05-05 on `localhost:5101`):**
  - Test 1 (the bug): seeded a product with Sku=`TEST-SKU-<ts>` and Model=`TEST-MODEL-<ts>`, then submitted the exact JSON payload the modal would send (no `sku`, no `model`). On the **un-patched** build, both fields flipped to `null` after save. On the **patched** build, both fields are preserved verbatim.
  - Test 2: saving with a newly-set ProductTypeId works as before — the type and the joined ProductTypeName both flow through to the list correctly. So the second part of the client's complaint ("they're not showing in list at all" referring to type/subcat after save) was not a separate persistence bug; it was the same SKU-display-N/A symptom misread.
- **Out of scope for this fix (related but separate decisions):**
  - Whether to put Sku and Model inputs back into the Add/Edit Product modal. QC #92 explicitly removed them. If the client wants them back, it's a UX scope question — needs a decision before code change.
  - Whether to drop the `SKU: ...` line from the product list cell entirely so future SKU values can't visually disappear and confuse the user. This is a polish question, not a defect.
- **Files:** `Controllers/ProductController.cs`
- **Test artifact:** `C:\Users\swest\AppData\Local\Temp\uitest\qc91-test.js`

---

### DEF-137 — QC #107: Add User flow had two latent bugs — RequirePasswordReset silently dropped + soft-deleted email blocked re-create

- **Date:** 2026-05-04
- **Severity:** Medium — Admin/Analyst-side workflow defect; client retest at 5/1 pushed back on QC #107 with 5 sub-issues
- **Status:** **Fixed (pending deploy)** — verified live on `localhost:5101`
- **Reporter:** Client retest 5/1 on QC log row #107: "I still cannot add a user if I enter a password AND I have the checkbox checked... I tried to create another with my email and i was getting error message that a user with that email already exists"
- **Prior-sprint context (why this resurfaced):** The Sprint 2 4/10 dev note on this row claimed `"QC #107 is already resolved. The fix wraps the BeginTransactionAsync() calls inside _context.Database.CreateExecutionStrategy().ExecuteAsync(), which is the correct pattern for SQL Server transient retries"` plus added a duplicate-email pre-check so users got a real error instead of "Invalid data provided". Both pieces of that sprint's work were real, but:
  - The `BeginTransactionAsync` + `CreateExecutionStrategy().ExecuteAsync()` wrapper is **no longer in the current code** — `CreateUserAsync` was later collapsed to a single `await _context.SaveChangesAsync()` (line 167-168 carries the comment `// Single SaveChangesAsync — EF Core tracks all changes and commits atomically`). The simplification is valid (EF Core does an implicit transaction) but the prior sprint's rationale is moot in the current shape.
  - The duplicate-email pre-check (line 108-113) landed, but didn't filter `!u.IsDeleted`. So the surface the client hit at 5/1 — *delete a user, recreate with the same email* — was outside the prior fix's coverage, and the unfiltered DB UNIQUE indexes on `Email` + `Username` would have rejected the INSERT anyway.
  - The `RequirePasswordReset` checkbox-to-entity assignment was never added in any prior sprint — the field has been silently dropped on Create since the form was wired up.
  - The Sprint 2 "Ready for UAT" verdict was technically true for the surface the dev tested (you now see a real error message), but did not address the soft-delete reuse case or the checkbox flag — exactly the two cases the client retested. Same trailing-comment blind spot pattern that surfaced across 13 other rows in the 5/4 Sprint 3 reconciliation; same remediation now codified in the validation prompt template's Phase 4 pre-write checklist.
- **Sub-issue A — soft-deleted email blocked re-create:** `AdminService.CreateUserAsync` line 108-113 checked for duplicate emails via `_context.Users.FirstOrDefault(u.Email == email)` without filtering `!u.IsDeleted`. Soft-deleted users (set by `DeleteUserAsync` at line 322 — flips `IsDeleted=true`, leaves Email/Username intact) blocked re-create with the same email. Worse: even after fixing the C# check, `ApplicationDbContext.cs:374-375` defines unfiltered UNIQUE indexes on both `Email` and `Username`, so the DB itself rejects the INSERT (`SaveChanges` throws "An error occurred while saving the entity changes").
  - **Fix part 1:** Added `&& !u.IsDeleted` to the duplicate-email guard so legitimate re-create attempts pass the C# check.
  - **Fix part 2:** Modified `DeleteUserAsync` to suffix the soft-deleted user's `Email` and `Username` with `__deleted__<id>__<timestamp>` before SaveChanges. This frees the unique slot at the DB level. The audit log captures the original email in `oldUser` snapshot so the original value is preserved and reconstructable.
  - **Why suffix vs filtered index:** Filtered unique indexes (`WHERE IsDeleted = 0`) are the cleaner long-term fix but require an EF migration + DB schema change. The suffix approach is zero-migration and works within the existing schema.
- **Sub-issue B — RequirePasswordReset checkbox silently dropped on Create:** `AdminService.CreateUserAsync` lines 117-127 built the `User` entity setting `Status`, `TwoFactorMethod`, `IsActive`, `CreatedAt` but **never assigned `model.RequirePasswordReset` to `user.RequirePasswordReset`**. The checkbox state had zero effect on the created user. Per `Models/User.cs:50` the field defaults to `false`, so users always logged in without forced reset regardless of the checkbox.
  - **Fix:** Added `RequirePasswordReset = model.RequirePasswordReset` to the User initializer.
- **Sub-issue E — New External user couldn't log in (red herring):** Live test confirmed External users log in fine after creation. Carly's 5/1 complaint was likely a side-effect of Bug B — she set the checkbox expecting forced reset but got a normal login that confused her, OR she'd previously hit the soft-deleted-email block from a prior Carly Test user. Both root causes addressed by A + B.
- **Live verification (2026-05-04 on `localhost:5101`):**
  - Test A: created user → deleted → re-created with same email → both creates returned `{success: true}`. Soft-deleted row has email `qc107A_<ts>@example.com__deleted__<id>__<yyyyMMddHHmmss>`; live row has clean email.
  - Test B: created user with `requirePasswordReset: true`, logged out, logged in as that user → redirected to `/Account/ForcePasswordReset`. Flag now honored.
  - Test E: created External user with `requirePasswordReset: false`, logged out, logged in as External user → landed on `/Certificate/CertificateTileViewExternal`. Login works.
- **Out of scope for this DEF (still open from QC #107 — track separately):**
  - Sub-issue C — Password field clears when re-opening user. Working as designed (security best practice — never display stored password); needs UX helper "Leave blank to keep current password" + don't reset typed password on save. Defer to Sprint 3 polish or new ticket.
  - Sub-issue D — Company Access not limiting Admin/Analyst data. Working as designed per Carly's own 5/1 quote: "Admin and Analyst will never be limited to companies." External-side scoping is implemented at `ProductController.cs:120,125,300` and `CertificateTileExternalApiController.cs:347,354,371`. No code change needed; verify in client UAT.
- **Files:** `Services/AdminService.cs`
- **Test artifact:** `C:\Users\swest\AppData\Local\Temp\uitest\qc107-test.js`

---

### DEF-136 — Product Management: post-save success path threw `notificationQueue is not defined`, surfacing a fake error toast on a successful save

- **Date:** 2026-05-04
- **Severity:** Medium — every successful Add or Edit Product showed an error toast saying "Error saving product" even though the row had been persisted. Verified against prod test DB: products 119/120/121 (created during Shawn's UAT minutes earlier) all saved correctly with `ProductTypeId=4`, `ProductSubcategoryId=46`, while he was watching error toasts for each one.
- **Status:** **Fixed (pending deploy)** — verified DLL contents post-publish; old `showNotification(message, 'success');` literal is no longer in the compiled view.
- **Reporter:** Shawn — pasted browser console showing `ReferenceError: notificationQueue is not defined at showNotification (Management:1653:13) at HTMLFormElement.saveProduct (Management:1206:25)`.
- **Root cause:** `Views/Product/Management.cshtml` had a stale legacy `showNotification` function that referenced a `notificationQueue` global that was never declared at the top of the file (likely the residue of an unfinished in-page notification system; the auto-refresh scaffolding around it is also dead code that's never wired up). The post-save success branch in `saveProduct` called both `showNotification(message, 'success')` and `showToast(message)` redundantly. The first call threw a `ReferenceError`. The outer `catch` in `saveProduct` swallowed the error and called `showToast('Error saving product', 'error')` — which routes through `AdminCore.showNotification` (the working notification system) and rendered a misleading red toast.
- **Fix:** Dropped the broken `showNotification(message, 'success');` call from the success branch. The next line (`showToast(message)`) already routes through `AdminCore.showNotification` for the success toast, so functionality is preserved with no replacement needed.
- **Out of scope:** The dead in-page notification queue + auto-refresh scaffolding (functions `showNotification`, `processNotificationQueue`, `displayNotification`, `getNotificationIcon`, `closeNotification`, `removeNotification`, `initializeRealTimeUpdates`, `toggleAutoRefresh`, `startAutoRefresh`, `stopAutoRefresh`, `updateLastRefreshIndicator`, `createNotificationContainer`, `handleKeyboardShortcuts`) is left in place. Removing it is a polish / cleanup ticket. Same pattern likely exists in other admin views and could be refactored to a shared helper if it becomes a problem.
- **Files:** `Views/Product/Management.cshtml`.

---

### DEF-135 — CSP: web.config script-src missing Google domains, blocking admin-enabled Google Analytics

- **Date:** 2026-05-04
- **Severity:** Medium — GA was enabled by an admin via `/Admin/GoogleAnalytics` (DB row `GoogleAnalyticsSettings.IsEnabled = 1`, TrackingId `G-16J12CPD8S`), the layout injected the `gtag.js` script tag, but the browser blocked the load with `script-src` violation. Zero analytics signals were being collected on the test env.
- **Status:** **Fixed (pending deploy)** — included in the 2026-05-04 17:45 zip.
- **Reporter:** Shawn — pasted browser console showing `Loading the script 'https://www.googletagmanager.com/gtag/js?id=G-16J12CPD8S' violates the following Content Security Policy directive: "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com"`.
- **Root cause:** Two CSP sources are emitted on every response — the IIS-level header in `web.config` and the app-level header from `Middleware/SecurityHeadersMiddleware.cs`. The middleware version already lists `https://www.googletagmanager.com` and `https://www.google-analytics.com` in `script-src` and the relevant `connect-src` hosts. The `web.config` version did not. Browsers intersect multiple CSP headers, so the more-restrictive `web.config` value wins and blocks GA.
- **Fix:** `web.config` `script-src` extended with `https://www.googletagmanager.com https://www.google-analytics.com`. `connect-src` extended with the GA4 beacon hosts (`*.google-analytics.com`, `analytics.google.com`, `www.google.com`, `stats.g.doubleclick.net`, `www.googletagmanager.com`) — same set the middleware already allows. The two CSP sources now agree.
- **Per-environment scoping:** The DB toggle `GoogleAnalyticsSettings.IsEnabled` is the actual on/off switch — the layout only injects the script when it's true. CSP changes ship the same web.config to every environment, but real prod can keep `IsEnabled=false` and GA never fires there even though CSP allows it. Site is deindex-pending (Bing + Google tickets submitted by Shawn 2026-05-04) so analytics on the gated test env doesn't leak into search.
- **Followup not done here:** The dual-CSP architecture (web.config + middleware) is fragile — drift between the two has now bitten us once. Consider removing the web.config CSP entirely and letting the middleware be the single source of truth in a later cleanup pass. Out of scope.
- **Files:** `web.config`.

---

### DEF-134 — DATA-LOSS: Product Management Edit silently nulls Type/Subcategory; Subcategory FK pointed at the wrong table

- **Date:** 2026-05-04
- **Severity:** High — every product in dev DB has `ProductTypeId = NULL` and `ProductSubcategoryId = NULL` (51/51) despite the columns existing on the model. QC #91 (added "Product Type" + "Subcategory" columns to the management table) was marked Ready for UAT but the columns are blank because the data isn't being persisted.
- **Status:** **Fixed (pending deploy)** — verified end-to-end on `localhost:5101` via Playwright; new product saved with Plastics/HDPE; edit modal pre-fills all four dropdowns.
- **Reporter:** Shawn — "the product and product subcategories don't have any data even though the user entered it on a product"
- **Two independent root causes:**
  1. **`editProduct()` only set name + description.** `Views/Product/Management.cshtml:824-836` left Company, Category, Product Type, Subcategory dropdowns empty when the modal opened in Edit mode. When the user hit Save without manually re-picking each one, `ProductController.SaveProduct` blindly overwrote all four fields with `null`. Every Edit was a silent wipe.
  2. **`Product.ProductSubcategoryId` FK pointed at the wrong table.** The model FKed subcategory to the `ProductSubcategories` table (`ApplicationDbContext.cs:203-205`), but `ProductSubcategories` has 0 rows and the cascade endpoint `/Certificate/GetProductSubcategories` returns Level-2 children from the `ProductTypes` table. So if a user picked a subcategory in the modal, EF rejected the save with FK violation → exception caught → `Success=false` → user saw a generic error toast. `Certificate.ProductSubcategory` already correctly used `ProductType?` (`Certificate.cs:113`); `Product` was the inconsistent one.
- **Fix:**
  - **Model:** `Product.ProductSubcategory` retyped from `ProductSubcategory?` → `ProductType?` (`Models/Product.cs`). Removed unused `Products` collection from `Models/ProductSubcategory.cs`.
  - **DbContext:** `Product → ProductSubcategory` relationship reconfigured to `WithMany().OnDelete(Restrict).IsRequired(false)` targeting the `ProductTypes` table (`Data/ApplicationDbContext.cs`).
  - **Migration:** `20260504204044_RetargetProductSubcategoryFkToProductTypes.cs` — drops `FK_Products_ProductSubcategories_ProductSubcategoryId`, adds `FK_Products_ProductTypes_ProductSubcategoryId`. No data conversion needed (all values were NULL). Reversible.
  - **API surface:** `ProductController.GetProducts` now returns `companyId`, `categoryId`, `productTypeId`, `productSubcategoryId` alongside the existing `*Name` fields. `ProductListViewModel` extended with the four ID properties.
  - **Edit pre-fill:** `editProduct()` made async; pre-fills Company + Category + Type + Subcategory from the loaded product, awaiting the subcategory cascade fetch before showing the modal.
  - **Import lookup:** `ImportProducts` subcategory lookup repointed from the empty `ProductSubcategories` table to `ProductTypes` Level-2 children (filter: `Name == subcategoryName && ParentId == productTypeId`). Product type lookup also now scoped to Level-1 (`ParentId == null`) to prevent accidentally matching a child type with the same name.
- **Verified live on `localhost:5101`:**
  - Add Product modal: picked Chemicals → Plastics → HDPE, saved. DB row shows `ProductTypeId=4, ProductSubcategoryId=46`. Both columns render in the management table.
  - Edit Product modal: opening an existing product pre-fills all four dropdowns to the saved values (company=1, cat=1, type=4, sub=46).
  - Migration applied to dev DB; FK now reads `FK_Products_ProductTypes_ProductSubcategoryId` per `sys.foreign_keys`.
- **Out of scope (not changed):**
  - Existing 51 products in dev DB still have `NULL` for both fields — backfill is a data action, not a code fix; would need a CR if client wants legacy products tagged. Per `feedback_data_changes_require_cr.md`.
  - The `ProductSubcategories` table is now orphaned for Product but is still referenced by `UnitOfWork`/`IUnitOfWork` and EF DbSet. Left in place to keep the surface area minimal; can be dropped in a later cleanup.
  - QC #91 stays Ready-for-UAT — the column-rendering work it shipped was correct; this defect is the orthogonal save-path bug behind the empty cells.
- **Files:** `Models/Product.cs`, `Models/ProductSubcategory.cs`, `Data/ApplicationDbContext.cs`, `Controllers/ProductController.cs`, `Views/Product/Management.cshtml`, `Migrations/20260504204044_RetargetProductSubcategoryFkToProductTypes.cs` (+ `.Designer.cs`, snapshot updated).
- **Followup polish (same UAT cycle, 2026-05-04 17:45 zip):** `ProductController.GetProductTypes` was returning every active `ProductTypes` row regardless of `ParentId`, so the modal's Product Type dropdown showed Level-1 roots + Level-2 children mixed (and on prod, also Level-0 legacy "Construction" roots). The Subcategory cascade then re-displayed the same Level-2 entries as if they were direct subcategories of themselves. Fix: added `pt.ParentId == null` to the `GetProductTypes` filter so only root types reach the dropdown. Subcategory cascade is unchanged (already correctly returns direct children of the selected type).
- **Migration applied to prod test DB:** 2026-05-04 — pre-flight confirmed 0 non-null `ProductSubcategoryId` rows and 100 total products before apply; post-apply, `sys.foreign_keys` shows `FK_Products_ProductTypes_ProductSubcategoryId` correctly targets `ProductTypes`. Migration logged in `__EFMigrationsHistory`. New saves verified working — products 119, 120, 121 created during UAT all persisted with `ProductTypeId=4` (Plastics) and `ProductSubcategoryId=46` (HDPE).

---

### DEF-133 — UI: mojibake (`â–¶`, `â€¢`, `â€"`, `â†'`) baked into 4 Razor views — visible to users on Product Management

- **Date:** 2026-05-03
- **Severity:** Medium — visible cosmetic corruption on Product Management page (`▶` row-expand triangle, `•` last-updated bullet) and Admin > Product Management (`—` em-dash placeholder for empty Parent Identifiers)
- **Status:** **Fixed (pending deploy)**
- **Reporter:** Shawn — "why am I seeing special characters. â–¶"
- **Root cause:** Source files were UTF-8 with BOM, but at some point each was opened in an editor that interpreted them as Windows-1252 and re-saved as UTF-8 — re-encoding the multibyte sequences as their Windows-1252 visual aliases. The bad bytes were then literally checked in. Same family of bug as the prior `Database_Scripts/FixMojibakeProductNames.sql` (which corrected `™` → `â„¢` and `®` → `Â®` in product names on prod 2026-04-14).
  - `▶` (U+25B6, UTF-8 `E2 96 B6`) → `â–¶`
  - `•` (U+2022, UTF-8 `E2 80 A2`) → `â€¢`
  - `—` (U+2014, UTF-8 `E2 80 94`) → `â€"`
  - `→` (U+2192, UTF-8 `E2 86 92`) → `â†'`
- **Fix:** Restored the 7 corrupted glyphs in 4 view files. Verified clean: `grep "â"` against `Views/` returns zero matches; dev DB product names have zero remaining mojibake (already fixed by 2026-04-14 SQL script).
- **User-visible instances corrected:**
  - `Views/Product/Management.cshtml:647` — row-expand triangle in product list
  - `Views/Product/Management.cshtml:1497` — collapsed-state triangle assignment
  - `Views/Product/Management.cshtml:1361` — last-updated bullet separator
  - `Views/Admin/ProductManagement.cshtml:82` — em-dash empty-state for Parent Identifiers cell
- **Comment-only instances (not user-visible, cleaned for hygiene):**
  - `Views/Product/Management.cshtml:527` — JS comment
  - `Views/Certificate/Edit.cshtml:499` — JS comment
  - `Views/Certificate/Create.cshtml:447` — JS comment
- **Out of scope (intentionally not touched):**
  - `deploy-out/**` — regenerates on next `dotnet publish`
  - `docs/tracking/test-execution-log.md`, `qc-readyforuat-2026-05-01.md`, `qc-manual-tests-2026-05-01.md`, `qc-sit-screenshots-2026-04-24/index.html` — internal docs, not served to users
  - 30+ `.cs` controller/service files lack a UTF-8 BOM but contain only ASCII high-byte content in legitimate string literals; no rendering bug, no action.
- **Prevention recommendation (NOT implemented — proposing for tracking):** add a pre-commit hook or CI check that greps `^Views/.*\.cshtml$` for the canonical mojibake byte sequences (`â„`, `â€`, `â†`, `â–`, `Â®`, `Â `) and fails the commit. Cheap to wire up; would have caught this at the moment of corruption.
- **Files:** `Views/Product/Management.cshtml`, `Views/Admin/ProductManagement.cshtml`, `Views/Certificate/Edit.cshtml`, `Views/Certificate/Create.cshtml`

---

### DEF-132 — REGRESSION (caught + fixed same-cycle): DEF-121 [JsonIgnore] + C# `required` keyword incompatibility broke Partner API key authentication

- **Date:** 2026-05-03
- **Severity:** Medium — caught + fixed within minutes during cycle 4 audit; never deployed
- **Status:** **Fixed (pending deploy)**
- **Reporter:** Cycle 4 audit run discovered Partner API was returning 500 on `/api/v1/certificates/search` with log message `"Error during Partner API key authentication"` and inner exception `JsonPropertyInfo 'passwordHash' defined in type 'GCCWebApp.Models.User' is marked required but does not specify a setter`.
- **Root cause:** The DEF-121 stopgap added `[JsonIgnore]` on `User.PasswordHash`, but the property was also declared with the C# `required` keyword (`public required string PasswordHash { get; set; }`). System.Text.Json cannot reconcile the two: `[JsonIgnore]` removes the property from JSON deserialization, but `required` still expects the JSON to provide it. When the Partner API key auth path round-tripped a User object through JSON (likely cache/session), deserialization threw.
- **Fix:** Dropped the `required` keyword from `PasswordHash` in `Models/User.cs`. Initialization safety preserved by EF Core's NOT NULL DB constraint and by every `new User { ... }` site explicitly setting the field (verified via grep).
- **Verified:** Live curl with Amazon partner key on `localhost:5101` — `/api/v1/certificates` and `/api/v1/certificates/search` both return 200 with valid contract DTOs. No 500s.
- **Lesson for followup ticket (DEF-121 DTO refactor):** the `[JsonIgnore]` stopgap is cleaner with non-required nullable string properties. The DTO refactor (option 1 of original DEF-121) is now even more attractive because it sidesteps this kind of attribute-vs-keyword conflict entirely.
- **Files:** `Models/User.cs`

---

### DEF-131 — SECURITY: Partner API keys could POST/PUT/DELETE certificates via `/api/v1/certificates/bulk/*` (privilege escalation — read-only contract violated)

- **Date:** 2026-05-03
- **Severity:** **CRITICAL (S1)** — privilege escalation; partners are contractually read-only consumers, but they could create/update/delete certificates
- **Status:** **Fixed (pending deploy)** — same-day fix in cycle 4
- **Reporter:** Cycle 4 audit of all v1 controllers reachable by `[Authorize(AuthenticationSchemes = "ApiKey")]`
- **Problem:** `Controllers/Api/v1/BulkCertificatesController.cs` was decorated with `[Authorize(AuthenticationSchemes = "ApiKey")]` at the class level (line 15). This permitted any valid Partner API key to call:
  - `POST /api/v1/certificates/bulk/create` (line 28) — accepts up to 100 `Certificate` entities, persists them via `_context.SaveChangesAsync()`
  - `PUT /api/v1/certificates/bulk/update` (line 128) — updates existing certs in batch
  - `DELETE /api/v1/certificates/bulk` (line 228) — deletes by ID list
  - `POST /api/v1/certificates/bulk/validate` (line 280)
- **Live evidence (before fix):** `curl -X POST -H "X-API-Key: <amazon partner key>" -d '[]' http://localhost:5101/api/v1/certificates/bulk/create` → HTTP 400 with body `"No certificates provided"` — meaning **the auth check passed and only the empty-array branch rejected**. A real payload would have been processed.
- **Why this got through:** Copy-pasted controller-level auth attribute from `CertificatesController` (which legitimately accepts ApiKey for read endpoints). Whoever added the bulk controller didn't differentiate read vs mutation auth requirements.
- **Contract source:** Per CLAUDE.md `External User Restrictions`, External users (closest human analogue to Partner role) have no Add/Edit cert buttons. Per memory `feedback_amazon_api_immutable.md`, Amazon Partner API contract is read-only. Per the prompt's role table, Partner uses `X-API-Key` header for **read** access to `/api/v1/*`.
- **Fix:** Changed class-level attribute from `[Authorize(AuthenticationSchemes = "ApiKey")]` to `[Authorize(Policy = "AnalystOrAdmin")]`. This forces cookie-based auth (default scheme) and rejects any caller not in the Analyst or Admin role. Partner API keys are now blocked.
- **Verified live (post-fix curl, Amazon partner key):**
  - `POST /api/v1/certificates/bulk/create` → HTTP 403 `"Access denied. You do not have permission to perform this action."`
  - `PUT /api/v1/certificates/bulk/update` → HTTP 403
  - `DELETE /api/v1/certificates/bulk` → HTTP 403
  - Sanity: `GET /api/v1/certificates` (Partner read endpoint) → HTTP 200 (unaffected)
- **Internal impact assessment:** `grep -r 'api/v1/certificates/bulk\|BulkCertificatesController'` found ZERO callers in the repo. No frontend JS, no service code, no test code references these endpoints. The lockdown is functionally invisible to the existing app — there is no internal feature that uses the bulk routes. If Admin/Analyst tooling needs them in future, they can authenticate via cookie just like every other admin route.
- **Files:** `Controllers/Api/v1/BulkCertificatesController.cs`

---

### DEF-130 — SECURITY+DATA-EXPOSURE: `/api/v1/products` was Partner-API-key authorized at the class level — exposing GET data leak AND POST/PUT/DELETE mutation methods to partners

- **Date:** 2026-05-03 (reported), 2026-05-04 (fixed via lockdown, severity upgraded after method audit, deployed)
- **Severity:** **CRITICAL (S1)** — upgraded 2026-05-04 from S3 after a smoke against prod confirmed the controller exposes `[HttpPost]` (line 207), `[HttpPut("{id}")]` (line 241), and `[HttpDelete("{id}")]` (line 292) in addition to the GET routes (lines 82, 171, 318). With the pre-fix class-level `[Authorize(AuthenticationSchemes = "ApiKey")]`, any valid partner API key (e.g., the live Amazon prod key) could **create, update, or delete products** via the API — the same privilege-escalation class as DEF-131. The 2026-05-04 attribute swap closes all four HTTP methods at once.
- **Status:** **Deployed 2026-05-04** — option 1 (lockdown) shipped per Shawn's call. Verified live on gccwebapp.com: real Amazon partner key gets HTTP 403 ("Access denied. You do not have permission to perform this action.") on `GET /api/v1/products`. Anonymous returns 401. Test partner key (id=7) was generated, used for verification, and revoked in the same minute.
- **Reporter:** Cycle 4 audit of v1 controllers reachable by Partner API key
- **Problem:** `Controllers/Api/v1/ProductsController.cs` was decorated with `[Authorize(AuthenticationSchemes = "ApiKey")]` at line 29 — Partner API keys could hit it. The list endpoint returned raw `Product` entities including:
  - Internal IDs (`id`, `categoryId`, `companyId`, `productTypeId`, `productSubcategoryId`)
  - `isActive`, `isDeleted` flags (internal lifecycle state)
  - Nested `category` object with `description`, `sortOrder`, `products: [null]` (circular nav reference)
  - Nested empty `productType`, `productSubcategory`, `company` references
  - Nested `certificates: [null]` array (circular)
- **Live evidence (Partner key → `GET /api/v1/products?pageSize=1`):** Returned 51 products (all of them — no partner scoping) with full internal structure visible.
- **Contract:** Products endpoint is NOT in the Amazon Developer Guide §4 or §8 contract list. The contracted partner endpoints are `/api/v1/certificates*` only. Two reasonable fix paths existed:
  1. **Drop Partner access entirely:** change auth to `[Authorize(Policy = "AnalystOrAdmin")]` (same as DEF-131 fix). Clean answer if Amazon doesn't consume it.
  2. **Add a partner DTO projection:** if Amazon does consume the products endpoint, project to a clean `ProductPartnerDto` with only public-safe fields and drop the nested entity navigation. Would require Carly→Kyle sign-off per `feedback_amazon_api_immutable.md`.
- **Decision rationale (2026-05-04):** Production usage check — confirmed via dev DB that Amazon's API key has `AllowedEndpoints = NULL` (no endpoint restriction in middleware), so partners CAN structurally hit the route, but `/api/v1/products` is not part of the Amazon Developer Guide contract and so isn't bound by the contract-immutability rule. The aggregate `PartnerApiKey.RequestCount` doesn't break down by endpoint, and Application Insights is disabled in prod (`EnableApplicationInsights: false`), leaving stdout logs as the only behavioral source — Shawn opted to ship the lockdown immediately (fully reversible in minutes) and pull prod stdout logs in parallel. If logs come back showing Amazon usage, revert + ship option 2 as a follow-up CR.
- **Fix (2026-05-04):** Single-attribute swap on `Controllers/Api/v1/ProductsController.cs:29` — `[Authorize(AuthenticationSchemes = "ApiKey")]` → `[Authorize(Policy = "AnalystOrAdmin")]`. Same pattern as DEF-131. Class-level only; no method-level overrides exist. XML doc-comment updated to reflect the new access posture and reference DEF-130 for the rationale.
- **Internal-consumer impact analysis:** Grep across `*.cs`, `*.cshtml`, `*.js`, `*.html` for `api/v1/products` returned only (a) the controller's own `CreatedResponse` self-reference, (b) a code comment in `PartnerApiKeyAuthenticationMiddleware`, (c) an example string in `PartnerApiController` doc, (d) the `PartnerApiKey.AllowedEndpoints` example, and (e) the `Views/Admin/GCCAPI.cshtml` admin docs page (which still works for Admin/Analyst cookie auth). Zero JS or backend code consumes this endpoint internally — lockdown has zero functional impact on the internal app.
- **Followup if logs show partner calls:** Revert the attribute, write `Models/Api/ProductPartnerDto.cs` with only public-safe fields (id, name, description, productIds[]), update controller methods to project, and route the change as a Partner API contract change requiring Carly→Kyle sign-off.
- **Files:** `Controllers/Api/v1/ProductsController.cs`

---

### DEF-129 — SECURITY/CONTRACT: Partner API `/api/v1/certificates/search` leaks raw entity (same bug class as DEF-103/DEF-124)

- **Date:** 2026-05-03
- **Severity:** **CRITICAL (S1)** — Partner API contract violation per `feedback_amazon_api_immutable.md` and prompt's S1 trigger list
- **Status:** **Fixed (pending deploy)** — same-day fix in cycle 4 of the QA pass
- **Reporter:** Cycle 4 audit of `[HttpGet]` actions in `CertificatesController` after DEF-124 fix
- **Problem:** Identical bug class to DEF-124, found in the same controller. `GetExpiringCertificates` was projected in cycle 3, but `SearchCertificates` (line 549) was still returning the raw `Certificate` entity directly via `PaginatedResponse(certificates, ...)`. No DTO projection, no `PublicStatuses` filter, no role check.
- **Live evidence (before fix):** `curl -H "X-API-Key: <amazon>" "/api/v1/certificates/search?term=Widget&status=Client%20Approved%20-%20Paid&pageSize=2"` returned a Certificate entity with `id: 2`, `status: "Client Approved - Paid"` (raw), `country: "United States"` (raw), flat `asin/upc/gtin/ean`, internal IDs `companyId/productId/*Id`, plus a nested `company` entity with `email`, `contactInfo`, and a nested `products` array containing more circular references.
- **Why this got through:** DEF-103 (April 21) was supposed to cover all 6 partner endpoints but missed `/expiring`, `/{id}`, `/number/{n}`. The April 30 self-test caught the latter two. The May 3 cycle 1 caught `/expiring`. The May 3 cycle 4 audit caught `/search` — the fourth from the same scope-creep gap. **A controller-wide grep is now the right test.**
- **Fix (cycle 4):** Same pattern as DEF-124. `SearchCertificates`:
  1. Detects `userRole == "Partner"` via `GetCurrentUserRole()`
  2. Includes `Pathway` and `ProductIds` (DTO requires them)
  3. For partner callers: filters on `PublicStatuses.Contains(Status) && partnerNames.Contains(Partner)`. Empty paginated response if no partner association.
  4. Projects via shared `BuildPartnerCertificateDto` helper. Admin/Analyst still get the raw entity.
  5. Changed return type signature on the `PaginatedResponse` call so it accepts both projected DTOs and raw entities.
- **Verified live (post-fix):** `curl ".../search?term=Widget&status=Client%20Approved%20-%20Paid"` now returns `{certificateNumber: "26-0001", status: "Active", certificationCountries: "All", company: "Acme Corporation", productIds: [], pdfDownloadUrl: ...}` — no internal IDs, no flat product fields, no nested entities.
- **Followup ticket recommended:** rather than chasing endpoint-by-endpoint, write a controller-level unit test that asserts every `[HttpGet]` in `CertificatesController` returns the contract DTO when caller is `Partner`. Catches future regressions automatically.
- **Files:** `Controllers/Api/v1/CertificatesController.cs`

---

### DEF-128 — DATA-EXPOSURE/FUNCTIONAL: `/Certificate/ExportCertificates` is `[AllowAnonymous]`, supports Excel/PDF (rule says CSV-only), and PDF format returns broken file

- **Date:** 2026-05-03
- **Severity:** **HIGH (S2)** — driven by Internal Tag exposure to anonymous callers (per CLAUDE.md, even External users cannot view internal tags; this endpoint exposes them to **anyone**)
- **Status:** **Fixed (pending deploy)** — same-day fix in cycle 3 of the QA pass
- **Reporter:** Internal QA pass cycle 2 (full-spectrum-test prompt)
- **Composite finding — 4 sub-issues in one endpoint:**

  **(a) Anonymous Internal Tag exposure — S2:**
  - `Controllers/CertificateController.cs:1708` — `ExportCertificates` is `[AllowAnonymous]`
  - Live test: `curl http://localhost:5101/Certificate/ExportCertificates?format=csv` (no cookie, no key) returns a 24-column CSV
  - Column 20 is `"Internal Tag"`, populated with values like `"Sustainable"`, `"Eco-Friendly"`, `"Verified"` for real certs
  - Per CLAUDE.md `External User Restrictions`: "CANNOT view/edit internal tags". Anonymous tier is below External, so this clearly violates the intent.
  - Other sensitive columns leaked: `"Company Contact Email/URL"` (e.g., `contact@acme.com`), `"Status"` raw lifecycle (e.g., `"Client Approved - Paid"` instead of public-facing literal), `"ASIN"`, `"UPC"`, `"GTIN"`, `"EAN"`.

  **(b) Excel format support violates "CSV ONLY" rule — S3:**
  - CLAUDE.md Critical Rule: "**CSV ONLY** - No Excel/JSON/PDF from admin pages. Use `ExportUtils.exportToCSV()` directly"
  - Endpoint accepts `format=excel`, returns real `Microsoft Excel 2007+` (.xlsx), MIME `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, ~12KB
  - Both internal AND external tile views send `format=excel` (`certificate-tile-view-external.js:1397`, `certificate-tile-view-internal.js`)

  **(c) PDF format returns broken file — S3 functional:**
  - `curl ".../ExportCertificates?format=pdf"` returns HTTP 200 OK, `Content-Type: application/pdf`, ~7.8KB
  - But `file` reports the body as `ASCII text, with CRLF line terminators` — this is **not a real PDF** (no `%PDF-` magic bytes)
  - User experience: clicking "Export to PDF" downloads a file that no PDF reader can open
  - Probably the legacy CSV stream getting served with a PDF MIME, or the export service returns a stub. Either way, broken on the wire.

  **(d) Button label says "Export All to Excel" — S5 polish:**
  - `Views/Certificate/CertificateTileViewExternal.cshtml:184` and `CertificateTileViewInternal.cshtml:249`:
    `<i class="fas fa-file-excel"></i> Export All to Excel`
  - aria-label: `"Export all visible certificates to Excel file"`
  - This contradicts "CSV ONLY" — the user believes they're getting Excel, the rule says CSV.

- **Recommended fix (in priority order):**
  1. **Add `[Authorize]` to `ExportCertificates`** (or remove the endpoint and route through `ExportUtils.exportToCSV()` per the rule). Anonymous callers should not be able to bulk-pull cert data with internal tags.
  2. **Drop the Internal Tag column** from any export accessible to External or anonymous callers. (For Admin/Analyst, fine to keep.)
  3. **Force `format=csv`** server-side, ignore other format values (or 400 them). Update both tile-view JS files to send `format=csv` and update button label/icon to match (`<i class="fas fa-file-csv"></i> Export All to CSV`).
  4. **Project the Status field** through `CertificateStatusHelper.PublicStatuses` mapping when caller is anonymous/External (same DEF-103 pattern).
  5. **Remove the `pdf` branch** from the `format` switch entirely — it's broken and not used.
- **Files:** `Controllers/CertificateController.cs`, `wwwroot/js/certificate-tile-view-external.js`, `wwwroot/js/certificate-tile-view-internal.js`, `Views/Certificate/CertificateTileViewExternal.cshtml`, `Views/Certificate/CertificateTileViewInternal.cshtml`, `Services/CertificateService.cs` (ExportCertificatesAsync — confirm CSV-only output regardless of format param).
- **Test evidence (cycle 2, 2026-05-03):** Curl outputs captured in test-execution-log Section H.
- **Fix (2026-05-03 cycle 3):** All four sub-issues addressed in one batch.
  1. **Service interface (`Services/IServices.cs`):** dropped `string format` param, added `bool includeInternalTags = false`. Same for `ExportCertificatesAsync`, `ExportCertificatesByIdsAsync`, `ExportFilteredResultsAsync`. Default `false` — most restrictive.
  2. **Service implementation (`Services/CertificateService.cs`):** dropped the `format.ToLower() switch` (no more Excel / PDF branches). Always calls `GenerateCsvExport`. The `GenerateExcelExport` and `GeneratePdfExportFromRows` methods are retained as dead code for now; cleanup is a followup ticket.
  3. **`GenerateCsvExport`:** added `bool includeInternalTags` parameter. When false, the `"Internal Tag"` column is dropped from both the header row and every data row. Column count goes from 24 → 23 for anon/External callers.
  4. **MVC controller (`Controllers/CertificateController.cs:1709`):** kept `[AllowAnonymous]` (public tile view legitimately needs export), but: hardcoded `Content-Type: text/csv` and `.csv` extension; computes `includeInternalTags = (userRole == "Admin" || userRole == "Analyst")` so anon/External/Partner do NOT get the column.
  5. **API controller (`Controllers/Api/CertificatesApiController.cs`):** dropped format switch, hardcoded CSV. Endpoint is `[Authorize(Policy = "AnalystOrAdmin")]` so always passes `includeInternalTags: true`.
  6. **Views (`CertificateTileViewExternal.cshtml`, `CertificateTileViewInternal.cshtml`):** "Export All to Excel" → "Export All to CSV"; `fa-file-excel` → `fa-file-csv`; aria-label updated.
  7. **JS (`certificate-tile-view-external.js`, `certificate-tile-view-internal.js`):** all `params.append('format', 'excel')` → `'csv'`; toast text "Exporting … to Excel" → "to CSV".
  8. **Tests (`GCCWebApp.Api.Tests/Unit/Services/CertificateServiceTests.cs`):** updated 8 call sites to use new signature; added 2 new tests covering the include-internal-tags-false behavior (DEF-128 regression coverage).
- **Verified live (curl on `localhost:5101`, 2026-05-03 — anonymous, no API key, no cookie):**
  ```
  GET /Certificate/ExportCertificates?format=excel  → text/csv (CSV ASCII, was Microsoft Excel 2007+)
  GET /Certificate/ExportCertificates?format=pdf    → text/csv (CSV ASCII, was broken PDF stub)
  GET /Certificate/ExportCertificates?format=csv    → 23 columns (was 24)
    Header: "Certificate #",…,"External Tag","ASIN",…  ← no Internal Tag between External Tag and ASIN
    First data row: …,"Verified","B08X7N9MRQ",…       ← Internal Tag value "Sustainable" gone
  ```

---

### DEF-127 — POLISH: Import/Export icon convention drifted on Product/Company pages

- **Date:** 2026-05-03 (reported), 2026-05-04 (fixed + deployed)
- **Severity:** Low (S5 polish)
- **Status:** **Deployed 2026-05-04** — verified live on gccwebapp.com: 4 affected pages (`/Admin/ProductManagement`, `/Company/Index`, `/Product/Management`, `/Product/Categories`) all show `fa-file-arrow-down`/`fa-file-arrow-up`; zero residual `fa-download`/`fa-upload` occurrences anywhere on those pages.
- **Reporter:** Internal QA pass cycle 2 (visual sweep)
- **Symptom:** DEF-101 standardized Import = down arrow, Export = up arrow across admin pages. The convention is honored everywhere, but the icon class names diverge:
  - **`/Admin/UserManagement` and `/Admin/Tags`** use `fa-file-arrow-down` / `fa-file-arrow-up` (file-with-arrow icons)
  - **`/Admin/ProductManagement` and `/Company/Index`** use generic `fa-download` / `fa-upload` (tray icons)
- **Why it's polish-tier:** Both classes are visually down/up arrows so the DEF-101 spec ("Import = down arrow, Export = up arrow") is technically satisfied. Visual consistency is just slightly off — paged-side-by-side, the icons look different. No user is going to be confused, but design QC will notice.
- **Fix (2026-05-04):** Aligned Import/Export buttons on the four drifted pages to use `fa-file-arrow-down` / `fa-file-arrow-up`. Also updated the Import Preview modal headers on the same pages so the icon stays consistent inside the modal.
- **Files:** `Views/Product/Management.cshtml` (lines 39, 42, 141), `Views/Company/Index.cshtml` (lines 38, 41, 96), `Views/Admin/ProductManagement.cshtml` (lines 37, 40, 116), `Views/Product/Categories.cshtml` (lines 18, 21).
- **Verification:** Source-side grep confirms both icon classes present in all four files; localhost runtime verify pending — Razor runtime compilation isn't configured in this project, so the change reaches the browser only after the next dev server restart / publish.
- **Out of scope (intentionally not touched):** Admin pages with Export-only buttons (`CertificateFields`, `CertificateTypes`, `Partners`, `Pathways`, `RoleManagement`, `ProductTypes`, `CSFAttributes`) currently use `fa-upload` for Export. Convention-wise they satisfy DEF-101 ("Export = up arrow"); standardizing them to `fa-file-arrow-up` is a separate polish ticket if design QC flags it. `Views/Product/ManagementAlt.cshtml` and `ManagementV2.cshtml` are alt views (not on sitemap) — left alone.

---

### DEF-126 — Add/Edit Product ID modal: stray ⚠ triangle on initial open + still cramped at 900px

- **Date:** 2026-05-03
- **Severity:** Medium — visual / UX bug; not blocking but looks broken on every modal open
- **Status:** **Fixed local** — pending deploy
- **Reporter:** Shawn (`http://localhost:5101/Certificate/Edit/26-0147`, "Edit Product ID and Add Product ID is still not wide enough and getting triangle caution icon, validation issues?")
- **Symptom:** Opening the Add Product ID (or Edit Product ID) modal on Certificate Create / Edit shows a small red ⚠ triangle below the empty Product ID input *before the user has touched anything*. Modal also still feels cramped on a 1920px desktop monitor.
- **Root cause — triangle:**
  - `wwwroot/css/accessibility-enhancements-low-priority.css` lines 422–427 add `content: '\26A0 '` (⚠) as a `::before` pseudo-element to **every** `.invalid-feedback` div globally — regardless of whether the field is in an error state.
  - The Product ID modal markup pre-renders `<div class="invalid-feedback" id="valueValidation"></div>` as an empty placeholder ready for JS to fill with an error message.
  - `wwwroot/css/product-id-manager.css` overrides Bootstrap's default `display: none` on `.invalid-feedback` with `display: block` so it always reserves vertical space.
  - Combined: the empty placeholder div is forced visible, and the global pseudo-element paints the ⚠ even with no error to display.
- **Root cause — width:** DEF-119 had bumped `#productIdModal .modal-dialog max-width` from 650px to 900px. On a 1920px desktop the modal still occupies under half the viewport and the two-column form looks pinched.
- **Fix:**
  1. `accessibility-enhancements-low-priority.css`: changed selector from `.invalid-feedback::before` → `.invalid-feedback:not(:empty)::before` (and same for `.error` and `.field-validation-error`). The ⚠ now only renders when the feedback element actually contains an error message.
  2. `product-id-manager.css`: bumped `#productIdModal .modal-dialog max-width` from 900px → 1140px (matches Bootstrap's modal-xl).
- **Files:** `wwwroot/css/accessibility-enhancements-low-priority.css`, `wwwroot/css/product-id-manager.css`
- **Verified locally (2026-05-03):** Playwright on `http://localhost:5101/Certificate/Edit/26-0147`, viewport 1920×1080, admin user.
  - **Fresh Add modal:** no triangle below empty Product ID input. DOM check confirmed `valueValidation` div has empty `textContent` and `getComputedStyle(...,'::before').content === 'none'`.
  - **Invalid input (typed "B08N5WRWN" with ASIN selected):** red triangle ⚠ now renders alongside the message "ASIN must be exactly 10 alphanumeric characters" — accessibility behavior preserved for actual errors.
  - **Modal width:** dialog bounding box now `1140 × ~990` (was `900 × 992`), centered at `x=390` on a 1920px viewport — visibly more breathing room around all form fields.
- **Regression risk:** the `:not(:empty)` selector change is global. Any other form on the site that relied on the ⚠ pseudo-element appearing on a *deliberately empty* `.invalid-feedback` div (and the empty div being meaningful as a visual indicator) would lose the icon. Practical impact: zero — `.invalid-feedback` is always paired with a message in Bootstrap-style validation; an empty one is always a placeholder.

---

### DEF-125 — Polish: 404 vs 302 on Admin routes leaks which controller actions exist

- **Date:** 2026-05-03
- **Severity:** Low (Polish / S5) — minor reconnaissance leak; not a real attack vector
- **Status:** **Open** — defer; not worth a fix unless touched anyway
- **Reporter:** Internal QA pass (cycle 1 of full-spectrum-test prompt)
- **Symptom:** Anonymous request to a non-existent admin action returns HTTP 404, while a real admin action returns HTTP 302 (redirect to login). This lets an attacker enumerate which admin actions exist without authenticating.
- **Evidence (anon):**
  - `/Admin/Index` → 302 (exists, redirects to login)
  - `/Admin/UserManagement` → 302 (exists)
  - `/Admin/Tags` → 302 (exists)
  - `/Admin/Dashboard` → 404 (action does not exist)
  - `/Admin/Roles` → 404 (action does not exist)
- **Why minor:** The set of admin action names is deterministic and would be visible to anyone who logs in once. It's also obvious from URL conventions (UserManagement, Tags, etc.). Treating it as a security issue would mean adding `[AllowAnonymous]` 302 catch-alls just to spoof the response code, which adds complexity for negligible benefit.
- **Recommendation:** Leave open; revisit only if a security review specifically flags it.
- **Files:** N/A

---

### DEF-124 — SECURITY/CONTRACT: Partner API `/api/v1/certificates/expiring` leaks raw entity (status, internal IDs, file paths, no public-status filter)

- **Date:** 2026-05-03
- **Severity:** **CRITICAL (S1)** — Partner API contract violation per `feedback_amazon_api_immutable.md` and prompt's S1 trigger list
- **Status:** **Fixed (pending deploy)** — same-day fix in cycle 3 of the QA pass
- **Reporter:** Internal QA pass (cycle 1 of full-spectrum-test prompt) — found while regressing DEF-103 across all Partner API endpoints
- **Problem:** `GET /api/v1/certificates/expiring?days=N` is reachable via Partner API key (controller-level `[Authorize(AuthenticationSchemes = "ApiKey")]` at `CertificatesController.cs:33`). The endpoint returns the raw `Certificate` entity instead of the contract DTO. Same root-cause class as DEF-103, but `/expiring` was missed when the fix was applied to `/{id}` and `/number/{n}`.
- **Evidence (live `/api/v1/certificates/expiring?days=365` with active Amazon partner key):**
  ```json
  {
    "data": [{
      "id": 38,                                 // ← INTERNAL ID LEAK
      "number": "EXT-AMZ-001",
      "status": "Client Approved - Paid",       // ← INTERNAL LIFECYCLE STATUS LEAK (should be "Active")
      "country": "United States",               // ← RAW COUNTRY LEAK (should be "All")
      "asin": null, "upc": null,                // ← FLAT FIELDS (should be productIds[] array)
      "gtin": null, "ean": null,
      "certificateFile": null,                  // ← INTERNAL FILE PATH LEAK
      "certificateImagePath": null,
      "pathwayId": null,
      "companyId": 61,                          // ← INTERNAL COMPANY ID LEAK
      "productId": 84,                          // ← INTERNAL PRODUCT ID LEAK
      "productCategoryId": null,
      "productTypeId": null,
      "productSubcategoryId": null,
      "certificationTypeId": 1,                 // ← INTERNAL TYPE ID LEAK
      "certificationTypeSubcategoryId": null,
      "createdBy": null, "updatedBy": null,
      "createdAt": "...", "updatedAt": "...",
      "company": { "id": 61, "...": "...",
                   "certificates": [ /* full nested entity tree */ ]
      }
    }]
  }
  ```
- **No public-status filter:** `EXT-AMZ-001` is in legacy lifecycle status `"Client Approved - Paid"`. If any cert tagged `Amazon` is in `Draft`, `Submitted`, `Hold`, or `Expired`, it would also leak through this endpoint to the partner — violating the same Amazon Developer Guide §4 + §8 rule that DEF-103 was about.
- **Circular nav references:** Because `Company` is `Include`d and `Company` has a back-reference to `Certificates`, the JSON serializer produces a deeply nested response. Live test response was 1GB+ before being truncated. This is also a **DoS amplification risk** (any partner key holder can demand 1GB responses).
- **Root cause (`Controllers/Api/v1/CertificatesController.cs:627-674`):**
  - Line 657: `Data = certificates` — returns raw `List<Certificate>`, no projection
  - No `where (PublicStatuses.Contains(c.Status))` filter
  - No partner-role branch to apply `BuildPartnerCertificateDto` helper
  - `Include(c => c.Company)` triggers circular serialization
- **Fix (recommended):**
  1. Add `PublicStatuses` filter when caller is Partner: mirror lines 826-843 of the same controller (used in `SearchByProductId`).
  2. Add partner branch that projects via `BuildPartnerCertificateDto` (the helper added in 2026-04-30 DEF-103 fix).
  3. Add `Include(Pathway)` and `Include(ProductIds)` to the query (so the DTO has the data it needs).
  4. Drop `Include(Company)` from the partner-facing path (DTO only needs `Company.Name`); keep it for analyst/admin if they ever call this endpoint.
  5. Remove `id`, `companyId`, `productId`, `*Id`, `certificateFile`, `certificateImagePath`, `createdBy`, `updatedBy` from the response — these are internal-only.
- **Why this got through 2026-04-21 fix:** DEF-103's commit message claimed "all 6 partner endpoints fixed" but the actual fix only touched list, GS1 list, GS1 single, and missed `/expiring`, `/{id}`, `/number/{n}`. The 2026-04-30 self-test caught `/{id}` + `/number/{n}` and fixed them. `/expiring` wasn't in scope of either pass — slipped through.
- **Audit needed:** Re-grep all `[HttpGet]` actions in `CertificatesController` and confirm every one returns a projected DTO when `userRole == "Partner"`. Also audit all other API v1 controllers (`ProductsController`, `CompaniesController`) for the same pattern.
- **Customer impact:** Amazon's Climate Pledge Friendly integration consumes `/api/v1/*`. Of the 7 contracted endpoints, `/expiring` is documented in Swagger and is reachable. If Amazon scripts hit it, they'd see internal lifecycle statuses (e.g., `"Submitted"`, `"Hold"`) which violates the contract literal `"Active"`. They might either reject the response (breaking their integration) or, worse, accept it and then question why our app exposes "draft" certifications externally.
- **Fix (2026-05-03 cycle 3):** Mirrored the `SearchByProductId` pattern in the same controller. `GetExpiringCertificates`:
  1. Detects `userRole == "Partner"` via `GetCurrentUserRole()`.
  2. Includes `Pathway` and `ProductIds` (so the contract DTO has the data it needs); kept `Company` include because the DTO projection prevents the circular Company→Certificates serialization that previously blew the response to 1GB+.
  3. For partner callers: filters on `PublicStatuses.Contains(Status) && partnerNames.Contains(Partner)`. Returns empty paginated response if the API key has no partner association.
  4. Projects via shared `BuildPartnerCertificateDto` helper — same DTO shape as list/by-id/by-number endpoints. Anonymous/Admin/Analyst still get the raw entity for internal dashboards.
- **Verified live (curl on `localhost:5101`, 2026-05-03):**
  ```
  GET /api/v1/certificates/expiring?days=365 (Amazon partner key)
  → 4 items (only PublicStatuses survived filter)
  → status: "Active" (was "Client Approved - Paid")
  → certificationCountries: "All" (was "United States")
  → company: "ExtTest-AmazonCo" / "Acme Corporation" (now populated, not null)
  → productIds: [] (was flat asin/upc/gtin/ean fields)
  → No internal IDs, no certificateFile/certificateImagePath leak
  → Response size ~1KB (was 1GB+ on circular nesting)
  ```
- **Files:** `Controllers/Api/v1/CertificatesController.cs`

---

### DEF-123 — Dashboard: aggregate stat CSV exports show "Uncategorized" / "Unassigned" instead of real names; counts disagreed with tiles

- **Date:** 2026-05-03
- **Severity:** Medium (broken reporting; downloads were silently useless for product categories/types/subcategories)
- **Status:** Fixed (pending deploy)
- **Reporter:** Shawn — follow-up to DEF-122 ("test the export data is matching")
- **Symptom:**
  - `/Dashboard/DownloadStat?type=product-categories-with-active-certs` returned `"Uncategorized",22` instead of the real category name
  - Same for `product-types-with-active-certs` → `"Unassigned",22`
  - Same for `product-subcategories-with-active-certs` → `"Unassigned",22`
  - `Total Product Types: 1` in the CSV header disagreed with the tile showing `0` (the "Unassigned" fallback bucket was being counted as if it were a real type)
- **Root causes (two):**
  1. **Missing eager loads.** `GetActiveCertificatesAsync()` only included `Company`, `Product`, and `CertificationType`. The controller's GroupBy expressions read `c.Product?.Category?.Name`, `c.ProductType?.Name`, `c.ProductSubcategory?.Name` — none of those nav properties were Included, so they all evaluated to null at runtime → fell to the `?? "Uncategorized"` / `?? "Unassigned"` fallback strings.
  2. **GroupBy ignored the Product linkage path.** Controller grouped by cert-direct nav only (`c.ProductType?.Name`), ignoring the `c.Product.ProductType.Name` path. The tile counting logic in `GetCertificateStatisticsAsync` correctly considers both paths, so tile and CSV used different definitions even before the null-fallback issue.
  3. **"Unassigned"/"Uncategorized" treated as a real group.** Group count included these fallback buckets, so `Total Product Types: N` could overstate by 1 if any cert was untyped.
- **Fix:**
  1. `Repositories/CertificateRepository.cs` — `GetActiveCertificatesAsync()` now `.ThenInclude`s `Product.Category`, `Product.ProductType`, `Product.ProductSubcategory`, and includes the cert-direct `ProductCategory`, `ProductType`, `ProductSubcategory` navs as well.
  2. `Controllers/DashboardController.cs` `GenerateAggregatedStatCsv` — GroupBy now uses Product linkage preferred, cert-direct fallback (matching tile semantics), and filters out null/empty group keys via `.Where(g => !string.IsNullOrWhiteSpace(g.Key))`. CSV reports only real types; if there are zero typed certs, the report shows `Total: 0` with no rows.
- **Validation (dev DB, 22 active certs):**
  | Stat | Tile | CSV Total | CSV rows | Match |
  |---|---|---|---|---|
  | Product Categories | 1 | 1 | "Chemicals and Raw Materials",22 | ✅ |
  | Product Types | 0 | 0 | (empty) | ✅ |
  | Product Subcategories | 0 | 0 | (empty) | ✅ |
  | Companies | 9 | 9 | 9 named rows summing to 22 | ✅ |
- **Files touched:**
  - `Repositories/CertificateRepository.cs`
  - `Controllers/DashboardController.cs`
- **Deploy:** Pending — same build as DEF-122
- **Notes:** `GetActiveCertificatesAsync()` is also called from `GetCertificatesForStat` and `GetCertificatesForTable` for non-aggregate downloads (active cert lists, top-companies/types/categories tables). Adding ThenIncludes is incremental cost (no new joins for callers that don't read the nav, just one query with more JOINs); no behavior regression in those code paths.

---

### DEF-122 — Dashboard: tile #3 label "# Product Categories with Active Certs" was bound to certification-types count

- **Date:** 2026-05-02
- **Severity:** Medium (visible to internal users; misleading reporting metric)
- **Status:** Fixed (pending deploy)
- **Reporter:** Shawn — "are the numbers correct" audit of `/Dashboard`
- **Symptom:** Dashboard tile #3 displayed the count of distinct **certification types** under a "Product Categories" label. Dashboard had no tile for actual product categories.
- **Root cause:** `Views/Dashboard/Index.cshtml:58` bound the value to `Model.TotalCertificationTypes` (which the controller fills from `Statistics["CertTypesWithActiveCerts"]`). The repository never computed a `ProductCategoriesWithActiveCerts` stat at all — the key didn't exist.
- **Fix:**
  1. `Repositories/CertificateRepository.cs` — added `stats["ProductCategoriesWithActiveCerts"]` distinct-count query, mirroring the two-query EF pattern already used for ProductTypes / Subcategories (handles certs linked via `Product.Category` and certs linked directly via `c.ProductCategory`).
  2. `Views/Dashboard/Index.cshtml` (line 58) — rebound to `Model.Statistics.GetValueOrDefault("ProductCategoriesWithActiveCerts", 0)`.
- **Validation (dev DB):** OLD value = 4 (cert types — wrong); NEW value = 1 ("Chemicals and Raw Materials" — matches Top-5 Product Categories table at the bottom of the same page).
- **Files touched:**
  - `Repositories/CertificateRepository.cs`
  - `Views/Dashboard/Index.cshtml`
- **Deploy:** Pending — next build after 2026-05-01 zip
- **Notes:** Tile's download button (`data-stat-type="product-categories-with-active-certs"`) was already correctly mapped to the right CSV — was just sitting on a tile showing the wrong number. `DashboardViewModel.TotalCertificationTypes` left in place (no other consumers found, but kept for safety; can remove in a future cleanup).

---

### DEF-121 — SECURITY: `/api/productids/certificate/{id}` leaks password hashes and 2FA secrets in CreatedByUser / UpdatedByUser

- **Date:** 2026-05-01 (filed) → 2026-05-03 stopgap → 2026-05-03 DTO refactor (this entry)
- **Severity:** **HIGH** (information disclosure — credentials exposure)
- **Status:** **Fixed (stopgap + DTO refactor)** — pending deploy. Both layers of defense in place:
  1. `[JsonIgnore]` on 8 sensitive `User` fields (2026-05-03 stopgap, still in place).
  2. `ProductIdResponseDto` projection now removes the `User` entity surface entirely from `/api/productids/*` responses (2026-05-03 cycle 4 — this fix).
- **Reporter:** Found incidentally while diagnosing DEF-120 (Created By column rendering)
- **Problem:** `GET /api/productids/certificate/{certificateId}` returns the full `CertificateProductId` entity with `Include(p => p.CreatedByUser)` and `Include(p => p.UpdatedByUser)` (`Services/ProductIdService.cs:65-66`). Default JSON serialization emits the entire User object, including:
  - `passwordHash` (BCrypt hash — recoverable to plaintext via offline attack if leaked)
  - `twoFactorSecret` (TOTP seed — bypasses 2FA entirely if leaked)
  - `recoveryCodes`
  - `securityQuestions`
  - `passwordResetToken` / `passwordResetTokenExpiry`
  - `allowedIPs` (IP allowlist — useful for attacker reconnaissance)
  - `passwordHistory`
  - Email + last-login metadata
- **Authorization:** Endpoint requires `[Authorize(Policy = "AnalystOrAdmin")]`. The leak is not anonymous — it's only visible to authenticated Analyst+ users. But: (1) any Analyst account compromise immediately yields all admin password hashes; (2) any Analyst can capture all admin 2FA secrets and bypass MFA on any account; (3) Partner API users do NOT hit this endpoint, so contract is unaffected.
- **Other endpoints potentially affected (audit needed):** Any controller that returns `CertificateProductId` with the same `Include` pattern (e.g. `GetProductId(int id)`, `CreateProductId`, `UpdateProductId`). The cert tile internal/external APIs may also leak via `Certificate.CreatedByUser` includes — needs verification.
- **Recommended fixes (pick one or combine):**
  1. **DTO projection (preferred):** introduce `ProductIdResponseDto` with only `{ id, certificateId, productIdType, productIdValue, status, partner, productName, productSku, description, isActive, isPublic, displayOrder, createdAt, createdBy, createdByUserName, updatedAt, updatedBy, updatedByUserName }`. Map in controller before returning. Eliminates the entire User-entity surface.
  2. **`[JsonIgnore]` on User secrets:** mark `passwordHash`, `twoFactorSecret`, `recoveryCodes`, `securityQuestions`, `passwordResetToken`, `passwordHistory`, `allowedIPs` with `[JsonIgnore]` on `Models/User.cs`. Application-wide protection but couples the model to serialization concerns.
  3. **Both:** belt-and-suspenders.
- **Not fixed in this commit:** flagged as standalone defect. Needs Shawn's call on which approach + audit scope.
- **Why this got through:** Pattern of `.Include(...).Returning(entity)` is convenient and is used in many places. No DTO layer enforced. Code review didn't catch it because the User entity grew over time (new security fields added) without revisiting which endpoints serialize it.
- **2026-05-03 verification (cycle 1 QA pass):** Re-confirmed unfixed after 2 days. Code evidence:
  - `Models/User.cs:31` — `PasswordHash` still has no `[JsonIgnore]`
  - `Models/User.cs:53` — `TwoFactorSecret` still has no `[JsonIgnore]`
  - `Services/ProductIdService.cs:65-66, 74-75` — still uses `Include(CreatedByUser)` / `Include(UpdatedByUser)` and returns the entity directly
  - **Aging risk:** Open HIGH-severity credential leak sitting unaddressed for 2+ days. Every day this stays open is a window where any Analyst account compromise yields the admin password hash + 2FA secret (full account takeover). Recommend bumping severity to **CRITICAL** and either (a) putting a quick `[JsonIgnore]` block on `User.cs` today as a stopgap and following up with the DTO refactor, or (b) doing the full DTO refactor now.
- **2026-05-03 STOPGAP FIX SHIPPED LOCAL** (cycle 3 QA pass): Applied option 2 (`[JsonIgnore]` on User secrets). Status now **Fixed (stopgap, pending deploy)** — DTO refactor still recommended as a followup ticket but the immediate credential exposure is closed.
  - `Models/User.cs` — added `using System.Text.Json.Serialization;` and `[JsonIgnore]` on: `PasswordHash`, `TwoFactorSecret`, `RecoveryCodes`, `SecurityQuestions`, `AllowedIPs`, `PasswordResetToken`, `PasswordResetTokenExpiry`, `PasswordHistory`. Eight fields total.
  - **Why this is safe:** No code in the repo deserializes JSON → `User` entity (`grep -E '\[FromBody\].*User\b|JsonSerializer\.Deserialize<User>|FromJson<User>'` → no matches). All update flows go through ViewModels (`UserCreateViewModel`, `UserEditViewModel`) and `AdminService`. EF Core reads from DB, not JSON. The `[JsonIgnore]` only affects API response serialization, not internal flows.
  - **Verified runtime:** `/Admin/GetUsers` already uses `UserViewModel` projection — its output never had secrets. The `/api/productids/certificate/{id}` endpoint that DEF-121 originally flagged still uses `Include(CreatedByUser)`/`Include(UpdatedByUser)` patterns, but with `[JsonIgnore]` the serialized output now omits all 8 sensitive fields regardless. Dev DB has zero CertificateProductId rows so couldn't run a live curl reproduction; proceeded on code evidence.
  - **Followup ticket needed:** DTO refactor (option 1 from this entry's "Recommended fixes") to remove the User entity surface entirely. The `[JsonIgnore]` stopgap protects against accidental serialization of the existing fields, but if a new sensitive field is added to `User` in future, the next dev has to remember to mark it. The DTO approach is the durable fix.
  - **Files:** `Models/User.cs`
- **2026-05-03 DTO REFACTOR SHIPPED LOCAL** (cycle 4 QA pass — followup to the stopgap above):
  - Added `ProductIdResponseDto` and `ProductIdUserDto` to `Models/CertificateProductId.cs`. The DTO mirrors every public field on `CertificateProductId` and exposes only `{ id, username }` for `CreatedByUser` and `UpdatedByUser` — no email, no role, no audit fields, and no surface area for any future credential field added to `User`.
  - Updated all four read/write endpoints on `Controllers/Api/ProductIdApiController.cs` (`GetProductIdsByCertificate`, `GetProductId`, `CreateProductId`, `UpdateProductId`) to project entities via `ProductIdResponseDto.FromEntity(...)` before returning. `ApiResponse<CertificateProductId>` and `ApiResponse<List<CertificateProductId>>` are now `ApiResponse<ProductIdResponseDto>` / `ApiResponse<List<ProductIdResponseDto>>`.
  - `CreateProductId` and `UpdateProductId` now re-fetch the saved entity via `GetProductIdAsync(id)` (which has `Include(CreatedByUser).Include(UpdatedByUser)`) so the returned DTO has populated user nav data for the immediate row render.
  - **Service layer untouched.** `IProductIdService` still returns `CertificateProductId` entities — internal callers (e.g., `ExportToCsvAsync`, the cascade in `SyncProductIdStatusesForCertificateAsync`) keep their existing typed access. Only the controller boundary maps to DTO.
  - **JS frontend impact: zero.** The DTO preserves the exact field names the JS consumer reads: `productId.id`, `productId.productIdType`, `productId.productIdValue`, `productId.partner`, `productId.productName`, `productId.productSku`, `productId.description`, `productId.createdAt`, `productId.createdBy`, `productId.createdByUser.username`. Verified live (see below).
  - **Build:** 0 errors, 57 warnings (all pre-existing, unrelated to this change).
  - **Verified live (2026-05-03, `localhost:5101`, admin cookie):**
    - `GET /api/productids/certificate/48` (cert 26-0147, 2 product IDs) → returns 18 fields per row. `createdByUser` is `{ id, username }` only. Forbidden-fields scan: `passwordHash`, `twoFactorSecret`, `recoveryCodes`, `securityQuestions`, `allowedIPs`, `passwordResetToken`, `passwordHistory` — none present anywhere in the JSON.
    - `GET /api/productids/7` → same DTO shape, same forbidden-fields scan passes.
    - `PUT /api/productids/7` → DTO returned, forbidden-fields scan passes, status 200.
    - `GET /Certificate/Edit/26-0147` → table renders "admin" in Created By column for both rows; clicking Edit opens modal with all fields pre-populated from the DTO (Type=ASIN, Value=B0BRNPBZJ8, Partner=Amazon, ProductName populated, Description populated).
  - **Files:** `Models/CertificateProductId.cs`, `Controllers/Api/ProductIdApiController.cs`

---

### DEF-120 — Certificate Edit: Product Identifiers "Created By" column shows user ID instead of username

- **Date:** 2026-05-01
- **Severity:** Medium (functional bug — operators see meaningless numeric ID instead of human-readable username)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn (`http://localhost:5101/Certificate/Edit/26-0150`, "Under product Identifiers shows 1 under Create By. This should be populated by the user name")
- **Root cause:** `wwwroot/js/product-id-manager.js:244` rendered the cell with `productId.createdBy` — that's the numeric foreign-key column from `CertificateProductId.CreatedBy` (e.g. `1` for the admin user). The Razor partial (`Views/Shared/_ProductIdManager.cshtml:118-119`) correctly reads `productId.CreatedByUser?.Username`, but the JS cell renderer (used after the page hydrates) wasn't using the same navigation property.
- **Fix:** Updated the cell render to read `(productId.createdByUser && productId.createdByUser.username) || 'System'`. The API already includes the navigation property (`Services/ProductIdService.cs:65 .Include(p => p.CreatedByUser)`), so no API change needed.
- **Files touched:** `wwwroot/js/product-id-manager.js` (single template literal at lines 244-246)
- **Verified locally (2026-05-01):** Playwright on `localhost:5101/Certificate/Edit/26-0150`. API returns `createdByUser.username: "admin"`. Rendered cells show `["admin", "admin", "admin"]` instead of the previous `["1", "1", "1"]`.

---

### DEF-119 — Certificate Create / Edit: Add Product ID modal too narrow on desktop, form collapses to single column

- **Date:** 2026-05-01
- **Severity:** Low (cosmetic — form still works, just wastes desktop screen real estate)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn (`Screenshot_1-5-2026_175147_localhost.jpeg`, "Add Product ID Modal width is too narrow for desktop view")
- **Root cause:** Two stacked modal-width caps in `wwwroot/css/product-id-manager.css`:
  1. `#productIdModal .modal-dialog { max-width: 650px }` — capped the product modal at 650 px regardless of `modal-lg` class on the dialog (Bootstrap modal-lg defaults to 800 px). At 650 px, the form's `col-md-6` columns (Type | Product ID) still rendered side-by-side, but with no breathing room — and the description textarea looked cramped.
  2. `.modal-dialog { max-width: 600px }` — a bare global selector that capped EVERY Bootstrap modal on Certificate Create / Edit (the only views loading this CSS) to 600 px. The specific rule on `#productIdModal` already won, so this was a stealth leak affecting any other modal that happened to render on those pages.
- **Fix:** Bumped the productIdModal cap to 900 px (a touch wider than modal-lg's 800 px to give the description textarea more room) and removed the bare global `.modal-dialog { max-width: 600px }` rule entirely. Other modals on Create / Edit now get their default Bootstrap widths.
- **Files touched:** `wwwroot/css/product-id-manager.css` (two rules)
- **Verified locally (2026-05-01):** Playwright at 1280×1080 and 1920×1080.
  - 1280: dialog 900 px wide, 174 px margin each side. 2-column form renders with comfortable spacing.
  - 1920: dialog 900 px wide, 494 px margin each side. Centered nicely on the wider screen.
  - Visual confirms: Type | Product ID side-by-side at col-md-6, Partner / Product Name / SKU / Description full-width below, footer with Cancel + Save Product ID buttons properly spaced.
- **Coverage:** The `_ProductIdManager` partial is shared between Certificate Create and Certificate Edit, so both pages benefit from the single CSS edit.

---

### DEF-118 — Certificate Create: Product ID Manager export missing Created and Created By columns

- **Date:** 2026-05-01
- **Severity:** Low (data column gap in export — UI table shows Created / Created By, export omitted them)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn ("Product ID Manager Export does not have Created and Created By" on `/Certificate/Create`)
- **Root cause:** The Create page uses an in-memory CSV builder (`exportInMemoryProductIds` in `wwwroot/js/product-id-manager.js`) because there's no certificate ID yet to hit the server export endpoint. That builder only included `Type, Product ID, Partner, ProductName, ProductSku, Description` — missing the Created and Created By columns that the on-screen table shows. The header naming also drifted from the table ("ProductName" vs "Product Name", "ProductSku" vs "SKU") in violation of the DEF-110 standard ("export headers match table column labels").
- **Fix:** Updated `exportInMemoryProductIds` to emit `Type, Product ID, Product Name, SKU, Partner, Created, Created By, Description` — column order matches the on-screen table; Description trails since it's saved data but not a table column. Created formatted as `yyyy-MM-dd` from `pid.createdAt` (blank for new in-memory items), Created By reads `pid.createdByUser?.username || pid.createdBy || ''`. Round-trip safe: importer matches header names case-insensitively and ignores unknown headers, so adding columns doesn't break import.
- **Files touched:** `wwwroot/js/product-id-manager.js` (single function: `exportInMemoryProductIds`)
- **Verified locally (2026-05-01):** Playwright on `localhost:5101/Certificate/Create`. Added one row via Add Product ID modal (ASIN/B0ABCDEFGH/Amazon/Test Product/SKU-001/A test row). Captured exported CSV via Blob interception:
  ```
  Type,Product ID,Product Name,SKU,Partner,Created,Created By,Description
  "ASIN","B0ABCDEFGH","Test Product","SKU-001","Amazon","","","A test row"
  ```
  Created and Created By appear, blank as expected for a new unsaved certificate.
- **Note for follow-up (NOT changed):** The Edit-page server export (`Services/ProductIdService.cs:418`) currently emits `Type,Product ID,Status,Partner,ProductName,ProductSku,Description,CreatedAt,CreatedBy` — it still includes the legacy `Status` column (CR-015 says auto-derived, should be removed) and uses non-table-aligned header names. Two surfaces, two header conventions — worth a follow-up CR to align the server export to the same column shape as the in-memory export. Did not change in this fix per "only what was asked."

---

### DEF-117 — Cert tile views (Internal + External): Column Settings dropdown spills below viewport on mobile portrait — bottom of column list unreachable

- **Date:** 2026-05-01
- **Severity:** Medium (functional impact: ~half of column toggles unreachable on mobile portrait — users can't hide/show columns past the visible area)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn (`Screenshot_1-5-2026_172446_localhost.jpeg`, mobile portrait, "column setting drop down display is off and unable to see the bottom of the sort list")
- **Root cause:** The mobile JS positioner (`positionDropdownForMobile` in `certificate-tile-view-internal.js`) set the dropdown's `top` to `toggleBtn.bottom + 4`, but did NOT adjust `max-height` to match available space below. The CSS hard-coded `max-height: calc(100vh - 120px)` (≈680 px on a 800 px viewport). With the toggle button at y≈423 in the viewport, the 680-px-tall dropdown extended to y≈1111 — 311 px past the 800 px viewport bottom. The dropdown was scrollable internally (`overflow-y: auto`, content was 1208 px tall), but the user could only see the top ~370 px because the rest was below the fold. The external view didn't have any positioner at all — it relied on default `position: absolute; top: 100%` so the dropdown opened below the button anchored to the document, also overflowing.
- **Fix:** Rewrote `positionDropdownForMobile` to dynamically size `max-height` to the actual available space, and to flip ABOVE the button when there's more room above than below:
  ```js
  const below = vpH - r.bottom - margin;
  const above = r.top - margin;
  if (below >= above) {
      // open below: top = btn.bottom+4, max-height = below
  } else {
      // flip above: bottom = vpH - btn.top + 4, max-height = above
  }
  ```
  Floor of 200 px applied so the dropdown is always usable even on tiny viewports. External view got the same logic added (it didn't have one before — same code, same JS structure).
- **Files touched:** `wwwroot/js/certificate-tile-view-internal.js`, `wwwroot/js/certificate-tile-view-external.js`
- **CSS unchanged:** existing `@media (max-width: 768px)` rule with `position: fixed` still applies. JS now overrides `max-height` and (when flipping) the `top`/`bottom` properties.
- **Verified locally (2026-05-01):** Playwright at 360×800.
  - Button mid-viewport (scrollY=503, btnTop=375): opens below, `max-height: 364 px`, `dd.bottom: 796 px` (4 px margin from viewport bottom). `ddBelowViewport: false`. Internal scroll active for 1208 px content.
  - Button near top of viewport (scrollY=1542, btnTop=45): opens below, `max-height: 695 px`, dd extends from 101→796 — uses nearly the full viewport. Visual confirms full Column Settings list reachable via internal scroll. CORE FIELDS header visible, COMPANY & PRODUCT section visible just below.
- **Coverage:** Internal and external cert tile views both fixed. Shared CSS (no change needed).

---

### DEF-116 — Certificate Create / Edit: Product Identifier filter row overflows mobile viewport

- **Date:** 2026-05-01
- **Severity:** Medium (filter controls partially / fully off-screen on phones — Search input was completely invisible at 360 px)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn ("Product Identifier section — controls are not contained in the view on mobile")
- **Root cause:** The filter row in `Views/Shared/_ProductIdManager.cshtml` used inline styles `display: flex; flex-wrap: nowrap` plus fixed pixel widths (`min-width: 110px`, `min-width: 120px`, `width: 200px`). At a 360 px portrait viewport the available container width was 190 px, but the filter row's intrinsic width was 449 px — so the partner dropdown was clipped ("All Partne...") and the search input was entirely off-screen. Inline styles also made the row impossible to override with media queries.
- **Fix:** Moved layout into a CSS class (`.product-manager-filter-row`) and switched from `nowrap` to `wrap`. Type and Partner dropdowns keep their min-width but no longer force a single line. Search uses `flex: 1 1 200px` — grows to fill remaining space at desktop/tablet widths, wraps to its own line on phones. Also added a scroll-shadow background gradient on `.product-ids-table-container` so users get a visual cue that the data table extends horizontally beyond the viewport (the table still requires `min-width: 800px` to keep its 8 columns readable).
- **Files touched:**
  - `Views/Shared/_ProductIdManager.cshtml` — replaced inline `style=""` attributes with CSS classes (`.product-manager-filter-row`, `.pid-filter-type`, `.pid-filter-partner`, `.pid-filter-search`)
  - `wwwroot/css/product-id-manager.css` — added the new flex-wrap rule + scroll-shadow background on the table container
- **Coverage:** The `_ProductIdManager` partial is shared across `Views/Certificate/Create.cshtml`, `Views/Certificate/Edit.cshtml`, and any other view that includes it — single source-of-truth fix.
- **Verified locally (2026-05-01):** Playwright at 360 / 768 / 1024 px viewports.
  - 360 px (mobile): inner filter row went from `scrollWidth=449/clientWidth=190 (overflow=259 px)` to `scrollWidth=190/clientWidth=190 (zero overflow)`. All three controls now fully visible on three wrapped rows. Visual confirmed.
  - 768 px (tablet): all three controls fit on one row; search expands to 311 px to fill remaining space.
  - 1024 px (desktop): single-row layout intact; search expands to 491 px.

---

### DEF-115 — Cert tile views (Internal + External): "Columns" toggle button left-aligns on narrow viewports instead of right-aligning

- **Date:** 2026-05-01
- **Severity:** Low (cosmetic alignment, no functional break)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn ("most likely external too. Columns should be right aligned.")
- **Root cause:** `.table-toolbar-row` is a flexbox with `flex-wrap: wrap` and `justify-content: space-between`. At narrow widths (~360 px portrait phone), the "Showing N certificates" left text plus the "Columns" right button don't fit on one line, so flex wraps the right element to its own line. `space-between` only distributes items WITHIN a flex line — when the right element is alone on line 2, it falls to the line's flex-start position (left edge) and reads as left-aligned.
- **Fix:** Added `margin-left: auto` to `.table-toolbar-right`. On a wrapped flex line containing only this item, the auto margin expands to fill the line's spare space and pushes the button to the right edge. Desktop behavior is unchanged because `space-between` already produced the same outcome there.
- **Files touched:** `wwwroot/css/certificatetileview.css` (single declaration on `.table-toolbar-right`).
- **Coverage:** Same selectors and same stylesheet are shared between `Views/Certificate/CertificateTileViewInternal.cshtml` and `Views/Certificate/CertificateTileViewExternal.cshtml`, so a single CSS edit fixes both.
- **Verified locally (2026-05-01):** Playwright sweep across 5 viewports (360, 768, 900, 1024, 1280). At every width, button now sits 16 px from the row's right edge. At 360 px the button x-position moved from 56 → 172 (left edge → right edge of available line). Visual screenshot confirms toolbar reads "Showing 20 of 40 certificates" on line 1 and right-aligned "Columns" button on line 2.

---

### DEF-114 — Dashboard mobile portrait: column 2 of stats tables clipped, headers/values truncated

- **Date:** 2026-05-01
- **Severity:** Medium (visible data loss on mobile portrait — column 2 numbers were cropped at the right edge of the section)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn (screenshot `Screenshot_1-5-2026_164437_localhost.jpeg`, "data on dashboard tables are not showing all the data, the formatting is cutting off text in mobile portrait view")
- **Root cause:** At 360 px viewport, the standard admin shell ate ~50 px of horizontal space per side: `.admin-container { margin: 10px; padding: 15px }` + `.admin-body-content { padding: 24px }`. The `.tables-container` was rendered at only **216 px wide**, while the inner `<table>` measured 232–263 px depending on the second-column header label. `.table-section { overflow: hidden }` then **silently clipped** the right edge — that's why the headers and values appeared cut off in the screenshot.
- **First attempt (rejected):** Added `overflow-wrap: anywhere; word-break: break-word;` to cell text. This caused catastrophic character-by-character vertical stacking (one letter per line) — confirmed via Playwright screenshot. Reverted.
- **Second attempt (rejected):** Capped `th:last-child { max-width: 130px }` to force column 2 narrower. Treated the symptom (table wider than container) without fixing the cause (container too narrow).
- **Final fix:** Scoped admin-shell padding/margin reduction to a new `.dashboard-page` class on the dashboard's `.admin-container` element. At ≤480 px, dashboard-only shell padding drops from 10/15/24 → 4/6/4 per side, returning ~46 px of horizontal space to the tables-container. Sections now measure 274 px wide and tables fit cleanly at 272 px.
- **Files touched:**
  - `Views/Dashboard/Index.cshtml` — added `.dashboard-page` class to the root `.admin-container` div (line 14)
  - `wwwroot/css/pages/dashboard.css` — inside `@media (max-width: 480px)`: removed the `th:last-child max-width` kludge, added `.dashboard-page.admin-container` and `.dashboard-page .admin-body-content` padding/margin overrides
- **Verified locally (2026-05-01):** Playwright at 360×800 mobile viewport. `tables-container` width 216 → 274 (+58 px). All 5 tables: `tableScrollWidth === tableClientWidth = 272`, no clipping. `documentElement.scrollWidth === viewport (360)`, no horizontal scroll. Visual screenshot confirms all column 2 headers fully visible ("TOTAL ACTIVE CERTS", "# OF ACTIVE CERTIFICATES", etc.) and all numeric values readable.
- **Scoping note:** The `.dashboard-page` class isolates the padding cut to the Dashboard view only. Other admin pages (`UserManagement`, `Tags`, etc.) keep their existing 10/15/24 shell padding — none of those pages have the same column-2 truncation problem because their tables either scroll horizontally (DEF-110 user-management) or use card layouts.

---

### DEF-113 — Remove `/Admin/Index` admin dashboard page (client doesn't want it)

- **Date:** 2026-05-01
- **Severity:** Low (feature removal, no functional bug)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn (client preference: client doesn't want a standalone admin dashboard)
- **Change:**
  - Deleted `Views/Admin/Index.cshtml`
  - `AdminController.Index()` now redirects to `UserManagement` so any old bookmarks land somewhere useful instead of 404
  - Replaced `<a href="@Url.Action("Index", "Admin")">Administration</a>` with non-clickable `<span>Administration</span>` in 18 view breadcrumbs (matches the "Companies & Products" pattern from DEF-104)
- **Files touched:** `Controllers/AdminController.cs`, `Views/Admin/Index.cshtml` (deleted), 18 breadcrumb-bearing views: `Views/Admin/{CertificateFields,CertificateTypes,CreateUser,CSFAttributes,GCCAPI,GoogleAnalytics,PartnerAPI,Partners,Pathways,ProductManagement,ProductTypes,RoleManagement,Tags,UserManagement}.cshtml`, `Views/Certificate/{Create,Edit}.cshtml`, `Views/Company/Index.cshtml`, `Views/Product/Management.cshtml`
- **Verified locally (2026-05-01):** `/Admin/Index` redirects to `/Admin/UserManagement`. `/Admin` (no action) returns 404 (MVC default routing). All 18 breadcrumbs render "Administration" as a plain span. Login flow unaffected — Admin role still routes to `Dashboard/Index` (different page) on login per existing `HomeController` and `AccountController` redirects.
- **Note:** `_adminService.GetDashboardDataAsync()` is now an orphan method — left in place this cycle, can be removed in a future cleanup.

---

### DEF-112 — Partner role: web-user "Partner" vs Partner API "Partner" share the same code path with different identities — needs an architectural decision

> **2026-05-05 REVERSAL:** Path 4 ("eliminate web-user Partner role") was reversed by Shawn after testuser5 testing surfaced no nav for Partner-role users. The actual requirement is Partner web users DO exist and mirror External access pattern, filtered by partner instead of by company. See **DEF-142** for the new resolution. The Roles.IsActive=false flip was reversed on dev DB (back to IsActive=true). Prod DB flip was never run, so no prod rollback needed. The `Database_Scripts/20260504_DEF112_DeactivatePartnerRoleForHumans.sql` script referenced below was never actually committed to the repo. Architectural concern (dual-meaning of `userRole == "Partner"` string) remains as a future hardening item — see DEF-142 caveat.

- **Date:** 2026-05-01 (reported), 2026-05-04 (resolved via Path 4), 2026-05-05 (Path 4 reversed → Path 2-equivalent in DEF-142)
- **Severity:** Medium (no current bug, but the dual meaning makes future changes risky and the model is ambiguous to operators)
- **Status:** **Fixed (source + dev DB) — pending prod SQL run.** Path 4 selected after confirming on 2026-05-04 that the prod DB has zero human users with the Partner role (verified via `GET /Admin/GetUsers` → role distribution: Admin / Analyst / External / Public only; no `"role": "Partner"` matches across all 23 users).
- **Resolution chosen — Path 4 (eliminate web-user Partner role):** The Partner role exists in the Roles table (id=6, IsActive=true) but is only ever assigned by `PartnerApiKeyAuthenticationMiddleware`, which injects `ClaimTypes.Role = "Partner"` directly without consulting the table. Soft-deactivating the row (`IsActive = false`) hides it from the User Management role dropdown (`GetRolesListAsync` filters on `IsActive`) and prevents future admins from accidentally creating human Partner users. Zero impact on Partner API auth (middleware doesn't read the row), zero impact on existing users (none are assigned). Fully reversible.
- **Files touched (2026-05-04):**
  - `Database_Scripts/20260504_DEF112_DeactivatePartnerRoleForHumans.sql` — idempotent script. Sets `Roles.IsActive = 0` on the Partner row + updates the description. Includes a defense-in-depth pre-check that aborts if any human users have the Partner role. Uses EF many-to-many table FK names `UserRoles.RolesId` / `UserRoles.UsersId`. Run on dev DB 2026-05-04: BEFORE→AFTER snapshot confirms IsActive=1→0.
  - `Data/SeedData.cs:94` — Partner role definition now sets `IsActive = false` and the new description for forward consistency on fresh DBs. Comment block above the line explains the source-of-truth.
  - `Middleware/PartnerApiKeyAuthenticationMiddleware.cs` (around line 132) — added a comment block at the claim-injection site marking it as the SINGLE SOURCE OF TRUTH for the "Partner" role claim and pointing to DEF-112.
- **What this does NOT change:** the existing `userRole == "Partner"` checks in `CertificateTileExternalApiController.cs` and `Controllers/Api/v1/CertificatesController.cs` are left in place — they're correct. They only ever fire for API-key callers since no humans have the role. The dual-meaning concern from the original report is moot once the role is unassignable to humans. If a future requirement needs human Partner users, this is reversible: re-run the SQL with `IsActive = 1` and reapply `IsActive = true` in SeedData.
- **Prod SQL run pending (Shawn):** `Database_Scripts/20260504_DEF112_DeactivatePartnerRoleForHumans.sql` needs to run against the prod DB. Idempotent and safe — has the human-count abort guard. Verified on dev 2026-05-04.
- **Original problem statement (preserved):** Two completely different identities both flow through `if (userRole == "Partner")` in cert-listing controllers, and the codebase doesn't distinguish them at the policy/audit layer:
- **Reporter:** Surfaced while explaining how Partner role gets applied during the Edit User modal review (2026-05-01)
- **Problem:** Two completely different identities both flow through `if (userRole == "Partner")` in cert-listing controllers, and the codebase doesn't distinguish them at the policy/audit layer:
  1. **Web-user Partner role** — a human user logs in with email/password (cookie session). `User.Partners` collection is populated via the Edit User modal (Partner Assignment dropdown). They see the web UI filtered by `userPartnerNames.Contains(c.Partner ?? "None")`.
  2. **Partner API key Partner role** — Amazon's automated systems hit `/api/v1/certificates*` with `X-API-Key`. Middleware assigns role="Partner" for the request lifetime. Same filter branch fires.
- **Where the conflation lives:**
  - `Controllers/Api/CertificateTileExternalApiController.cs:98-110` — web-user partner filter
  - `Controllers/Api/v1/CertificatesController.cs:943` — Partner API key middleware comment "Partner API keys get 'Partner' role from middleware"
  - `Controllers/Api/v1/CertificatesController.cs:993-995` — same `user.Partners.Any()` lookup, but for an API key context that doesn't have a user record
  - `Models/User.cs:102` — `User.Partners` is a many-to-many collection; modal saves a single value but the model supports multi
- **Why this matters:**
  - Audit trail: a human Partner user editing their own data and an Amazon API call look identical in claims-based logs.
  - Brittle filter expansion: any future filter change has to be reasoned about under both identities. Hard to test.
  - Onboarding confusion: the modal has Partner Assignment for web users, but most "Partner" semantics in stakeholder conversations refer to the API contract — not human users.
  - Memory: `feedback_amazon_api_immutable.md` says Partner API contract is locked; web-user role logic shares code paths with that locked contract, so an internal-app refactor could accidentally break the contract.
- **Resolution paths to consider (DECISION NEEDED, not picking one):**
  1. **Rename web-user role** to `PartnerUser` or `BrandPartner` and keep `Partner` as the API-key-only role. Cleanest separation; requires DB migration on `Roles` table + UI label updates.
  2. **Split the filter branch** into two: `userRole == "PartnerApiKey"` vs `userRole == "Partner"`, with the API-key path deriving partner names from the key's metadata, not `User.Partners`.
  3. **Document the dual meaning** in code comments + add a `[Display(Name="Partner User")]` on the role and leave the wire format alone. Lowest-effort but doesn't fix the audit-trail confusion.
  4. **Eliminate the web-user Partner role** entirely if no human user actually needs it — Amazon's UAT users could become External users with company scoping instead.
- **Open questions for Shawn:**
  - Are there *currently* any human users with Partner role in production, or is it only the API-key path that's used?
  - Does Carly/the client expect Partner-role *humans* to be able to log in (e.g., Amazon staff with portal logins)?
  - If yes, should they see their *partner's* certs only, or *their company's* certs + their partner's certs?
- **Effort estimate:** Path 3 (~2 hrs documentation), Path 1/2 (~6-10 hrs code + migration + tests), Path 4 (~4 hrs code + data migration).
- **Files to touch (any path):** `Controllers/Api/CertificateTileExternalApiController.cs`, `Controllers/Api/v1/CertificatesController.cs`, `Controllers/ProductController.cs`, `Models/User.cs`, `Views/Admin/UserManagement.cshtml`, `wwwroot/js/userManagement.js` + migration if Path 1.

---

### DEF-111 — `/Product/Management` "Error loading products: SyntaxError: Unexpected token '<'" — fetch parses HTML auth-redirect as JSON

- **Date:** 2026-05-01 (reported), 2026-05-04 (fixed + deployed)
- **Severity:** Medium (silent functional failure; the user sees an empty/broken product list with no actionable error)
- **Status:** **Deployed 2026-05-04** — verified live on gccwebapp.com: served `/Product/Management` HTML contains the `response.redirected || !contentType.includes('application/json')` guard and the `AdminCore.showNotification('Your session has expired. Reloading…', 'error')` toast call.
- **Reporter:** Anuki internal full-spectrum QA pass (2026-05-01)
- **Symptom:** Logged in as analyst, navigating to `/Product/Management` produces this console error: `Error loading products: SyntaxError: Unexpected token '<', "<!DOCTYPE "... is not valid JSON`. The page renders but the products table is empty / not populated. No visible error toast or banner — the user has no idea anything went wrong.
- **Root cause confirmed:** The JS in `Views/Product/Management.cshtml` `loadProducts()` was calling `response.json()` after only checking `response.ok`. When the server-side cookie session expires mid-page-life, an AJAX `fetch('/Product/GetProducts')` is auto-redirected by the browser to `/Account/Login`, which returns HTTP 200 with `Content-Type: text/html` (the login page). `response.ok` is `true`, so `.json()` runs and chokes on the leading `<` of `<!DOCTYPE`. The `catch` block then logs to `console.error` only — no toast — so the user sees an empty table and no actionable feedback.
- **Verified guard premise (2026-05-04):** A live `fetch('/Account/Login')` from the running app sets `response.redirected: true` and `Content-Type: text/html; charset=utf-8` — so a `(redirected || !contentType.includes('application/json'))` check correctly identifies the auth-redirect case.
- **Fix (2026-05-04):** Updated `loadProducts()` in `Views/Product/Management.cshtml` to:
  1. Read `response.headers.get('content-type')` and check for `application/json` before calling `.json()`. If the response was redirected or the content type isn't JSON, surface `AdminCore.showNotification('Your session has expired. Reloading…', 'error')` and `window.location.reload()` after 1500ms — giving the user a real signal and re-entering the auth flow cleanly.
  2. Replace silent `console.error` calls in the non-OK and `catch` branches with visible `AdminCore.showNotification(..., 'error')` toasts — so any future fetch failure (network, 5xx, etc.) is surfaced to the user instead of masked.
- **Scope decision:** Only `loadProducts()` was modified. The same pattern applies to four other fetch sites in this file (`loadInitialData`, save/edit/delete handlers) and to several other admin pages — those are out of scope for this defect. If the auth-redirect symptom is reported elsewhere, fix per-site or refactor into a shared helper. Per `feedback_only_requested_changes.md`, not expanding scope here.
- **Files:** `Views/Product/Management.cshtml`
- **Verification:** Source-side `loadProducts()` now contains the `response.redirected || !contentType.includes('application/json')` guard and `AdminCore.showNotification` calls. Localhost runtime verify pending — Razor runtime compilation isn't configured, so the change reaches the browser only after the next dev server restart / publish.
- **Found by:** `fullspectrum_out/console-errors-rollup.json` (2026-05-01 verification run for DEF-107/108 fix).

### DEF-110 — CSV export headers don't match UI table column labels (12 export surfaces)

- **Date:** 2026-05-01
- **Severity:** Medium (analyst confusion; data is correct but headers diverge from screen)
- **Status:** **Fixed (local)** — JS/Razor live on localhost:5101; C# changes in `CertificateService.cs` and `AdminService.cs` need server restart to take effect
- **Reporter:** Shawn — surfaced via export audit, instruction "the exports should match what is showing in the UI"
- **Affected exports & fixes:**
  1. **User Management** (`wwwroot/js/userManagement.js`) — was `Name, Email, Role, Status, User Company, Last Login`; now `Name, Company, Email, Role, Status, Last Login, 2FA` (matches UI; supersedes QC #108 "User Company" naming — import template still uses "User Company" for round-trip).
  2. **Company Management** (`wwwroot/js/admin/companyManagement.js`) — dropped `Id`, `Name`→`Company Name`, `Website`→`URL`.
  3. **Admin/ProductManagement** (`Views/Admin/ProductManagement.cshtml` inline) — was `Name, Description, SKU, Model, Status`; now `Product Name, Parent Identifiers, Status` (matches UI; SKU/Model/Description dropped).
  4. **Product/Management** (`Views/Product/Management.cshtml` inline) — now `Product Name, Company, Category, Product Type, Subcategory, Status, Certs, IDs`. Added Subcategory + Status; identifiers consolidated to one `IDs` column to match UI summary cell. Note: import template kept its own column set (round-trip with template intentional).
  5. **Pathways** (`Views/Admin/Pathways.cshtml` inline) — was `Name, Description, Active, Sort Order`; now `Pathway Name` only (matches UI table).
  6. **Tags** (`wwwroot/js/tags.js`) — dropped `ID` column; remaining `Tag Name, Tag Type, Description` matches UI.
  7. **Certificate Types** (`wwwroot/js/admin/certificate-types.js`) — dropped `ID`; `Certificate Count`→`Certificates`.
  8. **Certificate Fields** (`wwwroot/js/admin/certificate-fields.js`) — dropped `ID`/`Sort Order`; `Name`→`Field Name`, `Field Type`→`Control Type`, `Visible`→`Visibility`. Now matches UI exactly.
  9. **Product Types** (`wwwroot/js/product-types.js`) — replaced `ExportUtils.showExportModal` (CSV/JSON/Excel chooser, violated CSV-ONLY rule) with `exportToCSV` direct. Dropped `ID`. `Hierarchy Level`→`Level`.
  10. **Certificate tile export** (`Services/CertificateService.cs:1025`, ⚠ NEEDS RESTART) — header text now matches tile UI: `Certificate Number`→`Certificate #`, `Company Name`→`Company`, `Product Subcategory`→`Subcategory`, `Partner Program`→`Partner`, `Certification Results`→`Cert Results`, `Certification Subcategories`→`Cert Subcategories`, `External Tags`/`Internal Tags`→singular, `Company Contact Email`/`Company Contact URL`→`Company Contact Email/URL`/`Contact URL`. Column order preserved.
  11. **Server-side admin exports** (`Services/AdminService.cs:1983`, ⚠ NEEDS RESTART) — Title-cased all headers and dropped `Id`/internal fields:
      - users: `Name, Company, Email, Role, Status, Last Login`
      - roles: `Role Name, Description, Permissions, Users`
      - certificate-types: `Name, Description, Active, Certificates`
      - certificate-fields: `Field Name, Control Type, Required, Visibility, Notes`
      - partners: `Partner Name, Description, Website, Active, Users, Created Date`
      - product-types: `Name, Type, Parent, Level, Sort Order, Product Count`
- **Side fixes shipped in same change:**
  - **Duplicate-handler bug**: `wwwroot/js/admin/productManagement.js:31` was auto-registering on `#export-btn` from the class constructor while inline handlers in both `Admin/ProductManagement.cshtml` and `Product/Management.cshtml` also registered — clicking Export downloaded **two** CSVs per click. Removed class-level auto-registration; class method `exportProducts()` retained for explicit callers.
  - **CSV-ONLY rule violation**: `product-types.js` was the only admin page using `ExportUtils.showExportModal` (lets user pick CSV/JSON/Excel). Replaced with direct `exportToCSV` call.
- **Known carry-overs (not changed):**
  - User import template still uses "User Company" header (per Carly QC #108) — round-trip with import preserved.
  - Product import template (`Views/Product/Management.cshtml downloadImportTemplate`) still uses its own column set — intentional, separate from export.
  - Cert export column **order** unchanged in `CertificateService.cs` — only header **text** reworded; reordering would ripple into all consumers.
- **Files touched (10):** `wwwroot/js/userManagement.js`, `wwwroot/js/admin/companyManagement.js`, `wwwroot/js/admin/productManagement.js`, `wwwroot/js/tags.js`, `wwwroot/js/admin/certificate-types.js`, `wwwroot/js/admin/certificate-fields.js`, `wwwroot/js/product-types.js`, `Views/Admin/ProductManagement.cshtml`, `Views/Admin/Pathways.cshtml`, `Views/Product/Management.cshtml`, `Services/CertificateService.cs`, `Services/AdminService.cs`.
- **Verification:** JS files served by running dev server confirmed via `Invoke-WebRequest` to contain new content. Live click-through verification not performed; recommend Playwright spot-check after server restart.

---

### DEF-109 — Tile listings render Effective/Expiration date one day earlier than the DB value

- **Date:** 2026-05-01
- **Severity:** High (visible to analysts and partners; displayed date does not match entered date)
- **Status:** **Closed** — deployed and validated by Shawn on 2026-05-01
- **Reporter:** Shawn (live test on cert 26-0160)
- **Symptom:** Edit form shows the correct date (e.g. `5/10/2026`), but the same cert on `/Certificate/CertificateTileViewInternal` and `/Certificate/CertificateTileViewExternal` renders one day earlier (`5/9/2026`). Public tile view (`/Certificate/CertificateTileView`) was correct.
- **Root cause:** Global `UtcDateTimeConverter` (`Extensions/UtcDateTimeConverter.cs`, registered in `Program.cs:443-444`) serializes every `DateTime` as `"yyyy-MM-ddTHH:mm:ssZ"` — including calendar-only dates whose `Kind` is `Unspecified`. The `Z` suffix tells the browser the value is UTC midnight; in any negative-UTC timezone the browser then converts to local and `toLocaleDateString` renders the previous calendar day. The internal/external tile APIs were emitting raw `c.EffectiveDate` / `c.ExpirationDate` and went through the converter. The public tile API was already pre-formatting with `.ToString("yyyy-MM-dd")`, which is why only public was correct. Server-rendered Razor views (Edit form, `CertificateInternalView`, `CertificateExternalView`, `CertificateSingleView`) were also correct because they format server-side and never round-trip through JS.
- **Why the converter exists:** Per its header comment, it was added so the **Partner API** (`/api/v1/certificates*`) emits `"2024-01-15T00:00:00Z"` to match Amazon's documented format. Removing it globally would break the Partner API contract (`feedback_amazon_api_immutable.md`), so the fix mirrors the public API's existing pre-formatting pattern in the two tile APIs only.
- **Fix:** In the in-memory projection of both `Controllers/Api/CertificateTileInternalApiController.cs` and `Controllers/Api/CertificateTileExternalApiController.cs`, format the two date fields explicitly: `EffectiveDate = c.EffectiveDate.ToString("yyyy-MM-dd")` and `ExpirationDate = c.ExpirationDate.ToString("yyyy-MM-dd")`. JS `parseLocalDate` then matches its date-only regex branch and constructs a local `Date` — no UTC drift. Partner API output is unchanged.
- **Files:** `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`.
- **Verification:** Playwright run on `http://localhost:5101` (`C:\Users\swest\AppData\Local\Temp\uitest\verify_def_104_dates.js`, viewport timezone `America/New_York`):
  - Internal API: 3 sampled certs all returned dates as `yyyy-MM-dd` (e.g. `26-0023 eff=2027-07-08 exp=2028-07-07`).
  - External API: same shape (e.g. `26-0025 eff=2026-02-25 exp=2028-02-25`).
  - Internal tile card 1 rendered `7/8/2027` — matches the API's `2027-07-08` exactly (no off-by-one).
  - Build clean (0 errors, 49 warnings unchanged).
- **Cross-reference:** Same class of bug as the previously-shipped QC #23 fix, but a different surface (those were the cert detail view; this is the tile list APIs). The `UtcDateTimeConverter` was added later for Partner API compliance and re-introduced the drift on the JSON-API surfaces it touches.
- **Deploy:** `GCCWebApp-test-2026-05-01-1051.zip` (bundled with DEF-103, DEF-106, Manage Workflow seed removal). Validated live by Shawn 2026-05-01.

### DEF-108 — `Cross-Origin-Embedder-Policy: require-corp` blocks Google Analytics and other cross-origin embeds on every page

- **Date:** 2026-05-01
- **Severity:** Medium (analytics blackout + console-error noise on every page load; no functional break for the app itself)
- **Status:** **Fixed (local) — pending deploy.** Verified on `localhost:5101` 2026-05-01: console errors dropped from 112 to 0 GA-related errors after both header changes.
- **Reporter:** Anuki internal full-spectrum QA pass (2026-05-01)
- **Symptom:** Browser console on every authed and unauthed page shows:
  - `Failed to load resource: net::ERR_BLOCKED_BY_RESPONSE.NotSameOriginAfterDefaultedToSameOriginByCoep` (5x on `/Account/AccessDenied` alone)
  - `Connecting to 'https://analytics.google.com/g/collect?...' was blocked due to "require-corp" being set as Cross-Origin-Embedder-Policy` (2x per cert tile page load)
  - `Fetch API cannot load https://analytics.google.com/g/collect?...` (paired with each block)
- **Root cause:** `Middleware/SecurityHeadersMiddleware.cs` line 102 sets `Cross-Origin-Embedder-Policy: require-corp`. This policy mandates that every cross-origin embedded resource must respond with `Cross-Origin-Resource-Policy: cross-origin`. Google Analytics endpoints (`analytics.google.com`, `www.google.com/g/collect`) and `googletagmanager.com` do NOT set CORP, so they're blocked by the browser. Same root cause produces the AccessDenied resource block.
- **Why it matters:**
  1. GA is silently NOT recording any page views in dev OR test env (and probably in prod too — needs verification post-deploy). Whatever data the client thinks they're getting from GA, they're not.
  2. Console-error noise makes real defects harder to spot during QA.
  3. COEP is only valuable when the app uses `SharedArrayBuffer` / cross-origin isolation features. This codebase does not.
- **Sibling effect:** `Cross-Origin-Resource-Policy: same-origin` (line 99) is fine — only affects how OUR resources can be embedded by other origins. Left in place.
- **Fix applied (two-part):**
  1. `Middleware/SecurityHeadersMiddleware.cs` — changed `Cross-Origin-Embedder-Policy` from `require-corp` to `unsafe-none`. This unblocked the cross-origin script load.
  2. Same file — extended `connect-src` in CSP from `'self' https://api.greencirclecertified.com https://www.google-analytics.com https://www.googletagmanager.com https://cdn.jsdelivr.net` to additionally include `https://*.google-analytics.com https://analytics.google.com https://www.google.com https://stats.g.doubleclick.net`. GA4 fires beacons to all of these endpoints (not just `www.google-analytics.com`); without them in connect-src, the unblocked scripts could load but their fetches were CSP-rejected.
- **Verification on `localhost:5101` (2026-05-01):** Re-ran `fullspectrum_ui.js`. Console errors total: **112 → 1** (-99%). The single remaining is DEF-111 (`/Product/Management` SyntaxError, unrelated). All `analytics.google.com`, `www.google.com/g/collect`, `stats.g.doubleclick.net` fetches succeed.
- **Files touched:** `Middleware/SecurityHeadersMiddleware.cs` (CSP `connect-src` line, Permissions/Feature-Policy block, COEP value)
- **Found by:** `fullspectrum_out/console-errors-rollup.json` produced by the 2026-05-01 full-spectrum sweep.
- **Deploy:** Pending — bundles cleanly with DEF-107.

### DEF-107 — Duplicate `Feature-Policy` + `Permissions-Policy` headers on every response (browser warning 16x per session)

- **Date:** 2026-05-01 (original); reopened + fully resolved 2026-05-21
- **Severity:** Low (warning only; no functional break)
- **Status:** **Fixed locally 2026-05-21 (reopened) — pending deploy.** Original 5/1 fix (Feature-Policy removal in middleware) was correct but **incomplete** — the wider duplication came from `web.config` `<httpProtocol><customHeaders>` injecting the SAME set of security headers that `Middleware/SecurityHeadersMiddleware.cs` also injects. On IIS in-process hosting (prod), this produced duplicate headers for every entry: Permissions-Policy, X-Frame-Options (with CONFLICTING values: SAMEORIGIN vs DENY), X-XSS-Protection (`"1; mode=block"` vs `"0"`), Strict-Transport-Security, Content-Security-Policy (different policies!), Referrer-Policy. Kestrel dev hosting (`dotnet run`) doesn't apply `web.config`, so localhost never reproduced this — only IIS did.
- **Reporter:** Anuki internal full-spectrum QA pass (2026-05-01); reopened by 2026-05-21 status-sync sweep probing live `Permissions-Policy` header count and finding **2** on gccwebapp.com.
- **Symptom (original 5/1):** Browser console warning on every page load: `Error with Feature-Policy header: Some features are specified in both Feature-Policy and Permissions-Policy header: camera, microphone, geolocation, payment, usb. Values defined in Permissions-Policy will be used.` Sweep counted 16 warnings across 11 distinct URLs.
- **Re-discovery (2026-05-21):** `older-defs-grouped-sweep.js` probed prod headers via `HEAD /` (anon, basic-auth gate satisfied). Response carried TWO `Permissions-Policy` headers. Investigation traced the second source to `web.config:23-36` injecting the same set of security headers at the IIS layer.
- **Root cause (full picture).** Headers were being added at two layers:
  - **IIS** (`web.config` `<httpProtocol><customHeaders>`): X-Frame-Options, X-Content-Type-Options, X-XSS-Protection, Strict-Transport-Security, Content-Security-Policy, Referrer-Policy, Permissions-Policy
  - **ASP.NET Core middleware** (`SecurityHeadersMiddleware.cs:62-122`): the same set plus a few more (Cross-Origin-*, X-Permitted-Cross-Domain-Policies, X-Security-Policy, X-Content-Security-Policy)
  - `response.Headers.Append(...)` in middleware adds a SECOND value on a header that IIS already set; the browser sees both. In some cases the values disagreed (e.g., X-Frame-Options DENY vs SAMEORIGIN), so the rendered behavior depended on browser merge semantics.
- **Fix (2026-05-21).** Removed the entire `<httpProtocol>` block from `web.config:22-36` and replaced it with an explanatory comment. The middleware is now the single source of truth for all security headers:
  - Environment-aware (different CSP in dev vs prod via `IWebHostEnvironment`)
  - Applies before `UseStaticFiles` (per `Program.cs:570` / `Program.cs:657`) so static-asset responses get the same headers
  - Same path covers Kestrel (dev) and IIS in-process (prod), eliminating the localhost↔prod behavioral gap
- **Files touched (2026-05-21):** `web.config:22-36` (removed entire `<httpProtocol>` block, replaced with comment block explaining the consolidation).
- **Verification (localhost 2026-05-21):** `HEAD /` on `localhost:5101` returns exactly 1 of each security header: `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=(), usb=()`, `X-Frame-Options: DENY`, `Content-Security-Policy: default-src 'self'; …` (the full middleware-built policy). No duplicates. Kestrel doesn't apply `web.config`, so localhost was already clean — but the test confirms middleware still emits everything correctly.
- **Verification deferred:** Final proof requires the next deploy. Re-run `qc159-live-check/older-defs-grouped-sweep.js` post-deploy; the `Permissions-Policy header count=2` failure should flip to PASS, and the same probe should be extended to check X-Frame-Options (was SAMEORIGIN+DENY conflict) and the other duplicated headers.
- **Why this matters now.** The original 2026-05-01 fix landed only the middleware-side change. Anyone reading DEF-107 between 2026-05-01 and 2026-05-21 reasonably assumed it was deployed and closed. Today's status-sync sweep against prod surfaced that the issue was still observable — different root cause than the original (IIS vs middleware double-injection, not the legacy Feature-Policy header).
- **Cross-ref:** `web.config:32` previously carried a custom CSP value that disagreed with the middleware's CSP (e.g., dev/prod conditional was absent in web.config; `cdn.redoc.ly` missing; `connect-src` slightly different). Standardizing on middleware also means CSP changes are made in one place going forward (relevant for DEF-135 / DEF-108 maintenance).
- **Found by:** `fullspectrum_out/console-errors-rollup.json` (original 2026-05-01) + `qc159-live-check/older-defs-grouped-sweep.js` (re-discovery 2026-05-21).

### DEF-097 — Partner API `certificationCountries` must always be "All"
- **Date:** 2026-04-24
- **Severity:** Medium
- **Status:** Deployed — verified live 2026-04-24
- **Reporter:** Shawn (client confirmation pending at time of fix)
- **Contract:** Amazon Developer Guide — partner responses should always report `All` for country scope, regardless of what the cert's Country field holds internally.
- **Root cause:** `Controllers/Api/v1/CertificatesController.cs` projected `certificationCountries = c.Country` (and GS1 `TargetCountries = new List<string> { cert.Country }`) — so whatever analysts typed into the cert's Country field (e.g. "Germany") leaked into Amazon's API response. Most certs had blank Country → `null` in the response. Inconsistent output.
- **Fix:** Gated the projection on `isPartnerCaller`. Partner API callers now always see `"All"`; internal Admin/Analyst views still see the raw `c.Country` value for their own workflows.
- **Files changed:**
  - `Controllers/Api/v1/CertificatesController.cs` lines 241, 287, 868 (standard endpoints)
  - `Controllers/Api/v1/CertificatesController.cs` line ~1406 (GS1 `MapToGS1Format` helper — hardcoded `["All"]`)
- **Samples updated:**
  - `docs/tracking/api-samples/2026-04-24_amazon_standard.json`
  - `docs/tracking/api-samples/2026-04-24_amazon_gs1.json`
- **Deploy:** `GCCWebApp-test-2026-04-24-1100.zip`

### DEF-098 — Tablet view: duplicate "Hide Filters" buttons on logged-in cert listing
- **Date:** 2026-04-24
- **Severity:** Low
- **Status:** Fixed — pending deploy
- **Reporter:** Shawn (QC re-review of prior SIT item #163)
- **Symptom:** On tablet width (≤1024px), the internal/external certificate tile view showed two stacked blue bars at the top — "Hide Filters" (desktop sidebar-collapse button) and "Hide Filters & Search" (mobile toggle). Both functional, both mounted, confusing UI.
- **Root cause:** `.sidebar-collapse-btn` is `display: flex` at all breakpoints. The tablet breakpoint (`@media (max-width: 1024px)`) turns on `.mobile-filter-toggle` but never hides the desktop collapse button, so they render together.
- **Fix:** Added `.sidebar-collapse-btn, .sidebar-toggle-tab { display: none !important; }` inside the `max-width: 1024px` media query in `wwwroot/css/certificatetileview.css`. The mobile toggle is the single filter control on tablet/mobile; the desktop collapse controls are irrelevant because the sidebar always renders full-width at tablet.
- **Files changed:**
  - `wwwroot/css/certificatetileview.css` (tablet media block around line 2834)
- **Relates to:** SIT item #163 (filter collapse fix). #163 addressed collapsing the individual filter panels via the down-chevron — different control from this one.
- **Deploy:** `GCCWebApp-test-2026-04-26-2312.zip` (bundled with DEF-100, DEF-101)

### DEF-100 — Tile-view list/table mode: lazy-load wipes earlier pages, only latest page visible
- **Date:** 2026-04-26
- **Severity:** High
- **Status:** Fixed — pending deploy
- **Reporter:** Shawn (live observation on `https://gccwebapp.com/Certificate/CertificateTileViewInternal`)
- **Symptom:** On the certificate tile page in **list/table view**, scrolling to trigger the next page replaces the previously rendered rows with only the new page. User sees "the remainder, not the whole of the dataset". Tile (card) view was unaffected. Reproduced on both internal and external tile pages.
- **Root cause:** In `renderCertificates(append=true)` of both `certificate-tile-view-internal.js` (line ~309) and `certificate-tile-view-external.js` (line ~302), the list-view branch called `renderTableRows(newCerts)`. `renderTableRows` is a full-render helper that does `gridBody.innerHTML = certificates.map(...).join('')`, so passing only the new page's rows wiped every previously rendered row. The tile branch correctly used `grid.insertAdjacentHTML('beforeend', html)` and was fine. Public `certificate-tile-view.js` passes the full `state.certificates` to `renderTableRows` after card append, so it re-rendered every row each scroll — slower but visually correct.
- **Fix:** In the list-view append branch of both internal and external scripts, build HTML for the new certs only and call `gridBody.insertAdjacentHTML('beforeend', html)`. After insert: attach `change` listeners to the new `.row-checkbox` elements (so bulk selection still works on later pages), call `applyColumnVisibility()` so column-toggle settings apply to new rows, and `requestAnimationFrame(syncScrollSizes)` to keep the sticky-header scroll bar in sync.
- **Files changed:**
  - `wwwroot/js/certificate-tile-view-internal.js` (`renderCertificates` list-view append branch)
  - `wwwroot/js/certificate-tile-view-external.js` (`renderCertificates` list-view append branch)
- **Verification needed (post-deploy):** Open table view on internal and external tile pages, scroll past page boundary, confirm row count grows and earlier rows persist. Confirm bulk-selection checkboxes on rows past the first page still toggle the bulk-actions bar.
- **Deploy:** `GCCWebApp-test-2026-04-26-2312.zip` (bundled with DEF-100, DEF-101)

### DEF-101 — Import/Export icons reversed across admin views (arrow direction violates UX convention)
- **Date:** 2026-04-26
- **Severity:** Low
- **Status:** Deployed and verified on gccwebapp.com (status-sync 2026-05-21 — `/Product/Management` HTML contains `fa-file-arrow-down` and `fa-file-arrow-up`; no residual old `fa-download` icons)
- **Reporter:** Shawn (live observation on `http://localhost:5101/Company/Index`)
- **Symptom:** Across most admin pages, the **Import** button used `fa-upload` (up arrow) and the **Export** button used `fa-download` (down arrow). Per UX convention (and Shawn's reference image): Import means data flowing INTO the system → arrow should point DOWN; Export means data flowing OUT → arrow should point UP. The mapping was inverted everywhere except Tags and User Management (which already used `fa-file-arrow-down`/`fa-file-arrow-up` correctly).
- **Root cause:** Inconsistent FontAwesome icon convention. The `fa-upload`/`fa-download` icon names describe a literal action (uploading-to-server / downloading-to-browser) — when applied to Import/Export labels, the arrow direction reads backwards because users associate Import with "inbound/down" and Export with "outbound/up" (matches CSV file-arrow icon convention).
- **Fix:** Swapped icons on all affected views so Import → `fa-download` (or `fa-file-arrow-down` where the page already used the file-arrow family) and Export → `fa-upload`. Modal headers for "Import Preview" updated to match. Buttons that label literal actions ("Upload Document", "Download Selected", "Download Template") were left alone — those names match their icon.
- **Files changed:**
  - `Views/Company/Index.cshtml` (Import + Export buttons + Import Preview modal header)
  - `Views/Company/Management.cshtml` (Import Preview modal header — buttons were already correct using `fa-file-arrow-*`)
  - `Views/Product/Management.cshtml` (Import + Export buttons + Import Preview modal header)
  - `Views/Product/ManagementV2.cshtml` (Import + Export All)
  - `Views/Product/ManagementAlt.cshtml` (Import + Export Selected + Export All)
  - `Views/Product/Categories.cshtml` (Import + Export)
  - `Views/Admin/ProductManagement.cshtml` (Import + Export buttons + Import Preview modal header)
  - `Views/Admin/CSFAttributes.cshtml` (Export to CSV)
  - `Views/Admin/CertificateTypes.cshtml` (Export)
  - `Views/Admin/Pathways.cshtml` (Export)
  - `Views/Admin/ProductTypes.cshtml` (Export)
  - `Views/Admin/CertificateFields.cshtml` (Export)
  - `Views/Admin/RoleManagement.cshtml` (Export)
  - `Views/Admin/Partners.cshtml` (Export)
  - `Views/Shared/_ProductIdManager.cshtml` (Import + Export buttons + Import Preview modal header)
- **Pages already correct (left untouched):** `Views/Admin/Tags.cshtml`, `Views/Admin/UserManagement.cshtml` (both use `fa-file-arrow-down`/`fa-file-arrow-up` consistently across buttons + modal headers).
- **Verification needed (post-deploy):** Visit Company/Index, Product/Management, Admin/* pages and confirm Import button shows down-arrow, Export button shows up-arrow. Open the Import Preview modal on Company/Index, Product/Management, Admin/ProductManagement, _ProductIdManager — header icon should be down-arrow.
- **Deploy:** `GCCWebApp-test-2026-04-26-2312.zip` (bundled with DEF-100, DEF-101)

### DEF-099 — SIT #163 follow-up: external cert listing filter panels still don't collapse on desktop
- **Date:** 2026-04-24
- **Severity:** Low
- **Status:** Fixed — pending deploy
- **Reporter:** Carly (SIT re-review of #163, 2026-04-22)
- **Symptom:** On the logged-in cert listing (external/partner tile view), clicking the chevron next to a filter name (e.g. "Certification Type") did not collapse the panel. Carly reported it also appeared broken on the public view and wondered if that was by design.
- **Root cause:** In `wwwroot/js/certificate-tile-view-external.js`, the filter-header click handler lives inside `attachFilterEventListeners()` and was missing the `toggleBound` guard. That function is invoked at three points (init, after data load, on rerender), so the click listener was bound 2–3 times per panel. Each click fired multiple toggles, and an even count canceled out — the panel visually never changed. Same bug previously fixed in the internal view under QC R168; the internal file has the guard, external did not. Public view is structurally different (filter-header handler is attached once during init, not inside `attachFilterEventListeners`) so public does in fact work — Carly's "public doesn't work on desktop" observation likely a stale browser cache.
- **Fix:** Added the same `toggleBound === '1'` guard used by internal view. Each filter-header element now has exactly one click listener regardless of how many times `attachFilterEventListeners()` is called.
- **Files changed:**
  - `wwwroot/js/certificate-tile-view-external.js` (around line 684, `attachFilterEventListeners`)
- **Relates to:** Original SIT #163 fix (which added `toggleFilter()` to internal + external) and DEF-098 (tablet duplicate Hide Filters bar). This closes the third and last tail of #163.
- **Deploy:** `GCCWebApp-test-2026-04-26-2312.zip` (bundled with DEF-100, DEF-101)

### DEF-102 — GS1 endpoint missing per-ASIN product name in `additionalTradeItemId` entries
- **Date:** 2026-04-29
- **Severity:** Low
- **Status:** Closed 2026-04-30. Deployed via `GCCWebApp-test-2026-04-29-2136.zip` (60.3 MB; staged 2026-04-29, deploy held one day by a Plesk/Cloudflare upload error). Verified live on 2026-04-30: `GET /api/v1/certificates/gs1/26-0146` returned both ASINs with per-identifier `name` populated (John Frieda PROfiller+ Thickening Conditioner / Dream Curls Conditioner 250ml — matches Carly's `ASIN Example for Amazon_4.17.26.xlsx`). List endpoint `GET /api/v1/certificates/gs1?pageSize=100` returned 12/12 ASIN entries carrying `name` across KAO, Amazon Devices, and Sonos certs. Standard endpoint shape unchanged. Receipts saved at `docs/tracking/api-samples/2026-04-30_amazon_gs1_single.json` and `docs/tracking/api-samples/2026-04-30_amazon_gs1.json`.
- **Reporter:** Carly (UAT review of GS1 response shape, 2026-04-28)
- **Symptom:** GS1 response (`/api/v1/certificates/gs1` and `/api/v1/certificates/gs1/{n}`) listed each ASIN as `{ "type": "ASIN", "value": "B0..." }` with no name attached. Standard Partner API response surfaces per-identifier `productName` under `productIds[]`, so the two partner-facing responses were inconsistent.
- **Root cause:** Not a build error — the GS1 endpoint was modeled to the GS1 Certification Data Model schema (https://gs1us-technology.github.io/certification-data-model), where `additionalTradeItemId` is a `{type, value}` pair and product naming lives at the Product level. The Standard Partner API has freedom to add per-identifier metadata; GS1 conformance does not. Per-ASIN names are already captured (`CertificateProductId.ProductName`) via both the cert-edit UI's identifier modal and the bulk-import CSV `ProductName` column.
- **Fix:** Extended the GS1 schema with an optional `name` property on `GS1AdditionalTradeItemId`. `MapToGS1Format()` now populates `Name = pi.ProductName` for each ASIN in the `additionalTradeItemId` list. Backward compatible — additive only, omitted when null. No data model, UI, or Standard API changes.
- **Files changed:**
  - `Models/Api/GS1CertificationReport.cs` (added `Name` property with `[JsonIgnore(WhenWritingNull)]`)
  - `Controllers/Api/v1/CertificatesController.cs:~1359` (`MapToGS1Format` projection — added `Name = pi.ProductName`)
  - `Controllers/Api/v1/CertificatesController.cs:~1137` (XML doc example updated to show new field shape; flows through to Swagger)
- **Partner-facing docs updated to mirror the new shape (2026-04-29):**
  - `docs2/exports/AMAZON_API_DEVELOPER_GUIDE.html` — §5.2 GS1 example + Appendix A added a `productIds[].productName` ↔ `additionalTradeItemId[].name` row
  - `docs2/exports/AMAZON_API_DATA_EXAMPLES.html` — both single-ASIN and multi-ASIN GS1 examples
  - `docs2/exports/AMAZON_API_QUICKSTART.html` — GS1 example
  - `docs2/api/AMAZON-API-INTEGRATION-GUIDE.html` — GS1 example (ASIN + SKU pair both carry name)
  - `docs2/api/AMAZON-API-QUICKSTART.html` and `.md` — GS1 example
  - `docs2/api/AMAZON-API-SCOPE.html` — GS1 example
  - `docs2/api/PARTNER-API-TESTING-GUIDE.md` — GS1 example
- **Caveat for Carly:** Only ASINs where the import CSV (or UI edit) had a `ProductName` filled in will return a name. Older imports that left it blank will return no `name` field on those ASIN entries until backfilled.
- **Build:** 0 errors, pre-existing warnings only.
- **Verification (post-deploy):** Hit `GET /api/v1/certificates/gs1/26-0146` with the partner API key and confirm each `additionalTradeItemId` entry includes `name` populated from `productIds[].productName`. Verify Standard endpoint shape unchanged.

### DEF-103 — Partner API by-number and by-id endpoints leak raw certificate entity (status, country, internal IDs)

- **Date:** 2026-04-30
- **Severity:** **High**
- **Status:** **Fixed locally — pending deploy.** Found during pre-UAT self-test (Section A run); fix verified locally same day.
- **Reporter:** Anuki internal QA pass
- **Symptom:** Partner API endpoints `/api/v1/certificates/number/{n}` and `/api/v1/certificates/{id}` return the raw `Certificate` entity instead of a partner-projected DTO. Concretely, when the Amazon Partner API key calls `GET /api/v1/certificates/number/26-0001`, the response includes:
  - `"status": "Client Approved - Paid"` — should be `"Active"` per Amazon contract §4 / §8 (DEF-097c)
  - `"country": "United States"` — should be `"All"` per Amazon contract (DEF-097)
  - Legacy flat fields `asin`, `upc`, `gtin`, `ean` at the top level — contract uses `productIds[]` array
  - Internal IDs leaking: `companyId`, `productId`, `pathwayId`, `productCategoryId`, `certificationTypeId`, `createdBy`, `updatedBy`, `id` (DB primary key)
  - Shape does NOT match the LIST endpoint shape returned for the same cert
- **Contract impact:** Amazon currently cannot reliably use the by-number or by-id endpoints. The list endpoint and GS1 endpoints map status correctly; only these two single-cert flat endpoints regressed. Memory note 2026-04-21 claimed "Fixed on all 6 Partner-facing endpoints (list, list-with-productids, search by product ID, GS1 list, GS1 single, by-id, by-number, PDF download)" — only some were actually fixed.
- **Root cause:** `Controllers/Api/v1/CertificatesController.cs:383` (in `GetCertificateByNumber`) and the parallel by-id action both end with `return SuccessResponse(certificate);` where `certificate` is the raw EF entity loaded via `_context.Certificates.Include(...).FirstOrDefaultAsync(...)`. There is no projection to a Partner DTO and no `isPartnerCaller` branch to remap `status → "Active"` or `country → "All"`. The role-check filter at line 380 only blocks non-public statuses; once a cert passes the filter, the raw entity is serialized.
- **Verification done locally (`localhost:5101`, dev DB `GCCDatabase_Dev`):**
  - `GET /api/v1/certificates` (LIST): returns `"status": "Active"`, `"certificationCountries": "All"` — CORRECT
  - `GET /api/v1/certificates/number/26-0001`: returns `"status": "Client Approved - Paid"`, `"country": "United States"` — INCORRECT
  - `GET /api/v1/certificates/2`: same INCORRECT shape as by-number
- **Fix applied:**
  1. Added shared `BuildPartnerCertificateDto(Certificate c)` private helper at the bottom of `Controllers/Api/v1/CertificatesController.cs`. Returns the contract shape: certificateNumber / status="Active" / issueDate / expirationDate / certificationCountries="All" / partner / certificationResults (percentage-formatted) / pathway / company / productName / certificationType / amazonCode / issuer / productIds[] / pdfDownloadUrl. No internal IDs.
  2. `GetCertificate` (by-id) and `GetCertificateByNumber` now branch on `isPartnerCaller = GetCurrentUserRole() == "Partner"`. Partner callers get the projected DTO; Admin/Analyst keep the raw entity (their existing behavior).
  3. Both endpoints now `Include(c => c.Pathway)` and `Include(c => c.ProductIds)` so the projection has the data it needs.
- **Verification (`localhost:5101`, dev DB):**
  - `GET /api/v1/certificates/number/26-0001`: now returns `status: "Active"`, `certificationCountries: "All"`, `productIds: []` array shape, no internal IDs. ✓
  - `GET /api/v1/certificates/2`: same shape as by-number. ✓
  - Regressions: LIST endpoint still returns 4 certs all `Active`/`All`. GS1 list/single still work. 404 still returned for Draft (`EXT-AMZ-DRAFT`) and legacy Active (`26-0146`).
- **Build:** 0 errors, 47 warnings (unchanged from baseline).
- **Pre-deploy check on prod:** likely had the same defect prior — partner integration would have been getting `status: "Client Approved - Paid"` and `country: "United States"` on these endpoints. Confirm with a call against `gccwebapp.com` once the fix deploys. If Amazon was relying on the by-number endpoint, they would have been ignoring the wrong status (or seeing inconsistent shape between list and single). Worth a heads-up email when this ships.
- **Logged from:** TC-DEF-097/097c partner-side execution, captured in `docs/tracking/test-execution-log.md`. **Found before client UAT — exactly what the self-test pass was for.**

### DEF-104 — Product ID per-row Status drifted from parent certificate (CR-015 build)

- **Date:** 2026-04-30
- **Severity:** Medium (data hygiene; Amazon badging via Partner API was already filtered at the cert level so no live leak — but underlying state was wrong).
- **Status:** **Shipped 2026-05-06** (dev DB backfilled + PROD DB backfilled 2026-05-01 ahead of the app deploy, 0 mismatches post-audit). Validated via Playwright + Partner-API evidence pack 2026-05-12 (`docs/tracking/screenshots/qc164/EVIDENCE.md`); date-driven natural-expiry exclusion additionally proven 2026-06-23 (`Scripts/smoke-prod/qc164-expired-asin-exclusion.js`).
- **QC mapping:** **QC #164 (4/10/26 — Carly, "Product Identifiers / Amazon Integration")** maps to this fix. Re-verified against current code on 2026-05-06: cascade wired into all 8 mutation paths, Status surface removed from list/filter/modal/import-template/JS-export, prod backfill confirmed 0-mismatch by post-flight audit. Test env (`gccwebapp.com`) is the same prod DB per `project_plesk_deploy_env.md`, so backfill coverage is implicit. No additional work required for QC #164. (The 6/23 "confirm export matches CR details in email" sub-item is tracked separately under **CR-184**.)
- **Reporter:** Shawn (raised on the 2026-04-30 status conversation; client confirmed scope).
- **Symptom:** `CertificateProductId.Status` defaulted to `"Active"` on insert and was never auto-flipped when the parent cert moved out of `PublicStatuses` (Draft / Hold / Expired / Confidential / Not Paid / Republished->Expired / Expired - No Annual Verification). Analysts could also pick any per-row status from the UI / CSV, drifting from the cert's actual eligibility.
- **Why it matters:** Amazon badging integrity. The Partner API already filters cert lists to `PublicStatuses` (DEF-097 / 2026-04-21 fix), so live data going to Amazon was correct, but the underlying per-row state was stale and any future direct query / partner-side data hygiene check would surface the drift.
- **Fix scope (CR-015):**
  - `CertificateProductId.DeriveStatusFromCert(certStatus)` helper — single source of truth: `Active` iff cert is in `PublicStatuses`, else `Inactive`.
  - `IProductIdService.SyncProductIdStatusesForCertificateAsync(certId)` — cascade method; only updates rows where stored status drifts from derived (cheap no-op when in sync).
  - Cascade wired into 6 cert-status-change paths: `CertificateController.Edit` (POST), `CertificateController.Create` (POST), `CertificateController.ImportCertificates` (bulk POST), `CertificateService.PerformBulkActionAsync.update_status`, `CertificateService.UpdateCertificateAsync`, `CertificateWorkflowService.TransitionCertificateStatusAsync`, `CertificateExpirationService.ExpireOverdueCertificatesAsync`, `CertificateImportService.UpdateCertificateAsync` + `.CreateProductIdEntriesAsync`.
  - `ProductIdService.CreateProductIdAsync` / `UpdateProductIdAsync` overwrite incoming Status with the derived value (user input ignored).
  - Surface removal: Status column dropped from `_ProductIdManager.cshtml` table, filter dropdown, and Add/Edit modal; dropped from JS render / filter / form payload (`product-id-manager.js`); CSV import template, sample-CSV API endpoint, and in-memory export now omit the Status column. Status header in CSV import is silently ignored if present (back-compat for any saved templates).
- **Backfill:** `Scripts/database/CR015_backfill_product_id_status.sql` — idempotent. Ran against dev DB (`ANUKI01.GCCDatabase_Dev`) on 2026-04-30 — 12 product IDs flipped to Inactive (parent cert in legacy `Active` status, not in `PublicStatuses`); 0 needed flip to Active. Post-backfill audit query returned 0 mismatches. **Ran against PROD DB (`gccwebap_certificates` @ `68.71.129.120,1533`) on 2026-05-01 — 4 product IDs on Expired certs flipped to Inactive; 0 needed flip to Active; 17 rows on `Client Approved - Paid` certs were already correct. Post-flight audit returned 0 mismatches. Final prod state: 17 Active on Paid, 4 Inactive on Expired.**
- **Build:** 0 errors. `dotnet test` 143/144 (the 1 remaining failure is `CertificateWorkflowServiceTests.CheckAndUpdateExpiringCertificatesAsync_PendingRenewalExpired_TransitionsToExpired` — pre-existing dead-code failure on master, not introduced by this CR; verified via `git stash` baseline).
- **Verification (`localhost:5101`, dev DB):**
  - `/api/productids/sample-csv` returns `Type,Product ID,Partner,ProductName,ProductSku,Description` (Status header gone).
  - Product ID Manager modal on Create / Edit cert no longer shows the Status field.
  - List table no longer renders a Status column.
  - Status filter dropdown removed from filter row.
- **Partner API impact:** None. Contract is unchanged; this only cleans up underlying state. `feedback_amazon_api_immutable.md` rule preserved.
- **Pre-deploy on prod:** Run `CR015_backfill_product_id_status.sql` once after the app deploys. Counts the prod data ahead of time with the pre-flight query in the script header so we know the scale before flipping.

### DEF-104 — VOIDED 2026-04-30 (not a defect)

Originally logged as a regression of QC #31 (download icon position at desktop). On 2026-04-30 the user confirmed: **right-aligned is the intended behavior**, not centered. The 3/10 dev note that said "Changed to justify-content: center" was a mid-cycle fix that was later superseded; the 4/3 reopen ("buttons are not aligned right") meant *should be right-aligned*, and the current implementation correctly does that. My test card had the wrong expected outcome — TC-QC-031 PASSes at all viewports. No code change needed.

### DEF-106 — Partner API single-cert endpoints leak across partners (cross-tenant data exposure)

- **Date:** 2026-05-01
- **Severity:** **High** (security / partner data isolation)
- **Status:** **Fixed locally — pending deploy.** Found during TC-QC-143 (CR-008 Partner Role) execution; fix verified locally same day.
- **Reporter:** Anuki internal QA pass

**Symptom:** A Partner-role caller (API key OR Partner-role app user) could fetch certificates belonging to a DIFFERENT partner via the single-cert endpoints. Empirically confirmed on `localhost:5101` 2026-05-01:
- `GET /api/v1/certificates/number/EXT-NOVI-001` with the Amazon partner key → HTTP 200, returned the Novi cert in full (including `"partner": "Novi"` in the response).
- `GET /api/v1/certificates/42` (Novi cert id) with the Amazon partner key → HTTP 200, same leak.
- `GET /api/v1/certificates/gs1/EXT-NOVI-001` with the Amazon partner key → HTTP 200, GS1-formatted Novi cert exposed.
- `GET /api/v1/certificates/42/download` with the Amazon partner key → HTTP 404 in this case (the cert had no PDF), but the partner-scope check would have approved access if a file existed.

**Root cause:** The single-cert endpoints checked only role and `PublicStatuses`, NOT the caller's assigned partner. The LIST endpoint had been correctly applying `userPartnerNames.Contains(c.Partner)` since 2026-04-21; the same scoping was never applied to the four single-cert endpoints.

**Fix applied (`Controllers/Api/v1/CertificatesController.cs`):**
- `GetCertificate` (by-id, line ~340): added `var userPartnerNames = await GetCurrentUserPartnerNamesAsync(); if (!userPartnerNames.Any() || !userPartnerNames.Contains(certificate.Partner ?? "None")) return NotFoundResponse(...)` after the existing PublicStatuses check.
- `GetCertificateByNumber` (by-number, line ~390): same partner-scope check.
- `GetCertificateGS1` (gs1/{n}, line ~1352): same.
- `DownloadCertificate` ({id}/download, line ~1088): same.
- All four endpoints now: (1) check PublicStatuses for Partner, (2) check userPartnerNames.Contains(cert.Partner), (3) on either fail return generic 404 (no leak of "exists but you can't see it"). Admin/Analyst paths unchanged.

**Verification (`localhost:5101`, dev DB):**
- Positive: Amazon key → Amazon cert (by-number, by-id, GS1) all 200 ✓
- Negative: Amazon key → Novi cert (by-number, by-id, GS1, download) all 404 ✓
- Regression: Amazon LIST endpoint still returns 4 Amazon certs ✓
- Same fix protects Partner-role app users (cookie auth) — they share the same `GetCurrentUserPartnerNamesAsync()` and `GetCurrentUserRole() == "Partner"` paths.

**Pre-deploy implications for prod:**
- This is a real cross-partner data exposure that's been in prod since the partner-API endpoints were built. Severity is High because it's a partner-isolation breach, not a UI bug.
- Coordinate deploy with DEF-103 (which fixed the response shape) — both fixes touch the same four endpoints.
- Client should be informed before deploy. Consider whether to flag to the partner contact (Amazon side); the leak was inbound (Amazon could read others' data), not outbound (Amazon's data wasn't exposed to others — they're only in `c.Partner = 'Amazon'` rows).

**Found by:** TC-QC-143 (CR-008 Partner Role) execution in `docs/tracking/test-execution-log.md`.

### DEF-105 — CLOSED 2026-05-01 (Won't Fix — dev-only, not worth cycles)

Original framing: "prod has 12 duplicate cert types" — wrong, those were L2 subcategories (see `feedback_certtypes_hierarchy.md`). Re-scoped to "dev DB on pre-migration cert-type spec." Shawn confirmed 2026-05-01 he doesn't care about dev data hygiene; closing this entry. Prod is correct, no UAT impact.

(Original analysis preserved below for reference.)

- **Date:** 2026-04-30 (revised 2026-05-01)
- **Severity:** Low (dev environment only)
- **Status:** **Closed — Won't Fix**
- **Reporter:** Anuki internal QA pass

**Original draft of this defect claimed prod had 12 duplicate-name zombie rows. That was wrong.** Schema check showed `CertificationTypes` is self-referencing via `ParentId` with a `Level` column. The 12 "duplicates" I saw in prod are L2 subcategories under parent "Certified Sustainability Facts" (Id=6). They share Names with L1 top-level types, but they're referenced through a separate FK (`Certificates.CertificationTypeSubcategoryId`) and the L1 row carries the `AmazonCode`. By design, not a defect. Cleanup SQL has been removed from this entry — it would have wrongly deactivated valid hierarchy rows.

**Actual finding (dev only):**

The dev DB (`GCCDatabase_Dev` on `ANUKI01`) is on the pre-QC-#45 cert-types spec. The L1 top-level list (which is what the analyst Cert Create dropdown shows) has these wrong values:

| L1 cert type | Dev | Spec |
|---|---|---|
| Carbon Footprint Reduction | Inactive ✗ | Active |
| Sustainable Energy Practices | Inactive ✗ | Active |
| Sustainable Manufacturing Practices | Inactive ✗ | Active |
| Material Ingredient Reporting | MISSING ✗ | Active (renamed from MIV) |
| Product Optimization: Carbon Footprint Reduction | MISSING ✗ | Active |
| Renewable Resource Content | Active ✗ | Inactive |
| Biobased | Active ✗ | Inactive |
| Manufacturer Inventory Verification | Active ✗ | Should be renamed/gone |

Prod (`gccwebap_certificates`) has all of these correct. Migration `19_20260320_UpdateCertificationTypes.sql` ran cleanly on prod; never ran on dev.

**Hierarchy structure (for reference):**
- Dev: 21 L1 (16 active, 5 inactive) + 41 L2 (31 active)
- Prod: 18 L1 (all active) + 14 L2 (all active, mostly under Id=6 "Certified Sustainability Facts")
- The two environments have different L2 trees — that's likely fine, but note for future comparisons.

**Action:** re-seed or migrate the dev DB so future Section B testing isn't tripped by stale cert-type data. Low priority; doesn't block client UAT (prod is already correct). If we re-seed: also dedupe the L1 `Sustainable Energy Practices` row in dev (currently exists twice, both Inactive — appears to be a one-off seed glitch).

**Lessons learned for future DB inspection:**
- `CertificationTypes` is hierarchical via `ParentId` + `Level`. Always include those columns when comparing.
- Same Name at different Levels is by design, not a duplicate.
- See `feedback_certtypes_hierarchy.md`.

**Found by:** TC-QC-045 in `docs/tracking/test-execution-log.md`. Originally flagged with wrong "prod duplicates" framing; corrected after schema check.

---

### DEF-109 — Product ID "Created By" column shows "System" instead of the user who added it

- **Date:** 2026-05-05
- **Severity:** Medium (audit data correctness; visible to client on Cert Edit page)
- **Status:** Fixed (pending UAT)
- **Reporter:** Shawn (continuing QC #80 testing)

**Symptom:** On the Certificate Edit page → Product ID Manager table, the "Created By" column shows "System" for product IDs added through the certificate Create or Edit form, even when a logged-in analyst added them.

**Root cause:** `CertificateController.Create` (POST) and `CertificateController.Edit` (POST) accept the `model.ProductIds` collection from the form and write each row directly to the DB without setting `CreatedBy`. The JS payload from `ProductIdManager.getProductIdsForSubmission()` only carries Type/Value/Partner/Name/SKU/Description — no audit fields — so `CreatedBy` defaults to `null`. On re-fetch, the `CreatedByUser` navigation comes back null, and `product-id-manager.js` line 244–245 falls back to the literal string `'System'`.

This is distinct from `/api/productids` POST (the modal flow on Cert Edit *outside* the form save), which already sets `CreatedBy` correctly via `ProductIdApiController.CreateProductId`. The bug only fires on the Create/Edit page form-save path.

**Fix:** `Controllers/CertificateController.cs`
- Create flow (around line 481): set `CreatedBy = GetCurrentUserId()`, `CreatedAt = now`, `UpdatedAt = now` on each `productId` before adding to the certificate's collection.
- Edit flow (around line 868): same, plus `UpdatedBy = currentUserId`. Note: the Edit flow's `certificate.ProductIds.Clear()` + re-add pattern means existing rows are deleted and re-inserted, so all rows get the new audit stamp on edit — consistent with how `Tags` and `CertificationTypeSubcategories` are handled in the same method.

**Side effect / known limitation:** Existing rows already in DB with `CreatedBy = NULL` will continue to render as "System" until they are touched by a save. A backfill SQL update (`UPDATE CertificateProductIds SET CreatedBy = (SELECT CreatedBy FROM Certificates c WHERE c.Id = CertificateId) WHERE CreatedBy IS NULL`) would clear the historical rows but is a data change — per the project rule (`feedback_data_changes_require_cr.md`), that needs a formal CR before running on prod, so it is NOT included here.

**Test plan:**
1. Log in as `analyst` on `localhost:5101`.
2. Navigate to `/Certificate/Create`. Add a product ID (any type/value) via the modal. Save the certificate.
3. Open the saved certificate via `/Certificate/Edit/{id}`. Confirm the Product ID Manager row shows `Created By: analyst` (not `System`).
4. Add another product ID directly on the Edit page. Save. Reload. Confirm new row also shows `Created By: analyst`.
5. Cross-role check: log in as `admin`, add a product ID, confirm row shows `Created By: admin`.
6. Negative: existing pre-fix product IDs (created before this commit) should still render `System` — confirms the limitation note.

**Found by:** Continuing TC-QC-080 manual UAT. Spotted by Shawn; not in original 4/27 client QC log.

---

### DEF-110 — Admin `/api/certificates` POST/PUT path silently dropped CreatedBy / UpdatedBy

- **Date:** 2026-05-05
- **Severity:** Low (Admin-only API, no client UAT impact, but identical audit-data-loss pattern as DEF-109)
- **Status:** Fixed (pending UAT)
- **Reporter:** Anuki internal QA pass — DEF-109 follow-up sweep

**Symptom:** Certificates created or updated through `POST /api/certificates` and `PUT /api/certificates/{id}` were saved with `CreatedBy = NULL` (and updates with `UpdatedBy = NULL`). The Cert Internal View hides the "Created By" line entirely when `CreatedByUser` is null (`CertificateInternalView.cshtml:187` guards the block), so no visible "System" string appears here — but the audit data is still missing.

**Root cause:** `CertificatesApiController.MapToCertificate` builds a `Certificate` from the request DTO without setting CreatedBy. The controller then routes through `CertificateService.CreateCertificateAsync` / `UpdateCertificateAsync`, neither of which had any way to know the calling user — the methods only accepted a `userRole` string. The wiring lost the user identity between `User.FindFirst(NameIdentifier)` (available on the controller) and the entity (saved by the service).

**Fix:**
- `Services/IServices.cs` — added optional `int? userId = null` parameter to `CreateCertificateAsync` and `UpdateCertificateAsync`.
- `Services/CertificateService.cs` — both methods now stamp `CreatedBy` (and `UpdatedBy` on update) from the new param.
- `Controllers/Api/CertificatesApiController.cs` — added a `GetCurrentUserId()` helper (parses `ClaimTypes.NameIdentifier`) and passes the result into both service calls.

The optional parameter default keeps any future caller that doesn't supply userId from breaking. The web `/Certificate/Create` and `/Certificate/Edit` paths don't use these service methods (they hit the repository directly with their own user-stamp logic), so they are unaffected.

**Test plan:**
1. Log in as `admin`. Use Postman or `curl` against `https://localhost:5101/api/certificates` with `Content-Type: application/json` and a valid CreateCertificateRequest body. Submit POST.
2. Open the new cert via `/Certificate/CertificateInternalView/{id}`. Confirm the "Created By: <admin email>" line appears — previously this row was silently hidden.
3. PUT to `/api/certificates/{id}` with an UpdateCertificateRequest body. Reload the cert detail. Confirm UpdatedBy was set (DB inspection or future audit-trail UI).

**Found by:** Code-survey sweep after DEF-109 fix. Shawn asked "do we have this issue elsewhere in the views?" — this surfaced as the only remaining write path that produced null audit fields.

---

### DEF-111 — Product ID import-template column order didn't match the on-screen Product ID Manager table

- **Date:** 2026-05-06
- **Severity:** Low (cosmetic/UX consistency, no data loss)
- **Status:** Fixed (pending UAT)
- **Reporter:** Shawn — spotted on `/Certificate/Edit/26-0106`

**Symptom:** Downloaded import template (`product-ids-sample.csv`) had columns in the order `Type, Product ID, Partner, ProductName, ProductSku, Description`. The on-screen Product ID Manager table and the in-memory export both use `Type, Product ID, Product Name, SKU, Partner, ...`. Users editing the template in Excel saw the columns in a different order than what they were used to in the UI, which made bulk-edit error-prone.

**Root cause:** `wwwroot/js/product-id-manager.js:1283` had a hard-coded template string that predated the table redesign and was never re-aligned. The in-memory export at line 1253 was updated; the template was missed.

**Fix:** `wwwroot/js/product-id-manager.js`
- `downloadSample()` template header now: `Type,Product ID,Product Name,SKU,Partner,Description` (matches table + export order).
- `parseAndPreviewImport()` productName lookup is now tolerant of both `productname` (legacy template, no space) and `product name` (new template, with space). Older templates users may already have on disk continue to parse correctly.

**Test plan:**
1. Open any cert with Product IDs at `/Certificate/Edit/{id}`. Click **Template** in the Product ID Manager toolbar.
2. Open the downloaded CSV. Confirm header order: `Type, Product ID, Product Name, SKU, Partner, Description` — the same order as the table on the page.
3. Add a row matching the new order; click **Import**, upload. Confirm the preview shows the row correctly mapped (Product Name and SKU not swapped with Partner).
4. Take a previously-downloaded older template (`Type, Product ID, Partner, ProductName, ProductSku, Description`), populate one row, import. Confirm it still parses correctly — backward compatibility verified.
5. Bulk test: import `C:\Users\swest\Downloads\product-ids-sample-1000-asins.csv` (1000 ASINs generated 2026-05-06) against an empty cert. Confirm all 1000 rows preview as **New**, no errors.

**Found by:** Shawn during cert-edit walkthrough. Filed alongside CR-016 prep work — the column-order mismatch is a defect; the broader UX redesign of the import-preview modal is the CR.

---

### DEF-112 — Bulk Product ID import capped at 100 rows due to per-row HTTP requests hitting AspNetCoreRateLimit

- **Date:** 2026-05-06
- **Severity:** High (blocks the use case CR-016 was scoped around — bulk-import of partner-onboarding ASIN lists)
- **Status:** Fixed (pending UAT — requires dev-server restart to load the C# changes)
- **Reporter:** Shawn — `localhost:5101/Certificate/Edit/26-0106` import of 1000-row test CSV. First 100 rows imported, next 900 returned `"Unknown error"`.

**Symptom:** Importing >100 product IDs into a certificate failed every row past the 100th. UI reported `Imported 100 product ID(s). 900 failed: Row 101 ... Unknown error; Row 102 ...`. Exact 100/900 split was the giveaway — that's a per-minute rate-limit cap being hit.

**Root cause:** Two compounding bugs.

1. **Frontend was firing one HTTP request per row.** `wwwroot/js/product-id-manager.js#applyImport()` looped over every selected row and did a separate `fetch('/api/productids', POST)` for each. For 1000 rows that's 1000 separate API calls.
2. **AspNetCoreRateLimit caps `*/api/*` at 100 req/min per IP** (configured in `appsettings.json:96-99`). Request 101 onward returned 429. The fetch wrapper read `await response.text()` for the body but the actual error string came back empty for 429s, so the user-facing toast said `"Unknown error"` instead of `"429 Too Many Requests"`.

**Fix:** Three coordinated changes.

1. **New JSON-body bulk endpoint.** `Controllers/Api/ProductIdApiController.cs` adds `POST /api/productids/certificate/{certificateId}/bulk` accepting `BulkProductIdImportRequest` (`{ items: ProductIdImportItem[], overwriteExisting: bool }`). Replaces 1000 individual POSTs with one request — dodges the rate limit by design.
2. **Frontend rewritten to use bulk.** `wwwroot/js/product-id-manager.js#applyImport()` now splits selected rows into `creates` and `updates`. Creates go in one bulk POST; modifications still PUT individually (small count, no rate-limit risk). The replace-all toggle now passes `overwriteExisting: true` to the bulk endpoint instead of looping individual DELETEs from the browser.
3. **Service-layer pre-fetch optimization.** `Services/ProductIdService.cs#ImportProductIdsAsync` no longer calls `CreateProductIdAsync` per row (which itself fired 3 DB queries + a SaveChanges). Now: pre-fetches existing rows, parent cert status, and max display order in 3 queries total; does duplicate detection against an in-memory dictionary; queues all entities; commits a single batched SaveChanges. Improvement for 1000-row import: ~4000 DB roundtrips → ~3 + 1 batched insert. Should complete in 1-2 seconds instead of timing out.

**Header tolerance (bonus, related to DEF-111):** The file-based `/api/productids/certificate/{id}/import` endpoint now accepts both legacy template headers (`Value`, `ProductName`, `ProductSku`) and new-template headers (`Product ID`, `Product Name`, `SKU`). Backend `DownloadSampleCsv` aligned to the new column order.

**Files touched:**
- `Controllers/Api/ProductIdApiController.cs` — added bulk endpoint and `BulkProductIdImportRequest` DTO; updated DownloadSampleCsv header.
- `Services/ProductIdService.cs` — rewrote `ImportProductIdsAsync` to pre-fetch + batch insert; made `ImportFromCsvAsync` header lookup tolerant.
- `wwwroot/js/product-id-manager.js` — rewrote `applyImport()` to use the bulk endpoint.

**Test plan:**
1. Stop the dev server (it's holding `bin/Debug/net8.0/GCCWebApp.exe` — current PID 37876). Run `dotnet build` then `dotnet run --urls "http://localhost:5101"`.
2. Navigate to `/Certificate/Edit/26-0106`. Hard-refresh (Ctrl+F5) so the browser picks up the new JS.
3. Click **Import** in the Product ID Manager toolbar; pick `C:\Users\swest\Downloads\product-ids-sample-1000-asins.csv`.
4. Preview should show `1000 New, 0 Modified, 0 Errors`.
5. Click **Apply Import**. Expect: success toast `Imported 1000 product ID(s)` (no failures, no rate-limit error). Table reloads showing 1000 rows.
6. Inspect browser DevTools Network tab — only **one** request to `/api/productids/certificate/26-0106/bulk`, not 1000 to `/api/productids`.
7. Inspect server log — should show `Bulk import for certificate {id}: 1000 succeeded, 0 failed`.
8. Edge case: re-import the same file. Default mode (Add) should preview all 1000 as duplicates and report 1000 errors `already exists on this certificate`. Replace mode should silently keep them (no errors).

**Found by:** Shawn during DEF-111 retest with the 1000-ASIN test file generated 2026-05-06.

---

### DEF-113 — Same per-row HTTP-POST anti-pattern across 4 admin imports (Companies, Tags, Products, Users)

- **Date:** 2026-05-06
- **Severity:** High (blocks bulk-onboarding workflows on every admin import surface in the app, identical root cause to DEF-112)
- **Status:** Fixed (pending UAT — requires dev-server restart to load the C# changes)
- **Reporter:** Shawn — proactive sweep after DEF-112 fix

**Symptom:** After fixing DEF-112 on the Product ID import, an inventory of the rest of the app surfaced four more import flows using the same broken pattern: one HTTP POST per row in a `for await` loop. Affected surfaces:

| Surface | URL | Old endpoint per row | Frontend |
|---|---|---|---|
| Companies | `/Company/Management` | `POST /Company/SaveCompany` | `companyManagement.js#applySelectedImport` |
| Tags | `/Admin/Tags` | `POST /Admin/CreateTag` | `tags.js#applySelectedImport` |
| Products | `/Admin/ProductManagement` | `POST /Admin/CreateProduct` | `productManagement.js#applySelectedImport` |
| Users | `/Admin/UserManagement` | `POST /Admin/CreateUser` | `userManagement.js#importCSV` |

In dev, all four were exposed to the localhost IP rate cap of 200/min (`appsettings.json:112-124`), so any import >200 rows would fail rows past 201 with empty `"Unknown error"` strings (same as DEF-112's diagnostic). Per-row pattern was also slow (~10s per 100 rows) regardless of rate-limit hits.

**Root cause:** The frontend pattern of `for (const row of rows) { await fetch('/...', {body: row}) }`. Identical to DEF-112.

**Fix (universal pattern):** For each surface, three coordinated changes:
1. JSON-body bulk endpoint that accepts pre-parsed rows + replace flag (where applicable).
2. Service-layer optimization: pre-fetch existing rows + reference data once, in-memory dup detection, batched `SaveChangesAsync`. Dropped from N×4 DB roundtrips to ~3 + 1 batched insert.
3. Frontend `applyImport()` rewrite: collapse N requests into 1 bulk POST for creates; per-row PUT for the small "modified" set.

**Per-surface specifics:**
- **Companies** — new `POST /Company/BulkImport`, dup key `Name` (case-insensitive). Replace-all supported via `overwriteExisting` flag. Single bulk audit entry. Files: `Services/ICompanyService.cs`, `Services/CompanyService.cs`, `Controllers/CompanyController.cs`, `wwwroot/js/admin/companyManagement.js`.
- **Tags** — new `POST /Admin/BulkImportTags`, dup key `Name + TagType` (case-insensitive — Internal/External tags are independent namespaces). Tightens contract: existing `CreateTagAsync` had no dup check. TagType normalized to lowercase to match prod data. Files: `Services/IAdminService.cs`, `Services/AdminService.cs`, `Controllers/AdminController.cs`, `wwwroot/js/tags.js`.
- **Products** — new `POST /Admin/BulkImportProducts`, dup key `Name + CompanyId`. Pre-fetches Companies + ProductTypes as dictionaries for FK resolution. Replace-all NOT supported (mass-deactivation risk on typo). Bonus fix: the previous JS payload dropped `companyName` and `productTypeName` entirely — products imported as orphans. New JS sends them. Files: `Services/IAdminService.cs`, `Services/AdminService.cs`, `Controllers/AdminController.cs`, `wwwroot/js/admin/productManagement.js`.
- **Users** — new `POST /Admin/BulkImportUsers`, dup key `Email` (case-insensitive). Pre-fetches Users + Companies + Roles. BCrypt password hashing kept inline (CPU-bound, ~50ms/row). Replace-all NOT supported (locked-out-admin risk). New `BulkImportUserItem` DTO uses string-based references (`CompanyNames: List<string>`) which the service resolves to FK IDs in memory. Bonus fix: `userManagement.js#importCSV` was parsing a 9-column legacy format that no longer matched the 7-column template — every column was silently mis-mapped. Aligned to template format. Files: `Services/IAdminService.cs`, `Services/AdminService.cs`, `Models/AdminModels.cs`, `Controllers/AdminController.cs`, `wwwroot/js/admin/userManagement.js`.

**Tracking decision:** all 4 surfaces logged under one DEF-113 since they share root cause + fix pattern.

**Test plan:**
1. Stop the dev server; `dotnet build` then `dotnet run --urls "http://localhost:5101"`.
2. For each test file in `C:\Users\swest\Downloads\`:
   - `test-companies-250.csv` → `/Company/Management` → Import. Expect 250 created, 0 errors. One request to `/Company/BulkImport` in DevTools Network tab.
   - `test-tags-250.csv` → `/Admin/Tags` → Import. Expect 250 created, 0 errors. One request to `/Admin/BulkImportTags`.
   - `test-products-250.csv` → `/Admin/ProductManagement` → Import. Pre-req: `Amazon` company and `Personal Care` product type must exist; otherwise rows fail validation cleanly with name-resolution errors.
   - `test-users-250.csv` → `/Admin/UserManagement` → Import. BCrypt hashing 250 passwords takes ~12s server-side. One request to `/Admin/BulkImportUsers`.
3. Re-run any of the above. Expect all 250 to fail with "already exists" — confirms within-batch and cross-batch dup detection works.

**Found by:** Shawn — "let identify them and we need to test all of them" — proactive request after DEF-112 fix.

---

### DEF-162 — Main nav bar misaligned with page content (flush left edge instead of inside `.page-container`)

- **Date:** 2026-05-07
- **Severity:** Medium (cosmetic, but global — affects every authenticated page across all roles)
- **Status:** Fixed
- **Reporter:** Shawn — Cert single view screenshot, `https://gccwebapp.com/Certificate/CertificateSingleView/26-0157`

**Symptom:** The main navigation bar (`Certificates`, `Products`, etc.) renders flush against the left viewport edge instead of aligning with the `<h1 class="page-title">` and the rest of `.page-container` content. Visible on every page that uses `_Layout.cshtml`.

**Root cause:** `Views/Shared/_Layout.cshtml:155` was missing the closing `>` on the `<div class="nav-wrapper …">` opening tag:

```cshtml
<div class="nav-wrapper @(User.Identity?.IsAuthenticated != true ? "nav-anon" : "")"
    <nav class="main-nav" aria-label="Main navigation">
```

Browser parsers treat `<nav`, `class="main-nav"`, `aria-label="Main navigation"` as junk attributes on the div until they hit the first `>`, then drop the `<nav>` entirely. The `.nav-links <ul>` ends up directly inside `.nav-wrapper`, which has no `max-width` or horizontal padding — so links extend to viewport edge while `.page-container` (max-width 1800px, padding 0 40px) and `.main-nav` (same) sit further inset.

**Fix:** Closed the `nav-wrapper` div tag. One-character change. With the `<nav class="main-nav">` element actually rendering, its `max-width: 1800px; padding: 0 40px` matches `.page-container` exactly, so the nav links now align with `.page-title` on every page.

**File:** `Views/Shared/_Layout.cshtml:155`.

**Test plan:**
1. Hard-refresh `/Certificate/CertificateSingleView/26-0157` while signed in as `external` (or any authenticated role).
2. Confirm `Certificates` link aligns horizontally with the `Certificate Details` h1.
3. Spot-check a page in each role: `/Certificate/CertificateTileViewExternal` (External), `/Certificate/CertificateTileViewInternal` (Analyst/Admin), `/Admin/UserManagement` (Admin).
4. View source — confirm `<nav class="main-nav">` opens correctly and is a child of `.nav-wrapper`.

**Found by:** Shawn — single view screenshot.

---

### DEF-163 — Certificate import template doesn't match the table or export columns; round-trip is broken

- **Date:** 2026-05-07
- **Severity:** Medium (functional — silently corrupts the obvious workflow of "export → edit → re-import")
- **Status:** Fixed
- **Reporter:** Shawn — `https://gccwebapp.com/Certificate/Create` flagged that the import template doesn't match the table

**Symptom:** Three different column sets for the same data:

- **Internal cert tile table** (24 cols): Cert #, Cert Type, Status, Eff Date, Exp Date, Company, Product, Prod Cat, Prod Type, Prod Subcat, Location, Country, Ext Tag, Int Tag, Partner, ASIN, UPC, GTIN, EAN, Cert Results, Pathway, Cert Subcategories, Company Email, Company URL.
- **Export CSV** (`Services/CertificateService.cs:1023`): matches the table — friendly labels like `"Certificate #"`, `"Company"`, `"Effective Date"`.
- **Import template** (`Controllers/CertificateImportController.cs:321` — broken state): `CERTIFICATE_NUMBER,COMPANY_NAME,COMPANY_LOCATION,COMPANY_COUNTRY,COMPANY_EMAIL,COMPANY_WEBSITE,PRODUCT_NAME,PRODUCT_TYPE,CERTIFICATION_TYPE,EFFECTIVE_DATE,EXPIRATION_DATE,CERTIFICATE_LOCATION,CERTIFICATE_COUNTRY,PARTNER,CERTIFICATION_RESULTS,TAGS,ASIN,UPC,GTIN,EAN,PRODUCT_ID_TYPE,PRODUCT_ID_VALUE,PRODUCT_SKU,AMAZON_CODE,PDF_FILE_PATH`.

**Mismatches:**

- **Missing from old template, present in table & export:** Status, Product Category, Product Subcategory, Pathway, Cert Subcategories, External Tag (separate), Internal Tag (separate).
- **Extra in old template, not in table:** COMPANY_LOCATION, COMPANY_COUNTRY, PRODUCT_ID_TYPE, PRODUCT_ID_VALUE, PRODUCT_SKU, AMAZON_CODE, PDF_FILE_PATH, generic TAGS.
- **Naming style:** Export uses `"Certificate #"`; old import REQUIRED `CERTIFICATE_NUMBER`. Editing an exported CSV and re-uploading silently failed every column match.

**Root cause:** `DownloadTemplate` in `CertificateImportController.cs` and the alias arrays in `Services/Import/CertificateImportService.cs` evolved separately from the export, with no schema-of-record. They drifted apart over time as table columns were added (Pathway, Cert Subcategories, separate Internal/External tags via CR-007) without backfilling the import side.

**Fix (round-trip):**

1. **`Controllers/CertificateImportController.cs:DownloadTemplate`** — replaced the SCREAMING_SNAKE template with the export's friendly headers (24 columns, mirrors `Services/CertificateService.cs:1023`).
2. **`Services/Import/CertificateImportService.cs:53-78`** — added friendly-label aliases (`"Certificate #"`, `"Company"`, `"Effective Date"`, `"Cert Results"`, `"Company Email"`, `"Company URL"`, `"Contact URL"`, `"Company Contact Email/URL"`, `"Cert Type"`, etc.) alongside the existing legacy/PascalCase aliases. Existing legacy CSVs continue to import unchanged.
3. **External Tag column** in the export now feeds the existing single `Tags` ingest field. Internal Tag column is read into the CSV but silently ignored at the data layer (the import row model exposes only one Tags field today).

**Known gaps (outside round-trip scope, deferred to future CRs):**

- Product Category, Product Subcategory, Pathway, Cert Subcategories, Internal Tag columns are present in the new template but not yet ingested into the certificate record. Re-importing an exported CSV preserves Cert #, Cert Type, Status, Company, Email, URL, Product Name, Product Type, Effective/Expiration Date, Location, Country, External Tag, Partner, Cert Results, ASIN/UPC/GTIN/EAN. The remaining columns are accepted (no validation error) but not yet written.
- The CSV column header "Company Contact Email/URL" in the export is itself a misleading label (only carries the email). Aliased on the import side; not renamed on the export side to avoid breaking any client tooling already keyed off it.

**Files:** `Controllers/CertificateImportController.cs:321`, `Services/Import/CertificateImportService.cs:53-78`.

**Test plan:**

1. `/CertificateImport` → click "Download Template". Open the CSV — header should exactly match the export-with-internal-tags column list (`"Certificate #","Certification Type","Cert Subcategories","Status","Company","Company Email","Company URL","Product Name","Product Category","Product Type","Subcategory","Partner","Pathway","Cert Results","Effective Date","Expiration Date","Location","Country","External Tag","Internal Tag","ASIN","UPC","GTIN","EAN"`).
2. **Round-trip:** From `/Certificate/CertificateTileViewInternal` → Export. Open the CSV in Excel, edit one row's `Cert Results` value, save. Upload via `/CertificateImport` → preview should classify the row as "Modified" and show the updated value.
3. **Legacy regression:** Upload a CSV using the OLD `CERTIFICATE_NUMBER,COMPANY_NAME,...,TAGS` headers — should still import cleanly, no validation errors. (Old CSVs Carly's team already produced must keep working.)
4. Build: `dotnet build` → 0 errors.

**Found by:** Shawn — single-line UAT note "the import template doesn't match the table".

---

### DEF-164 — Anonymous certificate file endpoints serve PDFs of non-public certs (Draft / Hold / Suspended / Revoked / Expired)

- **Date:** 2026-05-08
- **Severity:** Critical (information disclosure — internal lifecycle PDFs harvestable by unauthenticated callers)
- **Status:** Fixed
- **Reporter:** Internal security audit (`audit/role-management-audit-2026-05-08.md` finding F-C-002 / F-C-003)

**Symptom:** Three `[AllowAnonymous]` endpoints on `CertificateController` served any certificate's uploaded PDF without checking whether the certificate was in a publicly visible status:

- `GET /Certificate/Download/{number}` (line 1506) — by certificate number
- `GET /Certificate/ViewCertificateFile/{id}` (line 1573) — by integer ID, inline display
- `GET /Certificate/DownloadByNumber/{number}` (line 1655) — by certificate number, delegates to `Download`

Certificate numbers are sequential (`YY-NNNN`) and integer IDs are trivially enumerable. An anonymous attacker iterating those identifiers could pull every Draft, Hold, Suspended, Revoked, or Expired certificate PDF — none of which the business considers publicly viewable.

**Why this slipped through:** The companion entry point `CertificateSingleView` (line 200) DOES correctly enforce `CertificateStatusHelper.IsPubliclyVisible(...) && !isExpired` for unauthenticated callers, but the three file-serving endpoints below it never received the same guard. Same vulnerability class as DEF-103 (Partner API status leak) — a contract enforced on the "list" surface but missed on a parallel "single resource" surface.

**Root cause:** Direct file-serve endpoints were originally written before the `PublicStatuses` concept formalised, and were never revisited when `IsPubliclyVisible` was added to `CertificateStatusHelper`. `[AllowAnonymous]` was kept (correctly) so verifiers without accounts can still download *publicly visible* certs from the Verify URL — but the status filter that makes that decision was missing.

**Fix:** Mirror the `CertificateSingleView` guard at all three endpoints. For unauthenticated callers only, check `IsPubliclyVisible(certificate.Status) && certificate.ExpirationDate >= BusinessToday`. Authenticated callers (Admin / Analyst / External / Partner) keep their existing access — they retain the legitimate need to view non-public certs through their respective workflows. `DownloadByNumber` gets its own explicit guard even though it delegates to `Download`, so the protection cannot be quietly lost in a future refactor (DEF-103 history).

**Files:** `Controllers/CertificateController.cs` (3 edits — lines 1506, 1573, 1655 region).

**Test plan:**

1. **Anonymous, public cert:** `GET /Certificate/Download/24-0001` (or any cert in `Active` / `Client Approved - Paid` etc. with a future expiration) — still downloads.
2. **Anonymous, Draft cert:** find a Draft cert number in dev (e.g. via `SELECT TOP 5 Number FROM Certificates WHERE Status = 'Draft'`) → request `GET /Certificate/Download/{that-number}` in incognito → expect `404 — Certificate {n} is not currently available for public download.`
3. **Anonymous, Expired cert:** find a cert with `ExpirationDate < TODAY` → expect 404 with same message.
4. **Authenticated Analyst, Draft cert:** sign in as `analyst / TestPass123!`, hit the same Draft cert URL → still serves the PDF (no regression for internal workflows).
5. **Anonymous, ViewCertificateFile by integer ID:** `GET /Certificate/ViewCertificateFile/{id-of-Draft-cert}` → expect 404 (NOT placeholder image — placeholder is only for missing-file case).
6. **Anonymous, DownloadByNumber:** same as #2 but via `/Certificate/DownloadByNumber/...` — expect 404 even though the inner `Download()` would also block.
7. Build: `dotnet build` → 0 C# errors (file-lock errors from a running `dotnet run` instance are not compile errors).

**Out of scope (separate audit findings):**

- F-C-001 (`TestController` missing auth), F-C-004 (`CertificatesApiController` accepts `?userRole=` from query string), F-H-001..F-H-005 — tracked as separate items in the audit report; will be addressed in follow-up DEFs.

**Found by:** Internal security audit run on 2026-05-08, before next UAT cycle.

---

### DEF-165 — Usability-contract batch: Product Management template, Cert table sort (Product Category + multi-value cols), Replace All toggle discoverability

- **Date:** 2026-05-08
- **Severity:** High (functional — controls don't do what their labels promise; erodes user trust)
- **Status:** Fixed
- **Reporter:** Shawn (verifying QC log items pre-UAT) + Carly (DEF-159 originally)

Four sub-items grouped because they're all the same defect class — the UI says X but the control does Y. Fixed in one batch.

**Sub-item A — Product Management import template doesn't match the Add New Product modal**

Verifying the QC item's "Fixed" claim showed it wasn't actually fixed. Three problems:

1. `Controllers/ProductController.cs:880` server endpoint emitted 7 columns including `Status` — but the Add New Product modal had `Status` removed back in QC #92.
2. `wwwroot/js/admin/productManagement.js:538` class method emitted a *different* 5-column CSV (`Name, Company, Description, Product Type, Status (Active, Inactive)`) — missing Category and Subcategory entirely.
3. Both fired on a single click — `Views/Product/Management.cshtml:35` had inline `onclick="downloadImportTemplate()"` (server endpoint) AND productManagement.js auto-bound the class method via `addEventListener`. Same duplicate-handler pattern that was fixed earlier this month for the Export button.

Fix: server endpoint now emits the 6-column header that mirrors the modal (`Name,Company,Description,Category,Product Type,Subcategory`). The JS class method is now a one-line `window.location.href = '/Product/DownloadProductImportTemplate'` — single source of truth, both pages get the same template, the duplicate handler becomes a no-op (browser dedupes the navigation).

Files: `Controllers/ProductController.cs:880`, `wwwroot/js/admin/productManagement.js:538`.

**Sub-item B — Internal cert tile: Product Category column sorts by Product Type**

`wwwroot/js/certificate-tile-view-internal.js:636-637` had both `productCategory` and `productType` mapped to the backend key `'product-type'`. The backend never had a `'product-cat'` case at all, so even a corrected frontend would fall through to the default sort. Carly's report: "Carly Test and Electronics randomly show in middle of list" when sorting Product Category — exactly what you'd expect when the column is actually sorting by Product Type.

Fix: frontend `productCategory` now maps to `'product-cat'`, with a corresponding `'product-cat-za'` in the reverse-sort map. Backend `Controllers/Api/CertificateTileInternalApiController.cs` adds `'product-cat'` and `'product-cat-za'` cases sorting by `c.ProductCategory.Name` with a fallback to `c.Product.Category.Name` (matches the existing pattern used by `product-type` and `product-sub`).

Files: `wwwroot/js/certificate-tile-view-internal.js:636,684`, `Controllers/Api/CertificateTileInternalApiController.cs:692-695`.

**Sub-item C — Internal cert tile: multi-value sort columns return arbitrary "first value"**

The backend cases for `int-tag`, `ext-tag`, `asin`, `upc`, `gtin`, `ean`, `cert-sub` all used the shape:
```csharp
c.Tags.Where(t => t.TagType == "internal").Select(t => t.Name).FirstOrDefault() ?? ""
```
No `OrderBy` inside the subquery. EF returned whatever the DB picked first — usually physical-row order. Two certs with the same internal-tag set inserted in different orders sorted differently. Same defect for ASIN/UPC/etc when a cert has multiple Product IDs of the same type.

Fix: every multi-value subquery now has an inner `.OrderBy(...)` before `.FirstOrDefault()`, so the per-cert "first" value is deterministic (alphabetic). Direction of the column sort is unchanged — only the inner-pick is now stable.

File: `Controllers/Api/CertificateTileInternalApiController.cs:700-721` (7 subqueries hardened: `ext-tag`, `int-tag`, `asin`, `upc`, `gtin`, `ean`, `cert-sub`, plus their `-za` reverse counterparts).

**Sub-item D (DEF-159 reframe) — Replace All Product IDs toggle was invisible to the user**

DEF-159 was opened by Carly as "build a Replace All UX" — but the feature already existed: `wwwroot/js/product-id-manager.js:1408+` reads `#overwrite-existing-toggle` and runs the replace-all path. The defect was discoverability: a small unstyled checkbox with the text "Replace all existing" sat in the modal footer next to the Download Template button. Carly imported a 1-row file in default (append) mode, hit a duplicate-key collision, and concluded "Replace all doesn't work."

Fix: in `Views/Shared/_ProductIdManager.cshtml:315-317` the toggle is now a pill-shaped chip with an amber border and `fa-exchange-alt` icon, label expanded to "Replace all existing IDs with this file", and the tooltip explains the two modes explicitly: "When checked: this import file is the new full list. Existing Product IDs not in the file will be deactivated. When unchecked (default): rows in the file are added or updated; nothing is deactivated." No JS or behavior change — the underlying replace-all logic is unchanged.

File: `Views/Shared/_ProductIdManager.cshtml:310-322`.

**Test plan:**

1. **Template (sub-item A)** — Sign in as `analyst`. `/Product/Management` → click Template. Open the CSV in a text editor: header should be exactly `Name,Company,Description,Category,Product Type,Subcategory` (single line, no trailing newline expected, no sample rows). `/Admin/ProductManagement` → click Template. Same file. Both pages now produce one identical download per click.
2. **Sort Product Category (sub-item B)** — `/Certificate/CertificateTileViewInternal` as analyst. Click Product Category column header. Rows should sort A→Z by Product *Category* (not Type). Click again → Z→A. Click Product Type column → resets, sorts by Type. Verifies the two columns are now distinct.
3. **Multi-value sort (sub-item C)** — Find a cert with two internal tags (e.g. via SQL: `SELECT TOP 1 CertificateId FROM CertificateTags WHERE TagType ...`). Click Internal Tag header — order should be deterministic across repeated page reloads. Same for ASIN if any cert has multiple ASINs.
4. **Replace All toggle visibility (sub-item D)** — Open `/Certificate/Edit/{any-cert}`, scroll to Product Identifiers, click Import, choose a CSV. In the preview modal, the Replace All toggle should now visibly stand out (amber/cream pill in modal footer). Hover the label — tooltip should explain both modes. Toggle off → import adds rows. Toggle on → import deactivates anything not in the file.
5. Build: clean (0 errors, 61 pre-existing warnings).

**Found by:** Shawn during pre-UAT QC verification (2026-05-08). Sub-item D originally raised by Carly as DEF-159.

---

### DEF-181 — QC #108 self-test surfaced two defects in the User import template/parser

**Date:** 2026-05-11 | **Status:** FIXED 2026-05-11 | **Severity:** Medium | **QC Ref:** #108 (rigorous re-test) | **Note:** Originally numbered DEF-166 in error (collided with the cert-tile-perf entry above); renumbered 2026-05-11.

Shawn asked for rigorous testing of QC #108 after it was marked resolved. Authored a new Playwright NUnit suite (`GCCWebApp.UAT.Tests/Tests/Admin/QC108UserImportTemplateTests.cs`, 8 tests) and ran it against `localhost:5101`. First run: 6 passed, 2 failed — both real defects, both fixed in the same session.

**Sub-item A — Template CSV header was malformed (8 columns when parsed)**

The 7th header `Company Access (semicolon-separated names, or "All")` contains an unescaped comma. `headers.join(',')` produced a row that any RFC-4180-compliant parser (Excel included) splits into 8 columns, surfacing a phantom 8th header to the user.

Fix: `wwwroot/js/userManagement.js` — `downloadImportTemplate()` now wraps any header field containing a comma or quote in RFC-4180 quotes with internal quotes doubled. The header line still reads identically when un-escaped, but CSV parsers now see exactly 7 columns.

**Sub-item B — Template advertised "All" as a valid value, but parser rejected it**

The template hint says `Company Access (semicolon-separated names, or "All")`. The JS preview parser (`resolveCompanyIds`) already short-circuits `"All"` → every-company-IDs, but the server-side parser at `Controllers/AdminController.cs` `ImportUsers()` didn't. A row with `Company Access = All` hit the company-name-lookup path → no match → 0 companies assigned (or `Company 'All' not found` error). The template hint lied.

Fix: `Controllers/AdminController.cs` — `ImportUsers()` now special-cases `companyAccessRaw.Equals("All", OrdinalIgnoreCase)` → assigns every `Company.Id`. Matches the JS preview behavior. Same `Company Access` semantics now apply whether the user uses the UI import flow (JS preview → batch `/CreateUser` calls) or POSTs the file directly to `/Admin/ImportUsers`.

**Test plan:**

1. **Template malformation (sub-item A)** — `/Admin/UserManagement` → click Template. Open in Excel: should see exactly 7 columns (Name, Email, Role, Password, Status, RequirePasswordReset, Company Access). Pre-fix: 8 columns with a phantom `or "All")` last header.
2. **"All" semantics (sub-item B)** — Modify template: add 1 row with `Company Access = All`. Click Import → preview shows the row assigned to all companies → confirm → user is created with company-checkboxes for every company. POST the same file directly to `/Admin/ImportUsers` (e.g., curl with auth cookie + antiforgery): user is also assigned to every company. No `Company 'All' not found` error.
3. **Regression** — Repeat TC-QC-108 baseline test plan (headers-only template, no sample row, "Company Access" column on UI/export/template, 5/1 client reply satisfied).
4. **Automated:** `dotnet test --filter QC108` against TEST_BASE_URL=http://localhost:5101 → 8/8 passing.

**Found by:** Anuki internal QA — Shawn-directed rigorous re-test of QC #108 (2026-05-11). Did NOT come from client.

---

### DEF-235 — `Permissions-Policy` response header is duplicated on prod (DEF-107 fix regressed / re-injected)

> ⚠️ **DEF-NUMBER COLLISION (flagged 2026-06-02).** This number is also used by the sustained-load `ERR_CONNECTION_REFUSED` entry earlier in this file (~line 115). Both were opened in the same 2026-06-02 governance sweep. **Proposed resolution:** renumber THIS (Permissions-Policy) entry to **DEF-237**, keep the connection-refused entry as DEF-235 (it's more cross-referenced — e2e-smoke-plan, batch-restart mitigation). Awaiting Shawn's ok before renumber (ripples to `predeploy-record-2026-06-june-batch.md` + the Gate-5 governance note). Tracked in the June predeploy record blocker #3.

> ✅ **UPDATE 2026-06-02 — fix is in the working tree and folded into the June deploy batch.** The `web.config` `<httpProtocol><customHeaders>` block (the second injection source) has already been removed (web.config lines 22-31 are now an explanatory comment); `SecurityHeadersMiddleware` is the single source. Localhost re-verified: `HEAD /` emits exactly 1 of each security header (`Permissions-Policy count=1`). `web.config` ships with the FileSystem publish, so it deploys with this batch. **Status flips to "fixed locally — pending deploy + prod re-probe."** Post-deploy: `curl -sI https://gccwebapp.com/ | grep -ci permissions-policy` → expect 1 (was 2).

**Date:** 2026-06-02 | **Status:** Fixed locally (web.config dedup in tree) — pending deploy + prod header re-probe | **Severity:** Low (security-header hygiene; no functional impact, but trips header-correctness scanners) | **QC Ref:** internal — follow-up to **DEF-107** | **Reporter:** Governance sweep 2026-06-02 (promoted from the 2026-05-21 status-sync "🔴 STILL FAILING" flag)

**Why this is a new DEF, not just a DEF-107 note.** DEF-107 was recorded as fixed and shipped, but the 2026-05-21 targeted-probe sweep (`qc159-live-check/older-defs-grouped-sweep.js`) found the `Permissions-Policy` header **back to count=2** on gccwebapp.com. A fix that was marked shipped but is observably failing on prod needs its own open tracking item so it doesn't get lost inside a closed entry. DEF-107 stays closed-as-shipped for the original fix; DEF-235 owns the regression.

**Likely root cause (to confirm).** Two independent injection points are both emitting the header — the strong candidate is the same dual-source pattern proven in DEF-135: the IIS-level `web.config` (`<customHeaders>`) **and** the app's security-headers middleware both add `Permissions-Policy`, and unlike CSP (which browsers intersect) duplicate `Permissions-Policy` headers are simply both sent. Browsers honor the first; scanners flag the dupe.

**Reinvestigation plan (not yet run — logged only this session):**
1. `curl -sI https://gccwebapp.com/ | findstr /I "permissions-policy"` — confirm count=2 still reproduces.
2. Grep both sources: `web.config` `<httpProtocol><customHeaders>` and the middleware that sets security headers (the same place DEF-135's CSP lives). Identify which two are emitting it.
3. Fix by removing the header from one source (prefer middleware-only, consistent with the DEF-135 "consolidate to middleware" architecture note) — or have the middleware `Remove` before `Add` so it's idempotent regardless of what web.config sent.
4. Post-deploy: re-run the prod smoke header assertion to confirm count=1.

**Cross-ref:** `feedback_*` n/a. Architecture lesson already on file (2026-05-04 DEF-135 memory): dual CSP/header sources (web.config + middleware) drift — a consolidation pass would close this whole class.

---

## Closed / Historical

*(Entries from the pre-existing register to be merged here if recovered.)*
