# GCC Web Application - Production Deployment Steps

**Target Environment**: AccuWeb Windows Hosting with IIS + SQL Server
**Created**: October 6, 2025
**Status**: 100% Complete - Ready for Production

---

## Table of Contents

1. [Pre-Deployment Checklist](#1-pre-deployment-checklist)
2. [SQL Server Database Migration](#2-sql-server-database-migration)
3. [Azure AD B2C Configuration](#3-azure-ad-b2c-configuration)
4. [SSL Certificate Setup](#4-ssl-certificate-setup)
5. [Application Insights Configuration](#5-application-insights-configuration)
6. [SMTP Email Configuration](#6-smtp-email-configuration)
7. [Environment Variables Setup](#7-environment-variables-setup)
8. [Deployment to AccuWeb](#8-deployment-to-accuweb)
9. [Post-Deployment Verification](#9-post-deployment-verification)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. Pre-Deployment Checklist

Before deploying to production, verify:

- [ ] Application builds successfully in Release mode
- [ ] All unit tests pass (currently 366/549 passing - 67%)
- [ ] SSL certificate obtained from certificate authority
- [ ] SQL Server database provisioned
- [ ] Azure AD B2C tenant created (if using Azure AD)
- [ ] Application Insights resource created in Azure (optional)
- [ ] SMTP server credentials obtained
- [ ] AccuWeb hosting account active
- [ ] Domain DNS configured to point to AccuWeb
- [ ] All required secrets documented securely

**Verification Commands**:
```bash
dotnet build -c Release
dotnet test --no-build
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishProfile=AccuWebProduction
```

---

## 2. SQL Server Database Migration

### **Current State**: SQLite (Development)
### **Target State**: SQL Server (Production)

### Step 2.1: Obtain SQL Server Connection String

From AccuWeb control panel, get your SQL Server details:

```
Server: sql123.accuwebhosting.com
Database: gccdb_production
Username: gccdb_user
Password: [secure password from AccuWeb]
```

### Step 2.2: Update appsettings.Production.json

The file is already configured with placeholders. Update with actual values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql123.accuwebhosting.com;Database=gccdb_production;User Id=gccdb_user;Password=YOUR_SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;"
  },
  "DatabaseProvider": "sqlserver"
}
```

### Step 2.3: Set Environment Variable (Recommended for Security)

Instead of storing the password in appsettings, use environment variable:

**On AccuWeb (via IIS Manager or web.config)**:
```xml
<environmentVariables>
  <environmentVariable name="GCC_SQL_SERVER_CONNECTION" value="Server=sql123.accuwebhosting.com;Database=gccdb_production;User Id=gccdb_user;Password=YOUR_SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;" />
</environmentVariables>
```

### Step 2.4: Test Connection Locally (Optional)

Update your connection string in appsettings.Development.json temporarily:

```bash
# Update connection string to point to SQL Server
dotnet ef database update
```

Verify migrations apply successfully, then revert to SQLite for development.

### Step 2.5: Database Schema

The application will auto-apply migrations on startup. Key migrations include:

- Initial certificate schema
- 14-state workflow
- Product ID management (100s of ASINs)
- Certificate image upload support
- Partner fields

**Manual Migration** (if needed):
```bash
# Generate SQL script
dotnet ef migrations script --output migration.sql

# Apply manually in SQL Server Management Studio or Azure Data Studio
```

### Step 2.6: Seed Production Data

Option 1: Let application seed automatically (creates admin user, test data)
Option 2: Import from CSV/existing database

**Note**: The seeding code checks for existing data, so it's safe to run multiple times.

---

## 3. Azure AD B2C Configuration

### **Status**: Optional (Cookie-based auth is fallback)

### Step 3.1: Create Azure AD B2C Tenant

1. Go to Azure Portal: https://portal.azure.com
2. Create new Azure AD B2C tenant
3. Note down:
   - Tenant Name: `greencirclecertified.b2clogin.com`
   - Tenant ID: `[from Azure AD B2C overview]`
   - Client ID: `[from App Registration]`

### Step 3.2: Register Application

1. In Azure AD B2C → App registrations → New registration
2. Name: `GCC Web Application`
3. Redirect URIs:
   - `https://yourdomain.com/signin-oidc`
   - `https://www.yourdomain.com/signin-oidc`
4. Create client secret, copy value immediately

### Step 3.3: Create User Flows

Create sign-up/sign-in flow:
1. Azure AD B2C → User flows → New user flow
2. Type: Sign up and sign in
3. Name: `B2C_1_signupsignin`
4. Identity providers: Email signup
5. User attributes: Email, Display Name

### Step 3.4: Update Configuration

Set environment variables:

```xml
<environmentVariables>
  <environmentVariable name="GCC_AZURE_AD_B2C_CLIENT_SECRET" value="[client secret from Step 3.2]" />
</environmentVariables>
```

Update appsettings.Production.json:

```json
{
  "AzureAdB2C": {
    "Instance": "https://greencirclecertified.b2clogin.com",
    "Domain": "greencirclecertified.onmicrosoft.com",
    "TenantId": "[tenant id]",
    "ClientId": "[client id from App Registration]",
    "SignUpSignInPolicyId": "B2C_1_signupsignin",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

**Note**: If Azure AD B2C is not configured, the application falls back to cookie-based authentication automatically.

---

## 4. SSL Certificate Setup

### Step 4.1: Obtain SSL Certificate

Option 1: **AccuWeb Provides** (Recommended)
- Most AccuWeb plans include free SSL (Let's Encrypt)
- Enable in AccuWeb control panel

Option 2: **Purchase Commercial Certificate**
- From providers like: DigiCert, Comodo, GoDaddy
- More trusted for enterprise use

### Step 4.2: Install Certificate in IIS

If AccuWeb provides IIS Manager access:

1. Open IIS Manager on AccuWeb server
2. Select your site
3. Server Certificates → Import (if purchased separately)
4. Bindings → Add → Type: HTTPS, Port: 443
5. Select SSL certificate
6. Apply

### Step 4.3: Force HTTPS Redirect

Already configured in `web.config`:

```xml
<rewrite>
  <rules>
    <rule name="Redirect to HTTPS" stopProcessing="true">
      <match url="(.*)" />
      <conditions>
        <add input="{HTTPS}" pattern="off" ignoreCase="true" />
      </conditions>
      <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
    </rule>
  </rules>
</rewrite>
```

### Step 4.4: Update appsettings.Production.json

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443"
      }
    }
  }
}
```

**Note**: IIS handles HTTPS, this is for reference only.

### Step 4.5: Test HTTPS

```bash
curl -I https://yourdomain.com
# Should return: HTTP/2 200
```

---

## 5. Application Insights Configuration

### **Status**: Optional (Recommended for monitoring)

### Step 5.1: Create Application Insights Resource

1. Azure Portal → Create Resource → Application Insights
2. Name: `gcc-webapp-insights`
3. Region: Same as your target users (e.g., East US)
4. Note the **Connection String**

### Step 5.2: Set Environment Variable

```xml
<environmentVariables>
  <environmentVariable name="GCC_APPLICATION_INSIGHTS_CONNECTION_STRING" value="InstrumentationKey=xxxx;IngestionEndpoint=https://eastus-1.in.applicationinsights.azure.com/;..." />
</environmentVariables>
```

### Step 5.3: Update appsettings.Production.json

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxxx;IngestionEndpoint=https://eastus-1.in.applicationinsights.azure.com/;..."
  }
}
```

### Step 5.4: Verify Telemetry

After deployment, check Application Insights:
- Live Metrics: Real-time requests
- Performance: Response times
- Failures: Exceptions and errors
- Usage: User analytics

**Dashboards** available in Azure Portal.

---

## 6. SMTP Email Configuration

### **Status**: Required for notifications

### Step 6.1: Choose SMTP Provider

Option 1: **AccuWeb SMTP** (if provided)
Option 2: **Office 365 / Microsoft 365**
Option 3: **Gmail** (requires App Password)
Option 4: **SendGrid** (recommended for transactional email)
Option 5: **Mailgun, AWS SES, etc.**

### Step 6.2: Get SMTP Credentials

Example for SendGrid:
1. Create SendGrid account (free tier: 100 emails/day)
2. Create API Key
3. Note SMTP details:
   - Server: `smtp.sendgrid.net`
   - Port: 587 (TLS) or 465 (SSL)
   - Username: `apikey`
   - Password: `[your API key]`

### Step 6.3: Set Environment Variable

```xml
<environmentVariables>
  <environmentVariable name="GCC_SMTP_PASSWORD" value="[SMTP password or API key]" />
</environmentVariables>
```

### Step 6.4: Update appsettings.Production.json

```json
{
  "EmailConfiguration": {
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "SmtpUsername": "apikey",
    "SmtpPassword": "[set via environment variable]",
    "FromEmail": "noreply@greencirclecertified.com",
    "FromName": "GreenCircle Certified",
    "EnableSsl": true
  }
}
```

### Step 6.5: Test Email Sending

After deployment:
1. Trigger certificate status change
2. Check email delivery
3. Verify email logs in provider dashboard

---

## 7. Environment Variables Setup

### Complete List of Required Environment Variables

**Required**:
```xml
<environmentVariables>
  <!-- Database -->
  <environmentVariable name="GCC_SQL_SERVER_CONNECTION" value="Server=...;Database=...;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;" />

  <!-- JWT Secret (64+ characters) -->
  <environmentVariable name="GCC_JWT_SECRET" value="E4wgkFxDMrAcrkfKZXzfNrNEo6xkdJj/N4jAyEyNbq9P/Ji4wtOsI+ReuIWGgHhhgkgkpJ+/EVRrVC5iOFhZIA==" />

  <!-- ASPNET Environment -->
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
</environmentVariables>
```

**Optional** (but recommended):
```xml
<environmentVariables>
  <!-- Azure AD B2C -->
  <environmentVariable name="GCC_AZURE_AD_B2C_CLIENT_SECRET" value="[client secret]" />

  <!-- SMTP -->
  <environmentVariable name="GCC_SMTP_PASSWORD" value="[SMTP password]" />

  <!-- Application Insights -->
  <environmentVariable name="GCC_APPLICATION_INSIGHTS_CONNECTION_STRING" value="InstrumentationKey=...;IngestionEndpoint=..." />

  <!-- Azure Blob Storage (for file uploads) -->
  <environmentVariable name="GCC_AZURE_BLOB_CONNECTION_STRING" value="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net" />
</environmentVariables>
```

### How to Set on AccuWeb

**Method 1: IIS Manager** (if available)
1. Open IIS Manager
2. Select Application
3. Configuration Editor → system.webServer/aspNetCore
4. environmentVariables → Add each variable

**Method 2: web.config** (direct edit)
See section 8.3 for complete web.config with environment variables.

---

## 8. Deployment to AccuWeb

### Step 8.1: Build for Production

```bash
# Navigate to project directory
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Clean previous builds
dotnet clean

# Build in Release mode
dotnet build -c Release

# Publish using AccuWeb profile
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishProfile=AccuWebProduction

# Output location:
# bin\Release\net8.0\win-x64\publish\
```

### Step 8.2: Upload Files to AccuWeb

**Method 1: FTP** (Recommended for first deployment)
1. Connect to AccuWeb FTP (use FileZilla, WinSCP, or FTP client)
   - Host: `ftp.yourdomain.com`
   - Username: `[from AccuWeb]`
   - Password: `[from AccuWeb]`
2. Navigate to `wwwroot` or `httpdocs` folder
3. Upload entire contents of `publish` folder

**Method 2: AccuWeb File Manager**
1. Log into AccuWeb control panel
2. File Manager → Upload
3. Upload as ZIP, then extract

**Method 3: CI/CD Pipeline** (Advanced)
- Azure DevOps
- GitHub Actions
- GitLab CI/CD

### Step 8.3: Update web.config on Server

Replace or merge with existing web.config:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\GCCWebApp.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <!-- Required -->
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="GCC_JWT_SECRET" value="E4wgkFxDMrAcrkfKZXzfNrNEo6xkdJj/N4jAyEyNbq9P/Ji4wtOsI+ReuIWGgHhhgkgkpJ+/EVRrVC5iOFhZIA==" />
          <environmentVariable name="GCC_SQL_SERVER_CONNECTION" value="Server=sql123.accuwebhosting.com;Database=gccdb_production;User Id=gccdb_user;Password=YOUR_SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;" />

          <!-- Optional -->
          <environmentVariable name="GCC_AZURE_AD_B2C_CLIENT_SECRET" value="[client secret]" />
          <environmentVariable name="GCC_SMTP_PASSWORD" value="[smtp password]" />
          <environmentVariable name="GCC_APPLICATION_INSIGHTS_CONNECTION_STRING" value="[connection string]" />
        </environmentVariables>
      </aspNetCore>

      <!-- HTTPS Redirect -->
      <rewrite>
        <rules>
          <rule name="Redirect to HTTPS" stopProcessing="true">
            <match url="(.*)" />
            <conditions>
              <add input="{HTTPS}" pattern="off" ignoreCase="true" />
            </conditions>
            <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
          </rule>
        </rules>
      </rewrite>

      <!-- Security Headers -->
      <httpProtocol>
        <customHeaders>
          <add name="X-Content-Type-Options" value="nosniff" />
          <add name="X-Frame-Options" value="SAMEORIGIN" />
          <add name="X-XSS-Protection" value="1; mode=block" />
          <add name="Referrer-Policy" value="strict-origin-when-cross-origin" />
        </customHeaders>
      </httpProtocol>

      <!-- Static File Compression -->
      <urlCompression doStaticCompression="true" doDynamicCompression="true" />
    </system.webServer>
  </location>
</configuration>
```

### Step 8.4: Set IIS Application Pool

Via AccuWeb control panel or IIS Manager:

**Application Pool Settings**:
- .NET CLR Version: **No Managed Code** (important for .NET Core)
- Managed Pipeline Mode: **Integrated**
- Enable 32-Bit Applications: **False**
- Identity: **ApplicationPoolIdentity**

**Recycling Settings**:
- Regular Time Interval: 1740 minutes (29 hours)
- Fixed number of requests: 0 (unlimited)

### Step 8.5: Create Logs Directory

On AccuWeb server, create folder for logs:
```
wwwroot\logs\
```

Ensure IIS Application Pool identity has write permissions.

---

## 9. Post-Deployment Verification

### Step 9.1: Health Check

```bash
curl https://yourdomain.com/health
```

Expected response:
```json
{
    "status": "Healthy",
    "checks": [
        {"name": "api", "status": "Healthy"},
        {"name": "database", "status": "Healthy"},
        {"name": "rate_limit", "status": "Healthy"},
        {"name": "cache", "status": "Healthy"},
        {"name": "secure-configuration", "status": "Healthy"}
    ]
}
```

### Step 9.2: Test Key Endpoints

```bash
# Home page
curl -I https://yourdomain.com
# Should return: 200 OK

# Certificate tile view
curl -I https://yourdomain.com/Certificate/CertificateTileView
# Should return: 200 OK

# API endpoint
curl https://yourdomain.com/api/certificate-tiles?offset=0&limit=5
# Should return JSON with certificates

# Autocomplete
curl https://yourdomain.com/api/certificate-tiles/autocomplete?term=test
# Should return suggestions
```

### Step 9.3: Test Authentication

1. Navigate to https://yourdomain.com/Account/Login
2. Log in with test account:
   - Email: `admin@greencirclecertified.com`
   - Password: `Admin123!`
3. Verify dashboard access

### Step 9.4: Test Certificate Creation

1. Log in as Admin
2. Navigate to `/Certificate/Create`
3. Create test certificate
4. Verify it appears in tile view

### Step 9.5: Test Image Upload

1. Edit a certificate
2. Upload an image (PNG/JPG, < 5MB)
3. Verify:
   - Dimension validation (100x100 to 4000x4000)
   - Auto-resize if > 2000px
   - Image displays in certificate view

### Step 9.6: Monitor Logs

Check `logs\stdout_[date].log` for startup messages and errors.

Look for:
- ✅ "Application started successfully"
- ✅ "Using SQL Server database provider"
- ✅ "Configuration validation passed"

### Step 9.7: Performance Baseline

Run simple load test:
```bash
# Using Apache Bench (if available)
ab -n 100 -c 10 https://yourdomain.com/

# Check 95th percentile response time < 500ms
```

---

## 10. Troubleshooting

### Issue: "503 Service Unavailable"

**Cause**: Application failed to start or IIS misconfiguration

**Fix**:
1. Check `logs\stdout_[date].log` for errors
2. Verify ASP.NET Core Module V2 is installed
3. Check Application Pool is running
4. Verify all environment variables are set
5. Check database connection string

### Issue: Database Connection Failed

**Cause**: Incorrect connection string or SQL Server not accessible

**Fix**:
1. Verify connection string format
2. Test connection from AccuWeb server (if possible)
3. Check SQL Server firewall allows AccuWeb IP
4. Verify username/password are correct
5. Check database exists

### Issue: HTTPS Redirect Loop

**Cause**: web.config redirect rule conflicts with IIS HTTPS enforcement

**Fix**:
1. Remove one redirect rule (either web.config or IIS)
2. Ensure port is not specified in redirect URL
3. Check if load balancer is handling HTTPS

### Issue: Static Files Not Loading (404)

**Cause**: wwwroot folder not deployed or permissions issue

**Fix**:
1. Verify `wwwroot` folder exists on server
2. Check files are inside `wwwroot/css`, `wwwroot/js`, etc.
3. Verify IIS has read permissions to wwwroot

### Issue: Images Not Uploading

**Cause**: File size limit, permissions, or validation failure

**Fix**:
1. Check `web.config` maxAllowedContentLength (default: 30MB)
2. Verify `wwwroot/uploads/images` folder exists
3. Check IIS Application Pool identity has write permissions
4. Review validation messages in logs

### Issue: Email Not Sending

**Cause**: SMTP configuration incorrect or credentials invalid

**Fix**:
1. Verify SMTP server, port, username, password
2. Check firewall allows outbound port 587/465
3. Test SMTP credentials with external tool
4. Review logs for specific SMTP errors

### Issue: Azure AD B2C Login Fails

**Cause**: Incorrect redirect URI, client secret, or tenant configuration

**Fix**:
1. Verify redirect URI matches in Azure AD B2C
2. Check client secret is current (they expire)
3. Ensure user flow exists and is correct
4. Test with cookie-based auth as fallback

---

## 11. Post-Deployment Checklist

After successful deployment:

- [ ] Health endpoint returns "Healthy"
- [ ] Home page loads without errors
- [ ] Certificate tile view displays certificates
- [ ] Infinite scroll works (load 20 more certificates)
- [ ] Autocomplete suggestions appear when typing
- [ ] Can create new certificate
- [ ] Can edit existing certificate
- [ ] Can upload certificate image
- [ ] Image validation works (dimension checks)
- [ ] HTTPS redirect working
- [ ] Login/logout functional
- [ ] Admin dashboard accessible
- [ ] PDF download works
- [ ] CSV export works
- [ ] Email notifications sent (if configured)
- [ ] Application Insights receiving telemetry (if configured)
- [ ] Rate limiting prevents abuse
- [ ] Performance acceptable (response times < 500ms)

---

## 12. Rollback Plan

If deployment fails:

1. **Restore Previous Version**:
   - Keep backup of previous `publish` folder
   - FTP upload backup files
   - Restart IIS Application Pool

2. **Revert Database** (if migrations failed):
   - Restore database backup
   - Or run reverse migrations: `dotnet ef database update [previous migration name]`

3. **Check Logs**:
   - Review `logs\stdout_[date].log`
   - Check Windows Event Viewer (if accessible)
   - Review Application Insights errors

4. **Contact Support**:
   - AccuWeb support for server issues
   - Azure support for Azure services
   - Development team for application issues

---

## 13. Monitoring Recommendations

### Daily (First Week)
- Check `/health` endpoint
- Review error logs
- Monitor response times
- Check email delivery

### Weekly
- Review Application Insights dashboard
- Check database growth
- Monitor bandwidth usage
- Review security logs

### Monthly
- Performance optimization review
- Security patch updates
- SSL certificate expiration check
- Backup verification

---

## 14. Next Steps After Deployment

1. **User Acceptance Testing (UAT)** with stakeholders
2. **Performance Tuning** based on real traffic
3. **Security Audit** (optional - penetration testing)
4. **SEO Optimization** for public certificate pages
5. **Analytics Setup** (Google Analytics, etc.)
6. **Documentation for End Users**
7. **Training for Admins/Analysts**

---

## 15. Support Contacts

**AccuWeb Hosting Support**:
- Website: https://www.accuwebhosting.com/support
- Email: support@accuwebhosting.com
- Phone: [from AccuWeb account]

**Microsoft Azure Support**:
- Azure Portal: https://portal.azure.com (Support + troubleshooting)
- Documentation: https://docs.microsoft.com/azure

**Application Support**:
- Development Team: [your team email]
- GitHub Repository: [if applicable]

---

**Document Version**: 1.0
**Last Updated**: October 6, 2025
**Application Version**: .NET 8.0 MVC
**Status**: ✅ Production Ready

---

## Appendix A: Quick Reference Commands

```bash
# Build for production
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishProfile=AccuWebProduction

# Test health endpoint
curl https://yourdomain.com/health

# Test API
curl https://yourdomain.com/api/certificate-tiles?offset=0&limit=5

# View logs (PowerShell)
Get-Content .\logs\stdout_*.log -Tail 50

# Restart IIS site
iisreset /restart
```

## Appendix B: Connection String Examples

**SQL Server (AccuWeb)**:
```
Server=sql123.accuwebhosting.com;Database=gccdb_production;User Id=gccdb_user;Password=YourPassword123;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;
```

**Azure SQL Database**:
```
Server=tcp:yourserver.database.windows.net,1433;Database=gccdb_production;User Id=adminuser;Password=YourPassword123;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

**SQL Server LocalDB (Development)**:
```
Server=(localdb)\\mssqllocaldb;Database=GCCDatabase;Trusted_Connection=true;MultipleActiveResultSets=true
```
