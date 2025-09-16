# 🚀 GCC WebApp - Quick Deployment Guide

## Choose Your Deployment Type

### 🟢 Simple Deployment (Recommended)
**Best for**: Small teams, development, testing
**Database**: SQLite (single file)
**Features**: Basic authentication, local file storage

```bash
# One command deployment
curl -s https://raw.githubusercontent.com/your-repo/deploy-plesk-simple.sh | bash

# Or manual:
chmod +x deploy-plesk-simple.sh
./deploy-plesk-simple.sh
```

### 🟡 PostgreSQL Deployment
**Best for**: Production with multiple users
**Database**: PostgreSQL
**Features**: Scalable database, better performance

```bash
# Set your database password first
export DB_PASSWORD="your_secure_password"
export JWT_SECRET="your_64_character_jwt_secret"

chmod +x deploy-plesk-postgresql.sh
./deploy-plesk-postgresql.sh
```

### 🔴 Enterprise Deployment
**Best for**: Large organizations using Azure
**Database**: PostgreSQL
**Features**: Azure AD, monitoring, cloud storage

```bash
# Set all Azure credentials
export DB_PASSWORD="your_db_password"
export JWT_SECRET="your_jwt_secret"
export AZURE_B2C_SECRET="your_azure_b2c_secret"
export AZURE_AD_SECRET="your_azure_ad_secret"
export AZURE_BLOB_CONNECTION="your_blob_connection_string"

chmod +x deploy-plesk-enterprise.sh
./deploy-plesk-enterprise.sh
```

## ⚡ Emergency Fixes

### Container Won't Start
```bash
# Check logs
docker logs gcc-webapp

# Restart container
docker restart gcc-webapp

# Nuclear option - redeploy
docker stop gcc-webapp && docker rm gcc-webapp
./deploy-plesk-simple.sh
```

### Generate New JWT Secret
```bash
# Generate secure 64-character key
openssl rand -base64 48

# Or if openssl not available
head -c 48 /dev/urandom | base64
```

### Database Issues
```bash
# Check SQLite database
ls -la /var/www/vhosts/*/httpdocs/data/gcc_database.db

# Reset database (WARNING: destroys data)
rm /var/www/vhosts/*/httpdocs/data/gcc_database.db
docker restart gcc-webapp
```

## 🔍 Health Checks

### Quick Status Check
```bash
curl -I http://localhost:5101
# ✅ 200 or 302 = Working
# ❌ Connection refused = Container down
# ❌ 500 = Configuration error
```

### Detailed Status
```bash
# Container status
docker ps | grep gcc-webapp

# Resource usage
docker stats gcc-webapp --no-stream

# Recent logs
docker logs gcc-webapp --tail 20
```

## 📱 Accessing Your App

- **Local**: http://localhost:5101
- **Plesk**: http://angry-pascal.70-32-82-212.plesk.page:5101
- **Admin Login**: admin@greencirclecertified.com / Admin123!

## 🆘 Support Commands

```bash
# Get container info
docker inspect gcc-webapp

# Enter container for debugging
docker exec -it gcc-webapp bash

# Export container logs
docker logs gcc-webapp > gcc-app.log

# Check environment variables
docker exec gcc-webapp env | grep -E "(JWT|CONNECTION|AZURE)"
```

## 📞 Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| Container exits immediately | Check environment variables |
| "Missing critical secrets" | Use deployment scripts with proper config |
| Database connection failed | Verify connection string format |
| App returns 500 errors | Check logs with `docker logs gcc-webapp` |
| Can't access from outside | Check port 5101 is open in firewall |

---
**Need help?** Check the full `CONFIGURATION_GUIDE.md` for detailed setup instructions.