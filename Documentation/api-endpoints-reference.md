# GreenCircle Certified API Endpoints Reference

## Base URL

```
Production: https://api.greencircle.com/v1
Staging: https://staging-api.greencircle.com/v1
Development: http://localhost:5000
```

## Table of Contents

1. [Authentication Endpoints](#authentication-endpoints)
2. [Certificate Endpoints](#certificate-endpoints)
3. [Company Endpoints](#company-endpoints)
4. [Product Endpoints](#product-endpoints)
5. [User Management Endpoints](#user-management-endpoints)
6. [Role Management Endpoints](#role-management-endpoints)
7. [Admin Configuration Endpoints](#admin-configuration-endpoints)
8. [Search and Filter Parameters](#search-and-filter-parameters)
9. [Response Formats](#response-formats)

---

## Authentication Endpoints

### Login
```http
POST /account/login
```

Authenticate a user and create a session.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "securepassword",
  "rememberMe": false
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh-token",
  "expiresIn": 3600,
  "user": {
    "id": 123,
    "name": "John Doe",
    "email": "user@example.com",
    "role": "user"
  }
}
```

**Error Response:** `401 Unauthorized`
```json
{
  "success": false,
  "message": "Invalid email or password",
  "errorCode": "INVALID_CREDENTIALS"
}
```

---

### Logout
```http
POST /account/logout
```

Terminate the current user session.

**Headers:**
```
Authorization: Bearer {token}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Successfully logged out"
}
```

---

### Get Current User
```http
GET /account/current
```

Retrieve information about the currently authenticated user.

**Headers:**
```
Authorization: Bearer {token}
```

**Response:** `200 OK`
```json
{
  "id": 123,
  "name": "John Doe",
  "email": "user@example.com",
  "role": "admin",
  "permissions": [
    "view-certificates",
    "edit-certificates",
    "manage-users"
  ],
  "companies": [
    {
      "id": 1,
      "name": "ASSA ABLOY"
    }
  ]
}
```

---

### Refresh Token
```http
POST /auth/refresh
```

Refresh an expired access token.

**Request Body:**
```json
{
  "refreshToken": "refresh-token-value"
}
```

**Response:** `200 OK`
```json
{
  "token": "new-jwt-token",
  "expiresIn": 3600
}
```

---

## Certificate Endpoints

### List Certificates
```http
GET /certificates
```

Retrieve a paginated list of certificates.

**Query Parameters:**
| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `page` | integer | Page number (1-based) | 1 |
| `pageSize` | integer | Items per page (max 100) | 20 |
| `search` | string | Search in certificate number, company, product | - |
| `status` | string | Filter by status: active, expired, pending, draft | - |
| `companyId` | integer | Filter by company ID | - |
| `productType` | string | Filter by product type | - |
| `sortBy` | string | Sort field: number, company, product, issueDate, expirationDate | issueDate |
| `sortOrder` | string | Sort order: asc, desc | desc |

**Example Request:**
```
GET /certificates?page=1&pageSize=10&status=active&search=lumber
```

**Response:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "id": "cert-123",
      "certificateNumber": "24-1605",
      "companyName": "ASSA ABLOY",
      "productName": "Door Hardware Series 500",
      "certificateType": "Recycled Content",
      "issueDate": "2024-01-15",
      "expirationDate": "2025-01-15",
      "status": "active"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalItems": 156,
    "totalPages": 16,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

---

### Get Certificate
```http
GET /certificates/{certificateId}
```

Retrieve detailed information about a specific certificate.

**Path Parameters:**
- `certificateId` (string): Certificate ID or number

**Response:** `200 OK`
```json
{
  "id": "cert-123",
  "certificateNumber": "24-1605",
  "company": {
    "id": 1,
    "name": "ASSA ABLOY"
  },
  "productName": "Door Hardware Series 500",
  "productDescription": "Sustainable door hardware made from recycled materials",
  "certificateType": "Recycled Content",
  "issueDate": "2024-01-15",
  "expirationDate": "2025-01-15",
  "status": "active",
  "attributes": {
    "recycledContent": "45%",
    "postConsumerContent": "30%",
    "preConsumerContent": "15%",
    "certificationStandard": "ISO 14021"
  },
  "documents": [
    {
      "id": "doc-456",
      "fileName": "certificate-24-1605.pdf",
      "fileType": "application/pdf",
      "fileSize": 2048576,
      "uploadedAt": "2024-01-15T10:00:00Z"
    }
  ],
  "createdAt": "2024-01-15T09:00:00Z",
  "updatedAt": "2024-01-20T14:30:00Z"
}
```

---

### Create Certificate
```http
POST /certificates
```

Create a new certificate.

**Request Body:**
```json
{
  "certificateNumber": "24-5678",
  "companyId": 1,
  "productName": "Sustainable Flooring",
  "productDescription": "Eco-friendly bamboo flooring",
  "certificateType": "Rapidly Renewable",
  "issueDate": "2024-02-01",
  "expirationDate": "2025-02-01",
  "attributes": {
    "renewableContent": "100%",
    "harvestCycle": "5 years",
    "certificationStandard": "FSC"
  }
}
```

**Response:** `201 Created`
```json
{
  "success": true,
  "message": "Certificate created successfully",
  "data": {
    "id": "cert-789",
    "certificateNumber": "24-5678",
    // ... full certificate object
  }
}
```

---

### Update Certificate
```http
PUT /certificates/{certificateId}
```

Update an existing certificate.

**Path Parameters:**
- `certificateId` (string): Certificate ID

**Request Body:**
```json
{
  "productName": "Updated Product Name",
  "productDescription": "Updated description",
  "expirationDate": "2025-06-01",
  "status": "active",
  "attributes": {
    "recycledContent": "50%"
  }
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Certificate updated successfully",
  "data": {
    // ... updated certificate object
  }
}
```

---

### Delete Certificate
```http
DELETE /certificates/{certificateId}
```

Delete a certificate.

**Path Parameters:**
- `certificateId` (string): Certificate ID

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Certificate deleted successfully"
}
```

---

### Import Certificates
```http
POST /certificates/import
```

Bulk import certificates from a CSV or Excel file.

**Request Body:** `multipart/form-data`
- `file`: CSV or Excel file

**Example CSV Format:**
```csv
Certificate Number,Company Name,Product Name,Type,Issue Date,Expiration Date
24-1234,ASSA ABLOY,Door Hardware,Recycled Content,2024-01-01,2025-01-01
24-1235,GAF,Roofing Material,Energy Star,2024-02-01,2025-02-01
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Import completed",
  "imported": 45,
  "failed": 2,
  "errors": [
    {
      "row": 15,
      "errors": ["Invalid date format", "Company not found"]
    },
    {
      "row": 23,
      "errors": ["Duplicate certificate number"]
    }
  ]
}
```

---

## Company Endpoints

### List Companies
```http
GET /companies
```

Retrieve a list of all companies.

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `search` | string | Search in company name or email |
| `sortBy` | string | Sort field: name, email, certificateCount |
| `sortOrder` | string | Sort order: asc, desc |

**Response:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "ASSA ABLOY",
      "email": "info@assaabloy.com",
      "url": "https://www.assaabloy.com",
      "certificateCount": 15
    },
    {
      "id": 2,
      "name": "GAF Materials",
      "email": "contact@gaf.com",
      "url": "https://www.gaf.com",
      "certificateCount": 8
    }
  ]
}
```

---

### Get Company
```http
GET /companies/{companyId}
```

Retrieve detailed information about a specific company.

**Path Parameters:**
- `companyId` (integer): Company ID

**Response:** `200 OK`
```json
{
  "id": 1,
  "name": "ASSA ABLOY",
  "email": "info@assaabloy.com",
  "url": "https://www.assaabloy.com",
  "address": "Box 70340, 107 23 Stockholm, Sweden",
  "phone": "+46 8 506 485 00",
  "certificateCount": 15,
  "createdAt": "2023-01-15T10:00:00Z",
  "certificates": [
    {
      "id": "cert-123",
      "certificateNumber": "24-1605",
      "productName": "Door Hardware Series 500",
      "status": "active"
    }
  ]
}
```

---

### Create Company
```http
POST /companies
```

Create a new company.

**Request Body:**
```json
{
  "name": "New Company Inc.",
  "email": "info@newcompany.com",
  "url": "https://www.newcompany.com",
  "address": "123 Main St, City, State 12345",
  "phone": "+1-555-123-4567"
}
```

**Response:** `201 Created`
```json
{
  "success": true,
  "message": "Company created successfully",
  "data": {
    "id": 10,
    "name": "New Company Inc.",
    // ... full company object
  }
}
```

---

### Update Company
```http
PUT /companies/{companyId}
```

Update an existing company.

**Path Parameters:**
- `companyId` (integer): Company ID

**Request Body:**
```json
{
  "name": "Updated Company Name",
  "email": "newemail@company.com",
  "phone": "+1-555-987-6543"
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Company updated successfully",
  "data": {
    // ... updated company object
  }
}
```

---

### Delete Company
```http
DELETE /companies/{companyId}
```

Delete a company.

**Path Parameters:**
- `companyId` (integer): Company ID

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Company deleted successfully"
}
```

---

## Product Endpoints

### List Product Categories
```http
GET /products/categories
```

Retrieve hierarchical list of product categories.

**Response:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Building Materials",
      "level": 1,
      "parentId": null,
      "productCount": 45,
      "status": "Active",
      "children": [
        {
          "id": 2,
          "name": "Lumber",
          "level": 2,
          "parentId": 1,
          "productCount": 25,
          "status": "Active",
          "children": [
            {
              "id": 3,
              "name": "Hardwood",
              "level": 3,
              "parentId": 2,
              "productCount": 15,
              "status": "Active",
              "children": []
            }
          ]
        }
      ]
    }
  ]
}
```

---

### Create Product Category
```http
POST /products/categories
```

Create a new product category.

**Request Body:**
```json
{
  "name": "New Category",
  "parentId": 1,
  "level": 2,
  "sortOrder": 10
}
```

**Response:** `201 Created`
```json
{
  "success": true,
  "message": "Category created successfully",
  "data": {
    "id": 20,
    "name": "New Category",
    "parentId": 1,
    "level": 2,
    "sortOrder": 10
  }
}
```

---

### Update Product Category
```http
PUT /products/categories/{categoryId}
```

Update an existing product category.

**Path Parameters:**
- `categoryId` (integer): Category ID

**Request Body:**
```json
{
  "name": "Updated Category Name",
  "sortOrder": 5
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Category updated successfully",
  "data": {
    // ... updated category object
  }
}
```

---

### Delete Product Category
```http
DELETE /products/categories/{categoryId}
```

Delete a product category.

**Path Parameters:**
- `categoryId` (integer): Category ID

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Category deleted successfully"
}
```

---

## User Management Endpoints

### List Users (Admin Only)
```http
GET /admin/users
```

Retrieve a list of all users.

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `search` | string | Search in name or email |
| `role` | string | Filter by role |
| `status` | string | Filter by status: active, inactive, pending |
| `companyId` | integer | Filter by company ID |

**Response:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Sarah Johnson",
      "email": "sarah.johnson@assaabloy.com",
      "role": "admin",
      "status": "active",
      "companyNames": ["ASSA ABLOY"],
      "partnerNames": ["Amazon"],
      "lastLogin": "2024-02-20T10:30:00Z"
    }
  ]
}
```

---

### Get User
```http
GET /admin/users/{userId}
```

Retrieve detailed information about a specific user.

**Path Parameters:**
- `userId` (integer): User ID

**Response:** `200 OK`
```json
{
  "id": 1,
  "name": "Sarah Johnson",
  "email": "sarah.johnson@assaabloy.com",
  "role": "admin",
  "status": "active",
  "companyNames": ["ASSA ABLOY"],
  "partnerNames": ["Amazon"],
  "permissions": [
    "view-certificates",
    "edit-certificates",
    "delete-certificates",
    "manage-users"
  ],
  "lastLogin": "2024-02-20T10:30:00Z",
  "createdAt": "2023-01-15T09:00:00Z",
  "updatedAt": "2024-02-20T10:30:00Z"
}
```

---

### Create User
```http
POST /admin/users
```

Create a new user.

**Request Body:**
```json
{
  "name": "John Smith",
  "email": "john.smith@company.com",
  "password": "SecurePassword123!",
  "role": "user",
  "companyIds": [1, 2],
  "partnerIds": [1],
  "requirePasswordReset": true
}
```

**Response:** `201 Created`
```json
{
  "success": true,
  "message": "User created successfully",
  "data": {
    "id": 10,
    "name": "John Smith",
    "email": "john.smith@company.com",
    "role": "user",
    "status": "pending"
  }
}
```

---

### Update User
```http
PUT /admin/users/{userId}
```

Update an existing user.

**Path Parameters:**
- `userId` (integer): User ID

**Request Body:**
```json
{
  "name": "John Smith Jr.",
  "role": "admin",
  "status": "active",
  "companyIds": [1, 2, 3]
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "User updated successfully",
  "data": {
    // ... updated user object
  }
}
```

---

### Delete User
```http
DELETE /admin/users/{userId}
```

Delete a user.

**Path Parameters:**
- `userId` (integer): User ID

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "User deleted successfully"
}
```

