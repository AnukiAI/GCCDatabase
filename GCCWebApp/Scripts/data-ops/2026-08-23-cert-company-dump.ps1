<#
.SYNOPSIS
  Dump certificate -> company (and product) from a GCC database as UTF-8 CSV.

.DESCRIPTION
  Companion to 2026-08-23-asin-record-log.py, which needs a company for every
  certificate in order to group ASINs the way the client counts them.

  Why a new script rather than reusing an existing one: BOTH existing certificate
  dumps take a plain -ConnectionString that DEFAULTS TO DEV
  (2026-08-13-migration-reconciliation.ps1, 2026-08-15-verify-dump.ps1). That
  default is the exact failure that cost a session on 2026-08-17 -- an operator
  ran a "test" verification against dev and every conclusion drawn from it was
  wrong. This script copies the SAFE pattern from
  2026-08-18-asin-reconciliation-dump.ps1 instead: -Target is MANDATORY, the
  resolved server/database is echoed before any query runs, and the target is
  stamped into the output filename.

  Uses System.Data.SqlClient and writes UTF-8 directly, deliberately NOT
  `sqlcmd -o`. A sqlcmd dump renders through the console codepage and mangles
  non-ASCII, which produced 3,086 FALSE mismatches the first time certificate
  text was compared. Company names carry the same characters (Estee Lauder is
  spelled with an accent in the source). Never compare text across a transport
  that can alter it.

.PARAMETER Target
  REQUIRED. 'dev', 'test', or 'custom'. No default, on purpose -- see above.

.PARAMETER ConnectionString
  Only used (and required) when -Target custom.

.PARAMETER OutFile
  Destination CSV (UTF-8, no BOM). Defaults to
  _reconciliation\loaded-certs-companies-<TARGET>.csv
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'custom')]
    [string]$Target,

    [string]$ConnectionString,
    [string]$OutFile
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Get-ProductionConnectionString {
    $settings = Join-Path $repoRoot 'appsettings.Production.json'
    if (-not (Test-Path $settings)) {
        throw "Cannot locate appsettings.Production.json at '$settings'. Use -Target custom -ConnectionString ..."
    }
    # JSONC: the file carries // comments, which PS 5.1 ConvertFrom-Json rejects.
    $raw = Get-Content $settings -Raw
    $m = [regex]::Match($raw, '"DefaultConnection"\s*:\s*"((?:[^"\\]|\\.)*)"')
    if (-not $m.Success) { throw "No ConnectionStrings:DefaultConnection in appsettings.Production.json." }
    return ($m.Groups[1].Value -replace '\\"', '"' -replace '\\\\', '\')
}

switch ($Target) {
    'dev' {
        $ConnectionString = "Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false"
    }
    'test' {
        # "Production" names the CONFIG FILE only. gccwebapp.com is the TEST environment.
        $ConnectionString = Get-ProductionConnectionString
    }
    'custom' {
        if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
            throw "-Target custom requires -ConnectionString."
        }
    }
}

$csb = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $ConnectionString
Write-Host ""
Write-Host "  TARGET   : $Target" -ForegroundColor Cyan
Write-Host "  SERVER   : $($csb['Data Source'])" -ForegroundColor Cyan
Write-Host "  DATABASE : $($csb['Initial Catalog'])" -ForegroundColor Cyan
Write-Host ""

if ([string]::IsNullOrWhiteSpace($OutFile)) {
    $OutFile = Join-Path $PSScriptRoot ("_reconciliation\loaded-certs-companies-{0}.csv" -f $Target.ToUpper())
}
$dir = Split-Path -Parent $OutFile
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

# CertificateId is carried alongside Number because CR-038 allows SEVERAL certificates
# to share one number. A consumer that keys on Number alone silently unions them; the
# record log needs to see the split.
$sql = @'
SELECT  c.Id                                       AS [CertificateId],
        c.Number                                   AS [Certificate #],
        ISNULL(co.Name, '')                        AS [Company],
        ISNULL(p.Name, '')                         AS [Product],
        c.Status                                   AS [Status],
        CONVERT(varchar(10), c.EffectiveDate, 23)  AS [Effective],
        CONVERT(varchar(10), c.ExpirationDate, 23) AS [Expiration]
FROM    Certificates c
LEFT JOIN Companies co ON co.Id = c.CompanyId
LEFT JOIN Products  p  ON p.Id  = c.ProductId
ORDER BY c.Number, c.Id;
'@

$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$conn.Open()
try {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $cmd.CommandTimeout = 1800
    $reader = $cmd.ExecuteReader()

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $writer = New-Object System.IO.StreamWriter($OutFile, $false, $utf8NoBom)
    try {
        $cols = @()
        for ($i = 0; $i -lt $reader.FieldCount; $i++) { $cols += $reader.GetName($i) }
        $writer.WriteLine(($cols | ForEach-Object { '"' + ($_ -replace '"', '""') + '"' }) -join ',')

        $n = 0
        while ($reader.Read()) {
            $vals = @()
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                $v = if ($reader.IsDBNull($i)) { '' } else { [string]$reader.GetValue($i) }
                $vals += '"' + ($v -replace '"', '""') + '"'
            }
            $writer.WriteLine($vals -join ',')
            $n++
        }
        Write-Host "wrote $n certificate rows -> $OutFile" -ForegroundColor Green
    }
    finally { $writer.Dispose(); $reader.Close() }
}
finally { $conn.Close() }
