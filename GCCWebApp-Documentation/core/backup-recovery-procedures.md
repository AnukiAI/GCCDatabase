# Backup & Recovery Procedures - GCC Database Solution

## Overview
This document defines comprehensive backup and recovery procedures for the GCC Database Solution, ensuring data protection, business continuity, and compliance with regulatory requirements. It covers all system components including databases, application files, configuration, and user data.

## Backup Strategy

### Recovery Objectives

#### Recovery Time Objectives (RTO)
| Component | RTO Target | Business Impact |
|-----------|------------|-----------------|
| Database | 1 hour | Critical - Core business operations |
| Application | 30 minutes | High - User access and functionality |
| File Storage | 2 hours | Medium - Document and certificate files |
| Configuration | 15 minutes | High - System functionality |
| Complete System | 4 hours | Critical - Full business operations |

#### Recovery Point Objectives (RPO)
| Component | RPO Target | Data Loss Tolerance |
|-----------|------------|-------------------|
| Database | 15 minutes | Minimal - Transactional data |
| Application Files | 24 hours | Low - Code and static content |
| User Files | 1 hour | Low - Uploaded documents |
| Configuration | 24 hours | Medium - System settings |
| Audit Logs | 5 minutes | Minimal - Compliance requirement |

### Backup Types and Schedule

#### Full Backups
- **Frequency**: Weekly (Sunday 2:00 AM)
- **Retention**: 12 weeks (3 months)
- **Storage**: Primary and secondary locations
- **Validation**: Automated restore testing weekly

#### Differential Backups
- **Frequency**: Daily (2:00 AM, except Sunday)
- **Retention**: 4 weeks
- **Storage**: Primary location with cloud replication
- **Validation**: Weekly sample restore testing

#### Transaction Log Backups
- **Frequency**: Every 15 minutes
- **Retention**: 7 days
- **Storage**: High-speed storage with immediate cloud sync
- **Validation**: Continuous monitoring

#### Configuration Backups
- **Frequency**: Before any changes + Daily
- **Retention**: 90 days
- **Storage**: Version-controlled repository
- **Validation**: Automated configuration drift detection

## Database Backup Procedures

### SQL Server Backup Implementation

#### Automated Backup Scripts
```sql
-- Full database backup procedure
CREATE PROCEDURE [dbo].[sp_BackupDatabase_Full]
    @BackupPath NVARCHAR(500) = 'C:\Backups\',
    @DatabaseName NVARCHAR(128) = 'GCCDatabase',
    @RetentionDays INT = 84
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BackupFileName NVARCHAR(500);
    DECLARE @BackupFileFullPath NVARCHAR(500);
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @Timestamp NVARCHAR(20);

    -- Generate timestamp for backup file
    SET @Timestamp = FORMAT(GETDATE(), 'yyyyMMdd_HHmmss');
    SET @BackupFileName = @DatabaseName + '_Full_' + @Timestamp + '.bak';
    SET @BackupFileFullPath = @BackupPath + @BackupFileName;

    -- Perform backup
    SET @SQL = N'BACKUP DATABASE [' + @DatabaseName + ']
                 TO DISK = ''' + @BackupFileFullPath + '''
                 WITH COMPRESSION,
                      CHECKSUM,
                      VERIFY,
                      STATS = 10,
                      FORMAT,
                      DESCRIPTION = ''Full backup of ' + @DatabaseName + ' on ' + CONVERT(VARCHAR(20), GETDATE()) + '''';

    EXEC sp_executesql @SQL;

    -- Log backup completion
    INSERT INTO BackupLog (DatabaseName, BackupType, BackupPath, StartTime, EndTime, Status)
    VALUES (@DatabaseName, 'Full', @BackupFileFullPath, GETDATE(), GETDATE(), 'Success');

    -- Clean up old backups
    EXEC sp_CleanupOldBackups @BackupPath, @RetentionDays;

    PRINT 'Full backup completed: ' + @BackupFileFullPath;
END;

-- Differential backup procedure
CREATE PROCEDURE [dbo].[sp_BackupDatabase_Differential]
    @BackupPath NVARCHAR(500) = 'C:\Backups\',
    @DatabaseName NVARCHAR(128) = 'GCCDatabase'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BackupFileName NVARCHAR(500);
    DECLARE @BackupFileFullPath NVARCHAR(500);
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @Timestamp NVARCHAR(20);

    SET @Timestamp = FORMAT(GETDATE(), 'yyyyMMdd_HHmmss');
    SET @BackupFileName = @DatabaseName + '_Diff_' + @Timestamp + '.bak';
    SET @BackupFileFullPath = @BackupPath + @BackupFileName;

    SET @SQL = N'BACKUP DATABASE [' + @DatabaseName + ']
                 TO DISK = ''' + @BackupFileFullPath + '''
                 WITH DIFFERENTIAL,
                      COMPRESSION,
                      CHECKSUM,
                      VERIFY,
                      STATS = 10,
                      DESCRIPTION = ''Differential backup of ' + @DatabaseName + ' on ' + CONVERT(VARCHAR(20), GETDATE()) + '''';

    EXEC sp_executesql @SQL;

    INSERT INTO BackupLog (DatabaseName, BackupType, BackupPath, StartTime, EndTime, Status)
    VALUES (@DatabaseName, 'Differential', @BackupFileFullPath, GETDATE(), GETDATE(), 'Success');

    PRINT 'Differential backup completed: ' + @BackupFileFullPath;
END;

-- Transaction log backup procedure
CREATE PROCEDURE [dbo].[sp_BackupDatabase_Log]
    @BackupPath NVARCHAR(500) = 'C:\Backups\Logs\',
    @DatabaseName NVARCHAR(128) = 'GCCDatabase'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BackupFileName NVARCHAR(500);
    DECLARE @BackupFileFullPath NVARCHAR(500);
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @Timestamp NVARCHAR(20);

    SET @Timestamp = FORMAT(GETDATE(), 'yyyyMMdd_HHmmss');
    SET @BackupFileName = @DatabaseName + '_Log_' + @Timestamp + '.trn';
    SET @BackupFileFullPath = @BackupPath + @BackupFileName;

    SET @SQL = N'BACKUP LOG [' + @DatabaseName + ']
                 TO DISK = ''' + @BackupFileFullPath + '''
                 WITH COMPRESSION,
                      CHECKSUM,
                      VERIFY,
                      STATS = 10,
                      DESCRIPTION = ''Transaction log backup of ' + @DatabaseName + ' on ' + CONVERT(VARCHAR(20), GETDATE()) + '''';

    EXEC sp_executesql @SQL;

    INSERT INTO BackupLog (DatabaseName, BackupType, BackupPath, StartTime, EndTime, Status)
    VALUES (@DatabaseName, 'Log', @BackupFileFullPath, GETDATE(), GETDATE(), 'Success');
END;
```

