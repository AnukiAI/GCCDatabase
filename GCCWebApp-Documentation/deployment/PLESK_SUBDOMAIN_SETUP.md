# Plesk Subdomain Configuration for ASP.NET Core 8.0

**Subdomain:** gcctest.anukihub.com
**Location:** Home directory (not httpdocs)
**Framework:** ASP.NET Core 8.0
**Issue:** HTTP 500 error

---

## CRITICAL: Plesk Subdomain Must Be Configured as IIS Application

The HTTP 500 error is likely because your subdomain is **NOT configured as a proper IIS Application** in Plesk.

### Check Current Configuration

1. **Log into Plesk Control Panel**
2. **Go to:** Domains > gcctest.anukihub.com
3. **Check:** Hosting Settings or IIS Settings

**Look for:**
- Is there an "Application Root" or "IIS Application" setting?
- What is the "Document Root" path?
- Is there a dedicated Application Pool?

---

## Option 1: Configure Subdomain in Plesk (RECOMMENDED)

### Step 1: Verify Subdomain Settings

1. **In Plesk:** Go to **Domains** > **gcctest.anukihub.com**
2. **Click:** Hosting Settings
3. **Verify:**
   - Document root points to your application folder
   - SSL/TLS is enabled (for HTTPS)
   - PHP is **disabled** (this is .NET, not PHP)

### Step 2: Enable ASP.NET Core Support

In Plesk, you need to ensure ASP.NET Core is enabled:

1. **Go to:** Websites & Domains > gcctest.anukihub.com
2. **Click:** "IIS Settings" or "ASP.NET Settings"
3. **Enable:** .NET Core support
4. **Select:** Dedicated application pool

### Step 3: Configure Application Pool

1. **In Plesk:** Go to Application Pools
2. **Find or Create:** gcctest.anukihub.com pool
3. **Configure:**
   - .NET CLR Version: **No Managed Code** (critical for .NET Core!)
   - Managed Pipeline Mode: Integrated
   - Identity: ApplicationPoolIdentity
   - Enable 32-bit Applications: **False**

### Step 4: Set Document Root

Ensure the document root is pointing to where you extracted the application:

Example paths in Plesk:
- `C:\Inetpub\vhosts\anukihub.com\gcctest`
- OR `C:\Inetpub\vhosts\anukihub.com\subdomains\gcctest\httpdocs`
- OR whatever your actual path is

---

## Option 2: Manual IIS Configuration (If Plesk Doesn't Support .NET Core UI)

If Plesk doesn't have UI options for .NET Core, configure IIS directly:

### Step 1: Access IIS Manager

```powershell
# Run IIS Manager
inetmgr
```

### Step 2: Find Your Subdomain

1. **Expand:** Server > Sites
2. **Look for:** gcctest.anukihub.com or anukihub.com (parent)
3. **Check:** Is gcctest a separate site or a subdirectory?

### Step 3: Convert to Application (if needed)

If gcctest is just a folder under anukihub.com:

1. **Right-click** the gcctest folder
2. **Select:** "Convert to Application"
3. **Application Pool:** Select or create a new pool
4. **Click:** OK

### Step 4: Configure Application Pool

1. **Go to:** Application Pools
2. **Find:** gcctest pool (or create new)
3. **Right-click > Advanced Settings:**
   - **.NET CLR Version:** No Managed Code ⚠️ CRITICAL!
   - **Managed Pipeline Mode:** Integrated
   - **Start Mode:** AlwaysRunning (optional, for better performance)
   - **Identity:** ApplicationPoolIdentity

### Step 5: Set Permissions

```powershell
$appPath = "C:\Inetpub\vhosts\anukihub.com\gcctest"  # UPDATE THIS PATH
$appPool = "gcctest.anukihub.com"  # UPDATE THIS

# Grant permissions
icacls "$appPath" /grant "IIS AppPool\${appPool}:(OI)(CI)RX" /t

# Create logs directory
New-Item -Path "$appPath\logs" -ItemType Directory -Force

# Grant write to logs
icacls "$appPath\logs" /grant "IIS AppPool\${appPool}:(OI)(CI)F" /t
```

---

## web.config Adjustments for Subdomain

Your current web.config should work, but here are two versions depending on your setup:

### Version 1: If Subdomain is IIS Application (RECOMMENDED)

**Current web.config is FINE** - use relative paths:

```xml
<aspNetCore processPath=".\GCCWebApp.exe"
            arguments=".\GCCWebApp.dll"
            stdoutLogEnabled="true"
            stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess" />
```

