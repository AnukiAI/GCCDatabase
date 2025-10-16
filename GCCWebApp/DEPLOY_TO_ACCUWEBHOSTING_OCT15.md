# Deploy to AccuWebHosting - October 15, 2025

## Package Ready to Upload

**File:** `GCCWebApp-SelfContained-SecurityFixes-20251015.zip`
**Size:** 100.3 MB
**Type:** Self-contained (includes .NET runtime)
**Includes:** All 6 security fixes from October 15, 2025

---

## Why Self-Contained?

Based on our October 11 deployment experience:
- AccuWebHosting server has .NET 8.0.20 runtime
- But we had .NET 9 dependency conflicts (System.Text.Json 9.0.0)
- **Solution**: Self-contained deployment includes ALL dependencies
- **Result**: No version conflicts, works reliably

---

## Simple Deployment Steps

### 1. Upload to AccuWebHosting

Via Plesk File Manager:
1. Navigate to `httpdocs` folder for anukihub.com
2. Backup existing files (rename to `httpdocs-backup-oct15`)
3. Upload `GCCWebApp-SelfContained-SecurityFixes-20251015.zip`
4. Extract the ZIP file

### 2. Verify web.config

The web.config should already have:
```xml
<aspNetCore processPath=".\GCCWebApp.exe"
            arguments=""
            ...>
```

**KEY**: Uses `.\GCCWebApp.exe` NOT `dotnet` (self-contained)

### 3. Restart IIS in Plesk

1. Go to "Websites & Domains"
2. Find anukihub.com
3. Click "Restart IIS" or "Restart App Pool"

### 4. Test

Visit: https://anukihub.com/Certificate/CertificateTileView

Check logs if issues: `logs\stdout-*.log`

---

## What's Included

### Security Fixes (6 total):
1. **CertificateTileExternalApiController** - Added API Key requirement
2. **CertificatesApiController (Export)** - Added Analyst/Admin authorization
3. **CertificatesApiController (Download)** - Fixed role determination
4. **DashboardController (DownloadStat)** - Removed AllowAnonymous
5. **DashboardController (DownloadTable)** - Removed AllowAnonymous
6. **CertificateController (4 actions)** - Added Analyst/Admin authorization

### Self-Contained Runtime:
- .NET 8.0 runtime included
- All dependencies bundled
- No server-side requirements
- Works on AccuWebHosting .NET 8.0.20

---

## Configuration

### Database Connection

Should already be configured in deployed appsettings.Production.json:
```
Server: 68.71.129.120,1533
Database: gccwebap_certificates
Username: GCC_adminxyz123
Password: [your secure password]
```

### Environment

Set in web.config:
```xml
<environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
<environmentVariable name="DatabaseProvider" value="SqlServer" />
<environmentVariable name="USE_SQL_SERVER" value="true" />
```

---

## If It Doesn't Work

### Check 1: Verify GCCWebApp.exe Exists

The extracted folder should have `GCCWebApp.exe` (self-contained marker).

### Check 2: Check Logs

Look in `logs\stdout-*.log` for startup errors.

### Check 3: web.config

Make sure it says:
```xml
processPath=".\GCCWebApp.exe"
```

NOT:
```xml
processPath="dotnet"  <!-- This won't work! -->
```

---

## Troubleshooting Reference

See: `ACCUWEBHOSTING_REQUIREMENTS.md` for complete requirements and troubleshooting.

Previous successful deployment: October 11, 2025 (GCCWebApp-SelfContained-20251011.zip)

---

## Summary

- Upload: `GCCWebApp-SelfContained-SecurityFixes-20251015.zip`
- Extract to: `httpdocs` folder
- Restart: IIS in Plesk
- Test: https://anukihub.com/Certificate/CertificateTileView

**This should work!** Same approach that worked on October 11.
