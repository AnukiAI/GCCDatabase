$date = Get-Date -Format 'yyyyMMdd_HHmmss'
$zipName = "GCCWebApp_Deployment_$date.zip"
$sourceFolder = "bin\Release\net8.0"

Write-Host "Creating deployment package from Release build..." -ForegroundColor Cyan

if (!(Test-Path $sourceFolder)) {
    Write-Host "Error: $sourceFolder not found" -ForegroundColor Red
    exit 1
}

# Create the zip
Write-Host "Compressing files..." -ForegroundColor Yellow
Compress-Archive -Path "$sourceFolder\*" -DestinationPath $zipName -Force

Write-Host "`nDeployment package created successfully!" -ForegroundColor Green
Write-Host "`nFile Details:" -ForegroundColor Yellow

$fileInfo = Get-Item $zipName
Write-Host "  Name: $($fileInfo.Name)" -ForegroundColor Cyan
Write-Host "  Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Cyan
Write-Host "  Location: $($fileInfo.FullName)" -ForegroundColor Cyan
Write-Host "  Created: $($fileInfo.LastWriteTime)" -ForegroundColor Cyan

# Verify the password is correct
Write-Host "`nVerifying configuration..." -ForegroundColor Yellow
$prodConfig = Get-Content "$sourceFolder\appsettings.Production.json" -Raw
if ($prodConfig -match "Q3Ez4e!gE_47KNi") {
    Write-Host "  Correct SQL password confirmed!" -ForegroundColor Green
} else {
    Write-Host "  WARNING: SQL password may be incorrect" -ForegroundColor Red
}

Write-Host "`nPackage Contents:" -ForegroundColor Yellow
Write-Host "  Updated NuGet packages:" -ForegroundColor White
Write-Host "    - Azure.Identity 1.17.0" -ForegroundColor Gray
Write-Host "    - Azure.Extensions.AspNetCore.DataProtection.Blobs 1.5.1" -ForegroundColor Gray
Write-Host "    - ClosedXML 0.105.0" -ForegroundColor Gray
Write-Host "    - Microsoft.AspNetCore.DataProtection 8.0.16" -ForegroundColor Gray
Write-Host "    - QRCoder 1.7.0" -ForegroundColor Gray
Write-Host "    - QuestPDF 2025.7.3" -ForegroundColor Gray
Write-Host "    - Swashbuckle.AspNetCore 9.0.6" -ForegroundColor Gray
Write-Host "  Production SQL Server config:" -ForegroundColor White
Write-Host "    - Server: 68.71.129.120,1533" -ForegroundColor Gray
Write-Host "    - Database: gccwebap_certificates" -ForegroundColor Gray
Write-Host "    - Username: GCC_adminxyz123" -ForegroundColor Gray
Write-Host "    - Password: Q3Ez4e!gE_47KNi (corrected)" -ForegroundColor Gray
Write-Host "  All application DLLs and dependencies" -ForegroundColor White
Write-Host "  Configuration files for all environments" -ForegroundColor White
Write-Host "  web.config for IIS deployment" -ForegroundColor White

Write-Host "`nReady for deployment to AccuWebHosting!" -ForegroundColor Green
Write-Host "`nDeployment Instructions:" -ForegroundColor Yellow
Write-Host "1. Extract zip to server" -ForegroundColor White
Write-Host "2. Ensure ASPNETCORE_ENVIRONMENT=Production" -ForegroundColor White
Write-Host "3. Configure IIS application pool for .NET 8.0" -ForegroundColor White
Write-Host "4. Run database migrations if needed" -ForegroundColor White