#### Backup Monitoring Table
```sql
-- Create backup monitoring table
CREATE TABLE BackupLog (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DatabaseName NVARCHAR(128) NOT NULL,
    BackupType NVARCHAR(50) NOT NULL,
    BackupPath NVARCHAR(500) NOT NULL,
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NULL,
    Status NVARCHAR(50) NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    BackupSizeMB DECIMAL(10,2) NULL,
    CompressionRatio DECIMAL(5,2) NULL,
    CreatedDate DATETIME2 DEFAULT GETUTCDATE()
);

-- Backup verification procedure
CREATE PROCEDURE [dbo].[sp_VerifyBackups]
    @HoursBack INT = 24
AS
BEGIN
    SET NOCOUNT ON;

    -- Check for missing backups
    SELECT
        'Missing Full Backup' as Issue,
        'No full backup in last 7 days' as Description
    WHERE NOT EXISTS (
        SELECT 1 FROM BackupLog
        WHERE BackupType = 'Full'
        AND StartTime > DATEADD(day, -7, GETDATE())
        AND Status = 'Success'
    )

    UNION ALL

    SELECT
        'Missing Differential Backup',
        'No differential backup in last 24 hours'
    WHERE NOT EXISTS (
        SELECT 1 FROM BackupLog
        WHERE BackupType = 'Differential'
        AND StartTime > DATEADD(hour, -24, GETDATE())
        AND Status = 'Success'
    )

    UNION ALL

    SELECT
        'Missing Log Backup',
        'No log backup in last hour'
    WHERE NOT EXISTS (
        SELECT 1 FROM BackupLog
        WHERE BackupType = 'Log'
        AND StartTime > DATEADD(hour, -1, GETDATE())
        AND Status = 'Success'
    );

    -- Recent backup status
    SELECT TOP 20
        DatabaseName,
        BackupType,
        StartTime,
        Status,
        BackupSizeMB,
        DATEDIFF(minute, StartTime, EndTime) as DurationMinutes
    FROM BackupLog
    WHERE StartTime > DATEADD(hour, -@HoursBack, GETDATE())
    ORDER BY StartTime DESC;
END;
```

### PowerShell Backup Automation

