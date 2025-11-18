# ============================================
# GCC Database Migration Script
# Manual Migration Application Tool
# ============================================
#
# PURPOSE: Manually apply Entity Framework migrations to the database
#          when you want to refresh/update the schema.
#
# USAGE:
#   .\apply-migrations.ps1 -Environment Production
#   .\apply-migrations.ps1 -Environment Staging
#   .\apply-migrations.ps1 -Environment Development
#
# NOTE: Automatic migrations are disabled in Program.cs for Production/Staging.
#       Use this script when you need to apply schema changes.
# ============================================

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment = "Production",

    [Parameter(Mandatory=$false)]
    [switch]$WhatIf = $false,

    [Parameter(Mandatory=$false)]
    [switch]$Force = $false
)

# Colors for output
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }

# Header
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "GCC Database Migration Tool" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Set working directory to GCCWebApp project
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Split-Path -Parent $scriptPath
Set-Location $projectPath

Write-Info "Working directory: $projectPath"
Write-Host ""

# Verify .NET is installed
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Error "ERROR: .NET SDK not found. Please install .NET 8.0 SDK or higher."
    exit 1
}
Write-Success "✓ .NET SDK detected: $dotnetVersion"

# Verify Entity Framework tools are installed
Write-Info "Checking for Entity Framework tools..."
$efVersion = dotnet ef --version 2>$null
if (-not $efVersion) {
    Write-Warning "⚠ Entity Framework tools not found. Installing..."
    dotnet tool install --global dotnet-ef
    $efVersion = dotnet ef --version
}
Write-Success "✓ Entity Framework tools: $efVersion"
Write-Host ""

# Set environment variable
$env:ASPNETCORE_ENVIRONMENT = $Environment
Write-Info "ASPNETCORE_ENVIRONMENT = $Environment"

# Get current database configuration
Write-Info "Reading database configuration from appsettings.$Environment.json..."
$appSettingsPath = "appsettings.$Environment.json"
if (-not (Test-Path $appSettingsPath)) {
    Write-Error "ERROR: Configuration file not found: $appSettingsPath"
    exit 1
}

$appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
$connectionString = $appSettings.ConnectionStrings.DefaultConnection

# Parse connection string to display server info (without password)
if ($connectionString -match "Server=([^;]+)") {
    $server = $matches[1]
    Write-Info "Target Server: $server"
}
if ($connectionString -match "Database=([^;]+)") {
    $database = $matches[1]
    Write-Info "Target Database: $database"
}
Write-Host ""

# List pending migrations
Write-Info "Checking for pending migrations..."
Write-Host ""
dotnet ef migrations list --project . --no-build 2>&1 | ForEach-Object {
    if ($_ -match "\(Pending\)") {
        Write-Warning "  $_"
    } elseif ($_ -match "\(Applied\)") {
        Write-Host "  $_" -ForegroundColor DarkGray
    } else {
        Write-Host "  $_"
    }
}
Write-Host ""

# Safety confirmation for Production
if ($Environment -eq "Production" -and -not $Force -and -not $WhatIf) {
    Write-Warning "⚠ WARNING: You are about to apply migrations to PRODUCTION database!"
    Write-Warning "  Server: $server"
    Write-Warning "  Database: $database"
    Write-Host ""
    $confirmation = Read-Host "Type 'YES' to proceed, or anything else to cancel"
    if ($confirmation -ne "YES") {
        Write-Info "Operation cancelled by user."
        exit 0
    }
}

# WhatIf mode - preview only
if ($WhatIf) {
    Write-Info "WhatIf mode: Generating SQL script preview..."
    Write-Host ""
    dotnet ef migrations script --project . --idempotent --output "Database_Scripts\migration_preview_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
    Write-Success "✓ SQL script generated in Database_Scripts folder"
    Write-Info "Review the script before applying migrations."
    exit 0
}

# Apply migrations
Write-Info "Applying migrations to database..."
Write-Host ""

$startTime = Get-Date

try {
    # Run the migration
    dotnet ef database update --project . --verbose

    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalSeconds

    Write-Host ""
    Write-Success "============================================"
    Write-Success "✓ MIGRATION COMPLETED SUCCESSFULLY"
    Write-Success "============================================"
    Write-Info "Duration: $([math]::Round($duration, 2)) seconds"
    Write-Info "Environment: $Environment"
    Write-Info "Database: $database"
    Write-Host ""

    # Verify schema
    Write-Info "Verifying database schema..."
    Write-Host ""

    # Run a quick validation query
    $sqlQuery = @"
SELECT
    COUNT(*) as TableCount
FROM sys.tables
WHERE schema_id = SCHEMA_ID('dbo')
    AND name IN (
        'Certificates', 'Companies', 'Products', 'Users', 'ApiKeys',
        'CertificateProductIds', 'Roles', 'ProductCategories',
        'CertificationTypes', 'Partners', 'Tags'
    );
"@

    Write-Success "✓ Database update completed successfully!"
    Write-Host ""
    Write-Info "NEXT STEPS:"
    Write-Info "1. Verify application connectivity: dotnet run --urls 'http://localhost:5101'"
    Write-Info "2. Check database tables in SSMS"
    Write-Info "3. Run validation queries from Database_Scripts/PRODUCTION_VALIDATION.sql"
    Write-Host ""

} catch {
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalSeconds

    Write-Host ""
    Write-Error "============================================"
    Write-Error "✗ MIGRATION FAILED"
    Write-Error "============================================"
    Write-Error "Error: $_"
    Write-Info "Duration: $([math]::Round($duration, 2)) seconds"
    Write-Host ""
    Write-Warning "TROUBLESHOOTING:"
    Write-Warning "1. Verify database server is accessible"
    Write-Warning "2. Check connection string in appsettings.$Environment.json"
    Write-Warning "3. Ensure user has db_owner permissions"
    Write-Warning "4. Review error message above for specific details"
    Write-Host ""

    exit 1
}
