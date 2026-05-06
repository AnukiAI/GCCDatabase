# GCC Database Solution — QC Retest Plan & Deployment Checklist
**Date:** 2026-04-15
**Prepared by:** Anuki Consulting
**Total QC Items:** 163

---

## Table of Contents
1. [Status Summary](#1-status-summary)
2. [Deployment Checklist](#2-deployment-checklist)
3. [SQL Scripts to Run (Pre-Deploy)](#3-sql-scripts-to-run)
4. [Critical/High Priority Items](#4-critical-items)
5. [Retest Plan — All Open Items with Test Cases](#5-retest-plan)
6. [Items Needing Discussion/Decision](#6-discussion-items)
7. [Timeline for Client](#7-timeline)

---

## 1. Status Summary

| Status | Count | Description |
|--------|-------|-------------|
| Complete/Closed | ~110 | Confirmed fixed by client or working as designed |
| Needs Tested | ~40 | Code fix written, awaiting client UAT verification |
| Needs Clarification | ~5 | Blocked on client response |
| Needs Discussion | ~4 | Requires design decision or Change Request |
| Not a Defect | ~4 | Informational, no code change needed |

### Unstaged Code Changes (24 files — must be committed and deployed):
**Controllers:** CertificateTileExternalApiController, CertificateTilesApiController, CertificatesController (v1 API), CertificateController, DashboardController, ExportController
**Models:** CertificateStatus.cs
**Services:** CertificateWorkflowService, FilterService
**Repositories:** CertificateRepository
**Views:** CertificateTileView.cshtml, Create.cshtml, Edit.cshtml
**CSS:** certificateTileViewExternal.css, certificatetileview.css, main.css
**JS:** admin-core.js, productManagement.js, certificate-tile-view-external.js, certificate-tile-view-internal.js, certificate-tile-view.js, dashboard.js, tags.js, userManagement.js

---

## 2. Deployment Checklist

### Pre-Deployment
- [ ] **BACKUP production database** before any changes
- [ ] Run all SQL migration scripts (see Section 3)
- [ ] Verify SQL script results with SELECT queries
- [ ] Commit all 24 unstaged files
- [ ] Build project: `dotnet build` — confirm 0 errors
- [ ] Publish: `dotnet publish -c Release`

### Deployment Steps
- [ ] Stop the application on the server
- [ ] Deploy the published files to production
- [ ] Clear browser cache / advise client to hard-refresh (Ctrl+Shift+R)
- [ ] Start the application
- [ ] Smoke test: public view loads, login works, dashboard loads

### Post-Deployment Verification
- [ ] Public tile view shows only "Client Approved - Paid" and "Republished" certs
- [ ] Dates display correctly (no off-by-one)
- [ ] Filters work (Certification Type, Company, Additional filters)
- [ ] Login works with admin/analyst/external test accounts
- [ ] Dashboard stats are accurate
- [ ] Create a test certificate and verify workflow

---

## 3. SQL Scripts to Run

Run these **in order** against the production database (`gccwebap_certificates`):

| Order | Script | QC Items | Purpose |
|-------|--------|----------|---------|
| 1 | `Scripts/UpdateCertificationTypes.sql` | QC 45, 47, 49 | Add 4 missing cert types, deactivate 2, rename 1, add subcategories |
| 2 | `Scripts/MIGRATE_CERTIFICATION_TYPE_SUBCATEGORY.sql` | QC 47-49 | Migrate child cert types to new SubcategoryId field |
| 3 | `Database_Scripts/FixMojibakeProductNames.sql` | QC 123 | Fix special characters (trademark/registered symbols) in 5 product names |
| 4 | `Scripts/CapitalizeStatus.sql` | QC 22 | Normalize status value capitalization |

**Important:** Script #3 (FixMojibake) has a manual COMMIT — review the preview output before uncommenting COMMIT.

### Data Cleanup (Manual)
- [ ] Delete test products imported via sample template data (QC 102)
- [ ] Delete test partner entries "test", "Carly", "cARLY tEST" (QC 145)
- [ ] Fix Product Type hierarchy: move HDPE, LDPE, LLDPE, PET, PP, PVC under "Plastics" parent (QC 94) — use Admin UI or SQL UPDATE

---

## 4. Critical Items (Must Verify First)

These are the items most likely to cause client-visible problems:

| QC | Priority | Issue | Risk if Broken |
|----|----------|-------|----------------|
| 135, 136 | **CRITICAL** | Dates off by 1 day / certs disappearing on expiration day | Certs vanish from public DB prematurely |
| 128 | **HIGH** | LEED/APR additional filters not working on deployed site | Core search broken |
| 107, 109 | **HIGH** | Cannot create/import users | Blocks all user management |
| 117 | **HIGH** | Product Types hierarchy creating wrong data | Data corruption risk |
| 134 | **HIGH** | Duplicate certificates appearing | Data integrity appearance |
| 154 | **HIGH** | Dashboard excluding Confidential certs from stats | Inaccurate reporting |
| 127 | **HIGH** | Company filter showing numbers instead of names | Filters unusable |

---

## 5. Retest Plan — All Open Items

### How to Use This Section
- Log in as the specified user role
- Follow the **Steps** exactly
- Check each **Expected Result**
- Mark Pass/Fail and note any issues
- Screenshots recommended for failures

---

### QC 32 — Product Categories Filter Hierarchy
**Status:** Needs Clarification | **Page:** Public view + Admin
**Issue:** Packaging/Paper/Plastic showing under wrong parent category

**Test Steps:**
1. Go to public view (no login required)
2. Look at the Product Categories filter sidebar on the left
3. Expand "Packaging" — verify Paper and Plastic are listed under it
4. Expand "Chemicals and Raw Materials" — verify Paper/Plastic are NOT listed here
5. Go to Admin > Product Types and verify the hierarchy tree

**Expected:** Paper and Plastic appear under Packaging only, not under Chemicals.
**Login:** None (public) + admin/TestPass123! (Admin)

---

### QC 50 — Create Cert Page Mobile View
**Status:** Needs Tested | **Priority:** Low | **Page:** Create cert page
**Issue:** Text/boxes distorted on mobile

**Test Steps:**
1. Login as analyst (analyst/TestPass123!)
2. Navigate to Certificate > Create
3. Open browser DevTools (F12) > Toggle device toolbar (Ctrl+Shift+M)
4. Test at iPhone 12 (390x844) and iPad (768x1024)
5. Verify form fields stack in single column on mobile
6. Verify all labels readable (16px+ font)
7. Verify PDF upload zone is tappable
8. Verify Submit button is accessible and full-width

**Expected:** Form is usable on mobile/tablet. Fields stack, text readable, no overflow.
**Login:** analyst / TestPass123!

---

### QC 51 — Random Characters in Product Names
**Status:** Needs Clarification | **Page:** Global
**Issue:** Unicode characters (trademark, registered, umlaut) displaying incorrectly

**Pre-Req:** Run `Database_Scripts/FixMojibakeProductNames.sql` first

**Test Steps:**
1. Login as admin
2. Go to Product Management
3. Search for products with IDs 62, 71, 73, 76, 80
4. Verify names display with proper symbols: HID Signo™, GSB-88®, Sika®, M3 CLIQ®
5. Go to public tile view and search for same products
6. Verify symbols render correctly in tiles

**Expected:** Trademark and registered symbols display correctly, no garbled characters.
**Login:** admin / TestPass123!

---

### QC 56 — Country Dropdown Only 5 Countries
**Status:** Open | **Page:** Create/Edit cert page
**Issue:** Only 5 countries listed, need full ISO list

**Test Steps:**
1. Login as analyst
2. Navigate to Certificate > Create
3. Click the Country dropdown
4. Verify full list of 195 countries appears
5. Verify United States appears near top or is easily findable
6. Type "Germ" — verify Germany appears (type-ahead if implemented)
7. Select a country, save the cert, then Edit — verify country is preserved

**Expected:** Full ISO country list, searchable, saves correctly.
**Login:** analyst / TestPass123!

---

### QC 59 — Type-Ahead Autocomplete on Dropdowns
**Status:** Open | **Page:** Create/Edit cert page
**Issue:** Long dropdowns need type-ahead filtering

**Test Steps:**
1. Login as analyst
2. Navigate to Certificate > Create
3. Click Company Name dropdown — start typing a company name
4. Verify list filters as you type
5. Click Certification Type dropdown — start typing
6. Verify list filters
7. Click Country dropdown — start typing
8. Verify list filters
9. Click Product Category — start typing
10. Verify list filters

**Expected:** All 5 key dropdowns (Company, Product, Cert Type, Country, Product Category) support type-ahead filtering.
**Login:** analyst / TestPass123!

---

### QC 60 — Cert Image Cutoff Post-Creation
**Status:** Open | **Page:** Certificate detail view
**Issue:** Certificate preview image getting cut off, buttons misaligned

**Test Steps:**
1. Login as analyst
2. Create a new certificate with a tall/portrait PDF uploaded
3. After creation, view the certificate detail page
4. Verify the certificate image displays fully (no clipping)
5. Verify the image container uses proper aspect ratio (8.5:11)
6. Verify Edit and Delete buttons are properly aligned below the image
7. Check on a certificate with landscape orientation too

**Expected:** Images display fully without cutoff. Buttons aligned below image.
**Login:** analyst / TestPass123!

---

### QC 66 — Status Text Font Size on Internal Tiles
**Status:** Needs Tested (re-fix) | **Page:** Analyst cert listing (tile view)
**Issue:** Status text still too large on tile cards

**Test Steps:**
1. Login as analyst (analyst/TestPass123!)
2. Go to Certificate listing (tile view)
3. Inspect the status badge text on any tile card
4. Verify font size is 13px (not 16px)
5. Check with a long product name — verify status doesn't push the layout

**Expected:** Status badges are 13px, visually smaller than product name text.
**Login:** analyst / TestPass123!

---

### QC 71 — Session Timeout Shows "This Page Isn't Working"
**Status:** Needs Tested | **Page:** Login / Global
**Issue:** HTTP 400 error on session timeout instead of redirect to login

**Test Steps:**
1. Login as analyst
2. Open browser DevTools > Application > Cookies
3. Delete the auth cookie manually (simulates session expiration)
4. Try navigating to any authenticated page
5. Verify you are redirected to the login page with a "Session expired" message
6. Verify you do NOT see "This page isn't working" / HTTP 400
7. Also test: Leave a page open, wait 30+ minutes, try to interact
8. Navigate to `/Account/LogoutPost` directly via URL bar
9. Verify it redirects to login (not HTTP 405)

**Expected:** All expired session paths redirect to login with clear message. No raw HTTP errors.
**Login:** analyst / TestPass123!

---

### QC 78 — Product ID Export Header "Value" vs "Product ID"
**Status:** Needs Tested | **Page:** Certificate Create/Edit > Product ID Manager
**Issue:** Export CSV header says "Value" instead of "Product ID"

**Test Steps:**
1. Login as analyst
2. Open a certificate with product IDs (Edit page)
3. In the Product ID Manager section, click Export
4. Open the downloaded CSV
5. Verify the header says "Product ID" (not "Value")
6. Click Template download
7. Verify template header also says "Product ID"
8. Try re-importing the exported file
9. Verify it imports successfully (column name matches)

**Expected:** Export and Template headers say "Product ID". Re-import works.
**Login:** analyst / TestPass123!

---

### QC 80 — Product ID Manager Edit Freezes/Grays Out
**Status:** Needs Tested | **Page:** Certificate Create > Product ID Manager
**Issue:** Editing product IDs causes modal to freeze/gray out

**Test Steps:**
1. Login as analyst
2. Navigate to Certificate > Create
3. In Product ID Manager, click Add to add a product ID
4. Fill in fields, click Save
5. Click Edit on the just-added product ID
6. Verify the Edit modal opens with fields editable (not grayed out)
7. Modify a value and Save
8. Verify the change is reflected
9. Repeat Add + Edit sequence 3-4 times — verify no duplicate modal backdrops

**Expected:** Edit modal opens cleanly every time. No gray overlay buildup. Fields are editable.
**Login:** analyst / TestPass123!

---

### QC 84 — Internal Cert List Missing Columns
**Status:** Needs Tested | **Page:** Login > Cert listing (list view)
**Issue:** Missing Company URL, Cert Subcategories, Cert Results, Pathway columns

**Test Steps:**
1. Login as analyst
2. Switch to list view on the certificate listing page
3. Click the "Columns" dropdown button
4. Verify these columns exist in the toggle list: Company URL, Certificate Subcategories, Certification Results, Pathway
5. Enable all four columns
6. Verify data appears in those columns for certs that have the data
7. Scroll right — verify Company Contact Email is the LAST column
8. Verify Company Contact Email text is not cut off (min 220px width)

**Expected:** All 4 columns available in toggle, display correct data. Contact Email is last column.
**Login:** analyst / TestPass123!

---

### QC 86 — List View Vertical Scrolling Broken
**Status:** Needs Tested | **Page:** Login > Cert listing (list view)
**Issue:** Can't scroll vertically to see more rows after horizontal scroll fix

**Test Steps:**
1. Login as analyst
2. Switch to list view
3. Scroll down with mouse wheel (no Shift key) — verify page scrolls vertically
4. Scroll to the bottom — verify infinite scroll loads more results
5. Scroll back up — verify all rows are still visible
6. Hold Shift + scroll — verify horizontal scrolling works
7. Verify the horizontal scrollbar at top/bottom still works for horizontal scroll

**Expected:** Normal vertical scroll works. Shift+scroll does horizontal. Infinite scroll loads more.
**Login:** analyst / TestPass123!

---

### QC 91 — Product Management Sort Arrows Overlapping Text
**Status:** Needs Tested (re-fix) | **Page:** Product Management
**Issue:** Sort arrows overlap column header text

**Test Steps:**
1. Login as admin
2. Go to Product Management
3. Verify sort arrows are inline next to header text (not overlapping)
4. Click each column header — verify sort arrow toggles between up/down
5. Verify Product Type and Subcategory columns are present and sortable

**Expected:** Sort arrows inline, no overlap. All columns sortable.
**Login:** admin / TestPass123!

---

### QC 95 — Product Management Sorting Broken
**Status:** Needs Tested (re-fix) | **Page:** Product Management
**Issue:** Ascending/descending sorting not working on columns

**Test Steps:**
1. Login as admin
2. Go to Product Management
3. Click "Company" header — verify list sorts by company name A-Z
4. Click again — verify Z-A
5. Repeat for: Category, Product Type, Subcategory
6. Click "Certificates" header — verify sorts by count (numeric)
7. Click "Identifiers" header — verify sorts by count (numeric)
8. Verify sort arrow icon updates correctly when switching columns

**Expected:** All columns sort correctly in both directions. Icons update properly.
**Login:** admin / TestPass123!

---

### QC 96 — Nav Appearing Behind Search Bar
**Status:** Needs Tested (re-fix) | **Page:** Login > Global
**Issue:** Navigation dropdown renders behind page content

**Test Steps:**
1. Login as analyst
2. On any page with a search bar or content near the top
3. Open the navigation dropdown menu
4. Verify the dropdown appears ABOVE/ON TOP OF all page content
5. Verify it doesn't render behind the search bar
6. Test on the certificate tile view page specifically

**Expected:** Nav dropdown always renders on top of all page content.
**Login:** analyst / TestPass123!

---

### QC 103 — Product Management Export Headers Don't Match
**Status:** Needs Tested (re-fix) | **Page:** Product Management
**Issue:** Export CSV columns don't match template/Add Product modal fields

**Test Steps:**
1. Login as admin
2. Go to Product Management
3. Click "Template" download — verify headers: Name, Company, Description, Category, Product Type, Subcategory
4. Click "Export" — verify headers match: Name, Company, Description, Category, Product Type, Subcategory (+ Identifier Type, Identifier Value)
5. Verify NO "Status" column in either file
6. Verify Description column IS present
7. Import the template with a test product — verify it imports correctly

**Expected:** Template and Export headers aligned. No Status column. Description present.
**Login:** admin / TestPass123!

---

### QC 104 — Breadcrumbs Broken Links
**Status:** Needs Tested (re-fix) | **Page:** Login > All pages
**Issue:** Admin Dashboard not accessible via nav, breadcrumbs go to error pages

**Test Steps:**
1. Login as admin
2. Verify "Administration" breadcrumb links to Admin Dashboard
3. Verify "Data Management", "API Management", "System Settings" breadcrumbs are NOT clickable links
4. Verify "Companies & Products" breadcrumb is NOT a clickable link
5. Navigate to Company Management — verify breadcrumb trail: Home > Administration > Companies & Products > Company Management
6. Verify current page breadcrumb is not a link
7. Check that Admin Dashboard is accessible from the nav dropdown

**Expected:** Administration links to dashboard. Category breadcrumbs are plain text. No broken links.
**Login:** admin / TestPass123!

---

### QC 106 — User Management Company Field
**Status:** Needs Tested (re-fix) | **Page:** Admin > User Management
**Issue:** Add New User missing Company field, need Check All, alphabetical order

**Test Steps:**
1. Login as admin
2. Go to Admin > User Management
3. Click "Add New User"
4. Verify Company field is visible for Admin, Analyst, and External roles
5. Switch role to "Partner" — verify Company field is replaced with Partner Assignment
6. With Admin selected, verify companies are listed alphabetically
7. Verify "Check All" checkbox at top of company list
8. Click "Check All" — verify all companies are selected
9. Uncheck "Check All" — verify all are deselected
10. Open an existing user for Edit — verify company selections are preserved

**Expected:** Company field visible for all roles except Partner. Check All works. Alphabetical order.
**Login:** admin / TestPass123!

---

### QC 107 — Cannot Create User
**Status:** Needs Tested (re-fix) | **Page:** Admin > User Management
**Issue:** "Failed to save user" error when adding users

**Test Steps:**
1. Login as admin
2. Go to Admin > User Management
3. Click "Add New User"
4. Fill in: Name = "Test User", Email = unique@test.com, Role = Analyst, Password = TestPass123!
5. Click Save — verify user is created successfully with toast notification
6. Try creating another user with the SAME email
7. Verify error message: "A user with email 'unique@test.com' already exists"
8. Try with a short password (less than 8 chars) — verify validation error is shown to user
9. Clean up: delete the test user after testing

**Expected:** User creation works. Duplicate email gives clear error. Validation errors visible.
**Login:** admin / TestPass123!

---

### QC 108 — User Template Export
**Status:** Needs Tested (re-fix) | **Page:** Admin > User Management
**Issue:** Template needs proper headers with hints, column alignment

**Test Steps:**
1. Login as admin
2. Go to Admin > User Management
3. Click "Template" download
4. Open CSV — verify headers include hints: Role (Admin/Analyst/External), Status (Active/Inactive), RequirePasswordReset (Yes/No), User Company
5. Verify NO sample data rows (headers only)
6. Click "Export" — verify "User Company" header (not just "Company")
7. Verify export does NOT include ID column
8. Verify multi-company users show semicolon-separated company names

**Expected:** Template has hint headers, no sample data. Export matches template naming.
**Login:** admin / TestPass123!

---

### QC 109 — User Import Not Working
**Status:** Needs Tested (re-fix) | **Page:** Admin > User Management
**Issue:** CSV import fails with error

**Test Steps:**
1. Login as admin
2. Download the Template (QC 108)
3. Fill in one row: TestImport, import@test.com, Analyst, TestPass123!, Active, Yes, [company name]
4. Go to User Management > Import
5. Select the CSV file
6. Verify preview shows correctly
7. Click Import — verify user is created
8. Verify the imported user appears in the user list
9. Try importing a file with errors (bad role, missing email) — verify error messages are shown per-row
10. Clean up: delete test user

**Expected:** Import works. Errors show specific failure reasons per row.
**Login:** admin / TestPass123!

---

### QC 117 — Product Types Hierarchy Wrong
**Status:** Needs Tested (re-fix) | **Page:** Admin > Product Types
**Issue:** Adding items creates them under wrong parent / can't delete

**Test Steps:**
1. Login as admin
2. Go to Admin > Product Types
3. Click "Add Category" — enter "Test Category" — Save
4. Verify it appears as a new root-level category (NOT under Chemicals)
5. Click to add a Type under "Test Category"
6. Verify the Type appears under "Test Category" (not elsewhere)
7. Add a Subcategory under the new Type — verify correct nesting
8. Delete the Subcategory — verify it deletes
9. Delete the Type — verify it deletes
10. Delete "Test Category" — verify it deletes
11. Verify existing hierarchy (Chemicals > Additives, Plastics, etc.) is intact

**Expected:** New items nest correctly. Deletion works at all levels. No data corruption.
**Login:** admin / TestPass123!

---

### QC 118 — Certificate Field Management Error
**Status:** Needs Tested (re-fix) | **Page:** Admin > Certificate Field Management
**Issue:** Error when adding a field; section headers should be removed from cert forms

**Test Steps:**
1. Login as admin
2. Go to Admin > Certificate Field Management
3. Click "Add New Field"
4. Fill in: Label = "Test Field", Type = Text, Section = any
5. Click Save — verify field is created (no error)
6. Navigate to Certificate > Create
7. Verify "Test Field" appears under "Additional Fields" section
8. Verify there are NO sub-section headers (all fields flat under one heading)
9. Fill in the test field value, save the certificate
10. Edit the certificate — verify the custom field value is preserved
11. Export certificates — verify the custom field appears as a column
12. Clean up: delete the test field

**Expected:** Fields create without error. Render flat under "Additional Fields". Export includes them.
**Login:** admin / TestPass123!

---

### QC 123 — Special Characters in Product Names
**Status:** Needs Tested | **Page:** Public view
**Issue:** Certificate #23-0375 showing garbled characters

**Pre-Req:** Run `Database_Scripts/FixMojibakeProductNames.sql`

**Test Steps:**
1. Go to public view
2. Search for certificate 23-0375
3. Verify product name displays correctly with proper trademark/registered symbols
4. Check other certs with ™ or ® symbols

**Expected:** Proper Unicode symbols display. No garbled characters.
**Login:** None required

---

### QC 127 — Company Filter Showing Numbers, Remove Type Labels
**Status:** Needs Tested | **Page:** Public view
**Issue:** Selecting company shows "227" instead of "Amazon"; filter chips have unnecessary type labels

**Test Steps:**
1. Go to public view (no login)
2. Check a Company checkbox in the filter sidebar
3. Verify the filter chip at the top shows the company NAME (e.g., "Amazon"), not a number
4. Verify the chip does NOT have a "Company:" prefix — just the name + X button
5. Select a Certification Type filter — verify chip shows type name without "Type:" prefix
6. Use the search bar — verify chip shows search term without "Search:" prefix
7. Clear all filters — verify all chips are removed

**Expected:** Filter chips show clean values (no type labels, no IDs).
**Login:** None required

---

### QC 128 — Additional Filters (LEED/APR) Not Working
**Status:** Needs Tested | **Page:** Public view
**Issue:** Selecting LEED tag shows all certs instead of filtering

**Test Steps:**
1. Go to public view
2. Under "Additional Filters", check the LEED checkbox
3. Verify ONLY certificates with the LEED tag are shown
4. Uncheck LEED, check APR — verify only APR-tagged certs show
5. Check both — verify certs with either tag show
6. Open browser DevTools > Network tab
7. Verify the API call uses `?tags=LEED` (not `?additional=LEED`)
8. Clear all filters — verify all certs return

**Expected:** LEED/APR filters correctly filter certificates by tag.
**Login:** None required

---

### QC 129 — Search Autocomplete Showing Non-Public Results
**Status:** Needs Tested | **Page:** Public view
**Issue:** Search suggestions include companies/products without public certs

**Test Steps:**
1. Go to public view
2. Start typing "Amazon" in the search bar
3. Verify ONLY companies/products with visible public certificates appear in suggestions
4. Verify "Amazon Devices Company" does NOT appear (no public certs)
5. Verify suggestions do NOT have type labels ("Company", "Product" etc.) appended
6. Type "Dude" — verify "Dude Wipes" does NOT appear if it has no public certs
7. Select a suggestion — verify it filters correctly

**Expected:** Autocomplete only suggests entities with public-visible certificates.
**Login:** None required

---

### QC 130 — Remove Product Type A-Z Sort Option
**Status:** Needs Tested | **Page:** Public view
**Issue:** Product Type doesn't show on tiles, so sort option is confusing

**Test Steps:**
1. Go to public view
2. Click the "Sort By" dropdown
3. Verify "Product Type A-Z" is NOT in the list
4. Verify remaining sort options: By Certificate (4), Alphabetical (4 — Company A-Z/Z-A, Product A-Z/Z-A), By Location (4)

**Expected:** Product Type A-Z sort removed from public view.
**Login:** None required

---

### QC 131 — Reduce Tile Size (Product Name 3 Lines, Remove Subcategory)
**Status:** Needs Tested | **Page:** Public view
**Issue:** Tiles too tall, reduce product name lines, remove product subcategory

**Test Steps:**
1. Go to public view
2. Look at tile cards
3. Verify product name truncates at 3-4 lines maximum with "..." ellipsis
4. Verify product subcategory (e.g., "Home Goods") is NOT displayed on tiles
5. Verify company name truncates with "..." for long names (1 fewer line than before)
6. Compare tile height to previous — should be noticeably smaller
7. Check internal and external tile views too — same changes should apply

**Expected:** Tiles are shorter. Product name max 3-4 lines. No subcategory. Company name truncated.
**Login:** None (public), analyst (internal), external (external)

---

### QC 132 — Country Filter Missing Countries
**Status:** Needs Tested (Public + Internal + External) | **Page:** Public, Internal, External tile views
**Issue:** Germany, Canada missing from country filter

**Test Steps:**
1. **Public** — go to `/Certificate/CertificateTileView` (logged out)
2. Look at the Country filter in the sidebar
3. Verify "United States" appears at the top of the list
4. Verify remaining countries are in A-Z order
5. If Germany/Canada have active certs, verify they appear in the list
6. Note: Filter only shows countries with active (Client Approved - Paid / Republished) non-expired certs
7. **Internal** — log in as analyst, go to `/Certificate/CertificateTileViewInternal`. Repeat steps 2-5. (Internal shows ALL countries including those used by Drafts/Holds — by design.)
8. **External** — log in as external user, go to `/Certificate/CertificateTileViewExternal`. Repeat steps 2-6.

**Expected:** United States at top, rest A-Z, on all three views. Public and External show only countries with public certs; Internal shows all.
**Login:** None (Public), analyst / TestPass123! (Internal), external / TestPass123! (External)
**Defect refs:** Original Public fix on 4/10 SP2; Internal + External coverage extended via DEF-144 (2026-05-05).

---

### QC 133 — Don't Prepopulate Dates on Cert Creation
**Status:** Needs Tested | **Page:** Certificate > Create
**Issue:** Effective and Expiration dates auto-filled, could cause errors

**Test Steps:**
1. Login as analyst
2. Navigate to Certificate > Create
3. Verify Effective Date field is BLANK (not today's date)
4. Verify Expiration Date field is BLANK (not 2 years from now)
5. Try to submit the form without dates — verify validation error
6. Fill in dates and submit — verify cert creates correctly
7. Edit the cert — verify dates are preserved correctly

**Expected:** Date fields start blank. Both required. Validation prevents empty submission.
**Login:** analyst / TestPass123!

---

### QC 134 — Duplicate Certificates Appearing
**Status:** Needs Tested | **Page:** Analyst cert listing
**Issue:** Newly created certs show twice intermittently

**Test Steps:**
1. Login as analyst
2. Create a new certificate
3. Immediately go to the certificate listing (tile view)
4. Verify the new cert appears exactly ONCE
5. Scroll down past the first page — verify no duplicates
6. Refresh the page — verify still no duplicates
7. Switch between tile and list view — verify no duplicates
8. Repeat create + check 2-3 times

**Expected:** Each certificate appears exactly once in all views.
**Login:** analyst / TestPass123!

---

### QC 135 — Certs Disappearing on Expiration Day
**Status:** Needs Tested | **CRITICAL** | **Page:** Public view
**Issue:** Certificates vanish from public DB on their expiration date instead of day after

**Test Steps:**
1. Login as analyst
2. Create or edit a certificate:
   - Status: "Client Approved - Paid"
   - Expiration Date: today (2026-04-15)
3. Go to public view (log out or incognito)
4. Verify the certificate IS visible (should show through end of expiration day)
5. If possible, test after 8pm EDT (when UTC rolls to next day) — verify cert still shows
6. The cert should disappear the DAY AFTER the expiration date

**Expected:** Cert visible on its expiration day. Disappears the next day.
**Login:** analyst / TestPass123! (to set up), then public view

---

### QC 136 — Dates Off by 1 Day in Public View
**Status:** Needs Tested | **CRITICAL** | **Page:** Public view
**Issue:** Dates display one day earlier than entered (e.g., entered 4/4, shows 4/3)

**Test Steps:**
1. Login as analyst
2. Create a cert with Effective Date = 2026-05-15, Expiration Date = 2027-05-15
3. Go to public tile view — click the cert tile
4. On the detail page, verify Effective Date shows "May 15, 2026" (not May 14)
5. Verify Expiration Date shows "May 15, 2027" (not May 14)
6. Check the tile card itself — verify date on card is correct
7. Check internal view (login as analyst) — verify dates correct
8. Check external view (login as external) — verify dates correct
9. Test in the evening (after 8pm EDT) — dates should still be correct

**Expected:** Dates match exactly what was entered. No off-by-one in any view or timezone.
**Login:** analyst / TestPass123!, external / TestPass123!

---

### QC 137 — Search Icon Overlapping Box Border
**Status:** Needs Tested | **Page:** Public view
**Issue:** Magnifying glass icon overlaps the search input border

**Test Steps:**
1. Go to public view
2. Look at the search bar
3. Verify the magnifying glass icon is inside the box with clear spacing from the left border
4. Verify the icon doesn't overlap the input border
5. Click into the search box — verify cursor position doesn't overlap with icon
6. Check on mobile viewport too

**Expected:** Search icon has clear spacing, no overlap with border.
**Login:** None required

---

### QC 138 — Mobile Spacing Below External Tags
**Status:** Needs Tested | **Page:** Public tile view (mobile)
**Issue:** Tags too close to card bottom on external view

**Test Steps:**
1. Go to public view on mobile (or DevTools mobile viewport)
2. Look at cert tiles with external tags (e.g., LEED, APR)
3. Verify there is adequate spacing (margin/padding) below the tag pills
4. Verify tags don't touch or crowd the download/view icons at the bottom

**Expected:** Visible breathing room between tags and card bottom.
**Login:** None required

---

### QC 139 — Remove "(Optional)" from Pathway Field
**Status:** Needs Tested | **Page:** Certificate Create/Edit
**Issue:** Pathway label shows "(Optional)" which is confusing

**Test Steps:**
1. Login as analyst
2. Go to Certificate > Create
3. Find the Pathway field
4. Verify label reads "Pathway (for Amazon CPF applicable certs)" — NO "(Optional)" text
5. Go to Certificate > Edit on an existing cert
6. Verify same label without "(Optional)"

**Expected:** Pathway label has no "(Optional)" text.
**Login:** analyst / TestPass123!

---

### QC 140 — List View Styling (Font, Wrap, Height, Actions)
**Status:** Needs Tested | **Page:** Login > Cert list view
**Issue:** Status font too large, text cut off, cells too tall, action icons misaligned

**Test Steps:**
1. Login as analyst
2. Switch to list view
3. Verify status badge font is smaller (12px or less)
4. Verify text wraps in columns instead of being cut off
5. Verify row height is condensed (compact)
6. Verify action icons (view, edit, delete) are vertically centered in the Actions column
7. Check columns with long text — verify word wrapping, no overflow

**Expected:** Compact rows, small status badges, text wraps, actions centered.
**Login:** analyst / TestPass123!

---

### QC 141 — Dashboard Export Issues
**Status:** Needs Tested | **Page:** Login > Dashboard
**Issue:** Product category/type/subcategory exports don't have detail columns; status export incomplete

**Test Steps:**
1. Login as admin
2. Go to Dashboard
3. Click export for "# Product Categories with Active Certs"
4. Open CSV — verify columns: Name, Count (not raw cert list)
5. Click export for "# Product Types with Active Certs" — verify same format
6. Click export for "# Product Subcategories" — verify same format
7. Click export for "# Certs by Status"
8. Verify CSV shows ALL 8 statuses with counts (not just Paid/Republished)
9. Click export for "Top 10 Product Types" — verify product type names with counts
10. Click export for "Top 5 Product Categories" — verify category names with counts

**Expected:** All dashboard exports contain correct summary data with proper headers.
**Login:** admin / TestPass123!

---

### QC 142 — Delete Product Popup Missing Button Text
**Status:** Needs Tested | **Page:** Product Management
**Issue:** Confirmation modal buttons have no visible text

**Test Steps:**
1. Login as admin
2. Go to Product Management
3. Click the delete (trash) icon on a product with 0 certificates
4. Verify the confirmation modal appears with visible button text (e.g., "Delete" / "Cancel")
5. Verify button text is white on the colored button background
6. Click Cancel — verify modal closes
7. Try deleting a product WITH certificates — verify block message appears with visible OK button

**Expected:** All modal buttons have visible white text. Delete blocked for products with certs.
**Login:** admin / TestPass123!

---

### QC 145 — Partner API Access: Delete Test Entries
**Status:** Needs Tested | **Page:** Admin > Partner API Access
**Issue:** Test entries "test", "Carly", "cARLY tEST" need deletion; revoke/delete workflow unclear

**Test Steps:**
1. Login as admin
2. Go to Admin > Partner API Access
3. Find any active API key — verify "Revoke" button (ban icon) is visible
4. Revoke a test key — verify status changes to "Revoked"
5. On the revoked key, verify "Delete" button (trash icon) appears
6. Delete the revoked key — verify it is removed from the list
7. Verify you cannot delete an active key without revoking first
8. Clean up test entries: "test", "Carly", "cARLY tEST"

**Expected:** Revoke flow: Active > Revoke > Delete. Can't skip revoke step.
**Login:** admin / TestPass123!

---

### QC 146 — Tags Page Header Alignment
**Status:** Ready for UAT | **Page:** Admin > Tags
**Issue:** Page title right-aligned next to buttons

**Test Steps:**
1. Login as admin
2. Go to Admin > Tags
3. Verify "Tag Management" title is LEFT-aligned
4. Verify action buttons (Add, Import, Export, Template) are RIGHT-aligned
5. Verify this matches the layout of other admin pages (Users, Products, etc.)

**Expected:** Title left, buttons right — consistent with other admin pages.
**Login:** admin / TestPass123!

---

### QC 147 — Import/Export Icon Arrows Wrong Direction
**Status:** Needs Tested | **Page:** Global (all admin pages)
**Issue:** Import and Export icons had wrong arrow directions

**Test Steps:**
1. Login as admin
2. Visit each admin page with Import/Export buttons: User Management, Product Management, Tags, Partner API
3. Verify Import icon: `fa-file-import` (arrow pointing INTO document)
4. Verify Export icon: `fa-file-export` (arrow pointing OUT OF document)
5. Verify consistency across ALL pages

**Expected:** Import = arrow in, Export = arrow out. Consistent everywhere.
**Login:** admin / TestPass123!

---

### QC 148 — Template Headers Need Option Hints
**Status:** Needs Tested | **Page:** Global templates
**Issue:** Template headers don't indicate valid values

**Test Steps:**
1. Login as admin
2. Download Product Management Template — verify header: "Status (Active, Inactive)"
3. Download Tags Template — verify header: "TagType (Internal, External)"
4. Download User Management Template — verify headers include hints per QC 108

**Expected:** Template headers include valid option values in parentheses.
**Login:** admin / TestPass123!

---

### QC 149 — Product Category Filter Level 2/3 Not Working
**Status:** Needs Tested | **Page:** Public view
**Issue:** Selecting a parent product type doesn't filter child types' certs

**Test Steps:**
1. Go to public view
2. In the Product Categories filter, expand to see Level 2 (Product Types)
3. Select a Level 2 parent type (e.g., "Plastics")
4. Verify certs assigned to child types (e.g., HDPE, LDPE) are ALSO included in results
5. Select a Level 3 child type directly (e.g., "HDPE") — verify only HDPE certs show
6. Deselect all — verify all certs return

**Expected:** Parent type selection includes children. Child selection is specific.
**Login:** None required

---

### QC 152 — Mobile Search Icon Overlapping Border
**Status:** Needs Tested | **Page:** Public view (mobile)
**Issue:** Magnifying glass crowding against input border on mobile

**Test Steps:**
1. Open public view on mobile or DevTools mobile viewport (iPhone 12)
2. Look at the search bar
3. Verify magnifying glass icon has clear spacing from the left border
4. Verify input text doesn't overlap with the icon
5. Type a search term — verify cursor/text starts after the icon

**Expected:** Clear spacing between icon and border on mobile.
**Login:** None required

---

### QC 153 — Mobile Cert Detail Image Cutoff and Order
**Status:** Needs Tested | **Page:** Certificate detail (mobile)
**Issue:** Image displayed above content on mobile; buttons not aligned

**Test Steps:**
1. Open a certificate detail page on mobile viewport
2. Verify certificate details (text content) appear FIRST
3. Verify certificate image appears BELOW the content (not above)
4. Verify download/action buttons are centered on mobile
5. Verify image is not cut off

**Expected:** Content first, image below. Buttons centered. Image fully visible.
**Login:** None required (public) or analyst (internal)

---

### QC 154 — Dashboard Not Counting Confidential Certs
**Status:** Needs Tested | **Page:** Login > Dashboard
**Issue:** "Client Approved - Confidential" excluded from active cert reports

**Test Steps:**
1. Login as admin
2. Note the "Total Active Certs" count on the dashboard
3. Create or find a cert with status "Client Approved - Confidential"
4. Verify it IS counted in the "Total Active Certs" number
5. Verify it IS counted in "Companies with Active Certs"
6. Verify it IS counted in the Published reports
7. Verify it does NOT appear on the PUBLIC tile view (stays hidden from public)
8. Compare: Dashboard counts should include Paid + Republished + Confidential

**Expected:** Confidential certs counted in dashboard stats but hidden from public.
**Login:** admin / TestPass123!

---

### QC 155 — Dashboard Product Cat/Type/Sub Export Missing Details
**Status:** Needs Tested | **Page:** Login > Dashboard
**Issue:** Exports don't have product category/type/subcategory columns

**Test Steps:**
1. Login as admin
2. Dashboard > click export for "# Product Categories with Active Certs"
3. Open CSV — verify each category listed with its active cert count
4. Click export for "# Product Types with Active Certs" — verify type + count
5. Click export for "# Product Subcategories with Active Certs" — verify sub + count

**Expected:** Each export shows name + count breakdown (not single summary number).
**Login:** admin / TestPass123!

---

### QC 156 — Published Stat Labels Unclear
**Status:** Needs Tested | **Page:** Login > Dashboard
**Issue:** Labels say "# Certs Published" but only count active ones

**Test Steps:**
1. Login as admin
2. Look at dashboard stat cards
3. Verify labels read:
   - "# Active Certs Published Last Month" (not "# Certs Published Last Month")
   - "# Active Certs Published This Year"
   - "# Active Certs Published Last Year"

**Expected:** Labels include "Active" to clarify scope.
**Login:** admin / TestPass123!

---

### QC 157 — List View Column Sorting Issues
**Status:** Needs Tested | **Page:** Login > Cert list view
**Issue:** Status only sorts ascending; Location, Internal Tag, Partner, ASIN, UPC not sorting right

**Test Steps:**
1. Login as analyst
2. Switch to list view
3. Click "Status" header — verify sorts ascending
4. Click again — verify sorts descending
5. Click "Location" header — verify alphabetical A-Z
6. Click again — verify Z-A
7. Repeat for: Internal Tag, Partner, ASIN, UPC
8. Each column should toggle between ascending and descending correctly

**Expected:** All columns sort both directions correctly.
**Login:** analyst / TestPass123!

---

### QC 159 — Product ID Bulk Import Errors / No Replace Option
**Status:** Needs Tested | **Page:** Certificate Create/Edit > Product ID Manager
**Issue:** Import errors on duplicates across certs; no way to replace existing list

**Test Steps:**
1. Login as analyst
2. Edit a certificate with existing product IDs
3. Open Product ID Manager > Import
4. Import a CSV with some IDs that already exist on THIS cert and some new ones
5. Verify "Replace all existing" checkbox is present in the preview modal
6. WITHOUT Replace: verify existing IDs are kept, new ones added, no duplicate errors
7. WITH Replace checked: verify all old IDs are removed, only imported ones remain
8. Try importing an ID that exists on a DIFFERENT cert — verify it succeeds (not blocked by global duplicate check)

**Expected:** Import works. Replace toggle available. No cross-certificate duplicate blocking.
**Login:** analyst / TestPass123!

---

### QC 161 — Cert List Column Dropdown Missing Fields
**Status:** Needs Tested | **Page:** Login > Cert list view
**Issue:** Column settings missing: Certification Results, Pathway, Certificate Subcategories, Contact URL

**Test Steps:**
1. Login as analyst
2. Switch to list view
3. Open the "Columns" dropdown/settings
4. Look for "Additional" section — verify these 4 columns exist:
   - Certification Results
   - Pathway
   - Certificate Subcategories
   - Contact URL
5. Toggle each one ON — verify column appears with data
6. Toggle each one OFF — verify column hides
7. Verify these columns append to the END of the table

**Expected:** All 4 columns available in column picker under "Additional" section.
**Login:** analyst / TestPass123!

---

### QC 163 — Filter Boxes Won't Collapse (Logged-in Views)
**Status:** Needs Tested | **Page:** Login > Cert listing (internal/external)
**Issue:** Can't collapse filter boxes by clicking header arrow (works on public view)

**Test Steps:**
1. Login as analyst
2. Go to certificate listing (tile view)
3. Click the down arrow next to "Certification Type" filter header
4. Verify the filter section collapses (hides the checkboxes)
5. Click again — verify it expands
6. Repeat for: Company, Product Categories, Country, Additional Filters
7. Login as external user — verify same collapse behavior works
8. Verify public view still works too

**Expected:** All filter sections collapse/expand on click in all views.
**Login:** analyst / TestPass123!, external / TestPass123!

---

## 6. Discussion Items (Require Client Decision)

| QC | Topic | Decision Needed |
|----|-------|-----------------|
| 143 | **Partner Role Type** | System doesn't have a "Partner" role — it has Admin/Analyst/External. To scope API users to partner-specific data, we need a new feature. **This is a Change Request, not a bug fix.** |
| 150 | **Forgot Password** | Needs Azure AD B2C email verification with real email addresses. Not a code defect — infrastructure/config task. |
| 151 | **Duo MFA** | Azure AD B2C may support Duo through custom policies. Needs B2C plan tier confirmation. Config change, not code. |
| 158 | **Thousands of ASINs** | Current system loads ALL product IDs at once. Recommendation: collapsible section showing first 20 with "Show all X" button. Needs scope approval. |
| 124 | **Missing Cert Types in Filter** | ⚠ **CONFLICT — pending Shawn review (2026-05-05).** Today's "yes do it for Public and External" decision (DEF-145) reverts the **4/10 SP2 fix** that explicitly switched cert types filter to "show all configured types" in response to client (Alice)'s original complaint that "Certified Water Savings", "LCA Opt", "RM", "WD" were missing. Git evidence: commit 866c0ec (Apr 14 2026) removed the strict scoping; DEF-145 today re-added it. Need Shawn's call before closing this row: stand by DEF-145 (and notify client), revert just the cert-types portion of DEF-145, or formalize as a CR. Filter shows only types with active public certs vs. all configured types — depends on which side wins. |

---

## 7. Timeline for Client

### Proposed Retest Schedule

| Phase | Items | Duration | Target |
|-------|-------|----------|--------|
| **Phase 1: Deploy + Critical** | Deploy code + SQL scripts, test QC 135/136 (dates), 128 (filters), 107/109 (users), 127 (company filter) | 1 day | Day 1 |
| **Phase 2: Core Functionality** | QC 117 (product types), 134 (duplicates), 154 (dashboard stats), 133 (dates blank), 149 (product filters) | 1 day | Day 2 |
| **Phase 3: UI/UX Polish** | QC 66, 86, 91, 95, 96, 131, 137, 138, 140, 142, 146, 147 (fonts, spacing, icons, styling) | 1 day | Day 3 |
| **Phase 4: Admin Features** | QC 103, 104, 106, 108, 118 (templates, breadcrumbs, user mgmt, cert fields) | 1 day | Day 4 |
| **Phase 5: Remaining + Regression** | QC 50, 59, 60, 78, 80, 84, 123, 129-132, 139, 145, 148, 152-163 | 1-2 days | Day 5-6 |
| **Phase 6: Discussion Items** | QC 143, 150, 151, 158, 124 — decisions with GCC | Call | Day 7 |

### Deployment Summary for Client Email

> **GCC Database Solution — Sprint 2 Update (2026-04-15)**
>
> Total QC items: 163
> - Confirmed fixed: 110
> - Code complete, ready for retest: 40+
> - Awaiting client decision: 5
> - Data fixes (SQL): 4 scripts ready
>
> **Next steps:**
> 1. Deploy latest code + run database migration scripts
> 2. Client UAT retest of 40+ items (test cases provided)
> 3. Schedule call for 5 discussion items requiring decisions
>
> Estimated retest window: 5-6 business days

---

## Appendix: Quick Reference — Test Accounts

| User | Password | Role | Access |
|------|----------|------|--------|
| admin | TestPass123! | Admin | Full access, all admin pages |
| analyst | TestPass123! | Analyst | Create/edit certs, internal views |
| external | TestPass123! | External | External views, associated company data only |
| (no login) | — | Public | Public tile view only |

## Appendix: Pages to Test By Role

**Public (no login):** Certificate Tile View, Certificate Detail, Search, Filters, Sort
**External:** External Tile View, External Detail View, Filters
**Analyst:** Internal Tile View, List View, Create/Edit Cert, Product ID Manager
**Admin:** Dashboard, User Management, Product Management, Product Types, Certificate Types, Certificate Fields, Tags, Partners, Partner API Access, Roles
