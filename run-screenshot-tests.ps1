# GCC Database - Automated Screenshot Test Suite
# Captures screenshots for all user roles with authentication
# Created: 2025-10-05

param(
    [string]$BaseUrl = "http://localhost:5101",
    [string]$OutputDir = "screenshots",
    [string]$Resolution = "1920,1080",
    [switch]$IncludeMobile,
    [switch]$IncludeTablet,
    [string]$Role = "all" # all, public, external, analyst, admin
)

Write-Host "=== GCC Screenshot Test Suite ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Output Directory: $OutputDir" -ForegroundColor Yellow
Write-Host "Resolution: $Resolution" -ForegroundColor Yellow
Write-Host ""

# Create output directories
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputPath = Join-Path $OutputDir $timestamp
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
New-Item -ItemType Directory -Force -Path "$outputPath\public" | Out-Null
New-Item -ItemType Directory -Force -Path "$outputPath\external" | Out-Null
New-Item -ItemType Directory -Force -Path "$outputPath\analyst" | Out-Null
New-Item -ItemType Directory -Force -Path "$outputPath\admin" | Out-Null

# Test credentials
$credentials = @{
    admin = @{
        email = "admin@greencirclecertified.com"
        password = "Admin123!"
    }
    analyst = @{
        email = "analyst@greencirclecertified.com"
        password = "Analyst123!"
    }
    external = @{
        email = "partner@amazon.com"
        password = "Partner123!"
    }
}

