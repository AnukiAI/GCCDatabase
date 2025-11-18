# Sync Dev Data to Production

**Last Updated:** November 17, 2025
**Time Required:** 10-15 minutes
**Purpose:** Sync Companies, Products, and Certificates from dev to production

---

## Overview

This guide walks through syncing all data from the development database (ANUKI01) to production (AccuWebHosting).

**What gets synced:**
- Companies (~28)
- Products (~39)
- Active Certificates (~178)
- Supporting data (lookup tables remain unchanged)

---

## Prerequisites

- Access to both dev and production databases
- sqlcmd installed
- Production database cleared of old test data (optional)

---

## Method: Two-Step Export/Import

### Step 1: Export from Dev Database

**Run:** `export_from_dev.cmd`

**Location:** Root directory of GCCWebApp

**What it does:**
- Connects to ANUKI01 / GCCDatabase_Dev
- Runs export_dev_to_production_scripts_v2.sql
- Generates production-compatible INSERT statements
- Saves output to `PRODUCTION_DATA_FROM_DEV_v2.sql`

**Command:**
```bash
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

export_from_dev.cmd
```

**Expected output:**
```
Connecting to dev database...
Exporting data...
Generated INSERT statements for:
- 28 companies
- 39 products
- 178 active certificates

Output saved to: PRODUCTION_DATA_FROM_DEV_v2.sql
```

**Time:** ~30 seconds

---

### Step 2: Import to Production Database

**Run:** `import_to_production.cmd`

**Location:** Root directory of GCCWebApp

**What it does:**
- Connects to 68.71.129.120,1533 / gccwebap_certificates
- Runs PRODUCTION_DATA_FROM_DEV_v2.sql
- Imports all companies, products, certificates
- Verifies data counts
- Saves log to `import_results.log`

**Command:**
```bash
import_to_production.cmd
```

**Expected output:**
```
Connecting to production database...
Importing data...

Data import complete:
✓ Companies: 28
✓ Products: 39
✓ Certificates: 178 (all Active status)

Results saved to: import_results.log
```

**Time:** ~1 minute

**Verify counts:**
```sql
SELECT 'Companies' as Entity, COUNT(*) as Count FROM Companies
UNION ALL
SELECT 'Products', COUNT(*) FROM Products
UNION ALL
SELECT 'Certificates', COUNT(*) FROM Certificates;
```

---

### Step 3: Restart Production Application

**Critical:** Application must restart to see new data.

**Option A: IIS Restart** (if you have IIS Manager access)
1. Open IIS Manager
2. Find GCCWebApp application pool
3. Right-click → Recycle

**Option B: Remote Desktop** (if you have RDP access)
```bash
iisreset /noforce
```

**Option C: Contact Hosting Provider**
If you don't have direct access, contact AccuWebHosting support.

---

### Step 4: Verify Production Site

1. **Open browser:** https://greencirclecertified.com/Certificate/CertificateTileView
2. **Verify:**
   - Certificates are displaying
   - Counts match expected numbers
   - Search and filtering work
   - Certificate details load correctly

---

## Alternative: One-Click Sync

**Run:** `sync_dev_to_production_complete.cmd`

This runs both export and import in sequence. Only use if you want to automate the entire process without reviewing export output first.

**Command:**
```bash
sync_dev_to_production_complete.cmd
```

**Time:** ~2 minutes

---

## Files Created

**Scripts:**
- `export_dev_to_production_scripts_v2.sql` - SQL query to generate INSERT statements
- `export_from_dev.cmd` - Run on dev to export
- `import_to_production.cmd` - Run to import to production
- `sync_dev_to_production_complete.cmd` - One-click full sync

**Output Files:**
- `PRODUCTION_DATA_FROM_DEV_v2.sql` - Generated INSERT statements
- `import_results.log` - Import execution log

**Old Files (no longer needed):**
- `PRODUCTION_SEED_CERTIFICATES.sql` - Old 22-certificate test seed
- `PRODUCTION_DATA_FROM_DEV.sql` - v1 export (superseded by v2)

---

## Troubleshooting

### Cannot connect to ANUKI01
**Fix:** Verify dev database is accessible, check VPN if required

### Cannot connect to 68.71.129.120,1533
**Fix:** Verify production credentials, check firewall (port 1533)

### Column count mismatch
**Fix:** Shouldn't happen with v2 export. If it does, check that export script ran without errors.

### "IDENTITY_INSERT is already ON"
**Fix:** Run this on production:
```sql
SET IDENTITY_INSERT dbo.Companies OFF;
SET IDENTITY_INSERT dbo.Products OFF;
SET IDENTITY_INSERT dbo.Certificates OFF;
```

### "Violation of PRIMARY KEY constraint"
**Fix:** Production database not cleared. Two options:

**Option 1: Clear existing data**
```sql
DELETE FROM CertificateProductIds;
DELETE FROM Certificates;
DELETE FROM Products;
DELETE FROM Companies;
```

**Option 2: Run complete rebuild**
See [Quick Start Guide](QUICK_START.md) for complete database rebuild.

### Data imported but not showing on website
**Fix:** Restart IIS application pool (see Step 3 above)

### Schema ownership issues
**Fix:** Verify tables are in `dbo` schema:
```sql
SELECT SCHEMA_NAME(schema_id), name
FROM sys.tables
ORDER BY SCHEMA_NAME(schema_id), name;
```

All tables must show schema: `dbo`

If tables are in `GCC_adminxyz123` schema:
→ See [Critical Schema Requirement](../reference/CRITICAL_SCHEMA_REQUIREMENT.md)

---

## Data Filtering

The export script filters data to ensure production-ready content:

**Companies:** All companies from dev
**Products:** All products from dev
**Certificates:**
- Status: Active only (no Draft, Expired, or Archived)
- IsPublic: true (publicly visible)
- Valid dates: Not expired

**Not Synced:**
- User accounts (keep production users separate)
- API keys (security)
- Audit logs
- System configuration

---

## Production Considerations

### Before First Sync
- Backup production data if it exists
- Verify dev data is production-ready
- Review certificate statuses
- Check for test/placeholder data

### Regular Syncs
- Schedule during low-traffic hours
- Communicate downtime to users
- Always backup before syncing
- Verify data after sync

### Rollback Plan
If sync causes issues:
1. Have production backup ready
2. Keep previous `PRODUCTION_DATA_FROM_DEV_v2.sql` file
3. Can restore from backup or re-import previous version

---

## Test Credentials

All test accounts use:
- **Password:** TestPass123!
- **Email:** swest@anukiconsulting.com

**Primary Test Accounts:**
- Username: `admin` | Role: Admin
- Username: `analyst` | Role: Analyst
- Username: `external` | Role: External

**Additional:** 11 more test users (sarah.johnson, mike.chen, etc.)

---

## See Also

- [Quick Start Guide](QUICK_START.md) - Complete database rebuild
- [First Time Deployment](FIRST_TIME_DEPLOYMENT.md) - Comprehensive setup
- [Common Issues](../troubleshooting/COMMON_ISSUES.md) - Troubleshooting
- [Schema Overview](../reference/SCHEMA_OVERVIEW.md) - Database structure

---

## Support

If you encounter issues:
1. Check the log files (`import_results.log`)
2. Verify database connection strings
3. Confirm production database schema matches dev
4. Review [Common Issues](../troubleshooting/COMMON_ISSUES.md)

**Contact:** swest@anukiconsulting.com
