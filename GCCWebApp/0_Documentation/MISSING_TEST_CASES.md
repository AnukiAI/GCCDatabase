# GCC Web Application - Missing Test Cases

**Date:** October 15, 2025
**Status:** Identified during system testing
**Priority:** HIGH - Required for Production Readiness

## Overview

This document outlines test cases that should be created to ensure the GCC Web Application functions correctly and is ready for production deployment. These test cases were identified during comprehensive system testing when database access issues prevented full functional testing.

## Test Categories

### 1. Database Connection & Recovery Tests

#### Test Case 1.1: SQL Server Connection with Valid Credentials
- **Test File:** `GCCWebApp.Tests/Database/ConnectionTests.cs`
- **Description:** Verify successful connection to SQL Server with valid credentials
- **Test Steps:**
  1. Create ApplicationDbContext with SQL Server connection string
  2. Attempt to open connection
  3. Verify CanConnect() returns true
- **Expected Result:** Connection successful
- **Priority:** HIGH

#### Test Case 1.2: SQL Server Logon Trigger Detection
- **Test File:** `GCCWebApp.Tests/Database/ConnectionTests.cs`
- **Description:** Detect and handle SQL Server logon trigger errors
- **Test Steps:**
  1. Attempt connection to SQL Server with logon trigger
  2. Catch SqlException with Error Number 17892
  3. Verify error message contains "trigger execution"
  4. Verify application logs appropriate error
- **Expected Result:** Error caught and logged, application continues in degraded mode
- **Priority:** HIGH

#### Test Case 1.3: Database Connection Retry Logic
- **Test File:** `GCCWebApp.Tests/Database/RetryTests.cs`
- **Description:** Verify retry logic with exponential backoff
- **Test Steps:**
  1. Configure SqlServerRetryingExecutionStrategy with 5 retries
  2. Simulate transient connection failures
  3. Verify retry attempts with increasing delays
  4. Verify success after transient failure resolved
- **Expected Result:** Connection succeeds after retries
- **Priority:** MEDIUM

#### Test Case 1.4: Database Does Not Exist
- **Test File:** `GCCWebApp.Tests/Database/ConnectionTests.cs`
- **Description:** Handle case where database doesn't exist
- **Test Steps:**
  1. Connection string points to non-existent database
  2. Attempt EnsureCreatedAsync()
  3. Verify database is created
  4. Verify migrations applied
- **Expected Result:** Database created automatically in development
- **Priority:** HIGH

#### Test Case 1.5: Migration Failure Recovery
- **Test File:** `GCCWebApp.Tests/Database/MigrationTests.cs`
- **Description:** Handle failed migrations gracefully
- **Test Steps:**
  1. Simulate migration failure (syntax error in migration)
  2. Verify error is caught and logged
  3. Verify application can continue with existing schema
- **Expected Result:** Application doesn't crash, error logged
- **Priority:** MEDIUM

---

### 2. Configuration Validation Tests

#### Test Case 2.1: Database Provider Detection - SQL Server
- **Test File:** `GCCWebApp.Tests/Configuration/DatabaseProviderTests.cs`
- **Description:** Verify SQL Server connection strings are detected correctly
- **Test Scenarios:**
  - `Server=tcp:hostname,port;Database=db;User Id=user;Password=pass`
  - `Server=(localdb)\\mssqllocaldb;Database=db;Trusted_Connection=true`
  - `Server=sqlexpress;Database=db;Integrated Security=true`
  - `Server=server.database.windows.net;Database=db`
- **Expected Result:** All recognized as SQL Server
- **Priority:** MEDIUM

#### Test Case 2.2: Database Provider Detection - SQLite
- **Test File:** `GCCWebApp.Tests/Configuration/DatabaseProviderTests.cs`
- **Description:** Verify SQLite connection strings are detected correctly
- **Test Scenarios:**
  - `Data Source=database.db`
  - `Data Source=:memory:`
  - `Data Source=/path/to/database.db`
- **Expected Result:** All recognized as SQLite
- **Priority:** MEDIUM

