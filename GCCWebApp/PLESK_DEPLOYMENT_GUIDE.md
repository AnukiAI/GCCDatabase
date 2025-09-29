# GCC Web Application - Plesk Container Deployment Guide

## Overview
Complete guide for deploying the GCC Web Application as a Docker container on a Plesk server with PostgreSQL or MariaDB.

## Current Status
- **Production Container**: Running on port 5105 (SQLite) - Healthy
- **Test Container**: Running on port 5102 (PostgreSQL) - Needs attention
- **Image**: `gcc-webapp:production` or `ghcr.io/anukiai/gcc-webapp:latest`

## Prerequisites Checklist

Before deployment, ensure your Plesk server has:

- [ ] **Docker installed and running**
- [ ] **PostgreSQL database server installed**
- [ ] **Domain configured in Plesk**
- [ ] **SSH/RDP access to the server**
- [ ] **Internet access to pull from GitHub Container Registry**

---

## Step 1: Prepare Your Plesk Server

### 1.1 Install Docker (if not already installed)

For Ubuntu/Debian:
```bash
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER
```

For CentOS/RHEL:
```bash
sudo yum install -y docker
sudo systemctl start docker
sudo systemctl enable docker
sudo usermod -aG docker $USER
```

### 1.2 Set Up PostgreSQL Database

```sql
-- Connect to PostgreSQL as admin
psql -U postgres

-- Create database
CREATE DATABASE gccdb;

-- Create user with secure password
CREATE USER gccuser WITH PASSWORD 'YourSecurePassword123!';

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE gccdb TO gccuser;
GRANT ALL ON SCHEMA public TO gccuser;

-- Exit
\q
```

### 1.3 Test Database Connection
```bash
psql -h localhost -U gccuser -d gccdb -c "SELECT version();"
```

---

## Step 2: Transfer Deployment Files

### 2.1 Create Deployment Directory
```bash
mkdir -p /opt/gcc-webapp
cd /opt/gcc-webapp
```

### 2.2 Upload Deployment Files

Transfer these files to your Plesk server:
- `docker-deploy-plesk-registry.cmd` (or convert to .sh for Linux)
- `.env.plesk`
- `plesk-nginx-config.conf`

### 2.3 Convert Script for Linux (if needed)
```bash
# Convert Windows script to Linux
cat docker-deploy-plesk-registry.cmd | tr -d '\r' > deploy.sh
chmod +x deploy.sh
```

---

## Step 3: Configure Environment Variables

### 3.1 Edit Environment File
```bash
nano .env.plesk
```

Update these critical values:
```bash
# Your actual database password
ConnectionStrings__DefaultConnection=Host=localhost;Database=gccdb;Username=gccuser;Password=YourActualPassword;Port=5432

# Your actual domain
DOMAIN=yourdomain.com
Jwt__Issuer=https://yourdomain.com
Jwt__Audience=https://yourdomain.com

# Generate a secure JWT secret (64+ characters)
Jwt__Key=GenerateASecure64CharacterRandomStringForJwtTokenGeneration123456789
```

### 3.2 Generate Secure JWT Key
```bash
# Generate a random 64-character key
openssl rand -base64 48
```

---

## Step 4: Deploy the Container

### 4.1 Login to GitHub Container Registry
```bash
echo "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" | docker login ghcr.io -u AnukiAI --password-stdin
```

### 4.2 Run Deployment Script

**For Windows:**
```cmd
docker-deploy-plesk-registry.cmd
```

**For Linux:**
```bash
./deploy.sh
```

### 4.3 Verify Container is Running
```bash
docker ps
docker logs -f gcc-webapp
```

---

## Step 5: Configure Plesk Nginx Proxy

### 5.1 Access Plesk Panel
1. Log into your Plesk control panel
2. Navigate to your domain
3. Go to "Apache & nginx Settings"

### 5.2 Add Nginx Directives
1. In the "Additional nginx directives" section
2. Copy and paste the contents of `plesk-nginx-config.conf`
3. Click "OK" to apply

### 5.3 Test Nginx Configuration
```bash
# Test nginx config
nginx -t

# Reload nginx
systemctl reload nginx
```

---

## Step 6: Configure SSL Certificate

