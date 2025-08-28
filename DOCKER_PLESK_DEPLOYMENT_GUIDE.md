# GCC Web Application - Plesk Server Docker Deployment Guide

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Plesk Server Preparation](#plesk-server-preparation)
3. [Docker Installation on Plesk](#docker-installation-on-plesk)
4. [Application Preparation](#application-preparation)
5. [Deployment Steps](#deployment-steps)
6. [SSL Configuration](#ssl-configuration)
7. [Database Setup](#database-setup)
8. [Monitoring and Maintenance](#monitoring-and-maintenance)
9. [Troubleshooting](#troubleshooting)
10. [Rollback Procedures](#rollback-procedures)

---

## Prerequisites

### Server Requirements
- **Plesk Version**: Obsidian 18.0.44 or later
- **OS**: Ubuntu 20.04/22.04 LTS, CentOS 7/8, or Debian 10/11
- **RAM**: Minimum 4GB (8GB recommended)
- **Storage**: 20GB free space minimum
- **CPU**: 2 cores minimum (4 recommended)
- **Ports**: 5102, 5103, 1434, 6380 (ensure firewall allows these)

### Required Plesk Extensions
1. **Docker Extension** (free from Plesk Extensions Catalog)
2. **Git Extension** (optional, for source control)
3. **Monitoring Extension** (recommended)

### Local Prerequisites
- Docker Desktop installed locally for building images
- Git for source control
- Access to Docker Hub or private registry

---

## Plesk Server Preparation

### Step 1: Access Plesk Server

```bash
# SSH into your Plesk server
ssh root@your-plesk-server.com

# Or use Plesk SSH Terminal extension
```

### Step 2: Update System

```bash
# For Ubuntu/Debian
apt-get update && apt-get upgrade -y

# For CentOS/RHEL
yum update -y
```

### Step 3: Install Required Packages

```bash
# Install essential tools
apt-get install -y curl wget git nano htop

# Install Docker prerequisites
apt-get install -y apt-transport-https ca-certificates gnupg lsb-release
```

---

## Docker Installation on Plesk

### Method 1: Via Plesk Extension (Recommended)

1. **Login to Plesk Panel**
   - Navigate to: https://your-server:8443
   - Login with admin credentials

2. **Install Docker Extension**
   - Go to **Extensions** → **Extensions Catalog**
   - Search for "Docker"
   - Click **Install** on Docker extension
   - Wait for installation to complete

3. **Configure Docker Extension**
   - Go to **Extensions** → **Docker**
   - Click **Settings**
   - Set Docker daemon options:
     ```json
     {
       "log-driver": "json-file",
       "log-opts": {
         "max-size": "10m",
         "max-file": "3"
       },
       "storage-driver": "overlay2"
     }
     ```

### Method 2: Manual Installation

```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose
curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose

# Start Docker service
systemctl start docker
systemctl enable docker

# Verify installation
docker --version
docker-compose --version
```

---

## Application Preparation

### Step 1: Prepare Application Files

On your **local machine**:

```bash
# Clone repository
git clone https://github.com/your-repo/gcc-webapp.git
cd gcc-webapp

# Build Docker image
docker build -t gcc-webapp:production .

# Test locally
docker run -p 5102:8080 gcc-webapp:production

# Tag for registry
docker tag gcc-webapp:production your-dockerhub/gcc-webapp:production

# Push to registry
docker login
docker push your-dockerhub/gcc-webapp:production
```

### Step 2: Create Deployment Package

Create a deployment package with these files:

```
deployment/
├── docker-compose.production.yml
├── .env.production
├── nginx/
│   ├── nginx.conf
│   └── ssl/
├── scripts/
│   ├── deploy.sh
│   ├── backup.sh
│   └── restore.sh
└── README.md
```

### Step 3: Create Production Docker Compose

Create `docker-compose.production.yml`:

```yaml
version: '3.8'

services:
  # GCC Web Application
  gccwebapp:
    image: your-dockerhub/gcc-webapp:production
    container_name: gcc-webapp-prod
    restart: always
    ports:
      - "127.0.0.1:5102:8080"  # Only expose to localhost for Nginx proxy
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
      - Jwt__Key=${JWT_SECRET_KEY}
      - Jwt__Issuer=${JWT_ISSUER}
      - Jwt__Audience=${JWT_AUDIENCE}
    volumes:
      - app-logs:/app/logs
      - app-uploads:/app/wwwroot/uploads
      - app-certificates:/app/certificates
    networks:
      - gcc-network
    depends_on:
      - sqlserver
      - redis
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  # SQL Server
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: gcc-sqlserver-prod
    restart: always
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${DB_SA_PASSWORD}
      - MSSQL_PID=Web  # Use Web edition for production
    ports:
      - "127.0.0.1:1433:1433"  # Only expose to localhost
    volumes:
      - sqlserver-data:/var/opt/mssql
    networks:
      - gcc-network
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$${SA_PASSWORD}" -Q "SELECT 1"
      interval: 30s
      timeout: 10s
      retries: 5

  # Redis Cache
  redis:
    image: redis:7-alpine
    container_name: gcc-redis-prod
    restart: always
    command: redis-server --requirepass ${REDIS_PASSWORD} --appendonly yes
    ports:
      - "127.0.0.1:6379:6379"  # Only expose to localhost
    volumes:
      - redis-data:/data
    networks:
      - gcc-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  app-logs:
    driver: local
  app-uploads:
    driver: local
  app-certificates:
    driver: local
  sqlserver-data:
    driver: local
  redis-data:
    driver: local

networks:
  gcc-network:
    driver: bridge
```

### Step 4: Create Production Environment File

Create `.env.production`:

```bash
# Database Configuration
DB_SA_PASSWORD=Your$trongP@ssw0rd2024!
DB_CONNECTION_STRING=Server=sqlserver;Database=GCC_Production;User Id=sa;Password=Your$trongP@ssw0rd2024!;TrustServerCertificate=true

# JWT Configuration
JWT_SECRET_KEY=your-production-jwt-secret-key-minimum-64-characters-for-security-requirements
JWT_ISSUER=https://your-domain.com
JWT_AUDIENCE=https://your-domain.com

# Redis Configuration
REDIS_PASSWORD=YourRedisP@ssw0rd2024!

# SMTP Configuration (SendGrid example)
SMTP_HOST=smtp.sendgrid.net
SMTP_PORT=587
SMTP_USERNAME=apikey
SMTP_PASSWORD=your-sendgrid-api-key
SMTP_FROM_EMAIL=noreply@your-domain.com

# Application Settings
ENABLE_HTTPS_REDIRECTION=true
ENABLE_2FA=true
```

---

## Deployment Steps

### Step 1: Transfer Files to Plesk Server

```bash
# On local machine, create archive
tar -czf gcc-deployment.tar.gz deployment/

# Transfer to server
scp gcc-deployment.tar.gz root@your-plesk-server:/root/

# On server, extract
ssh root@your-plesk-server
cd /root
tar -xzf gcc-deployment.tar.gz
cd deployment
```

### Step 2: Create Plesk Subscription

1. **In Plesk Panel**:
   - Go to **Websites & Domains**
   - Click **Add Domain**
   - Enter: `gcc.your-domain.com`
   - Select hosting type: **Website hosting**
   - Choose service plan

2. **Configure Domain**:
   - Document root: `/var/www/vhosts/gcc.your-domain.com/httpdocs`
   - PHP: Disable (not needed for Docker)
   - Enable SSL/TLS support

### Step 3: Deploy via Plesk Docker Extension

1. **Using Plesk UI**:
   - Go to **Extensions** → **Docker**
   - Click **Add Container**
   - Select **docker-compose.yml**
   - Upload your `docker-compose.production.yml`
   - Upload `.env.production`
   - Click **Deploy**

2. **Using Command Line**:

```bash
# Navigate to domain directory
cd /var/www/vhosts/gcc.your-domain.com

# Create docker directory
mkdir -p docker
cd docker

# Copy deployment files
cp /root/deployment/* .

# Start containers
docker-compose -f docker-compose.production.yml up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f
```

### Step 4: Configure Nginx Proxy in Plesk

1. **Go to Domain Settings**:
   - Navigate to your domain in Plesk
   - Go to **Apache & nginx Settings**

2. **Add Additional nginx directives**:

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

# Security headers
add_header X-Frame-Options "SAMEORIGIN" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-XSS-Protection "1; mode=block" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
```

3. **Click Apply**

---

## SSL Configuration

### Option 1: Let's Encrypt (Free SSL)

1. **In Plesk**:
   - Go to your domain
   - Click **SSL/TLS Certificates**
   - Click **Install Free Basic Certificate**
   - Select **Let's Encrypt**
   - Check all options
   - Click **Install**

2. **Auto-renewal is configured automatically**

### Option 2: Commercial SSL

1. **Purchase SSL Certificate**
2. **In Plesk**:
   - Go to **SSL/TLS Certificates**
   - Click **Add SSL/TLS Certificate**
   - Upload certificate files
   - Click **Upload Certificate**
   - Assign to domain

### Force HTTPS Redirect

In **Apache & nginx Settings**, add:

```nginx
if ($scheme != "https") {
    return 301 https://$server_name$request_uri;
}
```

---

## Database Setup

### Step 1: Initialize Database

```bash
# Connect to SQL Server container
docker exec -it gcc-sqlserver-prod /bin/bash

# Run SQL commands
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your$trongP@ssw0rd2024!'

# Create database
CREATE DATABASE GCC_Production;
GO

# Create application user (optional, more secure than SA)
USE GCC_Production;
CREATE LOGIN gcc_app WITH PASSWORD = 'AppP@ssw0rd2024!';
CREATE USER gcc_app FOR LOGIN gcc_app;
ALTER ROLE db_owner ADD MEMBER gcc_app;
GO
EXIT
```

### Step 2: Run Migrations

```bash
# Run migrations from application container
docker exec -it gcc-webapp-prod dotnet ef database update

# Or run as separate job
docker run --rm \
  --network gcc-network \
  -e ConnectionStrings__DefaultConnection="Server=sqlserver;Database=GCC_Production;User Id=sa;Password=Your$trongP@ssw0rd2024!" \
  your-dockerhub/gcc-webapp:production \
  dotnet ef database update
```

### Step 3: Seed Initial Data

```bash
# Run seed command
docker exec -it gcc-webapp-prod dotnet GCCWebApp.dll --seed-data

# Or create admin user
docker exec -it gcc-webapp-prod dotnet GCCWebApp.dll --create-admin \
  --email admin@your-domain.com \
  --password AdminP@ssw0rd2024!
```

---

## Monitoring and Maintenance

### Setup Monitoring in Plesk

1. **Install Monitoring Extension**:
   - Go to **Extensions Catalog**
   - Install **Monitoring** extension

2. **Configure Alerts**:
   - Set CPU usage alerts (> 80%)
   - Set Memory alerts (> 80%)
   - Set Disk space alerts (< 20% free)

### Docker Container Monitoring

```bash
# Create monitoring script
cat > /root/monitor-gcc.sh << 'EOF'
#!/bin/bash

# Check container health
check_health() {
    container=$1
    health=$(docker inspect --format='{{.State.Health.Status}}' $container 2>/dev/null)
    if [ "$health" != "healthy" ]; then
        echo "WARNING: $container is $health"
        # Send alert email
        echo "$container is $health" | mail -s "GCC Alert: $container unhealthy" admin@your-domain.com
    fi
}

# Check all containers
check_health gcc-webapp-prod
check_health gcc-sqlserver-prod
check_health gcc-redis-prod

# Check disk space
df -h | grep -E '^/dev/' | awk '{ if(int($5) > 80) print "WARNING: " $6 " is " $5 " full" }'

# Check memory
free -m | awk 'NR==2{printf "Memory Usage: %.2f%%\n", $3*100/$2 }'

# Container stats
docker stats --no-stream
EOF

chmod +x /root/monitor-gcc.sh

# Add to crontab (run every 5 minutes)
echo "*/5 * * * * /root/monitor-gcc.sh >> /var/log/gcc-monitor.log 2>&1" | crontab -
```

### Backup Configuration

```bash
# Create backup script
cat > /root/backup-gcc.sh << 'EOF'
#!/bin/bash

BACKUP_DIR="/backup/gcc/$(date +%Y%m%d_%H%M%S)"
mkdir -p $BACKUP_DIR

# Backup database
docker exec gcc-sqlserver-prod /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'Your$trongP@ssw0rd2024!' \
  -Q "BACKUP DATABASE [GCC_Production] TO DISK = N'/var/opt/mssql/backup.bak' WITH FORMAT"

docker cp gcc-sqlserver-prod:/var/opt/mssql/backup.bak $BACKUP_DIR/database.bak

# Backup volumes
docker run --rm \
  -v gcc_app-uploads:/data \
  -v $BACKUP_DIR:/backup \
  alpine tar czf /backup/uploads.tar.gz -C /data .

# Backup environment
cp /var/www/vhosts/gcc.your-domain.com/docker/.env.production $BACKUP_DIR/

# Keep only last 7 days
find /backup/gcc -type d -mtime +7 -exec rm -rf {} +

echo "Backup completed: $BACKUP_DIR"
EOF

chmod +x /root/backup-gcc.sh

# Schedule daily backup at 2 AM
echo "0 2 * * * /root/backup-gcc.sh >> /var/log/gcc-backup.log 2>&1" | crontab -
```

### Log Management

```bash
# Configure log rotation
cat > /etc/logrotate.d/gcc-docker << EOF
/var/lib/docker/containers/*/*.log {
    daily
    rotate 7
    compress
    delaycompress
    missingok
    notifempty
    create 0644 root root
}
EOF

# View logs
docker logs gcc-webapp-prod --tail 100 -f

# Export logs
docker logs gcc-webapp-prod > /var/log/gcc-app.log 2>&1
```

---

## Troubleshooting

### Common Issues and Solutions

#### 1. Container Won't Start

```bash
# Check logs
docker logs gcc-webapp-prod

# Check events
docker events --since '5m'

# Inspect container
docker inspect gcc-webapp-prod

# Common fix: Remove and recreate
docker-compose down
docker-compose up -d
```

#### 2. Database Connection Issues

```bash
# Test connection
docker exec gcc-webapp-prod curl http://sqlserver:1433

# Check SQL Server logs
docker logs gcc-sqlserver-prod

# Verify network
docker network inspect gcc-network

# Test from app container
docker exec -it gcc-webapp-prod /bin/bash
apt-get update && apt-get install -y iputils-ping
ping sqlserver
```

#### 3. Memory Issues

```bash
# Check memory usage
docker stats

# Set memory limits in docker-compose.yml
services:
  gccwebapp:
    mem_limit: 2g
    memswap_limit: 2g
```

#### 4. Port Conflicts

```bash
# Check port usage
netstat -tulpn | grep -E '5102|1433|6379'

# Kill process using port
fuser -k 5102/tcp
```

#### 5. SSL Certificate Issues

```bash
# Test SSL
openssl s_client -connect gcc.your-domain.com:443

# Renew Let's Encrypt
plesk bin extension --exec letsencrypt cli.php -d gcc.your-domain.com -m admin@your-domain.com
```

### Debug Mode

```bash
# Enable debug logging
docker-compose down

# Edit .env.production
echo "Logging__LogLevel__Default=Debug" >> .env.production

# Restart with verbose output
docker-compose up

# Watch logs in real-time
docker-compose logs -f --tail=100
```

---

## Rollback Procedures

### Quick Rollback

```bash
#!/bin/bash
# rollback.sh

# Stop current deployment
docker-compose -f docker-compose.production.yml down

# Restore previous image
docker tag gcc-webapp:production-backup gcc-webapp:production

# Restore database from backup
docker exec gcc-sqlserver-prod /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'Your$trongP@ssw0rd2024!' \
  -Q "RESTORE DATABASE [GCC_Production] FROM DISK = N'/backup/latest.bak' WITH REPLACE"

# Start services
docker-compose -f docker-compose.production.yml up -d

echo "Rollback completed"
```

### Full Disaster Recovery

```bash
#!/bin/bash
# disaster-recovery.sh

# Remove everything
docker-compose down -v
docker system prune -af

# Restore from backup
BACKUP_DIR="/backup/gcc/latest"

# Restore volumes
docker volume create gcc_app-uploads
docker run --rm \
  -v gcc_app-uploads:/data \
  -v $BACKUP_DIR:/backup \
  alpine tar xzf /backup/uploads.tar.gz -C /data

# Restore database
docker-compose up -d sqlserver
sleep 30
docker cp $BACKUP_DIR/database.bak gcc-sqlserver-prod:/var/opt/mssql/
docker exec gcc-sqlserver-prod /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'Your$trongP@ssw0rd2024!' \
  -Q "RESTORE DATABASE [GCC_Production] FROM DISK = N'/var/opt/mssql/database.bak' WITH REPLACE"

# Start application
docker-compose up -d

echo "Disaster recovery completed"
```

---

## Security Best Practices

### 1. Firewall Configuration

```bash
# Configure Plesk firewall
plesk bin firewall --enable
plesk bin firewall --add-rule --name "Docker GCC" --ports "5102/tcp,5103/tcp" --sources "127.0.0.1"

# Or use iptables
iptables -A INPUT -p tcp --dport 5102 -s 127.0.0.1 -j ACCEPT
iptables -A INPUT -p tcp --dport 5102 -j DROP
```

### 2. Docker Security

```bash
# Run security scan
docker scan gcc-webapp:production

# Use read-only root filesystem
services:
  gccwebapp:
    read_only: true
    tmpfs:
      - /tmp
      - /app/logs
```

### 3. Secrets Management

```bash
# Use Docker secrets instead of environment variables
echo "Your$trongP@ssw0rd2024!" | docker secret create db_password -

# Reference in compose file
services:
  sqlserver:
    secrets:
      - db_password
    environment:
      - SA_PASSWORD_FILE=/run/secrets/db_password
```

### 4. Regular Updates

```bash
# Update containers monthly
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker pull redis:7-alpine
docker pull your-dockerhub/gcc-webapp:production

docker-compose up -d
```

---

## Performance Optimization

### 1. Enable Docker BuildKit

```bash
export DOCKER_BUILDKIT=1
docker build -t gcc-webapp:production .
```

### 2. Multi-stage Builds

Already implemented in your Dockerfile

### 3. Container Resource Limits

```yaml
services:
  gccwebapp:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '0.5'
          memory: 512M
```

### 4. Database Optimization

```sql
-- Add indexes for common queries
CREATE INDEX IX_Certificates_Status ON Certificates(Status);
CREATE INDEX IX_Certificates_CompanyId ON Certificates(CompanyId);
CREATE INDEX IX_Certificates_CreatedDate ON Certificates(CreatedDate DESC);

-- Update statistics
UPDATE STATISTICS Certificates;
```

---

## Maintenance Schedule

### Daily Tasks
- Check application health endpoint
- Review error logs
- Verify backup completion

### Weekly Tasks
- Review resource usage
- Check for security updates
- Test backup restoration

### Monthly Tasks
- Update Docker images
- Review and rotate logs
- Performance analysis
- Security audit

### Quarterly Tasks
- Full disaster recovery test
- SSL certificate renewal check
- Capacity planning review
- Update documentation

---

## Support and Resources

### Useful Commands Reference

```bash
# Container Management
docker ps -a                          # List all containers
docker logs gcc-webapp-prod -f        # Follow logs
docker exec -it gcc-webapp-prod bash  # Shell access
docker restart gcc-webapp-prod        # Restart container

# Compose Management  
docker-compose ps                     # List services
docker-compose down                   # Stop all services
docker-compose up -d                  # Start all services
docker-compose restart                # Restart all services

# Cleanup
docker system prune -a                # Clean everything
docker volume prune                   # Clean unused volumes
docker image prune -a                 # Clean unused images
```

### Monitoring URLs

After deployment, access these endpoints:

- **Application**: https://gcc.your-domain.com
- **Health Check**: https://gcc.your-domain.com/health
- **Swagger API**: https://gcc.your-domain.com/swagger (if enabled)

### Getting Help

1. **Check Logs First**:
   ```bash
   docker logs gcc-webapp-prod --tail 100
   docker-compose logs
   ```

2. **Plesk Support**: https://support.plesk.com
3. **Docker Documentation**: https://docs.docker.com
4. **Application Issues**: Check `/app/logs` in container

---

## Checklist for Production Deployment

### Pre-Deployment
- [ ] Server meets minimum requirements
- [ ] Docker and Docker Compose installed
- [ ] Plesk Docker extension installed
- [ ] Domain configured in Plesk
- [ ] SSL certificate obtained
- [ ] Environment variables configured
- [ ] Secrets secured
- [ ] Backup strategy defined

### Deployment
- [ ] Images pushed to registry
- [ ] Compose file uploaded
- [ ] Environment file configured
- [ ] Containers started successfully
- [ ] Health checks passing
- [ ] Database migrated
- [ ] Initial data seeded

### Post-Deployment
- [ ] Application accessible via HTTPS
- [ ] All features working
- [ ] Monitoring configured
- [ ] Backup scheduled
- [ ] Log rotation configured
- [ ] Documentation updated
- [ ] Team notified

### Security
- [ ] Firewall rules configured
- [ ] Ports restricted to localhost
- [ ] Strong passwords set
- [ ] 2FA enabled for admin
- [ ] Security headers configured
- [ ] Regular updates scheduled

---

**Document Version**: 1.0  
**Last Updated**: August 2024  
**Author**: GCC DevOps Team

---