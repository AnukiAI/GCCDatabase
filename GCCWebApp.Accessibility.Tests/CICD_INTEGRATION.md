# CI/CD Integration Guide for Accessibility Tests

This guide explains how to integrate GCC Web App accessibility tests into your CI/CD pipeline.

## GitHub Actions Integration

### Basic Workflow
Create `.github/workflows/accessibility-tests.yml`:

```yaml
name: Accessibility Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  accessibility-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build application
      run: dotnet build --no-restore --configuration Release
    
    - name: Install Playwright browsers
      run: |
        cd GCCWebApp.Accessibility.Tests
        pwsh bin/Release/net9.0/playwright.ps1 install chromium
    
    - name: Start application
      run: |
        cd GCCWebApp
        dotnet run --configuration Release &
        sleep 30  # Wait for app to start
      env:
        ASPNETCORE_ENVIRONMENT: Testing
    
    - name: Run accessibility tests
      run: |
        cd GCCWebApp.Accessibility.Tests
        dotnet test --configuration Release --logger "trx;LogFileName=accessibility_results.trx" --results-directory TestResults
    
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: accessibility-test-results
        path: |
          GCCWebApp.Accessibility.Tests/TestResults/
          GCCWebApp.Accessibility.Tests/Reports/
    
    - name: Comment PR with results
      if: github.event_name == 'pull_request'
      uses: actions/github-script@v6
      with:
        script: |
          const fs = require('fs');
          const path = 'GCCWebApp.Accessibility.Tests/Reports';
          
          if (fs.existsSync(path)) {
            const files = fs.readdirSync(path);
            if (files.length > 0) {
              github.rest.issues.createComment({
                issue_number: context.issue.number,
                owner: context.repo.owner,
                repo: context.repo.repo,
                body: '⚠️ Accessibility violations found! Check the uploaded artifacts for details.'
              });
            } else {
              github.rest.issues.createComment({
                issue_number: context.issue.number,
                owner: context.repo.owner,
                repo: context.repo.repo,
                body: '✅ All accessibility tests passed!'
              });
            }
          }
```

### Advanced GitHub Actions with Matrix Testing
```yaml
strategy:
  matrix:
    browser: [chromium, firefox, webkit]
    
steps:
- name: Run accessibility tests
  run: |
    cd GCCWebApp.Accessibility.Tests
    dotnet test --configuration Release --logger "trx;LogFileName=accessibility_results_${{ matrix.browser }}.trx"
  env:
    BROWSER: ${{ matrix.browser }}
```

## Azure DevOps Integration

### Azure Pipelines YAML
Create `azure-pipelines.yml`:

```yaml
trigger:
- main
- develop

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '9.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'
    projects: '**/*.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Build application'
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '--configuration $(buildConfiguration) --no-restore'

- task: PowerShell@2
  displayName: 'Install Playwright browsers'
  inputs:
    targetType: 'inline'
    script: |
      cd GCCWebApp.Accessibility.Tests/bin/$(buildConfiguration)/net9.0
      pwsh playwright.ps1 install chromium

- task: DotNetCoreCLI@2
  displayName: 'Start application'
  inputs:
    command: 'run'
    projects: 'GCCWebApp/GCCWebApp.csproj'
    arguments: '--configuration $(buildConfiguration)'
  env:
    ASPNETCORE_ENVIRONMENT: Testing

- task: DotNetCoreCLI@2
  displayName: 'Run accessibility tests'
  inputs:
    command: 'test'
    projects: 'GCCWebApp.Accessibility.Tests/*.csproj'
    arguments: '--configuration $(buildConfiguration) --logger trx --results-directory $(Agent.TempDirectory)'

- task: PublishTestResults@2
  displayName: 'Publish test results'
  condition: always()
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/*.trx'
    searchFolder: '$(Agent.TempDirectory)'

- task: PublishBuildArtifacts@1
  displayName: 'Publish accessibility reports'
  condition: always()
  inputs:
    pathToPublish: 'GCCWebApp.Accessibility.Tests/Reports'
    artifactName: 'accessibility-reports'
```

## Jenkins Integration

