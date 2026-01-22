# Database Rebuild Documentation

Complete guide for rebuilding the GCC production test database from scratch.

## 📚 Documentation Files

This folder contains all scripts and documentation for rebuilding the database:

| File | Purpose | Type |
|------|---------|------|
| **REBUILD_COMPLETE_PRODUCTION.cmd** | 🚀 **RUN THIS** for automated rebuild | Executable |
| **PRODUCTION_REBUILD_GUIDE.md** | 📖 Comprehensive step-by-step guide | Documentation |
| **QUICK_REFERENCE.md** | 📋 Quick command reference card | Documentation |
| **REBUILD_PROCESS_FLOW.md** | 📊 Visual process flowcharts | Documentation |
| **README_REBUILD.md** | 📄 This file - overview and index | Documentation |
| **DROP_ALL_TABLES_SAFE.sql** | Step 2: Safely drop all tables | SQL Script |
| **FULL_TEST_DATA_SEED.sql** | Step 4: Seed comprehensive test data | SQL Script |

**Generated during rebuild:**
- `schema_migrations.sql` - Created in parent folder (GCCWebApp/) during Step 1

---

## 🎯 Quick Start

### Option 1: Automated (Recommended)

**One command does everything:**

```batch
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\Database_Scripts"
REBUILD_COMPLETE_PRODUCTION.cmd
```

**What happens:**
1. ✅ Generates fresh schema from Entity Framework migrations
2. ✅ Drops all existing tables safely
3. ✅ Recreates schema (35 tables)
4. ✅ Seeds test data (15 users, 28 companies, 43 products, 25 certificates)
5. ✅ Verifies success

**Time**: ~2-3 minutes total

---

### Option 2: Manual (For Learning/Debugging)

See **QUICK_REFERENCE.md** for individual commands.

---

## 📖 Which Guide Should I Read?

| If you want to... | Read this file |
|-------------------|----------------|
| **Just rebuild quickly** | Run `REBUILD_COMPLETE_PRODUCTION.cmd` - no reading needed! |
| **Understand the full process** | **PRODUCTION_REBUILD_GUIDE.md** (comprehensive, ~15 min read) |
| **Get quick commands** | **QUICK_REFERENCE.md** (cheat sheet, 2 min) |
| **See visual flowcharts** | **REBUILD_PROCESS_FLOW.md** (diagrams, 5 min) |
| **Troubleshoot issues** | **PRODUCTION_REBUILD_GUIDE.md** → Troubleshooting section |
| **Customize seed data** | Edit **FULL_TEST_DATA_SEED.sql** |

---

## ⚡ What Gets Created

After successful rebuild, your database will have:

### Schema (35 Tables)
```
ApiKeys                    PartnerApiKeys
AmazonApiSettings          Partners
AmazonSyncHistory          PartnerUser
CertificateAggregates      PermissionRole
CertificateDocuments       Permissions
CertificateProductIds      ProductCategories
Certificates               ProductIdentifiers
CertificateStatusHistory   Products
CertificateTag             ProductSubcategories
CertificationTypes         ProductTag
Companies                  ProductTypes
CompanyUser                Roles
CustomFields               RoleUser
CustomFieldValues          Tags
ETLAlerts                  Users
ETLConfigurations          __EFMigrationsHistory
ETLDataQualityIssues
ETLLogEntries
GoogleAnalyticsSettings
```

### Test Data

| Entity Type | Count | Examples |
|-------------|-------|----------|
| **Users** | 15 | admin, analyst, external, testuser, sarah.johnson, mike.chen, etc. |
| **Companies** | 28 | ASSA ABLOY, 3M, Interface, Steelcase, Herman Miller, etc. |
| **Products** | 43 | Exit devices, carpet tiles, LED lights, insulation, flooring |
| **Certificates** | 25 | Numbers 24-1605 through 24-1629 (active, draft, expired) |
| **Roles** | 4 | Admin, Analyst, External, Public |
| **Permissions** | 6 | Certificate CRUD, User Management, Report Access |
| **Partners** | 3 | Amazon, Wayfair, Other |
| **Product Categories** | 6 | Building Materials, Electronics, Furniture, Textiles, etc. |
| **Product Types** | 19 | Exit Hardware, Insulation, LED Lights, Flooring, Carpet, etc. |
| **Certification Types** | 5 | CEF, CSF, RC, ES, MIR |
| **Tags** | 10 | Environmental Facts, Recycled Content, Carbon Neutral, etc. |
| **API Keys** | 3 | Integration Test, Partner Access, Mobile App |

### Test Credentials

**All users have the same password**: `TestPass123!`

| Username | Email | Role | Notes |
|----------|-------|------|-------|
| **admin** | admin@gcc.com | Admin | Full system access |
| **analyst** | analyst@gcc.com | Analyst | Certificate management |
| **external** | external@company.com | External | Partner access |
| testuser | test@gcc.com | Public | Basic read access |
| sarah.johnson | sarah.johnson@gcc.com | Analyst | - |
| mike.chen | mike.chen@gcc.com | Analyst | - |
| lisa.rodriguez | lisa.rodriguez@gcc.com | External | - |
| david.kim | david.kim@gcc.com | External | - |
| emma.wilson | emma.wilson@gcc.com | External | - |
| alex.martinez | alex.martinez@gcc.com | External | - |
| jennifer.lee | jennifer.lee@gcc.com | Analyst | - |
| robert.taylor | robert.taylor@gcc.com | Analyst | - |
| maria.garcia | maria.garcia@gcc.com | External | - |
| public.user | public.user@gcc.com | Public | - |

---

