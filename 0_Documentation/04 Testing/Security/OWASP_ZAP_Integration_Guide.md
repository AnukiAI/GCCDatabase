# OWASP ZAP Integration Guide for GCC Web Application

## Overview

This guide provides comprehensive instructions for integrating OWASP ZAP (Zed Attack Proxy) into the GCC Web Application security testing workflow. ZAP is a free, open-source web application security scanner that can be integrated into development and testing processes.

## OWASP ZAP Setup and Configuration

### Installation

#### Desktop Installation
```bash
# Download from https://www.zaproxy.org/download/
# Windows: Install ZAP_2.14.0_windows.exe
# Linux: wget https://github.com/zaproxy/zaproxy/releases/download/v2.14.0/ZAP_2.14.0_Linux.tar.gz
# macOS: Install ZAP_2.14.0.dmg
```

#### Docker Installation
```bash
# Pull official OWASP ZAP Docker image
docker pull owasp/zap2docker-stable:latest

# Run ZAP in daemon mode
docker run -u zap -p 8080:8080 -i owasp/zap2docker-stable:latest zap.sh -daemon -host 0.0.0.0 -port 8080 -config api.addrs.addr.name=.* -config api.addrs.addr.regex=true
```

#### Automated Installation Script
```powershell
# PowerShell script for Windows automation
$zapVersion = "2.14.0"
$zapUrl = "https://github.com/zaproxy/zaproxy/releases/download/v$zapVersion/ZAP_${zapVersion}_windows.exe"
$zapInstaller = "$env:TEMP\ZAP_${zapVersion}_windows.exe"

# Download ZAP installer
Invoke-WebRequest -Uri $zapUrl -OutFile $zapInstaller

# Silent installation
Start-Process -FilePath $zapInstaller -ArgumentList "/S" -Wait

# Verify installation
$zapPath = "${env:ProgramFiles}\OWASP\Zed Attack Proxy\zap.bat"
if (Test-Path $zapPath) {
    Write-Host "OWASP ZAP installed successfully at: $zapPath"
} else {
    Write-Error "OWASP ZAP installation failed"
}
```

### Configuration for GCC Web Application

#### ZAP Context Configuration
```xml
<!-- GCC-WebApp-Context.xml -->
<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<configuration>
    <context>
        <name>GCC-WebApp</name>
        <desc>GCC Web Application Security Testing Context</desc>
        <inscope>true</inscope>
        <incregexes>
            <incregex>https://localhost:5001/.*</incregex>
            <incregex>https://gcc-webapp\..*</incregex>
        </incregexes>
        <excregexes>
            <excregex>.*logout.*</excregex>
            <excregex>.*\.js$</excregex>
            <excregex>.*\.css$</excregex>
            <excregex>.*\.png$</excregex>
            <excregex>.*\.jpg$</excregex>
            <excregex>.*\.gif$</excregex>
        </excregexes>
        <tech>
            <include>Db.Microsoft SQL Server</include>
            <include>Language.ASP.NET</include>
            <include>Language.C#</include>
            <include>Language.JavaScript</include>
            <include>OS.Windows</include>
            <include>WS.ASP.NET</include>
        </tech>
        <authentication>
            <type>1</type>
            <strategy>EACH_RESP</strategy>
            <pollurl></pollurl>
            <polldata></polldata>
            <pollheaders></pollheaders>
            <pollfreq>60</pollfreq>
            <pollunits>REQUESTS</pollunits>
            <form>
                <loginurl>https://localhost:5001/api/v1/auth/login</loginurl>
                <loginbody>{"Username":"{%username%}","Password":"{%password%}"}</loginbody>
                <loginpageurl></loginpageurl>
            </form>
        </authentication>
        <users>
            <user>
                <name>admin-user</name>
                <credentials>
                    <credential>
                        <name>username</name>
                        <value>securitytestadmin</value>
                    </credential>
                    <credential>
                        <name>password</name>
                        <value>SecurePassword123!</value>
                    </credential>
                </credentials>
                <enabled>true</enabled>
            </user>
            <user>
                <name>regular-user</name>
                <credentials>
                    <credential>
                        <name>username</name>
                        <value>securitytestuser</value>
                    </credential>
                    <credential>
                        <name>password</name>
                        <value>UserPassword123!</value>
                    </credential>
                </credentials>
                <enabled>true</enabled>
            </user>
        </users>
    </context>
</configuration>
```

