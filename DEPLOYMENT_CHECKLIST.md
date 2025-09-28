# GCC Web Application - Plesk .NET 8.0 Deployment Checklist

## Pre-Deployment Requirements

### Server Infrastructure
- [ ] Plesk Control Panel with .NET Toolkit Extension v2.3.0+
- [ ] .NET 8.0 Runtime support confirmed
- [ ] SSH access to Plesk server
- [ ] Domain configured in Plesk
- [ ] Database server (SQL Server/MySQL/SQLite)
- [ ] Minimum 2GB RAM available
- [ ] 5GB disk space available

### Access Requirements
- [ ] SSH access to server
- [ ] Plesk admin credentials
- [ ] Database admin credentials (if using external DB)
- [ ] Deployment package ready

## Deployment Steps Checklist

### Step 1: Local Build Verification
- [x] Application builds successfully with .NET 8.0
- [x] Production deployment package created (`gccwebapp-dotnet8.tar.gz`)
- [x] Deployment scripts ready (`deploy-to-plesk-dotnet8.sh`)
- [x] Configuration templates prepared
- [x] All nullable reference warnings resolved

### Step 2: Server Preparation
- [ ] Verify Plesk .NET Toolkit Extension is installed
- [ ] Confirm .NET 8.0 Runtime is available
- [ ] Test SSH access to server
- [ ] Backup existing application (if any)

### Step 3: Application Deployment
- [ ] Upload deployment package (`gccwebapp-dotnet8.tar.gz`)
- [ ] Extract to domain `/httpdocs/` directory
- [ ] Set correct permissions (`psacln:psaserv`)
- [ ] Verify essential files present (GCCWebApp.dll, wwwroot/, etc.)

### Step 4: Plesk .NET Toolkit Configuration
- [ ] Navigate to Websites & Domains → Your Domain
- [ ] Click '.NET Toolkit'
- [ ] Configure settings:
  - [ ] Startup File: `GCCWebApp.dll`
  - [ ] ASP.NET Core Version: `8.0`
  - [ ] Environment: `Production`
  - [ ] Port: (auto-assigned by Plesk)

### Step 5: Environment Configuration
- [ ] Set environment variables in Plesk:
  - [ ] `ASPNETCORE_ENVIRONMENT=Production`
  - [ ] `ConnectionStrings__DefaultConnection=your_connection_string`
  - [ ] `GCC_JWT_SECRET=your_jwt_secret_min_64_chars`
- [ ] Update `appsettings.Production.json` if needed

### Step 6: SSL Configuration
- [ ] SSL certificate installed
- [ ] HTTPS redirect configured
- [ ] Certificate auto-renewal enabled
- [ ] Mixed content issues resolved
- [ ] Security headers verified

## Post-Deployment Verification

### Application Functionality
- [ ] Home page loads correctly
- [ ] Static assets loading (CSS/JS/Images)
- [ ] Login functionality working
- [ ] Certificate tile view displaying
- [ ] Search functionality operational
- [ ] PDF download working
- [ ] Admin panel accessible (admin users)

### API Endpoints
- [ ] `/health` endpoint responding
- [ ] `/api/health` endpoint responding
- [ ] Public API endpoints accessible
- [ ] Authentication working for protected endpoints
- [ ] Rate limiting functioning

### Security Checks
- [ ] HTTPS enforced
- [ ] Security headers present
- [ ] No sensitive data in logs
- [ ] Database connections encrypted
- [ ] Firewall rules configured
- [ ] Container running as non-root user

### Performance Checks
- [ ] Page load time < 3 seconds
- [ ] API response time < 1 second
- [ ] Memory usage stable
- [ ] CPU usage normal
- [ ] No memory leaks detected
- [ ] Database queries optimized

## Monitoring Setup

### Logging
- [ ] Application logs accessible
- [ ] Error logs being captured
- [ ] Log rotation configured
- [ ] Log aggregation setup (optional)

### Health Monitoring
- [ ] Health check endpoint monitored
- [ ] Container auto-restart configured
- [ ] Alerts configured for failures
- [ ] Uptime monitoring active

### Backup Strategy
- [ ] Database backup scheduled
- [ ] Backup retention policy defined
- [ ] Backup restoration tested
- [ ] Off-site backup configured

## Rollback Plan

### Rollback Preparation
- [ ] Previous container image tagged
- [ ] Database backup before deployment
- [ ] Configuration files backed up
- [ ] Rollback script prepared

### Rollback Steps
1. [ ] Stop current container
2. [ ] Restore database backup (if needed)
3. [ ] Deploy previous container version
4. [ ] Verify application functionality
5. [ ] Communicate status to team

## Common Issues Checklist

### Container Won't Start
- [ ] Check docker logs for errors
- [ ] Verify environment variables
- [ ] Check port availability
- [ ] Verify database connection
- [ ] Check disk space

### Application Returns 500 Error
- [ ] Check application logs
- [ ] Verify database migrations
- [ ] Check connection strings
- [ ] Verify JWT configuration
- [ ] Check file permissions

### SSL/HTTPS Issues
- [ ] Certificate properly installed
- [ ] Nginx configuration correct
- [ ] Firewall allows port 443
- [ ] DNS properly configured

### Performance Issues
- [ ] Database indexes created
- [ ] Sufficient memory allocated
- [ ] CDN configured (optional)
- [ ] Caching enabled
- [ ] Database connection pooling

## Sign-off

### Deployment Team
- [ ] DevOps Engineer: _______________ Date: ___________
- [ ] System Administrator: ___________ Date: ___________
- [ ] QA Tester: ____________________ Date: ___________
- [ ] Project Manager: _______________ Date: ___________

### Production Readiness
- [ ] All checklist items completed
- [ ] No critical issues remaining
- [ ] Rollback plan tested
- [ ] Team trained on procedures
- [ ] Documentation updated

## Quick Commands Reference

```bash
# Check container status
docker ps | grep gcc

# View logs
docker logs -f gcc-webapp

# Restart container
docker restart gcc-webapp

# Check health
curl http://localhost:5102/health

# Database backup
pg_dump -h localhost -U gccuser gccdb > backup.sql

# Emergency stop
docker stop gcc-webapp
```

## Support Contacts

- **DevOps Team**: devops@greencirclecertified.com
- **Emergency Hotline**: +1-XXX-XXX-XXXX
- **Slack Channel**: #gcc-deployment
- **Documentation**: /opt/gcc-webapp/docs

---
*Version: 1.0*
*Last Updated: September 16, 2025*
*Next Review: October 16, 2025*