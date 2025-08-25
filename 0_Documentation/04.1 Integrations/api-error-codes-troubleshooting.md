# GreenCircle Certified API Error Codes & Troubleshooting Guide

## Table of Contents

1. [HTTP Status Codes](#http-status-codes)
2. [API Error Codes](#api-error-codes)
3. [Error Response Format](#error-response-format)
4. [Authentication Errors](#authentication-errors)
5. [Validation Errors](#validation-errors)
6. [Rate Limiting Errors](#rate-limiting-errors)
7. [Business Logic Errors](#business-logic-errors)
8. [System Errors](#system-errors)
9. [Troubleshooting Guide](#troubleshooting-guide)
10. [Debugging Tools](#debugging-tools)
11. [Common Solutions](#common-solutions)
12. [Support and Escalation](#support-and-escalation)

## HTTP Status Codes

The API uses standard HTTP status codes to indicate the success or failure of requests:

### 2xx Success

| Code | Status | Description | When Used |
|------|--------|-------------|-----------|
| `200` | OK | Request successful | GET, PUT, DELETE operations |
| `201` | Created | Resource created successfully | POST operations |
| `202` | Accepted | Request accepted for processing | Async operations |
| `204` | No Content | Request successful, no content returned | DELETE operations |

### 4xx Client Errors

| Code | Status | Description | Common Causes |
|------|--------|-------------|---------------|
| `400` | Bad Request | Invalid request syntax or parameters | Malformed JSON, missing required fields |
| `401` | Unauthorized | Authentication required or invalid | Missing/expired token, invalid credentials |
| `403` | Forbidden | Insufficient permissions | User lacks required role/permissions |
| `404` | Not Found | Resource not found | Invalid ID, deleted resource |
| `405` | Method Not Allowed | HTTP method not supported | Using GET on POST-only endpoint |
| `409` | Conflict | Resource conflict | Duplicate certificate number |
| `410` | Gone | Resource permanently deleted | Accessing archived resource |
| `413` | Payload Too Large | Request body too large | File upload exceeds limits |
| `422` | Unprocessable Entity | Validation errors | Invalid data format or values |
| `429` | Too Many Requests | Rate limit exceeded | Too many requests in time window |

### 5xx Server Errors

| Code | Status | Description | When It Occurs |
|------|--------|-------------|----------------|
| `500` | Internal Server Error | Unexpected server error | Database errors, application bugs |
| `502` | Bad Gateway | Upstream server error | Service dependency failure |
| `503` | Service Unavailable | Service temporarily unavailable | Maintenance, overload |
| `504` | Gateway Timeout | Request timeout | Slow database queries, network issues |

## API Error Codes

The API returns specific error codes to help identify the exact issue:

### Authentication Error Codes (AUTH_*)

| Error Code | Description | HTTP Status | Resolution |
|------------|-------------|-------------|------------|
| `AUTH_TOKEN_MISSING` | Authorization token not provided | 401 | Include Bearer token in Authorization header |
| `AUTH_TOKEN_INVALID` | Token is malformed or invalid | 401 | Refresh token or re-authenticate |
| `AUTH_TOKEN_EXPIRED` | Token has expired | 401 | Use refresh token to get new access token |
| `AUTH_INVALID_CREDENTIALS` | Email/password combination is invalid | 401 | Verify credentials and retry |
| `AUTH_ACCOUNT_LOCKED` | Account has been locked due to failed attempts | 401 | Contact support or wait for unlock |
| `AUTH_ACCOUNT_DISABLED` | Account has been disabled | 403 | Contact administrator |
| `AUTH_INSUFFICIENT_PERMISSIONS` | User lacks required permissions | 403 | Request access or use account with proper permissions |
| `AUTH_API_KEY_INVALID` | API key is invalid or revoked | 401 | Check API key in developer dashboard |
| `AUTH_IP_NOT_WHITELISTED` | Request from non-whitelisted IP | 403 | Add IP to whitelist or use VPN |
| `AUTH_OAUTH_SCOPE_INSUFFICIENT` | OAuth token lacks required scope | 403 | Re-authorize with additional scopes |

### Validation Error Codes (VAL_*)

| Error Code | Description | HTTP Status | Resolution |
|------------|-------------|-------------|------------|
| `VAL_REQUIRED_FIELD_MISSING` | Required field is missing | 422 | Include all required fields |
| `VAL_INVALID_FORMAT` | Field format is invalid | 422 | Check data format (email, date, etc.) |
| `VAL_VALUE_OUT_OF_RANGE` | Value exceeds allowed range | 422 | Use value within specified range |
| `VAL_INVALID_ENUM_VALUE` | Invalid enumeration value | 422 | Use one of the allowed values |
| `VAL_STRING_TOO_LONG` | String exceeds maximum length | 422 | Shorten the string value |
| `VAL_STRING_TOO_SHORT` | String below minimum length | 422 | Lengthen the string value |
| `VAL_INVALID_EMAIL` | Email format is invalid | 422 | Provide valid email address |
| `VAL_INVALID_DATE` | Date format is invalid | 422 | Use ISO 8601 date format (YYYY-MM-DD) |
| `VAL_INVALID_URL` | URL format is invalid | 422 | Provide valid URL with protocol |
| `VAL_INVALID_JSON` | JSON syntax is invalid | 400 | Fix JSON syntax errors |

### Business Logic Error Codes (BIZ_*)

| Error Code | Description | HTTP Status | Resolution |
|------------|-------------|-------------|------------|
| `BIZ_CERTIFICATE_NUMBER_EXISTS` | Certificate number already exists | 409 | Use unique certificate number |
| `BIZ_COMPANY_NOT_FOUND` | Referenced company doesn't exist | 422 | Verify company ID exists |
| `BIZ_CERTIFICATE_EXPIRED` | Certificate has already expired | 422 | Use future expiration date |
| `BIZ_INVALID_DATE_RANGE` | Issue date after expiration date | 422 | Ensure issue date is before expiration |
| `BIZ_COMPANY_HAS_CERTIFICATES` | Cannot delete company with certificates | 409 | Remove certificates first |
| `BIZ_USER_EMAIL_EXISTS` | Email address already registered | 409 | Use different email or update existing user |
| `BIZ_ROLE_IN_USE` | Cannot delete role assigned to users | 409 | Reassign users to different role first |
| `BIZ_INSUFFICIENT_QUOTA` | API quota exceeded | 429 | Upgrade plan or wait for quota reset |
| `BIZ_FEATURE_NOT_ENABLED` | Feature not available for current plan | 403 | Upgrade to access feature |
| `BIZ_OPERATION_NOT_PERMITTED` | Operation not allowed in current state | 409 | Check resource state and requirements |

### Rate Limiting Error Codes (RATE_*)

| Error Code | Description | HTTP Status | Resolution |
|------------|-------------|-------------|------------|
| `RATE_LIMIT_EXCEEDED` | Too many requests per time period | 429 | Wait for rate limit reset |
| `RATE_LIMIT_PER_MINUTE` | Per-minute rate limit exceeded | 429 | Wait 60 seconds before retrying |
| `RATE_LIMIT_PER_HOUR` | Hourly rate limit exceeded | 429 | Wait for next hour or upgrade plan |
| `RATE_LIMIT_BURST` | Burst rate limit exceeded | 429 | Slow down request frequency |
| `RATE_LIMIT_CONCURRENT` | Too many concurrent requests | 429 | Reduce concurrent request count |

### System Error Codes (SYS_*)

| Error Code | Description | HTTP Status | Resolution |
|------------|-------------|-------------|------------|
| `SYS_DATABASE_ERROR` | Database operation failed | 500 | Retry request, contact support if persistent |
| `SYS_SERVICE_UNAVAILABLE` | External service unavailable | 503 | Retry after delay |
| `SYS_TIMEOUT` | Request processing timeout | 504 | Retry with smaller payload or contact support |
| `SYS_MAINTENANCE` | System under maintenance | 503 | Check status page, retry later |
| `SYS_STORAGE_ERROR` | File storage operation failed | 500 | Retry file operation |
| `SYS_INTEGRATION_ERROR` | Third-party integration failed | 502 | Retry request or use alternative method |

## Error Response Format

All error responses follow a consistent format:

```json
{
  "success": false,
  "message": "Human-readable error message",
  "errorCode": "MACHINE_READABLE_ERROR_CODE",
  "errors": [
    {
      "field": "fieldName",
      "message": "Specific field error message",
      "code": "FIELD_SPECIFIC_CODE"
    }
  ],
  "details": {
    "requestId": "req_1234567890",
    "timestamp": "2024-02-20T10:30:00Z",
    "path": "/api/v1/certificates",
    "method": "POST"
  }
}
```

### Error Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Always `false` for error responses |
| `message` | string | Human-readable error description |
| `errorCode` | string | Machine-readable error identifier |
| `errors` | array | Array of specific field errors (validation) |
| `details` | object | Additional context about the error |
| `details.requestId` | string | Unique request identifier for support |
| `details.timestamp` | string | ISO 8601 timestamp when error occurred |
| `details.path` | string | API endpoint that generated the error |
| `details.method` | string | HTTP method used |

## Authentication Errors

### Missing Authorization Header

```json
{
  "success": false,
  "message": "Authorization header is required",
  "errorCode": "AUTH_TOKEN_MISSING",
  "details": {
    "requestId": "req_abc123",
    "timestamp": "2024-02-20T10:30:00Z"
  }
}
```

**Solution:**
```javascript
// Correct way to include authorization
const response = await fetch('/api/certificates', {
  headers: {
    'Authorization': 'Bearer your-jwt-token-here',
    'Content-Type': 'application/json'
  }
});
```

### Expired Token

```json
{
  "success": false,
  "message": "Access token has expired",
  "errorCode": "AUTH_TOKEN_EXPIRED",
  "details": {
    "requestId": "req_def456",
    "timestamp": "2024-02-20T10:30:00Z",
    "expiredAt": "2024-02-20T10:00:00Z"
  }
}
```

**Solution:**
```javascript
// Refresh token when expired
if (error.errorCode === 'AUTH_TOKEN_EXPIRED') {
  const newToken = await refreshAccessToken(refreshToken);
  // Retry original request with new token
  return makeRequest(originalRequest, newToken);
}
```

### Insufficient Permissions

```json
{
  "success": false,
  "message": "User lacks permission to perform this action",
  "errorCode": "AUTH_INSUFFICIENT_PERMISSIONS",
  "details": {
    "requiredPermissions": ["edit-certificates"],
    "userPermissions": ["view-certificates"]
  }
}
```

**Solution:** Contact administrator to grant required permissions or use an account with appropriate access.

## Validation Errors

### Multiple Field Validation Errors

```json
{
  "success": false,
  "message": "Validation failed",
  "errorCode": "VAL_VALIDATION_FAILED",
  "errors": [
    {
      "field": "email",
      "message": "Invalid email format",
      "code": "VAL_INVALID_EMAIL"
    },
    {
      "field": "certificateNumber",
      "message": "Certificate number is required",
      "code": "VAL_REQUIRED_FIELD_MISSING"
    },
    {
      "field": "expirationDate",
      "message": "Expiration date must be in the future",
      "code": "VAL_INVALID_DATE_RANGE"
    }
  ]
}
```

### Invalid Date Format

```json
{
  "success": false,
  "message": "Invalid date format",
  "errorCode": "VAL_INVALID_DATE",
  "errors": [
    {
      "field": "issueDate",
      "message": "Date must be in YYYY-MM-DD format",
      "code": "VAL_INVALID_FORMAT",
      "providedValue": "02/20/2024",
      "expectedFormat": "YYYY-MM-DD"
    }
  ]
}
```

**Solution:**
```javascript
// Correct date format
const certificate = {
  certificateNumber: "24-1605",
  issueDate: "2024-02-20",      // Correct: YYYY-MM-DD
  expirationDate: "2025-02-20"  // Correct: YYYY-MM-DD
};
```

## Rate Limiting Errors

### Rate Limit Exceeded

```json
{
  "success": false,
  "message": "Rate limit exceeded",
  "errorCode": "RATE_LIMIT_EXCEEDED",
  "details": {
    "limit": 1000,
    "remaining": 0,
    "resetTime": "2024-02-20T11:00:00Z",
    "retryAfter": 3600
  }
}
```

**Response Headers:**
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1708426800
Retry-After: 3600
```

**Solution:**
```javascript
// Handle rate limiting with exponential backoff
async function makeRequestWithBackoff(apiCall, maxRetries = 3) {
  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      return await apiCall();
    } catch (error) {
      if (error.errorCode === 'RATE_LIMIT_EXCEEDED') {
        const retryAfter = error.details?.retryAfter || Math.pow(2, attempt) * 1000;
        console.log(`Rate limited, waiting ${retryAfter}ms before retry`);
        await new Promise(resolve => setTimeout(resolve, retryAfter));
        continue;
      }
      throw error; // Re-throw non-rate-limit errors
    }
  }
  throw new Error('Max retries exceeded');
}
```

## Business Logic Errors

### Duplicate Certificate Number

```json
{
  "success": false,
  "message": "Certificate number already exists",
  "errorCode": "BIZ_CERTIFICATE_NUMBER_EXISTS",
  "details": {
    "duplicateValue": "24-1605",
    "existingCertificateId": "cert-456",
    "conflictField": "certificateNumber"
  }
}
```

**Solution:** Use a unique certificate number or update the existing certificate.

### Referenced Resource Not Found

```json
{
  "success": false,
  "message": "Referenced company does not exist",
  "errorCode": "BIZ_COMPANY_NOT_FOUND",
  "details": {
    "referencedField": "companyId",
    "providedValue": 999,
    "availableOptions": "/api/companies"
  }
}
```

**Solution:** Verify the company ID exists by calling GET /companies first.

## System Errors

### Database Connection Error

```json
{
  "success": false,
  "message": "Database operation failed",
  "errorCode": "SYS_DATABASE_ERROR",
  "details": {
    "requestId": "req_xyz789",
    "retryable": true,
    "estimatedRetryDelay": 5000
  }
}
```

**Solution:** Retry the request after a short delay. If persistent, contact support with the request ID.

### Service Maintenance

```json
{
  "success": false,
  "message": "Service temporarily unavailable for maintenance",
  "errorCode": "SYS_MAINTENANCE",
  "details": {
    "estimatedDuration": 1800,
    "statusPage": "https://status.greencircle.com",
    "maintenanceWindow": "2024-02-20T02:00:00Z to 2024-02-20T03:00:00Z"
  }
}
```

**Response Headers:**
```
Retry-After: 1800
```

## Troubleshooting Guide

### 1. Authentication Issues

#### Problem: Getting 401 Unauthorized
```bash
curl -X GET https://api.greencircle.com/v1/certificates
# Returns: 401 Unauthorized
```

**Diagnosis Steps:**
1. Check if Authorization header is included
2. Verify token format (should start with "Bearer ")
3. Check if token has expired
4. Validate token claims and permissions

**Solution:**
```bash
# First, authenticate to get a token
curl -X POST https://api.greencircle.com/v1/account/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}'

# Then use the token in subsequent requests
curl -X GET https://api.greencircle.com/v1/certificates \
  -H "Authorization: Bearer your-jwt-token-here"
```

#### Problem: Getting 403 Forbidden with valid token
**Diagnosis:** User has valid authentication but lacks permissions for the requested operation.

**Solution:**
1. Check user role and permissions in the dashboard
2. Contact administrator to grant required permissions
3. Use an account with appropriate access level

### 2. Request Format Issues

#### Problem: 400 Bad Request with JSON errors
```json
{
  "success": false,
  "message": "Invalid JSON syntax",
  "errorCode": "VAL_INVALID_JSON"
}
```

**Common JSON Issues:**
```javascript
// Bad: Trailing comma
{
  "name": "Test Product",
  "type": "Recycled Content",  // <- Trailing comma causes error
}

// Good: No trailing comma
{
  "name": "Test Product",
  "type": "Recycled Content"
}

// Bad: Unquoted keys
{
  name: "Test Product",  // <- Keys must be quoted
  type: "Recycled Content"
}

// Good: Quoted keys
{
  "name": "Test Product",
  "type": "Recycled Content"
}
```

### 3. Data Validation Issues

#### Problem: 422 Unprocessable Entity
**Diagnosis Steps:**
1. Check the `errors` array in the response
2. Identify which fields are causing validation failures
3. Review field requirements in the API documentation

**Example Fix:**
```javascript
// Error response indicates invalid date format
{
  "errors": [
    {
      "field": "issueDate",
      "message": "Date must be in YYYY-MM-DD format",
      "code": "VAL_INVALID_FORMAT"
    }
  ]
}

// Fix: Use correct date format
const certificateData = {
  certificateNumber: "24-1605",
  issueDate: "2024-02-20",      // Changed from "02/20/2024"
  expirationDate: "2025-02-20", // Changed from "02/20/2025"
  // ... other fields
};
```

### 4. Rate Limiting Issues

#### Problem: Getting 429 Too Many Requests

**Immediate Solution:**
```javascript
// Check rate limit headers to understand limits
const checkRateLimit = (response) => {
  const limit = response.headers.get('X-RateLimit-Limit');
  const remaining = response.headers.get('X-RateLimit-Remaining');
  const reset = response.headers.get('X-RateLimit-Reset');
  
  console.log(`Rate limit: ${remaining}/${limit} remaining`);
  console.log(`Resets at: ${new Date(reset * 1000)}`);
};
```

**Long-term Solution:**
```javascript
// Implement request queueing
class RequestQueue {
  constructor(maxConcurrent = 5, minDelay = 200) {
    this.queue = [];
    this.active = 0;
    this.maxConcurrent = maxConcurrent;
    this.minDelay = minDelay;
  }

  async add(requestFn) {
    return new Promise((resolve, reject) => {
      this.queue.push({ requestFn, resolve, reject });
      this.process();
    });
  }

  async process() {
    if (this.active >= this.maxConcurrent || this.queue.length === 0) {
      return;
    }

    this.active++;
    const { requestFn, resolve, reject } = this.queue.shift();

    try {
      const result = await requestFn();
      resolve(result);
    } catch (error) {
      reject(error);
    } finally {
      this.active--;
      setTimeout(() => this.process(), this.minDelay);
    }
  }
}
```

### 5. Network and Timeout Issues

#### Problem: Connection timeouts or network errors

**Diagnosis:**
```javascript
// Add timeout and retry logic
const fetchWithTimeout = async (url, options = {}, timeout = 30000) => {
  const controller = new AbortController();
  const id = setTimeout(() => controller.abort(), timeout);

  try {
    const response = await fetch(url, {
      ...options,
      signal: controller.signal
    });
    clearTimeout(id);
    return response;
  } catch (error) {
    clearTimeout(id);
    
    if (error.name === 'AbortError') {
      throw new Error('Request timeout');
    }
    
    throw error;
  }
};
```

**Solution with Retry Logic:**
```javascript
const retryWithBackoff = async (fn, maxRetries = 3, baseDelay = 1000) => {
  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      return await fn();
    } catch (error) {
      // Don't retry client errors (4xx)
      if (error.status >= 400 && error.status < 500) {
        throw error;
      }
      
      if (attempt === maxRetries - 1) {
        throw error;
      }
      
      const delay = baseDelay * Math.pow(2, attempt);
      console.log(`Attempt ${attempt + 1} failed, retrying in ${delay}ms`);
      await new Promise(resolve => setTimeout(resolve, delay));
    }
  }
};
```

## Debugging Tools

### 1. Request/Response Logging

```javascript
class ApiDebugger {
  constructor(client, options = {}) {
    this.client = client;
    this.logLevel = options.logLevel || 'info';
    this.logBody = options.logBody || false;
  }

  log(level, message, data) {
    if (this.shouldLog(level)) {
      console.log(`[API-${level.toUpperCase()}] ${message}`);
      if (data && this.logBody) {
        console.log(JSON.stringify(data, null, 2));
      }
    }
  }

  shouldLog(level) {
    const levels = ['debug', 'info', 'warn', 'error'];
    return levels.indexOf(level) >= levels.indexOf(this.logLevel);
  }

  wrapMethod(methodName) {
    const originalMethod = this.client[methodName];
    
    this.client[methodName] = async (...args) => {
      const requestId = Math.random().toString(36).substr(2, 9);
      
      this.log('info', `[${requestId}] Starting ${methodName}`, args);
      
      const startTime = Date.now();
      
      try {
        const result = await originalMethod.apply(this.client, args);
        const duration = Date.now() - startTime;
        
        this.log('info', `[${requestId}] ${methodName} completed in ${duration}ms`);
        this.log('debug', `[${requestId}] Response:`, result);
        
        return result;
      } catch (error) {
        const duration = Date.now() - startTime;
        
        this.log('error', `[${requestId}] ${methodName} failed in ${duration}ms: ${error.message}`);
        this.log('debug', `[${requestId}] Error details:`, error);
        
        throw error;
      }
    };
  }
}

// Usage
const debugger = new ApiDebugger(apiClient, { 
  logLevel: 'debug', 
  logBody: true 
});

debugger.wrapMethod('getCertificates');
debugger.wrapMethod('createCertificate');
```

### 2. Health Check Endpoint

```javascript
// Check API health before making requests
const checkApiHealth = async () => {
  try {
    const response = await fetch('https://api.greencircle.com/v1/health');
    const health = await response.json();
    
    if (health.status === 'healthy') {
      console.log('API is operational');
      return true;
    } else {
      console.warn('API is experiencing issues:', health.issues);
      return false;
    }
  } catch (error) {
    console.error('API health check failed:', error.message);
    return false;
  }
};
```

### 3. Error Analysis Tool

```javascript
class ErrorAnalyzer {
  constructor() {
    this.errorStats = new Map();
  }

  analyze(error) {
    const errorType = this.categorizeError(error);
    const stats = this.errorStats.get(errorType) || { count: 0, lastSeen: null };
    
    stats.count++;
    stats.lastSeen = new Date();
    
    this.errorStats.set(errorType, stats);
    
    return {
      category: errorType,
      isRetryable: this.isRetryable(error),
      suggestedAction: this.getSuggestedAction(error),
      troubleshootingUrl: this.getTroubleshootingUrl(error)
    };
  }

  categorizeError(error) {
    if (error.status >= 500) return 'SERVER_ERROR';
    if (error.status === 429) return 'RATE_LIMIT';
    if (error.status === 401 || error.status === 403) return 'AUTH_ERROR';
    if (error.status === 422) return 'VALIDATION_ERROR';
    if (error.status === 404) return 'NOT_FOUND';
    if (error.status >= 400) return 'CLIENT_ERROR';
    return 'NETWORK_ERROR';
  }

  isRetryable(error) {
    const retryableStatuses = [408, 429, 500, 502, 503, 504];
    return retryableStatuses.includes(error.status);
  }

  getSuggestedAction(error) {
    switch (error.errorCode) {
      case 'AUTH_TOKEN_EXPIRED':
        return 'Refresh your access token using the refresh token';
      case 'VAL_REQUIRED_FIELD_MISSING':
        return 'Check the API documentation for required fields';
      case 'RATE_LIMIT_EXCEEDED':
        return 'Implement exponential backoff and reduce request frequency';
      case 'BIZ_CERTIFICATE_NUMBER_EXISTS':
        return 'Use a unique certificate number or update the existing certificate';
      default:
        return 'Check the error message and API documentation for details';
    }
  }

  getTroubleshootingUrl(error) {
    const baseUrl = 'https://docs.greencircle.com/api/troubleshooting';
    
    if (error.errorCode) {
      return `${baseUrl}#${error.errorCode.toLowerCase()}`;
    }
    
    return `${baseUrl}#http-${error.status}`;
  }

  getErrorReport() {
    return Array.from(this.errorStats.entries()).map(([type, stats]) => ({
      errorType: type,
      count: stats.count,
      lastSeen: stats.lastSeen
    }));
  }
}

// Usage
const analyzer = new ErrorAnalyzer();

try {
  await apiClient.createCertificate(data);
} catch (error) {
  const analysis = analyzer.analyze(error);
  console.log('Error analysis:', analysis);
  
  if (analysis.isRetryable) {
    console.log('This error is retryable');
  }
}
```

## Common Solutions

### 1. Token Management

```javascript
class TokenManager {
  constructor() {
    this.token = null;
    this.refreshToken = null;
    this.tokenExpiry = null;
    this.refreshPromise = null;
  }

  setTokens(accessToken, refreshToken, expiresIn) {
    this.token = accessToken;
    this.refreshToken = refreshToken;
    this.tokenExpiry = Date.now() + (expiresIn * 1000);
  }

  isTokenExpired() {
    return !this.token || Date.now() >= this.tokenExpiry - 60000; // 1 minute buffer
  }

  async getValidToken() {
    if (!this.isTokenExpired()) {
      return this.token;
    }

    // Prevent multiple simultaneous refresh attempts
    if (!this.refreshPromise) {
      this.refreshPromise = this.refreshAccessToken();
    }

    try {
      await this.refreshPromise;
      return this.token;
    } finally {
      this.refreshPromise = null;
    }
  }

  async refreshAccessToken() {
    if (!this.refreshToken) {
      throw new Error('No refresh token available');
    }

    const response = await fetch('/api/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: this.refreshToken })
    });

    if (!response.ok) {
      throw new Error('Token refresh failed');
    }

    const data = await response.json();
    this.setTokens(data.token, this.refreshToken, data.expiresIn);
  }
}
```

### 2. Batch Error Handling

```javascript
class BatchProcessor {
  async processBatchWithErrorHandling(items, processor) {
    const results = [];
    const errors = [];
    
    for (let i = 0; i < items.length; i++) {
      try {
        const result = await processor(items[i], i);
        results.push({ index: i, success: true, data: result });
      } catch (error) {
        const errorInfo = {
          index: i,
          success: false,
          error: error.message,
          errorCode: error.errorCode,
          retryable: this.isRetryableError(error)
        };
        
        errors.push(errorInfo);
        results.push(errorInfo);
      }
    }
    
    return {
      results,
      errors,
      summary: {
        total: items.length,
        successful: results.filter(r => r.success).length,
        failed: errors.length,
        retryableErrors: errors.filter(e => e.retryable).length
      }
    };
  }

  async retryFailedItems(failedResults, processor, maxRetries = 2) {
    const retryableItems = failedResults.filter(r => r.retryable);
    
    for (const item of retryableItems) {
      let attempt = 0;
      while (attempt < maxRetries) {
        try {
          const result = await processor(item.originalData, item.index);
          item.success = true;
          item.data = result;
          delete item.error;
          delete item.errorCode;
          break;
        } catch (error) {
          attempt++;
          if (attempt >= maxRetries) {
            item.error = `Failed after ${maxRetries} retries: ${error.message}`;
          }
          
          // Wait between retries
          await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
        }
      }
    }
  }

  isRetryableError(error) {
    const retryableCodes = [
      'SYS_DATABASE_ERROR',
      'SYS_SERVICE_UNAVAILABLE',
      'SYS_TIMEOUT',
      'RATE_LIMIT_EXCEEDED'
    ];
    
    return retryableCodes.includes(error.errorCode) || error.status >= 500;
  }
}
```

### 3. Request Validation

```javascript
class RequestValidator {
  validateCertificateData(data) {
    const errors = [];
    
    // Required fields
    const requiredFields = ['certificateNumber', 'companyId', 'productName', 'certificateType'];
    for (const field of requiredFields) {
      if (!data[field]) {
        errors.push({
          field,
          message: `${field} is required`,
          code: 'VAL_REQUIRED_FIELD_MISSING'
        });
      }
    }
    
    // Date validation
    if (data.issueDate && !this.isValidDate(data.issueDate)) {
      errors.push({
        field: 'issueDate',
        message: 'Issue date must be in YYYY-MM-DD format',
        code: 'VAL_INVALID_DATE'
      });
    }
    
    if (data.expirationDate && !this.isValidDate(data.expirationDate)) {
      errors.push({
        field: 'expirationDate',
        message: 'Expiration date must be in YYYY-MM-DD format',
        code: 'VAL_INVALID_DATE'
      });
    }
    
    // Date range validation
    if (data.issueDate && data.expirationDate) {
      if (new Date(data.issueDate) >= new Date(data.expirationDate)) {
        errors.push({
          field: 'expirationDate',
          message: 'Expiration date must be after issue date',
          code: 'VAL_INVALID_DATE_RANGE'
        });
      }
    }
    
    // String length validation
    if (data.certificateNumber && data.certificateNumber.length > 50) {
      errors.push({
        field: 'certificateNumber',
        message: 'Certificate number must not exceed 50 characters',
        code: 'VAL_STRING_TOO_LONG'
      });
    }
    
    if (errors.length > 0) {
      throw new ValidationError('Validation failed', errors);
    }
  }

  isValidDate(dateString) {
    const regex = /^\d{4}-\d{2}-\d{2}$/;
    if (!regex.test(dateString)) return false;
    
    const date = new Date(dateString);
    return date instanceof Date && !isNaN(date.getTime());
  }
}

class ValidationError extends Error {
  constructor(message, errors) {
    super(message);
    this.name = 'ValidationError';
    this.errors = errors;
  }
}
```

## Support and Escalation

### When to Contact Support

Contact API support when you encounter:

1. **Persistent 500 errors** with the same request ID
2. **Unexpected behavior** that doesn't match the documentation
3. **Rate limiting issues** that seem incorrect for your plan
4. **Authentication problems** after verifying all credentials
5. **Data inconsistencies** or corruption
6. **Performance issues** with response times

### Support Channels

| Channel | Response Time | Best For |
|---------|---------------|----------|
| **Email** | 4-8 hours | Non-urgent issues, detailed problems |
| **Support Portal** | 2-4 hours | Account and billing issues |
| **Developer Chat** | Real-time | Quick questions during business hours |
| **Phone** | Immediate | Critical production issues |

### Information to Include

When contacting support, provide:

1. **Request ID** from error responses
2. **Timestamp** when the error occurred
3. **Complete request** (sanitize sensitive data)
4. **Complete response** including headers
5. **Environment** (production, staging, etc.)
6. **API client/SDK version** if applicable
7. **Steps to reproduce** the issue

### Example Support Request

```
Subject: 500 Internal Server Error on Certificate Creation

Environment: Production
Timestamp: 2024-02-20T10:30:00Z
Request ID: req_abc123xyz789

Issue Description:
Getting consistent 500 errors when creating certificates with recycled content attributes.

Request Details:
POST /api/v1/certificates
Authorization: Bearer [REDACTED]
Content-Type: application/json

{
  "certificateNumber": "24-TEST-001",
  "companyId": 1,
  "productName": "Test Product",
  "certificateType": "Recycled Content",
  "attributes": {
    "recycledContent": "45%"
  }
}

Response:
HTTP 500 Internal Server Error
{
  "success": false,
  "message": "Database operation failed",
  "errorCode": "SYS_DATABASE_ERROR",
  "details": {
    "requestId": "req_abc123xyz789"
  }
}

This worked fine yesterday and other certificate types are working normally.
```

### Status Page

Monitor API status at: https://status.greencircle.com

- Real-time service status
- Planned maintenance notifications
- Historical uptime data
- Incident reports and resolutions

### Emergency Contacts

For critical production issues:
- **Phone**: +1-555-API-HELP (24/7)
- **Email**: critical-support@greencircle.com
- **Slack**: #api-emergency (for enterprise customers)

---

This comprehensive error guide should help developers quickly identify, diagnose, and resolve API integration issues with the GreenCircle Certified API.