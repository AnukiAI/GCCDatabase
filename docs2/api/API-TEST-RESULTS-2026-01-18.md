# GCC API Test Results Report

**Date:** 2026-01-18
**Environment:** Development (localhost:5101)
**Tester:** Automated via curl
**Database:** SQL Server (ANUKI01)

---

## Executive Summary

| Category | Passed | Failed | Total | Status |
|----------|--------|--------|-------|--------|
| Authentication | 4 | 0 | 4 | PASS |
| Certificate Retrieval | 4 | 0 | 4 | PASS |
| Product ID Search | 3 | 0 | 3 | PASS |
| PDF Download | 0 | 2 | 2 | **FAIL** |
| Advanced Search | 2 | 0 | 2 | PASS |
| **TOTAL** | **13** | **2** | **15** | **87% PASS** |

---

## Test Environment Configuration

### Active API Keys Found

| ID | Partner | API Key (truncated) | Status | Expiration |
|----|---------|---------------------|--------|------------|
| 2 | Amazon | `gcc_partner_b451fb3b...` | Active | None |
| 1 | Amazon | `gcc_partner_959615...` | Active | Unknown |
| 3 | Novi | `gcc_partner_8c147b...` | **Revoked** | Expired |

### Partners Configured

| ID | Name | Status |
|----|------|--------|
| 1 | Amazon | Active |
| 2 | Novi | Active |
| 3 | Other | Active |
| 5 | Wayfair | Active |

---

## Detailed Test Results

### 1. Authentication Tests

#### AUTH-001: Valid Partner API Key
```
Endpoint: GET /api/v1/certificates?pageSize=2
Header: X-API-Key: gcc_partner_b451fb3b31c645e9dd900c3c7b2f6250fbb7480bbb4510d602efc5e52102fc4d
```
**Result:** PASS (HTTP 200)
- Returns 184 certificates
- Pagination working correctly
- Response includes all expected fields

#### AUTH-002: Invalid API Key
```
Endpoint: GET /api/v1/certificates?pageSize=1
Header: X-API-Key: invalid_key
```
**Result:** PASS (HTTP 401)
- Returns empty response with 401 status

#### AUTH-003: No Authentication Header
```
Endpoint: GET /api/v1/certificates?pageSize=1
Header: (none)
```
**Result:** PASS (HTTP 401)
- API correctly rejects unauthenticated requests

#### AUTH-004: Revoked API Key
```
Endpoint: GET /api/v1/certificates?pageSize=1
Header: X-API-Key: gcc_partner_8c147b7e4e1dfb11900883446f79c2ed861c0d72bd133114ff6e2aa87904c9d6
```
**Result:** PASS (HTTP 401)
```json
{
  "success": false,
  "errors": [{
    "code": "UNAUTHORIZED",
    "message": "Unauthorized: Invalid or expired API key"
  }]
}
```

---

### 2. Certificate Retrieval Tests

#### CERT-001: List Certificates (Basic)
```
Endpoint: GET /api/v1/certificates?pageSize=2
```
**Result:** PASS (HTTP 200)
```json
{
  "pagination": {
    "totalCount": 184,
    "pageNumber": 1,
    "pageSize": 2,
    "totalPages": 92
  },
  "success": true,
  "data": [...]
}
```

**Fields Returned:**
- id, certificateNumber, status
- effectiveDate, expirationDate
- location, country
- partner, certificationResults
- productName, productId
- companyName, companyId
- certificationType, certificationTypeId
- createdAt, updatedAt

#### CERT-002: List Certificates with Product IDs (Amazon Requirement)
```
Endpoint: GET /api/v1/certificates?pageSize=1&includeProductIds=true
```
**Result:** PASS (HTTP 200)

**Critical Amazon Fields Verified:**

| Field | Present | Example Value |
|-------|---------|---------------|
| certificateNumber | YES | "25-5047" |
| brandName | YES | "Milliken & Company" |
| productName | YES | "Bamboo Coffee Cup with Lid" |
| certificationName | YES | "Certified Environmental Facts" |
| issuer | YES | "Green Circle Certified" |
| standardVersion | YES | null (optional) |
| issueDate | YES | "2026-09-14T00:00:00" |
| expirationDate | YES | "2028-09-14T00:00:00" |
| certificationResults | YES | null (field exists) |
| productIdentifiers | YES | Array with ASIN, UPC |
| pdfDownloadUrl | YES | "/api/v1/certificates/25-5047/download" |

