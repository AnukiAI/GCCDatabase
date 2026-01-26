# Full debug import test
$ErrorActionPreference = "Continue"

# Step 1: Login
Write-Host "Logging in..."
$loginResponse = Invoke-WebRequest -Uri 'http://localhost:5101/Account/Login' -SessionVariable webSession -UseBasicParsing
$tokenMatch = [regex]::Match($loginResponse.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
$token = $tokenMatch.Groups[1].Value
$loginBody = @{ Email = 'admin'; Password = 'TestPass123!'; __RequestVerificationToken = $token }
$null = Invoke-WebRequest -Uri 'http://localhost:5101/Account/Login' -Method POST -Body $loginBody -WebSession $webSession -UseBasicParsing
Write-Host "Logged in"

# Step 2: Create CSV content directly (no file reading issues)
Write-Host "Creating CSV content..."
$csvContent = @"
CERTIFICATE_NUMBER,COMPANY_NAME,COMPANY_LOCATION,COMPANY_COUNTRY,COMPANY_EMAIL,COMPANY_WEBSITE,PRODUCT_NAME,PRODUCT_TYPE,CERTIFICATION_TYPE,EFFECTIVE_DATE,EXPIRATION_DATE,CERTIFICATE_LOCATION,CERTIFICATE_COUNTRY,PARTNER,CERTIFICATION_RESULTS,TAGS,ASIN,UPC,GTIN,EAN
IMP-001,Import Test Company,"Portland, OR",United States,import@test.com,https://import.com,Import Widget,Electronics,Recycled Content,2024-01-01,2025-01-01,"Portland, OR",United States,Amazon,95%,TestTag,B08IMPORT1,="123456789012",,
IMP-002,Import Test Company,"Portland, OR",United States,import@test.com,https://import.com,Import Gadget,Consumer Goods,Recycled Content,2024-02-01,2025-02-01,"Seattle, WA",United States,Novi,88%,TestTag,B08IMPORT2,,="0012345678901",
"@

Write-Host "CSV content created, length: $($csvContent.Length) chars"
Write-Host "Content preview:"
Write-Host $csvContent.Substring(0, 300)
Write-Host "..."

# Step 3: Upload using standard form encoding
Write-Host ""
Write-Host "Uploading via multipart form..."
$boundary = "----WebKitFormBoundary" + [System.Guid]::NewGuid().ToString("N").Substring(0,16)

# Build the multipart body manually
$LF = "`r`n"
$bodyParts = @()
$bodyParts += "--$boundary"
$bodyParts += "Content-Disposition: form-data; name=`"csvFile`"; filename=`"test.csv`""
$bodyParts += "Content-Type: text/csv"
$bodyParts += ""
$bodyParts += $csvContent
$bodyParts += "--$boundary--"

$body = $bodyParts -join $LF
$bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($body)

Write-Host "Body size: $($bodyBytes.Length) bytes"

$previewResponse = Invoke-WebRequest -Uri 'http://localhost:5101/api/CertificateImport/Preview' -Method POST -Body $bodyBytes -ContentType "multipart/form-data; boundary=$boundary" -WebSession $webSession -UseBasicParsing

Write-Host "Preview Status: $($previewResponse.StatusCode)"
Write-Host "Response:"
Write-Host $previewResponse.Content

if ($previewResponse.Headers['Content-Type'] -match 'json') {
    $result = $previewResponse.Content | ConvertFrom-Json
    Write-Host ""
    Write-Host "=== Parsed Preview ==="
    Write-Host "Import ID: $($result.importId)"
    Write-Host "Total: $($result.totalRows), New: $($result.newCertificates), Errors: $($result.invalidRows)"

    if ($result.errorRows -and $result.errorRows.Count -gt 0) {
        Write-Host "Errors:"
        foreach ($err in $result.errorRows) {
            Write-Host "  Row $($err.rowNumber): $($err.errorMessages -join ', ')"
        }
    }

    if ($result.canApply -and $result.newCertificates -gt 0) {
        Write-Host ""
        Write-Host "Applying import..."
        $applyBody = @{ importId = $result.importId } | ConvertTo-Json
        $applyResponse = Invoke-WebRequest -Uri 'http://localhost:5101/api/CertificateImport/Apply' -Method POST -Body $applyBody -ContentType "application/json" -WebSession $webSession -UseBasicParsing
        $applyResult = $applyResponse.Content | ConvertFrom-Json
        Write-Host "Result: Success=$($applyResult.success), Created=$($applyResult.certificatesCreated), Updated=$($applyResult.certificatesUpdated)"
        Write-Host "Message: $($applyResult.message)"
    }
}
