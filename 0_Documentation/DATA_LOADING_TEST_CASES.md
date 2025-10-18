# Data Loading Test Cases
**Version:** 1.0
**Last Updated:** October 17, 2025
**Test Environment:** All (Development, Staging, Production)

## Overview

This document defines comprehensive test cases to verify that all certificate, company, and product data is correctly loaded from SQL Server database with NO hardcoded values.

## Test Execution Summary

| Test Suite | Total Tests | Priority | Status |
|------------|-------------|----------|--------|
| Database Connection Tests | 5 | Critical | ⚠️ Pending |
| Data Loading Tests | 12 | Critical | ⚠️ Pending |
| API Endpoint Tests | 8 | High | ⚠️ Pending |
| Frontend Integration Tests | 6 | High | ⚠️ Pending |
| Performance Tests | 4 | Medium | ⚠️ Pending |
| Security Tests | 5 | Critical | ⚠️ Pending |
| **TOTAL** | **40** | - | ⚠️ Pending |

---

## Test Suite 1: Database Connection Tests

### TEST-DB-001: Verify SQL Server Connection (Development)
**Priority:** Critical
**Environment:** Development

**Prerequisites:**
- Application configured with Development environment
- ANUKI01 SQL Server instance running
- GCCDatabase_Dev database exists

**Test Steps:**
1. Start application in Development mode
2. Check application logs for database provider message
3. Verify connection string is loaded

**Expected Results:**
```
Using SQL Server database provider
Database connection successful: ANUKI01/GCCDatabase_Dev
```

**SQL Validation Query:**
```sql
SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    @@VERSION AS SQLServerVersion;
```

**Pass Criteria:**
- ✅ Application logs show "Using SQL Server database provider"
- ✅ No connection errors in logs
- ✅ Database queries execute successfully

**Status:** ⚠️ PENDING

---

### TEST-DB-002: Verify SQL Server Connection (Production)
**Priority:** Critical
**Environment:** Production

**Prerequisites:**
- Application configured with Production environment
- AccuWebHosting SQL Server accessible
- gccwebap_certificates database exists

**Test Steps:**
1. Start application in Production mode
2. Monitor application logs
3. Verify secure connection (SSL/TLS)

**Expected Results:**
```
Using SQL Server database provider
Connection to production database established
Server: 68.71.129.120,1533
Database: gccwebap_certificates
```

**Pass Criteria:**
- ✅ Connection uses SSL/TLS encryption
- ✅ Connection string loaded from secure configuration
- ✅ No errors in application logs

**Status:** ⚠️ PENDING

---

### TEST-DB-003: Verify Database Seeding (Development Only)
**Priority:** High
**Environment:** Development

**Test Steps:**
1. Drop and recreate GCCDatabase_Dev
2. Start application
3. Check for "Database seeded successfully" message
4. Query database for seeded data

**Validation Queries:**
```sql
-- Check seeded certification types
SELECT COUNT(*) AS CertTypeCount FROM CertificationTypes WHERE IsActive = 1;
-- Expected: >= 7

-- Check seeded companies
SELECT COUNT(*) AS CompanyCount FROM Companies WHERE IsActive = 1;
-- Expected: >= 10

-- Check seeded products
SELECT COUNT(*) AS ProductCount FROM Products WHERE IsActive = 1;
-- Expected: >= 20

-- Check seeded certificates
SELECT COUNT(*) AS CertificateCount FROM Certificates WHERE Status = 'active';
-- Expected: >= 50

-- Check test users
SELECT Email, COUNT(*) FROM Users GROUP BY Email;
-- Expected: admin@greencirclecertified.com, analyst@greencirclecertified.com, partner@amazon.com
```

**Pass Criteria:**
- ✅ Application logs show "✅ Development database seeded successfully"
- ✅ All reference data exists
- ✅ Test users created with proper roles
- ✅ Sample certificates created

**Status:** ⚠️ PENDING

---

### TEST-DB-004: Verify Entity Relationships
**Priority:** High
**Environment:** All

**Test Steps:**
1. Query certificates with related entities
2. Verify all foreign keys resolve correctly
3. Check for orphaned records

**Validation Queries:**
```sql
-- Check for certificates without companies
SELECT COUNT(*) AS OrphanedCerts
FROM Certificates c
LEFT JOIN Companies co ON c.CompanyId = co.Id
WHERE co.Id IS NULL;
-- Expected: 0

-- Check for certificates without products
SELECT COUNT(*) AS OrphanedCerts
FROM Certificates c
LEFT JOIN Products p ON c.ProductId = p.Id
WHERE p.Id IS NULL;
-- Expected: 0

-- Check for certificates without certification types
SELECT COUNT(*) AS OrphanedCerts
FROM Certificates c
LEFT JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE ct.Id IS NULL;
-- Expected: 0

-- Verify relationship integrity
SELECT
    c.Number,
    co.Name AS CompanyName,
    p.Name AS ProductName,
    ct.Name AS CertTypeName
FROM Certificates c
INNER JOIN Companies co ON c.CompanyId = co.Id
INNER JOIN Products p ON c.ProductId = p.Id
INNER JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE c.Status = 'active'
LIMIT 10;
-- Expected: All columns populated, no NULLs
```

