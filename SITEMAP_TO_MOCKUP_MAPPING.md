# GCC Database Solution - Sitemap to Mockup Mapping

## Overview
This document maps the current MVC application sitemap/navigation structure to the corresponding HTML mockup designs that were previously created. All mockups are located in `/0_Documentation/mockups/` and serve as the design reference for converting from HTML to MVC format.

## Master Design Reference
- **Master Design Guide**: `0_0_MasterDesignGuide.html`
- **Alternative Master Design**: `0_0_MasterDesign.html`
- **Notification Alerts**: `0_0_NotificationAlertsFinal.html`

---

## Public Pages (No Authentication Required)

### Certificate Views - Public Access

| Route | MVC View | Mockup Reference | Status | Notes |
|-------|----------|------------------|---------|-------|
| `/` | Home/Index | `1_0_CertificateViewTile.html` | ✅ Implemented | Landing page redirects to CertificateTileView |
| `/Certificate/CertificateTileView` | Certificate/CertificateTileView.cshtml | `1_0_CertificateViewTile.html` | ✅ Implemented | Main public certificate tile view |
| `/Certificate/Details/{id}` | Certificate/Details.cshtml | `1_1_CertificateSingleView.html` | ✅ Implemented | Certificate detail view for public |
| `/Certificate/Download/{id}` | N/A (Direct PDF) | N/A | ✅ Implemented | Direct PDF download |

### Authentication Pages

| Route | MVC View | Mockup Reference | Status | Notes |
|-------|----------|------------------|---------|-------|
| `/Account/Login` | Account/Login.cshtml | `0_1_Loginscreens.html` | ✅ Implemented | Login with Azure AD B2C + Cookie fallback |
| `/Account/SignUp` | Account/SignUp.cshtml | `0_1_Loginscreens.html` | ✅ Implemented | Registration page |
| `/Account/ForgotPassword` | Account/ForgotPassword.cshtml | `0_1_Loginscreens.html` | ✅ Implemented | Password reset request |
| `/Account/ResetPassword` | Account/ResetPassword.cshtml | `0_1_Loginscreens.html` | ✅ Implemented | Password reset form |
| `/Account/VerifyEmail` | Account/VerifyEmail.cshtml | `0_1_Loginscreens.html` | ✅ Implemented | Email verification |

---

## Authenticated Pages

### External Role Pages (Partner Access)

| Route | MVC View | Mockup Reference | Status | Notes |
|-------|----------|------------------|---------|-------|
| `/Certificate/CertificateTileViewExternal` | Certificate/CertificateTileViewExternal.cshtml | `3_0_CertificateExternal.html` | ✅ Implemented | Partner-specific certificate view |
| `/Certificate/CertificateExternalView/{id}` | Certificate/CertificateExternalView.cshtml | `1_1_CertificateExternalSingleView.html` | ✅ Implemented | External partner certificate details |

**Alternative External Designs:**
- `3_0_CertificateExternal_options.html` - Alternative layout options

### Internal Role Pages (Internal/Analyst/Admin)

#### Certificate Management

| Route | MVC View | Mockup Reference | Status | Notes |
|-------|----------|------------------|---------|-------|
| `/Certificate/Certificates` | Certificate/Certificates.cshtml | `3_2_Certificate_GCC (3).html` | ✅ Implemented | Internal certificate list view |
| `/Certificate/CertificateTileViewInternal` | Certificate/CertificateTileViewInternal.cshtml | `1_0_CertificateViewTile.html` | ✅ Implemented | Internal tile view |
| `/Certificate/CertificateInternalView/{id}` | Certificate/CertificateInternalView.cshtml | `3_1_CertificateInternalSingleView.html` | ✅ Implemented | Internal certificate details |
| `/Certificate/CertificateSingleView/{id}` | Certificate/CertificateSingleView.cshtml | `1_1_CertificateSingleView.html` | ✅ Implemented | General single certificate view |
| `/Certificate/Create` | Certificate/Create.cshtml | `3_3_Certificate_GCC_Add.html` | ✅ Implemented | Add new certificate |
| `/Certificate/Edit/{id}` | Certificate/Edit.cshtml | `3_4_Certificate_GCC_Edit.html` | ✅ Implemented | Edit existing certificate |

**Alternative Certificate Creation Designs:**
- `0.3_AddCertificate_multiselectdropdowns.html` - Multi-select dropdown version
- `0.3_AddCertificate_multiselectdropdowns (2).html` - Alternative multi-select version

#### Dashboard & Analytics

| Route | MVC View | Mockup Reference | Status | Notes |
|-------|----------|------------------|---------|-------|
| `/Dashboard` | Dashboard/Index.cshtml | `2_0_Dashboard.html` | ✅ Implemented | Main analytics dashboard |
| `/Dashboard/Index` | Dashboard/Index.cshtml | `2_0_Dashboard.html` | ✅ Implemented | Same as above |

#### Company & Product Management

