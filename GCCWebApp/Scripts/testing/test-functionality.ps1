# PowerShell script to test key functionality
Write-Host "Testing Certificate and Dashboard Functionality" -ForegroundColor Green

Write-Host "`n1. Testing Certificate API endpoints:" -ForegroundColor Yellow

# Test certificate tiles API
Write-Host "  - Testing /api/certificate-tiles..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5101/api/certificate-tiles" -Method GET -UseBasicParsing
    Write-Host "    Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test certificate filters API
Write-Host "  - Testing /api/certificate-tiles/filters..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5101/api/certificate-tiles/filters" -Method GET -UseBasicParsing
    Write-Host "    Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test autocomplete API
Write-Host "  - Testing /api/certificate-tiles/autocomplete..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5101/api/certificate-tiles/autocomplete?term=test" -Method GET -UseBasicParsing
    Write-Host "    Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n2. Testing Dashboard export endpoints:" -ForegroundColor Yellow

# Test stat download
Write-Host "  - Testing /Export/DownloadStat..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5101/Export/DownloadStat?type=total-active-certs&format=csv" -Method GET -UseBasicParsing
    Write-Host "    Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test table download
Write-Host "  - Testing /Export/DownloadTable..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5101/Export/DownloadTable?type=cert-types&format=csv" -Method GET -UseBasicParsing
    Write-Host "    Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n3. Testing page navigation:" -ForegroundColor Yellow

# Test Dashboard page
Write-Host "  - Testing /Dashboard..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5101/Dashboard" -Method GET -UseBasicParsing
    Write-Host "    Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Certificate Tile View
Write-Host "  - Testing /Certificate/CertificateTileView..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5101/Certificate/CertificateTileView" -Method GET -UseBasicParsing
    Write-Host "    Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTesting complete!" -ForegroundColor Green
Write-Host "If all status codes are 200, the functionality is working correctly." -ForegroundColor Cyan