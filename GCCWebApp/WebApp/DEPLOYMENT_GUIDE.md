# GCC Web Application - Deployment Guide

**Version**: 1.0
**Build Date**: October 31, 2025
**Framework**: .NET 8.0 (ASP.NET Core MVC)

## Prerequisites

### Server Requirements
- **OS**: Windows Server 2016+ or Linux (Ubuntu 20.04+)
- **.NET Runtime**: .NET 8.0 Runtime (ASP.NET Core) - **REQUIRED**
- **Database**: SQL Server 2019+ (must be deployed separately)
- **Memory**: 2 GB RAM minimum (4 GB recommended)
- **Disk**: 500 MB free space

### Install .NET 8.0 Runtime
Download from: https://dotnet.microsoft.com/download/dotnet/8.0
- Windows: Install "ASP.NET Core Runtime 8.0 + Hosting Bundle"
- Linux: Install "ASP.NET Core Runtime 8.0"

Verify installation:
```bash
dotnet --list-runtimes
# Should show: Microsoft.AspNetCore.App 8.0.x
```

## Configuration

### 1. Database Connection
Edit `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=YOUR_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=false;Encrypt=true;"
  }
}
```

**Important**: Database must be deployed first using the SQL scripts from the DatabaseScripts package.

### 2. JWT Secret Key
Generate a secure 64+ character key:

```bash
# PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})

# Linux
openssl rand -base64 64
```

Update in `appsettings.Production.json`:
```json
{
  "JwtSettings": {
    "SecretKey": "YOUR_GENERATED_SECRET_KEY_HERE_MINIMUM_64_CHARACTERS"
  }
}
```

### 3. Azure AD B2C (Optional)
If using Azure AD B2C authentication, update:
```json
{
  "AzureAdB2C": {
    "Instance": "https://yourb2c.b2clogin.com",
    "Domain": "yourb2c.onmicrosoft.com",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

### 4. Environment Variable
Set the environment to Production:

```bash
# Windows (Command Prompt)
set ASPNETCORE_ENVIRONMENT=Production

# Windows (PowerShell)
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Linux
export ASPNETCORE_ENVIRONMENT=Production
```

## Deployment Options

### Option A: IIS Deployment (Windows)

1. **Install Prerequisites**:
   - .NET 8.0 Hosting Bundle (includes Runtime + IIS integration)
   - IIS 10+ with ASP.NET Core Module

2. **Create Application Pool**:
   - Open IIS Manager
   - Create new Application Pool:
     - Name: `GCCWebApp`
     - .NET CLR Version: `No Managed Code`
     - Managed Pipeline Mode: `Integrated`

3. **Create Website/Application**:
   - Physical path: Point to this folder
   - Application Pool: `GCCWebApp`
   - Binding: HTTP port 80 (or HTTPS port 443)

4. **Set Permissions**:
   - Grant `IIS_IUSRS` read/execute on this folder
   - Grant `IIS_IUSRS` write permission on `logs` folder (created on first run)

5. **Set Environment Variable in IIS**:
   - Select Application in IIS
   - Configuration Editor
   - Section: `system.webServer/aspNetCore`
   - Add environment variable: `ASPNETCORE_ENVIRONMENT=Production`

6. **Start Application**:
   - Start the Application Pool
   - Browse to your website URL

### Option B: Kestrel (Standalone)

Run the application directly using Kestrel web server:

```bash
# Default port 5000
dotnet GCCWebApp.dll

# Custom port
dotnet GCCWebApp.dll --urls "http://0.0.0.0:8080"