#### Test Case 2.3: Invalid Connection String Format
- **Test File:** `GCCWebApp.Tests/Configuration/DatabaseProviderTests.cs`
- **Description:** Handle invalid connection strings
- **Test Steps:**
  1. Provide malformed connection string
  2. Verify exception is thrown
  3. Verify descriptive error message
- **Expected Result:** Appropriate exception with helpful message
- **Priority:** MEDIUM

#### Test Case 2.4: JWT Secret Validation
- **Test File:** `GCCWebApp.Tests/Configuration/SecurityConfigurationTests.cs`
- **Description:** Verify JWT secret key requirements
- **Test Scenarios:**
  - Missing JWT secret (should throw exception)
  - JWT secret < 64 characters (should throw exception)
  - JWT secret = 64 characters (should succeed)
  - JWT secret > 64 characters (should succeed)
- **Expected Result:** Appropriate validation and error messages
- **Priority:** HIGH

#### Test Case 2.5: Azure AD B2C Configuration
- **Test File:** `GCCWebApp.Tests/Configuration/AuthenticationConfigurationTests.cs`
- **Description:** Verify Azure AD B2C configuration detection
- **Test Scenarios:**
  - All B2C settings configured (should use Azure AD)
  - Missing TenantId (should fall back to cookie auth)
  - Missing ClientId (should fall back to cookie auth)
  - Placeholder values like `{tenant-id}` (should fall back)
- **Expected Result:** Correct authentication provider selected
- **Priority:** MEDIUM

---

### 3. Data Seeding & Initialization Tests

#### Test Case 3.1: Seed Data with Empty Database
- **Test File:** `GCCWebApp.Tests/Data/SeedDataTests.cs`
- **Description:** Verify complete data seeding
- **Test Steps:**
  1. Start with empty database
  2. Run SeedData.InitializeAsync()
  3. Verify all entities created (Companies, Products, Certificates, Users, Roles, etc.)
  4. Verify correct counts and relationships
- **Expected Result:** All seed data created successfully
- **Priority:** HIGH
- **Expected Counts:**
  - Companies: 25+
  - Products: 30+
  - Certificates: 180+ (primary + generated)
  - Users: 14
  - Roles: 4 (Admin, Analyst, External, Public)
  - Permissions: 6
  - Tags: 25
  - CertificationTypes: 5
  - ProductCategories: 6

#### Test Case 3.2: Seed Data Idempotency
- **Test File:** `GCCWebApp.Tests/Data/SeedDataTests.cs`
- **Description:** Verify seeding doesn't duplicate data
- **Test Steps:**
  1. Run SeedData.InitializeAsync() twice
  2. Verify counts remain the same
  3. Verify no duplicate entities created
- **Expected Result:** Second run exits early, no duplicates
- **Priority:** HIGH

#### Test Case 3.3: Certificate Foreign Key Relationships
- **Test File:** `GCCWebApp.Tests/Data/SeedDataTests.cs`
- **Description:** Verify all certificate relationships are valid
- **Test Steps:**
  1. Seed database
  2. Load all certificates with related entities
  3. Verify each certificate has valid CompanyId, ProductId, CertificationTypeId
  4. Verify navigation properties load correctly
- **Expected Result:** All relationships valid, no orphaned records
- **Priority:** HIGH

#### Test Case 3.4: User Role Assignments
- **Test File:** `GCCWebApp.Tests/Data/SeedDataTests.cs`
- **Description:** Verify users assigned correct roles
- **Test Steps:**
  1. Seed database
  2. Load all users with roles
  3. Verify admin users have Admin role
  4. Verify analyst users have Analyst role
  5. Verify external users have External role
- **Expected Result:** All users have appropriate roles
- **Priority:** MEDIUM

#### Test Case 3.5: API Key Seeding
- **Test File:** `GCCWebApp.Tests/Data/SeedDataTests.cs`
- **Description:** Verify API keys created correctly
- **Test Steps:**
  1. Seed database
  2. Verify at least 3 API keys created
  3. Verify keys have correct format (gcc_*)
  4. Verify expiration dates set correctly
  5. Verify scopes assigned
- **Expected Result:** API keys valid and functional
- **Priority:** MEDIUM

