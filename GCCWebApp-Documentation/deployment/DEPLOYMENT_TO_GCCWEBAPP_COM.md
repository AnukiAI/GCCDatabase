# Deployment to gccwebapp.com - Step-by-Step Guide

**Date:** October 9, 2025
**Package:** GCCWebApp-Production-20251009-FIXED.zip (185 MB)
**Target:** gccwebapp.com/httpdocs
**Hosting:** AccuWeb with Plesk Control Panel

---

## CRITICAL FIX INCLUDED

This package fixes the **HTTP 500 error** caused by Data Protection configuration:
- ✅ Changed `PersistKeysToAzureBlobStorage` from `true` to `false`
- ✅ Cleared `AzureBlobStorageConnectionString` (empty string)
- ✅ Prevents app crash on startup

---

## Prerequisites

- [ ] Plesk login credentials for gccwebapp.com
- [ ] File Manager or FTP access
- [ ] ASP.NET Core 8.0 Hosting Bundle installed on server (verify with hosting support)

---

## STEP 1: Upload the Package

### Option A: Using Plesk File Manager

1. **Login to Plesk**
   - Go to: https://manage.accuwebhosting.com/
   - Login with your credentials

2. **Navigate to File Manager**
   - Click on your domain: **gccwebapp.com**
   - Click **Files** in the left sidebar
   - Click **File Manager**

3. **Navigate to httpdocs**
   - You should see the `httpdocs` folder
   - This is where your website files go

4. **Upload the ZIP file**
   - Click **Upload** button at the top
   - Select `GCCWebApp-Production-20251009-FIXED.zip` from your computer
   - Wait for upload to complete (185 MB - may take 5-10 minutes)

### Option B: Using FTP

1. **Connect via FTP**
   - Host: ftp.gccwebapp.com (or IP provided by AccuWeb)
   - Username: Your FTP username
   - Password: Your FTP password
   - Port: 21 (or 22 for SFTP)

2. **Navigate to httpdocs**
   - Change directory to `/httpdocs`

3. **Upload ZIP**
   - Upload `GCCWebApp-Production-20251009-FIXED.zip`
   - Use Binary transfer mode

---

## STEP 2: Backup Current Application (if exists)

**In Plesk File Manager:**

1. If you have files in `httpdocs` already:
   - Select all files in httpdocs
   - Click **Archive** button
   - Name it: `backup-before-deploy-20251009.zip`
   - Click **OK**

2. Download the backup:
   - Right-click on `backup-before-deploy-20251009.zip`
   - Click **Download**
   - Save to your local computer

3. Delete old files (OPTIONAL - only if you want clean install):
   - Select all files EXCEPT the backup zip
   - Click **Remove**
   - Confirm deletion

---

## STEP 3: Extract the Package

**In Plesk File Manager:**

1. **Locate the uploaded ZIP**
   - You should see `GCCWebApp-Production-20251009-FIXED.zip` in httpdocs

2. **Extract the ZIP**
   - Right-click on `GCCWebApp-Production-20251009-FIXED.zip`
   - Select **Extract Files**
   - Extract to: **Current directory** (httpdocs)
   - Click **OK**
   - Wait for extraction to complete (3-5 minutes)

3. **Verify extraction**
   - You should now see these files/folders in httpdocs:
     - `GCCWebApp.dll`
     - `GCCWebApp.exe`
     - `web.config`
     - `appsettings.Production.json`
     - `wwwroot/` folder
     - Many DLL files
     - `logs/` folder (will be created)

4. **Delete the ZIP** (optional, to save space)
   - Select `GCCWebApp-Production-20251009-FIXED.zip`
   - Click **Remove**

---

## STEP 4: Configure Environment Variables (CRITICAL!)

### Where to Set Environment Variables in Plesk

**Method 1: Web Application Settings (RECOMMENDED)**

1. **Navigate to Web Applications**
   - In Plesk, go to your domain: **gccwebapp.com**
   - Click **Hosting & DNS** (or **Websites & Domains**)
   - Scroll down and find **Web Application Firewall** or **Application Settings**

2. **Look for IIS Application Settings**
   - Depending on your Plesk version, this might be under:
     - **IIS Settings** → **Application Pool**
     - **Hosting Settings** → **Web Application**
     - **Websites & Domains** → **IIS Application Pool**

3. **Add Environment Variable**
   - Look for **Environment Variables** section
   - Click **Add New Variable** or **+**

   **Add this variable:**
   - Name: `ASPNETCORE_ENVIRONMENT`
   - Value: `Production`
   - Click **OK** or **Save**

