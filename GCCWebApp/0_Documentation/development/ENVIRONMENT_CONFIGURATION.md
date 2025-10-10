# GCC Web Application - Environment Variables & Configuration

## Core Environment Variables

### Required Variables

| Variable | Description | Example/Default | Production Value |
|----------|-------------|-----------------|------------------|
| `ASPNETCORE_ENVIRONMENT` | Application environment | Development/Production | Production |
| `ASPNETCORE_URLS` | Listening URLs | http://+:8080 | http://+:8080 |
| `ConnectionStrings__DefaultConnection` | Database connection | See Database section | Required |
| `Jwt__Key` | JWT signing key (64+ chars) | Must be 64+ characters | Required |
| `Jwt__Issuer` | JWT issuer URL | https://api.greencirclecertified.com | Your domain |
| `Jwt__Audience` | JWT audience URL | https://api.greencirclecertified.com | Your domain |

### Optional Variables

| Variable | Description | Default | When Required |
|----------|-------------|---------|---------------|
| `GCC_JWT_SECRET` | Alternative JWT secret | None | If Jwt__Key not set |
| `GCC_AZURE_AD_B2C_CLIENT_SECRET` | Azure B2C secret | None | If using Azure B2C |
| `GCC_AZURE_AD_CLIENT_SECRET` | Azure AD secret | None | If using Azure AD |
| `GCC_AZURE_BLOB_CONNECTION_STRING` | Blob storage connection | None | If using blob storage |
| `GCC_APPLICATION_INSIGHTS_CONNECTION_STRING` | App Insights | None | If using monitoring |
| `GCC_SMTP_PASSWORD` | Email server password | None | If sending emails |
| `DatabaseProvider` | Database type | sqlite | postgresql/mariadb/sqlserver |

## Database Connection Strings

### SQLite (Development)
```
Data Source=gcc_database.db
```

### PostgreSQL (Recommended for Production)
```
Host=localhost;Database=gccdb;Username=gccuser;Password=YourPassword;Port=5432
```

### MariaDB
```
Server=localhost;Database=gccdb;User=gccuser;Password=YourPassword;Port=3306
```

### SQL Server
```
Server=localhost;Database=GCCDatabase;User Id=gccuser;Password=YourPassword;Encrypt=true;
```

## Docker Environment Configuration

### Development Container
```bash
docker run -d \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS="http://+:8080" \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db" \
  -e Jwt__Key="DevelopmentKeyForTestingOnly-ThisShouldBeAtLeast64CharactersLong123456" \
  gcc-webapp:development
```

### Production Container (PostgreSQL)
```bash
docker run -d \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="http://+:8080" \
  -e ConnectionStrings__DefaultConnection="Host=db-server;Database=gccdb;Username=gccuser;Password=SecurePassword;Port=5432" \
  -e Jwt__Key="ProductionSecretKeyMustBeVeryLongAndSecureAtLeast64CharactersForSecurity" \
  -e Jwt__Issuer="https://api.yourdomain.com" \
  -e Jwt__Audience="https://api.yourdomain.com" \
  gcc-webapp:production
```

## Environment Files (.env)

### .env.development
```bash
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5101
ConnectionStrings__DefaultConnection="Data Source=gcc_database.db"
Jwt__Key="DevelopmentKeyForTestingOnly-ThisShouldBeAtLeast64CharactersLong123456"
Jwt__Issuer="http://localhost:5101"
Jwt__Audience="http://localhost:5101"
```

### .env.production
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__DefaultConnection="Host=localhost;Database=gccdb;Username=gccuser;Password=CHANGE_THIS;Port=5432"
Jwt__Key="GENERATE_A_SECURE_64_CHARACTER_RANDOM_STRING_HERE_FOR_PRODUCTION_USE"
Jwt__Issuer="https://api.greencirclecertified.com"
Jwt__Audience="https://api.greencirclecertified.com"
DatabaseProvider=postgresql
```

## Docker Compose Environment

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=http://+:8080
  - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=gcc_database;Username=gcc_user;Password=${DB_PASSWORD}
  - Jwt__Key=${JWT_SECRET_KEY}
  - Jwt__Issuer=https://api.greencirclecertified.com
  - Jwt__Audience=https://api.greencirclecertified.com
  - DatabaseProvider=postgresql
```

## Generating Secure Values

### JWT Secret Key (64+ characters)
```bash
# Linux/Mac
openssl rand -base64 48

# Windows PowerShell
[System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))

# Or use online generator (for development only)
# https://www.grc.com/passwords.htm
```

### Database Password
```bash
# Generate 32-character password
openssl rand -base64 24 | tr -d "=+/" | cut -c1-32
```

## Feature Flags

Set in appsettings.json or as environment variables:

```json
{
  "Features": {
    "EnableAzureB2C": false,
    "EnableTwoFactor": true,
    "EnableApiRateLimiting": true,
    "EnableLazyLoading": false,
    "EnableAutocomplete": false,
    "EnablePartnerBranding": true
  }
}
```

As environment variables:
```bash
Features__EnableAzureB2C=false
Features__EnableTwoFactor=true
Features__EnableApiRateLimiting=true
```

## Health Check Configuration

The application exposes health checks at `/health`:

```json
{
  "HealthChecks": {
    "Database": {
      "Enabled": true,
      "Timeout": 30
    },
    "Memory": {
      "Enabled": true,
      "Threshold": 1024
    }
  }
}
```

## Logging Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

As environment variables:
```bash
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning
```

## Security Headers

Automatically configured in production:
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Content-Security-Policy: default-src 'self'

## Port Configuration

| Environment | Internal Port | External Port | URL |
|-------------|---------------|---------------|-----|
| Development | 5101 | 5101 | http://localhost:5101 |
| Docker Dev | 8080 | 5102 | http://localhost:5102 |
| Production | 8080 | 443 (nginx) | https://yourdomain.com |

## Troubleshooting Environment Issues

### Check Current Environment
```bash
docker exec gcc-webapp printenv | grep -E "(ASPNETCORE|Connection|Jwt)"
```

### Validate Connection String
```bash
docker exec gcc-webapp sh -c 'echo $ConnectionStrings__DefaultConnection'
```

### Test Database Connection
```bash
# PostgreSQL
docker exec gcc-webapp pg_isready -h localhost -p 5432

# From container
docker exec -it gcc-webapp /bin/bash
apt-get update && apt-get install -y postgresql-client
psql -h localhost -U gccuser -d gccdb -c "SELECT 1"
```

---
*Last Updated: September 16, 2025*