---

### 4. Certificate Data Loading Tests

#### Test Case 4.1: Certificate Tile View Loads Data
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/CertificateTileViewTests.cs`
- **Description:** Verify certificate tiles display correctly
- **Test Steps:**
  1. Seed database with certificates
  2. Navigate to /Certificate/CertificateTileView
  3. Verify tiles render
  4. Verify certificate count matches database
  5. Verify tile data (number, company, product, status)
- **Expected Result:** All certificates display correctly
- **Priority:** HIGH

#### Test Case 4.2: Certificate Filtering by Company
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/FilteringTests.cs`
- **Description:** Filter certificates by company
- **Test Steps:**
  1. Load tile view
  2. Select company filter (e.g., "ASSA ABLOY")
  3. Verify only certificates for that company display
  4. Verify certificate count updates
  5. Clear filter and verify all certificates return
- **Expected Result:** Filtering works correctly
- **Priority:** HIGH

#### Test Case 4.3: Certificate Filtering by Status
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/FilteringTests.cs`
- **Description:** Filter certificates by status
- **Test Scenarios:**
  - Filter by "active" status
  - Filter by "expired" status
  - Filter by "draft" status
  - Filter by "pending" status
  - Filter by "suspended" status
- **Expected Result:** Only matching certificates display
- **Priority:** HIGH

#### Test Case 4.4: Infinite Scroll Implementation
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/InfiniteScrollTests.cs`
- **Description:** Verify infinite scroll loads more certificates
- **Test Steps:**
  1. Load tile view (should show first batch, e.g., 20 certificates)
  2. Scroll to bottom of page
  3. Verify loading indicator appears
  4. Verify more certificates load
  5. Repeat until all certificates loaded
- **Expected Result:** All 180+ certificates accessible via infinite scroll
- **Priority:** HIGH

#### Test Case 4.5: Autocomplete Search
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/SearchTests.cs`
- **Description:** Test autocomplete search functionality
- **Test Steps:**
  1. Type "ASSA" in search box
  2. Verify dropdown appears with suggestions
  3. Verify categories: Companies, Products, Certificate Numbers
  4. Select suggestion
  5. Verify filtered results
- **Expected Result:** Search works with categorized suggestions
- **Priority:** HIGH

#### Test Case 4.6: Shareable URL for Filtered Search
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/ShareableUrlTests.cs`
- **Description:** Verify URL updates with filters and is shareable
- **Test Steps:**
  1. Apply filters (company, status, type)
  2. Verify URL query string updates
  3. Copy URL
  4. Open URL in new browser tab
  5. Verify same filters applied
- **Expected Result:** URL reflects filters, shareable
- **Priority:** HIGH

