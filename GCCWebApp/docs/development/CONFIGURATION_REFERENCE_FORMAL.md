# GCC Database Solution
## Configuration Reference Manual

**Document Information**
- **Document Type**: Technical Configuration Manual
- **Version**: 1.0
- **Generated**: September 18, 2025
- **Classification**: CONFIDENTIAL
- **Prepared for**: Green Circle Certified Development Team

---

## Executive Summary

This document provides a comprehensive reference for all configuration settings, credentials, and environment variables used in the GCC Database Solution. It serves as the authoritative source for system configuration across all environments.

**Important Security Notice**: This document contains sensitive information including passwords, API keys, and connection strings. Ensure appropriate access controls and secure handling.

---

## Table of Contents

1. [Authentication & Security Configuration](#1-authentication--security-configuration)
2. [Database Configuration](#2-database-configuration)
3. [Environment-Specific Settings](#3-environment-specific-settings)
4. [API Configuration](#4-api-configuration)
5. [Third-Party Service Integration](#5-third-party-service-integration)
6. [Docker & Container Configuration](#6-docker--container-configuration)
7. [Production Deployment Configuration](#7-production-deployment-configuration)
8. [Security & Compliance Settings](#8-security--compliance-settings)

---

## 1. Authentication & Security Configuration

### 1.1 JWT (JSON Web Token) Configuration

#### Development Environment
```json
{
  "Jwt": {
    "Key": "ThisIsAVeryLongSecretKeyForJwtTokenGenerationAtLeast64CharactersLong123456789",
    "Issuer": "GCCWebApp",
    "Audience": "GCCWebApp",
    "ExpiryMinutes": 60
  }
}
```

#### Production Environment
```json
{
  "Jwt": {
    "Key": "your-super-secret-jwt-key-must-be-at-least-64-characters-long-for-security",
    "Issuer": "GCCWebApp-Production",
    "Audience": "GCCWebApp-Production",
    "ExpiryMinutes": 15
  }
}
```

#### Test Environment
```json
{
  "Jwt": {
    "Key": "YOUR_TEST_JWT_SECRET_KEY_MIN_64_CHARS_FOR_SECURITY_REQUIREMENTS_HERE",
    "Issuer": "GCCWebApp-Test",
    "Audience": "GCCWebApp-Test",
    "ExpiryMinutes": 30
  }
}
```

#### Staging Environment
```json
{
  "Jwt": {
    "Key": "{STAGING_JWT_SECRET_KEY}",
    "Issuer": "GCCWebApp-Staging",
    "Audience": "GCCWebApp-Staging",
    "ExpiryMinutes": 30
  }
}
```

### 1.2 Authentication Features Configuration

#### Development
```json
{
  "Authentication": {
    "EnableTestMode": true,
    "Enable2FA": false
  },
  "Features": {
    "TwoFactorAuthentication": false,
    "RequireEmailConfirmation": false,
    "AllowGuestAccess": true
  }
}
```

#### Production
```json
{
  "Authentication": {
    "Enable2FA": true,
    "EnableTestMode": false,
    "RequireEmailConfirmation": false
  }
}
```

### 1.3 Azure AD B2C Configuration

```json
{
  "AzureAdB2C": {
    "Instance": "{AZURE_AD_B2C_INSTANCE}",
    "ClientId": "{AZURE_AD_B2C_CLIENT_ID}",
    "ClientSecret": "{AZURE_AD_B2C_CLIENT_SECRET}",
    "Domain": "{AZURE_AD_B2C_DOMAIN}",
    "TenantId": "{AZURE_AD_B2C_TENANT_ID}",
    "SignUpSignInPolicyId": "{SIGNUP_SIGNIN_POLICY}",
    "ResetPasswordPolicyId": "{RESET_PASSWORD_POLICY}",
    "EditProfilePolicyId": "{EDIT_PROFILE_POLICY}",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

---

## 2. Database Configuration

### 2.1 Connection Strings by Environment

#### Development - SQLite
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gcc_database.db"
  }
}
```

#### Development - SQL Server LocalDB
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GCCDatabase;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

#### Production - MySQL
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server={MYSQL_HOST};Port=3306;Database={DATABASE_NAME};User={DATABASE_USER};Password={DATABASE_PASSWORD};SslMode=Required"
  },
  "DatabaseProvider": "mysql"
}
```

#### Production - PostgreSQL
```json
{
  "ConnectionStrings": {
    "PostgresConnection": "Host=postgres;Port=5432;Database=gcc_database;Username=gcc_user;Password=password"
  }
}
```

#### Test Environment
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gcc_database_test.db"
  }
}
```

#### Staging Environment - Azure SQL
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:{staging-server}.database.windows.net,1433;Database=GCCDatabase_Staging;Authentication=Active Directory Default;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;"
  }
}
```

### 2.2 Database Provider Configuration

```json
{
  "DatabaseProvider": "mysql"
}
```

**Supported Providers:**
- `mysql` - MySQL/MariaDB
- `postgresql` - PostgreSQL
- `sqlserver` - Microsoft SQL Server
- `sqlite` - SQLite (development only)

---

## 3. Environment-Specific Settings

### 3.1 Development Environment (appsettings.Development.json)

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5101"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 3.2 Production Environment (appsettings.Production.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "GCCWebApp": "Information",
      "Microsoft.EntityFrameworkCore": "Error"
    }
  },
  "AllowedHosts": "*"
}
```

### 3.3 Test Environment (appsettings.Test.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

---

## 4. API Configuration

### 4.1 Rate Limiting Configuration

#### Development - Permissive
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": false,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1s",
        "Limit": 1000
      }
    ]
  }
}
```

#### Production - Restrictive
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": true,
    "RealIpHeader": "X-Forwarded-For",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*/api/*",
        "Period": "1m",
        "Limit": 60
      },
      {
        "Endpoint": "*/api/*",
        "Period": "1h",
        "Limit": 1000
      },
      {
        "Endpoint": "*/api/*",
        "Period": "1d",
        "Limit": 5000
      },
      {
        "Endpoint": "*/api/*/auth/login",
        "Period": "1m",
        "Limit": 5
      }
    ]
  }
}
```

