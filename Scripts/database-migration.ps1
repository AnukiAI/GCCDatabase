# Database Migration Script for GCC Web Application
# Handles both SQL Server and SQLite migrations

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet("SqlServer", "Sqlite")]
    [string]$Provider,
    
    [string]$ConnectionString,
    [switch]$CreateBackup = $true,
    [switch]$GenerateSql = $false,
    [switch]$Force = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Migration Script" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Provider: $Provider" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan

# Set working directory
Set-Location -Path "..\GCCWebApp"

# Step 1: Install EF Core tools
Write-Host "`nStep 1: Checking EF Core tools..." -ForegroundColor Green

$efVersion = dotnet ef --version 2>$null
if (-not $efVersion) {
    Write-Host "Installing EF Core tools..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
} else {
    Write-Host "EF Core tools version: $efVersion" -ForegroundColor Green
}

# Step 2: Load connection string
Write-Host "`nStep 2: Loading connection string..." -ForegroundColor Green

if (-not $ConnectionString) {
    # Load from appsettings
    $settingsFile = "appsettings.$Environment.json"
    if (-not (Test-Path $settingsFile)) {
        $settingsFile = "appsettings.json"
    }
    
    Write-Host "Loading connection string from $settingsFile" -ForegroundColor Yellow
    $settings = Get-Content $settingsFile | ConvertFrom-Json
    
    if ($Provider -eq "SqlServer") {
        $ConnectionString = $settings.ConnectionStrings.DefaultConnection
    } else {
        $ConnectionString = $settings.ConnectionStrings.SqliteConnection
    }
}

if (-not $ConnectionString) {
    Write-Error "Connection string not found. Please provide it as a parameter."
    exit 1
}

Write-Host "Connection string loaded." -ForegroundColor Green

# Step 3: Create backup if enabled
if ($CreateBackup) {
    Write-Host "`nStep 3: Creating database backup..." -ForegroundColor Green
    
    $backupPath = ".\Backups"
    if (-not (Test-Path $backupPath)) {
        New-Item -ItemType Directory -Path $backupPath | Out-Null
    }
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    
    if ($Provider -eq "Sqlite") {
        # For SQLite, copy the database file
        $dbFile = "gcc_database.db"
        if ($Environment -ne "Production") {
            $dbFile = "gcc_database_$($Environment.ToLower()).db"
        }
        
        if (Test-Path $dbFile) {
            $backupFile = "$backupPath\$dbFile.$timestamp.backup"
            Copy-Item -Path $dbFile -Destination $backupFile
            Write-Host "SQLite backup created: $backupFile" -ForegroundColor Green
        } else {
            Write-Warning "Database file not found. Skipping backup."
        }
    } else {
        # For SQL Server, use BACKUP DATABASE command
        Write-Host "Creating SQL Server backup..." -ForegroundColor Yellow
        
        $backupScript = @"
DECLARE @BackupPath NVARCHAR(500)
SET @BackupPath = N'C:\Backups\GCCDatabase_$Environment_$timestamp.bak'
BACKUP DATABASE [GCCDatabase] 
TO DISK = @BackupPath
WITH FORMAT, INIT, 
NAME = N'GCCDatabase-Full Database Backup', 
SKIP, NOREWIND, NOUNLOAD, STATS = 10
"@
        
        # Note: This would need to be executed through SQL connection
        Write-Host "Backup script generated. Execute manually if needed." -ForegroundColor Yellow
    }
} else {
    Write-Host "`nStep 3: Skipping backup (disabled)..." -ForegroundColor Yellow
}

# Step 4: Check for pending migrations
Write-Host "`nStep 4: Checking for pending migrations..." -ForegroundColor Green

$pendingMigrations = dotnet ef migrations list --connection "$ConnectionString" --json | ConvertFrom-Json
$pending = $pendingMigrations | Where-Object { $_.applied -eq $false }

if ($pending.Count -eq 0) {
    Write-Host "No pending migrations found." -ForegroundColor Green
    
    if (-not $Force) {
        Write-Host "Database is up to date. Exiting." -ForegroundColor Green
        exit 0
    }
} else {
    Write-Host "Found $($pending.Count) pending migration(s):" -ForegroundColor Yellow
    $pending | ForEach-Object { Write-Host "  - $($_.id)" -ForegroundColor Yellow }
}

# Step 5: Generate SQL script if requested
if ($GenerateSql) {
    Write-Host "`nStep 5: Generating SQL script..." -ForegroundColor Green
    
    $sqlPath = ".\Migrations\Scripts"
    if (-not (Test-Path $sqlPath)) {
        New-Item -ItemType Directory -Path $sqlPath | Out-Null
    }
    
    $scriptFile = "$sqlPath\Migration_$Environment_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
    
    dotnet ef migrations script --connection "$ConnectionString" --output $scriptFile --idempotent
    
    if (Test-Path $scriptFile) {
        Write-Host "SQL script generated: $scriptFile" -ForegroundColor Green
        Write-Host "Review the script before applying manually." -ForegroundColor Yellow
    } else {
        Write-Warning "Failed to generate SQL script."
    }
} else {
    Write-Host "`nStep 5: Skipping SQL script generation..." -ForegroundColor Yellow
}

# Step 6: Apply migrations
Write-Host "`nStep 6: Applying migrations..." -ForegroundColor Green

if (-not $Force) {
    Write-Host "The following migrations will be applied:" -ForegroundColor Yellow
    $pending | ForEach-Object { Write-Host "  - $($_.id)" -ForegroundColor Yellow }
    
    $confirm = Read-Host "`nDo you want to continue? (y/n)"
    if ($confirm -ne 'y') {
        Write-Host "Migration cancelled." -ForegroundColor Red
        exit 0
    }
}

Write-Host "Applying migrations..." -ForegroundColor Yellow

try {
    $env:ConnectionStrings__DefaultConnection = $ConnectionString
    dotnet ef database update --project .\GCCWebApp.csproj --verbose
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Migrations applied successfully!" -ForegroundColor Green
    } else {
        Write-Error "Migration failed with exit code: $LASTEXITCODE"
        exit 1
    }
} catch {
    Write-Error "Migration failed: $_"
    exit 1
}

# Step 7: Verify migration
Write-Host "`nStep 7: Verifying migration..." -ForegroundColor Green

$appliedMigrations = dotnet ef migrations list --connection "$ConnectionString" --json | ConvertFrom-Json
$stillPending = $appliedMigrations | Where-Object { $_.applied -eq $false }

if ($stillPending.Count -eq 0) {
    Write-Host "All migrations have been successfully applied." -ForegroundColor Green
} else {
    Write-Warning "Some migrations are still pending:"
    $stillPending | ForEach-Object { Write-Host "  - $($_.id)" -ForegroundColor Yellow }
}

# Step 8: Run data seeding (if applicable)
Write-Host "`nStep 8: Running data seeding..." -ForegroundColor Green

if ($Environment -eq "Development") {
    Write-Host "Seeding development data..." -ForegroundColor Yellow
    
    # This would typically call a seeding method in your application
    dotnet run --project .\GCCWebApp.csproj -- seed --environment $Environment
    
    Write-Host "Data seeding completed." -ForegroundColor Green
} else {
    Write-Host "Skipping data seeding for $Environment environment." -ForegroundColor Yellow
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Migration Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Green
Write-Host "Provider: $Provider" -ForegroundColor Green
Write-Host "Status: SUCCESS" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`nDatabase migration completed successfully!" -ForegroundColor Green