# 🚀 GCC Database Solution - API Integration Guide

## Partner API Integration (Amazon, Wayfair & Third-Party Systems)

### 📋 **Integration Status: PRODUCTION READY**

The GCC Database Solution API is **fully implemented** and **production-ready** for partner integration with comprehensive security, filtering, and monitoring capabilities.

---

## 🏗️ **API Architecture Overview**

### **Multi-Tier API Structure**
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Public API    │    │   Partner API    │    │   Admin API     │
│   (Read-Only)   │    │  (Filtered)      │    │  (Full CRUD)    │
├─────────────────┤    ├──────────────────┤    ├─────────────────┤
│ • Certificate   │    │ • Amazon Filter  │    │ • User Mgmt     │
│   Browsing      │    │ • Wayfair Filter │    │ • Certificate   │
│ • Public Search │    │ • API Keys       │    │   Management    │
│ • Downloads     │    │ • Rate Limited   │    │ • Admin Tools   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

### **Authentication Methods**
1. **API Key** (Recommended for Partners)
2. **JWT Bearer Token** (Standard OAuth-style)
3. **Cookie Authentication** (Web interface backup)

---

## 🔐 **Security Implementation**

### **Multi-Layer Security**
- **API Key Management**: Rotation, expiration, violation tracking
- **Rate Limiting**: 1000 calls/hour per partner (configurable)
- **IP Whitelisting**: Restrict to partner server IPs
- **Origin Validation**: Domain-specific access control
- **Request Logging**: Full audit trail with structured logging
- **Security Headers**: OWASP-compliant security middleware

### **Partner Data Isolation**
```sql
-- Amazon only sees:
WHERE (Certificate.Partner = 'Amazon' OR Certificate.Partner IS NULL)

-- Wayfair only sees:
WHERE (Certificate.Partner = 'Wayfair' OR Certificate.Partner IS NULL)

-- Public users see:
WHERE Certificate.Partner IS NULL
```

---

## 📡 **Partner API Endpoints**

### **Amazon Integration Endpoints**
```http
# Base URL: https://api.greencirclecertified.com (Production)
# Base URL: http://localhost:5101 (Development)

# Certificate Access
GET    /api/CertificateTileExternal/tiles
GET    /api/CertificateTileExternal/suggestions?q={search}
GET    /api/CertificateTileExternal/filters
GET    /api/CertificateTileExternal/certificate/{id}
POST   /api/CertificateTileExternal/export

# Authentication
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
```

### **Standard API Response Format**
```json
{
  "success": true,
  "data": {
    "certificates": [...],
    "totalCount": 150,
    "filteredCount": 25
  },
  "message": "Optional success message",
  "errors": [],
  "timestamp": "2025-09-08T15:30:00Z",
  "requestId": "unique-request-id",
  "version": "1.0"
}
```

### **Error Response Format**
```json
{
  "success": false,
  "data": null,
  "message": "Authentication required",
  "errors": [
    {
      "code": "UNAUTHORIZED",
      "message": "API key is invalid or expired",
      "field": null,
      "details": {
        "timestamp": "2025-09-08T15:30:00Z",
        "requestId": "unique-request-id"
      }
    }
  ],
  "timestamp": "2025-09-08T15:30:00Z",
  "requestId": "unique-request-id",
  "version": "1.0"
}
```

---

## 🏪 **Amazon Integration Guide**

### **1. Getting Started**

#### **Test Account Setup**
```bash
# Amazon Test Credentials
Email: partner@amazon.com
Password: Partner123!
Role: External (Partner Access)
```

#### **API Key Creation**
```bash
# Login as admin user first, then:
POST /Admin/CreateApiKey
Content-Type: application/json

{
  "name": "Amazon Marketplace Integration",
  "description": "Amazon certificate data access",
  "allowedOrigins": "*.amazon.com,*.amazonservices.com",
  "allowedIPs": "54.240.0.0/16,205.251.0.0/16", # Amazon IP ranges
  "rateLimit": 1000,
  "expiresAt": "2025-12-31T23:59:59Z"
}
```

### **2. Authentication Examples**

#### **API Key Authentication (Recommended)**
```bash
curl -X GET "https://api.greencirclecertified.com/api/CertificateTileExternal/tiles" \
     -H "X-API-Key: amazon-api-key-here" \
     -H "Accept: application/json"
```

