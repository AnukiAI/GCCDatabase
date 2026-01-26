# Simple import test using Invoke-WebRequest
$ErrorActionPreference = "Continue"

Write-Host "Step 1: Getting login page..."
$loginResponse = Invoke-WebRequest -Uri 'http://localhost:5101/Account/Login' -SessionVariable webSession -UseBasicParsing
$tokenMatch = [regex]::Match($loginResponse.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
$token = $tokenMatch.Groups[1].Value
Write-Host "Got token"

Write-Host "Step 2: Logging in..."
$loginBody = @{
    Username = 'admin'
    Password = 'TestPass123!'
    __RequestVerificationToken = $token
}
$loginResult = Invoke-WebRequest -Uri 'http://localhost:5101/Account/Login' -Method POST -Body $loginBody -WebSession $webSession -UseBasicParsing
Write-Host "Logged in - Status: $($loginResult.StatusCode)"

Write-Host "Step 3: Uploading CSV using form..."
$filePath = Join-Path $PSScriptRoot "test_import.csv"
$fileBytes = [System.IO.File]::ReadAllBytes($filePath)

$boundary = "----WebKitFormBoundary" + [System.Guid]::NewGuid().ToString("N").Substring(0,16)

$bodyStart = "--$boundary`r`nContent-Disposition: form-data; name=`"csvFile`"; filename=`"test_import.csv`"`r`nContent-Type: text/csv`r`n`r`n"
$bodyEnd = "`r`n--$boundary--`r`n"

$bodyStartBytes = [System.Text.Encoding]::UTF8.GetBytes($bodyStart)
$bodyEndBytes = [System.Text.Encoding]::UTF8.GetBytes($bodyEnd)

$body = New-Object byte[] ($bodyStartBytes.Length + $fileBytes.Length + $bodyEndBytes.Length)
[System.Buffer]::BlockCopy($bodyStartBytes, 0, $body, 0, $bodyStartBytes.Length)
[System.Buffer]::BlockCopy($fileBytes, 0, $body, $bodyStartBytes.Length, $fileBytes.Length)
[System.Buffer]::BlockCopy($bodyEndBytes, 0, $body, $bodyStartBytes.Length + $fileBytes.Length, $bodyEndBytes.Length)

try {
    $previewResponse = Invoke-WebRequest -Uri 'http://localhost:5101/api/CertificateImport/Preview' -Method POST -Body $body -ContentType "multipart/form-data; boundary=$boundary" -WebSession $webSession -UseBasicParsing
    Write-Host "Preview Status: $($previewResponse.StatusCode)"

    $result = $previewResponse.Content | ConvertFrom-Json
    Write-Host ""
    Write-Host "=== Preview Summary ==="
    Write-Host "Import ID: $($result.importId)"
    Write-Host "Total Rows: $($result.totalRows)"
    Write-Host "New: $($result.newCertificates)"
    Write-Host "Modified: $($result.modifiedCertificates)"
    Write-Host "Errors: $($result.invalidRows)"
    Write-Host "Companies to create: $($result.companiesToCreate)"
    Write-Host "Products to create: $($result.productsToCreate)"
    Write-Host "Can Apply: $($result.canApply)"

    if ($result.errorRows -and $result.errorRows.Count -gt 0) {
        Write-Host ""
        Write-Host "=== Errors ==="
        foreach ($err in $result.errorRows) {
            Write-Host "Row $($err.rowNumber): $($err.errorMessages -join ', ')"
        }
    }

    if ($result.canApply) {
        Write-Host ""
        Write-Host "Step 4: Applying import..."

        $applyBody = @{ importId = $result.importId } | ConvertTo-Json
        $applyResponse = Invoke-WebRequest -Uri 'http://localhost:5101/api/CertificateImport/Apply' -Method POST -Body $applyBody -ContentType "application/json" -WebSession $webSession -UseBasicParsing

        $applyResult = $applyResponse.Content | ConvertFrom-Json
        Write-Host ""
        Write-Host "=== Import Result ==="
        Write-Host "Success: $($applyResult.success)"
        Write-Host "Created: $($applyResult.certificatesCreated)"
        Write-Host "Updated: $($applyResult.certificatesUpdated)"
        Write-Host "Message: $($applyResult.message)"
    }
} catch {
    Write-Host "Error: $_"
    Write-Host $_.Exception.Response
}
