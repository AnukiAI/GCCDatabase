# Archive: Old Duplicate Files from September 28, 2025

## Purpose
This archive contains old duplicate files and folders that were found in the parent directory during cleanup on November 24, 2025.

## Contents

### Source Code Folders (17 folders - 3.2 MB)
These were duplicate copies of folders that exist in `GCCWebApp/`. The versions in `GCCWebApp/` are newer and current.

**Archived Folders:**
- `Attributes/`
- `Authentication/`
- `Configuration/`
- `Controllers/`
- `DataProtectionKeys/`
- `Extensions/`
- `HealthChecks/`
- `init-scripts/`
- `Middleware/`
- `Migrations/`
- `Models/`
- `Properties/`
- `Repositories/`
- `Services/`
- `Validation/`
- `Views/`
- `wwwroot/`

### Configuration Files (15 files)
Duplicate configuration and code files:
- `ApplyPartnersMigration.cs` - Migration code (duplicate)
- `appsettings.Development.json` - Dev config (duplicate)
- `appsettings.json` - Main config (duplicate)
- `appsettings.Performance.json` - Performance config (duplicate)
- `appsettings.Production.json` - Production config (duplicate)
- `appsettings.Production.template.json` - Production template (duplicate)
- `appsettings.Security.json` - Security config (duplicate)
- `appsettings.Staging.json` - Staging config (duplicate)
- `appsettings.Test.json` - Test config (duplicate)
- `Program.cs` - Main program file (duplicate, 23KB vs current 31KB)
- `web.config` - IIS config (duplicate)
- `nginx-plesk-config.conf` - Nginx config for Plesk
- `plesk-nginx-config.conf` - Plesk Nginx config
- `GCC_API_Postman_Collection.json` - Postman API collection
- `Technical_Specification.json` - Technical specs

### Database Files (1 file - 696 KB)
- `gcc_database.db` - Old SQLite database file (project now uses SQL Server)

### Temporary Files (2 files)
- `_ul` - Unknown temp file
- `test_data.json` - Empty test data file

## Why These Were Archived

### Issue Identified
On November 24, 2025, during project cleanup, we discovered that the parent directory contained duplicate copies of source code folders and files dated September 28, 2025. These appeared to be an accidental copy or backup that was created in the wrong location.

### Verification
- **File comparison:** Diff checks confirmed these files are different/older than current versions
- **Size comparison:** Current files in `GCCWebApp/` are larger and more recent
- **Date check:** All archived files dated September 28, while current `GCCWebApp/` has updates through November 24

### Decision
Rather than permanently deleting these files (which could cause issues if they contain something unique), they were archived for reference. After verifying the application works correctly without them, this archive can be safely deleted.

## What to Do With This Archive

### Short Term (Next 30 days)
✅ **Keep the archive** - Ensure application works correctly without these files

### After 30 Days
If the application has been working without issues:
1. Verify no one needs these old versions
2. Check that all functionality works
3. Safely delete this entire archive folder

### If Issues Arise
If you discover you need something from this archive:
1. Review the specific files needed
2. Compare with current versions in `GCCWebApp/`
3. Merge any unique content if necessary
4. Document what was recovered

## Archive Statistics

| Category | Count | Size |
|----------|-------|------|
| **Source Folders** | 17 | ~3.2 MB |
| **Config Files** | 15 | ~1.3 MB |
| **Database Files** | 1 | 696 KB |
| **Temp Files** | 2 | < 1 KB |
| **Total** | **35 items** | **~5.2 MB** |

## Important Notes

1. **These files are OUTDATED** - Current versions in `GCCWebApp/` are newer
2. **The application does NOT need these** - They were duplicate/backup copies
3. **Safe to delete** - After 30-day verification period
4. **Current code is in GCCWebApp/** - Always use those versions

## Questions?

If you're unsure whether to keep this archive:
- Check the last modified date of files in `GCCWebApp/`
- Compare file sizes (current should be larger/different)
- Verify application functionality
- If everything works, this archive can be deleted

---

*Archive Created: November 24, 2025*
*Original File Date: September 28, 2025*
*Reason: Duplicate files cleanup*
*Safe to delete after: December 24, 2025*
