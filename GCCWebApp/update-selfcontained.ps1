# Update self-contained package with security fixes

cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Clean temp folder
if (Test-Path 'C:\Temp\GCC-SC-Update') {
    Remove-Item 'C:\Temp\GCC-SC-Update' -Recurse -Force
}

# Extract old self-contained package
Write-Host "Extracting GCCWebApp-SelfContained-20251011.zip..."
Expand-Archive -Path 'GCCWebApp-SelfContained-20251011.zip' -DestinationPath 'C:\Temp\GCC-SC-Update'
Write-Host "Extracted successfully"

# Copy new security-fixed DLLs from publish-security-fixes
Write-Host "Copying security-fixed DLLs from publish-security-fixes..."
Copy-Item 'publish-security-fixes\GCCWebApp.dll' 'C:\Temp\GCC-SC-Update\GCCWebApp.dll' -Force
Copy-Item 'publish-security-fixes\GCCWebApp.pdb' 'C:\Temp\GCC-SC-Update\GCCWebApp.pdb' -Force -ErrorAction SilentlyContinue
Write-Host "DLLs updated with security fixes"

# Create new package
Write-Host "Creating GCCWebApp-SelfContained-SecurityFixes-20251015.zip..."
Compress-Archive -Path 'C:\Temp\GCC-SC-Update\*' -DestinationPath 'GCCWebApp-SelfContained-SecurityFixes-20251015.zip' -Force

# Show size
$size = (Get-Item 'GCCWebApp-SelfContained-SecurityFixes-20251015.zip').Length / 1MB
Write-Host ""
Write-Host "SUCCESS: Created GCCWebApp-SelfContained-SecurityFixes-20251015.zip"
Write-Host "Size: $([math]::Round($size, 2)) MB"
Write-Host ""
Write-Host "This package includes:"
Write-Host "  - Self-contained .NET runtime (works on AccuWebHosting)"
Write-Host "  - Security fixes from October 15, 2025"
Write-Host "  - Ready to deploy"