#### Automated Backup Script
```powershell
# GCC Database Backup Automation Script
param(
    [string]$ServerInstance = "localhost",
    [string]$DatabaseName = "GCCDatabase",
    [string]$BackupPath = "C:\Backups\",
    [string]$LogPath = "C:\Logs\BackupScript.log",
    [string]$EmailRecipients = "admin@gcc.com",
    [switch]$FullBackup,
    [switch]$DifferentialBackup,
    [switch]$LogBackup
)

# Import required modules
Import-Module SqlServer -ErrorAction SilentlyContinue

# Logging function
function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogEntry = "[$Timestamp] [$Level] $Message"
    Write-Host $LogEntry
    Add-Content -Path $LogPath -Value $LogEntry
}

try {
    Write-Log "Starting backup process for database: $DatabaseName"

    # Ensure backup directory exists
    if (!(Test-Path $BackupPath)) {
        New-Item -ItemType Directory -Path $BackupPath -Force
        Write-Log "Created backup directory: $BackupPath"
    }

    # Full Backup
    if ($FullBackup) {
        Write-Log "Starting full backup"
        $Query = "EXEC sp_BackupDatabase_Full @BackupPath = '$BackupPath', @DatabaseName = '$DatabaseName'"
        Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $Query -QueryTimeout 3600
        Write-Log "Full backup completed successfully"
    }

    # Differential Backup
    if ($DifferentialBackup) {
        Write-Log "Starting differential backup"
        $Query = "EXEC sp_BackupDatabase_Differential @BackupPath = '$BackupPath', @DatabaseName = '$DatabaseName'"
        Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $Query -QueryTimeout 1800
        Write-Log "Differential backup completed successfully"
    }

    # Transaction Log Backup
    if ($LogBackup) {
        Write-Log "Starting transaction log backup"
        $Query = "EXEC sp_BackupDatabase_Log @BackupPath = '$($BackupPath)Logs\', @DatabaseName = '$DatabaseName'"
        Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $Query -QueryTimeout 300
        Write-Log "Transaction log backup completed successfully"
    }

    # Verify backups
    Write-Log "Verifying recent backups"
    $VerificationQuery = "EXEC sp_VerifyBackups @HoursBack = 2"
    $VerificationResult = Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $VerificationQuery

    if ($VerificationResult) {
        Write-Log "Backup verification issues found:" "WARNING"
        $VerificationResult | ForEach-Object { Write-Log "$($_.Issue): $($_.Description)" "WARNING" }
    } else {
        Write-Log "All backup verifications passed"
    }

    # Copy to secondary location (if configured)
    $SecondaryBackupPath = "\\backup-server\gcc-backups\"
    if (Test-Path $SecondaryBackupPath) {
        Write-Log "Copying backups to secondary location"
        robocopy $BackupPath $SecondaryBackupPath /MIR /R:3 /W:10 /LOG+:$LogPath
    }

    Write-Log "Backup process completed successfully"

} catch {
    $ErrorMessage = "Backup process failed: $($_.Exception.Message)"
    Write-Log $ErrorMessage "ERROR"

    # Send alert email
    $EmailBody = @"
GCC Database Backup Failure

Database: $DatabaseName
Server: $ServerInstance
Time: $(Get-Date)
Error: $ErrorMessage

Please investigate immediately.
"@

    Send-MailMessage -To $EmailRecipients -Subject "GCC Backup Failure" -Body $EmailBody -SmtpServer "localhost"

    throw
}
```

#### Windows Task Scheduler Configuration
```xml
<!-- Task Scheduler XML for automated backups -->
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
  <RegistrationInfo>
    <Date>2025-09-17T12:00:00</Date>
    <Author>GCC Administrator</Author>
    <Description>GCC Database Backup Automation</Description>
  </RegistrationInfo>
  <Triggers>
    <!-- Full backup - Weekly Sunday 2 AM -->
    <CalendarTrigger>
      <StartBoundary>2025-09-17T02:00:00</StartBoundary>
      <ScheduleByWeek>
        <DaysOfWeek>
          <Sunday />
        </DaysOfWeek>
        <WeeksInterval>1</WeeksInterval>
      </ScheduleByWeek>
    </CalendarTrigger>
    <!-- Differential backup - Daily 2 AM (except Sunday) -->
    <CalendarTrigger>
      <StartBoundary>2025-09-17T02:00:00</StartBoundary>
      <ScheduleByWeek>
        <DaysOfWeek>
          <Monday />
          <Tuesday />
          <Wednesday />
          <Thursday />
          <Friday />
          <Saturday />
        </DaysOfWeek>
        <WeeksInterval>1</WeeksInterval>
      </ScheduleByWeek>
    </CalendarTrigger>
    <!-- Transaction log backup - Every 15 minutes -->
    <TimeTrigger>
      <StartBoundary>2025-09-17T00:00:00</StartBoundary>
      <Repetition>
        <Interval>PT15M</Interval>
      </Repetition>
    </TimeTrigger>
  </Triggers>
  <Actions>
    <Exec>
      <Command>PowerShell.exe</Command>
      <Arguments>-File "C:\Scripts\GCC-Backup.ps1" -FullBackup</Arguments>
    </Exec>
  </Actions>
</Task>
```

## Application and File Backup

