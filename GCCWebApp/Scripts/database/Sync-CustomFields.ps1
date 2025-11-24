# CustomFields Data Sync: Dev -> Production
# Uses .NET SqlDataAdapter to handle long text fields properly

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CustomFields Data Synchronization" -ForegroundColor Cyan
Write-Host "Dev (ANUKI01) -> Production (AccuWeb)" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Connection strings
$devConnStr = "Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;TrustServerCertificate=True"
$prodConnStr = "Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;TrustServerCertificate=True;Encrypt=True"

try {
    # Step 1: Read from Dev
    Write-Host "[Step 1] Connecting to Dev database..." -ForegroundColor Yellow
    $devConn = New-Object System.Data.SqlClient.SqlConnection($devConnStr)
    $devConn.Open()
    
    Write-Host "[Step 1] Reading CustomFields from Dev..." -ForegroundColor Yellow
    $devCmd = $devConn.CreateCommand()
    $devCmd.CommandText = "SELECT * FROM CustomFields ORDER BY Id"
    $devAdapter = New-Object System.Data.SqlClient.SqlDataAdapter($devCmd)
    $devDataTable = New-Object System.Data.DataTable
    $rowCount = $devAdapter.Fill($devDataTable)
    
    Write-Host "[Step 1] Read $rowCount records from Dev`n" -ForegroundColor Green
    $devConn.Close()
    
    # Step 2: Write to Production
    Write-Host "[Step 2] Connecting to Production database..." -ForegroundColor Yellow
    $prodConn = New-Object System.Data.SqlClient.SqlConnection($prodConnStr)
    $prodConn.Open()
    
    # Clear existing data
    Write-Host "[Step 2] Clearing existing CustomFields in Production..." -ForegroundColor Yellow
    $clearCmd = $prodConn.CreateCommand()
    $clearCmd.CommandText = "DELETE FROM CustomFields; DBCC CHECKIDENT ('CustomFields', RESEED, 0);"
    $clearCmd.ExecuteNonQuery() | Out-Null
    Write-Host "[Step 2] Existing data cleared`n" -ForegroundColor Green
    
    # Insert records
    Write-Host "[Step 3] Inserting records into Production..." -ForegroundColor Yellow
    $insertedCount = 0
    
    foreach ($row in $devDataTable.Rows) {
        $insertCmd = $prodConn.CreateCommand()
        $insertCmd.CommandText = @"
SET IDENTITY_INSERT CustomFields ON;
INSERT INTO CustomFields (
    Id, Name, FieldType, IsRequired, IsVisible, IsPublic, 
    SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId
) VALUES (
    @Id, @Name, @FieldType, @IsRequired, @IsVisible, @IsPublic,
    @SortOrder, @Options, @DefaultValue, @ValidationRules, @CertificateTypeId
);
SET IDENTITY_INSERT CustomFields OFF;
"@
        
        # Add parameters
        $insertCmd.Parameters.AddWithValue("@Id", $row["Id"]) | Out-Null
        $insertCmd.Parameters.AddWithValue("@Name", $row["Name"]) | Out-Null
        $insertCmd.Parameters.AddWithValue("@FieldType", $row["FieldType"]) | Out-Null
        $insertCmd.Parameters.AddWithValue("@IsRequired", $row["IsRequired"]) | Out-Null
        $insertCmd.Parameters.AddWithValue("@IsVisible", $row["IsVisible"]) | Out-Null
        $insertCmd.Parameters.AddWithValue("@IsPublic", $row["IsPublic"]) | Out-Null
        $insertCmd.Parameters.AddWithValue("@SortOrder", $row["SortOrder"]) | Out-Null
        
        # Handle nullable fields
        if ($row["Options"] -is [DBNull]) {
            $insertCmd.Parameters.AddWithValue("@Options", [DBNull]::Value) | Out-Null
        } else {
            $insertCmd.Parameters.AddWithValue("@Options", $row["Options"]) | Out-Null
        }
        
        if ($row["DefaultValue"] -is [DBNull]) {
            $insertCmd.Parameters.AddWithValue("@DefaultValue", [DBNull]::Value) | Out-Null
        } else {
            $insertCmd.Parameters.AddWithValue("@DefaultValue", $row["DefaultValue"]) | Out-Null
        }
        
        if ($row["ValidationRules"] -is [DBNull]) {
            $insertCmd.Parameters.AddWithValue("@ValidationRules", [DBNull]::Value) | Out-Null
        } else {
            $insertCmd.Parameters.AddWithValue("@ValidationRules", $row["ValidationRules"]) | Out-Null
        }
        
        $insertCmd.Parameters.AddWithValue("@CertificateTypeId", $row["CertificateTypeId"]) | Out-Null
        
        # Execute insert
        $insertCmd.ExecuteNonQuery() | Out-Null
        $insertedCount++
        
        if ($insertedCount % 10 -eq 0) {
            Write-Host "  Progress: $insertedCount records inserted..." -ForegroundColor Gray
        }
    }
    
    Write-Host "[Step 3] Inserted $insertedCount records`n" -ForegroundColor Green
    
    # Verify
    Write-Host "[Step 4] Verifying..." -ForegroundColor Yellow
    $verifyCmd = $prodConn.CreateCommand()
    $verifyCmd.CommandText = "SELECT COUNT(*) FROM CustomFields"
    $finalCount = $verifyCmd.ExecuteScalar()
    Write-Host "[Step 4] Production now has $finalCount CustomFields records`n" -ForegroundColor Green
    
    $prodConn.Close()
    
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "SUCCESS: CustomFields Synchronized!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Source (Dev):        $rowCount records" -ForegroundColor White
    Write-Host "Inserted (Prod):     $insertedCount records" -ForegroundColor White
    Write-Host "Verified (Prod):     $finalCount records" -ForegroundColor White
    
    exit 0
    
} catch {
    Write-Host "`n========================================" -ForegroundColor Red
    Write-Host "ERROR OCCURRED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host $_.Exception.StackTrace -ForegroundColor Gray
    exit 1
} finally {
    if ($devConn -and $devConn.State -eq 'Open') { $devConn.Close() }
    if ($prodConn -and $prodConn.State -eq 'Open') { $prodConn.Close() }
}
