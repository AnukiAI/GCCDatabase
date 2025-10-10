# Plesk SSH Deployment Guide - Step by Step

## Quick Overview
Deploy your ASP.NET Core MVC application on a Plesk server with multiple domains using SSH terminal access.

## Files Created
1. `install-dotnet-plesk.sh` - Installs .NET 8.0 runtime
2. `gcc-webapp.service` - Systemd service configuration
3. `nginx-plesk-config.conf` - Nginx proxy configuration
4. `deploy-plesk-native.sh` - Automated deployment script

---

## 📋 Step 1: Upload Files to Server

### Via SSH/SCP:
```bash
# From your local machine, upload the files
scp install-dotnet-plesk.sh root@yourserver:/tmp/
scp gcc-webapp.service root@yourserver:/tmp/
scp nginx-plesk-config.conf root@yourserver:/tmp/
scp deploy-plesk-native.sh root@yourserver:/tmp/

# Or upload your published application
dotnet publish -c Release -o ./publish
tar -czf gcc-webapp.tar.gz ./publish/*
scp gcc-webapp.tar.gz root@yourserver:/tmp/
```

---

## 📦 Step 2: Install .NET Runtime via SSH

### Connect to server:
```bash
ssh root@yourserver
cd /tmp
```

### Run installation script:
```bash
chmod +x install-dotnet-plesk.sh
sudo ./install-dotnet-plesk.sh
```

### Verify installation:
```bash
dotnet --version
# Should show: 8.0.x
```

---

## 🚀 Step 3: Deploy Application

### Option A: Automated Deployment
```bash
cd /tmp
chmod +x deploy-plesk-native.sh

# Run deployment script
sudo ./deploy-plesk-native.sh
```

When prompted:
- Enter your domain name
- Choose database type (sqlite for simplest setup)
- Follow the prompts

### Option B: Manual Deployment

#### 1. Create application directory:
```bash
DOMAIN="yourdomain.com"  # Replace with your actual domain
mkdir -p /var/www/vhosts/$DOMAIN/httpdocs
cd /var/www/vhosts/$DOMAIN/httpdocs
```

#### 2. Extract application files:
```bash
# If you uploaded tar.gz
tar -xzf /tmp/gcc-webapp.tar.gz -C .

# Or if using Git
git clone https://github.com/yourusername/GCCWebApp.git .
dotnet restore
dotnet publish -c Release -o .
```

#### 3. Create required directories:
```bash
mkdir -p App_Data logs DataProtectionKeys wwwroot/uploads
chown -R plesk:psacln .
chmod 755 App_Data logs DataProtectionKeys
chmod 777 wwwroot/uploads
```

#### 4. Create configuration:
```bash
# Generate JWT key
JWT_KEY=$(openssl rand -base64 48)

# Create appsettings.Production.json
cat > appsettings.Production.json << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App_Data/gcc_database.db"
  },
  "Jwt": {
    "Key": "$JWT_KEY",
    "Issuer": "https://$DOMAIN",
    "Audience": "https://$DOMAIN"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "AllowedHosts": "$DOMAIN;www.$DOMAIN"
}
EOF
```

#### 5. Setup systemd service:
```bash
# Copy service file
cp /tmp/gcc-webapp.service /etc/systemd/system/

# Update domain in service file
sed -i "s/yourdomain.com/$DOMAIN/g" /etc/systemd/system/gcc-webapp.service

# Enable and start service
systemctl daemon-reload
systemctl enable gcc-webapp
systemctl start gcc-webapp
```

---

## 🔧 Step 4: Configure Nginx in Plesk

### Via Plesk Panel:
1. Login to Plesk Admin Panel
2. Navigate to your domain
3. Go to **"Apache & nginx Settings"**
4. In **"Additional nginx directives"** section, paste:

```nginx
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

location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
    root /var/www/vhosts/YOURDOMAIN/httpdocs/wwwroot;
    expires 30d;
    add_header Cache-Control "public, immutable";
}

location /api/ {
    proxy_pass http://localhost:5000/api/;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
}
```

5. Replace `YOURDOMAIN` with your actual domain
6. Click **OK** or **Apply**

---

## 🔒 Step 5: Configure SSL

### In Plesk Panel:
1. Go to your domain → **SSL/TLS Certificates**
2. Click **Install Free Basic Certificate**
3. Select **Let's Encrypt**
4. Enable **Include www subdomain**
5. Click **Get it free**

### After SSL is installed, update nginx to force HTTPS:
Add to nginx directives:
```nginx
if ($scheme != "https") {
    return 301 https://$server_name$request_uri;
}
```

---

## ✅ Step 6: Verify Deployment

### Check service status:
```bash
systemctl status gcc-webapp
```

### View logs:
```bash
journalctl -u gcc-webapp -f
```

### Test endpoints:
```bash
# Local test
curl http://localhost:5000/health

# External test (after nginx configured)
curl https://yourdomain.com/health
```

### Browser test:
1. Navigate to `https://yourdomain.com`
2. Test login: `/Account/Login`
3. View certificates: `/Certificate/CertificateTileView`
4. API docs: `/swagger`

---

## 🔧 Troubleshooting

### Service won't start:
```bash
# Check detailed error
journalctl -u gcc-webapp -n 100

# Check if port is in use
lsof -i:5000

# Check permissions
ls -la /var/www/vhosts/yourdomain.com/httpdocs
```

### 502 Bad Gateway:
```bash
# Verify app is running
systemctl status gcc-webapp

# Test direct connection
curl http://localhost:5000

# Restart services
systemctl restart gcc-webapp
systemctl restart nginx
```

### Database issues (SQLite):
```bash
# Check database file exists
ls -la App_Data/gcc_database.db

# Fix permissions
chmod 664 App_Data/gcc_database.db
chown plesk:psacln App_Data/gcc_database.db

# Initialize database
dotnet ef database update
```

---

## 🔄 Updating Application

### Quick update:
```bash
cd /var/www/vhosts/yourdomain.com/httpdocs

# Pull latest changes (if using Git)
git pull

# Or upload new files via FTP/SCP

# Rebuild
dotnet restore
dotnet publish -c Release -o .

# Restart service
systemctl restart gcc-webapp
```

---

## 📝 Important Commands

```bash
# Service management
systemctl start gcc-webapp      # Start app
systemctl stop gcc-webapp       # Stop app
systemctl restart gcc-webapp    # Restart app
systemctl status gcc-webapp     # Check status

# Logs
journalctl -u gcc-webapp -f     # Live logs
tail -f logs/*.log              # Application logs

# Database (SQLite)
sqlite3 App_Data/gcc_database.db  # Open database
.tables                            # List tables
.exit                             # Exit SQLite

# Test application
curl http://localhost:5000/health
```

---

## 🎯 Summary

1. **Install .NET**: Run `install-dotnet-plesk.sh`
2. **Deploy App**: Run `deploy-plesk-native.sh` or follow manual steps
3. **Configure Nginx**: Add proxy rules in Plesk panel
4. **Enable SSL**: Use Let's Encrypt in Plesk
5. **Verify**: Test all endpoints

**Total Time**: ~30 minutes

**Support Files**:
- Main guide: `PLESK_NATIVE_DEPLOYMENT_GUIDE.md`
- Server requirements: `PLESK_SERVER_REQUIREMENTS.md`
- This quick guide: `PLESK_SSH_DEPLOYMENT_STEPS.md`