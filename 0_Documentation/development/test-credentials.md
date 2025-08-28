# GCCWebApp Test Credentials Inventory

## 🔴 CRITICAL SECURITY ISSUE: Hardcoded Credentials Found

This document inventories all hardcoded test credentials found in the GCCWebApp codebase that need to be secured or removed.

## Configuration Files (IMMEDIATE ACTION REQUIRED)

### 1. appsettings.Development.json:18-39
**Location**: `C:\...\GCCWebApp\appsettings.Development.json`
**Status**: 🔴 EXPOSED IN SOURCE CONTROL

```json
"TestUsers": [
  {
    "Email": "admin@test.com",
    "Password": "admin",
    "Role": "Admin"
  },
  {
    "Email": "analyst@test.com", 
    "Password": "analyst",
    "Role": "Analyst"
  },
  {
    "Email": "external@test.com",
    "Password": "external", 
    "Role": "External"
  },
  {
    "Email": "public@test.com",
    "Password": "public",
    "Role": "Public"
  }
]
```

### 2. JWT Secret Key
**Location**: `appsettings.Development.json:42`
**Status**: 🔴 WEAK PREDICTABLE KEY

```json
"Key": "DEVELOPMENT_JWT_SECRET_KEY_MINIMUM_64_CHARACTERS_FOR_SECURITY_COMPLIANCE_REQUIREMENTS_TESTING_PURPOSES"
```

## Database Seed Data (SeedData.cs)

### Test Users Created in Database
**Location**: `Data/SeedData.cs:667-860`
**Password**: All users have the same password: `"TestPass123!"`

| Username | Email | Full Name | Role | Status |
|----------|-------|-----------|------|---------|
| `admin` | `admin@gcc.com` | Admin User | Admin | Active |
| `analyst` | `analyst@gcc.com` | Jane Analyst | Analyst | Active |
| `external` | `external@company.com` | External User | External | Active |
| `testuser` | `test@gcc.com` | Test User | Public | Active |
| `sarah.johnson` | `sarah.johnson@gcc.com` | Sarah Johnson | Analyst | Active |
| `mike.chen` | `mike.chen@gcc.com` | Mike Chen | Analyst | Active |
| `lisa.rodriguez` | `lisa.rodriguez@assaabloy.com` | Lisa Rodriguez | External | Active |
| `david.kim` | `david.kim@3m.com` | David Kim | External | Active |
| `emma.wilson` | `emma.wilson@interface.com` | Emma Wilson | External | Active |
| `alex.martinez` | `alex.martinez@steelcase.com` | Alex Martinez | External | Active |
| `jennifer.lee` | `jennifer.lee@gcc.com` | Jennifer Lee | Analyst | Active |
| `robert.taylor` | `robert.taylor@gcc.com` | Robert Taylor | Admin | Active |
| `maria.garcia` | `maria.garcia@mohawkind.com` | Maria Garcia | External | Active |
| `public.user` | `public@example.com` | Public User | Public | Active |

### API Keys in Database
**Location**: `Data/SeedData.cs:904-976`

| Name | Key | Environment | Created By | Scopes |
|------|-----|-------------|------------|---------|
| Integration Test Key | `gcc_test_key_1234567890abcdef` | Development | admin | read,write,admin |
| Partner API Access | `gcc_partner_key_abcdef1234567890` | Production | external | read |
| Mobile App Key | `gcc_mobile_key_fedcba0987654321` | Production | analyst | read,write |
| Reporting Service | `gcc_report_key_1a2b3c4d5e6f7890` | Production | sarah.johnson | read,report |
| Legacy Integration | `gcc_legacy_key_0987654321abcdef` | Staging | mike.chen | read (INACTIVE) |

## Other Configuration Files

### 3. appsettings.Production.json
**Location**: `appsettings.Production.json:46-47`
**Status**: 🟡 PLACEHOLDER (Empty but in source control)

```json
"Username": "",
"Password": ""
```

### 4. appsettings.Security.json
**Location**: `appsettings.Security.json:106-107,145`
**Status**: 🟡 TEMPLATE PLACEHOLDERS

```json
"Username": "{SMTP_USERNAME}",
"Password": "{SMTP_PASSWORD}"
```

### 5. appsettings.Test.json
**Location**: `appsettings.Test.json:34-35`
**Status**: 🟡 PLACEHOLDER

```json
"Username": "YOUR_EMAIL@DOMAIN.COM",
"Password": "YOUR_EMAIL_PASSWORD"
```

## Documentation References

Multiple documentation files reference the test credentials:
- Test password `TestPass123!` mentioned in UAT testing docs
- Login credentials tables in observation files
- Requirements documents discussing password policies

## IMMEDIATE ACTIONS REQUIRED

### 1. Remove from Source Control (Priority 1)
- [ ] Remove hardcoded passwords from `appsettings.Development.json`
- [ ] Remove predictable JWT secret from development config
- [ ] Move test credentials to secure environment variables
- [ ] Update `.gitignore` to prevent future credential commits

### 2. Secure Database Seeds (Priority 2)
- [ ] Move seed user creation to environment-specific process
- [ ] Use random passwords for seed data
- [ ] Document secure test account creation process
- [ ] Remove or secure API key seeds

### 3. Environment Variables Setup (Priority 3)
- [ ] Create `.env.development` file (not in source control)
- [ ] Configure environment variable loading in `Program.cs`
- [ ] Update configuration documentation

### 4. Production Security (Priority 4)
- [ ] Generate cryptographically secure JWT secrets
- [ ] Remove all placeholder values from production configs
- [ ] Implement secure configuration management

## Recommended Environment Variables

```bash
# Development Environment
GCC_JWT_SECRET="[generate-secure-random-key-here]"
GCC_TEST_ADMIN_EMAIL="admin@test.local"
GCC_TEST_ADMIN_PASSWORD="[secure-random-password]"
GCC_TEST_ANALYST_EMAIL="analyst@test.local"
GCC_TEST_ANALYST_PASSWORD="[secure-random-password]"
GCC_TEST_EXTERNAL_EMAIL="external@test.local"
GCC_TEST_EXTERNAL_PASSWORD="[secure-random-password]"
GCC_TEST_PUBLIC_EMAIL="public@test.local"
GCC_TEST_PUBLIC_PASSWORD="[secure-random-password]"
```

## Configuration Code Changes Needed

Update `Program.cs` to load test users from environment variables:

```csharp
var testUsers = new List<TestUser>();
if (configuration.GetValue<bool>("Authentication:EnableTestMode"))
{
    testUsers.Add(new TestUser 
    {
        Email = Environment.GetEnvironmentVariable("GCC_TEST_ADMIN_EMAIL"),
        Password = Environment.GetEnvironmentVariable("GCC_TEST_ADMIN_PASSWORD"),
        Role = "Admin"
    });
    // ... add other test users from env vars
}
```

---
**SECURITY WARNING**: This document contains sensitive information. Do not commit to source control.  
**Generated**: 2025-08-27  
**Status**: Action Required - Critical Security Issue