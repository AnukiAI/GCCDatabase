# QC Log Validation Checklist

**Project:** GCC Database Solution
**Source:** Database QC Log_3.13.26_Updates.xlsx
**Test Environment:** https://gccwebapp.com
**Created:** 2026-03-25
**Last Updated:** 2026-03-25

## Status Key

| Symbol | Meaning |
|--------|---------|
| [ ] | Not tested |
| [P] | Pass |
| [F] | Fail — needs rework |
| [N/A] | Not applicable / deferred |
| [B] | Blocked — can't test (note reason) |

## Summary

| Section | Total | Pass | Fail | Blocked | Not Tested |
|---------|-------|------|------|---------|------------|
| 1. Public View | 20 | | | | 20 |
| 2. Public View - Mobile | 3 | | | | 3 |
| 3. Sign In | 5 | | | | 5 |
| 4. Dashboard | 9 | | | | 9 |
| 5. Cert Listing (Internal) | 11 | | | | 11 |
| 6. Create Certificate | 18 | | | | 18 |
| 7. Product ID Manager | 4 | | | | 4 |
| 8. Edit Certificate | 3 | | | | 3 |
| 9. Product Management | 13 | | | | 13 |
| 10. Admin Pages | 17 | | | | 17 |
| **TOTAL** | **103** | **0** | **0** | **0** | **103** |

## Test Credentials

| User | Password | Role |
|------|----------|------|
| admin | TestPass123! | Admin |
| analyst | TestPass123! | Analyst |
| external | TestPass123! | External |

---

## 1. PUBLIC VIEW (No Login Required)

Test URL: https://gccwebapp.com

| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 22 | [ ] | Only "Client Approved - Paid" and "Republished" certs visible. No Draft, Hold, Expired, Confidential, or "Expired - No Annual Verification" certs showing. | | | |
| 23 | [ ] | Certs with past expiration dates NOT showing even if status is approved | | | |
| 21 | [ ] | No "Expiring Soon" tag anywhere on tiles, filters, or sort options | | | |
| 25 | [ ] | Sort By has only 13 options in 3 groups: By Certificate (4), Alphabetical (5 — includes Product Type A-Z), By Location (4). No "By Date" or "Advanced" groups. | | | |
| 26 | [ ] | Each Sort By option actually changes the order (try at least 5 different sorts) | | | |
| 24 | [ ] | No popup/toast notification when filtering or sorting | | | |
| 33 | [ ] | APR/LEED under "Additional Filters" works — filter pills appear, certs filter correctly | | | |
| 36 | [ ] | Shareable URL: open `gccwebapp.com/?search=ASSA+ABLOY` in new browser — shows filtered results | | | |
| 29 | [ ] | Infinite scroll: scroll down and more certs load (20 at a time) | | | |
| 28 | [ ] | "Sign In" is a direct link (one click to login page, no dropdown) when not logged in | | | |
| 40 | [ ] | GCC logo in header links to homepage from every page (including cert detail opened in new tab) | | | |
| 38 | [ ] | When company has no email/URL, detail page shows dash (—), not "Available upon request" or "Not provided" | | | |
| 39 | [ ] | No "General Product" text anywhere — shows dash (—) when product type is blank | | | |
| 31 | [ ] | Download button centered under cert image on detail page | | | |
| 60 | [ ] | Cert image NOT cut off on detail page (tall/portrait images display fully) | | | |
| 37 | [ ] | Tags on tile cards have proper spacing from bottom (not jammed against action bar) | | | |
| 34 | [ ] | Long product names truncate with ellipsis (...) after ~5 lines on tile cards | | | |
| 96 | [ ] | Nav dropdown menus always render on top of page content (not behind search bar or other elements) | | | |
| 61 | [ ] | Certificate status is NOT visible on public cert detail page (CertificateSingleView) | | | |
| 100 | [ ] | Cert detail page shows "Database Listing URL" (not "Verification URL") | | | |

---

## 2. PUBLIC VIEW — MOBILE / TABLET

Test method: Browser DevTools responsive mode or actual device

| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 27 | [ ] | At <=1024px: "Show Filters & Search" toggle button visible and functional. Clicking toggles filter sidebar open/closed. | | | |
| 30 | [ ] | Company name wraps correctly on mobile tile cards (no overflow or clipping) | | | |
| 50 | [ ] | Create cert page mobile layout — form stacks to single column, fields full width, buttons full width, no overflow | | | |

---

## 3. SIGN IN PAGE

Test URL: https://gccwebapp.com/Account/Login

| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 41 | [ ] | Page text says "Login is restricted—public sign-up is not available." Field label says "Email" (no username mention). Placeholder says "Enter your email". | | | |
| 42 | [ ] | Enter wrong email/password — red error message "Invalid email or password" appears | | | |
| 43 | [ ] | "Return to GreenCircle database >" link present below Forgot Password, links to public homepage | | | |
| 44 | [ ] | Check "Remember Me", login, close browser, reopen — should still be logged in (14-day session, not 30 min) | | | |
| 71 | [ ] | Navigate directly to /Account/LogoutPost — should redirect gracefully (not "This page isn't working") | | | |

---

## 4. DASHBOARD

Test URL: https://gccwebapp.com/Admin/Dashboard (login as admin)

| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 67 | [ ] | Stat cards (Companies, Product Categories, Product Types, Certs Published) count only records tied to ACTIVE certificates — not all records in DB | | | |
| 68 | [ ] | "Certs by Status" shows all 8 workflow statuses with correct counts per status | | | |
| 69 | [ ] | Product type statistics section shows data (not "No product type data available") | | | |
| 70 | [ ] | "Top 5 Product Categories" shows actual product categories (Chemicals, Packaging, etc.) — NOT certification types (Recycled Content, etc.) | | | |
| 73 | [ ] | Export "Total Active Certs" — row count in downloaded file matches the stat card number exactly | | | |
| 74 | [ ] | "Download Expiring Certs (Next 3 Months)" exports actual CSV data (open file, verify it's data not HTML) | | | |
| 75 | [ ] | "Download Current Active Certs" exports actual CSV data | | | |
| 76 | [ ] | "Download Expiring Certs (Next 30 Days)" exports actual CSV data | | | |
| 72 | [ ] | Open exported file in Excel — date columns are recognized as dates (can sort by date, filter by year/month) | | | |

---

## 5. CERTIFICATE LISTING PAGE (Internal — Analyst/Admin)

Test URL: https://gccwebapp.com/Certificate/CertificateTileViewInternal (login as analyst or admin)

| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 11 | [ ] | All certificate fields present in table. Top scrollbar visible. Column headers frozen when scrolling down. | | | |
| 64 | [ ] | No duplicate certificate entries (each cert number appears exactly once) | | | |
| 65 | [ ] | Pagination with page numbers at bottom ("Showing X-Y of Z", Previous/Next, numbered pages) | | | |
| 66 | [ ] | Status text on tile cards is smaller font (13px), fits with long product names | | | |
| 82 | [ ] | Table view: header and body fonts are 13px (smaller than default 16px) | | | |
| 83 | [ ] | Clicking arrow icon on a row opens cert detail in a NEW browser tab | | | |
| 84 | [ ] | Company Contact Email/URL is the LAST column in the table (after EAN) | | | |
| 85 | [ ] | Effective Date and Expiration Date columns have sort arrows that don't overlap text | | | |
| 86 | [ ] | Hovering over table and using mouse wheel scrolls horizontally (Wrike-style) | | | |
| 87 | [ ] | All status badges are consistent: green = Client Approved - Paid, orange = Draft, red = Expired, grey = other | | | |
| 77 | [ ] | "Export All" includes ALL fields: cert subcategories, company email/URL, product type/subcategory, pathway, cert results, tags, custom fields | | | |

---

## 6. CREATE CERTIFICATE PAGE

Test URL: https://gccwebapp.com/Certificate/Create (login as analyst or admin)

| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 7 | [ ] | PDF upload: entire dashed-border zone is clickable (icon + text), shows pointer cursor on hover | | | |
| 45a | [ ] | Cert Type dropdown includes: Carbon Footprint Reduction, Sustainable Energy Practices, Sustainable Manufacturing Practices, Product Optimization: Carbon Footprint Reduction, Material Ingredient Reporting | | | |
| 45b | [ ] | Cert Type dropdown does NOT include: Renewable Resource Content, Biobased, Manufacturer Inventory Verification | | | |
| 46 | [ ] | Selecting any certification type does NOT throw an error (try multiple types) | | | |
| 47 | [ ] | "Waste Diversion Event" appears under Waste Diversion from Landfill subcategories | | | |
| 48 | [ ] | Certificate subcategories are in alphabetical order | | | |
| 49 | [ ] | ISO 14001 Certified, Verified Declare Label, and Verified HPD appear in subcategories | | | |
| 52a | [ ] | Product dropdown says "Select a company first..." until company is selected, then loads that company's products only | | | |
| 52b | [ ] | Selecting a product auto-fills Category, Product Type, and Subcategory | | | |
| 52c | [ ] | "Add New Product" modal has 6 fields (name, company, category, type, subcategory, description) with cascading dropdowns | | | |
| 53 | [ ] | Product Category shows "Select a category..." (NOT "All Categories"). Product Type shows "Select a type..." (NOT "All Types") | | | |
| 54 | [ ] | Status dropdown has exactly 8 options: Draft, Client Approved - Not Paid, Client Approved - Confidential, Client Approved - Paid, Hold, Expired, Republished, Expired - No Annual Verification | | | |
| 55 | [ ] | Pathway field label reads "Pathway (for Amazon CPF applicable certs)" | | | |
| 56 | [B] | **BLOCKED — Pending Business Clarification** Countries dropdown pulls from cert data. Need business to confirm: full ISO list or only countries in use? | | | |
| 59 | [ ] | Company name field has type-ahead/autocomplete (narrows options as you type) | | | |
| 62 | [ ] | Company Email and Company URL fields present, auto-populate when company is selected | | | |
| 63 | [ ] | Dropdown chevron arrows not blocked or overlapped by text | | | |
| 78 | [ ] | "Download Template" gives CSV with headers only — no sample/test data rows | | | |
| 118 | [ ] | Custom fields render as flat list under "Additional Fields" — no section sub-headers | | | |

---

## 7. PRODUCT ID MANAGER (within Create/Edit Certificate)

| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 79 | [ ] | Search bar in Product ID Manager works (filters the list) | | | |
| 80 | [ ] | Edit button on a product ID opens editable modal (not frozen/stuck) | | | |
| 81 | [ ] | Import CSV with long numbers (UPCs) — no scientific notation errors (1.23E+12 converts to full number) | | | |
| 99 | [ ] | Import CSV with 2+ rows — ALL rows import correctly (not just the first) | | | |

---

## 8. EDIT CERTIFICATE PAGE

Test URL: https://gccwebapp.com/Certificate/Edit/{id} (login as analyst or admin)

| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 60 | [ ] | Cert image displays fully (not cut off). Edit/Delete buttons aligned correctly. | | | |
| 52 | [ ] | Company pre-selected. Products dropdown pre-populated with that company's products. Saved product pre-selected. Category/Type/Subcategory filled. | | | |
| 58 | [ ] | Open form, wait 30+ min (or manually expire session) — form data should auto-save to localStorage and offer recovery | | | |

---

## 9. PRODUCT MANAGEMENT

Test URL: https://gccwebapp.com/Product/Management (login as admin)

| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 88 | [ ] | Sort arrows do NOT overlap column names (proper spacing) | | | |
| 89 | [ ] | Long text in table cells wraps properly (no overflow). Table scrolls horizontally on narrow screens. | | | |
| 90 | [ ] | Table paginates at 50 rows with numbered page buttons, Previous/Next, and "Showing X-Y of Z" | | | |
| 91 | [ ] | "Product Type" and "Subcategory" columns visible and populated | | | |
| 92 | [ ] | Add New Product popup: NO Status, SKU, or Model Number fields | | | |
| 93 | [ ] | Add New Product: selecting Category populates Product Type dropdown. Selecting Type populates Subcategory. | | | |
| 94 | [ ] | Export CSV includes Product Type and Subcategory columns | | | |
| 95 | [ ] | Column sorting works: Company, Category, Product Type, Subcategory, Certificates (numeric), Identifiers (numeric). Clicking different column resets previous sort icon. | | | |
| 97 | [ ] | Add New Product: leaving Company blank shows error notification, field is focused | | | |
| 98 | [ ] | Delete product with 0 certs — succeeds with confirmation. Delete product with certs — blocked with clear error message. | | | |
| 101 | [ ] | Active filter tags show value only (no "Search:" prefix, no "Active filters:" label) | | | |
| 102 | [ ] | Export does NOT include test/sample data rows ("Sample Product", "Another Product") | | | |
| 103 | [ ] | Download Template: headers are Name, Company, Description, Category, Product Type, Subcategory. No sample data rows. | | | |

---

## 10. ADMIN PAGES

### Breadcrumbs (all admin pages)
| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 104 | [ ] | "Administration" breadcrumb links to /Admin dashboard. Category labels ("Data Management", "API Management", "System Settings", "Companies & Products") are NOT clickable. Current page is NOT a link. | | | |

### Role Management (/Admin/RoleManagement)
| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 105a | [ ] | "Manage Workflow" description says "Manage certificate workflow state transitions" | | | |
| 105b | [ ] | "View Audit Logs" is NOT shown in the permissions list | | | |
| 105c | [ ] | "Manage Tags" description says "Classification tags (internal and external)" | | | |

### User Management (/Admin/UserManagement)
| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 106 | [ ] | "Add New User" modal shows Company field for ALL roles (except Partner) | | | |
| 107 | [ ] | Create user with valid data succeeds. Create user with duplicate email shows clear error. | | | |
| 108 | [ ] | "Download Template" gives CSV with headers only (no "John Doe" sample row) | | | |
| 109 | [ ] | Import users via CSV works. Error messages show actual failure reason (not just "Import failed"). | | | |

### Partner API Access (/Admin/Partners)
| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 110a | [ ] | Novi description reads: "Novi Connect is an AI shopping optimization platform" | | | |
| 110b | [ ] | Wayfair listed with description: "Online furniture and home goods retailer focused on sustainable product offerings" | | | |
| 121 | [ ] | Any partner added via Admin shows in cert creation Partner dropdown | | | |

### Certificate Types (/Admin/CertificateTypes)
| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 111 | [ ] | Add Certificate Type popup: no blue description box. Examples say "Recycled Content, Certified Energy Savings, etc." | | | |
| 112 | [ ] | Add Subcategory popup: no blue description box. Examples say "Attributes under multi-attribute label" | | | |
| 113 | [ ] | Cannot delete a certificate type that has certificates tied to it (clear error message) | | | |
| 114 | [ ] | Help modal: Type example = "Certified Sustainability Facts", Subcategory example = "Recycled Content, Closed Loop" | | | |

### Product Types (/Admin/ProductTypes)
| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 115 | [ ] | Edit Category: no inaccurate blue box. Dynamic examples from actual data labeled "Existing". | | | |
| 116 | [ ] | Help modal uses generic labels (Category/Type/Subcategory), no fake product names | | | |
| 117 | [ ] | Adding a new root-level item creates its own category (NOT under "Chemicals and Raw Materials") | | | |

### Certificate Field Management (/Admin/CertificateFields)
| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 119 | [ ] | Add a new custom field, fill value on a cert, export — new field appears as column in export | | | |

### Tags (/Admin/Tags)
| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 120 | [ ] | Download Template: CSV with headers only (no sample data) | | | |

---

## 11. DATA / CONFIRMATION ITEMS (No Code Fix Needed)

| QC# | Status | Test Description | Tester | Date | Notes |
|-----|--------|-----------------|--------|------|-------|
| 29 | [ ] | Infinite scroll is working as designed (20 per load) — confirmed not a bug | | | |
| 34 | [ ] | Character limits documented: Company/Product Name = 255 chars, Product Name truncates at ~5 lines on tiles | | | |
| 35 | [ ] | Default cert preview image is test data — will be cleaned before go-live | | | |
| 51 | [ ] | Random characters in names — data issue, not code. Verify no encoding issues in import pipeline. | | | |
| 57 | [ ] | Image #13 issue — resolved as part of other fixes | | | |
| 119 | [ ] | Custom fields auto-appear in export but NOT in table views — confirmed by design | | | |

---

## 12. NEW DEFECTS FOUND DURING VALIDATION

| ID | Priority | Page | Description | Status | Fix |
|----|----------|------|-------------|--------|-----|
| NEW-001 | Critical | Public view - direct URL | Anonymous users could access non-public certs (Draft, Hold, Confidential, Expired) via direct URL (e.g., /Certificate/CertificateSingleView/26-0003). Tile view filtered correctly but direct URL bypassed status check. | **Fixed** | Added status + expiration guard in CertificateController.CertificateSingleView. Returns 404 for anonymous users viewing non-public certs. |
| NEW-002 | Low | Public view - UI | Blue focus border appears on checkboxes and logo when clicked with mouse. Should only show for keyboard navigation. | **Fixed** | Changed `:focus` to `:focus-visible` for logo link, filter checkboxes, and table checkboxes in main.css and certificatetileview.css. |
| NEW-003 | High | Public view - mobile/tablet | "Show Filters & Search" button not displaying on mobile/tablet. Filters completely hidden with no way to open them. | **Fixed** | Root cause: `main.css` used `.sidebar.expanded` class but JS toggles `.sidebar.mobile-open`. Also `max-height: 60px; overflow: hidden` on sidebar prevented content from showing. Changed all `expanded` references to `mobile-open` and removed max-height constraint. |
| NEW-004 | Low | Public view - mobile/tablet | Empty nav bar (hamburger menu) showing for anonymous users on mobile/tablet — no nav items to display. Wastes screen space. | **Fixed** | Added `nav-anon` class to nav-wrapper for unauthenticated users. Hidden via `display: none` at <=1024px. Desktop still shows nav (Sign In link). |

---

## 13. OPEN ITEMS — STILL NEED RESOLUTION

| QC# | Priority | Page | Description | Status | Assigned |
|-----|----------|------|-------------|--------|----------|
| 32 | High | Public view + Admin | Packaging/Paper/Plastic under wrong category in filter | Need Clarification from Carly | Unassigned |
| 50 | Low | Create cert - mobile | Create cert page distorted on mobile | **Fixed** | Added `!important` to responsive grid overrides (base styles had `!important` that blocked media queries). Added mobile rules for admin header, form padding, PDF upload zone, product ID table scroll, buttons. |
| 56 | High | Create cert | Countries dropdown pulls from cert data, not full ISO list. Need business to confirm: full ISO list or only countries in use? | **Pending Business Clarification** | Unassigned |

---

## 13. DATABASE MIGRATION VERIFICATION

These SQL scripts must have been applied to the test database:

| Script | Description | Applied? | Verified By | Date |
|--------|-------------|----------|-------------|------|
| 18_20260319_UpdateStatusTypes.sql | Convert old statuses to 8 new statuses | [ ] | | |
| 19_20260320_UpdateCertificationTypes.sql | Add/remove/rename cert types and subcategories | [ ] | | |
| 20_20260324_UpdatePartnerDescriptions.sql | Update Novi description, add Wayfair | [ ] | | |
| 21_20260324_UpdatePermissionDescriptions.sql | Update permission descriptions | [ ] | | |

---

## Test Execution Log

| Date | Tester | Sections Tested | Pass | Fail | Notes |
|------|--------|----------------|------|------|-------|
| | | | | | |
| | | | | | |
| | | | | | |

---

## Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Dev Lead | | | |
| QA Tester | | | |
| Client (GCC) | | | |
