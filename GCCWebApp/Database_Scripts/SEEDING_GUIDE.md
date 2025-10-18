# GCC Database Seeding Guide

Complete guide for seeding the GCC Database with test and production data.

## Quick Start

### Seed All Data (Development)
```bash
cd Database_Scripts
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P DevP@ss2024!SQL -i "FULL_TEST_DATA_SEED.sql"
```

### Seed Specific Entities

```bash
# Companies (67 real companies)
seed-companies.cmd
# Select: 1 (Development)

# Certificate Fields (30 dynamic fields)
seed-certificate-fields.cmd
# Select: 1 (Development)

# CSF Attributes (50 sustainability attributes)
seed-csf-attributes.cmd
# Select: 1 (Development)
```

---

## Available Seed Scripts

### 1. FULL_TEST_DATA_SEED.sql ⭐ **Main Script**
**What it seeds**: Complete test dataset for development
- ✅ 6 Product Categories
- ✅ 5 Certification Types
- ✅ 3 Partners
- ✅ 28 Companies (NOTE: Use COMPANY_DATA_SEED.sql for 67 real companies)
- ✅ 19 Product Types (hierarchical)
- ✅ 43 Products
- ✅ 4 Roles
- ✅ 6 Permissions
- ✅ 15 Test Users
- ✅ 175 Certificates
- ✅ 10 Tags
- ✅ 3 API Keys

**When to use**:
- Fresh database setup
- Development/testing environments
- Demo environments

**How to run**:
```bash
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P DevP@ss2024!SQL -i "FULL_TEST_DATA_SEED.sql"
```

---

### 2. COMPANY_DATA_SEED.sql 🏢 **Real Companies**
**What it seeds**: 67 real building materials and manufacturing companies

**Includes**:
- Building materials manufacturers
- Windows/doors companies
- Furniture companies
- Flooring companies
- Technology companies
- Major retailers

**When to use**:
- Production environment
- When you need real company data instead of test data
- After running FULL_TEST_DATA_SEED.sql (replaces the 28 test companies)

**How to run**:
```bash
cd Database_Scripts
seed-companies.cmd
# Select: 1 (Development) or 2 (Production)
```

**Features**:
- ✅ Idempotent (safe to run multiple times)
- ✅ Soft-deletes fictional companies
- ✅ All companies have email and website
- ✅ Industry-relevant companies only

---

### 3. CERTIFICATE_FIELDS_DATA_SEED.sql 📋 **NEW**
**What it seeds**: 30 dynamic certificate field definitions

**Field Categories**:
- Basic Information (4 fields)
- Company & Product (3 fields)
- Location (2 fields)
- Product Identifiers (5 fields)
- Certification Details (4 fields)
- Environmental Data (4 fields)
- Compliance & Standards (4 fields)
- Additional Information (4 fields)

**When to use**:
- After database migrations
- When Certificate Fields page is empty
- Production and development environments

**How to run**:
```bash
cd Database_Scripts
seed-certificate-fields.cmd
# Select: 1 (Development) or 2 (Production)
```

**Example Fields**:
- Certificate Number (required, text)
- Effective Date (required, date)
- ASIN (optional, text with validation)
- Carbon Footprint (optional, number)
- LEED Eligible (optional, boolean)

---

### 4. CSF_ATTRIBUTES_DATA_SEED.sql 🌱 **NEW**
**What it seeds**: 50 Certified Sustainability Facts (CSF) attributes

**Attribute Categories**:
- Environmental Impact (5)
- Material Content (5)
- Chemical Safety (5)
- Energy & Efficiency (5)
- Product Lifecycle (5)
- Social Responsibility (5)
- Certification & Standards (8)
- Packaging (4)
- Transportation & Logistics (3)
- Indoor Environmental Quality (5)

**When to use**:
- After database migrations
- When CSF Attributes page is empty
- Production and development environments

**How to run**:
```bash
cd Database_Scripts
seed-csf-attributes.cmd
# Select: 1 (Development) or 2 (Production)
```

**Example Attributes**:
- Carbon Footprint
- Recycled Content Percentage
- VOC Emissions Level
- Energy Efficiency Rating
- FSC Certified
- Cradle to Cradle Level

---

### 5. DASHBOARD_DATA_SEED.sql 📊
**What it seeds**: Dashboard-specific metrics and sample data

**When to use**: When setting up dashboard functionality

---

### 6. PRODUCTION_ESSENTIAL_SEED.sql 🔐
**What it seeds**: Minimal production-ready data
- Roles and permissions
- Essential configuration
- No test users or certificates

**When to use**:
- Production environment initial setup
- When you want minimal seed data

---

## Database Configuration

### Development Database
```
Server: ANUKI01
Database: GCCDatabase_Dev
Authentication: SQL Authentication
Username: GCC_DevUser
Password: DevP@ss2024!SQL
```

### Production Database
```
Server: 68.71.129.120,1533
Database: gccwebap_certificates
Authentication: SQL Authentication
Username: GCC_adminxyz123
Password: [Secure password from vault]
```

---

## Seeding Strategy

### Fresh Database Setup
```bash
# 1. Apply migrations
cd GCCWebApp
dotnet ef database update

# 2. Seed all test data
cd Database_Scripts
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P DevP@ss2024!SQL -i "FULL_TEST_DATA_SEED.sql"

# 3. Replace test companies with real companies
seed-companies.cmd
# Select: 1 (Development)

# 4. Add certificate fields
seed-certificate-fields.cmd
# Select: 1 (Development)

# 5. Add CSF attributes
seed-csf-attributes.cmd
# Select: 1 (Development)
```

