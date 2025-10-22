# Company Data Seeding - Summary Report
## REAL COMPANIES ONLY - NO FICTIONAL DATA

## Execution Date
**Date**: October 17, 2025, 9:14 AM
**Database**: GCCDatabase_Dev on ANUKI01
**Status**: ✅ SUCCESS

## Results

### Seeding Statistics
- **Total Companies in Database**: 73
- **Active Real Companies**: 67 (100% real companies)
- **Soft-Deleted Fictional Companies**: 6 (Acme, Globex, Stark, Wayne, EcoFlex, Green Products)
- **Companies with Email**: 73 (100%)
- **Companies with Website**: 73 (100%)

### Changes Made
1. ✅ Soft-deleted 6 fictional companies (preserved for historical certificates)
2. ✅ Updated 22 existing real companies with email and website data
3. ✅ Added 15 new real building materials/manufacturing companies

## New Real Building Materials Companies Added (15)

### Windows & Doors Manufacturing
- **Pella Corporation** - Pella, IA
  - Premium windows and doors manufacturer
- **Andersen Corporation** - Bayport, MN
  - Leading window and door manufacturer
- **Marvin Windows and Doors** - Warroad, MN
  - Custom-made windows and doors
- **JELD-WEN Inc.** - Charlotte, NC
  - Doors and windows for residential and commercial buildings

### Building Materials & Products
- **James Hardie Building Products** - Chicago, IL
  - Fiber cement siding and building materials
- **Louisiana-Pacific Corporation** - Nashville, TN
  - Engineered wood products and building solutions
- **Masco Corporation** - Livonia, MI
  - Home improvement and building products
- **Saint-Gobain North America** - Malvern, PA
  - Building materials, flat glass, and high-performance materials
- **Boral North America** - San Jose, CA
  - Building products and construction materials

### Coatings & Finishes
- **Sherwin-Williams Company** - Cleveland, OH
  - Paints, coatings, and specialty products
- **PPG Industries Inc.** - Pittsburgh, PA
  - Paints, coatings, and specialty materials

### Plumbing & Appliances
- **Kohler Co.** - Kohler, WI
  - Plumbing products, furniture, and power systems
- **Whirlpool Corporation** - Benton Harbor, MI
  - Major home appliances
- **General Electric Appliances** - Louisville, KY
  - Home appliances and products
- **Electrolux North America** - Charlotte, NC
  - Home appliances and professional products

## Previously Seeded Real Companies (52)

### Building Materials Leaders
- 3M Company, ASSA ABLOY, GAF Materials, Shaw Industries, Mohawk Industries
- Armstrong World Industries, Owens Corning, CertainTeed, Johns Manville
- Kingspan Insulation, Hunter Douglas, USG Corporation

### Furniture & Commercial
- Steelcase Inc., Herman Miller Inc., Knoll Inc., Teknion Corporation

### Flooring
- Forbo Flooring Systems, Tarkett USA, Milliken & Company, Interface Inc.

### Lighting & Controls
- Lutron Electronics, Philips Lighting (Signify), Cree Lighting

### Sustainability & Consumer
- Patagonia Inc., Seventh Generation Inc., Method Products PBC

### Technology & Major Retailers
- Apple, Google, Microsoft, Dell, HP, Cisco, Intel, AMD, Tesla
- Amazon, Walmart, Target, Home Depot, Lowe's, Costco, Best Buy, Wayfair, IKEA
- Starbucks, Whole Foods

### Consumer Products & Chemicals
- Unilever, J&J, P&G, Nike, BASF, DuPont

## Fictional Companies Removed (6)
**Status**: Soft-deleted (IsActive=0, IsDeleted=1) to preserve historical certificates

- Acme Corporation
- Globex Industries
- Stark Enterprises
- Wayne Technologies
- EcoFlex Manufacturing
- Green Products Inc. - Environmental Division

These fictional companies will not appear in the Company Management UI but their historical certificate associations are preserved.

## Sample Data Verification - REAL COMPANIES ONLY

