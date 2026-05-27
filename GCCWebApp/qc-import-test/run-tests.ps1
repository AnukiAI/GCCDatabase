[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:5101',
    [string]$Email = 'swest@anukiconsulting.com',
    [string]$Password = 'TestPass123!',
    [string]$RoleLabel = 'Admin'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$resultsDir = Join-Path $here 'results'
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$tokenPattern = 'name="__RequestVerificationToken"[^>]*value="([^"]+)"'

function Parse-Csv {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return @() }
    $rows = Import-Csv -Path $Path
    $items = New-Object System.Collections.ArrayList
    foreach ($r in $rows) {
        $rawName = $r.Name
        if ($null -eq $rawName) { $rawName = '' }
        $name = $rawName.Trim()
        if (-not $name) { continue }

        $desc = $r.Description
        if ($null -eq $desc) { $desc = '' } else { $desc = $desc.Trim() }

        $co = $r.Company
        if ($null -eq $co) { $co = '' } else { $co = $co.Trim() }

        $pt = $r.'Product Type'
        if ($null -eq $pt) { $pt = '' } else { $pt = $pt.Trim() }

        $statusRaw = $r.Status
        if ($null -eq $statusRaw -or -not $statusRaw.Trim()) { $statusRaw = 'Active' }
        $isActive = ($statusRaw.Trim().ToLower() -ne 'inactive')

        $null = $items.Add([PSCustomObject]@{
            name            = $name
            description     = $desc
            companyName     = $co
            productTypeName = $pt
            isActive        = $isActive
        })
    }
    return ,@($items.ToArray())
}

Write-Host "=== Login as $RoleLabel ($Email) ===" -ForegroundColor Cyan
$sess = $null
$loginPage = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -SessionVariable sess -UseBasicParsing
$token = ''
if ($loginPage.Content -match $tokenPattern) {
    $token = $Matches[1]
}
$form = @{
    Email                      = $Email
    Password                   = $Password
    RememberMe                 = 'false'
    __RequestVerificationToken = $token
}
$loginResp = $null
try {
    $loginResp = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -Method POST -Body $form -WebSession $sess -UseBasicParsing -MaximumRedirection 0 -ErrorAction Stop
} catch {
    $loginResp = $_.Exception.Response
}
Write-Host ("Login HTTP: " + $loginResp.StatusCode)

$mgmt = Invoke-WebRequest -Uri "$BaseUrl/Product/Management" -WebSession $sess -UseBasicParsing -MaximumRedirection 1 -ErrorAction SilentlyContinue
Write-Host ("Product/Management HTTP: " + $mgmt.StatusCode)
$loggedIn = ($mgmt.Content -match 'Product Management')
Write-Host ("Logged in to admin UI: " + $loggedIn)

$pageToken = ''
if ($mgmt.Content -match $tokenPattern) {
    $pageToken = $Matches[1]
}
$preview = ''
if ($pageToken.Length -ge 20) { $preview = $pageToken.Substring(0, 20) } else { $preview = $pageToken }
Write-Host ("Page antiforgery token prefix: " + $preview)

$csvFiles = Get-ChildItem -Path $here -Filter '*.csv' | Sort-Object Name

