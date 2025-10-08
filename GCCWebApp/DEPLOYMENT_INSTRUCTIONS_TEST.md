# GCC Web Application - Test Environment Deployment Instructions

**Deployment Package:** `GCCWebApp-TestDeploy.tar.gz` (59 MB)
**Target Environment:** AccuWeb Windows Hosting - Test Environment
**Date:** October 7, 2025
**Application Version:** ASP.NET Core 8.0 / .NET 8.0

---

## Pre-Deployment Checklist

### 1. Server Requirements
- [ ] Windows Server with IIS 10.0 or higher
- [ ] .NET 8.0 Runtime (ASP.NET Core Runtime) installed
- [ ] SQL Server (Express, Standard, or higher) OR SQLite support
- [ ] SSL Certificate configured for HTTPS
- [ ] Minimum 2GB RAM, 5GB disk space

### 2. Database Preparation
Choose ONE of the following database options:

#### Option A: SQL Server (Recommended for Production-like Testing)
- [ ] SQL Server instance accessible
- [ ] Database created: `GCCDatabase_Test`
- [ ] Database user created with db_owner permissions
- [ ] Connection string ready (see Configuration section)

#### Option B: SQLite (Quick Testing)
- [ ] SQLite database file will be created automatically
- [ ] Ensure write permissions to application directory

### 3. IIS Configuration
- [ ] Application Pool created (Name: `GCCWebApp-Test`)
  - .NET CLR Version: No Managed Code
  - Managed Pipeline Mode: Integrated
  - Identity: ApplicationPoolIdentity
  - Start Mode: AlwaysRunning (optional)
- [ ] Website/Application created
  - Physical Path: `C:\inetpub\wwwroot\GCCWebApp-Test` (or your preferred path)
  - Application Pool: `GCCWebApp-Test`
  - Binding: HTTP (port 80) and HTTPS (port 443)

---

## Deployment Steps

### Step 1: Extract Deployment Package
1. Upload `GCCWebApp-TestDeploy.tar.gz` to your server
2. Extract contents to your application directory:
   ```
   C:\inetpub\wwwroot\GCCWebApp-Test\
   ```
3. Verify all files extracted correctly:
   - `GCCWebApp.dll`
   - `GCCWebApp.exe`
   - `web.config`
   - `appsettings.json`
   - `appsettings.Production.json`
   - `wwwroot/` folder with static files

### Step 2: Configure Application Settings

#### A. Database Connection (CRITICAL)

Edit `appsettings.Production.json` or use environment variables:

**For SQL Server:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SQL_SERVER;Database=GCCDatabase_Test;User Id=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;"
}
```

**For SQLite (Testing Only):**
```json
"ConnectionStrings": {
  "SqliteConnection": "Data Source=gcc_database_test.db"
}
```

#### B. JWT Secret Key (CRITICAL - SECURITY)
Generate a new 64+ character random key:
```json
"Jwt": {
  "Key": "GENERATE_A_NEW_64_CHARACTER_RANDOM_KEY_HERE",
  "Issuer": "GCCWebApp-Test",
  "Audience": "GCCWebApp-Test",
  "ExpiryMinutes": 60
}
```

#### C. CORS Origins (Update with your test domain)
```json
"AllowedOrigins": [
  "https://your-test-domain.com",
  "https://www.your-test-domain.com"
]
```

#### D. Feature Flags for Test Environment
```json
"FeatureFlags": {
  "EnableDetailedErrorMessages": true,
  "EnableDebugEndpoints": false,
  "EnableSwagger": true,
  "EnableDeveloperExceptionPage": false,
  "RequireHttpsForAllRequests": true,
  "EnableSecurityHeaders": true,
  "EnableCorsRestrictions": true
}
```

### Step 3: Set Environment Variables

**IMPORTANT:** Set `ASPNETCORE_ENVIRONMENT` to `Production` (for production-like testing) or `Test`:

In IIS Application Pool Configuration:
1. Select Application Pool > Advanced Settings
2. Add Environment Variable:
   - Name: `ASPNETCORE_ENVIRONMENT`
   - Value: `Production` or `Test`

Alternatively, in `web.config`:
```xml
<aspNetCore processPath="dotnet"
            arguments=".\GCCWebApp.dll"
            stdoutLogEnabled="false"
            stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  </environmentVariables>
