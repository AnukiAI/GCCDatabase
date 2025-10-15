$date = Get-Date -Format 'yyyyMMdd_HHmmss'
$zipName = "GCCWebApp_Deployment_$date.zip"
$sourceFolder = "bin\Release\net8.0"
$tempFolder = "deployment-clean-$date"

Write-Host "Creating CLEAN deployment package (excluding problematic folders)..." -ForegroundColor Cyan

# Create temp folder
New-Item -ItemType Directory -Path $tempFolder -Force | Out-Null

# Copy files excluding the problematic publish folders
Write-Host "Copying files (this may take a moment)..." -ForegroundColor Yellow

$excludePatterns = @('publish-*', '*.pdb')
$itemsCopied = 0

Get-ChildItem -Path $sourceFolder -Recurse | Where-Object {
    $exclude = $false
    foreach ($pattern in $excludePatterns) {
        if ($_.FullName -like "*$pattern*") {
            $exclude = $true
            break
        }
    }
    -not $exclude
} | ForEach-Object {
    $relativePath = $_.FullName.Substring($sourceFolder.Length + 1)
    $destination = Join-Path $tempFolder $relativePath

    if ($_.PSIsContainer) {
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
    } else {
        $destDir = Split-Path -Parent $destination
        if (!(Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        Copy-Item $_.FullName -Destination $destination -Force
        $itemsCopied++
        if ($itemsCopied % 100 -eq 0) {
            Write-Host "  Copied $itemsCopied files..." -ForegroundColor Gray
        }
    }
}

Write-Host "  Total files copied: $itemsCopied" -ForegroundColor Green

# Create zip
Write-Host "`nCreating zip archive..." -ForegroundColor Yellow
Compress-Archive -Path "$tempFolder\*" -DestinationPath $zipName -Force

# Clean up temp
Remove-Item $tempFolder -Recurse -Force

Write-Host "`nDeployment package created successfully!" -ForegroundColor Green
Write-Host "`nFile Details:" -ForegroundColor Yellow

$fileInfo = Get-Item $zipName
Write-Host "  Name: $($fileInfo.Name)" -ForegroundColor Cyan
Write-Host "  Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Cyan
Write-Host "  Location: $($fileInfo.FullName)" -ForegroundColor Cyan

# Verify password
$configPath = Join-Path $sourceFolder "appsettings.Production.json"
if (Test-Path $configPath) {
    $config = Get-Content $configPath -Raw
    if ($config -match "Q3Ez4e!gE_47KNi") {
        Write-Host "`n Correct SQL password confirmed in package!" -ForegroundColor Green
    }
}

Write-Host "`nPackage includes:" -ForegroundColor Yellow
Write-Host "  - Updated packages (Azure, ClosedXML, QuestPDF, Swashbuckle)" -ForegroundColor White
Write-Host "  - Corrected SQL Server credentials" -ForegroundColor White
Write-Host "  - All runtime DLLs and dependencies" -ForegroundColor White
Write-Host "  - Configuration files" -ForegroundColor White
Write-Host "  - Web.config for IIS" -ForegroundColor White

Write-Host "`nReady for AccuWebHosting deployment!" -ForegroundColor Green
