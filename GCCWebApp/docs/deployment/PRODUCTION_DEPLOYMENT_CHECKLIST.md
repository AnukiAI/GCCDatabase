# Production Deployment Checklist

**Target**: AccuWeb Windows Hosting + SQL Server
**Date**: October 6, 2025
**Status**: Ready for Production Deployment

---

## Pre-Deployment (Complete Before Starting)

### Environment Preparation
- [ ] AccuWeb hosting account active and accessible
- [ ] Domain registered and DNS configured to AccuWeb
- [ ] SSL certificate obtained or Let's Encrypt enabled
- [ ] SQL Server database provisioned on AccuWeb
- [ ] FTP/File Manager access verified
- [ ] IIS Manager access verified (if available)

### Credentials Collected
- [ ] SQL Server connection string (Server, Database, Username, Password)
- [ ] SMTP server credentials (Server, Port, Username, Password)
- [ ] Azure AD B2C details (optional): Tenant, ClientId, ClientSecret
- [ ] Application Insights connection string (optional)
- [ ] JWT secret key (64+ characters) - **Use existing or generate new**

### Local Verification
- [ ] Application builds successfully: `dotnet build -c Release`
- [ ] Core tests pass: `dotnet test --filter "FullyQualifiedName~FileUploadServiceTests|CertificateServiceTests"`
- [ ] Health check works locally: `curl http://localhost:5101/health`
- [ ] Backup of current production (if updating existing deployment)

---

## Step 1: Build & Publish

- [ ] Open terminal in project directory
- [ ] Run: `dotnet clean`
- [ ] Run: `dotnet build -c Release`
- [ ] Run: `dotnet publish -c Release -r win-x64 --self-contained false /p:PublishProfile=AccuWebProduction`
- [ ] Verify publish folder exists: `bin\Release\net8.0\win-x64\publish\`
- [ ] Check publish folder size (should be ~150-200MB)

---

## Step 2: Configure Production Settings

### Update web.config in publish folder

- [ ] Open `bin\Release\net8.0\win-x64\publish\web.config`
- [ ] Set `ASPNETCORE_ENVIRONMENT` to `Production`
- [ ] Add `GCC_SQL_SERVER_CONNECTION` environment variable with SQL Server connection string
- [ ] Add `GCC_JWT_SECRET` environment variable (use secure 64+ char string)
- [ ] Add `GCC_SMTP_PASSWORD` environment variable (if using email)
- [ ] Add `GCC_AZURE_AD_B2C_CLIENT_SECRET` (if using Azure AD)
- [ ] Add `GCC_APPLICATION_INSIGHTS_CONNECTION_STRING` (if using App Insights)
- [ ] Verify HTTPS redirect rule is present
- [ ] Verify security headers are set
- [ ] Save and close

**Example Environment Variables Section**:
```xml
<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  <environmentVariable name="GCC_JWT_SECRET" value="YOUR_64_CHAR_SECRET_HERE" />
  <environmentVariable name="GCC_SQL_SERVER_CONNECTION" value="Server=sql123.accuwebhosting.com;Database=gccdb_production;User Id=gccdb_user;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;" />
  <environmentVariable name="GCC_SMTP_PASSWORD" value="YOUR_SMTP_PASSWORD" />
