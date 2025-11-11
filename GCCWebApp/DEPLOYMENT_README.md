# GCC Web Application - Deployment Package

**Version**: 1.0
**Build Date**: October 31, 2025
**Build Configuration**: Release (Framework-Dependent)

## Package Overview

This deployment consists of **TWO SEPARATE PACKAGES**:

### 1. GCCWebApp-WebApp-YYYYMMDD-HHMMSS.zip (~93 MB)
- **Contains**: Complete web application ready to deploy
- **Deploy to**: Web server (IIS, Linux + Nginx, or Kestrel)
- **Requirements**: .NET 8.0 ASP.NET Core Runtime
- **Documentation**: See `DEPLOYMENT_GUIDE.md` inside ZIP

### 2. GCCWebApp-DatabaseScripts-YYYYMMDD-HHMMSS.zip (~6 KB)
- **Contains**: SQL schema scripts for database setup
- **Deploy to**: SQL Server (production server or separate database server)
- **Requirements**: SQL Server 2019+ or Azure SQL Database
- **Documentation**: See `README.md` inside ZIP

## Deployment Order

**IMPORTANT**: Deploy in this order:

### Step 1: Database Setup (FIRST)
1. Extract `GCCWebApp-DatabaseScripts-YYYYMMDD-HHMMSS.zip`
2. Follow instructions in `README.md`
3. Run `00_Complete_Schema.sql` on your SQL Server
4. Verify database created successfully

### Step 2: Web Application Setup (SECOND)
1. Extract `GCCWebApp-WebApp-YYYYMMDD-HHMMSS.zip`
2. Configure `appsettings.Production.json` (database connection, JWT secret)
3. Follow `DEPLOYMENT_GUIDE.md` for your platform:
   - **Windows**: IIS deployment instructions
   - **Linux**: systemd + Nginx instructions
   - **Testing**: Kestrel standalone instructions
4. Deploy and start application

### Step 3: Verification
1. Browse to `/health` - should return "Healthy"
2. Browse to `/Account/Login` - login page should load
3. Login with default admin credentials (see DEPLOYMENT_GUIDE.md)
4. **IMMEDIATELY** change default passwords

## Quick Start (Production Environment)

### Prerequisites Checklist
- [ ] .NET 8.0 ASP.NET Core Runtime installed on web server
- [ ] SQL Server 2019+ accessible (production server: 68.71.129.120,1533)
- [ ] Database credentials ready (User: GCC_adminxyz123)
- [ ] SSL certificate ready (for HTTPS)
- [ ] Firewall rules configured (allow 80/443, allow SQL port 1533)

### Database Deployment (5 minutes)
```bash
# Connect to SQL Server
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password]

# Run in SQL prompt:
CREATE DATABASE gccwebap_certificates;
GO
EXIT

# Deploy schema
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password] -d gccwebap_certificates -i 00_Complete_Schema.sql
```

### Web Application Deployment (10 minutes)