| Route | MVC View | Mockup Reference | Status | Notes |
|-------|----------|------------------|---------|-------|
| `/Company/Index` | Company/Index.cshtml | `5_1_Company.html` | ✅ Implemented | Company listing |
| `/Company/Management` | Company/Management.cshtml | `5_1_Company (2).html` | ✅ Implemented | Company management interface |
| `/Company/Create` | Company/Create.cshtml | `5_1_Company.html` | ✅ Implemented | Add new company |
| `/Product/Management` | Product/Management.cshtml | `5_2_ProductTypes.html` | ✅ Implemented | Product management |
| `/Product/ManagementV2` | Product/ManagementV2.cshtml | `5_2_ProductTypes (2).html` | ✅ Implemented | Alternative product management |
| `/Product/Categories` | Product/Categories.cshtml | `5_2_ProductTypes.html` | ✅ Implemented | Product categories view |

---

## Admin Only Pages

### User & Role Management

| Route | MVC View | Mockup Reference | Status | Notes |
|-------|----------|------------------|---------|-------|
| `/Admin/UserManagement` | Admin/UserManagement.cshtml | `7_1_1_UserManagement.html` | ✅ Implemented | User management interface |
| `/Admin/CreateUser` | Admin/CreateUser.cshtml | `7_1_1_UserManagement (2).html` | ✅ Implemented | Create new user |
| `/Admin/RoleManagement` | Admin/RoleManagement.cshtml | `7_1_RoleManagement.html` | ✅ Implemented | Role assignment management |

### System Configuration

| Route | MVC View | Mockup Reference | Status | Notes |
|-------|----------|------------------|---------|-------|
| `/Admin/CertificateTypes` | Admin/CertificateTypes.cshtml | `7_4_1_CertificateTypes.html` | ✅ Implemented | Certificate type management |
| `/Admin/ProductTypes` | Admin/ProductTypes.cshtml | `7_4_1_ProductTypes_Cascade.html` | ✅ Implemented | Product type management with cascading |
| `/Admin/ProductManagement` | Admin/ProductManagement.cshtml | `7_4_3_ProductManagement.html` | ✅ Implemented | Advanced product management |
| `/Admin/CertificateFields` | Admin/CertificateFields.cshtml | `7_5_1_CertificateFieldManagement.html` | ✅ Implemented | Certificate field configuration |
| `/Admin/CSFAttributes` | Admin/CSFAttributes.cshtml | `7_5_2_CSFAttributes.html` | ✅ Implemented | CSF attribute management |
| `/Admin/Tags` | Admin/Tags.cshtml | `7_5_3_TagManagement.html` | ✅ Implemented | Tag management system |
| `/Admin/Partners` | Admin/Partners.cshtml | `7_5_4_Partners.html` | ✅ Implemented | Partner management |

### Analytics & API Management

| Route | MVC View | Mockup Reference | Status | Notes |
|-------|----------|------------------|---------|-------|
| `/Admin/GoogleAnalytics` | Admin/GoogleAnalytics.cshtml | `7_2_2_GoogleAnalytics.html` | ✅ Implemented | Google Analytics integration |
| `/Admin/GCCAPI` | Admin/GCCAPI.cshtml | `7_3_APIManagement.html` | ✅ Implemented | API management interface |

**Alternative API Management Design:**
- `7_3_APIManagement (2).html` - Alternative API management layout

### Two-Factor Authentication

| Route | MVC View | Mockup Reference | Status | Notes |
|-------|----------|------------------|---------|-------|
| `/Account/TwoFactorPreferences` | Account/TwoFactorPreferences.cshtml | No specific mockup | ✅ Implemented | 2FA preferences |
| `/Account/SetupTotp` | Account/SetupTotp.cshtml | No specific mockup | ✅ Implemented | TOTP setup |
| `/Account/2FAAuthenticate` | Account/2FAAuthenticate.cshtml | No specific mockup | ✅ Implemented | 2FA authentication |

---

## API Endpoints (No Direct Mockups)

### Public API
- `GET /api/v1/certificates` - List certificates (paginated)
- `GET /api/v1/certificates/{id}` - Get certificate details
- `GET /api/v1/products` - List products
- `GET /api/v1/companies` - List companies

### External Partner API
- `GET /api/CertificateTileExternal/tiles` - Partner-specific certificates
- `GET /api/CertificateTileExternal/suggestions` - Search suggestions
- `GET /api/CertificateTileExternal/filters` - Available filters
- `GET /api/CertificateTileExternal/certificate/{id}` - Specific certificate
- `POST /api/CertificateTileExternal/export` - Export partner data

### Authenticated API
- `POST /api/v1/certificates` - Create certificate
- `PUT /api/v1/certificates/{id}` - Update certificate
- `DELETE /api/v1/certificates/{id}` - Delete certificate
- `POST /api/v1/apikeys` - Create API key
- `DELETE /api/v1/apikeys/{id}` - Revoke API key

---

## Implementation Status Summary

### ✅ Fully Implemented (Converted from Mockup to MVC)
- **Authentication System**: All login/signup flows
- **Certificate Views**: Public, External, Internal tile and detail views
- **Dashboard**: Analytics and reporting
- **Admin Panel**: Complete user, role, and system management
- **API Integration**: External partner API ready for Amazon integration

