# GCC Web App - API Manual Testing Guide

This guide provides step-by-step instructions for manually testing the API endpoints using curl, PowerShell, or Postman.

## Prerequisites

1. **Start the local server**:
   ```powershell
   cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"
   dotnet run --urls "http://localhost:5101"
   ```

2. **Verify server is running**:
   ```powershell
   curl http://localhost:5101/health
   ```
   Expected: `Healthy`

---

## Section 1: Public API Endpoints (No Authentication Required)

### 1.1 Certificate Tile View API

#### Get Certificates (Infinite Scroll)
```powershell
# Basic request - first page
curl "http://localhost:5101/api/certificate-tiles?page=1&pageSize=10"

# With search term
curl "http://localhost:5101/api/certificate-tiles?searchTerm=amazon&page=1&pageSize=10"

# With filters
curl "http://localhost:5101/api/certificate-tiles?certType=UL&productCategory=Electronics&page=1&pageSize=10"

# With sorting
curl "http://localhost:5101/api/certificate-tiles?sortBy=certificateNumber&sortDescending=true"
```

**Expected Response Structure**:
```json
{
  "success": true,
  "certificates": [
    {
      "id": 1,
      "certificateNumber": "24-0001",
      "companyName": "Test Company",
      "productName": "Test Product",
      "certificationType": "UL",
      "status": "Active",
      "expirationDate": "2025-12-31"
    }
  ],
  "totalCount": 100,
  "loadedCount": 10,
  "hasMore": true
}
```

#### Get Filter Options
```powershell
curl "http://localhost:5101/api/certificate-tiles/filters"
```

**Expected Response**:
```json
{
  "certificationTypes": ["UL", "CE", "FCC"],
  "companies": ["Company A", "Company B"],
  "countries": ["USA", "Canada"],
  "productCategories": ["Electronics", "Furniture"],
  "additionalTags": ["tag1", "tag2"]
}
```

#### Get Autocomplete Suggestions
```powershell
curl "http://localhost:5101/api/certificate-tiles/autocomplete?term=test"
```

**Expected Response**:
```json
{
  "suggestions": [
    {"category": "Certificate Number", "value": "24-0001", "display": "24-0001"},
    {"category": "Company", "value": "Test Company", "display": "Test Company"}
  ]
}
```

#### Download Certificate PDF
```powershell
# Download certificate PDF by ID
curl -o certificate.pdf "http://localhost:5101/api/certificate-tiles/1/download"
```

#### Generate Shareable Link
```powershell
curl -X POST "http://localhost:5101/api/certificate-tiles/share-link" `
  -H "Content-Type: application/json" `
  -d '{"search":"test","certType":"UL","productCategory":"Electronics"}'
```

**Expected Response**:
```json
{
  "url": "http://localhost:5101/Certificate/CertificateTileView?search=test&certType=UL&productCategory=Electronics"
}
```

---

## Section 2: Authenticated API Endpoints (Require API Key or Cookie)

### 2.1 Get API Key for Testing

**Option A: Use existing test API key from database**
```sql
-- Run in SSMS to get a test API key
SELECT TOP 1 KeyValue FROM ApiKeys WHERE IsBlocked = 0 AND IsDeleted = 0
```

**Option B: Login and get cookie**
```powershell
# Login to get authentication cookie
curl -c cookies.txt -X POST "http://localhost:5101/Account/Login" `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "Username=admin&Password=TestPass123!&RememberMe=false"
```

### 2.2 API v1 - Certificates (Requires API Key)

#### List Certificates with Pagination
```powershell
# Using API Key header
curl -H "X-API-Key: YOUR_API_KEY_HERE" `
  "http://localhost:5101/api/v1/certificates?pageNumber=1&pageSize=10"

# With search
curl -H "X-API-Key: YOUR_API_KEY_HERE" `
  "http://localhost:5101/api/v1/certificates?searchTerm=test&pageNumber=1&pageSize=10"

# With sorting
curl -H "X-API-Key: YOUR_API_KEY_HERE" `
  "http://localhost:5101/api/v1/certificates?sortBy=ExpirationDate&sortDescending=true"
```

**Expected Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "certificateNumber": "24-0001",
      "companyName": "Test Company",
      "status": "Active"
    }
  ],
  "pagination": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 50,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

#### Get Single Certificate
```powershell
curl -H "X-API-Key: YOUR_API_KEY_HERE" `
  "http://localhost:5101/api/v1/certificates/1"