### 4.2 CORS Configuration

#### Production
```json
{
  "AllowedOrigins": [
    "https://{PRODUCTION_DOMAIN}",
    "https://www.{PRODUCTION_DOMAIN}",
    "https://api.{PRODUCTION_DOMAIN}"
  ]
}
```

---

## 5. Third-Party Service Integration

### 5.1 SMTP Configuration

#### Production SMTP
```json
{
  "SmtpSettings": {
    "Host": "",
    "Port": "587",
    "EnableSsl": "true",
    "Username": "",
    "Password": "",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "GCC Database System"
  }
}
```

#### Test Environment SMTP
```json
{
  "SmtpSettings": {
    "Host": "YOUR_SMTP_HOST",
    "Port": "587",
    "EnableSsl": "true",
    "Username": "YOUR_EMAIL@DOMAIN.COM",
    "Password": "YOUR_EMAIL_PASSWORD",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "GCC Database - Test Environment"
  }
}
```

### 5.2 SMS Service Configuration

#### Twilio Configuration
```json
{
  "SmsSettings": {
    "Provider": "twilio",
    "Twilio": {
      "AccountSid": "{TWILIO_ACCOUNT_SID}",
      "AuthToken": "{TWILIO_AUTH_TOKEN}",
      "FromNumber": "{TWILIO_PHONE_NUMBER}",
      "TestNumber": "{TEST_PHONE_NUMBER}"
    }
  }
}
```

#### TextBelt Configuration
```json
{
  "SmsSettings": {
    "Provider": "textbelt",
    "TextBelt": {
      "ApiKey": "{TEXTBELT_API_KEY}",
      "TestNumber": "{TEST_PHONE_NUMBER}"
    }
  }
}
```

#### Mock SMS (Development)
```json
{
  "SmsSettings": {
    "Provider": "mock"
  }
}
```

---

## 6. Docker & Container Configuration

### 6.1 Environment Variables for Docker Deployment

#### PostgreSQL Stack
```bash
DB_PASSWORD=defaultpassword123
JWT_SECRET_KEY=your-super-secret-jwt-key-min-64-chars-for-security-compliance-here
REDIS_PASSWORD=redispassword123
```

#### MariaDB Stack
```bash
DB_PASSWORD=defaultpassword123
JWT_SECRET_KEY=your-super-secret-jwt-key-min-64-chars-for-security-compliance-here
REDIS_PASSWORD=redispassword123
```

#### Test Stack
```bash
DB_SA_PASSWORD=TestP@ssw0rd123!
JWT_SECRET_KEY=test-jwt-secret-key-min-64-chars-for-security-requirements-padding-here
REDIS_PASSWORD=TestRedisP@ss123
```

### 6.2 Container Port Configuration

| Service | Port | Protocol | Environment | Description |
|---------|------|----------|-------------|-------------|
| Web App | 8080 | HTTP | All | Main application |
| Web App | 8443 | HTTPS | Production | Secure application |
| PostgreSQL | 5432 | TCP | Production | Database |
| MariaDB | 3306 | TCP | Alternative | Database |
| Redis | 6379 | TCP | Production | Caching |
| SQL Server | 1433 | TCP | Test | Test database |

### 6.3 Current Container Status

| Container Name | Port | Status | Database | Purpose |
|----------------|------|---------|----------|---------|
| gcc-production | 5105 | Healthy | SQLite | Working production |
| gcc-webapp-fixed | 5102 | Unhealthy | PostgreSQL | Needs attention |

---

## 7. Production Deployment Configuration

### 7.1 Required Environment Variables

