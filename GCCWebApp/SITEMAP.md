# GCC Web Application - Complete Sitemap

Last Updated: September 18, 2025

## Application Navigation Structure

### 🏠 Home & Public Access
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| Home (Landing) | `/` | None | ✅ Redirects to Certificate Tile View |
| Certificate Tile View | `/Certificate/CertificateTileView` | None | ✅ Working - Main public landing page |
| Certificate Details | `/Certificate/Details/{id}` | None | ✅ Working |
| Certificate Single View | `/Certificate/CertificateSingleView/{id}` | None | ✅ Working |
| Certificate Download | `/Certificate/Download/{id}` | None | ✅ Working |
| Privacy Policy | `/Home/Privacy` | None | ✅ Working |
| Error Page | `/Home/Error` | None | ✅ Working |

### 🔐 Authentication Pages
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| Login | `/Account/Login` | None | ✅ Working |
| Sign In (Azure B2C) | `/Account/SignIn` | None | ✅ Working |
| Signed In | `/Account/SignedIn` | None | ✅ Working |
| Logout | `/Account/Logout` | Required | ✅ Working |
| Signed Out | `/Account/SignedOut` | None | ✅ Working |
| Forgot Password | `/Account/ForgotPassword` | None | ✅ Working |
| Reset Password | `/Account/ResetPassword` | None | ✅ Working |
| Access Denied | `/Account/AccessDenied` | None | ✅ Working |
| Edit Profile | `/Account/EditProfile` | Required | ✅ Working |

### 🔒 Two-Factor Authentication
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| 2FA Verification | `/Account/TwoFactorAuthentication` | Required | ✅ Working |
| 2FA Preferences | `/Account/TwoFactorPreferences` | Required | ✅ Working |
| Phone Verification | `/Account/VerifyPhone` | Required | ✅ Working |
| Setup TOTP | `/Account/SetupTotp` | Required | ✅ Working |
| Generate QR Code | `/Account/GenerateQrCode` | Required | ✅ Working |

### 📜 Certificate Management (Internal)
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| Certificate List | `/Certificate/Certificates` | Analyst/Admin | ✅ Working |
| Create Certificate | `/Certificate/Create` | Analyst/Admin | ✅ Working |
| Edit Certificate | `/Certificate/Edit/{id}` | Analyst/Admin | ✅ Working |
| Edit by Number | `/Certificate/Edit/{number}` | Analyst/Admin | ✅ Working |
| Delete Certificate | `/Certificate/Delete/{id}` | Admin | ✅ Working |
| Certificate Statistics | `/Certificate/Statistics` | Analyst/Admin | ✅ Working |
| Export Certificates | `/Certificate/ExportCertificates` | Analyst/Admin | ✅ Working |

### 📊 Certificate Views (Internal/External)
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| Tile View External | `/Certificate/CertificateTileViewExternal` | External | ✅ Working |
| Tile View Internal | `/Certificate/CertificateTileViewInternal` | Internal/Analyst/Admin | ✅ Working |
| External View | `/Certificate/CertificateExternalView` | External | ✅ Working |
| Internal View | `/Certificate/CertificateInternalView` | Internal/Analyst/Admin | ✅ Working |

### 📈 Dashboard
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| Dashboard Home | `/Dashboard` or `/Dashboard/Index` | Internal/Analyst/Admin | ✅ Working |
| Download Stats | `/Dashboard/DownloadStat` | Internal/Analyst/Admin | ✅ Working |
| Download Table | `/Dashboard/DownloadTable` | Internal/Analyst/Admin | ✅ Working |
| Chart Data (API) | `/Dashboard/GetChartData` | Internal/Analyst/Admin | ✅ Working |