## ⚠️ Important Warnings

### Before Running Rebuild

1. **⚠️ DATA LOSS**: This process **DELETES ALL DATA** in the database
2. **🔒 Target**: Only run on **production TEST** environment (68.71.129.120,1533)
3. **👥 Notify**: Inform other developers if shared environment
4. **💾 Backup**: Create backup if production has any data worth saving
5. **⏱️ Time**: Allocate 5 minutes - don't interrupt the process

### After Rebuild

1. **✅ Verify**: Check that table count = 35, user count = 15
2. **🔐 Test Login**: Ensure you can login as admin/TestPass123!
3. **📱 Test Pages**: Visit User Management and Product Management pages
4. **🎯 Schema**: Verify all tables are in 'dbo' schema (not username schema)

---

## 🔧 Common Issues & Solutions

### Issue: Script generates error about QUOTED_IDENTIFIER
**Solution**: The fix is already in REBUILD_COMPLETE_PRODUCTION.cmd - it adds SET statements automatically

### Issue: Only 3 tables created instead of 35
**Solution**: Drop __EFMigrationsHistory table and re-run:
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "DROP TABLE __EFMigrationsHistory"
# Then re-run REBUILD_COMPLETE_PRODUCTION.cmd
```

### Issue: "Cannot insert NULL into column..."
**Solution**: FULL_TEST_DATA_SEED.sql needs updating for new columns - check which column is mentioned and add it to INSERT statement

### Issue: Script hangs or times out
**Solution**: Check network connectivity to 68.71.129.120:1533, verify firewall rules

---

## 🎓 Learning Resources

### Understanding the Process

1. **Read**: REBUILD_PROCESS_FLOW.md for visual flowcharts
2. **Review**: DROP_ALL_TABLES_SAFE.sql to see how tables are dropped safely
3. **Explore**: FULL_TEST_DATA_SEED.sql to understand test data structure
4. **Learn**: PRODUCTION_REBUILD_GUIDE.md for detailed explanations

### Advanced Customization

- **Change test data**: Edit FULL_TEST_DATA_SEED.sql
- **Add new tables**: Create EF migration, regenerate schema_migrations.sql
- **Modify structure**: Update Entity Framework models, create migration
- **Different environment**: Change connection details in .cmd file

---

## 📞 Support & Help

### Self-Help Resources

1. **Error occurred?** → Check PRODUCTION_REBUILD_GUIDE.md → Troubleshooting section
2. **Need quick command?** → Check QUICK_REFERENCE.md
3. **Understanding flow?** → Check REBUILD_PROCESS_FLOW.md
4. **Want to customize?** → Edit FULL_TEST_DATA_SEED.sql

### Getting More Help

1. Review console error messages (they're usually very specific)
2. Check that all prerequisites are met (sqlcmd, dotnet CLI, permissions)
3. Verify database connection (try connecting with SQL Server Management Studio)
4. Review Recent Changes section below

---

## 📅 Recent Changes

### 2025-11-17 - Initial Complete Rebuild System
- ✅ Created REBUILD_COMPLETE_PRODUCTION.cmd for automated rebuild
- ✅ Created DROP_ALL_TABLES_SAFE.sql to safely drop all tables
- ✅ Fixed FULL_TEST_DATA_SEED.sql with all required columns:
  - Companies: Added IsDeleted, CreatedAt, UpdatedAt
  - Products: Added IsDeleted, ProductTypeId, CreatedAt, UpdatedAt
  - ApiKeys: Added RequireTLS, SecurityViolationCount, IsTemporarilyBlocked, SecurityViolations, RequestCount
- ✅ Added SET QUOTED_IDENTIFIER to schema generation
- ✅ Created comprehensive documentation (4 guides)
- ✅ Tested on production test environment
- ✅ Verified all 35 tables created successfully
- ✅ Verified all test data seeded correctly

---

## 🎯 Success Criteria

After running rebuild, verify these conditions:

```
✅ 35 tables exist
✅ 15 users can be queried
✅ 43 products exist
✅ 25 certificates exist
✅ Login works with admin/TestPass123!
✅ User Management page loads and shows 15 users
✅ Product Management page loads and shows 43 products
✅ Certificate Tile View shows 25 certificates
✅ All tables are in 'dbo' schema (verify with schema query)
```

---

## 🚀 Next Steps After Rebuild

1. **Test Application**: Visit https://gccwebapp.com and test core functionality
2. **Verify Pages**:
   - Login: https://gccwebapp.com/Account/Login
   - User Management: https://gccwebapp.com/Admin/UserManagement
   - Product Management: https://gccwebapp.com/Product/Management
   - Certificates: https://gccwebapp.com/Certificate/CertificateTileView
3. **Run Tests**: Execute automated test suite to ensure everything works
4. **Document Changes**: If you customized seed data, update this README

---

## 📂 Related Documentation

### In This Folder
- PRODUCTION_REBUILD_GUIDE.md - Comprehensive guide
- QUICK_REFERENCE.md - Command cheat sheet
- REBUILD_PROCESS_FLOW.md - Visual flowcharts

### In Project Root
- `docs/database/` - Complete database documentation
- `docs/database/deployment/QUICK_START.md` - Database setup guide
- `docs/database/reference/CRITICAL_SCHEMA_REQUIREMENT.md` - Schema ownership requirements
- `docs/database/troubleshooting/COMMON_ISSUES.md` - Troubleshooting guide

---

**Last Updated**: 2025-11-17
**Tested On**: Production Test Environment (68.71.129.120,1533)
**Status**: ✅ Verified Working
**Next Review**: When schema changes or new migrations added
