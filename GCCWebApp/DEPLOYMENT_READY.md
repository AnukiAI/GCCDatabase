# GCC Web Application - Production Deployment Package

**Build Date:** October 16, 2025
**Package Version:** Manual Migration Management Enabled
**Status:** READY FOR DEPLOYMENT

---

## Deployment Packages Ready

### ZIP Archive (Windows - Recommended)
**File:** `GCCWebApp_Production_Deployment_20251016.zip`
**Size:** 58 MB
**Format:** Standard ZIP (Windows compatible)
**Location:**
```
C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\GCCWebApp_Production_Deployment_20251016.zip
```

### TAR.GZ Archive (Linux/Mac)
**File:** `GCCWebApp_Production_Deployment_20251016.tar.gz`
**Size:** 58 MB
**Format:** Compressed TAR (Linux/Mac compatible)
**Location:**
```
C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\GCCWebApp_Production_Deployment_20251016.tar.gz
```

---

## What's Included

Both archives contain the complete production deployment:

- **GCCWebApp.exe** - Main application executable
- **GCCWebApp.dll** - Application library
- **All dependencies** - 300+ DLL files
- **Configuration files** - appsettings.*.json
- **Web configuration** - web.config for IIS
- **Database Scripts** - Migration and seeding tools
  - `00_CREATE_SCHEMA.sql` - Create all tables
  - `FULL_TEST_DATA_SEED.sql` - Seed test data
  - `DASHBOARD_DATA_SEED.sql` - Configure dashboard statistics
  - `apply-migrations.cmd` - Manual migration tool
  - `apply-migrations.ps1` - PowerShell migration script
  - `README_DATABASE_SETUP.md` - Complete database guide
- **Documentation** - DEPLOYMENT_INFO.txt

---

## Critical Changes in This Release

### AUTOMATIC MIGRATIONS DISABLED

This deployment includes a critical change to prevent the application from automatically creating or modifying database tables:

**Program.cs (Lines 632-675):**
- Production/Staging environments: Migrations DISABLED
- Application only validates database connectivity on startup
- No tables will be created/modified automatically
- Schema changes must be applied manually using `apply-migrations.cmd`

**What This Means:**
- You have full control over when database schema changes are applied
- No unexpected table creation when application starts
- All tables locked to `dbo` schema (no more wrong-schema tables)
- Manual migration management via dedicated scripts

---

## Quick Deployment Steps

### 1. Extract Archive

**Windows:**
```
Right-click GCCWebApp_Production_Deployment_20251016.zip
→ Extract All → Choose destination
```

**Linux/Mac:**
```bash
tar -xzf GCCWebApp_Production_Deployment_20251016.tar.gz -C /path/to/deployment
```

### 2. Upload to Server

Upload extracted files to your AccuWebHosting server:
- Via FTP/SFTP
- Via Plesk File Manager
- Via Remote Desktop

**Target Location:**
- `/var/www/vhosts/yourdomain.com/httpdocs/` (Linux)
- `C:\inetpub\wwwroot\gccwebapp\` (Windows)

### 3. Update Configuration

Edit `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=YOUR_PASSWORD;..."
  }
}
```

### 4. Restart Application

Stop old version, start new version via:
- Plesk Control Panel
- IIS Manager
- systemctl (Linux)

### 5. Verify Deployment

Check logs for this message:
```
Production environment - skipping automatic migrations (manual migration management enabled)
Production database connectivity verified
```

**This confirms automatic migrations are disabled!**

---

## On-Demand Migration Management

When you need to apply database schema changes:

```bash
cd Database_Scripts
.\apply-migrations.cmd Production
# Type "YES" when prompted
```

**Features:**
- Production confirmation required
- Shows pending migrations before applying
- Preview mode available (`-WhatIf`)
- Detailed error handling
- Automatic wrong-schema cleanup

---

## Documentation

### Quick Reference
- `DEPLOYMENT_INFO.txt` - In deployment package
- `Database_Scripts/README_DATABASE_SETUP.md` - Database setup guide

### Complete Guide
- `PRODUCTION_DEPLOYMENT_GUIDE.md` - Full deployment instructions
- `Database_Scripts/apply-migrations.ps1` - Migration tool documentation

---

## Verification Checklist

After deployment, verify:

- [ ] Old application stopped
- [ ] New files uploaded to server
- [ ] appsettings.Production.json updated
- [ ] Application started successfully
- [ ] Logs show "skipping automatic migrations"
- [ ] NO tables created on startup
- [ ] Health endpoint responding
- [ ] Certificate tile view loads
- [ ] Login page accessible
- [ ] No errors in application logs

---

## Support & Troubleshooting

### Common Issues

**Application still creating tables automatically:**
- You deployed the wrong version
- Verify Program.cs lines 632-675 show "skipping automatic migrations"

**Database connection failed:**
- Check connection string in appsettings.Production.json
- Verify SQL Server is accessible: `sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123`

**Tables in wrong schema:**
- Should NO LONGER happen with this version
- If it occurs, run `00_CREATE_SCHEMA.sql` which includes automatic cleanup

---

## Rebuild Deployment Package

If you need to rebuild the deployment package:

```bash
cd GCCWebApp/Scripts/deployment
.\deploy-accuweb-production.cmd
```

This will create a new `publish-accuweb-production` folder.

To create new archive files:
```powershell
.\create-zip.ps1
```

---

## Key Files Reference

```
GCCWebApp/
├── GCCWebApp_Production_Deployment_20251016.zip  ← Upload this
├── GCCWebApp_Production_Deployment_20251016.tar.gz  ← Or this (Linux)
├── PRODUCTION_DEPLOYMENT_GUIDE.md  ← Read this for complete instructions
├── DEPLOYMENT_READY.md  ← This file
├── publish-accuweb-production/  ← Source folder (not zipped)
├── Database_Scripts/
│   ├── apply-migrations.cmd  ← Run when you need to migrate
│   ├── apply-migrations.ps1
│   ├── 00_CREATE_SCHEMA.sql
│   ├── FULL_TEST_DATA_SEED.sql
│   └── README_DATABASE_SETUP.md
└── Scripts/deployment/
    └── deploy-accuweb-production.cmd  ← Rebuild if needed
```

---

## Summary

**What You Have:**
- 2 archive files (ZIP + TAR.GZ) - 58MB each
- Complete production deployment package
- Manual migration management tools
- Comprehensive documentation

**What Changed:**
- Automatic migrations DISABLED for production
- Manual control over database schema
- All tables locked to `dbo` schema
- On-demand migration via `apply-migrations.cmd`

**Next Step:**
1. Upload one of the archive files to your server
2. Extract and configure
3. Deploy and verify logs show "skipping automatic migrations"

---

**Deployment Package Built:** October 16, 2025, 22:55
**Manual Migration Management:** ENABLED
**Auto-Migrations:** DISABLED
**Schema:** Locked to dbo

**Ready for Production Deployment!**