## Automated ZAP Testing Scripts

### PowerShell Automation Script
```powershell
# ZAP-Security-Test.ps1
param(
    [string]$TargetUrl = "https://localhost:5001",
    [string]$ZapPath = "${env:ProgramFiles}\OWASP\Zed Attack Proxy\zap.bat",
    [string]$ReportPath = ".\SecurityReports",
    [int]$ZapPort = 8080
)

# Ensure report directory exists
if (!(Test-Path $ReportPath)) {
    New-Item -ItemType Directory -Path $ReportPath
}

Write-Host "Starting OWASP ZAP Security Testing for GCC Web Application" -ForegroundColor Green

# Start ZAP in daemon mode
Write-Host "Starting ZAP daemon..." -ForegroundColor Yellow
$zapProcess = Start-Process -FilePath $ZapPath -ArgumentList "-daemon", "-host", "127.0.0.1", "-port", $ZapPort, "-config", "api.addrs.addr.name=.*", "-config", "api.addrs.addr.regex=true" -PassThru

# Wait for ZAP to start
Start-Sleep -Seconds 30

# ZAP API base URL
$zapApiUrl = "http://127.0.0.1:$ZapPort"

try {
    # Create new session
    Write-Host "Creating new ZAP session..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$zapApiUrl/JSON/core/action/newSession/" -Method Get

    # Load context
    Write-Host "Loading GCC Web App context..." -ForegroundColor Yellow
    $contextPath = ".\GCC-WebApp-Context.xml"
    if (Test-Path $contextPath) {
        Invoke-RestMethod -Uri "$zapApiUrl/JSON/context/action/importContext/" -Method Get -Body @{contextFile = $contextPath}
    }

    # Spider the application
    Write-Host "Starting spider scan..." -ForegroundColor Yellow
    $spiderResult = Invoke-RestMethod -Uri "$zapApiUrl/JSON/spider/action/scan/" -Method Get -Body @{url = $TargetUrl; maxChildren = 10; recurse = "true"; contextName = "GCC-WebApp"}
    $spiderId = $spiderResult.scan

    # Wait for spider to complete
    do {
        Start-Sleep -Seconds 10
        $spiderStatus = Invoke-RestMethod -Uri "$zapApiUrl/JSON/spider/view/status/" -Method Get -Body @{scanId = $spiderId}
        Write-Host "Spider progress: $($spiderStatus.status)%" -ForegroundColor Cyan
    } while ([int]$spiderStatus.status -lt 100)

    # Ajax Spider for SPA content
    Write-Host "Starting Ajax spider for SPA content..." -ForegroundColor Yellow
    $ajaxSpiderResult = Invoke-RestMethod -Uri "$zapApiUrl/JSON/ajaxSpider/action/scan/" -Method Get -Body @{url = $TargetUrl; inScope = "true"}
    
    # Wait for Ajax spider
    do {
        Start-Sleep -Seconds 10
        $ajaxStatus = Invoke-RestMethod -Uri "$zapApiUrl/JSON/ajaxSpider/view/status/" -Method Get
        Write-Host "Ajax Spider status: $($ajaxStatus.status)" -ForegroundColor Cyan
    } while ($ajaxStatus.status -eq "running")

    # Active scan
    Write-Host "Starting active security scan..." -ForegroundColor Yellow
    $activeScanResult = Invoke-RestMethod -Uri "$zapApiUrl/JSON/ascan/action/scan/" -Method Get -Body @{url = $TargetUrl; recurse = "true"; inScopeOnly = "true"; scanPolicyName = "Default Policy"; method = "GET"; postData = ""}
    $activeScanId = $activeScanResult.scan

    # Wait for active scan to complete
    do {
        Start-Sleep -Seconds 30
        $activeScanStatus = Invoke-RestMethod -Uri "$zapApiUrl/JSON/ascan/view/status/" -Method Get -Body @{scanId = $activeScanId}
        Write-Host "Active scan progress: $($activeScanStatus.status)%" -ForegroundColor Cyan
    } while ([int]$activeScanStatus.status -lt 100)

    # Generate reports
    Write-Host "Generating security reports..." -ForegroundColor Yellow
    
    # HTML Report
    $htmlReport = Invoke-RestMethod -Uri "$zapApiUrl/OTHER/core/other/htmlreport/" -Method Get
    $htmlReport | Out-File -FilePath "$ReportPath\GCC-Security-Report-$(Get-Date -Format 'yyyyMMdd-HHmmss').html" -Encoding UTF8

    # XML Report
    $xmlReport = Invoke-RestMethod -Uri "$zapApiUrl/OTHER/core/other/xmlreport/" -Method Get
    $xmlReport | Out-File -FilePath "$ReportPath\GCC-Security-Report-$(Get-Date -Format 'yyyyMMdd-HHmmss').xml" -Encoding UTF8

    # JSON Report
    $jsonReport = Invoke-RestMethod -Uri "$zapApiUrl/JSON/core/view/alerts/" -Method Get
    $jsonReport | ConvertTo-Json -Depth 10 | Out-File -FilePath "$ReportPath\GCC-Security-Report-$(Get-Date -Format 'yyyyMMdd-HHmmss').json" -Encoding UTF8

    # Get alert summary
    $alerts = Invoke-RestMethod -Uri "$zapApiUrl/JSON/core/view/alertsSummary/" -Method Get
    
    Write-Host "Security scan completed!" -ForegroundColor Green
    Write-Host "Alert Summary:" -ForegroundColor White
    $alerts.alertsSummary | ForEach-Object {
        $riskColor = switch ($_.risk) {
            "High" { "Red" }
            "Medium" { "Yellow" }
            "Low" { "Cyan" }
            default { "White" }
        }
        Write-Host "  $($_.risk) Risk: $($_.count) alerts" -ForegroundColor $riskColor
    }

} finally {
    # Stop ZAP
    Write-Host "Stopping ZAP daemon..." -ForegroundColor Yellow
    try {
        Invoke-RestMethod -Uri "$zapApiUrl/JSON/core/action/shutdown/" -Method Get
    } catch {
        # Force kill if graceful shutdown fails
        if ($zapProcess -and !$zapProcess.HasExited) {
            $zapProcess.Kill()
        }
    }
}

Write-Host "Reports saved to: $ReportPath" -ForegroundColor Green
```

