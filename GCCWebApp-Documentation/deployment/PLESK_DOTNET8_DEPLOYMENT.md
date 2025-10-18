# Plesk .NET 8.0 Deployment Guide

## Prerequisites

1. **Plesk .NET Toolkit Extension** (v2.1.11 or later)
   - Supports ASP.NET Core 8.0 Runtime
   - Install via Plesk Extensions Catalog

2. **SSH Access to Plesk Server**

## Step 1: Install .NET 8.0 Runtime on Plesk

Through Plesk UI:
1. Go to **Extensions** → **.NET Toolkit**
2. Click **Manage Versions**
3. Select **ASP.NET Core 8.0 Runtime**
4. Click **Install**

Or via SSH:
```bash
# Check if .NET 8.0 is already installed
dotnet --list-runtimes | grep "8.0"

# If not installed, use the Plesk CLI
plesk bin extension --exec ".NET Toolkit" dotnet install 8.0
```

## Step 2: Prepare Application for Deployment

```bash
# On your development machine
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Clean and publish for production
dotnet clean
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish

# Create deployment package
tar -czf gccwebapp.tar.gz -C publish .
```

## Step 3: Upload to Plesk Server

```bash
# Upload via SCP
scp gccwebapp.tar.gz user@your-plesk-server:/var/www/vhosts/yourdomain.com/

# Or use Plesk File Manager
# Upload to: /httpdocs/
```

## Step 4: Deploy on Plesk Server

SSH into your Plesk server:

```bash
# Navigate to your domain directory
cd /var/www/vhosts/yourdomain.com/httpdocs

# Extract the application
tar -xzf ../gccwebapp.tar.gz

# Set permissions
chown -R psacln:psaserv .
chmod 755 .
```

## Step 5: Configure in Plesk

1. Go to **Websites & Domains** → Your Domain
2. Click **.NET Toolkit**
3. Configure:
   - **Startup File**: `GCCWebApp.dll`
   - **ASP.NET Core Version**: 8.0
   - **Environment**: Production
   - **Port**: (auto-assigned)

4. **Environment Variables**:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ConnectionStrings__DefaultConnection=your_connection_string
   GCC_JWT_SECRET=your_jwt_secret_min_64_chars
   ```

## Step 6: Database Configuration

For SQL Server:
```json
"Server=your-server;Database=GCCDatabase;User Id=your-user;Password=your-password;TrustServerCertificate=true"
```

For MySQL (if using):
```json
"Server=localhost;Database=gccdb;User=gccuser;Password=your-password"
```

## Step 7: Start the Application

Through Plesk UI:
1. Go to **.NET Toolkit** for your domain
2. Click **Start** or **Restart**

Or via SSH:
```bash
# Plesk will manage the systemd service
plesk bin site --update yourdomain.com -dotnet-startup-file GCCWebApp.dll
```

## Step 8: Configure Nginx (Automatic)

Plesk automatically configures Nginx as reverse proxy. The configuration will be at:
```
/etc/nginx/plesk.conf.d/vhosts/yourdomain.com.conf
```

## Step 9: Verify Deployment

```bash
# Check application status
systemctl status plesk-dotnet-yourdomain

# Check logs
journalctl -u plesk-dotnet-yourdomain -f

# Or in Plesk UI
# .NET Toolkit → View Logs
```

## Step 10: SSL Configuration

1. Go to **SSL/TLS Certificates**
2. Install Let's Encrypt certificate (free)
3. Enable **Permanent SEO-safe 301 redirect from HTTP to HTTPS**

## Troubleshooting

### Application won't start
```bash
# Check port availability
netstat -tlnp | grep dotnet

# Check file permissions
ls -la /var/www/vhosts/yourdomain.com/httpdocs/

# View detailed logs
tail -f /var/www/vhosts/yourdomain.com/logs/error_log
```

### Database connection issues
```bash
# Test connection from server
dotnet exec GCCWebApp.dll --test-db-connection
```

### Performance optimization
```bash
# Enable response caching
# Add to appsettings.Production.json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100
    }
  }
}
```

## Maintenance

### Update application
```bash
# Upload new gccwebapp.tar.gz
cd /var/www/vhosts/yourdomain.com/httpdocs
tar -xzf ../gccwebapp.tar.gz
# Restart via Plesk UI or:
plesk bin site --update yourdomain.com -dotnet-restart
```

### Backup
```bash
# Plesk automated backups will include the application
# Manual backup:
tar -czf backup-$(date +%Y%m%d).tar.gz httpdocs/
```

## Security Notes

1. **Never commit secrets** to repository
2. Use **environment variables** for sensitive data
3. Enable **firewall rules** in Plesk
4. Keep **.NET runtime updated** via Plesk
5. Use **strong passwords** for database
6. Enable **2FA** for Plesk admin access

## Quick Deployment Script

Save as `deploy-to-plesk.sh`:

```bash
#!/bin/bash
# Run from project directory

echo "Building and deploying GCC Web App to Plesk..."

# Build
dotnet clean
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish

# Package
tar -czf gccwebapp.tar.gz -C publish .

# Upload (configure your server details)
PLESK_SERVER="your-server.com"
PLESK_USER="your-user"
DOMAIN_PATH="/var/www/vhosts/yourdomain.com"

scp gccwebapp.tar.gz $PLESK_USER@$PLESK_SERVER:$DOMAIN_PATH/

# Deploy
ssh $PLESK_USER@$PLESK_SERVER << 'ENDSSH'
cd $DOMAIN_PATH/httpdocs
tar -xzf ../gccwebapp.tar.gz
chown -R psacln:psaserv .
plesk bin site --update yourdomain.com -dotnet-restart
ENDSSH

echo "Deployment complete!"
```

## Support

- **Plesk .NET Toolkit Documentation**: [Plesk Extensions](https://www.plesk.com/extensions/)
- **.NET 8.0 Documentation**: [Microsoft Docs](https://docs.microsoft.com/dotnet/core/)
- **Application Issues**: Check `/logs/` in Plesk File Manager

---

**Last Updated**: September 2025
**Compatible with**: Plesk .NET Toolkit v2.1.11+, ASP.NET Core 8.0