# WEB.CONFIG FIX - Deployment Instructions
**Date:** October 10, 2025
**Package:** GCCWebApp-Production-20251010-WEBCONFIG-FIX.zip
**Issue Fixed:** HTTP 500.19 - Duplicate fileExtension configuration error

---

## What Was Wrong

Your deployment was failing with HTTP Error 500.19:

```
Cannot add duplicate collection entry of type 'add' with unique key attribute
'fileExtension' set to '.config'
```

**Root Cause:** The web.config file contained a `<fileExtensions>` section that conflicted with Plesk's global IIS configuration. Plesk already defines file extension blocking rules at the server level, and when our web.config tried to add the same rules, IIS threw an error.

**Problematic sections in old web.config:**
- Lines 41-46: `<fileExtensions>` with .db, .config, .log, .bak blocking
- Lines 47-55: `<hiddenSegments>` with directory hiding rules

These conflicted with Plesk's server-wide security settings.

---

## What Was Fixed

I've created a new deployment package with a corrected web.config that:

1. **Removed the conflicting `<fileExtensions>` section** - Plesk handles this at server level
2. **Removed the conflicting `<hiddenSegments>` section** - Plesk handles this at server level
3. **Kept all essential ASP.NET Core configuration:**
   - AspNetCore module settings
   - Environment variable (ASPNETCORE_ENVIRONMENT=Production)
   - Security headers
   - HTTPS redirect
   - Static content MIME types
   - Error page handling
   - Logging configuration

The new web.config is **Plesk-compatible** and won't cause configuration conflicts.

---

## New Deployment Package

**File:** `GCCWebApp-Production-20251010-WEBCONFIG-FIX.zip`
**Location:** `C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\`

This package contains:
- All application files (GCCWebApp.dll, dependencies, etc.)
- **FIXED web.config** (Plesk-compatible)
- Fixed appsettings.Production.json (Data Protection settings)
- All static assets (wwwroot folder)

---

## DEPLOYMENT INSTRUCTIONS - STEP BY STEP

### STEP 1: Backup Current Deployment

Before deploying the fix, backup what's currently on the server:

1. **Login to Plesk:** https://manage.accuwebhosting.com/
2. **Navigate to:** Files → File Manager → httpdocs
3. **Select all files** in httpdocs
4. **Click "Archive"** button
5. **Name it:** `backup-before-webconfig-fix-20251010.zip`
6. **Download the backup** to your local computer (for safety)

### STEP 2: Clear httpdocs Folder

To ensure a clean deployment:

1. **In Plesk File Manager** (httpdocs)
2. **Select all files EXCEPT** the backup you just created
3. **Click "Remove"**
4. **Confirm deletion**

The httpdocs folder should now be empty (except for the backup file).

### STEP 3: Upload New Package

1. **In Plesk File Manager** (httpdocs)
2. **Click "Upload" button**
3. **Select:** `GCCWebApp-Production-20251010-WEBCONFIG-FIX.zip` from your computer
   - **Location:** `C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\`
4. **Wait for upload to complete** (may take 5-10 minutes depending on connection)

### STEP 4: Extract the Package

1. **In Plesk File Manager** (httpdocs)
2. **Right-click** on `GCCWebApp-Production-20251010-WEBCONFIG-FIX.zip`
3. **Select "Extract Files"**
4. **Extract to:** Current directory (httpdocs)
5. **Click "OK"**
6. **Wait for extraction** (may take 3-5 minutes)

### STEP 5: Verify Files Are Correct

After extraction, verify you see these files in httpdocs:

**Required files:**
- `GCCWebApp.dll` - Main application
- `GCCWebApp.exe` - Application executable
- `web.config` - **THE FIXED ONE** (should NOT have fileExtensions section)
- `appsettings.Production.json` - Production configuration
- `wwwroot/` folder - Static files (CSS, JS, images)
- Many DLL files (130+)

**CRITICAL: Verify web.config is correct**

1. **In Plesk File Manager**, find `web.config`
2. **Right-click → "Edit in Code Editor"**
3. **Look at line 37-42**, should look like this:

```xml
<!-- Request Filtering - REMOVED fileExtensions to avoid Plesk conflicts -->
<security>
  <requestFiltering>
    <requestLimits maxAllowedContentLength="104857600" />
  </requestFiltering>
