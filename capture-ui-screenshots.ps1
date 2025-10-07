# UI Screenshot Capture Script
# Opens browser windows for manual screenshot capture

$urls = @(
    # Public Views
    @{ Name = "01-Public-Certificate-Tile-View"; Url = "http://localhost:5101/Certificate/CertificateTileView"; Auth = "None" },
    @{ Name = "02-Login-Page"; Url = "http://localhost:5101/Account/Login"; Auth = "None" },

    # Admin Views - Login first as admin@greencirclecertified.com / Admin123!
    @{ Name = "03-Admin-Dashboard"; Url = "http://localhost:5101/Dashboard/Index"; Auth = "Admin" },
    @{ Name = "04-Admin-Certificate-Tile-Internal"; Url = "http://localhost:5101/Certificate/CertificateTileViewInternal"; Auth = "Admin" },
    @{ Name = "05-Admin-Certificate-Create"; Url = "http://localhost:5101/Certificate/Create"; Auth = "Admin" },
    @{ Name = "06-Admin-Companies"; Url = "http://localhost:5101/Company/Index"; Auth = "Admin" },
    @{ Name = "07-Admin-Products"; Url = "http://localhost:5101/Product/Management"; Auth = "Admin" },
    @{ Name = "08-Admin-User-Management"; Url = "http://localhost:5101/Admin/UserManagement"; Auth = "Admin" },
    @{ Name = "09-Admin-Role-Management"; Url = "http://localhost:5101/Admin/RoleManagement"; Auth = "Admin" },
    @{ Name = "10-Admin-Certificate-Types"; Url = "http://localhost:5101/Admin/CertificateTypes"; Auth = "Admin" },
    @{ Name = "11-Admin-Product-Types"; Url = "http://localhost:5101/Admin/ProductTypes"; Auth = "Admin" },
    @{ Name = "12-Admin-Certificate-Fields"; Url = "http://localhost:5101/Admin/CertificateFields"; Auth = "Admin" },
    @{ Name = "13-Admin-Partners"; Url = "http://localhost:5101/Admin/Partners"; Auth = "Admin" },

    # Analyst Views - Login as analyst@greencirclecertified.com / Analyst123!
    @{ Name = "14-Analyst-Dashboard"; Url = "http://localhost:5101/Dashboard/Index"; Auth = "Analyst" },

    # External Views - Login as partner@amazon.com / Partner123!
    @{ Name = "15-External-Certificate-Tile"; Url = "http://localhost:5101/Certificate/CertificateTileViewExternal"; Auth = "External" },
    @{ Name = "16-External-Products"; Url = "http://localhost:5101/Product/Management"; Auth = "External" }
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  GCC UI Screenshot Capture Guide" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script will open browser windows for each view." -ForegroundColor Yellow
Write-Host "Please take a screenshot of each page using Windows + Shift + S" -ForegroundColor Yellow
Write-Host "Save screenshots to: ui-screenshots\" -ForegroundColor Yellow
Write-Host ""

# Create screenshots directory
$screenshotsDir = "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\ui-screenshots"
if (!(Test-Path $screenshotsDir)) {
    New-Item -ItemType Directory -Path $screenshotsDir | Out-Null
    Write-Host "Created directory: $screenshotsDir" -ForegroundColor Green
}

Write-Host ""
Write-Host "Press Enter to start..." -ForegroundColor Cyan
Read-Host

foreach ($page in $urls) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "Opening: $($page.Name)" -ForegroundColor White
    Write-Host "Auth Required: $($page.Auth)" -ForegroundColor Gray
    Write-Host "URL: $($page.Url)" -ForegroundColor Gray
    Write-Host "========================================" -ForegroundColor Yellow

    if ($page.Auth -eq "Admin") {
        Write-Host "Login with: admin@greencirclecertified.com / Admin123!" -ForegroundColor Magenta
    }
    elseif ($page.Auth -eq "Analyst") {
        Write-Host "Login with: analyst@greencirclecertified.com / Analyst123!" -ForegroundColor Magenta
    }
    elseif ($page.Auth -eq "External") {
        Write-Host "Login with: partner@amazon.com / Partner123!" -ForegroundColor Magenta
    }

    Start-Process $page.Url

    Write-Host ""
    Write-Host "1. Take screenshot (Windows + Shift + S)" -ForegroundColor Cyan
    Write-Host "2. Save as: $($page.Name).png" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Press Enter when done to continue..." -ForegroundColor Green
    Read-Host
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Screenshot capture complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Please save all screenshots to:" -ForegroundColor Yellow
Write-Host "$screenshotsDir" -ForegroundColor White
