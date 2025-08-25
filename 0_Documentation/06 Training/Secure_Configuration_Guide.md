# 🔐 Secure Configuration Management Guide

## Overview

This guide covers the implementation of secure configuration management for the GCC Web Application, including Azure Key Vault integration, environment variable management, and security best practices.

## 🎯 Key Features

- **Azure Key Vault Integration**: Seamless integration with Azure Key Vault for production secrets
- **Environment Variable Support**: Flexible configuration using environment variables
- **Development Fallbacks**: Safe defaults for development environments
- **Startup Validation**: Comprehensive validation of configuration on application startup
- **Health Monitoring**: Built-in health checks for configuration status
- **Security Logging**: Detailed logging of configuration security status

## 📋 Prerequisites

### Development Environment
- No special setup required - the system provides safe development fallbacks
- Optional: Set environment variables for testing production-like configurations

### Production Environment
- Azure Key Vault instance
- Azure AD service principal or managed identity
- Properly configured environment variables

## 🛠️ Setup Instructions

### 1. Azure Key Vault Setup (Production)

#### Create Azure Key Vault
```bash
# Create Resource Group
az group create --name rg-gcc-webapp-prod --location eastus

# Create Key Vault
az keyvault create \
  --name kv-gcc-webapp-prod \
  --resource-group rg-gcc-webapp-prod \
  --location eastus \
  --sku standard
```

#### Configure Service Principal
```bash
# Create service principal
az ad sp create-for-rbac \
  --name "sp-gcc-webapp-keyvault" \
  --role "Key Vault Secrets User" \
  --scopes /subscriptions/{subscription-id}/resourceGroups/rg-gcc-webapp-prod/providers/Microsoft.KeyVault/vaults/kv-gcc-webapp-prod
```

#### Add Secrets to Key Vault
```bash
# JWT Secret Key (minimum 64 characters)
az keyvault secret set \
  --vault-name kv-gcc-webapp-prod \
  --name "GCC-Jwt-Key" \
  --value "your-256-bit-jwt-secret-key-for-production-must-be-at-least-64-characters-long"

# Azure AD B2C Client Secret
az keyvault secret set \
  --vault-name kv-gcc-webapp-prod \
  --name "GCC-AzureAdB2C-ClientSecret" \
  --value "your-azure-ad-b2c-client-secret"

# Database Connection String
az keyvault secret set \
  --vault-name kv-gcc-webapp-prod \
  --name "GCC-ConnectionStrings-DefaultConnection" \
  --value "Server=prod-server;Database=GCCDatabase;Trusted_Connection=true"
```

### 2. Environment Variables

#### Required Production Environment Variables
```bash
# Azure Key Vault Configuration
AZURE_KEY_VAULT_URL=https://kv-gcc-webapp-prod.vault.azure.net/
AZURE_CLIENT_ID=your-service-principal-client-id
AZURE_CLIENT_SECRET=your-service-principal-client-secret
AZURE_TENANT_ID=your-azure-tenant-id

# Alternative: Use Managed Identity (recommended for Azure deployments)
AZURE_CLIENT_ID=your-managed-identity-client-id
# No client secret needed with managed identity
```

#### Development Environment Variables (Optional)
```bash
# Override specific secrets for development
GCC_JWT_SECRET_KEY=dev-jwt-secret-key-for-local-development-only-must-be-64-chars
GCC_AZURE_AD_B2C_CLIENT_SECRET=dev-client-secret
GCC_DATABASE_CONNECTION_STRING=Data Source=gcc_database_dev.db
```

### 3. Application Configuration

#### Update appsettings.json
The application will automatically resolve placeholders like `{JWT_SECRET_KEY}` using the secure configuration service.

#### Environment-Specific Configuration Files
- `appsettings.Development.json` - Development settings with placeholders
- `appsettings.Production.json` - Production settings with secure placeholders
- `appsettings.Security.json` - Additional security-specific configurations

## 🔒 Secret Management

### Supported Secret Types

| Secret Type | Key Vault Name | Environment Variable | Configuration Path |
|-------------|----------------|---------------------|-------------------|
| JWT Secret | `GCC-Jwt-Key` | `GCC_JWT_SECRET_KEY` | `Jwt:Key` |
| Azure AD B2C Client Secret | `GCC-AzureAdB2C-ClientSecret` | `GCC_AZURE_AD_B2C_CLIENT_SECRET` | `AzureAdB2C:ClientSecret` |
| Database Connection | `GCC-ConnectionStrings-DefaultConnection` | `GCC_DATABASE_CONNECTION_STRING` | `ConnectionStrings:DefaultConnection` |
| SMTP Password | `GCC-EmailConfiguration-Password` | `GCC_SMTP_PASSWORD` | `EmailConfiguration:Password` |

### Resolution Priority

1. **Azure Key Vault** (Production only)
2. **Environment Variables**
3. **Development Fallbacks** (Development only)
4. **Configuration Files** (Placeholder values)

## 🏃‍♂️ Usage Examples

### Basic Usage in Code
```csharp
// Inject the secure configuration service
public class MyController : Controller
{
    private readonly ISecureConfigurationService _secureConfig;
    
    public MyController(ISecureConfigurationService secureConfig)
    {
        _secureConfig = secureConfig;
    }
    
    public IActionResult MyAction()
    {
        // Get secure configuration values
        var connectionString = _secureConfig.GetConnectionString("DefaultConnection");
        var jwtKey = _secureConfig.GetSecret("Jwt:Key");
        
        // Values are automatically resolved from Key Vault, environment variables, or fallbacks
        return View();
    }
}
```

