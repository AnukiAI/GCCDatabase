# GCC Database Schema Reference

**Last Updated:** October 17, 2025
**Schema Version:** 20251015162223_InitialCreate_SqlServer_Final
**Database:** SQL Server 2019+

---

## Quick Reference - Key Tables

### Certificates Table
**Primary table for certificate data**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| Id | INT IDENTITY | No | - | Primary key |
| Number | NVARCHAR(7) | No | - | Certificate number (unique) |
| Status | NVARCHAR(50) | No | 'Active' | Active, Expired, etc. |
| **EffectiveDate** | DATETIME2 | No | - | **NOT IssueDate** |
| ExpirationDate | DATETIME2 | No | - | When cert expires |
| Location | NVARCHAR(175) | Yes | - | Location string |
| Country | NVARCHAR(100) | Yes | - | Country name |
| Asin | NVARCHAR(MAX) | Yes | - | Legacy ASIN field |
| Upc | NVARCHAR(MAX) | Yes | - | Legacy UPC field |
| Gtin | NVARCHAR(MAX) | Yes | - | Legacy GTIN field |
| Ean | NVARCHAR(MAX) | Yes | - | Legacy EAN field |
| CertificateFile | NVARCHAR(MAX) | Yes | - | File path |
| CertificateImagePath | NVARCHAR(500) | Yes | - | Image path |
| Partner | NVARCHAR(100) | Yes | - | Partner name |
| CompanyId | INT | No | - | FK to Companies |
| ProductId | INT | No | - | FK to Products |
| CertificationTypeId | INT | No | - | FK to CertificationTypes |
| CreatedBy | INT | Yes | - | FK to Users |
| UpdatedBy | INT | Yes | - | FK to Users |
| CreatedAt | DATETIME2 | No | GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME2 | No | GETDATE() | Update timestamp |

**Foreign Keys:**
- CompanyId → Companies.Id
- ProductId → Products.Id
- CertificationTypeId → CertificationTypes.Id
- CreatedBy → Users.Id
- UpdatedBy → Users.Id

---

### CertificateAggregates Table
**Dashboard statistics and aggregates**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| **Period** | NVARCHAR(450) | No | - | **Format: 'yyyy-MM' (e.g., '2025-10')** |
| **PeriodDate** | DATETIME2 | No | - | **Month start date** |
| TotalCertificates | INT | No | 0 | Total count |
| ActiveCertificates | INT | No | 0 | Active count |
| ExpiredCertificates | INT | No | 0 | Expired count |
| **ExpiringThisMonth** | INT | No | 0 | **NOT ExpiringSoonCertificates** |
| **CertificatesByType** | TEXT | No | - | **JSON array string** |
| **CertificatesByCountry** | TEXT | No | - | **JSON array string** |
| **CertificatesByCompany** | TEXT | No | - | **JSON array string** |
| AverageValidityPeriod | FLOAT | No | 0 | Avg days (EffectiveDate to ExpirationDate) |
| CreatedAt | DATETIME2 | No | - | Creation timestamp |

**Composite Primary Key:** (Period, PeriodDate)

**Note:** This table does NOT have:
- ~~AggregateDate~~ (use PeriodDate)
- ~~ExpiringSoonCertificates~~ (use ExpiringThisMonth)
- ~~NewCertificatesThisMonth~~
- ~~UpdatedAt~~

---

### CertificateProductIds Table
**Many-to-many Product IDs (ASINs, UPCs, etc.) per certificate**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| Id | INT IDENTITY | No | - | Primary key |
| CertificateId | INT | No | - | FK to Certificates |
| ProductIdType | NVARCHAR(10) | No | - | ASIN, UPC, GTIN, etc. |
| ProductIdValue | NVARCHAR(50) | No | - | The actual ID value |
| Status | NVARCHAR(20) | No | 'Active' | Status |
| Partner | NVARCHAR(50) | Yes | - | Partner name |
| ProductName | NVARCHAR(200) | Yes | - | Optional name |
| ProductSku | NVARCHAR(100) | Yes | - | Optional SKU |
| Description | NVARCHAR(500) | Yes | - | Description |
| IsActive | BIT | No | 1 | Active flag |
| IsPublic | BIT | No | 1 | Public visibility |
| DisplayOrder | INT | Yes | - | Sort order |
| CreatedBy | INT | Yes | - | FK to Users |
| CreatedAt | DATETIME2 | No | GETDATE() | Creation timestamp |
| UpdatedBy | INT | Yes | - | FK to Users |
| UpdatedAt | DATETIME2 | No | GETDATE() | Update timestamp |

**Foreign Keys:**
- CertificateId → Certificates.Id (CASCADE)
- CreatedBy → Users.Id
- UpdatedBy → Users.Id

---

## All Tables Summary

### Independent Tables (13)

