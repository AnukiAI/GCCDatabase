# GCC Web Application - Deployment Quick Reference
**Date:** October 9, 2025

## Critical Information

### New JWT Secret Key
```
5T8hr4WpKKmqqDIHBzFW7OzfYdStIzuvKEZJYwQaSkfe5ADFryPGJHGAuxPCECnuo09ZI8qiLqURyVT1DR1NtQ==
```

### Database Connection
```
Server: 68.71.129.120,1533
Database: gccwebap_gccdb
User: adminGCC
Password: Q3Ez4elgE_47KNi
```

---

## Quick Deployment Commands

### 1. Stop Application
```powershell
Stop-IISAppPool -Name "GCCWebApp"
```

### 2. Extract Package
```powershell
cd C:\inetpub\wwwroot
tar -xzf C:\Temp\GCCWebApp-TestDeploy.tar.gz -C GCCWebApp
```

### 3. Set JWT Secret (Environment Variable)
```powershell
[System.Environment]::SetEnvironmentVariable("GCC_JWT_SECRET", "5T8hr4WpKKmqqDIHBzFW7OzfYdStIzuvKEZJYwQaSkfe5ADFryPGJHGAuxPCECnuo09ZI8qiLqURyVT1DR1NtQ==", [System.EnvironmentVariableTarget]::Machine)
```

### 4. Set Environment
```powershell
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", [System.EnvironmentVariableTarget]::Machine)
```

### 5. Set Permissions
```powershell
$deployPath = "C:\inetpub\wwwroot\GCCWebApp"
icacls $deployPath /grant "IIS AppPool\GCCWebApp:(OI)(CI)RX" /t
New-Item -Path "$deployPath\logs" -ItemType Directory -Force
icacls "$deployPath\logs" /grant "IIS AppPool\GCCWebApp:(OI)(CI)F" /t
```

### 6. Create/Configure App Pool
```powershell
Import-Module WebAdministration
$appPoolName = "GCCWebApp"
if (-not (Test-Path "IIS:\AppPools\$appPoolName")) {
    New-WebAppPool -Name $appPoolName
}
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
```

### 7. Restart IIS
```powershell
iisreset
```

### 8. Start Application
```powershell
Start-IISAppPool -Name "GCCWebApp"
Start-Website -Name "GCCWebApp"
```

### 9. Test Health
```powershell
Invoke-WebRequest -Uri "https://your-domain.com/health" -Method GET
```

---

## Quick Verification

### Test Endpoints
```
https://your-domain.com/
https://your-domain.com/Certificate/CertificateTileView
https://your-domain.com/Account/Login
https://your-domain.com/health
```

### Default Credentials (CHANGE IMMEDIATELY)
```
Admin: admin@greencirclecertified.com / Admin123!
Analyst: analyst@greencirclecertified.com / Analyst123!
External: partner@amazon.com / Partner123!
```

---

## Quick Troubleshooting

### View Logs
```powershell
Get-Content "C:\inetpub\wwwroot\GCCWebApp\logs\stdout_*.log" -Tail 50
```

### Restart App
```powershell
Restart-WebAppPool -Name "GCCWebApp"
```

### Test Database
```powershell
sqlcmd -S 68.71.129.120,1533 -d gccwebap_gccdb -U adminGCC -P Q3Ez4elgE_47KNi -Q "SELECT 1"
```

### Check App Pool Status
```powershell
Get-IISAppPool -Name "GCCWebApp"
```

---

## Rollback
```powershell
Stop-IISAppPool -Name "GCCWebApp"
Remove-Item "C:\inetpub\wwwroot\GCCWebApp\*" -Recurse -Force
Copy-Item "C:\Backups\GCCWebApp_20251009\*" "C:\inetpub\wwwroot\GCCWebApp\" -Recurse
Start-IISAppPool -Name "GCCWebApp"
```

---

**For detailed instructions, see: DEPLOYMENT_EXECUTION_GUIDE_20251009.md**
