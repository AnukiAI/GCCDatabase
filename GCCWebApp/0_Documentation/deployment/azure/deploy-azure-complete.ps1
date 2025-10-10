# ============================================
# Azure Complete Migration Script
# ============================================
# Deploys both App Service + Azure SQL Database
# Run this from PowerShell with admin privileges

param(
    [string]$resourceGroup = "GCCWebApp-RG",
    [string]$location = "eastus",
    [string]$appServicePlan = "GCCWebApp-Plan",
    [string]$webAppName = "gccwebapp-prod",
    [string]$sqlServerName = "gccwebapp-sql-server",
    [string]$sqlDatabaseName = "gccwebapp-db",
    [string]$sqlAdminUser = "gccadmin",
    [string]$sqlAdminPassword = "",
    [string]$appServiceSku = "B1",
    [string]$sqlDatabaseSku = "Basic"
)

# Functions
function Write-Step { param([string]$message); Write-Host "`n$('='*60)" -ForegroundColor Cyan; Write-Host $message -ForegroundColor Yellow; Write-Host $('='*60) -ForegroundColor Cyan }
function Write-Success { param([string]$message); Write-Host "[OK] $message" -ForegroundColor Green }
function Write-Error-Custom { param([string]$message); Write-Host "[ERROR] $message" -ForegroundColor Red }
function Write-Info { param([string]$message); Write-Host "[INFO] $message" -ForegroundColor Cyan }

# Pre-flight checks
Write-Step "Pre-flight Checks"
try { $null = az --version; Write-Success "Azure CLI installed" } catch { Write-Error-Custom "Install Azure CLI first!"; exit 1 }
$account = az account show 2>$null; if (-not $account) { az login | Out-Null }
Write-Success "Logged in to Azure"

# Get SQL password
if (-not $sqlAdminPassword) { $securePassword = Read-Host "Enter SQL Admin Password (complex password required)" -AsSecureString; $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword); $sqlAdminPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR) }

# Create resources
Write-Step "Creating Azure Resources"
az group create --name $resourceGroup --location $location | Out-Null; Write-Success "Resource group created"
az appservice plan create --name $appServicePlan --resource-group $resourceGroup --location $location --sku $appServiceSku --is-linux false | Out-Null; Write-Success "App Service Plan created ($appServiceSku - ~`$13/mo)"
az webapp create --name $webAppName --resource-group $resourceGroup --plan $appServicePlan --runtime "DOTNET|8.0" | Out-Null; $appUrl = az webapp show --name $webAppName --resource-group $resourceGroup --query defaultHostName --output tsv; Write-Success "Web App: https://$appUrl"
az sql server create --name $sqlServerName --resource-group $resourceGroup --location $location --admin-user $sqlAdminUser --admin-password $sqlAdminPassword | Out-Null; Write-Success "SQL Server created"
az sql server firewall-rule create --resource-group $resourceGroup --server $sqlServerName --name AllowAzure --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 | Out-Null
try { $myIp = (Invoke-WebRequest "https://api.ipify.org" -UseBasicParsing).Content.Trim(); az sql server firewall-rule create --resource-group $resourceGroup --server $sqlServerName --name MyIP --start-ip-address $myIp --end-ip-address $myIp | Out-Null; Write-Info "Your IP ($myIp) added to firewall" } catch {}
az sql db create --resource-group $resourceGroup --server $sqlServerName --name $sqlDatabaseName --service-objective $sqlDatabaseSku --backup-storage-redundancy Local | Out-Null; Write-Success "Database created ($sqlDatabaseSku - ~`$5/mo)"

# Configure app
Write-Step "Configuring App Settings"
$sqlServerFqdn = az sql server show --name $sqlServerName --resource-group $resourceGroup --query fullyQualifiedDomainName --output tsv
$connectionString = "Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};User ID=${sqlAdminUser};Password=${sqlAdminPassword};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;"
az webapp config appsettings set --name $webAppName --resource-group $resourceGroup --settings ASPNETCORE_ENVIRONMENT=Production GCC_JWT_SECRET="5T8hr4WpKKmqqDIHBzFW7OzfYdStIzuvKEZJYwQaSkfe5ADFryPGJHGAuxPCECnuo09ZI8qiLqURyVT1DR1NtQ==" | Out-Null
az webapp config connection-string set --name $webAppName --resource-group $resourceGroup --connection-string-type SQLServer --settings DefaultConnection="$connectionString" | Out-Null
az webapp log config --name $webAppName --resource-group $resourceGroup --application-logging filesystem --detailed-error-messages true --web-server-logging filesystem | Out-Null
az webapp config set --name $webAppName --resource-group $resourceGroup --always-on true | Out-Null; Write-Success "App configured"

# Build and deploy
Write-Step "Building and Deploying Application"
dotnet clean | Out-Null; dotnet publish -c Release -o ./publish
$publishZip = ".\publish.zip"; if (Test-Path $publishZip) { Remove-Item $publishZip -Force }
Compress-Archive -Path ./publish/* -DestinationPath $publishZip -Force; Write-Info "Package created"
az webapp deploy --name $webAppName --resource-group $resourceGroup --src-path $publishZip --type zip --timeout 300 | Out-Null; Write-Success "App deployed!"

# Results
Write-Step "DEPLOYMENT COMPLETE!"
$appIp = az webapp show --name $webAppName --resource-group $resourceGroup --query inboundIpAddress --output tsv
Write-Host "`nApp URL:      https://$appUrl" -ForegroundColor Cyan
Write-Host "Health:       https://$appUrl/health" -ForegroundColor White
Write-Host "Certificates: https://$appUrl/Certificate/CertificateTileView" -ForegroundColor White
Write-Host "`nSQL Server:   $sqlServerFqdn" -ForegroundColor Cyan
Write-Host "Database:     $sqlDatabaseName" -ForegroundColor White
Write-Host "Admin User:   $sqlAdminUser" -ForegroundColor White
Write-Host "`nFor Custom Domain (DNS):" -ForegroundColor Yellow
Write-Host "  A record:    $appIp" -ForegroundColor White
Write-Host "  CNAME:       $appUrl" -ForegroundColor White
Write-Host "`nNEXT STEPS:" -ForegroundColor Green
Write-Host "1. Migrate database from AccuWebHosting (see AZURE_COMPLETE_MIGRATION.md Part 2)" -ForegroundColor White
Write-Host "2. Test app: https://$appUrl" -ForegroundColor White
Write-Host "3. Configure custom domain & SSL" -ForegroundColor White
Write-Host "4. Run stable 24-48hrs before canceling AccuWebHosting" -ForegroundColor White
Write-Host "`nCost: ~`$18/month (App `$13 + DB `$5)" -ForegroundColor Yellow