**Product Identifiers Sample:**
```json
"productIdentifiers": [
  {
    "type": "ASIN",
    "value": "B08N5WRWNW",
    "partner": "Amazon",
    "status": "Active",
    "productName": "Sample Product",
    "productSku": "SKU123",
    "description": "Sample description"
  },
  {
    "type": "UPC",
    "value": "123456789012",
    "partner": "None",
    "status": "Active",
    "productName": "Another Product",
    "productSku": "SKU456",
    "description": "Another description"
  }
]
```

#### CERT-003: Get Certificate by ID
```
Endpoint: GET /api/v1/certificates/75
```
**Result:** PASS (HTTP 200)
- Returns full certificate details with nested company/product objects

#### CERT-004: Get Certificate by Number
```
Endpoint: GET /api/v1/certificates/number/25-5047
```
**Result:** PASS (HTTP 200)
- Returns same data as by-ID lookup

#### CERT-005: Get Certificate - Not Found
```
Endpoint: GET /api/v1/certificates/999999
```
**Result:** PASS (HTTP 404)
```json
{
  "success": false,
  "errors": [{
    "code": "RESOURCE_NOT_FOUND",
    "message": "Certificate with ID 999999 not found"
  }]
}
```

---

### 3. Product ID Search Tests (Critical for Amazon)

#### SEARCH-001: Search by ASIN
```
Endpoint: GET /api/v1/certificates/search/productid?productId=B08N5WRWNW&type=ASIN
```
**Result:** PASS (HTTP 200)
```json
{
  "success": true,
  "data": [{
    "id": 75,
    "certificateNumber": "25-5047",
    "productName": "Bamboo Coffee Cup with Lid",
    "company": "Milliken & Company",
    "certificationResults": null,
    "status": "Draft",
    "pdfUrl": "/api/v1/certificates/25-5047/download",
    "productIds": [
      {
        "type": "ASIN",
        "value": "B08N5WRWNW",
        "partner": "Amazon",
        "status": "Active",
        "productName": "Sample Product",
        "productSku": "SKU123",
        "description": "Sample description",
        "isMatchedId": true
      },
      {
        "type": "UPC",
        "value": "123456789012",
        "partner": "None",
        "status": "Active",
        "productName": "Another Product",
        "productSku": "SKU456",
        "description": "Another description",
        "isMatchedId": false
      }
    ]
  }],
  "message": "Found 1 certificate(s) with 2 total product IDs for search: B08N5WRWNW"
}
```

**Key Feature:** Returns ALL product IDs for certificate, not just matched one. `isMatchedId` flag indicates which one matched.

#### SEARCH-002: Search by UPC
```
Endpoint: GET /api/v1/certificates/search/productid?productId=123456789012&type=UPC
```
**Result:** PASS (HTTP 200)
- Returns same certificate with matched UPC flagged

#### SEARCH-003: Search - Product ID Not Found
```
Endpoint: GET /api/v1/certificates/search/productid?productId=NOTFOUND123&type=ASIN
```
**Result:** PASS (HTTP 200)
```json
{
  "success": true,
  "data": [],
  "message": "No certificates found for product ID: NOTFOUND123 (type: ASIN)"
}
```

---

### 4. PDF Download Tests

#### PDF-001: Download by Certificate Number
```
Endpoint: GET /api/v1/certificates/25-5047/download
```
**Result:** FAIL (HTTP 406 Not Acceptable)

**Issue:** Content negotiation failure. The endpoint has `[Produces("application/pdf")]` attribute which causes 406 when client doesn't specify exact Accept header.

**Evidence:**
- File exists: `uploads/certificates/25-5047_20260113_052520.pdf`
- Even with `Accept: application/pdf` header, returns 406
- Even with `Accept: */*` header, returns 406

#### PDF-002: Download - Certificate Not Found
```
Endpoint: GET /api/v1/certificates/99-9999/download
```
**Result:** FAIL (HTTP 406)
- Should return 404, but returns 406 due to same content negotiation issue

---

### 5. Advanced Search Tests

#### ADV-001: Search with Term and Status Filter
```
Endpoint: GET /api/v1/certificates/search?term=Bamboo&status=Draft
```
**Result:** PASS (HTTP 200)
- Returns 2 matching certificates
- Includes full certificate objects with relationships

#### ADV-002: Expiring Certificates
```
Endpoint: GET /api/v1/certificates/expiring?days=365
```
**Result:** PASS (HTTP 200)
- Returns certificates expiring within 365 days
- Pagination working (4 pages of results)

---

## Critical Issues Found

### BUG-001: PDF Download Returns 406 Not Acceptable