**Pass Criteria:**
- ✅ No orphaned certificates
- ✅ All relationships properly maintained
- ✅ Foreign key constraints enforced

**Status:** ⚠️ PENDING

---

### TEST-DB-005: Verify No SQLite or In-Memory Database
**Priority:** Critical
**Environment:** All

**Test Steps:**
1. Check application configuration
2. Verify database provider
3. Check connection strings

**Configuration Check:**
```bash
# Check appsettings for SQLite references
grep -r "Sqlite" appsettings*.json
# Expected: No matches

# Check for in-memory database configuration
grep -r "InMemory" appsettings*.json
# Expected: No matches

# Verify SQL Server provider
grep -r "UseSqlServer" Program.cs
# Expected: Found
```

**Pass Criteria:**
- ✅ No SQLite configuration found
- ✅ No InMemory database configuration found
- ✅ UseSqlServer confirmed in Program.cs

**Status:** ⚠️ PENDING

---

## Test Suite 2: Data Loading Tests

### TEST-DL-001: Verify Certificate Data Loaded from Database
**Priority:** Critical
**Environment:** All

**Test Steps:**
1. Start application
2. Call API endpoint: GET `/api/certificate-tiles?page=1&pageSize=10`
3. Verify response contains database data

**cURL Command:**
```bash
curl -s "http://localhost:5101/api/certificate-tiles?page=1&pageSize=10" | python -m json.tool
```

**Expected Response Structure:**
```json
{
    "success": true,
    "certificates": [
        {
            "id": 85,
            "certificateNumber": "25-5057",
            "certificationType": "Material Ingredient Reporting",
            "companyName": "3M Company",
            "productName": "Security Exit Device - EX700",
            "productType": "High-security exit device system",
            "effectiveDate": "2025-10-13",
            "expirationDate": "2027-10-13",
            "location": "Chicago, IL",
            "country": "United States",
            "status": "active",
            "isExpired": false,
            "isExpiringSoon": false
        }
    ],
    "totalCount": 194,
    "hasMore": true
}
```

**Validation Checks:**
- ✅ `success: true`
- ✅ `certificates` array not empty
- ✅ All required fields present in each certificate
- ✅ `totalCount` > 0
- ✅ Data matches database query

**Cross-Reference Database:**
```sql
SELECT TOP 10
    c.Id,
    c.Number,
    ct.Name AS CertificationType,
    co.Name AS CompanyName,
    p.Name AS ProductName,
    c.EffectiveDate,
    c.ExpirationDate,
    c.Location,
    c.Country,
    c.Status
FROM Certificates c
INNER JOIN Companies co ON c.CompanyId = co.Id
INNER JOIN Products p ON c.ProductId = p.Id
INNER JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE c.Status = 'active'
ORDER BY c.EffectiveDate DESC;
```

**Pass Criteria:**
- ✅ API response matches database query
- ✅ No hardcoded certificate data in response
- ✅ All data dynamically loaded

**Status:** ⚠️ PENDING

---

### TEST-DL-002: Verify Company Data Not Hardcoded
**Priority:** High
**Environment:** All

**Test Steps:**
1. Call API endpoint to get filter options
2. Verify company list comes from database
3. Cross-check with database

**API Call:**
```bash
curl -s "http://localhost:5101/api/certificate-tiles/filters" | python -m json.tool
```

**Expected Response:**
```json
{
    "certificationTypes": ["FSC Chain of Custody", "SFI Chain of Custody", ...],
    "companies": [
        {"id": 1, "name": "3M Company"},
        {"id": 2, "name": "ASSA ABLOY Access & Egress Hardware Group, Inc."},
        ...
    ],
    "countries": ["United States", "Canada", "Mexico", ...]
}
```

**Database Validation:**
```sql
SELECT Id, Name
FROM Companies
WHERE IsActive = 1
ORDER BY Name;
```

**Pass Criteria:**
- ✅ Company list matches database
- ✅ No hardcoded company names in code
- ✅ All companies from database

**Status:** ⚠️ PENDING

---

### TEST-DL-003: Verify Product Data Not Hardcoded
**Priority:** High
**Environment:** All

**Test Steps:**
1. Query products via API
2. Cross-reference with database
3. Verify no hardcoded product data

**Database Query:**
```sql
SELECT
    p.Id,
    p.Name,
    p.Description,
    COUNT(c.Id) AS CertificateCount
FROM Products p
LEFT JOIN Certificates c ON p.Id = c.ProductId AND c.Status = 'active'
WHERE p.IsActive = 1
GROUP BY p.Id, p.Name, p.Description
ORDER BY CertificateCount DESC;
```

**Pass Criteria:**
- ✅ All products from database
- ✅ No hardcoded product arrays in code

**Status:** ⚠️ PENDING

---

