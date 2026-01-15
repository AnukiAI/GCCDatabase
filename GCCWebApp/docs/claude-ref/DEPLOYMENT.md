# Deployment Reference

## Production Environment
- Host: AccuWebHosting (Plesk)
- URL: https://gccwebapp.com
- Database: 68.71.129.120,1533

## web.config (CRITICAL)

**Plesk has server-level IIS config that conflicts with these sections.**

### REMOVE these (cause HTTP 500.19):
```xml
<!-- DO NOT USE ON PLESK -->
<fileExtensions>
  <add fileExtension=".db" allowed="false" />
</fileExtensions>
<hiddenSegments>
  <add segment="bin" />
</hiddenSegments>
```

### Correct web.config:
```xml
<security>
  <requestFiltering>
    <requestLimits maxAllowedContentLength="104857600" />
    <!-- NO fileExtensions or hiddenSegments -->
  </requestFiltering>
</security>
```

## Deployment Checklist

```bash
# 1. Check SOURCE web.config (GCCWebApp/web.config)
# Verify NO <fileExtensions> or <hiddenSegments>

# 2. Clean publish
dotnet publish GCCWebApp.csproj -c Release -r win-x64 --self-contained false -o publish-production

# 3. Verify output web.config
# Double-check publish-production/web.config

# 4. Create logs folder
mkdir -p publish-production/logs

# 5. Create ZIP
powershell -Command "Compress-Archive -Path './publish-production/*' -DestinationPath './GCCWebApp-Production-Deploy.zip' -CompressionLevel Optimal -Force"
```

## Common Issues

### HTTP 500.19 - Duplicate collection entry
**Cause**: `<fileExtensions>` or `<hiddenSegments>` in web.config
**Fix**: Remove those sections from SOURCE web.config

### HTTP 500 - Data Protection Error
**Cause**: Azure Blob placeholders in appsettings.Production.json
**Fix**: Set `"PersistKeysToAzureBlobStorage": false`

### Missing Logs Folder
**Cause**: web.config requires logs/ folder
**Fix**: Create folder, set permissions: `icacls logs /grant "IIS_IUSRS:(OI)(CI)F"`

### Login Failures
**Cause**: BCrypt password hashes don't match
**Fix**: Run password reset script
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "Scripts\RESET_PRODUCTION_PASSWORDS.sql"
```

## Docker (Alternative)

```bash
./docker-run-production.sh  # SQLite on port 5105
docker-compose up -d        # Full stack with PostgreSQL
./docker-build.cmd          # Interactive menu
```

Health check: `/health`

## Environment Variables

```bash
ASPNETCORE_ENVIRONMENT=Production
GCC_JWT_SECRET=[64+ characters]
GCC_DB_CONNECTION=[connection string]
```

## Troubleshooting Tools

| Tool | Location |
|------|----------|
| Password reset SQL | Scripts/RESET_PRODUCTION_PASSWORDS.sql |
| BCrypt hash generator | Scripts/HashGenerator/ |
| Login troubleshooting | Scripts/FIX_PRODUCTION_LOGIN.md |
| Auth diagnostic | /Account/TestAuth?username=X&password=Y |
