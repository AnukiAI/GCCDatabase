# Deployment Troubleshooting Guide - gccwebapp.com

**Date:** October 9, 2025
**Status:** Deployment not working - diagnosing issue

---

## STEP 1: Check What Error You're Seeing

### What do you see when visiting https://gccwebapp.com/ ?

**A. HTTP 500 Error (Generic)**
- "500 - Internal Server Error"
- "An error occurred while starting the application"
- Go to → **Diagnostic Step 2** below

**B. HTTP 502.5 Error**
- "502.5 - Process Failure"
- "Failed to start application"
- **Cause:** ASP.NET Core Hosting Bundle not installed
- **Action:** Contact AccuWeb support - "Please install ASP.NET Core 8.0 Hosting Bundle"

**C. Blank Page / White Screen**
- No error message, just blank
- Go to → **Diagnostic Step 2** below

**D. 404 Error**
- "Page not found"
- **Cause:** Files not extracted to correct location
- **Action:** Verify files are in httpdocs root (not in a subfolder)

---

## STEP 2: Enable Detailed Error Messages (CRITICAL)

We need to see the actual error. Follow these steps:

### In Plesk File Manager:

1. **Navigate to httpdocs**
   - Files → File Manager → httpdocs

2. **Find appsettings.Production.json**
   - Look for the file in the file list
   - Right-click → **Edit in Code Editor** (or **Edit in Text Editor**)

3. **Scroll to bottom - Find FeatureFlags section**
   - Look for lines around line 168-176:
   ```json
   "FeatureFlags": {
     "EnableDetailedErrorMessages": false,
     "EnableDeveloperExceptionPage": false,
   ```

4. **Change BOTH to true:**
   ```json
   "FeatureFlags": {
     "EnableDetailedErrorMessages": true,
     "EnableDeveloperExceptionPage": true,
   ```

5. **Save the file**
   - Click **OK** or **Save** or **Apply**

6. **Restart the Application**
   - In Plesk: Websites & Domains → gccwebapp.com → Stop/Start
   - OR: Ask AccuWeb support to restart IIS Application Pool

7. **Visit the website again**
   - Go to: https://gccwebapp.com/
   - **You should now see a detailed error message with red text**
   - Take a screenshot or copy the error text

8. **SEND ME THE ERROR MESSAGE**
   - Copy the entire error text
   - Look for:
     - Exception type (e.g., "System.InvalidOperationException")
     - Error message
     - Stack trace
     - Inner exception

---

## STEP 3: Check the Logs

### In Plesk File Manager:

