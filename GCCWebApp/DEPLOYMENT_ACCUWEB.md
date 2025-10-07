# AccuWeb Windows Hosting Deployment Guide

## Prerequisites

### AccuWeb Server Requirements
- **Windows Server** with IIS 10+
- **.NET 8.0 Runtime** (ASP.NET Core Runtime 8.0.x)
- **ASP.NET Core Module V2** (ANCM) for IIS
- **SQL Server** database (provided by AccuWeb or external)
- **URL Rewrite Module** for IIS (for HTTPS redirect)

### Local Development Requirements
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Access to AccuWeb hosting control panel

## Step 1: Build and Publish

### Using Visual Studio
1. Right-click on `GCCWebApp` project
2. Select **Publish**
3. Choose the **AccuWebProduction** profile
4. Click **Publish**

### Using Command Line
```bash
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"
dotnet publish -c Release -p:PublishProfile=AccuWebProduction
```

Published files will be in: `bin\Release\net8.0\publish\`

## Step 2: Configure Database Connection

### Option A: Using appsettings.Production.json (Recommended)
Edit `appsettings.Production.json` before publishing:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=your-sql-server.database.windows.net;Database=GCCDatabase;User Id=your_username;Password=your_password;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;"
}
```

### Option B: Using Environment Variables (More Secure)
In AccuWeb IIS Application Settings, add:
- Key: `ConnectionStrings__DefaultConnection`
- Value: `Server=your-sql-server;Database=GCCDatabase;User Id=...;Password=...;`

### Option C: Using web.config
Add to web.config inside `<aspNetCore>`:
```xml
<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  <environmentVariable name="ConnectionStrings__DefaultConnection" value="Server=...;Database=...;" />
</environmentVariables>
```

## Step 3: Configure Required Secrets

### Required Environment Variables
Set these in AccuWeb control panel or web.config:

```
ASPNETCORE_ENVIRONMENT=Production
GCC_JWT_SECRET=your-super-secret-jwt-key-minimum-64-characters-long-please-change-this
ConnectionStrings__DefaultConnection=Server=...;Database=GCCDatabase;...
```

### Optional Environment Variables
```
AzureAdB2C__ClientId=your-azure-ad-client-id
AzureAdB2C__ClientSecret=your-azure-ad-client-secret
AzureAdB2C__TenantId=your-tenant-id
SmtpSettings__Host=smtp.your-provider.com
SmtpSettings__Username=your-smtp-username
SmtpSettings__Password=your-smtp-password
```

## Step 4: Upload Files to AccuWeb

### Via FTP/SFTP
1. Connect to your AccuWeb FTP
2. Navigate to your web application root (usually `wwwroot` or `httpdocs`)
3. Upload all files from `bin\Release\net8.0\publish\` folder
4. Ensure `web.config` is in the root directory

### Via AccuWeb File Manager
1. Log into AccuWeb control panel
2. Use File Manager to upload files
3. Upload as ZIP and extract on server

## Step 5: Database Migration

### Run Migrations on Production Database
```bash
# From local machine with connection to production DB
dotnet ef database update --connection "Server=your-production-server;Database=GCCDatabase;..."

# OR: Let the app auto-migrate on first run (configured in Program.cs)
# The app will automatically run migrations on startup in Production environment
```

### Initial Database Setup
The application will:
- Auto-detect SQL Server from connection string
- Apply all migrations automatically on first run
- NOT seed test data in Production

## Step 6: Configure IIS Application Settings

### In AccuWeb Control Panel:

1. **Application Pool Settings**
   - .NET CLR Version: **No Managed Code**
   - Managed Pipeline Mode: **Integrated**
   - Enable 32-bit Applications: **False**
   - Identity: **ApplicationPoolIdentity** (default)

2. **Application Settings**
   - Physical Path: Point to your publish folder
   - Enable Preload: **True** (optional, for faster first request)

3. **Environment Variables** (add in Configuration → Application Settings):
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `GCC_JWT_SECRET` = (your 64+ character secret)
   - `ConnectionStrings__DefaultConnection` = (your SQL Server connection)

4. **Handler Mappings**
   - Verify `aspNetCore` handler is registered
   - Should point to `AspNetCoreModuleV2`

## Step 7: Verify Installation

### Check Required Files
Ensure these files exist in your web root:
- `GCCWebApp.dll`
- `web.config`
- `appsettings.json`
- `appsettings.Production.json`
- `wwwroot/` folder with all static files

### Test Endpoints
After deployment, test:
- `https://yourdomain.com/` → Should redirect to login or certificate view
- `https://yourdomain.com/health` → Should return healthy status
- `https://yourdomain.com/Account/Login` → Should show login page

### View Logs
Logs are written to: `logs\stdout_*.log`
Check these if the application fails to start.

## Step 8: Post-Deployment Configuration

### Create Admin User
1. Navigate to `/Account/Login`
2. Use test credentials or create first admin via direct DB insert:

```sql
INSERT INTO Users (Email, PasswordHash, FirstName, LastName, IsActive, CreatedDate)
VALUES ('admin@greencirclecertified.com', 'hash-from-bcrypt', 'Admin', 'User', 1, GETDATE());

INSERT INTO UserRoles (UserId, RoleId)
VALUES (1, (SELECT Id FROM Roles WHERE Name = 'Admin'));
```

### Update Production Settings
Edit `appsettings.Production.json` on server to:
- Add your production domain to `AllowedOrigins`
- Configure SMTP for email notifications
- Set appropriate rate limits
- Configure Azure AD B2C (if using)

## Security Checklist

- [ ] JWT secret is 64+ characters and unique
- [ ] Database credentials are not in source control
- [ ] HTTPS is enforced (via web.config redirect)
- [ ] Security headers are enabled (in web.config)
- [ ] Rate limiting is configured for APIs
- [ ] Detailed error messages are disabled (`FeatureFlags.EnableDetailedErrorMessages = false`)
- [ ] Swagger is disabled in production (`FeatureFlags.EnableSwagger = false`)
- [ ] SQL Server uses encrypted connection (`Encrypt=True`)
- [ ] File extensions blocked (.db, .config, .log, .bak)

## Troubleshooting

### App Doesn't Start
1. Check `logs\stdout_*.log` for errors
2. Verify .NET 8.0 Runtime is installed on server
3. Check Application Pool is started
4. Verify `web.config` has correct `processPath="dotnet"` and `arguments=".\GCCWebApp.dll"`

### Database Connection Errors
1. Verify connection string is correct
2. Check SQL Server allows remote connections
3. Verify firewall rules allow connection
4. Test connection from server using SQL Server Management Studio

### 500 Internal Server Error
1. Enable detailed errors temporarily (set `ASPNETCORE_ENVIRONMENT=Development`)
2. Check event viewer on Windows Server
3. Check stdout logs
4. Verify all DLL dependencies are uploaded

### Static Files Not Loading
1. Check `wwwroot` folder was uploaded
2. Verify IIS static content serving is enabled
3. Check file permissions on server

## Updating the Application

1. Build and publish new version locally
2. Stop IIS application pool on server
3. Upload new files (overwrite existing)
4. Restart IIS application pool
5. Verify health endpoint

## Rollback Plan

1. Keep previous publish folder as backup
2. If issues occur, stop app pool
3. Replace files with previous version
4. Restart app pool
5. If database changes were made, restore database backup

## Support

For AccuWeb-specific issues:
- AccuWeb Support: https://www.accuwebhosting.com/support
- .NET Core Hosting: https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/

For application issues:
- Check CLAUDE.md for application architecture
- Review logs in `logs/` directory
- Check application health endpoint: `/health`
