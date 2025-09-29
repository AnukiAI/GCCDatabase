# GCC Web Application - Project Configuration

## Project Overview
GreenCircle Certified (GCC) Database Web Application - An ASP.NET Core MVC application for managing environmental certifications and certificates.

**Technology Stack:**
- ASP.NET Core 8.0 MVC
- Entity Framework Core 9.0.6
- Microsoft Identity Web 3.10.0
- SQL Server (LocalDB for development)
- Bootstrap CSS Framework
- jQuery

## Development Environment

### Prerequisites
- .NET 8.0 SDK (currently using 8.0.400, 9.0.102, or 9.0.203)
- SQL Server LocalDB
- Visual Studio or VS Code

### Build & Run Commands

```bash
# Restore packages
dotnet restore

# Build project (with warnings - see Known Issues)
dotnet build

# Run development server
dotnet run

# Run with HTTPS (default)
dotnet run --launch-profile https
```

**Development URLs:**
- HTTPS: https://localhost:7130
- HTTP: http://localhost:5101

### Database Configuration

**Connection String:** `Server=(localdb)\\mssqllocaldb;Database=GCCDatabase;Trusted_Connection=true;MultipleActiveResultSets=true`

**Note:** Database may not be fully initialized. SqlCmd access currently fails due to authentication issues.

## Project Structure

### Key Directories
- `Controllers/` - MVC Controllers (Admin, Certificate, Company, Product, Account, Dashboard, Home)
- `Models/` - Data models and ViewModels
- `Views/` - Razor views organized by controller
- `wwwroot/` - Static files (CSS, JavaScript, images)
- `Services/` - Business logic services (excluded from compilation)
- `docs/` - Project documentation and design files
- `pages/` - Static HTML mockups/prototypes

### Authentication & Authorization

**Current Setup:**
- Cookie-based authentication for testing
- Role-based authorization policies:
  - `AdminOnly` - Admin role required
  - `AnalystOrAdmin` - Admin or Analyst roles
  - `ExternalAccess` - External, Admin, or Analyst roles
  - `AuthenticatedUsers` - Any authenticated user

**Login Path:** `/Account/Login`

### Known Issues

1. **Build Warnings:** 58+ nullable reference type warnings in Models (CS8618)
   - Properties not initialized in constructors
   - Consider adding `required` modifier or nullable types

2. **Database Access:** Authentication issues with LocalDB connection

3. **Services Directory:** Currently excluded from compilation

### Configuration Files

- `appsettings.json` - Main configuration with Azure AD setup (test values)
- `appsettings.Development.json` - Development logging configuration
- `launchSettings.json` - Development server profiles

### Recent Changes (Git Status)
- Multiple controller and view modifications
- CSS and JavaScript updates
- Cleanup of legacy files and documentation
- New Account controller and authentication models added

## Development Notes

- Project appears to be in active development with extensive documentation and wireframes
- Contains both internal and external certificate views
- Implements comprehensive admin panel functionality
- Uses session management for user state
- Security headers configured for production deployment

## Next Steps

1. Fix nullable reference warnings in Models
2. Initialize/migrate database schema
3. Configure proper Azure AD authentication
4. Re-enable Services directory if needed
5. Address any remaining build warnings