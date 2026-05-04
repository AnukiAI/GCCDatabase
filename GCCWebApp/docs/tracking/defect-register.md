# GCC Defect Register

Canonical log of defects, bug fixes, and small corrective changes. Every QC fix gets a DEF-XXX entry here so we have a single audit trail.

Numbering continues from the pre-existing register (last known DEF-096 as of 2026-03-23). If an older version of this file is recovered from OneDrive or another location, merge entries by ID.

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

### DEF-130 — DATA-EXPOSURE: `/api/v1/products` leaks raw Product entity (internal IDs, nested category navigation, isDeleted flag) to Partner API keys

- **Date:** 2026-05-03
- **Severity:** **MEDIUM (S3)** — not in the contracted endpoint set; structural data exposure but no credentials or internal status
- **Status:** **Open** — fix deferred; needs `ProductPartnerDto` design and a check on whether Amazon currently consumes this route
- **Reporter:** Cycle 4 audit of v1 controllers reachable by Partner API key
- **Problem:** `Controllers/Api/v1/ProductsController.cs` is decorated with `[Authorize(AuthenticationSchemes = "ApiKey")]` at line 29 — Partner API keys can hit it. The list endpoint returns raw `Product` entities including:
  - Internal IDs (`id`, `categoryId`, `companyId`, `productTypeId`, `productSubcategoryId`)
  - `isActive`, `isDeleted` flags (internal lifecycle state)
  - Nested `category` object with `description`, `sortOrder`, `products: [null]` (circular nav reference)
  - Nested empty `productType`, `productSubcategory`, `company` references
  - Nested `certificates: [null]` array (circular)
- **Live evidence (Partner key → `GET /api/v1/products?pageSize=1`):** Returns 51 products (all of them — no partner scoping) with full internal structure visible.
- **Contract:** Products endpoint is NOT in the Amazon Developer Guide §4 or §8 contract list. The contracted partner endpoints are `/api/v1/certificates*` only. So we have two reasonable fix paths:
  1. **Drop Partner access entirely:** change auth to `[Authorize(Policy = "AnalystOrAdmin")]` (same as DEF-131 fix). This is the clean answer if Amazon doesn't consume it.
  2. **Add a partner DTO projection:** if Amazon does consume the products endpoint, project to a clean `ProductPartnerDto` with only public-safe fields (id, name, description, asin, upc, gtin, ean) and drop the nested entity navigation.
- **Why fix is deferred:** option 1 is a one-line lockdown; option 2 requires writing the DTO and updating the controller. The right choice depends on whether Amazon (or any other partner) currently calls `/api/v1/products`. That data lives in production logs, which I don't have access to in this dev environment. Recommend Shawn check production access logs or ask the partner-integration owner. If no calls in the last 30 days → option 1 (drop access). If calls exist → option 2 (DTO projection, treat as a Partner API contract change requiring sign-off per `feedback_amazon_api_immutable.md`).
- **Files (when fix is scoped):** `Controllers/Api/v1/ProductsController.cs`, plus optional new file `Models/Api/ProductPartnerDto.cs` for option 2.

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

- **Date:** 2026-05-03
- **Severity:** Low (S5 polish)
- **Status:** Open
- **Reporter:** Internal QA pass cycle 2 (visual sweep)
- **Symptom:** DEF-101 standardized Import = down arrow, Export = up arrow across admin pages. The convention is honored everywhere, but the icon class names diverge:
  - **`/Admin/UserManagement` and `/Admin/Tags`** use `fa-file-arrow-down` / `fa-file-arrow-up` (file-with-arrow icons)
  - **`/Admin/ProductManagement` and `/Company/Index`** use generic `fa-download` / `fa-upload` (tray icons)
- **Why it's polish-tier:** Both classes are visually down/up arrows so the DEF-101 spec ("Import = down arrow, Export = up arrow") is technically satisfied. Visual consistency is just slightly off — paged-side-by-side, the icons look different. No user is going to be confused, but design QC will notice.
- **Fix:** Align all Import/Export buttons to use `fa-file-arrow-down` / `fa-file-arrow-up` (the file-specific variants are clearer). Touch points: `Views/Product/Management.cshtml`, `Views/Company/Index.cshtml`, plus any partials they include.
- **Files:** `Views/Product/Management.cshtml`, `Views/Company/Index.cshtml`

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

