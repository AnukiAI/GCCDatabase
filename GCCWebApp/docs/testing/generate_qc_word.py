"""Generate QC Final Comments & Test Steps Word document."""
import os
from docx import Document
from docx.shared import Inches, Pt, Cm, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml.ns import qn

doc = Document()

# -- Page setup --
for section in doc.sections:
    section.top_margin = Cm(1.5)
    section.bottom_margin = Cm(1.5)
    section.left_margin = Cm(1.5)
    section.right_margin = Cm(1.5)

# -- Styles --
style = doc.styles['Normal']
style.font.name = 'Calibri'
style.font.size = Pt(10)
style.paragraph_format.space_after = Pt(4)

for level in range(1, 4):
    hs = doc.styles[f'Heading {level}']
    hs.font.name = 'Calibri'
    hs.font.color.rgb = RGBColor(0x1B, 0x3A, 0x5C)

def add_table(doc, headers, rows, col_widths=None):
    table = doc.add_table(rows=1 + len(rows), cols=len(headers))
    table.style = 'Table Grid'
    table.alignment = WD_TABLE_ALIGNMENT.LEFT
    # Header row
    for i, h in enumerate(headers):
        cell = table.rows[0].cells[i]
        cell.text = h
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        run = p.runs[0]
        run.bold = True
        run.font.size = Pt(9)
        run.font.name = 'Calibri'
        # Shade header
        shading = cell._element.get_or_add_tcPr()
        shading_elm = shading.makeelement(qn('w:shd'), {
            qn('w:val'): 'clear',
            qn('w:color'): 'auto',
            qn('w:fill'): '1B3A5C',
        })
        shading.append(shading_elm)
        run.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)
    # Data rows
    for r_idx, row_data in enumerate(rows):
        for c_idx, val in enumerate(row_data):
            cell = table.rows[r_idx + 1].cells[c_idx]
            cell.text = str(val)
            for p in cell.paragraphs:
                p.alignment = WD_ALIGN_PARAGRAPH.LEFT
                for run in p.runs:
                    run.font.size = Pt(9)
                    run.font.name = 'Calibri'
    if col_widths:
        for i, w in enumerate(col_widths):
            for row in table.rows:
                row.cells[i].width = Inches(w)
    return table

# ============================================================
# TITLE PAGE
# ============================================================
doc.add_paragraph()
doc.add_paragraph()
title = doc.add_paragraph()
title.alignment = WD_ALIGN_PARAGRAPH.CENTER
run = title.add_run('QC Log\nFinal Comments & Testing Steps')
run.bold = True
run.font.size = Pt(28)
run.font.color.rgb = RGBColor(0x1B, 0x3A, 0x5C)

doc.add_paragraph()
subtitle = doc.add_paragraph()
subtitle.alignment = WD_ALIGN_PARAGRAPH.CENTER
run = subtitle.add_run('GCC Database Solution')
run.font.size = Pt(16)
run.font.color.rgb = RGBColor(0x55, 0x55, 0x55)

doc.add_paragraph()
info = doc.add_paragraph()
info.alignment = WD_ALIGN_PARAGRAPH.CENTER
run = info.add_run('Prepared by: Anuki Consulting\nDate: March 25, 2026')
run.font.size = Pt(12)
run.font.color.rgb = RGBColor(0x55, 0x55, 0x55)

doc.add_page_break()

# ============================================================
# QC ITEMS DATA
# ============================================================