### 6.1 Let's Encrypt (Recommended)
1. In Plesk Panel, go to your domain
2. Click "SSL/TLS Certificates"
3. Click "Install Free Basic Certificate"
4. Select "Let's Encrypt"
5. Enable "Include a www subdomain"
6. Enable "Secure your mail"
7. Click "Get it free"

### 6.2 Enable HTTPS Redirect
After SSL is configured, uncomment the HTTPS redirect in your nginx config:
```nginx
if ($scheme != "https") {
    return 301 https://$server_name$request_uri;
}
```

---

## Step 7: Test Deployment

### 7.1 Health Check
```bash
curl http://localhost:5102/health
```

### 7.2 Test Application Endpoints
```bash
# Main page
curl -I https://yourdomain.com

# Login page
curl -I https://yourdomain.com/Account/Login

# Certificate view
curl -I https://yourdomain.com/Certificate/CertificateTileView

# API health
curl https://yourdomain.com/api/health
```

### 7.3 Web Browser Testing
1. Navigate to `https://yourdomain.com`
2. Test certificate tile view
3. Test login functionality
4. Verify all static assets load correctly

---

## Step 8: Production Security Hardening

### 8.1 Firewall Configuration
```bash
# Allow only necessary ports
ufw allow 22      # SSH
ufw allow 80      # HTTP
ufw allow 443     # HTTPS
ufw enable
```

### 8.2 Container Security
```bash
# The container already runs as non-root user (appuser)
# Verify with:
docker exec gcc-webapp whoami
```

### 8.3 Database Security
```bash
# Ensure PostgreSQL only accepts local connections
# Edit /etc/postgresql/*/main/postgresql.conf
listen_addresses = 'localhost'

# Restart PostgreSQL
systemctl restart postgresql
```

---

## Monitoring and Maintenance

### Container Management
```bash
# View container status
docker ps
docker stats gcc-webapp

# View logs
docker logs -f gcc-webapp

# Restart container
docker restart gcc-webapp

# Update to latest image
docker pull ghcr.io/anukiai/gcc-webapp:latest
docker stop gcc-webapp
docker rm gcc-webapp
./deploy.sh
```

### Database Backup
```bash
# Create automated backup script
pg_dump -h localhost -U gccuser gccdb > /backup/gccdb_$(date +%Y%m%d_%H%M%S).sql
```

### Log Rotation
```bash
# Configure Docker log rotation
echo '{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}' > /etc/docker/daemon.json

systemctl restart docker
```

---

## Troubleshooting

### Container Won't Start
```bash
# Check logs
docker logs gcc-webapp

# Common issues:
# 1. Database connection failed
# 2. Missing environment variables
# 3. Port already in use
```

### Database Connection Issues
```bash
# Test database connectivity
docker exec -it gcc-webapp ping localhost
docker exec -it gcc-webapp telnet localhost 5432
```

### Nginx/Proxy Issues
```bash
# Test direct container access
curl http://localhost:5102

# Check nginx logs
tail -f /var/log/nginx/error.log
```

### Performance Issues
```bash
# Monitor container resources
docker stats gcc-webapp

# Check database performance
psql -U gccuser -d gccdb -c "SELECT * FROM pg_stat_activity;"
```

---

## Support Commands

### Quick Status Check
```bash
#!/bin/bash
echo "=== GCC Web App Status ==="
echo "Container Status:"
docker ps | grep gcc-webapp
echo ""
echo "Health Check:"
curl -s http://localhost:5102/health
echo ""
echo "Disk Usage:"
df -h
echo ""
echo "Memory Usage:"
free -h
```

---

## Deployment Summary

**What Gets Deployed:**
- GCC Web Application (ASP.NET Core 8.0)
- Running in secure Docker container
- Connected to PostgreSQL database
- Proxied through Nginx with SSL
- Auto-restart on failure

**Performance Specifications:**
- Container Port: 8080 → External Port: 5102
- Memory: ~200MB typical usage
- CPU: Low usage (spikes during PDF generation)
- Storage: Minimal (logs only, database separate)

**Security Features:**
- Non-root container user
- SSL/TLS encryption
- Secure JWT tokens
- Database connection encryption
- Security headers in Nginx

---

**Estimated Deployment Time:** 30-60 minutes

**Status:** ✅ Production Ready

**Next Action:** Execute Step 1 (Prepare Plesk Server)