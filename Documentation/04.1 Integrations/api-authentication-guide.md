# GreenCircle Certified API Authentication Guide

## Overview

The GreenCircle Certified API uses industry-standard authentication mechanisms to ensure secure access to resources. This guide covers the authentication methods, implementation details, and best practices.

## Table of Contents

1. [Authentication Methods](#authentication-methods)
2. [Getting Started](#getting-started)
3. [Bearer Token Authentication](#bearer-token-authentication)
4. [Cookie-Based Authentication](#cookie-based-authentication)
5. [API Key Authentication](#api-key-authentication)
6. [OAuth 2.0 Integration](#oauth-20-integration)
7. [Security Best Practices](#security-best-practices)
8. [Rate Limiting](#rate-limiting)
9. [Troubleshooting](#troubleshooting)

## Authentication Methods

The API supports multiple authentication methods:

| Method | Use Case | Security Level | Suitable For |
|--------|----------|---------------|--------------|
| **Bearer Token (JWT)** | API-to-API communication | High | Mobile apps, SPAs, third-party integrations |
| **Cookie Authentication** | Browser-based applications | High | Web applications, first-party clients |
| **API Key** | Simple integrations | Medium | Webhooks, basic integrations |
| **OAuth 2.0** | Third-party applications | Highest | Partner integrations, public APIs |

## Getting Started

### 1. Register Your Application

Before you can authenticate with the API, you need to register your application:

```http
POST /api/applications/register
Content-Type: application/json

{
  "name": "Your Application Name",
  "description": "Application description",
  "redirectUris": ["https://yourapp.com/callback"],
  "type": "web" // or "mobile", "spa", "service"
}
```

Response:
```json
{
  "clientId": "your-client-id",
  "clientSecret": "your-client-secret",
  "apiKey": "your-api-key"
}
```

### 2. Choose Your Authentication Method

Select the appropriate authentication method based on your application type:

- **Web Applications**: Use Cookie Authentication or OAuth 2.0
- **Mobile Applications**: Use Bearer Token (JWT)
- **Single Page Applications**: Use Bearer Token with PKCE
- **Server-to-Server**: Use API Key or Client Credentials OAuth

## Bearer Token Authentication

### Obtaining a Token

```http
POST /account/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "securepassword"
}
```

Response:
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh-token-value",
  "expiresIn": 3600,
  "user": {
    "id": 123,
    "name": "John Doe",
    "email": "user@example.com",
    "role": "user"
  }
}
```

### Using the Token

Include the token in the Authorization header for all subsequent requests:

```http
GET /api/certificates
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token Refresh

When the access token expires, use the refresh token to obtain a new one:

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "refresh-token-value"
}
```

### Token Structure

Our JWT tokens contain the following claims:

```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "name": "User Name",
  "role": "user",
  "permissions": ["view-certificates", "edit-certificates"],
  "companyIds": [1, 2, 3],
  "iat": 1609459200,
  "exp": 1609462800,
  "iss": "https://api.greencircle.com"
}
```

### Implementation Examples

#### JavaScript/TypeScript

```typescript
class ApiClient {
  private token: string | null = null;
  private refreshToken: string | null = null;
  private baseUrl = 'https://api.greencircle.com/v1';

  async login(email: string, password: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/account/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });

    const data = await response.json();
    this.token = data.token;
    this.refreshToken = data.refreshToken;
    
    // Store tokens securely
    localStorage.setItem('access_token', this.token);
    localStorage.setItem('refresh_token', this.refreshToken);
  }

  async makeRequest(endpoint: string, options: RequestInit = {}): Promise<Response> {
    const headers = {
      ...options.headers,
      'Authorization': `Bearer ${this.token}`
    };

    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      ...options,
      headers
    });

    if (response.status === 401) {
      // Token expired, try to refresh
      await this.refreshAccessToken();
      // Retry the request
      return this.makeRequest(endpoint, options);
    }

    return response;
  }

  async refreshAccessToken(): Promise<void> {
    const response = await fetch(`${this.baseUrl}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: this.refreshToken })
    });

    const data = await response.json();
    this.token = data.token;
    localStorage.setItem('access_token', this.token);
  }
}
```

#### Python

```python
import requests
from datetime import datetime, timedelta

class GreenCircleAPI:
    def __init__(self):
        self.base_url = "https://api.greencircle.com/v1"
        self.token = None
        self.refresh_token = None
        self.token_expires = None
    
    def login(self, email, password):
        response = requests.post(
            f"{self.base_url}/account/login",
            json={"email": email, "password": password}
        )
        data = response.json()
        
        self.token = data["token"]
        self.refresh_token = data["refreshToken"]
        self.token_expires = datetime.now() + timedelta(seconds=data["expiresIn"])
        
        return data["user"]
    
    def _get_headers(self):
        if datetime.now() >= self.token_expires:
            self._refresh_token()
        
        return {
            "Authorization": f"Bearer {self.token}",
            "Content-Type": "application/json"
        }
    
    def _refresh_token(self):
        response = requests.post(
            f"{self.base_url}/auth/refresh",
            json={"refreshToken": self.refresh_token}
        )
        data = response.json()
        
        self.token = data["token"]
        self.token_expires = datetime.now() + timedelta(seconds=data["expiresIn"])
    
    def get_certificates(self, page=1, page_size=20):
        response = requests.get(
            f"{self.base_url}/certificates",
            headers=self._get_headers(),
            params={"page": page, "pageSize": page_size}
        )
        return response.json()
```

#### C# / .NET

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class GreenCircleApiClient
{
    private readonly HttpClient _httpClient;
    private string _accessToken;
    private string _refreshToken;
    private DateTime _tokenExpiry;

    public GreenCircleApiClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.greencircle.com/v1/")
        };
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var loginRequest = new
        {
            email = email,
            password = password
        };

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("account/login", content);
        
        if (!response.IsSuccessStatusCode)
            return false;

        var responseContent = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);

        _accessToken = loginResponse.Token;
        _refreshToken = loginResponse.RefreshToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(loginResponse.ExpiresIn);

        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _accessToken);

        return true;
    }

    public async Task<T> GetAsync<T>(string endpoint)
    {
        await EnsureValidTokenAsync();

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json);
    }

    private async Task EnsureValidTokenAsync()
    {
        if (DateTime.UtcNow >= _tokenExpiry.AddMinutes(-1))
        {
            await RefreshTokenAsync();
        }
    }

    private async Task RefreshTokenAsync()
    {
        var refreshRequest = new { refreshToken = _refreshToken };
        var json = JsonSerializer.Serialize(refreshRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("auth/refresh", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var refreshResponse = JsonSerializer.Deserialize<RefreshResponse>(responseContent);

        _accessToken = refreshResponse.Token;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(refreshResponse.ExpiresIn);

        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _accessToken);
    }
}
```

## Cookie-Based Authentication

Cookie authentication is ideal for browser-based applications and provides CSRF protection.

### Login

```javascript
// Using fetch API with credentials
const response = await fetch('https://api.greencircle.com/v1/account/login', {
  method: 'POST',
  credentials: 'include', // Important: include cookies
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'securepassword'
  })
});
```

### Subsequent Requests

```javascript
// All subsequent requests must include credentials
const certificates = await fetch('https://api.greencircle.com/v1/certificates', {
  credentials: 'include'
});
```

### CSRF Protection

When using cookie authentication, include the CSRF token in your requests:

```javascript
// Get CSRF token from meta tag or API endpoint
const csrfToken = document.querySelector('meta[name="csrf-token"]').content;

