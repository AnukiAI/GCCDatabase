# AccuWebHosting Production Server - Deployment Requirements

**PERMANENT REFERENCE - READ THIS FIRST BEFORE EVERY DEPLOYMENT**

---

## Server Configuration (Confirmed)

- **Hosting Provider**: AccuWebHosting
- **Server**: anukihub.com (64.27.52.66)
- **.NET Runtime**: .NET 8.0.20 (confirmed installed)
- **IIS Version**: IIS 10+ with AspNetCoreModuleV2
- **Database**: SQL Server (68.71.129.120,1533)

---

## CRITICAL: Deployment Type Required

**✅ MUST USE: Self-Contained Deployment**

**❌ DO NOT USE: Framework-Dependent Deployment**

### Why Self-Contained?

**Problem Identified (October 11, 2025):**
- Server has .NET 8.0.20 runtime
- Application dependencies included .NET 9 packages (System.Text.Json 9.0.0) **[RESOLVED - now using .NET 8.0.11]**
- Framework-dependent deployment failed with "Could not load file or assembly 'Microsoft.Extensions.Caching.Abstractions, Version=9.0.0.0'"
- **Solution**: Self-contained deployment includes ALL dependencies, avoiding version conflicts

**Current Status (October 15, 2025):**
- Application now fully .NET 8 compatible (EF Core 8.0.11, System.Text.Json 8.0.11)
- Still using self-contained deployment to avoid any server runtime dependency issues
- Architecture: Windows x64 (64-bit)

### Build Command

```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish-selfcontained
```

### web.config for Self-Contained

**CRITICAL: Use .exe with EMPTY arguments, NOT dotnet**

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath=".\GCCWebApp.exe"
                  arguments=""
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="DatabaseProvider" value="SqlServer" />
          <environmentVariable name="USE_SQL_SERVER" value="true" />
        </environmentVariables>
      </aspNetCore>
      <!-- Add security headers, URL rewrite, etc. as needed -->
    </system.webServer>
  </location>