### Application Files Backup
```bash
#!/bin/bash
# GCC Application Files Backup Script

BACKUP_DATE=$(date +%Y-%m-%d_%H-%M-%S)
APP_PATH="/app/gcc-webapp"
BACKUP_BASE_PATH="/backups/application"
RETENTION_DAYS=30

# Create backup directory
BACKUP_PATH="$BACKUP_BASE_PATH/$BACKUP_DATE"
mkdir -p "$BACKUP_PATH"

echo "Starting application backup: $BACKUP_DATE"

# Backup application files
tar -czf "$BACKUP_PATH/gcc-webapp-$BACKUP_DATE.tar.gz" \
    --exclude='*.log' \
    --exclude='temp/*' \
    --exclude='cache/*' \
    -C "$(dirname $APP_PATH)" \
    "$(basename $APP_PATH)"

# Backup configuration files
mkdir -p "$BACKUP_PATH/config"
cp /app/appsettings.Production.json "$BACKUP_PATH/config/"
cp /app/web.config "$BACKUP_PATH/config/" 2>/dev/null || true
cp -r /etc/nginx/sites-available/gcc "$BACKUP_PATH/config/" 2>/dev/null || true

# Backup SSL certificates
mkdir -p "$BACKUP_PATH/ssl"
cp -r /etc/ssl/certs/gcc* "$BACKUP_PATH/ssl/" 2>/dev/null || true

# Create backup manifest
cat > "$BACKUP_PATH/manifest.txt" << EOF
GCC Application Backup Manifest
Created: $(date)
Backup ID: $BACKUP_DATE
Application Path: $APP_PATH
Configuration Files: Included
SSL Certificates: Included
Database: Separate backup required

Files Included:
$(find "$BACKUP_PATH" -type f -exec ls -lh {} \; | awk '{print $9 " - " $5}')
EOF

# Verify backup integrity
echo "Verifying backup integrity..."
if tar -tzf "$BACKUP_PATH/gcc-webapp-$BACKUP_DATE.tar.gz" > /dev/null; then
    echo "Backup verification successful"
else
    echo "ERROR: Backup verification failed"
    exit 1
fi

# Clean up old backups
echo "Cleaning up backups older than $RETENTION_DAYS days..."
find "$BACKUP_BASE_PATH" -type d -mtime +$RETENTION_DAYS -exec rm -rf {} \;

# Upload to cloud storage (if configured)
if [ -n "$AZURE_STORAGE_CONNECTION_STRING" ]; then
    echo "Uploading to Azure Blob Storage..."
    az storage blob upload-batch \
        --destination gcc-backups \
        --source "$BACKUP_PATH" \
        --destination-path "application/$BACKUP_DATE"
fi

echo "Application backup completed: $BACKUP_PATH"
```

### User Files and Documents Backup
```csharp
// File storage backup service
public class FileBackupService
{
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<FileBackupService> _logger;
    private readonly IConfiguration _configuration;

    public async Task<BackupResult> BackupUserFiles()
    {
        var backupId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting user files backup: {BackupId}", backupId);

            // Get all user files from the last 24 hours or all files for full backup
            var filesToBackup = await GetFilesToBackup();

            var backupPath = $"backups/userfiles/{DateTime.UtcNow:yyyy/MM/dd}/{backupId}";
            var backedUpFiles = new List<string>();

            foreach (var file in filesToBackup)
            {
                try
                {
                    var fileStream = await _blobStorage.GetFileStreamAsync(file.Path);
                    var backupFilePath = $"{backupPath}/{file.RelativePath}";

                    await _blobStorage.UploadFileAsync(backupFilePath, fileStream);
                    backedUpFiles.Add(file.Path);

                    _logger.LogDebug("Backed up file: {FilePath}", file.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to backup file: {FilePath}", file.Path);
                }
            }

            // Create backup manifest
            var manifest = new BackupManifest
            {
                BackupId = backupId,
                BackupType = "UserFiles",
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                TotalFiles = filesToBackup.Count,
                BackedUpFiles = backedUpFiles.Count,
                BackupPath = backupPath,
                Files = backedUpFiles
            };

            await SaveBackupManifest(manifest);

            _logger.LogInformation("User files backup completed: {BackupId}, Files: {FileCount}",
                backupId, backedUpFiles.Count);

            return new BackupResult
            {
                Success = true,
                BackupId = backupId,
                FilesBackedUp = backedUpFiles.Count,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User files backup failed: {BackupId}", backupId);

            return new BackupResult
            {
                Success = false,
                BackupId = backupId,
                Error = ex.Message,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    private async Task<List<FileInfo>> GetFilesToBackup()
    {
        // Implementation to get files that need backup
        // This could be all files or incremental based on last backup
        return await _blobStorage.ListFilesAsync("userfiles/");
    }

    private async Task SaveBackupManifest(BackupManifest manifest)
    {
        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var manifestPath = $"{manifest.BackupPath}/manifest.json";
        await _blobStorage.UploadTextAsync(manifestPath, manifestJson);
    }
}
```

## Recovery Procedures

### Database Recovery Procedures

