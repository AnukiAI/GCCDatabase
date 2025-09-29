# SQL Server Local Setup Script for GCC Database Solution
# This script sets up SQL Server for local production testing

param(
    [string]$SqlServerInstance = "localhost",
    [string]$DatabaseName = "GCCDatabase_Prod",
    [string]$BackupDatabaseName = "GCCDatabase_Test",
    [switch]$InstallSqlServer = $false,
    [switch]$CreateDatabase = $false,
    [switch]$SetupPermissions = $false,
    [switch]$RunAll = $false
)

# Color output functions
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }

Write-Info "========================================="
Write-Info "GCC Database SQL Server Setup Script"
Write-Info "========================================="

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Error "This script must be run as Administrator!"
    Write-Warning "Please run PowerShell as Administrator and try again."
    exit 1
}

# Function to install SQL Server Express
function Install-SqlServerExpress {
    Write-Info "`nChecking for SQL Server installation..."
    
    $sqlInstalled = Get-Service -Name "MSSQLSERVER" -ErrorAction SilentlyContinue
    
    if ($sqlInstalled) {
        Write-Success "SQL Server is already installed."
        return
    }
    
    Write-Info "Downloading SQL Server 2022 Express..."
    $url = "https://download.microsoft.com/download/5/1/4/5145fe04-4d30-4b85-b0d1-39533663a2f1/SQL2022-SSEI-Expr.exe"
    $output = "$env:TEMP\SQL2022-SSEI-Expr.exe"
    
    try {
        Invoke-WebRequest -Uri $url -OutFile $output
        Write-Success "Download complete."
        
        Write-Info "Installing SQL Server 2022 Express..."
        Start-Process -FilePath $output -ArgumentList "/ACTION=Install", "/QUIET", "/IACCEPTSQLSERVERLICENSETERMS", "/FEATURES=SQLEngine,LocalDB", "/INSTANCENAME=MSSQLSERVER" -Wait
        
        Write-Success "SQL Server 2022 Express installed successfully."
    }
    catch {
        Write-Error "Failed to install SQL Server: $_"
        exit 1
    }
}

# Function to create database
function Create-Database {
    Write-Info "`nCreating databases..."
    
    $sqlScript = @"
-- Check if production database exists
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '$DatabaseName')
BEGIN
    CREATE DATABASE [$DatabaseName]
    ON PRIMARY 
    (
        NAME = N'$DatabaseName',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\$DatabaseName.mdf',
        SIZE = 100MB,
        MAXSIZE = UNLIMITED,
        FILEGROWTH = 10MB
    )
    LOG ON 
    (
        NAME = N'${DatabaseName}_log',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\${DatabaseName}_log.ldf',
        SIZE = 50MB,
        MAXSIZE = 2048MB,
        FILEGROWTH = 10MB
    )
    PRINT 'Production database created: $DatabaseName'
END
ELSE
BEGIN
    PRINT 'Production database already exists: $DatabaseName'
END

-- Check if test database exists
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '$BackupDatabaseName')
BEGIN
    CREATE DATABASE [$BackupDatabaseName]
    ON PRIMARY 
    (
        NAME = N'$BackupDatabaseName',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\$BackupDatabaseName.mdf',
        SIZE = 100MB,
        MAXSIZE = UNLIMITED,
        FILEGROWTH = 10MB
    )
    LOG ON 
    (
        NAME = N'${BackupDatabaseName}_log',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\${BackupDatabaseName}_log.ldf',
        SIZE = 50MB,
        MAXSIZE = 2048MB,
        FILEGROWTH = 10MB
    )
    PRINT 'Test database created: $BackupDatabaseName'
END
ELSE
BEGIN
    PRINT 'Test database already exists: $BackupDatabaseName'
END

-- Set recovery model
ALTER DATABASE [$DatabaseName] SET RECOVERY FULL;
ALTER DATABASE [$BackupDatabaseName] SET RECOVERY SIMPLE;

-- Enable query store for performance monitoring
ALTER DATABASE [$DatabaseName] SET QUERY_STORE = ON;
ALTER DATABASE [$DatabaseName] SET QUERY_STORE (OPERATION_MODE = READ_WRITE);
"@

    try {
        Invoke-Sqlcmd -ServerInstance $SqlServerInstance -Query $sqlScript -QueryTimeout 120
        Write-Success "Databases created successfully."
    }
    catch {
        Write-Error "Failed to create databases: $_"
        exit 1
    }
}

# Function to setup permissions
function Setup-Permissions {
    Write-Info "`nSetting up database permissions..."
    
    $sqlScript = @"
USE [$DatabaseName];

-- Create application login if not exists
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'gcc_app_user')
BEGIN
    CREATE LOGIN [gcc_app_user] WITH PASSWORD = 'GCC_Pr0d_2025!@#';
END

-- Create database user
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'gcc_app_user')
BEGIN
    CREATE USER [gcc_app_user] FOR LOGIN [gcc_app_user];