### CI/CD Integration

#### GitHub Actions Workflow
```yaml
# .github/workflows/security-scan.yml
name: Security Scan with OWASP ZAP

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]
  schedule:
    - cron: '0 2 * * 1' # Weekly Monday 2 AM

jobs:
  security-scan:
    runs-on: ubuntu-latest
    
    services:
      gcc-webapp:
        image: gcc-webapp:latest
        ports:
          - 5001:5001
        env:
          ASPNETCORE_ENVIRONMENT: Testing
          ConnectionStrings__DefaultConnection: "Server=localhost;Database=GCCTest;Trusted_Connection=true;"
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Build and Start Application
      run: |
        dotnet restore
        dotnet build --no-restore
        dotnet run --project GCCWebApp &
        sleep 30
    
    - name: Run OWASP ZAP Security Scan
      uses: zaproxy/action-full-scan@v0.7.0
      with:
        target: 'https://localhost:5001'
        rules_file_name: '.zap/rules.tsv'
        cmd_options: '-a -j -m 10 -T 60'
        issue_title: 'ZAP Security Scan'
        token: ${{ github.token }}
    
    - name: Upload ZAP Reports
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: zap-reports
        path: |
          report_html.html
          report_json.json
          report_md.md
    
    - name: Comment PR with Security Results
      if: github.event_name == 'pull_request'
      uses: actions/github-script@v6
      with:
        script: |
          const fs = require('fs');
          const report = fs.readFileSync('report_md.md', 'utf8');
          
          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: `## 🔒 Security Scan Results\n\n${report}`
          });
```

#### Azure DevOps Pipeline
```yaml
# azure-pipelines-security.yml
trigger:
  branches:
    include:
    - main
    - develop

pr:
  branches:
    include:
    - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  zapVersion: '2.14.0'
  targetUrl: 'https://localhost:5001'

