# Deploy Local to Development Environment Script
# Usage: .\deploy-local-to-dev.ps1 -Environment "Development" -ResourceGroup "gcc-dev-rg" -AppName "gcc-webapp-dev"

param(
    [Parameter(Mandatory=$true)]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$true)]
    [string]$AppName,
    
    [string]$ConnectionString,
    [string]$KeyVaultName,
    [switch]$RunMigrations = $true,
    [switch]$RunTests = $true
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "GCC Web Application Deployment Script" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan

# Step 1: Check prerequisites
Write-Host "`nStep 1: Checking prerequisites..." -ForegroundColor Green

# Check if Azure CLI is installed
$azureCliVersion = az --version 2>$null
if (-not $azureCliVersion) {
    Write-Error "Azure CLI is not installed. Please install it from https://aka.ms/installazurecliwindows"
    exit 1
}

# Check if .NET SDK is installed
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Error ".NET SDK is not installed. Please install it from https://dotnet.microsoft.com/download"
    exit 1
}

Write-Host "Prerequisites check passed." -ForegroundColor Green

# Step 2: Clean and build the solution
Write-Host "`nStep 2: Building the application..." -ForegroundColor Green

Set-Location -Path "..\GCCWebApp"

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release

# Restore NuGet packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

# Build the solution
Write-Host "Building solution..." -ForegroundColor Yellow
$buildResult = dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Please fix build errors before deploying."
    exit 1
}

# Step 3: Run tests if enabled
if ($RunTests) {
    Write-Host "`nStep 3: Running tests..." -ForegroundColor Green
    
    $testResult = dotnet test .\GCCWebApp.RoleBasedUI.Tests\GCCWebApp.RoleBasedUI.Tests.csproj --configuration Release --no-build --logger "console;verbosity=normal"
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Some tests failed. Review the test results above."
        $continue = Read-Host "Do you want to continue deployment? (y/n)"
        if ($continue -ne 'y') {
            Write-Host "Deployment cancelled." -ForegroundColor Red
            exit 1
        }
    }
    Write-Host "Tests completed." -ForegroundColor Green
} else {
    Write-Host "`nStep 3: Skipping tests (disabled)..." -ForegroundColor Yellow
}

# Step 4: Publish the application
Write-Host "`nStep 4: Publishing the application..." -ForegroundColor Green

$publishPath = ".\publish"
if (Test-Path $publishPath) {
    Remove-Item -Path $publishPath -Recurse -Force
}

dotnet publish .\GCCWebApp.csproj --configuration Release --output $publishPath
if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed."
    exit 1
}

# Step 5: Login to Azure
Write-Host "`nStep 5: Logging in to Azure..." -ForegroundColor Green

$loginStatus = az account show 2>$null
if (-not $loginStatus) {
    Write-Host "Please login to Azure..." -ForegroundColor Yellow
    az login
}

# Select the correct subscription
$subscription = az account show --query "name" -o tsv
Write-Host "Using subscription: $subscription" -ForegroundColor Yellow

# Step 6: Create deployment package
Write-Host "`nStep 6: Creating deployment package..." -ForegroundColor Green

$zipPath = ".\deploy.zip"
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}

Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -Force
Write-Host "Deployment package created: $zipPath" -ForegroundColor Green

# Step 7: Deploy to Azure App Service
Write-Host "`nStep 7: Deploying to Azure App Service..." -ForegroundColor Green

# Check if the app service exists
$appExists = az webapp show --name $AppName --resource-group $ResourceGroup 2>$null
if (-not $appExists) {
    Write-Error "App Service '$AppName' does not exist in resource group '$ResourceGroup'"
    exit 1
}

# Deploy the application
Write-Host "Deploying application to $AppName..." -ForegroundColor Yellow
$deployResult = az webapp deploy --resource-group $ResourceGroup --name $AppName --src-path $zipPath --type zip

if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed."
    exit 1
}

# Step 8: Update app settings
Write-Host "`nStep 8: Updating application settings..." -ForegroundColor Green

# Set environment variable
az webapp config appsettings set --resource-group $ResourceGroup --name $AppName --settings ASPNETCORE_ENVIRONMENT=$Environment

# If KeyVault is specified, configure managed identity
if ($KeyVaultName) {
    Write-Host "Configuring Key Vault access..." -ForegroundColor Yellow
    
    # Enable system-assigned managed identity
    $identity = az webapp identity assign --resource-group $ResourceGroup --name $AppName --query principalId -o tsv
    
    # Grant Key Vault access
    az keyvault set-policy --name $KeyVaultName --object-id $identity --secret-permissions get list
}

# Step 9: Run database migrations if enabled
if ($RunMigrations -and $ConnectionString) {
    Write-Host "`nStep 9: Running database migrations..." -ForegroundColor Green
    
    # Install EF Core tools if not already installed
    dotnet tool install --global dotnet-ef 2>$null
    
    # Run migrations
    $env:ConnectionStrings__DefaultConnection = $ConnectionString
    dotnet ef database update --project .\GCCWebApp.csproj
    
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Database migration failed. Please run migrations manually."
    } else {
        Write-Host "Database migrations completed successfully." -ForegroundColor Green
    }
} else {
    Write-Host "`nStep 9: Skipping database migrations..." -ForegroundColor Yellow
}

# Step 10: Restart the app service
Write-Host "`nStep 10: Restarting App Service..." -ForegroundColor Green
az webapp restart --resource-group $ResourceGroup --name $AppName

# Step 11: Health check
Write-Host "`nStep 11: Performing health check..." -ForegroundColor Green

Start-Sleep -Seconds 30  # Wait for app to start

$appUrl = "https://$AppName.azurewebsites.net"
Write-Host "Checking application health at $appUrl/health..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "$appUrl/health" -UseBasicParsing -TimeoutSec 30
    if ($response.StatusCode -eq 200) {
        Write-Host "Health check passed!" -ForegroundColor Green
    } else {
        Write-Warning "Health check returned status code: $($response.StatusCode)"
    }
} catch {
    Write-Warning "Health check failed: $_"
    Write-Warning "Please check the application manually at: $appUrl"
}

# Step 12: Clean up
Write-Host "`nStep 12: Cleaning up..." -ForegroundColor Green

Remove-Item -Path $zipPath -Force
Remove-Item -Path $publishPath -Recurse -Force

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Deployment Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroup" -ForegroundColor Green
Write-Host "App Name: $AppName" -ForegroundColor Green
Write-Host "App URL: $appUrl" -ForegroundColor Green
Write-Host "Deployment Status: SUCCESS" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`nDeployment completed successfully!" -ForegroundColor Green
Write-Host "You can access your application at: $appUrl" -ForegroundColor Yellow