# GCC Database Documentation

**Last Updated:** November 17, 2025

This directory contains all database documentation for the GCC Web Application.

---

## Quick Navigation

### 🚀 Getting Started

**New to the project?** Start here:
1. [Quick Start Guide](deployment/QUICK_START.md) - Get up and running in 5 minutes
2. [Critical Schema Requirement](reference/CRITICAL_SCHEMA_REQUIREMENT.md) - **MUST READ** before deployment
3. [Pre-Deployment Checklist](deployment/PRE_DEPLOYMENT_CHECKLIST.md) - Print and follow

### 📋 Deployment Guides

- [**Quick Start**](deployment/QUICK_START.md) - Complete database rebuild (5 minutes)
- [**First Time Deployment**](deployment/FIRST_TIME_DEPLOYMENT.md) - Comprehensive initial setup
- [**Sync Dev to Production**](deployment/SYNC_DEV_TO_PROD.md) - Standard sync (Companies, Products, Certificates)
- [**Complete 100% Sync**](deployment/COMPLETE_SYNC_GUIDE.md) - **Full sync including product IDs, tags, custom fields**
- [**Pre-Deployment Checklist**](deployment/PRE_DEPLOYMENT_CHECKLIST.md) - Step-by-step verification

### 📚 Reference Documentation

- [**Schema Overview**](reference/SCHEMA_OVERVIEW.md) - Database tables & structure
- [**Critical Schema Requirement**](reference/CRITICAL_SCHEMA_REQUIREMENT.md) - Why dbo schema is mandatory
- [**Connection Strings**](reference/CONNECTION_STRINGS.md) - Dev & production database info

### 🔧 Troubleshooting

- [**Common Issues**](troubleshooting/COMMON_ISSUES.md) - FAQ & quick fixes
- [**Zero Certificates Issue**](troubleshooting/ZERO_CERTIFICATES_ISSUE.md) - Specific fix guide
- [**Lessons Learned**](troubleshooting/LESSONS_LEARNED.md) - Historical issues & solutions

### 🗄️ Scripts

- [**Scripts Overview**](scripts/README.md) - How to use database scripts
- [**Rebuild Scripts**](scripts/rebuild/) - Complete database rebuild
- [**Sync Scripts**](scripts/sync/) - Dev to production sync
- [**Diagnostic Scripts**](scripts/diagnostic/) - Verification queries

---

## Critical Information

### ⚠️ Schema Ownership Requirement

**ALL TABLES MUST BE IN `dbo` SCHEMA**

Entity Framework requires tables as `dbo.TableName` (not `username.TableName`).

**Before creating tables:**
```sql
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
```

**Verify after deployment:**
```sql
SELECT SCHEMA_NAME(schema_id) as Schema, name as TableName
FROM sys.tables
ORDER BY Schema, TableName;
```

All tables must show schema: `dbo`

See [Critical Schema Requirement](reference/CRITICAL_SCHEMA_REQUIREMENT.md) for details.

---

## Database Environments

### Development
- **Server:** ANUKI01
- **Database:** GCCDatabase_Dev
- **Auth:** SQL Server (GCC_DevUser / DevP@ss2024!SQL)

### Production
- **Server:** 68.71.129.120,1533
- **Database:** gccwebap_certificates
- **Auth:** SQL Server (GCC_adminxyz123)

See [Connection Strings](reference/CONNECTION_STRINGS.md) for full details.

---

## Common Tasks

### First Time Setup
```bash
# Run the rebuild package
cd DB_REBUILD_PACKAGE_WITH_DATA
# Execute scripts in order: 01, 02, 03
```

See: [Quick Start Guide](deployment/QUICK_START.md)

### Sync Dev Data to Production
```bash
# Export from dev
export_from_dev.cmd

# Import to production
import_to_production.cmd
```

See: [Sync Dev to Production](deployment/SYNC_DEV_TO_PROD.md)

### Verify Schema Ownership
```sql
-- Check user default schema
SELECT default_schema_name
FROM sys.database_principals
WHERE name = 'GCC_adminxyz123';
-- Must return: dbo

-- Check table schemas
SELECT SCHEMA_NAME(schema_id), COUNT(*)
FROM sys.tables
GROUP BY SCHEMA_NAME(schema_id);
-- Must show: dbo | 35+
```

See: [Diagnostic Scripts](scripts/diagnostic/)

---

## Getting Help

### If you're experiencing:

**"No certificates displaying"**
→ [Zero Certificates Issue](troubleshooting/ZERO_CERTIFICATES_ISSUE.md)

**"Invalid object name 'dbo.Certificates'"**
→ [Critical Schema Requirement](reference/CRITICAL_SCHEMA_REQUIREMENT.md)

**"Login failed" / Connection errors**
→ [Common Issues](troubleshooting/COMMON_ISSUES.md)

**First time deploying**
→ [First Time Deployment](deployment/FIRST_TIME_DEPLOYMENT.md)

**Syncing environments**
→ [Sync Dev to Production](deployment/SYNC_DEV_TO_PROD.md)

---

## Documentation Status

| Document | Status | Last Updated |
|----------|--------|--------------|
| README.md (this file) | ✅ Current | 2025-11-17 |
| Quick Start | ✅ Current | 2025-11-17 |
| First Time Deployment | ✅ Current | 2025-11-17 |
| Sync Dev to Prod | ✅ Current | 2025-11-17 |
| Critical Schema Requirement | ✅ Current | 2025-11-17 |
| Common Issues | ✅ Current | 2025-11-17 |

---

## Change Log

### 2025-11-17
- ✅ Consolidated all database documentation into unified structure
- ✅ Created clear navigation hierarchy
- ✅ Removed duplicate and outdated documentation
- ✅ Updated all references and file paths

### Previous History
- See individual files for detailed change history
- Major consolidation from 25+ scattered files to organized structure

---

**For questions or issues, see the troubleshooting section or review relevant deployment guides.**
