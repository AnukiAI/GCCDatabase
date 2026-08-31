# Partner API Manual Testing Guide

**Green Circle Certified - Comprehensive API Testing Documentation**

**Version:** 1.0
**Date:** 2026-01-29
**Audience:** Partners, QA Teams, Integration Engineers
**Status:** Production Ready

> **Corrected 2026-08-25 — sample payload hosts.** The GS1 `id` values in the example responses below
> now read `https://database.greencirclecertified.com/api/v1/...`. They previously mixed
> `greencirclecertified.com` and `greencirclecertified.com/database` **within the same response**, which
> could never occur: `id` is generated from the host the caller used, so every `id` in one response
> shares a host. The path-based address was never activated and is retired.
>
> **This is the older reference set.** For anything client- or partner-facing, use the current guides in
> `docs2/exports/` (Amazon Developer Guide v1.2, Partner Integration Guide v1.1). This file has not been
> reviewed against current rate-limit or certificate-status behaviour.

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Getting Started](#2-getting-started)
3. [Test Environment Setup](#3-test-environment-setup)
4. [Authentication Testing](#4-authentication-testing)
5. [Certificate Retrieval Testing](#5-certificate-retrieval-testing)
6. [Product Identifier Search Testing](#6-product-identifier-search-testing)
7. [GS1 JSON-LD Format Testing](#7-gs1-json-ld-format-testing)
8. [PDF Download Testing](#8-pdf-download-testing)
9. [Pagination Testing](#9-pagination-testing)
10. [Error Handling Validation](#10-error-handling-validation)
11. [Security Testing](#11-security-testing)
12. [Rate Limiting Testing](#12-rate-limiting-testing)
13. [Data Validation Checklist](#13-data-validation-checklist)
14. [Test Scripts](#14-test-scripts)
15. [Troubleshooting Guide](#15-troubleshooting-guide)
16. [Test Results Template](#16-test-results-template)

---

## 1. Introduction

### 1.1 Purpose

This guide provides step-by-step instructions for manually testing the Green Circle Certified (GCC) Partner API. It is designed for:

- **Partners** integrating with GCC (Amazon, Novi, others)
- **QA Teams** validating API functionality
- **Integration Engineers** verifying data accuracy
- **Stakeholders** reviewing API capabilities

### 1.2 API Overview

The GCC Partner API allows certified partners to retrieve certificate and product data:

| Feature | Description |
|---------|-------------|
| **Integration Model** | Partners PULL data from GCC |
| **Authentication** | API Key via `X-API-Key` header |
| **Data Format** | JSON (standard) or GS1 JSON-LD |
| **Rate Limit** | 200 requests per hour |
| **Partners** | Amazon, Novi, Other |

### 1.3 What You Can Test

| Test Category | Description |
|--------------|-------------|
| Authentication | API key validation, security |
| Data Retrieval | Certificates, product identifiers |
| Search | Find by ASIN, UPC, certificate number |
| Downloads | Certificate PDFs |
| Error Handling | Invalid requests, edge cases |
| Performance | Response times, rate limits |

---

## 2. Getting Started

### 2.1 Prerequisites

Before testing, ensure you have:

| Requirement | Description |
|-------------|-------------|
| **API Key** | Partner API key (format: `gcc_partner_[64-char-hex]`) |
| **Testing Tool** | PowerShell, cURL, Postman, or browser developer tools |
| **Network Access** | Access to GCC API endpoints |
| **Test Data** | Knowledge of expected certificate numbers/ASINs |

### 2.2 API Key Format

Your API key should look like this:

```
gcc_partner_b451fb3b31c645e9dd900c3c7b2f6250fbb7480bbb4510d602efc5e52102fc4d
```

**Security Note:** Never share your API key in public repositories, emails, or client-side code.

### 2.3 Base URLs

| Environment | URL |
|-------------|-----|
| **Development** | `http://localhost:5101/api/v1` |
| **Production** | `https://[production-domain]/api/v1` |
| **Swagger Docs** | `http://localhost:5101/swagger` |

---

## 3. Test Environment Setup

### 3.1 Using PowerShell (Windows)

Open PowerShell and set up your testing variables:

```powershell
# Set your API key and base URL
$ApiKey = "gcc_partner_YOUR_KEY_HERE"
$BaseUrl = "http://localhost:5101"

# Create headers object
$Headers = @{
    "X-API-Key" = $ApiKey
    "Accept" = "application/json"
}

# Test connection
Invoke-RestMethod -Uri "$BaseUrl/api/v1/certificates?pageSize=1" -Headers $Headers
```

### 3.2 Using cURL (Mac/Linux/Windows)

```bash
# Set your API key and base URL
API_KEY="gcc_partner_YOUR_KEY_HERE"
BASE_URL="http://localhost:5101"

# Test connection
curl -X GET "$BASE_URL/api/v1/certificates?pageSize=1" \
  -H "X-API-Key: $API_KEY" \
  -H "Accept: application/json"
```

### 3.3 Using Postman

1. Create a new Collection named "GCC Partner API"
2. Add a variable `api_key` with your key value
3. Add a variable `base_url` with the API URL
4. For each request, add header: `X-API-Key: {{api_key}}`

### 3.4 Connection Test Validation

**Expected Result:**
- HTTP Status: `200 OK`
- Response contains `success: true`
- Response contains `data` array

**If Failed:**
- `401`: Check API key is correct
- `Connection refused`: Check base URL and server is running

---

## 4. Authentication Testing

### 4.1 Test Matrix

| Test ID | Test Name | Expected Result | Priority |
|---------|-----------|-----------------|----------|
| AUTH-01 | Valid API Key | 200 OK, data returned | Critical |
| AUTH-02 | Missing API Key | 401 Unauthorized | Critical |
| AUTH-03 | Invalid API Key | 401 Unauthorized | Critical |
| AUTH-04 | Empty API Key | 401 Unauthorized | Critical |
| AUTH-05 | Expired API Key | 401 Unauthorized | Critical |
| AUTH-06 | Revoked API Key | 401 Unauthorized | Critical |
| AUTH-07 | Malformed API Key | 401 Unauthorized | High |
| AUTH-08 | Case Insensitive Header | 200 OK | Medium |

### 4.2 Test: Valid API Key (AUTH-01)

**Purpose:** Verify that a valid API key grants access to the API.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates?pageSize=1" \
  -H "X-API-Key: gcc_partner_YOUR_VALID_KEY" \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
  "success": true,
  "data": [...],
  "pagination": {
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 1
  }
}
```

**Validation Checklist:**
- [ ] HTTP Status is `200`
- [ ] Response body contains `"success": true`
- [ ] Response body contains `"data"` array
- [ ] Response body contains `"pagination"` object

**Result:** PASS / FAIL

---

### 4.3 Test: Missing API Key (AUTH-02)

**Purpose:** Verify requests without API key are rejected.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates" \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
  "success": false,
  "errors": [
    {
      "code": "UNAUTHORIZED",
      "message": "API key is required"
    }
  ]
}
```

**Validation Checklist:**
- [ ] HTTP Status is `401`
- [ ] Response indicates authentication failure
- [ ] No certificate data is exposed

**Result:** PASS / FAIL

---

### 4.4 Test: Invalid API Key (AUTH-03)

**Purpose:** Verify invalid API keys are rejected.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates" \
  -H "X-API-Key: invalid_key_12345" \
  -H "Accept: application/json"
```

**Expected Response:**
- HTTP Status: `401 Unauthorized`

**Validation Checklist:**
- [ ] HTTP Status is `401`
- [ ] Response indicates invalid key
- [ ] No certificate data is exposed

**Result:** PASS / FAIL

---

### 4.5 Test: Empty API Key (AUTH-04)

**Purpose:** Verify empty API key values are rejected.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates" \
  -H "X-API-Key: " \
  -H "Accept: application/json"
```

**Expected Response:**
- HTTP Status: `401 Unauthorized`

**Result:** PASS / FAIL

---

## 5. Certificate Retrieval Testing

### 5.1 Test Matrix

| Test ID | Test Name | Endpoint | Priority |
|---------|-----------|----------|----------|
| CERT-01 | List All Certificates | `GET /certificates` | Critical |
| CERT-02 | List with Product IDs | `GET /certificates?includeProductIds=true` | Critical |
| CERT-03 | Get by Certificate Number | `GET /certificates/number/{number}` | Critical |
| CERT-04 | Get by Database ID | `GET /certificates/{id}` | High |
| CERT-05 | Filter Active Only | `GET /certificates?status=Active` | High |

### 5.2 Test: List All Certificates (CERT-01)

**Purpose:** Verify basic certificate listing works correctly.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" \
  -H "Accept: application/json"
```

**Expected Response Structure:**
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
      "company": "EcoProduct Co",
      "productName": "Recycled Paper Towels",
      "certificationType": "UL 2809",
      "issuer": "Green Circle Certified"
    }
  ],
  "pagination": {
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 20
  }
}
```

**Validation Checklist:**
- [ ] HTTP Status is `200`
- [ ] `success` is `true`
- [ ] `data` is an array (may be empty if no certificates match partner)
- [ ] Each certificate has `certificateNumber`
- [ ] Each certificate has `status`
- [ ] Each certificate has `issueDate` in ISO 8601 format
- [ ] Each certificate has `expirationDate` in ISO 8601 format
- [ ] `issuer` equals "Green Circle Certified"
- [ ] `pagination.totalCount` is a number
- [ ] `pagination.pageNumber` is `1`

**Result:** PASS / FAIL

---

### 5.3 Test: List with Product Identifiers (CERT-02) - CRITICAL

**Purpose:** Verify certificates include ASIN/UPC/GTIN when requested.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates?includeProductIds=true&pageSize=10" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" \
  -H "Accept: application/json"
```

**Expected Response with Product IDs:**
```json
{
  "success": true,
  "data": [
    {
      "id": 123,
      "certificateNumber": "24-0366",
      "status": "Active",
      "company": "EcoProduct Co",
      "productName": "Recycled Paper Towels",
      "certificationType": "UL 2809",
      "certificationResults": "95.5% recycled content verified",
      "productIds": [
        {
          "type": "ASIN",
          "value": "B08XQRT456",
          "status": "Active",
          "partner": "Amazon",
          "productName": "Recycled Paper Towels - 12 Pack",
          "productSku": "RPT-12PK-ECO",
          "description": "12-pack product"
        },
        {
          "type": "UPC",
          "value": "012345678901",
          "status": "Active",
          "partner": null,
          "productName": "Recycled Paper Towels"
        }
      ],
      "pdfDownloadUrl": "/api/v1/certificates/24-0366/download"
    }
  ]
}
```

**Validation Checklist:**
- [ ] HTTP Status is `200`
- [ ] `productIds` array is present in response
- [ ] Each product ID has `type` (ASIN, UPC, GTIN, etc.)
- [ ] Each product ID has `value`
- [ ] Each product ID has `status`
- [ ] ASIN values follow format (10 alphanumeric characters)
- [ ] UPC values are 12 digits
- [ ] `certificationResults` field is present (may be null)
- [ ] `pdfDownloadUrl` is present

**Result:** PASS / FAIL

---

### 5.4 Test: Get by Certificate Number (CERT-03)

**Purpose:** Retrieve a specific certificate by its number.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/number/24-0366" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "id": 123,
    "certificateNumber": "24-0366",
    "status": "Active",
    ...
  }
}
```

**Validation Checklist:**
- [ ] HTTP Status is `200`
- [ ] `certificateNumber` matches requested number
- [ ] All required fields present

**Error Case - Not Found:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/number/99-9999" \
  -H "X-API-Key: gcc_partner_YOUR_KEY"
```
- [ ] HTTP Status is `404`
- [ ] Error message indicates certificate not found

**Result:** PASS / FAIL

---

## 6. Product Identifier Search Testing

### 6.1 Test Matrix

| Test ID | Test Name | Endpoint | Priority |
|---------|-----------|----------|----------|
| SRCH-01 | Search by ASIN | `GET /certificates/search/productid?productId=B08XXX&type=ASIN` | Critical |
| SRCH-02 | Search by UPC | `GET /certificates/search/productid?productId=012345678901&type=UPC` | Critical |
| SRCH-03 | Search by GTIN | `GET /certificates/search/productid?productId=00012345678901&type=GTIN` | High |
| SRCH-04 | Search Any Type | `GET /certificates/search/productid?productId=B08XXX` | High |
| SRCH-05 | Search Not Found | Search with non-existent ID | Medium |

### 6.2 Test: Search by ASIN (SRCH-01) - CRITICAL

**Purpose:** Find certificates containing a specific Amazon ASIN.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/search/productid?productId=B08XQRT456&type=ASIN" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" \
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
      "productName": "Recycled Paper Towels",
      "company": "EcoProduct Co",
      "status": "Active",
      "certificationResults": "95.5% recycled content verified",
      "productIds": [
        {
          "type": "ASIN",
          "value": "B08XQRT456",
          "status": "Active",
          "partner": "Amazon",
          "isMatchedId": true
        }
      ],
      "pdfDownloadUrl": "/api/v1/certificates/24-0366/download"
    }
  ],
  "message": "Found 1 certificate(s) with 1 total product IDs for search: B08XQRT456"
}
```

**Validation Checklist:**
- [ ] HTTP Status is `200`
- [ ] Results contain certificates with matching ASIN
- [ ] `isMatchedId: true` indicates the matched identifier
- [ ] `certificationResults` field is present
- [ ] Result count matches `message` description

**Result:** PASS / FAIL

---

### 6.3 Test: Search by UPC (SRCH-02)

**Purpose:** Find certificates by UPC barcode.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/search/productid?productId=012345678901&type=UPC" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" \
  -H "Accept: application/json"
```

**Validation Checklist:**
- [ ] HTTP Status is `200`
- [ ] Results filtered to UPC type only
- [ ] Matched UPC has `isMatchedId: true`

**Result:** PASS / FAIL

---

### 6.4 Test: Search Not Found (SRCH-05)

**Purpose:** Verify proper response when product ID doesn't exist.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/search/productid?productId=NOTEXIST123&type=ASIN" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
  "success": true,
  "data": [],
  "message": "Found 0 certificate(s) for search: NOTEXIST123"
}
```

**Validation Checklist:**
- [ ] HTTP Status is `200` (not 404)
- [ ] `data` is an empty array
- [ ] `success` is `true`
- [ ] Message indicates 0 results

**Result:** PASS / FAIL

---

## 7. GS1 JSON-LD Format Testing

### 7.1 Overview

The GS1 endpoint provides data in the GS1 Web Vocabulary JSON-LD format, recommended for Amazon Climate Pledge Friendly integration.

### 7.2 Test: GS1 List Certificates (GS1-01)

**Purpose:** Retrieve certificates in GS1 JSON-LD format.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/gs1?pageSize=10" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
  "success": true,
  "data": [
    {
      "@context": "https://gs1us-technology.github.io/certification-data-model/cert_context.json",
      "$schema": "https://gs1us-technology.github.io/certification-data-model/cert_schema.json",
      "id": "https://database.greencirclecertified.com/api/v1/certificates/gs1/24-0366",
      "type": "CertificationReport",
      "issueDate": "2024-01-15",
      "issuer": "Green Circle Certified",
      "products": [
        {
          "id": "https://database.greencirclecertified.com/api/v1/certificates/gs1/24-0366/product",
          "type": "Product",
          "gtin": null,
          "additionalTradeItemId": [
            { "type": "ASIN", "value": "B08XQRT456", "name": "Recycled Paper Towels - 6 Pack" }
          ],
          "name": "Recycled Paper Towels",
          "certificationInfo": {
            "agency": "Green Circle Certified",
            "program": "UL 2809",
            "certificateId": "24-0366",
            "status": "ACTIVE",
            "value": "95.5% recycled content",
            "startDate": "2024-01-15",
            "endDate": "2025-01-15"
          }
        }
      ]
    }
  ]
}
```

**Validation Checklist:**
- [ ] HTTP Status is `200`
- [ ] `@context` URL is present
- [ ] `$schema` URL is present
- [ ] `type` equals "CertificationReport"
- [ ] `products` array is present
- [ ] Each product has `gtin` (14 digits with leading zeros)
- [ ] `additionalTradeItemId` contains ASIN entries
- [ ] `certificationInfo.status` is "ACTIVE" or "INACTIVE"
- [ ] Date format is ISO 8601 date only (YYYY-MM-DD)

**Result:** PASS / FAIL

---

### 7.3 Test: GS1 Single Certificate (GS1-02)

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/gs1/24-0366" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" \
  -H "Accept: application/json"
```

**Validation Checklist:**
- [ ] HTTP Status is `200`
- [ ] Single certificate object returned
- [ ] All GS1 fields present

**Result:** PASS / FAIL

---

## 8. PDF Download Testing

### 8.1 Test: Download Certificate PDF (PDF-01)

**Purpose:** Verify PDF certificate download works.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/24-0366/download" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" \
  -o certificate.pdf
```

**Validation Checklist:**
- [ ] HTTP Status is `200`
- [ ] Content-Type is `application/pdf`
- [ ] File downloads successfully
- [ ] PDF opens and displays certificate
- [ ] File size is greater than 0 bytes

**Result:** PASS / FAIL

---

### 8.2 Test: Download Non-Existent PDF (PDF-02)

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/99-9999/download" \
  -H "X-API-Key: gcc_partner_YOUR_KEY"
```

**Validation Checklist:**
- [ ] HTTP Status is `404`
- [ ] Error message indicates certificate not found

**Result:** PASS / FAIL

---

## 9. Pagination Testing

### 9.1 Test Matrix

| Test ID | Test Name | Parameters | Expected |
|---------|-----------|------------|----------|
| PAGE-01 | Default Pagination | No params | pageSize=20, pageNumber=1 |
| PAGE-02 | Custom Page Size | pageSize=50 | 50 items max |
| PAGE-03 | Page Navigation | pageNumber=2 | Second page |
| PAGE-04 | Max Page Size | pageSize=100 | 100 items max |
| PAGE-05 | Over Max Size | pageSize=500 | Capped at 100 |
| PAGE-06 | Empty Page | Large pageNumber | Empty data array |

### 9.2 Test: Pagination Response (PAGE-01)

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates?pageSize=5&pageNumber=1" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" \
  -H "Accept: application/json"
```

**Expected Response:**
```json
{
  "success": true,
  "data": [...],
  "pagination": {
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 5,
    "totalPages": 30,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

**Validation Checklist:**
- [ ] `data` array has 5 items (or less if total < 5)
- [ ] `pageNumber` equals requested page
- [ ] `pageSize` equals requested size
- [ ] `totalPages` = ceiling(totalCount / pageSize)
- [ ] `hasNext` is `true` if more pages exist
- [ ] `hasPrevious` is `false` for page 1

**Result:** PASS / FAIL

---

### 9.3 Test: Page Navigation (PAGE-03)

**Request Page 2:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates?pageSize=5&pageNumber=2" \
  -H "X-API-Key: gcc_partner_YOUR_KEY"
```

**Validation Checklist:**
- [ ] `pageNumber` is `2`
- [ ] `hasPrevious` is `true`
- [ ] Data items are different from page 1

**Result:** PASS / FAIL

---

### 9.4 Test: Over Max Page Size (PAGE-05)

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates?pageSize=500" \
  -H "X-API-Key: gcc_partner_YOUR_KEY"
```

**Validation Checklist:**
- [ ] `pageSize` is capped at `100`
- [ ] No more than 100 items returned

**Result:** PASS / FAIL

---

## 10. Error Handling Validation

### 10.1 Error Response Format

All errors follow this structure:

```json
{
  "success": false,
  "errors": [
    {
      "code": "ERROR_CODE",
      "message": "Human-readable error message"
    }
  ]
}
```

### 10.2 Test Matrix

| Test ID | Test Name | Input | Expected Status |
|---------|-----------|-------|-----------------|
| ERR-01 | Invalid Certificate ID | `/certificates/abc` | 400 Bad Request |
| ERR-02 | Negative Certificate ID | `/certificates/-1` | 400 Bad Request |
| ERR-03 | Certificate Not Found | `/certificates/999999` | 404 Not Found |
| ERR-04 | Invalid Page Number | `pageNumber=0` | 400 or defaults to 1 |
| ERR-05 | Invalid Product ID Type | `type=INVALID` | 400 Bad Request |
| ERR-06 | Empty Search Value | `productId=` | 400 Bad Request |
| ERR-07 | SQL Injection Attempt | `productId='; DROP TABLE--` | Sanitized, no error |

### 10.3 Test: Certificate Not Found (ERR-03)

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/999999" \
  -H "X-API-Key: gcc_partner_YOUR_KEY"
```

**Expected Response:**
```json
{
  "success": false,
  "errors": [
    {
      "code": "NOT_FOUND",
      "message": "Certificate not found"
    }
  ]
}
```

**Validation Checklist:**
- [ ] HTTP Status is `404`
- [ ] `success` is `false`
- [ ] Error code is `NOT_FOUND` or `RESOURCE_NOT_FOUND`

**Result:** PASS / FAIL

---

### 10.4 Test: Invalid Product ID Type (ERR-05)

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/search/productid?productId=12345&type=INVALID" \
  -H "X-API-Key: gcc_partner_YOUR_KEY"
```

**Validation Checklist:**
- [ ] HTTP Status is `400`
- [ ] Error message lists valid types: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN

**Result:** PASS / FAIL

---

## 11. Security Testing

### 11.1 Test Matrix

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| SEC-01 | Partner Data Isolation | Partners see only their data | Critical |
| SEC-02 | No Admin Access | Partner keys cannot access admin endpoints | Critical |
| SEC-03 | SQL Injection Prevention | Malicious input is sanitized | Critical |
| SEC-04 | XSS Prevention | HTML in responses is encoded | High |
| SEC-05 | API Key Not in Logs | Keys not logged in plain text | High |

### 11.2 Test: Partner Data Isolation (SEC-01)

**Purpose:** Verify partners only see certificates associated with them.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates?includeProductIds=true" \
  -H "X-API-Key: gcc_partner_AMAZON_KEY" \
  -H "Accept: application/json"
```

**Validation Checklist:**
- [ ] All returned certificates have `partner: "Amazon"` OR company association
- [ ] No certificates from other partners (Novi) visible
- [ ] Product IDs filtered to partner association

**Result:** PASS / FAIL

---

### 11.3 Test: No Admin Access (SEC-02)

**Purpose:** Verify partner API keys cannot access admin endpoints.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/admin/partner/list" \
  -H "X-API-Key: gcc_partner_YOUR_KEY"
```

**Validation Checklist:**
- [ ] HTTP Status is `403 Forbidden`
- [ ] No admin data exposed

**Result:** PASS / FAIL

---

### 11.4 Test: SQL Injection Prevention (SEC-03)

**Purpose:** Verify malicious SQL input is handled safely.

**Request:**
```bash
curl -X GET "http://localhost:5101/api/v1/certificates/search/productid?productId='; DROP TABLE certificates; --" \
  -H "X-API-Key: gcc_partner_YOUR_KEY"
```

**Validation Checklist:**
- [ ] Request completes without server error
- [ ] No database modification occurs
- [ ] Response is empty array or validation error (not 500)

**Result:** PASS / FAIL

---

## 12. Rate Limiting Testing

### 12.1 Rate Limits

| Metric | Limit |
|--------|-------|
| Requests per hour | 200 |
| Approximate per minute | ~3.3 |

### 12.2 Test: Rate Limit Exceeded (RATE-01)

**Purpose:** Verify rate limiting returns proper response.

**PowerShell Test Script:**
```powershell
$ApiKey = "gcc_partner_YOUR_KEY"
$BaseUrl = "http://localhost:5101"
$Headers = @{ "X-API-Key" = $ApiKey }

# Send requests until rate limited
1..250 | ForEach-Object {
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/api/v1/certificates?pageSize=1" `
            -Headers $Headers -Method Get -ErrorAction Stop
        Write-Host "Request $_: $($response.StatusCode)"
    }
    catch {
        Write-Host "Request $_: $($_.Exception.Response.StatusCode.value__) - Rate Limited!" -ForegroundColor Yellow
    }
}
```

**Expected Behavior:**
- Initial requests return `200 OK`
- After limit exceeded: `429 Too Many Requests`

**Validation Checklist:**
- [ ] Rate limit is enforced (429 returned after limit)
- [ ] Error response indicates rate limit exceeded
- [ ] Retry-After header may be present

**Result:** PASS / FAIL

---

## 13. Data Validation Checklist

### 13.1 Required Fields for Amazon Integration

| Field | Required | Format | Validation |
|-------|----------|--------|------------|
| `certificateNumber` | Yes | YY-NNNN | Two-digit year, hyphen, 4 digits |
| `status` | Yes | String | Draft, Active, Expired, Suspended, Revoked |
| `issueDate` | Yes | ISO 8601 | YYYY-MM-DDTHH:MM:SSZ |
| `expirationDate` | Yes | ISO 8601 | YYYY-MM-DDTHH:MM:SSZ |
| `company` | Yes | String | Company/brand name |
| `productName` | Yes | String | Product name |
| `certificationType` | Yes | String | e.g., "UL 2809" |
| `issuer` | Yes | String | "Green Circle Certified" |
| `certificationResults` | **Yes** | String | Threshold/score data |
| `productIds` | Yes | Array | When `includeProductIds=true` |
| `pdfDownloadUrl` | Yes | URL | Relative path to PDF |

### 13.2 Product Identifier Fields

| Field | Required | Valid Values |
|-------|----------|--------------|
| `type` | Yes | ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |
| `value` | Yes | The identifier value |
| `status` | Yes | Active, Inactive, Pending, Expired |
| `partner` | No | Amazon, Novi, None, Other |
| `productName` | No | Product name for this ID |
| `productSku` | No | SKU value |
| `description` | No | Notes |

### 13.3 Date Format Validation

**Test:** Verify all dates are ISO 8601 format

```bash
# Get certificates and check date formats
curl -X GET "http://localhost:5101/api/v1/certificates?pageSize=5" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" | jq '.data[].issueDate'
```

**Expected Format:** `2024-01-15T00:00:00Z`

---

## 14. Test Scripts

### 14.1 Complete PowerShell Test Suite

Save as `Test-GccPartnerApi.ps1`:

```powershell
#############################################
# GCC Partner API Test Suite
# Version: 1.0
# Usage: .\Test-GccPartnerApi.ps1 -ApiKey "your_key" -BaseUrl "http://localhost:5101"
#############################################

param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey,

    [string]$BaseUrl = "http://localhost:5101"
)

$Headers = @{
    "X-API-Key" = $ApiKey
    "Accept" = "application/json"
}

$Results = @()

function Test-Endpoint {
    param(
        [string]$TestId,
        [string]$Name,
        [string]$Url,
        [int]$ExpectedStatus,
        [scriptblock]$Validation = $null
    )

    $result = @{
        TestId = $TestId
        Name = $Name
        Status = "FAIL"
        ActualStatus = 0
        Details = ""
    }

    try {
        $response = Invoke-WebRequest -Uri $Url -Headers $Headers -Method Get -ErrorAction Stop
        $result.ActualStatus = $response.StatusCode

        if ($response.StatusCode -eq $ExpectedStatus) {
            $result.Status = "PASS"

            if ($Validation) {
                $content = $response.Content | ConvertFrom-Json
                $validationResult = & $Validation $content
                if (-not $validationResult) {
                    $result.Status = "FAIL"
                    $result.Details = "Validation failed"
                }
            }
        }
    }
    catch {
        $result.ActualStatus = $_.Exception.Response.StatusCode.value__
        if ($result.ActualStatus -eq $ExpectedStatus) {
            $result.Status = "PASS"
        }
        $result.Details = $_.Exception.Message
    }

    $color = if ($result.Status -eq "PASS") { "Green" } else { "Red" }
    Write-Host "[$($result.Status)] $TestId - $Name (HTTP $($result.ActualStatus))" -ForegroundColor $color

    return $result
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "GCC Partner API Test Suite" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"
Write-Host "API Key: $($ApiKey.Substring(0,20))...`n"

# Authentication Tests
Write-Host "`n--- Authentication Tests ---" -ForegroundColor Yellow
$Results += Test-Endpoint "AUTH-01" "Valid API Key" "$BaseUrl/api/v1/certificates?pageSize=1" 200
$Results += Test-Endpoint "AUTH-03" "Invalid API Key" "$BaseUrl/api/v1/certificates" 401 -Headers @{"X-API-Key"="invalid"}

# Certificate Tests
Write-Host "`n--- Certificate Retrieval Tests ---" -ForegroundColor Yellow
$Results += Test-Endpoint "CERT-01" "List Certificates" "$BaseUrl/api/v1/certificates" 200
$Results += Test-Endpoint "CERT-02" "List with Product IDs" "$BaseUrl/api/v1/certificates?includeProductIds=true" 200 {
    param($content)
    return $content.success -eq $true
}

# Search Tests
Write-Host "`n--- Search Tests ---" -ForegroundColor Yellow
$Results += Test-Endpoint "SRCH-01" "Search by ASIN" "$BaseUrl/api/v1/certificates/search/productid?productId=TEST&type=ASIN" 200
$Results += Test-Endpoint "SRCH-05" "Search Not Found" "$BaseUrl/api/v1/certificates/search/productid?productId=NOTEXIST999" 200 {
    param($content)
    return $content.data.Count -eq 0
}

# Pagination Tests
Write-Host "`n--- Pagination Tests ---" -ForegroundColor Yellow
$Results += Test-Endpoint "PAGE-01" "Default Pagination" "$BaseUrl/api/v1/certificates" 200
$Results += Test-Endpoint "PAGE-04" "Max Page Size" "$BaseUrl/api/v1/certificates?pageSize=100" 200

# Error Tests
Write-Host "`n--- Error Handling Tests ---" -ForegroundColor Yellow
$Results += Test-Endpoint "ERR-03" "Certificate Not Found" "$BaseUrl/api/v1/certificates/999999" 404

# GS1 Tests
Write-Host "`n--- GS1 Format Tests ---" -ForegroundColor Yellow
$Results += Test-Endpoint "GS1-01" "GS1 List" "$BaseUrl/api/v1/certificates/gs1?pageSize=5" 200

# Summary
$passed = ($Results | Where-Object { $_.Status -eq "PASS" }).Count
$total = $Results.Count
$percentage = [math]::Round(($passed / $total) * 100, 1)

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "TEST RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Passed: $passed / $total ($percentage%)" -ForegroundColor $(if($passed -eq $total){"Green"}else{"Yellow"})
Write-Host ""

# Export results
$Results | Export-Csv -Path "GCC-API-Test-Results-$(Get-Date -Format 'yyyy-MM-dd-HHmm').csv" -NoTypeInformation
Write-Host "Results exported to CSV file."
```

### 14.2 Complete Bash/cURL Test Suite

Save as `test-gcc-api.sh`:

```bash
#!/bin/bash
#############################################
# GCC Partner API Test Suite
# Version: 1.0
# Usage: ./test-gcc-api.sh API_KEY [BASE_URL]
#############################################

API_KEY="${1:-gcc_partner_test}"
BASE_URL="${2:-http://localhost:5101}"

PASSED=0
FAILED=0

test_endpoint() {
    local test_id=$1
    local name=$2
    local url=$3
    local expected=$4
    local extra_header="${5:-}"

    if [ -n "$extra_header" ]; then
        status=$(curl -s -o /dev/null -w "%{http_code}" -H "$extra_header" -H "Accept: application/json" "$url")
    else
        status=$(curl -s -o /dev/null -w "%{http_code}" -H "X-API-Key: $API_KEY" -H "Accept: application/json" "$url")
    fi

    if [ "$status" == "$expected" ]; then
        echo "[PASS] $test_id - $name (HTTP $status)"
        ((PASSED++))
    else
        echo "[FAIL] $test_id - $name (Expected $expected, Got $status)"
        ((FAILED++))
    fi
}

echo ""
echo "========================================"
echo "GCC Partner API Test Suite"
echo "========================================"
echo "Base URL: $BASE_URL"
echo "API Key: ${API_KEY:0:20}..."
echo ""

echo "--- Authentication Tests ---"
test_endpoint "AUTH-01" "Valid API Key" "$BASE_URL/api/v1/certificates?pageSize=1" "200"
test_endpoint "AUTH-03" "Invalid API Key" "$BASE_URL/api/v1/certificates" "401" "X-API-Key: invalid_key"

echo ""
echo "--- Certificate Tests ---"
test_endpoint "CERT-01" "List Certificates" "$BASE_URL/api/v1/certificates" "200"
test_endpoint "CERT-02" "List with Product IDs" "$BASE_URL/api/v1/certificates?includeProductIds=true" "200"

echo ""
echo "--- Search Tests ---"
test_endpoint "SRCH-01" "Search by ASIN" "$BASE_URL/api/v1/certificates/search/productid?productId=TEST&type=ASIN" "200"
test_endpoint "SRCH-05" "Search Not Found" "$BASE_URL/api/v1/certificates/search/productid?productId=NOTEXIST999" "200"

echo ""
echo "--- Pagination Tests ---"
test_endpoint "PAGE-01" "Default Pagination" "$BASE_URL/api/v1/certificates" "200"
test_endpoint "PAGE-04" "Max Page Size" "$BASE_URL/api/v1/certificates?pageSize=100" "200"

echo ""
echo "--- Error Handling Tests ---"
test_endpoint "ERR-03" "Certificate Not Found" "$BASE_URL/api/v1/certificates/999999" "404"

echo ""
echo "--- GS1 Format Tests ---"
test_endpoint "GS1-01" "GS1 List" "$BASE_URL/api/v1/certificates/gs1?pageSize=5" "200"

echo ""
echo "========================================"
echo "TEST RESULTS SUMMARY"
echo "========================================"
TOTAL=$((PASSED + FAILED))
PERCENTAGE=$((PASSED * 100 / TOTAL))
echo "Passed: $PASSED / $TOTAL ($PERCENTAGE%)"
echo ""
```

---

## 15. Troubleshooting Guide

### 15.1 Common Issues and Solutions

| Issue | Possible Cause | Solution |
|-------|----------------|----------|
| **401 Unauthorized** | Invalid/missing API key | Verify key is correct and not expired |
| **403 Forbidden** | Endpoint not permitted | Check key permissions with GCC Admin |
| **404 Not Found** | Resource doesn't exist | Verify certificate number/ID |
| **429 Too Many Requests** | Rate limit exceeded | Wait and retry with exponential backoff |
| **500 Internal Error** | Server error | Contact GCC support with request details |
| **Empty results** | No data for partner | Verify partner association is set up |
| **Missing productIds** | Parameter not set | Add `?includeProductIds=true` |
| **Connection refused** | Server not running | Check server status and URL |

### 15.2 Debug Request

Use verbose output to debug issues:

```bash
# cURL verbose mode
curl -v -X GET "http://localhost:5101/api/v1/certificates?pageSize=1" \
  -H "X-API-Key: gcc_partner_YOUR_KEY" \
  -H "Accept: application/json"
```

```powershell
# PowerShell verbose
Invoke-WebRequest -Uri "http://localhost:5101/api/v1/certificates?pageSize=1" `
  -Headers $Headers -Verbose
```

### 15.3 Contact Support

For issues not resolved by this guide:

- **Technical Support:** Contact GCC Technical Team
- **API Key Issues:** Contact GCC Administrator
- **Documentation:** Review Swagger at `/swagger`

---

## 16. Test Results Template

### 16.1 Test Execution Record

| Field | Value |
|-------|-------|
| **Tester Name** | |
| **Test Date** | |
| **Environment** | Development / Production |
| **API Key ID** | (last 8 chars only) |
| **Test Suite Version** | 1.0 |

### 16.2 Test Results Summary

| Category | Total | Passed | Failed | Pass Rate |
|----------|-------|--------|--------|-----------|
| Authentication | 8 | | | |
| Certificate Retrieval | 6 | | | |
| Product ID Search | 5 | | | |
| GS1 Format | 2 | | | |
| PDF Download | 2 | | | |
| Pagination | 6 | | | |
| Error Handling | 7 | | | |
| Security | 5 | | | |
| Rate Limiting | 1 | | | |
| **TOTAL** | **42** | | | |

### 16.3 Issues Found

| Issue # | Test ID | Description | Severity | Status |
|---------|---------|-------------|----------|--------|
| 1 | | | Critical/High/Medium/Low | Open/Fixed |
| 2 | | | | |
| 3 | | | | |

### 16.4 Sign-Off

| Role | Name | Signature | Date |
|------|------|-----------|------|
| QA Tester | | | |
| Developer | | | |
| Product Owner | | | |

---

## Appendix A: HTTP Status Codes Reference

| Code | Name | Description |
|------|------|-------------|
| 200 | OK | Request successful |
| 201 | Created | Resource created (not for partner API) |
| 400 | Bad Request | Invalid request parameters |
| 401 | Unauthorized | Invalid or missing API key |
| 403 | Forbidden | Endpoint not permitted |
| 404 | Not Found | Resource not found |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |

## Appendix B: Product Identifier Types

| Type | Description | Format Example |
|------|-------------|----------------|
| ASIN | Amazon Standard Identification Number | B08XQRT456 |
| UPC | Universal Product Code | 012345678901 |
| GTIN | Global Trade Item Number | 00012345678901 |
| EAN | European Article Number | 5901234123457 |
| ISBN | International Standard Book Number | 978-3-16-148410-0 |
| SKU | Stock Keeping Unit | RPT-12PK-ECO |
| MPN | Manufacturer Part Number | ABC-12345 |

## Appendix C: Certificate Status Values

| Status | Description |
|--------|-------------|
| Draft | Certificate created but not yet active |
| Active | Currently valid certificate |
| Expired | Past expiration date |
| Suspended | Temporarily inactive |
| Revoked | Permanently invalidated |

---

**Document Version:** 1.0
**Last Updated:** 2026-01-29
**Maintained By:** Green Circle Certified Development Team

---

*End of Partner API Manual Testing Guide*
