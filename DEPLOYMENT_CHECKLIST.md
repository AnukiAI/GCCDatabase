# GCC Web Application - Deployment Verification Checklist

## Pre-Deployment Checklist

### Infrastructure Requirements
- [ ] Plesk server with root/sudo access
- [ ] Docker installed and running
- [ ] PostgreSQL or MariaDB installed
- [ ] Domain configured in Plesk
- [ ] SSL certificate (Let's Encrypt or custom)
- [ ] Minimum 2GB RAM available
- [ ] 10GB disk space available

### Access Requirements
- [ ] SSH/RDP access to server
- [ ] Plesk admin credentials
- [ ] Database admin credentials
- [ ] GitHub Container Registry access token

## Deployment Steps Checklist

### Step 1: Database Setup
- [ ] Database server installed (PostgreSQL/MariaDB)
- [ ] Database created (`gccdb`)
- [ ] Database user created (`gccuser`)
- [ ] Permissions granted
- [ ] Connection tested successfully
- [ ] Backup of existing data (if applicable)

### Step 2: Docker Configuration
- [ ] Docker daemon running
- [ ] Docker compose installed (optional)
- [ ] GitHub registry authenticated
- [ ] Image pulled successfully
- [ ] Old containers cleaned up

### Step 3: Environment Configuration
- [ ] `.env` file created
- [ ] Database connection string configured
- [ ] JWT secret key generated (64+ chars)
- [ ] Domain URLs configured
- [ ] Port mappings verified
- [ ] Feature flags set appropriately

### Step 4: Container Deployment
- [ ] Container started successfully
- [ ] Container health check passing
- [ ] Logs showing no errors
- [ ] Application responding on configured port
- [ ] Database migrations completed

### Step 5: Nginx/Proxy Configuration
- [ ] Nginx configuration added in Plesk
- [ ] Proxy headers configured correctly
- [ ] WebSocket support enabled (if needed)
- [ ] Nginx configuration tested
- [ ] Nginx reloaded successfully

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