// Include in request headers
const response = await fetch('https://api.greencircle.com/v1/certificates', {
  method: 'POST',
  credentials: 'include',
  headers: {
    'Content-Type': 'application/json',
    'X-CSRF-Token': csrfToken
  },
  body: JSON.stringify(data)
});
```

## API Key Authentication

API keys are suitable for server-to-server communication and webhooks.

### Using API Keys

Include your API key in the request header:

```http
GET /api/certificates
X-API-Key: your-api-key-here
```

### Example Implementation

```python
import requests

class GreenCircleAPIKey:
    def __init__(self, api_key):
        self.api_key = api_key
        self.base_url = "https://api.greencircle.com/v1"
    
    def _get_headers(self):
        return {
            "X-API-Key": self.api_key,
            "Content-Type": "application/json"
        }
    
    def get_certificates(self):
        response = requests.get(
            f"{self.base_url}/certificates",
            headers=self._get_headers()
        )
        return response.json()

# Usage
api = GreenCircleAPIKey("your-api-key-here")
certificates = api.get_certificates()
```

### API Key Restrictions

You can configure restrictions for your API keys:

- **IP Whitelist**: Restrict to specific IP addresses
- **Rate Limits**: Custom rate limits per key
- **Scope**: Limit to specific endpoints or operations
- **Expiration**: Set expiration dates for temporary access

## OAuth 2.0 Integration

OAuth 2.0 is recommended for third-party applications that need to access user data.

### Authorization Code Flow

#### 1. Direct user to authorization endpoint

```
https://auth.greencircle.com/oauth/authorize?
  client_id=your-client-id&
  redirect_uri=https://yourapp.com/callback&
  response_type=code&
  scope=read:certificates write:certificates&
  state=random-state-value