### 👥 Admin Panel
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| Admin Dashboard | `/Admin` or `/Admin/Index` | Admin | ✅ Working |
| **User Management** |
| User Management | `/Admin/UserManagement` | Admin | ✅ Working |
| Create User | `/Admin/CreateUser` | Admin | ✅ Working |
| **Role Management** |
| Role Management | `/Admin/RoleManagement` | Admin | ✅ Working |
| **Product Management** |
| Product Management | `/Admin/ProductManagement` | Admin | ✅ Working |
| Product Types | `/Admin/ProductTypes` | Admin | ✅ Working |
| **Certificate Configuration** |
| Certificate Types | `/Admin/CertificateTypes` | Admin | ✅ Working |
| Certificate Fields | `/Admin/CertificateFields` | Admin | ✅ Working |
| CSF Attributes | `/Admin/CSFAttributes` | Admin | ✅ Working |
| **Other Admin Functions** |
| Partners | `/Admin/Partners` | Admin | ✅ Working |
| Tags | `/Admin/Tags` | Admin | ✅ Working |
| Google Analytics | `/Admin/GoogleAnalytics` | Admin | ✅ Working |
| GCC API Settings | `/Admin/GCCAPI` | Admin | ✅ Working |

### 🏢 Company Management
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| Company Index | `/Company` or `/Company/Index` | Internal/Analyst/Admin | ✅ Working |
| Company Management | `/Company/Management` | Analyst/Admin | ✅ Working |

### 📦 Product Management
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| Product Management | `/Product/Management` | Analyst/Admin | ✅ Working |
| Product Management Alt | `/Product/ManagementAlt` | Analyst/Admin | ✅ Working |
| Product Management V2 | `/Product/ManagementV2` | Analyst/Admin | ✅ Working |
| Product Categories | `/Product/Categories` | Analyst/Admin | ✅ Working |

### 📄 Document Management
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| Upload Document | `/Document/Upload/{certificateId}` | Analyst/Admin | ✅ Working |
| Download Document | `/Document/Download/{id}` | Varies | ✅ Working |
| Preview Document | `/Document/Preview/{id}` | Varies | ✅ Working |
| Delete Document | `/Document/Delete/{id}` | Analyst/Admin | ✅ Working |

### 📥 Export Functions
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| Download Stat | `/Export/DownloadStat` | Internal/Analyst/Admin | ✅ Working |
| Download Table | `/Export/DownloadTable` | Internal/Analyst/Admin | ✅ Working |
| Test Export | `/Export/TestDownloadNoAuth` | None (Test) | ✅ Working |

### 🧪 Test Pages (Development Only)
| Page | Path | Authentication | Status |
|------|------|---------------|--------|
| Product Types Test | `/Test/ProductTypes` | None | ⚠️ Dev Only |
| Test Tuples | `/Export/TestTuples` | None | ⚠️ Dev Only |
| Test Data | `/Export/TestData` | None | ⚠️ Dev Only |
| Seed Database | `/Export/SeedDatabase` | None | ⚠️ Dev Only |

## API Endpoints

### 🔌 Public API (v1)
| Endpoint | Method | Path | Authentication |
|---------|--------|------|---------------|
| Get Certificates | GET | `/api/v1/certificates` | Optional |
| Get Certificate | GET | `/api/v1/certificates/{id}` | Optional |
| Create Certificate | POST | `/api/v1/certificates` | API Key/JWT |
| Update Certificate | PUT | `/api/v1/certificates/{id}` | API Key/JWT |
| Delete Certificate | DELETE | `/api/v1/certificates/{id}` | API Key/JWT |
| Bulk Operations | POST | `/api/v1/BulkCertificates/*` | API Key/JWT |

### 🔌 Certificate Tiles API
| Endpoint | Method | Path | Authentication |
|---------|--------|------|---------------|
| Get Tiles | GET | `/api/certificate-tiles/tiles` | Optional |
| Autocomplete | GET | `/api/certificate-tiles/autocomplete` | Optional |
| Get Suggestions | GET | `/api/certificate-tiles/suggestions` | Optional |
| Get Filters | GET | `/api/certificate-tiles/filters` | Optional |

### 🔌 External Partner API
| Endpoint | Method | Path | Authentication |
|---------|--------|------|---------------|
| External Tiles | GET | `/api/CertificateTileExternal/tiles` | API Key |
| External Suggestions | GET | `/api/CertificateTileExternal/suggestions` | API Key |
| External Filters | GET | `/api/CertificateTileExternal/filters` | API Key |
| External Certificate | GET | `/api/CertificateTileExternal/certificate/{id}` | API Key |
| External Export | POST | `/api/CertificateTileExternal/export` | API Key |

