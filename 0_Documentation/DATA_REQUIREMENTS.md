# GCC Web Application - Data Requirements Specification
**Version:** 2.0
**Last Updated:** October 17, 2025
**Status:** Production Ready

## Table of Contents
1. [Overview](#overview)
2. [Data Source Requirements](#data-source-requirements)
3. [Database Schema Requirements](#database-schema-requirements)
4. [Data Loading Requirements](#data-loading-requirements)
5. [Environment-Specific Requirements](#environment-specific-requirements)
6. [Data Validation Requirements](#data-validation-requirements)
7. [Performance Requirements](#performance-requirements)
8. [Security Requirements](#security-requirements)

## Overview

### Purpose
This document defines the data requirements for the GCC Web Application, ensuring all certificate, company, and product data is properly sourced from SQL Server database with no hardcoded values.

### Scope
- All application environments (Development, Staging, Production)
- All data types (Certificates, Companies, Products, Users)
- All data access patterns (API, MVC Controllers, Services)

### Data Source Mandate
**REQUIREMENT DR-001**: All business data MUST be sourced from SQL Server database.
**REQUIREMENT DR-002**: NO hardcoded business data is permitted in application code.
**REQUIREMENT DR-003**: Test data MUST be seeded via database scripts or migrations.

---

## Data Source Requirements

### Primary Data Source

**Database Type:** Microsoft SQL Server
**Minimum Version:** SQL Server 2019
**Recommended Version:** SQL Server 2022

#### Development Environment
- **Server:** ANUKI01 (Local SQL Server Instance)
- **Database:** GCCDatabase_Dev
- **Authentication:** SQL Authentication
- **User:** GCC_DevUser
- **Password:** DevP@ss2024!SQL (stored securely)
- **Connection String:**
  ```
  Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false
  ```

#### Production Environment
- **Server:** 68.71.129.120,1533 (AccuWebHosting)
- **Database:** gccwebap_certificates
- **Authentication:** SQL Authentication
- **User:** GCC_adminxyz123
- **Password:** (stored in secure vault/environment variable)
- **Connection String:**
  ```
  Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password={from-vault};MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=true
  ```

### Entity Framework Core Configuration

**Provider:** Microsoft.EntityFrameworkCore.SqlServer
**Version:** 9.0.9+
**DbContext:** ApplicationDbContext
**Location:** `Data/ApplicationDbContext.cs`

**REQUIREMENT DR-004**: All database access MUST use Entity Framework Core with SQL Server provider.
**REQUIREMENT DR-005**: Direct SQL queries MUST only be used for performance-critical operations.

---

## Database Schema Requirements

### Core Entities

#### 1. Certificates Table
**Purpose:** Store all certificate records

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| Id | int | Yes | Primary Key, Auto-increment |
| Number | nvarchar(50) | Yes | Unique, Format: XX-XXXX |
| Status | nvarchar(20) | Yes | active, expired, pending, suspended, revoked |
| CompanyId | int | Yes | Foreign Key → Companies.Id |
| ProductId | int | Yes | Foreign Key → Products.Id |
| CertificationTypeId | int | Yes | Foreign Key → CertificationTypes.Id |
| EffectiveDate | datetime2 | Yes | Certificate start date |
| ExpirationDate | datetime2 | Yes | Certificate end date |
| Location | nvarchar(200) | No | City, State format |
| Country | nvarchar(100) | No | Full country name |
| Partner | nvarchar(50) | No | Amazon, Wayfair, None |
| CertificateFile | nvarchar(500) | No | Path to uploaded PDF |
| CertificateImagePath | nvarchar(500) | No | Path to certificate image |
| CreatedAt | datetime2 | Yes | Auto-generated |
| UpdatedAt | datetime2 | Yes | Auto-updated |
| CreatedBy | int | No | Foreign Key → Users.Id |
| UpdatedBy | int | No | Foreign Key → Users.Id |

**REQUIREMENT DR-006**: All certificates MUST have valid Company, Product, and CertificationType relationships.
**REQUIREMENT DR-007**: Certificate Numbers MUST be unique across all records.

#### 2. Companies Table
**Purpose:** Store company/organization information

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| Id | int | Yes | Primary Key, Auto-increment |
| Name | nvarchar(200) | Yes | Unique company name |
| Country | nvarchar(100) | No | Operating country |
| Location | nvarchar(200) | No | Primary location |
| Email | nvarchar(100) | No | Contact email |
| Website | nvarchar(200) | No | Company website |
| IsActive | bit | Yes | Default: true |
| CreatedAt | datetime2 | Yes | Auto-generated |
| UpdatedAt | datetime2 | Yes | Auto-updated |

**REQUIREMENT DR-008**: Company Names MUST be unique.
**REQUIREMENT DR-009**: All companies MUST have IsActive flag properly set.

#### 3. Products Table
**Purpose:** Store product information

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| Id | int | Yes | Primary Key, Auto-increment |
| Name | nvarchar(200) | Yes | Product name |
| Description | nvarchar(500) | No | Product description/category |
| Model | nvarchar(100) | No | Product model number |
| CategoryId | int | No | Foreign Key → ProductCategories.Id |
| CompanyId | int | No | Foreign Key → Companies.Id |
| IsActive | bit | Yes | Default: true |
| CreatedAt | datetime2 | Yes | Auto-generated |
| UpdatedAt | datetime2 | Yes | Auto-updated |

**REQUIREMENT DR-010**: Product Names should be unique within a company.

#### 4. CertificationTypes Table
**Purpose:** Define available certification types

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| Id | int | Yes | Primary Key, Auto-increment |
| Name | nvarchar(200) | Yes | Unique type name |
| Description | nvarchar(500) | No | Type description |
| IsActive | bit | Yes | Default: true |
| CreatedAt | datetime2 | Yes | Auto-generated |

**Standard Certification Types:**
- Certified Sustainability Facts (CSF)
- FSC Chain of Custody
- SFI Chain of Custody
- PEFC Chain of Custody
- Recycled Content
- Material Ingredient Reporting
- Green Building Standard

**REQUIREMENT DR-011**: Certification Type Names MUST be unique.
**REQUIREMENT DR-012**: At least one active certification type MUST exist in the database.

#### 5. CertificateProductIds Table
**Purpose:** Store multiple product IDs (ASINs, UPCs, etc.) per certificate

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| Id | int | Yes | Primary Key, Auto-increment |
| CertificateId | int | Yes | Foreign Key → Certificates.Id |
| ProductIdType | nvarchar(50) | Yes | ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |
| ProductIdValue | nvarchar(100) | Yes | Actual product ID value |
| IsPrimary | bit | Yes | Default: false |
| CreatedAt | datetime2 | Yes | Auto-generated |

**REQUIREMENT DR-013**: Each certificate can have 100+ product IDs.
**REQUIREMENT DR-014**: Product ID combinations (Type + Value) should be unique per certificate.

#### 6. Users Table
**Purpose:** Store user accounts

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| Id | int | Yes | Primary Key, Auto-increment |
| Email | nvarchar(256) | Yes | Unique user email |
| PasswordHash | nvarchar(max) | Yes | Hashed password |
| FirstName | nvarchar(100) | No | User first name |
| LastName | nvarchar(100) | No | User last name |
| IsActive | bit | Yes | Default: true |
| CreatedAt | datetime2 | Yes | Auto-generated |

**REQUIREMENT DR-015**: User emails MUST be unique.
**REQUIREMENT DR-016**: Passwords MUST be hashed using ASP.NET Core Identity.

#### 7. Roles & UserRoles Tables
**Purpose:** Define user roles and permissions

**Standard Roles:**
- Admin (Full access)
- Analyst (Read/Write certificates, no admin functions)
- External (Read-only, partner-specific data)

**REQUIREMENT DR-017**: Admin and Analyst roles MUST NOT be deletable.
**REQUIREMENT DR-018**: Each user MUST have at least one role.

---

## Data Loading Requirements

### Application Startup Data Loading

**REQUIREMENT DR-019**: Database MUST be seeded during first application startup.
**REQUIREMENT DR-020**: Data seeding MUST be idempotent (safe to run multiple times).

#### Data Seeding Process

1. **Check Database Existence**
   ```csharp
   await context.Database.EnsureCreatedAsync();
   ```

2. **Apply Migrations**
   ```csharp
   await context.Database.MigrateAsync();
   ```

3. **Seed Reference Data** (if empty)
   - Certification Types
   - Product Categories
   - Default Roles (Admin, Analyst, External)

4. **Seed Test Users** (Development only)
   - admin@greencirclecertified.com
   - analyst@greencirclecertified.com
   - partner@amazon.com

5. **Seed Sample Data** (Development only)
   - Sample Companies
   - Sample Products
   - Sample Certificates

**Location:** `Program.cs` → `SeedDevelopmentData()` method

### API Data Loading

**Endpoint:** `/api/certificate-tiles`
**Controller:** `CertificateTilesApiController.cs`
**Method:** `GetCertificates()`

**REQUIREMENT DR-021**: API MUST load data using Entity Framework queries.
**REQUIREMENT DR-022**: API responses MUST include pagination metadata.

#### Sample API Response Structure
```json
{
    "success": true,
    "certificates": [
        {
            "id": 85,
            "certificateNumber": "25-5057",
            "certificationType": "Material Ingredient Reporting",
            "companyName": "3M Company",
            "productName": "Security Exit Device - EX700",
            "effectiveDate": "2025-10-13",
            "expirationDate": "2027-10-13",
            "location": "Chicago, IL",
            "country": "United States",
            "status": "active"
        }
    ],
    "totalCount": 194,
    "loadedCount": 20,
    "hasMore": true
}
```

**REQUIREMENT DR-023**: All certificate data MUST come from database queries.
**REQUIREMENT DR-024**: NO certificate data may be hardcoded in API responses.

### Frontend Data Loading

**JavaScript File:** `wwwroot/js/certificate-tile-view.js`
**API Endpoint:** `/api/certificate-tiles`

**REQUIREMENT DR-025**: Frontend MUST fetch data via AJAX/Fetch API.
**REQUIREMENT DR-026**: Frontend MUST handle loading states and errors.
**REQUIREMENT DR-027**: Frontend MUST implement infinite scroll (no pagination).

#### Data Loading Flow
1. Page loads → Initialize state
2. Fetch initial certificates (page=1, pageSize=20)
3. Display certificates in tile view
4. On scroll to bottom → Fetch next page
5. Append new certificates to existing view
6. Continue until `hasMore: false`

---

## Environment-Specific Requirements

### Development Environment

**Database:** SQL Server (ANUKI01)
**Data Volume:** 50-200 test certificates
**Seeding:** Automatic on startup
**Features Enabled:**
- Test user accounts
- Sample data generation
- Debug logging
- Swagger UI

**REQUIREMENT DR-028**: Development MUST use SQL Server (not SQLite, not in-memory).
**REQUIREMENT DR-029**: Development data MAY be reset/reseeded.

### Staging Environment

**Database:** SQL Server (separate instance)
**Data Volume:** Production-like dataset
**Seeding:** Manual via scripts
**Features Enabled:**
- Production-like configuration
- Performance monitoring
- Real user accounts (test)

**REQUIREMENT DR-030**: Staging MUST mirror production schema.
**REQUIREMENT DR-031**: Staging data SHOULD be sanitized production data.

### Production Environment

**Database:** SQL Server (AccuWebHosting)
**Data Volume:** Full production dataset
**Seeding:** NEVER - data managed manually
**Features Enabled:**
- Production security
- Performance monitoring
- Real user accounts

**REQUIREMENT DR-032**: Production MUST NEVER use test data seeding.
**REQUIREMENT DR-033**: Production data MUST be backed up daily.
**REQUIREMENT DR-034**: Production SeedDatabase endpoint MUST be disabled.

---

## Data Validation Requirements

### Input Validation

**REQUIREMENT DR-035**: All user inputs MUST be validated before database insertion.
**REQUIREMENT DR-036**: Certificate numbers MUST match pattern: `XX-XXXX` or `XXXX`.
**REQUIREMENT DR-037**: Email addresses MUST be validated using regex.
**REQUIREMENT DR-038**: Dates MUST be validated (Expiration > Effective).

### Data Integrity

**REQUIREMENT DR-039**: Foreign key constraints MUST be enforced.
**REQUIREMENT DR-040**: Orphaned records MUST be prevented or cleaned up.
**REQUIREMENT DR-041**: Deleted parent records MUST cascade or block.

### Business Rules

**REQUIREMENT DR-042**: Certificate Status must be one of: active, expired, pending, suspended, revoked.
**REQUIREMENT DR-043**: Expired certificates (ExpirationDate < NOW) should auto-update status to "expired".
**REQUIREMENT DR-044**: Expiring soon certificates (ExpirationDate < NOW + 30 days) should be flagged.

---

## Performance Requirements

### Query Performance

**REQUIREMENT DR-045**: Certificate list queries MUST execute in < 500ms for 10,000 records.
**REQUIREMENT DR-046**: Single certificate lookups MUST execute in < 100ms.
**REQUIREMENT DR-047**: Search queries MUST support pagination and filtering.

### Indexing Requirements

**REQUIRED INDEXES:**
1. `Certificates.Number` (UNIQUE)
2. `Certificates.Status` (Non-clustered)
3. `Certificates.CompanyId` (Non-clustered)
4. `Certificates.ProductId` (Non-clustered)
5. `Certificates.CertificationTypeId` (Non-clustered)
6. `Certificates.EffectiveDate` (Non-clustered, DESC)
7. `Certificates.ExpirationDate` (Non-clustered)
8. `Companies.Name` (UNIQUE)
9. `Users.Email` (UNIQUE)
10. `CertificateProductIds (CertificateId, ProductIdType)` (Composite)

**REQUIREMENT DR-048**: All foreign keys MUST have indexes.
**REQUIREMENT DR-049**: Frequently searched columns MUST have indexes.

### Caching Strategy

**REQUIREMENT DR-050**: Reference data (Cert Types, Categories) MAY be cached for 1 hour.
**REQUIREMENT DR-051**: Certificate data MUST NOT be cached (real-time required).
**REQUIREMENT DR-052**: Search results MAY be cached for 5 minutes.

---

## Security Requirements

### Connection Security

**REQUIREMENT DR-053**: Production connections MUST use SSL/TLS encryption.
**REQUIREMENT DR-054**: Connection strings MUST be stored in secure configuration (not appsettings.json).
**REQUIREMENT DR-055**: Database credentials MUST be rotated quarterly.

### Access Control

**REQUIREMENT DR-056**: Database users MUST have minimal required permissions.
**REQUIREMENT DR-057**: Application user SHOULD NOT have db_owner role in production.
**REQUIREMENT DR-058**: Read-only endpoints MAY use read-only database user.

### SQL Injection Prevention

**REQUIREMENT DR-059**: All queries MUST use parameterized queries or Entity Framework.
**REQUIREMENT DR-060**: Direct SQL execution MUST be avoided except for performance.
**REQUIREMENT DR-061**: User inputs MUST NEVER be concatenated into SQL queries.

---

## Data Backup & Recovery

### Backup Requirements

**REQUIREMENT DR-062**: Production database MUST be backed up daily.
**REQUIREMENT DR-063**: Transaction logs MUST be backed up hourly.
**REQUIREMENT DR-064**: Backups MUST be tested monthly for restore capability.

### Recovery Requirements

**REQUIREMENT DR-065**: Point-in-time recovery MUST be possible for last 30 days.
**REQUIREMENT DR-066**: Recovery Time Objective (RTO): 4 hours.
**REQUIREMENT DR-067**: Recovery Point Objective (RPO): 1 hour.

---

## Testing Requirements

### Unit Tests

**REQUIREMENT DR-068**: Data access methods MUST have unit tests with in-memory database.
**REQUIREMENT DR-069**: Business logic MUST be tested independently of database.

### Integration Tests

**REQUIREMENT DR-070**: API endpoints MUST be tested with test database.
**REQUIREMENT DR-071**: Data loading MUST be tested end-to-end.
**REQUIREMENT DR-072**: Database seeding MUST be tested for idempotence.

### Performance Tests

**REQUIREMENT DR-073**: Data loading MUST be performance tested with 10,000+ records.
**REQUIREMENT DR-074**: Search queries MUST be tested for scalability.
**REQUIREMENT DR-075**: Infinite scroll MUST be tested with concurrent users.

---

## Monitoring & Logging

### Data Access Logging

**REQUIREMENT DR-076**: Failed database connections MUST be logged.
**REQUIREMENT DR-077**: Slow queries (> 1 second) MUST be logged.
**REQUIREMENT DR-078**: Data modification operations MUST be audited.

### Metrics Collection

**REQUIREMENT DR-079**: Query execution times MUST be tracked.
**REQUIREMENT DR-080**: Database connection pool metrics MUST be monitored.
**REQUIREMENT DR-081**: Certificate creation/update rates MUST be tracked.

---

## Appendix A: Sample Data Seed Script

```sql
-- Example: Seed Certification Types
INSERT INTO CertificationTypes (Name, Description, IsActive, CreatedAt)
VALUES
    ('Certified Sustainability Facts', 'CSF certification standard', 1, GETUTCDATE()),
    ('FSC Chain of Custody', 'Forest Stewardship Council certification', 1, GETUTCDATE()),
    ('SFI Chain of Custody', 'Sustainable Forestry Initiative', 1, GETUTCDATE());

-- Example: Seed Sample Company
INSERT INTO Companies (Name, Country, Location, IsActive, CreatedAt, UpdatedAt)
VALUES ('3M Company', 'United States', 'Maplewood, MN', 1, GETUTCDATE(), GETUTCDATE());

-- Example: Seed Sample Product
INSERT INTO Products (Name, Description, CompanyId, IsActive, CreatedAt, UpdatedAt)
VALUES ('Security Exit Device', 'High-security exit device system', SCOPE_IDENTITY(), 1, GETUTCDATE(), GETUTCDATE());
```

---

## Appendix B: Connection String Templates

### Development
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false"
  }
}
```

### Production
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password={from-vault};MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=true;Connection Timeout=30;"
  }
}
```

---

## Appendix C: Data Quality Checks

### Daily Checks
- [ ] Verify no null Company/Product/CertificationType on active certificates
- [ ] Check for orphaned records
- [ ] Validate all certificate numbers follow pattern
- [ ] Verify expiration dates > effective dates
- [ ] Check for duplicate certificate numbers

### Weekly Checks
- [ ] Analyze query performance metrics
- [ ] Review slow query log
- [ ] Verify index effectiveness
- [ ] Check database growth rate
- [ ] Validate backup success

---

**Document Control:**
- **Created By:** Claude Code AI Assistant
- **Last Review Date:** October 17, 2025
- **Next Review Date:** January 17, 2026
- **Approval Required:** Yes (Technical Lead, Database Administrator)
- **Version History:**
  - v1.0 (Initial Draft) - August 2025
  - v2.0 (Production Ready) - October 2025

---

**Related Documents:**
- [Database Schema Reference](DATABASE_SCHEMA_REFERENCE.md)
- [Hardcoded Data Audit](HARDCODED_DATA_AUDIT.md)
- [API Documentation](../docs/API_DOCUMENTATION.md)
- [Testing Strategy](../docs/TESTING_STRATEGY.md)