### TEST-DL-004: Verify Certification Types Not Hardcoded
**Priority:** High
**Environment:** All

**Test Steps:**
1. Get certification types from API
2. Verify against database
3. Check for hardcoded arrays

**Database Query:**
```sql
SELECT Id, Name, Description
FROM CertificationTypes
WHERE IsActive = 1
ORDER BY Name;
```

**Code Audit:**
```bash
# Search for hardcoded certification type arrays
grep -rn "FSC Chain of Custody" Controllers/ Services/ --include="*.cs" | grep "new\[\]"
# Expected: Only in test seeding (ExportController.SeedDatabase)
```

**Pass Criteria:**
- ✅ Certification types from database
- ✅ No hardcoded arrays except in test seeding

**Status:** ⚠️ PENDING

---

### TEST-DL-005: Verify Pagination Data from Database
**Priority:** Medium
**Environment:** All

**Test Steps:**
1. Call API with different page numbers
2. Verify each page returns different data
3. Confirm data consistency

**Test Scenario:**
```bash
# Page 1
curl -s "http://localhost:5101/api/certificate-tiles?page=1&pageSize=5"

# Page 2
curl -s "http://localhost:5101/api/certificate-tiles?page=2&pageSize=5"

# Page 3
curl -s "http://localhost:5101/api/certificate-tiles?page=3&pageSize=5"
```

**Validation:**
- Page 1 certificates differ from Page 2
- Page 2 certificates differ from Page 3
- No certificate appears on multiple pages
- `totalCount` consistent across requests

**Pass Criteria:**
- ✅ Each page returns unique certificates
- ✅ Pagination metadata correct
- ✅ No duplicate data across pages

**Status:** ⚠️ PENDING

---

### TEST-DL-006: Verify Search Results from Database
**Priority:** High
**Environment:** All

**Test Steps:**
1. Perform search query
2. Verify results match database
3. Confirm no hardcoded search results

**Test Searches:**
```bash
# Search by company
curl -s "http://localhost:5101/api/certificate-tiles?searchTerm=3M"

# Search by certificate number
curl -s "http://localhost:5101/api/certificate-tiles?searchTerm=25-5057"

# Search by product
curl -s "http://localhost:5101/api/certificate-tiles?searchTerm=Security"
```

**Database Validation:**
```sql
-- Verify search for "3M"
SELECT c.*, co.Name AS CompanyName
FROM Certificates c
INNER JOIN Companies co ON c.CompanyId = co.Id
WHERE co.Name LIKE '%3M%'
OR c.Number LIKE '%3M%';
```

**Pass Criteria:**
- ✅ Search results match database query
- ✅ No predefined search results

**Status:** ⚠️ PENDING

---

### TEST-DL-007: Verify Filtering Data from Database
**Priority:** High
**Environment:** All

**Test Steps:**
1. Apply filters (certification type, country, company)
2. Verify filtered results from database
3. Validate filter combinations

**Test Filters:**
```bash
# Filter by certification type
curl -s "http://localhost:5101/api/certificate-tiles?certType=FSC%20Chain%20of%20Custody"

# Filter by country
curl -s "http://localhost:5101/api/certificate-tiles?country=United%20States"

# Combined filters
curl -s "http://localhost:5101/api/certificate-tiles?certType=FSC&country=United%20States"
```

**Database Validation:**
```sql
-- FSC Chain of Custody + United States
SELECT c.*, ct.Name AS CertType
FROM Certificates c
INNER JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE ct.Name = 'FSC Chain of Custody'
AND c.Country = 'United States'
AND c.Status = 'active';
```

**Pass Criteria:**
- ✅ Filtered results match database
- ✅ Filter combinations work correctly

**Status:** ⚠️ PENDING

---

### TEST-DL-008: Verify Sort Options from Database
**Priority:** Medium
**Environment:** All

**Test Steps:**
1. Test different sort options
2. Verify sort order matches database
3. Validate multiple sort fields

**Test Sorts:**
```bash
# Sort by newest
curl -s "http://localhost:5101/api/certificate-tiles?sort=newest"

# Sort by company A-Z
curl -s "http://localhost:5101/api/certificate-tiles?sort=company-az"

# Sort by expiration date
curl -s "http://localhost:5101/api/certificate-tiles?sort=expiring"
```

**Database Validation:**
```sql
-- Newest (Effective Date DESC)
SELECT * FROM Certificates
WHERE Status = 'active'
ORDER BY EffectiveDate DESC;

-- Company A-Z
SELECT c.*, co.Name
FROM Certificates c
INNER JOIN Companies co ON c.CompanyId = co.Id
WHERE c.Status = 'active'
ORDER BY co.Name ASC;
```

**Pass Criteria:**
- ✅ Sort order matches database query
- ✅ All sort options work correctly

**Status:** ⚠️ PENDING

---

### TEST-DL-009: Verify Autocomplete Suggestions from Database
**Priority:** Medium
**Environment:** All

**Test Steps:**
1. Call autocomplete endpoint with various terms
2. Verify suggestions from database
3. Check categorization (Company, Product, Certificate)

