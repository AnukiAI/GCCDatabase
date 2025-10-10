# GCC Web Application - Deployment Execution Guide
**Date:** October 9, 2025
**Target Environment:** IIS on Windows (AccuWeb Hosting)
**Package:** GCCWebApp-TestDeploy.tar.gz (59 MB)
**Status:** READY TO DEPLOY

---

## CRITICAL: New JWT Secret Key Generated

**NEW PRODUCTION JWT SECRET KEY:**
```
5T8hr4WpKKmqqDIHBzFW7OzfYdStIzuvKEZJYwQaSkfe5ADFryPGJHGAuxPCECnuo09ZI8qiLqURyVT1DR1NtQ==
```

**SECURITY NOTICE:**
- This key was generated on October 9, 2025 using cryptographically secure random number generation
- This key is 88 characters (64 bytes base64-encoded)
- DO NOT commit this key to source control
- DO NOT share this key via insecure channels
- Store securely in your password manager
- Use environment variables in production (recommended method below)

---

## Pre-Deployment Checklist

### 1. Backup Current System

```powershell
# Backup database (if existing production system)
# Example for SQL Server:
# sqlcmd -S your-server -d GCCDatabase -Q "BACKUP DATABASE GCCDatabase TO DISK='C:\Backups\GCCDatabase_20251009.bak'"

# Backup current application files (if existing)
# xcopy "C:\inetpub\wwwroot\GCCWebApp" "C:\Backups\GCCWebApp_20251009" /E /I /H /Y

# Document current configuration
# Copy existing appsettings.Production.json to backup location
```

### 2. Prepare Database

**SQL Server Configuration:**

Your production database connection details (from appsettings.Production.json):
- Server: `68.71.129.120,1533`
- Database: `gccwebap_gccdb`
- User: `adminGCC`
- Password: `Q3Ez4elgE_47KNi`

**Test database connectivity before deployment:**

```powershell
# Test SQL Server connection
sqlcmd -S 68.71.129.120,1533 -d gccwebap_gccdb -U adminGCC -P Q3Ez4elgE_47KNi -Q "SELECT @@VERSION"
```

If successful, you should see SQL Server version information.

---

## Deployment Steps

### Step 1: Stop Current Application (if running)

```powershell
# Check if IIS App Pool exists
Get-IISAppPool -Name "GCCWebApp"

# If it exists, stop it
Stop-IISAppPool -Name "GCCWebApp"

# Wait for app pool to stop completely
Start-Sleep -Seconds 5
```

### Step 2: Prepare Deployment Directory

```powershell
# Create deployment directory if it doesn't exist
$deployPath = "C:\inetpub\wwwroot\GCCWebApp"
if (-not (Test-Path $deployPath)) {
    New-Item -Path $deployPath -ItemType Directory -Force
}

# Clear existing files if doing a clean deployment
# WARNING: Only do this if you have backups!
# Remove-Item "$deployPath\*" -Recurse -Force
```

### Step 3: Extract Deployment Package

**Transfer the package to the server first:**

Upload `GCCWebApp-TestDeploy.tar.gz` to the server (e.g., via FTP, SFTP, or RDP copy).

**Extract the package:**

```powershell
# If using tar (Windows 10/Server 2019+)
cd C:\inetpub\wwwroot
tar -xzf C:\Temp\GCCWebApp-TestDeploy.tar.gz -C GCCWebApp

# OR using 7-Zip (if tar not available)
# & "C:\Program Files\7-Zip\7z.exe" x C:\Temp\GCCWebApp-TestDeploy.tar.gz -oC:\Temp
# & "C:\Program Files\7-Zip\7z.exe" x C:\Temp\GCCWebApp-TestDeploy.tar -oC:\inetpub\wwwroot\GCCWebApp
```

### Step 4: Configure Production Secrets (CRITICAL)

**RECOMMENDED METHOD: Environment Variables**