1. **ApiKeys** - API authentication keys
2. **CertificateAggregates** - Dashboard statistics
3. **CertificationTypes** - FSC, PEFC, SFI, etc.
4. **Companies** - Certificate holders
5. **ETLAlerts** - ETL error alerts
6. **ETLConfigurations** - ETL job configs
7. **ETLDataQualityIssues** - Data quality tracking
8. **ETLLogEntries** - ETL execution logs
9. **Partners** - Amazon, Wayfair, etc.
10. **Permissions** - RBAC permissions
11. **ProductCategories** - Top-level categories
12. **Roles** - Admin, Analyst, External
13. **Tags** - Certificate and product tags
14. **Users** - Application users

### Level 1 Dependencies (7)

15. **CustomFields** - Custom cert fields (depends on CertificationTypes)
16. **ProductTypes** - Product types (depends on ProductCategories)
17. **PermissionRole** - Join table (Permissions ↔ Roles)
18. **CompanyUser** - Join table (Companies ↔ Users)
19. **PartnerUser** - Join table (Partners ↔ Users)
20. **RoleUser** - Join table (Roles ↔ Users)
21. **ProductSubcategories** - Product subcategories (depends on ProductTypes)

### Level 2 Dependencies (1)

22. **Products** - Products (depends on Companies, ProductCategories, ProductTypes, ProductSubcategories)

### Level 3 Dependencies (3)

23. **Certificates** - Main certificate table (depends on Companies, Products, CertificationTypes, Users)
24. **ProductIdentifiers** - Product IDs (depends on Products)
25. **ProductTag** - Join table (Products ↔ Tags)

### Level 4 Dependencies (4)

26. **CertificateDocuments** - Uploaded files (depends on Certificates, Users)
27. **CertificateProductIds** - Product IDs per cert (depends on Certificates, Users)
28. **CertificateTag** - Join table (Certificates ↔ Tags)
29. **CustomFieldValues** - Custom field data (depends on Certificates, CustomFields)

---

## Common Column Naming Patterns

### Date Columns - IMPORTANT!
- **CreatedAt** - Creation timestamp (DATETIME2, default GETDATE())
- **UpdatedAt** - Last update timestamp (DATETIME2, default GETDATE())
- **EffectiveDate** - Certificate effective date (NOT IssueDate!)
- **ExpirationDate** - Certificate expiration
- **PeriodDate** - Aggregate period start date
- **LastLoginAt** - User last login
- **LastUsedAt** - API key last use

### Common Columns
- **Id** - INT IDENTITY(1,1) primary key
- **Name** - NVARCHAR(100-255) entity name
- **Description** - NVARCHAR(MAX) or NVARCHAR(500)
- **IsActive** - BIT default 1
- **IsDeleted** - BIT default 0 (soft delete)
- **Status** - NVARCHAR(20-50) status field
- **SortOrder** - INT default 0

### Foreign Keys
- Format: `{EntityName}Id` → `{EntityName}.Id`
- Example: `CompanyId` → `Companies.Id`
- CreatedBy/UpdatedBy → Users.Id

---

## Important Notes for SQL Scripts

### ✅ DO Use These Column Names
- **EffectiveDate** (not IssueDate)
- **PeriodDate** (not AggregateDate)
- **ExpiringThisMonth** (not ExpiringSoonCertificates)
- **Period** (format: 'yyyy-MM')

### ❌ DON'T Use These (they don't exist)
- ~~IssueDate~~ → Use **EffectiveDate**
- ~~AggregateDate~~ → Use **PeriodDate**
- ~~ExpiringSoonCertificates~~ → Use **ExpiringThisMonth**
- ~~NewCertificatesThisMonth~~ → Not in CertificateAggregates
- ~~UpdatedAt~~ in CertificateAggregates → Not present

### Text/JSON Columns
These columns expect JSON arrays as TEXT:
- CertificatesByType
- CertificatesByCountry
- CertificatesByCompany
- Use `'[]'` for empty arrays

---

## Schema Verification Queries

### Check if table exists
```sql
SELECT * FROM sys.tables
WHERE name = 'TableName' AND schema_id = SCHEMA_ID('dbo')
```

### List all columns for a table
```sql
SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable,
    OBJECT_DEFINITION(c.default_object_id) AS DefaultValue
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.TableName')
ORDER BY c.column_id;
```

### Check foreign keys
```sql
SELECT
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) = 'TableName';
```

---

## Best Practices

1. **Always verify schema before writing SQL scripts**
   - Check column names in `00_CREATE_SCHEMA.sql`
   - Use schema verification queries above
   - Reference this document

2. **Use proper date column names**
   - Certificates: Use `EffectiveDate` and `ExpirationDate`
   - Aggregates: Use `PeriodDate`
   - Never assume column names without checking

3. **Handle NULL values**
   - Many columns are nullable
   - Use `ISNULL()` or `COALESCE()` for calculations
   - Check schema for default values

4. **Respect foreign key constraints**
   - Some use CASCADE delete
   - Some use NO ACTION
   - Check constraints before bulk operations

5. **Work with composite keys**
   - CertificateAggregates uses (Period, PeriodDate)
   - Join tables use (EntityAId, EntityBId)

---

**Remember:** When in doubt, check `Database_Scripts/00_CREATE_SCHEMA.sql` for the source of truth!
