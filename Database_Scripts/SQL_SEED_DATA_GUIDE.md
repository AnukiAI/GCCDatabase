# SQL Seed Data Migration Guide
**Version:** 2.0
**Last Updated:** October 17, 2025
**Purpose:** Maintain all seed data in SQL scripts for proper database migration management

## Overview

**CRITICAL REQUIREMENT:** All seed data MUST be maintained in SQL migration scripts, NOT in C# code.

This ensures:
- ✅ Consistent data across all environments
- ✅ Version-controlled seed data
- ✅ Repeatable deployments
- ✅ Easy rollback capability
- ✅ Database-first approach
- ✅ No hardcoded data in application code

---

## Seed Data Management Strategy

### 1. Reference Data (Always Seed)
**Location:** `REFERENCE_DATA_SEED.sql`
**When to Run:** Every environment setup, idempotent
**Contains:**
- Certification Types (FSC, SFI, PEFC, etc.)
- Product Categories
- System Roles (Admin, Analyst, External)
- Configuration settings

### 2. Development Test Data
**Location:** `FULL_TEST_DATA_SEED.sql`
**When to Run:** Development environment only
**Contains:**
- Sample companies
- Sample products
- Sample certificates
- Test user accounts

### 3. Production Essential Data
**Location:** `PRODUCTION_ESSENTIAL_SEED.sql`
**When to Run:** Production environment initialization
**Contains:**
- Production certification types
- Production user roles
- Initial admin accounts
- Essential configuration

---

## SQL Script Structure

### Idempotent Script Template

All seed scripts MUST be idempotent (safe to run multiple times):

```sql
-- ============================================
-- Script: REFERENCE_DATA_SEED.sql
-- Purpose: Seed reference data (idempotent)
-- Author: GCC Development Team
-- Date: 2025-10-17
-- Version: 2.0
-- ============================================

USE [GCCDatabase_Dev]; -- Or production database name
GO

-- Enable IDENTITY_INSERT if needed
SET IDENTITY_INSERT CertificationTypes ON;
GO

-- ============================================
-- 1. CERTIFICATION TYPES
-- ============================================
PRINT 'Seeding Certification Types...';

-- Use MERGE for idempotent insert/update
MERGE INTO CertificationTypes AS target
USING (
    VALUES
        (1, 'Certified Sustainability Facts', 'CSF certification standard', 1),
        (2, 'FSC Chain of Custody', 'Forest Stewardship Council certification', 1),
        (3, 'SFI Chain of Custody', 'Sustainable Forestry Initiative certification', 1),
        (4, 'PEFC Chain of Custody', 'Programme for Endorsement of Forest Certification', 1),
        (5, 'Recycled Content', 'Recycled material content certification', 1),
        (6, 'Material Ingredient Reporting', 'Material ingredient transparency certification', 1),
        (7, 'Green Building Standard', 'Green building compliance certification', 1)
) AS source (Id, Name, Description, IsActive)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        Description = source.Description,
        IsActive = source.IsActive,
        UpdatedAt = GETUTCDATE()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Name, Description, IsActive, CreatedAt)
    VALUES (source.Id, source.Name, source.Description, source.IsActive, GETUTCDATE());

PRINT '✅ Certification Types seeded successfully';
GO

SET IDENTITY_INSERT CertificationTypes OFF;
GO

-- ============================================
-- 2. PRODUCT CATEGORIES
-- ============================================
PRINT 'Seeding Product Categories...';

SET IDENTITY_INSERT ProductCategories ON;
GO

MERGE INTO ProductCategories AS target
USING (
    VALUES
        (1, 'Paper Products', 'Paper and paper-based products', 1),
        (2, 'Wood Products', 'Wood and lumber products', 1),
        (3, 'Packaging', 'Packaging materials and containers', 1),
        (4, 'Building Materials', 'Construction and building materials', 1),
        (5, 'Furniture', 'Furniture and furnishings', 1),
        (6, 'Textiles', 'Fabric and textile products', 1),
        (7, 'Flooring', 'Flooring materials and systems', 1)
) AS source (Id, Name, Description, IsActive)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        Description = source.Description,
        IsActive = source.IsActive,
        UpdatedAt = GETUTCDATE()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Name, Description, IsActive, CreatedAt)
    VALUES (source.Id, source.Name, source.Description, source.IsActive, GETUTCDATE());

PRINT '✅ Product Categories seeded successfully';
GO

SET IDENTITY_INSERT ProductCategories OFF;
GO

-- ============================================
-- 3. SYSTEM ROLES
-- ============================================
PRINT 'Seeding System Roles...';

SET IDENTITY_INSERT Roles ON;
GO

MERGE INTO Roles AS target
USING (
    VALUES
        (1, 'Admin', 'Full system access - manage users, certificates, and configuration'),
        (2, 'Analyst', 'Read/Write access to certificates - no admin functions'),
        (3, 'External', 'Read-only access to partner-specific certificates')
) AS source (Id, Name, Description)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        Description = source.Description
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Name, Description)
    VALUES (source.Id, source.Name, source.Description);

PRINT '✅ System Roles seeded successfully';
GO

SET IDENTITY_INSERT Roles OFF;
GO

-- ============================================
-- VERIFICATION QUERIES
-- ============================================
PRINT '============================================';
PRINT 'Verification Results:';
PRINT '============================================';

SELECT 'CertificationTypes' AS TableName, COUNT(*) AS RowCount FROM CertificationTypes WHERE IsActive = 1;
SELECT 'ProductCategories' AS TableName, COUNT(*) AS RowCount FROM ProductCategories WHERE IsActive = 1;
SELECT 'Roles' AS TableName, COUNT(*) AS RowCount FROM Roles;

PRINT '============================================';
PRINT '✅ Reference data seeding completed successfully';
PRINT '============================================';
GO
```

