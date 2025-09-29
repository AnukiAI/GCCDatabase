# GCC Web API - Versioning Guide

## Overview

The GCC Web API uses semantic versioning with URL path-based versioning for clear, predictable API evolution. This guide covers versioning strategy, backward compatibility, deprecation policies, and migration procedures.

## Current API Versions

### Version 1.1 (Current)
- **Base URL**: `/api/v1/`
- **Status**: Active, Stable
- **Released**: September 2025
- **Features**:
  - Certificate CRUD operations
  - Advanced search and filtering
  - Bulk operations
  - Health monitoring
  - Rate limiting
  - Response caching
  - Comprehensive error handling

### Version 1.0 (Legacy)
- **Base URL**: `/api/v1/`
- **Status**: Deprecated
- **Released**: August 2025
- **Deprecation Date**: December 2025
- **End of Life**: March 2026

## Versioning Strategy

### URL Path Versioning
```
https://api.greencirclecertified.com/api/v{version}/{resource}
```

**Examples:**
- `https://api.greencirclecertified.com/api/v1/certificates`
- `https://api.greencirclecertified.com/api/v2/certificates` (future)

### Version Format
- **Major.Minor** format (e.g., `v1.1`)
- **Major version** (`v1`, `v2`): Breaking changes
- **Minor version** (`v1.1`, `v1.2`): Backward-compatible additions

### Header-Based Version Information
All responses include version information:
```http
HTTP/1.1 200 OK
X-API-Version: 1.1
X-API-Deprecated: false
X-API-Sunset: 2026-03-01T00:00:00Z
```

## Backward Compatibility

