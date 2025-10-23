# GCC Database Solution - UAT Test Plan

**Release Version:** 1.0 Production Release
**UAT Window:** [To be scheduled - Recommended 5-7 days]
**Document Date:** October 22, 2025
**Status:** Ready for UAT Execution

---

## Release Overview

This User Acceptance Testing (UAT) plan covers the GCC Database Solution web application, which manages Green Circle Certified product certificates. The system includes:

- **Certificate Management** - Create, edit, view, and manage product certificates
- **Product ID Management** - Manage multiple product identifiers (ASINs, UPCs, GTINs, etc.)
- **Search & Filtering** - Advanced search with autocomplete and shareable URLs
- **User Management** - Role-based access control (Admin, Analyst, External, Public)
- **API Integration** - Partner API access for Amazon, Wayfair, and other external systems
- **Export Functionality** - CSV and Excel export capabilities
- **Dashboard & Analytics** - Certificate statistics and reporting

---

## UAT Objectives

The objectives of this UAT are to:

1. Validate that all features work as designed and meet business requirements
2. Ensure the system is user-friendly and intuitive for all user roles
3. Verify data accuracy, security, and performance
4. Identify any defects or issues before production deployment
5. Obtain stakeholder sign-off for production release

---

## Test Scenarios

### Priority Levels

- **P0 - CRITICAL**: Core functionality that must work for production release
- **P1 - HIGH**: Important features with significant business impact
- **P2 - MEDIUM**: Secondary features that enhance user experience
- **P3 - LOW**: Nice-to-have features or edge cases

---

## Section 1: Public User Scenarios

### Test Scenario 1.1: View Public Certificate Tiles (P0 - CRITICAL)

**User Role:** Public (Unauthenticated)
**Feature:** Certificate Tile View (Landing Page)
**Priority:** P0 - CRITICAL

**Scenario:** A public user visits the GCC website to view available certificates.

**Pre-conditions:**
- Application is running at http://localhost:5101 (or staging URL)
- Database contains sample certificates

**Test Steps:**

1. Open web browser (Chrome, Firefox, or Edge)
2. Navigate to: http://localhost:5101/Certificate/CertificateTileView
3. Observe the certificate tiles display on the page
4. Scroll down to see more certificates (infinite scroll)
5. Verify certificate information is visible:
   - Certificate number (e.g., "24-0366")
   - Company name (e.g., "ASSA ABLOY")
   - Product name (e.g., "Door Hardware")
   - Certification type (e.g., "FSC Certified")
   - Status badge (Active, Expired, etc.)
   - Expiration date

**Expected Results:**
- [ ] Page loads successfully within 2 seconds
- [ ] Certificate tiles display in a grid layout
- [ ] All certificate information is clearly visible and readable
- [ ] Infinite scroll loads more certificates when scrolling down
- [ ] No error messages or broken images appear
- [ ] Mobile responsive (tiles stack on smaller screens)

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 1.2: Search for Certificates (P0 - CRITICAL)

**User Role:** Public (Unauthenticated)
**Feature:** Autocomplete Search
**Priority:** P0 - CRITICAL

**Scenario:** A user searches for a specific certificate, company, or product.

**Pre-conditions:**
- User is on Certificate Tile View page

**Test Steps:**

1. Locate the search box at the top of the page
2. Type "ASSA" into the search box
3. Observe autocomplete dropdown appears with suggestions
4. Verify suggestions are categorized:
   - Companies (e.g., "ASSA ABLOY")
   - Products (e.g., "Door Hardware")
   - Certificate Numbers (e.g., "24-0366")
5. Select "ASSA ABLOY" from the dropdown
6. Verify page filters to show only ASSA ABLOY certificates
7. Clear the search to show all certificates again

**Expected Results:**
- [ ] Search box is clearly visible and accessible
- [ ] Typing displays autocomplete suggestions within 1 second
- [ ] Suggestions are organized by category (Companies, Products, Certificates)
- [ ] Selecting a suggestion filters the certificate list
- [ ] Filtered results are accurate (only matching certificates shown)
- [ ] Search can be cleared to return to full list

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 1.3: Filter Certificates by Category (P1 - HIGH)

**User Role:** Public (Unauthenticated)
**Feature:** Category Filtering
**Priority:** P1 - HIGH

**Scenario:** A user filters certificates by certification type, status, or company.

**Pre-conditions:**
- User is on Certificate Tile View page

**Test Steps:**