</configuration>
```

**Key Differences:**
- ✅ Self-Contained: `processPath=".\GCCWebApp.exe"` + `arguments=""` (EMPTY!)
- ❌ Framework-Dependent: `processPath="dotnet"` + `arguments=".\GCCWebApp.dll"`
- ❌ WRONG Self-Contained: `processPath=".\GCCWebApp.exe"` + `arguments=".\GCCWebApp.dll"` (will crash!)

**CRITICAL REQUIREMENTS:**
1. **arguments MUST be empty** (`arguments=""`) for self-contained
2. **logs folder MUST exist** for stdout logging to work
3. **Environment variables** should be in web.config (Production, SqlServer, etc.)

---

## Deployment Checklist

- [ ] Build as self-contained: `dotnet publish -c Release -r win-x64 --self-contained true`
- [ ] Verify GCCWebApp.exe exists in publish folder
- [ ] Update web.config with `processPath=".\GCCWebApp.exe"`
- [ ] Ensure appsettings.Production.json has correct database connection
- [ ] Create ZIP package
- [ ] Upload to AccuWebHosting via Plesk File Manager
- [ ] Extract to httpdocs folder
- [ ] Restart IIS in Plesk

---

## Package Size Expectations

- Framework-Dependent: ~60-80 MB
- **Self-Contained: ~150-200 MB** (includes .NET runtime)

**If your package is <100 MB, it's probably framework-dependent - REBUILD IT!**

---

## Known Working Deployments

1. **GCCWebApp-SelfContained-20251011.zip** - ✅ Working
   - Self-contained deployment
   - .exe-based web.config
   - Fixed .NET 9 dependency issue

2. **GCCWebApp-SecurityFixes-Production-20251015.zip** - ❌ Won't Work
   - Framework-dependent deployment
   - Uses `processPath="dotnet"`
   - Will fail with HTTP 500.19

---

## Database Configuration

**CRITICAL: Database Name**
- ✅ Production database: `gccwebap_certificates`
- ❌ Old/wrong name: `gccwebap_gccdb` (will cause crash!)

**Connection String:**
```
Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=adminGCC;Password=Q3Ez4elgE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;
```

**Notes:**
- Port 1533 (not default 1433)
- SQL Server authentication (not Windows auth)
- IP-based connection (domain name not configured)
- **ALWAYS verify database name** before deploying - old packages may have wrong name!

---

## Common Errors

### HTTP 500.19 Error
**Cause**: IIS cannot process web.config file
**Most Likely**: IIS URL Rewrite Module NOT installed on server
**Symptoms**: ALL requests (not just app) return 500.19 with win32 status 183
**Fix**:
1. **Quick Test**: Use web.config.minimal (no URL rewrite) to confirm diagnosis
2. **If minimal works**: Either keep it OR ask hosting to install IIS URL Rewrite Module
3. **If minimal still fails**: Different module missing (AspNetCoreModuleV2) - check Event Viewer

**Secondary Causes**:
- Using framework-dependent deployment with wrong processPath
- Malformed XML in web.config
- 32-bit IIS app pool with 64-bit assemblies (see below)

### "Could not load file or assembly" Error
**Cause**: .NET version mismatch between server and dependencies
**Fix**: Use self-contained deployment

### "Failed to start application"
**Cause**: GCCWebApp.exe not found or permissions issue
**Fix**:
1. Verify .exe exists in deployment
2. Check IIS app pool identity has read/execute permissions

---

## IIS Configuration

**App Pool Settings:**
- .NET CLR Version: **No Managed Code**
- Managed Pipeline Mode: **Integrated**
- Identity: **ApplicationPoolIdentity** (or custom account)

**Website Settings:**
- Physical Path: Points to httpdocs folder with GCCWebApp.exe
- Application Pool: Assigned to GCCWebApp pool

---

## Quick Reference Commands

### Build Self-Contained
```bash
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish-selfcontained
```

### Create Deployment ZIP
```powershell
$timestamp = Get-Date -Format "yyyyMMdd"
Compress-Archive -Path "./publish-selfcontained/*" -DestinationPath "GCCWebApp-SelfContained-$timestamp.zip" -Force
```

### Verify Package Type
```powershell
# Check if GCCWebApp.exe exists (self-contained)
Test-Path "./publish-selfcontained/GCCWebApp.exe"
# Should return: True

# Check package size (should be >100 MB for self-contained)
(Get-Item "GCCWebApp-SelfContained-*.zip").Length / 1MB
# Should be: ~150-200 MB
```

---

## History

- **October 11, 2025**: Identified .NET version conflict, switched to self-contained deployment
- **October 15, 2025**: Security fixes release - need to rebuild as self-contained

---

## IIS App Pool Configuration: 32-bit vs 64-bit

**IMPORTANT**: Our deployment is 64-bit (win-x64). IIS app pool MUST be configured correctly.

### How to Check IIS App Pool Bitness (via Plesk)

1. Go to "Websites & Domains" → Select your site
2. Click "Application Pool" or "Hosting Settings"
3. Look for setting: **"Enable 32-bit Applications"**
   - ✅ Should be: **FALSE** (disabled) - for 64-bit apps
   - ❌ If **TRUE** (enabled) - will not load 64-bit assemblies

### Symptoms of Bitness Mismatch

If IIS app pool is 32-bit but assemblies are 64-bit:
- HTTP 500.19 or HTTP 500.31 errors
- Error: "Could not load file or assembly" (bad format exception)
- Event Viewer shows: "An attempt was made to load a program with an incorrect format"

**Fix**: Contact AccuWebHosting support to set app pool to 64-bit mode (disable 32-bit applications)

### Rebuild for Different Architecture (if needed)

**For 64-bit (current/recommended):**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish-selfcontained
```

**For 32-bit (if server requires it):**
```bash
dotnet publish -c Release -r win-x86 --self-contained true -o ./publish-selfcontained-x86
```

**Note**: October 11 package was 64-bit and worked, so server supports 64-bit.

---

**REMEMBER: Always use self-contained deployment for AccuWebHosting!**

**Last Updated**: October 16, 2025 - Added HTTP 500.19 troubleshooting and 32-bit/64-bit guidance
