# Steps 6-10: Complete Deployment Guide for angry-pascal.70-32-82-212.plesk.page

## Step 6: Install Docker (if not installed)

### Check Docker Installation
```bash
docker --version
```

### Install Docker on Your Plesk Server

#### For Ubuntu/Debian:
```bash
# Update package index
sudo apt-get update

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add your user to docker group
sudo usermod -aG docker $USER

# Start and enable Docker
sudo systemctl start docker
sudo systemctl enable docker

# Verify installation
docker run hello-world
```

#### For CentOS/RHEL/AlmaLinux:
```bash
# Install Docker
sudo yum install -y yum-utils
sudo yum-config-manager --add-repo https://download.docker.com/linux/centos/docker-ce.repo
sudo yum install -y docker-ce docker-ce-cli containerd.io

# Start and enable Docker
sudo systemctl start docker
sudo systemctl enable docker

# Add user to docker group
sudo usermod -aG docker $USER

# Log out and back in, then verify
docker run hello-world
```

#### Through Plesk Extensions:
1. Login to Plesk Panel
2. Go to **Extensions** → **Extension Catalog**
3. Search for "Docker"
4. Install **Docker** extension
5. Once installed, go to **Extensions** → **Docker**

---

## Step 7: Login to GitHub Container Registry

### Connect to Your Server and Login
```bash
# Create working directory
mkdir -p /opt/gcc-webapp
cd /opt/gcc-webapp

# Login to GitHub Container Registry
echo "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" | docker login ghcr.io -u AnukiAI --password-stdin
```

**Expected Output:**
```
WARNING! Your password will be stored unencrypted in /root/.docker/config.json.
Login Succeeded
```

### Verify Access
```bash
# Pull your image to verify access
docker pull ghcr.io/anukiai/gcc-webapp:latest
```

---

## Step 8: Run Deployment Script

### Upload Files to Server

#### Method 1: Through Plesk File Manager
1. Login to Plesk Panel
2. Go to **Files** → **File Manager**
3. Navigate to `/opt/gcc-webapp/`
4. Upload these files:
   - `docker-deploy-plesk-registry-MARIADB.cmd`
   - `.env.plesk-UPDATED`
   - `plesk-nginx-config.conf`

#### Method 2: Via SSH/SCP
```bash
# From your local machine
scp docker-deploy-plesk-registry-MARIADB.cmd root@angry-pascal.70-32-82-212.plesk.page:/opt/gcc-webapp/
scp .env.plesk-UPDATED root@angry-pascal.70-32-82-212.plesk.page:/opt/gcc-webapp/.env
scp plesk-nginx-config.conf root@angry-pascal.70-32-82-212.plesk.page:/opt/gcc-webapp/
```

### Convert and Run Script

#### For Linux Servers:
```bash
cd /opt/gcc-webapp

# Convert Windows script to Linux format
sed 's/\r$//' docker-deploy-plesk-registry-MARIADB.cmd > deploy.sh

# Make executable
chmod +x deploy.sh

# Run deployment
./deploy.sh
```

#### Alternative: Manual Docker Run
```bash
# Stop and remove any existing container
docker stop gcc-webapp 2>/dev/null
docker rm gcc-webapp 2>/dev/null

# Run the container with MariaDB configuration
docker run -d \
  --name gcc-webapp \
  --restart unless-stopped \
  -p 5102:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="http://0.0.0.0:8080" \
  -e ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;" \
  -e Jwt__Key="a56257adb423a4c132e969faf96ecc3aeab11dbef571d95caf818144e6618047" \
  -e Jwt__Issuer="https://angry-pascal.70-32-82-212.plesk.page" \
  -e Jwt__Audience="https://angry-pascal.70-32-82-212.plesk.page" \
  ghcr.io/anukiai/gcc-webapp:latest
```

### Verify Container is Running
```bash
# Check container status
docker ps

# Check logs
docker logs -f gcc-webapp

# Test health endpoint
curl http://localhost:5102/health
```

---

## Step 9: Configure Plesk Nginx Proxy

### Access Plesk Panel
1. Open browser: `https://angry-pascal.70-32-82-212.plesk.page:8443`
2. Login with your Plesk admin credentials

### Configure Domain Settings
1. Go to **Websites & Domains**
2. Find `angry-pascal.70-32-82-212.plesk.page`
3. Click on **Apache & nginx Settings**

### Add Nginx Configuration
1. Scroll to **Additional nginx directives**
2. Copy and paste this configuration:

```nginx
# GCC Web App Proxy Configuration
location / {
    proxy_pass http://127.0.0.1:5102;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection 'upgrade';
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_set_header X-Forwarded-Host $server_name;

    # Timeouts
    proxy_connect_timeout 60s;
    proxy_send_timeout 60s;
    proxy_read_timeout 60s;

    # Buffer settings
    proxy_buffering off;
    proxy_buffer_size 4k;
    proxy_buffers 8 4k;
    proxy_busy_buffers_size 8k;
}

# Health check endpoint
location /health {
    proxy_pass http://127.0.0.1:5102/health;
    access_log off;
}

# API endpoints with longer timeouts
location /api/ {
    proxy_pass http://127.0.0.1:5102;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    # Longer timeouts for API
    proxy_connect_timeout 120s;
    proxy_send_timeout 120s;
    proxy_read_timeout 120s;
}

# Static files with caching
location ~* \.(jpg|jpeg|png|gif|ico|css|js|svg|woff|woff2|ttf|eot|pdf)$ {
    proxy_pass http://127.0.0.1:5102;
    expires 30d;
    add_header Cache-Control "public, immutable";
}

# Certificate downloads
location /Certificate/Download/ {
    proxy_pass http://127.0.0.1:5102;
    proxy_set_header Host $host;
    proxy_connect_timeout 180s;
    proxy_send_timeout 180s;
    proxy_read_timeout 180s;
    add_header Cache-Control "no-cache, must-revalidate";
}
```

