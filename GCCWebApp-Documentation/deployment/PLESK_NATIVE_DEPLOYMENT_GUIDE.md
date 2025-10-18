# Plesk Native Deployment Guide (Without Docker)
*For Development, Demo, and Direct Hosting*

## 🎯 Benefits of Native Deployment

### Why Deploy Without Docker:
- ✅ **Easier debugging** - Direct access to logs and files
- ✅ **Live development** - Make changes without rebuilding containers
- ✅ **Better performance** - No container overhead
- ✅ **Simpler troubleshooting** - Standard .NET hosting
- ✅ **Direct file access** - Edit configs and files directly

## 📋 Plesk Extensions Required

### 1. **.NET Hosting Bundle** (REQUIRED)
```bash
# Install via SSH (Ubuntu/Debian)
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0 --runtime aspnetcore
./dotnet-install.sh --channel 8.0

# Or download manually from:
# https://dotnet.microsoft.com/en-us/download/dotnet/8.0
```

### 2. **Database Server** (REQUIRED - Choose One)
- **SQLite** - No installation needed (simplest)
- **PostgreSQL** - Via Plesk extension catalog
- **MariaDB/MySQL** - Via Plesk extension catalog
- **SQL Server** - Install manually if needed

### 3. **Node.js** (OPTIONAL - for building JS/CSS)
```bash
# Install Node.js 18+ via Plesk or SSH
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs
```

## 🚀 Step-by-Step Deployment

### Step 1: Prepare Application Files

#### Option A: Upload via FTP/SFTP
```bash
# Build application locally first
cd "C:\...\GCCWebApp\GCCWebApp"
dotnet publish -c Release -o ./publish

# Upload contents of ./publish folder to Plesk
# Target directory: /var/www/vhosts/yourdomain.com/httpdocs
```

#### Option B: Deploy via Git (Recommended)
```bash
# In Plesk domain settings
1. Go to "Git" section
2. Add repository: https://github.com/AnukiAI/GCCWebApp.git
3. Set deployment path: /httpdocs
4. Enable automatic deployment
5. Run deployment script after pull:
   cd /var/www/vhosts/yourdomain.com/httpdocs/GCCWebApp
   dotnet restore
   dotnet publish -c Release -o ../httpdocs
```

### Step 2: Set Up Directory Structure
```bash
# SSH into server and create required directories
cd /var/www/vhosts/yourdomain.com/httpdocs

# Create necessary directories
mkdir -p App_Data
mkdir -p logs
mkdir -p DataProtectionKeys
mkdir -p wwwroot/uploads

# Set permissions
chown -R plesk:psacln .
chmod 755 App_Data logs DataProtectionKeys
chmod 777 wwwroot/uploads  # For file uploads
```

### Step 3: Configure Application Settings

Create/edit `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App_Data/gcc_database.db"
  },
  "Jwt": {
    "Key": "your-super-secret-jwt-key-minimum-64-characters-long-for-security-compliance",
    "Issuer": "https://yourdomain.com",
    "Audience": "https://yourdomain.com"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "yourdomain.com;www.yourdomain.com",
  "Features": {
    "EnableDetailedErrorMessages": false,
    "RequireHttpsForAllRequests": false,
    "EnableSwagger": true
  }
}
```

### Step 4: Create Systemd Service

Create service file: `/etc/systemd/system/gcc-webapp.service`
```ini
[Unit]
Description=GCC Web Application
After=network.target

[Service]
Type=simple
User=plesk
Group=psacln
WorkingDirectory=/var/www/vhosts/yourdomain.com/httpdocs
ExecStart=/usr/bin/dotnet /var/www/vhosts/yourdomain.com/httpdocs/GCCWebApp.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=gcc-webapp

# Environment variables
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Increase limits for production
LimitNOFILE=100000
LimitNPROC=4096

[Install]
WantedBy=multi-user.target
```

Enable and start service:
```bash
sudo systemctl daemon-reload
sudo systemctl enable gcc-webapp
sudo systemctl start gcc-webapp
sudo systemctl status gcc-webapp
```

