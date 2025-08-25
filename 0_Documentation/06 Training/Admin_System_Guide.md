# Admin System Guide

## Overview

The GCC Admin System provides comprehensive administrative functionality for managing all aspects of the GreenCircle Certified Database Application. All admin functions are secured with role-based authentication and connected to the live database.

## Access Requirements

- **URL**: `/Admin`
- **Required Role**: Admin
- **Authentication**: Azure AD B2C or fallback cookie authentication

## Admin Dashboard

The admin dashboard provides real-time statistics and quick access to all administrative functions.

### Key Metrics
- Total Users (with active user count)
- Total Companies
- Total Certificates (with pending count)
- Total Products
- Recent Activity Feed
- System Status Indicators

### Quick Actions
Direct navigation to all admin modules from the dashboard.

## Admin Modules

### 1. User Management (`/Admin/UserManagement`)

**Features:**
- Create new users with role assignment
- Edit user details and permissions
- Manage company associations
- Password reset functionality
- Bulk import/export users
- Soft delete with audit trail

**API Endpoints:**
- `GET /Admin/GetUsers` - List all users
- `POST /Admin/CreateUser` - Create new user
- `PUT /Admin/UpdateUser/{id}` - Update user
- `DELETE /Admin/DeleteUser/{id}` - Delete user

### 2. Role Management (`/Admin/RoleManagement`)

**Features:**
- Create and manage roles
- Assign permissions to roles
- View role user counts
- System role protection

**Available Permissions:**
- `view-certificates` - View certificate data
- `edit-certificates` - Edit certificates
- `delete-certificates` - Delete certificates
- `export-certificates` - Export certificate data
- `manage-users` - User management access
- `manage-companies` - Company management
- `view-analytics` - Analytics access
- `admin-access` - Full admin privileges

### 3. Product Types (`/Admin/ProductTypes`)

**Features:**
- Hierarchical product type management
- Parent-child relationships
- Sort order configuration
- Bulk operations

**Structure:**
```
Construction & Building Materials (Level 1)
├── Adhesives & Sealants (Level 2)
├── Building Materials (Level 2)
└── [Subcategories] (Level 3)

Paper & Packaging (Level 1)
├── Corrugated Products (Level 2)
└── [Subcategories] (Level 3)
```

### 4. Certificate Types (`/Admin/CertificateTypes`)

**Features:**
- Manage certification types
- Configure required fields
- Set validation rules
- Amazon integration codes

**Common Types:**
- Environmental Product Declaration (EPD)
- GREENGUARD Certified
- Cradle to Cradle
- ISO Certifications

### 5. Certificate Fields (`/Admin/CertificateFields`)

**Features:**
- Dynamic field configuration
- Field type management (text, date, select, etc.)
- Validation rules
- Visibility settings
- Sort order control

**Field Types:**
- Text
- Number
- Date
- Boolean
- Select (with options)
- Multi-select

### 6. Tags Management (`/Admin/Tags`)

**Features:**
- Internal/External tag classification
- Product association tracking
- Bulk tag operations

**Tag Types:**
- **Internal**: For internal classification and reporting
- **External**: Customer-facing tags for filtering

### 7. Partners (`/Admin/Partners`)

**Features:**
- Partner company management
- User association tracking
- Contact information
- Active/inactive status

### 8. Google Analytics (`/Admin/GoogleAnalytics`)

**Features:**
- Configure tracking ID
- Enable/disable tracking
- Event tracking settings
- Conversion tracking

### 9. Amazon API (`/Admin/AmazonAPI`)

**Features:**
- API credentials management
- Sync configuration
- Last sync status
- Marketplace settings

## Data Export

All admin modules support data export with the following formats:
- CSV (default)
- Excel (coming soon)

**Export Endpoint:**
`GET /Admin/ExportData?entityType={type}&format=csv`

**Available Entity Types:**
- users
- roles
- products
- certificates
- companies
- tags

## Security Features

### Authentication
- Azure AD B2C integration
- Fallback cookie authentication
- Session-based role management
- Automatic timeout (30 minutes)

### Authorization
- Policy-based access control
- `[Authorize(Policy = "AdminOnly")]` protection
- Permission-based operations
- Audit logging for all changes

### Password Security
- BCrypt.Net-Next hashing
- Minimum 8 characters
- Password reset enforcement
- Failed login tracking
- Account lockout after failed attempts

## Database Integration

### Service Architecture
```
AdminController
    ↓
IAdminService (Interface)
    ↓
AdminService (Implementation)
    ↓
ApplicationDbContext
    ↓
SQLite Database
```

### Key Services
- **IAdminService**: Admin operations interface
- **AdminService**: Database operations implementation
- **ApplicationDbContext**: Entity Framework context

## Technical Implementation

### Models
- `UserViewModel` - User data transfer
- `RoleViewModel` - Role management
- `ProductTypeViewModel` - Product type hierarchy
- `CertificateTypeViewModel` - Certificate types
- `AdminDashboardViewModel` - Dashboard statistics

### Database Tables
- `Users` - User accounts
- `Roles` - Role definitions
- `Permissions` - Permission definitions
- `UserRoles` - User-role mappings
- `RolePermissions` - Role-permission mappings

### Recent Enhancements (Latest Update)

1. **Complete Database Integration**
   - Replaced all mock data with real database operations
   - Added async/await patterns throughout
   - Implemented proper error handling

2. **Enhanced Models**
   - Added soft delete support (`IsDeleted`, `DeletedAt`)
   - Added audit fields (`CreatedAt`, `UpdatedAt`)
   - Added full name support for users

3. **Service Layer**
   - Created `IAdminService` interface
   - Implemented comprehensive `AdminService`
   - Added dependency injection registration

4. **Security Updates**
   - Added BCrypt password hashing
   - Implemented authorization policies
   - Added admin-only access control

## Troubleshooting

### Common Issues

1. **Access Denied**
   - Verify user has Admin role
   - Check authentication status
   - Confirm policy configuration

2. **Data Not Loading**
   - Check database connection
   - Verify service registration
   - Review browser console for errors

3. **Export Failures**
   - Confirm entity type is valid
   - Check user permissions
   - Verify data exists

### Debug Endpoints
- `/Admin/GetUsers` - Test user data retrieval
- `/Admin/GetRoles` - Test role configuration
- `/Admin/GetPermissions` - View all permissions

## Best Practices

1. **User Management**
   - Always use soft delete for users
   - Maintain audit trail
   - Regular permission audits

2. **Role Configuration**
   - Follow principle of least privilege
   - Document custom roles
   - Regular role reviews

3. **Data Management**
   - Regular backups before bulk operations
   - Test in development first
   - Monitor system logs

## Support

For additional support:
- Review application logs in `/logs`
- Check browser developer console
- Contact system administrator
- Review [Troubleshooting Guide](./Troubleshooting_Guide.md)