**IIS (Windows)**:
1. Extract WebApp ZIP to `C:\inetpub\wwwroot\gccwebapp\`
2. Edit `appsettings.Production.json` - update connection string
3. Create IIS Application Pool (No Managed Code)
4. Create IIS Site pointing to folder
5. Set environment: `ASPNETCORE_ENVIRONMENT=Production`
6. Start site

**Linux + Nginx**:
1. Extract WebApp ZIP to `/var/www/gccwebapp/`
2. Edit `appsettings.Production.json` - update connection string
3. Create systemd service (see DEPLOYMENT_GUIDE.md)
4. Configure Nginx reverse proxy
5. Start service: `sudo systemctl start gccwebapp`

## Production Environment Details

### Database Server (AccuWebHosting)
- **Server**: 68.71.129.120,1533
- **Database**: gccwebap_certificates
- **Username**: GCC_adminxyz123
- **Password**: [Provided separately]

### Connection String Template
```json
"DefaultConnection": "Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=YOUR_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=false;Encrypt=true;"
```

### Default Application Credentials (CHANGE IMMEDIATELY)
- **Admin**: admin@greencirclecertified.com / Admin123!
- **Analyst**: analyst@greencirclecertified.com / Analyst123!
- **External**: partner@amazon.com / Partner123!

## Package Contents Detail

### WebApp Package (~93 MB) Contains:
```
WebApp/
├── DEPLOYMENT_GUIDE.md         # Detailed deployment instructions
├── GCCWebApp.dll               # Main application assembly
├── appsettings.Production.json # Production configuration (MUST EDIT)
├── appsettings.*.json          # Other environment configs
├── web.config                  # IIS configuration
├── wwwroot/                    # Static files (CSS, JS, images)
├── Views/                      # MVC Razor views
├── LatoFont/                   # PDF generation fonts
└── [DLL files]                 # .NET and NuGet dependencies
```

### DatabaseScripts Package (~6 KB) Contains:
```
DatabaseScripts/
├── README.md                   # Database deployment instructions
└── 00_Complete_Schema.sql      # Complete idempotent database schema
```

## Key Features Included

- **Certificate Management**: 14-state workflow (Draft → Published → Archived)
- **Product ID Management**: Support for ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN
- **Role-Based Access**: Admin, Analyst, External roles with granular permissions
- **Tile Views**: Public, Internal, External certificate browsing
- **Infinite Scroll**: No pagination, smooth browsing experience
- **Search & Filters**: Database-driven filters, autocomplete search
- **Authentication**: Cookie auth + Azure AD B2C integration
- **API**: RESTful API with Swagger docs, JWT + API Key auth
- **Exports**: CSV, Excel, PDF certificate downloads
- **Audit Trail**: Complete activity logging

## Security Notes

### Before Going Live:
1. **Change all default passwords** via application UI
2. **Generate new JWT secret** (64+ characters) in appsettings.Production.json
3. **Configure HTTPS** with valid SSL certificate
4. **Set strong database password** (if not already done)
5. **Enable firewall rules**:
   - Allow: 80 (HTTP), 443 (HTTPS)
   - Allow: 1533 (SQL Server from app server only)
   - Block: 5000 (Kestrel direct access if behind proxy)
6. **Review CORS settings** in appsettings.Production.json
7. **Set up database backups** (daily recommended)

### OWASP Top 10 Protections:
- ✅ SQL Injection: Entity Framework parameterized queries
- ✅ XSS: Razor view encoding, Content Security Policy headers
- ✅ CSRF: Anti-forgery tokens on all forms
- ✅ Authentication: ASP.NET Core Identity + Azure AD B2C
- ✅ Authorization: Role-based + policy-based checks
- ✅ HTTPS: HSTS headers, forced HTTPS redirection
- ✅ Logging: Comprehensive audit trail
- ✅ Input Validation: Model validation + sanitization

## Troubleshooting

### Application won't start
- Check .NET 8.0 Runtime installed: `dotnet --list-runtimes`
- Check logs in `logs/` folder
- Verify database connection string
- Check Windows Event Viewer (IIS) or `journalctl` (Linux)

### Database connection fails
- Test SQL connection: `sqlcmd -S [server] -U [user] -P [password]`
- Verify firewall allows port 1533
- Check SQL Server allows remote connections
- Verify credentials in appsettings.Production.json

### 500 Internal Server Error
- Enable detailed errors in appsettings.Production.json (temporarily)
- Check application logs in `logs/` folder
- Verify database migrations applied (check `__EFMigrationsHistory` table)

### Static files not loading
- Verify `wwwroot/` folder deployed correctly
- Check IIS/Nginx serving static files
- Clear browser cache (Ctrl+F5)
- Check browser DevTools Network tab (F12)

## Rollback Procedure

1. **Stop application** (IIS/systemd)
2. **Restore database backup**:
   ```sql
   RESTORE DATABASE gccwebap_certificates
   FROM DISK = 'backup_path.bak' WITH REPLACE;
   ```
3. **Restore previous application files**
4. **Restart application**

## Support & Documentation

### Included Documentation:
- `DEPLOYMENT_GUIDE.md` (in WebApp ZIP) - Detailed deployment steps
- `README.md` (in DatabaseScripts ZIP) - Database deployment steps
- `/swagger` endpoint - Interactive API documentation
- Application logs in `logs/` folder

### Key URLs After Deployment:
- **Health Check**: `/health`
- **Login**: `/Account/Login`
- **Public Tile View**: `/Certificate/CertificateTileView`
- **Admin Dashboard**: `/Dashboard`
- **User Management**: `/Admin/UserManagement`
- **API Documentation**: `/swagger`

### Test Endpoints:
```bash
# Health check
curl http://your-server/health

# API test (requires authentication)
curl http://your-server/swagger
```

## Monitoring Recommendations

### Application Monitoring:
- Review logs daily: `logs/` folder
- Monitor disk space (logs rotate daily)
- Check application pool/service status
- Monitor memory usage

### Database Monitoring:
- Daily backups (recommended)
- Monitor database size growth
- Review slow query logs
- Check `__EFMigrationsHistory` for schema version

### Performance Monitoring:
- IIS/Nginx access logs
- Application Insights (if configured)
- SQL Server performance counters
- Response time monitoring

## Technical Support

For deployment issues:
1. Check application logs in `logs/` folder first
2. Review DEPLOYMENT_GUIDE.md for your platform
3. Verify all prerequisites installed
4. Test database connection separately
5. Check firewall and network connectivity

## Version History

- **1.0.0** (October 31, 2025) - Initial production release
  - Complete certificate management system
  - Multi-role authentication and authorization
  - Public/Internal/External tile views
  - Product ID management
  - API with comprehensive documentation
  - Ready for AccuWebHosting deployment

---

**Important**: Always backup your database before deploying updates!
