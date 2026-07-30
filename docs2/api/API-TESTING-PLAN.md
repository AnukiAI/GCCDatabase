# GCC API Testing Plan - End-to-End Documentation

**Version:** 1.0
**Date:** 2026-01-18
**Focus:** Amazon Partner Integration (Primary), Novi (Future)

---

## Table of Contents

1. [Overview](#1-overview)
2. [API Architecture](#2-api-architecture)
3. [Authentication Testing](#3-authentication-testing)
4. [Endpoint Inventory](#4-endpoint-inventory)
5. [Amazon Integration Test Cases](#5-amazon-integration-test-cases)
6. [Field Mapping Verification](#6-field-mapping-verification)
7. [Edge Cases & Error Handling](#7-edge-cases--error-handling)
8. [Security Testing](#8-security-testing)
9. [Performance & Rate Limiting](#9-performance--rate-limiting)
10. [Integration Test Scripts](#10-integration-test-scripts)
11. [Gaps & Issues Identified](#11-gaps--issues-identified)
12. [Test Checklist](#12-test-checklist)

---

## 1. Overview

### Purpose
This document provides comprehensive API testing coverage for the GCC (Green Circle Certified) Partner API, with primary focus on Amazon integration. Partners PULL certificate data from GCC using API Keys.

### Scope
- Partner API authentication via `X-API-Key` header
- Certificate retrieval endpoints
- Product ID search functionality
- PDF download capability
- Field mapping for Amazon requirements

### Out of Scope
- Internal/Admin API endpoints
- Web UI testing
- Push notifications/webhooks (Phase 2)

---

## 2. API Architecture

### Base URLs
```
Development:  http://localhost:5101/api/v1
Production:   https://[production-domain]/api/v1
Swagger:      http://localhost:5101/swagger
```

### Authentication Flow
```
1. Partner receives API key from GCC Admin (prefix: gcc_partner_)
2. Partner includes X-API-Key header in all requests
3. Middleware validates key and sets partner context
4. Response filtered by partner association
```

### API Controllers Hierarchy
```
/api/v1/certificates          → CertificatesController (v1) - PRIMARY PARTNER API
/api/v1/apikeys               → ApiKeysController (v1) - Admin only
/api/v1/products              → ProductsController (v1)
/api/v1/metrics               → MetricsController (v1)
/api/CertificatesApi          → CertificatesApiController - Legacy (internal use)
/api/productids               → ProductIdApiController - Internal use
/api/admin/partner            → PartnerApiController - Admin management
/api/admin/amazon             → AmazonApiController - Amazon settings
```

---

## 3. Authentication Testing

### 3.1 API Key Format
```
Partner keys: gcc_partner_[32-character-hash]
Example:      gcc_partner_9f8e7d6c5b4a3210abcdef1234567890
```

### 3.2 Authentication Test Cases

| Test ID | Test Case | Expected Result | Priority |
|---------|-----------|-----------------|----------|
| AUTH-001 | Valid partner API key | 200 OK, data returned | Critical |
| AUTH-002 | Invalid API key | 401 Unauthorized | Critical |
| AUTH-003 | Expired API key | 401 Unauthorized | Critical |
| AUTH-004 | Missing X-API-Key header | 401 Unauthorized | Critical |
| AUTH-005 | Empty API key value | 401 Unauthorized | High |
| AUTH-006 | Malformed API key | 401 Unauthorized | High |
| AUTH-007 | Revoked API key | 401 Unauthorized | Critical |
| AUTH-008 | API key without partner prefix | Handled by different auth handler | Medium |
| AUTH-009 | Case sensitivity of header name | Should be case-insensitive | Medium |
| AUTH-010 | Multiple API keys in header | Use first valid key | Low |

### 3.3 Sample Authentication Test Requests

```bash
# Valid authentication
curl -X GET "http://localhost:5101/api/v1/certificates" \
  -H "X-API-Key: gcc_partner_9f8e7d6c5b4a3210abcdef1234567890"

# Missing authentication
curl -X GET "http://localhost:5101/api/v1/certificates"
# Expected: 401 Unauthorized

# Invalid key
curl -X GET "http://localhost:5101/api/v1/certificates" \
  -H "X-API-Key: invalid_key_12345"
# Expected: 401 Unauthorized
```

---

## 4. Endpoint Inventory

### 4.1 Partner-Accessible Endpoints

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/api/v1/certificates` | GET | List certificates with pagination | Yes |
| `/api/v1/certificates/{id}` | GET | Get certificate by database ID | Yes |
| `/api/v1/certificates/number/{number}` | GET | Get certificate by number (e.g., 24-0366) | Yes |
| `/api/v1/certificates/search` | GET | Advanced search with filters | Yes |
| `/api/v1/certificates/search/productid` | GET | Search by ASIN/UPC/GTIN | Yes |
| `/api/v1/certificates/expiring` | GET | Get expiring certificates | Yes |
| `/api/v1/certificates/{id}/download` | GET | Download certificate PDF | Yes |

### 4.2 Query Parameters

**GET /api/v1/certificates**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `pageNumber` | int | 1 | Page number for pagination |
| `pageSize` | int | 20 | Items per page (max: 100) |
| `searchTerm` | string | null | Search across number, product, company |
| `sortBy` | string | null | certificatenumber, issuedate, expirationdate, productname, companyname |
| `sortDescending` | bool | false | Sort direction |
| `includeProductIds` | bool | false | **CRITICAL FOR AMAZON** - Include ASIN/UPC/GTIN |

**GET /api/v1/certificates/search/productid**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `productId` | string | Yes | The product identifier value (ASIN, UPC, etc.) |
| `type` | string | No | Filter by type: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |

---

## 5. Amazon Integration Test Cases

### 5.1 Primary Use Cases

| Test ID | Test Case | Endpoint | Priority |
|---------|-----------|----------|----------|
| AMZ-001 | List all certificates with ASINs | GET /certificates?includeProductIds=true | Critical |
| AMZ-002 | Search by specific ASIN | GET /certificates/search/productid?productId=B08XQRT456&type=ASIN | Critical |
| AMZ-003 | Get certificate by number | GET /certificates/number/24-0366 | Critical |
| AMZ-004 | Download certificate PDF | GET /certificates/24-0366/download | Critical |
| AMZ-005 | Verify CertificationResults field present | All endpoints | Critical |
| AMZ-006 | Pagination with large result sets | GET /certificates?pageSize=100&pageNumber=1 | High |
| AMZ-007 | Filter by status (Active only) | GET /certificates/search?status=Active | High |
| AMZ-008 | Search by UPC | GET /certificates/search/productid?productId=012345678901&type=UPC | Medium |

### 5.2 Amazon Field Requirements

| Amazon Field | GCC API Field | Location | Required |
|--------------|---------------|----------|----------|
| Product ASIN | productIdentifiers[].value (type=ASIN) | includeProductIds=true | Yes |
| Brand name | brandName | Response body | Yes |
| Product name | productName | Response body | Yes |
| Certificate ID | certificateNumber | Response body | Yes |
| Issue date | issueDate | Response body | Yes |
| Expiration date | expirationDate | Response body | Yes |
| Certification name | certificationName | Response body | Yes |
| Issuer | issuer (static: "Green Circle Certified") | Response body | Yes |
| Standard version | standardVersion | Response body | No |
| **Certification Results** | certificationResults | Response body | **Yes (Amazon threshold)** |
| PDF download URL | pdfDownloadUrl | Response body | Yes |

### 5.3 Sample Amazon Request/Response

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates?includeProductIds=true&pageSize=100" \
  -H "X-API-Key: gcc_partner_amazon_abc123..." \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 123,
      "certificateNumber": "24-0366",
      "status": "Active",
      "issueDate": "2024-01-15T00:00:00Z",
      "expirationDate": "2025-01-15T00:00:00Z",
      "location": null,
      "country": "USA",
      "partner": "Amazon",
      "certificationResults": "95.5% recycled content verified",
      "brandName": "EcoProduct Co",
      "productName": "Recycled Paper Towels",
      "certificationName": "UL 2809",
      "issuer": "Green Circle Certified",
      "standardVersion": "Level 2",
      "productIdentifiers": [
        {
          "type": "ASIN",
          "value": "B08XQRT456",
          "partner": "Amazon",
          "status": "Active",
          "productName": "Recycled Paper Towels - 12 Pack",
          "productSku": "RPT-12PK-ECO",
          "description": "12-pack of 100% recycled paper towels"
        },
        {
          "type": "UPC",
          "value": "012345678901",
          "partner": null,
          "status": "Active",
          "productName": "Recycled Paper Towels",
          "productSku": "RPT-SINGLE",
          "description": "Single roll recycled paper towels"
        }
      ],
      "pdfDownloadUrl": "/api/v1/certificates/24-0366/download"
    }
  ],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 100,
  "totalPages": 2,
  "hasNext": true,
  "hasPrevious": false
}
```

---

## 6. Field Mapping Verification

### 6.1 Response Field Tests

| Test ID | Field | Test | Expected |
|---------|-------|------|----------|
| FLD-001 | certificateNumber | Format check | YY-NNNN (e.g., 24-0366) |
| FLD-002 | issueDate | ISO 8601 format | 2024-01-15T00:00:00Z |
| FLD-003 | expirationDate | ISO 8601 format | 2025-01-15T00:00:00Z |
| FLD-004 | status | Valid enum value | Draft, Active, Expired, etc. |
| FLD-005 | issuer | Static value | "Green Circle Certified" |
| FLD-006 | certificationResults | String present | May be null/empty |
| FLD-007 | productIdentifiers | Array structure | type, value, partner, status, productName, productSku, description |
| FLD-008 | pdfDownloadUrl | Valid URL format | /api/v1/certificates/{number}/download |

### 6.1.1 Product Identifier Fields

Each item in the `productIdentifiers` array contains:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| type | string | Yes | ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |
| value | string | Yes | The product identifier value |
| status | string | Yes | Active, Inactive, Pending, Expired |
| partner | string | Yes | Amazon, Novi, None, Other |
| productName | string | No | Product name specific to this identifier |
| productSku | string | No | SKU for this product identifier |
| description | string | No | Optional description or notes |

### 6.2 Data Integrity Tests

| Test ID | Test Case | Expected |
|---------|-----------|----------|
| INT-001 | Certificate has valid company | brandName not null |
| INT-002 | Certificate has valid product | productName not null |
| INT-003 | Certificate has certification type | certificationName not null |
| INT-004 | Active certificate has PDF | Download returns 200 or appropriate error |
| INT-005 | Product IDs are Active status only | No "Inactive" product IDs in response |

---

## 7. Edge Cases & Error Handling

### 7.1 Error Response Format

```json
{
  "success": false,
  "message": "Error description",
  "errorCode": "ERROR_CODE",
  "traceId": "optional-trace-id"
}
```

### 7.2 Edge Case Tests

| Test ID | Test Case | Expected Response |
|---------|-----------|-------------------|
| ERR-001 | Certificate ID = 0 | 400 Bad Request |
| ERR-002 | Certificate ID = -1 | 400 Bad Request |
| ERR-003 | Certificate ID = 999999999 (not exists) | 404 Not Found |
| ERR-004 | Certificate number not found | 404 Not Found |
| ERR-005 | Invalid pageNumber (0) | 400 Bad Request or default to 1 |
| ERR-006 | Invalid pageSize (0) | 400 Bad Request or default |
| ERR-007 | pageSize > 100 | Capped at 100 |
| ERR-008 | Invalid sortBy value | Default sort applied |
| ERR-009 | Empty productId search | 400 Bad Request |
| ERR-010 | Invalid product ID type | 400 Bad Request with valid types listed |
| ERR-011 | Product ID not found | 200 with empty data array |
| ERR-012 | PDF file missing | 404 Not Found |
| ERR-013 | SQL injection attempt | Parameterized query protection |
| ERR-014 | XSS in search term | Sanitized/escaped |

### 7.3 Boundary Tests

| Test ID | Test Case | Input | Expected |
|---------|-----------|-------|----------|
| BND-001 | Max page size | pageSize=100 | Returns 100 items max |
| BND-002 | Over max page size | pageSize=1000 | Capped at 100 |
| BND-003 | Very large page number | pageNumber=999999 | Empty result set |
| BND-004 | Long search term | 500 character string | Handled gracefully |
| BND-005 | Special characters in search | "Test & Co." | Properly escaped |
| BND-006 | Unicode in search | "Compania Test" | Handled correctly |

---

## 8. Security Testing

### 8.1 Authentication Security

| Test ID | Test Case | Expected |
|---------|-----------|----------|
| SEC-001 | HTTP (non-HTTPS) in production | 401 or redirect to HTTPS |
| SEC-002 | API key in URL query string | Rejected (header only) |
| SEC-003 | API key logged in clear text | Not logged (check logs) |
| SEC-004 | Brute force API key attempts | Rate limited |
| SEC-005 | Timing attack on key validation | Constant-time comparison |

### 8.2 Authorization Security

| Test ID | Test Case | Expected |
|---------|-----------|----------|
| SEC-006 | Partner sees only their certificates | Filtered by partner association |
| SEC-007 | Access admin endpoints with partner key | 403 Forbidden |
| SEC-008 | Create/Update/Delete with partner key | 403 Forbidden or not allowed |
| SEC-009 | Access other partner's data | Not visible |

### 8.3 Injection Prevention

| Test ID | Test Case | Payload | Expected |
|---------|-----------|---------|----------|
| INJ-001 | SQL injection in search | `'; DROP TABLE--` | Sanitized, no error |
| INJ-002 | SQL injection in ID | `1 OR 1=1` | 400 Bad Request |
| INJ-003 | XSS in response | `<script>alert('xss')</script>` | HTML encoded |
| INJ-004 | Path traversal in download | `../../../etc/passwd` | 404 or sanitized |

---

## 9. Performance & Rate Limiting

### 9.1 Rate Limit Configuration

| Metric | Partner API Limit |
|--------|------------------|
| Requests per hour | 2,000 |
| Requests per minute | ~33 |
| Concurrent requests | TBD |

### 9.2 Performance Tests

| Test ID | Test Case | Target | Metric |
|---------|-----------|--------|--------|
| PERF-001 | Single certificate retrieval | < 200ms | Response time |
| PERF-002 | List 100 certificates | < 500ms | Response time |
| PERF-003 | List with includeProductIds | < 1000ms | Response time |
| PERF-004 | Product ID search | < 500ms | Response time |
| PERF-005 | PDF download | < 2000ms | Response time |
| PERF-006 | Concurrent requests (10) | No errors | Success rate |
| PERF-007 | Rate limit exceeded | 429 Too Many Requests | Status code |

### 9.3 Rate Limit Test Script

```bash
# Test rate limiting (100 requests in quick succession)
for i in {1..100}; do
  curl -s -o /dev/null -w "%{http_code}\n" \
    -H "X-API-Key: gcc_partner_test_key" \
    "http://localhost:5101/api/v1/certificates?pageSize=1" &
done
wait

# Should see 429 responses after hitting limit
```

---

## 10. Integration Test Scripts

### 10.1 PowerShell Test Script

```powershell
# GCC API Integration Test Script
# Save as: Test-GccApi.ps1

param(
    [string]$BaseUrl = "http://localhost:5101",
    [string]$ApiKey = "gcc_partner_test_key"
)

$headers = @{
    "X-API-Key" = $ApiKey
    "Accept" = "application/json"
}

function Test-Endpoint {
    param($Name, $Url, $ExpectedStatus)

    try {
        $response = Invoke-WebRequest -Uri $Url -Headers $headers -Method Get -ErrorAction Stop
        $status = $response.StatusCode
        $passed = ($status -eq $ExpectedStatus)
        Write-Host "$Name : $status $(if($passed){'PASS'}else{'FAIL'})" -ForegroundColor $(if($passed){'Green'}else{'Red'})
        return $passed
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        $passed = ($status -eq $ExpectedStatus)
        Write-Host "$Name : $status $(if($passed){'PASS'}else{'FAIL'})" -ForegroundColor $(if($passed){'Green'}else{'Red'})
        return $passed
    }
}

Write-Host "`n=== GCC API Integration Tests ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"
Write-Host "API Key: $($ApiKey.Substring(0,20))...`n"

# Test cases
$results = @()
$results += Test-Endpoint "List Certificates" "$BaseUrl/api/v1/certificates" 200
$results += Test-Endpoint "List with ProductIds" "$BaseUrl/api/v1/certificates?includeProductIds=true" 200
$results += Test-Endpoint "Certificate by ID" "$BaseUrl/api/v1/certificates/1" 200
$results += Test-Endpoint "Certificate by Number" "$BaseUrl/api/v1/certificates/number/24-0001" 200
$results += Test-Endpoint "Search by ASIN" "$BaseUrl/api/v1/certificates/search/productid?productId=B08TEST&type=ASIN" 200
$results += Test-Endpoint "Expiring Certificates" "$BaseUrl/api/v1/certificates/expiring?days=30" 200
$results += Test-Endpoint "Invalid ID" "$BaseUrl/api/v1/certificates/999999" 404

# Summary
$passed = ($results | Where-Object { $_ }).Count
$total = $results.Count
Write-Host "`n=== Results: $passed/$total tests passed ===" -ForegroundColor $(if($passed -eq $total){'Green'}else{'Yellow'})
```

### 10.2 cURL Test Script (Bash)

```bash
#!/bin/bash
# GCC API Integration Test Script
# Save as: test-gcc-api.sh

BASE_URL="${1:-http://localhost:5101}"
API_KEY="${2:-gcc_partner_test_key}"

echo "=== GCC API Integration Tests ==="
echo "Base URL: $BASE_URL"
echo ""

test_endpoint() {
    local name=$1
    local url=$2
    local expected=$3

    status=$(curl -s -o /dev/null -w "%{http_code}" \
        -H "X-API-Key: $API_KEY" \
        -H "Accept: application/json" \
        "$url")

    if [ "$status" == "$expected" ]; then
        echo "PASS: $name (HTTP $status)"
    else
        echo "FAIL: $name (Expected $expected, Got $status)"
    fi
}

# Authentication tests
test_endpoint "Valid API Key" "$BASE_URL/api/v1/certificates?pageSize=1" "200"

# List endpoints
test_endpoint "List Certificates" "$BASE_URL/api/v1/certificates" "200"
test_endpoint "List with ProductIds" "$BASE_URL/api/v1/certificates?includeProductIds=true" "200"
test_endpoint "Pagination" "$BASE_URL/api/v1/certificates?pageNumber=1&pageSize=10" "200"

# Get by ID/Number
test_endpoint "Get by ID" "$BASE_URL/api/v1/certificates/1" "200"
test_endpoint "Get by Number" "$BASE_URL/api/v1/certificates/number/24-0001" "200"

# Search
test_endpoint "Product ID Search" "$BASE_URL/api/v1/certificates/search/productid?productId=TEST123" "200"
test_endpoint "Expiring" "$BASE_URL/api/v1/certificates/expiring?days=30" "200"

# Error cases
test_endpoint "Not Found ID" "$BASE_URL/api/v1/certificates/999999" "404"
test_endpoint "Invalid API Key" "$BASE_URL/api/v1/certificates" "401"

echo ""
echo "=== Tests Complete ==="
```

---

## 11. Gaps & Issues Identified

### 11.1 Critical Gaps

| ID | Gap | Impact | Status | Recommendation |
|----|-----|--------|--------|----------------|
| GAP-001 | No partner-specific data filtering | Partners may see other partner data | **VERIFY** | Confirm partner filtering in middleware |
| GAP-002 | CertificationResults visibility | Amazon requires this field | **VERIFIED OK** | Field is exposed in all endpoints |
| GAP-003 | PDF download for missing files | Returns error instead of generating | Low | Implement dynamic PDF generation |

### 11.2 Documentation Mismatches

| ID | Documentation | Implementation | Status |
|----|---------------|----------------|--------|
| DOC-001 | `/api/v1/certificates/number/{number}` | Actually `/api/v1/certificates/number/{certificateNumber}` | Minor - Both work |
| DOC-002 | API Reference shows sample response | Actual fields slightly different | Update docs |

### 11.3 Questions Requiring Clarification

| ID | Question | Impact | Answer Required |
|----|----------|--------|-----------------|
| Q-001 | What specific certification result format does Amazon expect? | High | Before go-live |
| Q-002 | Should expired certificates be visible to partners? | Medium | Policy decision |
| Q-003 | What is the certificate number format validation? | Medium | YY-NNNN confirmed |
| Q-004 | Rate limit per partner or per API key? | Medium | Per API key |

---

## 12. Test Checklist

### Pre-Integration Checklist

- [ ] API key generated for Amazon partner
- [ ] Endpoint permissions configured for Amazon key
- [ ] Rate limits verified (2,000/hour)
- [ ] HTTPS certificate valid (production)
- [ ] Test data with CertificationResults populated
- [ ] Test certificates with multiple ASINs

### Integration Test Checklist

**Authentication (AUTH)**
- [ ] AUTH-001: Valid partner API key returns 200
- [ ] AUTH-002: Invalid API key returns 401
- [ ] AUTH-003: Expired API key returns 401
- [ ] AUTH-004: Missing header returns 401
- [ ] AUTH-007: Revoked key returns 401

**Amazon Use Cases (AMZ)**
- [ ] AMZ-001: List with includeProductIds returns ASINs
- [ ] AMZ-002: Search by ASIN returns correct certificate
- [ ] AMZ-003: Get by certificate number works
- [ ] AMZ-004: PDF download works
- [ ] AMZ-005: CertificationResults field present

**Field Validation (FLD)**
- [ ] FLD-001: certificateNumber format correct
- [ ] FLD-002: issueDate in ISO 8601
- [ ] FLD-003: expirationDate in ISO 8601
- [ ] FLD-005: issuer = "Green Circle Certified"
- [ ] FLD-006: certificationResults field exists

**Error Handling (ERR)**
- [ ] ERR-003: Non-existent ID returns 404
- [ ] ERR-004: Non-existent number returns 404
- [ ] ERR-011: Product ID not found returns empty array
- [ ] ERR-012: Missing PDF returns 404

**Security (SEC)**
- [ ] SEC-006: Partner sees only their certificates
- [ ] SEC-008: Partner cannot create/update/delete
- [ ] INJ-001: SQL injection prevented

**Performance (PERF)**
- [ ] PERF-001: Single retrieval < 200ms
- [ ] PERF-002: List 100 < 500ms
- [ ] PERF-007: Rate limit returns 429

### Post-Integration Verification

- [ ] Amazon receives valid certificate data
- [ ] CertificationResults values meet threshold requirements
- [ ] PDF downloads successfully on Amazon side
- [ ] No data exposure issues identified
- [ ] Response times acceptable

---

## Appendix A: Status Codes Reference

| Code | Meaning | When Used |
|------|---------|-----------|
| 200 | OK | Successful GET |
| 201 | Created | Successful POST (not for partners) |
| 204 | No Content | Successful DELETE (not for partners) |
| 400 | Bad Request | Invalid parameters |
| 401 | Unauthorized | Missing/invalid API key |
| 403 | Forbidden | Endpoint not allowed for key |
| 404 | Not Found | Resource doesn't exist |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server-side error |

## Appendix B: API Key Error Codes

| Error Code | Description |
|------------|-------------|
| `UNAUTHORIZED` | Invalid or missing API key |
| `FORBIDDEN` | Endpoint not permitted |
| `VALIDATION_ERROR` | Request validation failed |
| `NOT_FOUND` | Resource not found |
| `INTERNAL_ERROR` | Server error |
| `RATE_LIMIT_EXCEEDED` | Too many requests |

---

*Document maintained by: Development Team*
*Last updated: 2026-01-18*
