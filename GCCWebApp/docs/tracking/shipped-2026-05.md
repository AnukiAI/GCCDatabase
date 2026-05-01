# Shipped — May 2026

Snapshot of delivery for May 2026. Stakeholder-readable log.

Period: 2026-05-01 → ongoing

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
