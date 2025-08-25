# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## GCC Database Solution - ASP.NET Core 8.0 Web Application

### Build and Run Commands

```bash
# Quick Start (from GCCWebApp folder)
dotnet restore              # Restore NuGet packages
dotnet build                # Build the application
dotnet run                  # Run on http://localhost:5101

# Database Operations
dotnet ef database update   # Apply migrations to database
dotnet ef migrations add [MigrationName]  # Create new migration
dotnet ef database drop     # Drop database (use with caution)

# Run with specific environment
dotnet run --environment Development
dotnet run --environment Production
```

### Test Commands

```bash
# Unit Tests (from solution root)
dotnet test GCCWebApp.Tests

# Integration Tests with Coverage
dotnet test GCCWebApp.Tests --collect:"XPlat Code Coverage"

# UI Tests (requires Playwright)
pwsh GCCWebApp.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install  # First time only
dotnet test GCCWebApp.UAT.Tests

# Run specific test
dotnet test --filter "FullyQualifiedName~CertificateControllerTests"
```

### Mock Data Generation

```bash
# Generate mock data (from Scripts folder)
RunMockDataGeneration.cmd           # Large dataset (1000 certificates)
RunMockDataGeneration.cmd small     # Small dataset (300 certificates)
RunMockDataGeneration.cmd reset     # Clear and regenerate
```

## High-Level Architecture

### Core Components and Their Relationships

The application follows a **Clean Architecture** pattern with clear separation of concerns:

1. **Controllers** (`/Controllers`) - Handle HTTP requests and coordinate between services
   - `CertificateController` - Core business logic for certificate management
   - `AdminController` - Administrative functions and user management
   - `AccountController` - Authentication and user account operations
   - Controllers use dependency injection to access services and repositories

2. **Service Layer** (`/Services`) - Business logic implementation
   - `AdminService` - User/role management, company operations
   - `CertificateService` - Certificate business rules and workflows
   - `FilterService` - Advanced search and filtering logic
   - `ValidationService` - Business rule validation
   - `TwoFactorService` - 2FA implementation
   - Services coordinate between repositories and apply business rules

3. **Repository Layer** (`/Repositories`) - Data access abstraction
   - `ICertificateRepository` / `CertificateRepository` - Certificate data operations
   - Repositories use Entity Framework Core for database operations
   - Unit of Work pattern for transaction management

4. **Data Context** (`/Data`)
   - `ApplicationDbContext` - EF Core database context
   - `SeedData` - Initial database population
   - Manages 25+ entity types with complex relationships

5. **Authentication & Authorization**
   - **Primary**: Azure AD B2C integration (`AzureAdB2C` configuration)
   - **Fallback**: Cookie authentication for local development
   - **Roles**: Admin, Analyst, External, Internal, Public
   - Policy-based authorization throughout controllers

### Critical Integration Points

1. **Database Connectivity**
   - **Development**: SQLite database (`gcc_database.db`)
   - **Production**: SQL Server via connection string
   - Entity Framework Core manages all database operations
   - Migrations track schema changes

2. **Authentication Flow**
   - Azure AD B2C handles external authentication
   - Falls back to cookie auth when B2C not configured
   - User context resolved via `HttpContext.User`
   - Role-based access control enforced at controller level

3. **Certificate Export System** (Currently Broken - Priority Fix)
   - `CertificateController.DownloadCertificate()` at line 598
   - Returns mock text instead of PDF - needs PDF generation service
   - CSV export partially implemented

4. **Search and Filter Pipeline**
   - `FilterService` processes multi-criteria searches
   - Currently disconnected from database queries
   - Requires fixing LINQ query generation

### Known Critical Issues

1. **PDF Generation** (`CertificateController.cs:598`)
   - Currently returns: `Encoding.UTF8.GetBytes($"Certificate PDF for {certificate.Number}")`
   - Needs: Actual PDF generation service implementation

2. **User Context** (`CertificateController.cs:571-575`)
   - `GetCurrentUserId()` returns null
   - Breaks audit trails and user tracking

3. **Database Integration**
   - Many controllers return hardcoded/mock data
   - Search/filter functions not connected to database
   - Export functions need database query implementation

### Configuration Files

- `appsettings.json` - Primary configuration
- `appsettings.Development.json` - Development overrides
- Connection strings, Azure AD B2C settings, JWT configuration
- Environment-specific settings for different deployment scenarios

### Important Patterns and Conventions

1. **Dependency Injection** - All services registered in `Program.cs`
2. **Repository Pattern** - Data access through repository interfaces
3. **Policy-Based Authorization** - Policies defined in Program.cs
4. **ViewModels** - Separate models for views vs domain entities
5. **Async/Await** - Used throughout for database operations

### Current Project Status

- **Architecture**: ✅ Excellent (Clean, scalable design)
- **UI/UX**: ✅ Professional (Responsive Bootstrap design)
- **Core Features**: 🟡 85% Complete
- **Critical Gaps**: PDF export, search functionality, data integration
- **Timeline to Production**: 2-4 weeks with focused development

### Test User Credentials

```
Admin: admin@greencirclecertified.com / admin123
Analyst: analyst@greencirclecertified.com / analyst123
External: partner@amazon.com / partner123
```

### Development Notes

- Remember to update test scenarios when implementing new features
- Create documentation for new functionality as it's developed
- The application has comprehensive documentation in `/Documentation` folder
- Status reports available: PROJECT_COMMITTEE_STATUS_REPORT.md, EXECUTIVE_STATUS_REPORT.md