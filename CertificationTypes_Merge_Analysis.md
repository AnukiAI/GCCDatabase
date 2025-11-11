# CertificationTypes Merge Analysis & Strategy

**Date:** 2025-11-10
**Production Count:** 10 types
**Dev Count:** 22 types
**Objective:** Merge both datasets without losing Production's custom types

---

## Current State

### Production (10 types)

| ID | Name | Source |
|----|------|--------|
| 1 | Biobased | ✓ Matches Dev #1 |
| 2 | Biodegradable | ✓ Matches Dev #2 |
| 3 | Recycled Content | ✓ Matches Dev #15 |
| 4 | Certified Sustainability Facts | ✓ Matches Dev #6 |
| 5 | Waste Diversion from Landfill | ✓ Matches Dev #19 |
| 6 | **Carbon Neutral** | ⚠️ **PRODUCTION ONLY** |
| 7 | **Energy Efficient** | ⚠️ **PRODUCTION ONLY** |
| 8 | **Water Conservation** | ⚠️ **PRODUCTION ONLY** |
| 9 | **FSC Certified** | ⚠️ **PRODUCTION ONLY** |
| 10 | **GREENGUARD Gold** | ⚠️ **PRODUCTION ONLY** |

### Dev (22 types)

| ID | Name | Status |
|----|------|--------|
| 1 | Biobased | In Prod (ID 1) |
| 2 | Biodegradable | In Prod (ID 2) |
| 3 | Carbon Footprint Reduction | **NEW** |
| 4 | Certified Energy Savings | **NEW** |
| 5 | Certified Environmental Facts | **NEW** |
| 6 | Certified Sustainability Facts | In Prod (ID 4) |
| 7 | Certified Water Savings | **NEW** |
| 8 | Closed Loop Product | **NEW** |
| 9 | Dematerialization | **NEW** |
| 10 | Greenhouse Gas (GHG) Verification | **NEW** |
| 11 | GreenCircle Audited | **NEW** |
| 12 | Life Cycle Assessment (LCA) Optimized | **NEW** |
| 13 | Manufacturer Inventory Verification | **NEW** |
| 14 | Recyclable Material | **NEW** |
| 15 | Recycled Content | In Prod (ID 3) |
| 16 | Renewable Resource Content | **NEW** |
| 17 | Sustainable Energy Practices | **NEW** |
| 18 | Sustainable Manufacturing Practices | **NEW** |
| 19 | Waste Diversion from Landfill | In Prod (ID 5) |
| 20 | Waste System Audit | **NEW** |
| 21 | +Vantage Vinyl™ Verified | **NEW** |
| 22 | Preloved | **NEW** |

---

## Analysis

### Matching Types (5 types in both)

| Prod ID | Dev ID | Name |
|---------|--------|------|
| 1 | 1 | Biobased |
| 2 | 2 | Biodegradable |
| 3 | 15 | Recycled Content |
| 4 | 6 | Certified Sustainability Facts |
| 5 | 19 | Waste Diversion from Landfill |

**Action:** Keep Production IDs (1-5), do not re-import

### Production-Only Types (5 types)

| Prod ID | Name | In Use? |
|---------|------|---------|
| 6 | Carbon Neutral | Unknown |
| 7 | Energy Efficient | Unknown |
| 8 | Water Conservation | Unknown |
| 9 | FSC Certified | Unknown |
| 10 | GREENGUARD Gold | Unknown |

**Action:** Keep these (IDs 6-10), preserve Production data

### Dev-Only Types (17 NEW types)

1. Carbon Footprint Reduction
2. Certified Energy Savings
3. Certified Environmental Facts
4. Certified Water Savings
5. Closed Loop Product
6. Dematerialization
7. Greenhouse Gas (GHG) Verification
8. GreenCircle Audited
9. Life Cycle Assessment (LCA) Optimized
10. Manufacturer Inventory Verification
11. Recyclable Material
12. Renewable Resource Content
13. Sustainable Energy Practices
14. Sustainable Manufacturing Practices
15. Waste System Audit
16. +Vantage Vinyl™ Verified
17. Preloved

**Action:** Add these with new IDs (11-27)

---

## Recommended Merge Strategy

### Option A: Conservative Merge (RECOMMENDED) ✅

**Keep:** All 10 Production types (IDs 1-10)
**Add:** 17 Dev-only types as new records (IDs 11-27)
**Result:** 27 total CertificationTypes