stages:
- stage: SecurityScan
  displayName: 'OWASP ZAP Security Scan'
  jobs:
  - job: ZapScan
    displayName: 'Run ZAP Security Tests'
    steps:
    
    - task: Docker@2
      displayName: 'Pull OWASP ZAP Docker Image'
      inputs:
        command: 'pull'
        arguments: 'owasp/zap2docker-stable:$(zapVersion)'
    
    - task: DockerCompose@0
      displayName: 'Start Test Environment'
      inputs:
        action: 'Run services'
        dockerComposeFile: 'docker-compose.test.yml'
        buildImages: true
        detached: true
    
    - script: |
        # Wait for application to start
        sleep 30
        
        # Run ZAP baseline scan
        docker run -v $(System.DefaultWorkingDirectory):/zap/wrk/:rw \
          -t owasp/zap2docker-stable:$(zapVersion) \
          zap-baseline.py \
          -t $(targetUrl) \
          -g gen.conf \
          -J zap-baseline-report.json \
          -r zap-baseline-report.html
          
        # Run ZAP full scan
        docker run -v $(System.DefaultWorkingDirectory):/zap/wrk/:rw \
          -t owasp/zap2docker-stable:$(zapVersion) \
          zap-full-scan.py \
          -t $(targetUrl) \
          -g gen.conf \
          -J zap-full-report.json \
          -r zap-full-report.html
          
      displayName: 'Run OWASP ZAP Security Scans'
      continueOnError: true
    
    - task: PublishTestResults@2
      displayName: 'Publish ZAP Test Results'
      inputs:
        testResultsFormat: 'JUnit'
        testResultsFiles: '**/zap-*.xml'
        mergeTestResults: true
        testRunTitle: 'OWASP ZAP Security Tests'
    
    - task: PublishHtmlReport@1
      displayName: 'Publish Security Report'
      inputs:
        reportDir: '$(System.DefaultWorkingDirectory)'
        tabName: 'ZAP Security Report'
    
    - task: PublishBuildArtifacts@1
      displayName: 'Publish ZAP Reports'
      inputs:
        pathToPublish: '$(System.DefaultWorkingDirectory)'
        artifactName: 'zap-security-reports'
        publishLocation: 'Container'
```

## ZAP Custom Scan Policies

### GCC Web Application Scan Policy
```xml
<!-- GCC-WebApp-ScanPolicy.xml -->
<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<configuration>
    <policy>
        <name>GCC-WebApp-Policy</name>
        <desc>Custom scan policy for GCC Web Application</desc>
        <scanner>
            <level>MEDIUM</level>
            <strength>MEDIUM</strength>
        </scanner>
        
        <!-- SQL Injection -->
        <scanner id="40018" level="HIGH" strength="HIGH"/>
        
        <!-- XSS (Reflected) -->
        <scanner id="40012" level="HIGH" strength="HIGH"/>
        
        <!-- XSS (Persistent) -->
        <scanner id="40014" level="HIGH" strength="HIGH"/>
        
        <!-- Path Traversal -->
        <scanner id="6" level="HIGH" strength="HIGH"/>
        
        <!-- Command Injection -->
        <scanner id="90020" level="HIGH" strength="HIGH"/>
        
        <!-- Authentication Bypass -->
        <scanner id="10105" level="MEDIUM" strength="MEDIUM"/>
        
        <!-- Session Management -->
        <scanner id="10202" level="MEDIUM" strength="MEDIUM"/>
        
        <!-- CSRF -->
        <scanner id="20012" level="MEDIUM" strength="MEDIUM"/>
        
        <!-- Information Disclosure -->
        <scanner id="10009" level="LOW" strength="LOW"/>
        
        <!-- File Upload -->
        <scanner id="10006" level="MEDIUM" strength="MEDIUM"/>
    </policy>
</configuration>
```

## Integration with Automated Testing

### NUnit Integration
```csharp
// SecurityTests/ZapIntegrationTests.cs
using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

[TestFixture]
[Category("SecurityIntegration")]
public class ZapIntegrationTests
{
    private const string ZapApiUrl = "http://127.0.0.1:8080";
    private const string TargetUrl = "https://localhost:5001";
    private const string ZapPath = @"C:\Program Files\OWASP\Zed Attack Proxy\zap.bat";
    
