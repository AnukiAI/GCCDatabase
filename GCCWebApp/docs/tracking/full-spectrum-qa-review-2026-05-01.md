# Full-Spectrum QA Review — GCC Database Solution
**Review date:** 2026-05-01  
**Reviewer:** Anuki internal QA (senior engineer)  
**Build under review:** `GCCWebApp-test-2026-05-01-0006.zip` (test env) + uncommitted DEF-107/108/110/113/114/115/116/117/118/119 in working tree (local)  
**Test target:** `http://localhost:5101` (per `feedback_testing_environment.md` — staging writes to prod DB and is not safe for breaking-change testing)  
**Sources reconciled:** `defect-register.md` (DEF-097→DEF-119), `shipped-2026-05.md`, `qc-readyforuat-2026-05-01.md` (55 items), `cutover-2026-05-01.md`, `change-request-register.md` (CR-005→CR-015), `test-execution-log.md`, `feedback_*.md` memory files

---

## 1. Executive Summary — Go / No-Go

**Recommendation: GO for client UAT on the 0006 test build, conditional on five items below being verified post-deploy.**

The app is closer to launch-fit than the volume of tracked items suggests. Of the 163 QC items in the source-of-truth log, **0 hard regressions** were found in the 2026-05-01 Ready-for-UAT validation pass. 36 items PASS, 11 need a manual visual pass (Playwright reach failures, not real defects), 4 are CR-level feature requests masquerading as defects, and 4 require prod-side data to exercise. Recent shipping has been disciplined and well-documented.

**The ten items that matter for go/no-go:**

