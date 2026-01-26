# Alter certificate Number column size
$config = Get-Content 'appsettings.Development.json' | ConvertFrom-Json
$connString = $config.ConnectionStrings.DefaultConnection
Write-Host "Connecting to database..."

$sql = "ALTER TABLE Certificates ALTER COLUMN Number NVARCHAR(50) NOT NULL;"

$conn = New-Object System.Data.SqlClient.SqlConnection($connString)
$conn.Open()
$cmd = New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
$result = $cmd.ExecuteNonQuery()
$conn.Close()
Write-Host "Column altered successfully"