1. Locate the filter panel on the left side of the page
2. Expand "Certification Type" filter
3. Select "FSC Certified" checkbox
4. Verify page updates to show only FSC certificates
5. Expand "Status" filter
6. Select "Active" checkbox
7. Verify page shows only active FSC certificates
8. Click "Clear Filters" button
9. Verify all certificates are shown again

**Expected Results:**
- [ ] Filter panel is visible and easy to use
- [ ] Selecting filters updates certificate list immediately
- [ ] Multiple filters can be applied simultaneously
- [ ] Filter counts update dynamically (e.g., "FSC Certified (45)")
- [ ] Clear Filters button resets all selections
- [ ] URL updates with filter parameters (shareable link)

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 1.4: View Certificate Details (P0 - CRITICAL)

**User Role:** Public (Unauthenticated)
**Feature:** Certificate Details Page
**Priority:** P0 - CRITICAL

**Scenario:** A user clicks on a certificate to view detailed information.

**Pre-conditions:**
- User is on Certificate Tile View page

**Test Steps:**

1. Click on any certificate tile
2. Verify certificate details page opens
3. Review displayed information:
   - Certificate number
   - Company name and logo
   - Product name and description
   - Certification type
   - Effective date
   - Expiration date
   - Status
   - Product IDs (ASIN, UPC, GTIN, etc.)
4. Click "Download PDF" button
5. Verify PDF file downloads to your computer
6. Open the PDF and verify it contains certificate information
7. Click "Back to Search" or browser back button
8. Verify you return to the certificate tile view

**Expected Results:**
- [ ] Certificate details page loads quickly (< 2 seconds)
- [ ] All certificate information is displayed accurately
- [ ] Product IDs are clearly listed (if available)
- [ ] Download PDF button generates a valid PDF file
- [ ] PDF contains professional formatting and all certificate data
- [ ] Navigation back to search works properly

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 1.5: Shareable Search URL (P1 - HIGH)

**User Role:** Public (Unauthenticated)
**Feature:** Shareable URLs
**Priority:** P1 - HIGH

**Scenario:** A user creates a filtered search and shares the URL with a colleague.

**Pre-conditions:**
- User is on Certificate Tile View page

**Test Steps:**

1. Apply multiple filters:
   - Company: "ASSA ABLOY"
   - Status: "Active"
   - Certification Type: "FSC Certified"
2. Copy the URL from the browser address bar
3. Open a new browser tab (or incognito window)
4. Paste the copied URL and press Enter
5. Verify the new tab loads with the same filters applied
6. Verify certificate results match the original filtered view

**Expected Results:**
- [ ] URL updates as filters are applied
- [ ] URL contains filter parameters in query string
- [ ] Pasting URL in new tab applies the same filters
- [ ] Filtered results are identical in both tabs
- [ ] URL can be shared via email, chat, etc.

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

## Section 2: Analyst User Scenarios

### Test Scenario 2.1: Analyst Login (P0 - CRITICAL)

**User Role:** Analyst
**Feature:** Authentication
**Priority:** P0 - CRITICAL

**Scenario:** An analyst user logs into the system to manage certificates.

**Pre-conditions:**
- Analyst account exists in the system (see UAT User Credentials document)

**Test Steps:**

1. Navigate to http://localhost:5101/Account/Login
2. Enter analyst credentials:
   - Email: analyst@greencirclecertified.com
   - Password: Analyst123!
3. Click "Login" button
4. Verify redirect to Dashboard or Certificate Tile View (Internal)
5. Verify user name is displayed in top navigation bar
6. Verify "Logout" option is available