**Test Autocomplete:**
```bash
curl -s "http://localhost:5101/api/certificate-tiles/autocomplete?term=3M"
```

**Expected Response:**
```json
{
    "suggestions": [
        {"value": "3M Company", "label": "3M Company", "type": "Company"},
        {"value": "3M Security Device", "label": "3M Security Device", "type": "Product"},
        ...
    ]
}
```

**Database Validation:**
```sql
-- Companies matching "3M"
SELECT Name FROM Companies WHERE Name LIKE '%3M%' AND IsActive = 1 LIMIT 5;

-- Products matching "3M"
SELECT Name FROM Products WHERE Name LIKE '%3M%' AND IsActive = 1 LIMIT 5;

-- Certificates matching "3M"
SELECT Number FROM Certificates WHERE Number LIKE '%3M%' AND Status = 'active' LIMIT 5;
```

**Pass Criteria:**
- ✅ Suggestions match database queries
- ✅ Proper categorization
- ✅ No hardcoded suggestions

**Status:** ⚠️ PENDING

---

### TEST-DL-010: Verify Statistics from Database
**Priority:** Medium
**Environment:** All

**Test Steps:**
1. Get dashboard statistics
2. Verify counts match database
3. Check for hardcoded statistics

**API Call:**
```bash
curl -s "http://localhost:5101/api/Dashboard/statistics"
```

**Database Validation:**
```sql
-- Active certificates
SELECT COUNT(*) AS ActiveCount
FROM Certificates
WHERE Status = 'active' AND ExpirationDate > GETUTCDATE();

-- Expired certificates
SELECT COUNT(*) AS ExpiredCount
FROM Certificates
WHERE ExpirationDate <= GETUTCDATE();

-- Expiring soon (30 days)
SELECT COUNT(*) AS ExpiringSoonCount
FROM Certificates
WHERE ExpirationDate BETWEEN GETUTCDATE() AND DATEADD(DAY, 30, GETUTCDATE())
AND Status = 'active';

-- Companies with active certificates
SELECT COUNT(DISTINCT c.CompanyId) AS CompanyCount
FROM Certificates c
WHERE c.Status = 'active';
```

**Pass Criteria:**
- ✅ Statistics match database counts
- ✅ No hardcoded numbers

**Status:** ⚠️ PENDING

---

### TEST-DL-011: Verify Product IDs from Database
**Priority:** High
**Environment:** All

**Test Steps:**
1. Get certificate with product IDs
2. Verify product IDs loaded from database
3. Check for 100+ product IDs support

**Database Query:**
```sql
-- Get certificate with product IDs
SELECT
    c.Number,
    cp.ProductIdType,
    cp.ProductIdValue,
    cp.IsPrimary
FROM Certificates c
INNER JOIN CertificateProductIds cp ON c.Id = cp.CertificateId
WHERE c.Number = '25-5057';
```

**Pass Criteria:**
- ✅ Product IDs loaded from database
- ✅ Multiple product IDs per certificate supported
- ✅ No hardcoded product IDs

**Status:** ⚠️ PENDING

---

### TEST-DL-012: Verify User Data from Database
**Priority:** High
**Environment:** All

**Test Steps:**
1. Log in as test user
2. Verify user data from database
3. Check role assignments

**Database Validation:**
```sql
-- Check user exists
SELECT Id, Email, FirstName, LastName, IsActive
FROM Users
WHERE Email = 'admin@greencirclecertified.com';

-- Check user roles
SELECT u.Email, r.Name AS RoleName
FROM Users u
INNER JOIN UserRoles ur ON u.Id = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.Id
WHERE u.Email = 'admin@greencirclecertified.com';
```

**Pass Criteria:**
- ✅ User data from database
- ✅ Roles properly assigned
- ✅ No hardcoded user data

**Status:** ⚠️ PENDING

---

## Test Suite 3: API Endpoint Tests

### TEST-API-001: GET /api/certificate-tiles
**Endpoint:** `/api/certificate-tiles`
**Method:** GET
**Priority:** Critical

**Test Request:**
```bash
curl -X GET "http://localhost:5101/api/certificate-tiles?page=1&pageSize=20" \
  -H "Accept: application/json"
```

**Expected Response:**
- Status: 200 OK
- Content-Type: application/json
- Body contains `success: true`
- Body contains `certificates` array
- Body contains `totalCount` number

**Validation:**
```javascript
// Response structure validation
assert(response.success === true);
assert(Array.isArray(response.certificates));
assert(typeof response.totalCount === 'number');
assert(response.totalCount > 0);
```

**Pass Criteria:**
- ✅ Status 200
- ✅ Valid JSON response
- ✅ All certificates have required fields
- ✅ Data matches database

**Status:** ⚠️ PENDING

---

### TEST-API-002: GET /api/certificate-tiles/filters
**Endpoint:** `/api/certificate-tiles/filters`
**Method:** GET
**Priority:** High

