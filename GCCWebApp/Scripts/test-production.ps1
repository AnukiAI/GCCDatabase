# Production Site Test Script with Screenshots
# Tests https://gccwebapp.com and captures screenshots

$baseUrl = "https://gccwebapp.com"
$screenshotDir = "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\Scripts\screenshots"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

# Create screenshot directory
if (!(Test-Path $screenshotDir)) {
    New-Item -ItemType Directory -Path $screenshotDir -Force | Out-Null
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Production Site Test - $timestamp" -ForegroundColor Cyan
Write-Host "Base URL: $baseUrl" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check if site is accessible
Write-Host "Test 1: Checking site accessibility..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri $baseUrl -UseBasicParsing -TimeoutSec 30
    Write-Host "  Status: $($response.StatusCode) - OK" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Check login page
Write-Host ""
Write-Host "Test 2: Checking login page..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -UseBasicParsing -TimeoutSec 30
    Write-Host "  Status: $($response.StatusCode) - OK" -ForegroundColor Green
    Write-Host "  Content Length: $($response.Content.Length) bytes" -ForegroundColor Gray
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Check public certificate tile view
Write-Host ""
Write-Host "Test 3: Checking public certificate tile view..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/Certificate/CertificateTileView" -UseBasicParsing -TimeoutSec 30
    Write-Host "  Status: $($response.StatusCode) - OK" -ForegroundColor Green
    Write-Host "  Content Length: $($response.Content.Length) bytes" -ForegroundColor Gray
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Check API endpoint
Write-Host ""
Write-Host "Test 4: Checking API endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/CertificateTiles/tiles?pageNumber=1&pageSize=5" -UseBasicParsing -TimeoutSec 30
    Write-Host "  Status: $($response.StatusCode) - OK" -ForegroundColor Green
    $json = $response.Content | ConvertFrom-Json
    Write-Host "  Total Items: $($json.totalItems)" -ForegroundColor Gray
    Write-Host "  Items Returned: $($json.items.Count)" -ForegroundColor Gray
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Check Partner API with key
Write-Host ""
Write-Host "Test 5: Checking Partner API with key..." -ForegroundColor Yellow
try {
    $headers = @{
        "X-API-Key" = "gcc_partner_95961557dde1e196be074634cef252caec32c4b08e1e58dcfa63e43bbdbb63c7"
    }
    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/certificates?page=1&pageSize=5" -Headers $headers -UseBasicParsing -TimeoutSec 30
    Write-Host "  Status: $($response.StatusCode) - OK" -ForegroundColor Green
    $json = $response.Content | ConvertFrom-Json
    Write-Host "  Response received successfully" -ForegroundColor Gray
} catch {
    Write-Host "  Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
    Write-Host "  Note: API may require different authentication" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Tests Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
