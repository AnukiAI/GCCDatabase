# Complete 100% Dev to Production Sync

**Last Updated:** November 17, 2025
**Time Required:** 15-20 minutes
**Purpose:** Sync ALL business data from dev to production for 100% match

---

## Overview

This guide provides a **comprehensive sync** that includes ALL data tables, not just the core entities.

### What's Different from Standard Sync?

**Standard Sync** (SYNC_DEV_TO_PROD.md):
- Companies, Products, Certificates only
- ❌ Missing: CertificateProductIds (ASINs/UPCs)
- ❌ Missing: Certificate Tags
- ❌ Missing: Custom Field Values
- ❌ Missing: Product Identifiers

**Complete Sync** (this guide):
- ✅ ALL lookup tables
- ✅ Companies, Products, Certificates
- ✅ CertificateProductIds (100s of ASINs per certificate)
- ✅ Certificate Tags associations
- ✅ Custom Field Values
- ✅ Product Identifiers
- ✅ Product Subcategories
- ✅ **100% data match with dev**

---

## Prerequisites

- Access to both dev (ANUKI01) and production databases
- sqlcmd installed
- Approximately 20 minutes

---

## Complete Sync Process

### Step 1: Export from Dev Database

**Run:** `export_from_dev_COMPLETE.cmd`

**What it does:**
- Connects to ANUKI01 / GCCDatabase_Dev
- Runs export_dev_to_production_COMPLETE.sql
- Exports 14 different tables in correct dependency order
- Saves output to `PRODUCTION_DATA_COMPLETE.sql`

**Command:**
```bash
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

export_from_dev_COMPLETE.cmd
```

**Expected output:**
```
============================================
GCC Database - Complete Export from Dev
============================================

Running export script...

Export includes:
  - ProductCategories: 6
  - ProductTypes: 370
  - CertificationTypes: 5
  - Partners: 3
  - Tags: 25
  - Companies: 28
  - Products: 39
  - Certificates (Active): 178
  - Product IDs (ASINs/UPCs): 450+
  - Certificate Tags: 89
  - Custom Field Values: 156

Output saved to: PRODUCTION_DATA_COMPLETE.sql
```

**Time:** ~1 minute

---

### Step 2: Import to Production Database

**Run:** `import_to_production_COMPLETE.cmd`

**What it does:**
- Prompts to clear existing data (recommended)
- Connects to 68.71.129.120,1533 / gccwebap_certificates
- Imports PRODUCTION_DATA_COMPLETE.sql
- Verifies data counts
- Saves log to `import_results_COMPLETE.log`

**Command:**
```bash
import_to_production_COMPLETE.cmd
```

**Interactive prompts:**
```
Clear existing certificate data before import? (Y/N)
```

**Recommendation:** Choose **Y** to avoid duplicate key violations.

**Expected output:**
```
============================================
GCC Database - Complete Import to Production
============================================

File found: PRODUCTION_DATA_COMPLETE.sql

Clear existing certificate data before import? (Y/N) Y

Clearing existing data...
Data cleared successfully.

Importing data to production...
This may take several minutes...

============================================
IMPORT SUCCESSFUL
============================================

Verifying imported data...

Entity                     Count
-----------------------    -----
Certificates               178
CertificateProductIds      450
Companies                  28
Products                   39
Tags                       25
```

**Time:** ~5 minutes

---

### Step 3: Restart Production Application

**Critical:** Application must restart to see new data.

**IIS:**
```
1. Open IIS Manager
2. Find GCCWebApp application pool
3. Right-click → Recycle
```

**Or SSH/RDP:**
```bash
iisreset /noforce
```

**Or touch web.config:**
```bash
# SSH into server
touch /path/to/web.config
```

---

### Step 4: Verify Production Site

**1. Public Certificate Page:**
- URL: https://greencirclecertified.com/Certificate/CertificateTileView
- Should display all 178 certificates
- Filters populated correctly
- Search works

**2. Certificate Details:**
- Click on a certificate
- Verify **Product IDs (ASINs/UPCs)** display correctly
- Check that multiple ASINs show (if certificate has them)
- Verify tags are visible (if certificate has tags)

**3. Admin Pages:**
- Login: https://greencirclecertified.com/Account/Login
- Username: `admin` / Password: `TestPass123!`
- Check Product Management page
- Verify custom fields display on certificates

---

## What Gets Synced

### Lookup Tables (14 tables)

| Table | What It Is | Count (approx) |
|-------|------------|----------------|
| ProductCategories | Product categorization | 6 |
| ProductTypes | Product type hierarchy | 370 |
| ProductSubcategories | Product sub-categorization | Varies |
| CertificationTypes | Certificate types (CEF, CSF, RC, etc.) | 5 |
| Partners | Amazon, Wayfair, etc. | 3 |
| Tags | Certificate tags | 25 |
| CustomFields | Certificate custom fields | Varies |

### Business Data (7 tables)

| Table | What It Is | Count (approx) |
|-------|------------|----------------|
| Companies | Certificate holders | 28 |
| Products | Product catalog | 39 |
| ProductIdentifiers | Product ID management | Varies |
| Certificates | Active certificates only | 178 |
| **CertificateProductIds** | **ASINs/UPCs per certificate** | **450+** |
| CertificateTags | Certificate-tag associations | 89 |
| CustomFieldValues | Certificate custom field data | 156 |

---

## What Does NOT Get Synced

**Security & Configuration:**
- Users (keep production users separate)
- Roles (use production role config)
- Permissions (use production permissions)
- ApiKeys (security - never sync)
- PartnerApiKeys (security)
- AmazonApiSettings (security)
- GoogleAnalyticsSettings (production-specific)