```powershell
# Open IIS Manager
# Navigate to Application Pools > GCCWebApp (or create it)
# Go to Advanced Settings > Environment Variables
# Add the following variables:

# Method 1: PowerShell commands (requires IIS Administration Module)
Import-Module WebAdministration

# Set environment variables for the app pool
$appPoolName = "GCCWebApp"

# Create app pool if it doesn't exist
if (-not (Test-Path "IIS:\AppPools\$appPoolName")) {
    New-WebAppPool -Name $appPoolName
}

# Set .NET CLR version to "No Managed Code" (for .NET Core)
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""

# JWT Secret
[System.Environment]::SetEnvironmentVariable("GCC_JWT_SECRET", "5T8hr4WpKKmqqDIHBzFW7OzfYdStIzuvKEZJYwQaSkfe5ADFryPGJHGAuxPCECnuo09ZI8qiLqURyVT1DR1NtQ==", [System.EnvironmentVariableTarget]::Machine)

# Database Connection (using existing from appsettings.Production.json)
# Note: The connection string is already in the config file, but you can override via environment variable if preferred
$connString = "Server=68.71.129.120,1533;Database=gccwebap_gccdb;User Id=adminGCC;Password=Q3Ez4elgE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
[System.Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $connString, [System.EnvironmentVariableTarget]::Machine)
```

**ALTERNATIVE METHOD: Update Configuration File Directly**

If you prefer to update the file directly (less secure):

```powershell
# Navigate to deployment directory
cd C:\inetpub\wwwroot\GCCWebApp

# Backup original file
Copy-Item appsettings.Production.json appsettings.Production.json.backup

# Edit the file (use your preferred text editor)
# Replace the JWT Key line with the new key:
# "Key": "5T8hr4WpKKmqqDIHBzFW7OzfYdStIzuvKEZJYwQaSkfe5ADFryPGJHGAuxPCECnuo09ZI8qiLqURyVT1DR1NtQ==",
```

### Step 5: Set File Permissions

```powershell
$deployPath = "C:\inetpub\wwwroot\GCCWebApp"
$appPoolIdentity = "IIS AppPool\GCCWebApp"

# Grant read/execute permissions to app pool
icacls $deployPath /grant "${appPoolIdentity}:(OI)(CI)RX" /t

# Create logs directory if it doesn't exist
$logsPath = Join-Path $deployPath "logs"
if (-not (Test-Path $logsPath)) {
    New-Item -Path $logsPath -ItemType Directory -Force
}

# Grant full control to logs directory
icacls $logsPath /grant "${appPoolIdentity}:(OI)(CI)F" /t

# Grant write permissions to database file if using SQLite for testing
$dbPath = Join-Path $deployPath "gcc_database.db"
if (Test-Path $dbPath) {
    icacls $dbPath /grant "${appPoolIdentity}:F"
}
```

### Step 6: Configure IIS Site

```powershell
Import-Module WebAdministration

$siteName = "GCCWebApp"
$appPoolName = "GCCWebApp"
$physicalPath = "C:\inetpub\wwwroot\GCCWebApp"
$bindingInfo = @{
    Protocol = "https"
    Port = 443
    HostHeader = "your-domain.com"  # UPDATE THIS with your actual domain
}

# Create IIS Application Pool (if not exists)
if (-not (Test-Path "IIS:\AppPools\$appPoolName")) {
    New-WebAppPool -Name $appPoolName
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "enable32BitAppOnWin64" -Value $false
}

# Create IIS Site (if not exists)
if (-not (Test-Path "IIS:\Sites\$siteName")) {
    New-Website -Name $siteName -ApplicationPool $appPoolName -PhysicalPath $physicalPath -Port 80
}

# Add HTTPS binding (requires SSL certificate)
# New-WebBinding -Name $siteName -Protocol "https" -Port 443 -HostHeader "your-domain.com"

# Set site to use the app pool
Set-ItemProperty "IIS:\Sites\$siteName" -Name "applicationPool" -Value $appPoolName
```

### Step 7: Install ASP.NET Core Hosting Bundle (if not already installed)

