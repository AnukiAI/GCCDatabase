# Test using form-based upload page (not API)
$ErrorActionPreference = "Continue"

# Step 1: Login
Write-Host "Logging in..."
$loginResponse = Invoke-WebRequest -Uri 'http://localhost:5101/Account/Login' -SessionVariable webSession -UseBasicParsing
$tokenMatch = [regex]::Match($loginResponse.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
$token = $tokenMatch.Groups[1].Value
$loginBody = @{ Email = 'admin'; Password = 'TestPass123!'; __RequestVerificationToken = $token }
$null = Invoke-WebRequest -Uri 'http://localhost:5101/Account/Login' -Method POST -Body $loginBody -WebSession $webSession -UseBasicParsing
Write-Host "Logged in"

# Step 2: Get the import page to get the anti-forgery token
Write-Host "Getting import page..."
$importPage = Invoke-WebRequest -Uri 'http://localhost:5101/CertificateImport' -WebSession $webSession -UseBasicParsing
$formToken = [regex]::Match($importPage.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
Write-Host "Got form token"

# Step 3: Create a proper test CSV file
$csvPath = Join-Path $PSScriptRoot "test_form_upload.csv"
$csvContent = @"
CERTIFICATE_NUMBER,COMPANY_NAME,COMPANY_LOCATION,COMPANY_COUNTRY,COMPANY_EMAIL,COMPANY_WEBSITE,PRODUCT_NAME,PRODUCT_TYPE,CERTIFICATION_TYPE,EFFECTIVE_DATE,EXPIRATION_DATE,CERTIFICATE_LOCATION,CERTIFICATE_COUNTRY,PARTNER,CERTIFICATION_RESULTS,TAGS,ASIN,UPC,GTIN,EAN
FORM-001,Form Test Corp,"Denver, CO",United States,form@test.com,https://form.com,Form Widget,Electronics,Recycled Content,2024-01-01,2025-01-01,"Denver, CO",United States,Amazon,93%,FormTest,B08FORM001,="123456789012",,
FORM-002,Form Test Corp,"Denver, CO",United States,form@test.com,https://form.com,Form Gadget,Consumer Goods,Recycled Content,2024-02-01,2025-02-01,"Denver, CO",United States,Novi,89%,FormTest,B08FORM002,,="0012345678901",
"@

# Write with explicit UTF8 encoding without BOM
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($csvPath, $csvContent, $utf8NoBom)
Write-Host "CSV file created: $csvPath"

# Verify file content
$fileContent = [System.IO.File]::ReadAllText($csvPath)
$lines = $fileContent -split "`r?`n"
Write-Host "File has $($lines.Count) lines"

# Step 4: Upload using proper multipart/form-data with InvokeRestMethod
Write-Host ""
Write-Host "Uploading via form..."

# Use .NET HttpClient for proper multipart handling
Add-Type -AssemblyName System.Net.Http

$handler = New-Object System.Net.Http.HttpClientHandler
$handler.CookieContainer = New-Object System.Net.CookieContainer
foreach ($cookie in $webSession.Cookies.GetCookies("http://localhost:5101")) {
    $handler.CookieContainer.Add($cookie)
}
$client = New-Object System.Net.Http.HttpClient($handler)

$multipartContent = New-Object System.Net.Http.MultipartFormDataContent

# Add anti-forgery token
$tokenContent = New-Object System.Net.Http.StringContent($formToken)
$multipartContent.Add($tokenContent, "__RequestVerificationToken")

# Add the file
$fileBytes = [System.IO.File]::ReadAllBytes($csvPath)
$fileContent = New-Object System.Net.Http.ByteArrayContent($fileBytes)
$fileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("text/csv")
$multipartContent.Add($fileContent, "csvFile", "test_form_upload.csv")

$response = $client.PostAsync("http://localhost:5101/CertificateImport/Upload", $multipartContent).Result
$responseContent = $response.Content.ReadAsStringAsync().Result

Write-Host "Upload response status: $($response.StatusCode)"
Write-Host "Response length: $($responseContent.Length) chars"

# Check if we got the preview page
if ($responseContent -match "Preview Summary") {
    Write-Host "SUCCESS: Got preview page!"

    # Extract stats from HTML
    $totalMatch = [regex]::Match($responseContent, 'stat-card-value">(\d+)</div>\s*<div class="stat-card-label">Total Rows')
    $newMatch = [regex]::Match($responseContent, 'stat-card-value">(\d+)</div>\s*<div class="stat-card-label">New')
    $errMatch = [regex]::Match($responseContent, 'stat-card-value">(\d+)</div>\s*<div class="stat-card-label">Errors')

    Write-Host "Total: $($totalMatch.Groups[1].Value), New: $($newMatch.Groups[1].Value), Errors: $($errMatch.Groups[1].Value)"
} elseif ($responseContent -match "alert-danger") {
    $errorMatch = [regex]::Match($responseContent, 'alert-danger[^>]*>[^<]*<[^>]*>[^<]*</[^>]*>\s*([^<]+)')
    Write-Host "ERROR: $($errorMatch.Groups[1].Value)"
} else {
    Write-Host "Response preview:"
    Write-Host $responseContent.Substring(0, [Math]::Min(1500, $responseContent.Length))
}

$client.Dispose()
