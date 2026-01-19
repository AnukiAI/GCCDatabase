# Amazon API Quick Start Guide

**Green Circle Certified - Partner API Integration**

**Version:** 1.0
**Date:** 2026-01-18

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
| 429 | Rate limit exceeded (2,000 requests/hour) |

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
| `GET /certificates?includeProductIds=true` | List with ASINs/UPCs (recommended) |
| `GET /certificates/{id}` | Get certificate by database ID |
| `GET /certificates/number/{number}` | Get certificate by number (e.g., 24-0366) |
| `GET /certificates/search/productid` | Search by ASIN/UPC |
| `GET /certificates/{number}/download` | Download certificate PDF |

---

## 5. Common Use Cases

### 5.1 List All Certificates with Product Identifiers

**This is the primary endpoint for Amazon integration.**

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
      "brandName": "EcoProduct Co",
      "productName": "Recycled Paper Towels",
      "certificationName": "UL 2809",
      "issuer": "Green Circle Certified",
      "standardVersion": "Level 2",
      "certificationResults": "95.5% recycled content verified",
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

### 5.2 Search by ASIN

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
      "company": "EcoProduct Co",
      "certificationResults": "95.5% recycled content verified",
      "status": "Active",
      "pdfUrl": "/api/v1/certificates/24-0366/download",
      "productIds": [
        {
          "type": "ASIN",
          "value": "B08XQRT456",
          "partner": "Amazon",
          "status": "Active",
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
| Product ASIN | `productIdentifiers[].value` (where type=ASIN) | Use `includeProductIds=true` |
| Brand name | `brandName` | Company name |
| Product name | `productName` | Product name |
| Certificate ID | `certificateNumber` | Format: YY-NNNN |
| Issue date | `issueDate` | ISO 8601 format |
| Expiration date | `expirationDate` | ISO 8601 format |
| Certification name | `certificationName` | e.g., "UL 2809" |
| Issuer | `issuer` | Always "Green Circle Certified" |
| Standard version | `standardVersion` | May be null |
| **Certification Results** | `certificationResults` | Threshold/score data |
| PDF download | `pdfDownloadUrl` | Relative URL path |

### Product Identifier Fields

Each item in `productIdentifiers[]` contains:

| Field | Type | Description |
|-------|------|-------------|
| type | string | ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |
| value | string | The identifier value |
| status | string | Active, Inactive, Pending, Expired |
| partner | string | Amazon, Novi, None, Other |
| productName | string | Product name for this specific ID |
| productSku | string | SKU for this identifier |
| description | string | Optional notes |

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
| Requests per hour | 2,000 |
| Requests per minute | ~33 |

When rate limited, you'll receive HTTP 429. Implement exponential backoff in your integration.

---

## 9. Testing Checklist

Use this checklist to validate your integration:

- [ ] Authenticate with API key (expect 200)
- [ ] Test invalid API key (expect 401)
- [ ] List certificates with `includeProductIds=true`
- [ ] Verify ASIN values appear in `productIdentifiers[]`
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
    foreach ($pid in $cert.productIdentifiers) {
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
curl -s -H "X-API-Key: $API_KEY" "$BASE_URL/api/v1/certificates?includeProductIds=true&pageSize=5" | jq '.data[] | {cert: .certificateNumber, asins: [.productIdentifiers[] | select(.type=="ASIN") | .value]}'

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
*Last updated: 2026-01-18*
