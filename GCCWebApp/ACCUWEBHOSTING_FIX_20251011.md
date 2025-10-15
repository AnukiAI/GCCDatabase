# AccuWebHosting Deployment Fix - October 11, 2025

## Problem Identified

AccuWebHosting support claimed the issue was with `web.config`, but the **actual problem** was:

```
Could not load file or assembly 'Microsoft.Extensions.Caching.Abstractions, Version=9.0.0.0'
```

### Root Cause

Your application had a dependency conflict:
- **Server Runtime**: .NET 8.0.20 (installed on AccuWebHosting)
- **Your App Dependencies**: Mixed .NET 8 and .NET 9 packages
- **Problem Package**: `System.Text.Json 9.0.0` was pulling in .NET 9 dependencies

The server's .NET 8 runtime didn't have the .NET 9 assemblies, causing the crash.

## Solution Applied

Created a **self-contained deployment** that includes ALL required dependencies, so it doesn't rely on the server's runtime.

### What Changed

1. Downgraded `System.Text.Json` from 9.0.0 to 8.0.11 in `GCCWebApp.csproj`
2. Published as self-contained for Windows x64
3. Created deployment package: `GCCWebApp-SelfContained-20251011.zip`

## Deployment Instructions

### Step 1: Upload to AccuWebHosting

1. Log into Plesk File Manager
2. Navigate to `httpdocs` folder for `gccwebapp.com`
3. **Backup current files** (rename `httpdocs` to `httpdocs-backup-20251011`)
4. Create new empty `httpdocs` folder
5. Upload `GCCWebApp-SelfContained-20251011.zip`
6. Extract the ZIP file

### Step 2: Keep Your Existing web.config

Your current `web.config` is **fine** (despite what AccuWebHosting said). However, if you want a minimal version:

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
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

**IMPORTANT**: Change `processPath=".\GCCWebApp.exe"` (self-contained uses .exe, not `dotnet`)

### Step 3: Verify Configuration

Make sure you have:
- `appsettings.Production.json` with correct database connection string
- `logs` folder exists with write permissions
- Environment variables set in Plesk (if not in web.config):
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `GCC_JWT_SECRET` (your existing JWT secret)

### Step 4: Restart IIS

In Plesk:
1. Go to "Websites & Domains"
2. Find `gccwebapp.com`
3. Click "Restart IIS" or "Restart App Pool"

### Step 5: Test

Visit https://gccwebapp.com/Certificate/CertificateTileView

Check `logs\stdout-*.log` for any errors.

## Why Self-Contained?

**Pros:**
- Includes all .NET dependencies (no version conflicts)
- Not affected by server runtime updates
- Works on servers without matching .NET version

**Cons:**
- Larger file size (~100MB vs ~10MB)
- Must redeploy for .NET security updates (not automatic)

## Alternative: Framework-Dependent with .NET 9 Runtime

If AccuWebHosting can install **.NET 9 runtime**, you can use the original framework-dependent deployment. However, since they only have .NET 8.0.20, self-contained is the best solution.

## Files Location

- **Deployment Package**: `GCCWebApp-SelfContained-20251011.zip`
- **Location**: `C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\`
- **Source Folder**: `./publish-selfcontained/`

## Next Steps

1. Deploy this package to AccuWebHosting
2. If it works, you're done!
3. If there are still issues, check the `logs\stdout-*.log` files

## Support Response

Reply to AccuWebHosting support:

> Thank you for your help. The issue was not with web.config but with missing .NET 9 dependencies. I've created a self-contained deployment that includes all required assemblies. I've uploaded the new package. Please restart IIS for gccwebapp.com. The application should now start correctly.

---

**Created**: October 11, 2025
**Issue**: Missing Microsoft.Extensions.Caching.Abstractions 9.0.0
**Fix**: Self-contained deployment with all dependencies