1. **Navigate to httpdocs/logs/**
   - If the logs folder doesn't exist → **Problem: web.config may not be configured correctly**

2. **Look for stdout log files**
   - Files named: `stdout_20251009_XXXXXX.log` or similar
   - Download the **most recent** one (largest timestamp)

3. **Open the log file**
   - Look for lines containing:
     - `ERROR`
     - `FATAL`
     - `Exception`
     - `Failed to`
     - `Could not`

4. **Common errors to look for:**

   **Error A: "Unable to configure data protection"**
   ```
   System.InvalidOperationException: Unable to configure data protection
   ```
   - **Cause:** appsettings.Production.json still has old config
   - **Action:** Re-upload the FIXED package

   **Error B: "A network-related or instance-specific error occurred"**
   ```
   Error connecting to database at 68.71.129.120,1533
   ```
   - **Cause:** Can't connect to SQL Server
   - **Action:** Firewall blocking port 1533 - contact AccuWeb support

   **Error C: "Could not load file or assembly"**
   ```
   System.IO.FileNotFoundException: Could not load file or assembly
   ```
   - **Cause:** Missing DLL files
   - **Action:** Re-extract the ZIP, ensure all files copied

   **Error D: "The specified module could not be found"**
   ```
   HTTP Error 500.19 - The specified module could not be found
   ```
   - **Cause:** ASP.NET Core Hosting Bundle not installed
   - **Action:** Contact AccuWeb support

---

## STEP 4: Verify File Structure

### In Plesk File Manager → httpdocs:

**You should see these files directly in httpdocs (NOT in a subfolder):**

```
httpdocs/
├── GCCWebApp.dll           ← Main app
├── GCCWebApp.exe           ← Executable
├── web.config              ← IIS config
├── appsettings.json        ← Config
├── appsettings.Production.json  ← Production config
├── wwwroot/                ← Static files folder
│   ├── css/
│   ├── js/
│   ├── images/
│   └── lib/
├── logs/                   ← Log folder (may not exist yet)
└── Many .dll files (130+)
```

**Common mistakes:**

❌ **WRONG:** Files inside a subfolder
```
httpdocs/
└── publish/
    └── GCCWebApp.dll (← TOO DEEP!)
```

❌ **WRONG:** ZIP file not extracted
```
httpdocs/
└── GCCWebApp-Production-20251009-FIXED.zip (← EXTRACT THIS!)
```

✅ **CORRECT:** Files directly in httpdocs
```
httpdocs/
├── GCCWebApp.dll
├── web.config
└── (all other files)
```

**If files are in wrong location:**
1. Select all files in the subfolder
2. Move them to httpdocs root
3. Delete the empty subfolder

---

## STEP 5: Verify web.config

### In Plesk File Manager:

1. **Open web.config**
   - Right-click → Edit in Code Editor

2. **Check line 8-12 should look like this:**
   ```xml
   <aspNetCore processPath="dotnet" arguments=".\GCCWebApp.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
     <environmentVariables>
       <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
     </environmentVariables>
   </aspNetCore>
   ```

3. **Verify these settings:**
   - ✅ `processPath="dotnet"` (correct)
   - ✅ `arguments=".\GCCWebApp.dll"` (correct)
   - ✅ `stdoutLogEnabled="true"` (enables logging)
   - ✅ `<environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />` (environment set)

**If anything is different:**
- The wrong file was uploaded
- Re-upload the FIXED package

---

## STEP 6: Check Permissions

### In Plesk File Manager:

1. **Right-click on httpdocs folder**
   - Select **Change Permissions**

2. **Should be:** `755` or `rwxr-xr-x`

3. **Check logs folder permissions:**
   - If logs folder exists: Should be `755` or `777`
   - If logs folder doesn't exist:
     - Create it: Click **+ Create Directory** → Name: `logs`
     - Set permissions to `777` (full access)

---

## STEP 7: Database Connection Test

This error might be database-related. Let's check:

### Ask AccuWeb Support to Test:

**Request:** "Please test SQL Server connectivity from the web server"

**Connection Details:**
- Server: `68.71.129.120,1533`
- Database: `gccwebap_gccdb`
- User: `adminGCC`
- Password: `Q3Ez4elgE_47KNi`

**Test Command:**
```cmd
sqlcmd -S 68.71.129.120,1533 -d gccwebap_gccdb -U adminGCC -P Q3Ez4elgE_47KNi -Q "SELECT 1"
```

**If this fails:**
- Firewall is blocking port 1533
- Database server not accessible from web server
- AccuWeb needs to open firewall rules

---

## STEP 8: Verify ASP.NET Core Hosting Bundle

### Ask AccuWeb Support:

**Request:** "Please verify ASP.NET Core 8.0 Hosting Bundle is installed on the web server"

**Required Version:** ASP.NET Core 8.0.x Runtime + Hosting Bundle

**Download Link for Support:**
https://dotnet.microsoft.com/download/dotnet/8.0

**After installation:**
- Server must be restarted OR
- IIS must be restarted: `iisreset`

---

## STEP 9: Common Configuration Issues

### Issue A: Wrong Database Provider

**Check appsettings.Production.json line 35:**
```json
"DatabaseProvider": "sqlserver",
```

**Should be:** `"sqlserver"` (NOT `"mysql"` or `"postgres"`)

### Issue B: Wrong Connection String

**Check appsettings.Production.json line 28:**
```json
"DefaultConnection": "Server=68.71.129.120,1533;Database=gccwebap_gccdb;User Id=adminGCC;Password=Q3Ez4elgE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
```

**Verify:**
- Server address is correct
- Port 1533 is specified
- Database name is correct
- Credentials are correct

### Issue C: Data Protection Still Broken

**Check appsettings.Production.json line 162-167:**
```json
"DataProtection": {
  "ApplicationName": "GCC-Production",
  "KeyLifetime": "90.00:00:00",
  "PersistKeysToAzureBlobStorage": false,  ← MUST BE FALSE
  "AzureBlobStorageConnectionString": "",  ← MUST BE EMPTY
```

**If these are wrong:**
- You uploaded the OLD package by mistake
- Re-upload `GCCWebApp-Production-20251009-FIXED.zip`

---

## INFORMATION TO COLLECT FOR ME

Please gather this information so I can help:

1. **What error do you see in the browser?**
   - After enabling detailed errors (Step 2)
   - Screenshot or copy the full error text

2. **What's in the logs?**
   - Download the latest `stdout_*.log` file from httpdocs/logs/
   - Copy the last 50-100 lines
   - OR send me the file

3. **File structure screenshot**
   - Screenshot of Plesk File Manager showing httpdocs contents
   - So I can verify files are in correct location

4. **What did AccuWeb support say?**
   - Copy their response from the support ticket

5. **Verify these files exist in httpdocs:**
   - [ ] GCCWebApp.dll
   - [ ] GCCWebApp.exe
   - [ ] web.config
   - [ ] appsettings.Production.json
   - [ ] wwwroot folder
   - [ ] logs folder (might not exist yet)

---

## Quick Diagnostic Checklist

Run through this checklist:

**Files:**
- [ ] ZIP file was uploaded to httpdocs
- [ ] ZIP file was extracted
- [ ] Files are in httpdocs root (NOT in a subfolder)
- [ ] GCCWebApp.dll exists
- [ ] web.config exists
- [ ] appsettings.Production.json exists
- [ ] wwwroot folder exists with css/js/images inside

**Configuration:**
- [ ] web.config has environment variable set to Production
- [ ] appsettings.Production.json has `PersistKeysToAzureBlobStorage: false`
- [ ] logs folder created with 777 permissions

**Server:**
- [ ] ASP.NET Core 8.0 Hosting Bundle installed (ask AccuWeb)
- [ ] IIS Application Pool restarted after deployment
- [ ] Firewall allows connection to SQL Server port 1533

**Errors:**
- [ ] Detailed error messages enabled (temporarily)
- [ ] Visited website to see detailed error
- [ ] Checked logs folder for error logs

---

## What to Tell AccuWeb Support

**Copy this into your support ticket:**

```
Hello,

I've deployed an ASP.NET Core 8.0 application to gccwebapp.com/httpdocs but it's not working.

Please help verify:

1. Is ASP.NET Core 8.0 Hosting Bundle installed on the web server?
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Need: "Hosting Bundle" for Windows/IIS

2. Can the web server connect to SQL Server?
   - Server: 68.71.129.120 Port: 1533
   - Please test: sqlcmd -S 68.71.129.120,1533 -d gccwebap_gccdb -U adminGCC -P Q3Ez4elgE_47KNi -Q "SELECT 1"

3. Please check the application error logs at:
   - Path: httpdocs/logs/stdout_*.log
   - What errors do you see?

4. Please restart the IIS Application Pool for gccwebapp.com

Thank you!
```

---

## Next Steps

1. **Enable detailed errors** (Step 2 above) - DO THIS FIRST
2. **Take screenshot** of the detailed error
3. **Check logs** (Step 3 above)
4. **Send me:**
   - The detailed error message
   - Last 50 lines from stdout log
   - Screenshot of httpdocs file listing
   - AccuWeb support response

With this information, I can identify exactly what's wrong and provide the specific fix.

---

**Created:** October 9, 2025
**Status:** Awaiting diagnostic information
