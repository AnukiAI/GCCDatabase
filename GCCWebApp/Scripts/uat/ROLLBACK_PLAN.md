# Rollback Plan

Emergency procedures to revert to previous version if deployment fails.

## When to Rollback

Initiate rollback if:
- Application completely non-functional for 10+ minutes
- Critical security vulnerability discovered
- Database corruption detected
- Authentication completely broken
- Data loss detected

## Rollback Decision Tree

```
Application Down?
    |
    +-- NO --> Monitor, may recover
    |
    +-- YES --> How long?
                |
                +-- < 5 min --> Continue troubleshooting
                |
                +-- > 10 min --> INITIATE ROLLBACK
```

## Pre-Rollback Requirements

Before any deployment, ensure:
- [ ] Previous version backup exists on server
- [ ] Previous version backup downloaded locally
- [ ] Database backup from before deployment
- [ ] Team notified of rollback procedures

## Rollback Procedure

### Step 1: Stop Application (1 minute)

```
1. Login to Plesk control panel
2. Navigate to GCC application
3. Stop the application process
```

### Step 2: Restore Previous Files (5-10 minutes)

**Option A: Server has backup folder**
```
1. In Plesk File Manager
2. Rename current folder: httpdocs -> httpdocs_failed_[date]
3. Rename backup: httpdocs_backup_[date] -> httpdocs
4. Restart application
```

**Option B: Upload from local backup**
```
1. In Plesk File Manager
2. Delete or rename failed deployment folder
3. Upload previous version ZIP
4. Extract to httpdocs
5. Restart application
```

### Step 3: Verify Database (2 minutes)

If database was modified:
```sql
-- Check for any new tables that shouldn't exist
SELECT name FROM sys.tables WHERE create_date > '[deployment_date]';

-- If issues found, restore database backup
-- Contact DBA for database restore if needed
```

### Step 4: Restart Application (1 minute)

```
1. In Plesk, restart application
2. Wait for startup to complete
3. Check logs for errors
```

### Step 5: Verify Rollback (5 minutes)

- [ ] Homepage loads
- [ ] Login works
- [ ] Core features functional
- [ ] No new errors in logs

### Step 6: Notify Team

```
Subject: [GCC] Rollback Completed

Deployment rolled back due to: [reason]
Previous version restored: [version/date]
Current status: [Operational/Degraded]
Next steps: [Investigation/Fix/Retry]
```

## Rollback Files Location

| Backup Type | Location | Retention |
|-------------|----------|-----------|
| Server backup | /httpdocs_backup_[date] | Keep 3 versions |
| Local backup | GCCWebApp/backups/ | Keep 5 versions |
| Database | AccuWebHosting backups | Daily automatic |

## Database Rollback

**CAUTION**: Database rollback affects ALL data since backup

```sql
-- DO NOT run without DBA approval
-- Database restore requires AccuWebHosting support ticket
-- Or use backup restore from Plesk database management
```

## Post-Rollback Analysis

After rollback:
1. Collect all logs from failed deployment
2. Document exact failure point
3. Create bug report with reproduction steps
4. Schedule fix review meeting
5. Plan next deployment attempt

## Emergency Contacts

| Role | Contact | When to Contact |
|------|---------|-----------------|
| DBA | [TBD] | Database issues |
| Hosting | AccuWebHosting Support | Server issues |
| Team Lead | [TBD] | All rollbacks |

## Rollback Prevention

To minimize rollbacks:
1. Always deploy to staging first
2. Run full POST_DEPLOYMENT_CHECKLIST.md
3. Deploy during low-traffic hours
4. Have team member available during deployment
5. Keep previous version backup accessible

## Quick Rollback Commands

```bash
# On server (if SSH access available)
cd /var/www/vhosts/gccwebapp.com/
mv httpdocs httpdocs_failed_$(date +%Y%m%d)
mv httpdocs_backup httpdocs
systemctl restart gcc-webapp

# Or via Plesk:
# 1. Stop app
# 2. File Manager > Rename folders
# 3. Start app
```

## Rollback Checklist

- [ ] Application stopped
- [ ] Failed files preserved (for analysis)
- [ ] Backup files restored
- [ ] Application restarted
- [ ] Basic functionality verified
- [ ] Team notified
- [ ] Incident documented