**Severity:** HIGH
**Impact:** Partners cannot download certificate PDFs via API
**Affected Endpoints:**
- `GET /api/v1/certificates/{id}/download`

**Root Cause:**
The `[Produces("application/pdf")]` Swagger attribute on the download endpoint is causing ASP.NET Core's content negotiation to fail when the client doesn't send an exact matching Accept header.

**Location:** `Controllers/Api/v1/CertificatesController.cs:869`

**Recommended Fix:**
Remove or modify the `[Produces("application/pdf")]` attribute, or configure the endpoint to bypass content negotiation for file downloads.

```csharp
// Current (problematic):
[Produces("application/pdf")]
public async Task<IActionResult> DownloadCertificate([FromRoute] string id)

// Recommended fix - remove Produces or add bypass:
[ApiExplorerSettings(IgnoreApi = false)]
public async Task<IActionResult> DownloadCertificate([FromRoute] string id)
```

---

## Field Mapping Verification for Amazon

| Amazon Required Field | GCC API Field | Present | Verified |
|----------------------|---------------|---------|----------|
| Product ASIN | productIdentifiers[type=ASIN].value | YES | B08N5WRWNW |
| Brand name | brandName | YES | Milliken & Company |
| Product name | productName | YES | Bamboo Coffee Cup |
| Certificate ID | certificateNumber | YES | 25-5047 |
| Issue date | issueDate | YES | ISO 8601 format |
| Expiration date | expirationDate | YES | ISO 8601 format |
| Certification name | certificationName | YES | Certified Environmental Facts |
| Issuer | issuer | YES | "Green Circle Certified" |
| Standard version | standardVersion | YES | null (optional) |
| **Certification Results** | certificationResults | YES | Field exists (null in test data) |
| PDF download URL | pdfDownloadUrl | YES | /api/v1/certificates/{num}/download |

### Product Identifier Extended Fields

| Field | GCC API Field | Present | Description |
|-------|---------------|---------|-------------|
| Type | productIdentifiers[].type | YES | ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |
| Value | productIdentifiers[].value | YES | The product identifier value |
| Status | productIdentifiers[].status | YES | Active, Inactive, Pending, Expired |
| Partner | productIdentifiers[].partner | YES | Amazon, Novi, None, Other |
| Product Name | productIdentifiers[].productName | YES | Optional product name for this ID |
| Product SKU | productIdentifiers[].productSku | YES | Optional SKU |
| Description | productIdentifiers[].description | YES | Optional description or notes |

---

## Recommendations

### Immediate Actions Required

1. **FIX: PDF Download 406 Error** - Critical for partner integration
   - Remove `[Produces("application/pdf")]` attribute
   - Test with Amazon integration team

2. **VERIFY: CertificationResults Data**
   - Ensure analysts are populating this field for Amazon certificates
   - Add validation to require this field for Amazon partner certificates

### Pre-Production Checklist

- [ ] Fix PDF download endpoint (BUG-001)
- [ ] Generate production API key for Amazon
- [ ] Configure rate limiting (currently 2,000/hour)
- [ ] Set up endpoint permissions if needed
- [ ] Populate CertificationResults for existing Amazon certificates
- [ ] Test with actual Amazon integration

### Nice to Have

- [ ] Add caching headers to reduce API load
- [ ] Implement webhook notifications for certificate changes
- [ ] Add bulk download endpoint for multiple PDFs

---

## Test Commands Reference

```bash
# Authentication
curl -H "X-API-Key: YOUR_KEY" "http://localhost:5101/api/v1/certificates?pageSize=1"

# List with Product IDs (Amazon)
curl -H "X-API-Key: YOUR_KEY" "http://localhost:5101/api/v1/certificates?includeProductIds=true"

# Search by ASIN
curl -H "X-API-Key: YOUR_KEY" "http://localhost:5101/api/v1/certificates/search/productid?productId=B08XQRT456&type=ASIN"

# Get by certificate number
curl -H "X-API-Key: YOUR_KEY" "http://localhost:5101/api/v1/certificates/number/24-0366"

# Download PDF (currently broken)
curl -H "X-API-Key: YOUR_KEY" "http://localhost:5101/api/v1/certificates/24-0366/download"
```

---

## Appendix: Response Time Observations

| Endpoint | Response Time | Assessment |
|----------|---------------|------------|
| List certificates (2 items) | ~150ms | Good |
| List with productIds | ~200ms | Good |
| Get by ID | ~100ms | Good |
| Search by ASIN | ~120ms | Good |
| Advanced search | ~180ms | Good |

---

*Report generated: 2026-01-18 02:17 EST*
*Application version: Development build*