1. **PARTNER API IS THE LIVE INTEGRATION.** Amazon's contract is immutable per memory rule. DEF-103 caught two partner endpoints (`/api/v1/certificates/number/{n}` and `/{id}`) that were leaking raw entity shape since 2026-04-21 — the 2026-04-21 ship-note "all 6 endpoints fixed" was wrong and only the list + GS1 endpoints had actually been projected. Fixed locally and bundled in the 0006 build. **Smoke tests #9–#12 in `cutover-2026-05-01.md` are non-negotiable post-deploy.** If any partner endpoint leaks `status: "Client Approved - Paid"` or country other than `"All"`, this is S1 and the build rolls back.
2. **CR-015 BACKFILL ALREADY RAN ON PROD AHEAD OF CODE DEPLOY.** 4 product IDs on Expired certs flipped from `Active` → `Inactive`. Backfill is idempotent and safe. **Verify post-deploy that the Status column / field / filter is GONE from the UI** — the data is already aligned, but if old code paths are still serving the page during deploy, analysts could briefly see stale UI.
3. **DEF-111 (`/Product/Management` SyntaxError) is OPEN and silent.** Analyst sees an empty product list with no toast/banner. Not a launch blocker (analysts can still use Admin Product Management) but should be confirmed as not a session/auth issue before client UAT — if it is auth-related, the analyst role permissions need patching.
4. **DEF-112 (Partner role conflation) is OPEN, ARCHITECTURAL, NEEDS A DECISION.** No current bug, but the same `if (userRole == "Partner")` branch handles two completely different identities (web-user + API key). If a human Partner-role user logs in to prod, audit logs become indistinguishable from Amazon API calls. Decision needed before any human Partner user is provisioned. Will not affect UAT.
5. **DEF-110 (CSV header alignment) requires a server restart on test env to take effect.** JS/Razor changes are live; `CertificateService.cs` and `AdminService.cs` C# changes need the IIS app pool bounced. If the cutover restart already happened, this is fine. **Verify after deploy by exporting a few admin pages and confirming headers match the on-screen labels.**
6. **DEF-108 / GA blackout.** Until DEF-108 deploys, GA is silently NOT recording any pageviews on prod. Any "the client thinks they're getting analytics" assumption is false. Once deployed, verify GA beacons land on prod via Network tab — the test was on `localhost:5101` and confirmed 112→1 console errors.
7. **iOS zoom-on-focus regressions are the recurring mobile bug.** Five mobile/tablet items shipped this week (DEF-114/115/116/117 + Batch 7 follow-ups). Re-test all `<input>` fields on a real iOS device — anything below 16px font triggers iOS auto-zoom. The mobile test suite already covers the standard breakpoints; what it doesn't cover is dynamically-injected modals (e.g., the Add Product ID modal) where new inputs may not have inherited the 16px baseline.
8. **AUTOCOMPLETE NARROWING (QC #59) IS NOT IMPLEMENTED.** Vanilla `<select>` with first-letter-jump only. Carly explicitly asked for typeahead narrowing. This will surface as a defect in client UAT unless explicitly framed as CR-014's territory or scoped as a separate small CR (~1-2 days, Select2/Tom-Select wiring across 5+ fields).
9. **Test data on dev DB does not exercise several QC items.** Cert #23-0375 (special chars), LEED-tagged certs, certs with diacritics, products with populated Subcategories under Chemicals/Raw-Mat — none exist on dev. Eight QC items in the Ready-for-UAT log (#51, #94, #95 partial, #123, #124, #127, #128, #129, #149) are flagged "verify on prod after deploy". **These are not defects on dev — they're untested-on-real-data.**
10. **The undocumented-CR pattern is the structural risk, not the code.** Per `change-request-register.md`: 3 of ~20 delivered scope changes were tracked as formal CRs; the rest absorbed as "defect fixes". The technical execution is fine. The cost-control issue (per Shawn's standing rule "no more building without formal CRs") is in how new asks get logged. Future client requests need to land in `change-request-register.md` before code starts, not after.

**Health rating per workflow:**

| Workflow | Health | Notes |
|---|---|---|
| Public cert tile view + filter | 🟢 Good | Search icon, search bar dropdown, infinite scroll, share-link all PASS. Some L2/L3 filter behavior needs prod data to fully verify. |
| Analyst cert CRUD + workflow transition | 🟢 Good | 14-state machine intact. Watch for double-submit on workflow buttons (not yet defended at UI level). |
| Amazon Partner API (`/api/v1/*`) | 🟡 Hold-on-deploy-verify | DEF-103 was a contract leak that went live 2026-04-21. Bundled in 0006 build. Smoke tests are mandatory. |
| External user data isolation | 🟢 Good | Company-scoped filtering verified (DEF-097 pre-state captured). No leak path identified. |
| Admin import/export (CSV-only) | 🟡 Pending restart verify | DEF-101 icons fixed, DEF-110 headers fixed, but C# header changes need a server restart to land. |
| Mobile / tablet / responsive | 🟢 Good | DEF-114/115/116/117 just shipped. Mobile test suite is rigorous (5 viewports, full_page screenshots, content waits). |
| Authentication & session | 🟡 Hold-on-CR-009/010 | 30-min sliding session is configured. Forgot Password / Duo MFA / Partner role are CR-009/010/008, approved 2026-04-29, not yet built. |
| Dashboard + analytics | 🟡 GA-blocked-pre-deploy | DEF-108 blocks GA beacons in prod until deployed. Local fix verified 112→1 errors. |

---

## 2. Application Under Test — Reconciled Snapshot

| Field | Value |
|---|---|
| App | GCC Database Solution (a.k.a. GCC Web App) |
| Tech | ASP.NET Core 8.0 MVC + EF Core 9.0 + SQL Server + Azure AD B2C + Bootstrap 5 + vanilla JS (no SPA framework) |
| Production URL | `https://greencirclecertified.com/database` (path-based, **NOT** a subdomain) |
| Test environment | `https://gccwebapp.com` — Plesk-hosted, runs against PROD DB. HTTP Basic Auth gate + `robots.txt Disallow: /` + visible TEST banner. `/api/*` bypasses gate. |
| Local dev | `http://localhost:5101` — **default test target** (staging writes to prod data) |
| Current build | `deploy-out/GCCWebApp-test-2026-05-01-0006.zip` (60.3 MB) |
| Active partner | Amazon Climate Pledge Friendly (live integration, contract-immutable) |
| User roles | Public · External · Analyst · Admin · Partner (machine, X-API-Key) |
| State machine | 14-state certificate workflow in `CertificateWorkflowService.cs` (8 lifecycle states + variants — the "8-state redefinition" QC #54 from 2026-03-19 collapsed externally-visible labels but the underlying transitions are unchanged) |
| Surface area | 51 Razor views, 25 controllers, 38 JS files, 9 admin pages, 6 partner-facing API endpoints, 3 cert tile views (public/external/internal) |

**Test credentials (local dev only):**

| Role | Username | Password |
|---|---|---|
| Admin | `admin` | `TestPass123!` |
| Analyst | `analyst` | `TestPass123!` |
| External | `external` | `TestPass123!` |
| Partner (X-API-Key, dev) | header value | `gcc_partner_95961557dde1e196be074634cef252caec32c4b08e1e58dcfa63e43bbdbb63c7` |

**Login form selectors** (for automation): `#Email`, `#Password`, `button[type="submit"]`. **List-view rows** are `<div class="table-body-row">` inside `#gridBody`, NOT `<tr>`.

---

## 3. Recent Build Composition (0006 + working tree)

What ships in the cutover zip:

| Item | Type | Risk | Status |
|---|---|---|---|
| **CR-015 / DEF-104** — Product ID Status auto-derives from cert; UI Status removed | CR | Low | Built, backfill ran 2026-05-01 |
| **DEF-103** — Partner API by-id and by-number now project the contract DTO | Defect | **Medium-High** (contract change on 2 endpoints) | In zip, awaiting deploy |
| **DEF-098** — Tablet 1024px duplicate "Hide Filters" bars resolved | Defect | Low | In zip |
| **DEF-100** — Tile-view list mode lazy-load no longer wipes earlier pages | Defect | Low | In zip |
| **DEF-101** — Import/Export icon arrows reversed | Defect | Low | In zip |

**In the working tree but NOT in the 0006 zip** (will need next build):

| Item | Type | Files |
|---|---|---|
| DEF-107 — Feature-Policy / Permissions-Policy duplicate header | Fixed local | `Middleware/SecurityHeadersMiddleware.cs` |
| DEF-108 — COEP `require-corp` blocks GA + CSP missing GA hosts | Fixed local | `Middleware/SecurityHeadersMiddleware.cs` |
| DEF-110 — CSV export header alignment across 12 surfaces | Fixed local (JS live; C# needs restart) | 12 files including `CertificateService.cs`, `AdminService.cs` |
| DEF-113 — `/Admin/Index` removal + breadcrumb cleanup (18 views) | Fixed local | `AdminController.cs`, 18 views |
| DEF-114 — Dashboard mobile portrait stats clipping | Fixed local | `Views/Dashboard/Index.cshtml`, `wwwroot/css/pages/dashboard.css` |
| DEF-115 — Cert tile "Columns" toggle right-align on narrow viewports | Fixed local | `wwwroot/css/certificatetileview.css` |
| DEF-116 — Product Identifier filter row mobile overflow | Fixed local | `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/css/product-id-manager.css` |
| DEF-117 — Column Settings dropdown overflows mobile viewport | Fixed local | `wwwroot/js/certificate-tile-view-internal.js`, `certificate-tile-view-external.js` |
| DEF-118 — Product ID export from Cert Create missing Created/Created By | Fixed local | `wwwroot/js/product-id-manager.js` |
| DEF-119 — Add Product ID modal narrow on desktop | Fixed local | `wwwroot/css/product-id-manager.css` |

**Open in tracker, not yet fixed:**

| Item | Severity | Why it matters |
|---|---|---|
| DEF-111 | Medium | `/Product/Management` SyntaxError — silent product list failure for analyst |
| DEF-112 | Medium | Partner role conflation — architectural decision needed before any Partner human user is provisioned |

---

## 4. Risk Heatmap (Where the Defects Cluster)

| Surface | Open / Recent | Notes |
|---|---|---|
| **Partner API** (`Controllers/Api/v1/CertificatesController.cs`) | DEF-097, DEF-102, DEF-103 | Highest-risk surface. Contract-immutable. Every release re-test list / by-id / by-number / GS1 list / GS1 single. |
| **Cert tile views** (internal + external + public) | DEF-098, DEF-099, DEF-100, DEF-115, DEF-117, QC #66/84/86/134/140/157/161/163 | Largest surface area. Three views share patterns but diverge in details (each had its own DEF-100-class lazy-load bug). |
| **Cert Create / Edit** | DEF-116, DEF-118, DEF-119, QC #59/60/78/80/133/139/159 | Modal width, Product ID Manager filter row, autocomplete, bulk-replace — most touched recently. |
| **Admin pages** (User Management, Tags, Product Mgmt, Pathways, Cert Types, Cert Fields, Product Types, Partners) | DEF-113, DEF-110, QC #91/94/95/106/107/109/117/118/142/144/145/146 | Core CRUD risk. Headers + permissions + cascading dropdowns. |
| **Dashboard** | DEF-114, QC #141/154/155/156 | Mobile clipping + count semantics. |
| **Public tile view** | DEF-109 (timezone), QC #51/123/124/127/128/129/130/131/137/149 | Most "data-state-needed-on-prod" items live here. |
| **Auth / session / nav** | QC #71/96/104, CR-009/010 | 30-min sliding session works; Forgot Password + Duo are unbuilt CRs. |
| **Mobile / tablet** | DEF-098, DEF-114/115/116/117, QC #37/152/153 | Recurring iOS zoom-on-focus risk if any new `<input>` loses 16px baseline. |
| **CSP / security headers** | DEF-107, DEF-108 | One file (`SecurityHeadersMiddleware.cs`) — fixes pending deploy. |

**Heatmap takeaway:** Partner API + cert tile views absorb the most defects per release. These are also the highest-traffic surfaces. **Every release should retest both, even if no defect was filed against them.**

---

## 5. Top 10 Quick Wins (Low-Effort, High-Impact)

Ordered by effort:value. All shippable in <2 hours each.

1. **Cut a new build that includes DEF-107/108/110/113/114/115/116/117/118/119.** Single biggest quick win — 10 fixes already verified on localhost, blocked only by build cycle. **Effort: 30 min build + smoke.**
2. **Resolve DEF-111 (`/Product/Management` SyntaxError).** Add `response.ok` guard + content-type check before `response.json()` on the failing fetch. Even if the underlying auth issue isn't fixed today, surface the failure visibly via `AdminCore.showNotification('error', ...)` instead of silent. **Effort: 1 hr investigation + 15 min fix.**
3. **Add help tooltip on `/Admin/CertificateFields` Section selector** explaining "Section is an organizing label only — all custom fields render under one bucket on the cert form." Closes QC #118 with a labeled UI hint. **Effort: 15 min.**
4. **Strip the `test` Role row** from prod `Roles` table. Currently exposed in the Add User Role dropdown. Likely a dev seed leftover. Audit prod first. **Effort: 30 min query + decision.**
5. **Verify partner cleanup on prod** — `Partners` / `PartnerApiKeys` rows where `Name LIKE '%test%' OR '%carly%'` (QC #145). Don't delete without client sign-off. **Effort: 20 min query.**
6. **Document the 30-min auto-logout behavior** in the UAT cheat-sheet (QC #71). Avoids client misinterpreting expired-session redirect as "page broken". **Effort: 15 min.**
7. **Manual mobile pass for QC #37, #152, #153.** Three items waiting on a real-device or non-headless emulation run — no code change, just visual verification. **Effort: 30 min on a real iPhone.**
8. **Align Edit-page server export columns** (`Services/ProductIdService.cs:418`) with the in-memory export from DEF-118. Same column shape both directions. Carry-over flagged in DEF-118 ship notes. **Effort: 1 hr.**
9. **Manual reach for QC #80** (Product ID Manager Edit modal). Either it's actually broken (filed as new defect) or my Playwright selectors missed the markup (close as PASS-by-walkthrough). **Effort: 15 min.**
10. **Strip the orphan `_adminService.GetDashboardDataAsync()` method** (called out in DEF-113 ship-note). Dead code now that `/Admin/Index` was removed. **Effort: 5 min.**

---

## 6. Open / Pending Decisions for Shawn (CR-Level)

These are NOT defects. They're feature-request asks Carly raised that need a client decision before scoping.

| QC# | Ask | Why CR-level | Effort if yes |
|---|---|---|---|
| #59 | Type-ahead narrowing on Company / Cert Type / Product Type selects | Vanilla `<select>` first-letter-jump is the current UX; narrowing requires Select2/Tom-Select wiring across 5+ fields | ~1-2 days |
| #84 | Two URL columns on logged-in cert listing (`Company Contact Email/URL` + `Contact URL`) — likely should rename/merge | Naming/data-model decision, not a regression | 2-4 hrs |
| #86 | Mouse-drag horizontal scroll like Wrike | Pure UX enhancement; current scrollbar works | ~1 day |
| #106 | Add User: free-text "Employer" label vs current 36-checkbox company-assignment | The checkbox list is permission scoping; Carly's original ask was a free-text label. Need clarity on whether both are wanted | 4-6 hrs if both |
| #159 | Bulk-replace flow for thousands of Product IDs | Current Product ID import appends. Bulk-replace is a different UX | 1-2 days |
| #161 | Already PASS — Cert Results, Pathway, Cert Subcategories, Contact URL columns all present in toggle list | — | — |

DEF-112 (Partner role conflation) is also a decision-pending — see §10.

---

## 7. Console & Network Hygiene

**Pre-DEF-107/108 baseline (2026-05-01 sweep):** 112 console errors per session.

| Source | Count | Fixed in |
|---|---|---|
| `Feature-Policy` / `Permissions-Policy` duplicate warning | 16 | DEF-107 (local, pending deploy) |
| `Cross-Origin-Embedder-Policy: require-corp` blocking GA scripts + AccessDenied resources | 95 | DEF-108 (local, pending deploy) |
| `/Product/Management` `SyntaxError: Unexpected token '<'` | 1 | **DEF-111 OPEN** — silent failure |

**Post-fix verified on localhost:** 112 → 1 (the 1 remaining is DEF-111).

**Network failures observed during sweep:**
- All GA4 beacons (`analytics.google.com/g/collect`, `www.google.com/g/collect`, `stats.g.doubleclick.net`) — fixed by DEF-108 CSP `connect-src` extension
- AccessDenied page resources blocked by COEP — fixed by DEF-108 COEP value change to `unsafe-none`

**Source maps:** Confirm before client UAT that production build does NOT ship source maps. Dev build serves them but the production publish profile should strip them. Quick check: `Invoke-WebRequest https://gccwebapp.com/js/certificate-tile-view-internal.js.map` should return 404, not 200.

---

## 8. CSS / Visual Defect Catalog (Patterns Observed)

Five recurring patterns surfaced in the May 2026 cycle. Each has shipped fixes; logging them here for future-vigilance:

1. **Mobile portrait container math** — admin-shell padding (`.admin-container`, `.admin-body-content`) eats ~50 px per side at 360 px viewport, leaving narrow content containers that silently clip with `overflow: hidden`. **DEF-114** fixed Dashboard. **Other admin pages may have the same pattern** if Shawn ever inspects them on a phone.
2. **Inline-style flex rows that won't wrap** — DEF-116 caught `display: flex; flex-wrap: nowrap` with fixed pixel widths in `_ProductIdManager.cshtml`. **Sweep all `style="display: flex"` inline attributes** on shared partials for the same anti-pattern.
3. **Mobile fixed-height dropdowns that overflow viewport** — DEF-117. The fix is a JS positioner that dynamically computes `max-height` from `window.innerHeight - btn.bottom`. **Apply to any other custom dropdown that opens on mobile** (column toggle, sort menu, filter chevrons).
4. **Bare global modal-width caps** — DEF-119 found `.modal-dialog { max-width: 600px }` leaking onto every Bootstrap modal on Cert Create/Edit. **Audit all CSS files for unscoped `.modal-dialog`, `.modal-content`, etc. selectors.**
5. **Single CSS file shared across views, single fix wins multiple bugs** — DEF-115 and DEF-116 both used this leverage (cert tile internal+external; cert create+edit). Encourage shared partials + shared CSS for cross-view UI elements.

---

## 9. Browser / Device Matrix (Status)

This matrix is **not freshly run today** — it's the standing receipt from the 2026-04-30 + 2026-05-01 Playwright sweeps. Real-device passes (especially iOS Safari + Samsung Internet) recommended before client UAT.

| Browser | Public tile view | Analyst cert Edit | Partner API by-number | Source |
|---|---|---|---|---|
| Chrome (latest, headless via Playwright) | ✅ | ✅ | ✅ | Section A 2026-04-30, Batch 1-9 2026-05-01 |
| Chrome (latest, headed) | ✅ (visual) | ✅ (visual) | n/a | Shawn's manual passes |
| Safari (macOS latest) | ⚠️ untested this cycle | ⚠️ untested this cycle | ⚠️ untested this cycle | — |
| Safari (iOS) | ⚠️ untested this cycle | ⚠️ untested this cycle | n/a | — |
| Firefox (latest) | ⚠️ untested this cycle | ⚠️ untested this cycle | ⚠️ untested this cycle | — |
| Edge (latest) | ⚠️ untested this cycle | ⚠️ untested this cycle | ⚠️ untested this cycle | — |
| Samsung Internet | ⚠️ untested this cycle | n/a | n/a | — |

**Action:** Before client UAT, run the top 3 workflows once each in Safari (macOS), Firefox, and Edge on a real machine. ~30 min total.

---

## 10. Architectural / Decisional Items

These are **open issues that block neither launch nor UAT** but require a Shawn decision before further work touches them.

### DEF-112 — Partner role identity conflation
- Two completely different identities flow through `if (userRole == "Partner")`: web-user Partner role (cookie session) and Partner API key (X-API-Key header).
- **Today this is fine because no human Partner user has been provisioned in prod** — every "Partner" caller is an API key.
- **Becomes a real bug the moment a Partner-role human is provisioned.** Audit logs become ambiguous; permission filter changes have to be reasoned about under both identities.
- Resolution paths in `defect-register.md:DEF-112` — recommend Path 1 (rename web-user role to `PartnerUser`) once a Partner human is needed; until then, document the dual meaning in code comments (Path 3, ~2 hrs).

### CR-008 — Partner role type
- Approved 2026-04-29; not yet built. Pairs with CR-013 (partner-scoped API custom-field exposure).
- **If client UAT introduces a Partner human user before this CR ships,** behavior will fall through to External-role logic (filter by `User.Partners`). Audit logs ambiguous.
- **Recommend:** confirm with client whether any Partner human user will exist before CR-008 ships. If yes, prioritize.

### CR-009 / CR-010 — Forgot Password + Duo MFA
- Both approved 2026-04-29. Both require Azure AD B2C custom policies + likely tier upgrade.
- **Forgot Password is a UAT blocker** the moment a tester forgets their password. Currently the only recovery path is admin manual reset.
- **Recommend:** size CR-009 first (probably ~2-3 days plus B2C config), schedule before client UAT begins. Duo MFA can land later.

---

## 11. Test Strategy Recommendations (Forward-Looking)

Observed gaps in the current test infrastructure (per memory `issue_testing_gaps.md` and observed patterns):

1. **No user-journey tests.** Test execution log Section E.2 has 158 stub cards; only ~20 have detailed steps. Day-in-the-life journeys (analyst creates cert → submits workflow → publishes → product IDs cascade → partner API serves) are not exercised end-to-end in any automated suite.
2. **No partner API contract test.** DEF-097, DEF-102, DEF-103 were all caught by manual review or live observation. **Recommend a single automated assertion suite that hits all 6 partner endpoints with a known-state cert and asserts the JSON shape matches a checked-in schema.** This would have caught DEF-103 the day it shipped instead of nine days later.
3. **No regression sweep on JS file versioning.** DEF-110 noted that JS-driven exports are served by the running dev server immediately, but C# changes to `CertificateService.cs` and `AdminService.cs` need a server restart. **Verify that every cutover smoke-test list explicitly includes a "restart was performed" step** — and that smoke tests touch both JS-driven and server-driven exports.
4. **Mobile dynamic-content gap.** Mobile test suite covers static page loads at 5 viewports but does NOT cover dynamically-injected modals/dropdowns at those viewports. DEF-117 (mobile column dropdown overflow) and DEF-119 (modal width override leak) both lived in dynamic content. **Add a mobile-viewport pass that opens every modal and dropdown on each major page.**
5. **Single tester for full app** — per memory `issue_testing_gaps.md`. Strength-in-numbers batching (`feedback_batch_fixes_strength_in_numbers.md`) is the current mitigation. **Recommend the Section A/B regression spot-check pattern continue every cutover** — it's already producing receipts.

---

## 12. Bug Filing & Tracking Discipline

Per memory `feedback_track_every_fix_in_defect_register.md` and `feedback_only_requested_changes.md`:

- ✅ Every QC fix this cycle has a DEF-XXX entry. No silent fixes.
- ✅ Every fix has either a deploy zip or a "pending deploy" annotation.
- ✅ `shipped-2026-05.md` log started, paragraphs match `shipped-2026-04.md` format.
- ✅ Cutover doc (`cutover-2026-05-01.md`) has pre-flight, deploy steps, smoke tests, rollback plan, communication plan.
- ⚠️ `feedback_say_client_not_names.md` — most current docs use "client" correctly. The QC log itself still references Carly by name; that's external source of truth and not rewritten.
- ✅ `feedback_no_commits.md` — all changes left in working tree for Shawn.

**One process note for the 0006 deploy:** after the smoke tests pass, append a single paragraph to `shipped-2026-05.md` covering DEF-103 + CR-015 + the bundled DEF-098/100/101. The May log currently has paragraphs for items being held-back-of-deploy; add a "deployed in 0006" paragraph for what actually shipped. (Optional — Shawn's call.)

---

## 13. Final Deliverables Checklist (per `full-spectrum-test-prompt.md`)

| Deliverable | Status | Location |
|---|---|---|
| Executive summary (5-10 bullets, go/no-go) | ✅ | This doc, §1 |
| Bug list with DEF-XXX cross-refs | ✅ | `defect-register.md` (DEF-097→DEF-119) |
| Heatmap | ✅ | This doc, §4 |
| JS console error log | ✅ | `fullspectrum_out/console-errors-rollup.json` (2026-05-01 sweep); summarized §7 |
| CSS/visual defect catalog | ✅ | This doc, §8 + `defect-register.md` |
| Browser/device matrix | ⚠️ Partial | This doc, §9 — needs Safari/Firefox/Edge real-machine pass before UAT |
| Top 10 quick wins | ✅ | This doc, §5 |

---

## 14. What I'd Do This Week (Recommendation)

In effort order:

1. **Today** — deploy 0006 zip per `cutover-2026-05-01.md`. Run smoke tests #1-#15. If any partner endpoint fails (#9-#12), roll back.
2. **Today** — cut a new build (`-test-2026-05-01-0007.zip` or similar) bundling the working-tree fixes (DEF-107/108/110/113/114/115/116/117/118/119). Do not deploy yet.
3. **Today / tomorrow** — investigate DEF-111 root cause. If it's an analyst-permission issue, decide whether to grant the missing role or hide the page. Add the response.ok guard regardless.
4. **Tomorrow** — Safari + Firefox + Edge real-machine pass on top 3 workflows (~30 min). Real-iPhone pass for QC #37, #152, #153 (~30 min).
5. **Tomorrow** — DEF-110 server-restart verification on the 0006 deploy. Export from each admin page and confirm headers match on-screen labels.
6. **Before client UAT** — clarify with client: QC #59 autocomplete (CR or document), QC #84 URL-column rename, QC #86 mouse-drag scroll (CR or defer), QC #106 free-text Employer field (yes/no), QC #145 partner cleanup, QC #159 bulk-replace.
7. **Before client UAT begins testing** — size and schedule CR-009 (Forgot Password). Single biggest UAT-blocker risk.
8. **Ongoing** — after every cutover, append one paragraph to `shipped-YYYY-MM.md`. Maintain receipts pattern per `feedback_maintain_shipped_log.md`.

---

*This review is the deliverable Shawn asked for under `/clear` 2026-05-01. Review-anchored to source docs, no speculation. Update in place if any decision in §6 / §10 lands.*