### Step 5: Configure Nginx in Plesk

Go to domain → **Apache & nginx Settings** → **Additional nginx directives**:

```nginx
# Proxy to .NET application
location / {
    proxy_pass http://localhost:5000;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection keep-alive;
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_set_header X-Real-IP $remote_addr;
}

# Serve static files directly
location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
    root /var/www/vhosts/yourdomain.com/httpdocs/wwwroot;
    expires 30d;
    add_header Cache-Control "public, immutable";
}

# API specific handling
location /api {
    proxy_pass http://localhost:5000/api;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}

# WebSocket support for SignalR
location /hubs {
    proxy_pass http://localhost:5000/hubs;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
}
```

## 🔧 Development-Friendly Setup

### Enable Hot Reload for Development
```bash
# Install dotnet-watch tool
dotnet tool install --global dotnet-watch

# Run with file watching
cd /var/www/vhosts/yourdomain.com/httpdocs
dotnet watch run --urls "http://localhost:5000"
```

### Direct File Editing via Plesk
1. Use Plesk File Manager for quick edits
2. Set up FTP/SFTP for your IDE
3. Use Git deployment with webhooks

### VS Code Remote Development
```bash
# Install VS Code Server on Plesk
curl -fsSL https://code-server.dev/install.sh | sh

# Access via browser at https://yourdomain.com:8080
# Or use VS Code Remote SSH extension
```

## 🗄️ Database Configuration

### Option 1: SQLite (Simplest)
```json
// appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=App_Data/gcc_database.db"
}
```

### Option 2: PostgreSQL
```bash
# Create database via Plesk or SSH
sudo -u postgres psql
CREATE DATABASE gccdb;
CREATE USER gccuser WITH PASSWORD 'YourSecurePassword';
GRANT ALL PRIVILEGES ON DATABASE gccdb TO gccuser;
\q

# Update connection string
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=gccdb;Username=gccuser;Password=YourSecurePassword"
}
```

### Option 3: MariaDB/MySQL
```sql
-- Via Plesk Database management
CREATE DATABASE gccdb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'gccuser'@'localhost' IDENTIFIED BY 'YourSecurePassword';
GRANT ALL PRIVILEGES ON gccdb.* TO 'gccuser'@'localhost';
FLUSH PRIVILEGES;

-- Update connection string
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=gccdb;User=gccuser;Password=YourSecurePassword"
}
```

## 📝 Deployment Script

Create `/var/www/vhosts/yourdomain.com/deploy.sh`:
```bash
#!/bin/bash
# Deploy script for GCC Web Application

echo "=== GCC Deployment Starting ==="

# Variables
APP_PATH="/var/www/vhosts/yourdomain.com/httpdocs"
SERVICE_NAME="gcc-webapp"

# Navigate to application directory
cd $APP_PATH

# Pull latest changes (if using Git)
if [ -d ".git" ]; then
    echo "Pulling latest changes from Git..."
    git pull origin master
fi

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore

# Build application
echo "Building application..."
dotnet build -c Release

# Publish application
echo "Publishing application..."
dotnet publish -c Release -o ./publish

# Copy published files to httpdocs
echo "Copying files..."
cp -r ./publish/* ./

# Run database migrations
echo "Running database migrations..."
dotnet ef database update

# Set permissions
echo "Setting permissions..."
chown -R plesk:psacln $APP_PATH
chmod 755 $APP_PATH/App_Data

# Restart application
echo "Restarting application..."
sudo systemctl restart $SERVICE_NAME

# Check status
sleep 5
if systemctl is-active --quiet $SERVICE_NAME; then
    echo "✅ Application is running"
else
    echo "❌ Application failed to start"
    sudo journalctl -u $SERVICE_NAME -n 50
fi

echo "=== Deployment Complete ==="
```

Make executable:
```bash
chmod +x /var/www/vhosts/yourdomain.com/deploy.sh
```

## 🔍 Monitoring & Logs

