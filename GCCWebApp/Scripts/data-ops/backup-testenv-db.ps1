<#
.SYNOPSIS
  Logical backup / restore of the GCC test-environment database (gccwebap_certificates).

.DESCRIPTION
  The deployed test environment at gccwebapp.com runs against the hosted SQL Server at
  68.71.129.120,1533. The only credential we hold (GCC_adminxyz123) is
  db_ddladmin + db_datareader + db_datawriter -- it has NEITHER 'BACKUP DATABASE' NOR
  'VIEW DEFINITION'. That rules out both a server-side .bak and a sqlpackage .bacpac
  (sqlpackage fails with "you do not have View Definition permission").

  So this takes a *logical* backup instead: every user table's rows are exported to
  newline-delimited JSON, plus a schema snapshot read from the sys catalog views.

  Fidelity notes:
    * NULL is preserved distinctly from empty string (JSON null).
    * byte[] columns are base64-encoded; datetime/datetimeoffset use ISO 8601 round-trip ("o").
    * COMPUTED columns are deliberately excluded -- they cannot be inserted on restore and
      SQL Server recalculates them (CR-187 NormalizedName/NormalizedNumber etc.).
    * IDENTITY values ARE preserved on restore (IDENTITY_INSERT is toggled per table).

  This is a DATA backup. The authoritative schema is the EF Core migrations in this repo;
  schema.json is a snapshot for comparison/verification, not a CREATE script.

.PARAMETER Mode
  Export (default) or Restore.

.PARAMETER OutDir
  Backup folder. Export creates it; Restore reads it.

.PARAMETER ConnectionString
  Overrides the connection string. Defaults to DefaultConnection from appsettings.Production.json.

.PARAMETER Confirm
  Restore only. Without it, Restore runs as a dry run and writes nothing.

.EXAMPLE
  # Take a backup
  .\backup-testenv-db.ps1

.EXAMPLE
  # Dry-run a restore into a local scratch DB, then do it for real
  .\backup-testenv-db.ps1 -Mode Restore -OutDir <folder> -ConnectionString "Server=localhost;Database=GCCRestoreTest;Trusted_Connection=True;TrustServerCertificate=True;"
  .\backup-testenv-db.ps1 -Mode Restore -OutDir <folder> -ConnectionString "..." -Confirm
#>
[CmdletBinding()]
param(
    [ValidateSet('Export', 'Restore')]
    [string]$Mode = 'Export',

    [string]$OutDir,

    [string]$ConnectionString,

    [switch]$Confirm
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # ...\GCCWebApp\GCCWebApp

# ---------------------------------------------------------------- connection --
function Get-DefaultConnectionString {
    $settings = Join-Path $repoRoot 'appsettings.Production.json'
    if (-not (Test-Path $settings)) {
        throw "Cannot locate appsettings.Production.json at '$settings'. Pass -ConnectionString explicitly."
    }
    # NB: appsettings.Production.json is JSONC -- it carries // comments, which PowerShell 5.1's
    # ConvertFrom-Json rejects. Pull the one value we need with a regex rather than stripping
    # comments (naive stripping would also mangle the "https://" inside AllowedOrigins).
    $raw = Get-Content $settings -Raw
    $m = [regex]::Match($raw, '"DefaultConnection"\s*:\s*"((?:[^"\\]|\\.)*)"')
    if (-not $m.Success) {
        throw "Could not find ConnectionStrings:DefaultConnection in appsettings.Production.json."
    }
    $cs = $m.Groups[1].Value -replace '\\"', '"' -replace '\\\\', '\'
    if ([string]::IsNullOrWhiteSpace($cs)) {
        throw "ConnectionStrings:DefaultConnection is empty in appsettings.Production.json."
    }
    return $cs
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = Get-DefaultConnectionString
}

# NB: PowerShell resolves property access on SqlConnectionStringBuilder through the
# DbConnectionStringBuilder indexer, so .ConnectTimeout / .DataSource throw
# "Keyword not supported". Use the literal connection-string keywords instead.
$csb = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $ConnectionString
if ([int]$csb['Connect Timeout'] -lt 60) { $csb['Connect Timeout'] = 60 }
$ConnectionString = $csb.ConnectionString
$serverLabel = [string]$csb['Data Source']
$dbLabel = [string]$csb['Initial Catalog']
$loginLabel = [string]$csb['User ID']

function New-Connection {
    $conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    $conn.Open()
    return $conn
}

# Returns the SqlCommand, NOT the reader: SqlDataReader implements IEnumerable, so returning one
# from a PowerShell function makes the pipeline unroll it into DataRecordInternal rows.
function New-SqlCommand {
    param([System.Data.SqlClient.SqlConnection]$Conn, [string]$Sql)
    $cmd = $Conn.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = 600
    return $cmd
}

function Invoke-Rows {
    param([System.Data.SqlClient.SqlConnection]$Conn, [string]$Sql)
    $rows = @()
    $reader = (New-SqlCommand -Conn $Conn -Sql $Sql).ExecuteReader()
    try {
        while ($reader.Read()) {
            $o = [ordered]@{}
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                $o[$reader.GetName($i)] = if ($reader.IsDBNull($i)) { $null } else { $reader.GetValue($i) }
            }
            $rows += [pscustomobject]$o
        }
    }
    finally { $reader.Close() }
    return , $rows
}