**Test Request:**
```bash
curl -X GET "http://localhost:5101/api/certificate-tiles/filters" \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
    "certificationTypes": ["FSC Chain of Custody", "SFI Chain of Custody", ...],
    "companies": [{"id": 1, "name": "3M Company"}, ...],
    "countries": ["United States", "Canada", ...]
}
```

**Pass Criteria:**
- ✅ Status 200
- ✅ All filter arrays populated from database
- ✅ No hardcoded filter values

**Status:** ⚠️ PENDING

---

### TEST-API-003: GET /api/certificate-tiles/autocomplete
**Endpoint:** `/api/certificate-tiles/autocomplete`
**Method:** GET
**Priority:** Medium

**Test Request:**
```bash
curl -X GET "http://localhost:5101/api/certificate-tiles/autocomplete?term=3M" \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
    "suggestions": [
        {"value": "3M Company", "label": "3M Company", "type": "Company"},
        ...
    ]
}
```

**Pass Criteria:**
- ✅ Status 200
- ✅ Suggestions categorized properly
- ✅ Data from database

**Status:** ⚠️ PENDING

---

### TEST-API-004: GET /api/certificate-tiles/{id}/download
**Endpoint:** `/api/certificate-tiles/{id}/download`
**Method:** GET
**Priority:** Medium

**Test Request:**
```bash
curl -X GET "http://localhost:5101/api/certificate-tiles/1/download" \
  -H "Accept: application/pdf" \
  --output test-certificate.pdf
```

**Expected Response:**
- Status: 200 OK
- Content-Type: application/pdf
- PDF file generated

**Pass Criteria:**
- ✅ Status 200
- ✅ PDF downloaded successfully
- ✅ Certificate data from database

**Status:** ⚠️ PENDING

---

### TEST-API-005: POST /api/certificate-tiles/share-link
**Endpoint:** `/api/certificate-tiles/share-link`
**Method:** POST
**Priority:** Low

**Test Request:**
```bash
curl -X POST "http://localhost:5101/api/certificate-tiles/share-link" \
  -H "Content-Type: application/json" \
  -d '{"search":"3M","certType":"FSC Chain of Custody"}'
```

**Expected Response:**
```json
{
    "url": "http://localhost:5101/Certificate/CertificateTileView?search=3M&certType=FSC..."
}
```

**Pass Criteria:**
- ✅ Status 200
- ✅ Valid shareable URL generated

**Status:** ⚠️ PENDING

---

### TEST-API-006: Verify API Rate Limiting
**Priority:** Medium
**Environment:** All

**Test Steps:**
1. Make 100 API requests in quick succession
2. Verify rate limiting headers
3. Check for 429 status when limit exceeded

**Test Script:**
```bash
for i in {1..105}; do
  curl -I "http://localhost:5101/api/certificate-tiles?page=1" | grep -E "X-RateLimit|HTTP/1.1"
done
```

**Expected Headers:**
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 99
X-RateLimit-Reset: 1760731200
```

**Pass Criteria:**
- ✅ Rate limit headers present
- ✅ 429 status after limit exceeded
- ✅ Rate limit resets properly

**Status:** ⚠️ PENDING

---

### TEST-API-007: Verify API Error Handling
**Priority:** High
**Environment:** All

**Test Steps:**
1. Call API with invalid parameters
2. Verify proper error responses
3. Check error messages

**Test Cases:**
```bash
# Invalid page number
curl "http://localhost:5101/api/certificate-tiles?page=-1"
# Expected: 400 Bad Request

# Invalid page size
curl "http://localhost:5101/api/certificate-tiles?pageSize=0"
# Expected: 400 Bad Request

# Nonexistent certificate
curl "http://localhost:5101/api/certificate-tiles/999999/download"
# Expected: 404 Not Found
```

**Pass Criteria:**
- ✅ Proper HTTP status codes returned
- ✅ Descriptive error messages
- ✅ No stack traces exposed

**Status:** ⚠️ PENDING

---

### TEST-API-008: Verify API Response Times
**Priority:** Medium
**Environment:** All

**Test Steps:**
1. Make API calls with timing
2. Verify response times < 500ms
3. Test with various page sizes

**Test Script:**
```bash
# Test response time
time curl -s "http://localhost:5101/api/certificate-tiles?page=1&pageSize=50" > /dev/null
```

**Pass Criteria:**
- ✅ Response time < 500ms for 50 records
- ✅ Response time < 1s for 100 records
- ✅ Consistent performance across requests

**Status:** ⚠️ PENDING

---

## Test Suite 4: Frontend Integration Tests

### TEST-FE-001: Verify Certificate Tiles Load on Page
**Priority:** Critical
**Environment:** All

**Test Steps:**
1. Navigate to `/Certificate/CertificateTileView`
2. Wait for page load
3. Verify certificates displayed
4. Check browser console for errors

**Browser Console Check:**
```javascript
// Should see:
Loading certificates from: /api/certificate-tiles?page=1&pageSize=20...
Loaded 20 certificates, total: 194

