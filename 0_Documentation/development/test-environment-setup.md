# UAT Test Environment Setup & Management Guide
## GCC Web Application - User Acceptance Testing

**Document Version:** 1.0  
**Last Updated:** August 2025  
**Environment URL:** https://localhost:5101  
**Environment Type:** Dedicated UAT Instance

---

## 🔧 **Environment Architecture**

### **System Components**
```
┌─────────────────────────────────────────────┐
│           Load Balancer (Optional)          │
└─────────────────────────────────────────────┘
                     │
┌─────────────────────────────────────────────┐
│         Web Server (IIS/Kestrel)            │
│         https://localhost:5101               │
└─────────────────────────────────────────────┘
                     │
┌─────────────────────────────────────────────┐
│       Application Server (.NET 8)            │
│         GCC Web Application                  │
└─────────────────────────────────────────────┘
                     │
┌─────────────────────────────────────────────┐
│         Database Server (SQL/SQLite)         │
│           gcc_database.db                    │
└─────────────────────────────────────────────┘
```

### **Environment Specifications**
- **Operating System:** Windows Server 2019/2022 (or Windows 10/11 for local)
- **Web Server:** IIS 10+ or Kestrel
- **.NET Runtime:** .NET 8.0
- **Database:** SQLite (Development) / SQL Server (Production-like)
- **Memory:** Minimum 8GB RAM
- **Storage:** 50GB available space
- **Network:** HTTPS enabled with SSL certificate

---

## 🚀 **Initial Environment Setup**

### **Step 1: Prerequisites Installation**

#### **Required Software**
```powershell
# Check .NET 8 SDK installation
dotnet --version

# Install .NET 8 if not present
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0

# Check IIS installation (if using IIS)
Get-WindowsFeature -Name Web-Server

# Install IIS and ASP.NET Core Module
Enable-WindowsFeature -Name Web-Server -IncludeManagementTools
```

### **Step 2: Application Deployment**

#### **Option A: Direct Run (Development)**
```bash
# Clone repository
git clone [repository-url]
cd GCCWebApp

# Restore dependencies
dotnet restore

# Build application
dotnet build --configuration Release

# Run application
dotnet run --urls https://localhost:5101
```

#### **Option B: IIS Deployment**
```powershell
# Publish application
dotnet publish -c Release -o C:\inetpub\wwwroot\GCCWebApp

# Configure IIS Site
New-WebSite -Name "GCCWebApp-UAT" `
  -Port 5101 `
  -PhysicalPath "C:\inetpub\wwwroot\GCCWebApp" `
  -ApplicationPool "GCCWebApp-UAT"

# Configure SSL
New-WebBinding -Name "GCCWebApp-UAT" `
  -Protocol https `
  -Port 5101
```

### **Step 3: Database Setup**

#### **SQLite Setup (Default)**
```bash
# Database will be created automatically on first run
# Location: ./gcc_database.db

# To reset database:
rm gcc_database.db
dotnet run  # Will recreate and seed database
```

#### **SQL Server Setup (Production-like)**
```sql
-- Create database
CREATE DATABASE GCC_UAT;
GO

-- Create login for application
CREATE LOGIN [gcc_uat_user] WITH PASSWORD = 'Strong!Password123';
GO

USE GCC_UAT;
GO

-- Create user and grant permissions
CREATE USER [gcc_uat_user] FOR LOGIN [gcc_uat_user];
ALTER ROLE db_owner ADD MEMBER [gcc_uat_user];
GO
```

### **Step 4: Configuration**

#### **appsettings.json Configuration**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gcc_database.db"
    // For SQL Server:
    // "DefaultConnection": "Server=localhost;Database=GCC_UAT;User Id=gcc_uat_user;Password=Strong!Password123;TrustServerCertificate=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "BaseUrl": "https://localhost:5101",
  "Environment": "UAT"
}
```

---

## 👤 **Test Account Management**

### **Default Test Accounts**

