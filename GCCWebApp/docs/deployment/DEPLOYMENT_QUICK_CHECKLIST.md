# Quick Deployment Checklist - gccwebapp.com

**Package:** GCCWebApp-Production-20251009-FIXED.zip (185 MB)
**Location:** `GCC Database Solution\GCCWebApp\GCCWebApp-Production-20251009-FIXED.zip`

---

## Pre-Deployment

- [ ] Have Plesk login credentials ready
- [ ] Backup current httpdocs folder (if exists)

---

## Deployment Steps

### 1. Upload
- [ ] Login to Plesk: https://manage.accuwebhosting.com/
- [ ] Go to Files → File Manager → httpdocs
- [ ] Upload `GCCWebApp-Production-20251009-FIXED.zip`

### 2. Extract
- [ ] Right-click ZIP → Extract Files
- [ ] Extract to current directory (httpdocs)
- [ ] Verify files extracted (GCCWebApp.dll, web.config, wwwroot/, etc.)

### 3. Environment Variable (CRITICAL!)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`

**Where to set in Plesk:**
- Websites & Domains → gccwebapp.com → IIS Settings → Application Pool → Environment Variables

**OR via web.config:**
- Edit web.config in httpdocs
- Add under `<aspNetCore>`:
```xml
<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
</environmentVariables>
```

### 4. Permissions
- [ ] Create logs folder in httpdocs (if not exists)
- [ ] Set logs folder permissions to 755 or 777

### 5. Restart
- [ ] Restart IIS Application Pool for gccwebapp.com
- [ ] OR contact AccuWeb support to restart

---

## Verification

- [ ] Visit https://gccwebapp.com/ → Should load without HTTP 500
- [ ] Test https://gccwebapp.com/Certificate/CertificateTileView
- [ ] Test https://gccwebapp.com/Account/Login
- [ ] Check https://gccwebapp.com/health → Should return "Healthy"

---

## If Issues

1. **Check logs:** httpdocs/logs/stdout_*.log
2. **Enable detailed errors temporarily:**
   - Edit appsettings.Production.json
   - Set `EnableDetailedErrorMessages: true`
   - Set `EnableDeveloperExceptionPage: true`
   - Restart app pool
   - Visit site to see error
   - Set both back to false!

3. **Contact AccuWeb Support:**
   - Ticket: KFU974586N
   - Ask to verify ASP.NET Core 8.0 Hosting Bundle installed
   - Ask to verify environment variable is set

---

## Post-Deployment

- [ ] Change default passwords (admin@greencirclecertified.com, etc.)
- [ ] Verify HTTPS working
- [ ] Monitor logs for errors
- [ ] Test all major functions

---

**Detailed Guide:** See DEPLOYMENT_TO_GCCWEBAPP_COM.md
