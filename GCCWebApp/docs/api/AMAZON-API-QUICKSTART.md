# Amazon API Quick Start Guide

**Green Circle Certified - Partner API Integration**

**Version:** 2.1
**Date:** 2026-01-28

---

## Overview

This guide helps Amazon integrate with the GCC (Green Circle Certified) Partner API to retrieve certificate and product identifier data.

**Integration Model:** Amazon PULLS data from GCC using API keys.

---

## 1. Getting Your API Key

Contact Green Circle Certified to receive your partner API key. Keys follow this format:

```
gcc_partner_[32-character-hash]
```

Example: `gcc_partner_b451fb3b31c645e9dd900c3c7b2f6250fbb7480bbb4510d602efc5e52102fc4d`

**Important:** Keep your API key secure. Do not share it in public repositories or client-side code.

---

## 2. Authentication

All API requests require the `X-API-Key` header:

```bash
curl -X GET "https://[production-domain]/api/v1/certificates" \
  -H "X-API-Key: YOUR_API_KEY" \
  -H "Accept: application/json"
```

### Response Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 401 | Invalid or missing API key |
| 403 | Endpoint not permitted for your key |
| 404 | Resource not found |
| 429 | Rate limit exceeded (200 requests/hour) |

---

## 3. Data Access Scope

Your API key determines what data you can access:

| Association Type | Data Access |
|------------------|-------------|
| Partner = Amazon | Certificates where Partner field = "Amazon" |
| Company associations | Certificates for those specific companies |
| Both Partner + Companies | Certificates matching EITHER (OR logic) |
| No associations | No data access (empty results) |

**Note:** You will only see certificates and product identifiers associated with your partner account. This is automatic - no additional filtering is needed.

---

## 4. Key Endpoints

### Base URL
```
Production: https://[production-domain]/api/v1
```

### Available Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /certificates` | List all accessible certificates |
| `GET /certificates?includeProductIds=true` | List with ASINs/UPCs |
| `GET /certificates/gs1` | **GS1 JSON-LD format (RECOMMENDED for Amazon)** |
| `GET /certificates/gs1/{number}` | Single certificate in GS1 format |
| `GET /certificates/{id}` | Get certificate by database ID |
| `GET /certificates/number/{number}` | Get certificate by number (e.g., 24-0366) |
| `GET /certificates/search/productid` | Search by ASIN/UPC |
| `GET /certificates/{number}/download` | Download certificate PDF |

---

## 5. Common Use Cases

### 5.1 GS1 JSON-LD Format (RECOMMENDED)

**This is the primary endpoint for Amazon Climate Pledge Friendly integration.**

The GS1 endpoint returns data in the GS1 Web Vocabulary JSON-LD format, which matches Amazon's certification data requirements.

```bash
curl -X GET "https://[domain]/api/v1/certificates/gs1?pageSize=100" \
  -H "X-API-Key: YOUR_API_KEY" \
  -H "Accept: application/json"
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "@context": "https://gs1us-technology.github.io/certification-data-model/cert_context.json",
      "$schema": "https://gs1us-technology.github.io/certification-data-model/cert_schema.json",
      "id": "https://greencirclecertified.com/api/v1/certificates/gs1/24-0366",
      "type": "CertificationReport",
      "issueDate": "2024-01-15",
      "issuer": "Green Circle Certified",
      "products": [
        {
          "id": "https://id.gs1.org/01/00012345678901",
          "type": "Product",
          "gtin": "00012345678901",
          "additionalTradeItemId": [
            { "type": "ASIN", "value": "B08XQRT456" }
          ],
          "name": "Recycled Paper Towels",
          "description": "100% recycled paper towels",
          "certificationInfo": {
            "id": "https://greencirclecertified.com/api/v1/certificates/gs1/24-0366/certification",
            "type": "CertificationDetails",
            "agency": "Green Circle Certified",
            "agencyURL": "https://www.greencirclecertified.com",
            "program": "UL 2809",
            "certificateId": "24-0366",
            "standard": "UL 2809",
            "status": "ACTIVE",
            "value": "95.5% recycled content",
            "startDate": "2024-01-15",
            "endDate": "2025-01-15",
            "targetCountries": ["United States"]
          }
        }
      ]
    }
  ],
  "pagination": {
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 100
  }
}
```