```powershell
# Check if ASP.NET Core Module is installed
Get-WindowsFeature -Name Web-ASPNET45

# Download and install ASP.NET Core 8.0 Hosting Bundle if needed
# https://dotnet.microsoft.com/download/dotnet/8.0
# Direct link: https://download.visualstudio.microsoft.com/download/pr/[version]/dotnet-hosting-8.0-win.exe

# After installation, restart IIS
iisreset
```

### Step 8: Set ASPNETCORE_ENVIRONMENT

```powershell
# Set environment variable for production
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", [System.EnvironmentVariableTarget]::Machine)

# Restart IIS to pick up environment variable
iisreset
```

### Step 9: Database Migrations

The application will automatically run migrations on first startup. However, you can manually apply them:

```powershell
cd C:\inetpub\wwwroot\GCCWebApp

# Run migrations manually using dotnet CLI (if installed)
# dotnet ef database update

# OR the migrations will run automatically when the app starts
# Check logs in C:\inetpub\wwwroot\GCCWebApp\logs\ for migration status
```

### Step 10: Start Application

```powershell
# Start the IIS App Pool
Start-IISAppPool -Name "GCCWebApp"

# Start the IIS Site
Start-Website -Name "GCCWebApp"

# Monitor the logs
Get-Content "C:\inetpub\wwwroot\GCCWebApp\logs\stdout_*.log" -Wait
```

---

## Post-Deployment Verification

### Automated Health Checks

```powershell
# Test health endpoint
$domain = "https://your-domain.com"  # UPDATE THIS

Invoke-WebRequest -Uri "$domain/health" -Method GET

# Test home page
Invoke-WebRequest -Uri "$domain/" -Method GET

# Test certificate tile view (main landing page)
Invoke-WebRequest -Uri "$domain/Certificate/CertificateTileView" -Method GET

# Test login page
Invoke-WebRequest -Uri "$domain/Account/Login" -Method GET
```

### Manual Verification Checklist

Open a browser and verify each of these:

- [ ] **Home Page**: Navigate to `https://your-domain.com/`
- [ ] **Certificate Tile View**: `https://your-domain.com/Certificate/CertificateTileView`
- [ ] **Login Page**: `https://your-domain.com/Account/Login`
- [ ] **Admin Login**: Login with `admin@greencirclecertified.com` / `Admin123!`
- [ ] **Dashboard**: `https://your-domain.com/Dashboard`
- [ ] **Certificate Create**: `https://your-domain.com/Certificate/Create`
- [ ] **Admin Panel**: `https://your-domain.com/Admin/UserManagement`
- [ ] **API Health**: `https://your-domain.com/health`
- [ ] **API Certificates**: `https://your-domain.com/api/v1/certificates`

### Static Assets Verification

- [ ] CSS files loading (check browser dev tools Network tab)
- [ ] JavaScript files executing (check browser console for errors)
- [ ] Images displaying correctly
- [ ] PDF generation working (test certificate download)
- [ ] Excel export working (test certificate export)

### Security Verification

```powershell
# Check security headers (requires curl or similar)
curl -I https://your-domain.com/

# Expected headers:
# X-Frame-Options: SAMEORIGIN
# X-Content-Type-Options: nosniff
# X-XSS-Protection: 1; mode=block
# Strict-Transport-Security: max-age=31536000
# Content-Security-Policy: [policy]
```

### Database Verification

```sql
-- Connect to database and verify migrations
USE gccwebap_gccdb;

-- Check if __EFMigrationsHistory table exists
SELECT * FROM __EFMigrationsHistory;

-- Should show 12 migrations applied

-- Check if default users were created
SELECT * FROM AspNetUsers;

-- Check if certificates exist (if sample data was seeded)
SELECT COUNT(*) FROM Certificates;
```

### Log File Verification

```powershell
# Check stdout logs
Get-Content "C:\inetpub\wwwroot\GCCWebApp\logs\stdout_*.log" -Tail 50

# Check for errors in Event Viewer
Get-EventLog -LogName Application -Source "IIS AspNetCore Module*" -Newest 20
```

---

## Troubleshooting

### Application Won't Start

**Symptom:** HTTP 502.5 error or app pool crashes

