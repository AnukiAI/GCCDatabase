# Docker Deployment Guide for GCC Web Application

## 🚀 Overview

This guide shows you how to deploy the GCC Web Application using Docker containers. This approach is **much easier** than installing .NET Core directly and provides better isolation and portability.

## 📋 Prerequisites

### Required Software
1. **Docker Desktop** (Windows/Mac) or **Docker Engine** (Linux)
   - Download from: https://www.docker.com/products/docker-desktop
   - Minimum 4GB RAM allocated to Docker
   - At least 10GB free disk space

2. **Docker Compose** (usually included with Docker Desktop)

### System Requirements
- **Windows**: Windows 10/11 with WSL2 enabled
- **Linux**: Any modern distribution
- **macOS**: macOS 10.15 or newer

## 📁 Files Created

Your project now includes these Docker-related files:

```
GCCWebApp/
├── Dockerfile                    # Main application container
├── docker-compose.yml           # PostgreSQL setup
├── docker-compose.mariadb.yml   # MariaDB setup (alternative)
├── .env.example                 # Environment variables template
├── deploy.sh                    # Linux/Mac deployment script
├── deploy.cmd                   # Windows deployment script
└── init-scripts/
    └── 01-init-database.sql     # Database initialization
```

## 🛠️ Quick Start

### 1. Setup Environment Variables

```bash
# Copy the environment template
cp .env.example .env

# Edit .env with your settings
nano .env  # or use any text editor
```

**Required changes in `.env`:**
```bash
# Secure database password
DB_PASSWORD=your_very_secure_password_here

# JWT secret key (must be 64+ characters)
JWT_SECRET_KEY=your-super-secret-jwt-key-must-be-at-least-64-characters-long-for-security-compliance

# SMTP configuration for 2FA emails
SMTP_HOST=smtp.gmail.com
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-specific-password
SMTP_FROM_EMAIL=noreply@yourdomain.com
```

### 2. Deploy the Application

**Windows:**
```cmd
deploy.cmd
```

**Linux/Mac:**
```bash
chmod +x deploy.sh
./deploy.sh
```

### 3. Access the Application

- **Application**: http://localhost:8080
- **Database**: PostgreSQL on port 5432 (or MariaDB on 3306)

### 4. Default Login Credentials

```
Email: admin@greencirclecertified.com
Password: admin123
```

## 🔧 Deployment Options

### PostgreSQL (Default)
```bash
./deploy.sh --database postgresql
```

### MariaDB
```bash
./deploy.sh --database mariadb
```

### Advanced Options
```bash
# Rebuild from scratch
./deploy.sh --rebuild

# Use custom environment file
./deploy.sh --env-file .env.production

# View logs
./deploy.sh --logs

# Check status
./deploy.sh --status

# Stop containers
./deploy.sh --down
```

## 🗄️ Database Options

### Option 1: PostgreSQL (Recommended)
- **Pros**: Better JSON support, more robust, better for complex queries
- **Cons**: Slightly more memory usage
- **Use for**: Production environments, complex data relationships

### Option 2: MariaDB/MySQL
- **Pros**: Familiar to MySQL users, slightly lighter
- **Cons**: Less advanced JSON features
- **Use for**: Existing MySQL infrastructure, simpler deployments

## 📧 Email Configuration for 2FA

### Gmail Setup
1. Enable 2-Factor Authentication on your Gmail account
2. Generate an App Password: https://myaccount.google.com/apppasswords
3. Use these settings in `.env`:
```bash
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_ENABLE_SSL=true
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-16-character-app-password
```

### SendGrid Setup
```bash
SMTP_HOST=smtp.sendgrid.net
SMTP_PORT=587
SMTP_ENABLE_SSL=true
SMTP_USERNAME=apikey
SMTP_PASSWORD=your-sendgrid-api-key
```

### Disable 2FA (Testing Only)
```bash
ENABLE_2FA=false
```

## 🐳 Container Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Web App       │    │   Database      │    │   Redis Cache   │
│   (Port 8080)   │◄──►│   (Port 5432)   │    │   (Port 6379)   │
│                 │    │                 │    │   (Optional)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Containers Included:
1. **gccwebapp**: Main application (.NET 8.0)
2. **postgres** (or **mariadb**): Database
3. **redis**: Caching layer for 2FA codes (optional but recommended)