3. Click **OK** or **Apply**

### Test Nginx Configuration
```bash
# Test configuration syntax
nginx -t

# Reload nginx
systemctl reload nginx

# Check if proxy is working
curl -I http://angry-pascal.70-32-82-212.plesk.page
```

---

## Step 10: Configure SSL Certificate

### Option A: Let's Encrypt (Free SSL - Recommended)

1. In Plesk Panel, go to your domain
2. Click **SSL/TLS Certificates**
3. Click **Install Free Basic Certificate**
4. Select **Let's Encrypt**
5. Configure settings:
   - ✅ Include a "www" subdomain (if applicable)
   - ✅ Include webmail subdomain (optional)
   - ✅ Assign the certificate to the mail domain
   - ✅ Enable automatic renewal
6. Click **Get it free**

### Option B: Upload Existing SSL Certificate

1. Go to **SSL/TLS Certificates**
2. Click **Upload Certificate**
3. Upload:
   - Certificate file (.crt)
   - Private key (.key)
   - CA bundle (if available)
4. Click **Upload Certificate**
5. Select the certificate and click **Use Certificate**

### Enable HTTPS Redirect

After SSL is configured, update nginx settings:

1. Go back to **Apache & nginx Settings**
2. Add at the top of **Additional nginx directives**:

```nginx
# Force HTTPS redirect
if ($scheme != "https") {
    return 301 https://$server_name$request_uri;
}
```

3. Click **Apply**

---

## Step 11: Final Testing

### Test All Endpoints

```bash
# Test HTTP (should redirect to HTTPS)
curl -I http://angry-pascal.70-32-82-212.plesk.page

# Test HTTPS
curl -I https://angry-pascal.70-32-82-212.plesk.page

# Test health endpoint
curl https://angry-pascal.70-32-82-212.plesk.page/health

# Test login page
curl -I https://angry-pascal.70-32-82-212.plesk.page/Account/Login
```

### Browser Testing

1. **Main Application**: https://angry-pascal.70-32-82-212.plesk.page
2. **Login Page**: https://angry-pascal.70-32-82-212.plesk.page/Account/Login
3. **Certificate View**: https://angry-pascal.70-32-82-212.plesk.page/Certificate/CertificateTileView
4. **Health Check**: https://angry-pascal.70-32-82-212.plesk.page/health

### Monitor Application

```bash
# View real-time logs
docker logs -f gcc-webapp

# Check container resource usage
docker stats gcc-webapp

# Check MariaDB connections
mysql -u gccuser -p74Fzs0YFLMXLrKJG5fzexsTkk -e "SHOW PROCESSLIST;"
```

---

## Troubleshooting Guide

### Container Won't Start
```bash
# Check detailed logs
docker logs gcc-webapp --details

# Check if port 5102 is in use
netstat -tulpn | grep 5102

# Try running interactively to see errors
docker run -it --rm \
  -p 5102:8080 \
  -e ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;" \
  ghcr.io/anukiai/gcc-webapp:latest
```

### Database Connection Issues
```bash
# Test MariaDB connection from container
docker run --rm -it mysql:5.7 \
  mysql -h host.docker.internal -P 3306 -u gccuser -p74Fzs0YFLMXLrKJG5fzexsTkk gccdb -e "SELECT 1;"

# Check MariaDB is listening
netstat -tulpn | grep 3306

# Check MariaDB logs
journalctl -u mariadb -n 50
```

### Nginx Proxy Issues
```bash
# Check nginx error log
tail -f /var/log/nginx/error.log

# Test direct container access
curl http://localhost:5102

# Verify nginx configuration
nginx -T | grep -A 20 "angry-pascal"
```

### SSL Certificate Issues
```bash
# Check certificate status
certbot certificates

# Renew manually if needed
certbot renew --cert-name angry-pascal.70-32-82-212.plesk.page

# Check SSL configuration
openssl s_client -connect angry-pascal.70-32-82-212.plesk.page:443
```

---

## 🎉 Deployment Complete!

Your GCC Web Application should now be:
- ✅ Running in Docker container
- ✅ Connected to MariaDB database
- ✅ Accessible via Nginx proxy
- ✅ Secured with SSL certificate
- ✅ Auto-restarting on failures

**Production URLs:**
- Main App: https://angry-pascal.70-32-82-212.plesk.page
- Health: https://angry-pascal.70-32-82-212.plesk.page/health
- Login: https://angry-pascal.70-32-82-212.plesk.page/Account/Login
- API Docs: https://angry-pascal.70-32-82-212.plesk.page/swagger

**Admin Credentials:**
- Username: admin@greencirclecertified.com
- Password: Admin123!

---

## Maintenance Commands

```bash
# Restart container
docker restart gcc-webapp

# Update to latest image
docker pull ghcr.io/anukiai/gcc-webapp:latest
docker stop gcc-webapp
docker rm gcc-webapp
./deploy.sh

# Backup database
mysqldump -u gccuser -p74Fzs0YFLMXLrKJG5fzexsTkk gccdb > gccdb_backup_$(date +%Y%m%d).sql

# View container logs
docker logs gcc-webapp --tail 100 --follow

# Check disk usage
docker system df
docker system prune -a
```