#### **Account Creation Script**
```sql
-- Run this after database creation to ensure test accounts exist
-- Passwords are hashed with BCrypt: "TestPass123!"

INSERT INTO Users (Username, Email, PasswordHash, FullName, Status, Role, IsActive, CreatedAt)
VALUES 
  ('admin', 'admin@gcc.com', '$2a$11$[hash]', 'Admin User', 'active', 'Admin', 1, GETDATE()),
  ('analyst', 'analyst@gcc.com', '$2a$11$[hash]', 'Analyst User', 'active', 'Analyst', 1, GETDATE()),
  ('external', 'external@company.com', '$2a$11$[hash]', 'External User', 'active', 'External', 1, GETDATE()),
  ('testuser1', 'test1@gcc.com', '$2a$11$[hash]', 'Test User 1', 'active', 'User', 1, GETDATE()),
  ('testuser2', 'test2@gcc.com', '$2a$11$[hash]', 'Test User 2', 'active', 'User', 1, GETDATE());
```

### **Account Management Procedures**

#### **Reset User Password**
```bash
# Using application command
dotnet run --reset-password admin@gcc.com NewPass123!

# Or update directly in database
UPDATE Users 
SET PasswordHash = '$2a$11$[new-hash]'
WHERE Email = 'admin@gcc.com';
```

#### **Create New Test User**
```bash
# Using application command
dotnet run --create-user "newuser@gcc.com" "Test User" "TestPass123!" "Analyst"
```

---

## 🔄 **Environment Maintenance**

### **Daily Health Checks**

#### **Health Check Script**
```powershell
# UAT Environment Health Check Script
# Run daily at 8:00 AM

$results = @{}

# Check web server
$webResponse = Invoke-WebRequest -Uri "https://localhost:5101/health" -UseBasicParsing
$results["WebServer"] = if ($webResponse.StatusCode -eq 200) { "Healthy" } else { "Unhealthy" }

# Check database connectivity
$dbCheck = Invoke-SqlCmd -Query "SELECT 1" -ServerInstance "localhost" -Database "GCC_UAT"
$results["Database"] = if ($dbCheck) { "Healthy" } else { "Unhealthy" }

# Check disk space
$disk = Get-PSDrive C | Select-Object @{n='FreeGB';e={[math]::Round($_.Free/1GB,2)}}
$results["DiskSpace"] = if ($disk.FreeGB -gt 10) { "Healthy: $($disk.FreeGB)GB free" } else { "Warning: Low disk space" }

# Check memory
$memory = Get-CimInstance Win32_OperatingSystem | Select-Object @{n='FreeGB';e={[math]::Round($_.FreePhysicalMemory/1MB/1024,2)}}
$results["Memory"] = "Available: $($memory.FreeGB)GB"

# Send results
$results | ConvertTo-Json | Out-File "C:\UAT\HealthCheck_$(Get-Date -Format yyyyMMdd).json"
```

### **Data Refresh Procedures**

#### **Complete Data Reset**
```bash
# Stop application
iisreset /stop

# Backup current database
cp gcc_database.db "backups/gcc_database_$(date +%Y%m%d_%H%M%S).db"

# Delete current database
rm gcc_database.db

# Restart application (will recreate database)
iisreset /start

# Verify seed data
sqlite3 gcc_database.db "SELECT COUNT(*) FROM Users;"
```

#### **Partial Data Refresh**
```sql
-- Clear test data while preserving users
BEGIN TRANSACTION;

DELETE FROM Certificates WHERE CreatedAt > '2025-01-01';
DELETE FROM AuditLogs WHERE CreatedAt > '2025-01-01';
DELETE FROM TestResults WHERE CreatedAt > '2025-01-01';

-- Reset sequences
UPDATE sqlite_sequence SET seq = 100 WHERE name = 'Certificates';

-- Insert fresh test data
INSERT INTO Certificates (...) VALUES (...);

COMMIT;
```

---

## 🛡️ **Security Configuration**

### **SSL Certificate Setup**

#### **Self-Signed Certificate (UAT)**
```powershell
# Create self-signed certificate
New-SelfSignedCertificate `
  -DnsName "localhost", "uat.gcc.local" `
  -CertStoreLocation "cert:\LocalMachine\My" `
  -KeyAlgorithm RSA `
  -KeyLength 2048 `
  -KeyExportPolicy Exportable `
  -NotAfter (Get-Date).AddYears(2)

# Trust the certificate
$cert = Get-ChildItem -Path cert:\LocalMachine\My | Where-Object {$_.Subject -match "localhost"}
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root","LocalMachine")
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()
```

### **Firewall Rules**
```powershell
# Allow HTTPS traffic on port 5101
New-NetFirewallRule `
  -DisplayName "GCC UAT HTTPS" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 5101 `
  -Action Allow
```