#### Test Case 4.7: Certificate Details with All Product IDs
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/DetailsTests.cs`
- **Description:** Verify certificate details page shows all product IDs
- **Test Steps:**
  1. Navigate to certificate with ASIN, UPC, GTIN, EAN
  2. Verify all product IDs display
  3. Verify product ID labels match type
  4. Test certificate with only some product IDs
- **Expected Result:** All available product IDs display correctly
- **Priority:** MEDIUM

---

### 5. Authentication & Authorization Tests

#### Test Case 5.1: Admin Login
- **Test File:** `GCCWebApp.Tests/Authentication/LoginTests.cs`
- **Description:** Test admin user login
- **Test Steps:**
  1. Navigate to /Account/Login
  2. Enter admin credentials (admin@greencirclecertified.com / Admin123!)
  3. Submit login form
  4. Verify redirect to dashboard
  5. Verify session contains admin role
- **Expected Result:** Login successful, admin access granted
- **Priority:** HIGH

#### Test Case 5.2: Analyst Login
- **Test File:** `GCCWebApp.Tests/Authentication/LoginTests.cs`
- **Description:** Test analyst user login
- **Test Steps:**
  1. Login as analyst (analyst@greencirclecertified.com / Analyst123!)
  2. Verify access to certificate management
  3. Verify NO access to admin routes
- **Expected Result:** Analyst access works correctly
- **Priority:** HIGH

#### Test Case 5.3: External User Login
- **Test File:** `GCCWebApp.Tests/Authentication/LoginTests.cs`
- **Description:** Test external user login
- **Test Steps:**
  1. Login as external user (partner@amazon.com / Partner123!)
  2. Verify read-only access to certificates
  3. Verify NO access to create/edit/delete
  4. Verify NO access to admin routes
- **Expected Result:** External user has limited access
- **Priority:** HIGH

#### Test Case 5.4: Role-Based Route Access - Admin Routes
- **Test File:** `GCCWebApp.Tests/Authorization/RouteAuthorizationTests.cs`
- **Description:** Verify admin routes protected
- **Test Scenarios:**
  - Admin user accessing /Admin/UserManagement (should succeed)
  - Analyst user accessing /Admin/UserManagement (should fail - 403)
  - External user accessing /Admin/UserManagement (should fail - 403)
  - Unauthenticated user accessing /Admin/UserManagement (should redirect to login)
- **Expected Result:** Only admins can access admin routes
- **Priority:** HIGH

#### Test Case 5.5: Role-Based Route Access - Certificate Management
- **Test File:** `GCCWebApp.Tests/Authorization/RouteAuthorizationTests.cs`
- **Description:** Verify certificate CRUD permissions
- **Test Scenarios:**
  - Admin: Create, Read, Update, Delete (all allowed)
  - Analyst: Create, Read, Update (allowed), Delete (forbidden)
  - External: Read only (Create, Update, Delete forbidden)
  - Public: Read only (Create, Update, Delete forbidden)
- **Expected Result:** Permissions enforced correctly
- **Priority:** HIGH

#### Test Case 5.6: JWT Token Generation
- **Test File:** `GCCWebApp.Tests/Authentication/JwtTests.cs`
- **Description:** Test JWT token creation
- **Test Steps:**
  1. Call JwtTokenService.GenerateToken() with user claims
  2. Verify token created
  3. Verify token contains correct claims (user id, email, role)
  4. Verify token expiration set correctly
- **Expected Result:** Valid JWT token created
- **Priority:** MEDIUM

#### Test Case 5.7: JWT Token Validation
- **Test File:** `GCCWebApp.Tests/Authentication/JwtTests.cs`
- **Description:** Test JWT token validation
- **Test Scenarios:**
  - Valid token (should authenticate)
  - Expired token (should reject)
  - Invalid signature (should reject)
  - Missing required claims (should reject)
- **Expected Result:** Only valid tokens accepted
- **Priority:** MEDIUM

#### Test Case 5.8: API Key Authentication
- **Test File:** `GCCWebApp.Tests/Authentication/ApiKeyTests.cs`
- **Description:** Test API key authentication
- **Test Steps:**
  1. Make API request with valid API key in X-API-Key header
  2. Verify authentication succeeds
  3. Make request with invalid API key
  4. Verify authentication fails (401)
- **Expected Result:** API key authentication works
- **Priority:** MEDIUM

#### Test Case 5.9: Session Management
- **Test File:** `GCCWebApp.Tests/Authentication/SessionTests.cs`
- **Description:** Test session timeout and renewal
- **Test Steps:**
  1. Login user
  2. Verify session created
  3. Make request (verify sliding expiration)
  4. Wait for session timeout
  5. Verify session expired
  6. Verify redirect to login
- **Expected Result:** Session timeout works correctly
- **Priority:** MEDIUM

#### Test Case 5.10: Two-Factor Authentication - Email
- **Test File:** `GCCWebApp.Tests/Authentication/TwoFactorTests.cs`
- **Description:** Test 2FA with email
- **Test Steps:**
  1. Enable 2FA for user
  2. Login with password
  3. Verify 2FA code sent to email
  4. Enter correct code
  5. Verify login succeeds
  6. Test with incorrect code (should fail)
- **Expected Result:** 2FA email flow works
- **Priority:** LOW (feature disabled in development)

---

### 6. API Functionality Tests

#### Test Case 6.1: GET /api/v1/certificates - List All
- **Test File:** `GCCWebApp.Api.Tests/CertificatesApiTests.cs`
- **Description:** Get all certificates via API
- **Test Steps:**
  1. Authenticate with API key or JWT
  2. GET /api/v1/certificates
  3. Verify 200 OK response
  4. Verify JSON array returned
  5. Verify pagination metadata (page, pageSize, totalCount)
  6. Verify certificate objects have all required fields
- **Expected Result:** Paginated certificate list returned
- **Priority:** HIGH

#### Test Case 6.2: GET /api/v1/certificates/{id} - Get by ID
- **Test File:** `GCCWebApp.Api.Tests/CertificatesApiTests.cs`
- **Description:** Get specific certificate by ID
- **Test Steps:**
  1. GET /api/v1/certificates/1
  2. Verify 200 OK
  3. Verify certificate object with all fields
  4. Verify related entities (company, product, certification type)
  5. Test with invalid ID (verify 404)
- **Expected Result:** Specific certificate returned or 404
- **Priority:** HIGH

#### Test Case 6.3: POST /api/v1/certificates - Create Certificate
- **Test File:** `GCCWebApp.Api.Tests/CertificatesApiTests.cs`
- **Description:** Create new certificate via API
- **Test Steps:**
  1. Authenticate as admin or analyst
  2. POST /api/v1/certificates with valid certificate data
  3. Verify 201 Created response
  4. Verify Location header contains new certificate URL
  5. Verify certificate created in database
  6. Test with invalid data (verify 400 Bad Request)
  7. Test without authentication (verify 401)
  8. Test as external user (verify 403)
- **Expected Result:** Certificate created with proper authorization
- **Priority:** HIGH

#### Test Case 6.4: PUT /api/v1/certificates/{id} - Update Certificate
- **Test File:** `GCCWebApp.Api.Tests/CertificatesApiTests.cs`
- **Description:** Update existing certificate
- **Test Steps:**
  1. Authenticate as admin or analyst
  2. PUT /api/v1/certificates/1 with updated data
  3. Verify 200 OK or 204 No Content
  4. Verify changes persisted in database
  5. Test with invalid ID (verify 404)
  6. Test with invalid data (verify 400)
  7. Test as external user (verify 403)
- **Expected Result:** Certificate updated with proper authorization
- **Priority:** HIGH

#### Test Case 6.5: DELETE /api/v1/certificates/{id} - Soft Delete
- **Test File:** `GCCWebApp.Api.Tests/CertificatesApiTests.cs`
- **Description:** Soft delete certificate (admin only)
- **Test Steps:**
  1. Authenticate as admin
  2. DELETE /api/v1/certificates/1
  3. Verify 204 No Content
  4. Verify certificate IsDeleted=true in database
  5. Verify certificate not returned in list API
  6. Test as analyst (verify 403)
  7. Test as external user (verify 403)
- **Expected Result:** Only admins can delete
- **Priority:** HIGH

#### Test Case 6.6: API Rate Limiting
- **Test File:** `GCCWebApp.Api.Tests/RateLimitingTests.cs`
- **Description:** Verify rate limiting enforcement
- **Test Steps:**
  1. Make 100 requests rapidly (anonymous user)
  2. Verify 429 Too Many Requests after limit
  3. Wait for rate limit window to reset
  4. Verify requests succeed again
  5. Test authenticated user rate limits (should be higher)
- **Expected Result:** Rate limiting enforced
- **Priority:** MEDIUM

#### Test Case 6.7: Partner API Filtering - Amazon
- **Test File:** `GCCWebApp.Api.Tests/PartnerApiTests.cs`
- **Description:** Verify partner-specific API filtering
- **Test Steps:**
  1. Authenticate with Amazon API key
  2. GET /api/CertificateTileExternal/tiles
  3. Verify only Amazon-associated certificates returned
  4. Verify partner field in response
- **Expected Result:** Partner filtering works
- **Priority:** MEDIUM

#### Test Case 6.8: API Versioning
- **Test File:** `GCCWebApp.Api.Tests/VersioningTests.cs`
- **Description:** Test API versioning support
- **Test Steps:**
  1. GET /api/v1/certificates
  2. Verify v1 endpoint works
  3. Try GET /api/v2/certificates (if exists)
  4. Verify version in response headers
- **Expected Result:** API versioning supported
- **Priority:** LOW

---

### 7. UI/UX Testing

#### Test Case 7.1: Mobile Responsive - Certificate Tiles
- **Test File:** `GCCWebApp.UAT.Tests/Mobile/ResponsiveTests.cs`
- **Description:** Test mobile responsiveness
- **Test Steps:**
  1. Set viewport to 375x667 (iPhone SE)
  2. Load certificate tile view
  3. Verify tiles stack vertically
  4. Verify filter panel collapses
  5. Verify mobile menu works
  6. Test on 768x1024 (iPad)
  7. Test on 360x640 (Android)
- **Expected Result:** Layout adapts correctly
- **Priority:** MEDIUM

#### Test Case 7.2: Filter Panel Functionality
- **Test File:** `GCCWebApp.UAT.Tests/UI/FilterPanelTests.cs`
- **Description:** Test filter panel interactions
- **Test Steps:**
  1. Expand/collapse filter categories
  2. Select multiple filters
  3. Verify filter counts update
  4. Click "Clear Filters" button
  5. Verify all filters cleared
  6. Test filter persistence on page refresh
- **Expected Result:** All filter interactions work
- **Priority:** MEDIUM

#### Test Case 7.3: Certificate PDF Download
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/DownloadTests.cs`
- **Description:** Download certificate PDF
- **Test Steps:**
  1. Navigate to certificate details
  2. Click "Download PDF" button
  3. Verify PDF file downloaded
  4. Verify PDF contains certificate data
  5. Verify PDF formatting correct
