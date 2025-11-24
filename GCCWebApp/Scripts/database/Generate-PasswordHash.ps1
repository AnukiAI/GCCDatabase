# Generate BCrypt password hash for production database
# Usage: .\Generate-PasswordHash.ps1

param(
    [string]$Password = "TestPass123!"
)

Write-Host "Generating BCrypt hash for password: $Password" -ForegroundColor Cyan

# Load BCrypt from the main project's bin folder
$bcryptDll = Get-ChildItem -Path "..\GCCWebApp\bin" -Filter "BCrypt.Net-Next.dll" -Recurse | Select-Object -First 1

if ($bcryptDll) {
    Write-Host "Found BCrypt.Net-Next.dll at: $($bcryptDll.FullName)" -ForegroundColor Green
    Add-Type -Path $bcryptDll.FullName

    $hash = [BCrypt.Net.BCrypt]::HashPassword($Password)

    Write-Host "`nPassword: $Password" -ForegroundColor Yellow
    Write-Host "BCrypt Hash: $hash" -ForegroundColor Green
    Write-Host "`nSQL UPDATE statement:" -ForegroundColor Cyan
    Write-Host "UPDATE dbo.Users SET PasswordHash = '$hash' WHERE Username = 'admin';" -ForegroundColor White

    # Test verification
    $isValid = [BCrypt.Net.BCrypt]::Verify($Password, $hash)
    Write-Host "`nVerification test: $isValid" -ForegroundColor $(if($isValid){"Green"}else{"Red"})

    # Save to SQL file
    $sqlContent = @"
-- Reset admin password to: $Password
-- Generated on: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- BCrypt hash: $hash

UPDATE dbo.Users
SET PasswordHash = '$hash'
WHERE Username = 'admin';

-- Verify the update
SELECT Id, Username, Email, PasswordHash, Status, IsActive, IsDeleted
FROM dbo.Users
WHERE Username = 'admin';
"@

    $sqlFile = ".\reset-admin-password-generated.sql"
    $sqlContent | Out-File -FilePath $sqlFile -Encoding UTF8
    Write-Host "`nSQL file saved to: $sqlFile" -ForegroundColor Green

} else {
    Write-Host "ERROR: BCrypt.Net-Next.dll not found. Please build the project first:" -ForegroundColor Red
    Write-Host "  cd ..\GCCWebApp" -ForegroundColor Yellow
    Write-Host "  dotnet build" -ForegroundColor Yellow
}
