# Shipped — May 2026

Snapshot of delivery for May 2026. Stakeholder-readable log.

Period: 2026-05-01 → ongoing

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
