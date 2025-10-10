# Plesk Server Requirements for GCC MVC & API Service

## 🎯 Required Plesk Extensions & Components

### 1. **Docker Extension** (REQUIRED)
- **Extension Name**: Docker
- **Purpose**: Container management and deployment
- **Installation**: Plesk Extensions Catalog → Search "Docker" → Install
- **Configuration**:
  - Enable Docker service
  - Configure Docker proxy rules
  - Set up port mappings

### 2. **PostgreSQL or MariaDB Server** (REQUIRED - Choose One)
- **Option A - PostgreSQL**:
  - Extension: PostgreSQL Server
  - Version: 13+ recommended
  - Default port: 5432

- **Option B - MariaDB**:
  - Extension: MariaDB Server
  - Version: 10.5+ recommended
  - Default port: 3306

### 3. **Let's Encrypt Extension** (RECOMMENDED)
- **Extension Name**: Let's Encrypt
- **Purpose**: Free SSL certificates
- **Installation**: Plesk Extensions Catalog → Search "Let's Encrypt" → Install
- **Configuration**: Auto-renewal enabled

### 4. **Nginx or Apache with Proxy Rules** (REQUIRED)
- **Default in Plesk**: Usually pre-installed
- **Configuration Required**:
  ```nginx
  # Nginx proxy configuration
  location / {
      proxy_pass http://127.0.0.1:5104;
      proxy_http_version 1.1;
      proxy_set_header Upgrade $http_upgrade;
      proxy_set_header Connection keep-alive;
      proxy_set_header Host $host;
      proxy_cache_bypass $http_upgrade;
      proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
      proxy_set_header X-Forwarded-Proto $scheme;
  }
  ```

### 5. **Git Extension** (OPTIONAL but Helpful)
- **Extension Name**: Git
- **Purpose**: Deploy directly from repository
- **Installation**: Plesk Extensions Catalog → Search "Git" → Install

## 📦 Server Prerequisites

### Operating System Requirements
```bash
# Minimum OS versions
- Ubuntu 20.04 LTS or newer
- CentOS 7/8 or AlmaLinux 8/9
- Debian 10 or newer

# Required packages (install via SSH)
sudo apt-get update  # For Ubuntu/Debian
sudo apt-get install -y curl wget nano

# Or for CentOS/AlmaLinux
sudo yum install -y curl wget nano
```

### Resource Requirements
```yaml
Minimum:
  RAM: 2GB
  CPU: 2 cores
  Storage: 20GB free space

Recommended:
  RAM: 4GB
  CPU: 4 cores
  Storage: 40GB free space
  Network: 100Mbps+
```

## 🐳 Docker Setup on Plesk

### Step 1: Install Docker Extension
1. Login to Plesk Admin Panel
2. Go to **Extensions** → **Extensions Catalog**
3. Search for "Docker"
4. Click **Install**
5. Wait for installation to complete

### Step 2: Configure Docker
1. Go to **Extensions** → **Docker**
2. Settings to configure:
   - ✅ Enable automatic container restart
   - ✅ Set memory limits (2GB minimum for app)
   - ✅ Configure log rotation

### Step 3: Pull and Run Container
```bash
# SSH into server and run:
docker pull ghcr.io/anukiai/gcc-webapp:latest

# Or use local image
docker load < gcc-webapp-final.tar.gz

# Run container
docker run -d \
  --name gcc-webapp \
  --restart unless-stopped \
  -p 127.0.0.1:5104:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db" \
  -e Jwt__Key="[Your-64-character-secure-key]" \
  -v /var/docker/gcc-data:/app/data \
  gcc-webapp:final-fixes
```

## 🗄️ Database Setup

### Option 1: SQLite (Simplest for Testing)
```bash
# No additional setup required
# Database file created automatically in container
# Persisted via Docker volume
```

### Option 2: PostgreSQL (Recommended for Production)
```sql
-- Create database and user
CREATE DATABASE gccdb;
CREATE USER gccuser WITH PASSWORD 'SecurePassword123!';
GRANT ALL PRIVILEGES ON DATABASE gccdb TO gccuser;

-- Update connection string
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=gccdb;Username=gccuser;Password=SecurePassword123!"
```

### Option 3: MariaDB/MySQL
```sql
-- Create database and user
CREATE DATABASE gccdb;
CREATE USER 'gccuser'@'localhost' IDENTIFIED BY 'SecurePassword123!';
GRANT ALL PRIVILEGES ON gccdb.* TO 'gccuser'@'localhost';
FLUSH PRIVILEGES;

-- Update connection string
ConnectionStrings__DefaultConnection="Server=localhost;Database=gccdb;User=gccuser;Password=SecurePassword123!"
```

## 🌐 Domain & SSL Configuration

### 1. Add Domain in Plesk
1. Go to **Websites & Domains**
2. Click **Add Domain**
3. Enter your domain (e.g., `test.yourdomain.com`)
4. Select hosting type: **Website hosting**

### 2. Configure SSL Certificate
1. Go to domain settings
2. Click **SSL/TLS Certificates**
3. Either:
   - Use **Let's Encrypt** (free)
   - Upload your own certificate
4. Enable **Permanent SEO-safe 301 redirect from HTTP to HTTPS**