# ------------------------------------------------------------- value marshal --
function ConvertTo-JsonSafeValue {
    param($Value)
    if ($null -eq $Value -or $Value -is [System.DBNull]) { return $null }
    if ($Value -is [byte[]]) { return @{ '$binary' = [Convert]::ToBase64String($Value) } }
    if ($Value -is [datetime]) { return $Value.ToString('o') }
    if ($Value -is [System.DateTimeOffset]) { return $Value.ToString('o') }
    if ($Value -is [timespan]) { return $Value.ToString('c') }
    if ($Value -is [guid]) { return $Value.ToString() }
    if ($Value -is [decimal]) { return $Value.ToString([System.Globalization.CultureInfo]::InvariantCulture) }
    return $Value
}

function ConvertFrom-JsonSafeValue {
    param($Value, [string]$SqlType)
    if ($null -eq $Value) { return [System.DBNull]::Value }
    if ($Value -is [psobject] -and $Value.PSObject.Properties.Name -contains '$binary') {
        return [Convert]::FromBase64String($Value.'$binary')
    }
    switch -Regex ($SqlType) {
        '^(datetime2?|smalldatetime|date)$' { return [datetime]::Parse($Value, [System.Globalization.CultureInfo]::InvariantCulture, 'RoundtripKind') }
        '^datetimeoffset$' { return [System.DateTimeOffset]::Parse($Value, [System.Globalization.CultureInfo]::InvariantCulture, 'RoundtripKind') }
        '^time$' { return [timespan]::Parse($Value, [System.Globalization.CultureInfo]::InvariantCulture) }
        '^uniqueidentifier$' { return [guid]::Parse($Value) }
        '^(decimal|numeric|money|smallmoney)$' { return [decimal]::Parse($Value, [System.Globalization.CultureInfo]::InvariantCulture) }
        default { return $Value }
    }
}

# ------------------------------------------------------------------- catalog --
$SQL_TABLES = @'
SELECT  s.name AS SchemaName,
        t.name AS TableName,
        CAST(ISNULL(OBJECTPROPERTY(t.object_id, N'TableHasIdentity'), 0) AS bit) AS HasIdentity
FROM    sys.tables t
JOIN    sys.schemas s ON s.schema_id = t.schema_id
WHERE   t.is_ms_shipped = 0
ORDER BY s.name, t.name;
'@

$SQL_COLUMNS = @'
SELECT  s.name AS SchemaName,
        t.name AS TableName,
        c.name AS ColumnName,
        c.column_id AS ColumnId,
        TYPE_NAME(c.user_type_id) AS SqlType,
        c.max_length AS MaxLength,
        c.precision AS [Precision],
        c.scale AS Scale,
        c.is_nullable AS IsNullable,
        c.is_computed AS IsComputed,
        c.is_identity AS IsIdentity
FROM    sys.columns c
JOIN    sys.tables t ON t.object_id = c.object_id
JOIN    sys.schemas s ON s.schema_id = t.schema_id
WHERE   t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id;
'@

