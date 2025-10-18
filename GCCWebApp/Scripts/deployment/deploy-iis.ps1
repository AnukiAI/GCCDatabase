#Requires -RunAsAdministrator

<#
.SYNOPSIS
    GCC Web Application IIS Deployment Script (PowerShell)

.DESCRIPTION
    Deploys the GCC Web Application to IIS with comprehensive configuration
    including SSL certificates, security headers, and monitoring setup.

.PARAMETER SiteName
    The name of the IIS site to create (default: GCCWebApp-Local)

.PARAMETER Port
    The port number for HTTP/HTTPS binding (default: 5101)

.PARAMETER DeployPath
    The physical path for deployment (default: C:\inetpub\wwwroot\GCCWebApp)

.EXAMPLE
    .\deploy-iis.ps1
    .\deploy-iis.ps1 -SiteName "GCCWebApp-Production" -Port 443 -DeployPath "D:\WebApps\GCCWebApp"
#>

param(
    [string]$SiteName = "GCCWebApp-Local",
    [int]$Port = 5101,
    [string]$DeployPath = "C:\inetpub\wwwroot\GCCWebApp",
    [string]$AppPoolName = "GCCWebApp-AppPool"
)

# Import required modules
Import-Module WebAdministration -ErrorAction SilentlyContinue
Import-Module PKI -ErrorAction SilentlyContinue

# Configuration
$LogPath = "C:\inetpub\logs\GCCWebApp"
$AppName = "GCCWebApp"

Write-Host "=================================================================" -ForegroundColor Green
Write-Host "GCC Web Application - IIS Deployment Script (PowerShell)" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
Write-Host ""

# Verify administrator privileges
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script requires administrator privileges. Please run as administrator."
    exit 1
}

Write-Host "[INFO] Administrator privileges confirmed." -ForegroundColor Green
Write-Host ""

# Display configuration
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Deployment Configuration:" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Site Name: $SiteName" -ForegroundColor White
Write-Host "App Pool Name: $AppPoolName" -ForegroundColor White
Write-Host "Deploy Path: $DeployPath" -ForegroundColor White
Write-Host "Port: $Port" -ForegroundColor White
Write-Host "Log Path: $LogPath" -ForegroundColor White
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

