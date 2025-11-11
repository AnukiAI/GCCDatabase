# GCC Database Comparison Report
**Date:** 2025-11-10
**Databases Compared:**
- **Development:** ANUKI01 - GCCDatabase_Dev
- **Production:** 68.71.129.120,1533 - gccwebap_certificates

---

## Executive Summary

The development and production databases have **identical schema structures** but differ significantly in:
1. **Database permissions** (CRITICAL issue found)
2. **Data volumes** (Production has more business data, but missing some reference data)
3. **SQL Server versions** (Dev: 2022, Prod: 2019)

---

## 1. Migration Status ✅ IDENTICAL

Both databases have the same 5 EF Core migrations applied:

| Migration | Version |
|-----------|---------|
| 20251015162223_InitialCreate_SqlServer_Final | 8.0.11 |
| 20251029003704_AddLevelToCertificationType | 8.0.11 |
| 20251029005907_DistributeCertificatesAcrossTypes | 8.0.11 |
| 20251029011232_AddParentIdToCertificationType | 8.0.11 |
| 20251029163311_FixWasteSystemAuditLevel | 8.0.11 |

**Status:** ✅ Both databases are at the same migration level

---

## 2. Schema Comparison ✅ IDENTICAL

**Total Tables:** 30 (both databases)
**Schema Ownership:** All tables in 'dbo' schema (both databases)
**Column Definitions:** Identical across all tables

### Table List (Same in Both)
- __EFMigrationsHistory
- ApiKeys
- CertificateAggregates
- CertificateDocuments
- CertificateProductIds
- Certificates
- CertificateTag
- CertificationTypes
- Companies
- CompanyUser
- CustomFields
- CustomFieldValues
- ETLAlerts
- ETLConfigurations
- ETLDataQualityIssues
- ETLLogEntries
- Partners
- PartnerUser
- PermissionRole
- Permissions
- ProductCategories
- ProductIdentifiers
- Products
- ProductSubcategories
- ProductTag
- ProductTypes
- Roles
- RoleUser
- Tags
- Users

**Status:** ✅ Schema structures are 100% identical

---

## 3. Data Comparison ⚠️ SIGNIFICANT DIFFERENCES

### Record Count Comparison

| Table | Dev (ANUKI01) | Prod (AccuWeb) | Difference | Analysis |
|-------|--------------|----------------|------------|----------|
| **__EFMigrationsHistory** | 5 | 5 | 0 | ✅ Same migrations |
| **ApiKeys** | 5 | 0 | -5 | ⚠️ Dev has test API keys |
| **CertificateAggregates** | 0 | 0 | 0 | ✅ Empty in both |
| **CertificateDocuments** | 0 | 0 | 0 | ✅ Empty in both |
| **CertificateProductIds** | 0 | 0 | 0 | ✅ Empty in both |
| **Certificates** | 178 | 250 | +72 | ⚠️ Prod has more certificates |
| **CertificateTag** | 0 | 165 | +165 | ⚠️ Prod has tag relationships |
| **CertificationTypes** | 22 | 10 | -12 | ⚠️ Dev has more types |
| **Companies** | 28 | 50 | +22 | ⚠️ Prod has more companies |
| **CompanyUser** | 0 | 0 | 0 | ✅ Empty in both |
| **CustomFields** | 30 | 0 | -30 | 🔴 **CRITICAL: Prod missing custom fields** |
| **CustomFieldValues** | 0 | 0 | 0 | ✅ Empty in both |
| **ETLAlerts** | 0 | 0 | 0 | ✅ Empty in both |
| **ETLConfigurations** | 0 | 0 | 0 | ✅ Empty in both |
| **ETLDataQualityIssues** | 0 | 0 | 0 | ✅ Empty in both |
| **ETLLogEntries** | 0 | 0 | 0 | ✅ Empty in both |
| **Partners** | 3 | 3 | 0 | ✅ Same partners |
| **PartnerUser** | 0 | 0 | 0 | ✅ Empty in both |
| **PermissionRole** | 12 | 0 | -12 | 🔴 **CRITICAL: Prod missing permission mappings** |
| **Permissions** | 6 | 0 | -6 | 🔴 **CRITICAL: Prod missing permissions** |
| **ProductCategories** | 6 | 10 | +4 | ⚠️ Prod has more categories |
| **ProductIdentifiers** | 0 | 0 | 0 | ✅ Empty in both |
| **Products** | 39 | 150 | +111 | ⚠️ Prod has many more products |
| **ProductSubcategories** | 0 | 0 | 0 | ✅ Empty in both |
| **ProductTag** | 0 | 0 | 0 | ✅ Empty in both |
| **ProductTypes** | 19 | 0 | -19 | 🔴 **CRITICAL: Prod missing product types** |
| **Roles** | 4 | 4 | 0 | ✅ Same roles |
| **RoleUser** | 14 | 15 | +1 | ⚠️ Prod has 1 more user-role mapping |
| **Tags** | 26 | 0 | -26 | 🔴 **CRITICAL: Prod missing all tags** |
| **Users** | 14 | 15 | +1 | ⚠️ Prod has 1 more user |

