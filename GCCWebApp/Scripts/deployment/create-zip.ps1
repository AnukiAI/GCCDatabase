# Create ZIP file of deployment package
$ProgressPreference = 'SilentlyContinue'

Write-Host "Creating ZIP file of deployment package..." -ForegroundColor Cyan

# Remove problematic folders first
Get-ChildItem "publish-accuweb-production" -Recurse |
    Where-Object { $_.FullName -match 'publish-security-fixes' } |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Create the ZIP
Compress-Archive -Path "publish-accuweb-production\*" `
                 -DestinationPath "GCCWebApp_Production_Deployment_20251016.zip" `
                 -CompressionLevel Optimal `
                 -Force

if (Test-Path "GCCWebApp_Production_Deployment_20251016.zip") {
    $size = (Get-Item "GCCWebApp_Production_Deployment_20251016.zip").Length / 1MB
    Write-Host "SUCCESS: ZIP file created - $([math]::Round($size, 2)) MB" -ForegroundColor Green
} else {
    Write-Host "ERROR: ZIP file creation failed" -ForegroundColor Red
}