#### Point-in-Time Recovery
```sql
-- Point-in-time recovery procedure
CREATE PROCEDURE [dbo].[sp_RestoreDatabase_PointInTime]
    @DatabaseName NVARCHAR(128),
    @RestoreToTime DATETIME2,
    @BackupBasePath NVARCHAR(500),
    @RestoreDatabaseName NVARCHAR(128) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @RestoreDatabaseName IS NULL
        SET @RestoreDatabaseName = @DatabaseName + '_Restore_' + FORMAT(GETDATE(), 'yyyyMMddHHmmss');

    DECLARE @FullBackupFile NVARCHAR(500);
    DECLARE @DiffBackupFile NVARCHAR(500);
    DECLARE @SQL NVARCHAR(MAX);

    -- Find the latest full backup before restore time
    SELECT TOP 1 @FullBackupFile = BackupPath
    FROM BackupLog
    WHERE DatabaseName = @DatabaseName
      AND BackupType = 'Full'
      AND StartTime <= @RestoreToTime
      AND Status = 'Success'
    ORDER BY StartTime DESC;

    IF @FullBackupFile IS NULL
    BEGIN
        RAISERROR('No suitable full backup found before restore time', 16, 1);
        RETURN;
    END

    -- Find the latest differential backup before restore time
    SELECT TOP 1 @DiffBackupFile = BackupPath
    FROM BackupLog
    WHERE DatabaseName = @DatabaseName
      AND BackupType = 'Differential'
      AND StartTime <= @RestoreToTime
      AND StartTime >= (SELECT StartTime FROM BackupLog WHERE BackupPath = @FullBackupFile)
      AND Status = 'Success'
    ORDER BY StartTime DESC;

    PRINT 'Starting point-in-time recovery...';
    PRINT 'Full backup: ' + @FullBackupFile;
    PRINT 'Differential backup: ' + ISNULL(@DiffBackupFile, 'None');
    PRINT 'Restore to time: ' + CONVERT(VARCHAR(20), @RestoreToTime);

    -- Restore full backup
    SET @SQL = N'RESTORE DATABASE [' + @RestoreDatabaseName + ']
                 FROM DISK = ''' + @FullBackupFile + '''
                 WITH NORECOVERY, REPLACE, VERIFY,
                 MOVE ''' + @DatabaseName + ''' TO ''C:\Data\' + @RestoreDatabaseName + '.mdf'',
                 MOVE ''' + @DatabaseName + '_Log'' TO ''C:\Data\' + @RestoreDatabaseName + '_Log.ldf''';

    EXEC sp_executesql @SQL;
    PRINT 'Full backup restored';

    -- Restore differential backup if available
    IF @DiffBackupFile IS NOT NULL
    BEGIN
        SET @SQL = N'RESTORE DATABASE [' + @RestoreDatabaseName + ']
                     FROM DISK = ''' + @DiffBackupFile + '''
                     WITH NORECOVERY, VERIFY';

        EXEC sp_executesql @SQL;
        PRINT 'Differential backup restored';
    END

    -- Restore transaction logs up to the specified time
    DECLARE log_cursor CURSOR FOR
        SELECT BackupPath
        FROM BackupLog
        WHERE DatabaseName = @DatabaseName
          AND BackupType = 'Log'
          AND StartTime <= @RestoreToTime
          AND StartTime >= ISNULL((SELECT StartTime FROM BackupLog WHERE BackupPath = @DiffBackupFile),
                                  (SELECT StartTime FROM BackupLog WHERE BackupPath = @FullBackupFile))
          AND Status = 'Success'
        ORDER BY StartTime;

    DECLARE @LogBackupFile NVARCHAR(500);
    OPEN log_cursor;
    FETCH NEXT FROM log_cursor INTO @LogBackupFile;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @SQL = N'RESTORE LOG [' + @RestoreDatabaseName + ']
                     FROM DISK = ''' + @LogBackupFile + '''
                     WITH NORECOVERY, VERIFY,
                     STOPAT = ''' + CONVERT(VARCHAR(20), @RestoreToTime, 120) + '''';

        EXEC sp_executesql @SQL;
        PRINT 'Log backup restored: ' + @LogBackupFile;

        FETCH NEXT FROM log_cursor INTO @LogBackupFile;
    END;

    CLOSE log_cursor;
    DEALLOCATE log_cursor;

    -- Bring database online
    SET @SQL = N'RESTORE DATABASE [' + @RestoreDatabaseName + '] WITH RECOVERY';
    EXEC sp_executesql @SQL;

    PRINT 'Point-in-time recovery completed successfully';
    PRINT 'Restored database name: ' + @RestoreDatabaseName;
END;
```

#### Database Corruption Recovery
```sql
-- Database corruption detection and recovery
CREATE PROCEDURE [dbo].[sp_HandleDatabaseCorruption]
    @DatabaseName NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CorruptionLevel INT = 0;
    DECLARE @ErrorMessage NVARCHAR(MAX);

    -- Check for corruption
    BEGIN TRY
        DBCC CHECKDB(@DatabaseName) WITH NO_INFOMSGS;
        PRINT 'Database integrity check passed';
        RETURN;
    END TRY
    BEGIN CATCH
        SET @ErrorMessage = ERROR_MESSAGE();
        SET @CorruptionLevel = 1;
        PRINT 'Database corruption detected: ' + @ErrorMessage;
    END CATCH;

    -- Attempt page-level repair
    IF @CorruptionLevel = 1
    BEGIN
        BEGIN TRY
            DBCC CHECKDB(@DatabaseName, REPAIR_ALLOW_DATA_LOSS) WITH NO_INFOMSGS;
            PRINT 'Page-level repair successful';
            SET @CorruptionLevel = 0;
        END TRY
        BEGIN CATCH
            SET @ErrorMessage = ERROR_MESSAGE();
            SET @CorruptionLevel = 2;
            PRINT 'Page-level repair failed: ' + @ErrorMessage;
        END CATCH;
    END

    -- If repair failed, restore from backup
    IF @CorruptionLevel = 2
    BEGIN
        PRINT 'Initiating database restore from latest backup';

        -- Get latest full backup
        DECLARE @LatestBackup NVARCHAR(500);
        SELECT TOP 1 @LatestBackup = BackupPath
        FROM BackupLog
        WHERE DatabaseName = @DatabaseName
          AND BackupType = 'Full'
          AND Status = 'Success'
        ORDER BY StartTime DESC;

        IF @LatestBackup IS NOT NULL
        BEGIN
            -- Execute restore procedure
            EXEC sp_RestoreDatabase_PointInTime
                @DatabaseName = @DatabaseName,
                @RestoreToTime = '9999-12-31 23:59:59',
                @BackupBasePath = '',
                @RestoreDatabaseName = @DatabaseName;
        END
        ELSE
        BEGIN
            RAISERROR('No backup available for restoration', 16, 1);
        END
    END
END;
```

