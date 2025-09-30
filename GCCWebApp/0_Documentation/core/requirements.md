# GCC Database Solution - Requirements & User Stories

## Table of Contents
1. [Project Overview](#project-overview)
2. [User Stories & Requirements](#user-stories--requirements)
3. [Test Cases & Acceptance Criteria](#test-cases--acceptance-criteria)
4. [Progress Tracking](#progress-tracking)
5. [Technical Specifications](#technical-specifications)

---

## Project Overview

### Vision Statement
Build a comprehensive certificate management system for Green Circle Certified (GCC) that enables efficient certificate lifecycle management, partner integration, and public certificate verification.

### Key Business Objectives
- **Certificate Management**: Complete lifecycle from application to expiration
- **Partner Integration**: Support for Amazon, Wayfair, and other partners
- **Public Access**: Public-facing certificate verification database
- **Reporting**: Comprehensive analytics and export capabilities
- **Security**: Role-based access with Azure AD B2C integration

### User Roles & Permissions
- **Admin**: Full system access, user management, all certificate operations
- **Analyst**: Certificate management, reporting, no user management
- **External**: Partner access, limited to their certificates
- **Internal**: GCC staff, extended viewing permissions
- **Public**: Read-only access to public certificate data

---

## User Stories & Requirements

### EPIC 1: Certificate Management

#### US-001: Certificate Creation
**As an** Admin/Analyst  
**I want to** create new certificates  
**So that** I can issue certifications to companies

**Acceptance Criteria:**
- ✅ Can create certificate with all required fields
- ✅ 14-state lifecycle workflow enforced
- ✅ Partner assignment (Amazon/Wayfair/None)
- ✅ Validation for all required fields
- ✅ Audit trail created (CreatedBy, CreatedAt)

**Implementation Status:** ✅ COMPLETE
- Location: `Controllers/CertificateController.cs`
- View: `Views/Certificate/Create.cshtml`
- Service: `Services/CertificateService.cs`

#### US-002: Certificate Search & Filter
**As a** Public User  
**I want to** search and filter certificates  
**So that** I can find specific certifications

**Acceptance Criteria:**
- ✅ Search by certificate number, company, product
- ✅ Filter by status, partner, date range
- ⚠️ Autocomplete suggestions in search
- ⚠️ Unique URL for search results
- ✅ Export search results (CSV/PDF)

**Implementation Status:** 🟡 PARTIAL (70%)
- Missing: Autocomplete, unique URLs
- Location: `Services/FilterService.cs`

#### US-003: Certificate Tile View
**As a** Public User
**I want to** view certificates in a tile layout
**So that** I can browse certificates visually

**Acceptance Criteria:**
- ⚠️ Lazy loading for performance (currently paginated)
- ✅ Display all public fields on tiles
- ✅ Click tile to view details (opens in new tab)
- ✅ Sort by multiple columns
- ✅ Clear filters option
- ✅ Download certificate from tile
- ✅ Certificate details open in new tab when clicked
- ✅ Product name height consistency across certificate tiles only (140px fixed height)
- ⚠️ Clicking anywhere on tile opens in new tab (not just icons)
- ⚠️ Filter items should have minimal padding
- ⚠️ Result count must show accurate database count
- ⚠️ Sort dropdown must be functional
- ⚠️ Search input placeholder should only say "Search"
- ⚠️ Certificate preview must display uploaded image
- ⚠️ Section headers should have primary blue left bar
- ⚠️ Download icon positioned directly below certificate image
- ✅ Download icon in SingleCertificateView should be right-aligned
- ✅ Section containers (Certification Details, Certification Period, Contact Information) should have primary blue left bars instead of green
- ✅ Product name elements must have 140px height and 22px font-size on certificate tiles only (NOT SingleCertificateView)
- ✅ Product type elements must have 55px height on certificate tiles only (NOT SingleCertificateView)
- ✅ Product name in SingleCertificateView should be full width of its container

**Implementation Status:** 🟡 PARTIAL (75%)
- View: `Views/Certificate/CertificateTileView.cshtml`
- Missing: UI consistency fixes, lazy loading implementation
- Updated: Certificate details now open in new tab (2025-09-28)

#### US-004: Certificate Lifecycle Management
**As an** Admin/Analyst  
**I want to** manage certificate status transitions  
**So that** certificates follow proper workflow

**Acceptance Criteria:**
- ✅ 14 status states implemented
- ✅ Role-based transition permissions
- ✅ Automated expiration checking
- ✅ Status history tracking
- ✅ Email notifications on status change

**Implementation Status:** ✅ COMPLETE
- Service: `Services/CertificateWorkflowService.cs`
- Model: `Models/CertificateStatus.cs`

**Certificate Status Workflow:**
```
Draft → ApplicationSubmitted → UnderReview → TechnicalReview → 
ComplianceVerification → FinalApproval → Active → ExpiringSoon → 
RenewalInProgress → [Active|Expired|Archived|Cancelled|ClientDeclined]
```

### EPIC 2: Partner Integration

#### US-005: Partner-Specific Certificates
**As a** Partner (Amazon/Wayfair)  
**I want to** view only my certificates  
**So that** I can manage my certifications

**Acceptance Criteria:**
- ✅ Partner field in certificate model
- ✅ Filter certificates by partner
- ✅ Partner-specific dashboard
- ✅ Bulk export for partner
- ⚠️ Partner-specific branding

**Implementation Status:** 🟡 PARTIAL (80%)
- Migration: `20250827182202_AddPartnerFieldToCertificate`
- Missing: Partner-specific branding

### EPIC 3: Dashboard & Reporting

#### US-006: Analytics Dashboard
**As an** Admin/Analyst  
**I want to** view certificate analytics  
**So that** I can track business metrics

**Acceptance Criteria:**
- ✅ 12 statistical cards with metrics
- ✅ 5 data tables with breakdowns
- ✅ Export individual metrics
- ✅ Export full dashboard data
- ✅ Real-time data updates

**Implementation Status:** ✅ COMPLETE
- Controller: `Controllers/DashboardController.cs`
- View: `Views/Dashboard/Index.cshtml`
- JavaScript: `wwwroot/js/dashboard.js`

**Dashboard Metrics:**
1. Active Certificates Count
2. Total Companies
3. Expired Certificates
4. Pending Renewals
5. Certificate Type Breakdown
6. Top Companies by Certificate Count
7. Certificate Status Distribution
8. Product Types by Certificate Count
9. Product Categories Breakdown
10. Monthly Certificate Trends
11. Partner Distribution
12. Geographic Distribution

**Dashboard CSV Export Reports:**
The Dashboard includes three quick-action export buttons that generate CSV reports:

1. **Expiring Certificates Report (Next 3 Months)**
   - Button: "Download Expiring Certs Report (Next 3 Months)"
   - Endpoint: `/Export/DownloadReport?type=expiring-3-months&format=csv`
   - Filename: `certificates-expiring-next-3-months-YYYY-MM-DD.csv`
   - Data: All certificates with expiration dates within the next 90 days
   - Columns: Certificate Number, Company Name, Product Name, Certification Type, Effective Date, Expiration Date, Status, Location, Country

2. **Active Certificates Report**
   - Button: "Download Current Active Certs Report"
   - Endpoint: `/Export/DownloadReport?type=active-certificates&format=csv`
   - Filename: `active-certificates-YYYY-MM-DD.csv`
   - Data: All certificates with "Active" status
   - Columns: Certificate Number, Company Name, Product Name, Certification Type, Effective Date, Expiration Date, Location, Country

3. **Expiring Certificates Report (Next 30 Days)**
   - Button: "Download Expiring Certs Report (Next 30 Days)"
   - Endpoint: `/Export/DownloadReport?type=expiring-30-days&format=csv`
   - Filename: `certificates-expiring-next-30-days-YYYY-MM-DD.csv`
   - Data: All certificates with expiration dates within the next 30 days
   - Columns: Certificate Number, Company Name, Product Name, Certification Type, Effective Date, Expiration Date, Status, Location, Country

**Technical Implementation:**
- Format: CSV (Comma-Separated Values)
- Authentication: Required (Admin/Analyst roles only)
- Download Method: Client-side blob download via JavaScript
- Toast Notifications: Success/Error feedback to user
- Error Handling: 401/302 redirects for authentication, graceful failure messages

#### US-007: Export Functionality
**As a** User
**I want to** export certificate data
**So that** I can use it externally

**Acceptance Criteria:**
- ✅ Export to CSV format
- ✅ Export to PDF format
- ✅ Bulk export capabilities
- ✅ Filtered export (export search results)
- ⚠️ Excel export with formatting
- ✅ Download uploaded certificate files when available
- ✅ Fallback to generated PDF if no uploaded file exists
- ✅ Dashboard CSV export reports:
  - Export certificates expiring in next 3 months (CSV)
  - Export current active certificates (CSV)
  - Export certificates expiring in next 30 days (CSV)

**Implementation Status:** 🟡 PARTIAL (95%)
- PDF: `Services/CertificatePdfService.cs`
- CSV: `wwwroot/js/dashboard.js` (Dashboard reports)
- Missing: Excel formatting
- Updated: Certificate download now uses uploaded files when available (2025-09-28)
- Updated: Dashboard export buttons now generate CSV reports (2025-09-29)

### EPIC 4: User Management

#### US-008: User Authentication
**As a** User  
**I want to** securely log into the system  
**So that** I can access my authorized features

**Acceptance Criteria:**
- ✅ Azure AD B2C integration
- ✅ Cookie auth fallback
- ✅ Two-factor authentication
- ✅ Password reset functionality
- ✅ Session management

**Implementation Status:** ✅ COMPLETE
- Controller: `Controllers/AccountController.cs`
- Service: `Services/TwoFactorService.cs`

#### US-009: Role Management
**As an** Admin  
**I want to** manage user roles  
**So that** I can control system access

**Acceptance Criteria:**
- ✅ 5 role types defined
- ✅ Role assignment UI
- ✅ Policy-based authorization
- ⚠️ Bulk role updates
- ⚠️ Role audit trail

**Implementation Status:** 🟡 PARTIAL (70%)
- View: `Views/Admin/UserManagement.cshtml`
- Missing: Bulk updates, audit trail

### EPIC 5: API Integration

#### US-010: RESTful API
**As a** Third-party System  
**I want to** access certificate data via API  
**So that** I can integrate with GCC

**Acceptance Criteria:**
- ✅ Versioned API endpoints (v1)
- ✅ JWT authentication
- ✅ API key management
- ✅ Rate limiting
- ✅ OpenAPI documentation

**Implementation Status:** ✅ COMPLETE
- Controllers: `Controllers/Api/v1/`
- Middleware: Rate limiting configured

**API Endpoints:**
- `GET /api/v1/certificates` - List certificates
- `GET /api/v1/certificates/{id}` - Get certificate details
- `POST /api/v1/certificates` - Create certificate
- `PUT /api/v1/certificates/{id}` - Update certificate
- `DELETE /api/v1/certificates/{id}` - Delete certificate
- `GET /api/v1/products` - List products
- `GET /api/v1/apikeys` - Manage API keys

---

## Test Cases & Acceptance Criteria

### Certificate Management Test Cases

#### TC-001: Create Certificate - Happy Path
**Preconditions:** User logged in as Admin
**Steps:**
1. Navigate to /Certificate/Create
2. Fill all required fields
3. Select Partner (Amazon)
4. Click Submit

**Expected Result:**
- Certificate created successfully
- Redirect to certificate details
- Audit trail shows creation

**Automation:** ✅ `GCCWebApp.Tests/Controllers/CertificateControllerTests.cs`

#### TC-002: Search Certificates
**Preconditions:** Certificates exist in database
**Steps:**
1. Navigate to /Certificate/CertificateTileView
2. Enter search term "Amazon"
3. Apply filters (Active status)
4. Click Search
5. Click on a certificate tile

**Expected Result:**
- Only Amazon active certificates shown
- Results count displayed
- Export button available
- Certificate details open in new tab when clicked

**Automation:** ⚠️ Partial - `GCCWebApp.UAT.Tests`

#### TC-003: Certificate Status Workflow
**Preconditions:** Certificate in Draft status
**Steps:**
1. Open certificate details
2. Change status to "ApplicationSubmitted"
3. Save changes
4. Verify transition rules

**Expected Result:**
- Status updated successfully
- Invalid transitions blocked
- History record created

**Automation:** ✅ `GCCWebApp.Tests/Services/CertificateWorkflowServiceTests.cs`

#### TC-004: Certificate Export - PDF
**Preconditions:** Valid certificate exists
**Steps:**
1. Navigate to certificate details
2. Click "Download PDF"
3. Verify downloaded file

**Expected Result:**
- If certificate has uploaded file: Downloads the uploaded certificate file
- If no uploaded file: Downloads generated PDF with certificate data
- Proper formatting and branding for generated PDFs
- File downloads successfully with correct content type

**Automation:** ✅ `GCCWebApp.Tests/Services/CertificatePdfServiceTests.cs`
**Updated:** 2025-09-28 - Added uploaded file download support

### User Management Test Cases

#### TC-005: User Login - Azure AD B2C
**Preconditions:** Valid Azure AD B2C account
**Steps:**
1. Navigate to /Account/Login
2. Click "Sign in with Azure"
3. Enter credentials
4. Complete MFA if required

**Expected Result:**
- User authenticated
- Correct role assigned
- Redirect to dashboard

**Automation:** ✅ `GCCWebApp.Security.Tests`

#### TC-006: Role-Based Access Control
**Preconditions:** Users with different roles
**Steps:**
1. Login as External user
2. Try to access /Admin/UserManagement
3. Verify access denied
4. Access /Certificate/Certificates

**Expected Result:**
- Admin pages blocked
- Certificate pages accessible
- Proper error messages

**Automation:** ✅ `GCCWebApp.RoleBasedUI.Tests`

#### TC-007: Two-Factor Authentication
**Preconditions:** User with 2FA enabled
**Steps:**
1. Login with username/password
2. Enter 2FA code
3. Verify access granted

**Expected Result:**
- 2FA prompt appears
- Valid code grants access
- Invalid code rejected

**Automation:** ✅ `GCCWebApp.Security.Tests/TwoFactorAuthTests.cs`

### API Test Cases

#### TC-008: API Authentication
**Preconditions:** Valid API key
**Steps:**
1. Send GET to /api/v1/certificates
2. Include API key in header
3. Verify response

**Expected Result:**
- 200 OK response
- JSON data returned
- Proper pagination

**Automation:** ✅ `GCCWebApp.Api.Tests`

#### TC-009: API Rate Limiting
**Preconditions:** API key configured
**Steps:**
1. Send 100 requests in 1 minute
2. Send 101st request
3. Verify rate limit response

**Expected Result:**
- First 100 requests succeed
- 101st request returns 429
- Retry-After header present

**Automation:** ✅ `GCCWebApp.Api.Tests/RateLimitingTests.cs`

### Performance Test Cases

#### TC-010: Large Dataset Loading
**Preconditions:** 10,000+ certificates
**Steps:**
1. Navigate to certificate list
2. Measure page load time
3. Scroll to trigger lazy load
4. Measure response time

**Expected Result:**
- Initial load < 2 seconds
- Lazy load < 500ms
- No UI freezing

**Automation:** ✅ `GCCWebApp.Performance.Tests`

#### TC-011: Concurrent User Load
**Preconditions:** Test environment configured
**Steps:**
1. Simulate 100 concurrent users
2. Each user browses certificates
3. Monitor response times

**Expected Result:**
- Average response < 1 second
- No errors or timeouts
- Database connections stable

**Automation:** ✅ `GCCWebApp.Performance.Tests/LoadTests.cs`

### Dashboard Test Cases

#### TC-012: Dashboard Metrics Accuracy
**Preconditions:** Known dataset in database
**Steps:**
1. Navigate to /Dashboard
2. Verify each metric value
3. Export data and compare

**Expected Result:**
- All metrics match database
- Export contains same values
- Real-time updates work

**Automation:** ✅ `GCCWebApp.Tests/Controllers/DashboardControllerTests.cs`

#### TC-013: Certificate Single View Font Sizes
**Preconditions:** Certificate exists in database
**Steps:**
1. Navigate to /Certificate/CertificateSingleView/{id}
2. Inspect certificate name text
3. Inspect product name text
4. Inspect product type text

**Expected Result:**
- Certificate section titles: 20px font size
- Product name: 22px font size, bold
- Product type: 18px font size
- All text meets minimum 16px requirement

**Automation:** ⚠️ Manual - Visual verification required
**Added:** 2025-09-28 - Enhanced readability requirements

---

## Progress Tracking

### Current Sprint Focus
1. ⚠️ **Autocomplete Search** - Implement search suggestions
2. ⚠️ **Lazy Loading** - Replace pagination with infinite scroll
3. ⚠️ **Unique URLs** - Generate URLs for filtered searches
4. ⚠️ **Excel Export** - Add formatted Excel export
5. ⚠️ **Audit Trail UI** - Display status history

### Completed Features ✅
- Certificate CRUD operations
- 14-state workflow management
- Partner integration (Amazon/Wayfair)
- Dashboard analytics
- PDF generation
- API v1 implementation
- Authentication system
- Role-based access control
- Two-factor authentication
- Database migrations
- Performance indexes

### In Progress 🟡
- Advanced search features (70%)
- User management UI (70%)
- Bulk operations (50%)
- Export enhancements (80%)

### Not Started ❌
- Geographic filtering
- Saved search preferences
- Partner-specific branding
- Advanced Excel formatting
- Notification system
- Audit trail UI

### Test Coverage Status
- **Total Tests**: 299
- **Code Coverage**: ~85%
- **UI Test Coverage**: 75%
- **API Test Coverage**: 90%
- **Security Test Coverage**: 95%
- **Performance Test Coverage**: 80%

### Production Readiness Checklist
- [x] Core certificate management
- [x] Authentication system
- [x] API implementation
- [x] PDF generation
- [x] Partner support
- [x] Database indexes
- [x] Security measures
- [ ] Autocomplete search
- [ ] Lazy loading
- [ ] Unique search URLs
- [ ] Complete UI polish
- [ ] Performance optimization
- [ ] Production deployment scripts

### Estimated Timeline
- **Current Status**: 85% complete
- **Remaining Development**: 2-3 weeks
- **Testing & QA**: 1 week
- **UAT**: 1 week
- **Production Ready**: ~4-5 weeks

---

## Technical Specifications

### Database Schema

#### Certificate Table
```sql
CREATE TABLE Certificates (
    Id INT PRIMARY KEY,
    Number VARCHAR(50) UNIQUE,
    Status VARCHAR(50),
    Partner VARCHAR(100),
    CompanyId INT FOREIGN KEY,
    ProductId INT FOREIGN KEY,
    CertificationType VARCHAR(100),
    EffectiveDate DATE,
    ExpirationDate DATE,
    CreatedBy VARCHAR(100),
    CreatedAt DATETIME,
    UpdatedBy VARCHAR(100),
    UpdatedAt DATETIME
)
```

#### Key Relationships
- Certificate → Company (Many-to-One)
- Certificate → Product (Many-to-One)
- Certificate → User (CreatedBy/UpdatedBy)
- Certificate → CertificateStatus (Status field)

### API Specifications

#### Authentication
- **Method**: Bearer token (JWT)
- **Header**: `Authorization: Bearer {token}`
- **API Key**: `X-API-Key: {key}`

#### Response Format
```json
{
    "success": true,
    "data": {},
    "errors": [],
    "metadata": {
        "page": 1,
        "pageSize": 20,
        "totalCount": 100
    }
}
```

#### Error Codes
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `429` - Rate Limited
- `500` - Internal Server Error

### Performance Requirements

#### Response Times
- **Page Load**: < 2 seconds
- **API Response**: < 500ms
- **Search**: < 1 second
- **Export**: < 5 seconds for 1000 records

#### Scalability
- Support 10,000+ certificates
- Handle 100 concurrent users
- Process 1000 requests/minute

#### Availability
- **Target Uptime**: 99.9%
- **Maintenance Window**: Sunday 2-4 AM EST
- **Backup Schedule**: Daily at midnight

### Security Requirements

#### Authentication
- Azure AD B2C integration
- Multi-factor authentication
- Session timeout: 30 minutes
- Password complexity enforced

#### Authorization
- Role-based access control
- Policy-based permissions
- API key management
- Rate limiting: 100 requests/minute

#### Data Protection
- HTTPS only
- Input validation
- SQL injection prevention
- XSS protection
- CSRF tokens

---

## Appendix

### Glossary
- **GCC**: Green Circle Certified
- **B2C**: Business to Consumer
- **MFA/2FA**: Multi-Factor/Two-Factor Authentication
- **CRUD**: Create, Read, Update, Delete
- **UAT**: User Acceptance Testing

### References
- [Mockup Designs](../pages/)
- [Gap Analysis](Mockup-vs-Implementation-Gap-Analysis-2025-08-27.md)
- [Implementation Status](Requirements-vs-Implementation-Analysis-2025-08-27.md)
- [UAT Test Cases](MANUAL_UAT_TEST_CASES.md)

### Change Log
- **2025-08-28**: Document created, consolidated from multiple sources
- **2025-08-27**: Partner field added to certificates
- **2025-08-27**: 14-state workflow implemented
- **2025-08-27**: PDF generation fixed with QuestPDF

---

*Last Updated: August 28, 2025*