---

### Reset User Password
```http
POST /admin/users/{userId}/reset-password
```

Reset a user's password.

**Path Parameters:**
- `userId` (integer): User ID

**Request Body:**
```json
{
  "newPassword": "NewSecurePassword123!",
  "requirePasswordReset": true
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Password reset successfully"
}
```

---

## Role Management Endpoints

### List Roles
```http
GET /admin/roles
```

Retrieve a list of all roles.

**Response:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Admin",
      "description": "Full system access",
      "permissions": [
        "view-certificates",
        "edit-certificates",
        "delete-certificates",
        "manage-users",
        "admin-access"
      ],
      "userCount": 2
    },
    {
      "id": 2,
      "name": "User",
      "description": "Standard user access",
      "permissions": [
        "view-certificates",
        "edit-certificates"
      ],
      "userCount": 15
    }
  ]
}
```

---

### Create Role
```http
POST /admin/roles
```

Create a new role.

**Request Body:**
```json
{
  "name": "Manager",
  "description": "Manager level access",
  "permissions": [
    "view-certificates",
    "edit-certificates",
    "view-users"
  ]
}
```

**Response:** `201 Created`
```json
{
  "success": true,
  "message": "Role created successfully",
  "data": {
    "id": 3,
    "name": "Manager",
    "description": "Manager level access",
    "permissions": [
      "view-certificates",
      "edit-certificates",
      "view-users"
    ],
    "userCount": 0
  }
}
```

---

### Update Role
```http
PUT /admin/roles/{roleId}
```

Update an existing role.

**Path Parameters:**
- `roleId` (integer): Role ID

**Request Body:**
```json
{
  "description": "Updated manager role",
  "permissions": [
    "view-certificates",
    "edit-certificates",
    "view-users",
    "edit-users"
  ]
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Role updated successfully",
  "data": {
    // ... updated role object
  }
}
```

---

### Delete Role
```http
DELETE /admin/roles/{roleId}
```

Delete a role.

**Path Parameters:**
- `roleId` (integer): Role ID

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Role deleted successfully"
}
```