# Helper function to capture screenshot
function Capture-Screenshot {
    param(
        [string]$Url,
        [string]$OutputFile,
        [string]$Width = "1920",
        [string]$Height = "1080",
        [string]$AuthCookie = $null
    )

    Write-Host "  Capturing: $OutputFile" -ForegroundColor Gray

    # Build Edge command with authentication if provided
    $edgeCmd = "start msedge --headless --disable-gpu --window-size=$Width,$Height"

    if ($AuthCookie) {
        # For authenticated requests, we'd need to set cookies
        # This is a simplified version - full implementation would use Selenium or Playwright
        $edgeCmd += " --user-data-dir=`"$env:TEMP\edge-test-profile`""
    }

    $edgeCmd += " --screenshot=`"$OutputFile`" `"$Url`""

    Invoke-Expression $edgeCmd
    Start-Sleep -Seconds 5 # Wait for screenshot to complete
}

# Helper function to login and get auth token (simplified)
function Get-AuthToken {
    param([string]$Role)

    Write-Host "  Authenticating as $Role..." -ForegroundColor Yellow

    $cred = $credentials[$Role]
    $loginUrl = "$BaseUrl/Account/Login"

    # This is simplified - real implementation would use Invoke-WebRequest with session
    # For now, we'll use a different approach with Selenium or manual authentication

    return $null
}

# PUBLIC USER SCREENSHOTS
if ($Role -eq "all" -or $Role -eq "public") {
    Write-Host "`n[PUBLIC USER TESTS]" -ForegroundColor Green

    Capture-Screenshot -Url "$BaseUrl/Certificate/CertificateTileView" `
        -OutputFile "$outputPath\public\01-tile-view-landing.png"

    Capture-Screenshot -Url "$BaseUrl/Certificate/Details/1" `
        -OutputFile "$outputPath\public\02-certificate-details.png"

    Capture-Screenshot -Url "$BaseUrl/Account/Login" `
        -OutputFile "$outputPath\public\03-login-page.png"
}

# EXTERNAL USER SCREENSHOTS (Requires authentication)
if ($Role -eq "all" -or $Role -eq "external") {
    Write-Host "`n[EXTERNAL USER TESTS]" -ForegroundColor Green
    Write-Host "  Note: External user tests require manual authentication" -ForegroundColor Yellow
    Write-Host "  Please login manually and run: " -ForegroundColor Yellow
    Write-Host "  .\run-screenshot-tests.ps1 -Role external -ManualAuth" -ForegroundColor Cyan
}

# ANALYST USER SCREENSHOTS (Requires authentication)
if ($Role -eq "all" -or $Role -eq "analyst") {
    Write-Host "`n[ANALYST USER TESTS]" -ForegroundColor Green
    Write-Host "  Note: Analyst user tests require manual authentication" -ForegroundColor Yellow

    # These would work if we had Selenium/Playwright for authentication
    # For now, manual process:
    Write-Host "  Manual steps:" -ForegroundColor Cyan
    Write-Host "  1. Login as analyst@greencirclecertified.com / Analyst123!" -ForegroundColor White
    Write-Host "  2. Navigate to each URL and take screenshot" -ForegroundColor White
    Write-Host "     - $BaseUrl/Dashboard" -ForegroundColor Gray
    Write-Host "     - $BaseUrl/Certificate/Create" -ForegroundColor Gray
    Write-Host "     - $BaseUrl/Certificate/Edit/1" -ForegroundColor Gray
}

# ADMIN USER SCREENSHOTS (Requires authentication)
if ($Role -eq "all" -or $Role -eq "admin") {
    Write-Host "`n[ADMIN USER TESTS]" -ForegroundColor Green
    Write-Host "  Note: Admin user tests require manual authentication" -ForegroundColor Yellow

    Write-Host "  Manual steps:" -ForegroundColor Cyan
    Write-Host "  1. Login as admin@greencirclecertified.com / Admin123!" -ForegroundColor White
    Write-Host "  2. Navigate to each URL and take screenshot" -ForegroundColor White
    Write-Host "     - $BaseUrl/Dashboard" -ForegroundColor Gray
    Write-Host "     - $BaseUrl/Certificate/Create" -ForegroundColor Gray
    Write-Host "     - $BaseUrl/Certificate/Edit/1" -ForegroundColor Gray
    Write-Host "     - $BaseUrl/Admin/UserManagement" -ForegroundColor Gray
    Write-Host "     - $BaseUrl/Admin/RoleManagement" -ForegroundColor Gray
}

# MOBILE SCREENSHOTS (if requested)
if ($IncludeMobile) {
    Write-Host "`n[MOBILE SCREENSHOTS]" -ForegroundColor Green

    Capture-Screenshot -Url "$BaseUrl/Certificate/CertificateTileView" `
        -OutputFile "$outputPath\public\mobile-01-tile-view.png" `
        -Width "375" -Height "667"

    Capture-Screenshot -Url "$BaseUrl/Certificate/Details/1" `
        -OutputFile "$outputPath\public\mobile-02-certificate-details.png" `
        -Width "375" -Height "667"
}

# TABLET SCREENSHOTS (if requested)
if ($IncludeTablet) {
    Write-Host "`n[TABLET SCREENSHOTS]" -ForegroundColor Green

    Capture-Screenshot -Url "$BaseUrl/Certificate/CertificateTileView" `
        -OutputFile "$outputPath\public\tablet-01-tile-view.png" `
        -Width "1024" -Height "768"

    Capture-Screenshot -Url "$BaseUrl/Certificate/Details/1" `
        -OutputFile "$outputPath\public\tablet-02-certificate-details.png" `
        -Width "1024" -Height "768"
}

Write-Host "`n=== Screenshots Completed ===" -ForegroundColor Cyan
Write-Host "Output directory: $outputPath" -ForegroundColor Green
Write-Host ""
Write-Host "NEXT STEPS FOR AUTHENTICATED TESTS:" -ForegroundColor Yellow
Write-Host "1. Use Selenium-based screenshot tool (recommended)" -ForegroundColor White
Write-Host "2. Or manually login and capture screenshots" -ForegroundColor White
Write-Host "3. Or use the Playwright script: .\run-playwright-screenshots.js" -ForegroundColor White
Write-Host ""