### Application Recovery Procedures

#### Application Files Recovery
```bash
#!/bin/bash
# GCC Application Recovery Script

RESTORE_FROM_BACKUP="$1"
RESTORE_POINT="$2"
APP_PATH="/app/gcc-webapp"
BACKUP_BASE_PATH="/backups/application"

if [ -z "$RESTORE_FROM_BACKUP" ]; then
    echo "Usage: $0 <backup_date> [restore_point]"
    echo "Available backups:"
    ls -la "$BACKUP_BASE_PATH"
    exit 1
fi

BACKUP_PATH="$BACKUP_BASE_PATH/$RESTORE_FROM_BACKUP"

if [ ! -d "$BACKUP_PATH" ]; then
    echo "ERROR: Backup not found: $BACKUP_PATH"
    exit 1
fi

echo "Starting application recovery from backup: $RESTORE_FROM_BACKUP"

# Stop application services
echo "Stopping application services..."
sudo systemctl stop gcc-webapp
sudo systemctl stop nginx

# Backup current application (just in case)
CURRENT_BACKUP_PATH="/tmp/gcc-webapp-before-restore-$(date +%Y-%m-%d_%H-%M-%S)"
mkdir -p "$CURRENT_BACKUP_PATH"
cp -r "$APP_PATH" "$CURRENT_BACKUP_PATH/"
echo "Current application backed up to: $CURRENT_BACKUP_PATH"

# Restore application files
echo "Restoring application files..."
rm -rf "$APP_PATH"
mkdir -p "$APP_PATH"

tar -xzf "$BACKUP_PATH/gcc-webapp-$RESTORE_FROM_BACKUP.tar.gz" -C "$(dirname $APP_PATH)"

# Restore configuration files
echo "Restoring configuration files..."
cp "$BACKUP_PATH/config/appsettings.Production.json" "$APP_PATH/"
cp "$BACKUP_PATH/config/web.config" "$APP_PATH/" 2>/dev/null || true

# Restore nginx configuration
if [ -f "$BACKUP_PATH/config/gcc" ]; then
    sudo cp "$BACKUP_PATH/config/gcc" /etc/nginx/sites-available/
    sudo nginx -t
fi

# Restore SSL certificates
if [ -d "$BACKUP_PATH/ssl" ]; then
    echo "Restoring SSL certificates..."
    sudo cp -r "$BACKUP_PATH/ssl/"* /etc/ssl/certs/
fi

# Set proper permissions
echo "Setting permissions..."
sudo chown -R gcc-webapp:gcc-webapp "$APP_PATH"
sudo chmod +x "$APP_PATH/GCCWebApp"

# Verify application integrity
echo "Verifying application integrity..."
if [ -f "$APP_PATH/GCCWebApp" ]; then
    echo "Application files restored successfully"
else
    echo "ERROR: Application files not restored properly"
    exit 1
fi

# Start services
echo "Starting application services..."
sudo systemctl start gcc-webapp
sudo systemctl start nginx

# Wait for application to start
sleep 10

# Verify application is running
if curl -f http://localhost/health > /dev/null 2>&1; then
    echo "Application recovery completed successfully"
    echo "Application is responding at http://localhost/health"
else
    echo "WARNING: Application may not be responding properly"
    echo "Check logs: journalctl -u gcc-webapp -f"
fi

echo "Recovery completed from backup: $RESTORE_FROM_BACKUP"
```