- **Expected Result:** PDF generated correctly
- **Priority:** MEDIUM

#### Test Case 7.4: Excel Export
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/ExportTests.cs`
- **Description:** Export certificates to Excel
- **Test Steps:**
  1. Apply filters to certificate list
  2. Click "Export to Excel" button
  3. Verify Excel file downloaded
  4. Verify Excel contains filtered certificates
  5. Verify all columns present
  6. Verify formatting (headers, data types)
- **Expected Result:** Excel export works correctly
- **Priority:** MEDIUM

#### Test Case 7.5: Product ID Manager - Bulk Import
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/ProductIdManagerTests.cs`
- **Description:** Import product IDs from CSV
- **Test Steps:**
  1. Open Product ID Manager
  2. Upload CSV with 100+ ASINs
  3. Verify import progress indicator
  4. Verify validation errors displayed
  5. Verify successful imports saved
  6. Verify product IDs associated with certificate
- **Expected Result:** Bulk import works with validation
- **Priority:** MEDIUM

#### Test Case 7.6: Product ID Manager - Bulk Export
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/ProductIdManagerTests.cs`
- **Description:** Export product IDs to CSV
- **Test Steps:**
  1. Open Product ID Manager for certificate with 50+ ASINs
  2. Click "Export to CSV"
  3. Verify CSV file downloaded
  4. Verify all product IDs in CSV
  5. Verify CSV format correct (headers, encoding)
- **Expected Result:** Export includes all product IDs
- **Priority:** MEDIUM

---

### 8. Error Handling & Edge Cases

#### Test Case 8.1: Invalid Certificate Number Lookup
- **Test File:** `GCCWebApp.Tests/Controllers/CertificateControllerTests.cs`
- **Description:** Handle invalid certificate number
- **Test Steps:**
  1. Navigate to /Certificate/Details/99-9999 (non-existent)
  2. Verify 404 Not Found page
  3. Verify user-friendly error message
  4. Verify "Back to Search" link
- **Expected Result:** Graceful 404 handling
- **Priority:** MEDIUM

#### Test Case 8.2: Expired Certificate Display
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/StatusTests.cs`
- **Description:** Verify expired certificate styling
- **Test Steps:**
  1. Find expired certificate
  2. Verify "EXPIRED" badge displayed
  3. Verify expired styling (e.g., grayed out)
  4. Verify expiration date shown
