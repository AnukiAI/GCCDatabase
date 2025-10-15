$date = Get-Date -Format 'yyyyMMdd_HHmmss'
$zipName = "GCCWebApp_Deployment_$date.zip"
$tempFolder = "deployment-temp"

Write-Host "Creating clean deployment package..." -ForegroundColor Cyan

# Create temp folder
if (Test-Path $tempFolder) {
    Remove-Item $tempFolder -Recurse -Force
}
New-Item -ItemType Directory -Path $tempFolder | Out-Null

# Copy only the essential files from publish-output (exclude problematic subfolders)
Write-Host "Copying essential files..." -ForegroundColor Yellow

# Get all items in publish-output
$items = Get-ChildItem -Path "publish-output" -Exclude "publish-*"

foreach ($item in $items) {
    Copy-Item -Path $item.FullName -Destination $tempFolder -Recurse -Force
    Write-Host "  Copied: $($item.Name)" -ForegroundColor Gray
}

# Create the zip from temp folder
Write-Host "`nCreating zip archive..." -ForegroundColor Yellow
Compress-Archive -Path "$tempFolder\*" -DestinationPath $zipName -Force

# Clean up temp folder
Remove-Item $tempFolder -Recurse -Force

Write-Host "`nDeployment package created successfully!" -ForegroundColor Green
Write-Host "`nFile Details:" -ForegroundColor Yellow

$fileInfo = Get-Item $zipName
Write-Host "  Name: $($fileInfo.Name)" -ForegroundColor Cyan
Write-Host "  Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Cyan
Write-Host "  Location: $($fileInfo.FullName)" -ForegroundColor Cyan
Write-Host "  Created: $($fileInfo.LastWriteTime)" -ForegroundColor Cyan

Write-Host "`nPackage Contents:" -ForegroundColor Yellow
Write-Host "  - Updated packages (Azure.Identity 1.17.0, ClosedXML 0.105.0, QuestPDF 2025.7.3)" -ForegroundColor White
Write-Host "  - Corrected SQL Server password: Q3Ez4e!gE_47KNi (! not l)" -ForegroundColor White
Write-Host "  - SQL Server connection: 68.71.129.120,1533" -ForegroundColor White
Write-Host "  - Database: gccwebap_certificates" -ForegroundColor White
Write-Host "  - Username: GCC_adminxyz123" -ForegroundColor White
Write-Host "  - All application DLLs and dependencies" -ForegroundColor White
Write-Host "  - Web.config for IIS deployment" -ForegroundColor White

Write-Host "`nReady for deployment to AccuWebHosting!" -ForegroundColor Green