```

#### Search by Product ID (ASIN, UPC, etc.)
```powershell
# Search by ASIN
curl -H "X-API-Key: YOUR_API_KEY_HERE" `
  "http://localhost:5101/api/v1/certificates/search/productid?productId=B08N5WRWNW&type=ASIN"

# Search by UPC
curl -H "X-API-Key: YOUR_API_KEY_HERE" `
  "http://localhost:5101/api/v1/certificates/search/productid?productId=012345678901&type=UPC"
```

#### Get Expiring Certificates
```powershell
curl -H "X-API-Key: YOUR_API_KEY_HERE" `
  "http://localhost:5101/api/v1/certificates/expiring"
```

---

## Section 3: Internal API (Analyst/Admin Only)

### 3.1 Certificate Tile Internal API

```powershell
# Using cookie authentication (must be logged in as Analyst or Admin)
curl -b cookies.txt "http://localhost:5101/api/certificate-tiles-internal?page=1&pageSize=10"

# With status filter (internal only)
curl -b cookies.txt `
  "http://localhost:5101/api/certificate-tiles-internal?status=Draft&page=1&pageSize=10"

# With partner filter
curl -b cookies.txt `
  "http://localhost:5101/api/certificate-tiles-internal?partner=Amazon&page=1&pageSize=10"
```

**Additional Fields (Internal Only)**:
- `status`: Draft, Active, Expired, Pending Review, etc.
- `partner`: Amazon, Wayfair, None
- `internalTags`: Internal classification tags

---

## Section 4: External API (Partner Access)

### 4.1 Certificate Tile External API

```powershell
# Using Partner API Key (prefixed with gcc_partner_)
curl -H "X-API-Key: gcc_partner_YOUR_PARTNER_KEY" `
  "http://localhost:5101/api/CertificateTileExternal?page=1&pageSize=10"

# Or using cookie authentication (External role)
curl -b cookies.txt "http://localhost:5101/api/CertificateTileExternal?page=1&pageSize=10"
```

**Note**: External users only see certificates associated with their company/partner.

---

## Section 5: Partner API Management (Admin Only)

### 5.1 List Partners
```powershell
curl -b cookies.txt "http://localhost:5101/api/admin/partner/list"
```

### 5.2 Generate Partner API Key
```powershell
curl -b cookies.txt -X POST "http://localhost:5101/api/admin/partner/1/generatekey" `
  -H "Content-Type: application/json" `
  -d '{"name":"Amazon API Key","description":"For Amazon integration","scopes":"read:certificates"}'
```

### 5.3 Get Partner Usage Statistics
```powershell
curl -b cookies.txt "http://localhost:5101/api/admin/partner/1/usage"
```

---

## Section 6: JWT Authentication API

### 6.1 Login and Get Token
```powershell
curl -X POST "http://localhost:5101/api/v1/auth/login" `
  -H "Content-Type: application/json" `
  -d '{"username":"admin","password":"TestPass123!"}'
```

**Expected Response**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abc123...",
  "tokenType": "Bearer",
  "expiresIn": 86400,
  "user": {
    "id": 1,
    "username": "admin",
    "email": "swest@anukiconsulting.com",
    "roles": ["Admin"]
  }
}
```

### 6.2 Use JWT Token
```powershell
curl -H "Authorization: Bearer YOUR_ACCESS_TOKEN" `
  "http://localhost:5101/api/v1/certificates"
```

### 6.3 Refresh Token
```powershell
curl -X POST "http://localhost:5101/api/v1/auth/refresh" `
  -H "Content-Type: application/json" `
  -d '{"accessToken":"YOUR_EXPIRED_TOKEN","refreshToken":"YOUR_REFRESH_TOKEN"}'
```

---

## Section 7: Swagger UI Testing

Open in browser: **http://localhost:5101/swagger**

Three documentation tiers:
1. **GCC Web API - Admin & General** - Full API access
2. **Partner Data Access API** - Read-only certificate access
3. **Partner API Management** - Admin-only key management

---

## Section 8: Common Test Scenarios

### Test 1: Pagination Edge Cases
```powershell
# Page 0 (should default to 1)
curl "http://localhost:5101/api/certificate-tiles?page=0&pageSize=10"

# Negative page size (should default)
curl "http://localhost:5101/api/certificate-tiles?page=1&pageSize=-5"

# Very large page size (should cap)
curl "http://localhost:5101/api/certificate-tiles?page=1&pageSize=10000"