#### **JWT Token Authentication**
```bash
# 1. Get JWT Token
curl -X POST "https://api.greencirclecertified.com/api/v1/auth/login" \
     -H "Content-Type: application/json" \
     -d '{
       "username": "partner@amazon.com",
       "password": "Partner123!"
     }'

# Response:
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "refresh-token-here",
    "expiresIn": 3600,
    "user": {
      "username": "partner@amazon.com",
      "email": "partner@amazon.com",
      "role": "External"
    }
  }
}

# 2. Use JWT Token
curl -X GET "https://api.greencirclecertified.com/api/CertificateTileExternal/tiles" \
     -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
     -H "Accept: application/json"
```

### **3. Certificate Data Access**

#### **Get Amazon's Certificates**
```bash
curl -X GET "https://api.greencirclecertified.com/api/CertificateTileExternal/tiles" \
     -H "X-API-Key: amazon-key" \
     -H "Accept: application/json"

# Optional filters:
# ?search=product-name
# ?company=123
# ?status=active
# ?country=USA
# ?page=1&pageSize=50
```

#### **Search with Autocomplete**
```bash
curl -X GET "https://api.greencirclecertified.com/api/CertificateTileExternal/suggestions?q=green" \
     -H "X-API-Key: amazon-key" \
     -H "Accept: application/json"
```

#### **Export Certificate Data**
```bash
curl -X POST "https://api.greencirclecertified.com/api/CertificateTileExternal/export" \
     -H "X-API-Key: amazon-key" \
     -H "Content-Type: application/json" \
     -d '{
       "format": "csv",
       "searchTerm": "",
       "companyId": null,
       "userRole": "External"
     }' --output amazon-certificates.csv
```

### **4. Error Handling**

#### **Rate Limit Handling**
```javascript
// JavaScript example for handling rate limits
async function callGCCAPI(endpoint, options = {}) {
  try {
    const response = await fetch(endpoint, {
      ...options,
      headers: {
        'X-API-Key': 'amazon-api-key',
        'Accept': 'application/json',
        ...options.headers
      }
    });

    if (response.status === 429) {
      // Rate limit exceeded
      const retryAfter = response.headers.get('Retry-After') || 60;
      console.log(`Rate limited. Retry after ${retryAfter} seconds`);
      await new Promise(resolve => setTimeout(resolve, retryAfter * 1000));
      return callGCCAPI(endpoint, options); // Retry
    }

    return await response.json();
  } catch (error) {
    console.error('GCC API Error:', error);
    throw error;
  }
}
```

#### **Common Error Codes**
| HTTP Code | Error Code | Description | Action |
|-----------|------------|-------------|---------|
| 401 | UNAUTHORIZED | Invalid/expired API key | Refresh API key |
| 403 | FORBIDDEN | IP/Origin blocked | Check whitelist |
| 429 | RATE_LIMIT_EXCEEDED | Too many requests | Wait and retry |
| 404 | RESOURCE_NOT_FOUND | Certificate not found | Check ID |
| 500 | INTERNAL_SERVER_ERROR | Server error | Retry later |

---

## 🧪 **Testing & Development**

### **Development Environment**
```bash
# Local development server
Base URL: http://localhost:5101

# Swagger documentation
http://localhost:5101/swagger

# Test data available
# Health check
curl http://localhost:5101/health
```

### **Test Certificate Data**
The development environment includes test certificates with various partner tags:
- Amazon-tagged certificates
- Wayfair-tagged certificates  
- Public certificates (no partner tag)
- Various product categories and companies

### **API Testing Tools**

#### **Postman Collection**
```json
{
  "info": {
    "name": "GCC Partner API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "auth": {
    "type": "apikey",
    "apikey": [
      {"key": "key", "value": "X-API-Key"},
      {"key": "value", "value": "{{amazon_api_key}}"}
    ]
  },
  "item": [
    {
      "name": "Get Amazon Certificates",
      "request": {
        "method": "GET",
        "header": [],
        "url": "{{base_url}}/api/CertificateTileExternal/tiles"
      }
    }
  ]
}
```

#### **cURL Scripts**
```bash
#!/bin/bash
# test-amazon-api.sh

API_KEY="your-amazon-api-key"
BASE_URL="http://localhost:5101"

echo "Testing Amazon API Integration..."

# Test 1: Get certificates
echo "1. Getting Amazon certificates..."
curl -s -X GET "$BASE_URL/api/CertificateTileExternal/tiles" \
     -H "X-API-Key: $API_KEY" | jq .

# Test 2: Search suggestions
echo "2. Testing search suggestions..."
curl -s -X GET "$BASE_URL/api/CertificateTileExternal/suggestions?q=green" \
     -H "X-API-Key: $API_KEY" | jq .

# Test 3: Get specific certificate
echo "3. Getting specific certificate..."
curl -s -X GET "$BASE_URL/api/CertificateTileExternal/certificate/1" \
     -H "X-API-Key: $API_KEY" | jq .

echo "Amazon API testing complete!"
```

