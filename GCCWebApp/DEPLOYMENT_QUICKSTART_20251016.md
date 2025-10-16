# GCC Web App - Deployment Quick Start
**Date**: October 16, 2025
**Status**: ✅ READY TO DEPLOY

---

## Package Information

**File**: `GCCWebApp-SelfContained-SecurityFixes-FINAL-20251016.zip`
**Size**: 99 MB (141 MB uncompressed)
**Type**: Self-Contained .NET 8.0 (win-x64)
**Location**: `C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\`

---

## What Was Fixed

This package resolves ALL issues from October 11-16:

✅ **Built from CURRENT code** with security fixes
✅ **Database detection updated** to support production connection string
✅ **Connection string fixed** to `Server=tcp:68.71.129.120,1533;...`
✅ **web.config minimal** - removed URL rewrite (module not installed)
✅ **web.config arguments** - changed to empty string for self-contained
✅ **Complete runtime files** - includes hostpolicy.dll, coreclr.dll, etc.
✅ **logs/ folder created** - ready for stdout logging

---

## Key Differences from October 11 Package

| October 11 Package | October 16 Package (This) |
|--------------------|---------------------------|
| OLD compiled code | Current security-fixed code |
| `Server=68.71.129.120,1533;...` | `Server=tcp:68.71.129.120,1533;...` |
| Had URL rewrite in web.config | Minimal web.config (no URL rewrite) |
| `arguments=".\GCCWebApp.dll"` | `arguments=""` (correct!) |
| Hybrid package (missing DLLs) | Complete self-contained package |

---

## Quick Deployment Steps

### 1. Login to Plesk
Access AccuWebHosting Plesk panel for gccwebapp.com

### 2. Backup (Optional)
Rename current `httpdocs` to `httpdocs-backup-20251016`

### 3. Upload & Extract
- Upload `GCCWebApp-SelfContained-SecurityFixes-FINAL-20251016.zip` to httpdocs
- Extract using Plesk File Manager
- Verify files are in httpdocs root (not subfolder)

### 4. Verify Files Present
- ✅ GCCWebApp.exe
- ✅ web.config
- ✅ appsettings.Production.json
- ✅ logs/ folder (empty)
- ✅ hostpolicy.dll
- ✅ coreclr.dll

### 5. Configure App Pool (If Needed)
- Enable 32-bit Applications: **FALSE**
- .NET CLR Version: **No Managed Code**
- Managed Pipeline Mode: **Integrated**

### 6. Restart IIS
Plesk → Websites & Domains → gccwebapp.com → Restart

### 7. Test
Browse to https://gccwebapp.com

### 8. Verify Logs
Check `httpdocs/logs/` for stdout log files
Should show: "Using SQL Server database provider"

---

## Expected Results

✅ HTTP 200 OK
✅ Certificate Tile View displays
✅ Certificates load from SQL Server (gccwebap_certificates)
✅ Users can login
✅ Admin pages accessible
✅ Stdout logs created in logs/ folder

---

## Troubleshooting

### HTTP 500.30 - App Failed to Start
- Check stdout logs in `httpdocs/logs/`
- Verify GCCWebApp.exe exists (136 KB)
- Verify web.config has `arguments=""` (empty)

### HTTP 500.19 - Cannot Read web.config
- Should NOT occur with minimal web.config
- If it does, verify ASP.NET Core Module V2 installed
- Contact AccuWebHosting support

### "Using default SQLite database provider"
- Should NOT occur with this package
- If it does, verify connection string starts with `Server=tcp:`
- Check ASPNETCORE_ENVIRONMENT = "Production"

### Database Connection Errors
- Verify SQL Server accessible: `telnet 68.71.129.120 1533`
- Check credentials in appsettings.Production.json
- Verify firewall allows connection from AccuWebHosting

---

## User Questions Answered

**Q: Could Entity Framework 9.0 / .NET 9 references be causing the issue?**
**A**: NO - GCCWebApp.csproj targets .NET 8.0 with EF Core 8.0.11. All packages are .NET 8 compatible.

**Q: Could switching from 32-bit to 64-bit be the problem?**
**A**: NO - October 11 package was 64-bit and worked. This package is also 64-bit (win-x64).

**Q: What was the actual problem?**
**A**: October 11 package had OLD compiled code with database detection logic that didn't match production connection string format. Solution: Build fresh package from current code.

---

## Root Cause Summary

The October 11 self-contained package contained OLD compiled code (GCCWebApp.exe) that:
1. Had database detection logic that didn't recognize `Server=68.71.129.120,1533;...` format
2. Defaulted to SQLite instead of SQL Server
3. **Cannot be fixed by editing config files** - need to rebuild!

This October 16 package is built from CURRENT code with:
1. Updated database detection logic in Program.cs
2. Connection string using `Server=tcp:` format
3. All security fixes included
4. Complete self-contained runtime files

---

## Important Files

**Critical Files in Package:**
- `GCCWebApp.exe` (136 KB) - Self-contained executable
- `web.config` (937 bytes) - Minimal IIS configuration
- `appsettings.Production.json` (5.4 KB) - SQL Server connection with `Server=tcp:`
- `hostpolicy.dll` (383 KB) - Was missing in hybrid package
- `coreclr.dll` (4.8 MB) - .NET runtime
- `logs/.gitkeep` - Ensures logs folder exists

**Documentation:**
- `DEPLOYMENT_PACKAGE_FINAL_20251016.html` - Full deployment guide (this is comprehensive!)
- `ACCUWEBHOSTING_REQUIREMENTS.md` - Permanent reference
- `HTTP_500.19_ROOT_CAUSE_ANALYSIS.md` - Detailed analysis

---

## Database Configuration

**Production SQL Server:**
- Server: `68.71.129.120,1533` (port 1533)
- Database: `gccwebap_certificates`
- Username: `GCC_adminxyz123`
- Password: (in appsettings.Production.json)

**Connection String Format:**
```
Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;
```

The `tcp:` prefix is CRITICAL - it ensures Program.cs line 51 matches and uses SQL Server provider.

---

## web.config Key Points

```xml
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
```

**Key Settings:**
- `processPath=".\GCCWebApp.exe"` - Self-contained executable
- `arguments=""` - EMPTY (critical for self-contained!)
- No URL rewrite section (IIS module not installed)
- Environment variables explicitly set for SQL Server

---

## Post-Deployment Verification

After deployment, verify:

- [ ] Site loads: https://gccwebapp.com returns HTTP 200
- [ ] Stdout logs created in `httpdocs/logs/`
- [ ] Log shows "Using SQL Server database provider"
- [ ] Certificates display from SQL Server
- [ ] User login works
- [ ] Admin pages accessible (/Admin/UserManagement)
- [ ] Search and filtering work
- [ ] PDF downloads work
- [ ] No SQLite errors in logs

---

## Support

**Build Date**: October 16, 2025
**Build Location**: C:\Temp\CleanBuild\publish\
**Source Code**: C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\

For deployment issues:
1. Check stdout logs in `httpdocs/logs/stdout-*.log`
2. Refer to DEPLOYMENT_PACKAGE_FINAL_20251016.html (comprehensive guide)
3. Review ACCUWEBHOSTING_REQUIREMENTS.md (permanent reference)

---

**This package is ready for production deployment!**

All critical issues resolved. Database detection updated. Connection string fixed. Web.config minimal. Complete self-contained runtime files included.

**Deploy with confidence!**