**Solutions:**
1. Check if ASP.NET Core Hosting Bundle is installed
2. Verify `ASPNETCORE_ENVIRONMENT` is set to `Production`
3. Check app pool identity has proper permissions
4. Review stdout logs in `logs/` folder
5. Check Event Viewer for IIS errors

```powershell
# View recent logs
Get-Content "C:\inetpub\wwwroot\GCCWebApp\logs\stdout_*.log" -Tail 100

# Check app pool status
Get-IISAppPool -Name "GCCWebApp"

# Restart app pool
Restart-WebAppPool -Name "GCCWebApp"
```

### Database Connection Issues

**Symptom:** "Cannot connect to database" errors

**Solutions:**
1. Verify SQL Server connection string is correct
2. Test connectivity from server to database
3. Check firewall rules allow connection to port 1533
4. Verify database user has proper permissions

```powershell
# Test database connection
sqlcmd -S 68.71.129.120,1533 -d gccwebap_gccdb -U adminGCC -P Q3Ez4elgE_47KNi -Q "SELECT 1"
```

### JWT Authentication Issues

**Symptom:** Login fails or "Unauthorized" errors

**Solutions:**
1. Verify JWT key is set correctly (environment variable or config file)
2. Check JWT key length is 64+ characters
3. Ensure `ASPNETCORE_ENVIRONMENT=Production`
4. Clear browser cookies and try again

```powershell
# Verify environment variable is set
[System.Environment]::GetEnvironmentVariable("GCC_JWT_SECRET", [System.EnvironmentVariableTarget]::Machine)
```

### Static Files Not Loading

**Symptom:** 404 errors for CSS/JS/images

**Solutions:**
1. Verify `wwwroot` folder exists in deployment
2. Check IIS has Static Content feature enabled
3. Verify file permissions allow read access
4. Check `web.config` has proper static file handlers

```powershell
# Verify wwwroot exists and has files
Test-Path "C:\inetpub\wwwroot\GCCWebApp\wwwroot"
Get-ChildItem "C:\inetpub\wwwroot\GCCWebApp\wwwroot" -Recurse | Measure-Object

# Check IIS Static Content feature
Get-WindowsFeature -Name Web-Static-Content
```

### Performance Issues

**Symptom:** Slow page loads or timeouts

**Solutions:**
1. Check database indexes are created (migration should handle this)
2. Verify app pool has sufficient memory
3. Enable response compression
4. Check rate limiting isn't too restrictive

```powershell
# Increase app pool memory limit
Set-ItemProperty "IIS:\AppPools\GCCWebApp" -Name "recycling.periodicRestart.memory" -Value 2097152  # 2GB
```

---

## Security Post-Deployment

### Change Default Passwords

**CRITICAL:** Change these default passwords immediately:

1. Admin User: `admin@greencirclecertified.com`
2. Analyst User: `analyst@greencirclecertified.com`
3. External Partner: `partner@amazon.com`

Navigate to each user's profile and update passwords to meet the production password policy:
- Minimum 14 characters
- Requires uppercase, lowercase, digits, special characters
- Cannot reuse last 10 passwords

### Enable HTTPS

```powershell
# Add HTTPS binding with SSL certificate
# You need to have an SSL certificate installed first

$siteName = "GCCWebApp"
$certThumbprint = "YOUR_CERT_THUMBPRINT"  # Get from certificate

# Add HTTPS binding
New-WebBinding -Name $siteName -Protocol "https" -Port 443 -HostHeader "your-domain.com"

# Assign certificate to binding
$cert = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object { $_.Thumbprint -eq $certThumbprint }
$binding = Get-WebBinding -Name $siteName -Protocol "https"
$binding.AddSslCertificate($cert.Thumbprint, "my")
```

### Configure SSL Certificate

If using Let's Encrypt or other certificate provider:

1. Install certificate on server
2. Import to IIS certificate store
3. Bind to your site
4. Test HTTPS redirect is working

### Review Security Settings

