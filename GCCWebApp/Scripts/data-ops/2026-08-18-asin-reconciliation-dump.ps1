<#
.SYNOPSIS
  Dump the LOADED product IDs (ASIN/UPC/GTIN/...) from a GCC database as UTF-8 CSV,
  for value-level reconciliation against the ASIN master list.

.DESCRIPTION
  Companion to 2026-08-13-migration-reconciliation.ps1, which dumps CERTIFICATES.
  Nothing dumped product IDs, so reconciliation-summary.csv has 15 field rows and
  no ASIN row at all -- the ASIN half of the migration was never reconciled. This
  script closes that gap.

  Uses System.Data.SqlClient and writes UTF-8 directly, deliberately NOT
  `sqlcmd -o`. A sqlcmd dump renders through the console codepage and mangles
  non-ASCII, which produced 3,086 FALSE mismatches the first time we compared
  certificate text. Product names carry the same characters. Never compare text
  across a transport that can alter it.

.PARAMETER Target
  REQUIRED. 'dev', 'test', or 'custom'. There is no default on purpose: the
  existing verification scripts silently defaulted to dev while the operator
  believed they were reading test, and a whole session's conclusions were wrong.
  The resolved Server/Database is echoed before any query runs and is stamped
  into both the output filename and the file's first data column.

.PARAMETER ConnectionString
  Only used (and required) when -Target custom.

.PARAMETER OutFile
  Destination CSV (UTF-8, no BOM). Defaults to
  _reconciliation\loaded-productids-<TARGET>.csv
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
    # Regex out the one value we need rather than stripping comments (naive
    # stripping would also mangle the "https://" inside AllowedOrigins).
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
    $OutFile = Join-Path $PSScriptRoot ("_reconciliation\loaded-productids-{0}.csv" -f $Target.ToUpper())
}
$dir = Split-Path -Parent $OutFile
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

# CertificateId is carried alongside Number because CR-038 allows SEVERAL certificates
# to share one number -- the comparer needs to see when a number's IDs are split
# across rows rather than silently unioning them.
# No IsActive filter here: inactive rows are dumped and labelled so the comparer can
# report them as a distinct bucket. Filtering in SQL would hide them from both sides.
$sql = @'
SELECT  c.Number            AS [Certificate #],
        c.Id                AS [CertificateId],
        pid.ProductIdType   AS [Type],
        pid.ProductIdValue  AS [Value],
        pid.ProductName     AS [ProductName],
        pid.Partner         AS [Partner],
        pid.Status          AS [Status],
        CASE WHEN pid.IsActive = 1 THEN 1 ELSE 0 END AS [IsActive],
        CASE WHEN pid.IsPublic = 1 THEN 1 ELSE 0 END AS [IsPublic]
FROM    CertificateProductIds pid
JOIN    Certificates c ON c.Id = pid.CertificateId
ORDER BY c.Number, pid.ProductIdType, pid.ProductIdValue;
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
        Write-Host "wrote $n product-ID rows -> $OutFile" -ForegroundColor Green
    }
    finally { $writer.Dispose(); $reader.Close() }
}
finally { $conn.Close() }