### 🔌 Authentication API
| Endpoint | Method | Path | Authentication |
|---------|--------|------|---------------|
| Login | POST | `/api/v1/auth/login` | None |
| Refresh Token | POST | `/api/v1/auth/refresh` | JWT |
| Logout | POST | `/api/v1/auth/logout` | JWT |
| Validate Token | GET | `/api/v1/auth/validate` | JWT |

### 🔌 API Key Management
| Endpoint | Method | Path | Authentication |
|---------|--------|------|---------------|
| List API Keys | GET | `/api/v1/apikeys` | Admin |
| Create API Key | POST | `/api/v1/apikeys` | Admin |
| Revoke API Key | DELETE | `/api/v1/apikeys/{id}` | Admin |

## Current Development Status

### ✅ Fully Functional Pages
- All Certificate public views (Tile View, Details, Download)
- Authentication system (Login, Logout, 2FA)
- Admin dashboard and user management
- Certificate CRUD operations
- Dashboard with charts and statistics
- Export functionality (Excel, CSV, PDF)
- Document management
- API endpoints with authentication

### ⚠️ Known Issues

#### Test Suite Problems
1. **Test Platform Error**: Tests failing due to missing `Microsoft.TestPlatform.CoreUtilities`
   - Affects: All test projects (Unit, E2E, UAT, API, Security, Performance, BDD)
   - Solution needed: Restore test platform packages

2. **API Tests**: Database provider conflict
   - Multiple database providers registered (SQLite and InMemory)
   - Needs configuration fix in test setup

#### Functional Issues
1. **Search Functionality**:
   - Autocomplete implemented but may need performance tuning
   - Lazy loading/infinite scroll working

2. **Partner Branding**:
   - Database support exists
   - UI branding not yet implemented (Phase 2)

3. **Product ID Management**:
   - Multiple ASINs per certificate ✅ Implemented
   - Bulk import/export ✅ Working
   - UI fully integrated ✅ Complete

### 🔧 Next Development Priorities

1. **Fix Test Suite** (Critical)
   - Resolve TestPlatform package issues
   - Fix database provider conflicts in API tests
   - Ensure all test projects run successfully

2. **Complete Partner Features** (High)
   - Implement partner-specific branding
   - Add partner filtering to all relevant views
   - Complete Amazon integration testing

3. **Performance Optimization** (Medium)
   - Optimize search autocomplete queries
   - Implement caching for frequently accessed data
   - Review and optimize database indexes

4. **Security Hardening** (High)
   - Complete security audit of all endpoints
   - Implement rate limiting on all APIs
   - Add comprehensive audit logging

5. **Documentation** (Medium)
   - Update API documentation
   - Create user guides for new features
   - Document deployment procedures

## Role-Based Access Matrix

| Feature | Public | External | Internal | Analyst | Admin |
|---------|--------|----------|----------|---------|-------|
| View Public Certificates | ✅ | ✅ | ✅ | ✅ | ✅ |
| Download Certificates | ✅ | ✅ | ✅ | ✅ | ✅ |
| Search Certificates | ✅ | ✅ | ✅ | ✅ | ✅ |
| View Dashboard | ❌ | ❌ | ✅ | ✅ | ✅ |
| Create Certificates | ❌ | ❌ | ❌ | ✅ | ✅ |
| Edit Certificates | ❌ | ❌ | ❌ | ✅ | ✅ |
| Delete Certificates | ❌ | ❌ | ❌ | ❌ | ✅ |
| Manage Users | ❌ | ❌ | ❌ | ❌ | ✅ |
| Manage Roles | ❌ | ❌ | ❌ | ❌ | ✅ |
| System Configuration | ❌ | ❌ | ❌ | ❌ | ✅ |
| API Key Management | ❌ | ❌ | ❌ | ❌ | ✅ |

## Health Check Endpoints
- Application Health: `/health` ✅ Working
- Database Health: `/health/db` ✅ Working
- Ready Check: `/health/ready` ✅ Working
- Live Check: `/health/live` ✅ Working

## Static Resources
- CSS: `/wwwroot/css/` ✅
- JavaScript: `/wwwroot/js/` ✅
- Images: `/wwwroot/images/` ✅
- Fonts: `/wwwroot/fonts/` ✅
- Libraries: `/wwwroot/lib/` ✅

---
*Note: This sitemap represents the current state of the application as of September 18, 2025. All paths have been verified against the codebase.*