- [ ] HTTPS redirect is working
- [ ] Security headers are present (check with browser dev tools)
- [ ] CORS is properly configured for your domain
- [ ] Rate limiting is active (test with multiple rapid requests)
- [ ] 2FA is working for user accounts
- [ ] File upload restrictions are working

---

## Rollback Procedure

### If Deployment Fails

```powershell
# 1. Stop new application
Stop-IISAppPool -Name "GCCWebApp"
Stop-Website -Name "GCCWebApp"

# 2. Restore previous version from backup
Remove-Item "C:\inetpub\wwwroot\GCCWebApp\*" -Recurse -Force
Copy-Item "C:\Backups\GCCWebApp_20251009\*" "C:\inetpub\wwwroot\GCCWebApp\" -Recurse -Force

# 3. Restore database (if migrations were applied)
# sqlcmd -S your-server -Q "RESTORE DATABASE GCCDatabase FROM DISK='C:\Backups\GCCDatabase_20251009.bak' WITH REPLACE"

# 4. Restart application
Start-IISAppPool -Name "GCCWebApp"
Start-Website -Name "GCCWebApp"

# 5. Verify rollback
Invoke-WebRequest -Uri "https://your-domain.com/health" -Method GET
```

---

## Post-Deployment Monitoring

### Set Up Monitoring

1. **Application Insights** (if configured):
   - Verify telemetry is flowing
   - Set up alerts for errors and performance

2. **IIS Logs**:
   - Enable detailed error logging
   - Monitor logs for errors

```powershell
# Enable detailed IIS logging
Set-WebConfigurationProperty -Filter "system.webServer/httpLogging" -Name "dontLog" -Value $false -PSPath "IIS:\Sites\GCCWebApp"
```

3. **Windows Event Viewer**:
   - Monitor Application logs
   - Set up alerts for critical errors

4. **Database Monitoring**:
   - Monitor SQL Server performance
   - Set up alerts for connection issues

### Regular Checks

**Daily:**
- Check application is responding
- Review error logs for critical issues
- Monitor disk space (logs can grow)

**Weekly:**
- Review security logs for suspicious activity
- Check database backup status
- Review performance metrics

**Monthly:**
- Apply security patches
- Review and rotate JWT keys if needed
- Update SSL certificates if expiring

---

## Support and Documentation

**Deployment Documentation:**
- `DEPLOYMENT_READINESS_VERIFICATION_20251009.md` - Pre-deployment verification
- `DEPLOYMENT_INSTRUCTIONS_TEST.md` - Detailed deployment instructions
- `DEPLOYMENT_CHECKLIST.md` - Quick deployment checklist

**Application Documentation:**
- `CLAUDE.md` - Developer guide and project overview
- `0_Documentation/docs/Requirements.md` - Requirements and user stories
- `0_Documentation/docs/MANUAL_UAT_TEST_CASES.md` - Test cases

**Configuration Files:**
- `appsettings.Production.json` - Production configuration
- `web.config` - IIS configuration
- `launchSettings.json` - Development profiles

---

## Deployment Completion Checklist

- [ ] Backup completed
- [ ] Deployment package extracted
- [ ] JWT secret key configured
- [ ] Database connection string configured
- [ ] File permissions set
- [ ] IIS site configured
- [ ] ASP.NET Core Hosting Bundle installed
- [ ] Environment variables set
- [ ] Database migrations applied
- [ ] Application started successfully
- [ ] Health endpoint responding
- [ ] Login working
- [ ] Static assets loading
- [ ] Security headers present
- [ ] Default passwords changed
- [ ] HTTPS configured
- [ ] Monitoring set up
- [ ] Documentation updated

---

## Sign-Off

**Deployed By:** _______________________
**Deployment Date:** _______________________
**Verified By:** _______________________
**Verification Date:** _______________________

**Deployment Status:** ☐ Success  ☐ Success with issues  ☐ Failed (rolled back)

**Notes:**
```
_____________________________________________________________________________
_____________________________________________________________________________
_____________________________________________________________________________
```

---

**Generated:** October 9, 2025
**Package:** GCCWebApp-TestDeploy.tar.gz (59 MB)
**Status:** Ready for production deployment