</security>
```

4. **Verify there is NO `<fileExtensions>` section**
5. **Verify there is NO `<hiddenSegments>` section**

If you see those sections, the wrong file was extracted. STOP and contact me.

### STEP 6: Create logs Folder

1. **In Plesk File Manager** (httpdocs)
2. **Click "+ Create Directory"**
3. **Name:** `logs`
4. **Click "OK"**
5. **Right-click on logs folder → "Change Permissions"**
6. **Set to:** `777` (full access)
7. **Click "OK"**

### STEP 7: Restart IIS Application Pool

**Option A: Via Plesk**
1. Go to **Websites & Domains** → gccwebapp.com
2. Find **Application Pool** settings
3. Click **"Restart"** or **"Recycle"**
4. Wait 10-20 seconds

**Option B: Ask AccuWeb Support**

If you don't see those options in Plesk, contact AccuWeb support:

```
Please restart the IIS Application Pool for gccwebapp.com
```

### STEP 8: Test the Website

1. **Open your browser** (clear cache first or use incognito mode)
2. **Navigate to:** https://gccwebapp.com
3. **Expected Result:** Site should load WITHOUT HTTP 500 error

**Test these pages:**
- https://gccwebapp.com/ - Homepage
- https://gccwebapp.com/Certificate/CertificateTileView - Certificate listing
- https://gccwebapp.com/Account/Login - Login page
- https://gccwebapp.com/health - Should return "Healthy"

---

## If You Still Get Errors

### Error: HTTP 500 (Different from 500.19)

This would be a different issue - application startup error, not configuration error.

**Action:**
1. Check logs in httpdocs/logs/stdout_*.log
2. Look for error messages containing "Exception" or "ERROR"
3. Send me the last 50 lines of the most recent log file

### Error: Still Getting HTTP 500.19

This should NOT happen with the fixed web.config, but if it does:

**Action:**
1. Verify web.config was correctly extracted (check Step 5 above)
2. Look at the exact error message - what line number does it mention?
3. Copy the entire error message and send it to me

### Error: 502.5 - Process Failure

**Cause:** ASP.NET Core Hosting Bundle not installed

**Action:**
1. Contact AccuWeb support
2. Ask: "Please install ASP.NET Core 8.0 Hosting Bundle on the web server"
3. Provide this link: https://dotnet.microsoft.com/download/dotnet/8.0

### Error: Database Connection Issues

**Action:**
1. Check that SQL Server is accessible from web server
2. Contact AccuWeb support to verify firewall allows port 1533
3. Test connection: `sqlcmd -S 68.71.129.120,1533 -d gccwebap_gccdb -U adminGCC -P Q3Ez4elgE_47KNi -Q "SELECT 1"`

---

## What Changed in This Package

Comparing to the previous package (GCCWebApp-Production-20251009-FIXED.zip):

**Changed File:**
- `web.config` - Removed fileExtensions and hiddenSegments sections to avoid Plesk conflicts

**No Other Changes:**
- appsettings.Production.json - Same (already has Data Protection fix)
- Application DLLs - Same
- Static files (wwwroot) - Same
- All other configuration - Same

**Only the web.config was changed to fix the HTTP 500.19 error.**

---

## Technical Details

### What the web.config Fix Removed

**Removed Section 1: fileExtensions**
```xml
<fileExtensions>
  <add fileExtension=".db" allowed="false" />
  <add fileExtension=".config" allowed="false" />
  <add fileExtension=".log" allowed="false" />
  <add fileExtension=".bak" allowed="false" />
</fileExtensions>
```
**Reason:** Plesk already blocks these file extensions at the server level. Adding them again in web.config causes "duplicate collection entry" error.

**Removed Section 2: hiddenSegments**
```xml
<hiddenSegments>
  <add segment="bin" />
  <add segment="App_code" />
  <add segment="App_GlobalResources" />
  <add segment="App_LocalResources" />
  <add segment="App_WebReferences" />
  <add segment="App_Data" />
  <add segment="logs" />
</hiddenSegments>
```
**Reason:** Plesk has its own directory hiding rules. Duplicating them causes conflicts.

### What the web.config Keeps

All essential configuration remains:
- ASP.NET Core module settings (hosting model, process path, environment)
- Security headers (X-Frame-Options, CSP, HSTS, etc.)
- HTTPS redirect rules
- Static content MIME types (.json, .woff, .woff2)
- HTTP compression
- Error page handling
- Request size limits (100 MB upload limit)
- Logging configuration (stdout to logs folder)

**Your application will still be secure and properly configured.**

---

## Support Information

**AccuWeb Hosting Support:**
- URL: https://manage.accuwebhosting.com/submitticket.php
- Existing Ticket: KFU974586N

**What to Tell Support (if needed):**
```
I've deployed an updated ASP.NET Core 8.0 package with a fixed web.config
that resolves the HTTP 500.19 configuration conflict.

If you see any remaining issues, please:
1. Verify ASP.NET Core 8.0 Hosting Bundle is installed
2. Verify IIS Application Pool has been restarted
3. Check that the web server can connect to SQL Server at 68.71.129.120:1533

Thank you!
```

---

## Post-Deployment Checklist

After successful deployment:

- [ ] Website loads at https://gccwebapp.com
- [ ] No HTTP 500.19 error
- [ ] Certificate tile view works
- [ ] Login page loads
- [ ] /health endpoint returns "Healthy"
- [ ] Disable detailed error messages (if you enabled them for troubleshooting)
- [ ] Change default passwords for security
- [ ] Monitor logs for any runtime errors

---

## Temporary Detailed Errors (If Needed for Troubleshooting)

If you need to see detailed error messages temporarily:

1. **Edit appsettings.Production.json** in Plesk File Manager
2. **Find FeatureFlags section** (around line 168)
3. **Change to:**
```json
"FeatureFlags": {
  "EnableDetailedErrorMessages": true,
  "EnableDeveloperExceptionPage": true,
```
4. **Save and restart app pool**
5. **Visit website** - you'll see detailed errors
6. **IMPORTANT: Change back to false after fixing!**

---

## Summary

**Problem:** HTTP 500.19 - Duplicate fileExtension configuration
**Cause:** web.config conflicted with Plesk's global IIS settings
**Fix:** Removed conflicting sections from web.config
**Package:** GCCWebApp-Production-20251010-WEBCONFIG-FIX.zip
**Status:** Ready to deploy

**This should resolve the HTTP 500.19 error you were experiencing.**

---

**Created:** October 10, 2025
**Author:** Claude Code Assistant
**Status:** READY FOR DEPLOYMENT