**Method 2: Via web.config (ALTERNATIVE)**

If you can't find Environment Variables in Plesk:

1. **Edit web.config**
   - In File Manager, navigate to httpdocs
   - Find `web.config`
   - Right-click → **Edit in Code Editor**

2. **Add environment variable**
   - Find the `<aspNetCore>` section
   - Add this line inside it:
   ```xml
   <aspNetCore processPath=".\GCCWebApp.exe"
               arguments=".\GCCWebApp.dll"
               stdoutLogEnabled="true"
               stdoutLogFile=".\logs\stdout"
               hostingModel="inprocess">
     <environmentVariables>
       <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
     </environmentVariables>
   </aspNetCore>
   ```

3. **Save the file**
   - Click **OK** or **Save**

**Method 3: Contact AccuWeb Support**

If you're not sure where to add environment variables:

1. **Open Support Ticket** (or use existing ticket)
   - Ask them to set: `ASPNETCORE_ENVIRONMENT=Production`
   - For the IIS Application Pool for gccwebapp.com
   - At the machine level or app pool level

---

## STEP 5: Set File Permissions

### In Plesk

1. **Create logs folder** (if not exists)
   - In File Manager, navigate to httpdocs
   - Click **+ Create Directory**
   - Name: `logs`
   - Click **OK**

2. **Set permissions for logs folder**
   - Right-click on `logs` folder
   - Select **Change Permissions**
   - Set to: `755` or `777` (full access)
   - Check **Apply to subdirectories**
   - Click **OK**

3. **Verify IIS App Pool has access**
   - This is usually automatic in Plesk
   - If you have issues, contact AccuWeb support to verify:
     - IIS App Pool identity has read/execute on httpdocs
     - IIS App Pool identity has full control on logs folder

---

## STEP 6: Configure IIS Application Settings

### In Plesk - IIS Settings

1. **Navigate to IIS Settings**
   - Go to domain: **gccwebapp.com**
   - Find **IIS Settings** or **Hosting Settings**

2. **Check/Set Application Pool**
   - .NET CLR Version: **No Managed Code**
   - Pipeline Mode: **Integrated**
   - Enable 32-bit applications: **Unchecked** (should be 64-bit)

3. **Verify Application Root**
   - Document root should be: `/httpdocs`
   - This is where your extracted files are

4. **Save settings**

---

## STEP 7: Restart the Application

### In Plesk

**Option A: Restart Application Pool**

1. **Find Application Pool Settings**
   - In Plesk → Websites & Domains → gccwebapp.com
   - Look for **Application Pool** or **IIS Application Pool**

2. **Restart the Pool**
   - Click **Restart** or **Recycle**
   - Wait 10-20 seconds

**Option B: Restart IIS (if you have access)**

1. **In Plesk SSH/PowerShell** (if available)
   ```powershell
   iisreset
   ```

**Option C: Contact Support**

If you don't see these options:
- Ask AccuWeb support to restart the IIS Application Pool for gccwebapp.com

---

## STEP 8: Verify Deployment

### Test the Website

1. **Open your browser**
   - Navigate to: **https://gccwebapp.com**
   - Or: **https://www.gccwebapp.com**

2. **Expected Results:**
   - ✅ Page loads without HTTP 500 error
   - ✅ You see the GCC homepage or certificate tile view
   - ✅ No "Application Error" messages

3. **Test Key Pages:**
   - Home: https://gccwebapp.com/
   - Certificates: https://gccwebapp.com/Certificate/CertificateTileView
   - Login: https://gccwebapp.com/Account/Login
   - Health: https://gccwebapp.com/health (should return "Healthy")

---

## STEP 9: Check Logs (if issues occur)

### In Plesk File Manager

1. **Navigate to logs folder**
   - Go to httpdocs/logs/

2. **Look for stdout logs**
   - Files named: `stdout_YYYYMMDD_HHMMSS_XXXXX.log`
   - Download the most recent one

3. **Check for errors**
   - Look for lines containing:
     - `Application startup exception`
     - `System.Exception`
     - `ERROR`
     - `FATAL`

### Common Issues and Solutions

**Issue 1: Still getting HTTP 500**

Check:
- Environment variable `ASPNETCORE_ENVIRONMENT=Production` is set
- web.config exists in httpdocs
- appsettings.Production.json exists
- Check stdout logs for specific error

**Issue 2: "The specified module could not be found"**

Solution:
- ASP.NET Core Hosting Bundle not installed
- Contact AccuWeb support: "Please install ASP.NET Core 8.0 Hosting Bundle"

