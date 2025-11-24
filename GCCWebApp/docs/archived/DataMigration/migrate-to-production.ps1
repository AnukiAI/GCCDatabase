# GCC Database Migration Script
# Migrates data from Development (ANUKI01) to Production (AccuWebHosting)

$ErrorActionPreference = "Stop"

# Source (Development)
$sourceServer = "ANUKI01"
$sourceDatabase = "GCCDatabase_Dev"
$sourceAuth = "-E"  # Windows Authentication

# Target (Production)
$targetServer = "68.71.129.120,1533"
$targetDatabase = "gccwebap_certificates"
$targetUser = "GCC_adminxyz123"
$targetPassword = "Q3Ez4e!gE_47KNi"
$targetAuth = "-U $targetUser -P $targetPassword"

Write-Host "=== GCC Database Migration ===" -ForegroundColor Cyan
Write-Host "Source: $sourceServer / $sourceDatabase" -ForegroundColor Yellow
Write-Host "Target: $targetServer / $targetDatabase" -ForegroundColor Yellow
Write-Host ""

# Tables in dependency order
$tables = @(
    "Roles",
    "Users",
    "UserRoles",
    "Companies",
    "Partners",
    "CertificationTypes",
    "Products",
    "Tags",
    "ApiKeys",
    "CustomFieldDefinitions",
    "Certificates",
    "CertificateProductIds",
    "CertificateTags",
    "CustomFieldValues",
    "CertificateDocuments",
    "AuditLogs"
)

$totalTables = $tables.Count
$currentTable = 0

foreach ($table in $tables) {
    $currentTable++
    Write-Host "[$currentTable/$totalTables] Migrating table: $table..." -ForegroundColor Cyan

    try {
        # Check if table exists in source
        $checkQuery = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '$table'"
        $result = sqlcmd -S $sourceServer $sourceAuth -d $sourceDatabase -Q $checkQuery -h -1

        if ($result -eq "0") {
            Write-Host "  - Table $table does not exist in source, skipping..." -ForegroundColor Yellow
            continue
        }

        # Get row count
        $countQuery = "SELECT COUNT(*) FROM [$table]"
        $rowCount = sqlcmd -S $sourceServer $sourceAuth -d $sourceDatabase -Q $countQuery -h -1
        $rowCount = $rowCount.Trim()

        if ($rowCount -eq "0") {
            Write-Host "  - Table $table is empty, skipping..." -ForegroundColor Gray
            continue
        }

        Write-Host "  - Found $rowCount rows" -ForegroundColor Green

        # Export to BCP file
        $bcpFile = ".\$table.dat"
        $formatFile = ".\$table.fmt"

        Write-Host "  - Exporting data..." -ForegroundColor Gray

        # BCP export with native format
        $bcpExport = "bcp [$sourceDatabase].dbo.[$table] out `"$bcpFile`" -S $sourceServer -T -n -q"
        Invoke-Expression $bcpExport | Out-Null

        if (-not (Test-Path $bcpFile)) {
            Write-Host "  - ERROR: Failed to export $table" -ForegroundColor Red
            continue
        }

        Write-Host "  - Importing to production..." -ForegroundColor Gray

        # Disable identity insert if table has identity column
        $identityCheck = @"
SELECT OBJECTPROPERTY(OBJECT_ID('$table'), 'TableHasIdentity')
"@
        $hasIdentity = sqlcmd -S $targetServer $targetAuth -d $targetDatabase -Q $identityCheck -h -1

        if ($hasIdentity.Trim() -eq "1") {
            sqlcmd -S $targetServer $targetAuth -d $targetDatabase -Q "SET IDENTITY_INSERT [$table] ON" -b | Out-Null
        }

        # BCP import
        $bcpImport = "bcp [$targetDatabase].dbo.[$table] in `"$bcpFile`" -S $targetServer -U $targetUser -P $targetPassword -n -q -b 1000"
        $importResult = Invoke-Expression $bcpImport 2>&1

        if ($hasIdentity.Trim() -eq "1") {
            sqlcmd -S $targetServer $targetAuth -d $targetDatabase -Q "SET IDENTITY_INSERT [$table] OFF" -b | Out-Null
        }

        # Verify import
        $verifyCount = sqlcmd -S $targetServer $targetAuth -d $targetDatabase -Q "SELECT COUNT(*) FROM [$table]" -h -1
        $verifyCount = $verifyCount.Trim()

        if ($verifyCount -eq $rowCount) {
            Write-Host "  - SUCCESS: Imported $verifyCount rows" -ForegroundColor Green
        } else {
            Write-Host "  - WARNING: Expected $rowCount rows, found $verifyCount" -ForegroundColor Yellow
        }

        # Clean up temp files
        Remove-Item $bcpFile -ErrorAction SilentlyContinue
        Remove-Item $formatFile -ErrorAction SilentlyContinue

    } catch {
        Write-Host "  - ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }

    Write-Host ""
}

Write-Host "=== Migration Summary ===" -ForegroundColor Cyan
Write-Host ""

# Get final counts
Write-Host "Production Database Row Counts:" -ForegroundColor Yellow
foreach ($table in @("Certificates", "Companies", "Products", "Users", "Roles")) {
    try {
        $count = sqlcmd -S $targetServer $targetAuth -d $targetDatabase -Q "SELECT COUNT(*) FROM [$table]" -h -1
        $count = $count.Trim()
        Write-Host "  $table`: $count" -ForegroundColor Green
    } catch {
        Write-Host "  $table`: ERROR" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Migration completed!" -ForegroundColor Green
