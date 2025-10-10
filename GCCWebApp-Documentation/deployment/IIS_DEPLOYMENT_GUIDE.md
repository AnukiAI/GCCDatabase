# IIS Deployment Guide - GCC Web Application
## Production-Ready IIS Configuration

**Document Version:** 1.0  
**Last Updated:** August 2025  
**Application:** Green Circle Certified Web Application  
**Target Port:** 5101 (HTTP/HTTPS)

---

## 🎯 **Deployment Overview**

This guide provides complete instructions for deploying the GCC Web Application to Internet Information Services (IIS) for production use. The application will be configured to run on port 5101 with SSL/HTTPS support.

### **Benefits of IIS Deployment:**
✅ **Always Available:** Application runs as a Windows service, starts automatically  
✅ **Production Ready:** Enterprise-grade web server with advanced features  
✅ **SSL/HTTPS Support:** Built-in SSL certificate management  
✅ **Performance:** Optimized for high-performance web applications  
✅ **Management:** Easy administration through IIS Manager  
✅ **Monitoring:** Built-in logging and performance monitoring  

---

## 🛠️ **Prerequisites**

### **System Requirements:**
- **Operating System:** Windows 10/11 or Windows Server 2016+
- **IIS Version:** IIS 10.0 or later
- **.NET Runtime:** .NET 8.0 Runtime (ASP.NET Core)
- **Memory:** 4GB RAM minimum (8GB recommended)
- **Storage:** 2GB free space
- **Administrator Access:** Required for IIS configuration

### **Required IIS Features:**
```powershell
# Enable IIS and required features
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging
Enable-WindowsOptionalFeature -Online -FeatureName IIS-RequestFiltering
Enable-WindowsOptionalFeature -Online -FeatureName IIS-StaticContent
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DefaultDocument
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DirectoryBrowsing
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45
```

### **Install ASP.NET Core Hosting Bundle:**
1. Download from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Install "ASP.NET Core Runtime 8.0.x - Windows Hosting Bundle"
3. Restart IIS: `iisreset`

---

## 🚀 **Quick Deployment (Automated)**

### **Option 1: Using PowerShell Script (Recommended)**
```powershell
# Run as Administrator
.\deploy-iis.ps1
```

### **Option 2: Using Batch Script**
```cmd
REM Run as Administrator
deploy-iis.cmd
```

### **What the Scripts Do:**
1. Create IIS Application Pool with .NET 8 configuration
2. Build and publish the application
3. Create IIS Website bound to port 5101
4. Generate and install SSL certificate
5. Configure security headers and request filtering
6. Set appropriate folder permissions
7. Start the application
8. Perform health checks

---

## 📋 **Manual Deployment Steps**

### **Step 1: Prepare Application**
```bash
# Navigate to application directory
cd "C:\Path\To\GCCWebApp"

# Clean and build
dotnet clean --configuration Release
dotnet restore
dotnet build --configuration Release --no-restore

# Publish to deployment folder
dotnet publish --configuration Release --output "C:\inetpub\wwwroot\GCCWebApp" --no-build
```

### **Step 2: Create Application Pool**
```powershell
# Open IIS Manager or use PowerShell
Import-Module WebAdministration

# Create Application Pool
New-WebAppPool -Name "GCCWebApp-AppPool"
Set-ItemProperty -Path "IIS:\AppPools\GCCWebApp-AppPool" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty -Path "IIS:\AppPools\GCCWebApp-AppPool" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
Set-ItemProperty -Path "IIS:\AppPools\GCCWebApp-AppPool" -Name "processModel.loadUserProfile" -Value $true
Set-ItemProperty -Path "IIS:\AppPools\GCCWebApp-AppPool" -Name "processModel.idleTimeout" -Value "00:00:00"
```

### **Step 3: Create Website**
```powershell
# Create IIS Site
New-IISSite -Name "GCCWebApp-Local" -PhysicalPath "C:\inetpub\wwwroot\GCCWebApp" -BindingInformation "*:5101:" -Protocol http

# Assign Application Pool
Set-ItemProperty -Path "IIS:\Sites\GCCWebApp-Local" -Name "applicationPool" -Value "GCCWebApp-AppPool"
```