### Docker Container Recovery
```bash
#!/bin/bash
# Docker Container Recovery Script

CONTAINER_NAME="gcc-webapp"
BACKUP_IMAGE_TAG="$1"
ROLLBACK_TAG="latest-stable"

if [ -z "$BACKUP_IMAGE_TAG" ]; then
    echo "Usage: $0 <backup_image_tag>"
    echo "Available image tags:"
    docker images gcc-webapp --format "table {{.Tag}}\t{{.CreatedAt}}"
    exit 1
fi

echo "Starting container recovery with image tag: $BACKUP_IMAGE_TAG"

# Stop current container
echo "Stopping current container..."
docker stop $CONTAINER_NAME

# Backup current container (create image from container)
BACKUP_CONTAINER_TAG="pre-recovery-$(date +%Y%m%d-%H%M%S)"
echo "Creating backup of current container state..."
docker commit $CONTAINER_NAME gcc-webapp:$BACKUP_CONTAINER_TAG

# Remove current container
echo "Removing current container..."
docker rm $CONTAINER_NAME

# Start new container with backup image
echo "Starting container with backup image..."
docker run -d \
    --name $CONTAINER_NAME \
    -p 80:8080 \
    -p 443:8443 \
    -v gcc-data:/app/data \
    -v gcc-logs:/app/logs \
    -e ASPNETCORE_ENVIRONMENT=Production \
    gcc-webapp:$BACKUP_IMAGE_TAG

# Wait for container to start
echo "Waiting for container to start..."
sleep 30

# Verify container health
if docker exec $CONTAINER_NAME curl -f http://localhost:8080/health > /dev/null 2>&1; then
    echo "Container recovery completed successfully"
    echo "Container is healthy and responding"

    # Tag as latest stable if recovery was successful
    docker tag gcc-webapp:$BACKUP_IMAGE_TAG gcc-webapp:$ROLLBACK_TAG
else
    echo "ERROR: Container is not responding properly"
    echo "Checking container logs..."
    docker logs $CONTAINER_NAME --tail 50

    echo "Attempting to start with previous stable version..."
    docker stop $CONTAINER_NAME
    docker rm $CONTAINER_NAME

    docker run -d \
        --name $CONTAINER_NAME \
        -p 80:8080 \
        -p 443:8443 \
        -v gcc-data:/app/data \
        -v gcc-logs:/app/logs \
        -e ASPNETCORE_ENVIRONMENT=Production \
        gcc-webapp:$ROLLBACK_TAG
fi

echo "Container recovery process completed"
```

## Backup Testing and Validation

### Automated Backup Testing
```csharp
public class BackupValidationService
{
    private readonly ILogger<BackupValidationService> _logger;
    private readonly IConfiguration _configuration;

    public async Task<ValidationResult> ValidateBackups()
    {
        var results = new List<ValidationTest>();

        // Test database backup integrity
        results.Add(await TestDatabaseBackupIntegrity());

        // Test application file backups
        results.Add(await TestApplicationBackups());

        // Test configuration backups
        results.Add(await TestConfigurationBackups());

        // Test backup accessibility
        results.Add(await TestBackupAccessibility());

        // Test recovery procedures
        results.Add(await TestRecoveryProcedures());

        var overallResult = new ValidationResult
        {
            TestDate = DateTime.UtcNow,
            Tests = results,
            Success = results.All(r => r.Success),
            Summary = GenerateValidationSummary(results)
        };

        _logger.LogInformation("Backup validation completed: {Success}, Tests: {TestCount}",
            overallResult.Success, results.Count);

        return overallResult;
    }

    private async Task<ValidationTest> TestDatabaseBackupIntegrity()
    {
        try
        {
            // Get latest backup
            var latestBackup = await GetLatestDatabaseBackup();

            if (latestBackup == null)
            {
                return new ValidationTest
                {
                    TestName = "Database Backup Integrity",
                    Success = false,
                    Message = "No recent database backup found"
                };
            }

            // Restore to test database
            var testDbName = $"GCCDatabase_Test_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            await RestoreDatabaseBackup(latestBackup, testDbName);

            // Run integrity check
            var integrityResult = await CheckDatabaseIntegrity(testDbName);

            // Cleanup test database
            await DropDatabase(testDbName);

            return new ValidationTest
            {
                TestName = "Database Backup Integrity",
                Success = integrityResult,
                Message = integrityResult ? "Database backup validation successful" : "Database backup integrity check failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database backup validation failed");

            return new ValidationTest
            {
                TestName = "Database Backup Integrity",
                Success = false,
                Message = $"Validation error: {ex.Message}"
            };
        }
    }

    private async Task<ValidationTest> TestRecoveryProcedures()
    {
        try
        {
            // Test point-in-time recovery
            var testTime = DateTime.UtcNow.AddHours(-1);
            var testDbName = $"GCCDatabase_Recovery_Test_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            var recoveryResult = await PerformTestRecovery(testTime, testDbName);

            // Cleanup test database
            await DropDatabase(testDbName);

            return new ValidationTest
            {
                TestName = "Recovery Procedures",
                Success = recoveryResult.Success,
                Message = recoveryResult.Message
            };
        }
        catch (Exception ex)
        {
            return new ValidationTest
            {
                TestName = "Recovery Procedures",
                Success = false,
                Message = $"Recovery test failed: {ex.Message}"
            };
        }
    }
}
```