</aspNetCore>
```

### Step 4: Set Folder Permissions

**Grant IIS Application Pool Identity permissions:**

```powershell
# Run as Administrator
icacls "C:\inetpub\wwwroot\GCCWebApp-Test" /grant "IIS AppPool\GCCWebApp-Test:(OI)(CI)RX" /t
icacls "C:\inetpub\wwwroot\GCCWebApp-Test\logs" /grant "IIS AppPool\GCCWebApp-Test:(OI)(CI)F" /t
```

**Create logs directory if it doesn't exist:**
```powershell
New-Item -Path "C:\inetpub\wwwroot\GCCWebApp-Test\logs" -ItemType Directory -Force
```

### Step 5: Initialize Database

**Run migrations to create/update database schema:**

```powershell
cd C:\inetpub\wwwroot\GCCWebApp-Test
dotnet GCCWebApp.dll --migrate

# OR manually using Entity Framework CLI
dotnet ef database update --connection "YOUR_CONNECTION_STRING"
```

**Seed initial data (Admin user, roles):**
The application will automatically seed on first run.

**Test Credentials (created on first run):**
- Admin: `admin@greencirclecertified.com` / `Admin123!`
- Analyst: `analyst@greencirclecertified.com` / `Analyst123!`
- External: `partner@amazon.com` / `Partner123!`

### Step 6: Start Application

1. Start the Application Pool in IIS Manager
2. Start the Website/Application
3. Browse to: `https://your-test-domain.com`

---

## Post-Deployment Verification

### 1. Health Check Endpoint
```
GET https://your-test-domain.com/health
Expected: 200 OK with "Healthy" response
```

### 2. Public Certificate Tile View
```
GET https://your-test-domain.com/Certificate/CertificateTileView
Expected: Certificate listing page with infinite scroll
```

### 3. Login Page
```
GET https://your-test-domain.com/Account/Login
Expected: Login form
```

### 4. API Endpoint (if Swagger enabled)
```
GET https://your-test-domain.com/swagger
Expected: Swagger UI documentation
```

### 5. Test Authentication
1. Navigate to `/Account/Login`
2. Login with admin credentials
3. Verify redirect to `/Dashboard`
4. Check user role permissions

---

## Configuration Files Reference

### Required Files in Deployment Package
- `GCCWebApp.dll` - Main application assembly
- `GCCWebApp.exe` - Windows executable launcher
- `web.config` - IIS configuration
- `appsettings.json` - Base configuration
- `appsettings.Production.json` - Production overrides
- `wwwroot/` - Static files (CSS, JS, images)
- `LatoFont/` - PDF generation fonts

### Critical Configuration Settings

#### 1. Connection String (Choose ONE)
```json
// SQL Server
"DefaultConnection": "Server=...;Database=...;User Id=...;Password=...;"

// PostgreSQL (if using)
"PostgresConnection": "Host=...;Port=5432;Database=...;Username=...;Password=...;"

// SQLite (testing only)
"SqliteConnection": "Data Source=gcc_database_test.db"
```

#### 2. JWT Settings (MUST CHANGE)
```json
"Jwt": {
  "Key": "YOUR_64_PLUS_CHARACTER_SECRET_KEY_HERE",
  "Issuer": "GCCWebApp-Test",
  "Audience": "GCCWebApp-Test",
  "ExpiryMinutes": 60
}
```

#### 3. Authentication Settings
```json
"Authentication": {
  "Enable2FA": true,
  "EnableTestMode": false,
  "RequireEmailConfirmation": false
}
```

#### 4. Rate Limiting (API Protection)
```json
"IpRateLimiting": {
  "EnableEndpointRateLimiting": true,
  "GeneralRules": [
    {
      "Endpoint": "*/api/*",
      "Period": "1m",
      "Limit": 60
    }
  ]
}
```

---

## Troubleshooting

### Application Won't Start
1. Check IIS Application Pool is running
2. Check Event Viewer > Application logs
3. Enable stdout logging in `web.config`:
   ```xml
   <aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout">
   ```
4. Check `logs\stdout-*.log` files