```

#### 2. Handle the callback

```javascript
// Extract authorization code from URL
const urlParams = new URLSearchParams(window.location.search);
const code = urlParams.get('code');
const state = urlParams.get('state');

// Verify state to prevent CSRF
if (state !== savedState) {
  throw new Error('Invalid state parameter');
}
```

#### 3. Exchange code for tokens

```http
POST /oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&
code=authorization-code&
client_id=your-client-id&
client_secret=your-client-secret&
redirect_uri=https://yourapp.com/callback
```

### Client Credentials Flow

For server-to-server authentication:

```http
POST /oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&
client_id=your-client-id&
client_secret=your-client-secret&
scope=read:certificates
```

### Available Scopes

| Scope | Description |
|-------|-------------|
| `read:certificates` | Read certificate data |
| `write:certificates` | Create and update certificates |
| `delete:certificates` | Delete certificates |
| `read:companies` | Read company information |
| `write:companies` | Manage companies |
| `read:users` | Read user profiles |
| `write:users` | Manage users (admin only) |
| `admin` | Full administrative access |

## Security Best Practices

### 1. Token Storage

**DO:**
- Store tokens in secure, httpOnly cookies (web apps)
- Use secure storage APIs (mobile apps)
- Encrypt tokens at rest
- Implement token rotation

**DON'T:**
- Store tokens in localStorage (XSS vulnerable)
- Include tokens in URLs
- Log tokens
- Store tokens in plain text

### 2. HTTPS Only

Always use HTTPS for API communication:

```javascript
// Good
const apiUrl = 'https://api.greencircle.com/v1';

// Bad - will be rejected
const apiUrl = 'http://api.greencircle.com/v1';
```

### 3. Token Expiration

Implement proper token expiration handling:

```javascript
class TokenManager {
  constructor() {
    this.tokenExpiry = null;
    this.refreshTimer = null;
  }

  setToken(token, expiresIn) {
    this.token = token;
    this.tokenExpiry = Date.now() + (expiresIn * 1000);
    
    // Refresh 5 minutes before expiry
    const refreshIn = (expiresIn - 300) * 1000;
    this.refreshTimer = setTimeout(() => this.refreshToken(), refreshIn);
  }

  isTokenValid() {
    return this.token && Date.now() < this.tokenExpiry;
  }