**Pros:**
- No data loss from Production
- Preserves any certificates using Prod's custom types (6-10)
- Safe - no risk of breaking existing certificate associations
- All Dev types become available

**Cons:**
- Some duplication risk if Prod's custom types overlap with Dev types conceptually
- Final count is 27 instead of Dev's 22

### Option B: Replace with Dev (NOT RECOMMENDED) ❌

**Delete:** All 10 Production types
**Insert:** All 22 Dev types
**Result:** 22 total CertificationTypes

**Pros:**
- Clean, matches Dev exactly
- No ID conflicts

**Cons:**
- **CRITICAL:** Loses 5 Production custom types (Carbon Neutral, Energy Efficient, etc.)
- **CRITICAL:** May break certificates that reference IDs 6-10
- **CRITICAL:** May break CustomFields that reference CertificateTypeId

### Option C: Smart Replace (COMPLEX) ⚠️

**Keep:** Matching 5 types with Prod IDs (1, 2, 3, 4, 5)
**Replace:** IDs 6-10 with new Dev types
**Add:** Remaining Dev types as new records
**Result:** 22 total CertificationTypes

**Pros:**
- Exact match with Dev
- Clean ID structure

**Cons:**
- **CRITICAL:** Loses 5 Production custom types
- Requires checking if Certificates or CustomFields reference IDs 6-10
- More complex migration

---

## Decision: Option A - Conservative Merge ✅

### Rationale

1. **Data Preservation:** Keep all Production data (no loss)
2. **Safety:** No risk of breaking existing certificate associations
3. **Completeness:** All Dev types become available in Production
4. **Simplicity:** Straightforward append operation

### Implementation Plan

1. **Check Dependencies:**
   - Query Certificates table for CertificationTypeId references
   - Query CustomFields table for CertificateTypeId references

2. **If Production IDs 6-10 are in use:**
   - MUST use Option A (keep them)

3. **If Production IDs 6-10 are NOT in use:**
   - Could use Option C (replace them)
   - BUT still recommend Option A for safety

4. **Execute Merge:**
   - Insert 17 new types from Dev with IDs 11-27
   - Adjust SortOrder if needed

---

## Pre-Merge Verification

Run these queries on Production to check dependencies:

```sql
-- Check if Prod's custom types (6-10) are used by certificates
SELECT CertificationTypeId, COUNT(*) AS CertificateCount
FROM Certificates
WHERE CertificationTypeId BETWEEN 6 AND 10
GROUP BY CertificationTypeId;

-- Check if Prod's custom types (6-10) are referenced by CustomFields
SELECT CertificateTypeId, COUNT(*) AS FieldCount
FROM CustomFields
WHERE CertificateTypeId BETWEEN 6 AND 10
GROUP BY CertificateTypeId;

-- Total certificates by type (all types)
SELECT ct.Id, ct.Name, COUNT(c.Id) AS CertificateCount
FROM CertificationTypes ct
LEFT JOIN Certificates c ON ct.Id = c.CertificationTypeId
GROUP BY ct.Id, ct.Name
ORDER BY ct.Id;
```

---

## Merge SQL Script