- **Expected Result:** Expired status clear to user
- **Priority:** LOW

#### Test Case 8.3: Certificate with No Product IDs
- **Test File:** `GCCWebApp.UAT.Tests/Certificate/EdgeCaseTests.cs`
- **Description:** Handle certificate without product IDs
- **Test Steps:**
  1. View certificate with no ASIN, UPC, GTIN, EAN
  2. Verify page loads without error
  3. Verify "No product IDs" message or empty state
  4. Verify other certificate data displays
- **Expected Result:** Graceful handling of missing data
- **Priority:** LOW

#### Test Case 8.4: Certificate with 100+ ASINs
- **Test File:** `GCCWebApp.Performance.Tests/LargeDataTests.cs`
- **Description:** Performance with large product ID lists
- **Test Steps:**
  1. Create certificate with 100+ ASINs
  2. Load certificate details page
  3. Measure page load time (should be < 2 seconds)
  4. Verify all ASINs display
  5. Test Product ID Manager with this certificate
- **Expected Result:** Performance acceptable
- **Priority:** MEDIUM

#### Test Case 8.5: Large File Upload
- **Test File:** `GCCWebApp.Tests/Services/FileUploadTests.cs`
- **Description:** Handle large file uploads
- **Test Steps:**
  1. Upload 10MB CSV file
  2. Verify progress indicator
  3. Verify file size validation
  4. Upload file exceeding limit (e.g., 50MB)
  5. Verify error message