# Page beyond data (should return empty)
curl "http://localhost:5101/api/certificate-tiles?page=99999&pageSize=10"
```

### Test 2: Search Edge Cases
```powershell
# Empty search
curl "http://localhost:5101/api/certificate-tiles?searchTerm="

# Special characters
curl "http://localhost:5101/api/certificate-tiles?searchTerm=%26%3C%3E"

# Very long search term
curl "http://localhost:5101/api/certificate-tiles?searchTerm=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"

# SQL injection attempt (should be safe)
curl "http://localhost:5101/api/certificate-tiles?searchTerm='; DROP TABLE Certificates;--"
```

### Test 3: Authentication Tests
```powershell
# No API key (should return 401)
curl "http://localhost:5101/api/v1/certificates"

# Invalid API key (should return 401)
curl -H "X-API-Key: invalid_key_12345" "http://localhost:5101/api/v1/certificates"

# Expired API key (should return 401)
curl -H "X-API-Key: expired_key_here" "http://localhost:5101/api/v1/certificates"

# Blocked API key (should return 401)
curl -H "X-API-Key: blocked_key_here" "http://localhost:5101/api/v1/certificates"
```

### Test 4: Rate Limiting Tests
```powershell
# Run 100+ requests quickly to test rate limiting
for ($i=0; $i -lt 150; $i++) {
  curl -s -o $null -w "%{http_code}\n" `
    -H "X-API-Key: YOUR_API_KEY" `
    "http://localhost:5101/api/v1/certificates"
}
# Should start returning 429 (Too Many Requests) after limit exceeded
```

### Test 5: Partner Data Filtering
```powershell
# Login as External user (partner)
curl -c partner_cookies.txt -X POST "http://localhost:5101/Account/Login" `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "Username=external&Password=TestPass123!"

# Get certificates - should only see partner's data
curl -b partner_cookies.txt "http://localhost:5101/api/CertificateTileExternal?page=1&pageSize=100"

# Compare with Admin view
curl -c admin_cookies.txt -X POST "http://localhost:5101/Account/Login" `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "Username=admin&Password=TestPass123!"

curl -b admin_cookies.txt "http://localhost:5101/api/certificate-tiles-internal?page=1&pageSize=100"
```

---

## Section 9: Using Postman

### Import Collection
1. Open Postman
2. Import → Raw Text → Paste this:

```json
{
  "info": {
    "name": "GCC Web API Collection",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    {"key": "base_url", "value": "http://localhost:5101"},
    {"key": "api_key", "value": "YOUR_API_KEY_HERE"}
  ],
  "item": [
    {
      "name": "Public - Get Certificate Tiles",
      "request": {
        "method": "GET",
        "url": "{{base_url}}/api/certificate-tiles?page=1&pageSize=10"
      }
    },
    {
      "name": "Public - Get Filters",
      "request": {
        "method": "GET",
        "url": "{{base_url}}/api/certificate-tiles/filters"
      }
    },
    {
      "name": "Auth - Get Certificates (API Key)",
      "request": {
        "method": "GET",
        "url": "{{base_url}}/api/v1/certificates",
        "header": [{"key": "X-API-Key", "value": "{{api_key}}"}]
      }
    },
    {
      "name": "Auth - Login JWT",
      "request": {
        "method": "POST",
        "url": "{{base_url}}/api/v1/auth/login",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "body": {
          "mode": "raw",
          "raw": "{\"username\":\"admin\",\"password\":\"TestPass123!\"}"
        }
      }
    }
  ]
}
```

---

## Section 10: Expected HTTP Status Codes

| Code | Meaning | When |
|------|---------|------|
| 200 | OK | Successful GET/PUT |
| 201 | Created | Successful POST |
| 204 | No Content | Successful DELETE |
| 400 | Bad Request | Invalid input/validation error |
| 401 | Unauthorized | Missing/invalid authentication |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource doesn't exist |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server-side error |

---

## Troubleshooting

### Server Won't Start
```powershell
# Check if port is in use
netstat -ano | findstr :5101

# Kill existing process
taskkill /F /PID <PID>
```

### Database Connection Failed
```powershell
# Verify database connection string in appsettings.Development.json
# Test SQL Server connection
sqlcmd -S ANUKI01 -E -Q "SELECT 1"
```

### API Key Not Working
```sql
-- Check if API key exists and is valid
SELECT KeyValue, IsBlocked, ExpiresAt, LastUsedAt
FROM ApiKeys
WHERE KeyValue = 'your_key_here'
```

---

*Document created: 2025-12-07*
*Last updated: 2025-12-07*
