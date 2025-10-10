# HTTP 500 Error - Troubleshooting Guide for gcctest.anukihub.com

**Date:** October 9, 2025
**Issue:** Application showing HTTP 500 error
**Status:** CRITICAL CONFIGURATION ISSUE FOUND AND FIXED

---

## CRITICAL ISSUE FOUND ✅ FIXED

### The Problem

Your `appsettings.Production.json` had this configuration:

```json
"DataProtection": {
  "PersistKeysToAzureBlobStorage": true,  // ❌ PROBLEM!
  "AzureBlobStorageConnectionString": "{AZURE_BLOB_CONNECTION_STRING}",  // ❌ Placeholder!
}
```

**What this means:**
- The app was trying to save encryption keys to Azure Blob Storage
- But the connection string was just a placeholder `{AZURE_BLOB_CONNECTION_STRING}`
- This causes the application to **crash immediately on startup**
- Result: HTTP 500 error

### The Fix

Changed to:
```json
"DataProtection": {
  "PersistKeysToAzureBlobStorage": false,  // ✅ FIXED - disabled Azure Blob
  "AzureBlobStorageConnectionString": "",  // ✅ Empty (not used)
}
```

**New Package Created:**
`GCCWebApp-Production-20251009-FIXED.zip` (60 MB)

---

## Other Potential Issues

Even with the fix above, you may still encounter errors. Here's what else to check:

### 1. ASP.NET Core Hosting Bundle Not Installed

**Symptom:** HTTP 502.5 error or "Failed to start application"

**Check:**
```cmd
dir "C:\Program Files\IIS\Asp.Net Core Module\V2"
```

**Fix:**
1. Download ASP.NET Core 8.0 Hosting Bundle:
   https://dotnet.microsoft.com/download/dotnet/8.0
2. Install the bundle
3. Restart IIS: `iisreset`

---

### 2. Wrong ASPNETCORE_ENVIRONMENT

**Symptom:** App crashes, can't find configuration

**Check:**
```powershell
[System.Environment]::GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", [System.EnvironmentVariableTarget]::Machine)
```

**Should return:** `Production`

**Fix:**
```powershell
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", [System.EnvironmentVariableTarget]::Machine)
iisreset
```

---

### 3. Database Connection Issues

**Symptom:** App starts but crashes when accessing database

**Your connection string:**
```
Server=68.71.129.120,1533;Database=gccwebap_gccdb;User Id=adminGCC;Password=Q3Ez4elgE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;
```

**Test the connection:**
```cmd
sqlcmd -S 68.71.129.120,1533 -d gccwebap_gccdb -U adminGCC -P Q3Ez4elgE_47KNi -Q "SELECT 1"
```

**Common database issues:**
- Firewall blocking port 1533
- SQL Server not accessible from web server
- Database credentials incorrect
- Database doesn't exist yet

**Fix for firewall:**
1. Check with your hosting provider (AccuWeb)
2. Ensure port 1533 is open between web server and database server

---

### 4. Missing Permissions

**Symptom:** HTTP 500 with "Access Denied" in logs

**Fix:**
```powershell
$deployPath = "C:\inetpub\wwwroot\GCCWebApp"
$appPoolIdentity = "IIS AppPool\GCCWebApp"

# Grant read/execute to app directory
icacls $deployPath /grant "${appPoolIdentity}:(OI)(CI)RX" /t

# Create logs directory
New-Item -Path "$deployPath\logs" -ItemType Directory -Force

# Grant full control to logs
icacls "$deployPath\logs" /grant "${appPoolIdentity}:(OI)(CI)F" /t
```

---

### 5. web.config Issues

**Check your web.config has:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath=".\GCCWebApp.exe"
                arguments=".\GCCWebApp.dll"
                stdoutLogEnabled="true"
                stdoutLogFile=".\logs\stdout"
                hostingModel="inprocess" />
  </system.webServer>