- **Expected Result:** Large files handled gracefully
- **Priority:** LOW

#### Test Case 8.6: Concurrent User Sessions
- **Test File:** `GCCWebApp.Performance.Tests/ConcurrencyTests.cs`
- **Description:** Test multiple simultaneous users
- **Test Steps:**
  1. Simulate 50 concurrent users accessing tile view
  2. Measure response times
  3. Verify no database connection pool exhaustion
  4. Verify no race conditions
- **Expected Result:** System handles concurrent users
- **Priority:** LOW

#### Test Case 8.7: SQL Injection Prevention
- **Test File:** `GCCWebApp.Security.Tests/InjectionTests.cs`
- **Description:** Verify SQL injection protection
- **Test Steps:**
  1. Attempt SQL injection in search box: `' OR '1'='1`
  2. Verify no SQL error
  3. Verify no data leakage
  4. Try in certificate number field
  5. Try in other text inputs
- **Expected Result:** All inputs sanitized
- **Priority:** HIGH

#### Test Case 8.8: XSS Prevention
- **Test File:** `GCCWebApp.Security.Tests/XssTests.cs`
- **Description:** Verify XSS protection
- **Test Steps:**
  1. Submit `<script>alert('XSS')</script>` in various forms
  2. Verify script not executed
  3. Verify HTML encoded in output
  4. Test in comments, search, user input fields
- **Expected Result:** XSS prevented
- **Priority:** HIGH

---

## Implementation Priority

### Critical (Required for MVP)
- Database connection tests (5 test cases)
- Authentication/Authorization tests (5 test cases)
- Certificate data loading tests (7 test cases)
- API CRUD operation tests (5 test cases)

### High (Required for Production)
- Configuration validation tests (5 test cases)
- Data seeding tests (5 test cases)
- Security tests (2 test cases)

### Medium (Quality & UX)
- UI/UX tests (6 test cases)
- Edge case handling (4 test cases)
- API advanced features (3 test cases)

### Low (Nice to Have)
- Performance tests (2 test cases)
- Mobile responsiveness tests (1 test case)
- Advanced 2FA tests (1 test case)

---

## Test Project Structure