$SQL_KEYS = @'
SELECT  s.name AS SchemaName, t.name AS TableName, i.name AS IndexName,
        i.type_desc AS IndexType, i.is_unique AS IsUnique,
        i.is_primary_key AS IsPrimaryKey,
        STUFF((SELECT ', ' + c2.name + CASE WHEN ic2.is_descending_key = 1 THEN ' DESC' ELSE '' END
               FROM sys.index_columns ic2
               JOIN sys.columns c2 ON c2.object_id = ic2.object_id AND c2.column_id = ic2.column_id
               WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id AND ic2.is_included_column = 0
               ORDER BY ic2.key_ordinal
               FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS KeyColumns
FROM    sys.indexes i
JOIN    sys.tables t ON t.object_id = i.object_id
JOIN    sys.schemas s ON s.schema_id = t.schema_id
WHERE   t.is_ms_shipped = 0 AND i.type <> 0
ORDER BY s.name, t.name, i.name;
'@

$SQL_FKS = @'
SELECT  fk.name AS ForeignKeyName,
        SCHEMA_NAME(pt.schema_id) + '.' + pt.name AS ParentTable,
        SCHEMA_NAME(rt.schema_id) + '.' + rt.name AS ReferencedTable,
        fk.delete_referential_action_desc AS OnDelete,
        fk.update_referential_action_desc AS OnUpdate,
        STUFF((SELECT ', ' + pc.name
               FROM sys.foreign_key_columns fkc
               JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
               WHERE fkc.constraint_object_id = fk.object_id
               ORDER BY fkc.constraint_column_id
               FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS ParentColumns
FROM    sys.foreign_keys fk
JOIN    sys.tables pt ON pt.object_id = fk.parent_object_id
JOIN    sys.tables rt ON rt.object_id = fk.referenced_object_id
ORDER BY ParentTable, fk.name;
'@

function Get-Catalog {
    param([System.Data.SqlClient.SqlConnection]$Conn)
    return [pscustomobject]@{
        Tables  = Invoke-Rows -Conn $Conn -Sql $SQL_TABLES
        Columns = Invoke-Rows -Conn $Conn -Sql $SQL_COLUMNS
        Indexes = Invoke-Rows -Conn $Conn -Sql $SQL_KEYS
        ForeignKeys = Invoke-Rows -Conn $Conn -Sql $SQL_FKS
    }
}

function Get-TopoOrderedTables {
    <# Parents before children, so a restore can insert without disabling FKs.
       Self-references and cycles fall back to arbitrary order (handled by the
       NOCHECK/CHECK wrapper during restore). #>
    param($Tables, $ForeignKeys)

    $names = @($Tables | ForEach-Object { "$($_.SchemaName).$($_.TableName)" })
    $deps = @{}
    foreach ($n in $names) { $deps[$n] = New-Object System.Collections.Generic.HashSet[string] }
    foreach ($fk in $ForeignKeys) {
        if ($fk.ParentTable -eq $fk.ReferencedTable) { continue }   # self-ref
        if ($deps.ContainsKey($fk.ParentTable) -and $deps.ContainsKey($fk.ReferencedTable)) {
            [void]$deps[$fk.ParentTable].Add($fk.ReferencedTable)
        }
    }

    $ordered = New-Object System.Collections.Generic.List[string]
    $placed = New-Object System.Collections.Generic.HashSet[string]
    $remaining = [System.Collections.Generic.List[string]]$names
    while ($remaining.Count -gt 0) {
        $progress = $false
        foreach ($n in @($remaining)) {
            $unmet = @($deps[$n] | Where-Object { -not $placed.Contains($_) })
            if ($unmet.Count -eq 0) {
                $ordered.Add($n); [void]$placed.Add($n); [void]$remaining.Remove($n); $progress = $true
            }
        }
        if (-not $progress) {
            # cycle -- emit the rest as-is
            foreach ($n in $remaining) { $ordered.Add($n); [void]$placed.Add($n) }
            break
        }
    }
    return , $ordered
}

# -------------------------------------------------------------------- export --
function Invoke-Export {
    if ([string]::IsNullOrWhiteSpace($OutDir)) {
        $stamp = (Get-Date).ToString('yyyy-MM-dd-HHmmss')
        $OutDir = Join-Path (Split-Path -Parent (Split-Path -Parent $repoRoot)) "db-backups\$dbLabel-$stamp"
    }
    if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }
    $dataDir = Join-Path $OutDir 'data'
    if (-not (Test-Path $dataDir)) { New-Item -ItemType Directory -Path $dataDir -Force | Out-Null }

    Write-Host "Backing up $dbLabel on $serverLabel" -ForegroundColor Cyan
    Write-Host "  -> $OutDir`n"

    $conn = New-Connection
    try {
        $cat = Get-Catalog -Conn $conn
        $order = Get-TopoOrderedTables -Tables $cat.Tables -ForeignKeys $cat.ForeignKeys

        $cat | ConvertTo-Json -Depth 8 |
            Set-Content -Path (Join-Path $OutDir 'schema.json') -Encoding UTF8

        $tableManifest = @()
        $grandTotal = 0

        foreach ($full in $order) {
            $schemaName, $tableName = $full.Split('.', 2)
            $cols = @($cat.Columns | Where-Object {
                    $_.SchemaName -eq $schemaName -and $_.TableName -eq $tableName -and -not $_.IsComputed
                } | Sort-Object ColumnId)
            $computed = @($cat.Columns | Where-Object {
                    $_.SchemaName -eq $schemaName -and $_.TableName -eq $tableName -and $_.IsComputed
                } | ForEach-Object { $_.ColumnName })

            $colList = ($cols | ForEach-Object { "[$($_.ColumnName)]" }) -join ', '
            $sql = "SELECT $colList FROM [$schemaName].[$tableName]"

            $file = Join-Path $dataDir "$schemaName.$tableName.jsonl"
            $writer = New-Object System.IO.StreamWriter($file, $false, (New-Object System.Text.UTF8Encoding($false)))
            $rowCount = 0
            try {
                $reader = (New-SqlCommand -Conn $conn -Sql $sql).ExecuteReader()
                try {
                    while ($reader.Read()) {
                        $o = [ordered]@{}
                        for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                            $o[$reader.GetName($i)] = ConvertTo-JsonSafeValue $reader.GetValue($i)
                        }
                        $writer.WriteLine((ConvertTo-Json $o -Depth 5 -Compress))
                        $rowCount++
                    }
                }
                finally { $reader.Close() }
            }
            finally { $writer.Dispose() }

            $grandTotal += $rowCount
            $tableManifest += [pscustomobject]@{
                Table            = $full
                File             = "data/$schemaName.$tableName.jsonl"
                Rows             = $rowCount
                Columns          = @($cols | ForEach-Object { $_.ColumnName })
                ComputedExcluded = $computed
                HasIdentity      = [bool](($cat.Tables | Where-Object { $_.SchemaName -eq $schemaName -and $_.TableName -eq $tableName }).HasIdentity)
            }
            '{0,-45} {1,7} rows{2}' -f $full, $rowCount, $(if ($computed.Count) { "  (excl. computed: $($computed -join ', '))" } else { '' }) | Write-Host
        }

        $manifest = [pscustomobject]@{
            TakenUtc        = (Get-Date).ToUniversalTime().ToString('o')
            Server          = $serverLabel
            Database        = $dbLabel
            Login           = $loginLabel
            BackupType      = 'logical-jsonl (no BACKUP DATABASE / VIEW DEFINITION permission on this login)'
            TableCount      = $tableManifest.Count
            TotalRows       = $grandTotal
            RestoreOrder    = @($order)
            Tables          = $tableManifest
            SchemaAuthority = 'EF Core migrations in Migrations/ -- schema.json is a snapshot for verification only'
        }
        $manifest | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $OutDir 'manifest.json') -Encoding UTF8

        Write-Host "`nDone. $($tableManifest.Count) tables, $grandTotal rows." -ForegroundColor Green
        Write-Host "Manifest: $(Join-Path $OutDir 'manifest.json')"
    }
    finally { $conn.Close() }
}

# ------------------------------------------------------------------- restore --
function Invoke-Restore {
    if ([string]::IsNullOrWhiteSpace($OutDir)) { throw "-OutDir is required for Restore." }
    $manifestPath = Join-Path $OutDir 'manifest.json'
    if (-not (Test-Path $manifestPath)) { throw "No manifest.json in '$OutDir'." }
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

    Write-Host "Restore $($manifest.Database) (taken $($manifest.TakenUtc)) INTO $dbLabel on $serverLabel" -ForegroundColor Cyan
    if (-not $Confirm) {
        Write-Warning "DRY RUN -- nothing will be written. Re-run with -Confirm to apply."
    }
    elseif ($serverLabel -eq $manifest.Server -and $dbLabel -eq $manifest.Database) {
        Write-Warning "TARGET IS THE SOURCE DATABASE ($serverLabel / $dbLabel)."
        $answer = Read-Host "This OVERWRITES live data. Type the database name to proceed"
        if ($answer -ne $dbLabel) { throw "Aborted -- confirmation did not match." }
    }

    $conn = New-Connection
    try {
        $cat = Get-Catalog -Conn $conn
        $targetTables = @{}
        foreach ($t in $cat.Tables) { $targetTables["$($t.SchemaName).$($t.TableName)"] = $t }

        # Verify every backed-up table + column exists on the target before touching anything.
        $problems = @()
        foreach ($t in $manifest.Tables) {
            if (-not $targetTables.ContainsKey($t.Table)) { $problems += "missing table: $($t.Table)"; continue }
            $schemaName, $tableName = $t.Table.Split('.', 2)
            $targetCols = @($cat.Columns | Where-Object { $_.SchemaName -eq $schemaName -and $_.TableName -eq $tableName } | ForEach-Object { $_.ColumnName })
            foreach ($c in $t.Columns) {
                if ($targetCols -notcontains $c) { $problems += "missing column: $($t.Table).$c" }
            }
        }
        if ($problems.Count -gt 0) {
            $problems | ForEach-Object { Write-Warning $_ }
            throw "Target schema does not match the backup ($($problems.Count) mismatches). Run EF migrations on the target first."
        }
        Write-Host "Schema check passed: $($manifest.Tables.Count) tables." -ForegroundColor Green

        if (-not $Confirm) {
            foreach ($t in $manifest.Tables) { '{0,-45} {1,7} rows would be loaded' -f $t.Table, $t.Rows | Write-Host }
            Write-Host "`nDry run complete. No changes made." -ForegroundColor Yellow
            return
        }

        # Drop FK enforcement for the load, delete children-first, insert parents-first.
        $exec = {
            param($sql)
            $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 600
            [void]$cmd.ExecuteNonQuery()
        }
        & $exec 'EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT ALL"'
        try {
            $reverse = @($manifest.RestoreOrder); [array]::Reverse($reverse)
            foreach ($full in $reverse) {
                $s, $t = $full.Split('.', 2)
                & $exec "DELETE FROM [$s].[$t]"
            }

            foreach ($full in $manifest.RestoreOrder) {
                $entry = $manifest.Tables | Where-Object { $_.Table -eq $full }
                if (-not $entry) { continue }
                $schemaName, $tableName = $full.Split('.', 2)
                $file = Join-Path $OutDir ($entry.File -replace '/', '\')
                if (-not (Test-Path $file)) { throw "Missing data file: $file" }

                $colTypes = @{}
                foreach ($c in ($cat.Columns | Where-Object { $_.SchemaName -eq $schemaName -and $_.TableName -eq $tableName })) {
                    $colTypes[$c.ColumnName] = $c.SqlType
                }

                $table = New-Object System.Data.DataTable
                foreach ($c in $entry.Columns) { [void]$table.Columns.Add($c, [object]) }
                foreach ($line in [System.IO.File]::ReadLines($file)) {
                    if ([string]::IsNullOrWhiteSpace($line)) { continue }
                    $obj = $line | ConvertFrom-Json
                    $row = $table.NewRow()
                    foreach ($c in $entry.Columns) {
                        $row[$c] = ConvertFrom-JsonSafeValue $obj.$c $colTypes[$c]
                    }
                    $table.Rows.Add($row)
                }

                if ($table.Rows.Count -gt 0) {
                    $opts = [System.Data.SqlClient.SqlBulkCopyOptions]::KeepNulls
                    if ($entry.HasIdentity) { $opts = $opts -bor [System.Data.SqlClient.SqlBulkCopyOptions]::KeepIdentity }
                    $bulk = New-Object System.Data.SqlClient.SqlBulkCopy($conn, $opts, $null)
                    try {
                        $bulk.DestinationTableName = "[$schemaName].[$tableName]"
                        $bulk.BulkCopyTimeout = 600
                        foreach ($c in $entry.Columns) { [void]$bulk.ColumnMappings.Add($c, $c) }
                        $bulk.WriteToServer($table)
                    }
                    finally { $bulk.Close() }
                }
                '{0,-45} {1,7} rows restored' -f $full, $table.Rows.Count | Write-Host
            }
        }
        finally {
            & $exec 'EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL"'
        }
        Write-Host "`nRestore complete." -ForegroundColor Green
    }
    finally { $conn.Close() }
}

switch ($Mode) {
    'Export' { Invoke-Export }
    'Restore' { Invoke-Restore }
}