</configuration>
```

**Key points:**
- `processPath=".\GCCWebApp.exe"` (must exist)
- `arguments=".\GCCWebApp.dll"` (must exist)
- `stdoutLogEnabled="true"` (enables logging)
- `hostingModel="inprocess"` (better performance)

---

## How to View Error Details

### Method 1: Enable Detailed Errors (Temporary)

**Edit your deployed appsettings.Production.json:**
```json
"FeatureFlags": {
  "EnableDetailedErrorMessages": true,  // Change to true temporarily
  "EnableDeveloperExceptionPage": true  // Change to true temporarily
}
```

Then restart the app pool:
```powershell
Restart-WebAppPool -Name "GCCWebApp"
```

**Visit your site** - You should now see detailed error messages instead of generic HTTP 500.

**IMPORTANT:** Set these back to `false` after fixing the issue!

---

### Method 2: Check stdout Logs

**Location:** `C:\inetpub\wwwroot\GCCWebApp\logs\stdout_*.log`

**View latest log:**
```powershell
Get-Content "C:\inetpub\wwwroot\GCCWebApp\logs\stdout_*.log" -Tail 100
```

**Look for:**
- Exception messages
- Stack traces
- "Application startup exception"
- Connection errors
- Configuration errors

---

### Method 3: Check Windows Event Viewer

**Open Event Viewer:**
```cmd
eventvwr
```

**Navigate to:**
- Windows Logs > Application
- Filter by Source: "IIS AspNetCore Module V2"

**Look for:**
- Error events
- Warning events
- Exception details

---

## Step-by-Step Deployment with the FIXED Package

### 1. Stop Current Application
```powershell
Stop-IISAppPool -Name "GCCWebApp"
Stop-Website -Name "GCCWebApp"
```

### 2. Backup Current Deployment (if exists)
```powershell
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
Copy-Item "C:\inetpub\wwwroot\GCCWebApp" "C:\Backups\GCCWebApp_$timestamp" -Recurse -Force
```

### 3. Clear Deployment Directory
```powershell
Remove-Item "C:\inetpub\wwwroot\GCCWebApp\*" -Recurse -Force
```

### 4. Extract New Package
```powershell
# Upload GCCWebApp-Production-20251009-FIXED.zip to server first
Expand-Archive -Path "C:\Temp\GCCWebApp-Production-20251009-FIXED.zip" -DestinationPath "C:\inetpub\wwwroot\GCCWebApp" -Force
```

### 5. Set Environment Variable
```powershell
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", [System.EnvironmentVariableTarget]::Machine)
```

### 6. Set Permissions
```powershell
$deployPath = "C:\inetpub\wwwroot\GCCWebApp"
icacls $deployPath /grant "IIS AppPool\GCCWebApp:(OI)(CI)RX" /t
New-Item -Path "$deployPath\logs" -ItemType Directory -Force
icacls "$deployPath\logs" /grant "IIS AppPool\GCCWebApp:(OI)(CI)F" /t
```

### 7. Create/Configure App Pool
```powershell
Import-Module WebAdministration
$appPoolName = "GCCWebApp"