</environmentVariables>
```

---

## Step 3: Upload Files to AccuWeb

### Via FTP
- [ ] Connect to AccuWeb FTP (Host: ftp.yourdomain.com)
- [ ] Navigate to `wwwroot` or `httpdocs` folder
- [ ] Backup existing files (if any) by renaming folder to `wwwroot_backup_[date]`
- [ ] Upload entire contents of `publish` folder
- [ ] Verify all files uploaded (check `wwwroot`, `logs` folders exist)
- [ ] Check file count matches local publish folder

### Via AccuWeb File Manager
- [ ] Log into AccuWeb control panel
- [ ] Open File Manager
- [ ] Navigate to website root
- [ ] Backup existing files
- [ ] Upload publish folder as ZIP
- [ ] Extract ZIP file
- [ ] Verify extraction successful

---

## Step 4: Database Setup

- [ ] Verify SQL Server is accessible from AccuWeb
- [ ] Database exists: `gccdb_production` (or your chosen name)
- [ ] User has permissions: SELECT, INSERT, UPDATE, DELETE, CREATE TABLE
- [ ] Test connection from AccuWeb (if possible)

**Note**: Application will auto-apply EF Core migrations on first start. No manual SQL scripts needed unless you prefer manual control.

### If Manual Migration Preferred
- [ ] Generate SQL script: `dotnet ef migrations script --output migration.sql`
- [ ] Connect to SQL Server with SQL Server Management Studio or Azure Data Studio
- [ ] Execute migration.sql
- [ ] Verify tables created

---

## Step 5: IIS Configuration

### Application Pool Settings
- [ ] Open IIS Manager on AccuWeb (if available)
- [ ] Locate your application's Application Pool
- [ ] .NET CLR Version: **No Managed Code**
- [ ] Managed Pipeline Mode: **Integrated**
- [ ] Enable 32-Bit Applications: **False**
- [ ] Identity: **ApplicationPoolIdentity**
- [ ] Recycling: 1740 minutes (29 hours)
- [ ] Save settings
- [ ] Restart Application Pool

### Site Bindings
- [ ] Verify HTTP binding: Port 80
- [ ] Add HTTPS binding: Port 443
- [ ] Select SSL certificate (Let's Encrypt or purchased)
- [ ] Save bindings

### Permissions
- [ ] Verify IIS Application Pool identity has read access to wwwroot
- [ ] Verify write access to `logs` folder
- [ ] Verify write access to `wwwroot/uploads` folder

---

## Step 6: Create Required Folders

On AccuWeb server, create if not exist:

- [ ] `wwwroot/logs` (for stdout logs)
- [ ] `wwwroot/uploads/certificates` (for certificate PDFs)
- [ ] `wwwroot/uploads/images` (for certificate images)

Set permissions: IIS Application Pool identity needs **Modify** access.

---

## Step 7: SSL Certificate

### If Using Let's Encrypt (AccuWeb Provides)
- [ ] Enable in AccuWeb control panel
- [ ] Select domain(s) to secure
- [ ] Wait for certificate issuance (~5 minutes)
- [ ] Verify HTTPS works: `https://yourdomain.com`

### If Using Purchased Certificate
- [ ] Upload certificate files (.pfx or .cer + .key)
- [ ] Import in IIS Manager → Server Certificates
- [ ] Assign to site binding (port 443)
- [ ] Verify HTTPS works

---

## Step 8: Initial Startup & Verification

### Start Application
- [ ] In AccuWeb control panel or IIS Manager, start/restart site
- [ ] Wait 30 seconds for application initialization
- [ ] Check application pool status (should be Started)

### Check Health Endpoint
- [ ] Open browser: `https://yourdomain.com/health`
- [ ] Expected: `{"status":"Healthy",...}`
- [ ] All 5 checks show "Healthy":
  - [ ] api
  - [ ] database
  - [ ] rate_limit
  - [ ] cache
  - [ ] secure-configuration

### Check Logs
- [ ] Open `logs/stdout_[date].log` on server
- [ ] Look for: "Application started successfully"
- [ ] Look for: "Using SQL Server database provider"
- [ ] Look for: "Configuration validation passed"
- [ ] No errors or exceptions

---

## Step 9: Functional Testing

### Basic Navigation
- [ ] Home page loads: `https://yourdomain.com`
- [ ] Certificate tile view: `https://yourdomain.com/Certificate/CertificateTileView`
- [ ] Login page: `https://yourdomain.com/Account/Login`
- [ ] All static files load (CSS, JS, images)

### Authentication
- [ ] Log in with admin account: `admin@greencirclecertified.com` / `Admin123!`
- [ ] Dashboard loads successfully
- [ ] Can navigate to admin pages
- [ ] Log out works

### Certificate Management
- [ ] View certificates in tile view
- [ ] Click tile opens details in new tab
- [ ] Infinite scroll loads more certificates (scroll to bottom)
- [ ] Search/filter works
- [ ] Autocomplete suggestions appear when typing
- [ ] Create new certificate (as Admin)
- [ ] Edit certificate
- [ ] Upload certificate image (PNG/JPG < 5MB)
- [ ] Download certificate PDF

### API Endpoints
- [ ] `GET /api/certificate-tiles?offset=0&limit=20` returns JSON
- [ ] `GET /api/certificate-tiles/autocomplete?term=test` returns suggestions
- [ ] Rate limiting works (try 100 rapid requests - should get 429 after 60)

### Error Handling
- [ ] Navigate to non-existent page: `/does-not-exist` (should show 404)
- [ ] Try to access admin page as non-admin (should be denied)
- [ ] Upload invalid image (> 5MB or wrong format) - should show validation error

