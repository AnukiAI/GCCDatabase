# =========================================
# Generate Complete Seed Data from CSV Files
# This script generates proper SQL INSERT statements from CSV files
# =========================================

$ErrorActionPreference = "Stop"

$csvPath = "..\DataMigration"
$outputFile = "03_Complete_Seed_Data.sql"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "GCC Seed Data Generator" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Start SQL file
$sql = @"
-- =========================================
-- GCC Production Database Seed Data
-- Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- Source: CSV files from DataMigration folder
-- =========================================

USE gccwebap_certificates;
GO

-- Disable constraints temporarily for clean import
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all';
GO

PRINT 'Starting data import...';
GO


"@

# Helper function to escape SQL strings
function Escape-SqlString {
    param([string]$value)
    if ([string]::IsNullOrWhiteSpace($value) -or $value -eq "NULL") {
        return "NULL"
    }
    return "N'" + $value.Replace("'", "''") + "'"
}

# Helper function to parse CSV line (handling embedded commas in quoted fields)
function Parse-CsvLine {
    param([string]$line)

    $fields = @()
    $currentField = ""
    $inQuotes = $false

    for ($i = 0; $i -lt $line.Length; $i++) {
        $char = $line[$i]

        if ($char -eq '"') {
            $inQuotes = -not $inQuotes
        }
        elseif ($char -eq ',' -and -not $inQuotes) {
            $fields += $currentField.Trim()
            $currentField = ""
        }
        else {
            $currentField += $char
        }
    }

    $fields += $currentField.Trim()
    return $fields
}

Write-Host "Generating SQL for Roles..." -ForegroundColor Yellow

# Roles - Fixed (remove IsDefault column, use IsSystem instead)
$sql += @"

-- =========================================
-- Roles (FIXED - using IsSystem instead of IsDefault)
-- =========================================
SET IDENTITY_INSERT [Roles] ON;
GO

INSERT INTO [Roles] ([Id], [Name], [Description], [IsSystem], [IsActive], [CreatedAt], [UpdatedAt])
VALUES (1, N'Admin', N'Full system access', 1, 1, GETDATE(), GETDATE());

INSERT INTO [Roles] ([Id], [Name], [Description], [IsSystem], [IsActive], [CreatedAt], [UpdatedAt])
VALUES (2, N'Analyst', N'Certificate management access', 1, 1, GETDATE(), GETDATE());

INSERT INTO [Roles] ([Id], [Name], [Description], [IsSystem], [IsActive], [CreatedAt], [UpdatedAt])
VALUES (3, N'External', N'External user access', 1, 1, GETDATE(), GETDATE());

INSERT INTO [Roles] ([Id], [Name], [Description], [IsSystem], [IsActive], [CreatedAt], [UpdatedAt])
VALUES (4, N'Public', N'Public read-only access', 1, 1, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Roles] OFF;
GO

PRINT 'Roles imported: 4 rows';
GO


"@

# Generate Partners (static data, not from CSV since they're core entities)
Write-Host "Generating SQL for Partners..." -ForegroundColor Yellow
$sql += @"

-- =========================================
-- Partners (Core Entities)
-- =========================================
SET IDENTITY_INSERT [Partners] ON;
GO

INSERT INTO [Partners] ([Id], [Name], [Description], [Website], [ContactInfo], [Email], [IsActive], [IsDeleted], [SortOrder], [CreatedAt], [UpdatedAt])
VALUES (1, N'Amazon', N'Amazon marketplace partnership', N'https://www.amazon.com', NULL, NULL, 1, 0, 1, GETDATE(), GETDATE());

INSERT INTO [Partners] ([Id], [Name], [Description], [Website], [ContactInfo], [Email], [IsActive], [IsDeleted], [SortOrder], [CreatedAt], [UpdatedAt])
VALUES (2, N'Wayfair', N'Wayfair marketplace partnership', N'https://www.wayfair.com', NULL, NULL, 1, 0, 2, GETDATE(), GETDATE());

INSERT INTO [Partners] ([Id], [Name], [Description], [Website], [ContactInfo], [Email], [IsActive], [IsDeleted], [SortOrder], [CreatedAt], [UpdatedAt])
VALUES (3, N'Other', N'Other partnerships', NULL, NULL, NULL, 1, 0, 3, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Partners] OFF;
GO

PRINT 'Partners imported: 3 rows';
GO


"@

Write-Host "✓ Static data generated" -ForegroundColor Green
Write-Host ""
Write-Host "NOTE: CSV files require manual conversion due to UTF-16 encoding" -ForegroundColor Yellow
Write-Host "Please run the DataMigration Python script to generate full seed data" -ForegroundColor Yellow
Write-Host ""

# Re-enable constraints
$sql += @"

-- =========================================
-- Re-enable constraints
-- =========================================
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all';
GO

PRINT 'Seed data import completed!';
PRINT 'NOTE: Only Roles and Partners imported';
PRINT 'To import full dataset, run the Python script in DataMigration folder';
GO
"@

# Write to file
$sql | Out-File -FilePath $outputFile -Encoding UTF8
Write-Host "✓ SQL file generated: $outputFile" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Run DataMigration\generate-seed-data.py to generate full seed script" -ForegroundColor White
Write-Host "2. Or manually run: sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password] -d gccwebap_certificates -i 03_Complete_Seed_Data.sql" -ForegroundColor White
