<#
.SYNOPSIS
  CR-037 follow-up: split compound Country strings into one CertificateCountries row per country.

.DESCRIPTION
  The CR-037 migration backfills CertificateCountries by copying Certificate.Country verbatim:

      INSERT INTO CertificateCountries (CertificateId, Country)
      SELECT c.Id, LTRIM(RTRIM(c.Country)) FROM Certificates c WHERE ...

  That is correct for the ~1,310 certificates holding a single country. It is wrong for the 15 that
  hold several in one string - "United States; China; Poland", "United States, Canada" - which land
  as ONE row whose "country" is the whole string. Those values are not in CountryList, so they never
  match a filter, a picker or a search, and they show up as a bogus country of their own.

  This script finds those rows, inserts the individual countries, and removes the compound row it
  replaced. It only ever touches rows whose Country contains ';' or ','.

  SEPARATORS: splits on both ';' and ','. Verified 2026-09-01 that no CountryList entry contains a
  comma, so splitting on it cannot shred a legitimate name.

  Certificate.Country IS ALSO SPLIT (Shawn, 2026-09-01: "instead of compounded, it would be
  United States and Canada"). The compound string is replaced with the FIRST country listed, which
  becomes the denormalized *primary* - every country, the primary included, is still held as its own
  CertificateCountries row, so nothing is lost. "United States, Canada" therefore becomes primary
  "United States" plus rows for United States and Canada.

  !! ONE VISIBLE CONSEQUENCE, stated because it reaches the client: Certificate.Country is what the
  CSV export writes into its "Country" column (CertificateService.cs:1328) and the XLSX export
  (:1576), and it is the sort key. After this runs, those exports show "United States" for a
  certificate that used to read "United States, Canada". If the exports are meant to list the full
  set, they have to read CertificateCountries instead - that is CR-037 code work, not data ops.

.PARAMETER Target
  REQUIRED. 'test' (gccwebapp.com, connection read from appsettings.Production.json) or 'dev'
  (GCCDatabase_Dev). There is no default: two scripts in this folder silently defaulted to dev and
  produced findings about the wrong environment.

.PARAMETER Apply
  Without it the script is a DRY RUN: it reports every row it would write and delete, and changes
  nothing. With it, the changes are made inside a single transaction.

.NOTES
  Covered by CR-037. Run AFTER the CR-037 migration has been applied - it aborts if
  CertificateCountries does not exist.
  Idempotent: every insert is NOT EXISTS-guarded against the unique (CertificateId, Country) index,
  so a second run reports 0 to do.

.EXAMPLE
  .\2026-09-01-split-compound-certificate-countries.ps1 -Target test
  .\2026-09-01-split-compound-certificate-countries.ps1 -Target test -Apply
#>
[CmdletBinding()]
param(
    [ValidateSet('test', 'dev')]
    [string]$Target,

    # Escape hatch for validating against a restored copy of the target, which is what the
    # pre-deploy checklist's Gate 7 asks for. Mutually exclusive with -Target.
    [string]$ConnectionString,

    [switch]$Apply
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------- connection --
function Get-TargetConnectionString {
    param([string]$Which)

    if ($Which -eq 'dev') {
        return "Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false"
    }

    # appsettings.Production.json is JSONC (it carries // comments), which PowerShell 5.1's
    # ConvertFrom-Json rejects. Pull the one value we need with a regex instead of stripping
    # comments - naive stripping also mangles the "https://" inside AllowedOrigins.
    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $cfgPath = Join-Path $repoRoot 'appsettings.Production.json'
    if (-not (Test-Path $cfgPath)) { throw "cannot find $cfgPath" }
    $cfg = Get-Content $cfgPath -Raw
    if ($cfg -notmatch '"DefaultConnection"\s*:\s*"([^"]+)"') { throw "no DefaultConnection in $cfgPath" }
    return $Matches[1]
}

if ($Target -and $ConnectionString) { throw "give -Target or -ConnectionString, not both" }
if (-not $Target -and -not $ConnectionString) { throw "-Target (test|dev) is required, or pass -ConnectionString for a restored copy" }

$connectionString = if ($ConnectionString) { $ConnectionString } else { Get-TargetConnectionString -Which $Target }
$server = if ($connectionString -match 'Server=([^;]+)') { $Matches[1] } else { '?' }
$database = if ($connectionString -match 'Database=([^;]+)') { $Matches[1] } else { '?' }

Write-Host ""
Write-Host "CR-037 - split compound certificate countries"
$targetLabel = if ($Target) { $Target.ToUpper() } else { 'EXPLICIT CONNECTION' }
Write-Host ("  target   : {0}  ->  {1} / {2}" -f $targetLabel, $server, $database)
Write-Host ("  mode     : {0}" -f $(if ($Apply) { "APPLY - changes WILL be written" } else { "DRY RUN - nothing will be changed" }))
Write-Host ""

$conn = New-Object System.Data.SqlClient.SqlConnection $connectionString
$conn.Open()

function Invoke-Query {
    param([string]$Sql, [System.Data.SqlClient.SqlTransaction]$Transaction = $null)
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = 300
    if ($Transaction) { $cmd.Transaction = $Transaction }
    $da = New-Object System.Data.SqlClient.SqlDataAdapter $cmd
    $dt = New-Object System.Data.DataTable
    [void]$da.Fill($dt)
    return , $dt
}

try {
    # ------------------------------------------------------------- preflight --
    $pre = Invoke-Query "SELECT CASE WHEN OBJECT_ID('CertificateCountries') IS NULL THEN 0 ELSE 1 END AS TableExists"
    if ([int]$pre.Rows[0].TableExists -ne 1) {
        Write-Host "ABORT: CertificateCountries does not exist on this database." -ForegroundColor Red
        Write-Host "       Apply the CR-037 migration first (20260901164220_CR037_MultiCountryCertificates)."
        exit 2
    }

    # ------------------------------------------------------- what needs doing --
    # Enumerate the affected certificates in full. A count alone hides which rows moved, and this
    # writes to certification records - the log of what was done has to name every one of them.
    $compound = Invoke-Query @"
SELECT c.Id, c.Number, c.Status, c.Country
FROM Certificates c
WHERE c.Country IS NOT NULL
  AND (c.Country LIKE '%;%' OR c.Country LIKE '%,%')
ORDER BY c.Number;
"@

    if ($compound.Rows.Count -eq 0) {
        Write-Host "Nothing to do: no certificate holds more than one country in its Country field."
        exit 0
    }

    $plan = @()      # one entry per (certificate, individual country)
    $primaries = @()  # one entry per certificate: the compound string -> the primary that replaces it
    foreach ($row in $compound.Rows) {
        $parts = @($row.Country -split '[;,]' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        # The FIRST listed country becomes the primary. It is also still one of the rows below, so
        # the join table remains the complete set and the primary is never a value it lacks.
        $primaries += [pscustomobject]@{
            CertificateId = [int]$row.Id
            Number        = [string]$row.Number
            Was           = [string]$row.Country
            Becomes       = $parts[0]
        }
        foreach ($p in $parts) {
            $plan += [pscustomobject]@{
                CertificateId = [int]$row.Id
                Number        = [string]$row.Number
                Status        = [string]$row.Status
                Source        = [string]$row.Country
                Country       = $p
            }
        }
    }

    Write-Host ("Certificates holding several countries in one string: {0}" -f $compound.Rows.Count)
    Write-Host ("Individual country rows they should become          : {0}" -f $plan.Count)
    Write-Host ""
    Write-Host "  Certificate  Status                  Stored value                  ->  split into"
    Write-Host "  -----------  ----------------------  ----------------------------      ----------"
    foreach ($row in $compound.Rows) {
        $parts = ($plan | Where-Object { $_.CertificateId -eq [int]$row.Id }).Country
        Write-Host ("  {0,-11}  {1,-22}  {2,-28}  ->  {3}" -f $row.Number, $row.Status, $row.Country, ($parts -join ' | '))
    }

    # Which of those rows already exist, and which compound rows are still sitting there?
    $planTable = ($plan | ForEach-Object { "($($_.CertificateId), N'$($_.Country.Replace("'","''"))')" }) -join ", "
    $status = Invoke-Query @"
WITH wanted(CertificateId, Country) AS (SELECT * FROM (VALUES $planTable) v(CertificateId, Country))
SELECT
  (SELECT COUNT(*) FROM wanted w
     WHERE NOT EXISTS (SELECT 1 FROM CertificateCountries cc
                       WHERE cc.CertificateId = w.CertificateId AND cc.Country = w.Country)) AS RowsToInsert,
  (SELECT COUNT(*) FROM CertificateCountries cc
     WHERE cc.Country LIKE '%;%' OR cc.Country LIKE '%,%')                                    AS CompoundRowsToRemove;
"@
    $toInsert = [int]$status.Rows[0].RowsToInsert
    $toRemove = [int]$status.Rows[0].CompoundRowsToRemove

    Write-Host ""
    Write-Host "  Certificate.Country (the primary, and what the CSV/XLSX exports show):"
    foreach ($pr in $primaries) {
        Write-Host ("    {0,-11}  '{1}'  ->  '{2}'" -f $pr.Number, $pr.Was, $pr.Becomes)
    }

    $primaryPending = ($primaries | Where-Object { $_.Was -ne $_.Becomes }).Count
    Write-Host ""
    Write-Host ("  rows to INSERT : {0}" -f $toInsert)
    Write-Host ("  compound rows to DELETE from CertificateCountries : {0}" -f $toRemove)
    Write-Host ("  Certificate.Country values to rewrite             : {0}" -f $primaryPending)

    if ($toInsert -eq 0 -and $toRemove -eq 0 -and $primaryPending -eq 0) {
        Write-Host ""
        Write-Host "Already done - nothing to change. (This script is idempotent; a second run is a no-op.)"
        exit 0
    }

    if (-not $Apply) {
        Write-Host ""
        Write-Host "DRY RUN - nothing was changed. Re-run with -Apply to write these changes." -ForegroundColor Yellow
        exit 0
    }

    # ------------------------------------------------------------- apply -----
    # One transaction: the inserts and the removal of the compound rows they replace must not be
    # able to half-happen. A partial run would leave a certificate with neither its split rows nor
    # its original.
    $tx = $conn.BeginTransaction()
    try {
        $inserted = 0
        foreach ($item in $plan) {
            $country = $item.Country.Replace("'", "''")
            $sql = @"
INSERT INTO CertificateCountries (CertificateId, Country)
SELECT $($item.CertificateId), N'$country'
WHERE NOT EXISTS (
    SELECT 1 FROM CertificateCountries
    WHERE CertificateId = $($item.CertificateId) AND Country = N'$country');
SELECT @@ROWCOUNT AS Affected;
"@
            $r = Invoke-Query -Sql $sql -Transaction $tx
            $inserted += [int]$r.Rows[0].Affected
        }

        # Only remove a compound row once its parts are present. The EXISTS check makes that
        # explicit rather than trusting the loop above to have succeeded.
        $del = Invoke-Query -Transaction $tx -Sql @"
DELETE cc
OUTPUT deleted.CertificateId, deleted.Country
FROM CertificateCountries cc
WHERE (cc.Country LIKE '%;%' OR cc.Country LIKE '%,%')
  AND EXISTS (SELECT 1 FROM CertificateCountries other
              WHERE other.CertificateId = cc.CertificateId
                AND other.Country <> cc.Country);
"@
        # Rewrite the denormalized primary LAST and in the same transaction: the join rows must
        # already exist, or a failure here would leave a certificate whose primary is the only
        # record of the countries it used to name.
        $primaryUpdated = 0
        foreach ($pr in $primaries) {
            if ($pr.Was -eq $pr.Becomes) { continue }
            $becomes = $pr.Becomes.Replace("'", "''")
            $r = Invoke-Query -Transaction $tx -Sql @"
UPDATE Certificates SET Country = N'$becomes' WHERE Id = $($pr.CertificateId);
SELECT @@ROWCOUNT AS Affected;
"@
            $primaryUpdated += [int]$r.Rows[0].Affected
        }

        $tx.Commit()

        Write-Host ""
        Write-Host ("APPLIED: {0} row(s) inserted, {1} compound row(s) removed, {2} primary Country value(s) rewritten." -f $inserted, $del.Rows.Count, $primaryUpdated) -ForegroundColor Green
        if ($del.Rows.Count -gt 0) {
            Write-Host "  removed:"
            foreach ($d in $del.Rows) { Write-Host ("    certificate id {0}  '{1}'" -f $d.CertificateId, $d.Country) }
        }
    }
    catch {
        $tx.Rollback()
        Write-Host ""
        Write-Host ("FAILED - transaction rolled back, nothing was changed: {0}" -f $_.Exception.Message) -ForegroundColor Red
        exit 1
    }

    # ------------------------------------------------------------- verify ----
    $verify = Invoke-Query @"
WITH wanted(CertificateId, Country) AS (SELECT * FROM (VALUES $planTable) v(CertificateId, Country))
SELECT
  (SELECT COUNT(*) FROM wanted w
     WHERE NOT EXISTS (SELECT 1 FROM CertificateCountries cc
                       WHERE cc.CertificateId = w.CertificateId AND cc.Country = w.Country)) AS StillMissing,
  (SELECT COUNT(*) FROM CertificateCountries WHERE Country LIKE '%;%' OR Country LIKE '%,%')  AS CompoundRemaining,
  (SELECT COUNT(*) FROM CertificateCountries)                                                 AS TotalRows,
  (SELECT COUNT(*) FROM Certificates WHERE Country LIKE '%;%' OR Country LIKE '%,%')          AS CompoundPrimariesRemaining;
"@
    $missing = [int]$verify.Rows[0].StillMissing
    $remaining = [int]$verify.Rows[0].CompoundRemaining
    $primaryRemaining = [int]$verify.Rows[0].CompoundPrimariesRemaining

    Write-Host ""
    Write-Host "=== VERIFY ==="
    Write-Host ("  expected rows still missing : {0}   (must be 0)" -f $missing)
    Write-Host ("  compound rows remaining     : {0}   (must be 0)" -f $remaining)
    Write-Host ("  compound Certificate.Country : {0}   (must be 0)" -f $primaryRemaining)
    Write-Host ("  CertificateCountries total   : {0}" -f $verify.Rows[0].TotalRows)

    if ($missing -eq 0 -and $remaining -eq 0 -and $primaryRemaining -eq 0) {
        Write-Host ""
        Write-Host "GREEN - every compound value is now one row per country, and no certificate" -ForegroundColor Green
        Write-Host "still holds several countries in one string." -ForegroundColor Green
        Write-Host ""
        Write-Host "Reminder: the CSV and XLSX exports read Certificate.Country, so those certificates"
        Write-Host "now export the primary only. Listing the full set there is CR-037 code work."
        exit 0
    }

    Write-Host ""
    Write-Host "RED - the verification did not come back clean. Investigate before re-running." -ForegroundColor Red
    exit 1
}
finally {
    $conn.Close()
}
