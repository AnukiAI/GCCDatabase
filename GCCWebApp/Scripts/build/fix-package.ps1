# Fix the deployment package

# Create logs folder
New-Item -ItemType Directory -Force -Path 'C:\Temp\CheckPackage\logs' | Out-Null
New-Item -ItemType File -Path 'C:\Temp\CheckPackage\logs\.gitkeep' -Force | Out-Null
Write-Host "Created logs folder"

# Remove old package
$oldPackage = "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\GCCWebApp-SelfContained-SecurityFixes-20251015.zip"
if (Test-Path $oldPackage) {
    Remove-Item $oldPackage -Force
}

# Create new fixed package
$newPackage = "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\GCCWebApp-SelfContained-SecurityFixes-20251015-FIXED.zip"
Compress-Archive -Path 'C:\Temp\CheckPackage\*' -DestinationPath $newPackage -Force

# Show results
$size = (Get-Item $newPackage).Length / 1MB
Write-Host ""
Write-Host "SUCCESS: Created GCCWebApp-SelfContained-SecurityFixes-20251015-FIXED.zip"
Write-Host "Size: $([math]::Round($size, 2)) MB"
Write-Host ""
Write-Host "FIXES APPLIED:"
Write-Host "  1. web.config: arguments='' (was: arguments='.\GCCWebApp.dll')"
Write-Host "  2. web.config: Added environment variables"
Write-Host "  3. Created logs folder for stdout logging"
Write-Host ""
Write-Host "Ready to deploy!"
