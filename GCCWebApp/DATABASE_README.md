# GCC Database Documentation

**All database documentation has been consolidated.**

---

## 📚 Documentation Location

**See:** [docs/database/](docs/database/)

---

## Quick Links

### 🚀 Getting Started
- [**Quick Start**](docs/database/deployment/QUICK_START.md) - Get running in 5 minutes
- [**Critical Schema Requirement**](docs/database/reference/CRITICAL_SCHEMA_REQUIREMENT.md) - **MUST READ**

### 📋 Deployment
- [Quick Start Guide](docs/database/deployment/QUICK_START.md)
- [Sync Dev to Production](docs/database/deployment/SYNC_DEV_TO_PROD.md)

### 🔧 Troubleshooting
- [Common Issues](docs/database/troubleshooting/COMMON_ISSUES.md)
- [Zero Certificates Issue](docs/database/troubleshooting/ZERO_CERTIFICATES_ISSUE.md)

### 📚 Reference
- [Schema Overview](docs/database/reference/SCHEMA_OVERVIEW.md)
- [Connection Strings](docs/database/reference/CONNECTION_STRINGS.md)

---

## Database Scripts

**Rebuild Package:** `DB_REBUILD_PACKAGE_WITH_DATA/`
- Complete database rebuild with test data
- See [Quick Start Guide](docs/database/deployment/QUICK_START.md)

**Sync Scripts:**
- `export_from_dev.cmd` - Export dev data
- `import_to_production.cmd` - Import to production
- See [Sync Dev to Prod](docs/database/deployment/SYNC_DEV_TO_PROD.md)

---

## Critical Reminder

**ALL TABLES MUST BE IN `dbo` SCHEMA**

Before creating tables:
```sql
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
```

See: [Critical Schema Requirement](docs/database/reference/CRITICAL_SCHEMA_REQUIREMENT.md)

---

**For complete documentation index:** [docs/database/README.md](docs/database/README.md)
