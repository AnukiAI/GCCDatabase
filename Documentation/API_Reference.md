# GCC Database API Reference Documentation

## Table of Contents
1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Base URL and Versioning](#base-url-and-versioning)
4. [Common Headers](#common-headers)
5. [Error Handling](#error-handling)
6. [Rate Limiting](#rate-limiting)
7. [API Endpoints](#api-endpoints)
   - [Certificates](#certificates)
   - [Companies](#companies)
   - [Products](#products)
   - [Users](#users)
   - [Admin](#admin)
8. [Response Formats](#response-formats)
9. [Webhooks](#webhooks)
10. [SDKs and Code Examples](#sdks-and-code-examples)

## Overview

The GCC Database API provides programmatic access to certificate, company, and product data. This RESTful API uses JSON for request and response payloads and follows standard HTTP conventions.

### Key Features
- RESTful architecture
- JSON request/response format
- OAuth 2.0 and API key authentication
- Comprehensive error handling
- Rate limiting for fair usage
- Webhook support for real-time updates

## Authentication

### OAuth 2.0 (Recommended)

The API uses OAuth 2.0 for authentication. Obtain an access token using the client credentials flow:

```http
POST /oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id={your_client_id}
&client_secret={your_client_secret}
&scope=certificates.read certificates.write
```

**Response:**
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "certificates.read certificates.write"
}
```

### API Key Authentication

For simpler integrations, API key authentication is available:

```http
GET /api/v1/certificates
X-API-Key: {your_api_key}
```

### Bearer Token Usage

Include the access token in the Authorization header:

```http
GET /api/v1/certificates
Authorization: Bearer {access_token}
```

## Base URL and Versioning

### Base URLs
- **Production**: `https://api.gccdatabase.com`
- **Staging**: `https://staging-api.gccdatabase.com`
- **Development**: `https://localhost:5001`

### API Versioning
The API version is specified in the URL path:
- Current version: `/api/v1`
- Beta version: `/api/beta`

### Version Header
Optionally specify version via header:
```http
Accept: application/vnd.gcc.v1+json
```

## Common Headers

### Request Headers
| Header | Required | Description |
|--------|----------|-------------|
| `Authorization` | Yes* | Bearer token or API key |
| `Content-Type` | Yes** | `application/json` for POST/PUT requests |
| `Accept` | No | Preferred response format |
| `X-Request-ID` | No | Unique request identifier for tracking |
| `X-API-Key` | Yes* | API key (if not using OAuth) |

*One authentication method required
**Required for requests with body

### Response Headers
| Header | Description |
|--------|-------------|
| `X-Request-ID` | Request tracking ID |
| `X-Rate-Limit-Limit` | Maximum requests per window |
| `X-Rate-Limit-Remaining` | Remaining requests in window |
| `X-Rate-Limit-Reset` | Window reset timestamp |
| `ETag` | Resource version for caching |

## Error Handling

### Error Response Format
```json
{
  "error": {
    "code": "RESOURCE_NOT_FOUND",
    "message": "Certificate not found",
    "details": {
      "id": "CERT-12345",
      "timestamp": "2024-08-08T10:30:00Z"
    },
    "request_id": "req_abc123"
  }
}
```

### HTTP Status Codes
| Status Code | Description |
|-------------|-------------|
| `200 OK` | Successful request |
| `201 Created` | Resource created successfully |
| `204 No Content` | Successful request with no response body |
| `400 Bad Request` | Invalid request parameters |
| `401 Unauthorized` | Missing or invalid authentication |
| `403 Forbidden` | Insufficient permissions |
| `404 Not Found` | Resource not found |
| `409 Conflict` | Resource conflict (e.g., duplicate) |
| `422 Unprocessable Entity` | Validation error |
| `429 Too Many Requests` | Rate limit exceeded |
| `500 Internal Server Error` | Server error |
| `503 Service Unavailable` | Service temporarily unavailable |

### Error Codes
| Code | Description |
|------|-------------|
| `INVALID_REQUEST` | Request format is invalid |
| `AUTHENTICATION_FAILED` | Authentication credentials invalid |
| `INSUFFICIENT_PERMISSIONS` | User lacks required permissions |
| `RESOURCE_NOT_FOUND` | Requested resource doesn't exist |
| `DUPLICATE_RESOURCE` | Resource already exists |
| `VALIDATION_ERROR` | Input validation failed |
| `RATE_LIMIT_EXCEEDED` | Too many requests |
| `INTERNAL_ERROR` | Internal server error |

## Rate Limiting

### Default Limits
- **Standard tier**: 1000 requests/hour
- **Premium tier**: 10000 requests/hour
- **Enterprise tier**: Unlimited

### Rate Limit Headers
```http
X-Rate-Limit-Limit: 1000
X-Rate-Limit-Remaining: 999
X-Rate-Limit-Reset: 1628856000
```

### Rate Limit Exceeded Response
```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "API rate limit exceeded",
    "retry_after": 3600
  }
}
```

## API Endpoints

### REST API v1 (NEW)

The new REST API provides comprehensive programmatic access to all system resources with standardized JSON responses.

#### Base URLs
- **Development**: `https://localhost:5001/api/certificates`
- **Production**: `https://api.gccdatabase.com/api/certificates`

### Certificates REST API

#### List Certificates with Advanced Filtering
```http
GET /api/certificates
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | integer | Page number (default: 1) |
| `pageSize` | integer | Items per page (default: 20, max: 100) |
| `sort` | string | Sort field (newest, expiring, product-az, company-az, status, type) |
| `userRole` | string | User role for data access (Public, Admin, User) |
| `filters.searchTerm` | string | Search across certificate, company, product fields |
| `filters.certificationTypes` | array | Filter by certification type IDs |
| `filters.countries` | array | Filter by countries |
| `filters.statuses` | array | Filter by certificate statuses |
| `filters.effectiveDateStart` | date | Filter certificates effective from date |
| `filters.effectiveDateEnd` | date | Filter certificates effective to date |

**Response:**
```json
{
  "certificates": [
    {
      "id": 1,
      "certificateNumber": "GCC-24-1605",
      "certificationType": "Certified Sustainability Facts",
      "companyName": "ASSA ABLOY Access & Egress Hardware Group",
      "productName": "900ET Exit Device Trim with EcoFlex",
      "productCategory": "Security Hardware",
      "effectiveDate": "2024-09-01T00:00:00",
      "expirationDate": "2027-08-31T00:00:00",
      "location": "Berlin, CT",
      "country": "United States",
      "status": "Active",
      "badgeText": "CSF",
      "tagType": "External",
      "certificateUrl": "/Certificate/Details/1",
      "detailsUrl": "/Certificate/CertificateSingleView/1",
      "isExpiringSoon": false,
      "isExpired": false
    }
  ],
  "totalCount": 1,
  "pageSize": 20,
  "currentPage": 1,
  "currentSort": "newest",
  "appliedFilters": {
    "searchTerm": "",
    "certificationTypes": [],
    "countries": [],
    "statuses": []
  },
  "success": true,
  "message": "",
  "errors": []
}
```

#### Get Certificate by ID
```http
GET /api/certificates/{id}
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `userRole` | string | User role for data access (default: "Public") |

**Response:**
```json
{
  "id": 1,
  "certificateNumber": "GCC-24-1605",
  "certificationType": "Certified Sustainability Facts",
  "csfAttributes": "Energy Efficiency, Recycled Content",
  "status": "Active",
  "effectiveDate": "2024-09-01T00:00:00",
  "expirationDate": "2027-08-31T00:00:00",
  "companyName": "ASSA ABLOY Access & Egress Hardware Group",
  "companyContactEmail": "sustainability@assaabloy.com",
  "companyContactURL": "https://www.assaabloy.com",
  "productName": "900ET Exit Device Trim with EcoFlex",
  "productCategory": "Security Hardware",
  "location": "Berlin, CT",
  "country": "United States",
  "createdAt": "2024-08-08T00:00:00",
  "lastModified": "2024-08-08T00:00:00",
  "createdBy": "System Admin",
  "lastEditedBy": "System Admin"
}
```

#### Get Certificate by Number
```http
GET /api/certificates/by-number/{certificateNumber}
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `userRole` | string | User role for data access (default: "Public") |

**Response:** Same as Get Certificate by ID

#### Create Certificate (Admin Only)
```http
POST /api/certificates
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "certificateNumber": "GCC-24-0001",
  "status": "active",
  "effectiveDate": "2024-08-01T00:00:00",
  "expirationDate": "2027-07-31T00:00:00",
  "location": "New York, NY",
  "country": "United States",
  "asin": "B08XYZ1234",
  "upc": "123456789012",
  "gtin": "01234567890123",
  "ean": "1234567890123",
  "companyId": 1,
  "productId": 1,
  "certificationTypeId": 1
}
```

**Response:** `201 Created`
```json
{
  "id": 2,
  "certificateNumber": "GCC-24-0001",
  ...
}
```

#### Update Certificate (Admin Only)
```http
PUT /api/certificates/{id}
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "status": "renewed",
  "expirationDate": "2028-07-31T00:00:00",
  "location": "Updated Location"
}
```

**Response:** `200 OK` with updated certificate data

#### Delete Certificate (Admin Only)
```http
DELETE /api/certificates/{id}
Authorization: Bearer {admin_token}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Certificate deleted successfully."
}
```

#### Upload Certificate File (Admin Only)
```http
POST /api/certificates/{id}/upload
Authorization: Bearer {admin_token}
Content-Type: multipart/form-data
```

**Form Data:**
- `file`: PDF file (max 20MB)

**Response:** `200 OK`
```json
{
  "success": true,
  "filePath": "uploads/certificates/GCC-24-0001_20240808_143022.pdf",
  "fileName": "GCC-24-0001_20240808_143022.pdf",
  "fileSize": 245760,
  "message": "File uploaded successfully."
}
```

#### Download Certificate File
```http
GET /api/certificates/{id}/download
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `userRole` | string | User role for access control (default: "Public") |

**Response:** `200 OK` with PDF file download or `404 Not Found`

### File Upload System

#### Supported File Types
| Type | Extensions | Max Size | Content Types |
|------|------------|----------|---------------|
| Certificate | .pdf | 20MB | application/pdf |
| Image | .jpg, .jpeg, .png, .gif, .bmp, .webp | 5MB | image/* |
| Document | .pdf, .doc, .docx, .txt, .rtf | 10MB | Various document types |

#### File Storage Structure
```
/wwwroot/uploads/
├── certificates/     # Certificate PDFs
│   └── {cert-number}_{timestamp}.pdf
└── images/          # Images and graphics  
    └── {identifier}_{timestamp}.{ext}
```

#### File Validation
All uploaded files are validated for:
- File type and extension matching
- MIME type verification
- File size limits
- Malicious content scanning
- Unique filename generation

### Error Responses

#### Error Format
```json
{
  "message": "Validation failed.",
  "errors": [
    "Certificate number 'GCC-24-0001' is already in use.",
    "Expiration date must be after the effective date."
  ],
  "traceId": "00-abc123..."
}
```

#### HTTP Status Codes
| Status | Description |
|--------|-------------|
| `200 OK` | Successful request |
| `201 Created` | Resource created |
| `400 Bad Request` | Validation error |
| `401 Unauthorized` | Authentication required |
| `403 Forbidden` | Admin access required |
| `404 Not Found` | Resource not found |
| `500 Internal Server Error` | Server error |

### Legacy Web UI Endpoints

#### List Certificates
```http
GET /Certificate/Certificates
GET /Certificate/SearchApi
```

**Query Parameters for /Certificate/Certificates:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | integer | Page number (default: 1) |
| `pageSize` | integer | Items per page (default: 10, max: 100) |
| `searchTerm` | string | Search certificates by number, company, product name |
| `companyId` | integer | Filter by company ID |
| `productId` | integer | Filter by product ID |

**Advanced Search API - /Certificate/SearchApi:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `searchTerm` | string | Full-text search across certificate fields |
| `companyId` | integer | Filter by specific company |
| `productId` | integer | Filter by specific product |
| `certificationTypeId` | integer | Filter by certification type |
| `status` | string | Filter by status (active, expired, pending) |
| `location` | string | Filter by location (partial match) |
| `country` | string | Filter by country (partial match) |
| `effectiveDateFrom` | date | Filter certificates effective from date |
| `effectiveDateTo` | date | Filter certificates effective to date |
| `expirationDateFrom` | date | Filter certificates expiring from date |
| `expirationDateTo` | date | Filter certificates expiring to date |
| `sortBy` | string | Sort field (CreatedAt, Number, Company, Product, etc.) |
| `sortDescending` | boolean | Sort direction (default: true) |
| `page` | integer | Page number (default: 1) |
| `pageSize` | integer | Items per page (default: 10) |

**Response for /Certificate/SearchApi:**
```json
{
  "certificates": [
    {
      "id": 1,
      "number": "24-1605",
      "status": "active",
      "effectiveDate": "2024-09-01",
      "expirationDate": "2027-08-31",
      "company": {
        "id": 1,
        "name": "ASSA ABLOY Access & Egress Hardware Group, Inc."
      },
      "product": {
        "id": 1,
        "name": "900ET Exit Device Trim with EcoFlex"
      },
      "certificationType": {
        "id": 1,
        "name": "Certified Environmental Facts"
      },
      "location": "Berlin, CT",
      "country": "United States",
      "isExpired": false,
      "isExpiringSoon": false
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalCount": 5,
    "totalPages": 1,
    "hasNextPage": false,
    "hasPreviousPage": false
  }
}
```

#### Get Certificate Details
```http
GET /Certificate/Details/{id}
GET /Certificate/CertificateSingleView/{id}
```

**Response:**
```json
{
  "data": {
    "id": "CERT-12345",
    "number": "GCC-2024-001",
    "type": "Product Certification",
    "company": {
      "id": "COMP-001",
      "name": "ASSA ABLOY",
      "address": "123 Security St",
      "country": "USA"
    },
    "product": {
      "id": "PROD-001",
      "name": "Smart Lock Pro",
      "category": "Security Hardware",
      "description": "Advanced smart lock system"
    },
    "effective_date": "2024-01-01",
    "expiration_date": "2025-01-01",
    "status": "active",
    "attributes": {
      "recycled_content": "75%",
      "energy_efficiency": "A+",
      "carbon_footprint": "Low"
    },
    "documents": [
      {
        "id": "DOC-001",
        "name": "Certificate.pdf",
        "url": "/api/v1/documents/DOC-001",
        "type": "application/pdf"
      }
    ],
    "created_at": "2024-01-01T00:00:00Z",
    "updated_at": "2024-01-01T00:00:00Z"
  }
}
```

#### Create Certificate
```http
POST /Certificate/Create
Content-Type: application/x-www-form-urlencoded

Number=24-2024-001&
CompanyId=1&
ProductId=1&
CertificationTypeId=1&
EffectiveDate=2024-08-01&
ExpirationDate=2027-07-31&
Status=active&
Location=Example City&
Country=United States&
Asin=B08XYZ1234&
Upc=123456789012&
Gtin=01234567890123&
Ean=1234567890123
```

**Response:** `201 Created`
```json
{
  "data": {
    "id": "CERT-12346",
    "number": "GCC-2024-002",
    ...
  }
}
```

#### Update Certificate
```http
PUT /api/v1/certificates/{id}
Content-Type: application/json

{
  "expiration_date": "2026-02-01",
  "status": "renewed"
}
```

**Response:** `200 OK`

#### Delete Certificate
```http
DELETE /api/v1/certificates/{id}
```

**Response:** `204 No Content`

### Companies

#### List Companies
```http
GET /api/v1/companies
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | integer | Page number |
| `per_page` | integer | Items per page |
| `search` | string | Search by name |
| `filter[country]` | string | Filter by country |
| `filter[status]` | string | Filter by status |

#### Get Company
```http
GET /api/v1/companies/{id}
```

#### Create Company
```http
POST /api/v1/companies
Content-Type: application/json

{
  "name": "New Company Inc",
  "address": "456 Business Ave",
  "country": "USA",
  "contact_email": "info@newcompany.com",
  "contact_phone": "+1-555-0100"
}
```

#### Update Company
```http
PUT /api/v1/companies/{id}
```

#### Delete Company
```http
DELETE /api/v1/companies/{id}
```

### Products

#### List Products
```http
GET /api/v1/products
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | integer | Page number |
| `per_page` | integer | Items per page |
| `filter[category]` | string | Filter by category |
| `filter[company_id]` | string | Filter by company |
| `search` | string | Search products |

#### Get Product
```http
GET /api/v1/products/{id}
```

#### Create Product
```http
POST /api/v1/products
Content-Type: application/json

{
  "name": "Eco-Friendly Widget",
  "category": "Widgets",
  "company_id": "COMP-001",
  "description": "Sustainable widget made from recycled materials",
  "attributes": {
    "material": "Recycled plastic",
    "weight": "100g"
  }
}
```

#### Update Product
```http
PUT /api/v1/products/{id}
```

#### Delete Product
```http
DELETE /api/v1/products/{id}
```

### Users

#### Get Current User
```http
GET /api/v1/users/me
```

**Response:**
```json
{
  "data": {
    "id": "USER-001",
    "email": "user@example.com",
    "name": "John Doe",
    "role": "analyst",
    "permissions": [
      "certificates.read",
      "certificates.write",
      "companies.read"
    ],
    "created_at": "2024-01-01T00:00:00Z"
  }
}
```

#### Update User Profile
```http
PUT /api/v1/users/me
Content-Type: application/json

{
  "name": "Jane Doe",
  "notification_preferences": {
    "email": true,
    "sms": false
  }
}
```

### Dashboard & Analytics

#### Get Certificate Statistics
```http
GET /Certificate/Statistics
```

**Response:**
```json
{
  "Total": 5,
  "Active": 5,
  "Expired": 0,
  "ExpiringSoon": 0,
  "Status_active": 5
}
```

#### Get Dashboard Data
```http
GET /Dashboard/Index
```

**Authentication Required:** Yes (Admin or Analyst roles)

**Response:** HTML view with dashboard data including:
- Certificate statistics and counts
- Recent certificates list
- Expiring certificates alerts
- Company and product totals

#### Get Chart Data for Dashboard
```http
GET /Dashboard/GetChartData?chartType={type}
```

**Parameters:**
| Parameter | Type | Options | Description |
|-----------|------|---------|-------------|
| `chartType` | string | monthly, status, company, expiration | Type of chart data to retrieve |

**Authentication Required:** Yes (Admin or Analyst roles only)

**Response for chartType=monthly:**
```json
{
  "labels": ["2023-09", "2023-10", "2023-11", "2024-01"],
  "datasets": [{
    "label": "Certificates Created",
    "data": [2, 1, 1, 1],
    "backgroundColor": "rgba(54, 162, 235, 0.6)",
    "borderColor": "rgba(54, 162, 235, 1)",
    "borderWidth": 1
  }]
}
```

**Response for chartType=status:**
```json
{
  "labels": ["Active", "Expired", "Expiring Soon"],
  "datasets": [{
    "data": [5, 0, 0],
    "backgroundColor": [
      "rgba(75, 192, 192, 0.6)",
      "rgba(255, 99, 132, 0.6)", 
      "rgba(255, 205, 86, 0.6)"
    ]
  }]
}
```

### Admin

**Note:** All admin endpoints require `AdminOnly` policy authorization.

#### Admin Dashboard
```http
GET /Admin
```
Returns the admin dashboard view with real-time statistics.

#### Get System Statistics (API)
```http
GET /api/v1/admin/stats
```
**Deprecated:** Use `/Certificate/Statistics` instead

#### User Management

##### List Users
```http
GET /Admin/GetUsers
```
**Response:**
```json
{
  "success": true,
  "data": [{
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com",
    "role": "admin",
    "status": "active",
    "companyNames": ["ASSA ABLOY"],
    "lastLogin": "2024-01-15T10:30:00Z"
  }]
}
```

##### Create User
```http
POST /Admin/CreateUser
Content-Type: application/json

{
  "name": "Jane Smith",
  "email": "jane@example.com",
  "password": "SecurePassword123!",
  "role": "user",
  "companyIds": [1, 2],
  "requirePasswordReset": true
}
```

##### Update User
```http
PUT /Admin/UpdateUser/{id}
Content-Type: application/json

{
  "name": "Jane Smith Updated",
  "email": "jane@example.com",
  "role": "admin",
  "status": "active",
  "companyIds": [1, 2, 3]
}
```

##### Delete User
```http
DELETE /Admin/DeleteUser/{id}
```

#### Role Management

##### List Roles
```http
GET /Admin/GetRoles
```
**Response:**
```json
{
  "success": true,
  "data": [{
    "id": 1,
    "name": "Admin",
    "description": "Full system access",
    "permissions": ["view-certificates", "edit-certificates", "admin-access"],
    "userCount": 5
  }]
}
```

##### Get Permissions
```http
GET /Admin/GetPermissions
```
Returns all available system permissions.

#### Product Types
```http
GET /Admin/GetProductTypes
```
Returns hierarchical product type structure.

#### Certificate Types
```http
GET /Admin/GetCertificateTypes
```
Returns all certificate types with counts.

#### Certificate Fields
```http
GET /Admin/GetCertificateFields
```
Returns configurable certificate fields.

#### Tags
```http
GET /Admin/GetTags
```
Returns all tags (internal/external).

#### Partners
```http
GET /Admin/GetPartners
```
Returns partner companies.

#### Export Data
```http
GET /Admin/ExportData?entityType={type}&format=csv
```
**Entity Types:** users, roles, products, certificates, companies, tags
**Formats:** csv (xlsx coming soon)

#### Google Analytics Settings
```http
POST /Admin/SaveGoogleAnalytics
Content-Type: application/json

{
  "trackingId": "G-XXXXXXXXXX",
  "isEnabled": true,
  "trackPageViews": true,
  "trackEvents": true
}
```

#### Amazon API Settings
```http
POST /Admin/SaveAmazonAPISettings
Content-Type: application/json

{
  "accessKeyId": "AKIA...",
  "secretAccessKey": "...",
  "region": "us-east-1",
  "marketplaceId": "ATVPDKIKX0DER",
  "isEnabled": true,
  "syncIntervalHours": 24
}
```

## Response Formats

### Pagination
All list endpoints support pagination:
```json
{
  "data": [...],
  "meta": {
    "page": 1,
    "per_page": 20,
    "total": 100,
    "total_pages": 5
  },
  "links": {
    "self": "...",
    "first": "...",
    "prev": null,
    "next": "...",
    "last": "..."
  }
}
```

### Filtering
Use square brackets for nested filters:
```
GET /api/v1/certificates?filter[company]=ASSA&filter[status]=active
```

### Sorting
Use minus sign for descending order:
```
GET /api/v1/certificates?sort=-created_at,name
```

### Field Selection
Specify fields to include:
```
GET /api/v1/certificates?fields=id,number,company
```

### Including Related Resources
Include related resources in response:
```
GET /api/v1/certificates?include=company,product,documents
```

## Webhooks

### Webhook Events
Subscribe to real-time events:

| Event | Description |
|-------|-------------|
| `certificate.created` | New certificate created |
| `certificate.updated` | Certificate updated |
| `certificate.expired` | Certificate expired |
| `certificate.renewed` | Certificate renewed |
| `company.created` | New company added |
| `product.created` | New product added |

### Webhook Registration
```http
POST /api/v1/webhooks
Content-Type: application/json

{
  "url": "https://your-app.com/webhooks",
  "events": ["certificate.created", "certificate.updated"],
  "secret": "your-webhook-secret"
}
```

### Webhook Payload
```json
{
  "event": "certificate.created",
  "timestamp": "2024-08-08T10:00:00Z",
  "data": {
    "id": "CERT-12345",
    "number": "GCC-2024-001",
    ...
  }
}
```

### Webhook Security
Verify webhook signatures:
```javascript
const crypto = require('crypto');

function verifyWebhook(payload, signature, secret) {
  const hash = crypto
    .createHmac('sha256', secret)
    .update(payload)
    .digest('hex');
  
  return hash === signature;
}
```

## SDKs and Code Examples

### JavaScript/Node.js
```javascript
const GCCClient = require('@gcc/api-client');

const client = new GCCClient({
  apiKey: 'your-api-key',
  baseUrl: 'https://api.gccdatabase.com'
});

// List certificates
const certificates = await client.certificates.list({
  page: 1,
  perPage: 20,
  filter: { status: 'active' }
});

// Get specific certificate
const certificate = await client.certificates.get('CERT-12345');

// Create certificate
const newCert = await client.certificates.create({
  number: 'GCC-2024-003',
  type: 'Product Certification',
  companyId: 'COMP-001',
  productId: 'PROD-001'
});
```

### Python
```python
from gcc_api import Client

client = Client(api_key='your-api-key')

# List certificates
certificates = client.certificates.list(
    page=1,
    per_page=20,
    filter={'status': 'active'}
)

# Get specific certificate
certificate = client.certificates.get('CERT-12345')

# Create certificate
new_cert = client.certificates.create(
    number='GCC-2024-003',
    type='Product Certification',
    company_id='COMP-001',
    product_id='PROD-001'
)
```

### cURL
```bash
# List certificates
curl -X GET "https://api.gccdatabase.com/api/v1/certificates" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get specific certificate
curl -X GET "https://api.gccdatabase.com/api/v1/certificates/CERT-12345" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Create certificate
curl -X POST "https://api.gccdatabase.com/api/v1/certificates" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "number": "GCC-2024-003",
    "type": "Product Certification",
    "company_id": "COMP-001",
    "product_id": "PROD-001"
  }'
```

### C#/.NET
```csharp
using GCC.ApiClient;

var client = new GCCClient("your-api-key");

// List certificates
var certificates = await client.Certificates.ListAsync(new ListOptions
{
    Page = 1,
    PerPage = 20,
    Filter = new { Status = "active" }
});

// Get specific certificate
var certificate = await client.Certificates.GetAsync("CERT-12345");

// Create certificate
var newCert = await client.Certificates.CreateAsync(new Certificate
{
    Number = "GCC-2024-003",
    Type = "Product Certification",
    CompanyId = "COMP-001",
    ProductId = "PROD-001"
});
```

## API Changelog

### Version 1.1.0 (Current) - August 2025
- **UI/UX Updates**: Complete redesign of all Account authentication pages
  - Modern, responsive design with GreenCircle branding
  - Enhanced user experience across login, registration, password reset flows
  - Interactive elements with improved accessibility
  - Mobile-first responsive design
- **Enhanced Authentication Pages**:
  - Two-factor authentication with interactive code input
  - Account lockout page with countdown timer
  - Password reset with strength validation
  - Email verification with clear instructions
  - Welcome page with feature highlights
- **Design System**: Consistent color scheme and typography
  - Primary colors: GreenCircle Green (#92C83E) and Corporate Blue (#002B76)
  - Modern gradients and shadow effects
  - Professional Segoe UI typography

### Version 1.0.0 
- Initial API release
- Certificate management endpoints
- Company and product endpoints
- User authentication and management
- Webhook support

### Upcoming Features (v1.2.0)
- Batch operations support
- GraphQL endpoint
- Advanced search capabilities
- Real-time notifications via WebSocket
- Audit log access

## Support

For API support:
- Email: api-support@gccdatabase.com
- Documentation: https://docs.gccdatabase.com
- Status Page: https://status.gccdatabase.com
- GitHub Issues: https://github.com/gcc-database/api-issues