// Should NOT see:
Error loading certificates
Failed to fetch
```

**Visual Verification:**
- Certificate tiles are displayed
- Company names visible
- Product names visible
- Certificate numbers visible
- Images/icons loaded

**Pass Criteria:**
- ✅ Page loads without errors
- ✅ Certificates displayed in tile format
- ✅ No console errors
- ✅ Data loaded from API

**Status:** ⚠️ PENDING

---

### TEST-FE-002: Verify Infinite Scroll Works
**Priority:** High
**Environment:** All

**Test Steps:**
1. Load certificate tile view
2. Scroll to bottom of page
3. Verify more certificates load
4. Check for duplicate certificates

**Expected Behavior:**
1. Initial load: 20 certificates
2. Scroll to bottom
3. Loading indicator appears
4. Next 20 certificates append
5. Process repeats until all loaded

**Browser Console:**
```javascript
// Should see:
Loading more certificates...
Loaded 20 more certificates (40 total)
Loading more certificates...
Loaded 20 more certificates (60 total)
...
All certificates loaded (194 total)
```

**Pass Criteria:**
- ✅ Infinite scroll triggers correctly
- ✅ No duplicate certificates
- ✅ Correct total count reached
- ✅ "No more certificates" message appears

**Status:** ⚠️ PENDING

---

### TEST-FE-003: Verify Search Functionality
**Priority:** High
**Environment:** All

**Test Steps:**
1. Enter search term in search box
2. Verify results update
3. Check API call in network tab

**Test Searches:**
- "3M" - Should find 3M Company certificates
- "25-5057" - Should find specific certificate
- "FSC" - Should find FSC-related certificates

**Network Tab:**
```
GET /api/certificate-tiles?searchTerm=3M&page=1&pageSize=20
Status: 200 OK
Response: {"success": true, "certificates": [...]}
```

**Pass Criteria:**
- ✅ Search triggers API call
- ✅ Results update dynamically
- ✅ Correct certificates displayed

**Status:** ⚠️ PENDING

---

### TEST-FE-004: Verify Filter Functionality
**Priority:** High
**Environment:** All

**Test Steps:**
1. Open filter panel
2. Select certification type filter
3. Verify certificates update
4. Apply multiple filters

**Test Filters:**
- Certification Type: "FSC Chain of Custody"
- Country: "United States"
- Company: Select from dropdown

**Expected Behavior:**
- Filter dropdowns populated from database
- Selecting filter triggers API call with filter params
- Results update to show only matching certificates
- Multiple filters combine with AND logic

**Pass Criteria:**
- ✅ Filters populated from API
- ✅ Filtering works correctly
- ✅ Multiple filters work together
- ✅ Clear filters button works

**Status:** ⚠️ PENDING

---

### TEST-FE-005: Verify Autocomplete Suggestions
**Priority:** Medium
**Environment:** All

**Test Steps:**
1. Start typing in search box
2. Verify autocomplete suggestions appear
3. Click suggestion
4. Verify search triggered

**Test Input:**
- Type "3" - No suggestions yet (min 2 chars)
- Type "3M" - Suggestions appear
- Categories: Company, Product, Certificate

**Expected Suggestions:**
```
Company: 3M Company
Product: 3M Security Device
Certificate: 25-5057 - 3M Company
```

**Pass Criteria:**
- ✅ Suggestions appear after 2 characters
- ✅ Suggestions categorized properly
- ✅ Clicking suggestion triggers search
- ✅ Data from database (API)

**Status:** ⚠️ PENDING

---

### TEST-FE-006: Verify Shareable URL Functionality
**Priority:** Low
**Environment:** All

**Test Steps:**
1. Apply search and filters
2. Copy shareable URL
3. Open URL in new browser/incognito window
4. Verify same results displayed

**Test Scenario:**
1. Search: "3M"
2. Filter: Certification Type = "FSC Chain of Custody"
3. Copy URL
4. URL should be: `/Certificate/CertificateTileView?search=3M&certType=FSC%20Chain%20of%20Custody`

**Pass Criteria:**
- ✅ URL contains all search/filter params
- ✅ Opening URL loads correct results
- ✅ URL is shareable

**Status:** ⚠️ PENDING

---

## Test Suite 5: Performance Tests

### TEST-PERF-001: Load 1000 Certificates
**Priority:** Medium
**Environment:** Development/Staging

**Test Steps:**
1. Seed database with 1000 certificates
2. Call API to load all certificates
3. Measure response time

**Expected Performance:**
- First page (20 records): < 500ms
- 50 records: < 750ms
- 100 records: < 1s

**Load Test Script:**
```bash
# Apache Bench test
ab -n 1000 -c 10 "http://localhost:5101/api/certificate-tiles?page=1&pageSize=20"
```

**Pass Criteria:**
- ✅ 95th percentile < 500ms
- ✅ No timeouts
- ✅ Database queries optimized

**Status:** ⚠️ PENDING

---

### TEST-PERF-002: Concurrent User Load Test
**Priority:** Medium
**Environment:** Staging

**Test Steps:**
1. Simulate 50 concurrent users
2. Each user loads certificate tiles
3. Measure response times and errors

**Load Test:**
```bash
# Use k6 or Apache Bench
k6 run --vus 50 --duration 30s load-test-script.js
```

**Expected Results:**
- All requests succeed
- Average response time < 1s
- No database connection errors

**Pass Criteria:**
- ✅ 0% error rate
- ✅ Stable response times
- ✅ Database handles load

**Status:** ⚠️ PENDING

---

### TEST-PERF-003: Search Performance with Large Dataset
**Priority:** Medium
**Environment:** Development

**Test Steps:**
1. Seed 10,000 certificates
2. Perform various searches
3. Measure query execution times

**Test Searches:**
- Full-text search: "sustainable"
- Exact match: "25-5057"
- Prefix search: "25-"

**Database Query Profiling:**
```sql
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

