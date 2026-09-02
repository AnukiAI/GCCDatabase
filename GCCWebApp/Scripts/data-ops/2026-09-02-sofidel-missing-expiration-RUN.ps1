$ErrorActionPreference='Stop'
$repoRoot="C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"
$raw=Get-Content (Join-Path $repoRoot 'appsettings.Production.json') -Raw
$m=[regex]::Match($raw,'"DefaultConnection"\s*:\s*"((?:[^"\\]|\\.)*)"')
$cs=($m.Groups[1].Value -replace '\\"','"' -replace '\\\\','\')
$csb=New-Object System.Data.SqlClient.SqlConnectionStringBuilder $cs

$server = $csb['Data Source']
$db     = $csb['Initial Catalog']
$user   = $csb['User ID']
$pass   = $csb['Password']

$sqlFile = Join-Path $repoRoot "Scripts\data-ops\_sql_output\20260902-080944\migration-load.sql"

Write-Host "SERVER  : $server"
Write-Host "DATABASE: $db"
Write-Host "SCRIPT  : $sqlFile"
Write-Host ""
Write-Host "--- sqlcmd output ---"

& sqlcmd -S $server -d $db -U $user -P $pass -I -b -i $sqlFile
Write-Host ""
Write-Host "sqlcmd exit code: $LASTEXITCODE"
