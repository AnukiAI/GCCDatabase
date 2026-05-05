# Shipped — May 2026

Snapshot of delivery for May 2026. Stakeholder-readable log.

Period: 2026-05-01 → ongoing

---

## Certificate tile view — Company Email and Company URL renamed and pinned to the end of the table (2026-05-05)

The certificate tile views (both the analyst-facing internal view and the external partner view) carried a tangle of legacy labels for the company-contact columns. The internal view had a header reading "Company Contact Email/URL" and a separate header reading "Contact URL." The external view said "Company Contact" for the same email column and used "Company URL" for the URL column. The column toggle dropdown said "Company Contact" in some places and the columns themselves landed in the middle of the trailing block — sandwiched between EAN and the "Additional" group of cert-results / pathway / cert-subcategories — which meant Cert Results and Pathway and Subcategories columns were rendering AFTER the contact columns in the table. Custom admin-managed fields, when present, were appended at the very end of the row, so the visible column order was a confused middle-block of identifier and contact columns followed by custom fields at the right edge.

The client request was direct. Use "Company Email" for the email column and "Company URL" for the URL column, and put both of them at the end of the table, with every other column — including any admin-defined custom fields — sitting before them. Two columns, two labels, last two positions.

The fix touches only the certificate tile table surfaces. On both the internal and external Razor views, the sticky header cells for the email and URL columns are renamed to "Company Email" and "Company URL" respectively, and the two cells are reordered so they appear in that order at the very end of the header row, after Cert Subcategories. The column-toggle dropdown is similarly relabeled and reordered, and the now-redundant "Contact" section divider is folded into the existing "Additional" group so the dropdown reflects the same order as the table. The body-row template in the matching internal and external JavaScript renderers is reordered to match, so each rendered row's last two cells are Company Email and Company URL.

The custom-field placement is the second piece. The internal view's JavaScript renders custom-field cells per cert via `renderCustomFieldCells`, and that call was sitting after the static columns in the body-row template — so any admin-defined custom field rendered to the right of Company URL. The same applied to header injection: the dynamic header-cell builder for custom fields was using `headerInner.appendChild`, which always lands the new cell at the end. Both sites are changed. The body template now runs `renderCustomFieldCells` between Cert Subcategories and Company Email, and the header injection now does an `insertBefore` against the Company Email anchor cell. Custom-field columns therefore sit immediately before Company Email, with the standard static columns ahead of them. If the anchor is ever absent (a defensive guard against a future refactor), the code falls back to the previous append behavior so the headers still appear rather than silently disappearing.

