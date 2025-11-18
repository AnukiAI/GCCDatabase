# Database Quick Start Guide

**Last Updated:** November 17, 2025
**Time Required:** 5-10 minutes
**Purpose:** Complete database rebuild with test data

---

## Overview

This guide provides the fastest way to get a working GCC database with test certificates.

**What you'll get:**
- Complete database schema (35+ tables)
- Test data: 28 companies, 43 products, 175+ certificates
- 15 test user accounts
- Ready-to-use system for testing

---

## Prerequisites

- SQL Server Management Studio (SSMS) or sqlcmd
- Production database credentials
- DB_REBUILD_PACKAGE_WITH_DATA folder

---

## Quick Start (3 Steps)

### Step 1: Connect to Database

**Using SSMS:**
1. Open SQL Server Management Studio
2. Connect with these credentials:
   - **Server:** 68.71.129.120,1533
   - **Authentication:** SQL Server Authentication
   - **Login:** GCC_adminxyz123
   - **Password:** Q3Ez4e!gE_47KNi
   - **Database:** gccwebap_certificates

**Using sqlcmd:**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates
```

### Step 2: Run Scripts in Order

Execute these scripts in **exact order**:

#### 2a. Drop All Tables
```
File: DB_REBUILD_PACKAGE_WITH_DATA/01_DROP_ALL_TABLES.sql
```

**What it does:** Removes all existing tables and constraints

**Expected output:**
```
All tables dropped successfully
```

#### 2b. Create Schema
```
File: DB_REBUILD_PACKAGE_WITH_DATA/02_CREATE_SCHEMA.sql
```

**What it does:** Creates all 35+ database tables, indexes, and constraints

**Expected output:**
```
35+ tables created successfully
```

**Time:** ~30 seconds

#### 2c. Seed Data
```
File: DB_REBUILD_PACKAGE_WITH_DATA/03_SEED_DATA_WITH_CERTIFICATES.sql
```

**What it does:** Loads all test data

**Expected output:**
```
✓ 6 Product Categories
✓ 5 Certification Types
✓ 3 Partners
✓ 4 Roles
✓ 28 Companies
✓ 43 Products
✓ 175+ Certificates
✓ 15 Users
```

**Time:** ~1 minute

### Step 3: Restart Application

**Critical:** Application must restart to see new data.

**IIS:**
```
1. Open IIS Manager
2. Find GCCWebApp application pool
3. Right-click → Recycle
```

**Or touch web.config:**
```bash
# SSH into server
touch /path/to/web.config
```

**Or wait 2-3 minutes** for auto-restart.

---

## Verification

### Verify Data Counts

Run these queries in SSMS:

```sql
-- Should show 175+
SELECT COUNT(*) as CertificateCount FROM Certificates;

-- Should show 28
SELECT COUNT(*) as CompanyCount FROM Companies;

-- Should show 43
SELECT COUNT(*) as ProductCount FROM Products;

-- Should show 15
SELECT COUNT(*) as UserCount FROM Users;
```

### Verify Schema Ownership

**CRITICAL:** All tables must be in `dbo` schema.

```sql
SELECT SCHEMA_NAME(schema_id) as SchemaName, COUNT(*) as TableCount
FROM sys.tables
GROUP BY SCHEMA_NAME(schema_id);
```

**Expected result:**
```
SchemaName  TableCount
----------  ----------
dbo         35
```

**If you see `GCC_adminxyz123` schema:**
→ See [Critical Schema Requirement](../reference/CRITICAL_SCHEMA_REQUIREMENT.md)

### Verify Website

1. **Public page:** https://greencirclecertified.com/Certificate/CertificateTileView
   - Should display certificate tiles
   - Filters should be populated
   - Search should work

2. **Admin login:** https://greencirclecertified.com/Account/Login
   - Username: `admin`
   - Password: `TestPass123!`
   - Should access admin dashboard

---

## Test User Accounts

All test accounts use:
- **Password:** TestPass123!
- **Email:** swest@anukiconsulting.com

**Admin Access:**
- Username: `admin` (full system access)

**Analyst Access:**
- Username: `analyst` (certificate management)

**External Access:**
- Username: `external` (limited access)

**See script for all 15 test users.**

---

## Troubleshooting

### Script fails with "Table already exists"
**Fix:** Tables weren't dropped. Re-run `01_DROP_ALL_TABLES.sql`

### Script fails with "PRIMARY KEY constraint violation"
**Fix:** Old data still exists. Re-run `01_DROP_ALL_TABLES.sql` first

### Certificates don't display on website
**Checklist:**
1. Verify data exists: `SELECT COUNT(*) FROM Certificates WHERE Status = 'Active'`
2. Check schema: Must be `dbo.Certificates` (not `GCC_adminxyz123.Certificates`)
3. Restart application (IIS recycle)
4. Clear browser cache (Ctrl+Shift+Delete)
5. Check logs folder for errors

**If tables in wrong schema:**
→ See [Zero Certificates Issue](../troubleshooting/ZERO_CERTIFICATES_ISSUE.md)

### Login fails after seeding
**Fix:**
1. Verify users exist: `SELECT Username FROM Users`
2. Use exact credentials: `admin` / `TestPass123!`
3. Ensure 2FA is disabled in `appsettings.Production.json`

### "Invalid object name 'dbo.Certificates'"
**Fix:** Tables created in wrong schema
→ See [Critical Schema Requirement](../reference/CRITICAL_SCHEMA_REQUIREMENT.md)

---

## Alternative: Command Line Execution

If you prefer command line:

```bash
# Navigate to rebuild package
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\DB_REBUILD_PACKAGE_WITH_DATA"

# Run all scripts in sequence
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "01_DROP_ALL_TABLES.sql"

sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "02_CREATE_SCHEMA.sql"

sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "03_SEED_DATA_WITH_CERTIFICATES.sql"
```

---

## Next Steps

1. ✅ Verify public page displays certificates
2. ✅ Test search and filters
3. ✅ Login as admin
4. Begin customizing test data or adding real certificates
5. Consider syncing real dev data (see [Sync Dev to Prod](SYNC_DEV_TO_PROD.md))

---

## Production Notes

**This package includes test data for demonstration.**

For production use:
1. Run these scripts to get a working system
2. Login as admin (`admin` / `TestPass123!`)
3. Create your real admin account
4. Change default admin password
5. Replace test data with real production data

Or use [Sync Dev to Prod](SYNC_DEV_TO_PROD.md) to import real data.

---

## See Also

- [First Time Deployment](FIRST_TIME_DEPLOYMENT.md) - Comprehensive deployment guide
- [Sync Dev to Production](SYNC_DEV_TO_PROD.md) - Import real data
- [Critical Schema Requirement](../reference/CRITICAL_SCHEMA_REQUIREMENT.md) - Schema ownership
- [Common Issues](../troubleshooting/COMMON_ISSUES.md) - Troubleshooting guide
