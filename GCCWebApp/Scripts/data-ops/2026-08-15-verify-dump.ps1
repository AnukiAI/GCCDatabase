<#
.SYNOPSIS
  Dump certificates and product identifiers as UTF-8 CSV for the end-to-end
  spreadsheet-to-database verification.

.DESCRIPTION
  Companion to 2026-08-15-verify-spreadsheet-to-database.py.

  Uses System.Data.SqlClient and writes UTF-8 directly — deliberately NOT
  `sqlcmd -o`, whose console-codepage rendering mangles non-ASCII (® became
  two characters and produced 3,086 false mismatches the first time this class
  of comparison was attempted).
#>
[CmdletBinding()]
param(
    [string]$ConnectionString = "Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false",
    [string]$OutDir
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($OutDir)) { $OutDir = Join-Path $PSScriptRoot '_verify_input' }
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }

$queries = @{
    'db-certificates.csv' = @'
SELECT  c.Number                                   AS [Number],
        co.Name                                    AS [Company],
        p.Name                                     AS [Product],
        c.Status                                   AS [Status],
        CONVERT(varchar(10), c.EffectiveDate, 23)  AS [Effective],
        CONVERT(varchar(10), c.ExpirationDate, 23) AS [Expiration]
FROM    Certificates c
LEFT JOIN Companies co ON co.Id = c.CompanyId
LEFT JOIN Products  p  ON p.Id  = c.ProductId
ORDER BY c.Number, c.Id;
'@
    'db-productids.csv'   = @'
SELECT  c.Number          AS [Number],
        pid.ProductIdType AS [Type],
        pid.ProductIdValue AS [Value]
FROM    CertificateProductIds pid
JOIN    Certificates c ON c.Id = pid.CertificateId
ORDER BY c.Number, pid.ProductIdValue;
'@
}

$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$conn.Open()
try {
    foreach ($name in $queries.Keys) {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $queries[$name]
        $cmd.CommandTimeout = 1800
        $reader = $cmd.ExecuteReader()

        $path = Join-Path $OutDir $name
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        $writer = New-Object System.IO.StreamWriter($path, $false, $utf8NoBom)
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
            Write-Host ("{0,-24} {1,7} rows -> {2}" -f $name, $n, $path) -ForegroundColor Green
        }
        finally { $writer.Dispose(); $reader.Close() }
    }
}
finally { $conn.Close() }