### Database Connection Errors
1. Verify connection string is correct
2. Test database connectivity from server
3. Check SQL Server allows remote connections
4. Verify firewall rules for SQL Server port (1433)
5. Check user has proper database permissions

### 500 Internal Server Error
1. Check `ASPNETCORE_ENVIRONMENT` is set correctly
2. Review application logs in `logs/` directory
3. Check Event Viewer for detailed error messages
4. Verify all required .NET 8.0 components installed

### Static Files Not Loading (CSS/JS)
1. Verify `wwwroot/` folder exists and has files
2. Check IIS permissions on wwwroot folder
3. Verify static file middleware is configured

### Database Migration Fails
1. Ensure database user has CREATE TABLE permissions
2. Check database connection string
3. Run migrations manually:
   ```
   dotnet ef database update --connection "YOUR_CONNECTION_STRING"
   ```

---

## Security Checklist for Test Environment

- [ ] Changed default JWT secret key
- [ ] Updated connection strings with strong passwords
- [ ] Configured HTTPS with valid SSL certificate
- [ ] Changed default admin password after first login
- [ ] Disabled Swagger in production (set `EnableSwagger: false`)
- [ ] Enabled CORS restrictions with specific origins
- [ ] Set `EnableDetailedErrorMessages: false` for production
- [ ] Configured rate limiting for API endpoints
- [ ] Set up log monitoring
- [ ] Backed up database before deployment

---

## Application URLs (Update with your domain)

**Public Access:**
- Certificate Listing: `https://your-domain.com/Certificate/CertificateTileView`
- Certificate Details: `https://your-domain.com/Certificate/Details/{id}`
- Login: `https://your-domain.com/Account/Login`

**Admin/Analyst Access (Requires Authentication):**
- Dashboard: `https://your-domain.com/Dashboard`
- Certificate Management: `https://your-domain.com/Certificate/Create`
- User Management (Admin): `https://your-domain.com/Admin/UserManagement`
- Role Management (Admin): `https://your-domain.com/Admin/RoleManagement`

**API Endpoints:**
- Swagger UI: `https://your-domain.com/swagger`
- Health Check: `https://your-domain.com/health`
- Certificate API: `https://your-domain.com/api/v1/certificates`

---

## Support Information

**Application Log Locations:**
- Application Logs: `C:\inetpub\wwwroot\GCCWebApp-Test\logs\`
- IIS Logs: `C:\inetpub\logs\LogFiles\`
- Windows Event Logs: Event Viewer > Application

**IIS Management Commands:**
```powershell
# View Application Pool Status
Get-IISAppPool -Name "GCCWebApp-Test"

# Stop Application Pool
Stop-IISAppPool -Name "GCCWebApp-Test"

# Start Application Pool
Start-IISAppPool -Name "GCCWebApp-Test"

# Recycle Application Pool
Restart-IISAppPool -Name "GCCWebApp-Test"
```

---

## Next Steps After Deployment

1. **Test Core Functionality:**
   - [ ] Login with admin credentials
   - [ ] Create a test certificate
   - [ ] Upload certificate files
   - [ ] Test certificate search and filtering
   - [ ] Export certificates (CSV, Excel, PDF)
   - [ ] Test API endpoints

2. **Performance Testing:**
   - [ ] Load test with multiple concurrent users
   - [ ] Test infinite scroll with large datasets
   - [ ] Monitor memory and CPU usage

3. **Security Testing:**
   - [ ] Verify authentication and authorization
   - [ ] Test rate limiting
   - [ ] Verify HTTPS enforcement
   - [ ] Test XSS and CSRF protections

4. **Integration Testing:**
   - [ ] Test external partner API access
   - [ ] Verify email notifications (if configured)
   - [ ] Test file upload/download

---

**Deployment Package Location:**
`C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\GCCWebApp-TestDeploy.tar.gz`

**Build Configuration:** Release | win-x64 | Framework-dependent
**Build Date:** October 7, 2025

---

For questions or issues, refer to the main project documentation:
- `CLAUDE.md` - Development guide
- `0_Documentation/docs/Requirements.md` - User stories and requirements
- `0_Documentation/docs/MANUAL_UAT_TEST_CASES.md` - Test cases