---

### List Permissions
```http
GET /admin/permissions
```

Retrieve a list of all available permissions.

**Response:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "name": "view-certificates",
      "displayName": "View Certificates",
      "category": "Certificates",
      "description": "Allow viewing certificate details"
    },
    {
      "name": "edit-certificates",
      "displayName": "Edit Certificates",
      "category": "Certificates",
      "description": "Allow editing certificate information"
    },
    {
      "name": "delete-certificates",
      "displayName": "Delete Certificates",
      "category": "Certificates",
      "description": "Allow deleting certificates"
    },
    {
      "name": "manage-users",
      "displayName": "Manage Users",
      "category": "Administration",
      "description": "Allow full user management"
    }
  ]
}
```

---

## Admin Configuration Endpoints

### Get Google Analytics Settings
```http
GET /admin/settings/google-analytics
```

Retrieve current Google Analytics configuration.

**Response:** `200 OK`
```json
{
  "trackingId": "UA-123456789-1",
  "isEnabled": true,
  "trackPageViews": true,
  "trackEvents": true,
  "trackConversions": false,
  "lastUpdated": "2024-02-15T10:00:00Z",
  "updatedBy": "admin@greencircle.com"
}
```

---

### Update Google Analytics Settings
```http
PUT /admin/settings/google-analytics
```

Update Google Analytics configuration.

**Request Body:**
```json
{
  "trackingId": "G-ABC123XYZ",
  "isEnabled": true,
  "trackPageViews": true,
  "trackEvents": true,
  "trackConversions": true
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Settings updated successfully",
  "data": {
    "trackingId": "G-ABC123XYZ",
    "isEnabled": true,
    "trackPageViews": true,
    "trackEvents": true,
    "trackConversions": true,
    "lastUpdated": "2024-02-20T14:30:00Z",
    "updatedBy": "admin@greencircle.com"
  }
}
```

---

### List Certificate Types
```http
GET /admin/settings/certificate-types
```

Retrieve all certificate types.

**Response:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Recycled Content",
      "description": "Products containing recycled materials",
      "isActive": true,
      "certificateCount": 45
    },
    {
      "id": 2,
      "name": "Rapidly Renewable",
      "description": "Products from rapidly renewable resources",
      "isActive": true,
      "certificateCount": 23
    }
  ]
}
```

