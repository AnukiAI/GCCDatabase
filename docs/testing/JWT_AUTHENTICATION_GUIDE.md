# JWT Authentication API Guide

**Last Updated:** 2025-12-07
**API Version:** v1.0
**Base URL:** `http://localhost:5101` (Development) | `https://gccwebapp.com` (Production)

---

## Overview

The GCC Web App provides JWT (JSON Web Token) authentication for API access. This guide documents the authentication endpoints and their proper usage.

---

## Authentication Endpoints

### 1. Login - Get JWT Token

**Endpoint:** `POST /api/v1/auth/login`

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "username": "admin",
  "password": "TestPass123!"
}
```

**Important Notes:**
- Both `username` and `password` fields are **required**
- Field names are case-sensitive (use lowercase)
- `username` can be either the username OR email address
- Request must be JSON (not form-urlencoded)

**Successful Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": {
      "id": 1,
      "username": "admin",
      "email": "admin@greencirclecertified.com",
      "role": "Admin"
    }
  },
  "message": "Login successful"
}
```

**Error Responses:**

| Code | Reason | Response |
|------|--------|----------|
| 400 | Missing fields | `{"errors": [{"field": "username", "message": "The username field is required"}]}` |
| 401 | Invalid credentials | `{"message": "Invalid username or password"}` |
| 401 | Account deactivated | `{"message": "Account is deactivated"}` |

---

### 2. Refresh Token

**Endpoint:** `POST /api/v1/auth/refresh`

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abc123..."
}
```

**Successful Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "def456...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": {
      "id": 1,
      "username": "admin",
      "email": "admin@greencirclecertified.com",
      "role": "Admin"
    }
  },
  "message": "Token refreshed successfully"
}
```

---

### 3. Validate Token

**Endpoint:** `POST /api/v1/auth/validate`

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Request Body:** None (empty)

**Successful Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "username": "admin",
    "email": "admin@greencirclecertified.com",
    "role": "Admin"
  },
  "message": "Token is valid"
}
```

---

## Using the JWT Token

After obtaining a token via the login endpoint, include it in the `Authorization` header for authenticated requests:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Example: Get Certificates with JWT

```bash
curl -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  "http://localhost:5101/api/v1/certificates?pageNumber=1&pageSize=10"
```

---

## curl Examples

### Login Request (PowerShell)
```powershell
$body = @{
    username = "admin"
    password = "TestPass123!"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5101/api/v1/auth/login" `
  -Method POST `
  -ContentType "application/json" `
  -Body $body
```

### Login Request (curl)
```bash
curl -X POST "http://localhost:5101/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"TestPass123!"}'
```

### Login Request (Windows cmd)
```cmd
curl -X POST "http://localhost:5101/api/v1/auth/login" ^
  -H "Content-Type: application/json" ^
  -d "{\"username\":\"admin\",\"password\":\"TestPass123!\"}"
```

---

## Test Credentials

### Development (localhost:5101)
| Username | Password | Role |
|----------|----------|------|
| admin | TestPass123! | Admin |
| analyst | TestPass123! | Analyst |
| external | TestPass123! | External |

### Production (gccwebapp.com)
| Username | Password | Role |
|----------|----------|------|
| admin@greencirclecertified.com | Admin123! | Admin |
| analyst@greencirclecertified.com | Analyst123! | Analyst |
| partner@amazon.com | Partner123! | External |

---

## Token Configuration

JWT tokens are configured in `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "[64+ character secret key]",
    "Issuer": "GCCWebApp",
    "Audience": "GCCWebAppUsers",
    "ExpiryMinutes": 60
  }
}
```

- **Default Expiry:** 60 minutes (3600 seconds)
- **Algorithm:** HS256
- **Token Type:** Bearer

---

## Common Issues

### 1. "request field is required" (400 Bad Request)
**Cause:** Missing username or password in request body
**Fix:** Ensure both fields are present:
```json
{
  "username": "admin",
  "password": "TestPass123!"
}
```

### 2. "Invalid username or password" (401 Unauthorized)
**Cause:** Wrong credentials or user doesn't exist
**Fix:** Verify credentials against test accounts above

### 3. Content-Type Error
**Cause:** Not sending `Content-Type: application/json`
**Fix:** Add the header to your request

### 4. Case Sensitivity
**Cause:** Using `Username` instead of `username`
**Fix:** Use lowercase field names in JSON

---

## JWT vs API Key Authentication

| Feature | JWT Token | API Key |
|---------|-----------|---------|
| Use Case | User sessions | Partner/service access |
| Lifetime | 60 minutes (configurable) | Until revoked |
| Header | `Authorization: Bearer {token}` | `X-API-Key: {key}` |
| Contains | User info, roles, expiry | Just the key string |
| Refresh | Yes (via refresh endpoint) | No (permanent until revoked) |

**When to use JWT:**
- Web application login
- User-specific operations
- Time-limited access

**When to use API Key:**
- Partner integrations (Amazon, Wayfair)
- Service-to-service communication
- Long-lived access needs

---

## Code Reference

**Controller:** `Controllers/Api/v1/AuthController.cs`
**Models:**
- `LoginRequest` (lines 188-195)
- `RefreshTokenRequest` (lines 197-204)
- `TokenResponse` (lines 206-213)
- `UserInfo` (lines 215-221)

**JWT Service:** `Services/JwtTokenService.cs`

---

*Document created: 2025-12-07*
