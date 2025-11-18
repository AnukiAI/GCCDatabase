# AccuWeb Hosting Deployment Guide for GCC Web Application

## Overview
AccuWeb Hosting provides enterprise-grade infrastructure at budget-friendly pricing ($12/month), making it the best value option for ASP.NET Core 8.0 applications.

## Why AccuWeb?

### Value Proposition
- **Price**: $12/month (vs $30-75 for Azure S1)
- **Performance**: Enterprise SSD hardware
- **Support**: Expert .NET hosting support
- **Features**: Plesk panel, SSL, backups included
- **Perfect for**: Small to medium ASP.NET MVC apps

### What You Get
- Plesk Control Panel
- .NET 8.0 Runtime support
- SQL Server or MySQL databases
- Daily automated backups
- Free SSL certificates (Let's Encrypt)
- Email hosting
- FTP/SSH access
- 99.9% uptime SLA

## Prerequisites

1. **AccuWeb Hosting Account**
   - Sign up at https://www.accuwebhosting.com
   - Choose Windows hosting plan with Plesk
   - Verify .NET 8.0 support

2. **Domain Configuration**
   - Point domain to AccuWeb nameservers
   - Or use A record to server IP

3. **Deployment Package**
   - Already created: `gccwebapp-dotnet8.tar.gz` ✅
   - Or use FTP/Git deployment

## Deployment Methods

### Method 1: Plesk .NET Toolkit (Recommended)

#### Step 1: Access Plesk Panel
```
URL: https://your-server-ip:8443
Username: [provided by AccuWeb]
Password: [provided by AccuWeb]
```

#### Step 2: Create/Configure Domain
1. Navigate to **Websites & Domains**
2. Click **Add Domain** or select existing domain
3. Ensure domain is active and accessible

#### Step 3: Install .NET Toolkit Extension
1. Go to **Extensions** in Plesk
2. Search for ".NET Toolkit" or ".NET Core"
3. Click **Install** (should be available on AccuWeb servers)

#### Step 4: Upload Application
**Option A: Via File Manager**
1. Navigate to **Files** → **File Manager**
2. Go to `httpdocs/` directory
3. Upload `gccwebapp-dotnet8.tar.gz`
4. Extract the archive

**Option B: Via FTP**
```bash
# Using FileZilla or command line
ftp your-domain.com
# Login with credentials
cd httpdocs
put gccwebapp-dotnet8.tar.gz
# Extract via Plesk File Manager
```

**Option C: Via SSH** (if enabled)
```bash
ssh username@your-domain.com
cd httpdocs
scp user@local-machine:/path/to/gccwebapp-dotnet8.tar.gz .
tar -xzf gccwebapp-dotnet8.tar.gz
```

#### Step 5: Configure .NET Toolkit
1. Go to **Websites & Domains** → Your Domain
2. Click **.NET Toolkit** icon
3. Configure settings:

```
Application Root: /httpdocs/
Startup File: GCCWebApp.dll
Environment: Production
Framework Version: .NET 8.0
Process Manager: systemd (or Plesk default)
```

4. Click **Apply**

#### Step 6: Set Environment Variables
In Plesk .NET Toolkit → **Environment Variables**:

```
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = http://localhost:5000
ConnectionStrings__DefaultConnection = [your database connection]
GCC_JWT_SECRET = [your 64+ character secret key]
DatabaseProvider = SqlServer
Features__EnableAzureAdB2C = false
Features__EnableEmailNotifications = false
Features__EnableDataExport = true
```

#### Step 7: Configure Database

**For SQL Server (Windows hosting):**
1. Go to **Databases** → **Add Database**
2. Create new database: `GCCDatabase`
3. Create user with full permissions
4. Note connection string:
```
Server=localhost;Database=GCCDatabase;User Id=your_user;Password=your_password;TrustServerCertificate=True;
```

**For MySQL (if SQL Server unavailable):**
1. Create MySQL database
2. Update connection string:
```
Server=localhost;Database=gccdb;User=your_user;Password=your_password;
```
3. Update `appsettings.Production.json` to use MySQL provider

#### Step 8: Run Database Migrations
**Via SSH:**
```bash
cd /var/www/vhosts/your-domain.com/httpdocs
dotnet ef database update
```

**Via local migration script:**
```sql
-- Upload and run migration SQL script
-- Generated from: dotnet ef migrations script
```

#### Step 9: Configure SSL Certificate
1. Go to **SSL/TLS Certificates**
2. Click **Install Free Let's Encrypt Certificate**
3. Select your domain and www subdomain
4. Click **Get it free**
5. Enable **Redirect HTTP to HTTPS**

#### Step 10: Start Application
1. In **.NET Toolkit**, click **Restart Application**
2. Monitor logs for any startup errors
3. Visit https://your-domain.com

### Method 2: Traditional IIS Deployment (Alternative)

If .NET Toolkit is not available:

#### Step 1: Configure Application Pool
1. Go to **IIS Settings** in Plesk
2. Create new Application Pool
3. Set .NET CLR Version to **No Managed Code**
4. Set Managed Pipeline Mode to **Integrated**

#### Step 2: Deploy Application
1. Upload files to `httpdocs/`
2. Create `web.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\GCCWebApp.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

3. Ensure AspNetCoreModuleV2 is installed on server

## Post-Deployment Configuration

### Security Hardening
1. **Disable Directory Browsing**
   - IIS Settings → Directory Security → Disable

2. **Configure Request Filtering**
   - Block dangerous file extensions
   - Set maximum request size

3. **Enable HSTS**
   - Add to `appsettings.Production.json`:
```json
"Kestrel": {
  "Endpoints": {
    "Https": {
      "Url": "https://*:443",
      "Protocols": "Http1AndHttp2"
    }
  }
}
```

### Performance Optimization
1. **Enable Response Compression**
   - Already configured in `Program.cs`

2. **Configure Static File Caching**
   - In IIS, set cache headers for `wwwroot/`

3. **Database Connection Pooling**
   - Already enabled in connection string

### Monitoring and Logging
1. **Enable Application Logging**
   - Logs stored in `logs/` directory
   - Configure log rotation in Plesk

2. **Set Up Uptime Monitoring**
   - Use Plesk monitoring tools
   - Or external service (UptimeRobot, Pingdom)

3. **Health Check Endpoint**
   - Monitor: `https://your-domain.com/health`

## Verification Checklist

### Application Functionality
- [ ] Home page loads correctly
- [ ] HTTPS redirect working
- [ ] Login functionality operational
- [ ] Certificate tile view displaying
- [ ] Search and filters working
- [ ] PDF downloads functional
- [ ] API endpoints responding

### Performance Tests
- [ ] Page load time < 3 seconds
- [ ] API response time < 1 second
- [ ] No 500 errors in logs
- [ ] Memory usage stable

### Security Checks
- [ ] HTTPS enforced (no HTTP access)
- [ ] SSL certificate valid
- [ ] No sensitive data in error messages
- [ ] Database connections secure
- [ ] JWT secret properly configured

## Database Management

### Backup Strategy
1. **Automated Backups** (via Plesk)
   - Go to **Backup Manager**
   - Schedule daily backups
   - Retain 7 days of backups

2. **Manual Backup**
```bash
# SQL Server
sqlcmd -S localhost -U your_user -P your_pass -Q "BACKUP DATABASE GCCDatabase TO DISK='backup.bak'"

# MySQL
mysqldump -u your_user -p gccdb > backup.sql
```

### Restore Procedure
```bash
# SQL Server
sqlcmd -S localhost -U your_user -P your_pass -Q "RESTORE DATABASE GCCDatabase FROM DISK='backup.bak'"

# MySQL
mysql -u your_user -p gccdb < backup.sql
```

## Troubleshooting

### Application Won't Start

**Check 1: Review Logs**
```bash
# Location: httpdocs/logs/stdout_*.log
tail -f logs/stdout_*.log
```

**Check 2: Verify .NET Runtime**
```bash
dotnet --version
# Should show 8.0.x
```

**Check 3: Database Connection**
- Test connection string in Plesk
- Verify firewall allows database access

### 500 Internal Server Error

**Solution 1: Check Application Logs**
- Review `logs/` directory
- Look for startup exceptions

**Solution 2: Verify Permissions**
```bash
# Set proper ownership
chown -R username:username /var/www/vhosts/your-domain.com/httpdocs
chmod -R 755 /var/www/vhosts/your-domain.com/httpdocs
```

**Solution 3: Check Environment Variables**
- Ensure all required variables are set
- Verify JWT secret is 64+ characters

### SSL/HTTPS Issues

**Solution 1: Renew Certificate**
- Let's Encrypt certificates expire after 90 days
- Plesk should auto-renew, but check manually

**Solution 2: Force HTTPS Redirect**
Add to `web.config`:
```xml
<system.webServer>
  <rewrite>
    <rules>
      <rule name="HTTPS Redirect" stopProcessing="true">
        <match url="(.*)" />
        <conditions>
          <add input="{HTTPS}" pattern="off" />
        </conditions>
        <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
      </rule>
    </rules>
  </rewrite>
</system.webServer>
```

### Performance Issues

**Solution 1: Increase Application Pool Memory**
- IIS Settings → Application Pool → Advanced Settings
- Increase memory limits

**Solution 2: Enable Output Caching**
- Configure in IIS for static content

**Solution 3: Database Optimization**
- Ensure indexes are created
- Run query performance analysis

## Scaling Options

### Vertical Scaling
**Upgrade AccuWeb Plan:**
- Business Plan: $20-30/month (more CPU/RAM)
- Enterprise Plan: Custom pricing (dedicated resources)

### Horizontal Scaling
**If traffic exceeds single server:**
1. Move to Azure App Service S1 (auto-scaling)
2. Or set up load balancer with multiple AccuWeb servers

## Cost Comparison

| Scenario | AccuWeb | Azure S1 | Savings |
|----------|---------|----------|---------|
| Small app (< 10K users/month) | $12/month | $70/month | $58/month |
| Medium app (10-50K users/month) | $25/month | $70/month | $45/month |
| Large app (> 50K users/month) | Need to scale | $70-150/month | Consider Azure |

**Breakpoint:** AccuWeb is cost-effective until you need auto-scaling for variable traffic.

## Maintenance Schedule

### Daily
- Monitor application logs
- Check health endpoint

### Weekly
- Review error logs
- Check disk space
- Verify backups completed

### Monthly
- Apply security patches
- Review performance metrics
- Update SSL certificates (if needed)

## Support Resources

### AccuWeb Support
- **Support Portal**: https://www.accuwebhosting.com/support
- **Live Chat**: Available 24/7
- **Phone**: Check your account dashboard
- **Email**: support@accuwebhosting.com

### Application Support
- **Health Check**: https://your-domain.com/health
- **Application Logs**: `httpdocs/logs/`
- **Plesk Documentation**: https://docs.plesk.com

## Quick Command Reference

```bash
# Check application status
systemctl status your-app-service

# Restart application (via Plesk .NET Toolkit)
# Use web interface: Websites & Domains → .NET Toolkit → Restart

# View logs
tail -f /var/www/vhosts/your-domain.com/httpdocs/logs/stdout_*.log

# Database backup (SQL Server)
sqlcmd -S localhost -U user -P pass -Q "BACKUP DATABASE GCCDatabase TO DISK='backup.bak'"

# Database backup (MySQL)
mysqldump -u user -p gccdb > backup.sql

# Check disk space
df -h

# Check memory usage
free -m
```

## Migration to AccuWeb Checklist

- [ ] Sign up for AccuWeb hosting account
- [ ] Configure domain in Plesk
- [ ] Install .NET Toolkit extension
- [ ] Upload application files
- [ ] Create and configure database
- [ ] Set environment variables
- [ ] Configure SSL certificate
- [ ] Run database migrations
- [ ] Test application functionality
- [ ] Configure backups
- [ ] Set up monitoring
- [ ] Update DNS to point to AccuWeb

## Final Deployment Verification

1. **Smoke Test**
   ```bash
   curl https://your-domain.com/health
   # Should return: Healthy
   ```

2. **Load Test** (optional)
   ```bash
   # Use Apache Bench or similar tool
   ab -n 1000 -c 10 https://your-domain.com/
   ```

3. **Security Scan**
   - SSL Labs: https://www.ssllabs.com/ssltest/
   - Security Headers: https://securityheaders.com/

---

## Summary

**AccuWeb Deployment = Best Value**
- **Cost**: $12/month (vs $70/month Azure S1)
- **Setup Time**: 30-60 minutes
- **Management**: Plesk panel (easy)
- **Performance**: Enterprise SSD hardware
- **Support**: 24/7 expert .NET support

**When to Choose AccuWeb:**
- Budget-conscious deployments
- Small to medium traffic (< 50K users/month)
- Don't need auto-scaling
- Want managed hosting without high cloud costs

**When to Choose Azure Instead:**
- Need auto-scaling for variable traffic
- Require CI/CD pipelines
- Want serverless features
- Enterprise compliance requirements

---

**Deployment Time**: 30-60 minutes
**Monthly Cost**: $12-30
**Support**: 24/7 AccuWeb + Plesk documentation