SELECT c.*, co.Name, p.Name
FROM Certificates c
INNER JOIN Companies co ON c.CompanyId = co.Id
INNER JOIN Products p ON c.ProductId = p.Id
WHERE c.Number LIKE '25-%'
OR co.Name LIKE '%sustainable%';

-- Check execution plan and timing
```

**Pass Criteria:**
- ✅ Queries execute in < 500ms
- ✅ Indexes utilized effectively
- ✅ No table scans on large tables

**Status:** ⚠️ PENDING

---

### TEST-PERF-004: Infinite Scroll Performance
**Priority:** Low
**Environment:** All

**Test Steps:**
1. Load certificate tile view
2. Continuously scroll to load all pages
3. Monitor memory usage and performance

**Expected Behavior:**
- No memory leaks
- Smooth scrolling
- Consistent load times for each page

**Browser Performance Check:**
```javascript
// Monitor memory usage
performance.memory.usedJSHeapSize < 50MB

// Check for memory leaks
// Initial load: ~10MB
// After loading 200 certs: ~15MB
// After loading 500 certs: ~20MB (acceptable)
```

**Pass Criteria:**
- ✅ No memory leaks detected
- ✅ Smooth user experience
- ✅ Page doesn't freeze

**Status:** ⚠️ PENDING

---

## Test Suite 6: Security Tests

### TEST-SEC-001: Verify No Hardcoded Credentials
**Priority:** Critical
**Environment:** All

**Test Steps:**
1. Search codebase for hardcoded credentials
2. Check configuration files
3. Verify secure credential storage

**Search Commands:**
```bash
# Search for passwords
grep -rn "password\s*=\s*[\"']" --include="*.cs" --include="*.json" Controllers/ Services/ | grep -v "//\|/\*"

# Search for connection strings
grep -rn "Server.*Password=" appsettings*.json

# Search for API keys
grep -rn "ApiKey.*=.*[\"']" --include="*.cs"
```

**Pass Criteria:**
- ✅ No hardcoded passwords found
- ✅ Connection strings use placeholders or env vars
- ✅ API keys stored securely

**Status:** ⚠️ PENDING

---

### TEST-SEC-002: Verify SQL Injection Protection
**Priority:** Critical
**Environment:** All

**Test Steps:**
1. Attempt SQL injection in search fields
2. Verify parameterized queries used
3. Check for direct SQL execution

**Injection Attempts:**
```bash
# Try SQL injection in search
curl "http://localhost:5101/api/certificate-tiles?searchTerm=' OR '1'='1"

# Try in autocomplete
curl "http://localhost:5101/api/certificate-tiles/autocomplete?term='; DROP TABLE Certificates;--"
```

**Expected Result:**
- No SQL executed
- Search returns no results or error
- No database modification

**Code Audit:**
```bash
# Check for string concatenation in SQL
grep -rn "ExecuteSqlRaw.*+" Services/ Controllers/ --include="*.cs"
# Expected: No unsafe concatenation
```

**Pass Criteria:**
- ✅ No SQL injection vulnerabilities
- ✅ All queries parameterized
- ✅ No direct SQL string concatenation

**Status:** ⚠️ PENDING

---

### TEST-SEC-003: Verify API Authentication
**Priority:** High
**Environment:** All

**Test Steps:**
1. Call protected endpoints without auth
2. Verify 401 Unauthorized response
3. Check authentication middleware

**Test Endpoints:**
```bash
# Public endpoint (should work)
curl "http://localhost:5101/api/certificate-tiles"
# Expected: 200 OK

# Protected endpoint (should fail without auth)
curl -X POST "http://localhost:5101/api/v1/certificates"
# Expected: 401 Unauthorized
```

**Pass Criteria:**
- ✅ Public endpoints accessible
- ✅ Protected endpoints return 401
- ✅ JWT validation working

**Status:** ⚠️ PENDING

---

### TEST-SEC-004: Verify HTTPS in Production
**Priority:** Critical
**Environment:** Production

**Test Steps:**
1. Check production connection string
2. Verify SSL/TLS encryption
3. Test HTTPS redirect

**Configuration Check:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...;Encrypt=true;TrustServerCertificate=false"
  }
}
```

