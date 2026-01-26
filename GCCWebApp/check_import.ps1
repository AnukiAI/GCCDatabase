# Check imported certificates
$config = Get-Content 'appsettings.Development.json' | ConvertFrom-Json
$connString = $config.ConnectionStrings.DefaultConnection

$sql = @"
SELECT c.Id, c.Number, c.Status, co.Name as CompanyName, p.Name as ProductName, ct.Name as CertType
FROM Certificates c
JOIN Companies co ON c.CompanyId = co.Id
JOIN Products p ON c.ProductId = p.Id
JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
ORDER BY c.Id DESC
"@

$conn = New-Object System.Data.SqlClient.SqlConnection($connString)
$conn.Open()
$cmd = New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
$reader = $cmd.ExecuteReader()

Write-Host "=== Certificates in Database ==="
while ($reader.Read()) {
    Write-Host "ID: $($reader['Id']), Number: $($reader['Number']), Status: $($reader['Status']), Company: $($reader['CompanyName']), Product: $($reader['ProductName']), Type: $($reader['CertType'])"
}
$reader.Close()
$conn.Close()
