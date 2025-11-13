# GCC Partner API Integration Guide

**Version**: 1.0
**Last Updated**: November 12, 2025
**Target Audience**: Partner Developers (Amazon, Wayfair, etc.)

---

## Table of Contents

1. [Introduction](#introduction)
2. [Authentication](#authentication)
3. [API Endpoints](#api-endpoints)
4. [Making Requests](#making-requests)
5. [Response Format](#response-format)
6. [Error Handling](#error-handling)
7. [Rate Limiting](#rate-limiting)
8. [Code Examples](#code-examples)
9. [Best Practices](#best-practices)
10. [Support](#support)

---

## Introduction

### Overview

The GCC Partner API provides secure, read-only access to certificate data for authorized external partners. This RESTful API allows partners to retrieve certificate information programmatically.

### Base URL

```
Production: https://api.greencirclecertified.com
Staging:    https://staging-api.greencirclecertified.com
```

### API Version

Current version: **v1**

All endpoints are prefixed with `/api/v1/`

---

## Authentication

### API Key Authentication

All API requests must include your partner API key in the `Authorization` header using the Bearer token scheme.

#### Your API Key Format

```
gcc_partner_[64 character hexadecimal string]
```

#### Authentication Header

```http
Authorization: Bearer gcc_partner_a3f5c9d8e2b4f7a1c8d6e9f2b5a8c3d7e1f4a9b2c5d8e3f6a9b4c7d2e5f8a1b6c9
```

### Security Requirements

✅ **DO**:
- Store your API key securely (use environment variables or secure vaults)
- Use HTTPS for all requests
- Rotate your API key every 90 days
- Keep your key confidential

❌ **DON'T**:
- Hard-code your API key in source files
- Commit your API key to version control
- Share your API key with unauthorized parties
- Use HTTP (unencrypted) connections

### Example: Secure Key Storage

**Environment Variable (.env file)**:
```bash
GCC_API_KEY=gcc_partner_a3f5c9d8e2b4f7a1c8d6e9f2b5a8c3d7e1f4a9b2c5d8e3f6a9b4c7d2e5f8a1b6c9
```

**Loading in Code**:
```javascript
// Node.js
require('dotenv').config();
const apiKey = process.env.GCC_API_KEY;
```

```python
# Python
import os
from dotenv import load_dotenv

load_dotenv()
api_key = os.getenv('GCC_API_KEY')
```

---

## API Endpoints

### Available Endpoints

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/api/v1/certificates` | GET | List all certificates | Yes |
| `/api/v1/certificates/{id}` | GET | Get specific certificate | Yes |
| `/api/v1/certificates/search` | GET | Search certificates | Yes |
| `/api/v1/products` | GET | List products | Yes |
| `/api/v1/products/{id}` | GET | Get specific product | Yes |

### Endpoint Details

#### GET /api/v1/certificates

Retrieve a list of certificates with optional filtering.

**Query Parameters**:
- `page` (optional): Page number for pagination (default: 1)
- `limit` (optional): Results per page (default: 50, max: 100)
- `partner` (optional): Filter by partner name (e.g., "Amazon", "Wayfair")
- `status` (optional): Filter by status (e.g., "Active", "Expired")
- `product_id` (optional): Filter by product ID

**Example Request**:
```http
GET /api/v1/certificates?partner=Amazon&status=Active&page=1&limit=50
Authorization: Bearer gcc_partner_[your_key]
```

**Example Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "certificateNumber": "24-0001",
      "productName": "Sustainable Wood Chair",
      "company": "EcoFurniture Inc.",
      "partner": "Amazon",
      "status": "Active",
      "issueDate": "2024-01-15T00:00:00Z",
      "expiryDate": "2025-01-15T00:00:00Z",
      "pdfUrl": "https://api.greencirclecertified.com/api/v1/certificates/1/download",
      "productIds": [
        {
          "type": "ASIN",
          "value": "B08XYZ1234"
        }
      ]
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 50,
    "total": 247,
    "totalPages": 5
  }
}
```

#### GET /api/v1/certificates/{id}

Retrieve details for a specific certificate.

**Path Parameters**:
- `id`: Certificate ID

**Example Request**:
```http
GET /api/v1/certificates/1
Authorization: Bearer gcc_partner_[your_key]
```

**Example Response**:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "certificateNumber": "24-0001",
    "productName": "Sustainable Wood Chair",
    "company": "EcoFurniture Inc.",
    "partner": "Amazon",
    "status": "Active",
    "issueDate": "2024-01-15T00:00:00Z",
    "expiryDate": "2025-01-15T00:00:00Z",
    "certificationTypes": [
      {
        "name": "FSC Certified",
        "standard": "FSC-C123456"
      }
    ],
    "productIds": [
      {
        "type": "ASIN",
        "value": "B08XYZ1234"
      },
      {
        "type": "UPC",
        "value": "012345678901"
      }
    ],
    "tags": [
      "furniture",
      "wood",
      "sustainable"
    ],
    "pdfUrl": "https://api.greencirclecertified.com/api/v1/certificates/1/download"
  }
}
```

#### GET /api/v1/certificates/search

Search certificates by product ID (ASIN, UPC, etc.).

**Query Parameters**:
- `productId` (required): Product identifier to search for
- `type` (optional): Product ID type (ASIN, UPC, GTIN, etc.)

**Example Request**:
```http
GET /api/v1/certificates/search?productId=B08XYZ1234&type=ASIN
Authorization: Bearer gcc_partner_[your_key]
```

**Example Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "certificateNumber": "24-0001",
      "productName": "Sustainable Wood Chair",
      "status": "Active",
      "pdfUrl": "https://api.greencirclecertified.com/api/v1/certificates/1/download"
    }
  ]
}
```

---

## Making Requests

### Request Headers

Always include these headers in your requests:

```http
Authorization: Bearer gcc_partner_[your_key]
Accept: application/json
Content-Type: application/json
User-Agent: YourCompany/1.0
```

### HTTPS Only

All API requests must use HTTPS. HTTP requests will be rejected with a 403 Forbidden error.

### Request Method

Supported methods:
- `GET`: Retrieve data (read-only access)

### Query String Parameters

URL-encode all query parameters:

```javascript
// JavaScript example
const params = new URLSearchParams({
  partner: 'Amazon',
  status: 'Active',
  page: 1
});

const url = `https://api.greencirclecertified.com/api/v1/certificates?${params}`;
```

---

## Response Format

### Standard Response Structure

All API responses follow this structure:

```json
{
  "success": true|false,
  "data": { ... } | [ ... ],
  "message": "Optional success/error message",
  "errors": [ ... ] // Only present if success is false
}
```

### Success Response

```json
{
  "success": true,
  "data": {
    "id": 1,
    "certificateNumber": "24-0001",
    ...
  }
}
```

### Paginated Response

```json
{
  "success": true,
  "data": [ ... ],
  "pagination": {
    "page": 1,
    "limit": 50,
    "total": 247,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPrevPage": false
  }
}
```

### HTTP Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Request successful |
| 400 | Bad Request | Invalid request parameters |
| 401 | Unauthorized | Missing or invalid API key |
| 403 | Forbidden | API key valid but lacks permission |
| 404 | Not Found | Resource does not exist |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error (contact support) |

---

## Error Handling

### Error Response Format

```json
{
  "success": false,
  "message": "Authentication failed",
  "errorCode": "AUTH_INVALID_KEY",
  "errors": [
    {
      "field": "Authorization",
      "message": "API key is invalid or has been revoked"
    }
  ]
}
```

### Common Error Codes

| Error Code | HTTP Status | Description | Solution |
|------------|-------------|-------------|----------|
| `AUTH_MISSING_KEY` | 401 | Authorization header missing | Include API key in header |
| `AUTH_INVALID_KEY` | 401 | API key is invalid or revoked | Verify key or request new one |
| `AUTH_EXPIRED_KEY` | 401 | API key has expired | Request a new API key |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests | Slow down request rate |
| `RESOURCE_NOT_FOUND` | 404 | Certificate/product not found | Verify resource ID |
| `INVALID_PARAMS` | 400 | Invalid query parameters | Check parameter format |
| `INTERNAL_ERROR` | 500 | Server error | Contact GCC support |

### Handling Errors in Code

**JavaScript Example**:
```javascript
try {
  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${apiKey}`,
      'Accept': 'application/json'
    }
  });

  const data = await response.json();

  if (!response.ok) {
    // Handle HTTP errors
    console.error(`HTTP ${response.status}: ${data.message}`);

    if (response.status === 401) {
      // Authentication error - check API key
    } else if (response.status === 429) {
      // Rate limit - implement backoff
    }
    throw new Error(data.message);
  }

  if (!data.success) {
    // Handle API errors
    console.error('API Error:', data.errors);
    throw new Error(data.message);
  }

  // Success - use data
  return data.data;

} catch (error) {
  console.error('Request failed:', error);
  throw error;
}
```

**Python Example**:
```python
import requests

try:
    response = requests.get(
        url,
        headers={
            'Authorization': f'Bearer {api_key}',
            'Accept': 'application/json'
        }
    )

    # Raise HTTP errors
    response.raise_for_status()

    data = response.json()

    if not data['success']:
        print(f"API Error: {data['message']}")
        raise Exception(data['message'])

    # Success - use data
    return data['data']

except requests.exceptions.HTTPError as e:
    if e.response.status_code == 401:
        print("Authentication failed - check API key")
    elif e.response.status_code == 429:
        print("Rate limit exceeded - slow down")
    raise

except Exception as e:
    print(f"Request failed: {e}")
    raise
```

---

## Rate Limiting

### Limits

- **Default**: 100 requests per hour per API key
- **Burst**: Up to 10 requests per minute

### Rate Limit Headers

Check these response headers to monitor your usage:

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 87
X-RateLimit-Reset: 1699459200
```

| Header | Description |
|--------|-------------|
| `X-RateLimit-Limit` | Max requests allowed per hour |
| `X-RateLimit-Remaining` | Requests remaining in current window |
| `X-RateLimit-Reset` | Unix timestamp when limit resets |

### Handling Rate Limits

**Implement Exponential Backoff**:

```javascript
async function fetchWithBackoff(url, options, maxRetries = 3) {
  for (let i = 0; i < maxRetries; i++) {
    const response = await fetch(url, options);

    if (response.status !== 429) {
      return response;
    }

    // Rate limited - wait and retry
    const retryAfter = response.headers.get('Retry-After') || (2 ** i);
    console.log(`Rate limited. Retrying in ${retryAfter}s...`);
    await new Promise(resolve => setTimeout(resolve, retryAfter * 1000));
  }

  throw new Error('Max retries exceeded');
}
```

**Check Headers Before Continuing**:

```python
import time

def check_rate_limit(response):
    remaining = int(response.headers.get('X-RateLimit-Remaining', 100))
    reset_time = int(response.headers.get('X-RateLimit-Reset', 0))

    if remaining < 10:
        # Low on quota - wait until reset
        wait_time = reset_time - time.time()
        if wait_time > 0:
            print(f"Rate limit low. Waiting {wait_time}s...")
            time.sleep(wait_time)
```

---

## Code Examples

### JavaScript (Node.js)

```javascript
const axios = require('axios');
require('dotenv').config();

const API_BASE_URL = 'https://api.greencirclecertified.com/api/v1';
const API_KEY = process.env.GCC_API_KEY;

// Configure axios instance
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Authorization': `Bearer ${API_KEY}`,
    'Accept': 'application/json',
    'User-Agent': 'AmazonIntegration/1.0'
  }
});

// Get all active certificates for Amazon
async function getActiveCertificates() {
  try {
    const response = await api.get('/certificates', {
      params: {
        partner: 'Amazon',
        status: 'Active',
        limit: 100
      }
    });

    if (response.data.success) {
      console.log(`Found ${response.data.data.length} certificates`);
      return response.data.data;
    } else {
      throw new Error(response.data.message);
    }
  } catch (error) {
    console.error('Error fetching certificates:', error.message);
    throw error;
  }
}

// Search for certificate by ASIN
async function findCertificateByASIN(asin) {
  try {
    const response = await api.get('/certificates/search', {
      params: {
        productId: asin,
        type: 'ASIN'
      }
    });

    if (response.data.success && response.data.data.length > 0) {
      return response.data.data[0];
    } else {
      return null;
    }
  } catch (error) {
    console.error(`Error searching for ASIN ${asin}:`, error.message);
    return null;
  }
}

// Download certificate PDF
async function downloadCertificatePDF(certificateId, savePath) {
  try {
    const response = await api.get(`/certificates/${certificateId}/download`, {
      responseType: 'arraybuffer'
    });

    const fs = require('fs');
    fs.writeFileSync(savePath, Buffer.from(response.data));
    console.log(`Certificate saved to ${savePath}`);
  } catch (error) {
    console.error('Error downloading certificate:', error.message);
    throw error;
  }
}

// Usage
(async () => {
  try {
    // Get all active certificates
    const certificates = await getActiveCertificates();

    // Search for specific ASIN
    const cert = await findCertificateByASIN('B08XYZ1234');
    if (cert) {
      console.log(`Found certificate: ${cert.certificateNumber}`);

      // Download PDF
      await downloadCertificatePDF(cert.id, `./certificates/${cert.certificateNumber}.pdf`);
    }
  } catch (error) {
    process.exit(1);
  }
})();
```

### Python

```python
import requests
import os
from typing import List, Optional, Dict
from dotenv import load_dotenv

load_dotenv()

API_BASE_URL = 'https://api.greencirclecertified.com/api/v1'
API_KEY = os.getenv('GCC_API_KEY')

class GCCApiClient:
    def __init__(self, api_key: str, base_url: str = API_BASE_URL):
        self.base_url = base_url
        self.session = requests.Session()
        self.session.headers.update({
            'Authorization': f'Bearer {api_key}',
            'Accept': 'application/json',
            'User-Agent': 'AmazonIntegration/1.0'
        })

    def get_certificates(self, partner: str = None, status: str = None,
                        page: int = 1, limit: int = 50) -> List[Dict]:
        """Get list of certificates with optional filtering."""
        params = {'page': page, 'limit': limit}
        if partner:
            params['partner'] = partner
        if status:
            params['status'] = status

        response = self.session.get(
            f'{self.base_url}/certificates',
            params=params
        )
        response.raise_for_status()

        data = response.json()
        if data['success']:
            return data['data']
        else:
            raise Exception(data['message'])

    def search_by_product_id(self, product_id: str,
                            product_type: str = 'ASIN') -> Optional[Dict]:
        """Search for certificate by product ID (ASIN, UPC, etc.)."""
        params = {
            'productId': product_id,
            'type': product_type
        }

        response = self.session.get(
            f'{self.base_url}/certificates/search',
            params=params
        )
        response.raise_for_status()

        data = response.json()
        if data['success'] and len(data['data']) > 0:
            return data['data'][0]
        return None

    def download_certificate(self, certificate_id: int, save_path: str):
        """Download certificate PDF."""
        response = self.session.get(
            f'{self.base_url}/certificates/{certificate_id}/download',
            stream=True
        )
        response.raise_for_status()

        with open(save_path, 'wb') as f:
            for chunk in response.iter_content(chunk_size=8192):
                f.write(chunk)

        print(f'Certificate saved to {save_path}')

# Usage
if __name__ == '__main__':
    client = GCCApiClient(API_KEY)

    # Get all active certificates for Amazon
    certificates = client.get_certificates(
        partner='Amazon',
        status='Active',
        limit=100
    )
    print(f'Found {len(certificates)} active certificates')

    # Search for specific ASIN
    cert = client.search_by_product_id('B08XYZ1234', 'ASIN')
    if cert:
        print(f"Found certificate: {cert['certificateNumber']}")

        # Download PDF
        client.download_certificate(
            cert['id'],
            f"./certificates/{cert['certificateNumber']}.pdf"
        )
```

### cURL

```bash
#!/bin/bash

# Set your API key
API_KEY="gcc_partner_your_key_here"
BASE_URL="https://api.greencirclecertified.com/api/v1"

# Get all active certificates
curl -X GET \
  "${BASE_URL}/certificates?partner=Amazon&status=Active&limit=50" \
  -H "Authorization: Bearer ${API_KEY}" \
  -H "Accept: application/json" \
  -H "User-Agent: AmazonIntegration/1.0"

# Search by ASIN
curl -X GET \
  "${BASE_URL}/certificates/search?productId=B08XYZ1234&type=ASIN" \
  -H "Authorization: Bearer ${API_KEY}" \
  -H "Accept: application/json"

# Download certificate PDF
curl -X GET \
  "${BASE_URL}/certificates/1/download" \
  -H "Authorization: Bearer ${API_KEY}" \
  --output certificate_24-0001.pdf
```

---

## Best Practices

### 1. Caching

Implement caching to reduce API calls:

```javascript
const NodeCache = require('node-cache');
const cache = new NodeCache({ stdTTL: 3600 }); // 1 hour TTL

async function getCertificateWithCache(id) {
  const cacheKey = `cert_${id}`;

  // Check cache first
  const cached = cache.get(cacheKey);
  if (cached) {
    return cached;
  }

  // Fetch from API
  const cert = await fetchCertificate(id);

  // Store in cache
  cache.set(cacheKey, cert);

  return cert;
}
```

### 2. Batch Processing

Process certificates in batches to respect rate limits:

```python
import time

def process_certificates_batch(asins: List[str], batch_size: int = 10):
    for i in range(0, len(asins), batch_size):
        batch = asins[i:i+batch_size]

        for asin in batch:
            cert = client.search_by_product_id(asin)
            # Process certificate...

        # Wait between batches to avoid rate limiting
        if i + batch_size < len(asins):
            time.sleep(1)  # 1 second between batches
```

### 3. Error Logging

Log all API interactions for debugging:

```javascript
const winston = require('winston');

const logger = winston.createLogger({
  level: 'info',
  format: winston.format.json(),
  transports: [
    new winston.transports.File({ filename: 'api-error.log', level: 'error' }),
    new winston.transports.File({ filename: 'api-combined.log' })
  ]
});

async function fetchWithLogging(url, options) {
  logger.info('API Request', { url, method: 'GET' });

  try {
    const response = await fetch(url, options);
    logger.info('API Response', {
      url,
      status: response.status,
      remaining: response.headers.get('X-RateLimit-Remaining')
    });
    return response;
  } catch (error) {
    logger.error('API Error', { url, error: error.message });
    throw error;
  }
}
```

### 4. Retry Logic

Implement smart retry logic with exponential backoff:

```python
import time
from functools import wraps

def retry_with_backoff(max_retries=3, backoff_factor=2):
    def decorator(func):
        @wraps(func)
        def wrapper(*args, **kwargs):
            for attempt in range(max_retries):
                try:
                    return func(*args, **kwargs)
                except requests.exceptions.HTTPError as e:
                    if e.response.status_code == 429:
                        # Rate limited
                        if attempt < max_retries - 1:
                            wait_time = backoff_factor ** attempt
                            print(f"Rate limited. Waiting {wait_time}s...")
                            time.sleep(wait_time)
                            continue
                    raise
            return None
        return wrapper
    return decorator

@retry_with_backoff(max_retries=3)
def fetch_certificate(cert_id):
    return client.get_certificate(cert_id)
```

### 5. Webhook Integration (Future)

When webhooks become available, use them instead of polling:

```javascript
// Future: Webhook handler
app.post('/webhooks/gcc-certificates', (req, res) => {
  const event = req.body;

  switch (event.type) {
    case 'certificate.created':
      handleNewCertificate(event.data);
      break;
    case 'certificate.updated':
      handleUpdatedCertificate(event.data);
      break;
    case 'certificate.expired':
      handleExpiredCertificate(event.data);
      break;
  }

  res.sendStatus(200);
});
```

---

## Support

### Getting Help

**Technical Support**:
- Email: api-support@greencirclecertified.com
- Response Time: 24-48 hours (business days)

**Documentation**:
- API Reference: https://docs.greencirclecertified.com/api
- Status Page: https://status.greencirclecertified.com

**Requesting Features**:
Contact your GCC account manager to discuss feature requests or custom integrations.

### Reporting Issues

When reporting API issues, include:

1. **Your Partner Name**
2. **API Key** (last 8 characters only - e.g., `...1b6c9`)
3. **Request Details**:
   - Endpoint called
   - Request parameters
   - Request timestamp
4. **Response Details**:
   - HTTP status code
   - Error message
   - Response body (if applicable)
5. **Error Logs**:
   - Stack traces
   - Console output

### Changelog

Subscribe to API updates:
- Email: api-announcements@greencirclecertified.com
- RSS Feed: https://docs.greencirclecertified.com/api/changelog.xml

---

**Document Information**

- **Version**: 1.0
- **Release Date**: November 12, 2025
- **Author**: GCC Development Team
- **License**: Proprietary - For authorized partners only

---

© 2025 GreenCircle Certified. All rights reserved.

This document is confidential and intended only for authorized partners. Unauthorized distribution is prohibited.