**Issue 3: Database connection errors**

Check:
- SQL Server at 68.71.129.120,1533 is accessible
- Firewall allows connection on port 1533
- Contact AccuWeb to verify network access to database

**Issue 4: Static files not loading (CSS/JS/images)**

Check:
- wwwroot folder exists in httpdocs
- Files are in httpdocs/wwwroot/css/, httpdocs/wwwroot/js/, etc.
- Permissions are correct (755)

---

## STEP 10: Enable Detailed Errors (TEMPORARILY if needed)

If you're still having issues and need to see the actual error:

### Edit appsettings.Production.json

1. **In Plesk File Manager**
   - Navigate to httpdocs
   - Find `appsettings.Production.json`
   - Right-click → **Edit in Code Editor**

2. **Find FeatureFlags section**
   - Look for:
   ```json
   "FeatureFlags": {
     "EnableDetailedErrorMessages": false,
     "EnableDeveloperExceptionPage": false,
   ```

3. **Change to true (TEMPORARILY)**
   ```json
   "FeatureFlags": {
     "EnableDetailedErrorMessages": true,
     "EnableDeveloperExceptionPage": true,
   ```

4. **Save the file**

5. **Restart Application Pool** (Step 7)

6. **Visit the website**
   - You should now see detailed error messages instead of generic HTTP 500

7. **IMPORTANT: Change back to false after fixing!**
   - Set both back to `false`
   - Save and restart again

---

## Configuration Summary

**Deployment Path:** gccwebapp.com/httpdocs/
**Database:** SQL Server at 68.71.129.120,1533
**Database Name:** gccwebap_gccdb
**Environment:** Production
**JWT Key:** Configured (secure 88-character key)

**What's Deployed:**
- ✅ ASP.NET Core 8.0 application
- ✅ 130+ DLL dependencies
- ✅ Static assets (CSS, JS, images, fonts)
- ✅ 12 database migrations (auto-apply on first run)
- ✅ Fixed Data Protection configuration
- ✅ Production-ready web.config

---

## Default User Credentials

**CHANGE THESE IMMEDIATELY AFTER FIRST LOGIN!**

**Admin:**
- Email: admin@greencirclecertified.com
- Password: Admin123!

**Analyst:**
- Email: analyst@greencirclecertified.com
- Password: Analyst123!

**External Partner:**
- Email: partner@amazon.com
- Password: Partner123!

---

## Post-Deployment Security Checklist

- [ ] Verify HTTPS is working (https://gccwebapp.com)
- [ ] Change all default passwords
- [ ] Verify environment variable is set to Production
- [ ] Check that EnableDetailedErrorMessages is false
- [ ] Check that EnableDeveloperExceptionPage is false
- [ ] Test login functionality
- [ ] Test certificate viewing
- [ ] Test file uploads
- [ ] Monitor logs for errors

---

## Support Contacts

**AccuWeb Hosting Support:**
- Submit ticket at: https://manage.accuwebhosting.com/submitticket.php
- Existing ticket: KFU974586N

**What to Include in Support Ticket:**
- Domain: gccwebapp.com
- Issue: ASP.NET Core 8.0 application deployment
- Request: Verify ASP.NET Core Hosting Bundle installed
- Request: Help setting environment variable ASPNETCORE_ENVIRONMENT=Production

---

## Quick Reference Commands

If you have PowerShell/RDP access to the server:

```powershell
# Set environment variable
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", [System.EnvironmentVariableTarget]::Machine)

# Restart IIS
iisreset

# Check logs
Get-Content "C:\inetpub\wwwroot\gccwebapp.com\httpdocs\logs\stdout_*.log" -Tail 50

# Test database connection
sqlcmd -S 68.71.129.120,1533 -d gccwebap_gccdb -U adminGCC -P Q3Ez4elgE_47KNi -Q "SELECT 1"
```

---

## Rollback Plan

If deployment fails and you need to rollback:

1. **Stop the application** (IIS App Pool restart)
2. **Delete all files in httpdocs**
3. **Restore from backup:**
   - Upload your backup: `backup-before-deploy-20251009.zip`
   - Extract to httpdocs
4. **Restart application**
5. **Verify old version works**

---

**Generated:** October 9, 2025
**Package:** GCCWebApp-Production-20251009-FIXED.zip (185 MB)
**Status:** Ready for Deployment

**NEXT STEP:** Upload the package to gccwebapp.com/httpdocs and follow these instructions step-by-step.
