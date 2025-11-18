# GCC Database - Comprehensive UI Test Plan
## Visual Review & End-to-End Testing

**Created:** 2025-10-05
**Purpose:** Client-ready comprehensive testing for all user roles and scenarios
**Application:** GCC Certificate Management System

---

## Table of Contents
1. [Test Strategy](#test-strategy)
2. [Role-Based Test Coverage](#role-based-test-coverage)
3. [Page-by-Page Test Cases](#page-by-page-test-cases)
4. [End-to-End Workflows](#end-to-end-workflows)
5. [Screenshot Test Matrix](#screenshot-test-matrix)
6. [UI/UX Validation Checklist](#uiux-validation-checklist)

---

## Test Strategy

### Testing Objectives
1. **Visual Validation**: Ensure UI matches Master Design Guide specifications
2. **Functionality Testing**: Verify all features work as documented
3. **Role-Based Access**: Confirm proper permissions per user role
4. **End-to-End Workflows**: Test complete business processes
5. **Cross-Browser Compatibility**: Test on Edge, Chrome, Firefox
6. **Responsive Design**: Validate mobile, tablet, desktop views

### User Roles & Permissions Matrix

| Role | Certificate View | Certificate Create/Edit | Dashboard Access | User Management | API Access | Admin Pages |
|------|-----------------|------------------------|-----------------|----------------|------------|-------------|
| **Public** | ✅ Tile View Only | ❌ | ❌ | ❌ | ❌ | ❌ |
| **External** | ✅ Partner-filtered | ❌ | ❌ | ❌ | ✅ API Key | ❌ |
| **Internal** | ✅ All certificates | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Analyst** | ✅ All certificates | ✅ CRUD | ✅ View | ❌ | ✅ | ❌ |
| **Admin** | ✅ All certificates | ✅ CRUD | ✅ View/Export | ✅ Full | ✅ | ✅ |

### Test Credentials

```plaintext
Admin:    admin@greencirclecertified.com / Admin123!
Analyst:  analyst@greencirclecertified.com / Analyst123!
External: partner@amazon.com / Partner123!
```

---

## Role-Based Test Coverage

### 1. PUBLIC USER Tests (Unauthenticated)

#### Test Suite: PUBLIC-001
**Objective:** Validate public certificate browsing without authentication

| Test ID | Page | Action | Expected Result | Screenshot Needed |
|---------|------|--------|----------------|-------------------|
| PUB-001 | /Certificate/CertificateTileView | Load landing page | - Certificate tiles displayed<br>- Infinite scroll working<br>- No login required<br>- Sign In button visible | ✅ Full page |
| PUB-002 | /Certificate/CertificateTileView | Use search autocomplete | - Suggestions categorized (Company/Product/Certificate)<br>- Type badges colored correctly<br>- Font size 16px minimum | ✅ Dropdown expanded |
| PUB-003 | /Certificate/CertificateTileView | Apply filters | - Sidebar filters expand/collapse<br>- Multiple filter selection<br>- Clear filters button works | ✅ Filters applied |
| PUB-004 | /Certificate/CertificateTileView | Sort certificates | - Sort dropdown shows all options<br>- Results update correctly<br>- URL reflects sort choice | ✅ Sort dropdown |
| PUB-005 | /Certificate/CertificateTileView | Scroll for more | - Infinite scroll loads more tiles<br>- No pagination controls<br>- Smooth loading | ✅ After scroll |
| PUB-006 | /Certificate/Details/{id} | Click tile to view details | - Certificate details load<br>- Download PDF button visible<br>- Company info displayed | ✅ Details view |
| PUB-007 | /Certificate/Details/{id} | Download certificate PDF | - PDF generates successfully<br>- Proper branding/formatting<br>- All fields present | ✅ PDF preview |
| PUB-008 | /Account/Login | Click Sign In | - Login page loads<br>- Username/password fields<br>- Forgot password link | ✅ Login screen |

---

### 2. EXTERNAL USER Tests (Partner Access)

#### Test Suite: EXT-001
**Objective:** Validate partner-specific certificate access

| Test ID | Page | Action | Expected Result | Screenshot Needed |
|---------|------|--------|----------------|-------------------|
| EXT-001 | /Account/Login | Login as partner@amazon.com | - Authentication successful<br>- Redirect to tile view<br>- Session created | ✅ After login |
| EXT-002 | /Certificate/CertificateTileView | View certificates | - **Only Amazon certificates shown**<br>- Partner filter auto-applied<br>- Cannot see other partners | ✅ Filtered tiles |
| EXT-003 | /Certificate/Details/{id} | Access certificate details | - Can view Amazon certs<br>- Cannot view non-Amazon certs<br>- Proper 403 on unauthorized | ✅ Details view |
| EXT-004 | /Certificate/Create | Try to create certificate | - **Access denied (403)**<br>- Redirect to error page | ✅ Error page |
| EXT-005 | /Dashboard | Try to access dashboard | - **Access denied (403)**<br>- Proper error message | ✅ Error page |
| EXT-006 | /Admin/UserManagement | Try to access admin | - **Access denied (403)**<br>- Cannot see admin menu | ✅ Error page |
| EXT-007 | Navigation | Check visible menu items | - Only "Certificates" visible<br>- No Dashboard or Admin links | ✅ Nav bar |

---

### 3. ANALYST USER Tests (Certificate Management)

#### Test Suite: ANALYST-001
**Objective:** Validate analyst certificate CRUD operations

| Test ID | Page | Action | Expected Result | Screenshot Needed |
|---------|------|--------|----------------|-------------------|
| ANL-001 | /Account/Login | Login as analyst@... | - Authentication successful<br>- Redirect to dashboard | ✅ Dashboard |
| ANL-002 | /Dashboard | View analytics | - All 12 metrics visible<br>- 5 data tables displayed<br>- Export buttons present | ✅ Full dashboard |
| ANL-003 | /Certificate/CertificateTileView | View all certificates | - All partners visible<br>- Can filter by any partner<br>- Full search access | ✅ Tile view |
| ANL-004 | /Certificate/Create | Create new certificate | - Form loads successfully<br>- All required fields present<br>- Multi-select dropdowns work<br>- Partner dropdown visible | ✅ Create form |
| ANL-005 | /Certificate/Create | Submit new certificate | - Validation works<br>- Certificate created<br>- Redirect to details<br>- Success message shown | ✅ Success state |
| ANL-006 | /Certificate/Edit/{id} | Edit existing certificate | - Form pre-populated<br>- Product ID manager visible<br>- Can add/remove ASINs<br>- Workflow status dropdown | ✅ Edit form |
| ANL-007 | /Certificate/Edit/{id} | Change certificate status | - Valid transitions allowed<br>- Invalid transitions blocked<br>- Status history updated | ✅ Status change |
| ANL-008 | /Certificate/Edit/{id} | Manage Product IDs | - Can add 100+ ASINs<br>- Bulk CSV import works<br>- Export to CSV works<br>- Validation on duplicates | ✅ Product ID manager |
| ANL-009 | /Admin/UserManagement | Try to access user mgmt | - **Access denied (403)**<br>- No admin menu visible | ✅ Error page |

---

### 4. ADMIN USER Tests (Full Access)

#### Test Suite: ADMIN-001
**Objective:** Validate full administrative capabilities

| Test ID | Page | Action | Expected Result | Screenshot Needed |
|---------|------|--------|----------------|-------------------|
| ADM-001 | /Account/Login | Login as admin@... | - Authentication successful<br>- Full navigation visible | ✅ After login |
| ADM-002 | /Dashboard | View analytics dashboard | - All metrics displayed<br>- Export individual metrics<br>- Export full dashboard | ✅ Dashboard full |
| ADM-003 | /Dashboard | Export dashboard data | - Excel export with formatting<br>- CSV export works<br>- PDF export works | ✅ Export options |
| ADM-004 | /Certificate/Create | Create certificate | - All fields accessible<br>- Partner assignment works<br>- Audit trail created | ✅ Create form |
| ADM-005 | /Certificate/Edit/{id} | Full edit capabilities | - Can edit all fields<br>- Can change any status<br>- Can delete certificate | ✅ Edit form |
| ADM-006 | /Admin/UserManagement | Manage users | - User list displayed<br>- Can create new users<br>- Can edit user roles<br>- Can delete users (except admin) | ✅ User management |
| ADM-007 | /Admin/UserManagement | Assign roles | - All 5 roles available<br>- Role change works<br>- Audit trail created | ✅ Role assignment |
| ADM-008 | /Admin/UserManagement | Try delete admin user | - **Deletion blocked**<br>- Error message shown<br>- Admin role protected | ✅ Error state |
| ADM-009 | /Admin/RoleManagement | Manage roles | - Role list displayed<br>- Can create custom roles<br>- Can assign permissions | ✅ Role management |
| ADM-010 | /Admin/ApiKeys | API key management | - Can generate API keys<br>- Can revoke keys<br>- Rate limits configurable | ✅ API key page |

---

## Page-by-Page Test Cases

### Page: Certificate Tile View (Landing Page)
**URL:** `/Certificate/CertificateTileView`
**Role Access:** Public, External, Internal, Analyst, Admin

#### UI Design Validation

| Element | Requirement | Validation Steps |
|---------|------------|------------------|
| **Search Bar** | - 16px minimum font<br>- Autocomplete dropdown<br>- Categorized suggestions | 1. Click search input<br>2. Type partial text<br>3. Verify dropdown shows categories<br>4. Check font size ≥ 16px |
| **Suggestion Badges** | - Company: #e3f2fd bg, #1976d2 text<br>- Product: #f3e5f5 bg, #7b1fa2 text<br>- Certificate: #e8f5e8 bg, #388e3c text | 1. Inspect badge colors<br>2. Verify data-type attributes<br>3. Check contrast ratios |
| **Filter Sidebar** | - 300px width<br>- Collapsible panels<br>- Clear filters link | 1. Measure sidebar width<br>2. Expand/collapse filters<br>3. Click clear filters |
| **Sort Dropdown** | - 7 sort options<br>- Newest Effective Date<br>- Expiring Soon<br>- Product/Company A-Z | 1. Click sort dropdown<br>2. Verify all options present<br>3. Test each sort |
| **Certificate Tiles** | - Grid layout (3 columns desktop)<br>- Blue header (#3a5a97)<br>- Download icon<br>- Expand icon | 1. Count columns<br>2. Verify header color<br>3. Test action buttons |
| **Infinite Scroll** | - **NO PAGINATION**<br>- Load 20 items at a time<br>- Smooth loading | 1. Scroll to bottom<br>2. Verify more load<br>3. Check no page numbers |

#### Functional Tests

```
TEST: TC-TILE-001 - Search Autocomplete
STEPS:
1. Navigate to /Certificate/CertificateTileView
2. Click search input
3. Type "Amazon"
4. Wait for autocomplete dropdown

EXPECTED:
- Dropdown appears within 300ms
- Suggestions categorized:
  * Companies containing "Amazon"
  * Products containing "Amazon"
  * Certificates containing "Amazon"
- Type badges display correct colors
- Font size = 16px

SCREENSHOT: autocomplete-dropdown.png
```

```
TEST: TC-TILE-002 - Filter Application
STEPS:
1. Navigate to /Certificate/CertificateTileView
2. Expand "Product Categories" filter
3. Check "Construction / Building Products"
4. Expand "Countries" filter
5. Check "United States"
6. Click "Apply Filters" (if button exists) or auto-apply

EXPECTED:
- URL updates with filter parameters
- Certificate tiles refresh
- Results count updates
- Shareable URL works (copy/paste in new tab shows same filters)

SCREENSHOT: filters-applied.png
```

```
TEST: TC-TILE-003 - Infinite Scroll
STEPS:
1. Navigate to /Certificate/CertificateTileView
2. Scroll to bottom of page
3. Wait for more tiles to load
4. Repeat 3 times

EXPECTED:
- New tiles load automatically
- No "Next Page" button
- Loading indicator shows briefly
- Smooth scroll experience

SCREENSHOT: infinite-scroll.png
```

---

### Page: Certificate Details View
**URL:** `/Certificate/Details/{id}`
**Role Access:** Public (read), Analyst (edit), Admin (edit)

#### UI Elements Validation

| Element | Requirement | Test Steps |
|---------|------------|------------|
| **Header Section** | - Certificate type badge<br>- Certificate number<br>- Status indicator | Verify all elements present and styled |
| **Company Information** | - Company name<br>- Address<br>- Contact info | Check all fields populated |
| **Product Information** | - Product name<br>- Product category<br>- Product type | Validate hierarchy display |
| **Product ID Section** | - ASIN list (100+ support)<br>- Download list<br>- Bulk manage (Analyst+) | Test with certificate having 100+ ASINs |
| **Certification Details** | - Effective date<br>- Expiration date<br>- Certification type | Verify date formatting |
| **Download Button** | - PDF download<br>- QuestPDF generation<br>- Proper branding | Click and verify PDF contents |
| **Edit Button** | - Visible for Analyst/Admin<br>- Hidden for Public/External | Role-based visibility check |

#### Functional Tests

```
TEST: TC-DETAILS-001 - View Certificate Details
ROLE: Public User
STEPS:
1. Navigate to /Certificate/Details/1
2. Verify all sections visible
3. Check PDF download button

EXPECTED:
- All certificate info displayed
- Download PDF button works
- No Edit button visible
- Product IDs list shows (if >10, shows "View All")

SCREENSHOT: certificate-details-public.png
```

```
TEST: TC-DETAILS-002 - Edit Certificate (Analyst)
ROLE: Analyst
STEPS:
1. Login as analyst@greencirclecertified.com
2. Navigate to /Certificate/Details/1
3. Click "Edit" button
4. Redirects to /Certificate/Edit/1

EXPECTED:
- Edit button visible
- Click redirects to edit form
- Form pre-populated with current values

SCREENSHOT: certificate-details-analyst.png
```

---

### Page: Certificate Create/Edit Form
**URL:** `/Certificate/Create` or `/Certificate/Edit/{id}`
**Role Access:** Analyst, Admin

#### Form Field Validation

| Field | Type | Validation Rules | Test Steps |
|-------|------|-----------------|------------|
| Certificate Number | Text | Auto-generated or manual | Verify generation |
| Company | Dropdown | Required, searchable | Test search/select |
| Product | Dropdown | Required, cascading from company | Select company first |
| Certification Type | Multi-select | Required, multiple allowed | Select 2+ types |
| Partner | Dropdown | Amazon/Wayfair/None | Test all options |
| Effective Date | Date Picker | Required, cannot be future | Test date validation |
| Expiration Date | Date Picker | Required, must be > Effective | Test date range |
| Status | Dropdown | 14 workflow states | Test valid transitions |
| Location | Text | City, State, Country | Fill all fields |
| Product IDs | Manager Component | ASINs, UPCs, GTINs, etc. | Add 100+ IDs |

#### Functional Tests

```
TEST: TC-CREATE-001 - Create Certificate Happy Path
ROLE: Analyst
STEPS:
1. Login as analyst
2. Navigate to /Certificate/Create
3. Fill all required fields:
   - Company: "Test Company Inc."
   - Product: "Test Product"
   - Certification Type: ["CSF", "LCA"]
   - Partner: "Amazon"
   - Effective Date: Today
   - Expiration Date: Today + 3 years
   - Status: "Draft"
4. Click "Save"

EXPECTED:
- Validation passes
- Certificate created
- Redirect to /Certificate/Details/{new-id}
- Success message shown
- Audit trail created (CreatedBy = analyst email)

SCREENSHOT: create-certificate-success.png
```

```
TEST: TC-CREATE-002 - Validation Errors
ROLE: Analyst
STEPS:
1. Navigate to /Certificate/Create
2. Leave Company empty
3. Set Expiration Date < Effective Date
4. Click "Save"

EXPECTED:
- Validation errors displayed:
  * "Company is required"
  * "Expiration date must be after effective date"
- Form not submitted
- Focus on first error field

SCREENSHOT: create-certificate-validation-errors.png
```

```
TEST: TC-EDIT-001 - Product ID Manager
ROLE: Analyst
STEPS:
1. Navigate to /Certificate/Edit/1
2. Scroll to "Product ID Manager" section
3. Click "Add Product ID"
4. Select Type: "ASIN"
5. Enter Value: "B08XYZ12345"
6. Click "Add"
7. Repeat to add 100 ASINs
8. Click "Bulk Import CSV"
9. Upload 500 ASIN CSV file
10. Click "Export to CSV"

EXPECTED:
- Can add individual IDs
- Supports 100+ IDs in UI
- Bulk import validates and adds all
- Export CSV contains all IDs
- Duplicate detection works

SCREENSHOT: product-id-manager.png
```

---

### Page: Dashboard (Analytics)
**URL:** `/Dashboard`
**Role Access:** Analyst, Admin

#### Dashboard Metrics Validation

| Metric Card | Data Source | Expected Behavior |
|-------------|------------|-------------------|
| Total Active Certs | COUNT where Status = Active | Real-time count |
| Companies with Active Certs | DISTINCT Company WHERE Cert Active | Updates on create/expire |
| Expired Certificates | COUNT where Status = Expired | Includes historical |
| Pending Renewals | COUNT where Expiring < 30 days | Rolling 30-day window |
| Certificates by Type | GROUP BY CertificationType | Bar chart or table |
| Top Companies | ORDER BY cert count DESC LIMIT 10 | Ranked list |
| Monthly Trends | GROUP BY MONTH(EffectiveDate) | Line graph |
| Partner Distribution | GROUP BY Partner | Pie chart |

#### Functional Tests

```
TEST: TC-DASH-001 - View Dashboard Metrics
ROLE: Analyst
STEPS:
1. Login as analyst
2. Navigate to /Dashboard
3. Verify all 12 metric cards visible
4. Verify all 5 data tables visible

EXPECTED:
- 12 metrics displayed:
  1. Total Active Certs
  2. # Companies with Active Certs
  3. # Product Categories
  4. # Product Types
  5. # Product Subcategories
  6. # Products with Active Certs
  7. # Certs Published Last Month
  8. # Certs Expiring This Month
  9. # Certs Expired Last Month
  10. # Certs Expiring Next Month
  11. # Certs Published This Year
  12. # Certs Published Last Year
- 5 data tables:
  1. Active Certs by Certification Type
  2. Top 10 Companies by Cert Count
  3. Certs by Status
  4. Top 10 Product Types
  5. Top 5 Product Categories

SCREENSHOT: dashboard-full.png
```

```
TEST: TC-DASH-002 - Export Dashboard Data
ROLE: Admin
STEPS:
1. Login as admin
2. Navigate to /Dashboard
3. Click "Export Selected Metric" (select "Total Active Certs")
4. Click "Export Full Dashboard"

EXPECTED:
- Single metric exports as CSV/Excel
- Full dashboard exports all metrics
- Excel file has formatting (ClosedXML)
- File downloads successfully

SCREENSHOT: dashboard-export-options.png
```

---

### Page: User Management
**URL:** `/Admin/UserManagement`
**Role Access:** Admin ONLY

#### User Management Tests

```
TEST: TC-USER-001 - View User List
ROLE: Admin
STEPS:
1. Login as admin@greencirclecertified.com
2. Navigate to /Admin/UserManagement

EXPECTED:
- User list table displayed
- Columns: Email, Name, Role, Status, Actions
- All users visible
- Search/filter controls present

SCREENSHOT: user-management-list.png
```

```
TEST: TC-USER-002 - Create New User
ROLE: Admin
STEPS:
1. Navigate to /Admin/UserManagement
2. Click "Add User"
3. Fill form:
   - Email: testuser@example.com
   - Name: Test User
   - Role: Analyst
   - Send invite email: Yes
4. Click "Save"

EXPECTED:
- User created successfully
- Appears in user list
- Invitation email sent
- Default password generated

SCREENSHOT: user-management-create.png
```

```
TEST: TC-USER-003 - Change User Role
ROLE: Admin
STEPS:
1. Navigate to /Admin/UserManagement
2. Find user: analyst@greencirclecertified.com
3. Click "Edit"
4. Change Role from "Analyst" to "Internal"
5. Click "Save"

EXPECTED:
- Role updated successfully
- User permissions change immediately
- Audit log entry created
- User sees new role on next login

SCREENSHOT: user-management-edit-role.png
```

```
TEST: TC-USER-004 - Delete User (Protection Test)
ROLE: Admin
STEPS:
1. Navigate to /Admin/UserManagement
2. Find user: admin@greencirclecertified.com
3. Click "Delete"

EXPECTED:
- **Deletion blocked with error message**
- "Admin user cannot be deleted"
- User remains in list
- No data changed

SCREENSHOT: user-management-delete-blocked.png
```

```
TEST: TC-USER-005 - Access Denied (Analyst)
ROLE: Analyst
STEPS:
1. Login as analyst@greencirclecertified.com
2. Navigate to /Admin/UserManagement

EXPECTED:
- **403 Forbidden error**
- Error page displayed
- "Access denied" message
- No user data visible

SCREENSHOT: user-management-access-denied.png
```

---

## End-to-End Workflows

### Workflow 1: Public User Certificate Discovery

```
E2E-WORKFLOW-001: Public User Finds and Downloads Certificate

ACTOR: Anonymous public user
GOAL: Find specific certificate and download PDF

STEPS:
1. Navigate to http://localhost:5101/Certificate/CertificateTileView
   VERIFY: Landing page loads, tiles visible
   SCREENSHOT: 01-landing-page.png

2. Type "ASSA ABLOY" in search bar
   VERIFY: Autocomplete shows company suggestions
   SCREENSHOT: 02-search-autocomplete.png

3. Click company suggestion "ASSA ABLOY Access..."
   VERIFY: Results filter to ASSA ABLOY certificates
   SCREENSHOT: 03-search-results.png

4. Expand "Product Categories" filter
   VERIFY: Filter panel expands
   SCREENSHOT: 04-filter-panel.png

5. Check "Construction / Building Products"
   VERIFY: Results update, URL changes
   SCREENSHOT: 05-filter-applied.png

6. Click certificate tile #24-1605
   VERIFY: Details page loads
   SCREENSHOT: 06-certificate-details.png

7. Click "Download PDF" button
   VERIFY: PDF generates and downloads
   SCREENSHOT: 07-pdf-download.png

8. Open PDF file
   VERIFY: Proper branding, all fields present
   SCREENSHOT: 08-pdf-contents.png

SUCCESS CRITERIA:
- All pages load < 2 seconds
- Autocomplete responds < 300ms
- Filters apply without page refresh
- PDF contains all certificate data
- URL is shareable (copy/paste works)
```

### Workflow 2: Analyst Creates Certificate

```
E2E-WORKFLOW-002: Analyst Creates New Certificate

ACTOR: Analyst user
GOAL: Create certificate for Amazon partner

STEPS:
1. Navigate to /Account/Login
   SCREENSHOT: 01-login-page.png

2. Login with analyst@greencirclecertified.com / Analyst123!
   VERIFY: Redirect to dashboard
   SCREENSHOT: 02-dashboard-after-login.png

3. Click "Certificates" in navigation
   VERIFY: Certificate tile view loads
   SCREENSHOT: 03-certificate-list.png

4. Click "Create Certificate" button
   VERIFY: Create form loads
   SCREENSHOT: 04-create-form-empty.png

5. Fill certificate details:
   - Company: "Amazon.com, Inc." (select from dropdown)
   - Product: "Amazon Basics Paper Towels" (select from dropdown)
   - Certification Type: ["CSF"] (multi-select)
   - Partner: "Amazon" (dropdown)
   - Effective Date: 10/05/2025
   - Expiration Date: 10/04/2028
   - Status: "Draft"
   - Location: Seattle, WA, United States
   SCREENSHOT: 05-create-form-filled.png

6. Scroll to "Product ID Manager"
   Click "Add Product ID"
   - Type: ASIN
   - Value: B09XYZ123TEST
   Click "Add"
   Repeat for 10 ASINs
   SCREENSHOT: 06-product-ids-added.png

7. Click "Save Certificate"
   VERIFY: Validation passes
   SCREENSHOT: 07-before-save.png

8. After save success
   VERIFY: Redirect to /Certificate/Details/{new-id}
   SCREENSHOT: 08-certificate-created-success.png

9. Verify certificate details
   VERIFY: All entered data displays correctly
   SCREENSHOT: 09-certificate-details-verify.png

10. Click "Edit" button
    VERIFY: Edit form pre-populated
    SCREENSHOT: 10-edit-form-prepopulated.png

11. Change Status: "Draft" → "ApplicationSubmitted"
    Click "Save"
    VERIFY: Status updates, history entry created
    SCREENSHOT: 11-status-updated.png

SUCCESS CRITERIA:
- Login successful
- Create form validates all fields
- Certificate created with audit trail
- Product IDs saved correctly
- Status workflow enforced
- Edit form pre-populates
```

### Workflow 3: Admin Manages Users

```
E2E-WORKFLOW-003: Admin Creates User and Assigns Role

ACTOR: Admin user
GOAL: Create new analyst user and configure permissions

STEPS:
1. Login as admin@greencirclecertified.com / Admin123!
   SCREENSHOT: 01-admin-login.png

2. Navigate to /Admin/UserManagement
   VERIFY: User list loads
   SCREENSHOT: 02-user-management-list.png

3. Click "Add User" button
   VERIFY: Create user form displays
   SCREENSHOT: 03-add-user-form.png

4. Fill user details:
   - Email: newanalyst@greencirclecertified.com
   - Full Name: Jane Analyst
   - Role: Analyst
   - Send Invitation: Yes
   SCREENSHOT: 04-add-user-filled.png

5. Click "Create User"
   VERIFY: User created, invitation sent
   SCREENSHOT: 05-user-created-success.png

6. Find new user in list
   VERIFY: Appears with "Analyst" role
   SCREENSHOT: 06-new-user-in-list.png

7. Click "Edit" on new user
   VERIFY: Edit form loads
   SCREENSHOT: 07-edit-user-form.png

8. Change Role to "Internal"
   Click "Save"
   VERIFY: Role updated
   SCREENSHOT: 08-role-changed.png

9. Try to delete admin@greencirclecertified.com
   Click "Delete" on admin user
   VERIFY: Error message, deletion blocked
   SCREENSHOT: 09-admin-delete-blocked.png

10. Logout and login as newanalyst@...
    VERIFY: New user can login with Internal role
    SCREENSHOT: 10-new-user-login-verify.png

SUCCESS CRITERIA:
- User creation successful
- Email invitation sent
- Role assignment works
- Admin user protected from deletion
- Audit trail created for all actions
```

### Workflow 4: Partner API Access

```
E2E-WORKFLOW-004: External Partner Uses API

ACTOR: Amazon partner with API key
GOAL: Retrieve partner-specific certificates via API

STEPS:
1. Login as admin to create API key
   Navigate to /Admin/ApiKeys
   SCREENSHOT: 01-api-keys-page.png

2. Click "Generate API Key"
   - Name: Amazon Production Key
   - Partner: Amazon
   - Rate Limit: 1000/hour
   Click "Generate"
   VERIFY: API key created
   SCREENSHOT: 02-api-key-generated.png

3. Copy API key (save for next steps)
   SCREENSHOT: 03-api-key-copied.png

4. Open Postman/Swagger
   Configure GET request:
   - URL: http://localhost:5101/api/CertificateTileExternal/tiles
   - Header: X-API-Key: {copied-key}
   SCREENSHOT: 04-api-request-configured.png

5. Send request
   VERIFY: 200 OK response
   VERIFY: Only Amazon certificates returned
   SCREENSHOT: 05-api-response.png

6. Parse JSON response
   VERIFY: Partner filtering applied
   VERIFY: Certificate data complete
   SCREENSHOT: 06-api-response-json.png

7. Test rate limiting
   Send 1001 requests in < 1 hour
   VERIFY: 429 Too Many Requests on 1001st
   SCREENSHOT: 07-rate-limit-exceeded.png

8. Navigate to /swagger
   VERIFY: API documentation loads
   SCREENSHOT: 08-swagger-docs.png

SUCCESS CRITERIA:
- API key generation works
- Partner filtering automatic
- Rate limiting enforced
- Swagger docs complete
- JSON response valid
```

---

## Screenshot Test Matrix

### Automated Screenshot Test Suite

| Role | Page | Scenario | File Name | Status |
|------|------|----------|-----------|--------|
| **Public** | / Certificate/CertificateTileView | Landing page | `public-01-tile-view.png` | ⏳ Pending |
| **Public** | /Certificate/CertificateTileView | Autocomplete expanded | `public-02-autocomplete.png` | ⏳ Pending |
| **Public** | /Certificate/CertificateTileView | Filters applied | `public-03-filters-applied.png` | ⏳ Pending |
| **Public** | /Certificate/Details/1 | Certificate details | `public-04-cert-details.png` | ⏳ Pending |
| **Public** | /Account/Login | Login screen | `public-05-login.png` | ✅ Complete |
| **External** | /Certificate/CertificateTileView | Partner-filtered view | `external-01-filtered-view.png` | ⏳ Pending |
| **External** | /Certificate/Details/1 | Amazon cert details | `external-02-cert-details.png` | ⏳ Pending |
| **External** | /Certificate/Create | Access denied | `external-03-create-denied.png` | ⏳ Pending |
| **Analyst** | /Dashboard | Analytics dashboard | `analyst-01-dashboard.png` | ✅ Complete |
| **Analyst** | /Certificate/Create | Create form | `analyst-02-create-form.png` | ✅ Complete |
| **Analyst** | /Certificate/Edit/1 | Edit with Product IDs | `analyst-03-edit-form.png` | ✅ Complete |
| **Analyst** | /Admin/UserManagement | Access denied | `analyst-04-admin-denied.png` | ⏳ Pending |
| **Admin** | /Dashboard | Full dashboard | `admin-01-dashboard.png` | ✅ Complete |
| **Admin** | /Certificate/Create | Create form | `admin-02-create-form.png` | ✅ Complete |
| **Admin** | /Certificate/Edit/1 | Edit form | `admin-03-edit-form.png` | ✅ Complete |
| **Admin** | /Admin/UserManagement | User management | `admin-04-user-mgmt.png` | ✅ Complete |
| **Admin** | /Admin/RoleManagement | Role management | `admin-05-role-mgmt.png` | ⏳ Pending |
| **Admin** | /Admin/ApiKeys | API key management | `admin-06-api-keys.png` | ⏳ Pending |

### Screenshot Specifications

```
Resolution: 1920x1080 (Desktop)
           1024x768 (Tablet)
           375x667 (Mobile)

Browser: Microsoft Edge (Chromium)

Format: PNG, full page capture

File Naming: {role}-{sequence}-{page-description}.png
```

---

## UI/UX Validation Checklist

### Design System Compliance

#### Typography
- [ ] Font: Century Gothic used throughout
- [ ] Minimum font size: 16px (accessibility)
- [ ] Headings: Proper hierarchy (H1 > H2 > H3)
- [ ] Line height: 1.6 for body text

#### Color Palette
- [ ] Forest Green: #447638 (primary actions)
- [ ] Lime Green: #92C83E (secondary actions)
- [ ] Ocean Blue: #00AEEF (links, accents)
- [ ] Navy Blue: #002876 (headings)
- [ ] Off White: #F5F2E8 (backgrounds)

#### Component Validation
- [ ] Buttons: Rounded corners, proper padding
- [ ] Form inputs: 1px border, proper focus states
- [ ] Dropdowns: Consistent styling, clear selected state
- [ ] Modals: Centered, proper overlay opacity
- [ ] Cards: Proper shadow, border radius
- [ ] Icons: Font Awesome 6.5.1 (NOT 6.4.0)

### Accessibility Compliance

- [ ] Keyboard navigation works for all interactive elements
- [ ] Focus states visible on all controls
- [ ] ARIA labels present on icon buttons
- [ ] Color contrast meets WCAG AA standards
- [ ] Alt text on all images
- [ ] Form labels associated with inputs
- [ ] Error messages screen-reader friendly

### Responsive Design

#### Desktop (≥1280px)
- [ ] 3-column certificate grid
- [ ] Sidebar 300px width
- [ ] All navigation visible
- [ ] Dashboard metrics 4-column grid

#### Tablet (768px - 1279px)
- [ ] 2-column certificate grid
- [ ] Collapsible sidebar
- [ ] Responsive navigation
- [ ] Dashboard metrics 2-column grid

#### Mobile (<768px)
- [ ] 1-column certificate grid
- [ ] Hamburger menu
- [ ] Touch-friendly buttons (44px min)
- [ ] Dashboard metrics 1-column grid

### Performance Validation

- [ ] Page load time < 2 seconds
- [ ] Autocomplete response < 300ms
- [ ] API response time < 500ms
- [ ] Infinite scroll smooth (no jank)
- [ ] PDF generation < 5 seconds
- [ ] Image optimization (lazy loading)

### Browser Compatibility

- [ ] Microsoft Edge (latest)
- [ ] Google Chrome (latest)
- [ ] Mozilla Firefox (latest)
- [ ] Safari (latest) - Mac only
- [ ] Mobile browsers (iOS Safari, Chrome Mobile)

---

## Test Execution Checklist

### Pre-Test Setup
- [ ] Application running on http://localhost:5101
- [ ] Database seeded with test data
- [ ] All test user accounts created and verified
- [ ] API keys generated for partner tests
- [ ] Screenshot directory created

### Test Execution
- [ ] Execute all Public user tests
- [ ] Execute all External user tests
- [ ] Execute all Analyst user tests
- [ ] Execute all Admin user tests
- [ ] Execute all E2E workflows
- [ ] Capture all screenshots
- [ ] Validate UI/UX checklist
- [ ] Test responsive breakpoints
- [ ] Test browser compatibility

### Post-Test Validation
- [ ] Review all screenshots for UI issues
- [ ] Document all bugs found
- [ ] Verify all test cases passed
- [ ] Create bug tickets for failures
- [ ] Generate test report
- [ ] Archive screenshots for client review

---

## Bug Reporting Template

```markdown
### BUG-{ID}: {Short Description}

**Severity:** Critical / High / Medium / Low
**Found in Test:** {Test ID}
**Role:** {User Role}
**Page:** {URL}

**Steps to Reproduce:**
1. Step 1
2. Step 2
3. Step 3

**Expected Result:**
{What should happen}

**Actual Result:**
{What actually happened}

**Screenshot:** {filename.png}
**Browser:** Microsoft Edge {version}
**OS:** Windows {version}

**Additional Notes:**
{Any other relevant information}
```

---

## Test Metrics

### Coverage Targets
- **Page Coverage:** 100% (all pages tested)
- **Role Coverage:** 100% (all roles tested)
- **Feature Coverage:** 100% (all features tested)
- **UI Element Coverage:** 95% (critical elements)
- **Workflow Coverage:** 100% (all business processes)

### Success Criteria
- [ ] Zero critical bugs
- [ ] < 5 high-priority bugs
- [ ] All screenshots captured
- [ ] All UI/UX validations passed
- [ ] All workflows complete successfully
- [ ] Client approval obtained

---

**Test Plan Version:** 1.0
**Last Updated:** 2025-10-05
**Next Review:** Before client meeting