### 3. Set Up Proxy Rules
1. Go to domain → **Apache & nginx Settings**
2. Add to **Additional nginx directives**:
```nginx
# Proxy to Docker container
location / {
    proxy_pass http://127.0.0.1:5104;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection keep-alive;
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_set_header X-Real-IP $remote_addr;

    # Timeouts for long-running requests
    proxy_connect_timeout 60s;
    proxy_send_timeout 60s;
    proxy_read_timeout 60s;

    # Buffer settings
    proxy_buffering off;
    proxy_request_buffering off;
}

# API-specific settings
location /api {
    proxy_pass http://127.0.0.1:5104/api;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    # CORS headers for API
    add_header 'Access-Control-Allow-Origin' '*' always;
    add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, OPTIONS' always;
    add_header 'Access-Control-Allow-Headers' 'Authorization, Content-Type' always;
}

# Health check endpoint
location /health {
    proxy_pass http://127.0.0.1:5104/health;
    access_log off;
}
```

## 🔧 Environment Variables Configuration

### Create Environment File
```bash
# Create .env file on server
nano /var/docker/gcc-webapp/.env

# Required variables
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db"
Jwt__Key="your-super-secret-jwt-key-minimum-64-characters-long-for-security"
Jwt__Issuer="https://test.yourdomain.com"
Jwt__Audience="https://test.yourdomain.com"

# Optional Azure services (leave commented if not using)
# AzureAdB2C__ClientSecret="azure-secret"
# AzureBlob__ConnectionString="storage-connection"
# ApplicationInsights__ConnectionString="insights-key"
# EmailConfiguration__Password="smtp-password"

# Feature flags
Features__EnableDetailedErrorMessages=false
Features__EnableSwagger=true
Features__RequireHttpsForAllRequests=false  # Nginx handles SSL
```

## 🚀 Quick Deployment Script

Create this script on your Plesk server:

```bash
#!/bin/bash
# deploy-gcc.sh - Quick deployment script

echo "=== GCC Web Application Deployment ==="

# Variables
CONTAINER_NAME="gcc-webapp"
IMAGE_NAME="gcc-webapp:final-fixes"
PORT="5104"

# Stop and remove existing container
echo "Stopping existing container..."
docker stop $CONTAINER_NAME 2>/dev/null
docker rm $CONTAINER_NAME 2>/dev/null

# Load environment variables
if [ -f /var/docker/gcc-webapp/.env ]; then
    export $(cat /var/docker/gcc-webapp/.env | xargs)
fi

# Run new container
echo "Starting new container..."
docker run -d \
  --name $CONTAINER_NAME \
  --restart unless-stopped \
  -p 127.0.0.1:$PORT:8080 \
  --env-file /var/docker/gcc-webapp/.env \
  -v /var/docker/gcc-data:/app/data \
  -v /var/docker/gcc-logs:/app/logs \
  $IMAGE_NAME

# Wait for startup
echo "Waiting for application startup..."
sleep 10

# Check status
if docker ps | grep -q $CONTAINER_NAME; then
    echo "✅ Container started successfully"

    # Test health endpoint
    if curl -f http://localhost:$PORT/health > /dev/null 2>&1; then
        echo "✅ Application is responding"
    else
        echo "⚠️ Application not responding yet, check logs:"
        docker logs $CONTAINER_NAME --tail 20
    fi
else
    echo "❌ Container failed to start. Checking logs..."
    docker logs $CONTAINER_NAME --tail 50
fi

echo "=== Deployment Complete ==="
```

## ✅ Verification Checklist

### After deployment, verify:
- [ ] Container is running: `docker ps | grep gcc-webapp`
- [ ] Application responds: `curl http://localhost:5104/`
- [ ] Database connection works: Check logs for "Database seeded successfully"
- [ ] SSL certificate is valid: `https://test.yourdomain.com`
- [ ] API endpoints work: `https://test.yourdomain.com/api/v1/certificates`
- [ ] Login page loads: `https://test.yourdomain.com/Account/Login`
- [ ] Static files load: Check CSS/JS in browser console

## 🔍 Troubleshooting

### Common Issues and Solutions:

**1. Container keeps restarting**
```bash
# Check logs
docker logs gcc-webapp --tail 100

# Common fix: Create data directory
docker exec gcc-webapp mkdir -p /app/data
docker restart gcc-webapp
```

**2. 502 Bad Gateway**
```bash
# Check if container is running on correct port
docker ps | grep gcc-webapp
# Verify nginx proxy_pass matches container port
```

**3. Database connection fails**
```bash
# For SQLite: Ensure volume is mounted
docker inspect gcc-webapp | grep Mounts

# For PostgreSQL/MariaDB: Test connection
psql -h localhost -U gccuser -d gccdb
```

**4. SSL/HTTPS issues**
```bash
# Verify certificate
openssl s_client -connect test.yourdomain.com:443

# Check nginx configuration
nginx -t
systemctl reload nginx
```

## 📞 Support Resources

- **Plesk Documentation**: https://docs.plesk.com/
- **Docker on Plesk**: https://docs.plesk.com/en-US/obsidian/administrator-guide/docker/
- **Application Repository**: https://github.com/AnukiAI/GCCWebApp
- **Application Logs**: `docker logs gcc-webapp`
- **Container Shell**: `docker exec -it gcc-webapp /bin/bash`

---

## 🎯 Quick Start Summary

**Minimum Plesk Extensions Required:**
1. ✅ Docker Extension
2. ✅ Database (PostgreSQL or MariaDB)
3. ✅ SSL Certificate (Let's Encrypt)

**Quick Commands:**
```bash
# Pull and run container
docker run -d --name gcc-webapp -p 127.0.0.1:5104:8080 gcc-webapp:final-fixes

# Configure nginx proxy in Plesk
# Add domain → Set SSL → Add proxy rules → Done!
```

**Test URLs:**
- Health Check: `http://localhost:5104/health`
- Application: `https://test.yourdomain.com`
- API: `https://test.yourdomain.com/api/v1/certificates`

---

*Last Updated: September 17, 2025*
*Container Version: gcc-webapp:final-fixes*
*Documentation Version: 1.0*