---

## Step 10: Performance Verification

- [ ] Home page loads < 2 seconds
- [ ] Certificate tile view loads < 2 seconds
- [ ] API response times < 500ms (check /health endpoint)
- [ ] Image uploads complete < 5 seconds
- [ ] PDF generation < 3 seconds
- [ ] No console errors in browser (F12 Developer Tools)

---

## Step 11: Security Verification

- [ ] HTTP automatically redirects to HTTPS
- [ ] SSL certificate valid (no browser warnings)
- [ ] Security headers present (check browser Dev Tools → Network → Response Headers):
  - [ ] `X-Content-Type-Options: nosniff`
  - [ ] `X-Frame-Options: SAMEORIGIN`
  - [ ] `X-XSS-Protection: 1; mode=block`
  - [ ] `Referrer-Policy: strict-origin-when-cross-origin`
- [ ] Rate limiting active (verified in /health)
- [ ] Authentication required for admin pages
- [ ] JWT authentication works for API endpoints

---

## Step 12: Email Verification (If Configured)

- [ ] SMTP settings configured in web.config
- [ ] Trigger certificate status change
- [ ] Check email inbox for notification
- [ ] Email delivers successfully
- [ ] Email formatting looks correct
- [ ] Links in email work

---

## Step 13: Monitoring Setup (Optional)

### Application Insights (If Configured)
- [ ] Connection string set in web.config
- [ ] Open Azure Portal → Application Insights
- [ ] Verify Live Metrics showing data
- [ ] Verify requests logged
- [ ] Set up alerts for:
  - [ ] High response time (> 2 seconds)
  - [ ] High error rate (> 5%)
  - [ ] Application downtime

### Log Monitoring
- [ ] Set up log file monitoring (manual or automated)
- [ ] Configure log rotation (prevent large files)
- [ ] Set up alerts for critical errors

---

## Step 14: Backup Configuration

- [ ] Document all environment variables (securely)
- [ ] Backup web.config
- [ ] Backup database connection string
- [ ] Export database schema for reference
- [ ] Document AccuWeb account details
- [ ] Store SSL certificate files securely

---

## Step 15: Documentation

- [ ] Update internal docs with production URLs
- [ ] Document admin credentials (secure location)
- [ ] Document deployment process for future updates
- [ ] Create rollback plan
- [ ] Document support contacts

---

## Step 16: User Communication

- [ ] Notify stakeholders deployment is complete
- [ ] Provide production URL
- [ ] Provide admin login instructions
- [ ] Schedule training session (if needed)
- [ ] Collect feedback

---

## Post-Deployment Monitoring (First 7 Days)

### Daily
- [ ] Check /health endpoint
- [ ] Review error logs
- [ ] Monitor response times
- [ ] Check for failed emails
- [ ] Verify database growth is reasonable

### Weekly
- [ ] Review Application Insights (if configured)
- [ ] Check SSL certificate expiration (should be ~90 days)
- [ ] Verify backups running
- [ ] Review user feedback
- [ ] Performance optimization review

---

## Rollback Procedure (If Needed)

If deployment fails or critical issues found:

1. [ ] Stop IIS site
2. [ ] Restore previous version files from backup
3. [ ] Restore database from backup (if schema changed)
4. [ ] Restart IIS site
5. [ ] Verify previous version working
6. [ ] Investigate issue before re-attempting deployment

---

## Success Criteria

Deployment considered successful when:

- [x] Health endpoint returns "Healthy" for all checks
- [x] All functional tests pass
- [x] Performance meets targets (< 2s page loads, < 500ms API)
- [x] Security checks pass (HTTPS, rate limiting, auth)
- [x] No critical errors in logs
- [x] Admin can create/edit certificates
- [x] Public users can search/view certificates
- [x] Emails send successfully (if configured)
- [x] Application Insights receiving data (if configured)

---

## Final Sign-Off

- [ ] Technical lead approval
- [ ] Stakeholder approval
- [ ] UAT completed successfully
- [ ] Documentation complete
- [ ] Monitoring configured
- [ ] Support plan in place

**Deployed By**: ___________________________
**Date/Time**: ___________________________
**Version**: .NET 8.0 MVC - v1.0
**Status**: ✅ Production

---

**Document Version**: 1.0
**Last Updated**: October 6, 2025
**Next Review**: After first deployment
