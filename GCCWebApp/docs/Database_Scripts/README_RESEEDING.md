# Database Reseeding Guide

## Quick Start - Interactive Menu (Recommended)

```bash
# Double-click or run:
DATABASE_MANAGER.cmd
```

**Choose from:**
1. **Quick Reseed** - Clean data + reseed (fast, safe)
2. **Full Rebuild** - Drop + schema + seed (complete wipe)
3. **Clean Only** - Remove all data
4. **Seed Only** - Add test data

## Quick Start - Direct Scripts

### Option 1: Quick Reseed (Recommended for Regular Use)
```bash
RESEED_DATABASE_COMPLETE.cmd
```
- ✅ Clean all data (keeps schema intact)
- ✅ Reseed with 15 users + test data
- ✅ Reset identity seeds
- ⏱️ ~30 seconds

### Option 2: Full Rebuild (For Testing Schema Changes)
```bash
REBUILD_DATABASE_COMPLETE.cmd
```
- ⚠️ **DESTRUCTIVE** - Drops all tables
- ✅ Recreates schema from scratch
- ✅ Reseeds with test data
- ⏱️ ~2 minutes

### Option 2: Manual Steps

**Step 1: Clean Data**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i CLEAN_ALL_DATA.sql
```

**Step 2: Reseed Data**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i FULL_TEST_DATA_SEED.sql
```

## What Gets Created

### Users (15 total)
| Username | Email | Role | Password |
|----------|-------|------|----------|
| admin | admin@gcc.com | Admin | TestPass123! |
| analyst | analyst@gcc.com | Analyst | TestPass123! |
| external | external@company.com | External | TestPass123! |
| testuser | test@gcc.com | Public | TestPass123! |
| ... | ... | ... | TestPass123! |

### Other Data
- **4 Roles**: Admin, Analyst, External, Public
- **6 Permissions**: Certificate CRUD, User Management, Reports
- **3 Partners**: Amazon, Wayfair, Other
- **28 Companies**: ASSA ABLOY, 3M, Interface, Steelcase, etc.
- **5 Certification Types**: CEF, CSF, RC, ES, MIR
- **6 Product Categories**: Building Materials, Electronics, etc.
- **43 Products**: Door handles, insulation, LED lights, etc.
- **25 Certificates**: Active, draft, and expired examples
- **50+ Tags**: Internal and external tags

## Important Notes

### Safety Features
- **5-second countdown** before cleaning data
- **Transaction-based** - rolls back on error
- **Preserves schema** - only deletes data, not tables
- **Re-enables constraints** - even if errors occur

### When to Use

✅ **Good Use Cases:**
- Setting up a fresh test environment
- Fixing duplicate key errors
- Resetting after failed migrations
- Creating a clean demo environment

❌ **Don't Use When:**
- Production has real customer data
- You need to preserve existing data
- Testing incremental updates

### Troubleshooting

**"Cannot delete because of foreign key constraint"**
- The script disables constraints first
- If you see this, constraints weren't disabled
- Run: `EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'`

**"Identity seed already set"**
- Safe to ignore - just means table was already reset
- Doesn't affect functionality

**"Timeout expired"**
- Large databases may take longer
- Increase timeout: `sqlcmd -t 120` (120 seconds)

**"Login failed"**
- Check connection string in script
- Verify SQL Server authentication is enabled
- Confirm user has db_owner permissions

## File Reference

| File | Purpose | Destructive? |
|------|---------|--------------|
| `DATABASE_MANAGER.cmd` | 🎯 Interactive menu (recommended) | Varies |
| `REBUILD_DATABASE_COMPLETE.cmd` | Drop + Schema + Seed | ⚠️ YES |
| `RESEED_DATABASE_COMPLETE.cmd` | Clean + Seed | ❌ No (safe) |
| `CLEAN_ALL_DATA.sql` | Removes all data, keeps schema | ❌ No (safe) |
| `FULL_TEST_DATA_SEED.sql` | Seeds comprehensive test data | ❌ No (safe) |
| `SCHEMA_COMPLETE.sql` | Creates all tables from scratch | ❌ No (safe) |
| `DROP_ALL_TABLES.sql` | Drops entire schema | ⚠️ YES |
| `PRODUCTION_ESSENTIAL_SEED.sql` | Minimal production data only | ❌ No (safe) |

## After Reseeding

1. **Test Login:**
   - https://gccwebapp.com/Account/Login
   - Username: `admin`
   - Password: `TestPass123!`

2. **Verify Users:**
   - https://gccwebapp.com/Admin/UserManagement
   - Should see 15 users

3. **Check Certificates:**
   - https://gccwebapp.com/Certificate/CertificateTileView
   - Should see 25 certificates

## Production vs Development

### Production (gccwebapp.com)
- Uses scripts from this folder
- Connection: `68.71.129.120,1533`
- Database: `gccwebap_certificates`

### Development (localhost)
- Auto-seeds on startup (Development environment)
- Or run scripts manually against `ANUKI01`

## Best Practices

1. **Always backup production before reseeding**
   ```sql
   BACKUP DATABASE gccwebap_certificates
   TO DISK = 'C:\Backups\gccwebapp_backup.bak'
   ```

2. **Test scripts on dev first**
   - Run against development database
   - Verify results
   - Then run on production

3. **Use version control**
   - Commit script changes to git
   - Tag releases: `git tag v1.0-seed-scripts`

4. **Document custom changes**
   - If you modify scripts, update this README
   - Note what data you added/changed

## Support

If you encounter issues:
1. Check error messages in console output
2. Review troubleshooting section above
3. Check application logs
4. Verify database connectivity

For schema changes or migrations, see:
- `docs/database/README.md`
- `Migrations/` folder