**Test HTTPS:**
```bash
# Try HTTP (should redirect to HTTPS)
curl -I http://your-production-domain.com
# Expected: 301/302 redirect to HTTPS

# Test HTTPS
curl -I https://your-production-domain.com
# Expected: 200 OK with HSTS headers
```

**Pass Criteria:**
- ✅ HTTPS enforced
- ✅ SSL/TLS certificate valid
- ✅ Database encryption enabled

**Status:** ⚠️ PENDING

---

### TEST-SEC-005: Verify Disable Test Endpoints in Production
**Priority:** Critical
**Environment:** Production

**Test Steps:**
1. Attempt to access SeedDatabase endpoint in production
2. Verify 403 Forbidden or 404 Not Found
3. Check environment guards

**Test Request:**
```bash
curl -X POST "https://production-domain.com/Export/SeedDatabase"
# Expected: 403 Forbidden or 404 Not Found
```

**Code Check:**
```csharp
[HttpPost]
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> SeedDatabase()
{
    if (_environment.IsProduction())
    {
        return StatusCode(403, "Seeding not allowed in production");
    }
    // ...
}
```

**Pass Criteria:**
- ✅ Endpoint disabled in production
- ✅ Environment check in place
- ✅ Authorization required

**Status:** ⚠️ PENDING

---

## Test Execution Instructions

### Running All Tests

```bash
# 1. Run database connection tests
dotnet test --filter "Category=DatabaseConnection"

# 2. Run data loading tests
dotnet test --filter "Category=DataLoading"

# 3. Run API tests
dotnet test --filter "Category=APIEndpoint"

# 4. Run frontend tests
dotnet test --filter "Category=Frontend"

# 5. Run performance tests
dotnet test --filter "Category=Performance"

# 6. Run security tests
dotnet test --filter "Category=Security"
```

### Manual Test Checklist

- [ ] TEST-DB-001: SQL Server Connection (Dev)
- [ ] TEST-DB-002: SQL Server Connection (Prod)
- [ ] TEST-DB-003: Database Seeding
- [ ] TEST-DB-004: Entity Relationships
- [ ] TEST-DB-005: No SQLite/InMemory
- [ ] TEST-DL-001: Certificate Data from DB
- [ ] TEST-DL-002: Company Data from DB
- [ ] TEST-DL-003: Product Data from DB
- [ ] TEST-DL-004: Certification Types from DB
- [ ] TEST-DL-005: Pagination Data
- [ ] TEST-DL-006: Search Results from DB
- [ ] TEST-DL-007: Filtering from DB
- [ ] TEST-DL-008: Sort Options from DB
- [ ] TEST-DL-009: Autocomplete from DB
- [ ] TEST-DL-010: Statistics from DB
- [ ] TEST-DL-011: Product IDs from DB
- [ ] TEST-DL-012: User Data from DB
- [ ] TEST-API-001: GET /api/certificate-tiles
- [ ] TEST-API-002: GET /api/certificate-tiles/filters
- [ ] TEST-API-003: GET /api/certificate-tiles/autocomplete
- [ ] TEST-API-004: GET /api/certificate-tiles/{id}/download
- [ ] TEST-API-005: POST /api/certificate-tiles/share-link
- [ ] TEST-API-006: Rate Limiting
- [ ] TEST-API-007: Error Handling
- [ ] TEST-API-008: Response Times
- [ ] TEST-FE-001: Certificate Tiles Load
- [ ] TEST-FE-002: Infinite Scroll
- [ ] TEST-FE-003: Search Functionality
- [ ] TEST-FE-004: Filter Functionality
- [ ] TEST-FE-005: Autocomplete Suggestions
- [ ] TEST-FE-006: Shareable URLs
- [ ] TEST-PERF-001: Load 1000 Certificates
- [ ] TEST-PERF-002: Concurrent Users
- [ ] TEST-PERF-003: Search Performance
- [ ] TEST-PERF-004: Infinite Scroll Performance
- [ ] TEST-SEC-001: No Hardcoded Credentials
- [ ] TEST-SEC-002: SQL Injection Protection
- [ ] TEST-SEC-003: API Authentication
- [ ] TEST-SEC-004: HTTPS in Production
- [ ] TEST-SEC-005: Disable Test Endpoints

---

## Test Results Summary

**Date:** [To be filled]
**Tester:** [To be filled]
**Environment:** [To be filled]

| Test Suite | Passed | Failed | Skipped | Total |
|------------|--------|--------|---------|-------|
| Database Connection | 0 | 0 | 0 | 5 |
| Data Loading | 0 | 0 | 0 | 12 |
| API Endpoints | 0 | 0 | 0 | 8 |
| Frontend Integration | 0 | 0 | 0 | 6 |
| Performance | 0 | 0 | 0 | 4 |
| Security | 0 | 0 | 0 | 5 |
| **TOTAL** | **0** | **0** | **0** | **40** |

---

**Approved By:**
- [ ] Development Lead
- [ ] QA Lead
- [ ] Technical Architect

**Date Approved:** __________
