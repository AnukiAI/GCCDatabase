# Role-Based UI Test Log Viewer
# PowerShell script to view and analyze test logs

param(
    [string]$LogType = "latest",  # "latest", "summary", "all", or specific timestamp
    [switch]$OpenInNotepad,
    [switch]$ShowFailuresOnly,
    [switch]$ShowSummaryOnly
)

$LogsDir = Join-Path $PSScriptRoot "test-logs"
$ScreenshotsDir = Join-Path $PSScriptRoot "Screenshots"
$ReportsDir = Join-Path $PSScriptRoot "test-results"

Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host "                    Role-Based UI Test Log Viewer" -ForegroundColor Cyan
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host ""

# Check if logs directory exists
if (-not (Test-Path $LogsDir)) {
    Write-Host "❌ No test logs found. Please run the tests first." -ForegroundColor Red
    Write-Host "   Use: .\run-role-based-ui-tests.cmd" -ForegroundColor Yellow
    exit 1
}

# Get available log files
$LogFiles = Get-ChildItem $LogsDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
$SummaryFiles = Get-ChildItem $LogsDir -Filter "TestSummary_*.log" | Sort-Object LastWriteTime -Descending
$DetailedFiles = Get-ChildItem $LogsDir -Filter "RoleBasedUITests_*.log" | Sort-Object LastWriteTime -Descending

if ($LogFiles.Count -eq 0) {
    Write-Host "❌ No log files found in $LogsDir" -ForegroundColor Red
    exit 1
}

Write-Host "📁 Available test logs:" -ForegroundColor Green
Write-Host "   Logs Directory: $LogsDir" -ForegroundColor Gray
Write-Host "   Screenshots Directory: $ScreenshotsDir" -ForegroundColor Gray
Write-Host "   Reports Directory: $ReportsDir" -ForegroundColor Gray
Write-Host ""

if ($LogType -eq "latest" -or $ShowSummaryOnly) {
    # Show latest summary
    if ($SummaryFiles.Count -gt 0) {
        $LatestSummary = $SummaryFiles[0]
        Write-Host "📊 Latest Test Summary ($($LatestSummary.Name)):" -ForegroundColor Yellow
        Write-Host "   Last Modified: $($LatestSummary.LastWriteTime)" -ForegroundColor Gray
        Write-Host "========================================================================" -ForegroundColor Cyan
        Get-Content $LatestSummary.FullName | ForEach-Object {
            if ($_ -match "\[PASSED\]") {
                Write-Host $_ -ForegroundColor Green
            } elseif ($_ -match "\[FAILED\]") {
                Write-Host $_ -ForegroundColor Red
            } elseif ($_ -match "ALL TESTS PASSED") {
                Write-Host $_ -ForegroundColor Green
            } elseif ($_ -match "SOME TESTS FAILED") {
                Write-Host $_ -ForegroundColor Red
            } else {
                Write-Host $_
            }
        }
        Write-Host "========================================================================" -ForegroundColor Cyan
        Write-Host ""
    }
}

if ($LogType -eq "latest" -and -not $ShowSummaryOnly) {
    # Show latest detailed log
    if ($DetailedFiles.Count -gt 0) {
        $LatestDetailed = $DetailedFiles[0]
        Write-Host "📝 Latest Detailed Log ($($LatestDetailed.Name)):" -ForegroundColor Yellow
        Write-Host "   Last Modified: $($LatestDetailed.LastWriteTime)" -ForegroundColor Gray
        Write-Host "   Size: $([math]::Round($LatestDetailed.Length / 1KB, 2)) KB" -ForegroundColor Gray
        
        if ($ShowFailuresOnly) {
            Write-Host ""
            Write-Host "🔍 Showing failures and errors only:" -ForegroundColor Red
            Get-Content $LatestDetailed.FullName | Where-Object {
                $_ -match "FAIL|ERROR|Exception|❌|WARNING" -and $_ -notmatch "warning CS|warning MSB"
            } | ForEach-Object {
                Write-Host $_ -ForegroundColor Red
            }
        } elseif ($OpenInNotepad) {
            Write-Host ""
            Write-Host "🗒️  Opening detailed log in Notepad..." -ForegroundColor Green
            Start-Process notepad.exe -ArgumentList $LatestDetailed.FullName
        } else {
            Write-Host ""
            Write-Host "📄 Use -ShowFailuresOnly to see failures only, or -OpenInNotepad to open in editor" -ForegroundColor Yellow
        }
    }
}

if ($LogType -eq "all") {
    Write-Host "📋 All Available Logs:" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "Summary Files:" -ForegroundColor Green
    $SummaryFiles | ForEach-Object {
        Write-Host "  📊 $($_.Name) - $($_.LastWriteTime)" -ForegroundColor Cyan
    }
    Write-Host ""
    
    Write-Host "Detailed Log Files:" -ForegroundColor Green
    $DetailedFiles | ForEach-Object {
        $SizeKB = [math]::Round($_.Length / 1KB, 2)
        Write-Host "  📝 $($_.Name) - $($_.LastWriteTime) ($SizeKB KB)" -ForegroundColor Cyan
    }
}

# Show additional file information
Write-Host ""
Write-Host "📁 Additional Files:" -ForegroundColor Yellow

# Check for screenshots
if (Test-Path $ScreenshotsDir) {
    $Screenshots = Get-ChildItem $ScreenshotsDir -Filter "*.png" | Sort-Object LastWriteTime -Descending
    Write-Host "  📸 Screenshots: $($Screenshots.Count) files" -ForegroundColor Green
    if ($Screenshots.Count -gt 0) {
        Write-Host "     Latest: $($Screenshots[0].Name) - $($Screenshots[0].LastWriteTime)" -ForegroundColor Gray
    }
}

# Check for test reports
if (Test-Path $ReportsDir) {
    $Reports = Get-ChildItem $ReportsDir -Filter "*.trx" | Sort-Object LastWriteTime -Descending
    Write-Host "  📊 Test Reports: $($Reports.Count) files" -ForegroundColor Green
    if ($Reports.Count -gt 0) {
        Write-Host "     Latest: $($Reports[0].Name) - $($Reports[0].LastWriteTime)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "🔧 Usage Examples:" -ForegroundColor Yellow
Write-Host "  .\view-test-logs.ps1                    # View latest summary and log info"
Write-Host "  .\view-test-logs.ps1 -ShowSummaryOnly   # Show only test summary"
Write-Host "  .\view-test-logs.ps1 -ShowFailuresOnly  # Show only failures from latest log"
Write-Host "  .\view-test-logs.ps1 -OpenInNotepad     # Open latest detailed log in Notepad"
Write-Host "  .\view-test-logs.ps1 -LogType all       # List all available log files"
Write-Host ""

Write-Host "📂 Quick File Access:" -ForegroundColor Yellow
Write-Host "  explorer `"$LogsDir`"        # Open logs directory"
Write-Host "  explorer `"$ScreenshotsDir`"  # Open screenshots directory"
Write-Host "  explorer `"$ReportsDir`"      # Open reports directory"