### Version 2: If Subdomain is Virtual Directory (NOT RECOMMENDED)

You might need absolute paths:

```xml
<aspNetCore processPath="C:\Inetpub\vhosts\anukihub.com\gcctest\GCCWebApp.exe"
            arguments="C:\Inetpub\vhosts\anukihub.com\gcctest\GCCWebApp.dll"
            stdoutLogEnabled="true"
            stdoutLogFile="C:\Inetpub\vhosts\anukihub.com\gcctest\logs\stdout"
            hostingModel="inprocess" />
```

**But I recommend Option 1 (convert to application)**

---

## Common Plesk/Subdomain Issues

### Issue 1: Parent Domain web.config Interfering

**Problem:** The parent domain (anukihub.com) might have a web.config that conflicts.

**Solution:** Add `inheritInChildApplications="false"` to your web.config (already there on line 3!)

```xml
<location path="." inheritInChildApplications="false">
```

### Issue 2: Subdomain Not Isolated

**Problem:** Subdomain shares parent's application pool.

**Solution:** Create dedicated application pool for gcctest subdomain.

### Issue 3: ASP.NET Core Module Not Registered for Subdomain

**Problem:** The subdomain doesn't have ASP.NET Core Module configured.

**Check:**
```powershell
# In IIS Manager, select gcctest subdomain
# Double-click "Modules"
# Look for "AspNetCoreModuleV2" in the list
```

**If missing:**
- Install ASP.NET Core 8.0 Hosting Bundle
- Restart IIS: `iisreset`

### Issue 4: HTTPS/SSL Certificate Issues

**Problem:** Subdomain doesn't have proper SSL certificate.

**Check in Plesk:**
1. Go to gcctest.anukihub.com > SSL/TLS Certificates
2. Ensure certificate is installed and valid
3. Enable "Redirect from HTTP to HTTPS"

**Or temporarily disable HTTPS redirect:**

Edit web.config, comment out lines 22-32:
```xml
<!-- Temporarily disabled for testing
<rewrite>
  <rules>
    <rule name="Redirect to HTTPS" stopProcessing="true">
      ...
    </rule>
  </rules>
</rewrite>
-->
```

---

## Environment Variable for Subdomain

The subdomain application pool needs the environment variable:

### Via IIS Manager:

1. **Application Pools** > **gcctest.anukihub.com**
2. **Right-click** > **Advanced Settings**
3. Look for **Environment Variables** section
4. **Add:**
   - Name: `ASPNETCORE_ENVIRONMENT`
   - Value: `Production`

### Via PowerShell (Machine-wide):

```powershell
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", [System.EnvironmentVariableTarget]::Machine)
iisreset
```

**Note:** Machine-wide affects all sites. For subdomain-specific, use IIS Manager method above.

---

## Updated web.config for Better Error Visibility (TEMPORARY)

For troubleshooting, create this simplified web.config:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath=".\GCCWebApp.exe"
                  arguments=".\GCCWebApp.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

**Key changes for testing:**
1. Added environment variable directly in web.config
2. Removed HTTPS redirect (test HTTP first)
3. Removed custom error pages (see actual errors)
4. Removed security headers (simplify)

**After it works, restore the full web.config with security features!**

---

## Step-by-Step Deployment to Plesk Subdomain

### 1. Determine Your Actual Path

In Plesk File Manager, find where gcctest.anukihub.com points to.

Common Plesk paths:
- `C:\Inetpub\vhosts\anukihub.com\subdomains\gcctest\httpdocs`
- `C:\Inetpub\vhosts\anukihub.com\gcctest`
- Or check in Plesk: Domains > gcctest.anukihub.com > Hosting Settings > Document Root

### 2. Upload via Plesk File Manager

1. **Log into Plesk**
2. **Go to:** Files
3. **Navigate to:** gcctest subdomain folder
4. **Upload:** GCCWebApp-Production-20251009-FIXED.zip
5. **Extract** the ZIP

### 3. Verify Files Extracted

Check that you have:
- ✅ GCCWebApp.exe
- ✅ GCCWebApp.dll
- ✅ web.config
- ✅ appsettings.Production.json
- ✅ wwwroot folder
- ✅ LatoFont folder

### 4. Set Permissions via Plesk

1. **In Plesk File Manager**
2. **Select** the gcctest folder
3. **Click:** Change Permissions
4. **Ensure:** IIS_IUSRS and Application Pool identity have Read & Execute
5. **Create** a `logs` folder
6. **Set** logs folder to have Write permissions