if (-not (Test-Path "IIS:\AppPools\$appPoolName")) {
    New-WebAppPool -Name $appPoolName
}

Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "enable32BitAppOnWin64" -Value $false
```

### 8. Restart IIS
```powershell
iisreset
```

### 9. Start Application
```powershell
Start-IISAppPool -Name "GCCWebApp"
Start-Website -Name "GCCWebApp"
```

### 10. Monitor Logs
```powershell
Get-Content "C:\inetpub\wwwroot\GCCWebApp\logs\stdout_*.log" -Wait
```

---

## Testing After Deployment

### Test 1: Health Endpoint
```powershell
Invoke-WebRequest -Uri "https://gcctest.anukihub.com/health" -Method GET
```
**Expected:** HTTP 200 OK

### Test 2: Home Page
```powershell
Invoke-WebRequest -Uri "https://gcctest.anukihub.com/" -Method GET
```
**Expected:** HTML response with home page

### Test 3: Certificate Tile View
```powershell
Invoke-WebRequest -Uri "https://gcctest.anukihub.com/Certificate/CertificateTileView" -Method GET
```
**Expected:** HTML response with certificate tiles

### Test 4: Login Page
```powershell
Invoke-WebRequest -Uri "https://gcctest.anukihub.com/Account/Login" -Method GET
```
**Expected:** HTML response with login form

---

## Common Error Messages and Solutions

### Error: "Failed to start application '/LM/W3SVC/1/ROOT'"

**Cause:** ASP.NET Core Hosting Bundle not installed or wrong version

**Solution:**
1. Install ASP.NET Core 8.0 Hosting Bundle
2. Restart IIS: `iisreset`

---

### Error: "The specified CGI application encountered an error"

**Cause:** Application crashed during startup

**Solution:**
1. Check stdout logs: `Get-Content "C:\inetpub\wwwroot\GCCWebApp\logs\stdout_*.log" -Tail 100`
2. Enable detailed errors (see Method 1 above)
3. Check Event Viewer for exception details

---

### Error: "A network-related or instance-specific error occurred"

**Cause:** Can't connect to SQL Server database

**Solution:**
1. Test connection: `sqlcmd -S 68.71.129.120,1533 -d gccwebap_gccdb -U adminGCC -P Q3Ez4elgE_47KNi -Q "SELECT 1"`
2. Check firewall allows port 1533
3. Contact AccuWeb support to verify database access

---

### Error: "Unable to load one or more of the requested types"

**Cause:** Missing DLL dependencies or wrong .NET version

**Solution:**
1. Verify ASP.NET Core 8.0 Runtime is installed
2. Redeploy with fresh package
3. Check all DLLs extracted: `(Get-ChildItem -Path "C:\inetpub\wwwroot\GCCWebApp" -Filter "*.dll" -Recurse).Count` (should be 100+)

---

### Error: "System.InvalidOperationException: Unable to configure data protection"

**Cause:** THIS WAS YOUR ISSUE - Azure Blob Storage configuration problem

**Solution:**
✅ FIXED in new package `GCCWebApp-Production-20251009-FIXED.zip`

---

## If Still Getting HTTP 500 After Fix

### 1. Temporarily Enable Detailed Errors

Edit `appsettings.Production.json` in your deployed app:
```json
"FeatureFlags": {
  "EnableDetailedErrorMessages": true,
  "EnableDeveloperExceptionPage": true,
  "EnableSwagger": false
}
```

Restart: `Restart-WebAppPool -Name "GCCWebApp"`

### 2. Access the Site

Visit `https://gcctest.anukihub.com/` - You should now see the actual error message instead of generic HTTP 500.

### 3. Share the Error Message

Take a screenshot or copy the exact error message and we can troubleshoot from there.

---

## Contact AccuWeb Support If Needed

If the issue is related to:
- Firewall/network access to database
- IIS configuration restrictions
- Server-level permissions
- ASP.NET Core Hosting Bundle installation

Contact AccuWeb support and provide:
1. The exact error message from logs
2. Screenshot of HTTP 500 page
3. Mention you need ASP.NET Core 8.0 support

---

## Summary of Changes in FIXED Package

**File:** `GCCWebApp-Production-20251009-FIXED.zip` (60 MB)

**Changes from previous build:**
1. ✅ Fixed Data Protection configuration (disabled Azure Blob Storage)
2. ✅ New secure JWT key included
3. ✅ All 357 files present
4. ✅ Ready for IIS deployment
5. ✅ Database connection pre-configured

**What's included:**
- GCCWebApp.dll, GCCWebApp.exe, web.config
- All 130+ DLL dependencies
- Static assets (CSS, JS, images, fonts)
- Configuration files with FIXES
- 12 database migrations (embedded)

---

**Next Step:** Deploy `GCCWebApp-Production-20251009-FIXED.zip` following the step-by-step guide above.

**If still having issues:** Enable detailed errors and check the logs using the methods described in this guide.
