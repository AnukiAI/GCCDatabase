# Simple Database Migration Script
# Exports from Dev and Imports to Production using SQL Server Integration

$ErrorActionPreference = "Continue"

$sourceSer = "ANUKI01"
$sourceDB = "GCCDatabase_Dev"
$targetServer = "68.71.129.120,1533"
$targetDB = "gccwebap_certificates"
$targetUser = "GCC_adminxyz123"
$targetPass = "Q3Ez4e!gE_47KNi"

Write-Host "=== GCC Database Migration ===" -ForegroundColor Cyan
Write-Host "Copying data from $sourceDB to $targetDB" -ForegroundColor Yellow
Write-Host ""

# Tables to migrate in order
$tables = @(
    @{Name="Roles"; HasIdentity=$true},
    @{Name="Companies"; HasIdentity=$true},
    @{Name="Partners"; HasIdentity=$true},
    @{Name="CertificationTypes"; HasIdentity=$true},
    @{Name="Products"; HasIdentity=$true},
    @{Name="Users"; HasIdentity=$true},
    @{Name="Tags"; HasIdentity=$true},
    @{Name="Certificates"; HasIdentity=$true},
    @{Name="ApiKeys"; HasIdentity=$true}
)

foreach ($table in $tables) {
    $tableName = $table.Name
    Write-Host "Migrating $tableName..." -ForegroundColor Cyan

    # Get row count from source
    $countSql = "SELECT COUNT(*) FROM [$tableName]"
    try {
        $count = sqlcmd -S $sourceSer -E -d $sourceDB -Q $countSql -h -1 2>&1
        $count = $count.Trim()

        if ($count -eq "0" -or $count -match "Invalid") {
            Write-Host "  Skipping $tableName (empty or doesn't exist)" -ForegroundColor Gray
            continue
        }

        Write-Host "  Found $count rows" -ForegroundColor Green

        # Export to CSV
        $exportFile = ".\$tableName.csv"
        $exportSql = @"
SET NOCOUNT ON;
SELECT * FROM [$tableName]
"@

        # Export data
        $exportSql | sqlcmd -S $sourceSer -E -d $sourceDB -s"," -W -h-1 > $exportFile 2>&1

        if (!(Test-Path $exportFile)) {
            Write-Host "  ERROR: Failed to export" -ForegroundColor Red
            continue
        }

        # Read CSV and generate INSERT statements
        $insertFile = ".\$tableName-inserts.sql"
        $data = Import-Csv $exportFile

        if ($data.Count -eq 0) {
            Write-Host "  No data to import" -ForegroundColor Gray
            Remove-Item $exportFile -ErrorAction SilentlyContinue
            continue
        }

        # Generate INSERT statements
        $inserts = @()
        if ($table.HasIdentity) {
            $inserts += "SET IDENTITY_INSERT [$tableName] ON;"
        }

        $columns = ($data[0].PSObject.Properties | Select-Object -ExpandProperty Name)
        $columnList = ($columns | ForEach-Object { "[$_]" }) -join ", "

        foreach ($row in $data) {
            $values = @()
            foreach ($col in $columns) {
                $value = $row.$col
                if ($value -eq $null -or $value -eq "") {
                    $values += "NULL"
                } elseif ($value -match "^\d+$" -and $col -notmatch "Phone|Zip") {
                    $values += $value  # Numeric
                } elseif ($value -match "^\d{4}-\d{2}-\d{2}") {
                    $values += "'$value'"  # DateTime
                } elseif ($value -match "^(true|false|1|0)$" -and $col -match "Is|Enabled|Confirmed") {
                    $values += if ($value -eq "true" -or $value -eq "1") { "1" } else { "0" }  # Boolean
                } else {
                    $escapedValue = $value -replace "'", "''"
                    $values += "'$escapedValue'"
                }
            }
            $valueList = $values -join ", "
            $inserts += "INSERT INTO [$tableName] ($columnList) VALUES ($valueList);"
        }

        if ($table.HasIdentity) {
            $inserts += "SET IDENTITY_INSERT [$tableName] OFF;"
        }

        $inserts | Out-File $insertFile -Encoding UTF8

        Write-Host "  Generated $($data.Count) INSERT statements" -ForegroundColor Green
        Write-Host "  Importing to production..." -ForegroundColor Gray

        # Execute on production
        $importResult = sqlcmd -S $targetServer -U $targetUser -P $targetPass -d $targetDB -i $insertFile 2>&1

        if ($importResult -match "error") {
            Write-Host "  ERROR importing: $importResult" -ForegroundColor Red
        } else {
            # Verify
            $verifyCount = sqlcmd -S $targetServer -U $targetUser -P $targetPass -d $targetDB -Q $countSql -h -1 2>&1
            $verifyCount = $verifyCount.Trim()
            Write-Host "  SUCCESS: Production now has $verifyCount rows" -ForegroundColor Green
        }

        # Clean up
        Remove-Item $exportFile -ErrorAction SilentlyContinue
        # Keep insert file for review

    } catch {
        Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }

    Write-Host ""
}

Write-Host "=== Migration Summary ===" -ForegroundColor Cyan
Write-Host "Checking production counts..." -ForegroundColor Yellow

foreach ($checkTable in @("Roles", "Companies", "Products", "Certificates", "Users")) {
    $countSql = "SELECT COUNT(*) FROM [$checkTable]"
    $count = sqlcmd -S $targetServer -U $targetUser -P $targetPass -d $targetDB -Q $countSql -h -1 2>&1
    $count = $count.Trim()
    Write-Host "  $checkTable`: $count" -ForegroundColor Green
}

Write-Host ""
Write-Host "Migration completed!" -ForegroundColor Green
Write-Host "INSERT scripts saved in DataMigration folder for review" -ForegroundColor Yellow
