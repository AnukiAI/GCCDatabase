# GCC Web Application - Plesk Deployment Walkthrough

## Current Status
✅ **Application is containerized and ready for deployment**
- Production container running locally on port 5105 (healthy)
- Docker image: `gcc-webapp:production`
- GitHub Registry: `ghcr.io/anukiai/gcc-webapp:latest`

## Quick Deployment Process

### 1. Prepare Your Plesk Server (15 minutes)

```bash
# SSH into your Plesk server
ssh root@your-plesk-server.com

# Install Docker if needed
curl -fsSL https://get.docker.com | sh
systemctl start docker
systemctl enable docker

# Create PostgreSQL database
sudo -u postgres psql
CREATE DATABASE gccdb;
CREATE USER gccuser WITH PASSWORD 'ChooseSecurePassword';
GRANT ALL PRIVILEGES ON DATABASE gccdb TO gccuser;
\q

# Test database connection
psql -h localhost -U gccuser -d gccdb -c "SELECT 1;"
```

### 2. Deploy the Container (5 minutes)

```bash
# Create app directory
mkdir -p /opt/gcc-webapp
cd /opt/gcc-webapp

# Create deployment script
cat > deploy.sh << 'EOF'
#!/bin/bash
# GCC Web App Deployment

# Stop old container
docker stop gcc-webapp 2>/dev/null
docker rm gcc-webapp 2>/dev/null

# Run new container
docker run -d \
  --name gcc-webapp \
  --restart unless-stopped \
  -p 5102:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Database=gccdb;Username=gccuser;Password=ChooseSecurePassword;Port=5432" \
  -e Jwt__Key="GenerateA64CharacterSecureRandomStringHereForProductionUse123456789" \
  gcc-webapp:production

echo "Container deployed. Checking status..."
sleep 5
docker ps | grep gcc-webapp
EOF

chmod +x deploy.sh

# Run deployment
./deploy.sh
```

### 3. Configure Plesk Nginx (5 minutes)

In Plesk Panel:
1. Go to your domain → "Apache & nginx Settings"
2. Add to "Additional nginx directives":

```nginx
location / {
    proxy_pass http://localhost:5102;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection keep-alive;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_cache_bypass $http_upgrade;
}
```

3. Click "OK" to apply

### 4. Enable SSL (2 minutes)

In Plesk Panel:
1. Go to "SSL/TLS Certificates"
2. Click "Install Free Basic Certificate"
3. Choose "Let's Encrypt"
4. Click "Get it free"

### 5. Verify Deployment (3 minutes)

```bash
# Check container
docker ps
docker logs gcc-webapp

# Test endpoints
curl http://localhost:5102/health
curl https://your-domain.com

# Check application
# Browse to: https://your-domain.com
# You should see the certificate tile view
```

## Total Deployment Time: ~30 minutes

## What You Get

✅ **Fully functional GCC Web Application with:**
- Public certificate tile view (landing page)
- User authentication system
- Admin panel for certificate management
- API endpoints for external partners
- PDF generation for certificates
- Search and filter capabilities
- Role-based access control

## Environment Variables Summary

**Minimum Required:**
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection="Host=localhost;Database=gccdb;Username=gccuser;Password=YourPassword;Port=5432"
Jwt__Key="[64+ character secure string]"
```

**Optional (for enhanced features):**
```bash
GCC_AZURE_AD_B2C_CLIENT_SECRET="[if using Azure B2C]"
GCC_SMTP_PASSWORD="[if sending emails]"
GCC_APPLICATION_INSIGHTS_CONNECTION_STRING="[if using monitoring]"
```

## Current Working Configuration

Based on our testing, this configuration is **confirmed working**:

```bash
docker run -d \
  --name gcc-webapp \
  --restart unless-stopped \
  -p 5102:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="http://+:8080" \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Database=gccdb;Username=gccuser;Password=YourPassword;Port=5432" \
  -e Jwt__Key="ThisIsAVeryLongSecretKeyForJwtTokenGenerationAtLeast64CharactersLong123456789" \
  gcc-webapp:production
```

## Troubleshooting

### If container won't start:
```bash
docker logs gcc-webapp
# Usually database connection issue - verify PostgreSQL is running
```

### If you get 502 Bad Gateway:
```bash
# Check if container is running
docker ps
# Check if port 5102 is listening
netstat -tlnp | grep 5102
```

### If assets don't load:
```bash
# Verify nginx configuration
nginx -t
systemctl reload nginx
```

## Support Files

All necessary files are included in the project:
- `docker-deploy-plesk-FIXED.cmd` - Windows deployment script
- `Dockerfile` - Container build file
- `docker-compose.yml` - Full stack deployment
- `.env.plesk` - Environment template

## Next Steps After Deployment

1. **Test core functionality:**
   - Browse certificate tiles
   - Test search
   - Login as admin
   - Create test certificate

2. **Set up backups:**
   ```bash
   # Add to crontab
   0 2 * * * pg_dump -U gccuser gccdb > /backup/gccdb_$(date +\%Y\%m\%d).sql
   ```

3. **Monitor health:**
   ```bash
   # Add health check to monitoring
   */5 * * * * curl -f http://localhost:5102/health || docker restart gcc-webapp
   ```

---
**Ready to deploy? Start with Step 1 above!**

*Deployment typically takes 30 minutes from start to finish.*