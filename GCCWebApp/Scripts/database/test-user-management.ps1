# Test script to verify User Management functionality
$baseUrl = "http://localhost:5101"

# Step 1: Test login
Write-Host "Testing admin login..." -ForegroundColor Yellow
$loginResponse = try {
    Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method POST -ContentType "application/x-www-form-urlencoded" -Body "Email=admin@greencirclecertified.com&Password=admin123" -SessionVariable session
} catch {
    Write-Host "Login failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host "Login response: $($loginResponse.StatusCode)" -ForegroundColor Green

# Step 2: Test GetUsers API endpoint
Write-Host "Testing GetUsers API..." -ForegroundColor Yellow
try {
    $usersResponse = Invoke-WebRequest -Uri "$baseUrl/Admin/GetUsers" -Method GET -WebSession $session
    Write-Host "GetUsers API response: $($usersResponse.StatusCode)" -ForegroundColor Green
    
    # Parse JSON response
    $usersData = $usersResponse.Content | ConvertFrom-Json
    Write-Host "API Response Success: $($usersData.Success)" -ForegroundColor Cyan
    Write-Host "API Response Data Count: $($usersData.Data.Length)" -ForegroundColor Cyan
    
} catch {
    Write-Host "GetUsers API failed: $_" -ForegroundColor Red
}

# Step 3: Test User Management page
Write-Host "Testing User Management page..." -ForegroundColor Yellow
try {
    $pageResponse = Invoke-WebRequest -Uri "$baseUrl/Admin/UserManagement" -Method GET -WebSession $session
    Write-Host "User Management page response: $($pageResponse.StatusCode)" -ForegroundColor Green
    
    # Check if the page contains our new JavaScript
    if ($pageResponse.Content -match "userManagement\.js") {
        Write-Host "User Management page includes new JavaScript file" -ForegroundColor Green
    } else {
        Write-Host "User Management page does not include new JavaScript file" -ForegroundColor Red
    }
    
} catch {
    Write-Host "User Management page failed: $_" -ForegroundColor Red
}

Write-Host "User Management test completed!" -ForegroundColor Magenta