---

### Create Certificate Type
```http
POST /admin/settings/certificate-types
```

Create a new certificate type.

**Request Body:**
```json
{
  "name": "Carbon Neutral",
  "description": "Products with net-zero carbon emissions",
  "isActive": true
}
```

**Response:** `201 Created`
```json
{
  "success": true,
  "message": "Certificate type created successfully",
  "data": {
    "id": 10,
    "name": "Carbon Neutral",
    "description": "Products with net-zero carbon emissions",
    "isActive": true,
    "certificateCount": 0
  }
}
```

---

## Search and Filter Parameters

### Common Query Parameters

Most list endpoints support these common parameters:

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `search` | string | Free-text search | `search=lumber` |
| `page` | integer | Page number (1-based) | `page=2` |
| `pageSize` | integer | Items per page | `pageSize=50` |
| `sortBy` | string | Field to sort by | `sortBy=name` |
| `sortOrder` | string | Sort direction (asc/desc) | `sortOrder=asc` |
| `fields` | string | Comma-separated fields to include | `fields=id,name,email` |
| `exclude` | string | Comma-separated fields to exclude | `exclude=createdAt,updatedAt` |

### Advanced Filtering

Use dot notation for nested field filtering:

```
GET /certificates?company.name=ASSA%20ABLOY&attributes.recycledContent=45%
```

