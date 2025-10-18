# GCC Web Application - Production Deployment Guide
**Date:** October 16, 2025
**Build Version:** Production with Manual Migration Management

---

## Critical Changes in This Release

### Automatic Database Migrations DISABLED

**IMPORTANT:** This deployment includes critical changes to how database schema is managed:

- Automatic migrations are NOW DISABLED for Production/Staging environments
- Application will NO LONGER create or modify database tables on startup
- Schema changes must be applied MANUALLY using migration scripts
- All tables are locked to `dbo` schema (no more wrong-schema tables)

### What Changed in Program.cs

**Lines 632-675:** Modified to skip automatic migrations:

```csharp
// BEFORE (Old Behavior):
await context.Database.MigrateAsync();  // Auto-created tables on startup

// AFTER (New Behavior):
logger.LogInformation("Production environment - skipping automatic migrations");
var canConnect = await context.Database.CanConnectAsync();  // Only validates connectivity
```

---

## Deployment Package Location

```
C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\publish-accuweb-production\
```

**Package Contents:**
- GCCWebApp.exe (main application)
- GCCWebApp.dll (application library)
- All dependencies (300+ DLLs)
- appsettings.Production.json (connection string template)
- web.config (IIS/Kestrel configuration)
- Database_Scripts/ (migration and seeding scripts)
- DEPLOYMENT_INFO.txt (this deployment info)

---

## Step-by-Step Deployment Instructions

### STEP 1: Stop Production Application

**On AccuWebHosting Server:**

```bash
# Option A: Via Plesk Control Panel
# - Login to Plesk at https://manage.accuwebhosting.com
# - Navigate to: Domains > yourdomain.com > Applications
# - Click "Stop" on GCC Web Application

# Option B: Via SSH (if available)
systemctl stop gcc-webapp

# Option C: Kill process manually
taskkill /F /IM GCCWebApp.exe
taskkill /F /IM dotnet.exe
```

### STEP 2: Backup Current Application

**CRITICAL - Always backup before deploying:**

```bash
# Via SSH or FTP
cd /var/www/vhosts/yourdomain.com/
mv httpdocs httpdocs_backup_20251016

# Or via Windows Remote Desktop
cd C:\inetpub\wwwroot\gccwebapp
ren gccwebapp gccwebapp_backup_20251016
```

### STEP 3: Upload New Application Files

**Upload entire contents of:**
```
publish-accuweb-production\
```

**To server location:**
```
/var/www/vhosts/yourdomain.com/httpdocs/       (Linux/Plesk)
C:\inetpub\wwwroot\gccwebapp\                  (Windows/IIS)
```

**Upload Methods:**
- FTP/SFTP (FileZilla, WinSCP)
- Plesk File Manager
- Remote Desktop (Windows)
- SCP command: `scp -r publish-accuweb-production/* user@server:/path/`

### STEP 4: Update Configuration

**Edit appsettings.Production.json on the server:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=YOUR_PASSWORD_HERE;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false"
  },
  "Jwt": {
    "Key": "YOUR_64_CHAR_SECRET_KEY_HERE",
    "Issuer": "https://yourdomain.com",
    "Audience": "https://yourdomain.com"
  }
}
```

**Set correct permissions (Linux):**
```bash
chown -R www-data:www-data /var/www/vhosts/yourdomain.com/httpdocs
chmod 644 appsettings.Production.json
chmod 755 GCCWebApp
```

### STEP 5: Start Application

**On AccuWebHosting Server:**

```bash
# Option A: Via Plesk Control Panel
# - Navigate to: Domains > yourdomain.com > Applications
# - Click "Start" on GCC Web Application

# Option B: Via SSH (if available)
systemctl start gcc-webapp

# Option C: Manual start (Windows)
cd C:\inetpub\wwwroot\gccwebapp
.\GCCWebApp.exe --urls "http://localhost:5000"
```

### STEP 6: VERIFY Deployment

**Critical Checks:**

1. **Check application logs for "skipping automatic migrations":**

```bash
# Look for this in startup logs:
# "Production environment - skipping automatic migrations (manual migration management enabled)"
# "Production database connectivity verified"
```

2. **Verify NO tables are created automatically:**

Connect to SQL Server and check:
```sql
-- This should show NO new tables created by auto-migration
SELECT name, create_date
FROM sys.tables
WHERE schema_id = SCHEMA_ID('dbo')
ORDER BY create_date DESC;
```

3. **Test application endpoints:**

```bash
# Health check
curl http://yourdomain.com/health

# Main page
curl http://yourdomain.com/Certificate/CertificateTileView

# Login page
curl http://yourdomain.com/Account/Login
```

4. **Check logs for errors:**

```bash
# View application logs
tail -f /var/www/vhosts/yourdomain.com/httpdocs/logs/app.log

# Or Windows Event Viewer
eventvwr.msc > Application Logs
```

### STEP 7: Manage Database Migrations (When Needed)

**From now on, database changes are MANUAL:**

```bash
# When you need to apply schema changes:
cd Database_Scripts
.\apply-migrations.cmd Production

# This will:
# 1. Show pending migrations
# 2. Ask for confirmation (type "YES")
# 3. Apply migrations
# 4. Verify success
```

---

## Expected Log Output (After Deployment)

**GOOD - What you SHOULD see:**

```
info: GCCWebApp.Program[0]
      Production environment - skipping automatic migrations (manual migration management enabled)
info: Microsoft.EntityFrameworkCore.Database.Connection[20000]
      Connecting to database 'gccwebap_certificates' on server '68.71.129.120,1533'