### What We Guarantee
✅ **Stable** (Won't change within major version):
- Existing endpoint URLs
- Required fields in requests
- Response field names and types
- HTTP status codes for success scenarios
- Authentication methods

✅ **Additive** (Safe to add):
- New optional fields in requests
- New fields in responses
- New endpoints
- New HTTP headers
- New query parameters

### What May Change
⚠️ **Potentially Breaking** (Requires new major version):
- Required field additions
- Field name changes
- Data type changes
- Endpoint removals
- Authentication scheme changes
- Error response format changes

## Deprecation Policy

### Deprecation Timeline
1. **Announcement**: 6 months before deprecation
2. **Deprecation**: API marked as deprecated, still functional
3. **End of Life**: API removed, returns 410 Gone

### Deprecation Indicators

#### Response Headers
```http
X-API-Deprecated: true
X-API-Sunset: 2026-03-01T00:00:00Z
Deprecation: Sat, 01 Mar 2026 00:00:00 GMT
Link: <https://api.greencirclecertified.com/api/v2/certificates>; rel="successor-version"
```

#### Response Body
```json
{
  "success": true,
  "data": { ... },
  "warnings": [
    {
      "code": "DEPRECATED_API_VERSION",
      "message": "API version v1.0 is deprecated. Please migrate to v1.1 by March 1, 2026.",
      "details": {
        "deprecationDate": "2025-12-01T00:00:00Z",
        "sunsetDate": "2026-03-01T00:00:00Z",
        "migrationGuide": "https://docs.greencirclecertified.com/api/migration/v1.0-to-v1.1"
      }
    }
  ]
}
```

## Version Detection

### Client Implementation
```javascript
// Check API version support
async function checkApiVersion(baseUrl, version) {
  const response = await fetch(`${baseUrl}/api/${version}/health`);

  if (response.status === 404) {
    console.warn(`API version ${version} not supported`);
    return false;
  }

  const versionHeader = response.headers.get('X-API-Version');
  const deprecated = response.headers.get('X-API-Deprecated') === 'true';
  const sunset = response.headers.get('X-API-Sunset');

  if (deprecated) {
    console.warn(`API version ${version} is deprecated. Sunset: ${sunset}`);
  }

  return { supported: true, version: versionHeader, deprecated, sunset };
}
```

### Automatic Version Negotiation
```javascript
// Preferred version selection
const preferredVersions = ['v2.0', 'v1.1', 'v1.0'];

async function getLatestSupportedVersion(baseUrl) {
  for (const version of preferredVersions) {
    const support = await checkApiVersion(baseUrl, version);
    if (support.supported && !support.deprecated) {
      return version;
    }
  }
  throw new Error('No supported API version found');
}
```

## Migration Guide: v1.0 → v1.1

### Key Changes

#### 1. Enhanced Error Responses
**v1.0:**
```json
{
  "error": "Certificate not found",
  "status": 404
}
```

**v1.1:**
```json
{
  "success": false,
  "errors": [
    {
      "code": "RESOURCE_NOT_FOUND",
      "message": "Certificate not found",
      "field": null
    }
  ],
  "timestamp": "2025-09-18T10:30:00Z",
  "requestId": "abc123",
  "version": "1.1"
}
```

#### 2. Standardized Response Format
**v1.0:** Mixed response formats
**v1.1:** All responses use `StandardApiResponse` format

#### 3. New Features Added
- ✅ Bulk operations (`/bulk/create`, `/bulk/update`, `/bulk/delete`)
- ✅ Health monitoring (`/health`, `/health/ready`, `/health/live`)
- ✅ Rate limiting with headers
- ✅ Response caching
- ✅ Comprehensive error codes

### Migration Steps

#### Step 1: Update Error Handling
```javascript
// Old v1.0 error handling
if (response.status >= 400) {
  const error = await response.json();
  console.error('API Error:', error.error);
}

// New v1.1 error handling
if (!response.ok) {
  const result = await response.json();
  if (!result.success && result.errors) {
    result.errors.forEach(error => {
      console.error(`${error.code}: ${error.message}`);
    });
  }
}
```

#### Step 2: Handle New Response Format
```javascript
// Old v1.0 response handling
const certificates = await response.json();

// New v1.1 response handling
const result = await response.json();
if (result.success) {
  const certificates = result.data;
  console.log('Request ID:', result.requestId);
} else {
  console.error('Request failed:', result.errors);
}
```

#### Step 3: Add Rate Limit Handling
```javascript
// Check rate limit headers
const remaining = response.headers.get('X-RateLimit-Remaining');
const limit = response.headers.get('X-RateLimit-Limit');
const reset = response.headers.get('X-RateLimit-Reset');

if (remaining && parseInt(remaining) < 10) {
  console.warn(`Rate limit approaching: ${remaining}/${limit}`);
}

if (response.status === 429) {
  const retryAfter = response.headers.get('Retry-After');
  console.warn(`Rate limited. Retry after ${retryAfter} seconds`);
}
```

#### Step 4: Leverage New Bulk Operations
```javascript
// Instead of multiple individual requests
const certificates = [cert1, cert2, cert3];
for (const cert of certificates) {
  await createCertificate(cert);
}

// Use bulk operations in v1.1
const result = await fetch('/api/v1/certificates/bulk/create', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(certificates)
});
```

## Future Roadmap

### Version 1.2 (Planned: Q1 2026)
- Enhanced search with full-text capabilities
- Webhook notifications
- Advanced filtering options
- File upload endpoints

### Version 2.0 (Planned: Q3 2026)
- GraphQL support
- Real-time subscriptions
- Improved authentication (OAuth 2.0)
- Enhanced partner integration

## Best Practices

### For API Consumers

#### 1. Version Pinning
```javascript
// Pin to specific version in production
const apiUrl = 'https://api.greencirclecertified.com/api/v1.1';

// Use latest for development/testing
const devApiUrl = 'https://api.greencirclecertified.com/api/latest';
```

#### 2. Graceful Degradation
```javascript
async function getCertificates(options = {}) {
  try {
    // Try latest features first
    return await getCertificatesV11(options);
  } catch (error) {
    if (error.status === 404) {
      // Fallback to older version
      console.warn('Falling back to v1.0 API');
      return await getCertificatesV10(options);
    }
    throw error;
  }
}
```

#### 3. Monitor Deprecation Warnings
```javascript
// Check for deprecation warnings
const deprecated = response.headers.get('X-API-Deprecated');
if (deprecated === 'true') {
  const sunset = response.headers.get('X-API-Sunset');
  // Log to monitoring system
  logger.warn('Using deprecated API', {
    version: response.headers.get('X-API-Version'),
    sunsetDate: sunset,
    endpoint: request.url
  });
}
```

### For API Providers

#### 1. Version Support Matrix
| Version | Status | Support Until | Features |
|---------|--------|---------------|----------|
| v1.1 | Current | TBD | Full feature set |
| v1.0 | Deprecated | March 2026 | Basic operations |

#### 2. Breaking Change Checklist
- [ ] Increment major version number
- [ ] Update migration documentation
- [ ] Provide 6-month deprecation notice
- [ ] Maintain backward compatibility layer
- [ ] Update client SDKs
- [ ] Notify all registered developers

## Testing Across Versions

### Postman Environment Setup
```json
{
  "environments": [
    {
      "name": "GCC API v1.1",
      "values": [
        { "key": "base_url", "value": "https://api.greencirclecertified.com" },
        { "key": "api_version", "value": "v1" },
        { "key": "expected_version", "value": "1.1" }
      ]
    },
    {
      "name": "GCC API v1.0 (Legacy)",
      "values": [
        { "key": "base_url", "value": "https://api.greencirclecertified.com" },
        { "key": "api_version", "value": "v1" },
        { "key": "expected_version", "value": "1.0" }
      ]
    }
  ]
}
```

### Automated Version Testing
```bash
#!/bin/bash
# Test script for version compatibility

versions=("v1.0" "v1.1")
endpoints=("/certificates" "/products" "/health")

for version in "${versions[@]}"; do
  echo "Testing API version: $version"
  for endpoint in "${endpoints[@]}"; do
    response=$(curl -s -w "%{http_code}" \
      "https://api.greencirclecertified.com/api/$version$endpoint" \
      -H "X-API-Key: $API_KEY")

    http_code="${response: -3}"
    if [ "$http_code" -eq 200 ]; then
      echo "✅ $version$endpoint - OK"
    else
      echo "❌ $version$endpoint - Failed ($http_code)"
    fi
  done
done
```

## Support and Documentation

### Resources
- **API Documentation**: https://docs.greencirclecertified.com/api/
- **Migration Guides**: https://docs.greencirclecertified.com/api/migrations/
- **Status Page**: https://status.greencirclecertified.com
- **Developer Portal**: https://developers.greencirclecertified.com

### Getting Help
- **Email Support**: api-support@greencirclecertified.com
- **Developer Forums**: https://community.greencirclecertified.com
- **GitHub Issues**: https://github.com/gcc/api-issues
- **Slack Channel**: #api-support (for partners)

### Notifications
- **Breaking Changes**: 6-month advance notice via email
- **New Features**: Monthly newsletter
- **Status Updates**: Real-time via status page
- **Deprecation Alerts**: In-response warnings + email

---

*Last Updated: September 18, 2025*
*Document Version: 1.0*