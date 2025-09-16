# 🔧 GCC WebApp Configuration Guide

## Required vs Optional Configuration

### ✅ REQUIRED (App won't start without these)

#### 1. Database Connection
```bash
# SQLite (Recommended for simple deployments)
ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db"

# OR PostgreSQL (Production with multiple users)
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=gcc_database;Username=gcc_user;Password=your_secure_password"
```

#### 2. JWT Secret (Security Critical)
```bash
# Must be at least 64 characters for security
GCC_JWT_SECRET="your-super-secret-jwt-key-must-be-at-least-64-characters-long-for-security-purposes-12345"
# OR
Jwt__Key="your-super-secret-jwt-key-must-be-at-least-64-characters-long-for-security-purposes-12345"
```

#### 3. Data Protection (Required in Production)
```bash
# Disable Azure blob storage (recommended for simple deployments)
DataProtection__PersistKeysToAzureBlobStorage=false

# OR use Azure (enterprise deployments)
DataProtection__AzureBlobStorageConnectionString="DefaultEndpointsProtocol=https;AccountName=yourstorage;AccountKey=key;"
```

### 🔄 OPTIONAL (Can use dummy values)

#### 4. Azure AD B2C (Only if using Azure authentication)
```bash
# Use dummy values if not using Azure AD
AZURE_AD_B2C_CLIENT_SECRET="dummy-secret-not-using-azure"
AZURE_AD_CLIENT_SECRET="dummy-secret-not-using-azure"

# OR real values for Azure integration
AZURE_AD_B2C_CLIENT_SECRET="your-real-azure-b2c-secret"
AzureAdB2C__Instance="https://yourtenant.b2clogin.com"
AzureAdB2C__ClientId="your-client-id"
AzureAdB2C__Domain="yourtenant.onmicrosoft.com"
```

#### 5. Application Insights (Optional monitoring)
```bash
# Dummy value (no monitoring)
APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000"

# OR real monitoring
APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=your-real-key;IngestionEndpoint=https://region.in.applicationinsights.azure.com/"
```

#### 6. Email Configuration (Optional notifications)
```bash
# Dummy value (no emails)
SMTP_PASSWORD="dummy-password-no-email"

# OR real email setup
SMTP_PASSWORD="your-email-password"
EmailConfiguration__Host="smtp.gmail.com"
EmailConfiguration__Port="587"
EmailConfiguration__Username="your-email@gmail.com"
```

## 🚀 Deployment Configurations

### Option 1: Simple SQLite Deployment (Recommended)
```bash
# Minimal configuration for small deployments
docker run -d --name gcc-webapp --restart unless-stopped \
  -p 5101:8080 \
  -v /var/www/data:/app/data \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db" \
  -e Jwt__Key="your-64-character-secret-key-here-make-it-random-and-secure-123456789" \
  -e DataProtection__PersistKeysToAzureBlobStorage=false \
  -e AZURE_AD_B2C_CLIENT_SECRET="not-using-azure" \
  -e AZURE_AD_CLIENT_SECRET="not-using-azure" \
  -e APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000" \
  -e SMTP_PASSWORD="not-using-email" \
  ghcr.io/anukiai/gcc-webapp:production-ready
```

### Option 2: PostgreSQL + Azure Integration
```bash
# Full enterprise setup
docker run -d --name gcc-webapp --restart unless-stopped \
  -p 5101:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=gcc_database;Username=gcc_user;Password=your_secure_db_password" \
  -e Jwt__Key="your-64-character-secret-key-here-make-it-random-and-secure-123456789" \
  -e DataProtection__AzureBlobStorageConnectionString="DefaultEndpointsProtocol=https;AccountName=yourstorage;AccountKey=your_key;" \
  -e AZURE_AD_B2C_CLIENT_SECRET="your-azure-b2c-secret" \
  -e AZURE_AD_CLIENT_SECRET="your-azure-ad-secret" \
  -e APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=your-insights-key" \
  -e SMTP_PASSWORD="your-email-password" \
  ghcr.io/anukiai/gcc-webapp:production-ready
```

### Option 3: Environment File (Recommended for Production)
Create `.env` file:
```env
# .env file for production
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Data Source=/app/data/gcc_database.db
Jwt__Key=your-64-character-secret-key-here-make-it-random-and-secure-123456789
DataProtection__PersistKeysToAzureBlobStorage=false
AZURE_AD_B2C_CLIENT_SECRET=not-using-azure
AZURE_AD_CLIENT_SECRET=not-using-azure
APPLICATION_INSIGHTS_CONNECTION_STRING=InstrumentationKey=00000000-0000-0000-0000-000000000000
SMTP_PASSWORD=not-using-email
```

Then deploy:
```bash
docker run -d --name gcc-webapp --restart unless-stopped \
  -p 5101:8080 \
  -v /var/www/data:/app/data \
  --env-file .env \
  ghcr.io/anukiai/gcc-webapp:production-ready
```

## 🔐 Security Best Practices

### 1. Generate Secure JWT Key
```bash
# Generate a random 64-character key
openssl rand -base64 48
# OR
node -e "console.log(require('crypto').randomBytes(48).toString('base64'))"
```

### 2. Secure File Permissions
```bash
# Protect your .env file
chmod 600 .env
chown root:root .env
```

### 3. Database Security
```bash
# For PostgreSQL, create dedicated user
CREATE USER gcc_user WITH PASSWORD 'your_secure_password';
CREATE DATABASE gcc_database OWNER gcc_user;
GRANT ALL PRIVILEGES ON DATABASE gcc_database TO gcc_user;
```

## 🔍 Configuration Validation

Test your configuration:
```bash
# Check if container starts successfully
docker logs gcc-webapp

# Look for these success indicators:
# ✅ "Application started"
# ✅ "Now listening on: http://[::]:8080"
# ❌ "Production deployment missing critical secrets" = configuration error
```

## 🚨 Common Issues & Solutions

### Issue: "Missing critical secrets"
**Solution**: Ensure all required environment variables are set (see Required section above)

### Issue: "Database connection failed"
**Solution**: Check your connection string format and database accessibility

### Issue: "JWT key too short"
**Solution**: JWT key must be at least 64 characters long

### Issue: "Azure blob storage error"
**Solution**: Set `DataProtection__PersistKeysToAzureBlobStorage=false` for simple deployments

## 📋 Quick Checklist

Before deployment:
- [ ] Database connection string configured
- [ ] JWT secret key (64+ characters)
- [ ] Data protection method chosen (local files or Azure)
- [ ] Azure secrets provided (or dummy values if not using)
- [ ] Email configuration (or dummy values if not using)
- [ ] Test deployment locally first
- [ ] Secure your `.env` file permissions

---
*Configuration complete - ready for secure production deployment!* 🔒