# GCC Production Database - Setup & Validation Guide

**Server:** 68.71.129.120,1533
**Database:** gccwebap_certificates
**User:** GCC_adminxyz123
**Environment:** Production (AccuWebHosting)

---

## 📋 Table of Contents

1. [Quick Start](#quick-start)
2. [Database Seeding](#database-seeding)
3. [Validation Scripts](#validation-scripts)
4. [Test Credentials](#test-credentials)
5. [Troubleshooting](#troubleshooting)

---

## 🚀 Quick Start

### Option 1: Automatic Seeding via Application Startup (DEVELOPMENT ONLY)

The application automatically seeds data in Development mode:

```bash
# Set environment to Development
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Run application - seeds automatically
dotnet run

# Check console output for:
# "✅ Development database seeded successfully"
```

⚠️ **WARNING:** Never use this in Production! It will seed test data.

### Option 2: Manual Seeding via SeedData Class

For controlled seeding in any environment:

```bash
# Connect to SQL Server
sqlcmd -S 68.71.129.120,1533 -d gccwebap_certificates -U GCC_adminxyz123 -P Q3Ez4elgE_47KNi
```

Then use the validation scripts below to verify.

---

## 🌱 Database Seeding

### What Gets Seeded

The `SeedData.InitializeAsync()` method seeds:

- **6** Product Categories (Building Materials, Electronics, Paper Products, etc.)
- **5** Certification Types (CEF, CSF, RC, ES, MIR)
- **3** Partners (Amazon, Wayfair, Other)
- **28** Companies (realistic manufacturers)
- **8** Product Types with 10+ subcategories
- **35+** Products
- **175** Certificates (25 primary + 150 generated for testing)
- **25** Tags (external & internal)
- **4** Roles (Admin, Analyst, External, Public)
- **6** Permissions
- **14** Test Users with proper role assignments
- **5** API Keys

### Seeding via C# Code

The seeding happens automatically when you start the application in Development mode. See `Program.cs:620-622`:

```csharp
if (app.Environment.IsDevelopment())
{
    await SeedData.InitializeAsync(context);
    logger.LogInformation("✅ Development database seeded successfully");
}
```

### Production Migration-Only Mode

In Production, the application only applies migrations:

```csharp
else if (app.Environment.IsProduction())
{
    await context.Database.MigrateAsync();
    logger.LogInformation("✅ Production database migrations applied successfully");
}
```

---

## ✅ Validation Scripts

### 1. Basic Table and Row Counts

```sql
-- ============================================
-- COMPREHENSIVE DATABASE VALIDATION
-- ============================================

-- 1. List all tables
SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- 2. Row count for all major tables
SELECT
    'Certificates' AS TableName,
    COUNT(*) AS RowCount,
    'Should have 175+ records' AS Note
FROM Certificates
UNION ALL
SELECT 'Users', COUNT(*), 'Should have 14+ test users' FROM Users
UNION ALL
SELECT 'Companies', COUNT(*), 'Should have 28+ companies' FROM Companies
UNION ALL
SELECT 'Products', COUNT(*), 'Should have 35+ products' FROM Products
UNION ALL
SELECT 'ProductCategories', COUNT(*), 'Should have 6 categories' FROM ProductCategories
UNION ALL
SELECT 'CertificationTypes', COUNT(*), 'Should have 5 types' FROM CertificationTypes
UNION ALL
SELECT 'Partners', COUNT(*), 'Should have 3 partners' FROM Partners
UNION ALL
SELECT 'Roles', COUNT(*), 'Should have 4 roles' FROM Roles
UNION ALL
SELECT 'Permissions', COUNT(*), 'Should have 6 permissions' FROM Permissions
UNION ALL
SELECT 'Tags', COUNT(*), 'Should have 25 tags' FROM Tags
UNION ALL
SELECT 'ApiKeys', COUNT(*), 'Should have 5 API keys' FROM ApiKeys
ORDER BY TableName;
```

### 2. Verify Migration History

```sql
-- Verify all migrations applied
SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId;

-- Should show these migrations:
-- 20250808131552_InitialCreate
-- 20250808134211_UpdatedDatabaseSchema
-- 20250808143700_InitialMigration
-- 20250808160522_AdminEnhancements
-- 20250810151650_UpdateETLConfiguration
-- 20250811142726_AddProductIdentifiers
-- 20250813031456_FixModelIssues
-- 20250827043046_AddPerformanceIndexes
-- 20250827182202_AddPartnerFieldToCertificate
-- 20250909010923_AddPartnerEntity
-- 20250918212035_AddCertificateProductIds
-- 20251006162626_AddCertificateImagePath
-- 20251014002347_FixCompanyNameIndexForSqlServer   <-- SQL Server fix
```

### 3. Verify Users and Roles

```sql
-- Check all test users
SELECT
    u.Id,
    u.Username,
    u.Email,
    u.IsActive,
    u.IsStaff,
    u.IsSuperuser,
    STRING_AGG(r.Name, ', ') AS Roles
FROM Users u
LEFT JOIN RoleUser ru ON u.Id = ru.UsersId
LEFT JOIN Roles r ON ru.RolesId = r.Id
GROUP BY u.Id, u.Username, u.Email, u.IsActive, u.IsStaff, u.IsSuperuser
ORDER BY u.Username;

-- Verify specific admin user
SELECT Username, Email, IsActive, IsSuperuser
FROM Users
WHERE Email = 'admin@gcc.com';
-- Should return 1 row: admin / admin@gcc.com / True / True
```

### 4. Verify Certificates

```sql
-- Certificate status breakdown
SELECT
    Status,
    COUNT(*) AS Count,
    MIN(EffectiveDate) AS EarliestEffective,
    MAX(ExpirationDate) AS LatestExpiration
FROM Certificates
GROUP BY Status
ORDER BY Status;

-- Sample certificates
SELECT TOP 10
    Number,
    Status,
    EffectiveDate,
    ExpirationDate,
    Country,
    Asin,
    Upc
FROM Certificates
ORDER BY CreatedAt DESC;
```

### 5. Verify Product Categories and Types

```sql
-- Product categories
SELECT
    pc.Name AS Category,
    COUNT(DISTINCT pt.Id) AS ProductTypes,
    COUNT(DISTINCT p.Id) AS Products
FROM ProductCategories pc
LEFT JOIN ProductTypes pt ON pc.Id = pt.CategoryId
LEFT JOIN Products p ON pc.Id = p.CategoryId
GROUP BY pc.Id, pc.Name
ORDER BY pc.SortOrder;
```

### 6. Verify Partners

```sql
-- Partners
SELECT Id, Name, Website, IsActive, SortOrder
FROM Partners
ORDER BY SortOrder;

-- Should show:
-- Amazon, Wayfair, Other
```

### 7. Check Indexes (SQL Server Compatibility)

```sql
-- Verify all unique indexes have proper column types
SELECT
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    i.name AS IndexName,
    i.is_unique AS IsUnique
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE i.is_unique = 1
  AND t.is_ms_shipped = 0
  AND ty.name LIKE 'nvarchar%'
ORDER BY t.name, c.name;

-- All nvarchar columns with unique indexes should have explicit lengths (not MAX)
```

### 8. Verify Data Integrity

```sql
-- Check for orphaned certificates (should be 0)
SELECT COUNT(*) AS OrphanedCertificates,
       'Should be 0' AS Note
FROM Certificates c
WHERE NOT EXISTS (SELECT 1 FROM Companies co WHERE co.Id = c.CompanyId)
   OR NOT EXISTS (SELECT 1 FROM Products p WHERE p.Id = c.ProductId);

-- Check for certificates with future issue dates (should be 0)
SELECT COUNT(*) AS FutureCertificates,
       'Should be 0' AS Note
FROM Certificates
WHERE EffectiveDate > GETDATE();

-- Check for invalid expiration dates (should be 0)
SELECT COUNT(*) AS InvalidExpirations,
       'Should be 0' AS Note
FROM Certificates
WHERE ExpirationDate <= EffectiveDate;
```

### 9. API Keys Verification

```sql
-- Verify API keys
SELECT
    Name,
    IsActive,
    Environment,
    CreatedBy,
    ExpiresAt,
    RateLimitPerHour
FROM ApiKeys
ORDER BY CreatedAt DESC;

-- Should show 5 test API keys with different environments
```

### 10. Database Size and Performance

```sql
-- Database size
SELECT
    DB_NAME() AS DatabaseName,
    SUM(CAST(size AS BIGINT) * 8) / 1024 AS SizeMB,
    'SQL Server max size varies by plan' AS Note
FROM sys.database_files;

-- Index fragmentation check
SELECT
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent AS Fragmentation
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 30
  AND ips.page_count > 100
ORDER BY ips.avg_fragmentation_in_percent DESC;
```

---

## 🔐 Test Credentials

### Admin Users

```
Username: admin
Email: admin@gcc.com
Password: TestPass123!
Role: Admin
Capabilities: Full system access, user management, all CRUD operations
```

```
Username: robert.taylor
Email: robert.taylor@gcc.com
Password: TestPass123!
Role: Admin
Capabilities: Full system access
```

### Analyst Users

```
Username: analyst
Email: analyst@gcc.com
Password: TestPass123!
Role: Analyst
Capabilities: Certificate CRUD (except delete), read access to all
```

```
Username: sarah.johnson
Email: sarah.johnson@gcc.com
Password: TestPass123!
Role: Analyst
```

```
Username: mike.chen
Email: mike.chen@gcc.com
Password: TestPass123!
Role: Analyst
```

```
Username: jennifer.lee
Email: jennifer.lee@gcc.com
Password: TestPass123!
Role: Analyst
```

### External Users

```
Username: external
Email: external@company.com
Password: TestPass123!
Role: External
Capabilities: View certificates and reports only
```

```
Username: lisa.rodriguez
Email: lisa.rodriguez@assaabloy.com
Password: TestPass123!
Role: External
```

```
Username: david.kim
Email: david.kim@3m.com
Password: TestPass123!
Role: External
```

```
Username: emma.wilson
Email: emma.wilson@interface.com
Password: TestPass123!
Role: External
```

### Public Users

```
Username: public.user
Email: public@example.com
Password: TestPass123!
Role: Public
Capabilities: View public certificates only
```

### API Keys

```
Name: Integration Test Key
Key: gcc_test_key_1234567890abcdef
Environment: Development
Scopes: read,write,admin
```

```
Name: Partner API Access
Key: gcc_partner_key_abcdef1234567890
Environment: Production
Scopes: read
```

---

## 🔍 Troubleshooting

### Issue: "Column 'Name' is invalid for use as key column in an index"

**Cause:** SQL Server cannot create unique indexes on nvarchar(MAX) columns
**Fix:** Already resolved in migration `20251014002347_FixCompanyNameIndexForSqlServer`
**Verify:**
```sql
SELECT name, max_length
FROM sys.columns
WHERE object_id = OBJECT_ID('Companies') AND name = 'Name';
-- Should show: Name | 510 (255 * 2 for Unicode)
```

### Issue: No data in tables after migration

**Cause:** Production mode doesn't seed test data automatically
**Solution:** Either:
1. Temporarily set `ASPNETCORE_ENVIRONMENT=Development` and run app once
2. Manually insert essential data via SQL scripts
3. Import from backup/export

### Issue: Connection timeout

**Cause:** Firewall or network issues
**Test:**
```bash
# Test connectivity
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4elgE_47KNi -Q "SELECT @@VERSION"
```

### Issue: Migration errors

**Check migration history:**
```sql
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
```

**Manually run missing migrations:**
```bash
dotnet ef database update --connection "Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4elgE_47KNi;Encrypt=True;TrustServerCertificate=True"
```

---

## 📊 Expected Data Summary

After successful seeding, you should have:

| Entity | Count | Note |
|--------|-------|------|
| ProductCategories | 6 | Building Materials, Electronics, etc. |
| CertificationTypes | 5 | CEF, CSF, RC, ES, MIR |
| Partners | 3 | Amazon, Wayfair, Other |
| Companies | 28+ | Realistic manufacturers |
| ProductTypes | 8+ | With subcategories |
| Products | 35+ | Various certified products |
| Certificates | 175+ | 25 primary + 150 generated |
| Tags | 25 | External and internal tags |
| Roles | 4 | Admin, Analyst, External, Public |
| Permissions | 6 | Certificate and user permissions |
| Users | 14+ | Test users across all roles |
| ApiKeys | 5 | Test API keys |

---

## ✅ Final Verification Checklist

- [ ] All 13 migrations applied (check `__EFMigrationsHistory`)
- [ ] All tables exist (28+ tables)
- [ ] Row counts match expected values
- [ ] Admin user (`admin@gcc.com`) exists and can log in
- [ ] All 4 roles exist (Admin, Analyst, External, Public)
- [ ] Sample certificates visible (175+)
- [ ] Partners exist (Amazon, Wayfair, Other)
- [ ] All unique indexes have proper column lengths (not MAX)
- [ ] No orphaned records (foreign key integrity)
- [ ] Database size is reasonable (should be under 100MB for test data)
- [ ] Can connect from application successfully

---

## 🚀 Deployment Checklist

### Before Deployment

1. **Backup Production Database:**
   ```sql
   BACKUP DATABASE gccwebap_certificates
   TO DISK = 'C:\Backups\gccwebap_certificates_before_migration.bak';
   ```

2. **Test Connection:**
   ```bash
   sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4elgE_47KNi -Q "SELECT DB_NAME()"
   ```

3. **Run Validation Scripts Above**

### After Deployment

1. **Apply Migrations:**
   - Application automatically applies on startup in Production mode
   - Monitor stdout logs for: "✅ Production database migrations applied successfully"

2. **Verify Migration:**
   - Run all validation scripts above
   - Check application logs for errors
   - Test login with admin credentials
   - Verify certificate tile view loads

3. **Monitor Performance:**
   - Check database size
   - Monitor query performance
   - Review application logs

---

**Last Updated:** October 13, 2025
**SQL Server Compatibility:** Verified for AccuWebHosting SQL Server
**Migration Status:** Ready for Production