### Jenkinsfile
```groovy
pipeline {
    agent any
    
    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        ASPNETCORE_ENVIRONMENT = 'Testing'
    }
    
    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        
        stage('Restore') {
            steps {
                sh 'dotnet restore'
            }
        }
        
        stage('Build') {
            steps {
                sh 'dotnet build --configuration Release --no-restore'
            }
        }
        
        stage('Install Playwright') {
            steps {
                dir('GCCWebApp.Accessibility.Tests') {
                    sh 'pwsh bin/Release/net9.0/playwright.ps1 install chromium'
                }
            }
        }
        
        stage('Start Application') {
            steps {
                script {
                    sh 'dotnet run --project GCCWebApp/GCCWebApp.csproj --configuration Release &'
                    sleep 30
                }
            }
        }
        
        stage('Run Accessibility Tests') {
            steps {
                dir('GCCWebApp.Accessibility.Tests') {
                    sh 'dotnet test --configuration Release --logger "trx;LogFileName=accessibility_results.trx" --results-directory TestResults'
                }
            }
        }
    }
    
    post {
        always {
            publishTestResults testResultsPattern: 'GCCWebApp.Accessibility.Tests/TestResults/*.trx'
            archiveArtifacts artifacts: 'GCCWebApp.Accessibility.Tests/Reports/**/*', allowEmptyArchive: true
            
            script {
                def reportFiles = findFiles(glob: 'GCCWebApp.Accessibility.Tests/Reports/*.json')
                if (reportFiles.length > 0) {
                    currentBuild.result = 'UNSTABLE'
                    slackSend channel: '#dev-alerts', 
                             color: 'warning',
                             message: "Accessibility violations found in ${env.JOB_NAME} #${env.BUILD_NUMBER}"
                }
            }
        }
    }
}
```

## Quality Gates and Policies

### Branch Protection Rules
Set up branch protection rules that require accessibility tests to pass:

```yaml
# .github/branch-protection.yml
rules:
  main:
    required_status_checks:
      strict: true
      contexts:
        - "Accessibility Tests"
    required_pull_request_reviews:
      required_approving_review_count: 1
```

### SonarQube Integration
```xml
<!-- sonar-project.properties -->
sonar.projectKey=gcc-webapp
sonar.sources=.
sonar.exclusions=**/bin/**,**/obj/**
sonar.cs.vstest.reportsPaths=GCCWebApp.Accessibility.Tests/TestResults/*.trx
```

## Notifications and Reporting

### Slack Integration
```yaml
- name: Notify Slack on failure
  if: failure()
  uses: 8398a7/action-slack@v3
  with:
    status: failure
    text: 'Accessibility tests failed! Check the reports for violations.'
  env:
    SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK }}
```

### Email Reports
```yaml
- name: Send email report
  if: always()
  uses: dawidd6/action-send-mail@v3
  with:
    server_address: smtp.gmail.com
    server_port: 587
    username: ${{ secrets.EMAIL_USERNAME }}
    password: ${{ secrets.EMAIL_PASSWORD }}
    subject: "Accessibility Test Results - ${{ github.sha }}"
    body: "Accessibility test results are attached."
    attachments: GCCWebApp.Accessibility.Tests/Reports/*.json
```

## Performance Optimization

### Parallel Test Execution
```yaml
- name: Run tests in parallel
  run: |
    dotnet test --configuration Release --parallel --logger trx
```

### Browser Caching
```yaml
- name: Cache Playwright browsers
  uses: actions/cache@v3
  with:
    path: ~/.cache/ms-playwright
    key: playwright-${{ runner.os }}-${{ hashFiles('**/GCCWebApp.Accessibility.Tests.csproj') }}
```

### Docker Integration
```dockerfile
# Dockerfile.accessibility-tests
FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /app
COPY . .

RUN dotnet restore
RUN dotnet build --configuration Release

# Install Playwright
RUN pwsh GCCWebApp.Accessibility.Tests/bin/Release/net9.0/playwright.ps1 install chromium

CMD ["dotnet", "test", "GCCWebApp.Accessibility.Tests", "--configuration", "Release"]
```

## Monitoring and Dashboards

### Azure Application Insights
```csharp
// In test configuration
services.AddApplicationInsightsTelemetry();
```

### Custom Metrics
```yaml
- name: Send metrics to monitoring
  run: |
    VIOLATION_COUNT=$(find GCCWebApp.Accessibility.Tests/Reports -name "*.json" | wc -l)
    curl -X POST "https://api.monitoring.com/metrics" \
         -H "Authorization: Bearer ${{ secrets.MONITORING_TOKEN }}" \
         -d "accessibility_violations=$VIOLATION_COUNT"
```

## Best Practices

1. **Run on Every PR** - Catch accessibility issues early
2. **Fail Fast** - Stop deployment if critical violations found
3. **Trend Analysis** - Track violations over time
4. **Developer Feedback** - Provide clear violation reports
5. **Staged Rollout** - Start with warnings, then enforce failures
6. **Regular Updates** - Keep axe-core and Playwright updated

## Troubleshooting CI/CD Issues

### Common Problems
1. **Browser installation failures** - Use specific browser versions
2. **Application startup timeouts** - Increase wait times
3. **Network connectivity** - Configure proper timeouts
4. **Resource limitations** - Use appropriate VM sizes

### Debug Mode
```yaml
- name: Debug accessibility tests
  run: |
    cd GCCWebApp.Accessibility.Tests
    dotnet test --configuration Release --logger "console;verbosity=detailed"
  env:
    PLAYWRIGHT_DEBUG: 1
```