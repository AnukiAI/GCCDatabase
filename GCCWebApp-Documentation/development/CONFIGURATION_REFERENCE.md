# GCC Database Solution - Configuration Reference

**Document Type**: Configuration Documentation
**Generated**: September 18, 2025
**Status**: Complete Reference
**Security Level**: 🔴 CONFIDENTIAL - Contains Sensitive Information

---

## 📋 Table of Contents

1. [Database Connections](#database-connections)
2. [Test User Credentials](#test-user-credentials)
3. [Authentication & Security](#authentication--security)
4. [Third-Party Services](#third-party-services)
5. [Environment Variables](#environment-variables)
6. [Docker Configuration](#docker-configuration)
7. [Production Deployment](#production-deployment)
8. [Security Considerations](#security-considerations)

---

## 🗄️ Database Connections

### Development Environment
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GCCDatabase;Trusted_Connection=true;MultipleActiveResultSets=true",
  "SqliteConnection": "Data Source=gcc_database.db"
}
```

### Production Environment
```json
"ConnectionStrings": {
  "DefaultConnection": "Server={MYSQL_HOST};Port=3306;Database={DATABASE_NAME};User={DATABASE_USER};Password={DATABASE_PASSWORD};SslMode=Required",
  "PostgresConnection": "Host=postgres;Port=5432;Database=gcc_database;Username=gcc_user;Password=password",
  "SqliteConnection": "Data Source=gcc_database.db"
}
```

### Staging Environment
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=tcp:{staging-server}.database.windows.net,1433;Database=GCCDatabase_Staging;Authentication=Active Directory Default;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;",
  "SqliteConnection": "Data Source=gcc_database_staging.db"
}
```

### Test Environment
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=gcc_database_test.db",
  "SqliteConnection": "Data Source=gcc_database_test.db"
}
```

---

## 👥 Test User Credentials

### Seeded Database Users
**Default Password for ALL users**: `TestPass123!`

| Username | Email | Full Name | Role | Status | Description |
|----------|-------|-----------|------|---------|-------------|
| `admin` | `admin@gcc.com` | Admin User | Admin | Active | Primary admin account |
| `analyst` | `analyst@gcc.com` | Jane Analyst | Analyst | Active | Standard analyst |
| `external` | `external@company.com` | External User | External | Active | External partner |
| `testuser` | `test@gcc.com` | Test User | Public | Active | General test user |
| `sarah.johnson` | `sarah.johnson@gcc.com` | Sarah Johnson | Analyst | Active | Additional analyst |
| `mike.chen` | `mike.chen@gcc.com` | Mike Chen | Analyst | Active | Additional analyst |
| `lisa.rodriguez` | `lisa.rodriguez@assaabloy.com` | Lisa Rodriguez | External | Active | ASSA ABLOY partner |
| `david.kim` | `david.kim@3m.com` | David Kim | External | Active | 3M partner |
| `emma.wilson` | `emma.wilson@interface.com` | Emma Wilson | External | Active | Interface partner |
| `alex.martinez` | `alex.martinez@steelcase.com` | Alex Martinez | External | Active | Steelcase partner |
| `jennifer.lee` | `jennifer.lee@gcc.com` | Jennifer Lee | Analyst | Active | Additional analyst |
| `robert.taylor` | `robert.taylor@gcc.com` | Robert Taylor | Admin | Active | Additional admin |
| `maria.garcia` | `maria.garcia@mohawkind.com` | Maria Garcia | External | Active | Mohawk Industries |
| `public.user` | `public@example.com` | Public User | Public | Active | Public access user |

### API Keys (Seeded in Database)
| Name | Key | Environment | Created By | Scopes | Status |
|------|-----|-------------|------------|---------|---------|
| Integration Test Key | `gcc_test_key_1234567890abcdef` | Development | admin | read,write,admin | Active |
| Partner API Access | `gcc_partner_key_abcdef1234567890` | Production | external | read | Active |
| Mobile App Key | `gcc_mobile_key_fedcba0987654321` | Production | analyst | read,write | Active |
| Reporting Service | `gcc_report_key_1a2b3c4d5e6f7890` | Production | sarah.johnson | read,report | Active |
| Legacy Integration | `gcc_legacy_key_0987654321abcdef` | Staging | mike.chen | read | INACTIVE |

---

## 🔐 Authentication & Security

### JWT Configuration
```json
"Jwt": {
  "Key": "ThisIsAVeryLongSecretKeyForJwtTokenGenerationAtLeast64CharactersLong123456789",
  "Issuer": "GCCWebApp",
  "Audience": "GCCWebApp",
  "ExpiryMinutes": 60
}
```

### Azure AD B2C Configuration
```json
"AzureAdB2C": {
  "Instance": "https://{tenant-name}.b2clogin.com/",
  "ClientId": "{client-id}",
  "ClientSecret": "{client-secret}",
  "Domain": "{tenant-name}.onmicrosoft.com",
  "TenantId": "{tenant-id}",
  "SignUpSignInPolicyId": "B2C_1_SignUpSignIn",
  "ResetPasswordPolicyId": "B2C_1_PasswordReset",
  "EditProfilePolicyId": "B2C_1_ProfileEdit",
  "CallbackPath": "/signin-oidc",
  "SignedOutCallbackPath": "/signout-callback-oidc"
}
```

### Azure AD Configuration
```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "Domain": "{tenant-domain}",
  "TenantId": "{tenant-id}",
  "ClientId": "{client-id}",
  "ClientSecret": "{client-secret}",
  "CallbackPath": "/signin-oidc",
  "SignedOutCallbackPath": "/signout-callback-oidc"
}
```

---

## 📱 Third-Party Services

### SMS Services Configuration

#### Twilio
```json
"SmsSettings": {
  "Provider": "twilio",
  "Twilio": {
    "AccountSid": "{TWILIO_ACCOUNT_SID}",
    "AuthToken": "{TWILIO_AUTH_TOKEN}",
    "FromNumber": "{TWILIO_PHONE_NUMBER}",
    "TestNumber": "{TEST_PHONE_NUMBER}"
  }
}
```

#### TextBelt
```json
"SmsSettings": {
  "Provider": "textbelt",
  "TextBelt": {
    "ApiKey": "{TEXTBELT_API_KEY}",
    "TestNumber": "{TEST_PHONE_NUMBER}"
  }
}
```

#### Mock SMS (Development)
```json
"SmsSettings": {
  "Provider": "mock"
}
```

### Email/SMTP Configuration

#### Production SMTP
```json
"SmtpSettings": {
  "Host": "",
  "Port": "587",
  "EnableSsl": "true",
  "Username": "",
  "Password": "",
  "FromEmail": "noreply@yourdomain.com",
  "FromName": "GCC Database System"
}
```

#### Test Environment SMTP
```json
"SmtpSettings": {
  "Host": "YOUR_SMTP_HOST",
  "Port": "587",
  "EnableSsl": "true",
  "Username": "YOUR_EMAIL@DOMAIN.COM",
  "Password": "YOUR_EMAIL_PASSWORD",
  "FromEmail": "noreply@yourdomain.com",
  "FromName": "GCC Database - Test Environment"
}
```

### Microsoft Graph API
```json
"MicrosoftGraph": {
  "BaseUrl": "https://graph.microsoft.com/v1.0/",
  "Scopes": "User.Read User.ReadWrite.All Directory.Read.All"
}
```

---

## 🌍 Environment Variables

### Required Production Variables
```bash
# Core Application
ASPNETCORE_ENVIRONMENT=Production
GCC_JWT_SECRET=64-character-secure-random-string

# Azure AD B2C (Optional - Cookie auth fallback available)
GCC_AZURE_AD_B2C_CLIENT_SECRET=azure-b2c-client-secret
GCC_AZURE_AD_CLIENT_SECRET=azure-ad-client-secret

# Database
DATABASE_CONNECTION_STRING="Server=prod-server;Database=GCCDatabase;User Id=appuser;Password=secure-password;Encrypt=true;"

# Optional Services
GCC_AZURE_BLOB_CONNECTION_STRING=azure-storage-connection
GCC_APPLICATION_INSIGHTS_CONNECTION_STRING=app-insights-string
GCC_SMTP_PASSWORD=email-server-password

# JWT Configuration
JWT__Issuer=https://api.greencirclecertified.com
JWT__Audience=https://api.greencirclecertified.com
```

### Docker Environment Variables

#### PostgreSQL Docker Stack
```bash
DB_PASSWORD=defaultpassword123
JWT_SECRET_KEY=your-super-secret-jwt-key-min-64-chars-for-security-compliance-here
REDIS_PASSWORD=redispassword123
```

#### MariaDB Docker Stack
```bash
DB_PASSWORD=defaultpassword123
JWT_SECRET_KEY=your-super-secret-jwt-key-min-64-chars-for-security-compliance-here
REDIS_PASSWORD=redispassword123
```

#### Test Docker Stack
```bash
DB_SA_PASSWORD=TestP@ssw0rd123!
JWT_SECRET_KEY=test-jwt-secret-key-min-64-chars-for-security-requirements-padding-here
REDIS_PASSWORD=TestRedisP@ss123
```

#### HTTPS Docker Stack
```bash
DB_PASSWORD=SecurePassword123!
JWT_SECRET_KEY=ThisIsAVeryLongSecretKeyForJwtTokenGenerationAtLeast64CharactersLong123456789
REDIS_PASSWORD=RedisPassword123!
```

---

## 🐳 Docker Configuration

### Docker Compose Files Available

1. **docker-compose.yml** - PostgreSQL + Redis Stack
2. **docker-compose.mariadb.yml** - MariaDB + Redis Stack
3. **docker-compose.test.yml** - SQL Server Test Stack
4. **docker-compose.https.yml** - HTTPS-enabled Stack

### Production Container Ports
| Service | Port | Protocol | Description |
|---------|------|----------|-------------|
| Web Application | 8080 | HTTP | Main application |
| Web Application (HTTPS) | 8443 | HTTPS | Secure application |
| PostgreSQL | 5432 | TCP | Database |
| MariaDB | 3306 | TCP | Database (alternative) |
| Redis | 6379 | TCP | Caching |
| SQL Server Test | 1433 | TCP | Test database |

### Current Running Containers
| Container Name | Port | Status | Database | Purpose |
|----------------|------|---------|----------|---------|
| gcc-production | 5105 | Healthy | SQLite | Working production |
| gcc-webapp-fixed | 5102 | Unhealthy | Postgres | Needs attention |

---

## 🚀 Production Deployment

### Database Connection Strings by Environment

#### SQLite (Development/Simple Deployment)
```
Data Source=gcc_database.db
```

#### SQL Server (Traditional Production)
```
Server=(localdb)\\mssqllocaldb;Database=GCCDatabase;Trusted_Connection=true;MultipleActiveResultSets=true
```

#### PostgreSQL (Recommended Production)
```
Host=postgres;Port=5432;Database=gcc_database;Username=gcc_user;Password=${DB_PASSWORD}
```

#### MariaDB (Alternative Production)
```
Server=mariadb;Port=3306;Database=gcc_database;User=gcc_user;Password=${DB_PASSWORD};
```

### Deployment URLs
| Environment | URL | Purpose |
|-------------|-----|---------|
| Development | http://localhost:5101 | Local development |
| Development (HTTPS) | https://localhost:7130 | Local development (SSL) |
| Container | http://localhost:8080 | Docker container |
| Container (HTTPS) | https://localhost:8443 | Docker container (SSL) |

---

## ⚠️ Security Considerations

### 🔴 Critical Security Issues Found

1. **Hardcoded Passwords in Source Control**
   - All test users use the same password: `TestPass123!`
   - JWT secret key is predictable in development
   - Test credentials exposed in appsettings files

2. **API Keys in Database Seeds**
   - API keys are hardcoded in SeedData.cs
   - Keys are committed to source control
   - No key rotation implemented

3. **Configuration File Exposure**
   - Sensitive configuration in source control
   - Placeholder values in production files
   - No environment variable enforcement

### 🛡️ Recommended Security Actions

#### Immediate (Priority 1)
- [ ] Remove hardcoded passwords from appsettings files
- [ ] Generate secure JWT secrets for all environments
- [ ] Move test credentials to environment variables
- [ ] Update .gitignore to prevent credential commits

#### Short Term (Priority 2)
- [ ] Implement secure seed data process
- [ ] Use random passwords for database seeds
- [ ] Remove API key seeds from source control
- [ ] Implement proper secret management

#### Long Term (Priority 3)
- [ ] Integrate Azure Key Vault for production secrets
- [ ] Implement credential rotation policies
- [ ] Add secret scanning to CI/CD pipeline
- [ ] Enforce environment variable usage

### Rate Limiting Configuration
```json
"IpRateLimiting": {
  "EnableEndpointRateLimiting": true,
  "GeneralRules": [
    {
      "Endpoint": "*/api/*",
      "Period": "1m",
      "Limit": 100
    },
    {
      "Endpoint": "*/api/*",
      "Period": "1h",
      "Limit": 1000
    },
    {
      "Endpoint": "*/api/*",
      "Period": "1d",
      "Limit": 10000
    }
  ]
}
```

---

## 📞 Support Information

### Quick Access Commands
```bash
# Build and Run
dotnet restore
dotnet build
dotnet run --urls "http://localhost:5101"

# Database
dotnet ef database update
dotnet ef migrations add [MigrationName]

# Testing
dotnet test                                    # All tests
dotnet test GCCWebApp.Tests                   # Unit tests
dotnet test GCCWebApp.UAT.Tests              # UI tests
```

### Container Management
```bash
# Check container status
docker ps | grep gcc

# View container logs
docker logs -f gcc-production

# Start production container
./docker-run-production.sh

# Full stack deployment
docker-compose up -d
```

---

**⚠️ IMPORTANT SECURITY WARNING**: This document contains sensitive configuration information including passwords, connection strings, and API keys.

- Do NOT commit this document to source control
- Restrict access to authorized personnel only
- Use secure channels when sharing
- Regularly audit and update credentials
- Implement proper secret management in production

**Document Last Updated**: September 18, 2025
**Next Review Date**: October 18, 2025