### 🔧 Implementation Notes
1. **Design Consistency**: All MVC views follow the Master Design Guide (`0_0_MasterDesignGuide.html`)
2. **Accessibility**: Minimum 16px font size maintained throughout
3. **No Search Bar in Navigation**: As per requirements, search functionality is embedded in specific pages
4. **Partner Branding**: Phase 1 does not include partner-specific branding
5. **JavaScript Rendering**: All pages tested to ensure proper JavaScript functionality

### 📋 Key Design Elements from Mockups Applied
- **Color Scheme**: Green (#92C83E), Blue (#00AEEF), Navy (#002876), Cream (#F5F2E8)
- **Typography**: Consistent font sizing and hierarchy
- **Component Design**: Buttons, forms, tables, cards, and modals
- **Responsive Design**: Mobile-first approach with breakpoints
- **Accessibility**: Focus states, ARIA labels, keyboard navigation

### 🎯 MVC-Specific Enhancements
- **Server-Side Rendering**: All mockup components converted to Razor views
- **Model Binding**: ViewModels created for all forms and data display
- **Security**: CSRF protection, role-based authorization
- **Performance**: Lazy loading, caching, and optimized queries
- **Validation**: Client-side and server-side validation for all forms

---

## Mockup File Reference Index

### By Functional Area

#### Authentication & Access Control
- `0_1_Loginscreens.html` - Login, signup, password reset flows

#### Certificate Management
- `1_0_CertificateViewTile.html` - Main certificate tile view (public)
- `1_1_CertificateSingleView.html` - Certificate detail view
- `1_1_CertificateExternalSingleView.html` - External partner certificate view
- `3_0_CertificateExternal.html` - External partner tile view
- `3_1_CertificateInternalSingleView.html` - Internal certificate details
- `3_2_Certificate_GCC (3).html` - Internal certificate list
- `3_3_Certificate_GCC_Add.html` - Add certificate form
- `3_4_Certificate_GCC_Edit.html` - Edit certificate form

#### Dashboard & Analytics
- `2_0_Dashboard.html` - Main analytics dashboard
- `7_2_2_GoogleAnalytics.html` - Google Analytics integration

#### Company & Product Management
- `5_1_Company.html` - Company management
- `5_2_ProductTypes.html` - Product type management

#### Administration
- `7_1_1_UserManagement.html` - User management interface
- `7_1_RoleManagement.html` - Role management
- `7_3_APIManagement.html` - API management
- `7_4_1_CertificateTypes.html` - Certificate type configuration
- `7_4_3_ProductManagement.html` - Advanced product management
- `7_5_1_CertificateFieldManagement.html` - Certificate field management
- `7_5_2_CSFAttributes.html` - CSF attribute management
- `7_5_3_TagManagement.html` - Tag management
- `7_5_4_Partners.html` - Partner management

#### Design System
- `0_0_MasterDesignGuide.html` - Complete design system and components
- `0_0_NotificationAlertsFinal.html` - Notification and alert patterns

---

## Conversion Quality Checklist

### ✅ Successfully Converted Elements
- [x] **Layout Structure**: Header, navigation, content areas, footer
- [x] **Color Palette**: All mockup colors applied consistently
- [x] **Typography**: Font families, sizes, weights, and hierarchy
- [x] **Components**: Buttons, forms, tables, cards, modals, dropdowns
- [x] **Icons**: FontAwesome icons matching mockup designs
- [x] **Responsive Behavior**: Mobile-first responsive breakpoints
- [x] **Interactive Elements**: Hover states, focus indicators, active states
- [x] **Form Validation**: Visual feedback for validation states
- [x] **Data Tables**: Sorting, filtering, pagination functionality
- [x] **Navigation**: Role-based menu items and access control

### 🎯 MVC-Specific Additions (Not in Original Mockups)
- [x] **CSRF Protection**: Anti-forgery tokens in all forms
- [x] **Model Validation**: Server-side and client-side validation
- [x] **Session Management**: User session state and role management
- [x] **API Integration**: RESTful API endpoints for external access
- [x] **Security Headers**: Production-ready security middleware
- [x] **Performance Optimization**: Caching, lazy loading, minification
- [x] **Logging**: Comprehensive application logging
- [x] **Error Handling**: Custom error pages and exception handling

---

## Next Phase Considerations

### Phase 2 Potential Enhancements (Beyond Current Mockups)
- **Partner-Specific Branding**: Custom themes for Amazon, Wayfair partners
- **Advanced Search**: Elasticsearch integration for complex queries
- **Real-Time Updates**: SignalR for live certificate status updates
- **Mobile App**: Native mobile application
- **Advanced Analytics**: Custom reporting and business intelligence
- **Workflow Automation**: Automated certificate lifecycle management

### Maintenance Notes
- **Mockup Sync**: When updating designs, corresponding mockup files should be updated first
- **Design System**: All changes should follow the Master Design Guide
- **Testing**: UI changes require corresponding test updates
- **Documentation**: This mapping should be updated when new pages are added

---

*Last Updated: September 17, 2025*
*Document Purpose: Development reference for maintaining design consistency between HTML mockups and MVC implementation*