    private Process _zapProcess;
    private HttpClient _zapClient;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        // Start ZAP daemon
        _zapProcess = Process.Start(new ProcessStartInfo
        {
            FileName = ZapPath,
            Arguments = "-daemon -host 127.0.0.1 -port 8080 -config api.addrs.addr.name=.* -config api.addrs.addr.regex=true",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        // Wait for ZAP to start
        await Task.Delay(30000);
        
        _zapClient = new HttpClient();
    }

    [Test]
    public async Task RunBaselineScan_ShouldNotFindHighRiskVulnerabilities()
    {
        // Spider the application
        var spiderResponse = await _zapClient.GetAsync($"{ZapApiUrl}/JSON/spider/action/scan/?url={TargetUrl}&maxChildren=10&recurse=true");
        spiderResponse.EnsureSuccessStatusCode();

        // Wait for spider to complete
        string spiderStatus;
        do
        {
            await Task.Delay(10000);
            var statusResponse = await _zapClient.GetStringAsync($"{ZapApiUrl}/JSON/spider/view/status/");
            spiderStatus = statusResponse;
        } while (!spiderStatus.Contains("100"));

        // Get alerts
        var alertsResponse = await _zapClient.GetStringAsync($"{ZapApiUrl}/JSON/core/view/alerts/");
        
        // Parse and assert no high-risk vulnerabilities
        // Implementation depends on JSON parsing library
        Assert.That(alertsResponse, Does.Not.Contain("\"risk\":\"High\""), 
            "High-risk vulnerabilities found during baseline scan");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _zapClient?.Dispose();
        
        if (_zapProcess != null && !_zapProcess.HasExited)
        {
            _zapProcess.Kill();
            _zapProcess.Dispose();
        }
    }
}
```

## Reporting and Analysis

### Custom Report Generation
```python
# generate_gcc_security_report.py
import requests
import json
import datetime
from jinja2 import Template

class ZapReportGenerator:
    def __init__(self, zap_url="http://127.0.0.1:8080"):
        self.zap_url = zap_url
        self.session = requests.Session()
    
    def get_alerts(self):
        """Retrieve all alerts from ZAP"""
        response = self.session.get(f"{self.zap_url}/JSON/core/view/alerts/")
        return response.json()
    
    def get_alert_summary(self):
        """Get alert summary by risk level"""
        response = self.session.get(f"{self.zap_url}/JSON/core/view/alertsSummary/")
        return response.json()
    
    def generate_executive_report(self, alerts_data, output_file):
        """Generate executive summary report"""
        template = Template("""
        # GCC Web Application Security Assessment Report
        
        ## Executive Summary
        Generated: {{ timestamp }}
        
        ### Risk Summary
        {% for summary in alert_summary %}
        - **{{ summary.risk }} Risk**: {{ summary.count }} findings
        {% endfor %}
        
        ### Critical Findings
        {% for alert in high_risk_alerts %}
        - **{{ alert.name }}**: {{ alert.description }}
          - Risk: {{ alert.risk }}
          - Confidence: {{ alert.confidence }}
          - URL: {{ alert.url }}
        {% endfor %}
        
        ### Recommendations
        1. Immediate remediation required for High and Medium risk findings
        2. Implement security testing in CI/CD pipeline
        3. Regular penetration testing recommended
        4. Security training for development team
        """)
        
        high_risk_alerts = [alert for alert in alerts_data.get('alerts', []) 
                           if alert.get('risk') == 'High']
        
        report = template.render(
            timestamp=datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
            alert_summary=self.get_alert_summary().get('alertsSummary', []),
            high_risk_alerts=high_risk_alerts
        )
        
        with open(output_file, 'w') as f:
            f.write(report)

if __name__ == "__main__":
    generator = ZapReportGenerator()
    alerts = generator.get_alerts()
    generator.generate_executive_report(alerts, "gcc_security_executive_report.md")
```

This comprehensive OWASP ZAP integration provides automated security testing capabilities that complement the manual penetration testing framework, ensuring continuous security validation throughout the development lifecycle.