### **Step 4: Configure SSL Certificate**
```powershell
# Create self-signed certificate
$cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\LocalMachine\My" -KeyAlgorithm RSA -KeyLength 2048 -NotAfter (Get-Date).AddYears(2)

# Trust the certificate
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root","LocalMachine")
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()

# Add HTTPS binding
New-IISSiteBinding -Name "GCCWebApp-Local" -BindingInformation "*:5101:" -Protocol https -CertificateThumbPrint $cert.Thumbprint -CertStoreLocation "Cert:\LocalMachine\My"
```

### **Step 5: Set Permissions**
```powershell
# Set folder permissions
$acl = Get-Acl "C:\inetpub\wwwroot\GCCWebApp"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl -Path "C:\inetpub\wwwroot\GCCWebApp" -AclObject $acl

# Set log folder permissions
$logAcl = Get-Acl "C:\inetpub\wwwroot\GCCWebApp\logs"
$logAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$logAcl.SetAccessRule($logAccessRule)
Set-Acl -Path "C:\inetpub\wwwroot\GCCWebApp\logs" -AclObject $logAcl
```

### **Step 6: Start Application**
```powershell
# Start Application Pool and Website
Start-WebAppPool -Name "GCCWebApp-AppPool"
Start-IISSite -Name "GCCWebApp-Local"
```

---

## ⚙️ **Configuration Details**

### **Application Pool Configuration**
```xml
<!-- Key Application Pool Settings -->
<applicationPool>
    <name>GCCWebApp-AppPool</name>
    <managedRuntimeVersion></managedRuntimeVersion>
    <processModel identityType="ApplicationPoolIdentity" 
                  loadUserProfile="true" 
                  idleTimeout="00:00:00" />
    <recycling>
        <periodicRestart time="00:00:00" />
    </recycling>
</applicationPool>
```

### **Website Bindings**
- **HTTP:** http://localhost:5101
- **HTTPS:** https://localhost:5101 (with SSL certificate)

### **Security Configuration**
The `web.config` file includes:
- **Security Headers:** X-Frame-Options, X-Content-Type-Options, etc.
- **Request Filtering:** Protection against malicious requests
- **File Access Control:** Prevents access to sensitive files (.db, .config, .log)
- **HTTPS Redirect:** Automatic redirect from HTTP to HTTPS

---

## 📊 **Management & Monitoring**

### **Using Management Script**
```cmd
# Use the included management script
manage-iis.cmd

# Available options:
# 1. Start Application
# 2. Stop Application  
# 3. Restart Application
# 4. Check Status
# 5. View Logs
# 6. Open Application
# 7. Redeploy Application
# 8. Remove Application
```

### **Manual Management Commands**
```powershell
# Check Application Status
Get-IISSite -Name "GCCWebApp-Local"
Get-IISAppPool -Name "GCCWebApp-AppPool"

# Start/Stop Services
Start-IISSite -Name "GCCWebApp-Local"
Stop-IISSite -Name "GCCWebApp-Local"
Start-WebAppPool -Name "GCCWebApp-AppPool"
Stop-WebAppPool -Name "GCCWebApp-AppPool"

# Health Check
Invoke-WebRequest -Uri "https://localhost:5101" -UseBasicParsing
```

### **Log Locations**
- **Application Logs:** `C:\inetpub\wwwroot\GCCWebApp\logs\`
- **IIS Logs:** `C:\inetpub\logs\LogFiles\W3SVC1\`
- **Windows Event Log:** Applications and Services Logs > Microsoft > Windows > IIS-W3SVC-WP

---

## 🔍 **Health Checks & Testing**

### **Automated Health Check**
```powershell
# Health check script
$healthCheck = @{
    HTTP = (Invoke-WebRequest -Uri "http://localhost:5101" -UseBasicParsing).StatusCode
    HTTPS = (Invoke-WebRequest -Uri "https://localhost:5101" -UseBasicParsing -SkipCertificateCheck).StatusCode
    AppPool = (Get-IISAppPool -Name "GCCWebApp-AppPool").State
    Site = (Get-IISSite -Name "GCCWebApp-Local").State
}

Write-Output $healthCheck
```

### **Manual Testing Checklist**
- [ ] HTTP endpoint responds (http://localhost:5101)
- [ ] HTTPS endpoint responds with valid certificate (https://localhost:5101)
- [ ] Login page loads correctly
- [ ] Database connectivity works
- [ ] Static files serve correctly (CSS, JS, images)
- [ ] Application logs are being written
- [ ] Error pages display correctly (404, 500)

---

## 🚨 **Troubleshooting**

### **Common Issues**

#### **Issue: Application won't start**
```powershell
# Check Application Pool status
Get-IISAppPool -Name "GCCWebApp-AppPool"

