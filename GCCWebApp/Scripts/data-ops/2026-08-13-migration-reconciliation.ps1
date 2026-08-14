<#
.SYNOPSIS
  Dump the LOADED certificate data from a GCC database as UTF-8 CSV, for
  value-level reconciliation against the transform output.

.DESCRIPTION
  Plan §6 acceptance is COUNT-based ("assert post-load counts match the transform
  output"). That is not sufficient and we proved it: DEF-351 truncated 50.7% of
  product names while every count matched exactly, because the corruption was
  INSIDE fields rather than row loss. This dump exists so the companion script
  can compare VALUES, not totals.

  Uses System.Data.SqlClient and writes UTF-8 directly, deliberately NOT
  `sqlcmd -o`. A sqlcmd dump renders through the console codepage and mangles
  non-ASCII (en dashes, ®), which produced 3,086 FALSE mismatches on the first
  attempt at this comparison. Never compare text across a transport that can
  alter it.

.PARAMETER ConnectionString
  Target DB. Defaults to the local dev database.

.PARAMETER OutFile
  Destination CSV (UTF-8, no BOM).
#>
[CmdletBinding()]
param(
    [string]$ConnectionString = "Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false",
    [string]$OutFile
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($OutFile)) {
    $OutFile = Join-Path $PSScriptRoot '_reconciliation\loaded-certificates.csv'
}
$dir = Split-Path -Parent $OutFile
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

# LEFT JOINs throughout: a NULL lookup is a finding to report, not a row to drop.
# Partners/Tags are collections -> aggregated to a sorted, comma-joined string so
# the comparison is order-independent.
$sql = @'
SELECT  c.Number                                   AS [Certificate #],
        ct.Name                                    AS [Certification Type],
        c.Status                                   AS [Status],
        co.Name                                    AS [Company],
        p.Name                                     AS [Product Name],
        pc.Name                                    AS [Product Category],
        pt.Name                                    AS [Product Type],
        pw.Name                                    AS [Pathway],
        c.CertificationResults                     AS [Cert Results],
        CONVERT(varchar(10), c.EffectiveDate, 23)  AS [Effective Date],
        CONVERT(varchar(10), c.ExpirationDate, 23) AS [Expiration Date],
        c.Location                                 AS [Location],
        c.Country                                  AS [Country],
        ISNULL(STUFF((SELECT ', ' + pr.Name
               FROM CertificatePartners cp JOIN Partners pr ON pr.Id = cp.PartnersId
               WHERE cp.CertificatesId = c.Id
               ORDER BY pr.Name FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, ''), '') AS [Partner],
        ISNULL(STUFF((SELECT ', ' + t.Name
               FROM CertificateTag cta JOIN Tags t ON t.Id = cta.TagsId
               WHERE cta.CertificatesId = c.Id AND t.TagType = 'external'
               ORDER BY t.Name FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, ''), '') AS [External Tag],
        ISNULL(STUFF((SELECT ', ' + t.Name
               FROM CertificateTag cta JOIN Tags t ON t.Id = cta.TagsId
               WHERE cta.CertificatesId = c.Id AND t.TagType = 'internal'
               ORDER BY t.Name FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, ''), '') AS [Internal Tag]
FROM    Certificates c
LEFT JOIN CertificationTypes ct ON ct.Id = c.CertificationTypeId
LEFT JOIN Companies  co ON co.Id = c.CompanyId
LEFT JOIN Products   p  ON p.Id  = c.ProductId
LEFT JOIN ProductCategories pc ON pc.Id = c.ProductCategoryId
LEFT JOIN ProductTypes pt ON pt.Id = c.ProductTypeId
LEFT JOIN Pathways   pw ON pw.Id = c.PathwayId
ORDER BY c.Number;
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
        Write-Host "wrote $n rows -> $OutFile" -ForegroundColor Green
    }
    finally { $writer.Dispose(); $reader.Close() }
}
finally { $conn.Close() }
