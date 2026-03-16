# Database Rebuild - Quick Reference Card

## 🚀 One-Command Rebuild

```batch
cd "Database_Scripts"
REBUILD_COMPLETE_PRODUCTION.cmd
```

**What it does**: Drops all tables → Recreates schema → Seeds data (fully automated)

---

## 📋 Manual Step-by-Step

### 1. Generate Schema Script
```bash
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"
dotnet ef migrations script --idempotent > schema_migrations.sql
```

### 2. Drop All Tables
```bash
cd Database_Scripts
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i DROP_ALL_TABLES_SAFE.sql
```

### 3. Create Schema
```bash
cd ..
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i schema_migrations.sql
```

### 4. Seed Data
```bash
cd Database_Scripts
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i FULL_TEST_DATA_SEED.sql
```

---

## 🔍 Verification Commands

### Check table count (should be 35)
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'"
```

### List all tables
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT name FROM sys.tables ORDER BY name"
```

### Check data counts
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT 'Users' as [Table], COUNT(*) as [Count] FROM Users UNION ALL SELECT 'Products', COUNT(*) FROM Products UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates"
```

### Verify schema ownership (should all be 'dbo')
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT SCHEMA_NAME(schema_id) as Schema, name as TableName FROM sys.tables"
```

---

## 🔧 Troubleshooting

### Problem: Only 3 tables created
**Solution**: Drop __EFMigrationsHistory and re-run schema script
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "DROP TABLE __EFMigrationsHistory"
# Then re-run schema script
```

### Problem: "QUOTED_IDENTIFIER" error
**Solution**: Add SET statements to top of schema script
```sql
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
SET ANSI_WARNINGS ON;
GO
```

### Problem: "Cannot insert NULL into column 'UpdatedAt'"
**Solution**: Update FULL_TEST_DATA_SEED.sql INSERT statements to include missing columns

---

## 🔐 Test Login Credentials

**Admin User:**
- URL: https://gccwebapp.com/Account/Login
- Username: `admin`
- Password: `TestPass123!`

**All Test Users** (15 total):
- Password for all: `TestPass123!`
- Usernames: admin, analyst, external, testuser, sarah.johnson, mike.chen, etc.

---

## 📊 Expected Results

| Entity | Count |
|--------|-------|
| Tables | 35 |
| Users | 15 |
| Companies | 28 |
| Products | 43 |
| Certificates | 25 |
| Roles | 4 |
| Partners | 3 |
| API Keys | 3 |

---

## 🗂️ File Locations

```
Database_Scripts/
├── REBUILD_COMPLETE_PRODUCTION.cmd    ← Run this for full rebuild
├── DROP_ALL_TABLES_SAFE.sql           ← Step 2: Drop tables
├── FULL_TEST_DATA_SEED.sql            ← Step 4: Seed data
├── PRODUCTION_REBUILD_GUIDE.md        ← Full documentation
└── QUICK_REFERENCE.md                 ← This file

GCCWebApp/
└── schema_migrations.sql              ← Step 3: Create schema (generated)
```

---

## ⏱️ Estimated Time

- **Step 1** (Generate): ~10 seconds
- **Step 2** (Drop): ~5 seconds
- **Step 3** (Schema): ~30-60 seconds
- **Step 4** (Seed): ~30-60 seconds
- **Total**: ~2-3 minutes

---

## ⚠️ Important Notes

1. ⚠️ **ALWAYS backup before rebuild** if production has real data
2. ✅ Scripts are **idempotent** - safe to re-run if errors occur
3. 🔒 All tables MUST be in **dbo schema** (not username schema)
4. 📝 Update FULL_TEST_DATA_SEED.sql when adding new required columns
5. 🔄 Regenerate schema_migrations.sql after creating new EF migrations

---

## 🆘 Quick Help

**Full documentation**: `PRODUCTION_REBUILD_GUIDE.md`

**Connection issues?** Check:
- Network connectivity to 68.71.129.120:1533
- User `GCC_adminxyz123` has `db_owner` role
- Database `gccwebap_certificates` exists

**Still stuck?** Review console error messages carefully - they usually indicate exactly what's wrong.
