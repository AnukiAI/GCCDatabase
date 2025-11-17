# Production Issue: No Certificate Data Displaying

**Date:** 2025-11-14
**Status:** DIAGNOSED - SOLUTION PROVIDED
**Severity:** High (Public page empty)

---

## Issue Description

Production website (https://gccwebapp.com/) loads successfully but displays NO certificate data.

**User Report:**
> "The page loads but no certificate data is available. is the seed data sql script completely updated for all information"

---

## Root Cause Analysis

### ✅ What's Working:
- Website loads without HTTP 500 errors
- Database connection established
- Database schema created (35+ tables)
- Lookup tables populated (Categories, Types, Partners, Roles, Permissions)

### ❌ What's Missing:
- **0 Certificates** in database
- **0 Companies** in database
- **0 Products** in database

### Why This Happened:

The `PRODUCTION_ESSENTIAL_SEED.sql` script that was run only seeds **structure and lookup tables**. It does NOT include:
- Companies
- Products
- Certificates

Therefore, the public certificate tile view page has nothing to display.

---

## Solution

Run the **`PRODUCTION_SEED_CERTIFICATES.sql`** script to populate the database with test certificate data.

### What This Script Does:

1. **Seeds 10 Companies**
   - CertainTeed, Owens Corning, Johns Manville, Interface, Shaw Industries, Mohawk, Philips, P&G, Unilever, Herman Miller

2. **Seeds 10 Products**
   - Insulation, Carpet, Lighting, Consumer products
   - Mapped to appropriate companies

3. **Seeds 22 Active Certificates**
   - Certificate types: CEF, CSF, RC
   - Partners: Amazon, Wayfair, None
   - All marked as Active and Public
   - Expiration dates in 2027 (3 years from issue)

### Script Features:

- ✅ **Production-Compatible:** Includes all required fields (CreatedAt, UpdatedAt, IsPublic)
- ✅ **Safe:** Uses transactions with error handling
- ✅ **Idempotent:** Checks if data exists before inserting
- ✅ **Verified:** Includes SQL Server QUOTED_IDENTIFIER settings
- ✅ **Minimal:** Only 22 certificates for quick testing

---

## How to Apply Fix

### Method 1: SQL Server Management Studio (Recommended)

1. Open SQL Server Management Studio
2. Connect to: `68.71.129.120,1533`
3. Database: `gccwebap_certificates`
4. Open file: `PRODUCTION_SEED_CERTIFICATES.sql`
5. Execute (F5)
6. Verify success in Messages tab
7. Restart IIS application

### Method 2: Command Line (sqlcmd)

```cmd
sqlcmd -S 68.71.129.120,1533 ^
       -U GCC_adminxyz123 ^
       -P [password] ^
       -d gccwebap_certificates ^
       -i PRODUCTION_SEED_CERTIFICATES.sql
```

### Method 3: Use the ZIP Package

Extract `GCC-Database-With-Certificates.zip` and follow the README instructions.

---

## Expected Results After Fix

### Database Verification:
```sql
SELECT COUNT(*) FROM Certificates;  -- Should return: 22
SELECT COUNT(*) FROM Companies;     -- Should return: 10
SELECT COUNT(*) FROM Products;      -- Should return: 10
```

### Website Verification:

Visit: https://gccwebapp.com/

You should see:
- ✅ 22 certificate tiles displaying
- ✅ Company names visible (CertainTeed, Owens Corning, etc.)
- ✅ Product names shown
- ✅ Filters populated with data
- ✅ Search autocomplete working
- ✅ Certificate download functionality operational

Sample certificates:
- `24-0001` - CertainTeed Sustainable Insulation (CEF)
- `24-0010` - Owens Corning EcoTouch (CEF)
- `24-0100` - Interface TacTiles Carpet (CEF)
- `24-0200` - Philips LED Lighting (CEF)
- `24-0300` - P&G Tide Purclean (CEF)

---

## Files Created

| File | Purpose | Location |
|------|---------|----------|
| `PRODUCTION_SEED_CERTIFICATES.sql` | Main seed script | GCCWebApp\ |
| `INSTRUCTIONS_SEED_DATABASE.txt` | Step-by-step guide | GCCWebApp\ |
| `GCC-Database-With-Certificates.zip` | Complete package | GCCWebApp\ |
| `ISSUE_SUMMARY_NO_CERTIFICATES.md` | This document | GCCWebApp\ |

---

## Timeline

| Time | Event | Result |
|------|-------|--------|
| Earlier | Deployed ERROR-FIX package | ✅ 500 error resolved |
| Earlier | Ran database rebuild scripts | ✅ Schema created |
| Earlier | Ran PRODUCTION_ESSENTIAL_SEED.sql | ✅ Lookup tables populated |
| **Now** | No certificate data | ❌ Public page empty |
| **Next** | Run PRODUCTION_SEED_CERTIFICATES.sql | ✅ Will populate certificates |

---

## Prevention for Future Deployments

### Production Deployment Checklist:

1. ✅ Deploy application code (web.config, DLLs)
2. ✅ Run database migration scripts (CREATE SCHEMA)
3. ✅ Run PRODUCTION_ESSENTIAL_SEED.sql (lookup tables)
4. ✅ **Run PRODUCTION_SEED_CERTIFICATES.sql** (certificate data) ← THIS STEP WAS MISSING
5. ✅ Restart IIS application
6. ✅ Verify public page displays certificates

### Two Types of Seed Scripts:

**Essential Seed (PRODUCTION_ESSENTIAL_SEED.sql):**
- Product Categories
- Certification Types
- Partners
- Roles & Permissions
- Product Types
- **NO Certificates, NO Companies, NO Products**

**Certificate Data Seed (PRODUCTION_SEED_CERTIFICATES.sql):**
- Companies (10)
- Products (10)
- Certificates (22)
- **Use this for public page to display data**

---

## Alternative: Full Test Data

For comprehensive testing with 175+ certificates:

File: `Database_Scripts\FULL_TEST_DATA_SEED.sql`

**Note:** This larger script had compatibility issues with production SQL Server (UpdatedAt column constraints). The `PRODUCTION_SEED_CERTIFICATES.sql` script was created specifically to be production-compatible.

---

## Technical Details

### Database Connection:
- Server: `68.71.129.120,1533`
- Database: `gccwebap_certificates`
- User: `GCC_adminxyz123`
- Schema Owner: `dbo` (required for Entity Framework)

### Script Compatibility:
```sql
SET QUOTED_IDENTIFIER ON;  -- Required for indexed views/computed columns
SET ANSI_NULLS ON;         -- Required for SQL Server standards
SET NOCOUNT ON;            -- Suppress row count messages
```

### Error Handling:
```sql
BEGIN TRY
    BEGIN TRANSACTION;
    -- Seed data
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH
```

---

## Status: READY TO APPLY

All files created and tested. Ready for you to execute in SQL Server Management Studio.

**Next Action:** Run `PRODUCTION_SEED_CERTIFICATES.sql` in SSMS

---

## Support

If you encounter any issues after running the seed script:

1. Check the Messages tab in SSMS for errors
2. Verify data: `SELECT * FROM Certificates`
3. Check table ownership: `SELECT SCHEMA_NAME(schema_id), name FROM sys.tables`
4. Restart IIS application
5. Clear browser cache
6. Check application logs in `/logs` folder

---

**Created:** 2025-11-14
**Last Updated:** 2025-11-14
**Script Version:** 1.0 (Production-Compatible)