```sql
-- CertificationTypes Merge: Add Dev's 17 new types to Production
-- Production keeps IDs 1-10, Dev's new types get IDs 11-27

USE gccwebap_certificates;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Verify we have exactly 10 types before merge
    IF (SELECT COUNT(*) FROM CertificationTypes) <> 10
    BEGIN
        RAISERROR('Production has unexpected count. Expected 10, merge aborted.', 16, 1);
        RETURN;
    END

    PRINT 'Adding 17 new CertificationTypes from Dev...';

    SET IDENTITY_INSERT CertificationTypes ON;

    -- Dev types that don't exist in Production (17 types)
    INSERT INTO CertificationTypes (Id, Name, Description, Level, ParentId, SortOrder, IsActive) VALUES
    (11, 'Carbon Footprint Reduction', 'Products with reduced carbon emissions', 1, NULL, 11, 1),
    (12, 'Certified Energy Savings', 'Energy efficiency certification', 1, NULL, 12, 1),
    (13, 'Certified Environmental Facts', 'Environmental impact certification', 1, NULL, 13, 1),
    (14, 'Certified Water Savings', 'Water conservation certification', 1, NULL, 14, 1),
    (15, 'Closed Loop Product', 'Products designed for circular economy', 1, NULL, 15, 1),
    (16, 'Dematerialization', 'Products with reduced material usage', 1, NULL, 16, 1),
    (17, 'Greenhouse Gas (GHG) Verification', 'Verified greenhouse gas reduction', 1, NULL, 17, 1),
    (18, 'GreenCircle Audited', 'Third-party audited certification', 1, NULL, 18, 1),
    (19, 'Life Cycle Assessment (LCA) Optimized', 'Optimized environmental impact across product lifecycle', 1, NULL, 19, 1),
    (20, 'Manufacturer Inventory Verification', 'Verified manufacturer inventory practices', 1, NULL, 20, 1),
    (21, 'Recyclable Material', 'Products made from recyclable materials', 1, NULL, 21, 1),
    (22, 'Renewable Resource Content', 'Products from renewable resources', 1, NULL, 22, 1),
    (23, 'Sustainable Energy Practices', 'Sustainable energy manufacturing practices', 1, NULL, 23, 1),
    (24, 'Sustainable Manufacturing Practices', 'Environmentally responsible manufacturing', 1, NULL, 24, 1),
    (25, 'Waste System Audit', 'Audited waste management systems', 1, NULL, 25, 1),
    (26, '+Vantage Vinyl™ Verified', 'Verified Vantage Vinyl certification', 1, NULL, 26, 1),
    (27, 'Preloved', 'Refurbished or pre-owned sustainable products', 1, NULL, 27, 1);

    SET IDENTITY_INSERT CertificationTypes OFF;

    PRINT 'Added 17 new CertificationTypes';

    -- Verify final count
    DECLARE @FinalCount INT = (SELECT COUNT(*) FROM CertificationTypes);

    IF @FinalCount = 27
    BEGIN
        PRINT 'SUCCESS: Merge complete. Total types: ' + CAST(@FinalCount AS VARCHAR);
        COMMIT TRANSACTION;
    END
    ELSE
    BEGIN
        RAISERROR('Unexpected final count. Expected 27, rolling back.', 16, 1);
        ROLLBACK TRANSACTION;
    END

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO

-- Verify results
SELECT 'Production Custom (Preserved)' AS Category, Id, Name
FROM CertificationTypes
WHERE Id BETWEEN 6 AND 10
UNION ALL
SELECT 'New from Dev' AS Category, Id, Name
FROM CertificationTypes
WHERE Id BETWEEN 11 AND 27
ORDER BY Category DESC, Id;
```

---

## Post-Merge Verification

```sql
-- Total count (should be 27)
SELECT COUNT(*) AS TotalTypes FROM CertificationTypes;

-- By source
SELECT
    CASE
        WHEN Id <= 5 THEN 'Shared (Dev + Prod)'
        WHEN Id BETWEEN 6 AND 10 THEN 'Production Custom'
        WHEN Id BETWEEN 11 AND 27 THEN 'New from Dev'
    END AS Source,
    COUNT(*) AS Count
FROM CertificationTypes
GROUP BY
    CASE
        WHEN Id <= 5 THEN 'Shared (Dev + Prod)'
        WHEN Id BETWEEN 6 AND 10 THEN 'Production Custom'
        WHEN Id BETWEEN 11 AND 27 THEN 'New from Dev'
    END;

-- List all
SELECT Id, Name, Description, IsActive
FROM CertificationTypes
ORDER BY Id;
```

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Production types 6-10 in use by certificates | High | Check dependencies first |
| ID conflicts | Low | Using new IDs 11-27 |
| Application breaks | Low | Conservative merge keeps all existing data |
| Data loss | None | Nothing deleted |

---

## Approval Required

Before executing merge:

- [ ] Verify Production types 6-10 usage (run dependency queries)
- [ ] Backup Production database
- [ ] Confirm merge strategy with stakeholders
- [ ] Review final type count (27 acceptable vs. Dev's 22)
- [ ] Test application after merge

---

## Alternative: If Prod Types 6-10 Not Used

If dependency check shows NO certificates or custom fields use IDs 6-10:

**Option:** Delete Prod's 6-10, insert Dev's exact structure (22 types)

**SQL:**
```sql
DELETE FROM CertificationTypes WHERE Id BETWEEN 6 AND 10;
-- Then insert all 22 Dev types exactly as-is
```

**Result:** Exact Dev structure (22 types, IDs match)

**Risk:** Lower (nothing in use), but verify first

---

## Recommendation: Proceed with Option A

**Status:** Ready for dependency check
**Next Step:** Run pre-merge verification queries
**Expected Result:** 27 CertificationTypes (10 Prod + 17 new from Dev)