- **Date:** 2026-05-01
- **Severity:** Medium (no current bug, but the dual meaning makes future changes risky and the model is ambiguous to operators)
- **Status:** **Open** — flagged during a walkthrough of the User Management modal; needs Shawn's call before any fix
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

- **Date:** 2026-05-01
- **Severity:** Medium (silent functional failure; the user sees an empty/broken product list with no actionable error)
- **Status:** **Open** — surfaced during DEF-108 verification sweep, not fixed in this cycle
- **Reporter:** Anuki internal full-spectrum QA pass (2026-05-01)
- **Symptom:** Logged in as analyst, navigating to `/Product/Management` produces this console error: `Error loading products: SyntaxError: Unexpected token '<', "<!DOCTYPE "... is not valid JSON`. The page renders but the products table is empty / not populated. No visible error toast or banner — the user has no idea anything went wrong.
- **Suspected root cause:** An AJAX fetch on the Product Management page is hitting an API endpoint that returns HTML (almost certainly the login page or AccessDenied page after a session check) and the JS is calling `response.json()` without first checking `response.ok` or `Content-Type`. Two possible upstream causes:
  1. The endpoint requires Admin role and the analyst silently 403s/302s to a login page.
  2. There's a session/CSRF-token mismatch and auth middleware redirects the AJAX call to login.
- **Why it matters:** Real defect for analysts trying to view products (one of the top-3 critical workflows). User sees "page is broken" with no feedback. Silent failure also masks data-integrity issues if downstream code assumes the product list loaded.
- **Repro:** Login as `analyst` / `TestPass123!`, navigate to `http://localhost:5101/Product/Management`, open DevTools console.
- **Investigation needed (NOT done in this cycle):**
  1. Find the failing fetch URL via Network tab.
  2. Check whether endpoint requires `[Authorize(Roles="Admin")]` or similar — if so, hide the page from analyst nav OR add Admin/Analyst to the policy.
  3. Add `response.ok` / content-type guard to the JS so failures surface visibly via `AdminCore.showNotification('error', ...)` instead of console.
- **Files (pending investigation):** likely `Views/Product/Management.cshtml` JS block + whatever controller endpoint it calls.
- **Found by:** `fullspectrum_out/console-errors-rollup.json` (2026-05-01 verification run for DEF-107/108 fix).
- **Deploy:** Open

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

- **Date:** 2026-05-01
- **Severity:** Low (warning only; no functional break)
- **Status:** **Fixed (local) — pending deploy.** Verified on `localhost:5101` 2026-05-01: the 16x warning entry no longer appears in `console-errors-rollup.json`.
- **Reporter:** Anuki internal full-spectrum QA pass (2026-05-01)
- **Symptom:** Every page load (login, dashboard, public tile view, internal tile view, external tile view, all admin pages) emits this Chromium console warning: `Error with Feature-Policy header: Some features are specified in both Feature-Policy and Permissions-Policy header: camera, microphone, geolocation, payment, usb. Values defined in Permissions-Policy will be used.` Total warnings logged across the sweep: 16 across 11 distinct page URLs.
- **Root cause:** `Middleware/SecurityHeadersMiddleware.cs` lines 91 and 95 emit BOTH headers with overlapping directives. The comment on line 94 says "for older browsers" — but every browser that supports the cert tile view JS (ES2017+ async/await, fetch, etc.) supports `Permissions-Policy`. Sending both is redundant and produces the warning.
- **Why it matters:**
  1. Console-error noise during QA — masks real defects.
  2. The redundancy implies older-browser support that the rest of the codebase doesn't actually deliver.
- **Proposed fix (one-line):** Remove the `Feature-Policy` `Append` call (lines 94–96) in `Middleware/SecurityHeadersMiddleware.cs`. Keep `Permissions-Policy`.
- **Files touched (proposed):** `Middleware/SecurityHeadersMiddleware.cs` lines 94–96
- **Verification (when fixed):** Re-run `fullspectrum_ui.js`. The 16x warning entry should drop out of `console-errors-rollup.json`.
- **Found by:** `fullspectrum_out/console-errors-rollup.json` produced by the 2026-05-01 full-spectrum sweep.
- **Deploy:** Open

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
- **Status:** Fixed — pending deploy
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
- **Status:** **Fixed locally + dev DB backfilled + PROD DB backfilled 2026-05-01 — app deploy pending.**
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

## Closed / Historical

*(Entries from the pre-existing register to be merged here if recovered.)*
