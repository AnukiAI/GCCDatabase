# GCC Web Application Routing Requirements

## Overview
This document outlines the routing and navigation requirements for different user roles in the GCC Web Application.

## User Role-Based Routing

### Landing Page Behavior (Home/Index Route)

The application automatically redirects users to the appropriate landing page based on their authentication status and role:

| User Type | Authentication Status | Landing Page | Route |
|-----------|---------------------|--------------|-------|
| Public User | Not Authenticated | Certificate Tile View (Public) | `/Certificate/CertificateTileView` |
| External User | Authenticated | Certificate Tile View (External) | `/Certificate/CertificateTileViewExternal` |
| Analyst | Authenticated | Dashboard | `/Dashboard/Index` |
| Admin | Authenticated | Dashboard | `/Dashboard/Index` |

### Navigation Menu Items by Role

#### Public Users (Not Authenticated)
- **Certificates** → `/Certificate/CertificateTileView`
- Sign In option in user dropdown

#### External Users
- **Certificates** → `/Certificate/CertificateTileViewExternal`
- User profile options in dropdown

#### Analyst Users
- **Dashboard** → `/Dashboard/Index`
- **Certificates** → `/Certificate/CertificateTileViewInternal`
- **Companies & Products** (dropdown)
  - Companies → `/Company/Index`
  - Products → `/Product/Management`
- User profile options in dropdown

#### Admin Users
- All Analyst menu items, plus:
- **Administration** (dropdown) with various admin tools
- Additional admin-specific functionality

## Certificate Views

### Three Different Certificate Views

1. **CertificateTileView** (Public)
   - Accessible without authentication
   - Limited functionality
   - Read-only view of public certificates

2. **CertificateTileViewExternal** (External Users)
   - Requires authentication with External role
   - Extended view with additional certificate details
   - Export capabilities for authorized certificates

3. **CertificateTileViewInternal** (Analyst/Admin)
   - Requires authentication with Analyst or Admin role
   - Full functionality including:
     - Add/Edit certificates
     - Bulk operations
     - Advanced filtering
     - Full export capabilities

## Implementation Details

### HomeController.cs
The `Index` action in `HomeController` handles the initial routing logic:
- Checks authentication status
- Retrieves user role from session
- Redirects to appropriate landing page

### Layout Navigation
The `_Layout.cshtml` uses role-based CSS classes to show/hide navigation items:
- `role-public-show` - Visible to public users
- `role-external-show` - Visible to external users
- `role-analyst-show` - Visible to analyst users
- `role-admin-show` - Visible to admin users

### Breadcrumbs
Breadcrumbs are only visible to Admin users (`role-admin-show` class applied to breadcrumb container).

## Security Considerations

1. **Public Access**: The `/Certificate/CertificateTileView` route must remain accessible without authentication
2. **Role Verification**: Always verify user roles on the server side, not just client-side visibility
3. **Session Management**: User roles are stored in session and should be validated on each request
4. **Authorization Policies**: Use ASP.NET Core authorization policies for protecting sensitive routes

## Testing Checklist

- [ ] Verify public users can access CertificateTileView without login
- [ ] Verify public users are redirected to CertificateTileView when accessing home page
- [ ] Verify External users see CertificateTileViewExternal after login
- [ ] Verify Analyst users see Dashboard after login
- [ ] Verify Admin users see Dashboard after login
- [ ] Verify navigation menu shows correct items for each role
- [ ] Verify breadcrumbs only appear for Admin users
- [ ] Verify unauthorized access attempts are properly rejected