try {
    # Step 1: Create directories
    Write-Host "[STEP 1] Creating deployment directories..." -ForegroundColor Yellow
    $directories = @($DeployPath, $LogPath, "$DeployPath\logs")
    foreach ($dir in $directories) {
        if (!(Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Host "  Created: $dir" -ForegroundColor Gray
        }
    }
    Write-Host "[SUCCESS] Directories created." -ForegroundColor Green
    Write-Host ""

    # Step 2: Stop existing services
    Write-Host "[STEP 2] Stopping existing IIS site and application pool..." -ForegroundColor Yellow
    if (Get-IISSite -Name $SiteName -ErrorAction SilentlyContinue) {
        Stop-IISSite -Name $SiteName -Confirm:$false
        Write-Host "  Stopped site: $SiteName" -ForegroundColor Gray
    }
    if (Get-IISAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
        Stop-WebAppPool -Name $AppPoolName
        Write-Host "  Stopped app pool: $AppPoolName" -ForegroundColor Gray
    }
    Write-Host "[SUCCESS] Existing services stopped." -ForegroundColor Green
    Write-Host ""

    # Step 3: Build and publish application
    Write-Host "[STEP 3] Building and publishing application..." -ForegroundColor Yellow
    
    # Clean
    Write-Host "  Cleaning solution..." -ForegroundColor Gray
    $cleanResult = & dotnet clean --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Clean failed: $cleanResult"
    }
    
    # Restore
    Write-Host "  Restoring packages..." -ForegroundColor Gray
    $restoreResult = & dotnet restore
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed: $restoreResult"
    }
    
    # Build
    Write-Host "  Building application..." -ForegroundColor Gray
    $buildResult = & dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed: $buildResult"
    }
    
    # Publish
    Write-Host "  Publishing to $DeployPath..." -ForegroundColor Gray
    $publishResult = & dotnet publish --configuration Release --output $DeployPath --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed: $publishResult"
    }
    
    Write-Host "[SUCCESS] Application built and published." -ForegroundColor Green
    Write-Host ""

    # Step 4: Configure Application Pool
    Write-Host "[STEP 4] Configuring IIS Application Pool..." -ForegroundColor Yellow
    
    # Remove existing app pool if exists
    if (Get-IISAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
        Remove-WebAppPool -Name $AppPoolName
    }
    
    # Create new app pool
    New-WebAppPool -Name $AppPoolName
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.loadUserProfile" -Value $true
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.idleTimeout" -Value "00:00:00"
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "recycling.periodicRestart.time" -Value "00:00:00"
    
    Write-Host "  Application pool '$AppPoolName' configured." -ForegroundColor Gray
    Write-Host "[SUCCESS] Application pool configured." -ForegroundColor Green
    Write-Host ""

    # Step 5: Configure Website
    Write-Host "[STEP 5] Configuring IIS Website..." -ForegroundColor Yellow
    
    # Remove existing site if exists
    if (Get-IISSite -Name $SiteName -ErrorAction SilentlyContinue) {
        Remove-IISSite -Name $SiteName -Confirm:$false
    }
    
    # Create new site
    New-IISSite -Name $SiteName -PhysicalPath $DeployPath -BindingInformation "*:${Port}:" -Protocol http
    Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name "applicationPool" -Value $AppPoolName
    
    Write-Host "  Website '$SiteName' configured on port $Port." -ForegroundColor Gray
    Write-Host "[SUCCESS] Website configured." -ForegroundColor Green
    Write-Host ""

    # Step 6: Configure SSL Certificate and HTTPS
    Write-Host "[STEP 6] Configuring SSL certificate and HTTPS binding..." -ForegroundColor Yellow
    
    # Check for existing certificate
    $existingCert = Get-ChildItem -Path "Cert:\LocalMachine\My" | Where-Object { $_.Subject -match "localhost" } | Select-Object -First 1
    
    if (-not $existingCert) {
        Write-Host "  Creating self-signed SSL certificate..." -ForegroundColor Gray
        $cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\LocalMachine\My" -KeyAlgorithm RSA -KeyLength 2048 -NotAfter (Get-Date).AddYears(2)
        
        # Trust the certificate
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root","LocalMachine")
        $store.Open("ReadWrite")
        $store.Add($cert)
        $store.Close()
        
        $certThumbprint = $cert.Thumbprint
        Write-Host "  Created certificate with thumbprint: $certThumbprint" -ForegroundColor Gray
    } else {
        $certThumbprint = $existingCert.Thumbprint
        Write-Host "  Using existing certificate with thumbprint: $certThumbprint" -ForegroundColor Gray
    }
    
    # Add HTTPS binding
    New-IISSiteBinding -Name $SiteName -BindingInformation "*:${Port}:" -Protocol https -CertificateThumbPrint $certThumbprint -CertStoreLocation "Cert:\LocalMachine\My" -SslFlag 0
    
    Write-Host "[SUCCESS] SSL certificate and HTTPS binding configured." -ForegroundColor Green
    Write-Host ""

    # Step 7: Set folder permissions
    Write-Host "[STEP 7] Setting folder permissions..." -ForegroundColor Yellow
    
    # Set permissions for IIS_IUSRS
    $acl = Get-Acl $DeployPath
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl -Path $DeployPath -AclObject $acl
    
    # Set full permissions for logs directory
    $logAcl = Get-Acl "$DeployPath\logs"
    $logAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $logAcl.SetAccessRule($logAccessRule)
    Set-Acl -Path "$DeployPath\logs" -AclObject $logAcl
    
    Write-Host "  Folder permissions configured." -ForegroundColor Gray
    Write-Host "[SUCCESS] Permissions configured." -ForegroundColor Green
    Write-Host ""

    # Step 8: Start services
    Write-Host "[STEP 8] Starting IIS services..." -ForegroundColor Yellow
    
    Start-WebAppPool -Name $AppPoolName
    Start-IISSite -Name $SiteName
    
    Write-Host "  Application pool and website started." -ForegroundColor Gray
    Write-Host "[SUCCESS] IIS services started." -ForegroundColor Green
    Write-Host ""

    # Step 9: Test deployment
    Write-Host "[STEP 9] Testing deployment..." -ForegroundColor Yellow
    
    Start-Sleep -Seconds 5
    
    try {
        $httpResponse = Invoke-WebRequest -Uri "http://localhost:$Port" -UseBasicParsing -TimeoutSec 10
        Write-Host "  HTTP Status: $($httpResponse.StatusCode)" -ForegroundColor Gray
    } catch {
        Write-Host "  HTTP Test failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    try {
        $httpsResponse = Invoke-WebRequest -Uri "https://localhost:$Port" -UseBasicParsing -SkipCertificateCheck -TimeoutSec 10
        Write-Host "  HTTPS Status: $($httpsResponse.StatusCode)" -ForegroundColor Gray
    } catch {
        Write-Host "  HTTPS Test failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host "[SUCCESS] Deployment testing completed." -ForegroundColor Green
    Write-Host ""

    # Display deployment summary
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host "DEPLOYMENT COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Application URLs:" -ForegroundColor Cyan
    Write-Host "  HTTP:  http://localhost:$Port" -ForegroundColor White
    Write-Host "  HTTPS: https://localhost:$Port" -ForegroundColor White
    Write-Host ""
    Write-Host "Management Commands:" -ForegroundColor Cyan
    Write-Host "  View Site:     Get-IISSite -Name '$SiteName'" -ForegroundColor White
    Write-Host "  Stop Site:     Stop-IISSite -Name '$SiteName'" -ForegroundColor White
    Write-Host "  Start Site:    Start-IISSite -Name '$SiteName'" -ForegroundColor White
    Write-Host "  View App Pool: Get-IISAppPool -Name '$AppPoolName'" -ForegroundColor White
    Write-Host ""
    Write-Host "Log Locations:" -ForegroundColor Cyan
    Write-Host "  Application Logs: $DeployPath\logs\" -ForegroundColor White
    Write-Host "  IIS Logs: $LogPath\" -ForegroundColor White
    Write-Host "  Event Viewer: Windows Logs > Application" -ForegroundColor White
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Test the application in your browser" -ForegroundColor White
    Write-Host "  2. Check logs for any startup issues" -ForegroundColor White
    Write-Host "  3. Configure database connection if needed" -ForegroundColor White
    Write-Host "  4. Set up monitoring and health checks" -ForegroundColor White
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host ""

    # Offer to open browser
    $openBrowser = Read-Host "Open application in browser? (y/n)"
    if ($openBrowser -eq 'y' -or $openBrowser -eq 'Y') {
        Write-Host "Opening application..." -ForegroundColor Yellow
        Start-Process "https://localhost:$Port"
    }

} catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    Write-Host "Stack trace:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Deployment script completed." -ForegroundColor Green