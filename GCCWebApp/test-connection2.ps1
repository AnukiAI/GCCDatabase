# Test with corrected password: Q3Ez4e!gE_47KNi (! not l)
$tests = @(
    @{
        Name = "Set 1: GCC_adminxyz123 + gccwebap_certificates"
        Server = "68.71.129.120,1533"
        Database = "gccwebap_certificates"
        User = "GCC_adminxyz123"
        Password = "Q3Ez4e!gE_47KNi"
    },
    @{
        Name = "Set 2: adminGCC + gccwebap_gccdb"
        Server = "68.71.129.120,1533"
        Database = "gccwebap_gccdb"
        User = "adminGCC"
        Password = "Q3Ez4e!gE_47KNi"
    }
)

foreach ($test in $tests) {
    Write-Host "`n========================================"  -ForegroundColor Yellow
    Write-Host "Testing: $($test.Name)" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow

    $connStr = "Server=$($test.Server);Database=$($test.Database);User Id=$($test.User);Password=$($test.Password);Encrypt=True;TrustServerCertificate=True;Connection Timeout=10;"

    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
        $conn.Open()

        Write-Host "✓ Authentication successful!" -ForegroundColor Green
        Write-Host "✓ Connected to: $($conn.Database)" -ForegroundColor Green
        Write-Host "✓ Server: $($conn.DataSource)" -ForegroundColor Green
        Write-Host "✓ User: $($test.User)" -ForegroundColor Green

        # Try to query
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = 'SELECT DB_NAME() AS CurrentDB, @@VERSION AS Version'
        $reader = $cmd.ExecuteReader()

        if ($reader.Read()) {
            Write-Host "`nDatabase Info:" -ForegroundColor Cyan
            Write-Host "  Current DB: $($reader['CurrentDB'])" -ForegroundColor Cyan
            Write-Host "  Version: $($reader['Version'].ToString().Split([char]10)[0])" -ForegroundColor Cyan
        }
        $reader.Close()

        $conn.Close()
        Write-Host "`n✓✓✓ ALL TESTS PASSED! ✓✓✓" -ForegroundColor Green

    } catch {
        Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red

        if ($_.Exception.InnerException) {
            Write-Host "  Details: $($_.Exception.InnerException.Message)" -ForegroundColor Yellow
        }
    }
}