### Key Findings:

**Production has MORE business data:**
- +72 Certificates (250 vs 178)
- +22 Companies (50 vs 28)
- +111 Products (150 vs 39)
- +4 Product Categories (10 vs 6)
- +165 Certificate-Tag relationships

**Production is MISSING critical reference data:**
- 🔴 **0 ProductTypes** (Dev has 19) - This could break product management functionality
- 🔴 **0 Tags** (Dev has 26) - Tagging system won't work
- 🔴 **0 CustomFields** (Dev has 30) - Custom field functionality missing
- 🔴 **0 Permissions** (Dev has 6) - Permission system not configured
- 🔴 **0 PermissionRole** (Dev has 12) - Permission-role mappings missing
- 🔴 **0 CertificationTypes** at Level 2 (10 vs 22) - May impact certificate creation

---

## 4. Schema Ownership & Permissions

### Schema Ownership ✅ CORRECT

Both databases have correct schema setup:

| Database | User | Default Schema | Status |
|----------|------|----------------|--------|
| Dev | GCC_DevUser | dbo | ✅ Correct |
| Production | GCC_adminxyz123 | dbo | ✅ Correct |

All 30 tables are in the 'dbo' schema (not user-specific schemas).

### Database Permissions 🔴 CRITICAL ISSUE

| Database | User | Database Roles |
|----------|------|----------------|
| **Dev** | GCC_DevUser | **db_owner** |
| **Production** | GCC_adminxyz123 | db_datareader, db_datawriter, db_ddladmin |

**CRITICAL ISSUE:** According to CLAUDE.md requirements:
> Production user MUST have: db_owner role membership

**Current Status:** Production user does NOT have db_owner role.

**Impact:**
- Cannot manage security settings
- Cannot perform certain administrative operations
- May encounter permission errors during migrations or schema updates
- Limited ability to troubleshoot production issues

**Recommendation:** Grant db_owner role to GCC_adminxyz123:
```sql
USE gccwebap_certificates;
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
```

---

## 5. SQL Server Version Differences

| Environment | SQL Server Version | Build Date |
|-------------|-------------------|------------|
| **Dev (ANUKI01)** | SQL Server 2022 (16.0.1150.1) | Aug 13, 2025 |
| **Production** | SQL Server 2019 (15.0.4445.1) | Aug 13, 2025 |

**Impact:** Minor compatibility differences may exist, but both support EF Core 8.0.11. No immediate concerns.

---

## 6. Recommendations

### CRITICAL (Immediate Action Required)

1. **Grant db_owner role to production user:**
   ```sql
   USE gccwebap_certificates;
   ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
   ```

2. **Populate missing reference data in Production:**
   - **ProductTypes** (19 missing) - Export from Dev, import to Prod
   - **Tags** (26 missing) - Export from Dev, import to Prod
   - **CustomFields** (30 missing) - Export from Dev, import to Prod
   - **Permissions** (6 missing) - Export from Dev, import to Prod
   - **PermissionRole** (12 missing) - Export from Dev, import to Prod

### HIGH Priority

3. **Verify CertificationTypes data:** Production has only 10 vs Dev's 22. Determine if this is intentional or missing data.

4. **Consider API Keys:** Production has no API keys. If API access is needed, generate production API keys.

### MEDIUM Priority

5. **Document data strategy:** Clarify which environment is the "source of truth" for:
   - Business data (Certificates, Companies, Products) - appears to be Production
   - Reference data (Types, Tags, Fields) - appears to be Development

6. **Set up data synchronization:** Implement a process to keep reference data in sync between environments.

7. **Backup before changes:** Create full backups of both databases before making any changes.

---

## 7. Next Steps - Data Migration Script

Would you like me to generate SQL scripts to:
1. Export missing reference data from Dev?
2. Import that data into Production?
3. Verify the production database has db_owner permissions?

---

## Summary Status

| Component | Status | Details |
|-----------|--------|---------|
| Migrations | ✅ PASS | Both at same migration level |
| Schema Structure | ✅ PASS | 100% identical |
| Schema Ownership | ✅ PASS | All tables in dbo schema |
| Database Permissions | 🔴 FAIL | Production missing db_owner role |
| Business Data | ⚠️ WARNING | Production has more (expected) |
| Reference Data | 🔴 FAIL | Production missing critical lookup data |
| SQL Server Version | ⚠️ INFO | Different versions but compatible |

**Overall Assessment:** Database schemas are identical, but Production is missing critical reference data and proper permissions. This needs immediate attention before deployment.
