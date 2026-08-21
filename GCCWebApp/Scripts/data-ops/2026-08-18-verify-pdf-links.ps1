<#
.SYNOPSIS
  Post-link verification for the migrated certificate PDFs (runbook section 8 / section 11 step 4).

.DESCRIPTION
  READ-ONLY. Asserts the four things the linker's own summary cannot prove, because the linker
  reports success whether or not the files ended up where the application will look for them:

    1. how many certificates carry a PDF / thumbnail reference
    2. ZERO certificates share a CertificateFile path
    3. ZERO certificates share a CertificateImagePath path
       (CR-038 made this a real risk: thumbnails are named {Number}_thumb_{timestamp}.png and a
        certificate number is no longer unique, so siblings thumbnailed in the same second used to
        overwrite each other -- 123 thumbnails were shared before the guard was added)
    4. files on disk == database references, in BOTH directions, for the staging folder

  Step 4 is the one that matters after a staged upload. The database can hold 5,474 perfectly
  well-formed logical paths while the physical files sit on a workstation -- in which case every
  certificate PDF 404s on the server and nothing in the DB reveals it (DEF-319 in miniature).

  Run it twice: once locally against the staging folder right after link-pdfs, and again with
  -StagingRoot pointed at the server's upload root once the transfer is done.

.PARAMETER StagingRoot
  The uploads root to compare against -- the folder that directly contains certificates\ and images\.
  Omit to check the database only.

.PARAMETER ConnectionString
  Defaults to DefaultConnection from appsettings.Production.json (the test environment).

.EXAMPLE
  .\2026-08-18-verify-pdf-links.ps1 -StagingRoot C:\Users\swest\GCCUploadStaging
#>
[CmdletBinding()]
param(
    [string]$StagingRoot,
    [string]$ConnectionString
)

$ErrorActionPreference = 'Stop'
$proj = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    # appsettings.Production.json is JSONC -- PS 5.1's ConvertFrom-Json rejects the // comments.
    $raw = [System.IO.File]::ReadAllText((Join-Path $proj 'appsettings.Production.json'))
    $m = [regex]::Match($raw, '"DefaultConnection"\s*:\s*"((?:[^"\\]|\\.)*)"')
    if (-not $m.Success) { throw 'Could not read ConnectionStrings:DefaultConnection.' }
    $ConnectionString = $m.Groups[1].Value
}

Add-Type -AssemblyName System.Data
$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$conn.Open()

function Scalar([string]$sql) {
    $c = $conn.CreateCommand(); $c.CommandText = $sql; $c.CommandTimeout = 300
    return $c.ExecuteScalar()
}
function Column([string]$sql) {
    $c = $conn.CreateCommand(); $c.CommandText = $sql; $c.CommandTimeout = 300
    $r = $c.ExecuteReader()
    $out = New-Object System.Collections.Generic.List[string]
    while ($r.Read()) { if (-not $r.IsDBNull(0)) { $out.Add($r.GetString(0)) } }
    $r.Close()
    return $out
}

$fail = 0
function Assert([string]$name, [bool]$ok, [string]$detail) {
    if ($ok) { Write-Host "  PASS  $name  -- $detail" -ForegroundColor Green }
    else     { Write-Host "  FAIL  $name  -- $detail" -ForegroundColor Red; $script:fail++ }
}

Write-Host "`nDatabase: $((Scalar 'SELECT DB_NAME()'))" -ForegroundColor Cyan

$certs   = Scalar 'SELECT COUNT(*) FROM dbo.Certificates'
$withPdf = Scalar "SELECT COUNT(*) FROM dbo.Certificates WHERE CertificateFile IS NOT NULL AND CertificateFile <> ''"
$withThm = Scalar "SELECT COUNT(*) FROM dbo.Certificates WHERE CertificateImagePath IS NOT NULL AND CertificateImagePath <> ''"
Write-Host "  certificates=$certs  withPdf=$withPdf  withThumbnail=$withThm"

$sharePdf = Scalar @"
SELECT ISNULL(SUM(n),0) FROM (SELECT n=COUNT(*) FROM dbo.Certificates
  WHERE CertificateFile IS NOT NULL AND CertificateFile <> ''
  GROUP BY CertificateFile HAVING COUNT(*) > 1) x
"@
$shareThm = Scalar @"
SELECT ISNULL(SUM(n),0) FROM (SELECT n=COUNT(*) FROM dbo.Certificates
  WHERE CertificateImagePath IS NOT NULL AND CertificateImagePath <> ''
  GROUP BY CertificateImagePath HAVING COUNT(*) > 1) x
"@
Assert 'no certificates share a PDF path'       ([int]$sharePdf -eq 0) "$sharePdf sharing"
Assert 'no certificates share a thumbnail path' ([int]$shareThm -eq 0) "$shareThm sharing"

if ($PSBoundParameters.ContainsKey('StagingRoot') -and $StagingRoot) {
    if (-not (Test-Path $StagingRoot)) { throw "StagingRoot not found: $StagingRoot" }

    # DB stores a logical path: uploads/certificates/<file> -- compare on file name only.
    $dbPdf = [System.Collections.Generic.HashSet[string]]::new(
        [string[]](Column "SELECT CertificateFile FROM dbo.Certificates WHERE CertificateFile IS NOT NULL AND CertificateFile <> ''" |
            ForEach-Object { Split-Path $_ -Leaf }), [StringComparer]::OrdinalIgnoreCase)
    $dbThm = [System.Collections.Generic.HashSet[string]]::new(
        [string[]](Column "SELECT CertificateImagePath FROM dbo.Certificates WHERE CertificateImagePath IS NOT NULL AND CertificateImagePath <> ''" |
            ForEach-Object { Split-Path $_ -Leaf }), [StringComparer]::OrdinalIgnoreCase)

    foreach ($pair in @(
        @{ Label = 'PDF';       Dir = (Join-Path $StagingRoot 'certificates'); Db = $dbPdf },
        @{ Label = 'thumbnail'; Dir = (Join-Path $StagingRoot 'images');       Db = $dbThm })) {

        $disk = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        if (Test-Path $pair.Dir) {
            Get-ChildItem $pair.Dir -File | ForEach-Object { [void]$disk.Add($_.Name) }
        }

        # Compare SETS, not counts -- equal counts survive the very failure this is meant to detect.
        $missing = @($pair.Db  | Where-Object { -not $disk.Contains($_) })
        $extra   = @($disk     | Where-Object { -not $pair.Db.Contains($_) })

        Assert "$($pair.Label): every DB reference exists on disk" ($missing.Count -eq 0) `
            "db=$($pair.Db.Count) disk=$($disk.Count) missingOnDisk=$($missing.Count)"
        if ($missing.Count) { $missing | Select-Object -First 5 | ForEach-Object { Write-Host "        missing: $_" -ForegroundColor Yellow } }

        if ($extra.Count) {
            Write-Host "  NOTE  $($pair.Label): $($extra.Count) file(s) on disk with no DB reference (orphans)" -ForegroundColor Yellow
            $extra | Select-Object -First 5 | ForEach-Object { Write-Host "        orphan: $_" -ForegroundColor Yellow }
        }
    }
} else {
    Write-Host "  (skipped disk comparison -- pass -StagingRoot to run it)" -ForegroundColor DarkGray
}

$conn.Close()
Write-Host ""
if ($fail -eq 0) { Write-Host "ALL CHECKS PASSED" -ForegroundColor Green; exit 0 }
else { Write-Host "$fail CHECK(S) FAILED" -ForegroundColor Red; exit 1 }