### **Access Control**
```xml
<!-- web.config security settings -->
<configuration>
  <system.webServer>
    <security>
      <requestFiltering>
        <requestLimits maxAllowedContentLength="104857600" />
        <fileExtensions>
          <add fileExtension=".db" allowed="false" />
          <add fileExtension=".config" allowed="false" />
        </fileExtensions>
      </requestFiltering>
    </security>
    <httpProtocol>
      <customHeaders>
        <add name="X-Frame-Options" value="SAMEORIGIN" />
        <add name="X-Content-Type-Options" value="nosniff" />
        <add name="X-XSS-Protection" value="1; mode=block" />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
</configuration>
```

---

## 📊 **Monitoring & Logging**

### **Application Logging Configuration**

#### **Serilog Configuration**
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.File", "Serilog.Sinks.Console"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/uat-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
}
```

### **Performance Monitoring**
```powershell
# Monitor key metrics
Get-Counter -Counter "\Processor(_Total)\% Processor Time","\Memory\Available MBytes","\ASP.NET Applications(__Total__)\Requests/Sec" -Continuous -SampleInterval 5
```

---

## 🚨 **Troubleshooting Guide**

### **Common Issues & Solutions**

#### **Issue 1: Application Won't Start**
```bash
# Check event logs
Get-EventLog -LogName Application -Newest 20 | Where-Object {$_.Source -like "*ASP.NET*"}

# Verify .NET installation
dotnet --list-runtimes

# Check port availability
netstat -an | findstr :5101

# Solution: Change port or stop conflicting service
```

#### **Issue 2: Database Connection Failures**
```sql
-- Test database connectivity
SELECT @@VERSION;
SELECT name FROM sys.databases;

-- Check connection string
PRINT 'Connection String: ' + (SELECT value FROM appsettings WHERE key = 'ConnectionString');

-- Verify permissions
SELECT * FROM fn_my_permissions(NULL, 'DATABASE');
```

#### **Issue 3: Performance Issues**
```bash
# Check resource usage
Get-Process w3wp,dotnet | Select-Object Name,CPU,WorkingSet

# Clear application cache
iisreset /stop
Remove-Item -Path "C:\Windows\Temp\*" -Recurse -Force
iisreset /start

# Rebuild database indexes
sqlite3 gcc_database.db "VACUUM; ANALYZE;"
```

---

## 📅 **Environment Refresh Schedule**

### **Daily Tasks**
- **8:00 AM:** Health check script execution
- **8:30 AM:** Review health check results
- **6:00 PM:** Backup database

### **Weekly Tasks**
- **Monday 6:00 AM:** Full data refresh
- **Wednesday:** Security patches review
- **Friday:** Performance metrics review

### **Monthly Tasks**
- **First Monday:** Full environment validation
- **Security updates application**
- **Certificate renewal check**
- **Capacity planning review**

---

## 📞 **Support Contacts**

### **Environment Support Team**

| **Role** | **Name** | **Contact** | **Hours** |
|----------|----------|-------------|-----------|
| **Environment Admin** | [Name] | [email/phone] | 8 AM - 6 PM |
| **Database Admin** | [Name] | [email/phone] | 8 AM - 6 PM |
| **Network Admin** | [Name] | [email/phone] | 24/7 on-call |
| **Application Support** | [Name] | [email/phone] | 8 AM - 8 PM |

### **Escalation Path**
1. **Level 1:** Environment Admin (Response: <1 hour)
2. **Level 2:** IT Manager (Response: <2 hours)
3. **Level 3:** Infrastructure Director (Response: <4 hours)

---

## ✅ **Environment Readiness Checklist**

### **Pre-UAT Validation**
- [ ] Application deployed and accessible
- [ ] SSL certificate installed and valid
- [ ] All test accounts created and working
- [ ] Database seeded with test data
- [ ] Firewall rules configured
- [ ] Monitoring and logging enabled
- [ ] Backup procedures tested
- [ ] Health check scripts deployed
- [ ] Support team trained
- [ ] Documentation updated

### **Daily Validation**
- [ ] Application responsive
- [ ] All services running
- [ ] Database accessible
- [ ] Disk space adequate (>20GB free)
- [ ] Memory usage normal (<80%)
- [ ] No critical errors in logs
- [ ] Backup completed successfully

---

This guide ensures the UAT environment is properly configured, maintained, and monitored throughout the testing lifecycle.