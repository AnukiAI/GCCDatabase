# Test both credential sets with CORRECT password (! not l)
$connectionStrings = @(
    @{Name="Set 1 (GCC_adminxyz123)"; ConnStr="Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;Encrypt=True;TrustServerCertificate=True;Connection Timeout=10;"},
    @{Name="Set 2 (adminGCC)"; ConnStr="Server=tcp:68.71.129.120,1533;Database=gccwebap_gccdb;User Id=adminGCC;Password=Q3Ez4e!gE_47KNi;Encrypt=True;TrustServerCertificate=True;Connection Timeout=10;"}
)

foreach ($connInfo in $connectionStrings) {
    Write-Host "`n========================================" -ForegroundColor Yellow
    Write-Host "Testing: $($connInfo.Name)" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    $connectionString = $connInfo.ConnStr

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    Write-Host "✓ Connection successful to AccuWeb SQL Server" -ForegroundColor Green

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = 'SELECT @@VERSION'
    $version = $cmd.ExecuteScalar()
    Write-Host "Database Version: $($version.ToString().Split("`n")[0])" -ForegroundColor Cyan

    $cmd.CommandText = 'SELECT DB_NAME()'
    $dbName = $cmd.ExecuteScalar()
    Write-Host "Connected to Database: $dbName" -ForegroundColor Cyan

    $cmd.CommandText = 'SELECT SUSER_NAME()'
    $user = $cmd.ExecuteScalar()
    Write-Host "Logged in as: $user" -ForegroundColor Cyan

    $conn.Close()
    Write-Host "`n✓ All connection tests passed!" -ForegroundColor Green
} catch {
    Write-Host "✗ Connection failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Inner Exception: $($_.Exception.InnerException.Message)" -ForegroundColor Yellow
}
}