```
GCCWebApp-Tests/
├── GCCWebApp.Tests/                    # Unit & Integration Tests
│   ├── Database/
│   │   ├── ConnectionTests.cs
│   │   ├── RetryTests.cs
│   │   └── MigrationTests.cs
│   ├── Configuration/
│   │   ├── DatabaseProviderTests.cs
│   │   └── SecurityConfigurationTests.cs
│   ├── Authentication/
│   │   ├── LoginTests.cs
│   │   ├── JwtTests.cs
│   │   ├── ApiKeyTests.cs
│   │   ├── SessionTests.cs
│   │   └── TwoFactorTests.cs
│   ├── Authorization/
│   │   └── RouteAuthorizationTests.cs
│   ├── Data/
│   │   └── SeedDataTests.cs
│   ├── Controllers/
│   │   └── CertificateControllerTests.cs
│   └── Services/
│       └── FileUploadTests.cs
│
├── GCCWebApp.Api.Tests/                # API Tests
│   ├── CertificatesApiTests.cs
│   ├── PartnerApiTests.cs
│   ├── RateLimitingTests.cs
│   └── VersioningTests.cs
│
├── GCCWebApp.UAT.Tests/                # UI/End-to-End Tests
│   ├── Certificate/
│   │   ├── CertificateTileViewTests.cs
│   │   ├── FilteringTests.cs
│   │   ├── InfiniteScrollTests.cs
│   │   ├── SearchTests.cs
│   │   ├── ShareableUrlTests.cs
│   │   ├── DetailsTests.cs
│   │   ├── DownloadTests.cs
│   │   ├── ExportTests.cs
│   │   ├── ProductIdManagerTests.cs
│   │   ├── StatusTests.cs
│   │   └── EdgeCaseTests.cs
│   ├── Mobile/
│   │   └── ResponsiveTests.cs
│   └── UI/
│       └── FilterPanelTests.cs
│
├── GCCWebApp.Security.Tests/           # Security Tests
│   ├── InjectionTests.cs
│   └── XssTests.cs
│
└── GCCWebApp.Performance.Tests/        # Performance Tests
    ├── LargeDataTests.cs
    └── ConcurrencyTests.cs
```

---

## Test Data Requirements

### Required Test Databases
1. **Empty Database** - For seeding tests
2. **Fully Seeded Database** - For functional tests (180+ certificates)
3. **Minimal Database** - For performance tests (10 certificates)
4. **Edge Case Database** - Certificates with unusual data patterns

### Required Test Users
- Admin: admin@greencirclecertified.com / Admin123!
- Analyst: analyst@greencirclecertified.com / Analyst123!
- External: partner@amazon.com / Partner123!
- Public: public@example.com / Public123!

### Required Test API Keys
- Admin API Key: `gcc_test_admin_key_1234567890abcdef`
- Partner API Key: `gcc_partner_key_abcdef1234567890`
- Expired API Key: `gcc_expired_key_fedcba0987654321`

---

## Test Environment Setup

### Prerequisites
1. .NET 8.0 SDK
2. SQL Server LocalDB (or SQL Server Express)
3. xUnit test framework
4. Playwright (for UI tests)
5. FluentAssertions (for readable assertions)
6. Microsoft.AspNetCore.Mvc.Testing (for integration tests)

### Configuration Files Needed
- `appsettings.Testing.json` - Test environment configuration
- `test-database-connection.json` - Test database credentials
- `test-users.json` - Test user credentials

### CI/CD Integration
- Tests should run on every pull request
- Integration tests require test database
- UI tests require headless browser (Playwright)
- Performance tests optional in CI (long-running)

---

## Next Steps

1. **Immediate Actions:**
   - Fix SQL Server logon trigger issue
   - Create test database and seed data
   - Implement Critical priority test cases (database, auth, CRUD)

2. **Short-term (1-2 weeks):**
   - Implement High priority test cases
   - Set up CI/CD pipeline for automated testing
   - Create test data fixtures

3. **Medium-term (1 month):**
   - Implement Medium priority test cases
   - Add code coverage reporting
   - Performance testing suite

4. **Long-term (Ongoing):**
   - Maintain test coverage > 80%
   - Add regression tests for bugs found
   - Update tests when features change

---

## Useful Resources

- [xUnit Documentation](https://xunit.net/)
- [Playwright .NET](https://playwright.dev/dotnet/)
- [Entity Framework Core Testing](https://learn.microsoft.com/en-us/ef/core/testing/)
- [ASP.NET Core Integration Tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [FluentAssertions](https://fluentassertions.com/)

---

**Document Version:** 1.0
**Last Updated:** October 15, 2025
**Approved By:** Development Team
**Status:** DRAFT - Awaiting Implementation