### Production Database Setup
```bash
# 1. Seed minimal production data
sqlcmd -S 68.71.129.120,1533 -d gccwebap_certificates -U GCC_adminxyz123 -P [password] -i "PRODUCTION_ESSENTIAL_SEED.sql"

# 2. Add real companies
seed-companies.cmd
# Select: 2 (Production)

# 3. Add certificate fields
seed-certificate-fields.cmd
# Select: 2 (Production)

# 4. Add CSF attributes
seed-csf-attributes.cmd
# Select: 2 (Production)
```

### Update Existing Database
```bash
# Just run the specific seed script you need
# All scripts are idempotent (safe to run multiple times)

seed-companies.cmd              # Updates/adds companies
seed-certificate-fields.cmd     # Adds certificate field definitions
seed-csf-attributes.cmd         # Adds CSF attributes
```

---

## Verification

### Check Seeded Data
```sql
-- Count all entities
SELECT 'ProductCategories' AS Entity, COUNT(*) AS Count FROM ProductCategories
UNION ALL SELECT 'CertificationTypes', COUNT(*) FROM CertificationTypes
UNION ALL SELECT 'Partners', COUNT(*) FROM Partners
UNION ALL SELECT 'Companies', COUNT(*) FROM Companies WHERE IsActive=1 AND IsDeleted=0
UNION ALL SELECT 'ProductTypes', COUNT(*) FROM ProductTypes
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates
UNION ALL SELECT 'Tags', COUNT(*) FROM Tags
UNION ALL SELECT 'Roles', COUNT(*) FROM Roles
UNION ALL SELECT 'Users', COUNT(*) FROM Users
UNION ALL SELECT 'CertificateFields', COUNT(*) FROM CertificateFields
UNION ALL SELECT 'CSFAttributes', COUNT(*) FROM CSFAttributes
ORDER BY Entity;
```

### Expected Counts (After Full Seeding)
| Entity | Count |
|--------|-------|
| Product Categories | 6 |
| Certification Types | 5 |
| Partners | 3 |
| Companies | 67 (after COMPANY_DATA_SEED) |
| Product Types | 19 |
| Products | 43 |
| Roles | 4 |
| Permissions | 6 |
| Users | 15 |
| Certificates | 175 |
| Tags | 10 |
| API Keys | 3 |
| Certificate Fields | 30 ✨ NEW |
| CSF Attributes | 50 ✨ NEW |

---

## Test User Credentials

After running FULL_TEST_DATA_SEED.sql:

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@gcc.com | TestPass123! |
| Analyst | analyst@gcc.com | TestPass123! |
| External | partner@amazon.com | TestPass123! |

---

## Troubleshooting

### Script Fails with "Table does not exist"
**Solution**: Run database migrations first
```bash
cd GCCWebApp
dotnet ef database update
```

### Script Fails with "Identity column" error
**Solution**: This has been fixed in all scripts. If you see this error, you may have an old version of the script.

### Script Fails with Foreign Key constraint
**Solution**: The COMPANY_DATA_SEED.sql script now uses soft-delete instead of hard delete. Update to the latest version.

### Companies not showing in UI
**Solution**:
1. Check `IsActive=1` and `IsDeleted=0` in database
2. Verify the view loads from `/Company/GetCompanies` API endpoint
3. Check browser console (F12) for JavaScript errors

---

## File Structure

```
Database_Scripts/
├── FULL_TEST_DATA_SEED.sql          # Main comprehensive seed script
├── COMPANY_DATA_SEED.sql            # 67 real companies
├── CERTIFICATE_FIELDS_DATA_SEED.sql # 30 dynamic certificate fields ✨ NEW
├── CSF_ATTRIBUTES_DATA_SEED.sql     # 50 CSF sustainability attributes ✨ NEW
├── DASHBOARD_DATA_SEED.sql          # Dashboard metrics
├── PRODUCTION_ESSENTIAL_SEED.sql    # Minimal production data
├── seed-companies.cmd               # Batch script for companies
├── seed-certificate-fields.cmd      # Batch script for cert fields ✨ NEW
├── seed-csf-attributes.cmd          # Batch script for CSF attributes ✨ NEW
├── apply-migrations.cmd             # Apply EF Core migrations
├── SEEDING_GUIDE.md                 # This file
└── README_DATABASE_SETUP.md         # General database setup guide
```

---

## Recent Updates

### October 17, 2025
- ✅ Created CERTIFICATE_FIELDS_DATA_SEED.sql (30 fields)
- ✅ Created CSF_ATTRIBUTES_DATA_SEED.sql (50 attributes)
- ✅ Created batch scripts for new seed files
- ✅ Audited all views to ensure database loading
- ✅ Verified no hardcoded data in production views
- ✅ Updated COMPANY_DATA_SEED.sql to 67 real companies

---

## Support

For issues or questions:
- Database Schema: `Models/*.cs`
- Data Models: `Models/AdminModels.cs`
- API Endpoints: `Controllers/AdminController.cs`, `Controllers/CompanyController.cs`
- Migration History: `dotnet ef migrations list`

---

**Last Updated**: October 17, 2025
**Maintained By**: GCC Development Team