---

## Migration Script Execution Order

### Initial Setup (Fresh Database)

1. **Schema Creation**
   ```bash
   sqlcmd -S ANUKI01 -d master -i "00_CREATE_SCHEMA.sql"
   ```

2. **Apply EF Migrations**
   ```bash
   dotnet ef database update
   ```

3. **Seed Reference Data** (Required)
   ```bash
   sqlcmd -S ANUKI01 -d GCCDatabase_Dev -i "REFERENCE_DATA_SEED.sql"
   ```

4. **Seed Development Data** (Optional - Dev only)
   ```bash
   sqlcmd -S ANUKI01 -d GCCDatabase_Dev -i "FULL_TEST_DATA_SEED.sql"
   ```

### Production Deployment

1. **Backup Existing Database**
   ```sql
   BACKUP DATABASE [gccwebap_certificates]
   TO DISK = 'C:\Backups\gccwebap_certificates_before_migration.bak'
   WITH FORMAT, INIT, NAME = 'Pre-Migration Backup';
   ```

2. **Apply EF Migrations**
   ```bash
   dotnet ef database update --context ApplicationDbContext
   ```

3. **Seed Production Essential Data** (One-time)
   ```bash
   sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P {password} -d gccwebap_certificates -i "PRODUCTION_ESSENTIAL_SEED.sql"
   ```

4. **Verify Production Data**
   ```bash
   sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P {password} -d gccwebap_certificates -i "PRODUCTION_VALIDATION.sql"
   ```

---

## Available Seed Scripts

### 1. REFERENCE_DATA_SEED.sql
**Purpose:** Core reference data required for application to function
**When:** Every environment
**Idempotent:** Yes

**Contains:**
```sql
-- Certification Types (7 types)
-- Product Categories (7 categories)
-- System Roles (Admin, Analyst, External)
-- Default configuration settings
```

**Usage:**
```bash
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -i "REFERENCE_DATA_SEED.sql"
```

### 2. FULL_TEST_DATA_SEED.sql
**Purpose:** Development test data for local testing
**When:** Development environment only
**Idempotent:** No (drops and recreates data)

**Contains:**
```sql
-- 20+ Sample Companies
-- 50+ Sample Products
-- 100+ Sample Certificates
-- Test User Accounts
-- Sample Product IDs (ASINs, UPCs)
```

**Usage:**
```bash
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -i "FULL_TEST_DATA_SEED.sql"
```

### 3. PRODUCTION_ESSENTIAL_SEED.sql
**Purpose:** Minimal production startup data
**When:** Production initialization only
**Idempotent:** Yes

**Contains:**
```sql
-- Certification Types (production-verified)
-- System Roles
-- Initial admin user (password must be changed)
```