### View Application Logs
```bash
# System service logs
sudo journalctl -u gcc-webapp -f

# Application logs
tail -f /var/www/vhosts/yourdomain.com/httpdocs/logs/*.log

# Plesk domain logs
tail -f /var/www/vhosts/yourdomain.com/logs/error_log
```

### Performance Monitoring
```bash
# Check memory usage
ps aux | grep dotnet

# Monitor CPU and memory
htop

# Check port listening
netstat -tlpn | grep 5000
```

## 🚨 Troubleshooting

### Common Issues:

**1. Application won't start**
```bash
# Check for port conflicts
sudo lsof -i:5000

# Check permissions
ls -la /var/www/vhosts/yourdomain.com/httpdocs

# View detailed errors
sudo journalctl -u gcc-webapp -n 100
```

**2. Database connection fails**
```bash
# Test database connection
dotnet ef database update --verbose

# Check database file permissions (SQLite)
ls -la App_Data/gcc_database.db
chmod 664 App_Data/gcc_database.db
```

**3. 502 Bad Gateway**
```bash
# Verify app is running
systemctl status gcc-webapp

# Check if listening on correct port
curl http://localhost:5000/health

# Restart services
sudo systemctl restart gcc-webapp
sudo systemctl restart nginx
```

**4. Static files not loading**
```bash
# Check wwwroot permissions
ls -la wwwroot/

# Verify nginx static file configuration
nginx -t
```

## ✅ Verification Checklist

- [ ] .NET 8.0 Runtime installed: `dotnet --version`
- [ ] Application files deployed to httpdocs
- [ ] Database configured and accessible
- [ ] Service running: `systemctl status gcc-webapp`
- [ ] Nginx proxy configured correctly
- [ ] SSL certificate active
- [ ] Can access: https://yourdomain.com
- [ ] API responds: https://yourdomain.com/api/v1/certificates
- [ ] Static files load (CSS/JS)
- [ ] Can login with test credentials

## 🎯 Quick Commands Reference

```bash
# Start/Stop/Restart application
sudo systemctl start gcc-webapp
sudo systemctl stop gcc-webapp
sudo systemctl restart gcc-webapp

# View logs
sudo journalctl -u gcc-webapp -f

# Deploy updates
cd /var/www/vhosts/yourdomain.com
./deploy.sh

# Run migrations
dotnet ef database update

# Check application health
curl http://localhost:5000/health
```

## 📱 Development Tips

### Live Editing
1. Edit files directly in `/var/www/vhosts/yourdomain.com/httpdocs`
2. For C# changes: Run `dotnet build` then `systemctl restart gcc-webapp`
3. For Razor views: Changes take effect immediately
4. For static files: Changes immediate (clear browser cache)

### Remote Debugging
```bash
# Run in debug mode
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Enable detailed errors temporarily
export ASPNETCORE_DETAILEDERRORS=true
systemctl restart gcc-webapp
```

### Database Management
```bash
# Backup database (SQLite)
cp App_Data/gcc_database.db App_Data/backup_$(date +%Y%m%d).db

# Reset database
rm App_Data/gcc_database.db
dotnet ef database update
```

## 🔗 Access Points

After deployment, access your application at:
- **Main Site**: https://yourdomain.com
- **API Documentation**: https://yourdomain.com/swagger
- **Health Check**: https://yourdomain.com/health
- **Admin Panel**: https://yourdomain.com/Admin

---

## 📊 Comparison: Native vs Container

| Feature | Native Deployment | Docker Container |
|---------|------------------|------------------|
| **Setup Complexity** | Medium | Simple |
| **Performance** | Better | Good |
| **Debugging** | Easier | Harder |
| **File Access** | Direct | Through Docker |
| **Updates** | Quick | Rebuild Required |
| **Resource Usage** | Lower | Higher |
| **Isolation** | None | Full |
| **Scalability** | Manual | Easy |

**Choose Native Deployment When:**
- Active development and testing
- Need direct file access
- Want better debugging capabilities
- Running on resource-limited servers
- Prefer traditional hosting approach

---

*Last Updated: September 17, 2025*
*Application Version: ASP.NET Core 8.0*
*Guide Version: 1.0*