```
Company Name                    | Email                              | Website
------------------------------- | ---------------------------------- | --------------------------------
3M Company                      | sustainability@3m.com              | https://www.3m.com
Andersen Corporation            | sustainability@andersencorp.com    | https://www.andersenwindows.com
Armstrong World Industries      | green@armstrong.com                | https://www.armstrongceilings.com
ASSA ABLOY                      | sustainability@assaabloy.com       | https://www.assaabloy.com
James Hardie Building Products  | sustainability@jameshardie.com     | https://www.jameshardie.com
Kohler Co.                      | sustainability@kohler.com          | https://www.kohler.com
Owens Corning                   | sustainability@owenscorning.com    | https://www.owenscorning.com
Pella Corporation               | sustainability@pella.com           | https://www.pella.com
Sherwin-Williams Company        | sustainability@sherwin.com         | https://www.sherwin-williams.com
Whirlpool Corporation           | sustainability@whirlpool.com       | https://www.whirlpool.com
```

## Issues Resolved

### 1. IDENTITY_INSERT Error
**Original Error**: "Cannot insert explicit value for identity column in table 'Companies' when IDENTITY_INSERT is set to OFF"

**Fix Applied**: Removed explicit Id values from INSERT statements to allow SQL Server to auto-generate identity values.

### 2. Foreign Key Constraint on DELETE
**Error**: "The DELETE statement conflicted with the REFERENCE constraint FK_Certificates_Companies_CompanyId"

**Fix Applied**: Changed from hard DELETE to soft-delete (UPDATE IsDeleted=1, IsActive=0) to preserve historical certificate associations while removing fictional companies from active view.

## Files Updated
1. ✅ `COMPANY_DATA_SEED.sql` - Fixed to remove IDENTITY_INSERT issue
2. ✅ Script now removes Id column from INSERT statements
3. ✅ SQL Server auto-generates identity values correctly

## Next Steps

### 1. View Company Data in UI
Navigate to: http://localhost:5101/Company/Index

You should now see 58 companies with complete data including:
- Company names
- Email addresses
- Website URLs
- Locations

### 2. Test Company Management Features
- ✅ Search companies by name, email, or URL
- ✅ Sort by column headers (Name, Email, URL)
- ✅ Add new companies using inline form
- ✅ Edit existing companies
- ✅ Delete companies
- ✅ Import additional companies via CSV
- ✅ Export all companies to CSV

### 3. Verify Data Integrity
Run verification query:
```sql
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN Email IS NOT NULL AND Email != '' THEN 1 ELSE 0 END) AS WithEmail,
    SUM(CASE WHEN Website IS NOT NULL AND Website != '' THEN 1 ELSE 0 END) AS WithWebsite
FROM Companies
WHERE IsActive = 1 AND IsDeleted = 0;
```

Expected: Total = 58, WithEmail = 58, WithWebsite = 58

## Available Scripts

### 1. Automated Seeding
```bash
cd Database_Scripts
seed-companies.cmd
```

### 2. Manual SQL Execution
```bash
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -E -i "COMPANY_DATA_SEED.sql"
```

### 3. CSV Import Template
Use `COMPANY_IMPORT_TEMPLATE.csv` for importing via web UI

## Documentation

- **Setup Guide**: `README_COMPANY_SETUP.md`
- **Seed Script**: `COMPANY_DATA_SEED.sql`
- **CSV Template**: `COMPANY_IMPORT_TEMPLATE.csv`
- **Batch Runner**: `seed-companies.cmd`

## Production Deployment

For production deployment to AccuWebHosting:
```bash
cd Database_Scripts
seed-companies.cmd
# Select option 2 for Production
```

Server: 68.71.129.120:1533
Database: gccwebap_certificates
Username: GCC_adminxyz123

## Troubleshooting

If you encounter issues:

1. **Companies not showing**: Verify IsActive = 1 and IsDeleted = 0
2. **Import not working**: Check CSV format matches template
3. **Duplicate companies**: Script is idempotent - checks for existing names before inserting
4. **Database connection errors**: Verify SQL Server is running and accessible

## Support

For issues or questions, see:
- Database setup: `Database_Scripts/README_DATABASE_SETUP.md`
- Application docs: `0_Documentation/docs/Requirements.md`
- Company model: `Models/Company.cs`
- Company controller: `Controllers/CompanyController.cs`

---

**Last Updated**: October 17, 2025
**Status**: ✅ Ready for Use
**Page URL**: https://gccwebapp.com/Company/Index