### Health Check Integration
```csharp
// The health check is automatically registered
// Access via: /health
// Returns:
// - Healthy: All secrets properly configured
// - Degraded: Some secrets using fallbacks (development only)
// - Unhealthy: Missing required secrets in production
```

## 🚀 Deployment Scenarios

### Azure App Service Deployment

```bash
# Set application settings in Azure App Service
az webapp config appsettings set \
  --name your-app-name \
  --resource-group your-resource-group \
  --settings \
    AZURE_KEY_VAULT_URL=https://kv-gcc-webapp-prod.vault.azure.net/ \
    ASPNETCORE_ENVIRONMENT=Production
```

### Docker Deployment

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "GCCWebApp.dll"]
```

```bash
# Docker run with environment variables
docker run -d \
  -e AZURE_KEY_VAULT_URL=https://kv-gcc-webapp-prod.vault.azure.net/ \
  -e AZURE_CLIENT_ID=your-client-id \
  -e AZURE_CLIENT_SECRET=your-client-secret \
  -e AZURE_TENANT_ID=your-tenant-id \
  -e ASPNETCORE_ENVIRONMENT=Production \
  your-app-image
```

### Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: gcc-webapp
spec:
  template:
    spec:
      containers:
      - name: gcc-webapp
        image: your-registry/gcc-webapp:latest
        env:
        - name: AZURE_KEY_VAULT_URL
          value: "https://kv-gcc-webapp-prod.vault.azure.net/"
        - name: AZURE_CLIENT_ID
          valueFrom:
            secretKeyRef:
              name: gcc-webapp-secrets
              key: azure-client-id
        - name: AZURE_CLIENT_SECRET
          valueFrom:
            secretKeyRef:
              name: gcc-webapp-secrets
              key: azure-client-secret
```

## 🔍 Monitoring and Troubleshooting

### Configuration Validation Logs
```
🔍 Starting secure configuration validation...
✅ Secret 'JWT_SECRET_KEY' is properly configured
✅ Secret 'AZURE_AD_B2C_CLIENT_SECRET' is properly configured
⚠️  Placeholder detected for: DATABASE_CONNECTION_STRING
✅ Secure configuration validation passed
```

### Health Check Monitoring
- **Endpoint**: `/health`
- **Status**: Healthy/Degraded/Unhealthy
- **Details**: Configuration validation results

### Common Issues and Solutions

#### Issue: "Azure Key Vault configuration failed"
```
Solution:
1. Verify AZURE_KEY_VAULT_URL environment variable
2. Check Azure AD permissions for the service principal/managed identity
3. Ensure Key Vault exists and is accessible
4. Validate network connectivity to Key Vault
```

#### Issue: "Required secrets missing in production"
```
Solution:
1. Check all required secrets are added to Key Vault with correct naming
2. Verify environment variables are properly set
3. Review Key Vault access policies
4. Check application logs for specific missing secrets
```

#### Issue: "JWT key must be at least 64 characters long"
```
Solution:
1. Generate a secure 256-bit (32+ character) JWT key
2. Use a cryptographically secure random generator
3. Update the secret in Key Vault or environment variable
```

## 🛡️ Security Best Practices

### Key Management
1. **Use Azure Key Vault for production secrets**
2. **Rotate secrets regularly (90-day maximum)**
3. **Use managed identities when possible**
4. **Never commit secrets to source control**
5. **Use separate Key Vaults per environment**

### Access Control
1. **Principle of least privilege**
2. **Separate service principals per environment**
3. **Regular access reviews**
4. **Enable Key Vault logging and monitoring**

### Development Security
1. **Use development-specific fallbacks**
2. **Never use production secrets in development**
3. **Sanitize connection strings in logs**
4. **Enable configuration validation on startup**

## 📊 Configuration Status Dashboard

The application provides a comprehensive configuration dashboard accessible via:
- **Health Check Endpoint**: `/health`
- **Application Startup Logs**: Detailed validation results
- **Structured Logging**: Security-focused log entries

### Example Status Output
```
=== Configuration Security Status ===
Environment: Production
Production Mode: True
✓ Configured: JWT_SECRET_KEY
✓ Configured: AZURE_AD_B2C_CLIENT_SECRET
✓ Configured: DATABASE_CONNECTION_STRING
Secrets Status: 3/3 configured properly
=====================================
```

## 🔄 Migration from Legacy Configuration

### Step 1: Identify Current Secrets
Review existing configuration files for placeholder values:
- `{JWT_SECRET_KEY}`
- `{AZURE_AD_B2C_CLIENT_SECRET}`
- `{DATABASE_CONNECTION_STRING}`

### Step 2: Create Secure Alternatives
1. Generate new production-ready secrets
2. Add to Azure Key Vault with proper naming
3. Configure environment variables

### Step 3: Deploy and Validate
1. Deploy with secure configuration enabled
2. Monitor startup logs for validation results
3. Verify health check status
4. Test application functionality

This secure configuration system ensures that sensitive information is properly managed while maintaining development productivity and operational security.