# Check Windows Event Log
Get-EventLog -LogName Application -Source "IIS-W3SVC-WP" -Newest 10

# Restart services
Stop-WebAppPool -Name "GCCWebApp-AppPool"
Start-WebAppPool -Name "GCCWebApp-AppPool"
```

#### **Issue: 500 Internal Server Error**
```powershell
# Enable detailed errors in web.config (temporarily)
# Check application logs
Get-Content "C:\inetpub\wwwroot\GCCWebApp\logs\*.log" | Select-Object -Last 50

# Check .NET runtime is installed
dotnet --list-runtimes
```

#### **Issue: SSL Certificate Problems**
```powershell
# List certificates
Get-ChildItem -Path "Cert:\LocalMachine\My" | Where-Object {$_.Subject -match "localhost"}

# Recreate certificate if needed
$cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\LocalMachine\My" -KeyAlgorithm RSA -KeyLength 2048
```

#### **Issue: Permission Denied**
```powershell
# Reset permissions
icacls "C:\inetpub\wwwroot\GCCWebApp" /grant "IIS_IUSRS:(OI)(CI)RX" /t
icacls "C:\inetpub\wwwroot\GCCWebApp\logs" /grant "IIS_IUSRS:(OI)(CI)F" /t
```

### **Performance Issues**
- **Check Memory Usage:** Task Manager > App Pools > GCCWebApp-AppPool
- **Monitor Logs:** Look for performance-related error messages
- **Database Performance:** Check database connection and query performance
- **Static Files:** Ensure static files are properly compressed and cached

---

## 🔄 **Updates & Maintenance**

### **Application Updates**
```powershell
# Stop application
Stop-IISSite -Name "GCCWebApp-Local"
Stop-WebAppPool -Name "GCCWebApp-AppPool"

# Deploy new version
dotnet publish --configuration Release --output "C:\inetpub\wwwroot\GCCWebApp" --no-build

# Start application
Start-WebAppPool -Name "GCCWebApp-AppPool"
Start-IISSite -Name "GCCWebApp-Local"
```

### **Backup Procedures**
```powershell
# Backup application files
Copy-Item "C:\inetpub\wwwroot\GCCWebApp" -Destination "C:\Backups\GCCWebApp_$(Get-Date -Format 'yyyyMMdd')" -Recurse

# Backup IIS configuration
%WINDIR%\system32\inetsrv\appcmd.exe add backup "GCCWebApp_Backup_$(Get-Date -Format 'yyyyMMdd')"
```

### **Monitoring Script**
```powershell
# Create monitoring script to run as scheduled task
$monitoring = @{
    Timestamp = Get-Date
    SiteStatus = (Get-IISSite -Name "GCCWebApp-Local").State
    AppPoolStatus = (Get-IISAppPool -Name "GCCWebApp-AppPool").State
    HttpStatus = try { (Invoke-WebRequest -Uri "http://localhost:5101" -UseBasicParsing -TimeoutSec 30).StatusCode } catch { "Error" }
    HttpsStatus = try { (Invoke-WebRequest -Uri "https://localhost:5101" -UseBasicParsing -SkipCertificateCheck -TimeoutSec 30).StatusCode } catch { "Error" }
}

# Log to file or send alert if issues detected
$monitoring | ConvertTo-Json | Out-File "C:\Monitoring\GCCWebApp_$(Get-Date -Format 'yyyyMMdd').json" -Append
```

---

## ✅ **Post-Deployment Checklist**

### **Immediate (Day 1)**
- [ ] All endpoints responding correctly
- [ ] SSL certificate installed and trusted
- [ ] Application logs being written
- [ ] Database connectivity confirmed
- [ ] User authentication working
- [ ] Error handling functioning
- [ ] Performance acceptable

### **Short-term (Week 1)**
- [ ] Monitor application performance
- [ ] Review error logs daily
- [ ] Test backup and restore procedures
- [ ] Validate monitoring and alerts
- [ ] User acceptance testing completed

### **Long-term (Month 1)**
- [ ] Performance optimization based on usage patterns
- [ ] Security review and hardening
- [ ] Capacity planning and scaling preparation
- [ ] Documentation updates
- [ ] Team training on management procedures

---

**🎉 Congratulations! Your GCC Web Application is now deployed on IIS and ready for production use.**

**Access your application at: https://localhost:5101**