### Backup Monitoring Dashboard
```html
<!DOCTYPE html>
<html>
<head>
    <title>GCC Backup Monitoring Dashboard</title>
    <style>
        .dashboard { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; }
        .widget { border: 1px solid #ddd; padding: 20px; border-radius: 5px; }
        .status-ok { background-color: #d4edda; border-color: #c3e6cb; }
        .status-warning { background-color: #fff3cd; border-color: #ffeaa7; }
        .status-error { background-color: #f8d7da; border-color: #f5c6cb; }
        .metric { font-size: 2em; font-weight: bold; }
    </style>
</head>
<body>
    <h1>GCC Backup Monitoring Dashboard</h1>

    <div class="dashboard">
        <div class="widget status-ok">
            <h3>Last Full Backup</h3>
            <div id="last-full-backup" class="metric">--</div>
            <div id="full-backup-status">Checking...</div>
        </div>

        <div class="widget status-ok">
            <h3>Last Differential Backup</h3>
            <div id="last-diff-backup" class="metric">--</div>
            <div id="diff-backup-status">Checking...</div>
        </div>

        <div class="widget status-ok">
            <h3>Last Log Backup</h3>
            <div id="last-log-backup" class="metric">--</div>
            <div id="log-backup-status">Checking...</div>
        </div>

        <div class="widget">
            <h3>Backup Size Trend</h3>
            <canvas id="backup-size-chart" width="300" height="200"></canvas>
        </div>

        <div class="widget">
            <h3>Recovery Testing</h3>
            <div id="last-recovery-test" class="metric">--</div>
            <div id="recovery-test-status">Checking...</div>
        </div>

        <div class="widget">
            <h3>Storage Usage</h3>
            <div id="storage-usage" class="metric">--</div>
            <div id="storage-status">Checking...</div>
        </div>
    </div>

    <script>
        async function updateBackupStatus() {
            try {
                const response = await fetch('/api/monitoring/backup-status');
                const data = await response.json();

                // Update backup information
                document.getElementById('last-full-backup').textContent =
                    new Date(data.lastFullBackup).toLocaleDateString();
                document.getElementById('last-diff-backup').textContent =
                    new Date(data.lastDifferentialBackup).toLocaleDateString();
                document.getElementById('last-log-backup').textContent =
                    new Date(data.lastLogBackup).toLocaleTimeString();

                // Update status indicators
                updateStatusWidget('full-backup-status', data.fullBackupStatus);
                updateStatusWidget('diff-backup-status', data.differentialBackupStatus);
                updateStatusWidget('log-backup-status', data.logBackupStatus);

                // Update recovery test info
                document.getElementById('last-recovery-test').textContent =
                    new Date(data.lastRecoveryTest).toLocaleDateString();
                document.getElementById('recovery-test-status').textContent =
                    data.recoveryTestStatus;

                // Update storage usage
                document.getElementById('storage-usage').textContent =
                    `${data.storageUsedGB} GB`;
                document.getElementById('storage-status').textContent =
                    `${data.storageUsedPercent}% used`;

            } catch (error) {
                console.error('Failed to update backup status:', error);
            }
        }

        function updateStatusWidget(elementId, status) {
            const element = document.getElementById(elementId);
            const widget = element.closest('.widget');

            widget.className = 'widget ' + (status === 'OK' ? 'status-ok' :
                                          status === 'WARNING' ? 'status-warning' : 'status-error');
            element.textContent = status;
        }

        // Update every 5 minutes
        setInterval(updateBackupStatus, 300000);
        updateBackupStatus(); // Initial load
    </script>
</body>
</html>
```

## Compliance and Documentation

### Backup Compliance Checklist
```markdown
## SOC 2 Type II Compliance Checklist

### Access Controls
- [ ] Backup access restricted to authorized personnel
- [ ] Multi-factor authentication required for backup systems
- [ ] Backup access logged and monitored
- [ ] Regular access reviews conducted

### Data Protection
- [ ] Backups encrypted at rest and in transit
- [ ] Encryption keys managed securely
- [ ] Data retention policies enforced
- [ ] Secure deletion of expired backups

### Monitoring and Alerting
- [ ] Backup failures generate immediate alerts
- [ ] Backup success/failure logged
- [ ] Regular backup integrity testing
- [ ] Monitoring dashboard maintained

### Documentation
- [ ] Backup procedures documented and current
- [ ] Recovery procedures tested and documented
- [ ] Incident response procedures defined
- [ ] Regular review and updates of procedures

### Testing and Validation
- [ ] Monthly recovery testing performed
- [ ] Test results documented
- [ ] Issues identified and resolved
- [ ] Procedures updated based on test results
```

### Backup and Recovery SLA
```markdown
## Backup and Recovery Service Level Agreement

### Backup SLA Targets
| Backup Type | Frequency | Success Rate | Alert Time |
|-------------|-----------|--------------|------------|
| Full | Weekly | 99% | 4 hours |
| Differential | Daily | 99% | 2 hours |
| Transaction Log | 15 minutes | 99.5% | 30 minutes |
| Configuration | Daily | 98% | 4 hours |

### Recovery SLA Targets
| Recovery Type | RTO | RPO | Success Rate |
|---------------|-----|-----|--------------|
| Database | 1 hour | 15 minutes | 95% |
| Application | 30 minutes | 24 hours | 98% |
| Complete System | 4 hours | 1 hour | 90% |

### Escalation Procedures
1. **Level 1**: Backup failure alerts sent to operations team
2. **Level 2**: After 2 hours, escalate to senior administrator
3. **Level 3**: After 4 hours, escalate to CTO
4. **Level 4**: After 8 hours, escalate to executive team

### Penalty Clauses
- **SLA Miss**: Root cause analysis required within 24 hours
- **Critical Failure**: Post-incident review within 48 hours
- **Repeated Failures**: Process improvement plan within 1 week
```

---

This comprehensive backup and recovery strategy ensures data protection, business continuity, and regulatory compliance for the GCC Database Solution while providing clear procedures for both routine operations and disaster recovery scenarios.

---
*Last Updated: September 17, 2025*