**Usage:**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P {password} -d gccwebap_certificates -i "PRODUCTION_ESSENTIAL_SEED.sql"
```

### 4. COMPANY_DATA_SEED.sql
**Purpose:** Real company data import
**When:** As needed
**Idempotent:** Yes (MERGE statements)

**Contains:**
```sql
-- Production company records
-- Company locations and contacts
```

**Usage:**
```bash
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -i "COMPANY_DATA_SEED.sql"
```

### 5. CERTIFICATE_FIELDS_DATA_SEED.sql
**Purpose:** Dynamic certificate field definitions
**When:** After schema changes
**Idempotent:** Yes

**Usage:**
```bash
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -i "CERTIFICATE_FIELDS_DATA_SEED.sql"
```

### 6. CSF_ATTRIBUTES_DATA_SEED.sql
**Purpose:** Certified Sustainability Facts attribute definitions
**When:** CSF feature configuration
**Idempotent:** Yes

**Usage:**
```bash
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -i "CSF_ATTRIBUTES_DATA_SEED.sql"
```

---

## Removing Hardcoded Data from C# Code

### ❌ WRONG: Hardcoded Data in C# Code

```csharp
// DON'T DO THIS!
public async Task<IActionResult> SeedDatabase()
{
    var certTypeNames = new[] {
        "FSC Chain of Custody",
        "SFI Chain of Custody",
        "PEFC Chain of Custody"
    };

    foreach (var typeName in certTypeNames)
    {
        _context.CertificationTypes.Add(new CertificationType {
            Name = typeName,
            IsActive = true
        });
    }

    await _context.SaveChangesAsync();
}
```

**Problems:**
- ❌ Data embedded in application code
- ❌ Not version controlled separately
- ❌ Hard to maintain
- ❌ Can't be run independently
- ❌ Not database-first
- ❌ Risk of accidental execution in production

### ✅ CORRECT: SQL Seed Scripts

```sql
-- DO THIS INSTEAD!
-- File: REFERENCE_DATA_SEED.sql

MERGE INTO CertificationTypes AS target
USING (
    VALUES
        (1, 'FSC Chain of Custody', 'Forest Stewardship Council', 1),
        (2, 'SFI Chain of Custody', 'Sustainable Forestry Initiative', 1),
        (3, 'PEFC Chain of Custody', 'Programme for Endorsement', 1)
) AS source (Id, Name, Description, IsActive)
ON target.Id = source.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Name, Description, IsActive, CreatedAt)
    VALUES (source.Id, source.Name, source.Description, source.IsActive, GETUTCDATE());
```

**Benefits:**
- ✅ SQL-first approach
- ✅ Version controlled
- ✅ Idempotent (safe to rerun)
- ✅ Easy to maintain
- ✅ Can be run independently
- ✅ Environment-specific

---

## C# Application Startup (CORRECT Approach)

### ✅ Minimal Seeding in Program.cs

```csharp
// Program.cs - Minimal startup seeding
public static async Task SeedDevelopmentData(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();
    var environment = services.GetRequiredService<IWebHostEnvironment>();

    if (!environment.IsDevelopment())
    {
        logger.LogInformation("Skipping data seeding - not in development environment");
        return;
    }

    // Only check if reference data exists
    var certTypeCount = await context.CertificationTypes.CountAsync();
    if (certTypeCount == 0)
    {
        logger.LogWarning("⚠️ No certification types found!");
        logger.LogWarning("Run SQL seed scripts: REFERENCE_DATA_SEED.sql");
        logger.LogWarning("Command: sqlcmd -S ANUKI01 -d GCCDatabase_Dev -i 'Database_Scripts/REFERENCE_DATA_SEED.sql'");
        return;
    }

    var roleCount = await context.Roles.CountAsync();
    if (roleCount == 0)
    {
        logger.LogWarning("⚠️ No roles found!");
        logger.LogWarning("Run SQL seed scripts: REFERENCE_DATA_SEED.sql");
        return;
    }

    logger.LogInformation("✅ Reference data verified - application ready");
}
```

**Key Points:**
- Application only CHECKS for data existence
- Application does NOT seed data
- Clear instructions logged if data missing
- Database administrators run SQL scripts manually
- Production never attempts auto-seeding

---

## PowerShell Automation Scripts

### apply-migrations.ps1

```powershell
# Location: Database_Scripts/apply-migrations.ps1
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('Development','Production','Staging')]
    [string]$Environment,

    [switch]$IncludeTestData
)

$ErrorActionPreference = "Stop"

