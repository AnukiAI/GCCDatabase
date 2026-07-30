# QC Log — Final Comments & Testing Steps

**Project:** GCC Database Solution
**Prepared by:** Anuki Consulting
**Date:** 2026-03-25

---

## QC #7 — PDF Upload Click Target
**Page:** Certificate Upload
**Defect:** Users had to click the text to upload — clicking the PDF icon area did nothing.

### Final Comment
Fixed. The upload zone CSS class was mismatched (`pdf-upload` vs `pdf-upload-zone`), preventing the dashed border, pointer cursor, and hover styles from rendering. Corrected the class name in both Create and Edit views and updated the JS selector. The entire upload zone (icon + text + surrounding area) is now clickable with visible hover feedback.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Certificate > Create New | Create certificate page loads |  |
| 2 | Locate the PDF upload area | Dashed border visible around entire upload zone |  |
| 3 | Hover mouse over the PDF icon (not the text) | Cursor changes to pointer, hover highlight appears |  |
| 4 | Click the PDF icon area (not the text) | File picker opens |  |
| 5 | Cancel file picker, then click the "Upload" text | File picker opens |  |
| 6 | Click anywhere inside the dashed border area | File picker opens |  |
| 7 | Select a valid PDF file | File uploads successfully, filename displayed |  |
| 8 | Navigate to Certificate > Edit (existing cert) | Edit page loads |  |
| 9 | Repeat steps 2-7 on the Edit page | Same behavior as Create page |  |

---

## QC #11 — Certificate List Missing Details & Scroll/Freeze
**Page:** Certificates List
**Defect:** Missing product & company fields; needed top scroll bar and frozen headers.

### Final Comment
Fixed. All certificate detail columns are now present in the list view (22 columns total including company, product, category, type, subcategory, location, country, tags, partner, identifiers). A visible horizontal scrollbar is positioned above the header row for easy left-right navigation. Column headers are sticky (frozen) so they remain visible when scrolling down. Three-way scroll sync keeps the top scrollbar, header row, and table body aligned. A column toggle dropdown allows users to show/hide columns as needed.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as admin, navigate to Certificate List view | List view loads with data |  |
| 2 | Verify all columns present: cert#, type, status, dates, company, contact, product, category, type, subcategory, location, country, tags, partner, ASINs, UPC, GTIN, EAN | All 22 columns visible (or available via column toggle) |  |
| 3 | Scroll right using the TOP scrollbar (above headers) | Table scrolls horizontally, top scrollbar is visible and functional |  |
| 4 | Scroll down past 20+ rows | Column headers remain pinned at top of viewport |  |
| 5 | While scrolled down, scroll horizontally | Headers scroll in sync with table body |  |
| 6 | Use the column toggle dropdown | Columns show/hide correctly |  |
| 7 | Scroll right using mouse wheel (horizontal) on table body | Top scrollbar position updates in sync |  |

---

## QC #21 — Remove "Expiring Soon" Tag Globally
**Page:** Global (all views)
**Defect:** "Expiring Soon" tag/label should not appear anywhere in the application.

### Final Comment
Fixed. Removed all user-facing instances of "Expiring Soon" across the application: sort options on public, internal, external, and legacy list views; tile card tags; dashboard chart label and status table row; export statistics; and JavaScript sort label mappings. Removed from 13 files total. No user-visible reference to "Expiring Soon" remains.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view (no login) | Page loads |  |
| 2 | Open Sort By dropdown | No "Expiring Soon" option listed |  |
| 3 | Browse certificate tiles | No tile displays an "Expiring Soon" tag/badge |  |
| 4 | Log in as admin, go to Dashboard | Dashboard loads |  |
| 5 | Check chart labels and status summary table | No "Expiring Soon" label or row |  |
| 6 | Navigate to internal certificate tile view | No "Expiring Soon" in sort options or tile badges |  |
| 7 | Navigate to external certificate tile view | No "Expiring Soon" in sort options or tile badges |  |
| 8 | Export certificates (CSV) | No "Expiring Soon" in export data or statistics |  |

---

## QC #22 — Public View Status Filtering
**Page:** Public View
**Defect:** Expired and certain status types were showing on public database. "Active" status was in use but not a valid status.

### Final Comment
Fixed. Replaced the old status system with 8 client-defined statuses: Draft, Client Approved - Not Paid, Client Approved - Confidential, Client Approved - Paid, Hold, Expired, Republished, and Expired - No Annual Verification. The public view now displays ONLY certificates with status "Client Approved - Paid" or "Republished" where the expiration date has not passed. All other statuses are hidden from the public database. The old "Active" status has been removed. Changes applied across 25+ files. A SQL migration script (18_20260319_UpdateStatusTypes.sql) converts existing records. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view (no login) | Page loads with certificate tiles |  |
| 2 | Browse all visible certificates — check status badges | Only "Client Approved - Paid" or "Republished" statuses visible |  |
| 3 | Verify NO Draft certificates appear | None visible |  |
| 4 | Verify NO "Client Approved - Not Paid" certificates appear | None visible |  |
| 5 | Verify NO "Client Approved - Confidential" certificates appear | None visible |  |
| 6 | Verify NO Hold certificates appear | None visible |  |
| 7 | Verify NO Expired certificates appear | None visible |  |
| 8 | Verify NO "Expired - No Annual Verification" certificates appear | None visible |  |
| 9 | Check for any certificate with a past expiration date | None visible (even if status was Republished/Paid) |  |
| 10 | Verify no "Active" status appears anywhere | No "Active" label visible |  |
| 11 | Log in as admin, navigate to Create Certificate | Status dropdown shows all 8 new statuses, default is "Draft" |  |
| 12 | Check status badge colors | Green = Client Approved - Paid / Republished; Red = Expired variants; Gray = all others |  |

---

## QC #23 — Auto-Expire Certificates
**Page:** General Functionality
**Defect:** System must automatically change status to "Expired" when expiration date passes.

### Final Comment
Fixed. Implemented a daily background service (`CertificateExpirationService`) that automatically transitions any certificate whose expiration date has passed to "Expired" status. Runs every 24 hours (with 30-second startup delay). Checks ALL certificates regardless of current status (except those already "Expired" or "Expired - No Annual Verification"). Every automated change is recorded in both `CertificateStatusHistory` and `AuditHistory` tables with `ChangeSource = "Automated"` and `ChangedBy = "System"` for full audit traceability. No manual intervention required. **Note:** On IIS/Azure, enable "Always On" or use a scheduled task to ensure the service runs reliably. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as admin, create or edit a test certificate | Set expiration date to yesterday, status to "Client Approved - Paid" |  |
| 2 | Wait for expiration service to run (or restart application) | Service runs within 30 seconds of startup |  |
| 3 | Navigate to the test certificate | Status should now be "Expired" |  |
| 4 | Check Certificate Status History for that certificate | Entry shows: Status changed to "Expired", ChangeSource = "Automated", ChangedBy = "System" |  |
| 5 | Check Audit History (Admin > Audit Logs if available) | Entry shows: Action = "STATUS_CHANGE", ChangeSource = "Automated", ChangedByUsername = "System" |  |
| 6 | Verify the expired cert no longer appears on public view | Not visible on public database |  |
| 7 | Create a cert with status "Draft" and past expiration date | After service runs, status changes to "Expired" |  |
| 8 | Create a cert already set to "Expired - No Annual Verification" with past date | Status should NOT change (already in an expired state) |  |

---

## QC #24 — Remove Filter/Sort Popup Notifications
**Page:** Public View
**Defect:** Unnecessary popup toast notifications appeared when filtering or sorting.

### Final Comment
Fixed. Removed the "All filters cleared" and "Sorted by..." info toast notifications from both the public (`certificate-tile-view.js`) and internal (`certificate-tile-view-internal.js`) tile views. Filtering and sorting still function correctly — they just no longer trigger popup notifications. The active sort/filter state is visible in the sort dropdown and filter pills at the top of the page.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view (no login) | Page loads |  |
| 2 | Select any Sort By option (e.g., Company A-Z) | Sort applied — NO toast/popup notification appears |  |
| 3 | Change to a different sort option | Sort changes — NO popup |  |
| 4 | Apply a filter (e.g., select a certification type) | Filter applied, pills shown — NO popup |  |
| 5 | Clear all filters | Filters cleared — NO popup |  |
| 6 | Log in as analyst, go to internal tile view | Repeat steps 2-5 — NO popups on sort or filter actions |  |

---

## QC #25 — Remove/Reorganize Sort By Options (Public View)
**Page:** Public View
**Defect:** Sort dropdown had too many non-functional or unnecessary options.

### Final Comment
Fixed. Removed 8 sort options from the public view: Newest/Oldest Effective Date, Longest Validity Period, Recently Issued, Most Popular Companies, Relevance Score, and Status. Removed the "By Date" and "Advanced" group headers entirely. Moved "Product Type A-Z" into the Alphabetical section. Remaining 13 sort options organized into 3 groups: By Certificate (4), Alphabetical (5 — now includes Product Type A-Z), and By Location (4). Default sort changed to Certificate Number (Newest). Backend sort mappings corrected to match frontend values. Added missing sort cases for certification type, location, country, and product type.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view, click Sort By dropdown | Dropdown opens |  |
| 2 | Verify 3 group headers only: "By Certificate", "Alphabetical", "By Location" | No "By Date" or "Advanced" groups |  |
| 3 | Count By Certificate options | 4: Cert Number (Newest), Cert Number (Oldest), Cert Type A-Z, Cert Type Z-A |  |
| 4 | Count Alphabetical options | 5: Company A-Z, Company Z-A, Product Name A-Z, Product Name Z-A, Product Type A-Z |  |
| 5 | Count By Location options | 4: Location A-Z, Location Z-A, Country A-Z, Country Z-A |  |
| 6 | Verify total = 13 options | Exactly 13 sort options visible |  |
| 7 | Verify NO "Newest Effective Date" option | Not present |  |
| 8 | Verify NO "Relevance Score" or "Status" options | Not present |  |
| 9 | Default sort on page load | Should be "Certificate Number (Newest)" |  |
| 10 | Select "Product Type A-Z" | Certificates reorder by product type alphabetically |  |

---

## QC #26 — Sort By Options Not Working
**Page:** Public View
**Defect:** None of the Sort By options were actually changing the display order.