  async refreshToken() {
    // Implement refresh logic
  }
}
```

### 4. Rate Limiting Protection

Implement exponential backoff for rate limit errors:

```javascript
async function makeRequestWithRetry(url, options, maxRetries = 3) {
  for (let i = 0; i < maxRetries; i++) {
    const response = await fetch(url, options);
    
    if (response.status === 429) {
      const retryAfter = response.headers.get('Retry-After') || Math.pow(2, i);
      await new Promise(resolve => setTimeout(resolve, retryAfter * 1000));
      continue;
    }
    
    return response;
  }
  
  throw new Error('Max retries exceeded');
}
```

### 5. Input Validation

Always validate and sanitize input:

```javascript
function validateApiKey(apiKey) {
  const pattern = /^[A-Za-z0-9_-]{40,}$/;
  if (!pattern.test(apiKey)) {
    throw new Error('Invalid API key format');
  }
  return apiKey;
}
```

## Rate Limiting

The API implements rate limiting to ensure fair usage:

### Default Limits

| Authentication Type | Requests per Hour | Burst Rate |
|-------------------|------------------|------------|
| Unauthenticated | 100 | 10/minute |
| API Key | 1,000 | 100/minute |
| Bearer Token | 5,000 | 500/minute |
| OAuth 2.0 | 10,000 | 1000/minute |

### Rate Limit Headers

Monitor these headers in API responses:

```http
X-RateLimit-Limit: 5000
X-RateLimit-Remaining: 4999
X-RateLimit-Reset: 1609462800
Retry-After: 3600
```

### Handling Rate Limits

```javascript
class RateLimitHandler {
  async handleResponse(response) {
    const remaining = parseInt(response.headers.get('X-RateLimit-Remaining'));
    const reset = parseInt(response.headers.get('X-RateLimit-Reset'));
    
    if (remaining < 100) {
      console.warn(`Low rate limit: ${remaining} requests remaining`);
    }
    
    if (response.status === 429) {
      const retryAfter = response.headers.get('Retry-After');
      const waitTime = retryAfter ? parseInt(retryAfter) : 60;
      
      console.log(`Rate limited. Waiting ${waitTime} seconds...`);
      await new Promise(resolve => setTimeout(resolve, waitTime * 1000));
      
      // Retry the request
      return fetch(response.url, {
        method: response.method,
        headers: response.headers,
        body: response.body
      });
    }
    
    return response;
  }
}
```

## Troubleshooting

### Common Authentication Errors

| Error Code | Description | Solution |
|------------|-------------|----------|
| `401` | Unauthorized | Check token validity, refresh if expired |
| `403` | Forbidden | Verify user permissions and scopes |
| `429` | Too Many Requests | Implement rate limiting and backoff |
| `invalid_token` | Token is malformed | Ensure proper token format |
| `expired_token` | Token has expired | Refresh the token |
| `insufficient_scope` | Missing required scope | Request additional scopes |
| `invalid_client` | Client authentication failed | Verify client credentials |

### Debugging Authentication Issues

Enable debug logging:

```javascript
class DebugApiClient {
  constructor(debug = false) {
    this.debug = debug;
  }

  async request(url, options) {
    if (this.debug) {
      console.log('Request:', {
        url,
        method: options.method,
        headers: this.sanitizeHeaders(options.headers)
      });
    }

    const response = await fetch(url, options);

    if (this.debug) {
      console.log('Response:', {
        status: response.status,
        headers: Object.fromEntries(response.headers.entries())
      });
    }

    if (!response.ok && this.debug) {
      const error = await response.text();
      console.error('Error response:', error);
    }

    return response;
  }

  sanitizeHeaders(headers) {
    const sanitized = { ...headers };
    if (sanitized.Authorization) {
      sanitized.Authorization = 'Bearer [REDACTED]';
    }
    if (sanitized['X-API-Key']) {
      sanitized['X-API-Key'] = '[REDACTED]';
    }
    return sanitized;
  }
}
```

### Testing Authentication

Use our authentication testing endpoint:

```http
GET /api/auth/test
Authorization: Bearer your-token-here
```

Response:
```json
{
  "authenticated": true,
  "user": {
    "id": 123,
    "email": "user@example.com",
    "role": "user"
  },
  "permissions": ["read:certificates", "write:certificates"],
  "tokenExpiry": "2024-01-01T12:00:00Z",
  "scopes": ["read:certificates", "write:certificates"]
}
```

## Support

For authentication issues or questions:

- **Documentation**: https://docs.greencircle.com/api/auth
- **Status Page**: https://status.greencircle.com
- **Support Email**: api-support@greencircle.com
- **Developer Forum**: https://developers.greencircle.com/forum

## Changelog

### Version 1.0.0 (Current)
- Initial authentication implementation
- Support for JWT, Cookie, API Key, and OAuth 2.0
- Rate limiting implementation
- CSRF protection for cookie authentication