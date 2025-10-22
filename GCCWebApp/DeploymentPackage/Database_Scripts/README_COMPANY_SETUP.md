# Company Data Seeding Guide

## Overview
This guide covers seeding company data for the GCC Company Management page at https://gccwebapp.com/Company/Index

## Files

### SQL Scripts
- **COMPANY_DATA_SEED.sql** - Complete company data seeding script
  - Updates existing companies with email and website information
  - Adds 30+ new companies with complete contact details
  - Safe to run multiple times (idempotent)
  - Includes sustainability-focused companies and major retailers

### CSV Templates
- **COMPANY_IMPORT_TEMPLATE.csv** - CSV template for importing companies via the UI
  - Format: Company Name, Email, URL
  - Can be used with the Import button on /Company/Index

## Quick Start

### Option 1: Run SQL Script (Recommended for Initial Setup)

```bash
# From SQL Server Management Studio (SSMS)
# 1. Connect to your database server
# 2. Open COMPANY_DATA_SEED.sql
# 3. Execute the script

# OR from command line
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -i "Database_Scripts\COMPANY_DATA_SEED.sql"

# For production
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password] -d gccwebap_certificates -i "Database_Scripts\COMPANY_DATA_SEED.sql"
```

### Option 2: Import via Web UI

1. Navigate to https://gccwebapp.com/Company/Index
2. Click the "Import" button
3. Select COMPANY_IMPORT_TEMPLATE.csv (or your custom CSV)
4. Companies will be imported automatically

### Option 3: Manual Entry via Web UI

1. Navigate to https://gccwebapp.com/Company/Index
2. Click "Add New Company" button
3. Fill in the inline form:
   - Company Name (required)
   - Email
   - URL (will auto-add https:// if missing)
4. Click the checkmark to save

## What Gets Seeded

The COMPANY_DATA_SEED.sql script adds/updates:

### Updated Companies (28 existing)
- ASSA ABLOY, 3M, GAF Materials, Shaw Industries, Mohawk Industries
- Armstrong, Owens Corning, CertainTeed, Johns Manville, Kingspan
- Hunter Douglas, Steelcase, Herman Miller, Knoll, Teknion
- Forbo, Tarkett, Milliken, USG, Lutron, Philips, Cree
- Acme, Globex, Stark, Wayne Technologies, EcoFlex, Green Products

### New Companies Added (30+)
- **Sustainability Leaders**: Interface, Patagonia, Seventh Generation, Method Products
- **Tech Companies**: Apple, Google, Microsoft, Dell, HP, Cisco, Intel, AMD, Tesla
- **Consumer Products**: Unilever, Johnson & Johnson, P&G, Nike
- **Retailers**: IKEA, Starbucks, Whole Foods, Target, Walmart, Home Depot, Lowe's, Costco, Best Buy, Wayfair, Amazon
- **Chemical/Materials**: BASF, DuPont

All companies include:
- ✓ Company Name
- ✓ Email address
- ✓ Website URL
- ✓ Location (City, State)
- ✓ Country (United States/Canada)
- ✓ Active status
- ✓ Contact information

## Database Schema

```sql
Companies Table Structure:
- Id (int, PK, Identity)
- Name (nvarchar(255), required)
- ContactInfo (nvarchar(255))
- Email (nvarchar(255))
- Website (nvarchar(500))
- Location (nvarchar(175))
- Country (nvarchar(100))
- IsActive (bit, default: true)
- IsDeleted (bit, default: false)
- CreatedAt (datetime2, default: GETDATE())
- UpdatedAt (datetime2, default: GETDATE())
```

## Verification

After running the script, verify the data:

```sql
-- Check total companies
SELECT COUNT(*) AS TotalCompanies FROM dbo.Companies;

-- Check companies with complete data
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN Email IS NOT NULL THEN 1 ELSE 0 END) AS WithEmail,
    SUM(CASE WHEN Website IS NOT NULL THEN 1 ELSE 0 END) AS WithWebsite
FROM dbo.Companies
WHERE IsActive = 1 AND IsDeleted = 0;

-- View sample companies
SELECT TOP 20 Name, Email, Website, Location, Country
FROM dbo.Companies
WHERE IsActive = 1 AND IsDeleted = 0
ORDER BY Name;
```

Expected Results:
- **Total Companies**: 58+ (28 existing + 30 new)
- **With Email**: 58+
- **With Website**: 58+
- **Active**: 58+

## CSV Import Format

When creating your own CSV files for import:

```csv
Company Name,Email,URL
Example Corp,contact@example.com,https://www.example.com
Another Company,info@another.com,https://www.another.com
```

Notes:
- First row is the header (required)
- Company Name is required
- Email and URL are optional but recommended
- URLs without protocol (http/https) will auto-add https://
- Empty lines are ignored

## Company Management Features

The /Company/Index page provides:

1. **Inline Editing**: Click edit icon to modify company details
2. **Add New**: Click "Add New Company" to add via inline form
3. **Import CSV**: Bulk import companies from CSV file
4. **Export CSV**: Export all companies to CSV file
5. **Search**: Real-time search across name, email, and URL
6. **Sorting**: Click column headers to sort (Name, Email, URL)
7. **Delete**: Remove companies (soft delete)

## Troubleshooting

### Script Errors

**Error: "Cannot insert duplicate key"**
- Solution: The script is idempotent - it checks for existing companies before inserting

**Error: "Invalid object name 'dbo.Companies'"**
- Solution: Run migrations first: `dotnet ef database update`

**Error: "String or binary data would be truncated"**
- Solution: Check field lengths in your data match schema limits:
  - Name: 255 chars
  - Email: 255 chars
  - Website: 500 chars
  - Location: 175 chars
  - Country: 100 chars

### CSV Import Issues

**Import button not working**
- Check that you're logged in as Admin or Analyst
- Ensure CSV format matches template exactly
- Check browser console (F12) for JavaScript errors

**Companies not appearing after import**
- Verify IsActive = 1 and IsDeleted = 0
- Refresh the page (Ctrl+F5)
- Check database directly with SQL query

## Production Deployment

For production deployment to AccuWebHosting:

```bash
# Connect to production database
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password] -d gccwebap_certificates

# Run the script
:r Database_Scripts\COMPANY_DATA_SEED.sql
GO

# Verify
SELECT COUNT(*) FROM Companies;
GO
```

## Related Files

- **Models/Company.cs** - Company entity model (GCCWebApp:33)
- **Controllers/CompanyController.cs** - Company management controller (GCCWebApp:13)
- **Views/Company/Index.cshtml** - Company management UI (GCCWebApp:1)
- **Services/ICompanyService.cs** - Company service interface
- **FULL_TEST_DATA_SEED.sql** - Complete test data including companies

## Support

For issues or questions:
- Check application logs: `GCCWebApp/logs/`
- Review database setup: `Database_Scripts/README_DATABASE_SETUP.md`
- See main documentation: `0_Documentation/docs/Requirements.md`