Scope discipline. Two over-reach changes from the first pass were reverted before this shipped. The CSV export header in `CertificateService.cs` had been edited to match the new labels, and the `Display(Name=...)` attributes on `CertificateViewModel.CompanyContactEmail` and `CertificateDetailsViewModel.CompanyContact` had been edited similarly — both were rolled back to original values. The reasoning was that those surfaces feed downstream consumers (downloaded CSV files in stakeholders' inboxes, label rendering on Edit / Create form fields and validation messages), and the request was about the table view specifically. The C# property names (`CompanyContactEmail`, `CompanyContactURL`), the API sort keys (`data-sort-column="companyContact"` and `="companyUrl"`), the CSS class names (`tc-contact`, `tc-companyurl`), and the database column names are all unchanged — only the user-visible header text and the column position in the table moved. Backend sort APIs, column-width CSS, and the partner API contract are all unaffected.

Verified live on `http://localhost:5101` against both views as analyst and external user. Internal view total 24 sortable columns plus 1 custom field = 27 cells; the last three columns are `tc-cf-33` (the seeded test custom field), then `tc-contact` ("Company Email"), then `tc-companyurl` ("Company URL"). External view 23 sortable columns; last two cells are Company Email and Company URL with no custom fields injected because the external view does not surface custom fields. Zero JavaScript console errors. Header DOM directly inspected after page render; the `[Display]` attribute regression check on Edit-Certificate forms came back clean (label still reads what it always did because the model attributes were reverted).

The public anonymous tile view (`Views/Certificate/CertificateTileView.cshtml`) was not touched. The client request was specifically about the tables analysts and external users see, and the public view has a different layout that didn't surface the same complaint. If parity is wanted there later, it's a small follow-on.

Files (4): `Views/Certificate/CertificateTileViewInternal.cshtml`, `Views/Certificate/CertificateTileViewExternal.cshtml`, `wwwroot/js/certificate-tile-view-internal.js`, `wwwroot/js/certificate-tile-view-external.js`.

---

## Verifications run today on previously-shipped QC items (2026-05-05)

The QC items below were already coded, committed, and deployed to the test environment in earlier work. They are listed here separately from the shipped section because no new code changed today on these line items — what we did today was independent verification (source + live test environment + localhost) confirming the fixes are present and behaving as intended. This section exists so the trail is auditable: when the client retests these items in their next UAT cycle and asks "did anything change," the answer is no, the fix has been in place since its original ship date and was confirmed working again today.

**QC #80 — Product ID modal save/edit on Certificate Create.** The original defect was three failure modes stacked: (1) the modal save button collected empty form data because the wrapping element is a `<div>` not a `<form>`, so `new FormData(e.target)` returned nothing and saves silently failed; (2) clicking Edit on a not-yet-saved (temp) row hit the API at `/api/productids/-NNNN` with a negative ID and 404'd; and (3) repeated open/save/open of the modal stacked Bootstrap backdrops and progressively darkened the page. All three are fixed in `wwwroot/js/product-id-manager.js` — `bootstrap.Modal.getOrCreateInstance` instead of `new bootstrap.Modal` to prevent backdrop accumulation, individual `getElementById` reads to collect form values reliably, a negative-ID branch on Edit that loads the temp item from the in-memory array instead of calling the API, and an explicit backdrop cleanup tagged `// QC #80` after each temp save. Source diff is committed (last touched in commit `98ac5f0`). Today's verification: pulled the JS file from `https://gccwebapp.com/js/product-id-manager.js` with the test-env basic-auth credentials and confirmed all four fix markers are present in the deployed bytes. Then re-ran the full Playwright scenario on `localhost:5101` as `analyst` — added two product IDs back-to-back, edited the first temp row, and asserted on every checkpoint. Backdrop count zero after each of the three modal-close events. Body `modal-open` class cleared. Page outside the modal still editable (no invisible overlay). Edit modal pre-filled with the original ASIN value `B0QC80AAAA` from the in-memory store. Zero JavaScript console errors across the full run. Status remains Ready for UAT for the client to perform their own functional pass.

**QC #38 / QC #58 — Contact column position on the internal certificate tile view.** The original defect was that the Company Contact Email column (`tc-contact`) had been authored sitting right after the Company column, in the middle of the table, where it made no semantic sense — the related Contact URL column was at the far right end, and users wanted the contact-related columns grouped near each other and away from the core company / product / cert columns. The fix was committed in `f964e67` ("updates, table updates," 2026-03-16) and moved the Company Contact column from position 9 to position 20, immediately after EAN, with a new "Contact" section divider in the column-toggle dropdown to group it. Today's verification: source-side, the sticky header cells (lines 386–391 of `CertificateTileViewInternal.cshtml`), the body-row template in the matching JavaScript renderer (lines 1759–1764 of `certificate-tile-view-internal.js`), and the column-toggle dropdown in the cshtml (lines 345–352) all show the Contact column in the post-fix position with consistent ordering across header, body, and toggle. Live verification on `localhost:5101` walked the rendered DOM with Playwright, dumped the full 24-column header order, and confirmed the trailing block reads ASIN → UPC → GTIN → EAN → Company Contact → Cert Results → Pathway → Cert Subcategories → Contact URL, exactly as the fix description states. (As of today, the same two columns have been further moved to the last two positions of the table and renamed to "Company Email" and "Company URL" — captured in the shipped entry above this section. The QC #38 / #58 verification was run before the rename / reorder change, on the previously-shipped state.) Status remains Ready for UAT.

Both verifications were dry runs against the existing deployed fix to confirm no regressions accumulated since original ship, and to document the receipts so the next testing cycle can reference them. No new code or behavior change came out of this work.

---

## DEF-112 — "Partner" role separated cleanly between API-key callers and web users (2026-05-04)

The cert-listing controllers had a long-standing dual-meaning problem on the "Partner" role. The role meant two different things depending on how the request arrived. A human user could be assigned the Partner role via the User Management modal, log in with email and password, and see certificates filtered by their assigned partner names. An automated request from Amazon would hit the same `/api/v1/certificates*` endpoints with an API key, and the API key middleware would inject the same Partner role claim into the request, and the same filter branch would fire. Both identities flowed through the same `userRole == "Partner"` checks across `CertificateTileExternalApiController` and `CertificatesController`. The audit trail couldn't distinguish them, future filter changes had to be reasoned about under both identities at once, and the partner immutability rule on the Amazon API contract was sharing code paths with internal-app web-user logic.

The original report (DEF-112) framed this as needing an architectural decision and laid out four resolution paths. We pulled the prod user list before deciding and confirmed that across all 23 users in the production database, zero have the Partner role. The role was always intended for human Partner-org users (e.g., Amazon staff with portal logins) but no such users were ever created. Path 4 was the cleanest answer: eliminate the web-user side of the role entirely.

The fix is small. The Roles row for Partner gets soft-deactivated by setting `IsActive = false`. The User Management role-list endpoint already filters its dropdown on `IsActive`, so admins can no longer accidentally assign the role to a future human user. The Partner API key middleware is unaffected because it injects the role claim directly on each request without ever reading the Roles table — so partner API authentication continues to work exactly as before. The cert-listing `userRole == "Partner"` filter checks across both controllers are also untouched and still correct. They simply never fire for cookie-authenticated callers anymore because no human has the role. If a future requirement needs human Partner users, the change is reversible: re-run the SQL with `IsActive = 1` and the role reappears in the User Management dropdown.

Three small source changes accompany the data fix. `SeedData.cs` now declares the Partner role with `IsActive = false` and the new description so fresh database initialization matches the prod state. The `PartnerApiKeyAuthenticationMiddleware` claim-injection site picked up a comment block marking it as the single source of truth for the Partner role claim, with a pointer to DEF-112 for context. The SQL data-fix script is idempotent and includes a defense-in-depth abort if any human users currently have the Partner role; we verified it runs cleanly against the development database with the BEFORE/AFTER snapshot showing the IsActive flip.

Prod SQL run is pending — the script lives at `Database_Scripts/20260504_DEF112_DeactivatePartnerRoleForHumans.sql` and is safe to run on the production database whenever convenient. Shawn will execute it via the existing manual-migration workflow used for DEF-134's earlier today.

Files: `Data/SeedData.cs`, `Middleware/PartnerApiKeyAuthenticationMiddleware.cs`, `Database_Scripts/20260504_DEF112_DeactivatePartnerRoleForHumans.sql`.

---

## DEF-135 — Google Analytics now allowed by Content Security Policy on the test environment (2026-05-04)

The site has had Google Analytics configured for months. An admin enabled the property `G-16J12CPD8S` through the application's own admin page, the layout correctly injected the `gtag.js` script tag on every rendered view, and zero analytics signals were being collected because the browser was blocking the script load.

The cause was two Content Security Policy headers being emitted on every response and disagreeing with each other. The IIS host emits one CSP header from `web.config`, and the application emits a second CSP header from its security headers middleware. Browsers handle two CSP headers by intersection — every directive uses the most-restrictive value across all sources. The application's middleware-level CSP had already been updated at some point to include the Google Tag Manager and Google Analytics domains in `script-src` and `connect-src`. The IIS-level CSP in `web.config` had not. So the intersection allowed everything the IIS-level CSP allowed, which did not include Google, and the script load was blocked.

The fix is to bring `web.config`'s CSP in line with what the middleware already permits. `script-src` now includes `googletagmanager.com` and `google-analytics.com`. `connect-src` now allows the GA4 beacon hosts that the analytics library calls home — `analytics.google.com`, region-specific `*.google-analytics.com`, `www.google.com`, `stats.g.doubleclick.net`, and `googletagmanager.com`. The two CSP sources now agree.

Per-environment scoping is handled at the database level, not the CSP level. The `GoogleAnalyticsSettings.IsEnabled` row in the database is the on/off switch — the layout only injects the script tag when that flag is true. The same web.config ships to every environment, so CSP allows the Google domains everywhere, but real production can keep `IsEnabled = false` and the script tag is never emitted, so the analytics library never runs. The test environment currently has `IsEnabled = true` because the property `G-16J12CPD8S` was registered for analytics validation. This pairs with the Bing and Google search-console removal tickets Shawn submitted today — the test domain is being deindexed from public search, and analytics on the gated test environment does not leak into discoverable web traffic.

Out of scope but worth noting. Maintaining two CSP sources that have to be kept in sync is fragile, and the drift between them is exactly what caused this bug. A later cleanup pass should consider removing the IIS-level CSP entirely and treating the application middleware as the single source of truth, since the middleware version is environment-aware and can branch on dev vs production whereas a static web.config cannot. That refactor is intentionally not part of this fix.

Files: `web.config`.

---

## DEF-136 — Product Management: false-positive "Error saving product" toast on every successful save (2026-05-04)

After DEF-134's deploy enabled Type and Subcategory persistence on the Product Management page, every successful Add or Edit Product still surfaced a red error toast saying "Error saving product." The product was actually being saved correctly — verified against the prod test database, where three rows Shawn created during testing minutes earlier all persisted with the right type and subcategory IDs. The error toast was a false positive.

The cause is in the Product Management page's inline JavaScript. The success branch of the save handler called two notification functions back-to-back: a stale `showNotification` (a residue of an unfinished in-page notification queue system) and the working `showToast` (which routes through the shared admin notification system). The stale `showNotification` function references a `notificationQueue` global that was never declared at the top of the file, so calling it threw a `ReferenceError`. The save function's outer `catch` block then swallowed the JavaScript error and called `showToast('Error saving product', 'error')`, painting a misleading red toast over what had actually been a successful save.

The fix removes the stale `showNotification` call from the success branch. The very next line — `showToast(message)` — already handles the success notification through the shared admin notification system that does work, so functionality is preserved with no replacement needed.

Out of scope but worth flagging. The dead in-page notification queue scaffolding is still present in the file — a half-finished feature with about a dozen unwired functions. It does no harm now that nothing calls into it, but a future cleanup pass should excise the dead code or consolidate it onto a shared helper. The same pattern may exist in other admin views; not chasing it under this defect.

Files: `Views/Product/Management.cshtml`.

---

## DEF-134 — Product Management Type and Subcategory now actually save (2026-05-04)

The Product Management table added Product Type and Subcategory columns under QC #91 back in March, marked Ready for UAT, and the columns rendered correctly. On 2026-05-04 the client reported the columns were blank for every product even when the user had filled them in on the Add or Edit modal. A check of the dev database confirmed it: every one of the 51 products had `NULL` for both `ProductTypeId` and `ProductSubcategoryId`, including products created after the cascade dropdowns shipped. Two independent root causes were eating the data.

The first was a save-path silent wipe. The Edit Product modal's JavaScript only pre-filled the name and description fields when the modal opened. Company, Category, Product Type, and Subcategory dropdowns all stayed empty. When the user hit Save without manually re-picking each one, the controller blindly overwrote those four fields with the empty values. Every Edit operation was a silent erasure of the four fields that weren't pre-filled. The fix pre-fills all four dropdowns from the loaded product when the modal opens, awaiting the subcategory cascade fetch before showing the modal so the user sees the existing selection rather than an empty dropdown.

The second was a foreign-key target mismatch in the data model. The `Product.ProductSubcategoryId` column was foreign-keyed to a `ProductSubcategories` table, but that table is empty and no part of the application populates it. The cascade dropdown in the modal actually fetches subcategory options from `ProductTypes` Level-2 child rows via the `/Certificate/GetProductSubcategories` endpoint. So when the user picked a subcategory in the modal, the value sent to the backend was a `ProductTypes.Id` — and the FK constraint rejected it because the value didn't exist in the `ProductSubcategories` table. The save threw, the exception was caught and rendered as a generic error toast, and the user couldn't tell why their product wasn't saving. The Certificate model already correctly treats subcategory as a `ProductType` reference (matching the actual data flow); only the Product model was inconsistent. The fix retypes `Product.ProductSubcategory` from the empty side-table to `ProductType?` and ships an EF migration that drops the old FK and adds a new one targeting `ProductTypes`. No data conversion was needed because every value was already NULL.

Two follow-on fixes in the same change set. The product-type lookup in the CSV import path was scoped to Level-1 entries only, to avoid accidentally matching a child row sharing a name with a parent. The subcategory lookup in the import path was repointed from the empty table to `ProductTypes` Level-2 children. The Product API response was extended to include `companyId`, `categoryId`, `productTypeId`, and `productSubcategoryId` alongside the existing name fields, so the Edit modal has the IDs it needs to pre-select dropdown options.

Verified end-to-end on `localhost:5101` with admin credentials. Created a product with category Chemicals, type Plastics, subcategory HDPE; saved successfully; the management table renders Plastics and HDPE in the new columns; the database row shows `ProductTypeId=4` and `ProductSubcategoryId=46`. Re-opened the same product in the Edit modal and confirmed all four dropdowns pre-fill to the saved values. The migration applied cleanly and `sys.foreign_keys` confirms `FK_Products_ProductTypes_ProductSubcategoryId` now points at the `ProductTypes` table.

Out of scope. The 51 existing products in dev still have NULL for both fields — backfilling legacy product records is a data action and would need a CR. The orphaned `ProductSubcategories` DbSet and Repository pointer are left in place to keep this change set focused on the user-visible defect; they can be retired in a later cleanup. QC #91 stays Ready-for-UAT — the column-rendering work shipped in March was correct; this defect was the orthogonal save-path bug that left the cells empty.

Files: `Models/Product.cs`, `Models/ProductSubcategory.cs`, `Data/ApplicationDbContext.cs`, `Controllers/ProductController.cs`, `Views/Product/Management.cshtml`, `Migrations/20260504204044_RetargetProductSubcategoryFkToProductTypes.cs`.

---

## DEF-130 — Partner API products endpoint locked down to internal-only access (2026-05-04)

The `GET /api/v1/products` endpoint had been authorized for Partner API keys since the v1 controllers were first built. The endpoint returned raw Product database entities including internal database IDs, the soft-delete flag, the active flag, a circular nested category object containing all of its products as null entries, and circular nested certificate references — none of which are part of any partner contract. The endpoint is not listed in the Amazon Developer Guide §4 or §8 contract; the contracted partner endpoints are `/api/v1/certificates*` only. The data exposure was identified during a v1 controller audit as DEF-130.

Two fix paths were on the table. The clean lockdown — change the controller-level authorization attribute from the API-key scheme to the cookie-based Analyst-or-Admin policy — and the contract-track path of writing a partner DTO projection. The DTO path would have been needed only if Amazon was actually consuming the endpoint, in which case the change would also have required partner-side sign-off per the immutability rule on the Amazon contract. We confirmed via the dev database that Amazon's partner key has no endpoint restriction configured, so they CAN structurally call the endpoint, but the only way to know whether they DO is the production stdout logs — which live on the Plesk box and were not accessible from the development machine.

Shawn's call was to ship the lockdown immediately. The change is fully reversible within minutes if Amazon's logs come back showing usage, and `/api/v1/products` is not contractually bound, so the immutability rule does not apply here. The lockdown removes the data exposure today rather than waiting on a multi-day contract negotiation for an endpoint that may not be in use at all.

The fix is a single-line attribute swap on `Controllers/Api/v1/ProductsController.cs`. We also confirmed via repo-wide search that no internal frontend JavaScript, Razor view, or backend service calls `/api/v1/products` — the only references are the controller's own self-reference for the `Created` response, a code comment in the partner middleware, a couple of example strings in API documentation, and the admin API documentation page (which still works because admin cookie auth still satisfies the new policy). Internal consumer impact: zero.

Verification of the prod usage question is still pending — Shawn will pull `logs/stdout*.log` from the Plesk app folder and grep for `/api/v1/products`. If any partner calls show up, this fix is reverted and the partner-DTO path becomes a new CR routed through Carly and Kyle. If clean, the lockdown stays.

Files: `Controllers/Api/v1/ProductsController.cs`.

---

## DEF-127 — Import/Export icon convention now consistent across Product and Company pages (2026-05-04)

DEF-101 standardized the rule that Import buttons use a down-arrow icon and Export buttons use an up-arrow icon across the admin pages. The convention was honored on every page, but four of the pages — Product Management, Admin Product Management, Company Index, and Product Categories — were using the generic tray-style `fa-download` and `fa-upload` icons rather than the file-with-arrow variants `fa-file-arrow-down` and `fa-file-arrow-up` used on Tags and User Management. Both icon classes satisfy the down/up direction rule, so this was a low-severity polish item, but design QC noticed it when the pages were paged side-by-side.

The fix swaps the icon class on the Import button, the Export button, and the Import Preview modal header on each of the four pages. The four pages now match the file-with-arrow icon style already used on Tags and User Management. No JS or controller changes — purely the icon class on a handful of `<i>` tags.

Out of scope: the admin pages with Export-only buttons — Certificate Fields, Certificate Types, Partners, Pathways, Role Management, Product Types, and CSF Attributes — still use the tray-style `fa-upload` icon. They satisfy the DEF-101 direction rule and were not flagged in this defect, so they are left alone. Standardizing them onto the file-arrow style is a separate polish ticket if it comes up. The `Views/Product/ManagementAlt.cshtml` and `ManagementV2.cshtml` alt views are not on the sitemap and were also left alone.

Files: `Views/Product/Management.cshtml`, `Views/Company/Index.cshtml`, `Views/Admin/ProductManagement.cshtml`, `Views/Product/Categories.cshtml`.

---

## DEF-111 — Product Management page now surfaces a real error when the session expires instead of a silent broken table (2026-05-04)

The Product Management page has had a silent-failure defect since the 2026-05-01 verification sweep. When a user's cookie session expires while they have the page open and the page's JavaScript fires its background `loadProducts` fetch, the browser auto-follows the auth redirect to the login page. The login page returns HTTP 200 with HTML, the JS sees `response.ok === true` and calls `response.json()`, the JSON parser chokes on the leading `<` of `<!DOCTYPE`, and the `catch` block writes a `console.error` and returns. The user sees an empty product table and no toast or banner — the page just looks broken.

The fix updates the `loadProducts` function in the page's inline JavaScript to read the response's content type before calling `response.json()`. If the response was redirected or the content type is not `application/json`, the code shows a real `AdminCore.showNotification` toast — "Your session has expired. Reloading…" — and triggers a page reload after a brief delay, which re-enters the auth flow cleanly and lands the user back on the page after they re-authenticate. The non-OK and exception branches were also updated to surface a notification toast instead of writing only to the console, so any future fetch failure on this page is visible to the user instead of swallowed.

The same silent-failure pattern exists in four other fetch sites in this same file — initial-data loaders for categories, types, and dropdown sources — and on a number of other admin pages. They were not in scope for this defect and were not modified. If the auth-redirect symptom shows up elsewhere, those will be addressed individually or refactored into a shared helper.

Verified on source review and against a live `fetch('/Account/Login')` from the running app, which confirms the login page responds with `redirected: true` and `Content-Type: text/html`, exactly the pair the new guard checks for. The runtime behavior of the patched page renders only after the dev server is restarted; this project does not have Razor runtime view compilation enabled.

Files: `Views/Product/Management.cshtml`.

---

## DEF-121 (followup) — Product ID API now returns a DTO instead of the raw entity, removing the User surface entirely (2026-05-03)

The Product ID management endpoints under `/api/productids/*` have been the source of a known credential-exposure defect since 2026-05-01. The root cause was that they returned the full `CertificateProductId` database entity, which transitively pulls in the full `User` entity for the `CreatedByUser` and `UpdatedByUser` navigation properties. The `User` entity has accumulated credential-tier fields over time — password hash, two-factor secret, recovery codes, security-question answers, an IP allowlist, the password-reset token, password history — and any one of those serializing into an API response is an account-takeover risk for every user who has ever created or modified a product ID, almost always the admin. The endpoint requires Analyst-or-Admin authorization, so the leak was not anonymous, but a single Analyst account compromise would have yielded the admin password hash and 2FA seed.

The original 2026-05-03 stopgap added `[JsonIgnore]` attributes to the eight sensitive `User` fields, which blocks serialization across the entire app. That stopgap stays in place. This followup is the durable fix originally recommended in the defect register: a dedicated response DTO that has no `User` entity surface at all.

A new `ProductIdResponseDto` mirrors every public field on the entity and exposes a minimal `{ id, username }` user object for the Created By and Updated By columns — no email, no role, no audit fields, and no surface area for any future credential field added to the `User` model. All four read and write endpoints on the controller now project through this DTO before returning. The service layer still works in entities for internal callers, like the CSV exporter and the cascade that syncs product-ID status to its parent certificate.

The frontend was not changed — the DTO preserves the exact field names the JavaScript consumer reads when rendering the Product ID Manager table and the Add/Edit modal. Verified live on the dev environment with the admin cookie. The Created By column still renders "admin," the Edit modal still pre-populates all six fields from a saved row, a round-trip PUT works and returns the DTO with no leaked fields, and an explicit forbidden-fields scan over every JSON response confirms no password hash, no 2FA secret, no recovery codes, no security questions, no IP allowlist, no reset tokens, and no password history appear anywhere in the response — even where they previously would have leaked through the nested User object.

The defect register entry for DEF-121 captures the verification details, including the specific endpoints exercised and the response field shape. The defect status now closes both the original symptom and the followup-DTO ticket originally split out from it.

Files: `Models/CertificateProductId.cs`, `Controllers/Api/ProductIdApiController.cs`.

---

## DEF-131 — Bulk-mutation API endpoints no longer accept Partner API keys (2026-05-03)

A QA audit of the v1 API surface this afternoon turned up a security defect in the bulk certificate management endpoints. The `BulkCertificatesController` exposes four operations — bulk create, bulk update, bulk delete, and bulk validate — and was authorized at the class level with the API-key authentication scheme, the same scheme used by the contracted partner read endpoints. The practical consequence: any holder of a valid partner API key, including the Amazon production key, could submit a POST to bulk-create up to 100 certificate records, send a PUT to bulk-update existing certificates, or send a DELETE to remove certificates by ID list. None of these operations are part of the partner contract — partners are read-only consumers per the Amazon Developer Guide — and there is no internal feature in the codebase that calls these endpoints either, so they were sitting as a dormant attack surface.

We confirmed the issue with a live curl: posting an empty array to the bulk-create endpoint with the dev partner API key returned a 400 with the body "No certificates provided." The 400 is the controller's empty-body branch, which means the authentication check passed before that branch ran — a real payload would have been processed and persisted to the database. Bulk update and bulk delete behaved the same way under the same key.

The fix swaps the class-level authorization attribute from the API-key scheme to the `AnalystOrAdmin` policy, which forces cookie-based authentication via the default scheme. After the change, all four endpoints reject any caller not in the Analyst or Admin role with a 403 response. We confirmed the regular partner read endpoints (`GET /api/v1/certificates` and friends) are unaffected and continue to return 200 with the contracted DTO shape. We also grep-verified that no code in the repository — frontend JS, backend service, or test code — calls the bulk endpoints, so the lockdown has zero functional impact internally. If admin tooling needs bulk operations in the future, it can authenticate via cookie like every other admin route.

Files: `Controllers/Api/v1/BulkCertificatesController.cs`.

---

## DEF-129 — Partner API "search certificates" endpoint now returns the contract DTO instead of leaking the raw entity (2026-05-03)

A continuation of the same audit that found DEF-131 also turned up another endpoint in the certificates controller that was returning the raw database entity instead of the partner contract DTO. The `GET /api/v1/certificates/search` route — which accepts a search term and various filters — was still using the older raw-entity response shape. Like the `/expiring` endpoint we fixed earlier today, it was leaking internal database IDs, the raw lifecycle status string ("Client Approved - Paid" instead of the contracted literal "Active"), the raw country value instead of "All", flat `asin/upc/gtin/ean` fields instead of the `productIds[]` array, and a nested `company` object that included contact email, website, and a circular nav reference back to certificates and products.

This is the fourth occurrence of the same bug class. DEF-103 (April 21) caught the original by-id and by-number leaks. DEF-124 caught the `/expiring` leak this morning. The audit run this afternoon caught `/search` — the partner endpoint set is small enough that we can now say with confidence the gap is closed, since this was the last raw-entity-returning route reachable by an API key. The fix uses the same shared `BuildPartnerCertificateDto` helper that the other endpoints already use, applies the public-status filter so partner callers cannot see drafts or holds, and scopes results to the partner association on the API key.

We verified the fix with a live curl using the dev Amazon partner key. The same query that previously returned the raw entity now returns a clean DTO with `status: "Active"`, `certificationCountries: "All"`, populated company name, and an empty `productIds[]` array (no Amazon cert in the dev seed has product IDs assigned — coverage gap noted separately). Internal admin and analyst dashboards that call the same endpoint are unaffected and continue to receive the full entity for their own internal lifecycle reporting.

A followup ticket has been opened to add a controller-level unit test that asserts every read endpoint in `CertificatesController` returns the contract DTO when the caller is a Partner. That gives us automated regression coverage so future endpoint additions can't introduce another leak in the same class.

Files: `Controllers/Api/v1/CertificatesController.cs`.

---

## DEF-132 — Self-caught regression in the DEF-121 stopgap; Partner API authentication restored (2026-05-03)

While running the cycle 4 audit, the very first curl against `/api/v1/certificates/search` came back as a 500 error with the log message "Error during Partner API key authentication" and an inner exception about a JSON property `passwordHash` being marked required but not having a setter. The defect was caused by this morning's DEF-121 stopgap: we had added `[JsonIgnore]` to the `PasswordHash` property on the `User` entity, but the property was also declared with the C# `required` keyword. System.Text.Json cannot reconcile those two — the `[JsonIgnore]` removes the property from JSON deserialization, while the `required` keyword still expects the JSON to provide a value — and any code path that round-trips a User object through JSON (in this case, a cache or session lookup inside the partner authentication middleware) throws on deserialization.

The fix dropped the `required` keyword from the `PasswordHash` property. Initialization safety is preserved by EF Core's NOT NULL database constraint and by every `new User { ... }` site in the code already explicitly assigning the field — we grep-verified all 14 construction sites in `SeedData` and `AdminService` set `PasswordHash` immediately. After the change, partner authentication works again on every endpoint in the v1 controller, including the previously-broken `/search`. The DEF-121 credential-leak protection is preserved: serialization output of any User object still excludes `passwordHash` and the seven other annotated fields.

This lesson is being captured on the followup DTO refactor ticket. The `[JsonIgnore]` stopgap is cleaner with non-required nullable string properties, and the durable fix (a `UserResponseDto` projection that allowlists only the fields safe to serialize) sidesteps this kind of attribute-vs-keyword conflict entirely. The stopgap remains in place for now and is sufficient for the immediate exposure, but the DTO refactor is the right durable pattern.

Files: `Models/User.cs`.

---

## DEF-128 — Anonymous CSV export of certificate data no longer exposes internal tags or accepts non-CSV formats (2026-05-03)

The public certificate-tile pages have always offered an "Export" button that sends an HTTP request to a server endpoint and downloads a file. During an internal QA pass we discovered three issues with that endpoint that, taken together, were a meaningful data-exposure risk and a CLAUDE.md rule violation.

First, the endpoint accepted three different format values — `csv`, `excel`, `pdf` — and would return a real Microsoft Excel `.xlsx` file or a `Content-Type: application/pdf` blob (which was actually plain text masquerading as PDF — would have failed to open in any reader if anyone actually clicked the PDF option). The CLAUDE.md rule for this codebase is explicit: "CSV ONLY — no Excel/JSON/PDF". So we had a code path actively contradicting the documented rule.

Second, and more important, the CSV header included an "Internal Tag" column populated with real internal tag values like "Sustainable" or "Eco-Friendly". The codebase rule for external-facing data is that External users cannot view internal tags. Anonymous users (no login at all) are a stricter audience than External users, but they were getting the column anyway because the export endpoint was wide open with `[AllowAnonymous]` and didn't differentiate the column set by caller role.

Third, the corresponding "Export All to Excel" button label and `fa-file-excel` icon visible on the internal and external tile pages contradicted the CSV-only rule on the user-facing surface as well.

The fix collapses the three issues into one batch. Server-side, the export service no longer accepts a format parameter; CSV is the only format produced. The `format=excel` and `format=pdf` query strings still parse without error (so any cached browser bookmarks won't break) but the response is always CSV with the right MIME type and `.csv` extension. The CSV column set is now role-aware: Admin and Analyst callers continue to see the Internal Tag column, while anonymous, External, and Partner callers receive a 23-column CSV with the Internal Tag column removed entirely from both header and data rows. The `[AllowAnonymous]` attribute is preserved because the public tile view legitimately needs the endpoint, but the column-level filter ensures anonymous callers can never extract internal-only data.

On the user-facing side, the buttons in both the internal and external tile pages now read "Export All to CSV" with the matching `fa-file-csv` icon, and the toast notification on click confirms the file is exported as CSV. The aria-label was updated for screen-reader users.

Test coverage was extended at the same time. Eight unit tests in `CertificateServiceTests.cs` were updated for the new method signature, and two new tests were added that specifically exercise the include-internal-tags-false path so future work can't accidentally re-expose the column.

Verified locally on `http://localhost:5101` against the dev DB. Anonymous `curl` to all three format values returns a `text/csv` response with 23 columns and no Internal Tag header — the previous Excel and broken-PDF outputs are gone. Admin and Analyst callers (when authenticated through the `/api/v1/certificates/export` endpoint, which is gated to `AnalystOrAdmin`) continue to receive the full 24-column CSV including Internal Tag.

Files: `Controllers/CertificateController.cs`, `Controllers/Api/CertificatesApiController.cs`, `Services/IServices.cs`, `Services/CertificateService.cs`, `Views/Certificate/CertificateTileViewExternal.cshtml`, `Views/Certificate/CertificateTileViewInternal.cshtml`, `wwwroot/js/certificate-tile-view-external.js`, `wwwroot/js/certificate-tile-view-internal.js`, `GCCWebApp.Api.Tests/Unit/Services/CertificateServiceTests.cs`.

---

## DEF-124 — Partner API "expiring certificates" endpoint now returns the contract DTO instead of leaking the raw entity (2026-05-03)

The Amazon Partner API exposes a small set of endpoints documented in the developer guide, including a `GET /api/v1/certificates/expiring?days=N` route that returns certificates whose expiration falls within the given window. During an internal QA pass we discovered this endpoint was returning the raw `Certificate` database entity instead of the contract DTO that all the other partner endpoints have used since the late-April fix wave. The consequence: any partner with a valid API key was receiving internal-only fields — internal database IDs (`id`, `companyId`, `productId`, several type-IDs), the raw lifecycle status string ("Client Approved - Paid" instead of the contracted literal "Active"), the raw country value instead of the contracted "All", flat `asin/upc/gtin/ean` fields instead of the `productIds[]` array shape, and internal file paths (`certificateFile`, `certificateImagePath`) — plus a circular Company→Certificates navigation reference that, in dev DB testing, blew the response payload to over a gigabyte.

This is the same bug class as DEF-103 (which was caught and fixed for the by-id and by-number endpoints in late April). The 2026-04-21 fix wave was supposed to cover all six partner endpoints but `/expiring` slipped through scope. The 2026-04-30 self-test caught the by-id and by-number gaps; this latest pass caught `/expiring`.

The fix mirrors the existing pattern from the `SearchByProductId` endpoint in the same controller. When the caller is a Partner key, the query now filters to `PublicStatuses` (so Drafts, Submitted, Hold, and Expired certs are not surfaced — those are internal lifecycle states and the contract requires only "Active" certs in partner responses) and scopes to the partner association on the API key. The result set is then projected via the shared `BuildPartnerCertificateDto` helper that the list, by-id, by-number, and search endpoints already use. Admin and Analyst callers, who legitimately use this endpoint for internal expiry-tracking dashboards, continue to receive the full entity.

Verified locally with the Amazon partner key on `http://localhost:5101`: the live response now shows four certificates (the public-status Amazon certs in the dev DB), each with `status: "Active"`, `certificationCountries: "All"`, populated company name, an empty `productIds: []` array (no Amazon cert in the dev seed has product IDs assigned — coverage gap noted separately), and no internal IDs or file paths. The response payload is around one kilobyte; the previous one-gigabyte circular-reference response is gone.

A followup audit ticket has been opened to grep all `[HttpGet]` actions in `CertificatesController` and confirm every single one applies the partner DTO projection, so this scope-creep bug class can be closed for good.

Files: `Controllers/Api/v1/CertificatesController.cs`.

---

## DEF-121 — Stopgap fix for User-entity credential leak in product-ID API responses (2026-05-03)

DEF-121 was filed on 2026-05-01 after we discovered that `GET /api/productids/certificate/{id}` was returning a JSON payload that included the full `User` entity for the `CreatedByUser` and `UpdatedByUser` navigation properties. The `User` entity has, over time, accumulated a number of credential-tier fields that should never leave the database: the BCrypt password hash, the TOTP two-factor secret, recovery codes, password-reset tokens, password history, the IP allowlist, and the security questions JSON blob. The endpoint is gated to authenticated Analyst or Admin users (so this was not anonymous exposure), but any compromise of an Analyst account would yield the password hash and 2FA secret of every user who ever created or modified a product ID — most often the admin user — which is full account takeover.

The defect was open for two days while the team weighed two fix options: (1) introduce a `ProductIdResponseDto` projection that maps only the fields the client actually needs, eliminating the entire User-entity surface, and (2) annotate the User entity's sensitive fields with `[JsonIgnore]` so they never serialize regardless of who reads them.

This release is the option-2 stopgap. Eight fields on the `User` model are now marked `[JsonIgnore]`: `PasswordHash`, `TwoFactorSecret`, `RecoveryCodes`, `SecurityQuestions`, `AllowedIPs`, `PasswordResetToken`, `PasswordResetTokenExpiry`, and `PasswordHistory`. With this change applied, any code path that returns a `User` entity (directly, as a navigation property, or as a list) cannot accidentally leak credential material into the JSON response. The change is application-wide rather than tied to one endpoint, which closes the defect not just for the originally reported `/api/productids/certificate/{id}` route but also any future endpoints that include User navigation properties.

We confirmed nothing in the codebase deserializes JSON directly into the `User` entity, so the `[JsonIgnore]` annotations don't break any update flow — all user creation and modification goes through ViewModels (`UserCreateViewModel`, `UserEditViewModel`) and the `AdminService`, and EF Core reads from the database, not from JSON. Verified that `/Admin/GetUsers` (which uses `UserViewModel` projection) continues to return clean output.

The DTO refactor (option 1) remains as a followup ticket. The annotation approach is a good stopgap because it instantly closes the existing exposure surface, but a future developer adding a new sensitive field to `User` has to remember to mark it; the DTO approach, by contrast, allowlists what flows out and is durably safe. We recommend completing the DTO refactor in the next sprint.

Files: `Models/User.cs`.

---

## DEF-126 — Add/Edit Product ID modal: stray warning triangle gone, modal widened (2026-05-03)

While editing certificate 26-0147, the Add Product ID and Edit Product ID modals were showing a small red warning triangle below the empty Product ID input the moment the modal opened — before the user typed anything. The same modal also still felt cramped on a desktop monitor, with the two-column form (Type and Product ID side-by-side) pinched into a narrow strip and the Description textarea looking small.

Two underlying issues. The triangle came from a global accessibility CSS rule that adds a ⚠ symbol as a `::before` pseudo-element to every error-message container — useful when there's a real validation error to show, but our Product ID modal pre-renders an empty placeholder div ready to be populated by JavaScript when validation fails. Another rule on that modal forced the placeholder to always reserve vertical space, so the empty container plus the global pseudo-element painted a triangle on every modal open. The width was simply that DEF-119's earlier bump to 900 pixels still left the dialog occupying under half the viewport on a 1920-pixel monitor.

The triangle fix narrows the global selector to only paint the icon when the error container actually contains text (`:not(:empty)`). Real validation errors still render the triangle alongside their message — for example, typing a 9-character ASIN now shows "⚠ ASIN must be exactly 10 alphanumeric characters" with the icon in place. The width fix bumps the modal cap from 900 to 1140 pixels (matching Bootstrap's modal-xl).

Verified locally on `http://localhost:5101/Certificate/Edit/26-0147` at a 1920-pixel viewport. Fresh modal opens with no triangle. Invalid input reproduces the triangle and message correctly. Modal centers nicely with visibly more breathing room around all form fields.

Files: `wwwroot/css/accessibility-enhancements-low-priority.css`, `wwwroot/css/product-id-manager.css`.

---

## DEF-123 — Dashboard stat downloads now match the on-screen tile values (2026-05-03)

While verifying the DEF-122 fix, we caught a related but independent reporting defect: three of the four "download data" buttons on the Dashboard stat tiles were producing CSVs with placeholder names instead of the real ones. The Product Categories CSV listed every certificate under "Uncategorized." The Product Types CSV listed everything under "Unassigned." Same for Product Subcategories. The Companies CSV had always been correct.

The user-visible consequence: a stakeholder downloading the Product Types report saw a single row called "Unassigned, 22 certificates" — which would suggest 22 real certificates had a type called "Unassigned." In reality, the dev dataset has 22 active certificates and zero of them are linked to a product type, so the correct report is an empty list. The CSV header was also reporting `Total Product Types: 1` (counting the placeholder bucket as if it were a real type), which contradicted the tile correctly showing `0`.

Two underlying issues were responsible. First, the dashboard's "active certificates" query was eager-loading the Product entity but not the navigation properties hanging off Product (Category, ProductType, ProductSubcategory) or the cert-level direct links (ProductCategory, ProductType, ProductSubcategory). When the CSV builder tried to read those names, it got null on every row and fell back to placeholder strings. Second, the CSV's GroupBy logic only inspected the cert-level direct link and ignored the Product-level link entirely — which meant even with the navigation properties loaded, the count would have been inconsistent with the tile, since the tile combines both linkage paths.

The fix has two parts. The query now eagerly loads all six relevant navigation paths so the names are populated. The grouping logic now matches the tile's semantics (prefer the Product linkage, fall back to the cert direct link) and discards null/empty group keys so placeholder buckets aren't counted as real types.

Verified on the development dataset against all four downloads. After the fix, every CSV's "Total" line and row data matches the corresponding tile value: Product Categories shows "Chemicals and Raw Materials, 22" (tile = 1, CSV total = 1); Product Types and Subcategories both show empty result sets (tile = 0, CSV total = 0); Companies shows nine named rows summing to 22 (tile = 9, CSV total = 9).

Files: `Repositories/CertificateRepository.cs`, `Controllers/DashboardController.cs`.

---

## DEF-122 — Dashboard "Product Categories" tile was showing the wrong count (2026-05-02)

During an audit of the Dashboard ("are the numbers correct?"), the third stat tile labeled "# Product Categories with Active Certs" was found to be displaying the wrong number. The tile was wired to the count of distinct **certification types** (e.g. SCS Certified, USDA Organic) rather than product categories (e.g. Chemicals and Raw Materials, Personal Care). The dashboard had no tile at all for actual product categories. The "Top 5 Product Categories" breakdown table at the bottom of the same page was — and remains — correct, which is why the discrepancy was easy to confirm: the tile and the table sat next to each other showing different stories about the same data.

Root cause was that the repository never computed a "product categories with active certs" statistic; the matching dictionary key was missing entirely. The view binding had been quietly pointing at the certification-types statistic for a long time, with a Product Categories label glued on top.

The fix adds the missing distinct-count query (using the same two-pass EF Core pattern already used for Product Types and Subcategories, so certificates linked through `Product.Category` and certificates linked directly via `ProductCategory` are both counted), and points the tile at the new statistic. The CSV download button under that tile was already mapped to the correct export, so the export side needed no change — only the displayed number.

Verified against the development database: the tile previously showed `4` (count of certification types); after the fix it shows `1` ("Chemicals and Raw Materials" — the only product category with active certs in the dev dataset), which agrees row-for-row with the Top 5 Product Categories table immediately below it.

Files: `Repositories/CertificateRepository.cs`, `Views/Dashboard/Index.cshtml`.

---

## DEF-109 — Tile period dates off-by-one in negative-UTC timezones (2026-05-01)

Analysts editing a certificate would enter `5/10/2026` and see it correctly on the Edit form and on the cert detail page, but the same cert on the **internal** and **external tile listings** rendered as `5/9/2026` — one calendar day earlier. The public tile listing was already correct.

Root cause was a global JSON converter (`UtcDateTimeConverter`) that we added during the Amazon Partner API work to make every API timestamp end in `"Z"` — matching the format Amazon's developer guide examples use. That converter was correct for true timestamps (`createdAt`, `updatedAt`), but it also stamped a `Z` onto calendar-only dates like `EffectiveDate`. Browsers then read `"2026-05-10T00:00:00Z"` as UTC midnight and converted it to local time — which in US Eastern is the previous evening, so the calendar date displayed shifts back a day.

The public tile API had been pre-formatting these two fields as plain `yyyy-MM-dd` strings (no time component, no `Z`), which is why public didn't have the bug. The fix is to apply the same pre-formatting in the internal and external tile APIs. Two files, four lines. **Partner API output is unchanged** — the immutability rule on `/api/v1/certificates*` is preserved.

Verified locally on `http://localhost:5101` with Playwright in the `America/New_York` timezone: API responses now return `yyyy-MM-dd` strings, and rendered tile cards display the same calendar date as the database value.

Files: `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`.

---

## DEF-110 — Export CSV headers now match UI table column labels (2026-05-01)

An audit of every Export button across the application turned up that CSV headers and on-screen column labels had drifted apart over time on roughly a dozen pages. Same field would appear as "Company" on the User Management table but "User Company" in the CSV; "URL" on screen but "Website" in the CSV; "Subcategory" on screen but "Product Subcategory" in the CSV. Server-side admin exports were also using PascalCase tokens like `IsActive` and `UserCount` that don't show up anywhere in the UI. Two structural issues fell out of the audit on top of the naming drift:

1. The Product Management pages registered the `Export` click handler in **two** places — once in the page's inline script and again in the constructor of `productManagement.js`. Clicking the button was downloading two CSVs at once with different column sets.
2. The Product Types admin page was the only admin page popping a CSV/JSON/Excel chooser modal instead of going straight to CSV. That violates the project's CSV-ONLY rule for admin exports.

Both shipped in the same change. Twelve export surfaces touched: User Management, Company Management, Admin Product Management, per-Company Product Management, Pathways, Tags, Certificate Types, Certificate Fields, Product Types, plus the certificate tile export and the server-side admin entity exports. Each export's header line now reads the same as the column labels on the page that owns it.

Two specific carry-overs by design: the user **import template** still uses "User Company" so existing CSV imports keep round-tripping, and the product import template keeps its own column set for the same reason. Export headers and import templates are deliberately decoupled.

Verified locally on `http://localhost:5101` that JavaScript-driven exports are serving the new content. The two C# changes (`CertificateService.cs` for cert tile export, `AdminService.cs` for server-side admin entity exports) need a dev-server restart to take effect.

Files (12): `wwwroot/js/userManagement.js`, `wwwroot/js/admin/companyManagement.js`, `wwwroot/js/admin/productManagement.js`, `wwwroot/js/tags.js`, `wwwroot/js/admin/certificate-types.js`, `wwwroot/js/admin/certificate-fields.js`, `wwwroot/js/product-types.js`, `Views/Admin/ProductManagement.cshtml`, `Views/Admin/Pathways.cshtml`, `Views/Product/Management.cshtml`, `Services/CertificateService.cs`, `Services/AdminService.cs`.

---

## DEF-107 / DEF-108 — Restored Google Analytics + cleaned up duplicate-header browser warnings (2026-05-01)

A full-spectrum QA pass on `http://localhost:5101` produced 112 console errors per session — almost all from the security headers middleware being more aggressive than the app actually needs. Two specific issues stood out and both ship together because they share the same source file.

**The Feature-Policy / Permissions-Policy duplicate** (DEF-107). Every page response was emitting both headers with overlapping directives. Modern Chromium logs `Error with Feature-Policy header: Some features are specified in both Feature-Policy and Permissions-Policy header` 16 times per session. Permissions-Policy is the standard; Feature-Policy was kept "for older browsers" but every browser the app actually supports already understands the new header. The legacy Feature-Policy emit is removed.

**The Google Analytics blackout** (DEF-108). GA was silently not recording any pageviews. Two layers were combining to block it:

1. `Cross-Origin-Embedder-Policy: require-corp` was rejecting any cross-origin script that didn't return a matching `Cross-Origin-Resource-Policy: cross-origin` header. Google's GA scripts don't return CORP — so the GA script load was blocked at the COEP layer. The codebase doesn't use SharedArrayBuffer or any feature that requires cross-origin isolation, so the policy is moved to `unsafe-none`.
2. Once the script was unblocked, the actual GA4 *beacons* still failed because the CSP `connect-src` whitelist only included `www.google-analytics.com`. GA4 actually fires its `g/collect` events to several other Google hosts: `analytics.google.com`, `www.google.com/g/collect`, region-specific `*.google-analytics.com`, and `stats.g.doubleclick.net` for demographics. Each of those is added to `connect-src`.

Verified on `localhost:5101` — console errors went from **112 → 1** session-wide (-99%). The single remaining error is unrelated (DEF-111, a separate analyst-side data fetch on `/Product/Management`, filed for a future cycle). Every GA beacon now succeeds. The CSP and other origin policies stay in place; only the headers that actively fought the app's own use cases were relaxed.

Both fixes are in one file, so they ship together.

Files: `Middleware/SecurityHeadersMiddleware.cs`.

---

## DEF-114 — Dashboard mobile portrait: stats tables no longer clip column 2 (2026-05-01)

On a 360-pixel-wide phone in portrait orientation, the Dashboard's five stats tables (Total Active Certs by Certification Type, Top 10 Companies, # Certs by Status, Top 10 Product Types, Top 5 Product Categories) were silently cropping the right edge of column 2 — both the headers ("# OF ACTIVE CERTIFICATES" appeared as "# OF ACTIV…") and the numeric values were getting cut off.

The cause was the standard admin shell padding chain — `.admin-container` margin + padding plus `.admin-body-content` padding — eating ~50 pixels of horizontal space on each side. At a 360 px viewport that left the tables-container at only 216 px wide while the inner table needed 232–263 px depending on the second-column header label. The table-section's `overflow: hidden` then hid the overflow without any horizontal scrollbar to hint at it.

The fix scopes a single class — `dashboard-page` — to the Dashboard view's root container, then trims the admin-shell padding and margin only on that page only at ≤480 px. Other admin pages keep their existing padding because their tables either scroll horizontally or use card layouts and didn't have this problem. Tables-container goes from 216 → 274 px (+58 px), every table fits cleanly at 272 px with no clipping, and there's no horizontal scroll on the document.

Verified locally on `http://localhost:5101/Dashboard` at 360×800 mobile viewport via Playwright. All five tables: column 2 headers fully readable, numeric values not truncated, no horizontal overflow.

Files: `Views/Dashboard/Index.cshtml`, `wwwroot/css/pages/dashboard.css`.

---

## DEF-115 — "Columns" toggle now right-aligns on narrow viewports (2026-05-01)

On the certificate tile views (both internal and external), the "Columns" toggle button — which lets users hide or show specific table columns — was supposed to sit on the right side of the table toolbar opposite the "Showing N certificates" counter. On phones in portrait orientation the two elements stopped fitting side-by-side and the row wrapped to two lines — but instead of staying right-aligned on its own line, the Columns button fell back to the left edge.

The cause is a flexbox quirk: `justify-content: space-between` distributes items within their flex line, but a lone item on a wrapped line has nothing to be "between" and ends up at flex-start (the left edge). One CSS declaration — `margin-left: auto` on the right side of the toolbar — pushes the button to the right edge of its line whether the row is single or wrapped to two lines.

A single CSS file change covers both internal and external views since they share the same toolbar markup and stylesheet.

Verified locally on `http://localhost:5101/Certificate/CertificateTileViewInternal` at 360 / 768 / 900 / 1024 / 1280 pixel viewports. At every width the button now sits 16 pixels from the right edge of the toolbar row.

Files: `wwwroot/css/certificatetileview.css`.

---

## DEF-116 — Product Identifier filter controls now stay on-screen on phones (2026-05-01)

On Certificate Create and Certificate Edit, the Product Identifier section's filter row — Type dropdown, Partner dropdown, and Search box — was forcing all three controls onto a single row with fixed pixel widths totaling around 450 pixels. On a 360-pixel-wide phone the partner dropdown got clipped to "All Partne…" and the search box was pushed completely off-screen.

The fix replaces the inline-style flex row with a CSS class that wraps onto multiple lines when there isn't enough horizontal room, while keeping the original single-row layout on tablet and desktop. The search box now flexes to fill any remaining width on wider viewports (so it gets bigger on a desktop than it used to be at the old fixed 200-pixel width) and falls to its own line on phones.

Verified locally on `http://localhost:5101/Certificate/Create` at 360 / 768 / 1024 pixel viewports. On phones, all three filters are now fully visible on three wrapped rows. On tablet and desktop the layout is unchanged from before.

The same partial view is reused on Certificate Edit, so both pages get the fix from a single CSS edit.

Files: `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/css/product-id-manager.css`.

---

## DEF-117 — Column Settings dropdown stays inside the viewport on mobile portrait (2026-05-01)

On both the internal and external certificate tile views, clicking the "Columns" toggle on a phone in portrait orientation opened the Column Settings dropdown at a fixed height of about 680 pixels — but with the toggle button typically sitting somewhere in the middle of the screen, that meant the bottom 300 pixels of the dropdown spilled past the viewport edge. The list was technically scrollable inside the dropdown, but users couldn't see those scrollable rows because the visible portion was capped at whatever space was actually available between the button and the bottom of the screen. End result: roughly half the column toggles (the ones in PRODUCT IDS, CONTACT, and ADDITIONAL sections) couldn't be reached at all without zooming out the page itself.

The fix sizes the dropdown to the actual available space at the moment it's opened, and flips it to open above the button when there's more vertical room above than below. The dropdown now always fits cleanly inside the viewport with internal scroll for any rows that don't fit. A 200-pixel minimum height keeps it usable even on very short viewports.

The external tile view didn't previously have any mobile positioner at all — it just used the default below-button position. Same fix applies to both views.

Verified locally on `http://localhost:5101/Certificate/CertificateTileViewInternal` at 360×800 mobile viewport. With the toggle button mid-screen, the dropdown opens below it with a 364-pixel height — exactly the room available — and the bottom of the dropdown sits 4 pixels above the viewport edge. With the toggle near the top of the viewport (after scrolling), it opens below with a 695-pixel height. In both cases the full column list is reachable via internal scroll.

Files: `wwwroot/js/certificate-tile-view-internal.js`, `wwwroot/js/certificate-tile-view-external.js`.

---

## DEF-118 — Product ID export from Certificate Create now includes Created and Created By (2026-05-01)

On the Certificate Create page, exporting the in-memory product ID list produced a CSV with only six columns (Type, Product ID, Partner, ProductName, ProductSku, Description). The on-screen table shows two more columns — Created and Created By — that were missing from the export. The header naming had also drifted from the table labels ("ProductName" vs "Product Name", "ProductSku" vs "SKU"), which is the kind of drift the export-alignment cleanup last week was supposed to eliminate.

The export now emits the same columns in the same order as the table: `Type, Product ID, Product Name, SKU, Partner, Created, Created By, Description`. For a brand-new certificate that hasn't been saved yet, the in-memory items don't have a created timestamp or a creator yet, so those two cells render blank in the export — accurate for that state. Once the certificate is saved and reopened in Edit, the values populate from the database.

Verified locally on `http://localhost:5101/Certificate/Create` by adding a sample product ID and capturing the exported CSV. Headers and column order match the on-screen table.

A small follow-up worth noting: the Edit-page server export still uses the older header names and still emits a Status column. That export goes through a different code path (`Services/ProductIdService.cs`) and was not changed in this fix to avoid touching unasked surfaces. Worth aligning in a future CR so both export entry points produce the same column shape.

Files: `wwwroot/js/product-id-manager.js`.

---

## DEF-119 — Add Product ID modal now uses full desktop width (2026-05-01)

On a desktop monitor, the Add Product ID modal on Certificate Create and Certificate Edit was rendering at only 650 pixels wide regardless of viewport size — leaving 300 to 600 pixels of empty margin on each side and squeezing the two-column form (Type and Product ID side-by-side, then Partner) into a cramped layout.

The CSS file had two overlapping caps. The first was `#productIdModal .modal-dialog { max-width: 650px }` — which capped the modal regardless of the `modal-lg` Bootstrap class that's already on the dialog. The second was a bare `.modal-dialog { max-width: 600px }` rule that leaked onto every other modal on those two pages, capping them all at 600 pixels application-wide on Certificate Create and Edit.

The fix bumps the product modal cap to 900 pixels (slightly wider than Bootstrap's modal-lg default of 800 to give the description textarea more breathing room) and removes the bare global rule entirely. Other modals on the Create / Edit pages now get their default Bootstrap widths back.

Verified locally on `http://localhost:5101/Certificate/Create` at 1280 and 1920 pixel viewports. The Type and Product ID dropdowns now render side-by-side at half-width as the col-md-6 grid intends, the description textarea spans the full modal width, and the modal centers nicely with the wasted margin removed.

Files: `wwwroot/css/product-id-manager.css`.

---

## DEF-120 — Product Identifiers "Created By" now shows username instead of numeric ID (2026-05-01)

On the Certificate Edit page, the Product Identifiers table's "Created By" column was rendering a numeric user ID (e.g. `1`) instead of the username (e.g. `admin`). The server-side Razor partial that renders the initial table read the username through the `CreatedByUser` navigation property correctly, but the JavaScript renderer that re-draws rows after the page hydrates was reading the raw `createdBy` foreign-key column instead.

The API already includes the navigation property, so the fix is a one-line change in the JavaScript renderer to read `createdByUser.username` with a `'System'` fallback for legacy rows that have no associated user. Single template literal in `product-id-manager.js`.

Verified locally on `http://localhost:5101/Certificate/Edit/26-0150`. All three product ID rows now show "admin" in the Created By column instead of "1".

Files: `wwwroot/js/product-id-manager.js`.