items = [
    {
        "id": 7, "title": "PDF Upload Click Target", "page": "Certificate Upload", "priority": "Medium", "status": "Fixed",
        "defect": "Users had to click the text to upload - clicking the PDF icon area did nothing.",
        "comment": 'Fixed. The upload zone CSS class was mismatched (pdf-upload vs pdf-upload-zone), preventing the dashed border, pointer cursor, and hover styles from rendering. Corrected the class name in both Create and Edit views and updated the JS selector. The entire upload zone (icon + text + surrounding area) is now clickable with visible hover feedback.',
        "steps": [
            ("Navigate to Certificate > Create New", "Create certificate page loads"),
            ("Locate the PDF upload area", "Dashed border visible around entire upload zone"),
            ("Hover mouse over the PDF icon (not the text)", "Cursor changes to pointer, hover highlight appears"),
            ("Click the PDF icon area (not the text)", "File picker opens"),
            ('Cancel file picker, then click the "Upload" text', "File picker opens"),
            ("Click anywhere inside the dashed border area", "File picker opens"),
            ("Select a valid PDF file", "File uploads successfully, filename displayed"),
            ("Navigate to Certificate > Edit (existing cert)", "Edit page loads"),
            ("Repeat steps 2-7 on the Edit page", "Same behavior as Create page"),
        ]
    },
    {
        "id": 11, "title": "Certificate List Missing Details & Scroll/Freeze", "page": "Certificates List", "priority": "High", "status": "Fixed",
        "defect": "Missing product & company fields; needed top scroll bar and frozen headers.",
        "comment": "Fixed. All certificate detail columns are now present in the list view (22 columns total including company, product, category, type, subcategory, location, country, tags, partner, identifiers). A visible horizontal scrollbar is positioned above the header row for easy left-right navigation. Column headers are sticky (frozen) so they remain visible when scrolling down. Three-way scroll sync keeps the top scrollbar, header row, and table body aligned. A column toggle dropdown allows users to show/hide columns as needed.",
        "steps": [
            ("Log in as admin, navigate to Certificate List view", "List view loads with data"),
            ("Verify all columns present: cert#, type, status, dates, company, contact, product, category, type, subcategory, location, country, tags, partner, ASINs, UPC, GTIN, EAN", "All 22 columns visible (or available via column toggle)"),
            ("Scroll right using the TOP scrollbar (above headers)", "Table scrolls horizontally, top scrollbar is visible and functional"),
            ("Scroll down past 20+ rows", "Column headers remain pinned at top of viewport"),
            ("While scrolled down, scroll horizontally", "Headers scroll in sync with table body"),
            ("Use the column toggle dropdown", "Columns show/hide correctly"),
            ("Scroll right using mouse wheel (horizontal) on table body", "Top scrollbar position updates in sync"),
        ]
    },
    {
        "id": 21, "title": 'Remove "Expiring Soon" Tag Globally', "page": "Global (all views)", "priority": "Medium", "status": "Fixed",
        "defect": '"Expiring Soon" tag/label should not appear anywhere in the application.',
        "comment": 'Fixed. Removed all user-facing instances of "Expiring Soon" across the application: sort options on public, internal, external, and legacy list views; tile card tags; dashboard chart label and status table row; export statistics; and JavaScript sort label mappings. Removed from 13 files total. No user-visible reference to "Expiring Soon" remains.',
        "steps": [
            ("Open public view (no login)", "Page loads"),
            ("Open Sort By dropdown", 'No "Expiring Soon" option listed'),
            ("Browse certificate tiles", 'No tile displays an "Expiring Soon" tag/badge'),
            ("Log in as admin, go to Dashboard", "Dashboard loads"),
            ("Check chart labels and status summary table", 'No "Expiring Soon" label or row'),
            ("Navigate to internal certificate tile view", 'No "Expiring Soon" in sort options or tile badges'),
            ("Navigate to external certificate tile view", 'No "Expiring Soon" in sort options or tile badges'),
            ("Export certificates (CSV)", 'No "Expiring Soon" in export data or statistics'),
        ]
    },
    {
        "id": 22, "title": "Public View Status Filtering", "page": "Public View", "priority": "High", "status": "Fixed",
        "defect": 'Expired and certain status types were showing on public database. "Active" status was in use but not a valid status.',
        "comment": 'Fixed. Replaced the old status system with 8 client-defined statuses: Draft, Client Approved - Not Paid, Client Approved - Confidential, Client Approved - Paid, Hold, Expired, Republished, and Expired - No Annual Verification. The public view now displays ONLY certificates with status "Client Approved - Paid" or "Republished" where the expiration date has not passed. All other statuses are hidden from the public database. The old "Active" status has been removed. Changes applied across 25+ files. A SQL migration script converts existing records.',
        "steps": [
            ("Open public view (no login)", "Page loads with certificate tiles"),
            ("Browse all visible certificates", 'Only "Client Approved - Paid" or "Republished" statuses visible'),
            ("Verify NO Draft certificates appear", "None visible"),
            ('Verify NO "Client Approved - Not Paid" certificates appear', "None visible"),
            ('Verify NO "Client Approved - Confidential" certificates appear', "None visible"),
            ("Verify NO Hold certificates appear", "None visible"),
            ("Verify NO Expired certificates appear", "None visible"),
            ('Verify NO "Expired - No Annual Verification" certificates appear', "None visible"),
            ("Check for any certificate with a past expiration date", "None visible"),
            ('Verify no "Active" status appears anywhere', 'No "Active" label visible'),
            ("Log in as admin, navigate to Create Certificate", 'Status dropdown shows all 8 statuses, default is "Draft"'),
            ("Check status badge colors", "Green = approved/republished; Red = expired; Gray = others"),
        ]
    },
    {
        "id": 23, "title": "Auto-Expire Certificates", "page": "General Functionality", "priority": "High", "status": "Fixed",
        "defect": 'System must automatically change status to "Expired" when expiration date passes.',
        "comment": 'Fixed. Implemented a daily background service (CertificateExpirationService) that automatically transitions any certificate whose expiration date has passed to "Expired" status. Runs every 24 hours. Checks ALL certificates regardless of current status (except already Expired). Every automated change is recorded in both CertificateStatusHistory and AuditHistory tables with ChangeSource = "Automated" and ChangedBy = "System" for full audit traceability.',
        "steps": [
            ("Log in as admin, create or edit a test certificate", 'Set expiration date to yesterday, status to "Client Approved - Paid"'),
            ("Wait for expiration service to run (or restart application)", "Service runs within 30 seconds of startup"),
            ("Navigate to the test certificate", 'Status should now be "Expired"'),
            ("Check Certificate Status History", 'Entry shows: ChangeSource = "Automated", ChangedBy = "System"'),
            ("Check Audit History", 'Entry shows: Action = "STATUS_CHANGE", ChangeSource = "Automated"'),
            ("Verify the expired cert no longer appears on public view", "Not visible on public database"),
            ('Create a cert with status "Draft" and past expiration date', 'After service runs, status changes to "Expired"'),
            ('Create a cert already "Expired - No Annual Verification" with past date', "Status should NOT change"),
        ]
    },
    {
        "id": 24, "title": "Remove Filter/Sort Popup Notifications", "page": "Public View", "priority": "Low", "status": "Fixed",
        "defect": "Unnecessary popup toast notifications appeared when filtering or sorting.",
        "comment": 'Fixed. Removed the "All filters cleared" and "Sorted by..." info toast notifications from both the public and internal tile views. Filtering and sorting still function correctly - they just no longer trigger popup notifications.',
        "steps": [
            ("Open public view (no login)", "Page loads"),
            ("Select any Sort By option (e.g., Company A-Z)", "Sort applied - NO toast/popup notification appears"),
            ("Change to a different sort option", "Sort changes - NO popup"),
            ("Apply a filter (e.g., select a certification type)", "Filter applied, pills shown - NO popup"),
            ("Clear all filters", "Filters cleared - NO popup"),
            ("Log in as analyst, go to internal tile view", "Repeat steps 2-5 - NO popups on sort or filter actions"),
        ]
    },
    {
        "id": 25, "title": "Remove/Reorganize Sort By Options", "page": "Public View", "priority": "Medium", "status": "Fixed",
        "defect": "Sort dropdown had too many non-functional or unnecessary options.",
        "comment": "Fixed. Removed 8 sort options from public view. Removed 'By Date' and 'Advanced' group headers entirely. Moved 'Product Type A-Z' into Alphabetical section. Remaining 13 sort options in 3 groups: By Certificate (4), Alphabetical (5), By Location (4). Default sort changed to Certificate Number (Newest). Backend sort mappings corrected.",
        "steps": [
            ("Open public view, click Sort By dropdown", "Dropdown opens"),
            ('Verify 3 group headers only: "By Certificate", "Alphabetical", "By Location"', 'No "By Date" or "Advanced" groups'),
            ("Count By Certificate options", "4: Cert Number (Newest/Oldest), Cert Type A-Z/Z-A"),
            ("Count Alphabetical options", "5: Company A-Z/Z-A, Product Name A-Z/Z-A, Product Type A-Z"),
            ("Count By Location options", "4: Location A-Z/Z-A, Country A-Z/Z-A"),
            ("Verify total = 13 options", "Exactly 13 sort options visible"),
            ("Default sort on page load", 'Should be "Certificate Number (Newest)"'),
            ('Select "Product Type A-Z"', "Certificates reorder by product type alphabetically"),
        ]
    },
    {
        "id": 26, "title": "Sort By Options Not Working", "page": "Public View", "priority": "High", "status": "Fixed",
        "defect": "None of the Sort By options were actually changing the display order.",
        "comment": 'Fixed. Root cause: the sort parameter had a hardcoded default value that was never null, so the JavaScript-supplied sortBy value was always ignored. Changed sort default to null and flipped the priority so the user\'s selected sort value takes precedence. All 13 remaining sort options now correctly change the certificate display order.',
        "steps": [
            ("Open public view", "Default sort = Certificate Number (Newest)"),
            ('Select "Company A-Z"', "Certificates reorder alphabetically by company name (A first)"),
            ('Select "Company Z-A"', "Order reverses (Z first)"),
            ('Select "Cert Type A-Z"', "Certificates reorder by certification type alphabetically"),
            ('Select "Product Name A-Z"', "Certificates reorder by product name alphabetically"),
            ('Select "Location A-Z"', "Certificates reorder by location alphabetically"),
            ('Select "Country A-Z"', "Certificates reorder by country alphabetically"),
            ('Select "Cert Number (Oldest)"', "Oldest certificate numbers appear first"),
            ("Try at least 3 more sort options", "Each one changes the display order correctly"),
        ]
    },
    {
        "id": 27, "title": "Mobile/Tablet Filters & Menu Not Working", "page": "Public View - Tablet/Mobile", "priority": "High", "status": "Fixed",
        "defect": "Filter sidebar and hamburger menu non-functional at tablet/mobile screen sizes.",
        "comment": 'Fixed. The mobile filter toggle button existed in HTML but was hardcoded to display: none with no CSS or JS to make it functional. Changes: (1) Removed inline display: none from toggle button and added JS click handler. (2) Added responsive CSS: at <=1024px, sidebar goes full-width, filter content hidden by default, toggle button shown. (3) Toggle switches between "Show Filters & Search" / "Hide Filters & Search" with chevron animation.',
        "steps": [
            ("Open public view, resize to <=1024px (or use DevTools)", "Layout changes to mobile/tablet view"),
            ('Verify "Show Filters & Search" button is visible', "Blue toggle button displayed"),
            ("Verify filter sidebar is HIDDEN by default", "Filters not visible on load"),
            ('Click "Show Filters & Search"', 'Filter sidebar opens, button text changes to "Hide Filters & Search"'),
            ("Verify search bar and all filter panels are accessible", "Search input, filters all visible"),
            ("Apply a filter while in mobile view", "Filter works, results update"),
            ('Click "Hide Filters & Search"', "Sidebar collapses, button text reverts"),
            ("Resize to <=768px", "Tighter padding, elements stack properly"),
            ("Resize back to >1024px (desktop)", "Sidebar returns to normal desktop layout"),
        ]
    },
    {
        "id": 28, "title": "Sign In Extra Click (Dropdown)", "page": "Public View", "priority": "Medium", "status": "Fixed",
        "defect": '"Sign In" showed a dropdown menu requiring a second click instead of going directly to login.',
        "comment": 'Fixed. When not authenticated, "Sign In" is now a direct link to the login page - one click, no dropdown. When authenticated, the dropdown with 2FA Settings and Logout still functions as before.',
        "steps": [
            ("Open public view (not logged in)", '"Sign In" visible in header'),
            ('Click "Sign In"', "Navigates directly to login page - NO dropdown appears"),
            ("Log in as admin", "Dashboard loads"),
            ("Click username/avatar in header", "Dropdown appears with 2FA Settings and Logout"),
        ]
    },
    {
        "id": 29, "title": "Not All Certs Showing / Pagination", "page": "Public View", "priority": "High", "status": "Working As Designed",
        "defect": "Not all certificates visible; appeared limited to first batch.",
        "comment": "Working As Designed. The public view uses infinite scroll - loads 20 certificates at a time as the user scrolls down. All certificates load progressively. The appearance of missing certs was due to limited test data. No code changes needed.",
        "steps": [
            ("Open public view", "First 20 certificates load"),
            ("Scroll to bottom of page", "Additional 20 certificates load automatically"),
            ("Continue scrolling", "More batches load until all certificates displayed"),
        ]
    },
    {
        "id": 30, "title": "Company Name Wrapping on Mobile", "page": "Public View - Mobile", "priority": "High", "status": "Fixed",
        "defect": "Company name not wrapping correctly on mobile tile cards.",
        "comment": "Fixed. Two CSS issues: (1) max-width: 120px was globally applied to .company-name at 1024px breakpoint - scoped to table view only. (2) Added word-wrap: break-word to tile card .company-name so long names wrap properly.",
        "steps": [
            ("Open public view, resize to mobile (<=768px)", "Mobile layout active"),
            ("Find a tile with a long company name", "Company name wraps within card boundaries"),
            ("Check at tablet size (<=1024px)", "Same - company name wraps correctly"),
        ]
    },
    {
        "id": 31, "title": "Download Button Not Aligned with Cert Image", "page": "Public View - Detail", "priority": "Medium", "status": "Fixed",
        "defect": "Download button was offset from the certificate image on detail page.",
        "comment": "Fixed. The download button was right-aligned within a 400px column while the image box was 350px wide. Changed to justify-content: center with max-width: 350px to center the button directly under the certificate image.",
        "steps": [
            ("Open any certificate detail page", "Detail page loads with cert image"),
            ("Check download button position", "Button is centered directly under the certificate image"),
        ]
    },
    {
        "id": 32, "title": "Product Categories Filter Hierarchy Wrong", "page": "Public View + Admin", "priority": "High", "status": "Open - Needs Clarification",
        "defect": "Packaging/Paper/Plastic appearing under Chemicals and Raw Materials.",
        "comment": "Awaiting clarification from client. Database hierarchy appears correct (Paper & Plastic are under Packaging, not Chemicals). Follow-up questions sent: Which view shows the issue? Is it a visual grouping problem or actual wrong parent?",
        "steps": [
            ("Open public view, expand Product Categories filter", "Packaging should be its own top-level section"),
            ("Verify Paper and Plastic are under Packaging", "Not under Chemicals and Raw Materials"),
            ("Check Admin > Product Types", "Packaging is a separate root category"),
        ]
    },
    {
        "id": 33, "title": "APR/LEED Additional Filters Not Working", "page": "Public View", "priority": "High", "status": "Fixed",
        "defect": "Selecting APR/LEED under Additional Filters did nothing.",
        "comment": "Fixed. The Additional Filters were completely non-functional - missing from 6 integration points. All 6 now wired up: filterMap, state.filters, buildApiParams, URL state, clear filters, and active filter tags. The API controller already handled the parameter correctly - it just never received it from the frontend.",
        "steps": [
            ("Open public view", "Page loads"),
            ('Expand "Additional Filters" section', "APR and LEED checkboxes visible"),
            ("Check APR checkbox", "Filter pill appears, certificates filter to APR-tagged certs only"),
            ("Uncheck APR, check LEED", "Filter updates to LEED-tagged certs"),
            ("Check both APR and LEED", "Both filter pills appear"),
            ('Click "Clear All Filters"', "APR/LEED checkboxes uncheck, all certs show"),
            ("Apply APR filter, copy URL, paste in new tab", "New tab shows APR-filtered results"),
        ]
    },
    {
        "id": 34, "title": "Character Limits & Tile Height", "page": "General", "priority": "Medium", "status": "Informational",
        "defect": "Needed clarity on character limits and tile card display truncation.",
        "comment": "Informational - no code changes needed. Database limits: Company Name 255 chars, Product Name 255 chars. Tile card: Product Name truncates at 5 lines (~90-100 visible chars) with ellipsis; Product Type truncates at 3 lines; Company Name wraps freely. Reducing Product Name from 5 to 3 lines would cut ~70px per card if desired.",
        "steps": [
            ("Find a cert tile with a long product name", 'Product name truncates with "..." after ~5 lines'),
            ("Find a cert tile with a long product type", 'Product type truncates with "..." after ~3 lines'),
            ("Find a cert tile with a long company name", "Company name wraps fully (no truncation)"),
        ]
    },
    {
        "id": 35, "title": "Remove Default/Test PDF from Public View", "page": "Public View", "priority": "High", "status": "Test Data Only",
        "defect": "A test PDF was showing as if it were a real certificate.",
        "comment": 'Test Data Only - no code changes needed. The "default page" showing was an uploaded test PDF from development, not a system-generated default. All production certificates will have their actual PDF uploaded by analysts. Will be resolved through data cleanup before go-live.',
        "steps": [
            ("Open a cert detail page that has a PDF", "Certificate PDF/image displays correctly"),
            ("If a cert has no PDF, check placeholder", 'Shows generic "Certificate Unavailable" icon'),
        ]
    },
    {
        "id": 36, "title": "Shareable URL Not Working", "page": "Public View", "priority": "High", "status": "Fixed",
        "defect": "URL parameters like ?search=ASSA+ABLOY were ignored on page load.",
        "comment": "Fixed. URL parameters were parsed AFTER the initial certificate load had already fired with empty filters. Moved setupUrlParameters() to run first in the initialization sequence so URL filters are applied before certificates load.",
        "steps": [
            ("Open gccwebapp.com/?search=ASSA+ABLOY in a new tab", '"ASSA ABLOY" in search bar, only matching certs shown'),
            ("Apply a filter, copy the URL from browser", "URL contains filter parameters"),
            ("Paste URL in a new incognito window", "Same filtered results appear"),
        ]
    },
    {
        "id": 37, "title": "Tags Too Close to Bottom of Tile", "page": "Public View - Mobile", "priority": "Low", "status": "Fixed",
        "defect": "Tags on tile cards pressed against the action bar below.",
        "comment": "Fixed. Added 8px bottom margin to .certificate-tags inside tile cards for breathing room between tag pills and download/view icons.",
        "steps": [
            ("Open public view, find a cert tile with tags", "Tags visible on card"),
            ("Check spacing between tags and bottom action bar", "Visible gap between tags and icons below"),
        ]
    },
    {
        "id": 38, "title": "Company Email/URL Placeholder Text", "page": "Public View / General", "priority": "Medium", "status": "Fixed",
        "defect": '"Available upon request" and "Not provided" shown instead of dash when no email/URL.',
        "comment": 'Fixed. Changed placeholders to display a dash (\u2014) when no value exists. Updated across all 4 detail views.',
        "steps": [
            ("Open a cert detail page where company has no email", 'Shows "\u2014" (dash), not "Available upon request"'),
            ("Check company URL on same cert", 'Shows "\u2014" (dash), not "Not provided"'),
            ("Open a cert where company HAS email and URL", "Shows actual email and URL"),
        ]
    },
    {
        "id": 39, "title": '"General Product" Text Showing', "page": "Public View / General", "priority": "Medium", "status": "Fixed",
        "defect": '"General Product" appeared when no Product Type was assigned.',
        "comment": 'Fixed. Removed hardcoded "General Product" fallback text. Now displays a dash (\u2014) when no Product Type is assigned. Updated across all 4 detail views.',
        "steps": [
            ("Find a cert with no Product Type assigned", 'Shows "\u2014" (dash), not "General Product"'),
            ('Search/filter for "General Product"', "No results - term does not appear anywhere"),
        ]
    },
    {
        "id": 40, "title": "GCC Logo Not Linking to Homepage", "page": "All Pages", "priority": "High", "status": "Fixed",
        "defect": "GCC logo in header didn't link back to the certificate homepage.",
        "comment": 'Fixed. Wrapped the GCC logo and text in the header with a link to "/" so clicking the logo from any page navigates back to the certificate homepage. Works for all page types and all login roles.',
        "steps": [
            ("Open any page, click the GCC logo", "Navigates to homepage"),
            ("Open a cert detail in a new tab, click logo", "Returns to homepage in that tab"),
            ("Log in as admin, navigate to any admin page, click logo", "Returns to homepage"),
        ]
    },
    {
        "id": 41, "title": "Login Page Text & Username Reference", "page": "Sign In", "priority": "Medium", "status": "Fixed",
        "defect": 'Needed specific login page text and removal of "username" references.',
        "comment": 'Fixed. Replaced "Access your sustainability certification database" with "Login is restricted \u2014 public sign-up is not available." Removed all username references: label changed to "Email" only, placeholder changed to "Enter your email".',
        "steps": [
            ("Navigate to login page", 'Page text says "Login is restricted \u2014 public sign-up is not available."'),
            ("Check field label", 'Says "Email" (no mention of username)'),
            ("Check input placeholder", 'Says "Enter your email"'),
        ]
    },
    {
        "id": 42, "title": "No Error Message on Failed Login", "page": "Sign In", "priority": "High", "status": "Fixed",
        "defect": "No visible error when entering wrong credentials.",
        "comment": 'Fixed. The controller was setting a ModelState error on failed login, but the view had no element to render it. Added validation summary display. Error now shows in red alert style.',
        "steps": [
            ("Navigate to login page", "Page loads"),
            ("Enter invalid email and password, click Sign In", 'Red error message: "Invalid email or password"'),
            ("Enter valid email, wrong password", "Same error message"),
            ("Enter valid credentials", "Logs in successfully"),
        ]
    },
    {
        "id": 43, "title": "No Return Link on Sign In Page", "page": "Sign In", "priority": "Low", "status": "Fixed",
        "defect": "No way to get back to the public homepage from the login page.",
        "comment": 'Fixed. Added "Return to GreenCircle database >" link below the Forgot Password link on the sign-in page.',
        "steps": [
            ("Navigate to login page", '"Return to GreenCircle database >" link visible'),
            ("Click the link", "Navigates to public homepage"),
        ]
    },
    {
        "id": 44, "title": "Remember Me Not Working", "page": "Sign In", "priority": "High", "status": "Fixed",
        "defect": '"Remember Me" checkbox didn\'t keep users logged in.',
        "comment": "Fixed. Root cause: auth cookie was hardcoded to 30-minute expiration regardless of Remember Me state. Changed to 14-day expiration when Remember Me is checked, 30 minutes when unchecked.",
        "steps": [
            ('Log in with "Remember Me" checked', "Login successful"),
            ("Close browser completely", "Browser closed"),
            ("Reopen browser, navigate to site", "Still logged in"),
            ("Wait 30+ minutes, refresh page", "Still logged in (14-day session)"),
            ('Log in WITHOUT "Remember Me"', "Login successful"),
            ("Close and reopen browser", "Should need to log in again"),
        ]
    },
    {
        "id": 45, "title": "Missing Cert Types in Dropdown", "page": "Create Certificate", "priority": "High", "status": "Fixed",
        "defect": "Several certification types missing, two needed removal, one needed renaming.",
        "comment": "Fixed via SQL migration script. Added 4 parent types (Carbon Footprint Reduction, Sustainable Energy Practices, Sustainable Manufacturing Practices, Product Optimization). Deactivated 2 (Renewable Resource Content, Biobased). Renamed Manufacturer Inventory Verification to Material Ingredient Reporting.",
        "steps": [
            ("Navigate to Create Certificate, open Certification Type dropdown", "All types visible with scrollbar"),
            ('Verify "Carbon Footprint Reduction" present', "Listed"),
            ('Verify "Sustainable Energy Practices" present', "Listed"),
            ('Verify "Sustainable Manufacturing Practices" present', "Listed"),
            ('Verify "Renewable Resource Content" NOT present', "Not listed"),
            ('Verify "Biobased" NOT present', "Not listed"),
            ('Verify "Material Ingredient Reporting" present', "Renamed correctly"),
        ]
    },
    {
        "id": 46, "title": "Error When Selecting Certification Type", "page": "Create Certificate", "priority": "High", "status": "Fixed",
        "defect": "Error occurred when selecting a certification type.",
        "comment": "Fixed. Added try-catch around custom field loading call so errors don't break cert type selection.",
        "steps": [
            ("Navigate to Create Certificate", "Page loads"),
            ("Select each Certification Type from the dropdown", "No errors - type selection works for all types"),
            ("Check browser console", "No JavaScript errors on type selection"),
        ]
    },
    {
        "id": 47, "title": "Add Waste Diversion Event Subcategory", "page": "Create Certificate", "priority": "Medium", "status": "Fixed",
        "defect": '"Waste Diversion Event" needed as a subcategory.',
        "comment": 'Fixed via SQL migration script. Added "Waste Diversion Event" as a new subcategory. Script is idempotent (safe to re-run).',
        "steps": [
            ("Navigate to Create Certificate", "Page loads"),
            ('Select "Waste Diversion from Landfill" as Certification Type', "Type selected"),
            ("Check Certificate Subcategories", '"Waste Diversion Event" is listed'),
        ]
    },
    {
        "id": 48, "title": "Subcategories Not in Alphabetical Order", "page": "Create Certificate", "priority": "Low", "status": "Fixed",
        "defect": "Certificate subcategories listed in random/database order.",
        "comment": "Fixed. Added alphabetical sort to subcategory filtering in both Create and Edit views.",
        "steps": [
            ("Navigate to Create Certificate, select a type with subcategories", "Subcategories populate"),
            ("Verify subcategories are in A-Z alphabetical order", "Alphabetically sorted"),
            ("Navigate to Edit Certificate, check subcategories", "Also in alphabetical order"),
        ]
    },
    {
        "id": 49, "title": "Add ISO 14001, Verified Declare Label, Verified HPD", "page": "Create Certificate", "priority": "Medium", "status": "Fixed",
        "defect": "Three subcategories missing from the dropdown.",
        "comment": "Fixed via SQL migration script. Added: ISO 14001 Certified, Verified Declare Label, Verified HPD as new subcategories.",
        "steps": [
            ("Navigate to Create Certificate, select applicable type", "Subcategories load"),
            ('Verify "ISO 14001 Certified" present', "Listed"),
            ('Verify "Verified Declare Label" present', "Listed"),
            ('Verify "Verified HPD" present', "Listed"),
        ]
    },
    {
        "id": 50, "title": "Create Cert Mobile Layout Distorted", "page": "Create Certificate - Mobile", "priority": "Low", "status": "Not Completed",
        "defect": "Form layout distorted and non-functional on mobile.",
        "comment": "Not completed - low priority per Carly. Certificate creation is not expected from mobile devices.",
        "steps": [
            ("Open Create Certificate on mobile/tablet", "Form loads (may have layout issues - known low-priority item)"),
        ]
    },
    {
        "id": 51, "title": "Random Characters in Names", "page": "Global", "priority": "Medium", "status": "Data Issue",
        "defect": "Random/garbled characters appearing in some names.",
        "comment": "Data issue, not code. Likely caused by encoding issues (UTF-8 vs Windows-1252) during data import or copy-paste from Excel/PDF. No code changes needed - data cleanup item.",
        "steps": [
            ("Browse certificates and companies", "Check for garbled/random characters in names"),
            ("If found, note which records", "Document for data cleanup"),
        ]
    },
    {
        "id": 52, "title": "Products Not Tied to Company / Cascading Dropdowns", "page": "Create Certificate", "priority": "High", "status": "Fixed",
        "defect": "Product list showed all products, not filtered by company. Category/Type/Subcategory didn't cascade.",
        "comment": 'Fixed across 5 files. New GetProductsByCompany API endpoint. Create page: product dropdown starts empty ("Select a company first..."), loads company-specific products. Product selection auto-fills category/type/subcategory. "Add New Product" modal expanded from 3 to 6 fields with cascade logic.',
        "steps": [
            ("Navigate to Create Certificate", 'Product dropdown says "Select a company first..."'),
            ("Select a company", "Product dropdown populates with ONLY that company's products"),
            ("Select a product", "Category, Type, and Subcategory auto-fill"),
            ("Change the company", "Product name clears, dropdown reloads"),
            ('Click "Add New Product"', "Modal shows 6 fields with cascade logic"),
            ("In modal: select a Category", "Type dropdown filters to matching types"),
            ("Save new product", "Product appears in dropdown, fields auto-fill"),
        ]
    },
    {
        "id": 53, "title": 'Remove "All Categories" / "All Types"', "page": "Create Certificate", "priority": "Medium", "status": "Fixed",
        "defect": '"All Categories" and "All Types" options caused filtering issues.',
        "comment": 'Fixed. Changed placeholder text from filter-style labels ("All Categories", "All Types") to form prompts ("Select a category...", "Select a type..."). Product Management filter dropdowns unchanged.',
        "steps": [
            ("Navigate to Create Certificate", 'Category dropdown says "Select a category..." (not "All Categories")'),
            ("Check Product Type dropdown", 'Says "Select a type..." (not "All Types")'),
            ("Check Product Management page filters", '"All Categories" still present (correct for filters)'),
        ]
    },
    {
        "id": 54, "title": "Update Status Types", "page": "Create Certificate", "priority": "Medium", "status": "Fixed",
        "defect": "Status dropdown needed the 8 client-defined statuses.",
        "comment": "Fixed. The 8 statuses are implemented in Models/CertificateStatus.cs and used across the entire application. Default is Draft.",
        "steps": [
            ("Navigate to Create Certificate, check Status dropdown", 'All 8 statuses listed, default is "Draft"'),
            ("Navigate to Edit Certificate", "Same 8 statuses"),
        ]
    },
    {
        "id": 55, "title": "Update Pathway Field Label", "page": "Create Certificate", "priority": "Low", "status": "Fixed",
        "defect": "Pathway field label needed update.",
        "comment": 'Fixed. Updated label on both Create and Edit pages to "Pathway (for Amazon CPF applicable certs) (Optional)".',
        "steps": [
            ("Navigate to Create Certificate, find Pathway field", 'Label reads "Pathway (for Amazon CPF applicable certs) (Optional)"'),
            ("Navigate to Edit Certificate", "Same label"),
        ]
    },
    {
        "id": 56, "title": "Only 5 Countries Listed", "page": "Create Certificate", "priority": "High", "status": "Open",
        "defect": "Country dropdown only had 5 hardcoded countries.",
        "comment": "Open. Create and Edit pages both hardcode only 5 countries (US, Canada, Mexico, UK, Germany). Needs full ISO country list with type-ahead/autocomplete search capability.",
        "steps": [
            ("Navigate to Create Certificate, open Country dropdown", "Full list of countries available (not just 5)"),
            ('Type "Aus" in country field', 'Autocomplete shows "Australia", "Austria"'),
            ('Select a country not in original 5 (e.g., "Japan")', "Country saved successfully"),
        ]
    },
    {
        "id": 57, "title": "Unknown UI Element", "page": "Create Certificate", "priority": "Medium", "status": "Fixed",
        "defect": "Unclear UI element referenced in screenshot.",
        "comment": "Fixed. Addressed as part of the country list fix.",
        "steps": [
            ("Navigate to Create Certificate", "No confusing/unclear UI elements"),
        ]
    },
    {
        "id": 58, "title": "Session Timeout Clears Form Data", "page": "Create Certificate", "priority": "High", "status": "Partial Fix",
        "defect": "Long-open form cleared on submit due to session timeout.",
        "comment": "Partial fix. Root cause: 30-minute session timeout caused stale anti-forgery token. Remember Me fix extends auth cookie to 14 days, but session idle timeout is still 30 minutes. Extended form sessions may still expire.",
        "steps": [
            ('Log in with "Remember Me" checked', "Login successful"),
            ("Open Create Certificate, fill in several fields", "Form populated"),
            ("Wait 35+ minutes without interacting", "Session may expire"),
            ("Click Submit", "Form should save or show clear error (not silently clear data)"),
        ]
    },
    {
        "id": 59, "title": "Autocomplete/Type-Ahead on Dropdowns", "page": "Create Certificate", "priority": "High", "status": "Fixed",
        "defect": "Dropdowns needed type-ahead search to handle long lists.",
        "comment": "Fixed. Company dropdown has autocomplete. Country field includes type-ahead search. Product dropdown populates based on company selection (shorter list).",
        "steps": [
            ("Navigate to Create Certificate, start typing in Company field", "Autocomplete suggestions appear"),
            ("Start typing in Country field", "Type-ahead filters country list"),
            ("Select a company, start typing in Product field", "Product options filter as you type"),
        ]
    },
    {
        "id": 60, "title": "Cert Image Cut Off on Detail Page", "page": "Create Certificate - Detail", "priority": "High", "status": "Fixed",
        "defect": "Certificate image/PDF getting cut off; buttons misaligned.",
        "comment": "Fixed. 3 detail views had inline height: 350px; overflow: hidden forcing a square container that clips tall images. Removed all inline styles. Updated CSS to max-height: 448px. Also fixed PDF save failure and product pre-select timing issue on Edit page.",
        "steps": [
            ("Open a cert detail page with a tall/portrait PDF", "Full image visible - not cut off"),
            ("Open a cert with a wide/landscape PDF", "Image displays within bounds"),
            ("Check all 3 detail views", "Image displays correctly in all"),
            ("Edit a certificate, verify product is pre-selected", "Correct product shown"),
        ]
    },
    {
        "id": 61, "title": "Status Visible on Public Detail Page", "page": "Create Certificate - Detail", "priority": "Medium", "status": "No Fix Needed",
        "defect": "Status should only show for logged-in users.",
        "comment": "Already correct - no fix needed. Public and External views do not show Status. Internal/Admin views show Status as expected.",
        "steps": [
            ("Open cert detail page while NOT logged in", "No Status field visible"),
            ("Log in as external user, open cert detail", "No Status field visible"),
            ("Log in as analyst, open cert detail", "Status IS visible"),
        ]
    },
    {
        "id": 62, "title": "Missing Company Email/URL Fields on Create/Edit", "page": "Create Certificate", "priority": "High", "status": "Fixed",
        "defect": "Company Email and URL fields not available during certificate creation.",
        "comment": "Fixed across 6 files. Added CompanyEmail and CompanyWebsite fields to Create and Edit forms. Fields auto-populate via AJAX when company is selected. On save, changes update the Company record.",
        "steps": [
            ("Navigate to Create Certificate", "Company Email and Company URL fields visible"),
            ("Select a company with existing email/URL", "Fields auto-populate"),
            ("Edit the email/URL, save the certificate", "Changes saved to Company record"),
            ("Create another cert for same company", "Updated email/URL auto-populate"),
        ]
    },
    {
        "id": 63, "title": "Dropdown Arrow Blocked by Text", "page": "Create Certificate", "priority": "Low", "status": "Fixed",
        "defect": "Dropdown chevron arrow overlapped by text in select fields.",
        "comment": "Fixed. CSS override removed Bootstrap's default right padding. Added specific .form-select rule to restore right padding for the chevron.",
        "steps": [
            ("Navigate to Create Certificate", "All dropdown fields visible"),
            ("Check each dropdown", "Chevron arrow visible, not overlapped by text"),
        ]
    },
    {
        "id": 64, "title": "Duplicate Certificates in Listing", "page": "Analyst - Cert Listing", "priority": "High", "status": "Fixed",
        "defect": "Certificate showing twice in the list view.",
        "comment": "Fixed. Infinite scroll appended new results by re-mapping ALL certificates (old + new). Added tracking of new certificate count per API response and only render the new slice when appending.",
        "steps": [
            ("Log in as analyst, navigate to cert listing", "Certificates listed"),
            ("Scroll through all certificates", "No duplicate cert numbers"),
            ("Search for a specific cert number", "Only one result"),
        ]
    },
    {
        "id": 65, "title": "No Page Numbers on Cert Listing", "page": "Analyst - Cert Listing", "priority": "High", "status": "Fixed",
        "defect": "No pagination controls - only infinite scroll with no navigation.",
        "comment": 'Fixed. Added page-number pagination with Previous/Next arrows, numbered buttons with ellipsis, "Showing X-Y of Z" info text, and direct page navigation.',
        "steps": [
            ("Log in as analyst, navigate to cert listing", "Pagination controls visible at bottom"),
            ('Check "Showing X-Y of Z" text', "Accurate count"),
            ("Click page 2", "Shows next page of results"),
            ("Click Previous", "Returns to page 1"),
        ]
    },
    {
        "id": 66, "title": "Status Text Too Large on Internal Tiles", "page": "Analyst - Cert Listing", "priority": "High", "status": "Fixed",
        "defect": "Status badge font too large, competing with product names.",
        "comment": "Fixed. Reduced status badge, status label, and partner badge/label font from 16px to 13px on tile cards - internal view only.",
        "steps": [
            ("Log in as analyst, navigate to cert tile view", "Status badges visible"),
            ("Check status text size", "Smaller than product name text, fits well"),
        ]
    },
    {
        "id": 67, "title": "Dashboard Stats Inaccurate", "page": "Dashboard", "priority": "High", "status": "Fixed",
        "defect": "Company, product category, and product type counts based on all records, not active certs only.",
        "comment": "Fixed. Dashboard stat cards were counting ALL records in the database, not just those associated with active certificates. Added filtered counts that count distinct values from only active certificates.",
        "steps": [
            ("Log in as admin, go to Dashboard", "Stat cards load"),
            ('Verify "Active Certs" count', "Matches actual count of active certificates"),
            ('Verify "Companies" count', "Only companies WITH active certs counted"),
            ('Verify "Product Categories" count', "Only categories WITH active certs counted"),
            ("Cross-check with database query", "Numbers match"),
        ]
    },
    {
        "id": 68, "title": "Dashboard Expired Count Wrong", "page": "Dashboard", "priority": "High", "status": "Fixed",
        "defect": "Expired count included double-counting; status table only showed Active/Expired.",
        "comment": "Fixed. Expired stat changed to count by status only (not also by expiration date). Status breakdown table expanded to show all 8 workflow statuses with accurate counts.",
        "steps": [
            ("Log in as admin, go to Dashboard", "Status breakdown table visible"),
            ("Verify all 8 statuses listed", "All present with counts"),
            ("Verify Expired count", "Matches certs with Expired status only"),
            ("Cross-check totals", "Sum of all status counts = total certificates"),
        ]
    },
    {
        "id": 69, "title": '"No Product Type Data Available"', "page": "Dashboard", "priority": "Medium", "status": "Fixed",
        "defect": "Product type chart showed no data when data existed.",
        "comment": "Fixed. Added fallback to Certificate's direct ProductType field, then combines and deduplicates both sources.",
        "steps": [
            ("Log in as admin, go to Dashboard", "Product type chart/data visible"),
            ("Verify product types are listed with counts", 'Data present, not "No product type data available"'),
        ]
    },
    {
        "id": 70, "title": "Top 5 Product Categories Showing Cert Types", "page": "Dashboard", "priority": "High", "status": "Fixed",
        "defect": '"Top 5 Product Categories" was actually showing certification types.',
        "comment": 'Fixed. The view was using CertificationTypeBreakdown labeled as "Product Categories." Added new GetProductCategoryStatisticsAsync() to show actual product categories.',
        "steps": [
            ("Log in as admin, go to Dashboard", '"Top 5 Product Categories" section visible'),
            ("Verify entries are product categories", 'Shows categories like "Chemicals", "Packaging" - NOT cert types'),
        ]
    },
    {
        "id": 71, "title": 'Logout Returns "This Page Isn\'t Working"', "page": "Login", "priority": "High", "status": "Fixed",
        "defect": "Navigating to /Account/LogoutPost via GET returned a 405 error page.",
        "comment": "Fixed. /Account/LogoutPost was POST-only. Added GET handler that redirects to GET Logout. Added SessionExpired endpoint for graceful session expiration redirect.",
        "steps": [
            ("Navigate directly to /Account/LogoutPost in browser", 'Redirects gracefully to login page (not "This page isn\'t working")'),
            ("Log in, then click Logout normally", "Logs out, redirects to login page"),
        ]
    },
    {
        "id": 72, "title": "Excel Date Format Not Recognized", "page": "Data Export", "priority": "High", "status": "Fixed",
        "defect": "Exported dates not recognized as date format in Excel.",
        "comment": "Fixed. Dates were written as strings via .ToString(). Changed to write actual DateTime values with proper format so Excel recognizes them as dates. Enables date sorting/filtering in Excel.",
        "steps": [
            ("Export certificates to Excel", "File downloads"),
            ("Open in Excel, click a date cell", 'Cell type shows as "Date" (not "Text")'),
            ("Sort by date column", "Sorts chronologically"),
            ("Use Excel date filter", "Year/Month breakdown available"),
        ]
    },
    {
        "id": 73, "title": "Export Count Mismatch", "page": "Dashboard Export", "priority": "Medium", "status": "Fixed",
        "defect": "Active certs stat showed 21 but export only had 19 rows.",
        "comment": "Fixed. Stat used ExpirationDate >= now (includes today) but export used ExpirationDate > now (excludes today). Aligned all export queries to match stat calculation.",
        "steps": [
            ("Log in as admin, note Active Certs count on dashboard", "Record the number"),
            ("Export Active Certs report", "Download completes"),
            ("Count rows in export (minus header)", "Matches dashboard count exactly"),
        ]
    },
    {
        "id": 74, "title": "Dashboard Reports Download HTML (Expiring 3mo)", "page": "Dashboard Export", "priority": "Medium", "status": "Fixed",
        "defect": "Expiring Certs Report (3 months) downloaded HTML code instead of data.",
        "comment": "Fixed. Download functions didn't validate response content-type. If session expired, server returned HTML login page saved as .csv. Added content-type validation.",
        "steps": [
            ("Log in as admin, go to Dashboard", "Dashboard loads"),
            ('Click "Download Expiring Certs (3 Months)"', "CSV file downloads with actual data"),
            ("Open downloaded file", "Contains proper CSV data, not HTML code"),
        ]
    },
    {
        "id": 75, "title": "Dashboard Reports Download HTML (Active)", "page": "Dashboard Export", "priority": "Medium", "status": "Fixed",
        "defect": "Active Certs Report downloaded HTML code instead of data.",
        "comment": "Fixed. Same root cause as QC #74. Added content-type validation to prevent downloading HTML as CSV.",
        "steps": [
            ('Click "Download Active Certs Report"', "CSV file downloads with actual certificate data"),
            ("Open downloaded file", "Contains proper CSV data, not HTML code"),
        ]
    },
    {
        "id": 76, "title": "Dashboard Reports Download HTML (Expiring 30d)", "page": "Dashboard Export", "priority": "Medium", "status": "Fixed",
        "defect": "Expiring Certs Report (30 days) downloaded HTML code instead of data.",
        "comment": "Fixed. Same root cause as QC #74. Added content-type validation.",
        "steps": [
            ('Click "Download Expiring Certs (30 Days)"', "CSV file downloads with actual data"),
            ("Open downloaded file", "Contains proper CSV data, not HTML code"),
        ]
    },
    {
        "id": 77, "title": "Export Missing Certificate Fields", "page": "Data Export", "priority": "High", "status": "Fixed",
        "defect": "Certificate export missing product type, subcategory, cert results, pathway, notes, custom fields.",
        "comment": "Fixed. Export now includes ALL certificate fields. Added 9 missing columns: Certification Subcategories, Company Contact Email/URL, Product Type, Product Subcategory, Pathway, Certification Results, External/Internal Tags. Admin custom fields dynamically appended.",
        "steps": [
            ("Export certificates from listing page", "File downloads"),
            ("Verify column: Certification Subcategories", "Present"),
            ("Verify column: Product Type", "Present"),
            ("Verify column: Pathway", "Present"),
            ("Verify column: Certification Results", "Present"),
            ("Add a custom field, fill on a cert, re-export", "Custom field appears as additional column"),
        ]
    },
    {
        "id": 78, "title": "Template Has Sample Data", "page": "Create Certificate", "priority": "Low", "status": "Fixed",
        "defect": "Certificate import template included sample data rows.",
        "comment": "Fixed. Template now downloads with headers only - no sample data rows.",
        "steps": [
            ('Click "Download Template" on certificate import', "CSV downloads"),
            ("Open the file", "Headers only, no sample/example data rows"),
        ]
    },
    {
        "id": 79, "title": "Product ID Manager Search Not Working", "page": "Product ID Manager", "priority": "High", "status": "Fixed",
        "defect": "Search bar and add/edit functions in Product ID Manager not working.",
        "comment": "Fixed. Two bugs: (1) handleFormSubmit used button element instead of form fields - data always empty. (2) showEditModal called API for temp items with negative IDs. Both fixed.",
        "steps": [
            ("Navigate to Create Certificate, scroll to Product ID Manager", "Manager visible"),
            ("Add a new product ID (e.g., UPC)", "ID appears in the list"),
            ("Use search bar to filter by ID value", "Results filter correctly"),
            ("Save the certificate", "All product IDs saved"),
        ]
    },
    {
        "id": 80, "title": "Product ID Manager Edit Freezes", "page": "Product ID Manager", "priority": "High", "status": "Fixed",
        "defect": "Clicking Edit on a product ID froze/did nothing.",
        "comment": "Fixed. Same root cause as QC #79. handleFormSubmit collected empty form data, and edit for temp items called API with negative IDs. Both fixed.",
        "steps": [
            ("Add a product ID in Product ID Manager", "ID added"),
            ("Click Edit on the added ID", "Edit modal opens with pre-populated data"),
            ("Change the value, click Save", "Value updates in the list"),
        ]
    },
    {
        "id": 81, "title": "CSV Import Scientific Notation Issue", "page": "Product ID Import", "priority": "High", "status": "Fixed",
        "defect": "Long numbers in CSV converted to scientific notation by Excel, causing import failure.",
        "comment": "Fixed. Added fixScientificNotation() method that detects Excel's scientific notation (e.g., 1.23E+12) and converts back to full number string.",
        "steps": [
            ("Create a CSV with long UPC numbers (12+ digits)", "File ready"),
            ("Open in Excel, save (converts to scientific notation)", "Numbers show as 1.23E+12"),
            ("Import the CSV into Product ID Manager", "All numbers import correctly as full values"),
        ]
    },
    {
        "id": 82, "title": "List View Font Too Large", "page": "Analyst - Cert Listing", "priority": "Medium", "status": "Fixed",
        "defect": "Font sizes in list view too large compared to cert number/dates.",
        "comment": "Fixed. Reduced table header and body font from 16px to 13px, status badges from 16px to 12px. Reduced row padding for denser layout.",
        "steps": [
            ("Log in as analyst, navigate to cert list view", "Table visible"),
            ("Check font sizes", "Header and body text at 13px, compact and readable"),
        ]
    },
    {
        "id": 83, "title": "View Details Opens in Same Tab", "page": "Analyst - Cert Listing", "priority": "Medium", "status": "Fixed",
        "defect": "Arrow icon opened cert detail in same tab, losing list position.",
        "comment": "Fixed. Arrow button now opens certificate in new tab (window.open). Row click still navigates in same tab.",
        "steps": [
            ("Log in as analyst, navigate to cert listing", "List visible"),
            ("Click the arrow icon on a certificate row", "Opens cert detail in NEW tab"),
            ("Check original tab", "Still on cert listing at same scroll position"),
        ]
    },
    {
        "id": 84, "title": "Move Contact Column to End of Table", "page": "Analyst - Cert Listing", "priority": "Low", "status": "Fixed",
        "defect": "Company Contact Email/URL column should be last.",
        "comment": "Fixed. Moved Contact column to last position (after EAN) in header, body rows, column toggle, and column array.",
        "steps": [
            ("Log in, navigate to cert listing", "Table visible"),
            ("Scroll to the rightmost columns", "Contact column is the last column"),
        ]
    },
    {
        "id": 85, "title": "Sort Arrow Spacing on Date Columns", "page": "Analyst - Cert Listing", "priority": "Low", "status": "Fixed",
        "defect": "Expiration Date missing sort arrow; Effective Date arrow running into line.",
        "comment": "Fixed. Both date columns had sort arrows - fixed arrow spacing with flex-shrink: 0 and margins.",
        "steps": [
            ("Navigate to cert listing", "Date columns visible"),
            ("Check Expiration Date header", "Sort arrow visible with proper spacing"),
            ("Check Effective Date header", "Sort arrow visible, not overlapping border"),
        ]
    },
    {
        "id": 86, "title": "Horizontal Scroll UX (Mouse Wheel)", "page": "Analyst - Cert Listing", "priority": "High", "status": "Fixed",
        "defect": "Had to use scroll bar specifically - couldn't scroll horizontally with mouse.",
        "comment": "Fixed. Added wheel event listener that converts vertical mouse scroll to horizontal scroll when cursor is over the table (similar to Wrike).",
        "steps": [
            ("Navigate to cert listing, hover mouse over table", "Cursor over table area"),
            ("Scroll mouse wheel up/down", "Table scrolls horizontally (left/right)"),
            ("Move cursor outside table area", "Normal vertical page scroll resumes"),
        ]
    },
    {
        "id": 87, "title": "Inconsistent Status Badge Colors", "page": "Analyst - Cert Listing", "priority": "Medium", "status": "Fixed",
        "defect": 'Some "Active" statuses in green boxes, others without.',
        "comment": "Fixed. Added explicit CSS for all status types: active (green), draft (orange), expired (red), default (grey). All statuses render consistently.",
        "steps": [
            ("Navigate to cert listing", "Status badges visible"),
            ("Find certs with different statuses", "Each status type has consistent color"),
            ("Verify: approved = green, draft = orange, expired = red", "Colors match"),
        ]
    },
    {
        "id": 88, "title": "Sort Arrows Overlapping (Product Mgmt)", "page": "Product Management", "priority": "Medium", "status": "Fixed",
        "defect": "Sort icons too large, overlapping column header text.",
        "comment": "Fixed. Reduced sort icon size from 16px to 10px, added white-space: nowrap on th, reduced header padding.",
        "steps": [
            ("Navigate to Product Management", "Table visible"),
            ("Check all column headers", "Sort arrows properly spaced, not overlapping text"),
        ]
    },
    {
        "id": 89, "title": "Text Wrapping in Product Management Table", "page": "Product Management", "priority": "Low", "status": "Fixed",
        "defect": "Long text not wrapping properly in table cells.",
        "comment": "Fixed. Added word-wrap: break-word on td cells, min-width on all columns, horizontal scroll with min-width: 1100px.",
        "steps": [
            ("Navigate to Product Management", "Table visible"),
            ("Find rows with long product/company names", "Text wraps within cell boundaries"),
        ]
    },
    {
        "id": 90, "title": "Product Management Needs Pagination", "page": "Product Management", "priority": "High", "status": "Fixed",
        "defect": "Page would get very long with no way to navigate through large datasets.",
        "comment": 'Fixed. Added page-number pagination at 50 rows per page with Previous/Next arrows, numbered buttons with ellipsis, "Showing 1-50 of X products" format. Resets to page 1 on filter/search.',
        "steps": [
            ("Navigate to Product Management with 50+ products", "Pagination controls visible at bottom"),
            ("Verify first page shows 50 rows max", "Correct row count"),
            ("Click page 2", "Next 50 rows shown"),
            ("Apply a search filter", "Resets to page 1 with filtered results"),
        ]
    },
    {
        "id": 91, "title": "Missing Product Type & Subcategory Columns", "page": "Product Management", "priority": "High", "status": "Fixed",
        "defect": "Product Type and Product Subcategory columns not in the table.",
        "comment": "Fixed. Added ProductSubcategoryName to API response and view model. Added two new sortable table columns. Updated search, export, and sort logic.",
        "steps": [
            ("Navigate to Product Management", "Table visible"),
            ('Verify "Product Type" column present', "Column visible and sortable"),
            ('Verify "Subcategory" column present', "Column visible and sortable"),
            ("Export products", "CSV includes Product Type and Subcategory"),
        ]
    },
    {
        "id": 92, "title": "Remove Status/SKU/Model from Add Product", "page": "Product Management", "priority": "Low", "status": "Fixed",
        "defect": "Status, SKU, and Model Number fields should not be in add product popup.",
        "comment": "Fixed. Removed Status dropdown, SKU input, and Model Number input from the Add New Product modal completely.",
        "steps": [
            ('Navigate to Product Management, click "Add New Product"', "Modal opens"),
            ("Verify NO Status dropdown", "Not present"),
            ("Verify NO SKU field", "Not present"),
            ("Verify NO Model Number field", "Not present"),
        ]
    },
    {
        "id": 93, "title": "Product Type/Subcategory Dropdowns Empty", "page": "Product Management", "priority": "High", "status": "Fixed",
        "defect": "Product Type and Subcategory dropdowns not populating.",
        "comment": "Fixed. Added cascade logic: selecting a Category filters Product Type dropdown. Selecting a Product Type fetches Subcategory via API. Changing Category resets dependent fields.",
        "steps": [
            ('Click "Add New Product"', "Modal opens"),
            ("Select a Category", "Product Type dropdown populates with matching types"),
            ("Select a Product Type", "Subcategory dropdown populates"),
            ("Change the Category", "Type and Subcategory reset and reload"),
        ]
    },
    {
        "id": 94, "title": "Export Missing Subcategory", "page": "Product Management Export", "priority": "High", "status": "Fixed",
        "defect": "Product export CSV missing Subcategory column.",
        "comment": "Fixed. Export CSV headers and data rows now include Product Type and Subcategory columns.",
        "steps": [
            ("Export products from Product Management", "CSV downloads"),
            ("Open file, check columns", "Product Type and Subcategory columns present"),
        ]
    },
    {
        "id": 95, "title": "Sorting Broken for Multiple Columns", "page": "Product Management", "priority": "Medium", "status": "Fixed",
        "defect": "Company and Category sorting broken; Certificates and Identifiers don't sort.",
        "comment": "Fixed. data-sort attribute values didn't match JS property names. Added sortMap to translate. Added numeric sorting for Certificates and Identifiers columns. Fixed sort icon reset.",
        "steps": [
            ("Navigate to Product Management", "Table visible"),
            ("Click Company column header", "Sorts A-Z, click again Z-A"),
            ("Click Certificates column header", "Sorts by count"),
            ("Click Identifiers column header", "Sorts by count"),
        ]
    },
    {
        "id": 96, "title": "Nav Dropdown Behind Search Bar", "page": "All Pages", "priority": "High", "status": "Fixed",
        "defect": "Navigation dropdown menus rendered behind page content.",
        "comment": "Fixed. .header had z-index but no position property (z-index ignored). Added position: relative to .header and z-index: 1000 to .nav-wrapper.",
        "steps": [
            ("Log in, navigate to any page with a search bar", "Page loads"),
            ("Open navigation dropdown menu", "Menu renders ON TOP of search bar and page content"),
        ]
    },
    {
        "id": 97, "title": "Company Required on Add Product", "page": "Product Management", "priority": "Medium", "status": "Fixed",
        "defect": "Could save a product without selecting a company.",
        "comment": "Fixed. Frontend: validation checks if company is selected before submitting. Backend: rejects null/zero CompanyId. Also fixed missing anti-forgery token.",
        "steps": [
            ('Click "Add New Product"', "Modal opens"),
            ("Leave Company blank, click Save", 'Error: "Company is required"'),
            ("Select a company, save", "Saves successfully"),
        ]
    },
    {
        "id": 98, "title": "Delete Product Not Working", "page": "Product Management", "priority": "High", "status": "Fixed",
        "defect": "Delete button threw error; products with certificates could be deleted without warning.",
        "comment": 'Fixed. If product has certificates, deletion blocked with clear message. If product has 0 certificates, hard delete succeeds. Frontend: errors show as modal notifications. Removed confusing silent soft-delete.',
        "steps": [
            ("Find a product with 0 certificates, click delete", "Confirmation modal appears"),
            ("Confirm delete", "Product deleted, success message shown"),
            ("Find a product WITH certificates, click delete", "Error: cannot delete, has X certificates"),
        ]
    },
    {
        "id": 99, "title": "Product ID Import Only Adding 1 of 2", "page": "Product ID Import", "priority": "High", "status": "Fixed",
        "defect": "Importing 2 product IDs from Excel only added 1.",
        "comment": "Fixed. Three root causes: (1) Scientific notation failing validation. (2) Global duplicate check blocking valid IDs across certificates. (3) Silent errors with no per-row feedback. All three fixed.",
        "steps": [
            ("Create a CSV with 2+ product IDs", "File ready"),
            ("Import via Product ID Manager", "All rows import successfully"),
            ("Verify count matches CSV rows", "All IDs present"),
        ]
    },
    {
        "id": 100, "title": '"Verification URL" Label Change', "page": "Cert Detail Page", "priority": "Low", "status": "Fixed",
        "defect": 'Label said "Verification URL" - should say "Database Listing URL".',
        "comment": 'Fixed. Changed "Verification URL" to "Database Listing URL" across all 3 detail views.',
        "steps": [
            ("Open any cert detail page", "Check URL label"),
            ('Verify it says "Database Listing URL"', 'Not "Verification URL"'),
        ]
    },
    {
        "id": 101, "title": 'Remove "Search:" Prefix from Filter Tags', "page": "Product Management", "priority": "Low", "status": "Fixed",
        "defect": "Filter tags wasted space with prefix labels.",
        "comment": "Fixed. Removed filter type prefix and 'Active filters:' label. Tags now show just the value with an X button.",
        "steps": [
            ("Navigate to Product Management, apply a filter", "Filter tag appears"),
            ("Check tag text", 'Shows only the value (no "Search:" prefix)'),
        ]
    },
    {
        "id": 102, "title": "Export Contains Test/Sample Data", "page": "Product Management Export", "priority": "High", "status": "Fixed",
        "defect": "Export included 2 rows of sample data.",
        "comment": "Fixed. The export function exports live DB data - the sample rows were imported into DB during testing. Template fixed (QC #103). Any test data in DB needs manual cleanup.",
        "steps": [
            ("Export products", "CSV downloads"),
            ('Check for "Sample Product" rows', "Not present (if DB cleaned)"),
        ]
    },
    {
        "id": 103, "title": "Template Missing Fields / Has Sample Data", "page": "Product Management Template", "priority": "High", "status": "Fixed",
        "defect": "Template had wrong columns and included fake data rows.",
        "comment": "Fixed. New template: Name, Company, Description, Category, Product Type, Subcategory - headers only, no sample data. Matches Add New Product popup fields.",
        "steps": [
            ("Download product import template", "CSV downloads"),
            ("Open file - check columns", "Name, Company, Description, Category, Product Type, Subcategory"),
            ("Verify no sample/fake data rows", "Headers only"),
        ]
    },
    {
        "id": 104, "title": "Breadcrumb Links Broken", "page": "All Pages (19 views)", "priority": "Medium", "status": "Fixed",
        "defect": 'Breadcrumb links going to error pages or "#" (nowhere).',
        "comment": 'Fixed across 19 views. "Administration" links to Admin Dashboard. Category labels changed to non-clickable spans. Current page breadcrumb changed to non-clickable span.',
        "steps": [
            ("Navigate to any Admin page", "Breadcrumbs visible"),
            ('Click "Administration" breadcrumb', "Goes to Admin Dashboard"),
            ('Check category labels (e.g., "Data Management")', "NOT clickable"),
            ("Check current page breadcrumb", "NOT clickable"),
        ]
    },
    {
        "id": 105, "title": "Role Permission Descriptions", "page": "Admin - Roles", "priority": "Medium", "status": "Fixed",
        "defect": "Manage Workflow, View Audit Logs, and Manage Tags descriptions unclear/incorrect.",
        "comment": 'Fixed. "Manage Workflow" description updated. "View Audit Logs" hidden from UI (no page exists yet). "Manage Tags" description changed to "Classification tags (internal and external)". Note: Live DB needs SQL UPDATE.',
        "steps": [
            ("Log in as admin, go to Admin > Roles", "Role management page loads"),
            ('Check "Manage Workflow" description', '"Manage certificate workflow state transitions"'),
            ('Check "Manage Tags" description', '"Classification tags (internal and external)"'),
            ('Verify "View Audit Logs" is NOT visible', "Hidden from permission list"),
        ]
    },
    {
        "id": 106, "title": "Add User Missing Company Field", "page": "Admin - Users", "priority": "High", "status": "Fixed",
        "defect": "Company field only appeared for External role, not for all roles.",
        "comment": "Fixed. Company field visible for all roles except Partner (which shows Partner Assignment instead). Help text updated.",
        "steps": [
            ('Go to Admin > Users, click "Add New User"', "Modal opens"),
            ("Select Admin role", "Company field visible"),
            ("Select Analyst role", "Company field visible"),
            ("Select External role", "Company field visible"),
            ("Select Partner role", "Partner Assignment field shown instead"),
        ]
    },
    {
        "id": 107, "title": '"Failed to Create User" Error', "page": "Admin - Users", "priority": "High", "status": "Fixed",
        "defect": "Generic error message with no detail on why user creation failed.",
        "comment": 'Fixed. Added duplicate email pre-check. Changed CreateUserAsync return type to include error message. Specific errors now shown to user (e.g., "A user with email x already exists").',
        "steps": [
            ("Try to create user with existing email", 'Clear error: "A user with email x already exists"'),
            ("Try to create user with password < 8 chars", "Validation error shown"),
            ("Create user with valid data", "User created successfully"),
        ]
    },
    {
        "id": 108, "title": "User Template Has Sample Data", "page": "Admin - Users", "priority": "Low", "status": "Fixed",
        "defect": 'Template CSV included a fake "John Doe" example row.',
        "comment": "Fixed. Template now downloads as headers-only CSV.",
        "steps": [
            ("Download user import template", "CSV downloads"),
            ("Open file", 'Headers only, no "John Doe" data'),
        ]
    },
    {
        "id": 109, "title": "User Import Not Working", "page": "Admin - Users", "priority": "High", "status": "Fixed",
        "defect": "Importing users via CSV failed.",
        "comment": "Fixed. Same root causes as QC #107 and #108. Import errors now show actual failure reasons. Company column from CSV properly parsed with resolveCompanyIds().",
        "steps": [
            ("Download fresh template (headers only)", "Template downloads"),
            ("Fill in valid user data, save CSV", "File ready"),
            ("Import the CSV", "Users created successfully"),
            ("Import CSV with duplicate email", "Clear error message identifying the duplicate"),
        ]
    },
    {
        "id": 110, "title": "Partner Descriptions Update + Add Wayfair", "page": "Admin - Partners", "priority": "Medium", "status": "Fixed",
        "defect": "Novi description incorrect; needed Wayfair as new partner.",
        "comment": 'Fixed. Novi description updated to "Novi Connect is an AI shopping optimization platform." Wayfair added as new partner. Migration script created for live database.',
        "steps": [
            ("Go to Admin > Partner API Access", "Partner list visible"),
            ("Check Novi description", '"Novi Connect is an AI shopping optimization platform"'),
            ("Verify Wayfair is listed", "Present with correct description"),
            ("Check Create Certificate Partner dropdown", "Wayfair available as option"),
        ]
    },
    {
        "id": 111, "title": "Cert Type Add Popup Description Wrong", "page": "Admin - Certificate Types", "priority": "Low", "status": "Fixed",
        "defect": "Blue info box had incorrect description text and examples.",
        "comment": 'Fixed. Removed incorrect blue description box. Updated examples to "Recycled Content, Certified Energy Savings, etc."',
        "steps": [
            ('Go to Admin > Certificate Types, click "Add"', "Modal opens"),
            ("Verify no misleading blue description box", "Not present or corrected"),
            ("Check examples text", '"Recycled Content, Certified Energy Savings, etc."'),
        ]
    },
    {
        "id": 112, "title": "Subcategory Add Popup Description Wrong", "page": "Admin - Certificate Types", "priority": "Low", "status": "Fixed",
        "defect": "Blue info box had incorrect description text.",
        "comment": 'Fixed. Removed blue description box. Updated examples to "Attributes under multi-attribute label."',
        "steps": [
            ('Click "Add Subcategory"', "Modal opens"),
            ("Check examples text", '"Attributes under multi-attribute label"'),
        ]
    },
    {
        "id": 113, "title": "Allow Deletion of Cert Type with Certificates", "page": "Admin - Certificate Types", "priority": "High", "status": "Fixed",
        "defect": "Could delete a certificate type even if certificates were tied to it.",
        "comment": "Fixed. Client-side check blocks deletion if type has certificates. Server-side guard added as defense in depth.",
        "steps": [
            ("Find a cert type WITH certificates, click delete", "Error: cannot delete, has X certificates"),
            ("Find a cert type with 0 certificates, click delete", "Deletion succeeds"),
        ]
    },
    {
        "id": 114, "title": "Help Modal Language Update", "page": "Admin - Certificate Types", "priority": "Low", "status": "Fixed",
        "defect": "Help modal had incorrect examples.",
        "comment": 'Fixed. Updated all examples: Type level = "Certified Sustainability Facts", Subcategory level = "Recycled Content, Closed Loop", Assignment = "Recycled Content -> Certified Sustainability Facts."',
        "steps": [
            ("Go to Admin > Certificate Types, click Help", "Help modal opens"),
            ("Check type example", '"Certified Sustainability Facts"'),
            ("Check subcategory example", '"Recycled Content, Closed Loop"'),
        ]
    },
    {
        "id": 115, "title": "Product Types Edit Box Description Wrong", "page": "Admin - Product Types", "priority": "Medium", "status": "Fixed",
        "defect": "Blue info box had inaccurate description with fake examples.",
        "comment": 'Fixed. Changed to generic language. Removed fake examples. Inline examples now dynamically pulled from actual data, labeled "Existing."',
        "steps": [
            ("Go to Admin > Product Types, click Edit on a category", "Edit modal opens"),
            ("Verify no fake examples", 'No "Personal Care", "Skincare", etc.'),
            ("Check inline examples", 'Show actual data, labeled "Existing"'),
        ]
    },
    {
        "id": 116, "title": "Product Types Help Examples Inaccurate", "page": "Admin - Product Types", "priority": "Low", "status": "Fixed",
        "defect": "Help modal used hardcoded fake product examples.",
        "comment": "Fixed. Removed all hardcoded fake examples. Uses generic labels: Category -> Type -> Subcategory.",
        "steps": [
            ("Go to Admin > Product Types, click Help", "Help modal opens"),
            ("Verify no fake product names", "No specific products listed"),
            ("Check hierarchy explanation", "Generic: Category -> Type -> Subcategory"),
        ]
    },
    {
        "id": 117, "title": "New Product Types Defaulting to Wrong Category", "page": "Admin - Product Types", "priority": "High", "status": "Fixed",
        "defect": 'New root-level product types always defaulted to "Chemicals and Raw Materials."',
        "comment": "Fixed. CreateProductTypeAsync used FirstOrDefaultAsync() with no filter, always returning first record. Now creates matching ProductCategory for each new root-level category. Also fixed rename sync.",
        "steps": [
            ("Go to Admin > Product Types", "Page loads"),
            ('Add a new root-level category (e.g., "Electronics")', "Category created"),
            ("Verify it appears as its own category", 'NOT under "Chemicals and Raw Materials"'),
            ("Add a subcategory under the new category", "Appears under correct parent"),
        ]
    },
    {
        "id": 118, "title": "Certificate Field Sections Confusing", "page": "Certificate Field Mgmt", "priority": "Medium", "status": "Fixed",
        "defect": "Section headers on Create/Edit forms were confusing and unnecessary.",
        "comment": 'Fixed. Removed section headers from Create/Edit forms. All custom fields display flat under "Additional Fields." Section column remains in admin table for internal use.',
        "steps": [
            ("Add a custom field with a Section value via Admin", "Field created"),
            ("Navigate to Create Certificate", 'Custom fields listed flat under "Additional Fields" - no section headers'),
            ("Check Admin > Certificate Field Management", "Section column still visible"),
        ]
    },
    {
        "id": 119, "title": "Custom Fields Auto-Added to Export?", "page": "Certificate Field Mgmt", "priority": "Medium", "status": "Informational",
        "defect": "Need confirmation that new custom fields appear in export automatically.",
        "comment": "Confirmed. Exports dynamically query all custom fields and append them as additional columns. New fields appear automatically in next export. Custom fields do NOT appear in tile view tables.",
        "steps": [
            ("Add a new custom field via Admin", "Field created"),
            ("Edit a certificate, fill in the new field", "Value saved"),
            ("Export certificates", "New custom field appears as a column"),
            ("Check tile view table", "Custom field NOT in table columns (expected)"),
        ]
    },
    {
        "id": 120, "title": "Tags Template Has Sample Data", "page": "Admin - Tags", "priority": "Low", "status": "Fixed",
        "defect": "Template export included sample data.",
        "comment": "Fixed. Tags template export now downloads a CSV with headers only.",
        "steps": [
            ('Go to Admin > Tags, click "Download Template"', "CSV downloads"),
            ("Open file", "Headers only, no sample data"),
        ]
    },
    {
        "id": 121, "title": "New Partner Not in Cert Creation Dropdown", "page": "Partners / Create Cert", "priority": "High", "status": "Fixed",
        "defect": "Partners added via Admin didn't appear in certificate creation dropdown.",
        "comment": "Fixed. Partner dropdown was hardcoded with only None, Amazon, Novi, Other. Now dynamically loads from the database so new partners appear automatically.",
        "steps": [
            ("Go to Admin > Partners, add a new partner", "Partner created"),
            ("Navigate to Create Certificate, open Partner dropdown", "New partner appears in list"),
            ("Select the new partner, save the certificate", "Saves with correct partner"),
        ]
    },
]