$summary = New-Object System.Collections.ArrayList
foreach ($csv in $csvFiles) {
    Write-Host ""
    Write-Host ("--- Test: " + $csv.Name + " ---") -ForegroundColor Yellow
    $items = Parse-Csv -Path $csv.FullName
    Write-Host ("Parsed rows: " + $items.Count)

    $bodyObj = @{ items = @($items) }
    $body = $bodyObj | ConvertTo-Json -Depth 6 -Compress

    $url = "$BaseUrl/Admin/BulkImportProducts"
    $headers = @{
        'Content-Type'              = 'application/json'
        'RequestVerificationToken'  = $pageToken
    }
    $statusCode = 0
    $respBody = ''
    $finalUrl = ''
    try {
        $resp = Invoke-WebRequest -Uri $url -Method POST -Body $body -Headers $headers -WebSession $sess -UseBasicParsing -MaximumRedirection 0 -ErrorAction Stop
        $statusCode = [int]$resp.StatusCode
        $respBody = $resp.Content
        if ($resp.BaseResponse -and $resp.BaseResponse.ResponseUri) {
            $finalUrl = $resp.BaseResponse.ResponseUri.AbsoluteUri
        }
    } catch [System.Net.WebException] {
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            try {
                $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $respBody = $sr.ReadToEnd()
            } catch {}
            if ($_.Exception.Response.ResponseUri) {
                $finalUrl = $_.Exception.Response.ResponseUri.AbsoluteUri
            }
            if ($_.Exception.Response.Headers['Location']) {
                $finalUrl = $finalUrl + ' -> Location: ' + $_.Exception.Response.Headers['Location']
            }
        } else {
            $statusCode = -1
            $respBody = $_.Exception.Message
        }
    } catch {
        $statusCode = -1
        $respBody = $_.Exception.Message
    }

    $bodyTrim = $respBody
    if ($respBody.Length -gt 800) { $bodyTrim = $respBody.Substring(0, 800) + '...[trunc]' }

    $trimmedStart = $respBody.TrimStart()
    $isJson = $false
    if ($trimmedStart.Length -gt 0) {
        $firstChar = $trimmedStart[0]
        if ($firstChar -eq '{' -or $firstChar -eq '[') { $isJson = $true }
    }

    $parsed = $null
    if ($isJson) {
        try { $parsed = $respBody | ConvertFrom-Json } catch {}
    }

    $rowSuccess = $null
    $rowMsg = ''
    $rowSucCnt = $null
    $rowErrCnt = $null
    $rowErrors = ''
    $rowWarn = ''
    if ($parsed) {
        $rowSuccess = $parsed.success
        $rowMsg = $parsed.message
        if ($parsed.data) {
            $rowSucCnt = $parsed.data.successCount
            $rowErrCnt = $parsed.data.errorCount
            if ($parsed.data.errors) { $rowErrors = ($parsed.data.errors -join '; ') }
            if ($parsed.data.warnings) { $rowWarn = ($parsed.data.warnings -join '; ') }
        }
    }

    $row = [PSCustomObject]@{
        Csv          = $csv.Name
        ParsedRows   = $items.Count
        HttpStatus   = $statusCode
        FinalUrl     = $finalUrl
        IsJson       = $isJson
        Success      = $rowSuccess
        ServerMsg    = $rowMsg
        SuccessCount = $rowSucCnt
        ErrorCount   = $rowErrCnt
        Errors       = $rowErrors
        Warnings     = $rowWarn
        BodyPreview  = $bodyTrim
    }
    $null = $summary.Add($row)

    Write-Host ("  HTTP {0} | success={1} | new={2} errs={3} | {4}" -f $statusCode, $rowSuccess, $rowSucCnt, $rowErrCnt, $rowMsg)
    if ($rowErrors) { Write-Host ("  errors: " + $rowErrors) -ForegroundColor DarkYellow }
    if ($rowWarn) { Write-Host ("  warnings: " + $rowWarn) -ForegroundColor DarkGray }
}

$out = Join-Path $resultsDir ($RoleLabel + '-results.json')
$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $out -Encoding utf8
Write-Host ""
Write-Host ("Results: " + $out) -ForegroundColor Green

$gp = Invoke-WebRequest -Uri "$BaseUrl/Product/GetProducts" -WebSession $sess -UseBasicParsing -ErrorAction SilentlyContinue
$gpJson = $null
try { $gpJson = $gp.Content | ConvertFrom-Json } catch {}
$qcRows = @()
if ($gpJson -and $gpJson.success -and $gpJson.data) {
    $qcRows = $gpJson.data | Where-Object { $_.name -like 'QCTest*' } | Select-Object id, name, companyName, productTypeName, status
}
$qcOut = Join-Path $resultsDir ($RoleLabel + '-grid-qctest.json')
$qcRows | ConvertTo-Json -Depth 4 | Set-Content -Path $qcOut -Encoding utf8
Write-Host ("Grid contains {0} QCTest* products. Saved to {1}" -f $qcRows.Count, $qcOut)
