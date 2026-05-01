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