### Date Range Filtering

```
GET /certificates?issueDateFrom=2024-01-01&issueDateTo=2024-12-31
```

### Multiple Value Filtering

Use comma-separated values:

```
GET /certificates?status=active,pending&companyId=1,2,3
```

---

## Response Formats

### Success Response

All successful responses follow this format:

```json
{
  "success": true,
  "message": "Optional success message",
  "data": {
    // Response data
  },
  "meta": {
    // Optional metadata
  }
}
```

### Error Response

All error responses follow this format:

```json
{
  "success": false,
  "message": "Error description",
  "errorCode": "ERROR_CODE",
  "errors": [
    {
      "field": "email",
      "message": "Invalid email format"
    }
  ],
  "timestamp": "2024-02-20T10:30:00Z"
}
```

### Pagination Response

Paginated endpoints include pagination metadata:

```json
{
  "success": true,
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 156,
    "totalPages": 8,
    "hasNext": true,
    "hasPrevious": false,
    "links": {
      "first": "/api/certificates?page=1&pageSize=20",
      "last": "/api/certificates?page=8&pageSize=20",
      "next": "/api/certificates?page=2&pageSize=20",
      "previous": null
    }
  }
}
```

### Batch Operation Response

```json
{
  "success": true,
  "message": "Batch operation completed",
  "results": [
    {
      "id": "item-1",
      "success": true,
      "message": "Processed successfully"
    },
    {
      "id": "item-2",
      "success": false,
      "message": "Validation failed",
      "errors": ["Invalid data"]
    }
  ],
  "summary": {
    "total": 10,
    "successful": 8,
    "failed": 2
  }
}
```

---

## HTTP Status Codes

| Code | Description | Use Case |
|------|-------------|----------|
| `200` | OK | Successful GET, PUT |
| `201` | Created | Successful POST creating new resource |
| `204` | No Content | Successful DELETE |
| `400` | Bad Request | Invalid request data |
| `401` | Unauthorized | Missing or invalid authentication |
| `403` | Forbidden | Authenticated but lacking permissions |
| `404` | Not Found | Resource not found |
| `409` | Conflict | Resource conflict (e.g., duplicate) |
| `422` | Unprocessable Entity | Validation errors |
| `429` | Too Many Requests | Rate limit exceeded |
| `500` | Internal Server Error | Server error |
| `503` | Service Unavailable | Maintenance or overload |

---

## Rate Limiting

All endpoints are subject to rate limiting:

| Tier | Requests/Hour | Burst |
|------|--------------|-------|
| Free | 1,000 | 100/min |
| Basic | 5,000 | 500/min |
| Pro | 25,000 | 2,500/min |
| Enterprise | Unlimited | Custom |

Rate limit information is included in response headers:

```
X-RateLimit-Limit: 5000
X-RateLimit-Remaining: 4995
X-RateLimit-Reset: 1709226000
```

---

## Versioning

The API uses URL versioning:

- Current version: `v1`
- Base URL: `https://api.greencircle.com/v1`

Version changes will be announced at least 90 days in advance. Previous versions will be supported for at least 12 months after deprecation.

---

## Support

For API support and questions:

- **Documentation**: https://docs.greencircle.com/api
- **Status Page**: https://status.greencircle.com
- **Support Email**: api-support@greencircle.com
- **Developer Forum**: https://developers.greencircle.com/forum