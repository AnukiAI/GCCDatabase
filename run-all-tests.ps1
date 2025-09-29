# Comprehensive Test Runner for GCC Web Application
# This script runs all test suites and generates detailed reports

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "GCC Web Application - Comprehensive Test Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$testResultsDir = "TestResults_$timestamp"
$reportFile = "$testResultsDir\TestSummary.html"

# Create results directory
New-Item -ItemType Directory -Force -Path $testResultsDir | Out-Null

# Function to kill processes
function Kill-TestProcesses {
    Write-Host "Cleaning up running processes..." -ForegroundColor Yellow
    Get-Process | Where-Object {$_.ProcessName -eq "dotnet" -or $_.ProcessName -eq "testhost"} | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# Function to run tests for a project
function Run-TestProject {
    param(
        [string]$ProjectPath,
        [string]$ProjectName
    )

    Write-Host ""
    Write-Host "Testing: $ProjectName" -ForegroundColor Green
    Write-Host "----------------------------------------" -ForegroundColor Gray

    $resultFile = "$testResultsDir\$ProjectName.trx"
    $logFile = "$testResultsDir\$ProjectName.log"

    try {
        # Clean and restore
        dotnet clean $ProjectPath --verbosity quiet 2>&1 | Out-File $logFile
        dotnet restore $ProjectPath --verbosity quiet 2>&1 | Out-File $logFile -Append

        # Build
        Write-Host "  Building..." -NoNewline
        $buildResult = dotnet build $ProjectPath --no-restore --verbosity quiet 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " OK" -ForegroundColor Green
        } else {
            Write-Host " FAILED" -ForegroundColor Red
            $buildResult | Out-File $logFile -Append
            return @{
                Project = $ProjectName
                Status = "Build Failed"
                Passed = 0
                Failed = 0
                Skipped = 0
                Total = 0
                Duration = "N/A"
            }
        }

        # Run tests
        Write-Host "  Running tests..." -NoNewline
        $testOutput = dotnet test $ProjectPath `
            --no-build `
            --logger "trx;LogFileName=$resultFile" `
            --logger "console;verbosity=minimal" `
            --collect:"XPlat Code Coverage" `
            2>&1

        $testOutput | Out-File $logFile -Append

        # Parse results
        $passed = 0
        $failed = 0
        $skipped = 0
        $total = 0
        $duration = "N/A"

        foreach ($line in $testOutput) {
            if ($line -match "Passed:\s+(\d+)") { $passed = $matches[1] }
            if ($line -match "Failed:\s+(\d+)") { $failed = $matches[1] }
            if ($line -match "Skipped:\s+(\d+)") { $skipped = $matches[1] }
            if ($line -match "Total:\s+(\d+)") { $total = $matches[1] }
            if ($line -match "Duration:\s+(.+)") { $duration = $matches[1] }
        }

        if ($total -eq 0) {
            Write-Host " No tests found" -ForegroundColor Yellow
            return @{
                Project = $ProjectName
                Status = "No Tests"
                Passed = 0
                Failed = 0
                Skipped = 0
                Total = 0
                Duration = "N/A"
            }
        }

        if ($failed -gt 0) {
            Write-Host " FAILED ($failed/$total)" -ForegroundColor Red
            $status = "Failed"
        } else {
            Write-Host " PASSED ($passed/$total)" -ForegroundColor Green
            $status = "Passed"
        }

        return @{
            Project = $ProjectName
            Status = $status
            Passed = $passed
            Failed = $failed
            Skipped = $skipped
            Total = $total
            Duration = $duration
        }

    } catch {
        Write-Host " ERROR" -ForegroundColor Red
        $_.Exception.Message | Out-File $logFile -Append
        return @{
            Project = $ProjectName
            Status = "Error"
            Passed = 0
            Failed = 0
            Skipped = 0
            Total = 0
            Duration = "N/A"
        }
    }
}

# Main execution
$results = @()

# Kill existing processes
Kill-TestProcesses

# Define test projects
$testProjects = @(
    @{Path="..\GCCWebApp.Tests\GCCWebApp.Tests.csproj"; Name="Unit Tests"},
    @{Path="..\GCCWebApp.Api.Tests\GCCWebApp.Api.Tests.csproj"; Name="API Tests"},
    @{Path="..\GCCWebApp.UAT.Tests\GCCWebApp.UAT.Tests.csproj"; Name="UAT Tests"},
    @{Path="..\GCCWebApp.E2E.Tests\GCCWebApp.E2E.Tests.csproj"; Name="E2E Tests"},
    @{Path="..\GCCWebApp.Performance.Tests\GCCWebApp.Performance.Tests.csproj"; Name="Performance Tests"},
    @{Path="..\GCCWebApp.Security.Tests\GCCWebApp.Security.Tests.csproj"; Name="Security Tests"},
    @{Path="..\GCCWebApp.BDD.Tests\GCCWebApp.BDD.Tests.csproj"; Name="BDD Tests"},
    @{Path="..\GCCWebApp.Accessibility.Tests\GCCWebApp.Accessibility.Tests.csproj"; Name="Accessibility Tests"}
)

# Run each test project
foreach ($project in $testProjects) {
    if (Test-Path $project.Path) {
        $result = Run-TestProject -ProjectPath $project.Path -ProjectName $project.Name
        $results += $result
    } else {
        Write-Host ""
        Write-Host "Skipping: $($project.Name) (not found)" -ForegroundColor Yellow
        $results += @{
            Project = $project.Name
            Status = "Not Found"
            Passed = 0
            Failed = 0
            Skipped = 0
            Total = 0
            Duration = "N/A"
        }
    }
}

# Generate HTML Report
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Generating Test Report..." -ForegroundColor Cyan

$totalPassed = ($results | Measure-Object -Property Passed -Sum).Sum
$totalFailed = ($results | Measure-Object -Property Failed -Sum).Sum
$totalSkipped = ($results | Measure-Object -Property Skipped -Sum).Sum
$totalTests = ($results | Measure-Object -Property Total -Sum).Sum

$htmlReport = @"
<!DOCTYPE html>
<html>
<head>
    <title>GCC Web Application - Test Results</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        h1 { color: #333; border-bottom: 3px solid #007acc; padding-bottom: 10px; }
        .summary { background: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .summary-stats { display: flex; justify-content: space-around; margin-top: 15px; }
        .stat { text-align: center; }
        .stat-value { font-size: 2em; font-weight: bold; }
        .stat-label { color: #666; margin-top: 5px; }
        .passed { color: #28a745; }
        .failed { color: #dc3545; }
        .skipped { color: #ffc107; }
        .total { color: #007acc; }
        table { width: 100%; background: white; border-collapse: collapse; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        th { background: #007acc; color: white; padding: 12px; text-align: left; }
        td { padding: 10px; border-bottom: 1px solid #e0e0e0; }
        tr:hover { background: #f8f9fa; }
        .status-passed { color: #28a745; font-weight: bold; }
        .status-failed { color: #dc3545; font-weight: bold; }
        .status-error { color: #dc3545; font-weight: bold; }
        .status-no-tests { color: #ffc107; }
        .status-not-found { color: #6c757d; }
        .timestamp { color: #666; font-size: 0.9em; margin-top: 20px; }
        .coverage-bar { background: #e0e0e0; height: 20px; border-radius: 10px; overflow: hidden; }
        .coverage-fill { background: linear-gradient(to right, #28a745, #20c997); height: 100%; }
    </style>
</head>
<body>
    <h1>GCC Web Application - Test Results</h1>

    <div class="summary">
        <h2>Test Summary</h2>
        <div class="summary-stats">
            <div class="stat">
                <div class="stat-value passed">$totalPassed</div>
                <div class="stat-label">Passed</div>
            </div>
            <div class="stat">
                <div class="stat-value failed">$totalFailed</div>
                <div class="stat-label">Failed</div>
            </div>
            <div class="stat">
                <div class="stat-value skipped">$totalSkipped</div>
                <div class="stat-label">Skipped</div>
            </div>
            <div class="stat">
                <div class="stat-value total">$totalTests</div>
                <div class="stat-label">Total Tests</div>
            </div>
        </div>
        $(if ($totalTests -gt 0) {
            $successRate = [math]::Round(($totalPassed / $totalTests) * 100, 2)
            @"
        <div style="margin-top: 20px;">
            <div>Success Rate: $successRate%</div>
            <div class="coverage-bar">
                <div class="coverage-fill" style="width: $successRate%;"></div>
            </div>
        </div>
"@
        })
    </div>

    <h2>Test Results by Project</h2>
    <table>
        <thead>
            <tr>
                <th>Project</th>
                <th>Status</th>
                <th>Passed</th>
                <th>Failed</th>
                <th>Skipped</th>
                <th>Total</th>
                <th>Duration</th>
            </tr>
        </thead>
        <tbody>
"@

foreach ($result in $results) {
    $statusClass = "status-" + $result.Status.ToLower().Replace(" ", "-")
    $htmlReport += @"
            <tr>
                <td><strong>$($result.Project)</strong></td>
                <td class="$statusClass">$($result.Status)</td>
                <td class="passed">$($result.Passed)</td>
                <td class="failed">$($result.Failed)</td>
                <td class="skipped">$($result.Skipped)</td>
                <td>$($result.Total)</td>
                <td>$($result.Duration)</td>
            </tr>
"@
}

$htmlReport += @"
        </tbody>
    </table>

    <div class="timestamp">
        Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
    </div>
</body>
</html>
"@

# Save HTML report
$htmlReport | Out-File $reportFile -Encoding UTF8

# Display summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Execution Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "  Total Tests: $totalTests"
Write-Host "  Passed: $totalPassed" -ForegroundColor Green
Write-Host "  Failed: $totalFailed" -ForegroundColor Red
Write-Host "  Skipped: $totalSkipped" -ForegroundColor Yellow
if ($totalTests -gt 0) {
    $successRate = [math]::Round(($totalPassed / $totalTests) * 100, 2)
    Write-Host "  Success Rate: $successRate%" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "Results saved to: $testResultsDir" -ForegroundColor Cyan
Write-Host "HTML Report: $reportFile" -ForegroundColor Cyan
Write-Host ""

# Open report in browser
Start-Process $reportFile