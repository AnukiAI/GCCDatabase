# Green Circle Certified
# Partner API Integration Guide

## Amazon Integration

---

**Document Information**

| Field | Value |
|-------|-------|
| Document Title | Partner API Integration Guide - Amazon |
| Version | 1.0 |
| Status | Final |
| Classification | Confidential - Partner Use Only |
| Prepared For | Amazon Sustainability Team |
| Prepared By | Green Circle Certified |
| Date | January 18, 2026 |

---

## Document Control

### Revision History

| Version | Date | Author | Description |
|---------|------|--------|-------------|
| 1.0 | 2026-01-18 | GCC Technical Team | Initial release |

### Distribution List

| Name | Organization | Role |
|------|--------------|------|
| Amazon Integration Team | Amazon | Technical Integration |
| GCC Technical Support | Green Circle Certified | API Support |

### Document Approval

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Technical Lead | | | |
| Partner Manager | | | |

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Introduction](#2-introduction)
3. [System Architecture](#3-system-architecture)
4. [Authentication & Security](#4-authentication--security)
5. [Data Access & Permissions](#5-data-access--permissions)
6. [API Endpoints Reference](#6-api-endpoints-reference)
7. [Data Models & Field Specifications](#7-data-models--field-specifications)
8. [Integration Workflows](#8-integration-workflows)
9. [Error Handling & Troubleshooting](#9-error-handling--troubleshooting)
10. [Rate Limiting & Performance](#10-rate-limiting--performance)
11. [Testing & Validation](#11-testing--validation)
12. [Support & Escalation](#12-support--escalation)
13. [Appendices](#13-appendices)

---

## 1. Executive Summary

### 1.1 Purpose

This document provides comprehensive technical guidance for Amazon's integration with the Green Circle Certified (GCC) Partner API. The API enables Amazon to programmatically access certified product data, including certification details, product identifiers (ASINs), and downloadable certificate documents.

### 1.2 Integration Overview

Green Circle Certified provides third-party certification for sustainable products. Through this API integration, Amazon can:

- Retrieve certification data for products listed on Amazon's marketplace
- Verify product sustainability claims using certificate information
- Access certification threshold results for compliance validation
- Download official certificate PDF documents
- Search certificates by ASIN or other product identifiers

### 1.3 Key Benefits

| Benefit | Description |
|---------|-------------|
| **Automated Verification** | Programmatic access eliminates manual certificate verification |
| **Real-Time Data** | Access current certification status and expiration dates |
| **Product Mapping** | Direct ASIN-to-certificate lookup capability |
| **Threshold Validation** | Access certification results for compliance thresholds |
| **Audit Trail** | Comprehensive logging for compliance reporting |

### 1.4 Technical Summary

| Attribute | Specification |
|-----------|---------------|
| Protocol | HTTPS (TLS 1.2+) |
| Format | RESTful JSON API |
| Authentication | API Key (Header-based) |
| Rate Limit | 200 requests/hour |
| Availability | 99.9% SLA target |

---

## 2. Introduction

### 2.1 About Green Circle Certified

Green Circle Certified (GCC) is a third-party certification body that verifies environmental and sustainability claims for consumer products. GCC certifications include:

- Recycled content verification
- Biodegradability certification
- Environmental impact assessments
- Sustainability standard compliance (UL 2809, etc.)

### 2.2 Scope of Integration

This integration covers the Partner API, which provides read-only access to certification data. The scope includes:

**In Scope:**
- Certificate retrieval and search
- Product identifier (ASIN/UPC/GTIN) lookup
- Certificate PDF download
- Certification results data

**Out of Scope:**
- Certificate creation or modification
- Administrative functions
- User management
- Webhook/push notifications (future phase)

### 2.3 Terminology

| Term | Definition |
|------|------------|
| Certificate | A formal document issued by GCC verifying a product meets specific certification standards |
| Certificate Number | Unique identifier in format YY-NNNN (e.g., 24-0366) |
| Product Identifier | A unique product code such as ASIN, UPC, GTIN, or SKU |
| Partner | An organization with API access (Amazon, Novi, etc.) |
| Certification Results | Quantitative results from certification testing (e.g., "95.5% recycled content") |
| API Key | A secure token used to authenticate API requests |

### 2.4 Prerequisites

Before beginning integration, ensure you have:

1. **API Key** - Issued by GCC upon partnership agreement execution
2. **Technical Resources** - Development team familiar with REST APIs
3. **Test Environment** - Ability to make HTTPS requests to external APIs
4. **ASIN Data** - List of ASINs to validate during testing

---

## 3. System Architecture

### 3.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              AMAZON SYSTEMS                                  │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐         │
│  │  Product Catalog │    │  Sustainability  │    │   Compliance    │         │
│  │     System       │◄──►│    Platform      │◄──►│    Reports      │         │
│  └─────────────────┘    └────────┬────────┘    └─────────────────┘         │
│                                  │                                          │
│                                  │ HTTPS/REST                               │
└──────────────────────────────────┼──────────────────────────────────────────┘
                                   │
                                   ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                           GCC PARTNER API                                    │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                        API Gateway Layer                                 ││
│  │  ┌──────────┐  ┌──────────────┐  ┌───────────────┐  ┌───────────────┐  ││
│  │  │   Auth   │  │ Rate Limiting │  │   Logging     │  │  Routing      │  ││
│  │  └──────────┘  └──────────────┘  └───────────────┘  └───────────────┘  ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                    │                                         │
│  ┌─────────────────────────────────▼───────────────────────────────────────┐│
│  │                        Application Layer                                 ││
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────────────────┐ ││
│  │  │  Certificates  │  │  Product IDs   │  │  Document Management       │ ││
│  │  │    Service     │  │    Service     │  │       Service              │ ││
│  │  └────────────────┘  └────────────────┘  └────────────────────────────┘ ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                    │                                         │
│  ┌─────────────────────────────────▼───────────────────────────────────────┐│
│  │                          Data Layer                                      ││
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────────────────┐ ││
│  │  │   SQL Server   │  │  File Storage  │  │     Partner Context        │ ││
│  │  │   Database     │  │   (PDFs)       │  │       Filtering            │ ││
│  │  └────────────────┘  └────────────────┘  └────────────────────────────┘ ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Integration Model

The GCC Partner API uses a **PULL** integration model:

1. **Amazon initiates** all data requests
2. **GCC responds** with requested data filtered by partner permissions
3. **No push notifications** - Amazon polls for updates as needed

### 3.3 Environment Information

| Environment | Base URL | Purpose |
|-------------|----------|---------|
| Production | `https://[production-domain]/api/v1` | Live integration |
| Staging | `https://[staging-domain]/api/v1` | Pre-production testing |

**Note:** Specific domain URLs will be provided separately for security reasons.

### 3.4 API Versioning

The API uses URL-based versioning:

```
/api/v1/certificates    ← Current version
/api/v2/certificates    ← Future versions (not yet available)
```

Version 1 (v1) is the current and only supported version. Partners will receive 6 months advance notice before any breaking changes.

---

## 4. Authentication & Security

### 4.1 Authentication Method

The GCC Partner API uses **API Key authentication** via HTTP headers.

**Header Name:** `X-API-Key`

**Key Format:** `gcc_partner_[64-character-hexadecimal-hash]`

**Example:**
```
X-API-Key: gcc_partner_b451fb3b31c645e9dd900c3c7b2f6250fbb7480bbb4510d602efc5e52102fc4d
```

### 4.2 Authentication Flow

```
┌─────────────────┐                              ┌─────────────────┐
│     Amazon      │                              │   GCC API       │
│     Client      │                              │   Server        │
└────────┬────────┘                              └────────┬────────┘
         │                                                │
         │  1. HTTPS Request + X-API-Key Header           │
         │───────────────────────────────────────────────►│
         │                                                │
         │                          2. Validate API Key   │
         │                          3. Load Partner Context│
         │                          4. Apply Data Filters │
         │                                                │
         │  5. JSON Response (Filtered Data)              │
         │◄───────────────────────────────────────────────│
         │                                                │
```

### 4.3 Security Requirements

| Requirement | Specification |
|-------------|---------------|
| Transport | HTTPS required (HTTP requests rejected) |
| TLS Version | TLS 1.2 or higher |
| Key Storage | API keys must be stored securely (encrypted at rest) |
| Key Transmission | Keys must only be sent in headers, never in URLs |
| Key Rotation | Keys can be rotated upon request |

### 4.4 API Key Management

**Obtaining an API Key:**
1. Complete partnership agreement with GCC
2. Request API key from GCC Partner Management
3. Key is generated and securely transmitted
4. Key is immediately active upon receipt

**Key Lifecycle:**

| State | Description |
|-------|-------------|
| Active | Key is valid and can authenticate requests |
| Revoked | Key has been permanently disabled |
| Expired | Key has passed its expiration date (if set) |

**Key Revocation:**
- Keys can be revoked immediately upon request
- Compromised keys should be reported to GCC immediately
- New keys can be generated to replace revoked keys

### 4.5 Request Signing (Optional)

For additional security, requests can include a timestamp header to prevent replay attacks:

```
X-API-Key: gcc_partner_[key]
X-Request-Timestamp: 2026-01-18T14:30:00Z
```

---

## 5. Data Access & Permissions

### 5.1 Data Access Model

The GCC Partner API implements **automatic data filtering** based on partner associations. You will only receive data that your organization is authorized to access.

### 5.2 Access Determination

Your API key is linked to a Partner account, which determines data visibility:

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        DATA ACCESS DETERMINATION                          │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │                         API Key Validation                          ││
│  │                    (gcc_partner_amazon_xxx...)                       ││
│  └──────────────────────────────┬──────────────────────────────────────┘│
│                                 │                                        │
│                                 ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │                      Load Partner Context                           ││
│  │                       Partner = "Amazon"                             ││
│  └──────────────────────────────┬──────────────────────────────────────┘│
│                                 │                                        │
│                                 ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │                     Apply Access Filters                            ││
│  │                                                                      ││
│  │   IF Partner Association:                                           ││
│  │      → Return certificates where Partner = "Amazon"                 ││
│  │                                                                      ││
│  │   IF Company Association:                                           ││
│  │      → Return certificates for associated companies                 ││
│  │                                                                      ││
│  │   IF Both:                                                          ││
│  │      → Return certificates matching EITHER (OR logic)               ││
│  │                                                                      ││
│  │   IF Neither:                                                       ││
│  │      → Return empty result set                                      ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

### 5.3 Access Scenarios

| Scenario | Configuration | Data Returned |
|----------|---------------|---------------|
| Partner Only | API Key linked to "Amazon" partner | All certificates with Partner = "Amazon" |
| Company Only | API Key linked to specific companies | Certificates for those companies only |
| Partner + Company | Both associations configured | Certificates matching partner OR company |
| No Associations | Key has no partner/company links | Empty result set (no data access) |

### 5.4 Amazon-Specific Access

For Amazon integration, your API key is configured with:

- **Partner Association:** Amazon
- **Visibility:** All certificates where the Partner field = "Amazon"

This means you automatically receive:
- Certificates specifically designated for Amazon
- Product identifiers marked with Partner = "Amazon"
- All related certification data for those certificates

### 5.5 Data Privacy Guarantees

| Guarantee | Description |
|-----------|-------------|
| **Partner Isolation** | Amazon cannot see Novi certificates and vice versa |
| **Automatic Filtering** | No additional parameters needed for filtering |
| **No Cross-Partner Access** | API keys cannot access other partners' data |
| **Audit Logging** | All access is logged for compliance |

---

## 6. API Endpoints Reference

### 6.1 Base URL

```
Production: https://[production-domain]/api/v1
```

### 6.2 Common Headers

All requests must include:

| Header | Required | Value |
|--------|----------|-------|
| `X-API-Key` | Yes | Your API key |
| `Accept` | Recommended | `application/json` |
| `Content-Type` | For POST/PUT | `application/json` |

### 6.3 Endpoint Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/certificates` | List certificates with pagination |
| GET | `/certificates/{id}` | Get certificate by database ID |
| GET | `/certificates/number/{number}` | Get certificate by certificate number |
| GET | `/certificates/search/productid` | Search by product identifier (ASIN) |
| GET | `/certificates/{number}/download` | Download certificate PDF |

---

### 6.4 Endpoint Details

#### 6.4.1 List Certificates

Retrieves a paginated list of certificates accessible to your API key.

**Endpoint:** `GET /api/v1/certificates`

**Query Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `pageNumber` | integer | 1 | Page number (1-based) |
| `pageSize` | integer | 20 | Items per page (max: 100) |
| `includeProductIds` | boolean | false | **Include ASIN/UPC data** |
| `searchTerm` | string | null | Search across multiple fields |
| `sortBy` | string | null | Sort field (see options below) |
| `sortDescending` | boolean | false | Sort direction |

**Sort Options:**
- `certificatenumber`
- `issuedate`
- `expirationdate`
- `productname`
- `companyname`

**Example Request:**
```bash
curl -X GET "https://[domain]/api/v1/certificates?includeProductIds=true&pageSize=100" \
  -H "X-API-Key: gcc_partner_[your_key]" \
  -H "Accept: application/json"
```

**Example Response:**
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
      "location": "United States",
      "country": "USA",
      "partner": "Amazon",
      "certificationResults": "95.5% recycled content verified",
      "company": "EcoProduct Co",
      "companyId": 45,
      "productName": "Recycled Paper Towels",
      "productId": 78,
      "certificationType": "UL 2809",
      "certificationTypeId": 3,
      "certificationTypeSubcategory": "Level 2",
      "certificationTypeSubcategoryId": 12,
      "issuer": "Green Circle Certified",
      "productIds": [
        {
          "type": "ASIN",
          "value": "B08XQRT456",
          "status": "Active",
          "partner": "Amazon",
          "productName": "Recycled Paper Towels - 12 Pack",
          "productSku": "RPT-12PK-ECO",
          "description": "12-pack of 100% recycled paper towels"
        },
        {
          "type": "UPC",
          "value": "012345678901",
          "status": "Active",
          "partner": "None",
          "productName": "Recycled Paper Towels",
          "productSku": "RPT-SINGLE",
          "description": "Single roll recycled paper towels"
        }
      ],
      "pdfDownloadUrl": "/api/v1/certificates/24-0366/download",
      "createdAt": "2024-01-10T09:30:00Z",
      "updatedAt": "2024-01-15T14:22:00Z"
    }
  ],
  "pagination": {
    "totalCount": 184,
    "pageNumber": 1,
    "pageSize": 100,
    "totalPages": 2,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

---

#### 6.4.2 Get Certificate by ID

Retrieves a single certificate by its database ID.

**Endpoint:** `GET /api/v1/certificates/{id}`

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | integer | Database ID of the certificate |

**Example Request:**
```bash
curl -X GET "https://[domain]/api/v1/certificates/123" \
  -H "X-API-Key: gcc_partner_[your_key]" \
  -H "Accept: application/json"
```

**Response:** Single certificate object (same structure as list response)

---

#### 6.4.3 Get Certificate by Number

Retrieves a certificate by its human-readable certificate number.

**Endpoint:** `GET /api/v1/certificates/number/{number}`

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `number` | string | Certificate number (format: YY-NNNN) |

**Example Request:**
```bash
curl -X GET "https://[domain]/api/v1/certificates/number/24-0366" \
  -H "X-API-Key: gcc_partner_[your_key]" \
  -H "Accept: application/json"
```

---

#### 6.4.4 Search by Product Identifier (ASIN)

**This is the primary endpoint for ASIN-based lookups.**

**Endpoint:** `GET /api/v1/certificates/search/productid`

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `productId` | string | Yes | The product identifier value |
| `type` | string | No | Filter by type: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |

**Example Request:**
```bash
curl -X GET "https://[domain]/api/v1/certificates/search/productid?productId=B08XQRT456&type=ASIN" \
  -H "X-API-Key: gcc_partner_[your_key]" \
  -H "Accept: application/json"
```

**Example Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 123,
      "certificateNumber": "24-0366",
      "productName": "Recycled Paper Towels",
      "productType": "Paper Products",
      "company": "EcoProduct Co",
      "companyId": 45,
      "partner": "Amazon",
      "certificationResults": "95.5% recycled content verified",
      "status": "Active",
      "certificationType": "UL 2809",
      "issueDate": "2024-01-15T00:00:00Z",
      "expirationDate": "2025-01-15T00:00:00Z",
      "location": "United States",
      "country": "USA",
      "pdfDownloadUrl": "/api/v1/certificates/24-0366/download",
      "productIds": [
        {
          "type": "ASIN",
          "value": "B08XQRT456",
          "status": "Active",
          "partner": "Amazon",
          "productName": "Recycled Paper Towels - 12 Pack",
          "productSku": "RPT-12PK-ECO",
          "description": "12-pack of 100% recycled paper towels",
          "isMatchedId": true
        },
        {
          "type": "UPC",
          "value": "012345678901",
          "status": "Active",
          "partner": "None",
          "productName": "Recycled Paper Towels",
          "productSku": "RPT-SINGLE",
          "description": "Single roll recycled paper towels",
          "isMatchedId": false
        }
      ]
    }
  ],
  "message": "Found 1 certificate(s) with 2 total product IDs for search: B08XQRT456"
}
```

**Note:** The `isMatchedId` field indicates which product identifier matched the search criteria.

---

#### 6.4.5 Download Certificate PDF

Downloads the official certificate PDF document.

**Endpoint:** `GET /api/v1/certificates/{number}/download`

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `number` | string | Certificate number (format: YY-NNNN) |

**Example Request:**
```bash
curl -X GET "https://[domain]/api/v1/certificates/24-0366/download" \
  -H "X-API-Key: gcc_partner_[your_key]" \
  -o certificate_24-0366.pdf
```

**Response:**
- Content-Type: `application/pdf`
- Body: Binary PDF file

---

## 7. Data Models & Field Specifications

### 7.1 Certificate Object

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `id` | integer | No | Database ID |
| `certificateNumber` | string | No | Certificate number (format: YY-NNNN) |
| `status` | string | No | Certificate status (see 7.3) |
| `issueDate` | datetime | No | ISO 8601 format |
| `expirationDate` | datetime | Yes | ISO 8601 format |
| `location` | string | Yes | Geographic location |
| `country` | string | Yes | Country code or name |
| `partner` | string | Yes | Partner designation ("Amazon", "Novi", etc.) |
| `certificationResults` | string | Yes | Certification test results/threshold data |
| `company` | string | No | Company/brand name |
| `companyId` | integer | No | Company database ID |
| `productName` | string | No | Product name |
| `productId` | integer | No | Product database ID |
| `productType` | string | Yes | Product type name (in search results) |
| `certificationType` | string | No | Type of certification (e.g., "UL 2809") |
| `certificationTypeId` | integer | No | Certification type database ID |
| `certificationTypeSubcategory` | string | Yes | Standard version or level |
| `certificationTypeSubcategoryId` | integer | Yes | Subcategory database ID |
| `issuer` | string | No | Always "Green Circle Certified" |
| `productIds` | array | Yes | Array of product identifiers (see 7.2) |
| `pdfDownloadUrl` | string | No | Relative URL for PDF download |
| `createdAt` | datetime | No | Record creation timestamp |
| `updatedAt` | datetime | No | Last update timestamp |

### 7.2 Product Identifier Object

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `type` | string | No | Identifier type (see 7.4) |
| `value` | string | No | The identifier value |
| `status` | string | No | Active, Inactive, Pending, Expired |
| `partner` | string | Yes | Partner association (Amazon, Novi, None, Other) |
| `productName` | string | Yes | Product name for this specific identifier |
| `productSku` | string | Yes | SKU for this identifier |
| `description` | string | Yes | Additional notes or description |
| `isMatchedId` | boolean | Yes | Only in search results - indicates matched ID |

### 7.3 Certificate Status Values

| Status | Description |
|--------|-------------|
| `Draft` | Certificate is being prepared, not yet active |
| `Active` | Certificate is currently valid |
| `Expired` | Certificate has passed its expiration date |
| `Suspended` | Certificate is temporarily inactive |
| `Revoked` | Certificate has been permanently invalidated |
| `PendingRenewal` | Certificate is awaiting renewal |
| `UnderReview` | Certificate is under compliance review |

### 7.4 Product Identifier Types

| Type | Description | Example |
|------|-------------|---------|
| `ASIN` | Amazon Standard Identification Number | B08XQRT456 |
| `UPC` | Universal Product Code | 012345678901 |
| `GTIN` | Global Trade Item Number | 00012345678905 |
| `EAN` | European Article Number | 4006381333931 |
| `ISBN` | International Standard Book Number | 978-3-16-148410-0 |
| `SKU` | Stock Keeping Unit | RPT-12PK-ECO |
| `MPN` | Manufacturer Part Number | MFG-2024-001 |

### 7.5 Date/Time Format

All dates and times use ISO 8601 format:

```
2024-01-15T00:00:00Z       (UTC)
2024-01-15T09:30:00-05:00  (With timezone offset)
```

---

## 8. Integration Workflows

### 8.1 Initial Data Sync

**Objective:** Retrieve all available certificates for initial database population.

```
┌─────────────────────────────────────────────────────────────────────┐
│                    INITIAL DATA SYNC WORKFLOW                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Step 1: Get Total Count                                            │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ GET /certificates?pageSize=1                                 │   │
│  │ Response: { pagination: { totalCount: 184, totalPages: 184 }}│   │
│  └─────────────────────────────────────────────────────────────┘   │
│                          │                                          │
│                          ▼                                          │
│  Step 2: Paginate Through All Records                               │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ FOR page = 1 TO totalPages:                                  │   │
│  │   GET /certificates?includeProductIds=true&pageNumber={page} │   │
│  │   Store certificates in local database                       │   │
│  │   Respect rate limits (add delay if needed)                  │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                          │                                          │
│                          ▼                                          │
│  Step 3: Download PDFs (Optional)                                   │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ FOR each certificate:                                        │   │
│  │   GET /certificates/{number}/download                        │   │
│  │   Store PDF locally or in document management system         │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 8.2 ASIN Verification Workflow

**Objective:** Verify certification status for a specific ASIN.

```
┌─────────────────────────────────────────────────────────────────────┐
│                   ASIN VERIFICATION WORKFLOW                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Input: ASIN = "B08XQRT456"                                        │
│                          │                                          │
│                          ▼                                          │
│  Step 1: Search by ASIN                                             │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ GET /certificates/search/productid?productId=B08XQRT456     │   │
│  │                        &type=ASIN                            │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                          │                                          │
│                          ▼                                          │
│  Step 2: Process Response                                           │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ IF data.length == 0:                                         │   │
│  │   → ASIN not found in certified products                     │   │
│  │ ELSE:                                                        │   │
│  │   → Extract certificate details                              │   │
│  │   → Check status == "Active"                                 │   │
│  │   → Check expirationDate > today                             │   │
│  │   → Extract certificationResults for threshold validation    │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                          │                                          │
│                          ▼                                          │
│  Step 3: Validate Certification Results (If Required)              │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ Parse certificationResults field                             │   │
│  │ Compare against Amazon threshold requirements                │   │
│  │ Return validation result                                     │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 8.3 Incremental Sync Workflow

**Objective:** Keep local data synchronized with GCC updates.

**Recommended Approach:**
1. Store `updatedAt` timestamp from last sync
2. Poll for all certificates periodically (daily recommended)
3. Compare `updatedAt` to detect changes
4. Update local records as needed

```
┌─────────────────────────────────────────────────────────────────────┐
│                  INCREMENTAL SYNC WORKFLOW                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Daily at 02:00 UTC (or preferred schedule):                       │
│                          │                                          │
│                          ▼                                          │
│  Step 1: Retrieve All Certificates                                  │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ GET /certificates?includeProductIds=true&pageSize=100       │   │
│  │ (Paginate through all pages)                                 │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                          │                                          │
│                          ▼                                          │
│  Step 2: Compare and Update                                         │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ FOR each certificate:                                        │   │
│  │   IF certificate.id NOT IN local_db:                         │   │
│  │     → INSERT new record                                      │   │
│  │   ELSE IF certificate.updatedAt > local_record.updatedAt:    │   │
│  │     → UPDATE existing record                                 │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                          │                                          │
│                          ▼                                          │
│  Step 3: Check for Deletions (Optional)                            │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ Compare local IDs with received IDs                          │   │
│  │ Mark missing records as "no longer available"                │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 9. Error Handling & Troubleshooting

### 9.1 Error Response Format

All error responses follow a consistent format:

```json
{
  "success": false,
  "errors": [
    {
      "code": "ERROR_CODE",
      "message": "Human-readable error description",
      "field": "fieldName"
    }
  ],
  "traceId": "abc123-def456-ghi789"
}
```

### 9.2 HTTP Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Request successful |
| 201 | Created | Resource created (not applicable for partners) |
| 204 | No Content | Request successful, no body returned |
| 400 | Bad Request | Invalid request parameters |
| 401 | Unauthorized | Invalid or missing API key |
| 403 | Forbidden | Endpoint not permitted for your key |
| 404 | Not Found | Resource does not exist |
| 406 | Not Acceptable | Content negotiation failed |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server-side error |
| 503 | Service Unavailable | Temporary service outage |

### 9.3 Error Codes Reference

| Error Code | HTTP Status | Description | Resolution |
|------------|-------------|-------------|------------|
| `UNAUTHORIZED` | 401 | Invalid or missing API key | Verify X-API-Key header |
| `FORBIDDEN` | 403 | Endpoint access denied | Contact GCC for permissions |
| `RESOURCE_NOT_FOUND` | 404 | Certificate/resource not found | Verify ID or number |
| `VALIDATION_ERROR` | 400 | Invalid request parameters | Check parameter values |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests | Wait and retry with backoff |
| `INTERNAL_ERROR` | 500 | Server error | Retry later, contact support if persistent |

### 9.4 Common Issues & Solutions

| Issue | Possible Cause | Solution |
|-------|----------------|----------|
| Empty data array returned | No matching certificates for your partner | Verify partner association in GCC admin |
| 401 on all requests | Invalid API key | Request new key from GCC |
| 401 after key was working | Key revoked or expired | Contact GCC partner management |
| 404 for known certificate | Certificate not associated with your partner | Verify certificate partner field in GCC |
| Slow response times | Large result set | Use pagination, reduce pageSize |
| Missing productIdentifiers | includeProductIds not set | Add `?includeProductIds=true` |

### 9.5 Retry Strategy

Implement exponential backoff for transient errors:

```
Attempt 1: Wait 1 second
Attempt 2: Wait 2 seconds
Attempt 3: Wait 4 seconds
Attempt 4: Wait 8 seconds
Attempt 5: Wait 16 seconds
Maximum: 5 attempts, then fail and alert
```

**Retryable Errors:**
- 429 Too Many Requests
- 500 Internal Server Error
- 503 Service Unavailable
- Network timeouts

**Non-Retryable Errors:**
- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found

---

## 10. Rate Limiting & Performance

### 10.1 Rate Limits

| Metric | Limit |
|--------|-------|
| Requests per hour | 200 |
| Requests per minute | ~3 (averaged) |
| Concurrent requests | 10 |
| Maximum page size | 100 items |

### 10.2 Rate Limit Headers

Responses include rate limit information:

| Header | Description |
|--------|-------------|
| `X-RateLimit-Limit` | Maximum requests per hour |
| `X-RateLimit-Remaining` | Remaining requests in current window |
| `X-RateLimit-Reset` | Unix timestamp when limit resets |

### 10.3 Rate Limit Response

When rate limit is exceeded:

```json
{
  "success": false,
  "errors": [
    {
      "code": "RATE_LIMIT_EXCEEDED",
      "message": "Rate limit exceeded. Please wait before retrying.",
      "retryAfter": 120
    }
  ]
}
```

HTTP Status: 429 Too Many Requests

### 10.4 Performance Best Practices

| Practice | Recommendation |
|----------|----------------|
| Pagination | Use `pageSize=100` for bulk operations |
| Caching | Cache responses locally (TTL: 1 hour recommended) |
| Incremental Sync | Only sync daily, not real-time |
| Parallel Requests | Limit to 5 concurrent requests |
| Compression | Accept gzip encoding for faster transfers |

### 10.5 Expected Response Times

| Endpoint | Expected Response Time |
|----------|----------------------|
| List certificates (20 items) | < 200ms |
| List certificates (100 items) | < 500ms |
| List with productIdentifiers | < 500ms |
| Get by ID/Number | < 150ms |
| Search by ASIN | < 200ms |
| PDF Download | < 2000ms |

---

## 11. Testing & Validation

### 11.1 Pre-Integration Checklist

| Item | Status |
|------|--------|
| API key received from GCC | [ ] |
| API key stored securely | [ ] |
| HTTPS client configured | [ ] |
| Test ASINs identified | [ ] |
| Error handling implemented | [ ] |
| Rate limit handling implemented | [ ] |

### 11.2 Integration Test Cases

#### Authentication Tests

| Test ID | Test Case | Expected Result |
|---------|-----------|-----------------|
| AUTH-01 | Valid API key | 200 OK |
| AUTH-02 | Invalid API key | 401 Unauthorized |
| AUTH-03 | Missing API key | 401 Unauthorized |
| AUTH-04 | Expired API key | 401 Unauthorized |

#### Functional Tests

| Test ID | Test Case | Expected Result |
|---------|-----------|-----------------|
| FUNC-01 | List certificates | 200 with data array |
| FUNC-02 | List with includeProductIds=true | Response includes productIdentifiers |
| FUNC-03 | Get certificate by ID | 200 with certificate object |
| FUNC-04 | Get certificate by number | 200 with certificate object |
| FUNC-05 | Search by valid ASIN | 200 with matching certificate |
| FUNC-06 | Search by invalid ASIN | 200 with empty data array |
| FUNC-07 | Download PDF | Binary PDF file returned |
| FUNC-08 | Get non-existent certificate | 404 Not Found |

#### Field Validation Tests

| Test ID | Test Case | Expected Result |
|---------|-----------|-----------------|
| FIELD-01 | certificateNumber format | Matches YY-NNNN pattern |
| FIELD-02 | issueDate format | Valid ISO 8601 |
| FIELD-03 | expirationDate format | Valid ISO 8601 |
| FIELD-04 | issuer value | "Green Circle Certified" |
| FIELD-05 | certificationResults present | Field exists (may be null) |
| FIELD-06 | productIdentifiers structure | Array with type, value, status, partner |

### 11.3 Test Script (PowerShell)

```powershell
# GCC API Integration Test Script
param(
    [string]$BaseUrl = "https://[production-domain]",
    [string]$ApiKey = "YOUR_API_KEY"
)

$headers = @{
    "X-API-Key" = $ApiKey
    "Accept" = "application/json"
}

function Test-Endpoint {
    param($Name, $Url, $Expected)
    try {
        $response = Invoke-WebRequest -Uri $Url -Headers $headers -Method Get
        $status = $response.StatusCode
        $passed = ($status -eq $Expected)
        $result = if($passed) { "PASS" } else { "FAIL" }
        Write-Host "$Name : HTTP $status [$result]" -ForegroundColor $(if($passed){'Green'}else{'Red'})
        return $passed
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        $passed = ($status -eq $Expected)
        $result = if($passed) { "PASS" } else { "FAIL" }
        Write-Host "$Name : HTTP $status [$result]" -ForegroundColor $(if($passed){'Green'}else{'Red'})
        return $passed
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "GCC Partner API Integration Tests" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Run tests
$results = @()
$results += Test-Endpoint "Authentication" "$BaseUrl/api/v1/certificates?pageSize=1" 200
$results += Test-Endpoint "List with ProductIds" "$BaseUrl/api/v1/certificates?includeProductIds=true&pageSize=1" 200
$results += Test-Endpoint "Search by ASIN" "$BaseUrl/api/v1/certificates/search/productid?productId=B08TEST123&type=ASIN" 200
$results += Test-Endpoint "Invalid Certificate" "$BaseUrl/api/v1/certificates/999999" 404

# Summary
$passed = ($results | Where-Object { $_ }).Count
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Results: $passed/$($results.Count) tests passed" -ForegroundColor $(if($passed -eq $results.Count){'Green'}else{'Yellow'})
Write-Host "========================================`n" -ForegroundColor Cyan
```

### 11.4 Test Script (Bash/cURL)

```bash
#!/bin/bash
# GCC API Integration Test Script

BASE_URL="${1:-https://[production-domain]}"
API_KEY="${2:-YOUR_API_KEY}"

echo "========================================"
echo "GCC Partner API Integration Tests"
echo "========================================"
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
        return 0
    else
        echo "FAIL: $name (Expected $expected, Got $status)"
        return 1
    fi
}

# Run tests
test_endpoint "Authentication" "$BASE_URL/api/v1/certificates?pageSize=1" "200"
test_endpoint "List with ProductIds" "$BASE_URL/api/v1/certificates?includeProductIds=true&pageSize=1" "200"
test_endpoint "Search by ASIN" "$BASE_URL/api/v1/certificates/search/productid?productId=B08TEST123&type=ASIN" "200"
test_endpoint "Invalid Certificate" "$BASE_URL/api/v1/certificates/999999" "404"

echo ""
echo "========================================"
echo "Tests Complete"
echo "========================================"
```

### 11.5 Validation Checklist

Use this checklist before going live:

| Category | Item | Verified |
|----------|------|----------|
| **Authentication** | API key authenticates successfully | [ ] |
| | Invalid key returns 401 | [ ] |
| **Data Access** | Returns only Amazon-associated certificates | [ ] |
| | Cannot see other partners' data | [ ] |
| **Endpoints** | List certificates works | [ ] |
| | includeProductIds returns ASINs | [ ] |
| | ASIN search returns correct certificate | [ ] |
| | PDF download works | [ ] |
| **Fields** | All required fields present | [ ] |
| | certificationResults accessible | [ ] |
| | Dates in ISO 8601 format | [ ] |
| **Error Handling** | 404 handled correctly | [ ] |
| | Rate limit handling implemented | [ ] |
| **Performance** | Response times acceptable | [ ] |
| | Pagination working correctly | [ ] |

---

## 12. Support & Escalation

### 12.1 Support Contacts

| Type | Contact | Response Time |
|------|---------|---------------|
| Technical Support | api-support@greencirclecertified.com | 4 business hours |
| Partner Management | partners@greencirclecertified.com | 1 business day |
| Emergency (Outages) | [Emergency hotline] | 1 hour |

### 12.2 Support Ticket Information

When submitting a support request, include:

1. **API Key** (last 8 characters only)
2. **Timestamp** of the issue (UTC)
3. **Endpoint** being called
4. **HTTP Status Code** received
5. **Error Response** (full JSON body)
6. **Request ID / Trace ID** (if provided)
7. **Steps to Reproduce**

### 12.3 Escalation Matrix

| Severity | Definition | Response Target | Resolution Target |
|----------|------------|-----------------|-------------------|
| Critical | Complete API outage | 1 hour | 4 hours |
| High | Major feature unavailable | 4 hours | 1 business day |
| Medium | Non-critical issue | 1 business day | 3 business days |
| Low | Question or enhancement | 3 business days | As scheduled |

### 12.4 Maintenance Windows

| Type | Schedule | Notification |
|------|----------|--------------|
| Scheduled Maintenance | Sundays 02:00-06:00 UTC | 72 hours advance notice |
| Emergency Maintenance | As needed | Best effort notification |
| Version Upgrades | Quarterly | 30 days advance notice |

---

## 13. Appendices

### Appendix A: Sample Request Collection

#### A.1 List All Certificates with ASINs

```bash
curl -X GET "https://[domain]/api/v1/certificates?includeProductIds=true&pageSize=100" \
  -H "X-API-Key: gcc_partner_[key]" \
  -H "Accept: application/json"
```

#### A.2 Search by ASIN

```bash
curl -X GET "https://[domain]/api/v1/certificates/search/productid?productId=B08XQRT456&type=ASIN" \
  -H "X-API-Key: gcc_partner_[key]" \
  -H "Accept: application/json"
```

#### A.3 Get by Certificate Number

```bash
curl -X GET "https://[domain]/api/v1/certificates/number/24-0366" \
  -H "X-API-Key: gcc_partner_[key]" \
  -H "Accept: application/json"
```

#### A.4 Download Certificate PDF

```bash
curl -X GET "https://[domain]/api/v1/certificates/24-0366/download" \
  -H "X-API-Key: gcc_partner_[key]" \
  -o certificate.pdf
```

---

### Appendix B: Field Mapping Summary

| Amazon Requirement | GCC API Field | Notes |
|--------------------|---------------|-------|
| ASIN | `productIds[].value` where `type="ASIN"` | Requires `includeProductIds=true` |
| Brand Name | `company` | Company/brand name |
| Product Name | `productName` | From Product entity |
| Product Type | `productType` | Product type name (in search results) |
| Certificate ID | `certificateNumber` | Format: YY-NNNN |
| Issue Date | `issueDate` | ISO 8601 format |
| Expiration Date | `expirationDate` | ISO 8601 format |
| Certification Type | `certificationType` | e.g., "UL 2809" |
| Standard Version | `certificationTypeSubcategory` | May be null |
| Issuer | `issuer` | Always "Green Circle Certified" |
| Certification Results | `certificationResults` | Threshold/score data for compliance |
| Certificate PDF | `pdfDownloadUrl` | Relative URL for download |
| Partner | `partner` | "Amazon" for filtered results |
| Status | `status` | Active, Expired, etc. |

---

### Appendix C: Glossary

| Term | Definition |
|------|------------|
| API Key | A unique token used to authenticate API requests |
| ASIN | Amazon Standard Identification Number - Amazon's product identifier |
| Certificate | A document verifying a product meets specific sustainability standards |
| Certificate Number | GCC's unique identifier for a certificate (format: YY-NNNN) |
| Certification Results | Quantitative results from certification testing |
| EAN | European Article Number - a product barcode standard |
| GTIN | Global Trade Item Number - an international product identifier |
| ISO 8601 | International standard for date and time representation |
| Partner | An organization with authorized API access |
| Product Identifier | A unique code identifying a product (ASIN, UPC, GTIN, etc.) |
| Rate Limit | Maximum number of API requests allowed per time period |
| REST | Representational State Transfer - an architectural style for APIs |
| TLS | Transport Layer Security - encryption protocol for HTTPS |
| UPC | Universal Product Code - a barcode standard used in North America |
| UTC | Coordinated Universal Time - the primary time standard |

---

### Appendix D: Change Log

| Version | Date | Changes |
|---------|------|---------|
| 1.1 | 2026-01-28 | Standardized field names to camelCase across all endpoints. Changed: `brandName`→`company`, `certificationName`→`certificationType`, `standardVersion`→`certificationTypeSubcategory`, `productIdentifiers`→`productIds`. Added `companyId`, `productId`, `productType`, `certificationTypeId`, `certificationTypeSubcategoryId` fields. |
| 1.0 | 2026-01-18 | Initial release |

---

**Document End**

*This document is confidential and intended for Amazon's integration team. Unauthorized distribution is prohibited.*

*Green Circle Certified | www.greencirclecertified.com*