# ============================================================
# TABLE OF CONTENTS
# ============================================================
doc.add_heading('Table of Contents', level=1)
for item in items:
    p = doc.add_paragraph(style='List Number')
    run = p.add_run(f'QC #{item["id"]} \u2014 {item["title"]}')
    run.font.size = Pt(10)
    p.paragraph_format.space_after = Pt(2)

doc.add_page_break()

# ============================================================
# EACH QC ITEM
# ============================================================
for item in items:
    doc.add_heading(f'QC #{item["id"]} \u2014 {item["title"]}', level=2)

    # Meta info
    meta_data = [
        ("Page", item["page"]),
        ("Priority", item["priority"]),
        ("Status", item["status"]),
    ]
    add_table(doc, ["Field", "Value"], meta_data, [1.5, 5.0])
    doc.add_paragraph()

    # Defect
    p = doc.add_paragraph()
    run = p.add_run("Defect: ")
    run.bold = True
    run.font.size = Pt(10)
    run = p.add_run(item["defect"])
    run.font.size = Pt(10)

    # Final Comment
    doc.add_heading('Final Comment', level=3)
    p = doc.add_paragraph(item["comment"])
    p.style.font.size = Pt(10)

    # Test Steps
    doc.add_heading('Test Steps', level=3)
    test_rows = []
    for i, (action, expected) in enumerate(item["steps"], 1):
        test_rows.append((str(i), action, expected, ""))
    add_table(doc, ["Step", "Action", "Expected Result", "Pass/Fail"], test_rows, [0.4, 3.0, 2.8, 0.6])

    doc.add_paragraph()  # spacing

# ============================================================
# SUMMARY TABLE
# ============================================================
doc.add_page_break()
doc.add_heading('Summary \u2014 All Items', level=1)

summary_rows = [(str(item["id"]), item["page"], item["status"], item["priority"]) for item in items]
add_table(doc, ["QC#", "Page", "Status", "Priority"], summary_rows, [0.5, 2.5, 2.0, 1.0])

doc.add_paragraph()
doc.add_heading('Status Counts', level=2)

from collections import Counter
status_counts = Counter(item["status"] for item in items)
count_rows = [(status, str(count)) for status, count in status_counts.most_common()]
count_rows.append(("Total", str(len(items))))
add_table(doc, ["Status", "Count"], count_rows, [3.0, 1.0])

# ============================================================
# SAVE
# ============================================================
output_path = os.path.join(
    r"C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\docs\testing",
    "QC_FINAL_COMMENTS_AND_TEST_STEPS.docx"
)
doc.save(output_path)
print(f"Word document saved to: {output_path}")
