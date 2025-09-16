# GCC Web App - Docker to Plesk Deployment Guide

## Current Status: ✅ READY FOR DEPLOYMENT

Your Docker image has been successfully built and tested locally. The container runs properly and responds to HTTP requests on the correct ports.

**Working Image:** `gcc-webapp:production`

---

## Next Steps for Plesk Deployment

### Step 1: Tag and Push to Docker Registry

#### Option A: Docker Hub (Public)
```bash
# Login to Docker Hub
docker login

# Tag for Docker Hub
docker tag gcc-webapp:production YOUR_USERNAME/gcc-webapp:latest

# Push to Docker Hub
docker push YOUR_USERNAME/gcc-webapp:latest
```

#### Option B: Private Registry
```bash
# Tag for private registry
docker tag gcc-webapp:production your-registry.com/gcc-webapp:latest

# Push to private registry
docker push your-registry.com/gcc-webapp:latest
```

### Step 2: Transfer to Plesk Server

#### Method 1: Pull from Registry (Recommended)
```bash
# On Plesk server
docker login
docker pull YOUR_USERNAME/gcc-webapp:latest
```

#### Method 2: Save/Load Image File
```bash
# On local machine
docker save gcc-webapp:production > gcc-webapp.tar

# Transfer to server (via SCP/SFTP)
scp gcc-webapp.tar user@your-plesk-server:/tmp/

# On Plesk server
docker load < /tmp/gcc-webapp.tar
```

### Step 3: Configure PostgreSQL Connection

Update the deployment script with your actual database details:

```bash
# Edit docker-deploy-plesk.sh
POSTGRES_HOST="localhost"           # Your PostgreSQL host
POSTGRES_DB="gccdb"                 # Your database name
POSTGRES_USER="gccuser"             # Your database user
POSTGRES_PASSWORD="YourActualPass"  # Your actual password
POSTGRES_PORT="5432"                # PostgreSQL port
DOMAIN="your-actual-domain.com"     # Your actual domain
```

**Required Environment Variables:**
```bash
ConnectionStrings__DefaultConnection="Host=localhost;Database=gccdb;Username=gccuser;Password=YourActualPass;Port=5432"
```

### Step 4: Set Up Nginx Proxy in Plesk

1. **Access Plesk Panel**
   - Navigate to your domain settings
   - Go to "Apache & nginx Settings"

2. **Add Nginx Directives**
   Add these directives to the nginx configuration:

```nginx
# Proxy to Docker container
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

# Static files with caching
location ~* \.(jpg|jpeg|png|gif|ico|css|js|svg|woff|woff2|ttf|eot)$ {
    proxy_pass http://127.0.0.1:5102;
    expires 30d;
    add_header Cache-Control "public, immutable";
}
```

3. **Apply Configuration**
   - Click "OK" or "Apply" to save the nginx configuration
   - Test the proxy setup

### Step 5: Configure SSL Certificates

#### Option A: Let's Encrypt (Free)
1. In Plesk Panel:
   - Go to your domain
   - Click "SSL/TLS Certificates"
   - Click "Install Free Basic Certificate"
   - Select "Let's Encrypt"
   - Enable auto-renewal

#### Option B: Commercial SSL
1. Upload your SSL certificate files in Plesk
2. Assign to your domain

#### Force HTTPS Redirect
Add to nginx directives:
```nginx
if ($scheme != "https") {
    return 301 https://$server_name$request_uri;
}
```

---

## Deployment Commands

### 1. Deploy Container
```bash
# Make script executable
chmod +x docker-deploy-plesk.sh

# Run deployment
./docker-deploy-plesk.sh
```

### 2. Verify Deployment
```bash
# Check container status
docker ps

# Check logs
docker logs -f gcc-webapp

# Test health endpoint
curl http://localhost:5102/health
```

### 3. Monitor Application
```bash
# Container stats
docker stats gcc-webapp

# Follow logs
docker logs -f gcc-webapp

# Restart if needed
docker restart gcc-webapp
```

---

## Required Environment Setup

Before deployment, ensure your Plesk server has:

### ✅ Prerequisites Checklist
- [ ] Docker installed and running
- [ ] PostgreSQL database created and accessible
- [ ] Database user with proper permissions
- [ ] Firewall allows port 5432 (PostgreSQL) from Docker network
- [ ] Domain configured in Plesk
- [ ] SSL certificate ready (Let's Encrypt or commercial)

### Database Setup
```sql
-- Create database
CREATE DATABASE gccdb;

-- Create user
CREATE USER gccuser WITH PASSWORD 'YourSecurePassword';

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE gccdb TO gccuser;
GRANT ALL ON SCHEMA public TO gccuser;
```

### Test Database Connection
```bash
# Test from Plesk server
psql -h localhost -U gccuser -d gccdb -c "SELECT version();"
```

---

## Configuration Files

### Environment Variables Template
Copy and customize `.env.docker`:
```bash
# PostgreSQL Connection
ConnectionStrings__DefaultConnection=Host=localhost;Database=gccdb;Username=gccuser;Password=YourPassword;Port=5432

# Security
Jwt__Key=YourSecureJwtKeyAtLeast64CharactersLong
Jwt__Issuer=https://your-domain.com
Jwt__Audience=https://your-domain.com

# Required placeholders (use dummy values)
GCC_AZURE_AD_B2C_CLIENT_SECRET=dummy-secret
GCC_AZURE_AD_CLIENT_SECRET=dummy-secret
GCC_APPLICATION_INSIGHTS_CONNECTION_STRING=InstrumentationKey=00000000-0000-0000-0000-000000000000
GCC_SMTP_PASSWORD=your-smtp-password
```

---

## Troubleshooting

### Container Won't Start
```bash
# Check logs
docker logs gcc-webapp

# Common issues:
# 1. Missing environment variables
# 2. Database connection failed
# 3. Port already in use
```

### Application Returns 500 Error
```bash
# Check application logs
docker logs gcc-webapp | grep -i error

# Check database connectivity
docker exec -it gcc-webapp ping postgres-host
```

### Nginx Proxy Issues
```bash
# Test direct container access
curl http://localhost:5102

# Check nginx error logs in Plesk
tail -f /var/log/nginx/error.log
```

---

## Port Configuration

| Service | Internal Port | External Port | Purpose |
|---------|---------------|---------------|---------|
| GCC App | 8080 | 5102 | Main application |
| PostgreSQL | 5432 | 5432 | Database |
| Nginx | 80/443 | 80/443 | Web proxy |

---

## Security Checklist

- [ ] Database uses strong password
- [ ] JWT secret is randomly generated (64+ characters)
- [ ] Container runs as non-root user
- [ ] Only necessary ports exposed
- [ ] SSL/TLS encryption enabled
- [ ] Regular backups configured
- [ ] Log rotation configured
- [ ] Firewall rules properly configured

---

## Final Verification

After deployment, verify these endpoints:

1. **Main Application:** https://your-domain.com
2. **Health Check:** https://your-domain.com/health
3. **Login Page:** https://your-domain.com/Account/Login
4. **Certificate View:** https://your-domain.com/Certificate/CertificateTileView

---

**Status:** Ready for Production Deployment ✅

**Estimated Deployment Time:** 30-60 minutes

**Next Action:** Execute Step 1 (Tag and Push to Registry)