## 🔒 Security Features

### Included Security Measures:
- ✅ Non-root user in application container
- ✅ Environment variable isolation
- ✅ Network isolation between containers
- ✅ Health checks for all services
- ✅ Automatic database initialization
- ✅ Secure password storage
- ✅ JWT token security
- ✅ Rate limiting

### Production Security Checklist:
- [ ] Change default database passwords
- [ ] Set strong JWT secret key
- [ ] Configure proper SMTP credentials
- [ ] Use HTTPS reverse proxy (nginx/traefik)
- [ ] Regular container updates
- [ ] Backup strategy for database volumes

## 🌐 Production Deployment

### Reverse Proxy Setup (nginx example)
```nginx
server {
    listen 80;
    server_name yourdomain.com;
    
    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### SSL Certificate (Let's Encrypt)
```bash
# Install certbot
sudo apt install certbot python3-certbot-nginx

# Get certificate
sudo certbot --nginx -d yourdomain.com
```

### Environment Variables for Production
```bash
# Update .env for production
ASPNETCORE_ENVIRONMENT=Production
JWT_SECRET_KEY=your-production-secret-key-64-chars-minimum
DB_PASSWORD=your-very-secure-production-password
SMTP_HOST=your-production-smtp-server
```

## 📊 Monitoring and Logs

### View Application Logs
```bash
# Real-time logs
./deploy.sh --logs

# Specific container logs
docker logs gcc-webapp
docker logs gcc-postgres
docker logs gcc-redis
```

### Health Checks
```bash
# Check container health
docker ps

# Application health endpoint
curl http://localhost:8080/health
```

### Database Access
```bash
# PostgreSQL
docker exec -it gcc-postgres psql -U gcc_user -d gcc_database

# MariaDB
docker exec -it gcc-mariadb mysql -u gcc_user -p gcc_database
```

## 🔧 Troubleshooting

### Common Issues

**1. Port Already in Use**
```bash
# Check what's using the port
netstat -tulpn | grep :8080

# Kill the process or change port in docker-compose.yml
```

**2. Database Connection Failed**
- Check database container is running: `docker ps`
- Verify environment variables in `.env`
- Check database logs: `docker logs gcc-postgres`

**3. Email Not Sending**
- Verify SMTP credentials in `.env`
- Check application logs for email errors
- Test with `ENABLE_2FA=false` first

**4. Container Build Fails**
```bash
# Clean rebuild
./deploy.sh --down
docker system prune -f
./deploy.sh --rebuild
```

**5. Permission Denied**
```bash
# Linux/Mac: Make scripts executable
chmod +x deploy.sh

# Windows: Run as Administrator
```

### Reset Everything
```bash
# Stop and remove all containers
./deploy.sh --down

# Remove all data (WARNING: This deletes your database!)
docker volume rm gccwebapp_postgres-data gccwebapp_redis-data

# Start fresh
./deploy.sh --rebuild
```

## 🚀 Deployment to Cloud

### AWS (using ECS or EC2)
1. Push image to ECR
2. Use ECS task definitions
3. Configure RDS for database
4. Use ElastiCache for Redis

### Azure (using Container Instances)
1. Push to Azure Container Registry
2. Use Azure Container Instances
3. Configure Azure Database for PostgreSQL
4. Use Azure Cache for Redis

### Google Cloud (using Cloud Run)
1. Push to Google Container Registry
2. Deploy to Cloud Run
3. Use Cloud SQL for PostgreSQL
4. Use Memorystore for Redis

## 📚 Additional Resources

- **Docker Documentation**: https://docs.docker.com/
- **ASP.NET Core in Docker**: https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/
- **PostgreSQL Docker Image**: https://hub.docker.com/_/postgres
- **MariaDB Docker Image**: https://hub.docker.com/_/mariadb

## 💡 Tips for Success

1. **Start Simple**: Use default PostgreSQL setup first
2. **Test Locally**: Always test with Docker locally before production
3. **Backup Strategy**: Implement regular database backups
4. **Monitor Resources**: Watch CPU/Memory usage in production
5. **Keep Updated**: Regularly update base images for security

---

**Need Help?** Check the logs first with `./deploy.sh --logs` - most issues are configuration-related and will show clear error messages.

Last Updated: August 21, 2025