# Load environment-specific settings
switch ($Environment) {
    'Development' {
        $Server = "ANUKI01"
        $Database = "GCCDatabase_Dev"
        $AuthMode = "Integrated"
    }
    'Production' {
        $Server = "68.71.129.120,1533"
        $Database = "gccwebap_certificates"
        $AuthMode = "SQL"
        $Username = "GCC_adminxyz123"
        # Password from secure source
        $Password = Read-Host "Enter production database password" -AsSecureString
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Migration - $Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Apply EF Migrations
Write-Host "`n1. Applying Entity Framework migrations..." -ForegroundColor Yellow
dotnet ef database update --context ApplicationDbContext
if ($LASTEXITCODE -ne 0) {
    throw "EF Migration failed"
}
Write-Host "✅ EF Migrations applied" -ForegroundColor Green

# 2. Apply Reference Data Seed
Write-Host "`n2. Seeding reference data..." -ForegroundColor Yellow
if ($AuthMode -eq "Integrated") {
    sqlcmd -S $Server -d $Database -i "REFERENCE_DATA_SEED.sql"
} else {
    sqlcmd -S $Server -U $Username -P $Password -d $Database -i "REFERENCE_DATA_SEED.sql"
}
if ($LASTEXITCODE -ne 0) {
    throw "Reference data seeding failed"
}
Write-Host "✅ Reference data seeded" -ForegroundColor Green

# 3. Apply Test Data (Development only)
if ($Environment -eq 'Development' -and $IncludeTestData) {
    Write-Host "`n3. Seeding test data..." -ForegroundColor Yellow
    sqlcmd -S $Server -d $Database -i "FULL_TEST_DATA_SEED.sql"
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Test data seeding failed (non-critical)"
    } else {
        Write-Host "✅ Test data seeded" -ForegroundColor Green
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "✅ Migration completed successfully" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
```

**Usage:**
```powershell
# Development with test data
.\apply-migrations.ps1 -Environment Development -IncludeTestData

# Production (reference data only)
.\apply-migrations.ps1 -Environment Production

# Staging
.\apply-migrations.ps1 -Environment Staging
```

---

## Best Practices

### 1. Version Control All SQL Scripts
```
Database_Scripts/
├── README_DATABASE_SETUP.md
├── 00_CREATE_SCHEMA.sql (v1.0)
├── REFERENCE_DATA_SEED.sql (v2.0)
├── FULL_TEST_DATA_SEED.sql (v2.0)
├── PRODUCTION_ESSENTIAL_SEED.sql (v1.5)
└── migrations/
    ├── 001_InitialSchema.sql
    ├── 002_AddProductIds.sql
    └── 003_AddPartnerField.sql
```

### 2. Document Each Script
```sql
-- ============================================
-- Script: REFERENCE_DATA_SEED.sql
-- Version: 2.0
-- Author: GCC Development Team
-- Date: 2025-10-17
-- Purpose: Seed essential reference data
-- Changelog:
--   v2.0 - Added Material Ingredient Reporting cert type
--   v1.5 - Added Green Building Standard
--   v1.0 - Initial version
-- ============================================
```

### 3. Always Use MERGE for Idempotence
```sql
-- GOOD: Idempotent with MERGE
MERGE INTO CertificationTypes AS target
USING (VALUES (1, 'FSC Chain of Custody', 'Description', 1)) AS source
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET Name = source.Name
WHEN NOT MATCHED THEN INSERT VALUES (...);

-- BAD: Not idempotent
INSERT INTO CertificationTypes (Name, Description)
VALUES ('FSC Chain of Custody', 'Description');
```

### 4. Include Verification Queries
```sql
-- Always verify after seeding
SELECT 'CertificationTypes' AS Table, COUNT(*) AS Count
FROM CertificationTypes WHERE IsActive = 1;
-- Expected: 7

SELECT 'Roles' AS Table, COUNT(*) AS Count FROM Roles;
-- Expected: 3
```

### 5. Never Auto-Seed in Production
```csharp
// Program.cs
if (_environment.IsProduction())
{
    // NEVER seed data automatically in production
    logger.LogInformation("Production mode - skipping auto-seed");
    return;
}
```

---

## Troubleshooting

### Issue: "Reference data not found"

**Solution:**
```bash
# Check if reference data exists
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -Q "SELECT COUNT(*) FROM CertificationTypes"

# If count is 0, run seed script
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -i "Database_Scripts/REFERENCE_DATA_SEED.sql"
```

### Issue: "Duplicate key error"

**Solution:**
Use MERGE statements instead of INSERT:
```sql
-- Instead of INSERT (can fail if data exists)
MERGE INTO table_name ...
```

### Issue: "Script execution fails"

**Checklist:**
1. Check database connection
2. Verify user permissions
3. Check for syntax errors
4. Verify database exists
5. Check for dependencies (run schema scripts first)

---

## Summary

**Golden Rules:**
1. ✅ ALL seed data in SQL scripts
2. ✅ NO hardcoded data in C# code
3. ✅ Scripts must be idempotent (MERGE)
4. ✅ Version control all SQL scripts
5. ✅ Environment-specific scripts (Dev/Prod)
6. ✅ Document script purpose and version
7. ✅ Verify data after seeding
8. ✅ Never auto-seed in production
9. ✅ Use PowerShell for automation
10. ✅ Test scripts before production use

---

**Related Documents:**
- [Database Schema Reference](DATABASE_SCHEMA_REFERENCE.md)
- [Data Requirements](../0_Documentation/DATA_REQUIREMENTS.md)
- [Hardcoded Data Audit](../0_Documentation/HARDCODED_DATA_AUDIT.md)

**Approved By:**
- [ ] Database Administrator
- [ ] Development Lead
- [ ] DevOps Lead

**Date:** ___________________
