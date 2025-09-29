# GCC Database Solution - Administrator Guide

## Overview
This guide provides comprehensive instructions for administrators managing the GCC Database Solution. Administrators have full access to all system features including user management, role assignment, and system configuration.

## Login and Access
- **URL**: http://localhost:5101/Account/Login
- **Default Credentials**: admin@greencirclecertified.com / Admin123!
- **Role**: Admin (full system access)

## Core Administrative Functions

### User Management
Navigate to `/Admin/UserManagement` to manage system users.

#### Create User
1. Click "Create New User"
2. Fill in required information:
   - Email address (must be unique)
   - Full name
   - Role assignment (Admin, Analyst, External)
3. Set initial password
4. Save user

#### Manage User Permissions
1. Select user from user list
2. Click "Edit"
3. Modify role assignments:
   - **Admin**: Full system access including user management
   - **Analyst**: Certificate management and reporting
   - **External**: Limited access to assigned certificates
4. Update password if needed
5. Save changes

#### Reset Password
1. Locate user in user management
2. Click "Reset Password"
3. Generate new temporary password
4. Communicate new password securely to user

### Role Management
Navigate to `/Admin/RoleManagement` to configure system roles and permissions.

#### Configure Role Permissions
1. Select role to modify
2. Review current permissions
3. Add/remove specific permissions:
   - Certificate creation and editing
   - Company management
   - Report generation
   - User management (Admin only)
4. Save role configuration

### Certificate Management
Administrators have full access to all certificates in the system.

#### Upload Certificate
1. Navigate to `/Certificate/Create`
2. Fill in certificate details:
   - Certificate number
   - Company information
   - Product details
   - Expiration date
3. Upload certificate PDF file
4. Add product IDs (ASINs, UPCs, etc.)
5. Save certificate

#### Search Certificate
1. Use search functionality on main page
2. Filter by:
   - Certificate number
   - Company name
   - Product type
   - Date range
3. Export search results if needed

#### Manage Company
1. Navigate to company management section
2. Add new companies or edit existing
3. Update company information:
   - Name and contact details
   - Address information
   - Primary contact person
4. Associate certificates with companies

### Data Management

#### Export Data
1. Navigate to certificate list view
2. Apply any necessary filters
3. Click "Export" button
4. Choose export format:
   - CSV for data analysis
   - Excel for reporting
   - PDF for printing
5. Download exported file

#### Generate Report
1. Access reporting section from dashboard
2. Select report type:
   - Certificate summary
   - Expiration reports
   - Company reports
   - Activity reports
3. Set date range and filters
4. Generate and download report

## System Configuration

### API Management
Configure API access for external partners.

#### API Key Management
1. Navigate to API configuration
2. Generate new API keys for partners
3. Set rate limits (default: 1000 requests/hour)
4. Configure access permissions per key
5. Monitor API usage

#### Partner Configuration
1. Set up partner-specific access
2. Configure Amazon integration settings
3. Set Wayfair access parameters
4. Test partner connections

### Security Settings

#### Password Policies
- Minimum 8 characters
- Must include uppercase, lowercase, number
- Special character recommended
- Password expiration: 90 days

#### Session Management
- Session timeout: 30 minutes of inactivity
- Force logout on browser close
- Multi-session handling

### Backup and Maintenance

#### Database Backup
1. Schedule regular backups (daily recommended)
2. Test backup restoration procedures
3. Monitor database performance
4. Review logs for errors

#### System Updates
1. Apply security patches promptly
2. Test updates in staging environment
3. Notify users of scheduled maintenance
4. Document changes in changelog

## Troubleshooting

### Common Issues

#### User Cannot Login
1. Verify user account exists and is active
2. Check password hasn't expired
3. Verify role assignments are correct
4. Clear browser cache/cookies

#### Certificate Upload Fails
1. Check file size limits (10MB max)
2. Verify PDF format is valid
3. Ensure all required fields are completed
4. Check disk space availability

#### API Access Problems
1. Verify API key is valid and active
2. Check rate limit status
3. Review API endpoint configuration
4. Monitor API logs for errors

### Support Contacts
- **Technical Support**: IT Department
- **System Issues**: System Administrator
- **User Training**: Training Coordinator

## Best Practices

### Security
- Regularly review user accounts and remove inactive users
- Monitor failed login attempts
- Keep system updated with latest security patches
- Use strong passwords and enable two-factor authentication

### Data Management
- Perform regular data backups
- Monitor certificate expiration dates
- Maintain accurate company information
- Regular cleanup of old/invalid data

### User Training
- Provide role-specific training for new users
- Update training materials when system changes
- Regular refresher training sessions
- Maintain user documentation

---

*Last Updated: September 2025*
*Version: 1.0*