### 5. Configure Application in Plesk

1. **Go to:** Websites & Domains > gcctest.anukihub.com
2. **Click:** IIS Application Pool Settings (or similar)
3. **Set:**
   - .NET CLR Version: No Managed Code
   - Pipeline: Integrated
   - Enable 32-bit: No

### 6. Restart Subdomain

In Plesk:
1. **Go to:** Websites & Domains > gcctest.anukihub.com
2. **Click:** Stop/Start or Restart
3. **Or run:** `iisreset` via RDP/SSH

### 7. Test

Visit: `http://gcctest.anukihub.com/` (try HTTP first)

**If still HTTP 500:**
- Check logs in `logs` folder
- Check Plesk error logs
- Try the simplified web.config above

---

## Getting Detailed Error Information

### Method 1: Enable in appsettings.Production.json

Edit the deployed `appsettings.Production.json`:

```json
"FeatureFlags": {
  "EnableDetailedErrorMessages": true,
  "EnableDeveloperExceptionPage": true
}
```

Restart the app pool, then visit the site - you'll see detailed errors.

### Method 2: Check Plesk Logs

1. **In Plesk:** Websites & Domains > gcctest.anukihub.com
2. **Click:** Logs
3. **View:** Error log and Access log
4. **Look for:** Recent 500 errors with details

### Method 3: Check stdout Logs

1. **Via Plesk File Manager:** Navigate to `gcctest/logs/`
2. **Download:** latest `stdout_*.log` file
3. **Open** and look for exception details

### Method 4: Windows Event Viewer (if you have RDP)

1. **Run:** eventvwr
2. **Go to:** Windows Logs > Application
3. **Filter by:** Source = "IIS AspNetCore Module V2"
4. **Look for:** Recent errors with gcctest

---

## Checklist for Plesk Subdomain Deployment

- [ ] Subdomain exists in Plesk
- [ ] Subdomain configured as IIS Application (not just folder)
- [ ] Dedicated Application Pool created for subdomain
- [ ] Application Pool set to "No Managed Code"
- [ ] ASP.NET Core 8.0 Hosting Bundle installed on server
- [ ] Files extracted to subdomain document root
- [ ] web.config present in root folder
- [ ] Permissions set (IIS_IUSRS can read, logs folder writable)
- [ ] Environment variable ASPNETCORE_ENVIRONMENT=Production set
- [ ] SSL certificate configured (if using HTTPS)
- [ ] Application pool restarted
- [ ] IIS restarted

---

## Quick Test Commands (if you have RDP/PowerShell access)

```powershell
# Find your subdomain path
$subdomainPath = "C:\Inetpub\vhosts\anukihub.com\gcctest"  # UPDATE THIS

# Check files exist
Test-Path "$subdomainPath\GCCWebApp.exe"  # Should be True
Test-Path "$subdomainPath\GCCWebApp.dll"  # Should be True
Test-Path "$subdomainPath\web.config"     # Should be True

# Check app pool
Import-Module WebAdministration
Get-WebApplication | Where-Object { $_.path -like "*gcctest*" }

# View recent logs
Get-Content "$subdomainPath\logs\stdout_*.log" -Tail 50
```

---

## If You Need to Contact AccuWeb Support

Provide them with:

1. **Issue:** ASP.NET Core 8.0 app showing HTTP 500 on subdomain gcctest.anukihub.com
2. **Request:**
   - Confirm ASP.NET Core 8.0 Hosting Bundle is installed
   - Confirm subdomain is configured as IIS Application
   - Confirm application pool is set to "No Managed Code"
   - Check IIS error logs for specific error message
3. **Attach:**
   - Screenshot of error
   - Contents of `logs/stdout_*.log` file (if accessible)

---

## Summary

**Most Likely Issue:** Subdomain is not properly configured as an IIS Application in Plesk.

**Most Likely Fix:**
1. Convert subdomain to IIS Application
2. Create dedicated Application Pool
3. Set pool to "No Managed Code"
4. Deploy FIXED package
5. Set ASPNETCORE_ENVIRONMENT=Production

**Use the simplified web.config during testing, then restore full security web.config after it works.**

---

## Next Steps

1. **Verify** subdomain is IIS Application in Plesk/IIS
2. **Deploy** GCCWebApp-Production-20251009-FIXED.zip
3. **Use** simplified web.config for initial testing
4. **Check** logs if still getting errors
5. **Report back** with specific error message from logs

Would you like me to create the simplified web.config file for testing?