**Key GS1 Fields:**
- `@context`, `$schema`: JSON-LD metadata for schema validation
- `gtin`: Global Trade Item Number (14 digits, padded with leading zeros)
- `additionalTradeItemId`: Array of ASIN, SKU, MPN, ISBN identifiers
- `certificationInfo.status`: ACTIVE or INACTIVE
- `certificationInfo.value`: Certification results/level (e.g., recycled content percentage)

### 5.2 List All Certificates with Product Identifiers

**Alternative endpoint with flat JSON structure.**

```bash
curl -X GET "https://[domain]/api/v1/certificates?includeProductIds=true&pageSize=100" \
  -H "X-API-Key: YOUR_API_KEY" \
  -H "Accept: application/json"
```

**Response:**
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
      "companyId": 45,
      "productName": "Recycled Paper Towels",
      "productId": 78,
      "certificationType": "UL 2809",
      "certificationTypeId": 3,
      "certificationTypeSubcategory": "Level 2",
      "certificationTypeSubcategoryId": 12,
      "issuer": "Green Circle Certified",
      "certificationResults": "95.5% recycled content verified",
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
          "partner": null,
          "productName": "Recycled Paper Towels",
          "productSku": "RPT-SINGLE",
          "description": "Single roll recycled paper towels"
        }
      ],
      "pdfDownloadUrl": "/api/v1/certificates/24-0366/download"
    }
  ],
  "pagination": {
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 100,
    "totalPages": 2,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

### 5.3 Search by ASIN

Find certificates containing a specific ASIN:

```bash
curl -X GET "https://[domain]/api/v1/certificates/search/productid?productId=B08XQRT456&type=ASIN" \
  -H "X-API-Key: YOUR_API_KEY" \
  -H "Accept: application/json"
```

**Response:**
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
      "pdfDownloadUrl": "/api/v1/certificates/24-0366/download",
      "productIds": [
        {
          "type": "ASIN",
          "value": "B08XQRT456",
          "status": "Active",
          "partner": "Amazon",
          "productName": "Recycled Paper Towels - 12 Pack",
          "productSku": "RPT-12PK-ECO",
          "description": "12-pack product",
          "isMatchedId": true
        }
      ]
    }
  ],
  "message": "Found 1 certificate(s) with 1 total product IDs for search: B08XQRT456"
}
```

**Note:** The `isMatchedId: true` flag indicates which product identifier matched your search.

### 5.3 Get Certificate by Number

```bash
curl -X GET "https://[domain]/api/v1/certificates/number/24-0366" \
  -H "X-API-Key: YOUR_API_KEY" \
  -H "Accept: application/json"
```

### 5.4 Download Certificate PDF

```bash
curl -X GET "https://[domain]/api/v1/certificates/24-0366/download" \
  -H "X-API-Key: YOUR_API_KEY" \
  -o certificate.pdf
```

---

## 6. Field Mapping Reference

### Certificate Fields

| Amazon Requirement | GCC API Field | Notes |
|--------------------|---------------|-------|
| Product ASIN | `productIds[].value` (where type="ASIN") | Use `includeProductIds=true` |
| Brand name | `company` | Company/brand name |
| Product name | `productName` | Product name |
| Product type | `productType` | Product type (in search results) |
| Certificate ID | `certificateNumber` | Format: YY-NNNN |
| Issue date | `issueDate` | ISO 8601 format |
| Expiration date | `expirationDate` | ISO 8601 format |
| Certification type | `certificationType` | e.g., "UL 2809" |
| Standard version | `certificationTypeSubcategory` | May be null |
| Issuer | `issuer` | Always "Green Circle Certified" |
| **Certification Results** | `certificationResults` | Threshold/score data for compliance |
| Partner | `partner` | "Amazon" for filtered results |
| Status | `status` | Active, Expired, etc. |
| PDF download | `pdfDownloadUrl` | Relative URL path |

### Product Identifier Fields

Each item in `productIds[]` contains:

| Field | Type | Description |
|-------|------|-------------|
| type | string | ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |
| value | string | The identifier value |
| status | string | Active, Inactive, Pending, Expired |
| partner | string | Amazon, Novi, None, Other |
| productName | string | Product name for this specific ID |
| productSku | string | SKU for this identifier |
| description | string | Optional notes |
| isMatchedId | boolean | True if this ID matched search (search only) |

---

## 7. Pagination

Use these query parameters to paginate results:

| Parameter | Default | Max | Description |
|-----------|---------|-----|-------------|
| `pageNumber` | 1 | - | Page to retrieve |
| `pageSize` | 20 | 100 | Items per page |

**Example:** Get page 2 with 50 items:
```bash
curl "https://[domain]/api/v1/certificates?includeProductIds=true&pageNumber=2&pageSize=50" \
  -H "X-API-Key: YOUR_API_KEY"
