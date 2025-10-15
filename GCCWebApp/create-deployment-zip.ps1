$date = Get-Date -Format 'yyyyMMdd_HHmmss'
$zipName = "GCCWebApp_Deployment_$date.zip"

Write-Host "Creating deployment package..." -ForegroundColor Cyan

# Create the zip
Compress-Archive -Path 'publish-output\*' -DestinationPath $zipName -Force

Write-Host "`nDeployment package created successfully!" -ForegroundColor Green
Write-Host "`nFile Details:" -ForegroundColor Yellow

$fileInfo = Get-Item $zipName
Write-Host "  Name: $($fileInfo.Name)" -ForegroundColor Cyan
Write-Host "  Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Cyan
Write-Host "  Location: $($fileInfo.FullName)" -ForegroundColor Cyan
Write-Host "  Created: $($fileInfo.LastWriteTime)" -ForegroundColor Cyan

Write-Host "`nPackage Contents:" -ForegroundColor Yellow
Write-Host "  - Updated packages (Azure.Identity, ClosedXML, QuestPDF, etc.)" -ForegroundColor White
Write-Host "  - Corrected SQL Server password in appsettings.Production.json" -ForegroundColor White
Write-Host "  - All application DLLs and dependencies" -ForegroundColor White
Write-Host "  - Web.config for IIS deployment" -ForegroundColor White

Write-Host "`nReady for deployment to AccuWebHosting!" -ForegroundColor Green
