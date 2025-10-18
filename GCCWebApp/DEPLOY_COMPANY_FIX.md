# Company/Index Page Fix - Deployment Instructions

## Issue Fixed
The Company/Index page was showing hardcoded fictional data instead of loading from the database.

## Changes Made
**File**: `Views/Company/Index.cshtml`

### Changes:
1. ✅ Removed hardcoded fictional company data (Acme, Globex, Stark, Wayne)
2. ✅ Added `loadCompanies()` function to fetch data from `/Company/GetCompanies` API endpoint
3. ✅ Updated initialization to call `loadCompanies()` on page load
4. ✅ Added console logging for debugging

## Deployment Steps

### Option 1: Quick Deploy to Production (Recommended)

```bash
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Build the application
dotnet build --configuration Release

# Deploy to AccuWebHosting
.\Scripts\deployment\deploy-accuweb-production.cmd
```

### Option 2: Test Locally First

```bash
# Build and run locally
dotnet run --urls "http://localhost:5101"

# Open browser to
http://localhost:5101/Company/Index

# You should see 67 real companies from the database
```

### Option 3: Manual Deploy

1. Build the application
2. Copy `Views/Company/Index.cshtml` to production server
3. Restart the application

## Seed Production Database

**IMPORTANT**: The data was seeded to the development database (ANUKI01).
You need to seed the production database as well:

```bash
cd Database_Scripts

# Seed production database
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password] -d gccwebap_certificates -i "COMPANY_DATA_SEED.sql"
```

Or use the batch script:
```bash
cd Database_Scripts
seed-companies.cmd
# Select option 2 for Production
```

## Verification

After deployment, verify the fix:

### 1. Check https://gccwebapp.com/Company/Index
- Should load companies from database
- Should show "Showing X of X companies" with actual count
- Should display real company names, emails, and URLs

### 2. Check Browser Console (F12)
Look for: `Loaded XX companies from database`

### 3. Test Functionality
- ✅ Search companies
- ✅ Sort by columns (Name, Email, URL)
- ✅ Add new company
- ✅ Edit existing company
- ✅ Delete company
- ✅ Export to CSV
- ✅ Import from CSV

## Database Verification

### Development Database (ANUKI01)
```bash
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -E -Q "SELECT COUNT(*) FROM Companies WHERE IsActive=1 AND IsDeleted=0"
```
Expected: 67 companies

### Production Database (AccuWebHosting)
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -d gccwebap_certificates -Q "SELECT COUNT(*) FROM Companies WHERE IsActive=1 AND IsDeleted=0"
```
Expected: 67 companies (after seeding)

## Troubleshooting

### Companies Still Not Loading

1. **Check browser console** (F12) for errors
2. **Verify API endpoint** - Navigate to: https://gccwebapp.com/Company/GetCompanies
   - Should return JSON with `success: true` and company data
3. **Check authentication** - Ensure you're logged in as Admin or Analyst
4. **Check database connection** - Verify production connection string in web.config

### API Returns Empty Data

```bash
# Check if production database has companies
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -d gccwebap_certificates -Q "SELECT Name FROM Companies WHERE IsActive=1 AND IsDeleted=0"
```

If no data, run the seed script on production database.

### Authorization Error (401/403)

- Company/Index requires `AnalystOrAdmin` policy
- Ensure you're logged in with proper role
- Check role assignments in production database

## Files Modified

1. ✅ `Views/Company/Index.cshtml` - Fixed to load from database
2. ✅ `Database_Scripts/COMPANY_DATA_SEED.sql` - Seeds real companies only
3. ✅ `Database_Scripts/COMPANY_IMPORT_TEMPLATE.csv` - Real company examples

## Expected Results

After successful deployment:
- **67 active real companies** displayed
- **0 fictional companies** in active view
- All companies have email and website
- Search, sort, CRUD operations work correctly
- Data persists across page refreshes

---

**Status**: Ready to Deploy
**Priority**: High
**Estimated Time**: 5-10 minutes
