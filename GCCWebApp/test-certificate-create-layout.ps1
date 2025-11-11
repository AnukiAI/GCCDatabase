# Test Certificate Create Layout
# Takes a screenshot of the Create Certificate page for verification

Write-Host "Testing Certificate Create Layout..." -ForegroundColor Cyan
Write-Host ""

# Check if application is running
$appRunning = Get-Process -Name "GCCWebApp" -ErrorAction SilentlyContinue
if (-not $appRunning) {
    $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
        $_.MainWindowTitle -like "*GCCWebApp*"
    }
    if (-not $dotnetProcesses) {
        Write-Host "⚠️ Application doesn't appear to be running" -ForegroundColor Yellow
        Write-Host "Start the application first: dotnet run --urls `"http://localhost:5101`"" -ForegroundColor Yellow
        Write-Host ""
    }
}

# Test if page is accessible
Write-Host "1. Testing if application is responding..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5101" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    Write-Host "   ✅ Application is running (HTTP $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Cannot connect to application at http://localhost:5101" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "2. Checking CSS file..." -ForegroundColor Cyan
$cssPath = "wwwroot\css\certificate-create.css"
if (Test-Path $cssPath) {
    $cssSize = (Get-Item $cssPath).Length
    Write-Host "   ✅ CSS file exists ($cssSize bytes)" -ForegroundColor Green

    # Check for CSS variables (should be none)
    $cssContent = Get-Content $cssPath -Raw
    $varCount = ([regex]::Matches($cssContent, "var\(--")).Count
    if ($varCount -eq 0) {
        Write-Host "   ✅ No CSS variables found (all hardcoded)" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ Found $varCount CSS variables (should be 0)" -ForegroundColor Yellow
    }

    # Check for grid styles
    if ($cssContent -match "display:\s*grid") {
        Write-Host "   ✅ Grid CSS found" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Grid CSS not found" -ForegroundColor Red
    }
} else {
    Write-Host "   ❌ CSS file not found at $cssPath" -ForegroundColor Red
}

Write-Host ""
Write-Host "3. Manual Testing Instructions:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   a) Open browser and navigate to:" -ForegroundColor White
Write-Host "      http://localhost:5101/Certificate/Create" -ForegroundColor Yellow
Write-Host ""
Write-Host "   b) Login with admin credentials:" -ForegroundColor White
Write-Host "      Email: admin@greencirclecertified.com" -ForegroundColor Yellow
Write-Host "      Password: Admin123!" -ForegroundColor Yellow
Write-Host ""
Write-Host "   c) Verify the following:" -ForegroundColor White
Write-Host "      ✓ Single header with subtitle and 'Back to Management' button" -ForegroundColor Gray
Write-Host "      ✓ Two-column layout: Main form (left) + Sidebar (right)" -ForegroundColor Gray
Write-Host "      ✓ Main form has 2 columns of fields" -ForegroundColor Gray
Write-Host "      ✓ Sidebar shows image upload and tags sections" -ForegroundColor Gray
Write-Host "      ✓ NO duplicate headers" -ForegroundColor Gray
Write-Host ""
Write-Host "   d) Open DevTools F12 and check:" -ForegroundColor White
Write-Host "      • Console tab: No CSS errors" -ForegroundColor Gray
Write-Host "      • Elements tab: Inspect .form-grid" -ForegroundColor Gray
Write-Host "        → display: grid" -ForegroundColor Gray
Write-Host "        → grid-template-columns: 1fr 380px" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Test Layout with Debug Borders:" -ForegroundColor Cyan
Write-Host "   http://localhost:5101/test-layout.html" -ForegroundColor Yellow
Write-Host "   (Shows red/blue/green borders around grid elements)" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Take Screenshot:" -ForegroundColor Cyan
Write-Host "   • Press Win+Shift+S to take a screenshot" -ForegroundColor Gray
Write-Host "   • Or use browser DevTools screenshot feature" -ForegroundColor Gray
Write-Host ""

# Open browser automatically
$openBrowser = Read-Host "Open browser to test page now? (Y/N)"
if ($openBrowser -eq "Y" -or $openBrowser -eq "y") {
    Write-Host ""
    Write-Host "Opening browser..." -ForegroundColor Cyan
    Start-Process "http://localhost:5101/Certificate/Create"
    Start-Sleep -Seconds 2
    Start-Process "http://localhost:5101/test-layout.html"
}

Write-Host ""
Write-Host "Testing complete. Review CERTIFICATE_CREATE_LAYOUT_FIXES.md for details." -ForegroundColor Green
Write-Host ""