**Expected Results:**
- [ ] Login page loads successfully
- [ ] Valid credentials are accepted
- [ ] User is redirected to internal view after login
- [ ] User name is displayed in navigation
- [ ] Session is maintained (page refresh doesn't log out)

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 2.2: Create New Certificate (P0 - CRITICAL)

**User Role:** Analyst
**Feature:** Certificate Creation
**Priority:** P0 - CRITICAL

**Scenario:** An analyst creates a new product certificate.

**Pre-conditions:**
- Analyst user is logged in
- User is on Certificate Tile View (Internal)

**Test Steps:**

1. Click "Add Certificate" button
2. Verify Create Certificate form opens
3. Fill in required fields:
   - Certificate Number: "UAT-2025-001"
   - Company: Select "ASSA ABLOY" from dropdown
   - Product: Select existing product or create new
   - Certification Type: Select "FSC Certified"
   - Effective Date: Select today's date
   - Expiration Date: Select date 1 year from today
   - Status: Select "Active"
4. Click "Save" button
5. Verify success message appears
6. Verify redirected to certificate details page or tile view
7. Search for "UAT-2025-001" to confirm it was created

**Expected Results:**
- [ ] Add Certificate button is visible and clickable
- [ ] Create form loads with all required fields
- [ ] Company and Product dropdowns are populated
- [ ] Date pickers work correctly
- [ ] Form validates required fields (error if empty)
- [ ] Certificate is created successfully
- [ ] Success message confirms creation
- [ ] New certificate appears in search results

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 2.3: Edit Existing Certificate (P0 - CRITICAL)

**User Role:** Analyst
**Feature:** Certificate Editing
**Priority:** P0 - CRITICAL

**Scenario:** An analyst updates information on an existing certificate.

**Pre-conditions:**
- Analyst user is logged in
- Certificate "UAT-2025-001" exists (created in previous test)

**Test Steps:**

1. Search for certificate "UAT-2025-001"
2. Click on the certificate tile
3. Click "Edit" button on certificate details page
4. Verify Edit Certificate form loads with existing data pre-filled
5. Update the following:
   - Product: Change to a different product
   - Expiration Date: Change to a different date
6. Click "Save Changes" button
7. Verify success message appears
8. Verify updated information is displayed on details page

**Expected Results:**
- [ ] Edit button is visible (Analyst has permission)
- [ ] Edit form loads with current certificate data
- [ ] All fields are editable
- [ ] Changes are saved successfully
- [ ] Success message confirms update
- [ ] Updated data is displayed correctly

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 2.4: Product ID Management - Add Product IDs (P1 - HIGH)

**User Role:** Analyst
**Feature:** Product ID Manager
**Priority:** P1 - HIGH

**Scenario:** An analyst adds multiple product IDs (ASINs, UPCs) to a certificate.

**Pre-conditions:**
- Analyst user is logged in
- Certificate "UAT-2025-001" exists

**Test Steps:**

1. Navigate to certificate "UAT-2025-001" details page
2. Click "Edit" button
3. Locate "Product ID Manager" section on the edit form
4. Click "Add Product ID" button
5. Enter the following product IDs one at a time:
   - ASIN: B08ABC1234
   - UPC: 123456789012
   - GTIN: 01234567890123
6. Verify each product ID is added to the list
7. Click "Save Changes" to save the certificate
8. Verify product IDs are displayed on certificate details page

**Expected Results:**
- [ ] Product ID Manager section is visible on edit form
- [ ] Add Product ID button works correctly
- [ ] Product ID type can be selected (ASIN, UPC, GTIN, etc.)
- [ ] Product ID value can be entered
- [ ] Multiple product IDs can be added
- [ ] Product IDs are saved with the certificate
- [ ] Product IDs display on certificate details page

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 2.5: Product ID Manager - Bulk Import CSV (P1 - HIGH)

**User Role:** Analyst
**Feature:** Product ID Bulk Import
**Priority:** P1 - HIGH

**Scenario:** An analyst imports 100+ ASINs from a CSV file.

**Pre-conditions:**
- Analyst user is logged in
- Certificate "UAT-2025-001" exists
- CSV file with ASINs is prepared (or use template)

**Test Steps:**

1. Navigate to certificate "UAT-2025-001" edit page
2. In Product ID Manager section, click "Import from CSV" button
3. Download the CSV template (if needed)
4. Upload CSV file with the following format:
   ```
   ProductIdType,ProductIdValue
   ASIN,B08XYZ1111
   ASIN,B08XYZ2222
   ASIN,B08XYZ3333
   ```
5. Click "Upload" or "Import" button
6. Verify import progress indicator appears
7. Verify success message shows number of imported IDs
8. Verify imported product IDs appear in the list
9. Click "Save Changes"
10. Verify all imported product IDs are saved

**Expected Results:**
- [ ] Import from CSV button is visible
- [ ] CSV template can be downloaded
- [ ] CSV file upload works correctly
- [ ] Import validates CSV format
- [ ] Progress indicator shows during import
- [ ] Success message displays import count
- [ ] All valid product IDs are imported
- [ ] Invalid entries show error messages
- [ ] Imported IDs are saved with certificate

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 2.6: Export Certificates to CSV (P1 - HIGH)

**User Role:** Analyst
**Feature:** Certificate Export
**Priority:** P1 - HIGH

**Scenario:** An analyst exports filtered certificate data to CSV format.

**Pre-conditions:**
- Analyst user is logged in
- Certificates exist in the database

**Test Steps:**

1. Navigate to Certificate Tile View (Internal)
2. Apply filters:
   - Company: "ASSA ABLOY"
   - Status: "Active"
3. Click "Export" button
4. Select "Export to CSV" option
5. Verify CSV file downloads to your computer
6. Open the CSV file in Excel or text editor
7. Verify the following:
   - CSV contains only filtered certificates (ASSA ABLOY, Active)
   - All columns are present (Certificate Number, Company, Product, etc.)
   - Data is formatted correctly (no errors, readable)
   - Special characters are handled properly

**Expected Results:**
- [ ] Export button is visible
- [ ] Export to CSV option is available
- [ ] CSV file downloads successfully
- [ ] CSV contains filtered data (not all certificates)
- [ ] All certificate fields are included in CSV
- [ ] Data is accurate and properly formatted
- [ ] File can be opened in Excel

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

## Section 3: Admin User Scenarios

### Test Scenario 3.1: Admin Login and Dashboard Access (P0 - CRITICAL)

**User Role:** Admin
**Feature:** Admin Authentication & Dashboard
**Priority:** P0 - CRITICAL

**Scenario:** An admin user logs in and accesses the admin dashboard.

**Pre-conditions:**
- Admin account exists in the system

**Test Steps:**

1. Navigate to http://localhost:5101/Account/Login
2. Enter admin credentials:
   - Email: admin@greencirclecertified.com
   - Password: Admin123!
3. Click "Login" button
4. Verify redirect to Dashboard
5. Verify Dashboard displays:
   - Total certificates count
   - Active certificates count
   - Expiring certificates count
   - Recent activity
6. Verify "Admin" menu is visible in navigation
7. Click "Admin" menu and verify options:
   - User Management
   - Role Management
   - Tag Management
   - Product Management
   - Company Management
   - Certificate Types
   - Partners

**Expected Results:**
- [ ] Admin login successful
- [ ] Dashboard loads with statistics
- [ ] Statistics display accurate data
- [ ] Admin menu is visible (not visible to Analyst)
- [ ] All admin menu options are accessible

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 3.2: User Management - Create New User (P0 - CRITICAL)

**User Role:** Admin
**Feature:** User Management
**Priority:** P0 - CRITICAL

**Scenario:** An admin creates a new user account.

**Pre-conditions:**
- Admin user is logged in

**Test Steps:**

1. Navigate to Admin > User Management
2. Verify user list displays existing users
3. Click "Add New User" button
4. Fill in user details:
   - Name: "UAT Test User"
   - Email: "uattest@greencirclecertified.com"
   - Company: Select "ASSA ABLOY"
   - Role: Select "Analyst"
   - Password: "TestUser123!"
   - Require Password Reset: Check this option
5. Click "Save" button
6. Verify success message appears
7. Verify new user appears in user list
8. Verify user can log in with provided credentials

**Expected Results:**
- [ ] Add New User button is visible
- [ ] User creation form loads correctly
- [ ] All fields are editable
- [ ] Role dropdown is populated
- [ ] Company dropdown is populated
- [ ] User is created successfully
- [ ] User appears in user list
- [ ] New user can log in

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 3.3: User Management - Edit User Role (P1 - HIGH)

**User Role:** Admin
**Feature:** User Role Management
**Priority:** P1 - HIGH

**Scenario:** An admin changes a user's role from Analyst to External.

**Pre-conditions:**
- Admin user is logged in
- User "UAT Test User" exists (created in previous test)

**Test Steps:**

1. Navigate to Admin > User Management
2. Find "UAT Test User" in the user list
3. Click "Edit" button for this user
4. Change role from "Analyst" to "External"
5. Click "Save Changes" button
6. Verify success message appears
7. Verify role is updated in user list
8. Log out and log in as "UAT Test User"
9. Verify user now has External role permissions (read-only access)

**Expected Results:**
- [ ] Edit button works for user
- [ ] Edit form loads with current user data
- [ ] Role can be changed via dropdown
- [ ] Changes are saved successfully
- [ ] Updated role displays in user list
- [ ] User permissions reflect new role

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 3.4: User Management - Delete User (P1 - HIGH)

**User Role:** Admin
**Feature:** User Deletion
**Priority:** P1 - HIGH

**Scenario:** An admin deletes a user account.

**Pre-conditions:**
- Admin user is logged in
- User "UAT Test User" exists

**Test Steps:**

1. Navigate to Admin > User Management
2. Find "UAT Test User" in the user list
3. Click "Delete" button (or Actions dropdown > Delete)
4. Verify confirmation modal appears with warning message
5. Click "Delete" button in the modal
6. Verify success message appears
7. Verify user is removed from the user list
8. Try to log in as the deleted user
9. Verify login fails with appropriate error message

**Expected Results:**
- [ ] Delete button is visible
- [ ] Confirmation modal appears before deletion
- [ ] Modal warns about irreversible action
- [ ] User is deleted successfully after confirmation
- [ ] User is removed from user list
- [ ] Deleted user cannot log in
- [ ] Appropriate error message for deleted user login

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 3.5: Delete Certificate (Admin Only) (P1 - HIGH)

**User Role:** Admin
**Feature:** Certificate Deletion
**Priority:** P1 - HIGH

**Scenario:** An admin deletes a certificate (soft delete).

**Pre-conditions:**
- Admin user is logged in
- Certificate "UAT-2025-001" exists

**Test Steps:**

1. Search for certificate "UAT-2025-001"
2. Open certificate details page
3. Click "Edit" button
4. Verify "Delete Certificate" button is visible (Admin only)
5. Click "Delete Certificate" button
6. Verify confirmation modal appears
7. Click "Delete" in the modal
8. Verify success message appears
9. Verify redirected to certificate tile view
10. Search for "UAT-2025-001"
11. Verify certificate no longer appears in search results
12. (Optional) Verify certificate is soft-deleted in database (IsDeleted = true)

**Expected Results:**
- [ ] Delete button visible only to Admin (not Analyst)
- [ ] Confirmation modal appears before deletion
- [ ] Certificate is deleted successfully
- [ ] Success message confirms deletion
- [ ] Certificate is removed from public view
- [ ] Certificate is soft-deleted (not permanently removed from database)

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 3.6: Tag Management (P2 - MEDIUM)

**User Role:** Admin
**Feature:** Tag Management
**Priority:** P2 - MEDIUM

**Scenario:** An admin creates, edits, and deletes tags.

**Pre-conditions:**
- Admin user is logged in

**Test Steps:**

1. Navigate to Admin > Tag Management
2. Verify existing tags are displayed
3. Click "Add New Tag" button
4. Enter tag details:
   - Name: "UAT Test Tag"
   - Tag Type: "Internal"
   - Description: "Test tag for UAT"
5. Click "Save" button
6. Verify tag is created and appears in list
7. Click "Edit" for the new tag
8. Change Tag Type to "External"
9. Click "Save Changes"
10. Verify tag is updated
11. Click "Delete" for the tag
12. Confirm deletion in modal
13. Verify tag is removed from list

**Expected Results:**
- [ ] Tag Management page loads successfully
- [ ] Existing tags are displayed
- [ ] Add New Tag button works
- [ ] Tag is created successfully
- [ ] Tag can be edited
- [ ] Tag can be deleted with confirmation
- [ ] All changes are saved correctly

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

## Section 4: External Partner User Scenarios

### Test Scenario 4.1: External User Login (P0 - CRITICAL)

**User Role:** External (Partner)
**Feature:** External Authentication
**Priority:** P0 - CRITICAL

**Scenario:** An external partner (e.g., Amazon) logs in to view their certificates.

**Pre-conditions:**
- External user account exists

**Test Steps:**

1. Navigate to http://localhost:5101/Account/Login
2. Enter external user credentials:
   - Email: partner@amazon.com
   - Password: Partner123!
3. Click "Login" button
4. Verify redirect to Certificate Tile View (External)
5. Verify only certificates associated with Amazon partner are displayed
6. Verify user name and company are displayed in navigation
7. Verify "Edit" and "Delete" buttons are NOT visible on certificates

**Expected Results:**
- [ ] External user login successful
- [ ] User sees only partner-specific certificates (Amazon filter applied)
- [ ] User has read-only access (no create, edit, delete)
- [ ] User interface is simplified for external users
- [ ] No admin menu options visible

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 4.2: External User - View and Download Certificates (P0 - CRITICAL)

**User Role:** External (Partner)
**Feature:** Certificate Viewing and Download
**Priority:** P0 - CRITICAL

**Scenario:** An external user views and downloads their certificates.

**Pre-conditions:**
- External user is logged in (partner@amazon.com)

**Test Steps:**

1. Verify certificate tile view shows only Amazon-associated certificates
2. Click on a certificate tile
3. Verify certificate details page opens
4. Review certificate information
5. Click "Download PDF" button
6. Verify PDF downloads successfully
7. Open PDF and verify it contains certificate data
8. Verify "Edit" button is NOT visible (read-only access)

**Expected Results:**
- [ ] External user sees only their company's certificates
- [ ] Certificate details page loads correctly
- [ ] All certificate information is visible
- [ ] Download PDF button works
- [ ] PDF is generated successfully
- [ ] No edit or delete options are available

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 4.3: External User - Export Certificates (P1 - HIGH)

**User Role:** External (Partner)
**Feature:** Certificate Export
**Priority:** P1 - HIGH

**Scenario:** An external user exports their certificates to CSV.

**Pre-conditions:**
- External user is logged in

**Test Steps:**

1. Navigate to Certificate Tile View (External)
2. Verify only partner-specific certificates are visible
3. Click "Export" button
4. Select "Export to CSV"
5. Verify CSV file downloads
6. Open CSV file
7. Verify CSV contains only partner's certificates
8. Verify all required columns are present

**Expected Results:**
- [ ] Export button is visible to external users
- [ ] Export to CSV option works
- [ ] CSV contains only partner-specific certificates
- [ ] CSV is formatted correctly
- [ ] External user cannot export other companies' data

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

## Section 5: API Integration Scenarios

### Test Scenario 5.1: API - Authenticate with API Key (P0 - CRITICAL)

**User Role:** API Consumer (Developer/Partner)
**Feature:** API Authentication
**Priority:** P0 - CRITICAL

**Scenario:** An API consumer authenticates using an API key.

**Pre-conditions:**
- API key exists for testing (see UAT User Credentials document)
- API testing tool (Postman, Insomnia, or curl) is available

**Test Steps:**

1. Open API testing tool (e.g., Postman)
2. Create a new GET request to:
   ```
   http://localhost:5101/api/CertificateTileExternal/tiles
   ```
3. Add header:
   - Key: X-API-Key
   - Value: [Use API key from credentials document]
4. Send the request
5. Verify response:
   - Status: 200 OK
   - Body: JSON array of certificates
6. Remove the X-API-Key header
7. Send request again
8. Verify response:
   - Status: 401 Unauthorized

**Expected Results:**
- [ ] API request with valid API key returns 200 OK
- [ ] Response contains JSON array of certificates
- [ ] API request without API key returns 401 Unauthorized
- [ ] Error message is clear and helpful

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 5.2: API - Get Partner-Specific Certificates (P0 - CRITICAL)

**User Role:** API Consumer (Amazon Partner)
**Feature:** Partner Filtering
**Priority:** P0 - CRITICAL

**Scenario:** Amazon partner API returns only Amazon-associated certificates.

**Pre-conditions:**
- Amazon partner API key is configured

**Test Steps:**

1. Open API testing tool
2. Create GET request to:
   ```
   http://localhost:5101/api/CertificateTileExternal/tiles
   ```
3. Add Amazon partner API key in X-API-Key header
4. Send request
5. Review response JSON
6. Verify all returned certificates have Partner field = "Amazon" (or are associated with Amazon)
7. Verify certificates from other partners (Wayfair, etc.) are NOT included

**Expected Results:**
- [ ] API returns only Amazon-associated certificates
- [ ] Response does not include other partners' certificates
- [ ] JSON structure is correct
- [ ] All required certificate fields are present
- [ ] Response time is acceptable (< 2 seconds)

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 5.3: API - Rate Limiting (P2 - MEDIUM)

**User Role:** API Consumer
**Feature:** API Rate Limiting
**Priority:** P2 - MEDIUM

**Scenario:** API enforces rate limiting to prevent abuse.

**Pre-conditions:**
- API key is configured
- Rate limit is set (e.g., 1000 requests per hour)

**Test Steps:**

1. Use API testing tool or script
2. Make 1000 API requests rapidly (within a few minutes)
3. Verify first 1000 requests succeed (200 OK)
4. Make the 1001st request
5. Verify response:
   - Status: 429 Too Many Requests
   - Body: Error message explaining rate limit exceeded
6. Wait for rate limit window to reset (1 hour)
7. Make another request
8. Verify request succeeds (200 OK)

**Expected Results:**
- [ ] API allows up to 1000 requests per hour
- [ ] Request 1001 returns 429 Too Many Requests
- [ ] Error message explains rate limit
- [ ] Rate limit resets after time window
- [ ] Requests succeed after reset

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

## Section 6: Performance & Load Testing

### Test Scenario 6.1: Page Load Performance (P1 - HIGH)

**User Role:** Any
**Feature:** Application Performance
**Priority:** P1 - HIGH

**Scenario:** Verify key pages load within acceptable time limits.

**Pre-conditions:**
- Application is running
- Database contains 180+ certificates

**Test Steps:**

1. Clear browser cache
2. Navigate to Certificate Tile View
3. Measure page load time (use browser Developer Tools > Network tab)
4. Record load time
5. Repeat for other pages:
   - Certificate Details
   - Dashboard
   - Admin > User Management
6. Verify all pages load within target times:
   - Certificate Tile View: < 2 seconds
   - Certificate Details: < 1.5 seconds
   - Dashboard: < 2 seconds
   - Admin pages: < 2 seconds

**Expected Results:**
- [ ] Certificate Tile View loads in < 2 seconds
- [ ] Certificate Details loads in < 1.5 seconds
- [ ] Dashboard loads in < 2 seconds
- [ ] Admin pages load in < 2 seconds
- [ ] No JavaScript errors in browser console
- [ ] Images and resources load properly

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 6.2: Infinite Scroll Performance (P1 - HIGH)

**User Role:** Public
**Feature:** Infinite Scroll
**Priority:** P1 - HIGH

**Scenario:** Verify infinite scroll loads additional certificates smoothly.

**Pre-conditions:**
- Certificate Tile View is loaded
- Database contains 180+ certificates

**Test Steps:**

1. Open Certificate Tile View
2. Observe initial load (first 20-30 certificates)
3. Scroll down to bottom of page
4. Verify loading indicator appears
5. Measure time for next batch to load
6. Verify next batch (20-30 more certificates) loads within 1 second
7. Continue scrolling and loading until all certificates are visible
8. Verify no errors or slowdowns

**Expected Results:**
- [ ] Initial batch loads quickly (< 2 seconds)
- [ ] Loading indicator appears when scrolling to bottom
- [ ] Next batch loads within 1 second
- [ ] Scrolling is smooth (no lag or freezing)
- [ ] All 180+ certificates load successfully
- [ ] No duplicate certificates appear

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

## Section 7: Security & Error Handling

### Test Scenario 7.1: Unauthorized Access Prevention (P0 - CRITICAL)

**User Role:** Unauthenticated User
**Feature:** Authorization & Access Control
**Priority:** P0 - CRITICAL

**Scenario:** Verify unauthenticated users cannot access protected pages.

**Pre-conditions:**
- User is NOT logged in (logout if necessary)

**Test Steps:**

1. Try to access protected pages directly via URL:
   - http://localhost:5101/Certificate/Create
   - http://localhost:5101/Certificate/Edit/1
   - http://localhost:5101/Admin/UserManagement
   - http://localhost:5101/Dashboard
2. For each page, verify:
   - Page redirects to /Account/Login
   - User is prompted to log in
   - After login, user is redirected to intended page (if authorized)

**Expected Results:**
- [ ] All protected pages redirect to login page
- [ ] User cannot access pages without authentication
- [ ] After login, user is redirected to originally requested page
- [ ] Unauthorized users (e.g., Analyst accessing Admin pages) see 403 Forbidden

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 7.2: Invalid Certificate Number Handling (P2 - MEDIUM)

**User Role:** Any
**Feature:** Error Handling
**Priority:** P2 - MEDIUM

**Scenario:** User tries to view a certificate that doesn't exist.

**Pre-conditions:**
- User is on Certificate Tile View

**Test Steps:**

1. Manually navigate to a non-existent certificate:
   ```
   http://localhost:5101/Certificate/Details/99-9999
   ```
2. Verify error page is displayed (404 Not Found)
3. Verify error message is user-friendly:
   - "Certificate not found" or similar message
   - Suggestion to return to search
4. Click "Back to Search" link
5. Verify user is redirected to Certificate Tile View

**Expected Results:**
- [ ] 404 error page is displayed
- [ ] Error message is clear and user-friendly
- [ ] No technical error details exposed to user
- [ ] Navigation link back to search is provided
- [ ] Application does not crash

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

### Test Scenario 7.3: Form Validation (P1 - HIGH)

**User Role:** Analyst
**Feature:** Input Validation
**Priority:** P1 - HIGH

**Scenario:** Verify form validation prevents invalid data entry.

**Pre-conditions:**
- Analyst user is logged in
- User is on Create Certificate form

**Test Steps:**

1. Click "Save" button without filling any fields
2. Verify validation errors appear for required fields:
   - Certificate Number
   - Company
   - Product
   - Effective Date
   - Expiration Date
3. Fill in Certificate Number: "TEST-001"
4. Fill in Effective Date: "2025-10-22"
5. Fill in Expiration Date: "2025-01-01" (earlier than effective date)
6. Click "Save" button
7. Verify validation error: "Expiration date must be after effective date"
8. Correct Expiration Date to "2026-10-22"
9. Fill in remaining required fields
10. Click "Save"
11. Verify certificate is created successfully

**Expected Results:**
- [ ] Form validates required fields before submission
- [ ] Validation errors are displayed clearly
- [ ] Date logic is validated (expiration > effective)
- [ ] User cannot submit invalid data
- [ ] Valid data is accepted and saved

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

## Section 8: Mobile Responsiveness

### Test Scenario 8.1: Mobile View - Certificate Tiles (P2 - MEDIUM)

**User Role:** Public
**Feature:** Mobile Responsive Design
**Priority:** P2 - MEDIUM

**Scenario:** Verify certificate tile view works on mobile devices.

**Pre-conditions:**
- Application is running
- Mobile device or browser responsive mode is available

**Test Steps:**

1. Open browser Developer Tools (F12)
2. Enable device toolbar (mobile emulation)
3. Select iPhone 12 (or similar mobile device)
4. Navigate to Certificate Tile View
5. Verify layout adjustments:
   - Tiles stack vertically (single column)
   - Filter panel collapses into expandable menu
   - Navigation menu becomes hamburger menu
   - Text is readable (16px minimum font size)
   - Buttons are touch-friendly (44px minimum)
6. Try scrolling, filtering, and searching
7. Verify all functionality works on mobile
8. Test on tablet size (iPad)
9. Verify layout adapts appropriately

**Expected Results:**
- [ ] Layout is responsive and adapts to screen size
- [ ] Tiles stack vertically on mobile
- [ ] Filter panel is accessible via hamburger menu
- [ ] All text is readable (no tiny fonts)
- [ ] Buttons are large enough to tap easily
- [ ] Infinite scroll works on mobile
- [ ] Search functionality works on mobile
- [ ] No horizontal scrolling required

**Actual Results:** _______________________________________________

**Status:** [ ] Not Tested | [ ] Pass | [ ] Fail

**Notes/Comments:** _______________________________________________

---

## UAT Sign-Off

### Test Execution Summary

**Total Test Scenarios:** 33
**Scenarios Passed:** ________
**Scenarios Failed:** ________
**Scenarios Not Tested:** ________

**Pass Rate:** ________%

---

### Critical Issues Found

| Issue # | Scenario | Description | Severity | Status |
|---------|----------|-------------|----------|--------|
| 1 | | | | |
| 2 | | | | |
| 3 | | | | |

---

### Go/No-Go Decision

**Production Readiness Criteria:**

- [ ] All P0 (Critical) test scenarios pass (100%)
- [ ] At least 90% of P1 (High) test scenarios pass
- [ ] No critical or high-severity defects remain open
- [ ] Performance targets are met
- [ ] Security vulnerabilities are addressed
- [ ] Stakeholder sign-off obtained

**Decision:** [ ] GO - Approved for Production | [ ] NO-GO - Additional work required

---

### Stakeholder Sign-Off

**Tested By:**

| Name | Role | Signature | Date |
|------|------|-----------|------|
| | | | |
| | | | |
| | | | |

**Approved By:**

| Name | Title | Signature | Date |
|------|-------|-----------|------|
| | Product Owner | | |
| | Technical Lead | | |
| | QA Manager | | |

---

**UAT Test Plan Version:** 1.0
**Document Created:** October 22, 2025
**Last Updated:** October 22, 2025
**Next Review:** [After UAT Execution]

---

## Appendix

### UAT Environment Details

- **Application URL:** http://localhost:5101 (or staging URL)
- **Database:** GCC_UAT (isolated test database)
- **Test Data:** Pre-seeded with sample certificates
- **Browser Requirements:** Chrome 90+, Firefox 88+, Edge 90+, Safari 14+

### Test Data Requirements

- **Certificates:** 180+ sample certificates across various companies and types
- **Companies:** ASSA ABLOY, Wayfair, Amazon, and others
- **Users:** Admin, Analyst, External (see UAT User Credentials document)
- **Product IDs:** Sample ASINs, UPCs, GTINs for testing

### Support Contacts

- **Technical Support:** [Contact information]
- **UAT Coordinator:** [Contact information]
- **Project Manager:** [Contact information]

---

**END OF UAT TEST PLAN**