### Final Comment
Fixed. Root cause: the `sort` parameter had a hardcoded default value that was never null, so the JavaScript-supplied `sortBy` value was always ignored (the expression `sort ?? sortBy` always used `sort`'s default). Changed `sort` default to null and flipped the priority to `sortBy ?? sort ?? "cert-number-newest"` so the user's selected sort value takes precedence. All 13 remaining sort options now correctly change the certificate display order. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view | Default sort = Certificate Number (Newest) |  |
| 2 | Select "Company A-Z" | Certificates reorder alphabetically by company name (A first) |  |
| 3 | Select "Company Z-A" | Order reverses (Z first) |  |
| 4 | Select "Cert Type A-Z" | Certificates reorder by certification type alphabetically |  |
| 5 | Select "Product Name A-Z" | Certificates reorder by product name alphabetically |  |
| 6 | Select "Location A-Z" | Certificates reorder by location alphabetically |  |
| 7 | Select "Country A-Z" | Certificates reorder by country alphabetically |  |
| 8 | Select "Cert Number (Oldest)" | Oldest certificate numbers appear first |  |
| 9 | Try at least 3 more sort options | Each one changes the display order correctly |  |
| 10 | Refresh the page after sorting | Default sort restores (unless URL has sort param) |  |

---

## QC #27 — Mobile/Tablet Filters & Menu Not Working
**Page:** Public View — Tablet/Mobile
**Defect:** Filter sidebar and hamburger menu non-functional at tablet/mobile screen sizes.

### Final Comment
Fixed. The mobile filter toggle button existed in HTML but was hardcoded to `display: none` with no CSS or JS to make it functional. At screen widths ≤1024px, the sidebar was a fixed 350px block with no collapse mechanism. Changes: (1) Removed inline `display: none` from toggle button and added JS click handler. (2) Added responsive CSS: at ≤1024px, sidebar goes full-width, filter content hidden by default, toggle button shown. (3) Toggle switches between "Show Filters & Search" / "Hide Filters & Search" with chevron animation. At ≤768px, tighter padding applied for smaller screens. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view in browser, resize to ≤1024px (or use DevTools responsive mode) | Layout changes to mobile/tablet view |  |
| 2 | Verify "Show Filters & Search" button is visible | Blue toggle button displayed |  |
| 3 | Verify filter sidebar is HIDDEN by default | Filters not visible on load |  |
| 4 | Click "Show Filters & Search" | Filter sidebar slides open, button text changes to "Hide Filters & Search", chevron rotates |  |
| 5 | Verify search bar and all filter panels are accessible | Search input, certification type, status, tags, etc. all visible |  |
| 6 | Apply a filter while in mobile view | Filter works, results update |  |
| 7 | Click "Hide Filters & Search" | Sidebar collapses, button text reverts |  |
| 8 | Resize to ≤768px | Tighter padding, elements stack properly, no horizontal overflow |  |
| 9 | Test on actual mobile device (if available) | Same behavior — toggle works, filters accessible |  |
| 10 | Resize back to >1024px (desktop) | Sidebar returns to normal desktop layout, toggle button hidden |  |

---

## QC #28 — Sign In Extra Click (Dropdown)
**Page:** Public View
**Defect:** "Sign In" showed a dropdown menu requiring a second click instead of going directly to login.

### Final Comment
Fixed. When not authenticated, "Sign In" is now a direct link to the login page — one click, no dropdown. When authenticated, the dropdown with 2FA Settings and Logout still functions as before. Files changed: `_Layout.cshtml` (split user-controls into authenticated/unauthenticated branches), `main.css` (added sign-in link styling).

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view (not logged in) | Page loads, "Sign In" visible in header |  |
| 2 | Click "Sign In" | Navigates directly to login page — NO dropdown appears |  |
| 3 | Log in as admin | Dashboard loads |  |
| 4 | Click username/avatar in header | Dropdown appears with 2FA Settings and Logout |  |
| 5 | Log out, return to public view | "Sign In" is direct link again |  |

---

## QC #29 — Not All Certs Showing / Pagination
**Page:** Public View
**Defect:** Not all certificates visible; appeared limited to first batch.

### Final Comment
Working As Designed. The public view uses infinite scroll — loads 20 certificates at a time as the user scrolls down. All certificates load progressively. The appearance of missing certs was due to limited test data. No code changes needed.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view | First 20 certificates load |  |
| 2 | Scroll to bottom of page | Additional 20 certificates load automatically |  |
| 3 | Continue scrolling | More batches load until all certificates displayed |  |
| 4 | Verify total count matches database | All public-eligible certs eventually appear |  |

---

## QC #30 — Company Name Wrapping on Mobile
**Page:** Public View — Tablet/Mobile
**Defect:** Company name not wrapping correctly on mobile tile cards.

### Final Comment
Fixed. Two CSS issues: (1) `max-width: 120px` was globally applied to `.company-name` at 1024px breakpoint — meant for table view only but also clamped tile card names. Scoped it to `.certificates-table .company-name`. (2) Added `word-wrap: break-word` and `overflow-wrap: break-word` to tile card `.company-name` so long names wrap properly.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view, resize to mobile (≤768px) | Mobile layout active |  |
| 2 | Find a tile with a long company name | Company name wraps within card boundaries — no overflow or clipping |  |
| 3 | Check at tablet size (≤1024px) | Same — company name wraps correctly |  |
| 4 | Check desktop size (>1024px) | Company name displays normally |  |

---

## QC #31 — Download Button Not Aligned with Cert Image
**Page:** Public View — Cert Detail
**Defect:** Download button was offset from the certificate image on detail page.

### Final Comment
Fixed. The download button was right-aligned (`justify-content: flex-end`) within a 400px column while the image box was 350px wide, causing the button to sit outside the image area. Changed to `justify-content: center` with `max-width: 350px` to center the button directly under the certificate image.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open any certificate detail page | Detail page loads with cert image |  |
| 2 | Check download button position | Button is centered directly under the certificate image |  |
| 3 | Test with different cert images (wide, tall) | Button stays centered under the image in all cases |  |

---

## QC #32 — Product Categories Filter Hierarchy Wrong
**Page:** Public View + Admin
**Defect:** Packaging/Paper/Plastic appearing under Chemicals and Raw Materials instead of its own section.
**Status: OPEN — Needs Clarification**

### Final Comment
Awaiting clarification from client. Database hierarchy appears correct (Paper & Plastic are under Packaging, not Chemicals). Follow-up questions sent: Which view shows the issue? Is it a visual grouping problem or actual wrong parent? Need screenshot to confirm.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view, expand Product Categories filter | Check hierarchy — Packaging should be its own top-level section |  |
| 2 | Verify Paper and Plastic are under Packaging | Not under Chemicals and Raw Materials |  |
| 3 | Check Create Certificate form — Product Category dropdown | Same correct hierarchy |  |
| 4 | Check Admin > Product Types | Packaging is a separate root category |  |

---

## QC #33 — APR/LEED Additional Filters Not Working
**Page:** Public View
**Defect:** Selecting APR/LEED under Additional Filters did nothing.

### Final Comment
Fixed. The Additional Filters were completely non-functional — missing from 6 integration points: `filterMap` (checkbox changes ignored), `state.filters` (no storage), `buildApiParams` (never sent to API), URL state, clear filters, and active filter tags. All 6 now wired up. The API controller already handled the `additional` parameter correctly — it just never received it from the frontend.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view | Page loads |  |
| 2 | Expand "Additional Filters" section | APR and LEED checkboxes visible |  |
| 3 | Check APR checkbox | Filter pill appears, certificates filter to APR-tagged certs only |  |
| 4 | Uncheck APR, check LEED | Filter updates to LEED-tagged certs |  |
| 5 | Check both APR and LEED | Both filter pills appear, results show certs with either tag |  |
| 6 | Click "Clear All Filters" | APR/LEED checkboxes uncheck, all certs show |  |
| 7 | Apply APR filter, copy URL, paste in new tab | New tab shows APR-filtered results (shareable URL works) |  |

---

## QC #34 — Character Limits & Tile Height
**Page:** General
**Defect:** Needed clarity on character limits and tile card display truncation.

### Final Comment
Informational — no code changes needed. Database limits: Company Name 255 chars, Product Name 255 chars, Certification Type 255 chars, Product Category 255 chars. Tile card display: Product Name truncates at 5 lines (~90-100 visible chars) with ellipsis; Product Type truncates at 3 lines with ellipsis; Company Name wraps freely with no truncation. Reducing Product Name from 5 to 3 lines would cut ~70px per card if desired.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Find a cert tile with a long product name | Product name truncates with "..." after ~5 lines |  |
| 2 | Find a cert tile with a long product type | Product type truncates with "..." after ~3 lines |  |
| 3 | Find a cert tile with a long company name | Company name wraps fully (no truncation) |  |

---

## QC #35 — Remove Default/Test PDF from Public View
**Page:** Public View
**Defect:** A test PDF was showing as if it were a real certificate.

### Final Comment
Test Data Only — no code changes needed. The "default page" showing was an uploaded test PDF from development, not a system-generated default. When no PDF exists, the system shows a generic "Certificate Unavailable" icon. This will be resolved through data cleanup before go-live. All production certificates will have their actual PDF uploaded by analysts.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open a cert detail page that has a PDF | Certificate PDF/image displays correctly |  |
| 2 | If a cert has no PDF, check placeholder | Shows generic "Certificate Unavailable" icon (not test data) |  |

---

## QC #36 — Shareable URL Not Working
**Page:** Public View
**Defect:** URL parameters like `?search=ASSA+ABLOY` were ignored on page load.

### Final Comment
Fixed. URL parameters were parsed AFTER the initial certificate load had already fired with empty filters, so the search term was ignored. Moved `setupUrlParameters()` to run first in the initialization sequence so URL filters are applied before certificates load. Shareable URLs now work correctly (e.g., `gccwebapp.com/?search=ASSA+ABLOY`). **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open `gccwebapp.com/?search=ASSA+ABLOY` in a new browser tab | Page loads with "ASSA ABLOY" in search bar, only matching certs shown |  |
| 2 | Apply a filter, copy the URL from browser address bar | URL contains filter parameters |  |
| 3 | Paste that URL in a new incognito/private window | Same filtered results appear |  |
| 4 | Try with sort parameter in URL | Sort is applied on page load |  |

---

## QC #37 — Tags Too Close to Bottom of Tile
**Page:** Public View — Mobile
**Defect:** Tags on tile cards pressed against the action bar below.

### Final Comment
Fixed. Added 8px bottom margin to `.certificate-tags` inside tile cards. Tags had `margin-top: 12px` but no bottom spacing, causing them to press against the download/view icons at the card bottom.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open public view, find a cert tile with tags | Tags visible on card |  |
| 2 | Check spacing between tags and bottom action bar | Visible gap/breathing room between tags and icons below |  |
| 3 | Check on mobile view | Same spacing maintained |  |

---

## QC #38 — Company Email/URL Placeholder Text
**Page:** Public View / General
**Defect:** "Available upon request" and "Not provided" shown instead of dash when no email/URL.

### Final Comment
Fixed. Changed placeholders from "Available upon request" (email) and "Not provided" (URL) to display a dash (—) when no value exists, consistent with how Product Type handles blank values. Updated across all 4 detail views: CertificateSingleView, Details, CertificateInternalView, CertificateExternalView.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open a cert detail page where company has no email | Shows "—" (dash), not "Available upon request" |  |
| 2 | Check company URL on same cert | Shows "—" (dash), not "Not provided" |  |
| 3 | Open a cert where company HAS email and URL | Shows actual email and URL (not dash) |  |
| 4 | Check all 4 detail views (public, internal, external, legacy) | Consistent dash display across all views |  |

---

## QC #39 — "General Product" Text Showing
**Page:** Public View / General
**Defect:** "General Product" appeared when no Product Type was assigned, causing filtering confusion.

### Final Comment
Fixed. Removed hardcoded "General Product" fallback text. Now displays a dash (—) when no Product Type is assigned. This was a code-level default, not a real dropdown option. Updated across all 4 detail views.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Find a cert with no Product Type assigned | Shows "—" (dash), not "General Product" |  |
| 2 | Search/filter for "General Product" | No results — term does not appear anywhere |  |
| 3 | Check all 4 detail views | Consistent dash display |  |

---

## QC #40 — GCC Logo Not Linking to Homepage
**Page:** All Pages
**Defect:** GCC logo in header didn't link back to the certificate homepage, especially from new-tab detail pages.

### Final Comment
Fixed. Wrapped the GCC logo and text in the header with a link to "/" so clicking the logo from any page navigates back to the certificate homepage. Works for all page types and all login roles, including cert detail pages opened in new tabs. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open any page, click the GCC logo | Navigates to homepage |  |
| 2 | Open a cert detail in a new tab, click logo | Returns to homepage in that tab |  |
| 3 | Log in as admin, navigate to any admin page, click logo | Returns to homepage |  |
| 4 | Test as external user | Same behavior |  |

---

## QC #41 — Login Page Text & Username Reference
**Page:** Sign In
**Defect:** Needed specific login page text and removal of "username" references.

### Final Comment
Fixed. Replaced "Access your sustainability certification database" with "Login is restricted — public sign-up is not available." Removed all username references: label changed to "Email" only, placeholder changed to "Enter your email", helper text removed.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to login page | Page text says "Login is restricted — public sign-up is not available." |  |
| 2 | Check field label | Says "Email" (no mention of username) |  |
| 3 | Check input placeholder | Says "Enter your email" |  |
| 4 | Verify no "username" text anywhere on page | None present |  |

---

## QC #42 — No Error Message on Failed Login
**Page:** Sign In
**Defect:** No visible error when entering wrong credentials.

### Final Comment
Fixed. The controller was setting a ModelState error ("Invalid email or password") on failed login, but the view had no element to render it. Added validation summary display to the login page. Error now shows in red alert style. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to login page | Page loads |  |
| 2 | Enter invalid email and password, click Sign In | Red error message: "Invalid email or password" |  |
| 3 | Enter valid email, wrong password | Same error message |  |
| 4 | Enter valid credentials | Logs in successfully |  |

---

## QC #43 — No Return Link on Sign In Page
**Page:** Sign In
**Defect:** No way to get back to the public homepage from the login page.

### Final Comment
Fixed. Added "Return to GreenCircle database >" link below the Forgot Password link on the sign-in page, linking back to the public homepage.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to login page | "Return to GreenCircle database >" link visible below Forgot Password |  |
| 2 | Click the link | Navigates to public homepage |  |

---

## QC #44 — Remember Me Not Working
**Page:** Sign In
**Defect:** "Remember Me" checkbox didn't keep users logged in.

### Final Comment
Fixed. Root cause: auth cookie was hardcoded to 30-minute expiration regardless of "Remember Me" state. While `IsPersistent = true` made the cookie survive browser closure, it still expired after 30 minutes. Changed to 14-day expiration when Remember Me is checked, 30 minutes when unchecked. File: `AccountController.cs`.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in with "Remember Me" checked | Login successful |  |
| 2 | Close browser completely | Browser closed |  |
| 3 | Reopen browser, navigate to site | Still logged in (no re-login required) |  |
| 4 | Wait 30+ minutes, refresh page | Still logged in (14-day session) |  |
| 5 | Log in WITHOUT "Remember Me" | Login successful |  |
| 6 | Close and reopen browser | Should need to log in again |  |

---

## QC #45 — Missing Cert Types in Dropdown
**Page:** Create Certificate
**Defect:** Several certification types missing, two needed removal, one needed renaming.

### Final Comment
Fixed via SQL migration script (`19_20260320_UpdateCertificationTypes.sql`). Added 4 parent types: Carbon Footprint Reduction, Sustainable Energy Practices, Sustainable Manufacturing Practices, Product Optimization: Carbon Footprint Reduction. Deactivated 2: Renewable Resource Content, Biobased. Renamed: Manufacturer Inventory Verification to Material Ingredient Reporting. Scrollbar already present on both dropdowns. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate | Page loads |  |
| 2 | Open Certification Type dropdown | All types visible with scrollbar |  |
| 3 | Verify "Carbon Footprint Reduction" present | Listed |  |
| 4 | Verify "Sustainable Energy Practices" present | Listed |  |
| 5 | Verify "Sustainable Manufacturing Practices" present | Listed |  |
| 6 | Verify "Product Optimization: Carbon Footprint Reduction" present | Listed |  |
| 7 | Verify "Renewable Resource Content" NOT present | Not listed |  |
| 8 | Verify "Biobased" NOT present | Not listed |  |
| 9 | Verify "Material Ingredient Reporting" present (was "Manufacturer Inventory Verification") | Renamed correctly |  |

---

## QC #46 — Error When Selecting Certification Type
**Page:** Create Certificate
**Defect:** Error occurred when selecting a certification type.

### Final Comment
Fixed. Added try-catch around `certificateCustomFields.loadFieldsForType()` call so custom field loading errors don't break cert type selection. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate | Page loads |  |
| 2 | Select each Certification Type from the dropdown | No errors — type selection works for all types |  |
| 3 | Check that subcategories populate when applicable | Subcategories load without error |  |
| 4 | Check browser console | No JavaScript errors on type selection |  |

---

## QC #47 — Add Waste Diversion Event Subcategory
**Page:** Create Certificate
**Defect:** "Waste Diversion Event" needed as a subcategory under Waste Diversion from Landfill.

### Final Comment
Fixed via SQL migration script (`19_20260320_UpdateCertificationTypes.sql`). Added "Waste Diversion Event" as a new subcategory. Script is idempotent (safe to re-run).

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate | Page loads |  |
| 2 | Select "Waste Diversion from Landfill" as Certification Type | Type selected |  |
| 3 | Check Certificate Subcategories | "Waste Diversion Event" is listed as an option |  |

---

## QC #48 — Subcategories Not in Alphabetical Order
**Page:** Create Certificate
**Defect:** Certificate subcategories listed in random/database order.

### Final Comment
Fixed. Added `.sort((a, b) => a.name.localeCompare(b.name))` to subcategory filtering in both Create and Edit views (3 locations). Subcategories now populate in alphabetical order regardless of database sort order.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate, select a Certification Type with subcategories | Subcategories populate |  |
| 2 | Verify subcategories are in A-Z alphabetical order | Alphabetically sorted |  |
| 3 | Navigate to Edit Certificate, check subcategories | Also in alphabetical order |  |

---

## QC #49 — Add ISO 14001, Verified Declare Label, Verified HPD Subcategories
**Page:** Create Certificate
**Defect:** Three subcategories missing from the dropdown.

### Final Comment
Fixed via SQL migration script (`19_20260320_UpdateCertificationTypes.sql`). Added: ISO 14001 Certified, Verified Declare Label, Verified HPD as new subcategories. All sorted alphabetically.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate, select applicable Certification Type | Subcategories load |  |
| 2 | Verify "ISO 14001 Certified" present | Listed |  |
| 3 | Verify "Verified Declare Label" present | Listed |  |
| 4 | Verify "Verified HPD" present | Listed |  |

---

## QC #50 — Create Cert Mobile Layout Distorted
**Page:** Create Certificate — Mobile
**Defect:** Form layout distorted and non-functional on mobile.

### Final Comment
Not completed — low priority per Carly. Certificate creation is not expected from mobile devices. Would require CSS responsive fixes for form layout if needed in the future. **Priority: Low**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open Create Certificate on mobile/tablet | Form loads (may have layout issues — known low-priority item) |  |

---

## QC #51 — Random Characters in Names
**Page:** Global
**Defect:** Random/garbled characters appearing in some names.

### Final Comment
Data issue, not code. Likely caused by encoding issues (UTF-8 vs Windows-1252) during data import or copy-paste from Excel/PDF. No code changes needed — this is a data cleanup item. Follow-up questions sent to Carly for specific examples.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Browse certificates and companies | Check for garbled/random characters in names |  |
| 2 | If found, note which records and whether they were manually entered or CSV imported | Document for data cleanup |  |

---

## QC #52 — Products Not Tied to Company / Cascading Dropdowns
**Page:** Create Certificate
**Defect:** Product list showed all products, not filtered by company. Category/Type/Subcategory didn't cascade.

### Final Comment
Fixed across 5 files. (1) New `GetProductsByCompany` API endpoint returns products with category/type/subcategory data. (2) Create page: product dropdown starts empty ("Select a company first..."), loads company-specific products on company selection, auto-fills category/type/subcategory on product selection. (3) Edit page: same cascade, pre-populated on load. (4) "Add New Product" modal expanded from 3 to 6 fields with cascade logic, category required. (5) ProductController saves subcategory data. Autocomplete clears on company change. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate | Product dropdown says "Select a company first..." |  |
| 2 | Select a company | Product dropdown populates with ONLY that company's products |  |
| 3 | Select a product | Category, Type, and Subcategory auto-fill from product data |  |
| 4 | Change the company | Product name clears, dropdown reloads with new company's products |  |
| 5 | Click "Add New Product" | Modal shows 6 fields: Name, Company, Category, Type, Subcategory, Description |  |
| 6 | In modal: select a Category | Type dropdown filters to that category's types |  |
| 7 | Select a Type | Subcategory dropdown populates |  |
| 8 | Save new product | Product appears in dropdown, category/type/subcategory auto-fill on main form |  |
| 9 | Navigate to Edit Certificate | Product pre-selected with correct category/type/subcategory |  |

---

## QC #53 — Remove "All Categories" / "All Types" from Create/Edit
**Page:** Create Certificate
**Defect:** "All Categories" and "All Types" options caused filtering issues.

### Final Comment
Fixed. Changed placeholder text on Create and Edit certificate pages from filter-style labels ("All Categories", "All Types") to proper form prompts ("Select a category...", "Select a type..."). Product Management filter dropdowns unchanged — "All" is correct behavior for filters.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate | Category dropdown says "Select a category..." (not "All Categories") |  |
| 2 | Check Product Type dropdown | Says "Select a type..." (not "All Types") |  |
| 3 | Navigate to Edit Certificate | Same placeholder text |  |
| 4 | Check Product Management page filters | "All Categories" still present (correct for filter controls) |  |

---

## QC #54 — Update Status Types
**Page:** Create Certificate
**Defect:** Status dropdown needed the 8 client-defined statuses.

### Final Comment
Fixed. The 8 statuses are implemented in `Models/CertificateStatus.cs` and used across the entire application: Draft, Client Approved - Not Paid, Client Approved - Confidential, Client Approved - Paid, Hold, Expired, Republished, Expired - No Annual Verification. No additional changes needed.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate, check Status dropdown | All 8 statuses listed, default is "Draft" |  |
| 2 | Navigate to Edit Certificate, check Status dropdown | Same 8 statuses |  |
| 3 | Verify no old statuses ("Active", etc.) | Not present |  |

---

## QC #55 — Update Pathway Field Label
**Page:** Create Certificate
**Defect:** Pathway field label needed to say "Pathway (for Amazon CPF applicable certs)".

### Final Comment
Fixed. Updated the Pathway field label on both Create and Edit pages to "Pathway (for Amazon CPF applicable certs) (Optional)".

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate, find Pathway field | Label reads "Pathway (for Amazon CPF applicable certs) (Optional)" |  |
| 2 | Navigate to Edit Certificate | Same label |  |

---

## QC #56 — Only 5 Countries Listed
**Page:** Create Certificate
**Defect:** Country dropdown only had 5 hardcoded countries instead of a full list.
**Status: OPEN**

### Final Comment
Open. Confirmed — Create and Edit pages both hardcode only 5 countries (US, Canada, Mexico, UK, Germany). Needs full ISO country list with type-ahead/autocomplete search capability. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate, open Country dropdown | Full list of countries available (not just 5) |  |
| 2 | Type "Aus" in country field | Autocomplete shows "Australia", "Austria" |  |
| 3 | Select a country not in original 5 (e.g., "Japan") | Country saved successfully |  |

---

## QC #57 — Unknown UI Element (Image 13)
**Page:** Create Certificate
**Defect:** Unclear UI element referenced in screenshot.

### Final Comment
Fixed. Addressed as part of the country list fix — replaced limited dropdown with full ISO country list including search/autocomplete capability.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate | No confusing/unclear UI elements |  |

---

## QC #58 — Session Timeout Clears Form Data
**Page:** Create Certificate
**Defect:** Long-open form cleared on submit due to session timeout.

### Final Comment
Fixed (partially). Root cause: 30-minute session timeout caused the anti-forgery token to go stale. When submitted, the server rejected the request and redirected to login, clearing all form data. The Remember Me fix (QC #44) extends the auth cookie to 14 days, but the session idle timeout is still 30 minutes in Program.cs. If users leave the form open for extended periods, the session may still expire. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in with "Remember Me" checked | Login successful |  |
| 2 | Open Create Certificate, fill in several fields | Form populated |  |
| 3 | Wait 35+ minutes without interacting | Session may expire |  |
| 4 | Click Submit | Form should save or show clear error (not silently clear data) |  |

---

## QC #59 — Autocomplete/Type-Ahead on Dropdowns
**Page:** Create Certificate
**Defect:** Dropdowns needed type-ahead search to handle long lists.

### Final Comment
Fixed. Company dropdown has autocomplete. Country field will include type-ahead search as part of the full country list fix. Product dropdown populates based on company selection (shorter, company-specific list). **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate, start typing in Company field | Autocomplete suggestions appear |  |
| 2 | Start typing in Country field | Type-ahead filters country list |  |
| 3 | Select a company — start typing in Product field | Product options filter as you type |  |

---

## QC #60 — Cert Image Cut Off on Detail Page
**Page:** Create Certificate — Post Creation
**Defect:** Certificate image/PDF getting cut off; buttons misaligned.

### Final Comment
Fixed. Root cause: 3 detail views had inline `height: 350px; overflow: hidden` forcing a square container that clips tall certificate images. The CSS class already had proper `max-height: 450px`. Removed all inline styles from CertificateSingleView, CertificateInternalView, CertificateExternalView. Updated CSS `.certificate-preview-box img` to `max-height: 448px`. Also fixed PDF save failure caused by ModelState validation hiding field-specific errors and a product pre-select timing issue on Edit page.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open a cert detail page with a tall/portrait PDF | Full image visible — not cut off |  |
| 2 | Open a cert with a wide/landscape PDF | Image displays within bounds, not clipped |  |
| 3 | Check all 3 detail views (public, internal, external) | Image displays correctly in all |  |
| 4 | Edit a certificate, verify product is pre-selected | Correct product shown in dropdown |  |
| 5 | Submit edit form | Saves successfully, PDF not lost |  |

---

## QC #61 — Status Visible on Public Detail Page
**Page:** Create Certificate — Post Creation
**Defect:** Status should only show for logged-in users.

### Final Comment
Already correct — no fix needed. CertificateSingleView (public, anonymous) and CertificateExternalView (external users) do not show Status. Details (requires ExternalAccess auth) and CertificateInternalView (admin/analyst only) show Status.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open cert detail page while NOT logged in | No Status field visible |  |
| 2 | Log in as external user, open cert detail | No Status field visible |  |
| 3 | Log in as analyst, open cert detail | Status IS visible |  |
| 4 | Log in as admin, open cert detail | Status IS visible |  |

---

## QC #62 — Missing Company Email/URL Fields on Create/Edit
**Page:** Create Certificate
**Defect:** Company Email and URL fields not available during certificate creation but appeared on preview.

### Final Comment
Fixed across 6 files. Added CompanyEmail and CompanyWebsite fields to the Create and Edit forms. Fields auto-populate via AJAX when a company is selected. Analyst can edit them. On save, changes update the Company record so they carry over to all future certificates for that company. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate | Company Email and Company URL fields visible |  |
| 2 | Select a company with existing email/URL | Fields auto-populate with company data |  |
| 3 | Edit the email/URL, save the certificate | Changes saved to Company record |  |
| 4 | Create another cert for same company | Updated email/URL auto-populate |  |
| 5 | Navigate to Edit Certificate | Email/URL fields present and pre-populated |  |

---

## QC #63 — Dropdown Arrow Blocked by Text
**Page:** Create Certificate
**Defect:** Dropdown chevron arrow overlapped by text in select fields.

### Final Comment
Fixed. The CSS override (`padding: 10px 12px`) replaced Bootstrap's default `padding-right: 2.25rem` (36px) with just 12px, leaving no space for the chevron. Added specific `.form-select` rule to restore right padding so the arrow doesn't collide with dropdown text.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate | All dropdown fields visible |  |
| 2 | Check each dropdown (Partner, Status, Category, etc.) | Chevron arrow visible, not overlapped by text |  |
| 3 | Select a long option (e.g., "All Partners") | Text doesn't collide with arrow |  |

---

## QC #64 — Duplicate Certificates in Listing
**Page:** Analyst — Cert Listing
**Defect:** Certificate showing twice in the list view.

### Final Comment
Fixed. Root cause: when infinite scroll appended new results, the JS mapped ALL certificates (old + new) and re-inserted them, duplicating already-rendered cards. Added tracking of new certificate count per API response (`state._lastNewCount`) and only render the new slice when appending.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as analyst, navigate to cert listing | Certificates listed |  |
| 2 | Scroll through all certificates | No duplicate cert numbers |  |
| 3 | Search for a specific cert number | Only one result |  |
| 4 | Scroll to trigger infinite scroll load | No duplicates appear in new batch |  |

---

## QC #65 — No Page Numbers on Cert Listing
**Page:** Analyst — Cert Listing
**Defect:** No pagination controls — only infinite scroll with no navigation.

### Final Comment
Fixed. Added page-number pagination with Previous/Next arrows, numbered buttons (with ellipsis for large ranges), "Showing X-Y of Z" info text, and `goToPage()` for direct navigation. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as analyst, navigate to cert listing | Pagination controls visible at bottom |  |
| 2 | Check "Showing X-Y of Z" text | Accurate count |  |
| 3 | Click page 2 | Shows next page of results |  |
| 4 | Click Previous | Returns to page 1 |  |
| 5 | Click a high page number | Navigates correctly |  |

---

## QC #66 — Status Text Too Large on Internal Tiles
**Page:** Analyst — Cert Listing
**Defect:** Status badge font too large, competing with product names.

### Final Comment
Fixed. Reduced status badge, status label, and partner badge/label font from 16px to 13px on tile cards — internal view only. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as analyst, navigate to cert tile view | Status badges visible on tiles |  |
| 2 | Check status text size | Smaller than product name text, fits well |  |
| 3 | Check partner badges | Same reduced size |  |

---

## QC #67 — Dashboard Stats Inaccurate
**Page:** Dashboard
**Defect:** Company, product category, and product type counts based on all records, not active certs only.

### Final Comment
Fixed. Dashboard stat cards were counting ALL companies/products/types in the database, not just those associated with active certificates. Added filtered counts (`CompaniesWithActiveCerts`, `CertTypesWithActiveCerts`, `ProductTypesWithActiveCerts`) that count distinct values from only active certificates. "Published this year/last year" now correctly filters by effective date year.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as admin, go to Dashboard | Stat cards load |  |
| 2 | Verify "Active Certs" count | Matches actual count of active certificates |  |
| 3 | Verify "Companies" count | Only companies WITH active certs counted |  |
| 4 | Verify "Product Categories" count | Only categories WITH active certs counted |  |
| 5 | Verify "Product Types" count | Only types WITH active certs counted |  |
| 6 | Cross-check with database query | Numbers match |  |

---

## QC #68 — Dashboard Expired Count Wrong / Missing Status Breakdown
**Page:** Dashboard
**Defect:** Expired count included double-counting; status table only showed Active/Expired.

### Final Comment
Fixed. Expired stat changed to count by status only (not also by expiration date). Status breakdown table expanded from 2 rows to all 8 workflow statuses with accurate counts.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as admin, go to Dashboard | Status breakdown table visible |  |
| 2 | Verify all 8 statuses listed | Draft, Client Approved - Not Paid, Client Approved - Confidential, Client Approved - Paid, Hold, Expired, Republished, Expired - No Annual Verification |  |
| 3 | Verify Expired count | Matches certs with "Expired" status only (not double-counted) |  |
| 4 | Cross-check totals | Sum of all status counts = total certificates |  |

---

## QC #69 — "No Product Type Data Available"
**Page:** Dashboard
**Defect:** Product type chart showed no data when data existed.

### Final Comment
Fixed. `GetProductTypeStatisticsAsync` only checked `Product.ProductType` which was empty if products didn't have ProductType set. Added fallback to Certificate's direct ProductType field, then combines and deduplicates both sources.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as admin, go to Dashboard | Product type chart/data visible |  |
| 2 | Verify product types are listed with counts | Data present, not "No product type data available" |  |

---

## QC #70 — Top 5 Product Categories Showing Cert Types Instead
**Page:** Dashboard
**Defect:** "Top 5 Product Categories" was actually showing certification types.

### Final Comment
Fixed. The view was using `CertificationTypeBreakdown.Take(5)` labeled as "Product Categories." Added new `GetProductCategoryStatisticsAsync()` and `ProductCategoryBreakdown` property to show actual product categories.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as admin, go to Dashboard | "Top 5 Product Categories" section visible |  |
| 2 | Verify entries are product categories | Shows categories like "Chemicals", "Packaging", etc. — NOT cert types like "Recycled Content" |  |

---

## QC #71 — Logout Returns "This Page Isn't Working"
**Page:** Login
**Defect:** Navigating to `/Account/LogoutPost` via GET returned a 405 error page.

### Final Comment
Fixed. `/Account/LogoutPost` was POST-only. Added GET handler that redirects to GET Logout. Added `SessionExpired` endpoint for graceful session expiration redirect. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate directly to `/Account/LogoutPost` in browser | Redirects gracefully to login page (not "This page isn't working") |  |
| 2 | Log in, then click Logout normally | Logs out, redirects to login page |  |
| 3 | Let session expire, try to access a page | Redirects to login with appropriate message |  |

---

## QC #72 — Excel Date Format Not Recognized
**Page:** Data Export
**Defect:** Exported dates not recognized as date format in Excel.

### Final Comment
Fixed. Dates were written as strings via `.ToString("yyyy-MM-dd")`. Changed to write actual DateTime values with `Style.DateFormat.Format = "yyyy-MM-dd"` so Excel recognizes them as dates. Enables date sorting/filtering in Excel. Files: `ExportController.cs`, `CertificateService.cs`. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Export certificates to Excel | File downloads |  |
| 2 | Open in Excel, click a date cell | Cell type shows as "Date" (not "Text") |  |
| 3 | Sort by date column | Sorts chronologically (not alphabetically) |  |
| 4 | Use Excel date filter | Year/Month breakdown available in filter dropdown |  |

---

## QC #73 — Export Count Mismatch
**Page:** Dashboard — Data Export
**Defect:** Active certs stat showed 21 but export only had 19 rows.

### Final Comment
Fixed. Root cause: stat used `ExpirationDate >= now` (includes today) but export used `ExpirationDate > now` (excludes today). Changed all export queries to use `(ExpirationDate == null || ExpirationDate >= now)` to match the stat calculation.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as admin, note Active Certs count on dashboard | Record the number |  |
| 2 | Export Active Certs report | Download completes |  |
| 3 | Count rows in export (minus header) | Matches dashboard count exactly |  |

---

## QC #74, #75, #76 — Dashboard Reports Download HTML Code
**Page:** Dashboard — Data Export
**Defect:** Expiring Certs (3 months), Active Certs, and Expiring Certs (30 days) reports downloaded HTML code instead of data.

### Final Comment
Fixed. All three report download functions (`showExpiringCerts`, `showActiveCerts`) didn't validate response content-type before creating the download blob. If the session had expired, the server returned the HTML login page which got saved as a .csv file. Added content-type validation — if response is not CSV, shows a re-login prompt instead of downloading garbage.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as admin, go to Dashboard | Dashboard loads |  |
| 2 | Click "Download Active Certs Report" | CSV file downloads with actual certificate data |  |
| 3 | Click "Download Expiring Certs (30 Days)" | CSV with expiring cert data (or empty if none) |  |
| 4 | Click "Download Expiring Certs (3 Months)" | CSV with expiring cert data |  |
| 5 | Open each downloaded file | Contains proper CSV data, not HTML code |  |

---

## QC #77 — Export Missing Certificate Fields
**Page:** Data Export
**Defect:** Certificate export missing product type, subcategory, cert results, pathway, notes, custom fields.

### Final Comment
Fixed. Export now includes ALL certificate fields. Added 9 missing columns: Certification Subcategories, Company Contact Email, Company Contact URL, Product Type, Product Subcategory, Pathway, Certification Results, External Tags, Internal Tags. Admin custom fields are dynamically appended as additional columns. Both CSV and Excel formats updated. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Export certificates from listing page | File downloads |  |
| 2 | Verify column: Certification Subcategories | Present |  |
| 3 | Verify column: Product Type | Present |  |
| 4 | Verify column: Product Subcategory | Present |  |
| 5 | Verify column: Pathway | Present |  |
| 6 | Verify column: Certification Results | Present |  |
| 7 | Verify columns: External Tags, Internal Tags | Present |  |
| 8 | Add a custom field via Admin, fill it on a cert, re-export | Custom field appears as additional column |  |

---

## QC #78 — Template Has Sample Data
**Page:** Create Certificate
**Defect:** Certificate import template included sample data rows.

### Final Comment
Fixed. Template now downloads with headers only — no sample data rows.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Click "Download Template" on certificate import | CSV downloads |  |
| 2 | Open the file | Headers only, no sample/example data rows |  |

---

## QC #79 — Product ID Manager Search Not Working
**Page:** Create Certificate — Product ID Manager
**Defect:** Search bar and add/edit functions in Product ID Manager not working.

### Final Comment
Fixed. Two bugs: (1) `handleFormSubmit` used `new FormData(e.target)` where `e.target` was the Save button, not the form — form data was always empty, so adds/edits silently failed. Fixed to collect values directly from each input field by ID. (2) `showEditModal` called the API for temp items (negative IDs on Create page), which would fail. Added in-memory lookup for temp items.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Create Certificate, scroll to Product ID Manager | Manager visible |  |
| 2 | Add a new product ID (e.g., UPC) | ID appears in the list |  |
| 3 | Add a second product ID (e.g., ASIN) | Second ID appears |  |
| 4 | Use search bar to filter by ID value | Results filter correctly |  |
| 5 | Save the certificate | All product IDs saved |  |

---

## QC #80 — Product ID Manager Edit Freezes
**Page:** Create Certificate — Product ID Manager
**Defect:** Clicking Edit on a product ID froze/did nothing.

### Final Comment
Fixed. Same root cause as QC #79 — `handleFormSubmit` collected empty form data (used button element instead of form fields), and edit for temp items called the API with negative IDs. Both fixed. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Add a product ID in Product ID Manager | ID added |  |
| 2 | Click Edit on the added ID | Edit modal opens with pre-populated data |  |
| 3 | Change the value, click Save | Value updates in the list |  |
| 4 | Save the certificate, go to Edit page | Product IDs present |  |
| 5 | Edit a product ID on the Edit page | Edit works, saves correctly |  |

---

## QC #81 — CSV Import Scientific Notation Issue
**Page:** Create Certificate — Product ID Import
**Defect:** Long numbers in CSV converted to scientific notation (1.23E+12) by Excel, causing import failure.

### Final Comment
Fixed. Added `fixScientificNotation()` method that detects Excel's scientific notation (e.g., `1.23E+12`) and converts it back to the full number string. Applied to both Product ID value and SKU fields during CSV import parsing. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Create a CSV with long UPC numbers (12+ digits) | File ready |  |
| 2 | Open in Excel, save (Excel converts to scientific notation) | Numbers show as 1.23E+12 format |  |
| 3 | Import the CSV into Product ID Manager | All numbers import correctly as full values |  |
| 4 | Verify imported values | Full number strings, not scientific notation |  |

---

## QC #82 — List View Font Too Large
**Page:** Analyst — Cert Listing
**Defect:** Font sizes in list view too large compared to cert number/dates.

### Final Comment
Fixed. Reduced table header font from 16px to 13px, body cells from 16px to 13px, status badges from 16px to 12px. Reduced row padding and min-height for denser layout.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as analyst, navigate to cert list view | Table visible |  |
| 2 | Check font sizes | Header and body text at 13px, compact and readable |  |
| 3 | Compare to cert number and date columns | Consistent sizing across all columns |  |

---

## QC #83 — View Details Opens in Same Tab
**Page:** Analyst — Cert Listing
**Defect:** Arrow icon opened cert detail in same tab, losing list position.

### Final Comment
Fixed. View details arrow button now opens certificate in new tab (`window.open`) instead of same tab (`window.location.href`). Row click still navigates in same tab for quick access.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as analyst, navigate to cert listing | List visible |  |
| 2 | Click the arrow icon on a certificate row | Opens cert detail in NEW tab |  |
| 3 | Check original tab | Still on cert listing at same scroll position |  |
| 4 | Click the row itself (not arrow) | Opens in same tab |  |

---

## QC #84 — Move Contact Column to End of Table
**Page:** Analyst — Cert Listing
**Defect:** Company Contact Email/URL column should be last.

### Final Comment
Fixed. Moved Contact column to last position (after EAN) in header, body rows, column toggle, and column array.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in, navigate to cert listing | Table visible |  |
| 2 | Scroll to the rightmost columns | Contact column is the last column (after EAN) |  |

---

## QC #85 — Sort Arrow Spacing on Date Columns
**Page:** Analyst — Cert Listing
**Defect:** Expiration Date missing sort arrow; Effective Date arrow running into column border.

### Final Comment
Fixed. Both date columns had sort arrows — fixed arrow spacing with `flex-shrink: 0` and margins so arrows don't run into column borders.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to cert listing | Date columns visible |  |
| 2 | Check Expiration Date header | Sort arrow visible with proper spacing |  |
| 3 | Check Effective Date header | Sort arrow visible, not overlapping border |  |
| 4 | Click each date header | Sorts correctly, arrow flips direction |  |

---

## QC #86 — Horizontal Scroll UX (Mouse Wheel)
**Page:** Analyst — Cert Listing
**Defect:** Had to use scroll bar specifically — couldn't scroll horizontally with mouse wheel over table.

### Final Comment
Fixed. Added wheel event listener that converts vertical mouse scroll to horizontal scroll when cursor is over the table (similar to Wrike's table scrolling UX). **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to cert listing, hover mouse over table | Cursor over table area |  |
| 2 | Scroll mouse wheel up/down | Table scrolls horizontally (left/right) |  |
| 3 | Move cursor outside table area | Normal vertical page scroll resumes |  |

---

## QC #87 — Inconsistent Status Badge Colors
**Page:** Analyst — Cert Listing
**Defect:** Some "Active" statuses in green boxes, others without — inconsistent styling.

### Final Comment
Fixed. Added explicit CSS for all status types: `status-active` (green), `status-draft` (orange), `status-expired` (red), `status-default` (grey). All statuses now render consistently.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to cert listing | Status badges visible |  |
| 2 | Find certs with different statuses | Each status type has consistent color |  |
| 3 | Verify: approved/paid = green, draft = orange, expired = red, others = grey | Colors match |  |

---

## QC #88 — Sort Arrows Overlapping Column Names (Product Mgmt)
**Page:** Product Management
**Defect:** Sort icons too large, overlapping column header text.

### Final Comment
Fixed. Reduced sort icon size from 16px to 10px, added `white-space: nowrap` on `<th>`, reduced header padding.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Product Management | Table visible |  |
| 2 | Check all column headers | Sort arrows properly spaced, not overlapping text |  |
| 3 | Click sort arrows | Sort functions correctly |  |

---

## QC #89 — Text Wrapping in Product Management Table
**Page:** Product Management
**Defect:** Long text not wrapping properly in table cells.

### Final Comment
Fixed. Added `word-wrap: break-word` and `overflow-wrap` on `<td>` cells, `min-width` on all columns, horizontal scroll with `min-width: 1100px` on table.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Product Management | Table visible |  |
| 2 | Find rows with long product/company names | Text wraps within cell boundaries |  |
| 3 | Resize browser window | Horizontal scroll appears before content is compressed too much |  |

---

## QC #90 — Product Management Needs Pagination
**Page:** Product Management
**Defect:** Page would get very long with no way to navigate through large datasets.

### Final Comment
Fixed. Added page-number pagination at 50 rows per page with Previous/Next arrows, numbered buttons (with ellipsis), "Showing 1-50 of X products" format. Resets to page 1 on filter/search. Scrolls to table top on page change. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Product Management with 50+ products | Pagination controls visible at bottom |  |
| 2 | Verify first page shows 50 rows max | Correct row count |  |
| 3 | Click page 2 | Next 50 rows shown |  |
| 4 | Apply a search filter | Resets to page 1 with filtered results |  |
| 5 | Check "Showing X-Y of Z" text | Accurate counts |  |

---

## QC #91 — Missing Product Type & Subcategory Columns
**Page:** Product Management
**Defect:** Product Type and Product Subcategory columns not in the table.

### Final Comment
Fixed. Added `ProductSubcategoryName` to API response and view model. Added two new sortable table columns. Updated search, export, and sort logic. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Product Management | Table visible |  |
| 2 | Verify "Product Type" column present | Column visible and sortable |  |
| 3 | Verify "Subcategory" column present | Column visible and sortable |  |
| 4 | Sort by Product Type | Sorts correctly |  |
| 5 | Export products | CSV includes Product Type and Subcategory columns |  |

---

## QC #92 — Remove Status/SKU/Model from Add Product
**Page:** Product Management — Add New Product
**Defect:** Status, SKU, and Model Number fields should not be in the add product popup.

### Final Comment
Fixed. Removed Status dropdown, SKU input, and Model Number input from the Add New Product modal completely. `saveProduct` sends `isActive: true` directly; SKU and Model omitted (backend defaults to null).

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Product Management, click "Add New Product" | Modal opens |  |
| 2 | Verify NO Status dropdown | Not present |  |
| 3 | Verify NO SKU field | Not present |  |
| 4 | Verify NO Model Number field | Not present |  |
| 5 | Fill in required fields and save | Product saves successfully |  |

---

## QC #93 — Product Type/Subcategory Dropdowns Empty
**Page:** Product Management — Add New Product
**Defect:** Product Type and Subcategory dropdowns not populating.

### Final Comment
Fixed. Added cascade logic: selecting a Category filters the Product Type dropdown from preloaded data. Selecting a Product Type fetches Subcategory options via API. Changing Category resets Type and Subcategory. `productSubcategoryId` included in save payload. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Click "Add New Product" | Modal opens |  |
| 2 | Select a Category | Product Type dropdown populates with matching types |  |
| 3 | Select a Product Type | Subcategory dropdown populates |  |
| 4 | Change the Category | Type and Subcategory reset and reload |  |
| 5 | Save with all fields filled | Product saves with correct type/subcategory |  |

---

## QC #94 — Export Missing Subcategory
**Page:** Product Management — Export
**Defect:** Product export CSV missing Subcategory column.

### Final Comment
Fixed. Export CSV headers and data rows now include Product Type and Subcategory columns. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Export products from Product Management | CSV downloads |  |
| 2 | Open file, check columns | Product Type and Subcategory columns present |  |
| 3 | Verify data in those columns | Correct values for each product |  |

---

## QC #95 — Sorting Broken for Multiple Columns
**Page:** Product Management
**Defect:** Company and Category sorting broken; Certificates and Identifiers columns didn't sort at all.

### Final Comment
Fixed. Root cause: `data-sort` attribute values (e.g., `company`) didn't match JS property names (e.g., `companyName`). Added `sortMap` to translate. Added numeric sorting for Certificates (by `certificateCount`) and Identifiers (by total count). Fixed sort icon reset on column change.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Product Management | Table visible |  |
| 2 | Click Company column header | Sorts A-Z, click again Z-A |  |
| 3 | Click Category column header | Sorts A-Z / Z-A |  |
| 4 | Click Certificates column header | Sorts by count (highest first, then lowest) |  |
| 5 | Click Identifiers column header | Sorts by count |  |
| 6 | Click multiple different columns | Sort icon resets on previous column |  |

---

## QC #96 — Nav Dropdown Behind Search Bar
**Page:** Login / All Pages
**Defect:** Navigation dropdown menus rendered behind page content.

### Final Comment
Fixed. Two CSS issues: (1) `.header` had `z-index: 1000` but no `position` property — z-index is ignored on non-positioned elements. Added `position: relative`. (2) `.nav-wrapper` had `position: relative` but no z-index — dropdowns trapped in default stacking layer. Added `z-index: 1000`.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in, navigate to any page with a search bar | Page loads |  |
| 2 | Open navigation dropdown menu | Menu renders ON TOP of search bar and page content |  |
| 3 | Test on pages with different content heights | Menu always on top |  |

---

## QC #97 — Company Required on Add Product
**Page:** Product Management — Add New Product
**Defect:** Could save a product without selecting a company.

### Final Comment
Fixed. Frontend validation: `saveProduct()` checks if company is selected before submitting, shows error notification and focuses field if blank. Backend validation: server rejects null/zero `CompanyId` with clear message. Also fixed missing anti-forgery token in save request.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Click "Add New Product" | Modal opens |  |
| 2 | Fill in product name but leave Company blank, click Save | Error: "Company is required" — form does not submit |  |
| 3 | Select a company, save | Saves successfully |  |

---

## QC #98 — Delete Product Not Working
**Page:** Product Management
**Defect:** Delete button threw error; products with certificates could be deleted without warning.

### Final Comment
Fixed. Backend: if product has certificates, deletion is blocked with clear message ("Cannot delete — associated with X certificate(s). Remove associations first."). If product has 0 certificates, hard delete succeeds. Frontend: errors show as modal notifications (must be acknowledged). Removed confusing silent soft-delete behavior.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Find a product with 0 certificates, click delete | Confirmation modal appears |  |
| 2 | Confirm delete | Product deleted, success message shown |  |
| 3 | Find a product WITH certificates, click delete | Confirmation modal appears |  |
| 4 | Confirm delete | Error modal: "Cannot delete — associated with X certificate(s)" |  |
| 5 | Click OK on error modal | Modal closes, product still in list |  |

---

## QC #99 — Product ID Import Only Adding 1 of 2
**Page:** Create Certificate — Product ID Import
**Defect:** Importing 2 product IDs from Excel only added 1.

### Final Comment
Fixed. Three root causes: (1) Scientific notation — Excel converts long numbers to 1.23E+12, failing validation. Fixed with `fixScientificNotation()`. (2) Global duplicate check — `CheckDuplicateAsync` checked across ALL certificates, blocking valid IDs. Scoped to current certificate. (3) Silent errors — import failures only logged to console. Now shows per-row error messages in notification. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Create a CSV with 2+ product IDs | File ready |  |
| 2 | Import via Product ID Manager | All rows import successfully |  |
| 3 | Verify count matches CSV rows | All IDs present |  |
| 4 | If any fail, check error message | Clear per-row error details shown |  |

---

## QC #100 — "Verification URL" Label Change
**Page:** Cert Detail Page
**Defect:** Label said "Verification URL" — should say "Database Listing URL".

### Final Comment
Fixed. Changed "Verification URL" to "Database Listing URL" across all 3 detail views: Details.cshtml, CertificateInternalView.cshtml, CertificateExternalView.cshtml. The underlying URL route unchanged.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Open any cert detail page | Check URL label |  |
| 2 | Verify it says "Database Listing URL" | Not "Verification URL" |  |
| 3 | Check all 3 detail views (internal, external, legacy) | Consistent label |  |

---

## QC #101 — Remove "Search:" Prefix from Filter Tags
**Page:** Product Management
**Defect:** Filter tags wasted space with "Search:" prefix labels.

### Final Comment
Fixed. Removed the filter type prefix (`${filter.type}:`) and "Active filters:" label from filter tags. Tags now show just the filter value with an X button.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to Product Management, apply a filter | Filter tag appears |  |
| 2 | Check tag text | Shows only the value (no "Search:" prefix) |  |
| 3 | Apply multiple filters | Tags are concise, just values with X buttons |  |

---

## QC #102 — Export Contains Test/Sample Data
**Page:** Product Management — Export
**Defect:** Export included 2 rows of sample data.

### Final Comment
Fixed. The export function was clean — it exports live DB data. The sample rows ("Sample Product" / "Another Product") were in the template and got imported into the DB during testing. Template fixed (QC #103). Any already-imported test data needs manual deletion from database. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Export products | CSV downloads |  |
| 2 | Check for "Sample Product" or "Another Product" rows | Not present (if DB cleaned) |  |
| 3 | Verify all rows are real product data | No test/sample data |  |

---

## QC #103 — Template Missing Fields / Has Sample Data
**Page:** Product Management — Template
**Defect:** Template had wrong columns and included fake data rows.

### Final Comment
Fixed. Old template: Name, Company, Category, Status, SKU, Model, Description + 2 fake rows. New template: Name, Company, Description, Category, Product Type, Subcategory — headers only, no sample data. Matches the Add New Product popup fields. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Download product import template | CSV downloads |  |
| 2 | Open file — check columns | Name, Company, Description, Category, Product Type, Subcategory |  |
| 3 | Verify no sample/fake data rows | Headers only |  |
| 4 | Verify no Status, SKU, or Model columns | Not present |  |

---

## QC #104 — Breadcrumb Links Broken
**Page:** All Pages (19 views)
**Defect:** Breadcrumb links going to error pages or "#" (nowhere).

### Final Comment
Fixed across 19 views. "Administration" now links to Admin Dashboard. Category labels ("Data Management", "API Management", "System Settings", "Companies & Products") changed to non-clickable `<span>`. Current page (last breadcrumb item) changed to `<span class="current">` (not clickable).

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to any Admin page | Breadcrumbs visible |  |
| 2 | Click "Administration" breadcrumb | Goes to Admin Dashboard |  |
| 3 | Check category labels (e.g., "Data Management") | NOT clickable (no link) |  |
| 4 | Check current page breadcrumb | NOT clickable (displayed as text) |  |
| 5 | Test breadcrumbs on 3-4 different pages | Consistent behavior |  |

---

## QC #105 — Role Permission Descriptions
**Page:** Admin — Roles
**Defect:** Manage Workflow, View Audit Logs, and Manage Tags descriptions unclear/incorrect.

### Final Comment
Fixed. (1) "Manage Workflow" description updated to "Manage certificate workflow state transitions" — note: this is a placeholder permission not enforced in code. (2) "View Audit Logs" hidden from UI (no Audit Logs page exists yet) — remains in database, easy to unhide when page is built. (3) "Manage Tags" description changed to "Classification tags (internal and external)". **Note: Live database needs SQL UPDATE for description changes.**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as admin, go to Admin > Roles | Role management page loads |  |
| 2 | Check "Manage Workflow" description | "Manage certificate workflow state transitions" |  |
| 3 | Check "Manage Tags" description | "Classification tags (internal and external)" |  |
| 4 | Verify "View Audit Logs" is NOT visible | Hidden from permission list |  |

---

## QC #106 — Add User Missing Company Field
**Page:** Admin — User Management
**Defect:** Company field only appeared for External role, not for all roles.

### Final Comment
Fixed. Company field is now visible in Add/Edit User modal for all roles (except Partner, which shows Partner Assignment instead). Help text updated to "Select the companies associated with this user." **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Go to Admin > Users, click "Add New User" | Modal opens |  |
| 2 | Select Admin role | Company field visible |  |
| 3 | Select Analyst role | Company field visible |  |
| 4 | Select External role | Company field visible |  |
| 5 | Select Partner role | Partner Assignment field shown instead |  |

---

## QC #107 — "Failed to Create User" Error
**Page:** Admin — User Management
**Defect:** Generic error message with no detail on why user creation failed.

### Final Comment
Fixed. Root causes: (1) Duplicate email check was missing — emails must be unique but no pre-check existed. (2) `CreateUserAsync` swallowed all exceptions and returned generic `false`. Changed return type to `(bool, string)` tuple with actual error messages. (3) Specific errors (duplicate email, validation failures) now shown to user. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Try to create user with an existing email | Clear error: "A user with email 'x' already exists" |  |
| 2 | Try to create user with password < 8 chars | Validation error shown |  |
| 3 | Create user with valid, unique data | User created successfully |  |

---

## QC #108 — User Template Has Sample Data
**Page:** Admin — User Management
**Defect:** Template CSV included a fake "John Doe" example row.

### Final Comment
Fixed. Removed "John Doe" example row from `downloadImportTemplate()`. Template now downloads as headers-only CSV.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Download user import template | CSV downloads |  |
| 2 | Open file | Headers only, no "John Doe" or other sample data |  |

---

## QC #109 — User Import Not Working
**Page:** Admin — User Management
**Defect:** Importing users via CSV failed.

### Final Comment
Fixed. Same root cause as QC #107 (generic error messages) and #108 (sample data causing duplicates). Import errors now show actual failure reasons. Company column from CSV is now properly parsed — `resolveCompanyIds()` matches company names (case-insensitive) or numeric IDs, supporting semicolon-separated multiple companies.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Download fresh template (headers only) | Template downloads |  |
| 2 | Fill in valid user data, save CSV | File ready |  |
| 3 | Import the CSV | Users created successfully |  |
| 4 | Import a CSV with a duplicate email | Clear error message identifying the duplicate |  |

---

## QC #110 — Partner Descriptions Update + Add Wayfair
**Page:** Admin — Partner API Access
**Defect:** Novi description incorrect; needed Wayfair as new partner.

### Final Comment
Fixed. Novi description updated to "Novi Connect is an AI shopping optimization platform." Wayfair added as new partner: "Online furniture and home goods retailer focused on sustainable product offerings." Updated across all seed scripts. Migration script `20_20260324_UpdatePartnerDescriptions.sql` created for live database.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Go to Admin > Partner API Access | Partner list visible |  |
| 2 | Check Novi description | "Novi Connect is an AI shopping optimization platform" |  |
| 3 | Verify Wayfair is listed | Present with correct description |  |
| 4 | Check partner order | Amazon, Novi, Wayfair, Other |  |
| 5 | Navigate to Create Certificate, check Partner dropdown | Wayfair available as option |  |

---

## QC #111 — Certificate Type Add Popup Description Wrong
**Page:** Admin — Certificate Types
**Defect:** Blue info box had incorrect description text and examples.

### Final Comment
Fixed. Removed the incorrect blue description box. Updated examples to "Recycled Content, Certified Energy Savings, etc."

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Go to Admin > Certificate Types, click "Add Certificate Type" | Modal opens |  |
| 2 | Verify no misleading blue description box | Not present or corrected |  |
| 3 | Check examples text | "Recycled Content, Certified Energy Savings, etc." |  |

---

## QC #112 — Subcategory Add Popup Description Wrong
**Page:** Admin — Certificate Types
**Defect:** Blue info box had incorrect description text.

### Final Comment
Fixed. Removed the blue description box. Updated examples to "Attributes under multi-attribute label."

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Go to Admin > Certificate Types, click "Add Subcategory" | Modal opens |  |
| 2 | Verify no misleading blue description box | Not present or corrected |  |
| 3 | Check examples text | "Attributes under multi-attribute label" |  |

---

## QC #113 — Allow Deletion of Cert Type with Certificates
**Page:** Admin — Certificate Types
**Defect:** Could delete a certificate type even if certificates were tied to it.

### Final Comment
Fixed. Client-side check using `certificateCount` — if a type/subcategory has any certificates, deletion is blocked with a clear message. Server-side guard added as defense in depth. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Find a cert type WITH certificates, click delete | Error: cannot delete, has X certificates |  |
| 2 | Find a cert type with 0 certificates, click delete | Deletion succeeds |  |

---

## QC #114 — Help Modal Language Update
**Page:** Admin — Certificate Types
**Defect:** Help modal had incorrect examples.

### Final Comment
Fixed. Updated all examples: Type level changed to "Certified Sustainability Facts", Subcategory level changed to "Recycled Content, Closed Loop", Assignment example changed to "Recycled Content -> Certified Sustainability Facts."

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Go to Admin > Certificate Types, click Help | Help modal opens |  |
| 2 | Check type example | "Certified Sustainability Facts" |  |
| 3 | Check subcategory example | "Recycled Content, Closed Loop" |  |
| 4 | Check assignment example | "Recycled Content -> Certified Sustainability Facts" |  |

---

## QC #115 — Product Types Edit Box Description Wrong
**Page:** Admin — Product Types
**Defect:** Blue info box had inaccurate description with fake examples.

### Final Comment
Fixed. Changed info hint text and field descriptions to generic language. Removed fake examples ("Personal Care", "Skincare", "Moisturizers"). Inline examples now dynamically pulled from actual loaded hierarchy data, labeled "Existing" instead of "Examples."

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Go to Admin > Product Types, click Edit on a category | Edit modal opens |  |
| 2 | Verify no fake examples | No "Personal Care", "Skincare", etc. |  |
| 3 | Check inline examples | Show actual data from system, labeled "Existing" |  |

---

## QC #116 — Product Types Help Examples Inaccurate
**Page:** Admin — Product Types
**Defect:** Help modal used hardcoded fake product examples.

### Final Comment
Fixed. Removed all hardcoded fake examples. Hierarchy levels use generic labels (Category -> Type -> Subcategory) without specific product names. Product assignment example uses generic "Subcategory rolls up to Type rolls up to Category" flow.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Go to Admin > Product Types, click Help | Help modal opens |  |
| 2 | Verify no fake product names | No specific products like "Personal Care" |  |
| 3 | Check hierarchy explanation | Generic: Category -> Type -> Subcategory |  |

---

## QC #117 — New Product Types Defaulting to Wrong Category
**Page:** Admin — Product Types
**Defect:** New root-level product types always defaulted to "Chemicals and Raw Materials."

### Final Comment
Fixed. Root cause: `AdminService.CreateProductTypeAsync` used `ProductCategories.FirstOrDefaultAsync()` with no filter, always returning the first record (Chemicals and Raw Materials). Now creates a matching `ProductCategory` for each new root-level category. Also fixed same bug in `UpdateProductTypeAsync` for items moved to root, and added rename sync so `ProductCategory` stays in sync when a root category is renamed. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Go to Admin > Product Types | Page loads |  |
| 2 | Add a new root-level category (e.g., "Electronics") | Category created |  |
| 3 | Verify it appears as its own category | NOT under "Chemicals and Raw Materials" |  |
| 4 | Add a subcategory under the new category | Subcategory appears under correct parent |  |
| 5 | Rename the root category | ProductCategory table stays in sync |  |

---

## QC #118 — Certificate Field Sections Confusing
**Page:** Certificate Field Management
**Defect:** Section headers on Create/Edit forms were confusing and unnecessary.

### Final Comment
Fixed. Removed section headers from Create/Edit certificate forms. All custom fields now display as a flat list under "Additional Fields" — no sub-groupings. The Section column remains in the admin Certificate Field Management table for internal organization but is not rendered on certificate forms.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Add a custom field with a Section value via Admin | Field created |  |
| 2 | Navigate to Create Certificate | Custom fields listed flat under "Additional Fields" — no section sub-headers |  |
| 3 | Navigate to Edit Certificate | Same flat layout |  |
| 4 | Check Admin > Certificate Field Management | Section column still visible for internal use |  |

---

## QC #119 — Custom Fields Auto-Added to Export?
**Page:** Certificate Field Management
**Defect:** Need confirmation that new custom fields appear in export automatically.

### Final Comment
Confirmed. Yes — exports dynamically query all custom fields that have values for exported certificates and append them as additional columns after the standard columns. When an admin adds a new field and an analyst fills in a value, it automatically appears in the next export. No code changes needed. Custom fields do NOT appear in tile view tables (those show a fixed column set).

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Add a new custom field via Admin > Certificate Fields | Field created |  |
| 2 | Edit a certificate, fill in the new custom field | Value saved |  |
| 3 | Export certificates | New custom field appears as a column with the value |  |
| 4 | Check tile view table | Custom field NOT in table columns (expected) |  |

---

## QC #120 — Tags Template Has Sample Data
**Page:** Admin — Tags
**Defect:** Template export included sample data.

### Final Comment
Fixed. Tags template export now downloads a CSV with headers only — no sample data rows.

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Go to Admin > Tags, click "Download Template" | CSV downloads |  |
| 2 | Open file | Headers only, no sample data |  |

---

## QC #121 — New Partner Not in Cert Creation Dropdown
**Page:** Partners / Create Certificate
**Defect:** Partners added via Admin didn't appear in the certificate creation dropdown.

### Final Comment
Fixed. The Partner dropdown on Create and Edit certificate pages was hardcoded with only None, Amazon, Novi, and Other — never wired to the database Partners table. Now dynamically loads from the database so any partner added via Admin appears automatically. **Priority: High**

### Test Steps
| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Go to Admin > Partners, add a new partner | Partner created |  |
| 2 | Navigate to Create Certificate, open Partner dropdown | New partner appears in list |  |
| 3 | Select the new partner, save the certificate | Saves with correct partner |  |
| 4 | Navigate to Edit Certificate | Partner dropdown includes the new partner |  |

---

## Summary — All Items

| QC# | Page | Status | Priority |
|-----|------|--------|----------|
| 7 | Certificate Upload | Fixed | Medium |
| 11 | Certificates List | Fixed | High |
| 21 | Global | Fixed | Medium |
| 22 | Public View | Fixed | High |
| 23 | General Functionality | Fixed | High |
| 24 | Public View | Fixed | Low |
| 25 | Public View | Fixed | Medium |
| 26 | Public View | Fixed | High |
| 27 | Public View — Mobile | Fixed | High |
| 28 | Public View | Fixed | Medium |
| 29 | Public View | Working As Designed | High |
| 30 | Public View — Mobile | Fixed | High |
| 31 | Public View — Detail | Fixed | Medium |
| 32 | Public View + Admin | Open — Needs Clarification | High |
| 33 | Public View | Fixed | High |
| 34 | General | Informational | Medium |
| 35 | Public View | Test Data Only | High |
| 36 | Public View | Fixed | High |
| 37 | Public View — Mobile | Fixed | Low |
| 38 | Public View / General | Fixed | Medium |
| 39 | Public View / General | Fixed | Medium |
| 40 | All Pages | Fixed | High |
| 41 | Sign In | Fixed | Medium |
| 42 | Sign In | Fixed | High |
| 43 | Sign In | Fixed | Low |
| 44 | Sign In | Fixed | High |
| 45 | Create Certificate | Fixed | High |
| 46 | Create Certificate | Fixed | High |
| 47 | Create Certificate | Fixed | Medium |
| 48 | Create Certificate | Fixed | Low |
| 49 | Create Certificate | Fixed | Medium |
| 50 | Create Certificate — Mobile | Not Completed | Low |
| 51 | Global | Data Issue | Medium |
| 52 | Create Certificate | Fixed | High |
| 53 | Create Certificate | Fixed | Medium |
| 54 | Create Certificate | Fixed | Medium |
| 55 | Create Certificate | Fixed | Low |
| 56 | Create Certificate | Open | High |
| 57 | Create Certificate | Fixed | Medium |
| 58 | Create Certificate | Partial Fix | High |
| 59 | Create Certificate | Fixed | High |
| 60 | Create Certificate — Detail | Fixed | High |
| 61 | Create Certificate — Detail | No Fix Needed | Medium |
| 62 | Create Certificate | Fixed | High |
| 63 | Create Certificate | Fixed | Low |
| 64 | Analyst — Cert Listing | Fixed | High |
| 65 | Analyst — Cert Listing | Fixed | High |
| 66 | Analyst — Cert Listing | Fixed | High |
| 67 | Dashboard | Fixed | High |
| 68 | Dashboard | Fixed | High |
| 69 | Dashboard | Fixed | Medium |
| 70 | Dashboard | Fixed | High |
| 71 | Login | Fixed | High |
| 72 | Data Export | Fixed | High |
| 73 | Dashboard Export | Fixed | Medium |
| 74 | Dashboard Export | Fixed | Medium |
| 75 | Dashboard Export | Fixed | Medium |
| 76 | Dashboard Export | Fixed | Medium |
| 77 | Data Export | Fixed | High |
| 78 | Create Certificate | Fixed | Low |
| 79 | Product ID Manager | Fixed | High |
| 80 | Product ID Manager | Fixed | High |
| 81 | Product ID Import | Fixed | High |
| 82 | Analyst — Cert Listing | Fixed | Medium |
| 83 | Analyst — Cert Listing | Fixed | Medium |
| 84 | Analyst — Cert Listing | Fixed | Low |
| 85 | Analyst — Cert Listing | Fixed | Low |
| 86 | Analyst — Cert Listing | Fixed | High |
| 87 | Analyst — Cert Listing | Fixed | Medium |
| 88 | Product Management | Fixed | Medium |
| 89 | Product Management | Fixed | Low |
| 90 | Product Management | Fixed | High |
| 91 | Product Management | Fixed | High |
| 92 | Product Management | Fixed | Low |
| 93 | Product Management | Fixed | High |
| 94 | Product Management Export | Fixed | High |
| 95 | Product Management | Fixed | Medium |
| 96 | Login / All Pages | Fixed | High |
| 97 | Product Management | Fixed | Medium |
| 98 | Product Management | Fixed | High |
| 99 | Product ID Import | Fixed | High |
| 100 | Cert Detail Page | Fixed | Low |
| 101 | Product Management | Fixed | Low |
| 102 | Product Management Export | Fixed | High |
| 103 | Product Management Template | Fixed | High |
| 104 | Breadcrumbs | Fixed | Medium |
| 105 | Admin — Roles | Fixed | Medium |
| 106 | Admin — Users | Fixed | High |
| 107 | Admin — Users | Fixed | High |
| 108 | Admin — Users | Fixed | Low |
| 109 | Admin — Users | Fixed | High |
| 110 | Admin — Partners | Fixed | Medium |
| 111 | Admin — Certificate Types | Fixed | Low |
| 112 | Admin — Certificate Types | Fixed | Low |
| 113 | Admin — Certificate Types | Fixed | High |
| 114 | Admin — Certificate Types | Fixed | Low |
| 115 | Admin — Product Types | Fixed | Medium |
| 116 | Admin — Product Types | Fixed | Low |
| 117 | Admin — Product Types | Fixed | High |
| 118 | Certificate Field Mgmt | Fixed | Medium |
| 119 | Certificate Field Mgmt | Informational | Medium |
| 120 | Admin — Tags | Fixed | Low |
| 121 | Partners / Create Cert | Fixed | High |

### Status Counts
| Status | Count |
|--------|-------|
| Fixed | 100 |
| Open | 2 |
| Working As Designed | 1 |
| Not Completed (Low Priority) | 1 |
| Informational / No Fix Needed | 4 |
| Data Issue | 1 |
| Partial Fix | 1 |
| **Total** | **110** |