---

## 🚀 **Production Deployment**

### **Environment Requirements**

#### **Required Configuration**
```bash
# Environment Variables (Production)
GCC_JWT_SECRET=64-character-secure-random-string
DATABASE_CONNECTION_STRING="Server=prod;Database=GCC;User Id=app;Password=***;Encrypt=true;"

# Optional but Recommended
GCC_AZURE_AD_B2C_CLIENT_SECRET=azure-b2c-secret
GCC_APPLICATION_INSIGHTS_CONNECTION_STRING=app-insights
GCC_SMTP_PASSWORD=email-password
```

#### **Production URLs**
```bash
# API Base URL
https://api.greencirclecertified.com

# Swagger Documentation
https://api.greencirclecertified.com/swagger (if enabled)

# Health Check
https://api.greencirclecertified.com/health
```

### **Deployment Options**

#### **Docker Deployment**
```dockerfile
# Use existing Dockerfile in project root
docker build -t gcc-webapp:latest .

# Production container
docker run -d \
  --name gcc-webapp-prod \
  -p 443:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e GCC_JWT_SECRET="${JWT_SECRET}" \
  -e DATABASE_CONNECTION_STRING="${DB_CONNECTION}" \
  -v /path/to/ssl:/https \
  gcc-webapp:latest
```

#### **Azure App Service**
```bash
# Deploy to Azure App Service
az webapp create \
  --resource-group gcc-rg \
  --plan gcc-app-plan \
  --name gcc-api \
  --deployment-container-image-name gcc-webapp:latest

# Configure environment variables
az webapp config appsettings set \
  --resource-group gcc-rg \
  --name gcc-api \
  --settings GCC_JWT_SECRET="${JWT_SECRET}"
```

### **Production Checklist**

#### ✅ **Application Ready**
- [x] API controllers implemented and tested
- [x] Authentication systems configured  
- [x] Partner filtering working
- [x] Rate limiting configured
- [x] Security middleware active
- [x] Error handling comprehensive
- [x] Logging and monitoring ready
- [x] Docker containerization ready

#### 🔧 **Configuration Needed**
- [ ] JWT secret key (64+ characters)
- [ ] Production database connection
- [ ] SSL certificate for HTTPS
- [ ] Partner API keys generated
- [ ] Partner IP whitelist configured
- [ ] Monitoring alerts configured

#### **Estimated Deployment Time**: 2-4 hours (configuration only)

---

## 📊 **Monitoring & Analytics**

### **API Metrics**
- Request volume per partner
- Response times by endpoint  
- Error rates and types
- Rate limit violations
- Security incidents

### **Available Logs**
- Request/response logging
- Authentication events
- Rate limit violations  
- API key usage tracking
- Partner-specific metrics

### **Health Monitoring**
```bash
# Health check endpoint
GET /health

# Returns:
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "cache": "Healthy", 
    "external_services": "Healthy"
  },
  "timestamp": "2025-09-08T15:30:00Z"
}
```

---

## 📞 **Support & Contact**

### **Technical Support**
- **Email**: api-support@greencirclecertified.com
- **Documentation**: https://docs.greencirclecertified.com/api
- **Status Page**: https://status.greencirclecertified.com

### **Integration Support for Partners**
- **Amazon Integration**: amazon-integration@greencirclecertified.com  
- **API Key Requests**: api-keys@greencirclecertified.com
- **Emergency Support**: emergency@greencirclecertified.com (24/7)

---

## 🎯 **Conclusion**

The GCC Database Solution API is **production-ready** and **fully functional** for Amazon and other partner integrations. The implementation includes:

- ✅ **Complete API functionality** with CRUD operations
- ✅ **Partner-specific data filtering** and access control
- ✅ **Enterprise-grade security** with multiple authentication methods
- ✅ **Comprehensive monitoring** and logging capabilities  
- ✅ **Production-ready deployment** options (Docker, Azure, etc.)
- ✅ **Full documentation** and testing tools

**Partners can begin integration development immediately** using the development environment, with seamless transition to production requiring only configuration changes.

---

*Document Version: 1.0*  
*Last Updated: September 8, 2025*  
*Next Review: October 8, 2025*