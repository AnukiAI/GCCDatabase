# Pre-Deployment Checklist

Complete each item before deploying to production.

## 1. Code Verification

- [ ] **Build succeeds** - Run `dotnet build` with 0 errors
- [ ] **Tests pass** - Run `dotnet test` and verify critical tests pass
- [ ] **No uncommitted changes** - Run `git status` to verify clean state
- [ ] **Code reviewed** - All changes have been reviewed

## 2. Configuration Verification

- [ ] **appsettings.Production.json** - Verify correct settings:
  - [ ] Connection string points to production database (68.71.129.120,1533)
  - [ ] `PersistKeysToAzureBlobStorage: false`
  - [ ] `EnableSwagger: false`
  - [ ] `EnableDebugEndpoints: false`
  - [ ] JWT key is set (64+ characters)

- [ ] **web.config** - Verify Plesk compatibility:
  - [ ] NO `<fileExtensions>` section
  - [ ] NO `<hiddenSegments>` section
  - [ ] `ASPNETCORE_ENVIRONMENT` = Production
  - [ ] `stdoutLogEnabled` = true

## 3. Database Verification

- [ ] **Production database accessible** - Test connection
- [ ] **Schema is correct** - All tables in `dbo` schema
- [ ] **No pending migrations** that could break production
- [ ] **Backup exists** - Recent backup of production data

## 4. Build Deployment Package

```bash
# Navigate to project directory
cd GCCWebApp

# Clean previous builds
dotnet clean

# Build and publish
dotnet publish GCCWebApp.csproj -c Release -r win-x64 --self-contained false -o publish-production
```

## 5. Verify Package Contents

- [ ] `GCCWebApp.dll` exists
- [ ] `GCCWebApp.exe` exists
- [ ] `appsettings.json` exists
- [ ] `appsettings.Production.json` exists
- [ ] `web.config` exists
- [ ] `wwwroot/` folder with CSS, JS, images
- [ ] `logs/` folder exists (create if missing)

## 6. Create Logs Folder

```bash
mkdir -p publish-production/logs
touch publish-production/logs/.gitkeep
```

## 7. Final Verification Commands

```bash
# Check web.config for Plesk issues
grep -c "fileExtensions" publish-production/web.config
# Should return: 0

grep -c "hiddenSegments" publish-production/web.config
# Should return: 0

# Verify environment variable
grep "ASPNETCORE_ENVIRONMENT" publish-production/web.config
# Should show: value="Production"
```

## 8. Create ZIP Package (Optional)

```powershell
Compress-Archive -Path './publish-production/*' -DestinationPath './GCCWebApp-Production-Deploy.zip' -CompressionLevel Optimal -Force
```

## 9. Deployment Steps

1. **Login to Plesk** at AccuWebHosting
2. **Stop the application** in Plesk control panel
3. **Backup current files** on server (download or rename)
4. **Upload new files** via Plesk File Manager or FTP
5. **Verify appsettings.Production.json** connection string
6. **Restart the application** in Plesk

## 10. Notify Stakeholders

- [ ] Notify team of deployment start time
- [ ] Confirm expected downtime window
- [ ] Have rollback plan ready

---

## Quick Reference Commands

```bash
# Full build and package
Scripts/deployment/deploy-accuweb-production.cmd

# Or manual steps:
dotnet clean
dotnet restore
dotnet build -c Release
dotnet publish GCCWebApp.csproj -c Release -r win-x64 --self-contained false -o publish-production
mkdir publish-production/logs
```

## Red Flags - STOP Deployment If:

- Build has errors
- Critical tests failing
- web.config has fileExtensions/hiddenSegments sections
- Connection string points to wrong database
- No recent backup exists