# HTTPS
dotnet GCCWebApp.dll --urls "https://0.0.0.0:443;http://0.0.0.0:80"
```

**Note**: For production, run Kestrel behind a reverse proxy (IIS, Nginx, Apache).

### Option C: Linux with systemd + Nginx

1. **Copy files to server**:
   ```bash
   scp -r * user@server:/var/www/gccwebapp/
   ```

2. **Create systemd service** (`/etc/systemd/system/gccwebapp.service`):
   ```ini
   [Unit]
   Description=GCC Web Application
   After=network.target

   [Service]
   Type=notify
   WorkingDirectory=/var/www/gccwebapp
   ExecStart=/usr/bin/dotnet /var/www/gccwebapp/GCCWebApp.dll
   Restart=always
   RestartSec=10
   KillSignal=SIGINT
   SyslogIdentifier=gccwebapp
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

   [Install]
   WantedBy=multi-user.target
   ```

3. **Start service**:
   ```bash
   sudo systemctl enable gccwebapp
   sudo systemctl start gccwebapp
   sudo systemctl status gccwebapp
   ```

4. **Configure Nginx** (`/etc/nginx/sites-available/gccwebapp`):
   ```nginx
   server {
       listen 80;
       server_name your-domain.com;

       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```

   Enable site:
   ```bash
   sudo ln -s /etc/nginx/sites-available/gccwebapp /etc/nginx/sites-enabled/
   sudo nginx -t
   sudo systemctl reload nginx
   ```

## Post-Deployment Verification

### 1. Health Check
Browse to: `http://your-server/health`
Expected response: `Healthy`

### 2. Login Page
Browse to: `http://your-server/Account/Login`

**Default Admin Credentials** (CHANGE IMMEDIATELY):
- Email: `admin@greencirclecertified.com`
- Password: `Admin123!`

### 3. API Documentation
Browse to: `http://your-server/swagger`

### 4. Check Application Logs
- Logs are written to `logs/` folder in application directory
- Check for startup errors or warnings

## Security Checklist

- [ ] Change default admin password
- [ ] Update JWT secret key (64+ characters)
- [ ] Configure HTTPS with valid SSL certificate
- [ ] Set strong database password
- [ ] Enable firewall rules (allow 80/443, block 5000)
- [ ] Review and restrict CORS settings if needed
- [ ] Enable HSTS (already configured in app)
- [ ] Regular database backups configured

## Default User Accounts

After first run, these accounts are created:

| Email | Password | Role | Purpose |
|-------|----------|------|---------|
| admin@greencirclecertified.com | Admin123! | Admin | Full system access |
| analyst@greencirclecertified.com | Analyst123! | Analyst | Certificate management |
| partner@amazon.com | Partner123! | External | View assigned certificates |

**IMPORTANT**: Change all default passwords immediately after first login.

## Key Routes

- **Public Tile View**: `/Certificate/CertificateTileView`
- **Internal Tile View**: `/Certificate/CertificateTileViewInternal` (authenticated)
- **External Tile View**: `/Certificate/CertificateTileViewExternal` (authenticated)
- **Login**: `/Account/Login`
- **Admin Dashboard**: `/Dashboard`
- **User Management**: `/Admin/UserManagement`
- **API Docs**: `/swagger`
- **Health**: `/health`

## Troubleshooting

### Application won't start

**Check .NET Runtime**:
```bash
dotnet --list-runtimes
```
Must show `Microsoft.AspNetCore.App 8.0.x`

**Check logs**:
- Look in `logs/` folder for error details
- Windows Event Viewer (if using IIS)
- Linux: `journalctl -u gccwebapp -f`

### Database connection fails

**Test connection**:
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password] -d gccwebap_certificates -Q "SELECT @@VERSION"
```

**Check**:
- Firewall allows SQL Server port (1533)
- SQL Server allows remote connections
- Credentials in `appsettings.Production.json` are correct
- Database exists and schema is deployed

### 500 Internal Server Error

1. Enable detailed errors temporarily in `appsettings.Production.json`:
   ```json
   {
     "DetailedErrors": true,
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

2. Check application logs in `logs/` folder

3. Verify database connection and migrations applied

### Static files not loading (CSS/JS missing)

**IIS**: Ensure Static File Handler is enabled
**Nginx**: Verify static file serving configured
**Both**: Check browser DevTools Network tab (F12) for 404 errors

Clear browser cache and hard refresh (Ctrl+F5)

## Rollback Procedure

1. Stop application (IIS/systemd service)
2. Restore previous application files from backup
3. Restore database backup (if schema changed)
4. Restart application

## Support

For issues:
- Check application logs in `logs/` folder
- Review database connection in `appsettings.Production.json`
- Verify all prerequisites installed
- Check firewall and network connectivity

## Version Information

- **Application**: GCC Web Application v1.0
- **Framework**: .NET 8.0 / ASP.NET Core MVC
- **Database**: SQL Server 2019+
- **Build Configuration**: Release (Framework-Dependent)
