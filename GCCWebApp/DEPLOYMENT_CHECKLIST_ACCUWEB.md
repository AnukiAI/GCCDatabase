# AccuWeb IIS Deployment Checklist

## ✅ Pre-Deployment Verification

### Local Build
- [ ] Build succeeds: `dotnet build -c Release`
- [ ] Publish succeeds: `dotnet publish -c Release -p:PublishProfile=AccuWebProduction`
- [ ] Published files at: `bin\Release\net8.0\win-x64\publish\`

### Required Files Present
- [ ] `GCCWebApp.dll`
- [ ] `web.config`
- [ ] `appsettings.Production.json`
- [ ] `wwwroot/` folder with CSS/JS/images

## 🔧 AccuWeb Server Setup

### Prerequisites
- [ ] .NET 8.0 Runtime installed on server
- [ ] ASP.NET Core Module V2 (ANCM) installed
- [ ] URL Rewrite Module installed
- [ ] SQL Server database created and accessible

### IIS Configuration
- [ ] Application Pool: .NET CLR Version = **No Managed Code**
- [ ] Pipeline Mode = **Integrated**
- [ ] Enable 32-bit = **False**

## 🔐 Configuration (CRITICAL)

### Database Connection
Choose one method and check when complete:

- [ ] **Method A**: Updated `appsettings.Production.json` before publish
- [ ] **Method B**: Added to `web.config` environmentVariables
- [ ] **Method C**: Set in AccuWeb Application Settings panel

Connection string format:
```
Server={server};Database=GCCDatabase;User Id={user};Password={pass};Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;
```

### JWT Secret (64+ chars required)
- [ ] Generated new random secret (minimum 64 characters)
- [ ] Set `GCC_JWT_SECRET` environment variable
- [ ] **NOT** using default/example value

### Environment
- [ ] `ASPNETCORE_ENVIRONMENT=Production` set

## 📤 File Upload

- [ ] Connected to AccuWeb FTP/File Manager
- [ ] Uploaded ALL files from publish folder
- [ ] `web.config` in root directory
- [ ] `wwwroot` folder uploaded completely
- [ ] Permissions correct on server

## 🚀 First Launch

- [ ] Application pool started
- [ ] No errors in `logs\stdout_*.log`
- [ ] Health endpoint works: `/health`
- [ ] Home page loads: `/`
- [ ] Login page accessible: `/Account/Login`
- [ ] HTTPS redirect working
- [ ] Static files (CSS/JS) loading

## 👤 Admin User Setup

- [ ] Created first admin user
- [ ] Can login successfully
- [ ] Admin dashboard accessible

## 🔒 Security Validation

- [ ] JWT secret is unique (not example value)
- [ ] Database password is strong
- [ ] HTTPS enforced (HTTP redirects)
- [ ] `/swagger` returns 404 or redirects
- [ ] `.db`, `.config`, `.log` files blocked
- [ ] Security headers present (F12 → Network)

## 📊 Functional Testing

- [ ] Certificate tile view loads
- [ ] Search functionality works
- [ ] Certificate details page works
- [ ] PDF download works
- [ ] Admin pages accessible (admin role)
- [ ] API endpoints respond

## 📝 Documentation

- [ ] Deployment notes recorded
- [ ] Configuration documented
- [ ] Admin credentials stored securely
- [ ] Support contacts updated

## 🔄 Rollback Preparation

- [ ] Previous version backed up (if updating)
- [ ] Database backed up before migration
- [ ] Rollback procedure documented

---

**Deployment Date**: ____________
**Deployed By**: ____________
**Production URL**: ____________

**Status**: ⬜ Ready | ⬜ In Progress | ⬜ Complete | ⬜ Failed
