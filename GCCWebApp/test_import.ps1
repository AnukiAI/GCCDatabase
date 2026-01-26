# Test import API
$ErrorActionPreference = "Continue"

# Create web session
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# Step 1: Get login page for anti-forgery token
Write-Host "Getting login page..."
$loginPage = Invoke-WebRequest -Uri 'http://localhost:5101/Account/Login' -SessionVariable session -UseBasicParsing
$tokenMatch = [regex]::Match($loginPage.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
$token = $tokenMatch.Groups[1].Value
Write-Host "Got token"

# Step 2: Login as admin - follow redirects
Write-Host "Logging in as admin..."
$loginBody = @{
    Username = 'admin'
    Password = 'TestPass123!'
    __RequestVerificationToken = $token
}

$loginResult = Invoke-WebRequest -Uri 'http://localhost:5101/Account/Login' -Method POST -Body $loginBody -WebSession $session -UseBasicParsing
Write-Host "Login completed"

# Step 3: Upload CSV for preview using multipart form
Write-Host "Uploading CSV for preview..."
$filePath = Join-Path $PSScriptRoot "test_import.csv"

Add-Type -AssemblyName System.Net.Http

# Copy cookies from session to handler
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.CookieContainer = New-Object System.Net.CookieContainer
foreach ($cookie in $session.Cookies.GetCookies("http://localhost:5101")) {
    $handler.CookieContainer.Add($cookie)
}
$httpClient = New-Object System.Net.Http.HttpClient($handler)

$multipartContent = New-Object System.Net.Http.MultipartFormDataContent
$fileBytes = [System.IO.File]::ReadAllBytes($filePath)
$byteContent = New-Object System.Net.Http.ByteArrayContent($fileBytes)
$byteContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("text/csv")
$multipartContent.Add($byteContent, "csvFile", "test_import.csv")

$response = $httpClient.PostAsync("http://localhost:5101/api/CertificateImport/Preview", $multipartContent).Result
$responseContent = $response.Content.ReadAsStringAsync().Result

Write-Host "Preview response status: $($response.StatusCode)"
Write-Host "Preview response:"
Write-Host $responseContent

# Parse response to get importId
if ($response.IsSuccessStatusCode) {
    $previewData = $responseContent | ConvertFrom-Json
    Write-Host ""
    Write-Host "=== Preview Summary ==="
    Write-Host "Import ID: $($previewData.importId)"
    Write-Host "Total Rows: $($previewData.totalRows)"
    Write-Host "New Certificates: $($previewData.newCertificates)"
    Write-Host "Modified: $($previewData.modifiedCertificates)"
    Write-Host "Errors: $($previewData.invalidRows)"
    Write-Host "Companies to create: $($previewData.companiesToCreate)"
    Write-Host "Products to create: $($previewData.productsToCreate)"

    if ($previewData.errorRows -and $previewData.errorRows.Count -gt 0) {
        Write-Host ""
        Write-Host "=== Errors ==="
        foreach ($err in $previewData.errorRows) {
            Write-Host "Row $($err.rowNumber): $($err.errorMessages -join ', ')"
        }
    }

    if ($previewData.canApply) {
        Write-Host ""
        Write-Host "Applying import..."

        $applyBody = @{
            importId = $previewData.importId
        } | ConvertTo-Json

        $applyContent = New-Object System.Net.Http.StringContent($applyBody, [System.Text.Encoding]::UTF8, "application/json")
        $applyResponse = $httpClient.PostAsync("http://localhost:5101/api/CertificateImport/Apply", $applyContent).Result
        $applyResult = $applyResponse.Content.ReadAsStringAsync().Result

        Write-Host "Apply response status: $($applyResponse.StatusCode)"
        Write-Host "Apply response:"
        Write-Host $applyResult

        if ($applyResponse.IsSuccessStatusCode) {
            $applyData = $applyResult | ConvertFrom-Json
            Write-Host ""
            Write-Host "=== Import Result ==="
            Write-Host "Success: $($applyData.success)"
            Write-Host "Created: $($applyData.certificatesCreated)"
            Write-Host "Updated: $($applyData.certificatesUpdated)"
            Write-Host "Message: $($applyData.message)"
        }
    } else {
        Write-Host ""
        Write-Host "Cannot apply - has critical errors"
    }
} else {
    Write-Host "Preview failed with status: $($response.StatusCode)"
}

$httpClient.Dispose()