```

**Pagination Response Fields:**
- `totalCount` - Total number of certificates
- `pageNumber` - Current page
- `pageSize` - Items per page
- `totalPages` - Total pages available
- `hasNext` - More pages available
- `hasPrevious` - Previous pages exist

---

## 8. Rate Limits

| Metric | Limit |
|--------|-------|
| Requests per hour | 200 |
| Requests per minute | ~3 |

When rate limited, you'll receive HTTP 429. Implement exponential backoff in your integration.

---

## 9. Testing Checklist

Use this checklist to validate your integration:

- [ ] Authenticate with API key (expect 200)
- [ ] Test invalid API key (expect 401)
- [ ] List certificates with `includeProductIds=true`
- [ ] Verify ASIN values appear in `productIds[]`
- [ ] Search by a known ASIN
- [ ] Verify `certificationResults` field is present
- [ ] Download a certificate PDF
- [ ] Test pagination (page 1, page 2)
- [ ] Verify date formats are ISO 8601

---

## 10. Sample Test Script

### PowerShell

```powershell
$apiKey = "YOUR_API_KEY"
$baseUrl = "https://[production-domain]"

$headers = @{
    "X-API-Key" = $apiKey
    "Accept" = "application/json"
}

# Test authentication
$response = Invoke-RestMethod -Uri "$baseUrl/api/v1/certificates?pageSize=1" -Headers $headers
Write-Host "Total certificates: $($response.pagination.totalCount)"

# List with product IDs
$response = Invoke-RestMethod -Uri "$baseUrl/api/v1/certificates?includeProductIds=true&pageSize=5" -Headers $headers
foreach ($cert in $response.data) {
    Write-Host "Certificate: $($cert.certificateNumber) - $($cert.productName)"
    foreach ($pid in $cert.productIds) {
        if ($pid.type -eq "ASIN") {
            Write-Host "  ASIN: $($pid.value)"
        }
    }
}
```

### Bash/cURL

```bash
API_KEY="YOUR_API_KEY"
BASE_URL="https://[production-domain]"

# Test authentication
curl -s -H "X-API-Key: $API_KEY" "$BASE_URL/api/v1/certificates?pageSize=1" | jq '.pagination.totalCount'

# List with product IDs
curl -s -H "X-API-Key: $API_KEY" "$BASE_URL/api/v1/certificates?includeProductIds=true&pageSize=5" | jq '.data[] | {cert: .certificateNumber, asins: [.productIds[] | select(.type=="ASIN") | .value]}'

# Search by ASIN
curl -s -H "X-API-Key: $API_KEY" "$BASE_URL/api/v1/certificates/search/productid?productId=B08XQRT456&type=ASIN" | jq '.'
```

---

## 11. Error Handling

### Error Response Format

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

### Common Error Codes

| Code | Description | Resolution |
|------|-------------|------------|
| `UNAUTHORIZED` | Invalid or missing API key | Check X-API-Key header |
| `FORBIDDEN` | Endpoint not permitted | Contact GCC for permissions |
| `RESOURCE_NOT_FOUND` | Certificate/resource not found | Verify ID or number |
| `VALIDATION_ERROR` | Invalid request parameters | Check query parameters |
| `RATE_LIMIT_EXCEEDED` | Too many requests | Wait and retry with backoff |

---

## 12. Support

For API issues or questions:

- **Technical Support:** [Contact GCC Technical Team]
- **API Key Issues:** [Contact GCC Admin]
- **Documentation:** See full API reference at `/swagger` (development only)

---

## Appendix: Quick Reference

### Minimum Required Headers
```
X-API-Key: YOUR_API_KEY
Accept: application/json
```

### Most Common Request
```bash
GET /api/v1/certificates?includeProductIds=true&pageSize=100
```

### Certificate Status Values
- `Draft` - Not yet active
- `Active` - Currently valid
- `Expired` - Past expiration date
- `Suspended` - Temporarily inactive
- `Revoked` - Permanently invalidated

---

*Document maintained by: Green Circle Certified*
*Last updated: 2026-01-28*