**Runtime Data:**
- ETLConfigurations
- ETLLogEntries
- ETLAlerts
- AmazonSyncHistory
- CertificateAggregates (calculated data)

---

## Comparison: Standard vs Complete Sync

| Feature | Standard Sync | Complete Sync |
|---------|---------------|---------------|
| Companies | ✅ | ✅ |
| Products | ✅ | ✅ |
| Certificates | ✅ (Active only) | ✅ (Active only) |
| **CertificateProductIds** | ❌ | ✅ |
| **Certificate Tags** | ❌ | ✅ |
| **Custom Field Values** | ❌ | ✅ |
| Product Identifiers | ❌ | ✅ |
| Product Subcategories | ❌ | ✅ |
| Custom Fields | ❌ | ✅ |
| All Lookup Tables | Partial | ✅ |
| **100% Data Match** | ❌ | ✅ |

---

## Troubleshooting

### Export Fails

**Cannot connect to ANUKI01:**
```
Fix: Verify VPN connection, check dev database is running
```

**Permission denied:**
```
Fix: Verify GCC_DevUser credentials in export script
```

### Import Fails

**Duplicate key violations:**
```
Fix: Choose "Y" when prompted to clear existing data
Solution: Run import again after clearing data
```

**Foreign key constraint violations:**
```
Cause: Data inserted in wrong order
Fix: This shouldn't happen - export script handles dependencies
Check: Verify dev database schema matches production
```

**Timeout errors:**
```
Fix: Import script uses -t 600 (10 minutes timeout)
If still fails: Increase timeout in import_to_production_COMPLETE.cmd
Change: -t 600 to -t 1200 (20 minutes)
```

### Data Imported but Not Visible

**Certificates show but no product IDs:**
```
Fix: Restart application (IIS recycle)
Verify: SELECT COUNT(*) FROM CertificateProductIds;
Should show: 450+ records
```

**Tags not displaying:**
```
Verify: SELECT COUNT(*) FROM CertificateTags;
Check: Tag associations imported correctly
```

**Custom fields empty:**
```
Verify: SELECT COUNT(*) FROM CustomFieldValues;
Check: Custom field values imported
```

---

## Verification Queries

### Check All Data Counts

```sql
-- Lookup tables
SELECT 'ProductCategories' as Table, COUNT(*) FROM ProductCategories
UNION ALL SELECT 'ProductTypes', COUNT(*) FROM ProductTypes
UNION ALL SELECT 'CertificationTypes', COUNT(*) FROM CertificationTypes
UNION ALL SELECT 'Partners', COUNT(*) FROM Partners
UNION ALL SELECT 'Tags', COUNT(*) FROM Tags
UNION ALL SELECT 'CustomFields', COUNT(*) FROM CustomFields

-- Business data
UNION ALL SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates
UNION ALL SELECT 'CertificateProductIds', COUNT(*) FROM CertificateProductIds
UNION ALL SELECT 'CertificateTags', COUNT(*) FROM CertificateTags
UNION ALL SELECT 'CustomFieldValues', COUNT(*) FROM CustomFieldValues

ORDER BY Table;
```

### Verify Product IDs Per Certificate

```sql
-- Should show certificates with multiple product IDs
SELECT
    c.Number as CertificateNumber,
    COUNT(*) as ProductIdCount
FROM Certificates c
LEFT JOIN CertificateProductIds cpi ON c.Id = cpi.CertificateId
GROUP BY c.Number
HAVING COUNT(*) > 1
ORDER BY ProductIdCount DESC;
```

### Verify Certificate Tags

```sql
-- Should show certificates with tags
SELECT
    c.Number as CertificateNumber,
    STRING_AGG(t.Name, ', ') as Tags
FROM Certificates c
INNER JOIN CertificateTags ct ON c.Id = ct.CertificateId
INNER JOIN Tags t ON ct.TagId = t.Id
GROUP BY c.Number;
```

---

## Performance Notes

**Export Time:**
- Small dataset (~200 certs): ~30 seconds
- Medium dataset (~1000 certs): ~2 minutes
- Large dataset (~5000+ certs): ~10 minutes

**Import Time:**
- Depends on:
  - Number of certificates
  - Number of product IDs per certificate
  - Network latency to production server
- Typical: 5-10 minutes

**Total Process:**
- End-to-end: 15-20 minutes including verification

---

## When to Use Complete Sync

**Use Complete Sync when:**
- First time deploying to production
- Dev and prod are significantly out of sync
- You've added certificates with many ASINs/UPCs
- Certificate tags have been updated
- Custom fields have changed
- Need absolute 100% data match

**Use Standard Sync when:**
- Just updating a few certificates
- Only companies/products changed
- Quick updates needed
- Don't need product IDs or tags

---

## See Also

- [Standard Sync Guide](SYNC_DEV_TO_PROD.md) - Simpler, faster sync
- [Quick Start Guide](QUICK_START.md) - Complete database rebuild
- [Critical Schema Requirement](../reference/CRITICAL_SCHEMA_REQUIREMENT.md) - Schema ownership
- [Common Issues](../troubleshooting/COMMON_ISSUES.md) - Troubleshooting

---

## Summary

**Complete Sync includes:**
✅ 14 lookup tables
✅ 7 business data tables
✅ CertificateProductIds (CRITICAL - ASINs/UPCs)
✅ Certificate Tags associations
✅ Custom Field Values
✅ All supporting data
✅ **100% dev/prod data match**

**Does NOT include:**
❌ Users, Roles, Permissions (keep production separate)
❌ API keys, settings (security)
❌ Runtime/log data

**For questions:** swest@anukiconsulting.com