END

-- Create roles
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'db_gcc_admin')
BEGIN
    CREATE ROLE [db_gcc_admin];
END

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'db_gcc_analyst')
BEGIN
    CREATE ROLE [db_gcc_analyst];
END

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'db_gcc_readonly')
BEGIN
    CREATE ROLE [db_gcc_readonly];
END

-- Grant permissions to roles
-- Admin role - full access
GRANT CONTROL ON DATABASE::[$DatabaseName] TO [db_gcc_admin];

-- Analyst role - read/write on main tables
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO [db_gcc_analyst];
GRANT EXECUTE ON SCHEMA::dbo TO [db_gcc_analyst];

-- Readonly role - select only
GRANT SELECT ON SCHEMA::dbo TO [db_gcc_readonly];

-- Add application user to admin role (for initial setup)
ALTER ROLE [db_gcc_admin] ADD MEMBER [gcc_app_user];

-- Create audit login
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'gcc_audit_user')
BEGIN
    CREATE LOGIN [gcc_audit_user] WITH PASSWORD = 'GCC_Aud1t_2025!@#';
    CREATE USER [gcc_audit_user] FOR LOGIN [gcc_audit_user];
    ALTER ROLE [db_gcc_readonly] ADD MEMBER [gcc_audit_user];
END

-- Setup for test database
USE [$BackupDatabaseName];

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'gcc_app_user')
BEGIN
    CREATE USER [gcc_app_user] FOR LOGIN [gcc_app_user];
    GRANT CONTROL ON DATABASE::[$BackupDatabaseName] TO [gcc_app_user];
END

PRINT 'Permissions setup completed successfully';
"@

    try {
        Invoke-Sqlcmd -ServerInstance $SqlServerInstance -Query $sqlScript -QueryTimeout 120
        Write-Success "Permissions configured successfully."
        
        # Output connection strings
        Write-Info "`n========================================="
        Write-Info "Connection Strings for appsettings.json:"
        Write-Info "========================================="
        
        Write-Success "`nProduction Database:"
        Write-Host "Server=$SqlServerInstance;Database=$DatabaseName;User Id=gcc_app_user;Password=GCC_Pr0d_2025!@#;TrustServerCertificate=true;MultipleActiveResultSets=true" -ForegroundColor Yellow
        
        Write-Success "`nTest Database:"
        Write-Host "Server=$SqlServerInstance;Database=$BackupDatabaseName;User Id=gcc_app_user;Password=GCC_Pr0d_2025!@#;TrustServerCertificate=true;MultipleActiveResultSets=true" -ForegroundColor Yellow
        
        Write-Success "`nAudit User (Read-Only):"
        Write-Host "Server=$SqlServerInstance;Database=$DatabaseName;User Id=gcc_audit_user;Password=GCC_Aud1t_2025!@#;TrustServerCertificate=true;MultipleActiveResultSets=true" -ForegroundColor Yellow
        
    }
    catch {
        Write-Error "Failed to setup permissions: $_"
        exit 1
    }
}

# Function to test connection
function Test-DatabaseConnection {
    Write-Info "`nTesting database connections..."
    
    $connectionString = "Server=$SqlServerInstance;Database=$DatabaseName;User Id=gcc_app_user;Password=GCC_Pr0d_2025!@#;TrustServerCertificate=true"
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $connection.Open()
        Write-Success "Successfully connected to $DatabaseName"
        $connection.Close()
        
        # Test query
        $testQuery = "SELECT SUSER_NAME() as CurrentUser, DB_NAME() as CurrentDatabase"
        $result = Invoke-Sqlcmd -ServerInstance $SqlServerInstance -Database $DatabaseName -Username "gcc_app_user" -Password "GCC_Pr0d_2025!@#" -Query $testQuery -TrustServerCertificate
        
        Write-Success "Current User: $($result.CurrentUser)"
        Write-Success "Current Database: $($result.CurrentDatabase)"
    }
    catch {
        Write-Error "Connection test failed: $_"
    }
}

# Main execution
if ($RunAll -or $InstallSqlServer) {
    Install-SqlServerExpress
}

if ($RunAll -or $CreateDatabase) {
    Create-Database
}

if ($RunAll -or $SetupPermissions) {
    Setup-Permissions
}

# Always test connection at the end
Test-DatabaseConnection

Write-Info "`n========================================="
Write-Success "SQL Server setup completed!"
Write-Info "========================================="
Write-Info "`nNext steps:"
Write-Info "1. Update appsettings.json with the connection string above"
Write-Info "2. Run: dotnet ef database update"
Write-Info "3. Test the application with SQL Server"
Write-Info "`nSecurity Notes:"
Write-Warning "- Change the default passwords in production!"
Write-Warning "- Enable SQL Server auditing for compliance"
Write-Warning "- Configure backup strategy"
Write-Warning "- Enable Transparent Data Encryption (TDE) if required"