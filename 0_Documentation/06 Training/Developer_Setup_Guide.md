# GCC Database Web Application - Developer Setup Guide

## 🚀 QUICK START (1 Minute Setup)
**For immediate development with full functionality:**

1. **Clone and navigate to the project**:
   ```bash
   cd "GCC Database Solution\GCCWebApp\GCCWebApp"
   ```

2. **Apply database migrations and run**:
   ```bash
   dotnet ef database update  # Apply latest admin system migrations
   dotnet run
   ```

3. **Run tests** (optional):
   ```bash
   # Unit Tests
   dotnet test ../GCCWebApp.Tests
   
   # UAT Tests
   dotnet test ../GCCWebApp.UAT.Tests
   
   # E2E Tests (requires Playwright)
   pwsh ../GCCWebApp.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install  # First time only
   dotnet test ../GCCWebApp.E2E.Tests
   ```

4. **Access the application**:
   - Open browser to: http://localhost:5101
   - Use these test credentials for login:
     - **Admin**: admin@greencirclecertified.com / admin123
     - **Analyst**: analyst@greencirclecertified.com / analyst123
     - **External**: partner@amazon.com / partner123

✅ **That's it!** The application includes:
- ✅ Pre-configured SQLite database with 200+ sample certificates
- ✅ Automatic database seeding on first run
- ✅ Full authentication system with test users
- ✅ Complete certificate management functionality
- ✅ Full admin system with database integration
- ✅ User and role management with permissions
- ✅ Modern, responsive Account pages with GreenCircle branding
- ✅ **NEW**: Complete REST API with OpenAPI/Swagger documentation
- ✅ **NEW**: Advanced validation system with business rules
- ✅ **NEW**: Secure file upload system for certificates
- ✅ **NEW**: Enhanced error handling and logging
- ✅ **NEW**: Comprehensive test suite (Unit, UAT, and E2E tests)

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [Project Setup](#project-setup)
4. [Database Configuration](#database-configuration)
5. [Authentication Setup](#authentication-setup)
6. [Running the Application](#running-the-application)
7. [Development Tools](#development-tools)
8. [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software
- **Operating System**: Windows 10/11, macOS 10.15+, or Linux (Ubuntu 20.04+)
- **.NET SDK**: Version 8.0 or higher ✅ **CONFIRMED WORKING**
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify installation: `dotnet --version`
- **Git**: Version 2.30 or higher
  - Download: https://git-scm.com/
  - Verify installation: `git --version`
- **Database**: 
  - **Development**: SQLite ✅ **FULLY CONFIGURED** (no installation required - embedded)
  - **Production**: SQL Server 2019+ or Azure SQL Database
  - **Alternative**: SQL Server LocalDB for local testing

### ⚠️ IMPORTANT: Current System Status
The application is now **FULLY OPERATIONAL** with:
- ✅ Complete database integration (Entity Framework Core 9.0)
- ✅ Comprehensive service layer implementation including AdminService
- ✅ 200+ sample certificates with realistic data
- ✅ Working authentication (Azure AD B2C + Cookie fallback)
- ✅ Advanced search and filtering capabilities
- ✅ Full admin system with all management functions
- ✅ BCrypt.Net password hashing for security
- ✅ Complete UI redesign with modern, responsive Account pages ✅ **NEW**
- ✅ Enhanced user experience with GreenCircle branding ✅ **NEW**
- ✅ Interactive authentication flows with improved accessibility ✅ **NEW**

### Optional Software
- **Node.js**: Version 18.x or higher (for advanced front-end tooling)
  - Download: https://nodejs.org/
  - Note: Not required for basic development
- **Azure CLI**: For Azure AD B2C configuration
  - Download: https://docs.microsoft.com/cli/azure/install-azure-cli

### Recommended IDEs
- **Visual Studio 2022** (Community Edition or higher)
  - Download: https://visualstudio.microsoft.com/
  - Required Workloads:
    - ASP.NET and web development
    - .NET desktop development
    - Azure development
- **Visual Studio Code** (Alternative)
  - Download: https://code.visualstudio.com/
  - Required Extensions:
    - C# (Microsoft)
    - C# Dev Kit
    - SQL Server (mssql)
    - Azure Tools

## Environment Setup

### 1. Clone the Repository
```bash
git clone [repository-url]
cd GCCWebApp
```

### 2. Install .NET Dependencies
```bash
dotnet restore
```

### 3. Install Node.js Dependencies
```bash
cd wwwroot
npm install
cd ..
```

### 4. Configure Environment Variables
The application includes sensible defaults and **zero-configuration setup**. You can run it immediately or create a custom `appsettings.Development.json` file:

**Option 1: Zero Configuration (Recommended for Quick Start)**
- Just run `dotnet run` - the application will:
  - Use embedded SQLite database
  - Auto-create database with sample data
  - Use cookie authentication for development

**Option 2: Custom Configuration**
Create `appsettings.Development.json` in the project root:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=gcc_database.db"
  },
  "AzureAdB2C": {
    "Instance": "https://your-tenant.b2clogin.com/",
    "ClientId": "{your-client-id}",
    "Domain": "your-tenant.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_signupsignin",
    "ResetPasswordPolicyId": "B2C_1_passwordreset", 
    "EditProfilePolicyId": "B2C_1_profileediting",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout/B2C_1_signupsignin"
  }
}
```

**Important Notes:**
- If Azure AD B2C is not configured (contains `{placeholders}`), app automatically uses cookie auth
- Database will be created automatically on first run
- Sample data is seeded automatically

## Project Setup

### Quick Start (Zero Configuration)
```bash
# 1. Clone and enter project directory
git clone [repository-url]
cd "GCC Database Solution/GCCWebApp/GCCWebApp"

# 2. Build and run (database will be created automatically)
dotnet build
dotnet run
```

**That's it!** The application will:
- Build successfully
- Create SQLite database automatically
- Seed with sample data (5 companies, 5 products, 5 certificates)
- Start on https://localhost:5001

### Manual Setup (Optional)

If you need custom configuration:

### 1. Build the Project
```bash
dotnet build
```

### 2. Database Setup (Automatic)
The application automatically handles:
- Database creation using Entity Framework Code First
- Migration application  
- Sample data seeding

**Manual Override** (if needed):
```bash
# Only if you want to use Entity Framework CLI tools
dotnet tool install --global dotnet-ef

# Create migration (already exists)
dotnet ef migrations add InitialMigration

# Apply migration (done automatically on startup)  
dotnet ef database update
```

### 3. Verify Sample Data
Sample data automatically includes:
- **5 Companies**: EcoFloor Solutions, GreenBuild Materials, Sustainable Systems, etc.
- **5 Products**: Each with category, descriptions, and certifications
- **5 Certificates**: Various types with realistic data and relationships
- **5 Certification Types**: CEF, Recycled Content, Sustainability Facts, etc.
- **Users and Roles**: Admin, Analyst, External, Public user types

## Database Configuration

The application uses **dual-database strategy**:

### Development Database: SQLite (Default - No Setup Required)
- **File**: `gcc_database.db` (created automatically in project root)
- **Benefits**: 
  - No installation required
  - Zero configuration 
  - Perfect for development and testing
  - Portable across platforms
- **Connection**: Automatic (handled in `appsettings.json`)

### Production Database: SQL Server or Azure SQL Database
- **Configuration**: Modified connection strings in production `appsettings.json`
- **Migration**: Automatic using Entity Framework migrations

### Alternative Local Databases (Optional)

**Option 1: SQL Server LocalDB**
```bash
# Verify LocalDB is available
sqllocaldb info

# Connection string in appsettings.Development.json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GCCDatabase_Dev;Trusted_Connection=true;"
}
```

**Option 2: Full SQL Server**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=GCCDatabase_Dev;Trusted_Connection=true;"
}
```

### Database Features Implemented
- **Automatic Creation**: Database file created on first run
- **Migration System**: Entity Framework Code First migrations
- **Seed Data**: Comprehensive sample data automatically loaded
- **Relationships**: Full foreign key constraints and navigation properties
- **Advanced Queries**: Optimized repository methods with Include() statements

## Authentication Setup

The application implements **dual-mode authentication** with automatic fallback:

### Development Authentication (Default)
**Zero Configuration Required!**
- Automatically uses **cookie authentication** for development
- Test user accounts available through `/Account/Login`
- Role-based access control fully functional
- Session management with secure cookies

**Test Accounts Available:**
- Admin: Any email ending with `@greencirclecertified.com` 
- Analyst: Any email ending with `@contractor.com` or `@analyst.com`
- External: Emails containing `partner` or ending with `@amazon.com`
- Public: All other email addresses

### Production Authentication (Azure AD B2C)
Only needed for production deployment:

**1. Create Azure AD B2C Tenant:**
   - Navigate to Azure Portal → Create Azure AD B2C resource
   - Note tenant name and domain

**2. Register Application:**
   - App registrations → New registration
   - Name: "GCC Database App"
   - Redirect URI: `https://your-domain.com/signin-oidc`
   - Note Application (Client) ID

**3. Configure User Flows:**
   - Sign-up/Sign-in policy: `B2C_1_signupsignin`
   - Password reset policy: `B2C_1_passwordreset`
   - Profile editing policy: `B2C_1_profileediting`

**4. Update Configuration:**
Replace placeholder values in `appsettings.json`:
```json
"AzureAdB2C": {
  "ClientId": "your-actual-client-id",
  "Domain": "your-tenant.onmicrosoft.com"
}
```

### Automatic Mode Detection
- Application detects valid Azure AD B2C configuration automatically
- If placeholders found (e.g., `{your-client-id}`), falls back to cookie auth
- No code changes needed between development and production

## Running the Application

### Development Server

1. **Using .NET CLI**:
```bash
dotnet run --project GCCWebApp.csproj
```

2. **Using Visual Studio**:
   - Open `GCCWebApp.sln`
   - Press F5 or click "Start Debugging"

3. **Using Visual Studio Code**:
   - Open project folder
   - Press F5 (ensure launch.json is configured)

### Access Points
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:5001
- **Swagger UI**: https://localhost:5001/swagger (if configured)

### Hot Reload
```bash
dotnet watch run
```

## Development Tools

### Entity Framework Core Tools

1. **Install EF Core Tools**:
```bash
dotnet tool install --global dotnet-ef
```

2. **Common EF Commands**:
```bash
# Add migration
dotnet ef migrations add [MigrationName]

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script
```

### Package Management

1. **Add NuGet Package**:
```bash
dotnet add package [PackageName]
```

2. **Update Packages**:
```bash
dotnet list package --outdated
dotnet add package [PackageName] --version [Version]
```

### Code Quality Tools

1. **Install Code Analysis**:
```bash
dotnet add package Microsoft.CodeAnalysis.NetAnalyzers
```

2. **Run Code Analysis**:
```bash
dotnet build /p:RunAnalyzersDuringBuild=true
```

## Troubleshooting

### Common Issues and Solutions

#### 1. Database Connection Issues
**Problem**: Cannot connect to database
**Solution**:
- Verify SQL Server service is running
- Check connection string in appsettings.json
- Ensure database exists
- Check firewall settings

#### 2. Certificate Trust Issues
**Problem**: HTTPS certificate not trusted
**Solution**:
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

#### 3. Port Already in Use
**Problem**: Port 5000/5001 already in use
**Solution**:
- Change ports in launchSettings.json
- Or find and stop the process using the port:
```bash
# Windows
netstat -ano | findstr :5000
taskkill /PID [ProcessId] /F

# Linux/Mac
lsof -i :5000
kill -9 [ProcessId]
```

#### 4. NuGet Restore Failures
**Problem**: Package restore failed
**Solution**:
```bash
dotnet nuget locals all --clear
dotnet restore --force
```

#### 5. Migration Issues
**Problem**: Migrations fail to apply
**Solution**:
- Check migration history: `dotnet ef migrations list`
- Reset database: `dotnet ef database drop --force`
- Reapply migrations: `dotnet ef database update`

### Debug Configuration

1. **Enable Detailed Logging**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

2. **Enable Developer Exception Page**:
Already configured in Program.cs for Development environment

3. **SQL Query Logging**:
Add to Program.cs:
```csharp
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
```

## REST API Development ✅ **NEW**

### API Testing with Built-in Swagger
1. **Start the application**: `dotnet run`
2. **Access Swagger UI**: http://localhost:5101/swagger
3. **API Base URL**: http://localhost:5101/api/certificates

### API Endpoints Overview
```http
# Public endpoints (no auth required)
GET /api/certificates              # List certificates with filtering
GET /api/certificates/{id}         # Get certificate by ID
GET /api/certificates/by-number/{number}  # Get certificate by number
GET /api/certificates/{id}/download      # Download certificate file

# Admin endpoints (Bearer token required)
POST /api/certificates             # Create new certificate
PUT /api/certificates/{id}         # Update certificate  
DELETE /api/certificates/{id}      # Delete certificate
POST /api/certificates/{id}/upload # Upload certificate file
```

### API Testing Examples

**1. List Certificates with Filtering:**
```bash
curl -X GET "http://localhost:5101/api/certificates?page=1&pageSize=5&sort=newest&filters.searchTerm=ASSA" \
  -H "accept: application/json"
```

**2. Get Specific Certificate:**
```bash
curl -X GET "http://localhost:5101/api/certificates/1?userRole=Admin" \
  -H "accept: application/json"
```

**3. Create Certificate (Admin Only):**
```bash
curl -X POST "http://localhost:5101/api/certificates" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "certificateNumber": "GCC-24-TEST",
    "status": "active",
    "effectiveDate": "2024-08-01T00:00:00",
    "expirationDate": "2027-07-31T00:00:00",
    "companyId": 1,
    "productId": 1,
    "certificationTypeId": 1
  }'
```

### File Upload Testing
The system supports secure file uploads with validation:

```bash
# Upload certificate PDF (Admin only)
curl -X POST "http://localhost:5101/api/certificates/1/upload" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -F "file=@certificate.pdf"
```

**Upload Requirements:**
- **File Types**: PDF only for certificates
- **Max Size**: 20MB for certificates, 5MB for images
- **Security**: Automatic virus scanning and path validation
- **Storage**: Files saved in `/wwwroot/uploads/certificates/`

### Service Architecture Testing

**1. Validation Service Testing:**
```csharp
// Test certificate validation
var validationService = serviceProvider.GetService<IValidationService>();
var result = await validationService.ValidateCertificateAsync(certificate);
Assert.True(result.IsValid);
```

**2. File Upload Service Testing:**  
```csharp
// Test file upload
var fileUploadService = serviceProvider.GetService<IFileUploadService>();
var result = await fileUploadService.UploadCertificateAsync(file, "GCC-24-001");
Assert.True(result.IsSuccess);
```

## Additional Resources

- **Project Documentation**: /Documentation folder  
- **API Documentation**: /Documentation/API_Reference.md ✅ **UPDATED**
- **OpenAPI/Swagger**: http://localhost:5101/swagger ✅ **NEW**
- **Database Schema**: /Documentation/Database_ERD_Updated.mermaid
- **Test Strategy**: /Documentation/GCC_Test_Strategy_and_Plan.md
- **Architecture Guide**: /Documentation/Architecture_Overview.md ✅ **UPDATED**

## Support

For development support:
- Check existing documentation in /Documentation folder
- Review GitHub issues for known problems
- Contact the development team lead

## Version History

- **v1.1.0** - Updated with new Account pages UI redesign ✅ **NEW**
  - Added information about modern authentication UI
  - Updated setup instructions for enhanced user experience
  - Documented new responsive design features
- **v1.0.0** - Initial setup guide
- Last Updated: August 2025