info: Microsoft.EntityFrameworkCore.Database.Connection[20001]
      Opened connection to database 'gccwebap_certificates' on server '68.71.129.120,1533'
info: GCCWebApp.Program[0]
      Production database connectivity verified
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**BAD - What you should NOT see:**

```
# These indicate auto-migrations are still enabled (OLD BEHAVIOR):
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20251015162223_InitialCreate_SqlServer_Final'
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (5ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE [dbo].[Certificates] ...

# If you see these, the old version is still deployed!
```

---

## Rollback Plan (If Needed)

If deployment fails or issues occur:

### Quick Rollback

```bash
# Stop new version
systemctl stop gcc-webapp

# Restore backup
rm -rf /var/www/vhosts/yourdomain.com/httpdocs
mv /var/www/vhosts/yourdomain.com/httpdocs_backup_20251016 httpdocs

# Start old version
systemctl start gcc-webapp
```

---

## Database Management (Post-Deployment)

### Manual Migration Workflow

```bash
# 1. Create a new migration (on development machine)
cd GCCWebApp
dotnet ef migrations add AddNewFeature

# 2. Test locally
cd Database_Scripts
.\apply-migrations.ps1 -Environment Development

# 3. Preview production changes (generates SQL)
.\apply-migrations.ps1 -Environment Production -WhatIf

# 4. Review: Database_Scripts/migration_preview_*.sql

# 5. Deploy migration to production (ON SERVER)
cd Database_Scripts
.\apply-migrations.cmd Production
# Type "YES" when prompted

# 6. Verify
systemctl restart gcc-webapp
tail -f /var/logs/gccwebapp.log
```

### Available Database Scripts

**In Database_Scripts folder:**

- `00_CREATE_SCHEMA.sql` - Creates all tables (clean install)
- `FULL_TEST_DATA_SEED.sql` - Populates test data
- `apply-migrations.cmd` - Apply EF Core migrations
- `apply-migrations.ps1` - PowerShell migration script
- `README_DATABASE_SETUP.md` - Complete database setup guide

---

## Troubleshooting

### Issue: Application still creating tables automatically

**Symptoms:** Logs show "Applying migration" messages on startup

**Solution:**
1. Verify you deployed the NEW build from `publish-accuweb-production`
2. Check Program.cs lines 632-675 on server - should have "skipping automatic migrations"
3. Restart application

### Issue: Database connection failed

**Symptoms:** "Production database connection failed!" error on startup

**Solution:**
1. Verify connection string in appsettings.Production.json
2. Test connection: `sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -d gccwebap_certificates`
3. Check firewall allows port 1533
4. Verify SQL Server is running

### Issue: Tables created in wrong schema

**Symptoms:** Tables appear in `GCC_adminxyz123` schema instead of `dbo`

**Solution:**
1. This should NO LONGER happen with the new version
2. If it occurs, you're running the OLD version - redeploy
3. Clean up wrong-schema tables:
   ```sql
   -- 00_CREATE_SCHEMA.sql has automatic cleanup
   -- Just re-run the schema script
   ```

### Issue: Need to rollback migrations

**Solution:**
```bash
# Roll back to specific migration
dotnet ef database update [MigrationName] --project GCCWebApp

# Or restore database from backup
```

---

## Post-Deployment Checklist

- [ ] Old application stopped
- [ ] Backup created (httpdocs_backup_20251016)
- [ ] New files uploaded to server
- [ ] appsettings.Production.json updated with correct connection string
- [ ] Permissions set correctly
- [ ] Application started successfully
- [ ] Logs show "skipping automatic migrations"
- [ ] No tables created automatically on startup
- [ ] Health endpoint responding: http://yourdomain.com/health
- [ ] Certificate tile view loading: http://yourdomain.com/Certificate/CertificateTileView
- [ ] Login page accessible: http://yourdomain.com/Account/Login
- [ ] Database connectivity verified in logs
- [ ] No errors in application logs

---

## Support & Documentation

**Deployment Scripts:**
- `Scripts/deployment/deploy-accuweb-production.cmd` - Build & package script
- `Database_Scripts/apply-migrations.cmd` - Apply migrations on-demand

**Documentation:**
- `Database_Scripts/README_DATABASE_SETUP.md` - Complete database setup guide
- `0_Documentation/docs/Requirements.md` - Feature requirements
- `.claude/CLAUDE.md` - Development guide

**Key Files Modified:**
- `Program.cs` (lines 632-675) - Disabled auto-migrations
- `Database_Scripts/00_CREATE_SCHEMA.sql` - Schema with dbo prefix and cleanup
- `Database_Scripts/apply-migrations.ps1` - Manual migration tool

---

## Summary

### What This Deployment Fixes

1. **Stops automatic table creation** - Application no longer modifies database on startup
2. **Locks schema to dbo** - No more wrong-schema tables
3. **Enables manual control** - You decide when to apply migrations
4. **Provides migration tools** - apply-migrations.cmd for on-demand schema changes
5. **Adds safety** - Production confirmation required before migrations

### What You Need to Do

1. Deploy the new application files (publish-accuweb-production folder)
2. Verify logs show "skipping automatic migrations"
3. Use `apply-migrations.cmd` when you need to update schema
4. Monitor first startup to ensure no auto-migrations occur

---

**Deployment Package Built:** October 16, 2025, 22:33
**Manual Migration Management:** ENABLED
**Auto-Migrations:** DISABLED for Production/Staging
**Schema Locked:** dbo

---

**Questions or Issues?**
Refer to:
- DEPLOYMENT_INFO.txt (in publish folder)
- Database_Scripts/README_DATABASE_SETUP.md
- Check application logs for detailed startup messages
