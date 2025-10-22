# REAL COMPANIES ONLY - Data Seeding Complete

## Summary
All company data has been updated to include ONLY real, actual companies. No fictional data remains in the active dataset.

## Final Statistics
- **Total Companies**: 73
- **Active Real Companies**: 67 ✅
- **Soft-Deleted Fictional Companies**: 6 (preserved for historical certificates)
- **100% have email addresses**
- **100% have website URLs**

## What Was Changed

### Fictional Companies Removed
These 6 fictional companies were soft-deleted (IsActive=0, IsDeleted=1):
1. Acme Corporation
2. Globex Industries
3. Stark Enterprises
4. Wayne Technologies
5. EcoFlex Manufacturing
6. Green Products Inc. - Environmental Division

**Note**: Soft-delete preserves foreign key relationships with existing certificates while removing these companies from the active Company Management UI.

### Real Companies Added
15 new real building materials companies were added:
- **Windows/Doors**: Pella, Andersen, Marvin, JELD-WEN
- **Building Materials**: James Hardie, Louisiana-Pacific, Masco, Saint-Gobain, Boral
- **Coatings**: Sherwin-Williams, PPG Industries
- **Plumbing/Appliances**: Kohler, Whirlpool, GE Appliances, Electrolux

### Previously Seeded Real Companies (52)
All previously seeded companies are real and active:
- Building materials leaders (3M, ASSA ABLOY, GAF, Shaw, Mohawk, Armstrong, Owens Corning, etc.)
- Furniture manufacturers (Steelcase, Herman Miller, Knoll, Teknion)
- Flooring companies (Forbo, Tarkett, Milliken, Interface)
- Technology companies (Apple, Google, Microsoft, Dell, HP, Cisco, Intel, AMD, Tesla)
- Major retailers (Amazon, Walmart, Target, Home Depot, Lowe's, Costco, Best Buy, Wayfair, IKEA)
- Consumer products (Unilever, J&J, P&G, Nike)
- Chemicals/materials (BASF, DuPont)

## Verification Queries

### View Active Real Companies Only
```sql
SELECT Name, Email, Website, Location, Country
FROM Companies
WHERE IsActive = 1 AND IsDeleted = 0
ORDER BY Name;
```

### View Soft-Deleted Fictional Companies
```sql
SELECT Name, IsActive, IsDeleted
FROM Companies
WHERE IsDeleted = 1;
```

### Count by Status
```sql
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN IsActive = 1 AND IsDeleted = 0 THEN 1 ELSE 0 END) AS Active,
    SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS Deleted
FROM Companies;
```

## Access the Data

**Company Management Page**: http://localhost:5101/Company/Index

You will see 67 real companies with complete contact information. The 6 fictional companies will NOT appear in this view.

## Files Updated
1. ✅ COMPANY_DATA_SEED.sql - Soft-deletes fictional companies, seeds only real companies
2. ✅ COMPANY_IMPORT_TEMPLATE.csv - Contains only real company examples
3. ✅ COMPANY_SEEDING_SUMMARY.md - Updated with real companies information
4. ✅ REAL_COMPANIES_ONLY.md - This file

## Re-Running the Script

The script is idempotent and safe to run multiple times:

```bash
cd Database_Scripts
seed-companies.cmd
```

Or directly:
```bash
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -E -i "COMPANY_DATA_SEED.sql"
```

On each run:
- Fictional companies will remain soft-deleted
- Real companies will be updated with latest contact info
- New real companies will be added if they don't exist
- No duplicate entries will be created

## Industry Relevance

All 67 active companies are real companies from industries relevant to GCC certifications:
- Building materials and construction
- Windows, doors, and architectural products
- Flooring and carpeting
- Furniture and office systems
- Lighting and electrical systems
- Insulation and energy efficiency
- Coatings and finishes
- Plumbing and fixtures
- Major appliance manufacturers
- Sustainability-focused companies

These are companies that would realistically need environmental certifications like FSC, PEFC, SFI, LEED, Energy Star, etc.

---

**Status**: ✅ Complete - All Data is Real
**Date**: October 17, 2025
**Database**: GCCDatabase_Dev on ANUKI01