```bash
# Core Application
ASPNETCORE_ENVIRONMENT=Production
GCC_JWT_SECRET=64-character-secure-random-string

# Azure Services (Optional)
GCC_AZURE_AD_B2C_CLIENT_SECRET=azure-b2c-client-secret
GCC_AZURE_AD_CLIENT_SECRET=azure-ad-client-secret

# Database
DATABASE_CONNECTION_STRING="Server=prod-server;Database=GCCDatabase;User Id=appuser;Password=secure-password;Encrypt=true;"

# Optional Services
GCC_AZURE_BLOB_CONNECTION_STRING=azure-storage-connection
GCC_APPLICATION_INSIGHTS_CONNECTION_STRING=app-insights-string
GCC_SMTP_PASSWORD=email-server-password
```

### 7.2 Production Feature Flags

```json
{
  "FeatureFlags": {
    "EnableDetailedErrorMessages": false,
    "EnableDebugEndpoints": false,
    "EnableSwagger": false,
    "EnableDeveloperExceptionPage": false,
    "RequireHttpsForAllRequests": true,
    "EnableSecurityHeaders": true,
    "EnableCorsRestrictions": true
  }
}
```

### 7.3 Security Configuration

#### Password Policy (Production)
```json
{
  "PasswordPolicy": {
    "MinLength": 14,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigits": true,
    "RequireSpecialCharacters": true,
    "MaxPasswordHistory": 10,
    "PasswordExpirationDays": 90,
    "AccountLockoutThreshold": 3,
    "AccountLockoutDurationMinutes": 30,
    "RequireTwoFactor": true
  }
}
```

#### Data Protection (Production)
```json
{
  "DataProtection": {
    "ApplicationName": "GCC-Production",
    "KeyLifetime": "90.00:00:00",
    "PersistKeysToAzureBlobStorage": true,
    "AzureBlobStorageConnectionString": "{AZURE_BLOB_CONNECTION_STRING}",
    "AzureBlobStorageContainerName": "gcc-dataprotection-keys"
  }
}
```

### 7.4 Security Monitoring
```json
{
  "SecurityMonitoring": {
    "EnableSecurityEventLogging": true,
    "EnableFailedLoginTracking": true,
    "EnableSuspiciousActivityDetection": true,
    "AlertThresholds": {
      "FailedLoginsPerMinute": 10,
      "SuspiciousIpDetectionEnabled": true,
      "UnusualUserAgentDetection": true
    }
  }
}
```

---

## 8. Security & Compliance Settings

### 8.1 Test User Credentials

**⚠️ SECURITY WARNING**: All test users currently use the same password for development convenience.

| Username | Email | Role | Password | Status |
|----------|-------|------|----------|---------|
| admin | admin@gcc.com | Admin | TestPass123! | Active |
| analyst | analyst@gcc.com | Analyst | TestPass123! | Active |
| external | external@company.com | External | TestPass123! | Active |
| testuser | test@gcc.com | Public | TestPass123! | Active |

### 8.2 API Keys (Development)

| Name | Key | Environment | Scopes | Status |
|------|-----|-------------|---------|---------|
| Integration Test Key | gcc_test_key_1234567890abcdef | Development | read,write,admin | Active |
| Partner API Access | gcc_partner_key_abcdef1234567890 | Production | read | Active |
| Mobile App Key | gcc_mobile_key_fedcba0987654321 | Production | read,write | Active |

### 8.3 Security Recommendations

#### Immediate Actions Required
1. ✅ **Replace Development JWT Key**: Current key is predictable
2. ✅ **Generate Production JWT Secrets**: Use cryptographically secure random strings
3. ❌ **Remove Hardcoded Passwords**: Move to environment variables
4. ❌ **Implement Key Rotation**: Establish regular credential updates

#### Short-Term Security Improvements
1. **Environment Variable Migration**: Move all secrets out of appsettings files
2. **Azure Key Vault Integration**: For production secret management
3. **Secret Scanning**: Implement automated credential detection
4. **Audit Logging**: Enhanced security event monitoring

---

## Appendix A: Quick Reference Commands

### Development Commands
```bash
# Build and Run
dotnet restore
dotnet build
dotnet run --urls "http://localhost:5101"

# Database Operations
dotnet ef database update
dotnet ef migrations add [MigrationName]

# Testing
dotnet test                      # All tests
dotnet test GCCWebApp.Tests     # Unit tests
dotnet test GCCWebApp.UAT.Tests # UI tests
```

### Container Management
```bash
# Check container status
docker ps | grep gcc

# View logs
docker logs -f gcc-production

# Start production container
./docker-run-production.sh

# Full stack deployment
docker-compose up -d
```

---

## Appendix B: Configuration File Locations

| Environment | File Path | Purpose |
|-------------|-----------|---------|
| Development | `appsettings.Development.json` | Local development settings |
| Production | `appsettings.Production.json` | Production deployment settings |
| Test | `appsettings.Test.json` | Automated testing configuration |
| Staging | `appsettings.Staging.json` | Pre-production environment |
| Docker | `docker-compose.yml` | Container orchestration |

---

**Document Classification**: CONFIDENTIAL
**Prepared by**: GCC Development Team
**Date**: September 18, 2025
**Version**: 1.0
**Next Review**: October 18, 2025

**Security Notice**: This document contains sensitive configuration information. Ensure proper access controls, secure storage, and regular review of credentials and settings.