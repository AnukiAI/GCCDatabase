# GCC Web Application - Plesk MS SQL Deployment Guide

**Target:** Plesk (gccwebapp.com)  
**Database:** MS SQL Server (68.71.129.120,1533)  
**Database Name:** gccwebap_gccdb  
**Database User:** adminGCC

---

## Quick Start: Deployment Steps

### 1. Clear httpdocs Directory in Plesk

**Keep:**
- `.well-known/` folder (SSL verification)

**Delete Everything Else:**
- `App_Data/`
- `index.html`
- `web.config`
- `.user.ini`

### 2. Upload Deployment Package

1. Go to **Websites & Domains > gccwebapp.com > Files**
2. Navigate to `httpdocs` folder
3. Upload `GCCWebApp-TestDeploy.tar.gz`
4. Extract files to current directory
5. Delete the `.tar.gz` after extraction

### 3. Configure appsettings.Production.json

**Edit in Plesk File Manager:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=68.71.129.120,1533;Database=gccwebap_gccdb;User Id=adminGCC;Password=YOUR_ACTUAL_PASSWORD;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
  },
  
  "DatabaseProvider": "sqlserver",
  
  "Jwt": {
    "Key": "GENERATE_64_CHAR_SECRET_KEY",
    "Issuer": "GCCWebApp-Production",
    "Audience": "GCCWebApp-Production",
    "ExpiryMinutes": 60
  },
  
  "AllowedOrigins": [
    "https://gccwebapp.com",
    "https://www.gccwebapp.com"
  ],
  
  "FeatureFlags": {
    "EnableSwagger": false,
    "EnableDetailedErrorMessages": false
  }
}
```

**Generate JWT Key:**
```powershell
# 64 character random key
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

### 4. Configure Plesk .NET Core Settings

1. **Websites & Domains > gccwebapp.com > .NET Core Settings**
2. Set:
   - Application Root: `httpdocs`
   - Application Startup File: `GCCWebApp.dll`
   - Environment Variables:
     ```
     ASPNETCORE_ENVIRONMENT=Production
     ```

### 5. Initialize Database

**Option A: Automatic (Recommended)**
The app will run migrations on first start.

**Option B: Manual SSH**
```bash
cd /var/www/vhosts/gccwebapp.com/httpdocs
export ConnectionStrings__DefaultConnection="Server=68.71.129.120,1533;Database=gccwebap_gccdb;User Id=adminGCC;Password=YOUR_PASSWORD;..."
dotnet GCCWebApp.dll --migrate
```

### 6. Restart Application in Plesk

### 7. Test Deployment

- Health: https://gccwebapp.com/health
- Public: https://gccwebapp.com/Certificate/CertificateTileView
- Login: https://gccwebapp.com/Account/Login (admin@greencirclecertified.com / Admin123!)

---

## Important Security Settings

⚠️ **MUST DO BEFORE PRODUCTION:**
1. Change JWT secret key (64+ characters)
2. Update database password in connection string
3. Change default admin password after first login
4. Set EnableSwagger to false
5. Verify HTTPS is enforced

---

## Default Test Credentials

**CHANGE THESE IMMEDIATELY:**
- Admin: admin@greencirclecertified.com / Admin123!
- Analyst: analyst@greencirclecertified.com / Analyst123!
- External: partner@amazon.com / Partner123!

---

## Troubleshooting

**Application Won't Start:**
```bash
# Check logs in Plesk: Websites & Domains > Logs
# Enable detailed logging in web.config:
<aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout">
```

**Database Connection Failed:**
- Verify password is correct
- Check firewall allows port 1533
- Verify TrustServerCertificate=True if using self-signed cert
- Test connection: `sqlcmd -S 68.71.129.120,1533 -U adminGCC -P 'password'`

**500 Errors:**
- Verify ASPNETCORE_ENVIRONMENT is set
- Check JWT key is set (64+ chars)
- Check database migrations ran
- Review logs in httpdocs/logs/

---

**Deployment Package:** GCCWebApp-TestDeploy.tar.gz (59 MB)  
**Location:** Working directory  
**Build Date:** October 7, 2025
